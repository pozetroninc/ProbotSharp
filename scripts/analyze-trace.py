#!/usr/bin/env python3
"""
Analyze .NET performance traces in Speedscope format.

Extracts key metrics from trace.speedscope.json:
- Total samples and CPU time
- Top hotspot methods
- Thread activity
- GC and allocation patterns

Usage:
    python3 analyze-trace.py trace.speedscope.json [--output metrics.json] [--markdown summary.md]
"""

import json
import sys
import argparse
from collections import Counter
from datetime import datetime
from pathlib import Path


def parse_speedscope(trace_path):
    """Parse Speedscope JSON format and extract metrics."""
    with open(trace_path, 'r') as f:
        data = json.load(f)

    frames = data.get('shared', {}).get('frames', [])
    profiles = data.get('profiles', [])

    if not frames or not profiles:
        return {
            'error': 'Empty trace file - no frames or profiles found',
            'total_samples': 0,
            'cpu_time_ms': 0,
            'top_methods': [],
            'thread_count': 0
        }

    # Calculate total samples across all profiles
    total_samples = 0
    frame_samples = Counter()

    for profile in profiles:
        samples = profile.get('samples', [])
        weights = profile.get('weights', [])

        # Count samples per frame
        for i, frame_idx in enumerate(samples):
            weight = weights[i] if i < len(weights) else 1
            total_samples += weight
            frame_samples[frame_idx] += weight

    # Estimate CPU time (1ms per sample is typical for dotnet-trace cpu-sampling)
    cpu_time_ms = total_samples * 1.0

    # Get top hotspot methods (frames with most samples)
    top_frames = frame_samples.most_common(10)
    top_methods = []

    for frame_idx, sample_count in top_frames:
        if frame_idx < len(frames):
            frame = frames[frame_idx]
            frame_name = frame.get('name', 'Unknown')

            # Clean up frame name for readability
            if '!' in frame_name:
                # Format: "Assembly!Namespace.Class.Method"
                parts = frame_name.split('!')
                if len(parts) > 1:
                    method_path = parts[1]
                else:
                    method_path = frame_name
            else:
                method_path = frame_name

            # Calculate percentage
            percentage = (sample_count / total_samples * 100) if total_samples > 0 else 0

            top_methods.append({
                'method': method_path,
                'samples': sample_count,
                'cpu_time_ms': sample_count * 1.0,
                'percentage': round(percentage, 2)
            })

    # Analyze threads
    thread_count = len(profiles)

    # Look for GC and allocation patterns
    gc_samples = 0
    alloc_samples = 0

    for frame_idx, count in frame_samples.items():
        if frame_idx < len(frames):
            frame_name = frames[frame_idx].get('name', '').lower()
            if 'gc' in frame_name or 'garbage' in frame_name:
                gc_samples += count
            if 'alloc' in frame_name:
                alloc_samples += count

    gc_percentage = (gc_samples / total_samples * 100) if total_samples > 0 else 0
    alloc_percentage = (alloc_samples / total_samples * 100) if total_samples > 0 else 0

    return {
        'total_samples': total_samples,
        'cpu_time_ms': round(cpu_time_ms, 2),
        'top_methods': top_methods,
        'thread_count': thread_count,
        'gc_samples': gc_samples,
        'gc_percentage': round(gc_percentage, 2),
        'alloc_samples': alloc_samples,
        'alloc_percentage': round(alloc_percentage, 2),
        'timestamp': datetime.utcnow().isoformat() + 'Z'
    }


def generate_markdown(metrics, baseline=None):
    """Generate markdown summary of metrics."""
    lines = ['# Performance Trace Analysis\n']

    if 'error' in metrics:
        lines.append(f"**Error:** {metrics['error']}\n")
        return '\n'.join(lines)

    # Summary stats
    lines.append('## Summary\n')
    lines.append(f"- **Total Samples:** {metrics['total_samples']:,}")
    lines.append(f"- **Estimated CPU Time:** {metrics['cpu_time_ms']:.2f} ms")
    lines.append(f"- **Thread Count:** {metrics['thread_count']}")
    lines.append(f"- **GC Activity:** {metrics['gc_percentage']:.2f}% of samples")
    lines.append('')

    # Baseline comparison
    if baseline and 'total_samples' in baseline and baseline['total_samples'] > 0:
        lines.append('## Comparison to Baseline (main)\n')

        cpu_diff = metrics['cpu_time_ms'] - baseline.get('cpu_time_ms', 0)
        cpu_pct = (cpu_diff / baseline.get('cpu_time_ms', 1)) * 100

        if cpu_diff > 0:
            emoji = '🔴' if cpu_pct > 10 else '🟡'
            lines.append(f"{emoji} **CPU Time:** +{cpu_diff:.2f} ms (+{cpu_pct:.1f}%)")
        elif cpu_diff < 0:
            emoji = '🟢'
            lines.append(f"{emoji} **CPU Time:** {cpu_diff:.2f} ms ({cpu_pct:.1f}%)")
        else:
            lines.append(f"⚪ **CPU Time:** No change")

        lines.append('')

    # Top hotspots
    lines.append('## Top 10 Hotspot Methods\n')
    lines.append('| Method | Samples | CPU Time | % |')
    lines.append('|--------|---------|----------|---|')

    for method in metrics['top_methods']:
        method_name = method['method']
        # Truncate very long method names
        if len(method_name) > 80:
            method_name = method_name[:77] + '...'

        lines.append(f"| `{method_name}` | {method['samples']:,} | {method['cpu_time_ms']:.1f} ms | {method['percentage']:.1f}% |")

    lines.append('')

    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description='Analyze .NET performance traces')
    parser.add_argument('trace_file', help='Path to trace.speedscope.json')
    parser.add_argument('--output', '-o', help='Output metrics to JSON file')
    parser.add_argument('--markdown', '-m', help='Output summary to markdown file')
    parser.add_argument('--baseline', '-b', help='Baseline metrics JSON for comparison')

    args = parser.parse_args()

    # Parse trace
    metrics = parse_speedscope(args.trace_file)

    # Load baseline if provided
    baseline = None
    if args.baseline and Path(args.baseline).exists():
        with open(args.baseline, 'r') as f:
            baseline = json.load(f)

    # Output JSON
    if args.output:
        with open(args.output, 'w') as f:
            json.dump(metrics, f, indent=2)
        print(f"Metrics written to {args.output}")
    else:
        print(json.dumps(metrics, indent=2))

    # Output markdown
    if args.markdown:
        markdown = generate_markdown(metrics, baseline)
        with open(args.markdown, 'w') as f:
            f.write(markdown)
        print(f"Summary written to {args.markdown}")


if __name__ == '__main__':
    main()

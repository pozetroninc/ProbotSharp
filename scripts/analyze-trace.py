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
from datetime import datetime, timezone
from pathlib import Path


def format_duration(ms):
    """Convert milliseconds to human-readable format.

    Examples:
        125 ms -> "125 ms"
        2500 ms -> "2.5 sec"
        65000 ms -> "1.1 min"
        2472055 ms -> "41.2 min"
    """
    if ms < 1000:
        return f"{ms:.0f} ms"
    elif ms < 60000:
        return f"{ms / 1000:.1f} sec"
    elif ms < 3600000:
        return f"{ms / 60000:.1f} min"
    else:
        return f"{ms / 3600000:.1f} hours"


def format_percentage_bar(percentage, width=20):
    """Create visual progress bar for percentages.

    Example:
        40.0 -> "████████░░░░░░░░░░░░ 40.0%"
    """
    filled = int((percentage / 100.0) * width)
    empty = width - filled
    bar = '█' * filled + '░' * empty
    return f"{bar} {percentage:.1f}%"


def categorize_method(method_name):
    """Categorize method as app code, framework, or noise.

    Returns:
        'app': ProbotSharp application code
        'framework': .NET framework code (potentially interesting)
        'noise': Threading/wait methods, meta-frames (not actionable)
    """
    method_lower = method_name.lower()

    # Noise: Thread management, meta-frames, unmanaged code
    noise_patterns = [
        'threads', 'non-activities', 'process64', 'unmanaged_code_time',
        'threading.portablethreadpool', 'threading.lowlevellifosemaphore',
        'threading.semaphoreslim', 'threading.monitor.wait',
        'threading.manualreseteventslim', 'threading.thread.sleep',
        'waithandle', 'taskawaiter'
    ]

    if any(pattern in method_lower for pattern in noise_patterns):
        return 'noise'

    # App code: ProbotSharp namespaces
    if 'probotsharp' in method_lower:
        return 'app'

    # Everything else is framework (might be interesting)
    return 'framework'


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

    # Calculate total samples/time across all profiles
    # Speedscope supports two profile types: 'sampled' and 'evented'
    total_samples = 0
    frame_samples = Counter()

    for profile in profiles:
        profile_type = profile.get('type', 'sampled')

        if profile_type == 'sampled':
            # Sampled format: samples and weights arrays
            samples = profile.get('samples', [])
            weights = profile.get('weights', [])

            # Count samples per frame
            for i, frame_idx in enumerate(samples):
                weight = weights[i] if i < len(weights) else 1
                total_samples += weight
                frame_samples[frame_idx] += weight

        elif profile_type == 'evented':
            # Evented format: open/close events with timestamps
            events = profile.get('events', [])
            start_value = profile.get('startValue', 0)
            end_value = profile.get('endValue', 0)

            # Track stack and calculate time for each frame
            stack = []  # Stack of (frame_idx, open_time)

            for event in events:
                event_type = event.get('type')
                frame_idx = event.get('frame')
                timestamp = event.get('at')

                if event_type == 'O':  # Open frame
                    stack.append((frame_idx, timestamp))
                elif event_type == 'C':  # Close frame
                    # Match with most recent open of same frame
                    if stack and stack[-1][0] == frame_idx:
                        _, open_time = stack.pop()
                        duration = timestamp - open_time
                        # Convert duration to samples (treat 1ms = 1 sample for consistency)
                        frame_samples[frame_idx] += duration
                        total_samples += duration

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
                'percentage': round(percentage, 2),
                'category': categorize_method(method_path)
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
        'timestamp': datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')
    }


def generate_markdown(metrics, baseline=None):
    """Generate user-friendly markdown summary of metrics."""
    lines = []

    if 'error' in metrics:
        lines.append('# Performance Trace Analysis\n')
        lines.append(f"**Error:** {metrics['error']}\n")
        return '\n'.join(lines)

    # ===== VERDICT & SUMMARY =====
    lines.append('## 📊 Performance Summary\n')

    # Determine verdict based on baseline comparison
    verdict_emoji = '✅'
    verdict_text = 'No significant performance impact detected'

    if baseline and 'cpu_time_ms' in baseline and baseline['cpu_time_ms'] > 0:
        cpu_diff = metrics['cpu_time_ms'] - baseline['cpu_time_ms']
        cpu_pct = (cpu_diff / baseline['cpu_time_ms']) * 100

        if cpu_pct > 15:
            verdict_emoji = '🔴'
            verdict_text = f'Performance regression detected (+{cpu_pct:.1f}%)'
        elif cpu_pct > 5:
            verdict_emoji = '⚠️'
            verdict_text = f'Minor performance impact (+{cpu_pct:.1f}%)'
        elif cpu_pct < -5:
            verdict_emoji = '🚀'
            verdict_text = f'Performance improvement detected ({cpu_pct:.1f}%)'
        else:
            verdict_emoji = '✅'
            verdict_text = f'Performance within normal variance ({cpu_pct:+.1f}%)'

    lines.append(f"**Verdict:** {verdict_emoji} {verdict_text}")
    lines.append(f"**Trace Duration:** {format_duration(metrics['cpu_time_ms'])}")
    lines.append(f"**Test Workload:** 40 webhook requests (20 issues, 20 pull requests)\n")
    lines.append('---\n')

    # ===== KEY METRICS TABLE =====
    lines.append('### Key Metrics\n')
    lines.append('| Metric | Value | Visual |')
    lines.append('|--------|-------|--------|')

    # CPU Time
    cpu_time_human = format_duration(metrics['cpu_time_ms'])
    cpu_bar = format_percentage_bar(100, width=20)
    lines.append(f"| Total CPU Time | {cpu_time_human} | `{cpu_bar}` |")

    # Thread Count
    lines.append(f"| Thread Count | {metrics['thread_count']} threads | |")

    # GC Activity
    gc_pct = metrics['gc_percentage']
    gc_bar = format_percentage_bar(min(gc_pct, 100), width=20)
    gc_status = '✅ Minimal' if gc_pct < 5 else ('⚠️ Moderate' if gc_pct < 15 else '🔴 High')
    lines.append(f"| GC Activity | {gc_pct:.2f}% {gc_status} | `{gc_bar}` |")

    lines.append('')

    # ===== BASELINE COMPARISON (if available) =====
    if baseline and 'cpu_time_ms' in baseline and baseline['cpu_time_ms'] > 0:
        lines.append('### 📈 Baseline Comparison\n')

        cpu_diff = metrics['cpu_time_ms'] - baseline['cpu_time_ms']
        cpu_pct = (cpu_diff / baseline['cpu_time_ms']) * 100

        if cpu_diff > 0:
            trend = '↑'
            emoji = '🔴' if cpu_pct > 15 else ('⚠️' if cpu_pct > 5 else '🟡')
            lines.append(f"{emoji} **CPU Time vs Baseline (main):** +{format_duration(abs(cpu_diff))} ({trend} +{cpu_pct:.1f}%)")
        elif cpu_diff < 0:
            trend = '↓'
            emoji = '🚀'
            lines.append(f"{emoji} **CPU Time vs Baseline (main):** -{format_duration(abs(cpu_diff))} ({trend} {cpu_pct:.1f}%)")
        else:
            trend = '→'
            lines.append(f"✅ **CPU Time vs Baseline (main):** No change ({trend})")

        # Add interpretation
        if abs(cpu_pct) < 5:
            lines.append('\n_Changes are within normal variance - no action needed._')
        elif cpu_pct > 15:
            lines.append('\n_Significant regression detected - consider investigating hotspots below._')

        lines.append('')

    lines.append('---\n')

    # ===== APPLICATION HOTSPOTS =====
    app_methods = [m for m in metrics['top_methods'] if m['category'] == 'app']

    if app_methods:
        lines.append('### 🔥 Application Hotspots\n')
        lines.append('_Top methods in ProbotSharp code consuming CPU:_\n')
        lines.append('| Method | CPU Time | Percentage |')
        lines.append('|--------|----------|------------|')

        for method in app_methods[:5]:  # Show top 5 app methods
            method_name = method['method']
            if len(method_name) > 60:
                method_name = method_name[:57] + '...'

            cpu_time = format_duration(method['cpu_time_ms'])
            pct_bar = format_percentage_bar(method['percentage'], width=15)
            lines.append(f"| `{method_name}` | {cpu_time} | `{pct_bar}` |")

        lines.append('')
    else:
        lines.append('### 🔥 Application Hotspots\n')
        lines.append('_No significant application hotspots detected. Most time spent in framework/wait operations._\n')

    # ===== FRAMEWORK METHODS (collapsible) =====
    framework_methods = [m for m in metrics['top_methods'] if m['category'] == 'framework']

    if framework_methods:
        lines.append('<details>')
        lines.append('<summary>📚 Framework Methods (click to expand)</summary>\n')
        lines.append('| Method | CPU Time | Percentage |')
        lines.append('|--------|----------|------------|')

        for method in framework_methods[:10]:
            method_name = method['method']
            if len(method_name) > 70:
                method_name = method_name[:67] + '...'

            cpu_time = format_duration(method['cpu_time_ms'])
            pct_bar = format_percentage_bar(method['percentage'], width=15)
            lines.append(f"| `{method_name}` | {cpu_time} | `{pct_bar}` |")

        lines.append('\n</details>\n')

    # ===== FULL DETAILS (collapsible) =====
    lines.append('<details>')
    lines.append('<summary>📋 Full Method Details (all categories)</summary>\n')
    lines.append('| Method | Category | CPU Time | Percentage |')
    lines.append('|--------|----------|----------|------------|')

    for method in metrics['top_methods']:
        method_name = method['method']
        if len(method_name) > 60:
            method_name = method_name[:57] + '...'

        category_badge = {'app': '🎯 App', 'framework': '📦 Framework', 'noise': '🔇 Noise'}
        category = category_badge.get(method['category'], method['category'])
        cpu_time = format_duration(method['cpu_time_ms'])
        pct_bar = format_percentage_bar(method['percentage'], width=12)
        lines.append(f"| `{method_name}` | {category} | {cpu_time} | `{pct_bar}` |")

    lines.append('\n</details>\n')

    # ===== TECHNICAL DETAILS (collapsible) =====
    lines.append('<details>')
    lines.append('<summary>🔍 Technical Details</summary>\n')
    lines.append(f"- **Total Samples:** {metrics['total_samples']:,.0f}")
    lines.append(f"- **CPU Time (raw):** {metrics['cpu_time_ms']:.2f} ms")
    lines.append(f"- **Sample Rate:** 1ms intervals")
    lines.append(f"- **Trace Format:** Speedscope evented")
    lines.append(f"- **GC Samples:** {metrics['gc_samples']:,.0f} ({metrics['gc_percentage']:.2f}%)")
    lines.append(f"- **Timestamp:** {metrics['timestamp']}")
    lines.append('\n</details>')

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

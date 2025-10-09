#!/usr/bin/env python3
"""
Verify GitHub Links in Markdown Files

This script scans all Markdown files in the repository and verifies that
GitHub links (github.com URLs) are accessible and return 200 OK responses.

Usage:
    python3 scripts/verify-github-links.py
    python3 scripts/verify-github-links.py --verbose
    python3 scripts/verify-github-links.py --exclude probot/
    python3 scripts/verify-github-links.py --no-skip-placeholders
"""

import argparse
import re
import sys
import time
from pathlib import Path
from typing import Dict, List, Set, Tuple
from urllib.parse import urlparse

try:
    import requests
except ImportError:
    print("Error: requests library not found. Install with: pip install requests")
    sys.exit(1)


# Placeholder patterns to skip by default (used in documentation examples)
PLACEHOLDER_PATTERNS = [
    'your-org',
    'your-repo',
    'your-bot',
    'yourusername',
    'yourname',
    'example.com',
    'example-org',
]


class Colors:
    """ANSI color codes for terminal output"""
    GREEN = '\033[92m'
    RED = '\033[91m'
    YELLOW = '\033[93m'
    BLUE = '\033[94m'
    BOLD = '\033[1m'
    RESET = '\033[0m'


def find_markdown_files(root_dir: Path, exclude_patterns: List[str]) -> List[Path]:
    """Find all Markdown files in the repository, excluding specified patterns."""
    markdown_files = []

    for md_file in root_dir.rglob("*.md"):
        # Check if file should be excluded
        relative_path = md_file.relative_to(root_dir)
        if any(pattern in str(relative_path) for pattern in exclude_patterns):
            continue
        markdown_files.append(md_file)

    return sorted(markdown_files)


def extract_github_links(content: str) -> List[Tuple[str, int]]:
    """
    Extract GitHub URLs from markdown content.
    Returns list of (url, line_number) tuples.
    """
    links = []

    # Pattern for markdown links: [text](url) and bare URLs
    markdown_link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'
    bare_url_pattern = r'(?:^|[\s(])(https?://github\.com/[^\s)<>"\',;]+)'

    lines = content.split('\n')

    for line_num, line in enumerate(lines, 1):
        # Find markdown-style links
        for match in re.finditer(markdown_link_pattern, line):
            url = match.group(2)
            if 'github.com' in url:
                links.append((url, line_num))

        # Find bare URLs
        for match in re.finditer(bare_url_pattern, line):
            url = match.group(1)
            links.append((url, line_num))

    return links


def normalize_github_url(url: str) -> str:
    """
    Normalize GitHub URLs for verification.
    Converts /blob/ URLs to raw.githubusercontent.com for better verification.
    """
    # Remove fragments and query strings for checking
    url = url.split('#')[0].split('?')[0]

    # For blob URLs, we'll use the GitHub web URL directly
    # GitHub returns 200 for valid blob URLs
    return url


def is_placeholder_url(url: str, patterns: List[str]) -> bool:
    """
    Check if a URL contains placeholder patterns.
    Returns True if the URL contains any of the placeholder patterns.
    """
    url_lower = url.lower()
    return any(pattern.lower() in url_lower for pattern in patterns)


def verify_url(url: str, timeout: int = 10) -> Tuple[bool, int, str]:
    """
    Verify that a URL is accessible.
    Returns (success, status_code, error_message).
    """
    try:
        # Add headers to avoid GitHub rate limiting
        headers = {
            'User-Agent': 'ProbotSharp-Link-Checker/1.0',
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        }

        response = requests.head(url, headers=headers, timeout=timeout, allow_redirects=True)

        # GitHub sometimes returns 403 for HEAD requests, try GET
        if response.status_code == 403:
            response = requests.get(url, headers=headers, timeout=timeout, allow_redirects=True)

        # Consider 200-399 as success
        if 200 <= response.status_code < 400:
            return (True, response.status_code, "")
        else:
            return (False, response.status_code, f"HTTP {response.status_code}")

    except requests.exceptions.Timeout:
        return (False, 0, "Timeout")
    except requests.exceptions.ConnectionError:
        return (False, 0, "Connection Error")
    except requests.exceptions.TooManyRedirects:
        return (False, 0, "Too Many Redirects")
    except Exception as e:
        return (False, 0, str(e))


def main():
    parser = argparse.ArgumentParser(
        description="Verify GitHub links in Markdown files",
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Show verbose output including successful links'
    )
    parser.add_argument(
        '--exclude',
        nargs='+',
        default=['node_modules/', '.git/', '.aidocs/'],
        help='Patterns to exclude from search (default: node_modules/ .git/ .aidocs/)'
    )
    parser.add_argument(
        '--timeout',
        type=int,
        default=10,
        help='Request timeout in seconds (default: 10)'
    )
    parser.add_argument(
        '--delay',
        type=float,
        default=0.5,
        help='Delay between requests in seconds (default: 0.5)'
    )
    parser.add_argument(
        '--no-skip-placeholders',
        action='store_true',
        help='Do not skip placeholder URLs (your-org, yourusername, etc.)'
    )

    args = parser.parse_args()

    # Get repository root (assume script is in scripts/)
    repo_root = Path(__file__).resolve().parent.parent

    print(f"{Colors.BOLD}GitHub Link Verification Tool{Colors.RESET}")
    print(f"Repository: {repo_root}")
    print(f"Excluded patterns: {', '.join(args.exclude)}")
    print()

    # Find all markdown files
    print(f"{Colors.BLUE}Scanning for Markdown files...{Colors.RESET}")
    markdown_files = find_markdown_files(repo_root, args.exclude)
    print(f"Found {len(markdown_files)} Markdown files")
    print()

    # Extract all GitHub links
    print(f"{Colors.BLUE}Extracting GitHub links...{Colors.RESET}")
    all_links: Dict[str, List[Tuple[Path, int]]] = {}  # url -> [(file, line_num), ...]

    for md_file in markdown_files:
        try:
            content = md_file.read_text(encoding='utf-8')
            links = extract_github_links(content)

            for url, line_num in links:
                normalized_url = normalize_github_url(url)
                if normalized_url not in all_links:
                    all_links[normalized_url] = []
                all_links[normalized_url].append((md_file, line_num))
        except Exception as e:
            print(f"{Colors.YELLOW}Warning: Could not read {md_file}: {e}{Colors.RESET}")

    unique_links = len(all_links)
    total_occurrences = sum(len(occurrences) for occurrences in all_links.values())

    print(f"Found {total_occurrences} GitHub link(s) ({unique_links} unique URLs)")
    print()

    if not all_links:
        print(f"{Colors.GREEN}No GitHub links found to verify.{Colors.RESET}")
        return 0

    # Separate placeholder URLs from real URLs
    skip_placeholders = not args.no_skip_placeholders
    placeholder_links: Dict[str, List[Tuple[Path, int]]] = {}
    real_links: Dict[str, List[Tuple[Path, int]]] = {}

    for url, occurrences in all_links.items():
        if skip_placeholders and is_placeholder_url(url, PLACEHOLDER_PATTERNS):
            placeholder_links[url] = occurrences
        else:
            real_links[url] = occurrences

    # Verify each real link
    print(f"{Colors.BLUE}Verifying links...{Colors.RESET}")
    if placeholder_links and skip_placeholders:
        print(f"{Colors.YELLOW}Skipping {len(placeholder_links)} placeholder URL(s){Colors.RESET}")
    print()

    failed_links: List[Tuple[str, int, str, List[Tuple[Path, int]]]] = []
    verified_count = 0
    links_to_verify = len(real_links)

    for i, (url, occurrences) in enumerate(sorted(real_links.items()), 1):
        if i > 1:
            time.sleep(args.delay)  # Rate limiting

        success, status_code, error_msg = verify_url(url, args.timeout)

        if success:
            verified_count += 1
            if args.verbose:
                print(f"{Colors.GREEN}✓{Colors.RESET} [{i}/{links_to_verify}] {url}")
                if len(occurrences) > 1:
                    print(f"  Found in {len(occurrences)} location(s)")
        else:
            failed_links.append((url, status_code, error_msg, occurrences))
            print(f"{Colors.RED}✗{Colors.RESET} [{i}/{links_to_verify}] {url}")
            print(f"  {Colors.RED}Error: {error_msg}{Colors.RESET}")

    # Print summary
    print()
    print(f"{Colors.BOLD}{'='*70}{Colors.RESET}")
    print(f"{Colors.BOLD}Summary{Colors.RESET}")
    print(f"{Colors.BOLD}{'='*70}{Colors.RESET}")
    print(f"Total unique URLs: {unique_links}")
    print(f"Total occurrences: {total_occurrences}")
    if placeholder_links and skip_placeholders:
        print(f"{Colors.YELLOW}Skipped (placeholders): {len(placeholder_links)}{Colors.RESET}")
    print(f"{Colors.GREEN}Verified: {verified_count}{Colors.RESET}")
    print(f"{Colors.RED}Failed: {len(failed_links)}{Colors.RESET}")
    print()

    # Print detailed failure report
    if failed_links:
        print(f"{Colors.BOLD}Failed Links:{Colors.RESET}")
        print()

        for url, status_code, error_msg, occurrences in failed_links:
            print(f"{Colors.RED}✗ {url}{Colors.RESET}")
            print(f"  Error: {error_msg}")
            print(f"  Found in:")
            for file_path, line_num in occurrences:
                rel_path = file_path.relative_to(repo_root)
                print(f"    - {rel_path}:{line_num}")
            print()

        # Show skipped placeholders if verbose
        if placeholder_links and skip_placeholders and args.verbose:
            print(f"{Colors.BOLD}Skipped Placeholder URLs:{Colors.RESET}")
            print()
            for url, occurrences in sorted(placeholder_links.items()):
                print(f"{Colors.YELLOW}⊘ {url}{Colors.RESET}")
                print(f"  Found in {len(occurrences)} location(s)")
            print()

        return 1
    else:
        print(f"{Colors.GREEN}✓ All GitHub links verified successfully!{Colors.RESET}")

        # Show skipped placeholders if verbose
        if placeholder_links and skip_placeholders and args.verbose:
            print()
            print(f"{Colors.BOLD}Skipped Placeholder URLs:{Colors.RESET}")
            print()
            for url, occurrences in sorted(placeholder_links.items()):
                print(f"{Colors.YELLOW}⊘ {url}{Colors.RESET}")
                print(f"  Found in {len(occurrences)} location(s)")
            print()

        return 0


if __name__ == "__main__":
    sys.exit(main())

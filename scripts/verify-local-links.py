#!/usr/bin/env python3
"""
Verify Local Links in Markdown Files

This script scans all Markdown files in the repository and verifies that:
- Local file references exist (relative paths)
- Intra-document anchors exist (headings in the same file)
- Cross-document anchors exist (headings in other files)
- Image references exist (local image files)

External HTTP/HTTPS links are ignored (handled by verify-github-links.py).

Usage:
    python3 scripts/verify-local-links.py
    python3 scripts/verify-local-links.py --verbose
    python3 scripts/verify-local-links.py --exclude probot/
"""

import argparse
import re
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple, Optional
from urllib.parse import unquote


class Colors:
    """ANSI color codes for terminal output"""
    GREEN = '\033[92m'
    RED = '\033[91m'
    YELLOW = '\033[93m'
    BLUE = '\033[94m'
    BOLD = '\033[1m'
    RESET = '\033[0m'


def github_slugify(text: str, existing_slugs: Dict[str, int]) -> str:
    """
    Convert text to a GitHub-compatible anchor slug.
    Matches GitHub's markdown rendering rules:
    - Convert to lowercase
    - Replace spaces with hyphens
    - Remove special characters (keep alphanumeric, hyphens, underscores)
    - Handle duplicate headers by adding -1, -2, etc.

    Args:
        text: The header text to slugify
        existing_slugs: Dict tracking slug counts for duplicate handling

    Returns:
        The slugified anchor text
    """
    # Convert to lowercase
    slug = text.lower()

    # Replace spaces with hyphens
    slug = slug.replace(' ', '-')

    # Remove special characters, keep alphanumeric, hyphens, underscores
    # Also keep Chinese/Japanese/Korean characters and other Unicode letters
    slug = re.sub(r'[^\w\-]', '', slug, flags=re.UNICODE)

    # Remove leading/trailing hyphens (but keep consecutive hyphens in the middle)
    slug = slug.strip('-')

    # Handle duplicates by adding -1, -2, etc.
    base_slug = slug
    if slug in existing_slugs:
        count = existing_slugs[slug]
        existing_slugs[slug] += 1
        slug = f"{base_slug}-{count}"
    else:
        existing_slugs[slug] = 1

    return slug


def extract_headers(content: str) -> Dict[str, int]:
    """
    Extract all headers from markdown content and create anchor map.
    Returns dict of {anchor: line_number}.
    """
    headers = {}
    slug_counts: Dict[str, int] = {}
    lines = content.split('\n')

    # ATX-style headers (# Header)
    atx_pattern = re.compile(r'^(#{1,6})\s+(.+?)(?:\s+#{1,6})?\s*$')

    # Setext-style headers (underlined with === or ---)
    setext_pattern = re.compile(r'^(=+|-+)\s*$')

    for i, line in enumerate(lines, 1):
        # Check ATX-style headers
        match = atx_pattern.match(line)
        if match:
            header_text = match.group(2).strip()
            # Remove inline code, links, and other markdown formatting
            header_text = re.sub(r'`[^`]+`', '', header_text)
            header_text = re.sub(r'\[([^\]]+)\]\([^\)]+\)', r'\1', header_text)
            header_text = re.sub(r'\*\*([^\*]+)\*\*', r'\1', header_text)
            header_text = re.sub(r'\*([^\*]+)\*', r'\1', header_text)

            slug = github_slugify(header_text, slug_counts)
            headers[slug] = i
            continue

        # Check Setext-style headers (need to look at previous line)
        if i > 1:
            match = setext_pattern.match(line)
            if match and lines[i-2].strip():
                header_text = lines[i-2].strip()
                # Remove inline code, links, and other markdown formatting
                header_text = re.sub(r'`[^`]+`', '', header_text)
                header_text = re.sub(r'\[([^\]]+)\]\([^\)]+\)', r'\1', header_text)
                header_text = re.sub(r'\*\*([^\*]+)\*\*', r'\1', header_text)
                header_text = re.sub(r'\*([^\*]+)\*', r'\1', header_text)

                slug = github_slugify(header_text, slug_counts)
                headers[slug] = i - 1

    return headers


def extract_links(content: str) -> List[Tuple[str, int, str]]:
    """
    Extract all local links from markdown content.
    Returns list of (url, line_number, link_type) tuples.
    link_type is one of: 'file', 'anchor', 'image', 'external'
    """
    links = []
    lines = content.split('\n')

    # Pattern for markdown links: [text](url) and images: ![alt](url)
    inline_link_pattern = re.compile(r'(!?)\[([^\]]+)\]\(([^\)]+)\)')

    # Pattern for reference-style links: [text][ref] or [text]
    reference_link_pattern = re.compile(r'\[([^\]]+)\](?:\[([^\]]*)\])?')

    # Pattern for reference definitions: [ref]: url
    reference_def_pattern = re.compile(r'^\s*\[([^\]]+)\]:\s+(.+?)(?:\s+"[^"]*")?\s*$')

    # Build reference definitions map
    references: Dict[str, str] = {}
    for line in lines:
        match = reference_def_pattern.match(line)
        if match:
            ref_id = match.group(1).lower()
            url = match.group(2).strip()
            references[ref_id] = url

    for line_num, line in enumerate(lines, 1):
        # Skip reference definitions
        if reference_def_pattern.match(line):
            continue

        # Find inline links and images
        for match in inline_link_pattern.finditer(line):
            is_image = match.group(1) == '!'
            url = match.group(3).strip()

            # Skip external links (handled by verify-github-links.py)
            if url.startswith(('http://', 'https://', 'ftp://', 'mailto:')):
                continue

            # Decode URL-encoded characters
            url = unquote(url)

            link_type = 'image' if is_image else self._classify_link(url)
            links.append((url, line_num, link_type))

        # Find reference-style links (only for non-images, as reference images are rare)
        for match in reference_link_pattern.finditer(line):
            # Skip if it's part of a reference definition
            if line.strip().startswith('[') and ']:' in line:
                continue

            text = match.group(1)
            ref_id = match.group(2) if match.group(2) else text
            ref_id = ref_id.lower()

            if ref_id in references:
                url = references[ref_id]

                # Skip external links
                if url.startswith(('http://', 'https://', 'ftp://', 'mailto:')):
                    continue

                url = unquote(url)
                link_type = self._classify_link(url)
                links.append((url, line_num, link_type))

    return links


def _classify_link(url: str) -> str:
    """Classify a link as 'file', 'anchor', or 'cross-doc'."""
    if url.startswith('#'):
        return 'anchor'
    elif '#' in url:
        return 'cross-doc'
    else:
        return 'file'


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


def resolve_path(current_file: Path, link_path: str, root_dir: Path) -> Optional[Path]:
    """
    Resolve a relative link path from the current file.
    Returns absolute Path object or None if invalid.
    """
    try:
        # Remove anchor if present
        if '#' in link_path:
            link_path = link_path.split('#')[0]

        # Handle empty path (pure anchor)
        if not link_path:
            return current_file

        # Handle repository-root relative paths (starting with /)
        # GitHub treats /docs/file.md as relative to repository root
        if link_path.startswith('/'):
            target_path = (root_dir / link_path.lstrip('/')).resolve()
        else:
            # Resolve relative to current file's directory
            current_dir = current_file.parent
            target_path = (current_dir / link_path).resolve()

        # Ensure the resolved path is within the repository
        try:
            target_path.relative_to(root_dir)
        except ValueError:
            # Path is outside repository
            return None

        return target_path
    except Exception:
        return None


def validate_links(
    markdown_files: List[Path],
    root_dir: Path,
    verbose: bool
) -> Tuple[int, List[Tuple[Path, int, str, str]]]:
    """
    Validate all local links in markdown files.
    Returns (success_count, failures_list).
    failures_list contains (file, line_num, link, error_message) tuples.
    """
    failures = []
    success_count = 0

    # Build header map for all files
    print(f"{Colors.BLUE}Building anchor map for all files...{Colors.RESET}")
    anchor_map: Dict[Path, Dict[str, int]] = {}
    for md_file in markdown_files:
        try:
            content = md_file.read_text(encoding='utf-8')
            anchor_map[md_file] = extract_headers(content)
        except Exception as e:
            print(f"{Colors.YELLOW}Warning: Could not read {md_file}: {e}{Colors.RESET}")
            anchor_map[md_file] = {}

    print(f"Built anchor map for {len(anchor_map)} files")
    print()

    # Validate links in each file
    print(f"{Colors.BLUE}Validating local links...{Colors.RESET}")
    for md_file in markdown_files:
        try:
            content = md_file.read_text(encoding='utf-8')
            links = extract_links(content)

            for url, line_num, link_type in links:
                error = None

                if link_type == 'anchor':
                    # Intra-document anchor
                    anchor = url[1:]  # Remove leading #
                    if anchor not in anchor_map[md_file]:
                        error = f"Anchor '#{anchor}' not found in document"

                elif link_type == 'file' or link_type == 'image':
                    # Local file reference
                    target_path = resolve_path(md_file, url, root_dir)
                    if target_path is None:
                        error = f"Invalid path: {url}"
                    elif not target_path.exists():
                        error = f"File not found: {url}"

                elif link_type == 'cross-doc':
                    # Cross-document anchor
                    path_part, anchor = url.split('#', 1)
                    target_path = resolve_path(md_file, path_part, root_dir)

                    if target_path is None:
                        error = f"Invalid path: {path_part}"
                    elif not target_path.exists():
                        error = f"File not found: {path_part}"
                    elif target_path.suffix.lower() != '.md':
                        # Non-markdown file with anchor (e.g., source code with line numbers)
                        # These are GitHub-style references (e.g., file.cs#L123)
                        # Skip validation as they're meant for GitHub's web interface
                        if verbose:
                            print(f"{Colors.YELLOW}⊘{Colors.RESET} Skipping non-markdown anchor: {url}")
                        continue
                    elif target_path not in anchor_map:
                        error = f"Target file not in anchor map: {path_part}"
                    elif anchor not in anchor_map[target_path]:
                        error = f"Anchor '#{anchor}' not found in {path_part}"

                if error:
                    failures.append((md_file, line_num, url, error))
                    if verbose:
                        rel_path = md_file.relative_to(root_dir)
                        print(f"{Colors.RED}✗{Colors.RESET} {rel_path}:{line_num} - {url}")
                        print(f"  {Colors.RED}{error}{Colors.RESET}")
                else:
                    success_count += 1
                    if verbose:
                        rel_path = md_file.relative_to(root_dir)
                        print(f"{Colors.GREEN}✓{Colors.RESET} {rel_path}:{line_num} - {url}")

        except Exception as e:
            print(f"{Colors.YELLOW}Warning: Error processing {md_file}: {e}{Colors.RESET}")

    return success_count, failures


# Fix the _classify_link reference in extract_links
def extract_links_fixed(content: str) -> List[Tuple[str, int, str]]:
    """
    Extract all local links from markdown content.
    Returns list of (url, line_number, link_type) tuples.
    link_type is one of: 'file', 'anchor', 'image', 'cross-doc'
    """
    links = []
    lines = content.split('\n')

    # Pattern for markdown links: [text](url) and images: ![alt](url)
    inline_link_pattern = re.compile(r'(!?)\[([^\]]+)\]\(([^\)]+)\)')

    # Pattern for reference-style links: [text][ref] or [text]
    reference_link_pattern = re.compile(r'\[([^\]]+)\](?:\[([^\]]*)\])?')

    # Pattern for reference definitions: [ref]: url
    reference_def_pattern = re.compile(r'^\s*\[([^\]]+)\]:\s+(.+?)(?:\s+"[^"]*")?\s*$')

    # Build reference definitions map
    references: Dict[str, str] = {}
    for line in lines:
        match = reference_def_pattern.match(line)
        if match:
            ref_id = match.group(1).lower()
            url = match.group(2).strip()
            references[ref_id] = url

    for line_num, line in enumerate(lines, 1):
        # Skip reference definitions
        if reference_def_pattern.match(line):
            continue

        # Find inline links and images
        for match in inline_link_pattern.finditer(line):
            is_image = match.group(1) == '!'
            url = match.group(3).strip()

            # Skip external links (handled by verify-github-links.py)
            if url.startswith(('http://', 'https://', 'ftp://', 'mailto:')):
                continue

            # Decode URL-encoded characters
            url = unquote(url)

            link_type = 'image' if is_image else _classify_link(url)
            links.append((url, line_num, link_type))

        # Find reference-style links (only for non-images, as reference images are rare)
        for match in reference_link_pattern.finditer(line):
            # Skip if it's part of a reference definition
            if line.strip().startswith('[') and ']:' in line:
                continue

            # Skip inline links (already processed)
            if match.end() < len(line) and line[match.end()] == '(':
                continue

            text = match.group(1)
            ref_id = match.group(2) if match.group(2) else text
            ref_id = ref_id.lower()

            if ref_id in references:
                url = references[ref_id]

                # Skip external links
                if url.startswith(('http://', 'https://', 'ftp://', 'mailto:')):
                    continue

                url = unquote(url)
                link_type = _classify_link(url)
                links.append((url, line_num, link_type))

    return links


# Replace the original extract_links with the fixed version
extract_links = extract_links_fixed


def main():
    parser = argparse.ArgumentParser(
        description="Verify local links in Markdown files",
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
        default=['node_modules/', '.git/', '.aidocs/', 'TEST-MARKDOWN-VERIFIER.md'],
        help='Patterns to exclude from search (default: node_modules/ .git/ .aidocs/ TEST-MARKDOWN-VERIFIER.md)'
    )

    args = parser.parse_args()

    # Get repository root (assume script is in scripts/)
    repo_root = Path(__file__).resolve().parent.parent

    print(f"{Colors.BOLD}Local Link Verification Tool{Colors.RESET}")
    print(f"Repository: {repo_root}")
    print(f"Excluded patterns: {', '.join(args.exclude)}")
    print()

    # Find all markdown files
    print(f"{Colors.BLUE}Scanning for Markdown files...{Colors.RESET}")
    markdown_files = find_markdown_files(repo_root, args.exclude)
    print(f"Found {len(markdown_files)} Markdown files")
    print()

    if not markdown_files:
        print(f"{Colors.YELLOW}No Markdown files found to verify.{Colors.RESET}")
        return 0

    # Validate all links
    success_count, failures = validate_links(markdown_files, repo_root, args.verbose)

    # Print summary
    print()
    print(f"{Colors.BOLD}{'='*70}{Colors.RESET}")
    print(f"{Colors.BOLD}Summary{Colors.RESET}")
    print(f"{Colors.BOLD}{'='*70}{Colors.RESET}")
    print(f"{Colors.GREEN}Valid links: {success_count}{Colors.RESET}")
    print(f"{Colors.RED}Broken links: {len(failures)}{Colors.RESET}")
    print()

    # Print detailed failure report
    if failures:
        print(f"{Colors.BOLD}Broken Links:{Colors.RESET}")
        print()

        for file_path, line_num, link, error in failures:
            rel_path = file_path.relative_to(repo_root)
            print(f"{Colors.RED}✗ {rel_path}:{line_num}{Colors.RESET}")
            print(f"  Link: {link}")
            print(f"  Error: {error}")
            print()

        return 1
    else:
        print(f"{Colors.GREEN}✓ All local links verified successfully!{Colors.RESET}")
        return 0


if __name__ == "__main__":
    sys.exit(main())

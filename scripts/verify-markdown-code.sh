#!/bin/bash
# Verify C# code blocks in Markdown documentation

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Markdown C# Code Verifier${NC}"
echo

# Get repository root
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Build project first to generate assemblies
echo "Building project for assembly references..."
dotnet build --configuration Release --verbosity quiet
echo -e "${GREEN}✓${NC} Build complete"
echo

# Run verifier
# Exclude: .aidocs/ (internal AI documentation and plans), TEST-MARKDOWN-VERIFIER.md (test file)
#          CLAUDE.md (AI instructions), TestingStrategy.md (test pattern examples)
#          .worktrees/ (git worktrees for parallel development)
#          docs/plans/ (implementation plans with incomplete code examples)
dotnet run --project tools/MarkdownCodeVerifier --configuration Release --no-build -- "$REPO_ROOT" ".aidocs/,TEST-MARKDOWN-VERIFIER.md,CLAUDE.md,TestingStrategy.md,.worktrees/,docs/plans/" "$@"

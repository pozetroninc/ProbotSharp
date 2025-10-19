#!/bin/bash
# Run full coverage analysis for ProbotSharp with line and branch coverage

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/../.." && pwd )"

# Configuration
COVERAGE_DIR="$PROJECT_ROOT/coverage-results"
REPORT_DIR="$PROJECT_ROOT/coverage-report"
SETTINGS_FILE="$PROJECT_ROOT/coverlet.runsettings"

echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   ProbotSharp Coverage Analysis - Full Suite${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Clean previous results
echo -e "${YELLOW}► Cleaning previous coverage results...${NC}"
rm -rf "$COVERAGE_DIR"
rm -rf "$REPORT_DIR"
mkdir -p "$COVERAGE_DIR"
mkdir -p "$REPORT_DIR"

# Build in Debug mode for accurate coverage instrumentation
echo -e "${YELLOW}► Building in Debug mode for accurate coverage...${NC}"
dotnet build "$PROJECT_ROOT/ProbotSharp.sln" --configuration Debug --no-restore

# Run tests with coverage (Debug mode prevents JIT optimizations that affect coverage)
echo -e "${YELLOW}► Running all tests with coverage collection...${NC}"
echo ""

dotnet test "$PROJECT_ROOT/ProbotSharp.sln" \
    --configuration Debug \
    --no-build \
    --settings "$SETTINGS_FILE" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$COVERAGE_DIR" \
    --logger "console;verbosity=minimal"

echo ""
echo -e "${GREEN}✓ Test execution complete${NC}"
echo ""

# Find all coverage files
echo -e "${YELLOW}► Locating coverage files...${NC}"
COVERAGE_FILES=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -type f)
COVERAGE_COUNT=$(echo "$COVERAGE_FILES" | wc -l)

if [ -z "$COVERAGE_FILES" ]; then
    echo -e "${RED}✗ No coverage files found!${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Found $COVERAGE_COUNT coverage file(s)${NC}"
echo ""

# Generate HTML report with ReportGenerator
echo -e "${YELLOW}► Generating comprehensive coverage report...${NC}"
echo ""

# Build the file list for ReportGenerator
FILE_LIST=""
for file in $COVERAGE_FILES; do
    if [ -n "$FILE_LIST" ]; then
        FILE_LIST="$FILE_LIST;$file"
    else
        FILE_LIST="$file"
    fi
done

# Run ReportGenerator with branch coverage
dotnet reportgenerator \
    "-reports:$FILE_LIST" \
    "-targetdir:$REPORT_DIR" \
    "-reporttypes:Html;Cobertura;TextSummary;JsonSummary;Badges" \
    "-assemblyfilters:+ProbotSharp.*;-*.Tests;-*.Tests.*" \
    "-classfilters:+*;-*Migrations*;-*DbContextModelSnapshot" \
    "-filefilters:-*.LogMessages.cs;-*.g.cs;-*.cshtml.g.cs;-*.razor.g.cs" \
    "-verbosity:Info" \
    "-title:ProbotSharp Coverage Report" \
    "-tag:$(git rev-parse --short HEAD 2>/dev/null || echo 'local')"

echo ""
echo -e "${GREEN}✓ Report generation complete${NC}"
echo ""

# Display summary
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   Coverage Summary${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

if [ -f "$REPORT_DIR/Summary.txt" ]; then
    cat "$REPORT_DIR/Summary.txt"
else
    echo -e "${YELLOW}Summary file not found, checking JSON summary...${NC}"
    if [ -f "$REPORT_DIR/Summary.json" ]; then
        # Extract key metrics from JSON
        LINE_COVERAGE=$(grep -o '"linecoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
        BRANCH_COVERAGE=$(grep -o '"branchcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)

        echo -e "Line Coverage:   ${GREEN}${LINE_COVERAGE}%${NC}"
        echo -e "Branch Coverage: ${GREEN}${BRANCH_COVERAGE}%${NC}"
    fi
fi

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Generate custom badges
echo -e "${YELLOW}► Generating coverage badges...${NC}"
if [ -f "$SCRIPT_DIR/generate-badges.sh" ]; then
    "$SCRIPT_DIR/generate-badges.sh" "$REPORT_DIR/badges" 2>/dev/null || echo -e "${YELLOW}⚠ Badge generation skipped (missing dependencies)${NC}"
fi

# Report location
echo -e "${GREEN}✓ Coverage report generated successfully!${NC}"
echo ""
echo -e "📊 HTML Report: ${BLUE}file://$REPORT_DIR/index.html${NC}"
echo -e "📄 Cobertura:   ${BLUE}$REPORT_DIR/Cobertura.xml${NC}"
echo -e "🏷️  Badges:      ${BLUE}$REPORT_DIR/badges/${NC}"
echo ""

# Open report in browser if available
if command -v xdg-open &> /dev/null; then
    echo -e "${YELLOW}Opening report in browser...${NC}"
    xdg-open "$REPORT_DIR/index.html" 2>/dev/null || true
elif command -v open &> /dev/null; then
    echo -e "${YELLOW}Opening report in browser...${NC}"
    open "$REPORT_DIR/index.html" 2>/dev/null || true
fi

echo -e "${GREEN}✅ Coverage analysis complete!${NC}"
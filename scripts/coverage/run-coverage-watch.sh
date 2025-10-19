#!/bin/bash
# Watch mode for continuous coverage feedback during development

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/../.." && pwd )"

# Configuration
COVERAGE_DIR="$PROJECT_ROOT/coverage-results-watch"
REPORT_DIR="$PROJECT_ROOT/coverage-report-watch"
SETTINGS_FILE="$PROJECT_ROOT/coverlet.runsettings"

# Default watch target
WATCH_TARGET=${1:-"all"}
WATCH_INTERVAL=${2:-5}

echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   ProbotSharp Coverage Watch Mode${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${CYAN}Target: $WATCH_TARGET${NC}"
echo -e "${CYAN}Refresh interval: ${WATCH_INTERVAL}s${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop watching${NC}"
echo ""

# Function to run coverage
run_coverage() {
    local TARGET=$1
    local TIMESTAMP=$(date '+%H:%M:%S')

    echo -e "${CYAN}[$TIMESTAMP] Running coverage...${NC}"

    # Clean previous results
    rm -rf "$COVERAGE_DIR"
    rm -rf "$REPORT_DIR"
    mkdir -p "$COVERAGE_DIR"
    mkdir -p "$REPORT_DIR"

    # Determine test target
    local TEST_TARGET=""
    case $TARGET in
        "domain")
            TEST_TARGET="tests/ProbotSharp.Domain.Tests"
            ;;
        "application")
            TEST_TARGET="tests/ProbotSharp.Application.Tests"
            ;;
        "infrastructure")
            TEST_TARGET="tests/ProbotSharp.Infrastructure.Tests"
            ;;
        "adapters")
            TEST_TARGET="tests/ProbotSharp.Adapter.Tests"
            ;;
        "shared")
            TEST_TARGET="tests/ProbotSharp.Shared.Tests"
            ;;
        "all"|*)
            TEST_TARGET="ProbotSharp.sln"
            ;;
    esac

    # Build in Debug mode for accurate coverage
    dotnet build "$PROJECT_ROOT/$TEST_TARGET" --configuration Debug --no-restore > /dev/null 2>&1

    # Run tests with coverage in Debug mode (suppress output for cleaner watch mode)
    if dotnet test "$PROJECT_ROOT/$TEST_TARGET" \
        --configuration Debug \
        --no-build \
        --settings "$SETTINGS_FILE" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$COVERAGE_DIR" \
        --logger "console;verbosity=quiet" \
        > /dev/null 2>&1; then

        # Find coverage files
        local COVERAGE_FILES=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -type f)

        if [ -n "$COVERAGE_FILES" ]; then
            # Build file list
            local FILE_LIST=""
            for file in $COVERAGE_FILES; do
                if [ -n "$FILE_LIST" ]; then
                    FILE_LIST="$FILE_LIST;$file"
                else
                    FILE_LIST="$file"
                fi
            done

            # Generate report silently
            dotnet reportgenerator \
                "-reports:$FILE_LIST" \
                "-targetdir:$REPORT_DIR" \
                "-reporttypes:Html;JsonSummary" \
                "-assemblyfilters:+ProbotSharp.*;-*.Tests;-*.Tests.*" \
                "-classfilters:+*;-*Migrations*;-*DbContextModelSnapshot" \
                "-filefilters:-*.LogMessages.cs;-*.g.cs" \
                "-verbosity:Error" \
                "-title:ProbotSharp Coverage (Watch Mode)" \
                > /dev/null 2>&1

            # Display summary
            if [ -f "$REPORT_DIR/Summary.json" ]; then
                local LINE_COVERAGE=$(grep -o '"linecoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
                local BRANCH_COVERAGE=$(grep -o '"branchcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)

                echo -e "${CYAN}[$TIMESTAMP]${NC} Line: ${GREEN}${LINE_COVERAGE}%${NC} | Branch: ${GREEN}${BRANCH_COVERAGE}%${NC}"
            else
                echo -e "${CYAN}[$TIMESTAMP]${NC} ${YELLOW}Coverage report generated${NC}"
            fi
        else
            echo -e "${CYAN}[$TIMESTAMP]${NC} ${RED}No coverage data generated${NC}"
        fi
    else
        echo -e "${CYAN}[$TIMESTAMP]${NC} ${RED}Test execution failed${NC}"
    fi
}

# Function to check for file changes
get_last_modified() {
    find "$PROJECT_ROOT/src" "$PROJECT_ROOT/tests" \
        -name "*.cs" -type f \
        -exec stat -c %Y {} \; 2>/dev/null | \
        sort -rn | head -1
}

# Initial run
run_coverage "$WATCH_TARGET"
echo ""
echo -e "${GREEN}Report available at: ${BLUE}file://$REPORT_DIR/index.html${NC}"
echo ""

# Store last modification time
LAST_MODIFIED=$(get_last_modified)

# Watch loop
while true; do
    sleep "$WATCH_INTERVAL"

    # Check for changes
    CURRENT_MODIFIED=$(get_last_modified)

    if [ "$CURRENT_MODIFIED" != "$LAST_MODIFIED" ]; then
        echo ""
        echo -e "${YELLOW}Changes detected, re-running coverage...${NC}"
        run_coverage "$WATCH_TARGET"
        LAST_MODIFIED=$CURRENT_MODIFIED
    fi
done
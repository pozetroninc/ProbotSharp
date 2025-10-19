#!/bin/bash
# Run coverage analysis per architectural layer with specific thresholds

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
COVERAGE_DIR="$PROJECT_ROOT/coverage-results"
SETTINGS_FILE="$PROJECT_ROOT/coverlet.runsettings"

# Coverage thresholds per layer
declare -A LINE_THRESHOLDS=(
    ["Domain"]=90
    ["Application"]=80
    ["Infrastructure"]=70
    ["Adapters"]=70
    ["Shared"]=85
    ["Bootstrap"]=60
)

declare -A BRANCH_THRESHOLDS=(
    ["Domain"]=85
    ["Application"]=75
    ["Infrastructure"]=65
    ["Adapters"]=65
    ["Shared"]=80
    ["Bootstrap"]=50
)

# Test project mapping
declare -A TEST_PROJECTS=(
    ["Domain"]="tests/ProbotSharp.Domain.Tests"
    ["Application"]="tests/ProbotSharp.Application.Tests"
    ["Infrastructure"]="tests/ProbotSharp.Infrastructure.Tests"
    ["Adapters"]="tests/ProbotSharp.Adapter.Tests"
    ["Shared"]="tests/ProbotSharp.Shared.Tests"
    ["Bootstrap"]="tests/ProbotSharp.Bootstrap.Api.Tests"
)

echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   ProbotSharp Coverage Analysis - By Layer${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Track overall status
OVERALL_STATUS=0

# Function to run coverage for a specific layer
run_layer_coverage() {
    local LAYER=$1
    local TEST_PROJECT=$2
    local LINE_THRESHOLD=${LINE_THRESHOLDS[$LAYER]}
    local BRANCH_THRESHOLD=${BRANCH_THRESHOLDS[$LAYER]}

    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}   Layer: $LAYER${NC}"
    echo -e "${CYAN}   Thresholds: Line ≥ $LINE_THRESHOLD%, Branch ≥ $BRANCH_THRESHOLD%${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""

    # Check if test project exists
    if [ ! -d "$PROJECT_ROOT/$TEST_PROJECT" ]; then
        echo -e "${YELLOW}⚠ Test project not found: $TEST_PROJECT${NC}"
        echo ""
        return 1
    fi

    # Clean layer-specific coverage
    local LAYER_COVERAGE_DIR="$COVERAGE_DIR/$LAYER"
    local LAYER_REPORT_DIR="$PROJECT_ROOT/coverage-report-$LAYER"

    rm -rf "$LAYER_COVERAGE_DIR"
    rm -rf "$LAYER_REPORT_DIR"
    mkdir -p "$LAYER_COVERAGE_DIR"
    mkdir -p "$LAYER_REPORT_DIR"

    # Build test project in Debug mode for this layer
    echo -e "${YELLOW}► Building $LAYER layer tests in Debug mode...${NC}"
    dotnet build "$PROJECT_ROOT/$TEST_PROJECT" --configuration Debug --no-restore

    # Run tests with coverage for this layer (Debug mode for accurate instrumentation)
    echo -e "${YELLOW}► Running tests for $LAYER layer...${NC}"

    dotnet test "$PROJECT_ROOT/$TEST_PROJECT" \
        --configuration Debug \
        --no-build \
        --settings "$SETTINGS_FILE" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$LAYER_COVERAGE_DIR" \
        --logger "console;verbosity=minimal"

    # Find coverage files
    local COVERAGE_FILE=$(find "$LAYER_COVERAGE_DIR" -name "coverage.cobertura.xml" -type f | head -1)

    if [ -z "$COVERAGE_FILE" ]; then
        echo -e "${RED}✗ No coverage file found for $LAYER layer${NC}"
        echo ""
        return 1
    fi

    # Generate report for this layer
    echo -e "${YELLOW}► Generating report for $LAYER layer...${NC}"

    dotnet reportgenerator \
        "-reports:$COVERAGE_FILE" \
        "-targetdir:$LAYER_REPORT_DIR" \
        "-reporttypes:Html;TextSummary;JsonSummary;Badges" \
        "-assemblyfilters:+ProbotSharp.$LAYER*;-*.Tests;-*.Tests.*" \
        "-classfilters:+*;-*Migrations*;-*DbContextModelSnapshot" \
        "-filefilters:-*.LogMessages.cs;-*.g.cs" \
        "-verbosity:Error" \
        "-title:ProbotSharp $LAYER Coverage" \
        "-tag:$(git rev-parse --short HEAD 2>/dev/null || echo 'local')"

    # Check coverage against thresholds
    if [ -f "$LAYER_REPORT_DIR/Summary.json" ]; then
        local LINE_COVERAGE=$(grep -o '"linecoverage":[0-9.]*' "$LAYER_REPORT_DIR/Summary.json" | head -1 | cut -d: -f2 | cut -d. -f1)
        local BRANCH_COVERAGE=$(grep -o '"branchcoverage":[0-9.]*' "$LAYER_REPORT_DIR/Summary.json" | head -1 | cut -d: -f2 | cut -d. -f1)

        # Default to 0 if not found
        LINE_COVERAGE=${LINE_COVERAGE:-0}
        BRANCH_COVERAGE=${BRANCH_COVERAGE:-0}

        echo ""
        echo -e "${BLUE}Results:${NC}"

        # Check line coverage
        if [ "$LINE_COVERAGE" -ge "$LINE_THRESHOLD" ]; then
            echo -e "  Line Coverage:   ${GREEN}${LINE_COVERAGE}%${NC} (threshold: $LINE_THRESHOLD%) ✓"
        else
            echo -e "  Line Coverage:   ${RED}${LINE_COVERAGE}%${NC} (threshold: $LINE_THRESHOLD%) ✗"
            OVERALL_STATUS=1
        fi

        # Check branch coverage
        if [ "$BRANCH_COVERAGE" -ge "$BRANCH_THRESHOLD" ]; then
            echo -e "  Branch Coverage: ${GREEN}${BRANCH_COVERAGE}%${NC} (threshold: $BRANCH_THRESHOLD%) ✓"
        else
            echo -e "  Branch Coverage: ${RED}${BRANCH_COVERAGE}%${NC} (threshold: $BRANCH_THRESHOLD%) ✗"
            OVERALL_STATUS=1
        fi

        echo -e "  Report: ${BLUE}file://$LAYER_REPORT_DIR/index.html${NC}"
    else
        echo -e "${RED}✗ Could not parse coverage results for $LAYER${NC}"
        OVERALL_STATUS=1
    fi

    echo ""
}

# Clean previous results
echo -e "${YELLOW}► Cleaning previous coverage results...${NC}"
rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"
echo ""

# Process each layer
for LAYER in "${!TEST_PROJECTS[@]}"; do
    run_layer_coverage "$LAYER" "${TEST_PROJECTS[$LAYER]}"
done

# Summary
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   Coverage Analysis Summary${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Create combined summary
SUMMARY_FILE="$PROJECT_ROOT/coverage-summary.md"
{
    echo "# ProbotSharp Coverage Summary"
    echo ""
    echo "Generated: $(date)"
    echo ""
    echo "## Coverage by Layer"
    echo ""
    echo "| Layer | Line Coverage | Line Threshold | Branch Coverage | Branch Threshold | Status |"
    echo "|-------|--------------|----------------|-----------------|------------------|--------|"

    for LAYER in Domain Application Infrastructure Adapters Shared Bootstrap; do
        REPORT_DIR="$PROJECT_ROOT/coverage-report-$LAYER"
        if [ -f "$REPORT_DIR/Summary.json" ]; then
            LINE_COV=$(grep -o '"linecoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
            BRANCH_COV=$(grep -o '"branchcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
            LINE_THRESH=${LINE_THRESHOLDS[$LAYER]}
            BRANCH_THRESH=${BRANCH_THRESHOLDS[$LAYER]}

            LINE_COV_INT=$(echo "$LINE_COV" | cut -d. -f1)
            BRANCH_COV_INT=$(echo "$BRANCH_COV" | cut -d. -f1)

            if [ "$LINE_COV_INT" -ge "$LINE_THRESH" ] && [ "$BRANCH_COV_INT" -ge "$BRANCH_THRESH" ]; then
                STATUS="✅ Pass"
            else
                STATUS="❌ Fail"
            fi

            echo "| $LAYER | ${LINE_COV}% | ${LINE_THRESH}% | ${BRANCH_COV}% | ${BRANCH_THRESH}% | $STATUS |"
        else
            echo "| $LAYER | N/A | ${LINE_THRESHOLDS[$LAYER]}% | N/A | ${BRANCH_THRESHOLDS[$LAYER]}% | ⚠️ No data |"
        fi
    done

    echo ""
    echo "## Reports"
    echo ""
    for LAYER in Domain Application Infrastructure Adapters Shared Bootstrap; do
        REPORT_DIR="coverage-report-$LAYER"
        if [ -d "$PROJECT_ROOT/$REPORT_DIR" ]; then
            echo "- [$LAYER Coverage Report](./$REPORT_DIR/index.html)"
        fi
    done
} > "$SUMMARY_FILE"

echo -e "${GREEN}✓ Summary written to: $SUMMARY_FILE${NC}"
echo ""

# Display summary
cat "$SUMMARY_FILE"
echo ""

if [ $OVERALL_STATUS -eq 0 ]; then
    echo -e "${GREEN}✅ All layers meet coverage thresholds!${NC}"
else
    echo -e "${RED}❌ Some layers are below coverage thresholds${NC}"
fi

echo ""
echo -e "${BLUE}Individual layer reports:${NC}"
for LAYER in Domain Application Infrastructure Adapters Shared Bootstrap; do
    REPORT_DIR="$PROJECT_ROOT/coverage-report-$LAYER"
    if [ -d "$REPORT_DIR" ]; then
        echo -e "  $LAYER: ${BLUE}file://$REPORT_DIR/index.html${NC}"
    fi
done
echo ""

exit $OVERALL_STATUS
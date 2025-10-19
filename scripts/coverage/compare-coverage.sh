#!/bin/bash
# Compare coverage between branches or track coverage trends over time

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
HISTORY_DIR="$PROJECT_ROOT/.coverage-history"
COVERAGE_DIR="$PROJECT_ROOT/coverage-results"
REPORT_DIR="$PROJECT_ROOT/coverage-report"

# Command line arguments
COMMAND=${1:-"snapshot"}
BRANCH_TO_COMPARE=${2:-"main"}

echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   ProbotSharp Coverage Comparison${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Ensure history directory exists
mkdir -p "$HISTORY_DIR"

# Function to save current coverage snapshot
save_snapshot() {
    local SNAPSHOT_NAME=${1:-$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "snapshot")}
    local TIMESTAMP=$(date '+%Y%m%d_%H%M%S')
    local SNAPSHOT_FILE="$HISTORY_DIR/${SNAPSHOT_NAME}_${TIMESTAMP}.json"

    echo -e "${YELLOW}► Saving coverage snapshot for: $SNAPSHOT_NAME${NC}"

    # Run coverage if no recent report exists
    if [ ! -f "$REPORT_DIR/Summary.json" ] || [ $(find "$REPORT_DIR/Summary.json" -mmin +60 | wc -l) -gt 0 ]; then
        echo -e "${CYAN}Running coverage analysis...${NC}"
        "$SCRIPT_DIR/run-coverage-all.sh" > /dev/null 2>&1
    fi

    if [ -f "$REPORT_DIR/Summary.json" ]; then
        # Extract key metrics
        local LINE_COV=$(grep -o '"linecoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
        local BRANCH_COV=$(grep -o '"branchcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
        local METHOD_COV=$(grep -o '"methodcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2 || echo "0")

        # Create snapshot JSON
        cat > "$SNAPSHOT_FILE" <<EOF
{
    "branch": "$SNAPSHOT_NAME",
    "timestamp": "$TIMESTAMP",
    "commit": "$(git rev-parse HEAD 2>/dev/null || echo 'unknown')",
    "line_coverage": $LINE_COV,
    "branch_coverage": $BRANCH_COV,
    "method_coverage": $METHOD_COV
}
EOF

        echo -e "${GREEN}✓ Snapshot saved: $SNAPSHOT_FILE${NC}"

        # Also save as latest for this branch
        cp "$SNAPSHOT_FILE" "$HISTORY_DIR/${SNAPSHOT_NAME}_latest.json"
    else
        echo -e "${RED}✗ No coverage report found${NC}"
        return 1
    fi
}

# Function to compare with another branch
compare_branches() {
    local CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "current")
    local TARGET_BRANCH=$1

    echo -e "${YELLOW}► Comparing coverage: ${CYAN}$CURRENT_BRANCH${NC} vs ${CYAN}$TARGET_BRANCH${NC}"
    echo ""

    # Save current coverage
    save_snapshot "$CURRENT_BRANCH" > /dev/null 2>&1

    # Load current metrics
    local CURRENT_FILE="$HISTORY_DIR/${CURRENT_BRANCH}_latest.json"
    if [ ! -f "$CURRENT_FILE" ]; then
        echo -e "${RED}✗ No coverage data for current branch${NC}"
        return 1
    fi

    # Try to find target branch coverage
    local TARGET_FILE="$HISTORY_DIR/${TARGET_BRANCH}_latest.json"
    if [ ! -f "$TARGET_FILE" ]; then
        echo -e "${YELLOW}No saved coverage for $TARGET_BRANCH, checking out and running...${NC}"

        # Stash any changes
        git stash push -m "Coverage comparison stash" > /dev/null 2>&1

        # Checkout target branch
        git checkout "$TARGET_BRANCH" > /dev/null 2>&1

        # Run coverage
        "$SCRIPT_DIR/run-coverage-all.sh" > /dev/null 2>&1

        # Save snapshot
        save_snapshot "$TARGET_BRANCH" > /dev/null 2>&1

        # Return to original branch
        git checkout "$CURRENT_BRANCH" > /dev/null 2>&1

        # Restore stashed changes
        git stash pop > /dev/null 2>&1 || true
    fi

    # Load metrics
    local CURRENT_LINE=$(grep -o '"line_coverage":[0-9.]*' "$CURRENT_FILE" | cut -d: -f2)
    local CURRENT_BRANCH_COV=$(grep -o '"branch_coverage":[0-9.]*' "$CURRENT_FILE" | cut -d: -f2)
    local CURRENT_METHOD=$(grep -o '"method_coverage":[0-9.]*' "$CURRENT_FILE" | cut -d: -f2)

    local TARGET_LINE=$(grep -o '"line_coverage":[0-9.]*' "$TARGET_FILE" | cut -d: -f2)
    local TARGET_BRANCH_COV=$(grep -o '"branch_coverage":[0-9.]*' "$TARGET_FILE" | cut -d: -f2)
    local TARGET_METHOD=$(grep -o '"method_coverage":[0-9.]*' "$TARGET_FILE" | cut -d: -f2)

    # Calculate deltas
    local LINE_DELTA=$(echo "$CURRENT_LINE - $TARGET_LINE" | bc)
    local BRANCH_DELTA=$(echo "$CURRENT_BRANCH_COV - $TARGET_BRANCH_COV" | bc)
    local METHOD_DELTA=$(echo "$CURRENT_METHOD - $TARGET_METHOD" | bc)

    # Display comparison table
    echo -e "${BLUE}Coverage Comparison Results${NC}"
    echo ""
    echo "┌────────────────┬──────────────┬──────────────┬──────────────┐"
    echo "│ Metric         │ $CURRENT_BRANCH │ $TARGET_BRANCH │ Delta        │"
    echo "├────────────────┼──────────────┼──────────────┼──────────────┤"

    # Line coverage
    printf "│ Line Coverage  │ %12s │ %12s │ " "${CURRENT_LINE}%" "${TARGET_LINE}%"
    if (( $(echo "$LINE_DELTA > 0" | bc -l) )); then
        printf "${GREEN}+%-11s${NC} │\n" "${LINE_DELTA}%"
    elif (( $(echo "$LINE_DELTA < 0" | bc -l) )); then
        printf "${RED}%-12s${NC} │\n" "${LINE_DELTA}%"
    else
        printf "%-12s │\n" "${LINE_DELTA}%"
    fi

    # Branch coverage
    printf "│ Branch Coverage│ %12s │ %12s │ " "${CURRENT_BRANCH_COV}%" "${TARGET_BRANCH_COV}%"
    if (( $(echo "$BRANCH_DELTA > 0" | bc -l) )); then
        printf "${GREEN}+%-11s${NC} │\n" "${BRANCH_DELTA}%"
    elif (( $(echo "$BRANCH_DELTA < 0" | bc -l) )); then
        printf "${RED}%-12s${NC} │\n" "${BRANCH_DELTA}%"
    else
        printf "%-12s │\n" "${BRANCH_DELTA}%"
    fi

    # Method coverage
    printf "│ Method Coverage│ %12s │ %12s │ " "${CURRENT_METHOD}%" "${TARGET_METHOD}%"
    if (( $(echo "$METHOD_DELTA > 0" | bc -l) )); then
        printf "${GREEN}+%-11s${NC} │\n" "${METHOD_DELTA}%"
    elif (( $(echo "$METHOD_DELTA < 0" | bc -l) )); then
        printf "${RED}%-12s${NC} │\n" "${METHOD_DELTA}%"
    else
        printf "%-12s │\n" "${METHOD_DELTA}%"
    fi

    echo "└────────────────┴──────────────┴──────────────┴──────────────┘"
    echo ""

    # Overall assessment
    if (( $(echo "$LINE_DELTA > 0" | bc -l) )) && (( $(echo "$BRANCH_DELTA > 0" | bc -l) )); then
        echo -e "${GREEN}✅ Coverage improved compared to $TARGET_BRANCH${NC}"
    elif (( $(echo "$LINE_DELTA < -5" | bc -l) )) || (( $(echo "$BRANCH_DELTA < -5" | bc -l) )); then
        echo -e "${RED}❌ Significant coverage decrease compared to $TARGET_BRANCH${NC}"
        echo -e "${YELLOW}   Consider adding more tests before merging${NC}"
    elif (( $(echo "$LINE_DELTA < 0" | bc -l) )) || (( $(echo "$BRANCH_DELTA < 0" | bc -l) )); then
        echo -e "${YELLOW}⚠ Coverage decreased slightly compared to $TARGET_BRANCH${NC}"
    else
        echo -e "${BLUE}ℹ Coverage is similar to $TARGET_BRANCH${NC}"
    fi
}

# Function to show coverage trend
show_trend() {
    local BRANCH=${1:-$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "current")}

    echo -e "${YELLOW}► Coverage trend for: ${CYAN}$BRANCH${NC}"
    echo ""

    # Find all snapshots for this branch
    local SNAPSHOTS=$(ls -1 "$HISTORY_DIR/${BRANCH}_"*.json 2>/dev/null | grep -v "_latest.json" | sort)

    if [ -z "$SNAPSHOTS" ]; then
        echo -e "${YELLOW}No historical data found for $BRANCH${NC}"
        echo -e "Run '${CYAN}$0 snapshot${NC}' to create a snapshot"
        return 1
    fi

    echo -e "${BLUE}Historical Coverage Data${NC}"
    echo ""
    echo "┌──────────────────┬──────────┬────────┬────────┐"
    echo "│ Timestamp        │ Line %   │ Branch%│ Method%│"
    echo "├──────────────────┼──────────┼────────┼────────┤"

    for snapshot in $SNAPSHOTS; do
        local TIMESTAMP=$(grep -o '"timestamp":"[^"]*' "$snapshot" | cut -d'"' -f4)
        local LINE_COV=$(grep -o '"line_coverage":[0-9.]*' "$snapshot" | cut -d: -f2)
        local BRANCH_COV=$(grep -o '"branch_coverage":[0-9.]*' "$snapshot" | cut -d: -f2)
        local METHOD_COV=$(grep -o '"method_coverage":[0-9.]*' "$snapshot" | cut -d: -f2)

        # Format timestamp
        local FORMATTED_TIME="${TIMESTAMP:0:4}-${TIMESTAMP:4:2}-${TIMESTAMP:6:2} ${TIMESTAMP:9:2}:${TIMESTAMP:11:2}"

        printf "│ %-16s │ %8s │ %6s │ %6s │\n" "$FORMATTED_TIME" "$LINE_COV" "$BRANCH_COV" "$METHOD_COV"
    done

    echo "└──────────────────┴──────────┴────────┴────────┘"
    echo ""

    # Calculate trend
    local FIRST_LINE=$(echo "$SNAPSHOTS" | head -1 | xargs grep -o '"line_coverage":[0-9.]*' | cut -d: -f2)
    local LAST_LINE=$(echo "$SNAPSHOTS" | tail -1 | xargs grep -o '"line_coverage":[0-9.]*' | cut -d: -f2)
    local TREND=$(echo "$LAST_LINE - $FIRST_LINE" | bc)

    if (( $(echo "$TREND > 0" | bc -l) )); then
        echo -e "${GREEN}📈 Coverage trending upward (+${TREND}%)${NC}"
    elif (( $(echo "$TREND < 0" | bc -l) )); then
        echo -e "${RED}📉 Coverage trending downward (${TREND}%)${NC}"
    else
        echo -e "${BLUE}📊 Coverage stable${NC}"
    fi
}

# Main command handling
case "$COMMAND" in
    "snapshot")
        save_snapshot
        ;;
    "compare")
        compare_branches "$BRANCH_TO_COMPARE"
        ;;
    "trend")
        show_trend "$BRANCH_TO_COMPARE"
        ;;
    "clean")
        echo -e "${YELLOW}► Cleaning coverage history...${NC}"
        rm -rf "$HISTORY_DIR"
        mkdir -p "$HISTORY_DIR"
        echo -e "${GREEN}✓ Coverage history cleaned${NC}"
        ;;
    *)
        echo "Usage: $0 [command] [branch]"
        echo ""
        echo "Commands:"
        echo "  snapshot        Save current coverage snapshot"
        echo "  compare [branch] Compare current branch with target branch (default: main)"
        echo "  trend [branch]   Show coverage trend for branch"
        echo "  clean           Clean coverage history"
        echo ""
        echo "Examples:"
        echo "  $0 snapshot                  # Save current coverage"
        echo "  $0 compare main              # Compare with main branch"
        echo "  $0 trend feature-branch      # Show trend for feature-branch"
        exit 1
        ;;
esac

echo ""
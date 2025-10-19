#!/bin/bash

# ==============================================================================
# Coverage Trend Tracker for ProbotSharp
# ==============================================================================
# Tracks coverage metrics over time to visualize trends and improvements
# Stores historical data in JSON format for analysis and reporting
#
# Usage:
#   ./scripts/coverage/track-trends.sh <command> [options]
#
# Commands:
#   record    Record current coverage metrics
#   show      Display recent trends
#   graph     Generate ASCII graph of trends
#   export    Export trend data as CSV
#   clean     Clean old trend data (keep last 30 entries)
#
# Examples:
#   ./scripts/coverage/track-trends.sh record
#   ./scripts/coverage/track-trends.sh show --last 10
#   ./scripts/coverage/track-trends.sh graph --metric line
# ==============================================================================

set -euo pipefail

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Project paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
COVERAGE_DIR="$PROJECT_ROOT/coverage-report"
TRENDS_DIR="$PROJECT_ROOT/.coverage-trends"
TRENDS_FILE="$TRENDS_DIR/trends.json"

# Ensure trends directory exists
mkdir -p "$TRENDS_DIR"

# Initialize trends file if it doesn't exist
if [[ ! -f "$TRENDS_FILE" ]]; then
    echo '{"entries": []}' > "$TRENDS_FILE"
fi

# Function to extract coverage from summary JSON
extract_coverage_metrics() {
    local json_file="$1"

    if [[ ! -f "$json_file" ]]; then
        echo "{}"
        return
    fi

    # Try to extract from reportgenerator summary
    if jq -e '.summary' "$json_file" &>/dev/null; then
        jq '{
            line: .summary.lineCoverage,
            branch: .summary.branchCoverage,
            coveredLines: .summary.coveredLines,
            coverableLines: .summary.coverableLines,
            coveredBranches: .summary.coveredBranches,
            totalBranches: .summary.totalBranches
        }' "$json_file"
    else
        echo "{}"
    fi
}

# Command: Record current coverage
record_coverage() {
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}   Recording Coverage Metrics${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"

    # Find the most recent coverage summary
    local summary_file=""
    if [[ -f "$COVERAGE_DIR/Summary.json" ]]; then
        summary_file="$COVERAGE_DIR/Summary.json"
    elif [[ -f "$COVERAGE_DIR/coverage.json" ]]; then
        summary_file="$COVERAGE_DIR/coverage.json"
    else
        # Try to find any summary JSON
        summary_file=$(find "$COVERAGE_DIR" -name "Summary.json" -o -name "coverage.json" | head -1)
    fi

    if [[ -z "$summary_file" || ! -f "$summary_file" ]]; then
        echo -e "${RED}✗ No coverage summary found. Run 'make coverage' first.${NC}"
        exit 1
    fi

    echo -e "${YELLOW}► Extracting metrics from: $summary_file${NC}"
    local metrics=$(extract_coverage_metrics "$summary_file")

    if [[ "$metrics" == "{}" ]]; then
        echo -e "${RED}✗ Could not extract coverage metrics${NC}"
        exit 1
    fi

    # Add timestamp and commit info
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    local commit=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")
    local branch=$(git branch --show-current 2>/dev/null || echo "unknown")

    # Create new entry
    local new_entry=$(jq -n \
        --arg timestamp "$timestamp" \
        --arg commit "$commit" \
        --arg branch "$branch" \
        --argjson metrics "$metrics" \
        '{
            timestamp: $timestamp,
            commit: $commit,
            branch: $branch,
            metrics: $metrics
        }')

    # Add to trends file
    jq ".entries += [$new_entry]" "$TRENDS_FILE" > "$TRENDS_FILE.tmp" && mv "$TRENDS_FILE.tmp" "$TRENDS_FILE"

    echo -e "${GREEN}✓ Recorded coverage metrics:${NC}"
    echo "$metrics" | jq -r 'to_entries[] | "  \(.key): \(.value)%"'
    echo -e "  Commit: $commit"
    echo -e "  Branch: $branch"
    echo -e "  Time:   $timestamp"
}

# Command: Show recent trends
show_trends() {
    local last=${1:-10}

    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}   Coverage Trends (Last $last entries)${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"

    if [[ ! -f "$TRENDS_FILE" ]]; then
        echo -e "${RED}✗ No trend data found${NC}"
        exit 1
    fi

    # Get last N entries
    local entries=$(jq ".entries | .[-$last:]" "$TRENDS_FILE")

    if [[ "$entries" == "[]" || "$entries" == "null" ]]; then
        echo -e "${YELLOW}No trend data available yet${NC}"
        exit 0
    fi

    # Display in table format
    printf "%-20s %-10s %-10s %-10s %-10s %-15s\n" "Timestamp" "Line %" "Branch %" "Commit" "Branch" "Delta"
    printf "%s\n" "$(printf '%.0s-' {1..85})"

    local prev_line=0
    echo "$entries" | jq -r '.[] |
        "\(.timestamp) \(.metrics.line // 0) \(.metrics.branch // 0) \(.commit) \(.branch)"' | \
    while read -r timestamp line branch commit git_branch; do
        # Calculate delta
        local delta=""
        if [[ "$prev_line" != "0" ]]; then
            delta=$(echo "$line - $prev_line" | bc)
            if (( $(echo "$delta > 0" | bc -l) )); then
                delta="+$delta"
                color=$GREEN
            elif (( $(echo "$delta < 0" | bc -l) )); then
                color=$RED
            else
                delta="="
                color=$YELLOW
            fi
        else
            color=$NC
        fi

        printf "%-20s %-10s %-10s %-10s %-15s ${color}%-10s${NC}\n" \
            "${timestamp:0:19}" "$line%" "$branch%" "$commit" "${git_branch:0:15}" "$delta"

        prev_line=$line
    done
}

# Command: Generate ASCII graph
generate_graph() {
    local metric=${1:-line}
    local entries=${2:-20}

    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}   Coverage Trend Graph - ${metric^} Coverage${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"

    if [[ ! -f "$TRENDS_FILE" ]]; then
        echo -e "${RED}✗ No trend data found${NC}"
        exit 1
    fi

    # Get data points
    local data=$(jq -r ".entries | .[-$entries:] | .[] | .metrics.$metric // 0" "$TRENDS_FILE" 2>/dev/null)

    if [[ -z "$data" ]]; then
        echo -e "${YELLOW}No data available for metric: $metric${NC}"
        exit 0
    fi

    # Find min and max for scaling
    local max=$(echo "$data" | sort -rn | head -1)
    local min=$(echo "$data" | sort -n | head -1)

    if [[ -z "$max" || "$max" == "0" ]]; then
        echo -e "${YELLOW}Insufficient data for graph${NC}"
        exit 0
    fi

    # Generate ASCII graph
    local height=15
    local width=60

    echo "  100% ┤"

    for row in $(seq $((height - 1)) -1 0); do
        local threshold=$(echo "scale=2; $row * 100 / $height" | bc)
        printf "%5.0f%% │" "$threshold"

        local col=0
        echo "$data" | while read -r value; do
            local scaled=$(echo "scale=0; ($value * $height) / 100" | bc)
            if (( $(echo "$scaled >= $row" | bc -l) )); then
                printf "█"
            else
                printf " "
            fi
            col=$((col + 1))
            if [[ $col -ge $width ]]; then
                break
            fi
        done
        echo
    done

    echo "       └$(printf '%.0s─' {1..60})"
    echo "         $(date -d '-20 days' +%b-%d)                              $(date +%b-%d)"
}

# Command: Export as CSV
export_csv() {
    local output=${1:-"coverage-trends.csv"}

    echo -e "${BLUE}Exporting coverage trends to CSV...${NC}"

    if [[ ! -f "$TRENDS_FILE" ]]; then
        echo -e "${RED}✗ No trend data found${NC}"
        exit 1
    fi

    # Export to CSV
    echo "Timestamp,Commit,Branch,Line Coverage,Branch Coverage,Covered Lines,Total Lines,Covered Branches,Total Branches" > "$output"

    jq -r '.entries[] |
        "\(.timestamp),\(.commit),\(.branch),\(.metrics.line // 0),\(.metrics.branch // 0),\(.metrics.coveredLines // 0),\(.metrics.coverableLines // 0),\(.metrics.coveredBranches // 0),\(.metrics.totalBranches // 0)"' \
        "$TRENDS_FILE" >> "$output"

    echo -e "${GREEN}✓ Exported to: $output${NC}"
}

# Command: Clean old entries
clean_trends() {
    local keep=${1:-30}

    echo -e "${BLUE}Cleaning old trend entries (keeping last $keep)...${NC}"

    if [[ ! -f "$TRENDS_FILE" ]]; then
        echo -e "${YELLOW}No trend data to clean${NC}"
        exit 0
    fi

    local total=$(jq '.entries | length' "$TRENDS_FILE")

    if [[ $total -le $keep ]]; then
        echo -e "${GREEN}✓ No cleanup needed ($total entries)${NC}"
        exit 0
    fi

    # Keep only last N entries
    jq ".entries |= .[-$keep:]" "$TRENDS_FILE" > "$TRENDS_FILE.tmp" && mv "$TRENDS_FILE.tmp" "$TRENDS_FILE"

    local removed=$((total - keep))
    echo -e "${GREEN}✓ Removed $removed old entries${NC}"
}

# Main command dispatch
case "${1:-}" in
    record)
        record_coverage
        ;;
    show)
        show_trends "${2:-10}"
        ;;
    graph)
        generate_graph "${2:-line}" "${3:-20}"
        ;;
    export)
        export_csv "${2:-coverage-trends.csv}"
        ;;
    clean)
        clean_trends "${2:-30}"
        ;;
    *)
        echo "Usage: $0 {record|show|graph|export|clean} [options]"
        echo ""
        echo "Commands:"
        echo "  record          Record current coverage metrics"
        echo "  show [N]        Show last N trends (default: 10)"
        echo "  graph [metric]  Generate ASCII graph (metric: line|branch)"
        echo "  export [file]   Export to CSV file"
        echo "  clean [N]       Keep only last N entries (default: 30)"
        exit 1
        ;;
esac
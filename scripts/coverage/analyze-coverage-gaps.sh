#!/bin/bash
# Analyze coverage gaps and identify areas that need testing

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
REPORT_DIR="$PROJECT_ROOT/coverage-report"
GAP_REPORT="$PROJECT_ROOT/coverage-gaps.md"

echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   ProbotSharp Coverage Gap Analysis${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Check if coverage report exists
if [ ! -d "$REPORT_DIR" ] || [ ! -f "$REPORT_DIR/Summary.json" ]; then
    echo -e "${YELLOW}► No coverage report found. Running coverage analysis first...${NC}"
    echo ""
    "$SCRIPT_DIR/run-coverage-all.sh"
    echo ""
fi

# Function to analyze OpenCover format for detailed information
analyze_opencover() {
    local OPENCOVER_FILE=$(find "$COVERAGE_DIR" -name "coverage.opencover.xml" -type f | head -1)

    if [ -z "$OPENCOVER_FILE" ]; then
        echo -e "${YELLOW}No OpenCover format file found for detailed analysis${NC}"
        return 1
    fi

    echo -e "${CYAN}Analyzing coverage details from OpenCover format...${NC}"
    echo ""

    # Parse uncovered methods
    echo "## Uncovered Methods" > "$GAP_REPORT.tmp"
    echo "" >> "$GAP_REPORT.tmp"

    # Extract methods with 0% coverage
    grep -E '<Method.*SequenceCoverage="0"' "$OPENCOVER_FILE" | \
        sed -E 's/.*Name="([^"]+)".*/\1/' | \
        sort -u | \
        while read -r method; do
            # Extract class name if available
            echo "- \`$method\`" >> "$GAP_REPORT.tmp"
        done

    # Count uncovered methods
    local UNCOVERED_COUNT=$(grep -E '<Method.*SequenceCoverage="0"' "$OPENCOVER_FILE" | wc -l)
    echo "" >> "$GAP_REPORT.tmp"
    echo "_Total uncovered methods: ${UNCOVERED_COUNT}_" >> "$GAP_REPORT.tmp"
    echo "" >> "$GAP_REPORT.tmp"
}

# Function to analyze JSON summary for high-level gaps
analyze_summary() {
    if [ ! -f "$REPORT_DIR/Summary.json" ]; then
        echo -e "${RED}Summary.json not found${NC}"
        return 1
    fi

    echo -e "${CYAN}Analyzing coverage summary...${NC}"

    # Extract coverage metrics
    local LINE_COVERAGE=$(grep -o '"linecoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
    local BRANCH_COVERAGE=$(grep -o '"branchcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
    local METHOD_COVERAGE=$(grep -o '"methodcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2 || echo "0")

    # Calculate gaps
    local LINE_GAP=$(echo "100 - $LINE_COVERAGE" | bc)
    local BRANCH_GAP=$(echo "100 - $BRANCH_COVERAGE" | bc)

    {
        echo "# ProbotSharp Coverage Gap Analysis"
        echo ""
        echo "Generated: $(date)"
        echo ""
        echo "## Overall Coverage Metrics"
        echo ""
        echo "| Metric | Coverage | Gap | Target |"
        echo "|--------|----------|-----|--------|"
        echo "| Line Coverage | ${LINE_COVERAGE}% | ${LINE_GAP}% | 80% |"
        echo "| Branch Coverage | ${BRANCH_COVERAGE}% | ${BRANCH_GAP}% | 75% |"
        if [ "$METHOD_COVERAGE" != "0" ]; then
            local METHOD_GAP=$(echo "100 - $METHOD_COVERAGE" | bc)
            echo "| Method Coverage | ${METHOD_COVERAGE}% | ${METHOD_GAP}% | 85% |"
        fi
        echo ""
    } > "$GAP_REPORT"
}

# Function to find files with low coverage
find_low_coverage_files() {
    echo -e "${CYAN}Identifying files with low coverage...${NC}"

    {
        echo "## Files with Low Coverage (<50%)"
        echo ""
        echo "| File | Line Coverage | Priority |"
        echo "|------|--------------|----------|"
    } >> "$GAP_REPORT"

    # Parse Cobertura XML for file-level coverage
    local COBERTURA_FILE=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -type f | head -1)

    if [ -n "$COBERTURA_FILE" ]; then
        # Extract file coverage data
        grep -E '<class.*line-rate=' "$COBERTURA_FILE" | while read -r line; do
            local FILE_NAME=$(echo "$line" | sed -E 's/.*filename="([^"]+)".*/\1/')
            local LINE_RATE=$(echo "$line" | sed -E 's/.*line-rate="([^"]+)".*/\1/')
            local LINE_PERCENT=$(echo "$LINE_RATE * 100" | bc)

            # Only show files with less than 50% coverage
            if (( $(echo "$LINE_PERCENT < 50" | bc -l) )); then
                # Determine priority based on file location
                local PRIORITY="Low"
                if [[ "$FILE_NAME" == *"Domain"* ]]; then
                    PRIORITY="High"
                elif [[ "$FILE_NAME" == *"Application"* ]]; then
                    PRIORITY="Medium"
                fi

                echo "| ${FILE_NAME##*/} | ${LINE_PERCENT}% | $PRIORITY |" >> "$GAP_REPORT"
            fi
        done
    fi

    echo "" >> "$GAP_REPORT"
}

# Function to generate testing recommendations
generate_recommendations() {
    echo -e "${CYAN}Generating testing recommendations...${NC}"

    {
        echo "## Testing Recommendations"
        echo ""
        echo "### Priority 1: Domain Layer"
        echo "Focus on achieving >90% coverage in the Domain layer as it contains core business logic:"
        echo "- Value Objects: Ensure all validation paths are tested"
        echo "- Domain Services: Test all business rule implementations"
        echo "- Specifications: Test composition (And/Or/Not) scenarios"
        echo ""
        echo "### Priority 2: Application Layer"
        echo "Target >80% coverage for use cases and command handlers:"
        echo "- Use Cases: Test success and failure paths"
        echo "- Port implementations: Mock and verify interactions"
        echo "- Event handlers: Test all webhook event scenarios"
        echo ""
        echo "### Priority 3: Infrastructure Layer"
        echo "Achieve >70% coverage with focus on critical paths:"
        echo "- Adapters: Test error handling and resilience"
        echo "- External integrations: Use integration tests"
        echo "- Caching: Test cache hits, misses, and expiration"
        echo ""
        echo "### Testing Strategies"
        echo ""
        echo "1. **Unit Tests**: For Domain and Application layers"
        echo "2. **Integration Tests**: For Infrastructure adapters"
        echo "3. **Contract Tests**: For API endpoints"
        echo "4. **Property-Based Tests**: For value objects and specifications"
        echo ""
    } >> "$GAP_REPORT"
}

# Function to create actionable tasks
create_action_items() {
    echo -e "${CYAN}Creating action items...${NC}"

    {
        echo "## Action Items"
        echo ""
        echo "### Immediate Actions"
        echo "- [ ] Add unit tests for uncovered Domain value objects"
        echo "- [ ] Increase branch coverage in Application use cases"
        echo "- [ ] Add integration tests for Infrastructure adapters"
        echo ""
        echo "### Short-term Goals (1-2 weeks)"
        echo "- [ ] Achieve 90% line coverage in Domain layer"
        echo "- [ ] Achieve 80% line coverage in Application layer"
        echo "- [ ] Set up mutation testing to verify test quality"
        echo ""
        echo "### Long-term Goals (1 month)"
        echo "- [ ] Achieve overall 80% line coverage"
        echo "- [ ] Achieve overall 75% branch coverage"
        echo "- [ ] Implement coverage gates in CI/CD pipeline"
        echo ""
    } >> "$GAP_REPORT"
}

# Main execution
echo -e "${YELLOW}► Analyzing coverage summary...${NC}"
analyze_summary

echo -e "${YELLOW}► Finding low coverage files...${NC}"
find_low_coverage_files

echo -e "${YELLOW}► Analyzing detailed coverage gaps...${NC}"
if [ -f "$GAP_REPORT.tmp" ]; then
    cat "$GAP_REPORT.tmp" >> "$GAP_REPORT"
    rm "$GAP_REPORT.tmp"
fi

echo -e "${YELLOW}► Generating recommendations...${NC}"
generate_recommendations

echo -e "${YELLOW}► Creating action items...${NC}"
create_action_items

echo ""
echo -e "${GREEN}✅ Coverage gap analysis complete!${NC}"
echo ""
echo -e "${BLUE}Report saved to: ${NC}$GAP_REPORT"
echo ""

# Display summary
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   Key Findings${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Extract and display key metrics
if [ -f "$REPORT_DIR/Summary.json" ]; then
    LINE_COVERAGE=$(grep -o '"linecoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)
    BRANCH_COVERAGE=$(grep -o '"branchcoverage":[0-9.]*' "$REPORT_DIR/Summary.json" | head -1 | cut -d: -f2)

    echo -e "Current Line Coverage:   ${YELLOW}${LINE_COVERAGE}%${NC}"
    echo -e "Current Branch Coverage: ${YELLOW}${BRANCH_COVERAGE}%${NC}"
    echo ""

    # Check against targets
    if (( $(echo "$LINE_COVERAGE < 80" | bc -l) )); then
        LINES_NEEDED=$(echo "80 - $LINE_COVERAGE" | bc)
        echo -e "${RED}⚠ Need ${LINES_NEEDED}% more line coverage to reach 80% target${NC}"
    else
        echo -e "${GREEN}✓ Line coverage meets 80% target${NC}"
    fi

    if (( $(echo "$BRANCH_COVERAGE < 75" | bc -l) )); then
        BRANCHES_NEEDED=$(echo "75 - $BRANCH_COVERAGE" | bc)
        echo -e "${RED}⚠ Need ${BRANCHES_NEEDED}% more branch coverage to reach 75% target${NC}"
    else
        echo -e "${GREEN}✓ Branch coverage meets 75% target${NC}"
    fi
fi

echo ""
echo -e "${CYAN}View the full report: ${BLUE}cat $GAP_REPORT${NC}"
echo ""
#!/bin/bash

# Script to run all fuzzer types for Neo.IO
# This script runs each fuzzer target type in sequence and provides a summary of the results

# ANSI color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Progress bar function
show_progress() {
    local current=$1
    local total=$2
    local width=50
    local percentage=$((current * 100 / total))
    local completed=$((width * current / total))
    local remaining=$((width - completed))
    
    printf "\r[${GREEN}"
    for ((i=0; i<completed; i++)); do
        printf "█"
    done
    printf "${NC}"
    for ((i=0; i<remaining; i++)); do
        printf "░"
    done
    printf "${NC}] ${BLUE}%d%%${NC} (%d/%d)" $percentage $current $total
}

echo -e "${BLUE}===== Neo.IO Fuzzer - Running All Target Types =====${NC}"
echo -e "${YELLOW}Started at $(date)${NC}"
echo ""

# Set common parameters
ITERATIONS=10000  # Reduced iterations for faster testing
CACHE_ITERATIONS=1000  # Even fewer iterations for cache strategies
REPORT_INTERVAL=1000
TIMEOUT=10000  # 10 seconds timeout per test
OUTPUT_DIR="./fuzzer_results"
mkdir -p "$OUTPUT_DIR"

# Known problematic cache strategies
# Previously included BasicCacheFuzzStrategy and CapacityFuzzStrategy, but they have been fixed
PROBLEMATIC_STRATEGIES=(
    # Empty list as we've fixed the previously problematic strategies
)

# Function to check if a strategy is in the problematic list
is_problematic_strategy() {
    local strategy=$1
    for prob in "${PROBLEMATIC_STRATEGIES[@]}"; do
        if [ "$strategy" = "$prob" ]; then
            return 0
        fi
    done
    return 1
}

# Function to run a fuzzer and capture its output
run_fuzzer() {
    local target_type=$1
    local target_class=$2
    local output_file="$OUTPUT_DIR/${target_type}_${target_class// /_}_results.txt"
    
    echo -e "${BLUE}Running fuzzer with target type: ${GREEN}$target_type${NC}"
    
    if [ -n "$target_class" ]; then
        echo -e "Target class: ${GREEN}$target_class${NC}"
        
        # Skip known problematic cache targets
        if [ "$target_type" = "Cache" ] && is_problematic_strategy "$target_class"; then
            echo -e "${YELLOW}Skipping known problematic target: $target_type with $target_class${NC}"
            echo -e "${YELLOW}This target is currently being investigated for issues.${NC}"
            return
        fi
        
        echo -e "Starting with ${YELLOW}$CACHE_ITERATIONS${NC} iterations at $(date)"
        
        # Run the fuzzer and capture output
        dotnet run --configuration Release -- -t "$target_type" -c "$target_class" -i $CACHE_ITERATIONS --report-interval $REPORT_INTERVAL --timeout $TIMEOUT > "$output_file" 2>&1
        
        # Check if the process completed successfully
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}Completed successfully at $(date)${NC}"
        else
            echo -e "${RED}Failed with error code $?${NC}"
            echo -e "${RED}Last 5 lines of output:${NC}"
            tail -n 5 "$output_file" | while read line; do
                echo -e "  ${RED}$line${NC}"
            done
        fi
    else
        echo -e "Starting with ${YELLOW}$ITERATIONS${NC} iterations at $(date)"
        
        # Run the fuzzer and capture output
        dotnet run --configuration Release -- -t "$target_type" -i $ITERATIONS --report-interval $REPORT_INTERVAL --timeout $TIMEOUT > "$output_file" 2>&1
        
        # Check if the process completed successfully
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}Completed successfully at $(date)${NC}"
        else
            echo -e "${RED}Failed with error code $?${NC}"
            echo -e "${RED}Last 5 lines of output:${NC}"
            tail -n 5 "$output_file" | while read line; do
                echo -e "  ${RED}$line${NC}"
            done
        fi
    fi
    
    # Extract and display summary
    echo -e "  ${GREEN}Results saved to $output_file${NC}"
    
    if [ -f "$output_file" ]; then
        echo -e "  ${YELLOW}Summary:${NC}"
        if grep -q "Final Statistics" "$output_file"; then
            grep -A 10 "Final Statistics" "$output_file" | tail -n 6 | while read line; do
                echo -e "  ${BLUE}$line${NC}"
            done
        else
            echo -e "  ${RED}No final statistics found in output file.${NC}"
        fi
    else
        echo -e "  ${RED}Output file not found.${NC}"
    fi
    
    echo ""
}

# Run each fuzzer type
echo -e "${YELLOW}1. SerializableSpan Target${NC}"
run_fuzzer "SerializableSpan"

echo -e "${YELLOW}2. Serializable Target${NC}"
run_fuzzer "Serializable"

echo -e "${YELLOW}3. Composite Target${NC}"
run_fuzzer "Composite"

echo -e "${YELLOW}4. Differential Target${NC}"
run_fuzzer "Differential"

echo -e "${YELLOW}5. Stateful Target${NC}"
run_fuzzer "Stateful"

echo -e "${YELLOW}6. Performance Target${NC}"
run_fuzzer "Performance"

# Run working cache fuzzing strategies
echo -e "${YELLOW}7. Cache Target - Key-Value Mutation Strategy${NC}"
run_fuzzer "Cache" "KeyValueMutationStrategy"

echo -e "${YELLOW}8. Cache Target - Concurrency Strategy${NC}"
run_fuzzer "Cache" "ConcurrencyFuzzStrategy"

echo -e "${YELLOW}9. Cache Target - Composite Strategy${NC}"
run_fuzzer "Cache" "CompositeCacheFuzzStrategy"

echo -e "${YELLOW}10. Cache Target - Basic Strategy${NC}"
run_fuzzer "Cache" "BasicCacheFuzzStrategy"

echo -e "${YELLOW}11. Cache Target - Capacity Strategy${NC}"
run_fuzzer "Cache" "CapacityFuzzStrategy"

# Generate summary report
echo -e "${BLUE}===== Summary Report =====${NC}"
echo -e "${YELLOW}All fuzzer runs completed at $(date)${NC}"
echo ""
echo -e "${BLUE}Coverage points by target type:${NC}"

# Check if there are any result files
if ls "$OUTPUT_DIR"/*_results.txt 1> /dev/null 2>&1; then
    for file in "$OUTPUT_DIR"/*_results.txt; do
        target=$(basename "$file" _results.txt)
        if grep -q "Coverage points:" "$file"; then
            coverage=$(grep "Coverage points:" "$file" | tail -n 1 | awk '{print $3}')
            echo -e "  ${GREEN}$target${NC}: ${YELLOW}$coverage points${NC}"
        else
            echo -e "  ${GREEN}$target${NC}: ${RED}No coverage data available${NC}"
        fi
    done
else
    echo -e "${RED}No result files found.${NC}"
fi

echo ""
echo -e "${BLUE}All results are available in the $OUTPUT_DIR directory${NC}"

# Create a consolidated report
REPORT_FILE="$OUTPUT_DIR/consolidated_report.md"
echo "# Neo.IO.Fuzzer Consolidated Report" > $REPORT_FILE
echo "" >> $REPORT_FILE
echo "Report generated at: $(date)" >> $REPORT_FILE
echo "" >> $REPORT_FILE
echo "## Test Configuration" >> $REPORT_FILE
echo "" >> $REPORT_FILE
echo "- Iterations per target: $ITERATIONS" >> $REPORT_FILE
echo "- Cache iterations: $CACHE_ITERATIONS" >> $REPORT_FILE
echo "- Report interval: $REPORT_INTERVAL" >> $REPORT_FILE
echo "- Timeout per test: $TIMEOUT ms" >> $REPORT_FILE
echo "" >> $REPORT_FILE

# Add information about skipped targets
echo "## Skipped Targets" >> $REPORT_FILE
echo "" >> $REPORT_FILE
echo "No targets were skipped." >> $REPORT_FILE
echo "" >> $REPORT_FILE

echo "## Summary of Results" >> $REPORT_FILE
echo "" >> $REPORT_FILE
echo "| Target Type | Strategy | Tests Executed | Interesting Inputs | Corpus Size | Crashes | Coverage Points |" >> $REPORT_FILE
echo "|------------|----------|---------------|-------------------|------------|---------|-----------------|" >> $REPORT_FILE

# Check if there are any result files
if ls "$OUTPUT_DIR"/*_results.txt 1> /dev/null 2>&1; then
    for file in "$OUTPUT_DIR"/*_results.txt; do
        target=$(basename "$file" _results.txt)
        
        # Extract metrics if available
        tests=""
        interesting=""
        corpus=""
        crashes=""
        coverage=""
        
        if grep -q "Tests executed:" "$file"; then
            tests=$(grep "Tests executed:" "$file" | tail -n 1 | awk '{print $3}')
        fi
        
        if grep -q "Interesting inputs:" "$file"; then
            interesting=$(grep "Interesting inputs:" "$file" | tail -n 1 | awk '{print $3}')
        fi
        
        if grep -q "Corpus size:" "$file"; then
            corpus=$(grep "Corpus size:" "$file" | tail -n 1 | awk '{print $3}')
        fi
        
        if grep -q "Crashes:" "$file"; then
            crashes=$(grep "Crashes:" "$file" | tail -n 1 | awk '{print $3}')
        fi
        
        if grep -q "Coverage points:" "$file"; then
            coverage=$(grep "Coverage points:" "$file" | tail -n 1 | awk '{print $3}')
        fi
        
        # Split target into type and strategy
        target_type=$(echo $target | cut -d'_' -f1)
        target_strategy=$(echo $target | cut -d'_' -f2- | sed 's/_/ /g')
        
        echo "| $target_type | $target_strategy | $tests | $interesting | $corpus | $crashes | $coverage |" >> $REPORT_FILE
    done
else
    echo "No result files found." >> $REPORT_FILE
fi

echo "" >> $REPORT_FILE
echo "## Troubleshooting Notes" >> $REPORT_FILE
echo "" >> $REPORT_FILE
echo "### Common Issues" >> $REPORT_FILE
echo "" >> $REPORT_FILE
echo "No known issues at this time." >> $REPORT_FILE
echo "" >> $REPORT_FILE

echo -e "${GREEN}Consolidated report generated at $REPORT_FILE${NC}"

# Print troubleshooting information
echo -e "${BLUE}===== Troubleshooting Information =====${NC}"
echo -e "${YELLOW}Known Issues:${NC}"
echo -e "  ${RED}None${NC}"
echo -e ""
echo -e "${BLUE}All results are available in the $OUTPUT_DIR directory${NC}"

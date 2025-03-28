# Running the Neo.IO.Fuzzer

## Overview
This document provides instructions for building and running the Neo.IO.Fuzzer, including common command-line options and example configurations.

## Building the Fuzzer
Before running the fuzzer, you need to build the project:

```bash
# Navigate to the Neo.IO.Fuzzer directory
cd /Users/jinghuiliao/git/neo-pr/neo/fuzzers/Neo.IO.Fuzzer

# Build the project in Release mode
dotnet build -c Release
```

## Running the Fuzzer
After building, you can run the fuzzer with various configurations:

### Basic Usage
```bash
dotnet run -- -t SerializableSpan -i 1000
```

### Command-Line Options
The fuzzer supports the following command-line options:

| Option | Description | Default |
|--------|-------------|---------|
| `-t, --target-type` | Type of target to fuzz (SerializableSpan, Composite, Differential, Stateful, Performance) | Required |
| `-c, --target-class` | Target class to fuzz or comma-separated list of classes | Empty |
| `-i, --iterations` | Number of iterations to run | 1000 |
| `-s, --seed` | Random seed (0 for random) | 0 |
| `--seed-count` | Number of seed inputs to generate if corpus is empty | 100 |
| `--max-input-size` | Maximum size of generated inputs in bytes | 10240 |
| `--max-mutations` | Maximum number of mutations to apply to an input | 5 |
| `--corpus-selection-probability` | Probability of selecting an input from the corpus vs generating a new one | 0.8 |
| `--timeout` | Timeout for test execution in milliseconds | 5000 |
| `--corpus-dir` | Directory for storing corpus files | "./corpus" |
| `--crash-dir` | Directory for storing crash files | "./crashes" |
| `--report-file` | File to write reports to | Empty |
| `--enable-enhanced-coverage` | Enable enhanced coverage tracking | False |
| `--enable-guided-mutation` | Enable guided mutation based on coverage | False |
| `--enable-neo-serialization-mutator` | Enable Neo.IO-specific serialization mutator | False |
| `--performance-baseline-iterations` | Number of iterations for performance baseline | 100 |
| `--performance-anomaly-threshold` | Threshold for performance anomalies | 5.0 |

## Example Configurations

### Fuzzing SerializableSpan Implementations
```bash
dotnet run -- -t SerializableSpan -c "Neo.UInt160" -i 5000 --enable-enhanced-coverage --enable-guided-mutation
```

### Fuzzing Multiple Targets with Composite Target
```bash
dotnet run -- -t Composite -c "Neo.UInt160,Neo.UInt256,Neo.Cryptography.ECC.ECPoint" -i 3000
```

### Performance Fuzzing
```bash
dotnet run -- -t Performance -c "Neo.UInt160" -i 2000 --performance-baseline-iterations 200 --performance-anomaly-threshold 3.0
```

### Stateful Fuzzing
```bash
dotnet run -- -t Stateful -c "Neo.IO.Fuzzer.StatefulFuzzerState" -i 5000
```

## Monitoring and Results
- Progress reports are displayed in the console at regular intervals
- Crashes and interesting inputs are saved to the specified directories
- Detailed reports can be written to a file using the `--report-file` option

# Neo.IO.Fuzzer

A comprehensive fuzzing tool for testing the Neo.IO namespace in the Neo blockchain project.

## Overview

Neo.IO.Fuzzer is designed to identify potential issues in the Neo.IO namespace through systematic fuzzing of serialization, deserialization, and memory handling operations. The fuzzer generates both valid and invalid binary inputs to test the robustness and security of Neo.IO components.

## Features

- **Structure-aware fuzzing** of Neo.IO serialization formats
- **Comprehensive coverage tracking** to ensure thorough testing
- **Multiple fuzzing strategies** including bit flipping, boundary testing, and format corruption
- **Targeted testing** of specific Neo.IO components (MemoryReader, ISerializable, ISerializableSpan)
- **Class-agnostic serialization testing** with RandomSerializationTarget
- **Detailed reporting** of issues found during fuzzing
- **Integration with CI/CD pipelines** for continuous security testing

## Getting Started

### Prerequisites

- .NET SDK 8.0 or later
- Neo project source code

### Building

```bash
dotnet build
```

### Running

Basic usage:

```bash
dotnet run -- --iterations 1000 --output-dir ./results
```

Advanced usage:

```bash
dotnet run -- --target MemoryReader --strategy BitFlip,ValueMutation --seed 12345 --iterations 5000 --output-dir ./results --verbose
```

## Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `-t, --target-type` | Target type to fuzz (SerializableSpan, Serializable, Composite, Differential, Stateful, Performance, Cache) | SerializableSpan |
| `-c, --target-class` | Target class to fuzz (for SerializableSpan/Serializable), comma-separated list of classes (for Composite), or strategy name (for Cache: BasicCacheFuzzStrategy, KeyValueMutationStrategy, etc.) | |
| `-i, --iterations` | Number of fuzzing iterations to perform | 1000 |
| `-s, --seed` | Random seed for reproducible fuzzing | 0 (random) |
| `--seed-count` | Number of seed inputs to generate if corpus is empty | 100 |
| `--max-input-size` | Maximum size of generated inputs in bytes | 10240 |
| `--max-mutations` | Maximum number of mutations to apply to an input | 5 |
| `--corpus-selection-probability` | Probability of selecting an input from the corpus vs generating a new one | 0.8 |
| `--timeout` | Timeout in milliseconds for each fuzzing iteration | 5000 |
| `--report-interval` | Interval for progress reporting | 100 |
| `--corpus-dir` | Directory for storing corpus inputs | |
| `--crash-dir` | Directory for storing crash inputs | |
| `--report-file` | File for detailed reporting | |
| `--verbose` | Enable verbose output | false |
| `--ci-mode` | Format output for CI systems | false |

## Target Types

The fuzzer supports multiple target types for comprehensive testing:

- **SerializableSpan**: Tests classes that implement the ISerializableSpan interface
- **Serializable**: Tests classes that implement the ISerializable interface
- **MemoryReader**: Tests the MemoryReader class and its operations
- **RandomSerialization**: Tests raw serialization and deserialization without specific class types
- **Composite**: Tests multiple targets simultaneously for more efficient fuzzing
- **Differential**: Tests multiple implementations of the same interface to find discrepancies
- **Stateful**: Tests stateful operations on objects to find issues with state tracking
- **Performance**: Tests performance characteristics to identify anomalies
- **Cache**: Tests cache implementations with various strategies to ensure proper behavior

## Fuzzing Strategies

The fuzzer supports multiple strategies that can be combined for comprehensive testing:

- **BitFlip**: Randomly flips bits in the input
- **ByteFlip**: Randomly flips bytes in the input
- **ValueMutation**: Replaces values with boundary cases or special values
- **StructureMutation**: Modifies the structure of the input
- **EndiannessMutation**: Swaps endianness of multi-byte values

### Cache Fuzzing Strategies

For the Cache target type, the following specialized strategies are available:

- **BasicCacheFuzzStrategy**: Tests fundamental cache operations (add, get, remove)
- **KeyValueMutationStrategy**: Tests various key-value mutations and edge cases
- **ConcurrencyFuzzStrategy**: Tests thread safety and concurrent operations
- **CapacityFuzzStrategy**: Tests cache capacity limits and eviction policies
- **CompositeCacheFuzzStrategy**: Combines multiple strategies for comprehensive testing

Each cache strategy can be run with the following command:

```bash
dotnet run -- -t cache -c BasicCacheFuzzStrategy -i 500 --report-interval 100
```

## Output Format

The fuzzer generates several types of output files:

- **summary.json**: Summary of the fuzzing run
- **issues.json**: Details of any issues found
- **coverage.json**: Coverage information
- **corpus/**: Directory containing interesting inputs
- **crashes/**: Directory containing inputs that caused crashes

## Documentation

Detailed documentation is available in the `Documentation` directory:

- [Architecture Overview](Documentation/README.md): Overview of the fuzzer architecture and components
- [Fuzzing Strategies](Documentation/FuzzingStrategies.md): Detailed explanation of fuzzing strategies
- [Implementation Architecture](Documentation/ImplementationArchitecture.md): Details of the implementation
- [Coverage Tracking](Documentation/CoverageTracking.md): How code coverage is tracked and utilized
- [Mutation Components](Documentation/MutationComponents.md): Documentation of mutation engine components
- [Cache Fuzzing Troubleshooting](Documentation/CacheFuzzingTroubleshooting.md): Guide for resolving common issues with cache fuzzing strategies

For information on specific targets and advanced features, see the [Documentation Structure](Documentation/README.md#documentation-structure) section in the documentation overview.

## Contributing

Contributions to the Neo.IO.Fuzzer are welcome. Please follow these guidelines:

1. Follow the documentation-first approach: update or create documentation before implementing changes
2. Ensure all code changes are covered by tests
3. Maintain consistent code style with the rest of the project
4. Submit pull requests with clear descriptions of changes

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Neo Project Team
- Fuzzing community for best practices and techniques

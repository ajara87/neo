# Command Line Options

This document details the command-line options available for the Neo.IO.Fuzzer.

## Basic Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--target-type` | `-t` | Type of target to fuzz (SerializableSpan, Serializable, Composite, Differential, Stateful, Performance, Cache) | Required |
| `--target-class` | `-c` | Target class to fuzz (for SerializableSpan) | Empty |
| `--target-strategy` | | Strategy name for cache fuzzing | Empty |
| `--iterations` | `-i` | Number of iterations to run | 1000 |
| `--seed` | `-s` | Random seed (0 for random) | 0 |
| `--timeout` | | Timeout for test execution in milliseconds | 5000 |
| `--report-interval` | | Interval for progress reporting in iterations | 100 |
| `--report-file` | | Path to file for detailed reporting | Empty |

## Corpus Options

| Option | Description | Default |
|--------|-------------|---------|
| `--corpus-dir` | Directory for storing interesting inputs | ./corpus |
| `--crash-dir` | Directory for storing inputs that cause crashes | ./crashes |
| `--seed-count` | Number of seed inputs to generate if corpus is empty | 100 |
| `--max-input-size` | Maximum size of generated inputs in bytes | 10240 |
| `--corpus-selection-probability` | Probability of selecting an input from the corpus vs generating a new one | 0.8 |

## Mutation Options

| Option | Description | Default |
|--------|-------------|---------|
| `--max-mutations` | Maximum number of mutations to apply to an input | 5 |
| `--guided-mutation` | Enable coverage-guided mutation | false |
| `--neo-serialization-mutator` | Enable Neo.IO-specific serialization mutator | true |

## Coverage Options

| Option | Description | Default |
|--------|-------------|---------|
| `--enhanced-coverage` | Enable enhanced coverage tracking | false |
| `--coverage-file` | Path to file for coverage reporting | Empty |

## Performance Options

| Option | Description | Default |
|--------|-------------|---------|
| `--performance-baseline-iterations` | Number of iterations for establishing performance baseline | 100 |
| `--performance-anomaly-threshold` | Threshold for detecting performance anomalies (multiplier of baseline) | 2.0 |

## Cache Fuzzing Options

| Option | Description | Default |
|--------|-------------|---------|
| `--use-enhanced` | Use enhanced strategies for cache fuzzing | true |

## Target-Specific Formats

### Cache Target

For the cache target, the `--target-strategy` option specifies the strategy to use:

```
--target-type cache --target-strategy Basic
```

This will use the enhanced Basic strategy by default. To use the original strategy:

```
--target-type cache --target-strategy Basic:original
```

Or you can explicitly specify to use the enhanced strategy:

```
--target-type cache --target-strategy Basic:enhanced
```

You can also use the `--use-enhanced` option to control the default behavior:

```
--target-type cache --target-strategy Basic --use-enhanced false
```

Available cache fuzzing strategies:

- `Basic`: Tests fundamental cache operations
- `Capacity`: Tests capacity management and eviction policies
- `Concurrency`: Tests thread safety with parallel operations
- `StateTracking`: Tests state tracking capabilities
- `KeyValueMutation`: Tests with various key and value types
- `Composite`: Combines multiple strategies

### Differential Target

For the differential target, the `--target-class` option specifies the implementations to compare:

```
--target-type differential --target-class "Impl1:Namespace.Class1, Impl2:Namespace.Class2"
```

### Composite Target

For the composite target, the `--target-class` option specifies the classes to fuzz:

```
--target-type composite --target-class "Namespace.Class1, Namespace.Class2"
```

## Examples

Test the serializable span implementation with 10,000 iterations:

```
dotnet run -- -t serializablespan -i 10000
```

Test the cache with the enhanced concurrency strategy:

```
dotnet run -- -t cache --target-strategy Concurrency
```

Test the cache with the original capacity strategy:

```
dotnet run -- -t cache --target-strategy Capacity:original
```

Test with enhanced coverage and guided mutation:

```
dotnet run -- -t serializablespan --enhanced-coverage --guided-mutation
```

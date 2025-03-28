# Cache Fuzzing Strategies

This document describes the various fuzzing strategies implemented for testing the Neo caching system. Each strategy focuses on different aspects of the caching system to ensure comprehensive coverage and robust testing.

## Strategy Overview

| Strategy | Purpose | Target Components | Key Features |
|----------|---------|-------------------|-------------|
| Basic | Test fundamental cache operations | All cache types | Add, Get, Remove, Contains operations |
| Capacity | Test capacity management | All cache types | Eviction policies, overflow handling |
| Concurrency | Test thread safety | All cache types | Parallel operations, race condition detection |
| StateTracking | Test state management | DataCache, tracked caches | State transitions, snapshots, commit/rollback |
| KeyValueMutation | Test key/value handling | All cache types | Various key types, value sizes, null handling |
| Composite | Comprehensive testing | All cache types | Combines multiple strategies |

## Strategy Implementations

### Basic Cache Fuzzing Strategy

The `BasicCacheFuzzStrategy` tests fundamental cache operations to ensure the basic functionality works correctly:

- **Add Operations**: Tests adding items to the cache
- **Get Operations**: Tests retrieving items from the cache
- **Remove Operations**: Tests removing items from the cache
- **Contains Operations**: Tests checking if items exist in the cache
- **Clear Operations**: Tests clearing the entire cache
- **Disposable Values**: Tests proper disposal of IDisposable values when removed from cache

This strategy provides a baseline for cache functionality testing and is suitable for all cache implementations.

### Capacity Fuzzing Strategy

The `CapacityFuzzStrategy` focuses on testing the cache's capacity management and eviction policies:

- **Capacity Limits**: Tests that the cache respects its maximum capacity
- **Eviction Policies**: Tests that items are evicted according to the cache's policy (e.g., FIFO)
- **Boundary Testing**: Tests edge cases around capacity limits
- **Overflow Handling**: Tests behavior when capacity is exceeded

This strategy is critical for ensuring the cache properly manages memory and resources.

### Concurrency Fuzzing Strategy

The `ConcurrencyFuzzStrategy` tests the thread safety of the cache under concurrent access scenarios:

- **Parallel Operations**: Tests multiple threads performing operations simultaneously
- **Race Condition Detection**: Attempts to trigger race conditions
- **Deadlock Detection**: Tests for potential deadlocks in locking mechanisms
- **Consistency Verification**: Ensures cache remains consistent under concurrent access

This strategy is essential for validating the thread safety guarantees of the caching system.

### State Tracking Fuzzing Strategy

The `StateTrackingFuzzStrategy` focuses on testing the state tracking capabilities of advanced caches like DataCache:

- **State Transitions**: Tests transitions between states (None, Added, Changed, Deleted)
- **Snapshot Creation**: Tests creating and using snapshots
- **Commit Operations**: Tests committing changes to the underlying store
- **Rollback Operations**: Tests rolling back changes

This strategy is specifically designed for caches that implement state tracking functionality.

### Key-Value Mutation Fuzzing Strategy

The `KeyValueMutationStrategy` tests the cache with various types of keys and values:

- **String Keys**: Tests using string-based keys
- **Numeric Keys**: Tests using numeric keys
- **Complex Object Keys**: Tests using complex objects as keys
- **Small/Large Values**: Tests different value sizes
- **Null Value Handling**: Tests handling of null values
- **Disposable Values**: Tests proper disposal of IDisposable values when removed from cache

This strategy ensures the cache handles different data types and edge cases correctly.

### Composite Cache Fuzzing Strategy

The `CompositeCacheFuzzStrategy` combines multiple strategies to provide comprehensive testing:

- **Strategy Composition**: Executes multiple strategies with a single input
- **Input Division**: Divides the input data among the strategies
- **Aggregated Coverage**: Combines coverage information from all strategies
- **Flexible Configuration**: Allows adding or removing strategies

This strategy provides the most thorough testing by leveraging all other strategies.

## Common Utilities

To improve maintainability and reduce code duplication across the cache fuzzing strategies, we've implemented a set of common utilities that standardize key functionality:

### TestCacheBase

The `TestCacheBase<TKey, TValue>` class provides a common foundation for test cache implementations used across all strategies:

```csharp
// Create a string key cache
var stringKeyCache = TestCacheBase<string, byte[]>.CreateStringKeyCache(100);

// Create a numeric key cache
var numericKeyCache = TestCacheBase<int, byte[]>.CreateNumericKeyCache(100);

// Create a string key cache for IDisposable values
var disposableStringKeyCache = TestCacheBase<string, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(TestCacheType.StringKey, TestValueType.Disposable);

// Create a numeric key cache for IDisposable values
var disposableNumericKeyCache = TestCacheBase<int, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(TestCacheType.NumericKey, TestValueType.Disposable);
```

The `TestCacheBase` class now includes:
- A public `Count` property to track the number of items in the cache
- Specialized implementations for handling `IDisposable` objects
- Generic type parameters that avoid naming conflicts
- A default implementation for the `GetKeyForItem` method

This eliminates the need for each strategy to implement its own test cache class, ensuring consistent behavior and reducing maintenance overhead.

### CoverageTrackerHelper

The `CoverageTrackerHelper` class standardizes coverage tracking across all strategies:

```csharp
// Initialize coverage tracker
var coverage = new CoverageTrackerHelper("StrategyName");
coverage.InitializePoints("Add", "Remove", "Contains", "Access");

// Track coverage
coverage.IncrementPoint("Add");
```

This provides thread-safe coverage tracking with consistent reporting formats, making it easier to compare results across strategies.

### InputProcessingUtilities

The `InputProcessingUtilities` class provides standardized methods for processing fuzzing input:

```csharp
// Divide input into chunks
var chunks = InputProcessingUtilities.DivideInput(input);

// Extract parameters from input
int capacity = InputProcessingUtilities.ExtractNumericParameter(input, 0, 1, 100);
```

This ensures consistent input handling across strategies and simplifies the implementation of new strategies.

### ExceptionHandlingUtilities

The `ExceptionHandlingUtilities` class provides standardized exception handling:

```csharp
// Execute with standard exception handling
bool result = ExceptionHandlingUtilities.ExecuteWithExceptionHandling(
    () => PerformOperation(),
    "OperationName",
    coverage);
```

This ensures consistent exception tracking and reporting across all strategies.

## Strategy Implementation Details

All strategies implement the `ICacheFuzzStrategy` interface, which provides a standard way to execute fuzzing operations and retrieve coverage information:

```csharp
public interface ICacheFuzzStrategy
{
    bool Execute(byte[] input);
    object GetCoverage();
    string GetName();
}
```

Additionally, our enhanced strategies implement the `IEnhancedCacheFuzzStrategy` interface, which provides access to the coverage tracker:

```csharp
public interface IEnhancedCacheFuzzStrategy : ICacheFuzzStrategy
{
    CoverageTrackerHelper GetCoverageTracker();
}
```

### DisposableTestObject

The `DisposableTestObject` class is used to test proper disposal of objects in the cache:

```csharp
public class DisposableTestObject : IDisposable
{
    public int Id { get; }
    public byte[] Data { get; }
    public bool IsDisposed { get; private set; }

    public DisposableTestObject(int id, byte[] data)
    {
        Id = id;
        Data = data;
        IsDisposed = false;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
```

This class is used to verify that caches properly dispose of objects when they are removed or when the cache is cleared.

## Running Cache Fuzzing Tests

To run cache fuzzing tests, use the following command:

```bash
dotnet run -t cache -c <strategy_name> -i <iterations> --report-interval <interval>
```

Where:
- `<strategy_name>` is the name of the strategy to run (e.g., BasicCacheFuzzStrategy, KeyValueMutationStrategy)
- `<iterations>` is the number of iterations to run
- `<interval>` is the reporting interval

For example:

```bash
dotnet run -t cache -c BasicCacheFuzzStrategy -i 100 --report-interval 10
```

## Coverage Metrics

The cache fuzzing strategies track various coverage metrics:

- **Operation Coverage**: Tracks which cache operations have been executed
- **Error Handling Coverage**: Tracks which error handling paths have been executed
- **Edge Case Coverage**: Tracks testing of edge cases
- **Success Rates**: Tracks the success rate of operations
- **Disposal Coverage**: Tracks proper disposal of IDisposable objects

Coverage information is reported by each strategy's `GetCoverage()` method and can be aggregated by the `CompositeCacheFuzzStrategy` to provide a comprehensive view of testing coverage.

# Cache Fuzzing Troubleshooting Guide

This document provides information about common issues encountered when running the cache fuzzing strategies and how to resolve them.

## Known Issues and Fixes

### 1. Generic Type Parameter Handling in TestCacheBase

#### Problem Description

The `BasicCacheFuzzStrategy` and `CapacityFuzzStrategy` classes were encountering issues when trying to use the `TestCacheBase.CreateDisposableTestCache<TDisposable>` method. The issue was related to how generic type parameters were handled, particularly with the `DisposableTestObject` type.

Specifically, the following issues were observed:

- Type casting errors when attempting to cast the result of `CreateDisposableTestCache<DisposableTestObject>` to `StringKeyCache<DisposableTestObject>` or `NumericKeyCache<DisposableTestObject>`
- Incorrect nested generic class references like `TestCacheBase<string, TDisposable>.StringKeyCache<TDisposable>`
- The fuzzer would get stuck during execution when using these strategies

#### Solution

We made the following changes to fix these issues:

1. **Updated TestCacheBase.cs**:
   - Fixed the `CreateDisposableTestCache<TDisposable>` method to properly return the correct generic cache implementation
   - Simplified the generic type parameter naming in `StringKeyCache<TValue>` and `NumericKeyCache<TValue>` classes
   - Ensured consistent implementation of the `GetKeyForItem` method across all cache types

2. **Updated BasicCacheFuzzStrategy.cs**:
   - Modified the `TestBasicCacheOperations` method to use reflection for safely interacting with the disposable cache
   - Improved error handling to prevent the fuzzer from getting stuck
   - Added more robust type checking when working with generic cache implementations

3. **Updated CapacityFuzzStrategy.cs**:
   - Rewrote the `ExecuteInternal` method to properly create and use the appropriate cache type
   - Eliminated unsafe type casting that was causing runtime errors
   - Added better error reporting for cache creation failures

#### Verification

Both strategies have been tested with the following command and completed successfully without errors:

```bash
dotnet run --configuration Release -- -t cache -c BasicCacheFuzzStrategy -i 100 --report-interval 10 --timeout 10000
dotnet run --configuration Release -- -t cache -c CapacityFuzzStrategy -i 100 --report-interval 10 --timeout 10000
```

The tests completed 100 iterations with the following results:
- No crashes or errors
- 56 coverage points
- 0% crash ratio

This confirms that our fixes have successfully resolved the issues with generic type parameter handling in both strategies.

### 2. Running Cache Fuzzing Strategies

#### Problem Description

When running the cache fuzzing strategies, users might encounter issues if they don't specify the correct target type or if they try to run problematic strategies.

#### Solution

To properly run the cache fuzzing strategies:

1. Use the correct target type: `-t cache` (not "Cache" or "Composite")
2. Specify the strategy explicitly: `-c KeyValueMutationStrategy`
3. Consider reducing the number of iterations for cache strategies to improve testing speed

Example command:
```bash
dotnet run -- -t cache -c KeyValueMutationStrategy -i 1000 --report-interval 100
```

### 3. Timeout and Hanging Issues

#### Problem Description

Some cache fuzzing strategies might appear to hang or take an extremely long time to complete, especially when dealing with disposable objects or complex cache operations.

#### Solution

We've implemented the following solutions:

1. Added timeouts in the run_all_fuzzers.sh script to prevent indefinite hanging
2. Improved error handling to detect and report stuck processes
3. Added more detailed logging to help diagnose issues
4. Reduced the number of iterations for cache strategies in the run_all_fuzzers.sh script

## Recommended Approach for Cache Fuzzing

For the most reliable results when testing Neo's cache implementations:

1. All cache strategies now work correctly:
   - KeyValueMutationStrategy
   - ConcurrencyFuzzStrategy
   - CompositeCacheFuzzStrategy
   - BasicCacheFuzzStrategy (fixed)
   - CapacityFuzzStrategy (fixed)

2. Use smaller iteration counts initially (100-1,000) to verify functionality

3. For comprehensive testing, the run_all_fuzzers.sh script now includes all strategies

4. Always check the output logs for any error messages or warnings

## Future Improvements

The following improvements are planned for future updates:

1. Further refactor the TestCacheBase class to simplify generic type handling
2. Add more comprehensive exception handling in all cache strategies
3. Implement better progress monitoring and reporting
4. Add more specialized testing for edge cases in cache implementations

## Related Documentation

- [Cache Implementations Overview](../README.md#target-types)
- [Cache Fuzzing Strategies](../README.md#cache-fuzzing-strategies)
- [Running the Fuzzer](../README.md#running)

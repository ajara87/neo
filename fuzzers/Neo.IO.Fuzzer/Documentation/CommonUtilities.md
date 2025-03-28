# Common Utilities for Neo.IO.Fuzzer

This document describes the common utility components designed to reduce code duplication and improve maintainability of the Neo.IO.Fuzzer project. These utilities provide shared functionality across different fuzzing strategies and targets.

## Overview

The Neo.IO.Fuzzer project contains several components with duplicated code patterns, particularly in the cache fuzzing strategies. The common utilities described in this document aim to consolidate these patterns into reusable components.

## Components

### TestCacheBase

The `TestCacheBase` class provides a common base implementation for test caches used across different fuzzing strategies.

#### Purpose

- Eliminate duplicate cache implementations across strategy classes
- Provide consistent cache behavior for testing
- Allow for specialized cache implementations through inheritance

#### Features

- Generic implementation supporting different key and value types
- Configurable capacity management
- Standard key generation through virtual methods
- Consistent exception handling

#### Usage

```csharp
// Basic string key cache
public class StringKeyCache : TestCacheBase<string, byte[]>
{
    public StringKeyCache(int maxCapacity) : base(maxCapacity) { }

    protected override string GetKeyForItem(byte[] item)
    {
        return Convert.ToBase64String(item.Take(Math.Min(8, item.Length)).ToArray());
    }
}

// Numeric key cache
public class NumericKeyCache : TestCacheBase<int, byte[]>
{
    public NumericKeyCache(int maxCapacity) : base(maxCapacity) { }

    protected override int GetKeyForItem(byte[] item)
    {
        return item.Length > 0 ? BitConverter.ToInt32(item, 0) % 1000 : 0;
    }
}
```

### CoverageTrackerHelper

The `CoverageTrackerHelper` class provides standardized coverage tracking functionality for fuzzing strategies.

#### Purpose

- Eliminate duplicate coverage tracking code
- Ensure consistent coverage reporting
- Simplify adding new coverage points

#### Features

- Thread-safe counter incrementation
- Standard coverage point initialization
- Consistent coverage data format
- Merge functionality for combining coverage from different sources

#### Usage

```csharp
// Initialize coverage tracker
var coverage = new CoverageTrackerHelper("StrategyName");
coverage.InitializePoint("Operation1");
coverage.InitializePoint("Operation2");

// Track coverage
coverage.IncrementPoint("Operation1");
coverage.IncrementPoint("Operation2", 5); // Increment by 5

// Get coverage data
var coverageData = coverage.GetCoverage();
```

### InputProcessingUtilities

The `InputProcessingUtilities` class provides common methods for processing fuzzing input data.

#### Purpose

- Eliminate duplicate input processing code
- Provide consistent input handling
- Simplify extraction of test parameters from input bytes

#### Features

- Methods for dividing input into chunks
- Utilities for extracting numeric parameters from input
- Functions for generating test keys and values from input
- Helpers for creating structured test data

#### Usage

```csharp
// Divide input into chunks
var chunks = InputProcessingUtilities.DivideInput(input, chunkCount);

// Extract numeric parameter
int parameter = InputProcessingUtilities.ExtractNumericParameter(input, 0, 1, 100);

// Generate test keys
var keys = InputProcessingUtilities.GenerateTestKeys(input, 10);

// Generate test values
var values = InputProcessingUtilities.GenerateTestValues(input, 10);
```

### ExceptionHandlingUtilities

The `ExceptionHandlingUtilities` class provides standardized exception handling for fuzzing operations.

#### Purpose

- Ensure consistent exception handling across strategies
- Provide detailed logging of exceptions
- Track exception statistics

#### Features

- Standard try-catch patterns
- Exception logging with configurable verbosity
- Exception categorization
- Integration with coverage tracking

#### Usage

```csharp
// Execute operation with standard exception handling
bool result = ExceptionHandlingUtilities.ExecuteWithExceptionHandling(
    () => PerformOperation(),
    "OperationName",
    coverage);

// Execute operation with custom exception handling
bool result = ExceptionHandlingUtilities.ExecuteWithExceptionHandling(
    () => PerformOperation(),
    ex => 
    {
        // Custom exception handling logic
        Console.WriteLine($"Custom handler: {ex.Message}");
        return false;
    },
    "OperationName",
    coverage);
```

## Integration with Existing Components

These utilities are designed to integrate seamlessly with the existing Neo.IO.Fuzzer components:

1. **Strategy Classes**: Will inherit from or use these utilities to reduce code duplication
2. **Target Classes**: Will benefit from standardized exception handling and coverage tracking
3. **Test Harness**: Will receive more consistent coverage and exception data

## Implementation Plan

1. Create the utility classes in a new `Utilities` namespace
2. Refactor one strategy at a time to use the new utilities
3. Update tests to ensure functionality is preserved
4. Update documentation to reflect the new structure

## Benefits

- **Reduced Code Duplication**: Eliminates redundant code across multiple strategies
- **Improved Maintainability**: Changes to common functionality only need to be made in one place
- **Consistent Behavior**: Ensures consistent handling of inputs, exceptions, and coverage
- **Easier Extension**: Makes it easier to add new strategies and targets
- **Better Testing**: Facilitates more comprehensive testing of shared components

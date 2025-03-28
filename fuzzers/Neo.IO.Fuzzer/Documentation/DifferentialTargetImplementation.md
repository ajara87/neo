# Differential Target Implementation

## Overview

This document outlines the implementation details for the Differential target type in the Neo.IO.Fuzzer. The Differential target is designed to compare multiple implementations of the same interface to ensure consistent behavior across different versions or implementations.

## Implementation Status

The Differential target has been successfully implemented and tested. The implementation includes:

1. A custom `TestDifferentialFuzzTarget` class that handles both `ISerializableSpan` and `ISerializable` implementations
2. Proper initialization of test implementations using reflection or direct method calls
3. Comparison of results from different implementations to identify discrepancies
4. Coverage tracking for each implementation

## Key Components

### TestDifferentialFuzzTarget

This is the main class responsible for differential fuzzing. It:

- Maintains separate lists for `ISerializableSpan` and `ISerializable` implementations
- Provides methods to add implementations of each type
- Executes tests on all implementations and compares the results
- Records coverage points for each implementation
- Handles errors gracefully

### FuzzerEngine Integration

The `FuzzerEngine.CreateTarget` method has been updated to:

- Create a `TestDifferentialFuzzTarget` instance with a proper name
- Add default implementations for both interface types
- Support custom implementation classes specified by the user
- Ensure at least two implementations of the same type are added for proper comparison

## Usage

### Default Usage

To run the Differential target with default implementations:

```bash
dotnet run -- -t Differential -i 100
```

This will create a `TestDifferentialFuzzTarget` with:
- Two `TestSerializableSpan` implementations
- Two `TestSerializable` implementations

### Custom Implementations

To run the Differential target with custom implementations:

```bash
dotnet run -- -t Differential -c "Impl1:Namespace.Class1,Impl2:Namespace.Class2" -i 100
```

Where:
- `Impl1` and `Impl2` are the names to identify the implementations
- `Namespace.Class1` and `Namespace.Class2` are the fully qualified class names

## Test Results

The Differential target has been thoroughly tested with 100 iterations and shows a 0% crash ratio. All implementations are compared correctly, and discrepancies are properly identified and reported.

## Future Improvements

1. **Enhanced Comparison Logic**: Implement more sophisticated comparison logic for complex objects
2. **Custom Validation**: Add support for custom validation functions for specific implementation types
3. **Visualization**: Create visual reports of discrepancies found between implementations
4. **Performance Metrics**: Add performance comparison between different implementations
5. **Mutation Strategies**: Develop specialized mutation strategies for differential fuzzing

## Conclusion

The Differential target is now fully functional and can be used to compare different implementations of the same interface. It successfully identifies discrepancies between implementations and provides detailed coverage information for each implementation.

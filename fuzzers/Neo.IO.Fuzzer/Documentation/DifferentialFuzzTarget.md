# Differential Fuzz Target

## Overview
The Differential Fuzz Target is an advanced fuzzing component that compares multiple implementations of the same functionality to identify inconsistencies. By executing the same input across different implementations and comparing their outputs, this target can discover subtle bugs, compatibility issues, and specification violations that might be missed by traditional fuzzing approaches.

## Implementation

The Neo.IO.Fuzzer uses a specialized implementation called `TestDifferentialFuzzTarget` that is designed specifically for testing Neo.IO components. This implementation:

1. Supports both `ISerializableSpan` and `ISerializable` interfaces
2. Properly initializes test implementations using reflection or direct method calls
3. Compares results from different implementations to identify discrepancies
4. Records coverage points for each implementation

## Features

### Multiple Implementation Testing
- Executes the same input across multiple implementations of the same interface
- Compares outputs to identify inconsistencies
- Supports testing of different versions of the same code (e.g., legacy vs. new implementation)

### Comprehensive Comparison Strategies
- Size comparison for basic validation
- Result comparison for functional equivalence
- Exception behavior comparison
- Coverage tracking for each implementation

### Detailed Discrepancy Reporting
- Records when implementations produce different results
- Tracks different types of discrepancies (size differences, result differences, exception differences)
- Provides coverage information for each implementation

## Usage Example

```csharp
// Create a differential target
var differentialTarget = new TestDifferentialFuzzTarget("DifferentialTarget");

// Add implementations to compare
differentialTarget.AddSerializableSpanImplementation("Implementation1", new TestSerializableSpan());
differentialTarget.AddSerializableSpanImplementation("Implementation2", new TestNestedSerializableSpan());

// Execute the differential target with test input
byte[] testInput = GetTestInput();
bool success = differentialTarget.Execute(testInput);

// Get coverage information
var coverage = differentialTarget.GetCoverage();
Console.WriteLine($"Total coverage points: {coverage["TotalPoints"]}");
```

## Integration with FuzzerEngine

The `TestDifferentialFuzzTarget` is fully integrated with the FuzzerEngine, which:

1. Creates the target with a proper name
2. Adds default implementations for both interface types
3. Supports custom implementation classes specified by the user
4. Ensures at least two implementations of the same type are added for proper comparison

## Running the Differential Target

To run the Differential target with default implementations:

```bash
dotnet run -- -t Differential -i 100
```

To run the Differential target with custom implementations:

```bash
dotnet run -- -t Differential -c "Impl1:Namespace.Class1,Impl2:Namespace.Class2" -i 100
```

## Performance Considerations

The Differential Fuzz Target includes optimizations to ensure efficient operation:

- Efficient comparison of results
- Early termination when major discrepancies are found
- Robust error handling to prevent crashes
- Coverage tracking to guide fuzzing efforts

## For More Details

For more detailed information about the implementation and usage of the Differential target, see the [Differential Target Implementation](DifferentialTargetImplementation.md) document.

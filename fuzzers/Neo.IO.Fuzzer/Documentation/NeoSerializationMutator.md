# Neo Serialization Mutator

## Overview
The Neo Serialization Mutator is a specialized mutation component designed to target Neo.IO-specific serialization edge cases. This mutator understands the Neo serialization format and applies mutations that are likely to uncover bugs in code that processes serialized Neo objects.

## Features

### Format-Aware Mutation
- Recognizes Neo serialization formats including VarInt, VarString, and fixed-size arrays
- Preserves overall structure while mutating specific fields
- Maintains format validity while exploring edge cases

### Targeted Edge Cases
- Length field manipulation (maximum values, zero values, inconsistent lengths)
- Boundary testing for integer fields (INT_MAX, INT_MIN, etc.)
- Special byte sequences known to cause issues in deserialization
- Invalid UTF-8 sequences in string fields
- Malformed variable-length integers

### Smart Fuzzing Strategies
- Identifies field boundaries in serialized data
- Applies different mutation strategies based on field type
- Preserves checksums and updates them when necessary
- Generates valid but unexpected object hierarchies

## Usage Example

```csharp
// Create a Neo serialization mutator
var neoMutator = new NeoSerializationMutator();

// Apply Neo-specific mutations to an input
byte[] originalData = GetTestInput();
byte[] mutatedData = neoMutator.Mutate(originalData, random);

// Use in combination with the GuidedMutationEngine
var mutators = new List<IMutator>
{
    new BitFlipMutator(),
    new ByteFlipMutator(),
    new NeoSerializationMutator()
};

var guidedEngine = new GuidedMutationEngine(mutators, random);
```

## Integration with Other Components

The Neo Serialization Mutator works in conjunction with:

1. **GuidedMutationEngine**: Provides feedback on which mutations are effective
2. **EnhancedCoverageTracker**: Helps identify which parts of the code are exercised by specific mutations
3. **FuzzerEngine**: Orchestrates the overall fuzzing process

## Implementation Details

The mutator implements several strategies:

1. **Structure Detection**: Attempts to identify Neo serialization structures in the input
2. **Field Mutation**: Applies targeted mutations to specific fields while preserving overall structure
3. **Boundary Testing**: Generates values at the boundaries of valid ranges
4. **Format Violation**: Deliberately creates malformed data that tests error handling

## Performance Considerations

The Neo Serialization Mutator includes optimizations to ensure efficient operation:

- Uses heuristics to quickly identify serialization formats
- Caches structure information for repeated mutations of similar inputs
- Implements lightweight parsing to avoid full deserialization overhead
- Balances between structure-aware mutations and random mutations

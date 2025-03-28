# Mutation Components

## Overview
The mutation components are responsible for transforming input data during the fuzzing process. These components are essential for exploring the input space effectively and discovering edge cases that might lead to bugs or vulnerabilities in the Neo.IO serialization code.

## Architecture

The mutation system consists of three main components:

1. **IMutator Interface**: Defines the contract for individual mutation strategies
2. **IMutationEngine Interface**: Defines the contract for mutation engines that orchestrate multiple mutators
3. **Concrete Implementations**: Various mutators and engines that implement specific mutation strategies

### Component Diagram

```
┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │
│  IMutationEngine│◄────┤  MutationEngine │
│                 │     │                 │
└────────┬────────┘     └────────┬────────┘
         │                       │
         │                       │
         │                       │
         │                       ▼
         │              ┌─────────────────┐
         │              │                 │
         │              │GuidedMutation   │
         │              │Engine           │
         │              │                 │
         │              └────────┬────────┘
         │                       │
         ▼                       │
┌─────────────────┐              │
│                 │              │
│    IMutator     │◄─────────────┘
│                 │
└────────┬────────┘
         │
         ├─────────┬─────────┬─────────┬─────────┬─────────┐
         │         │         │         │         │         │
         ▼         ▼         ▼         ▼         ▼         ▼
┌─────────────┐ ┌─────────┐ ┌───────────┐ ┌────────────┐ ┌───────────┐ ┌─────────────┐
│             │ │         │ │           │ │            │ │           │ │             │
│BitFlipMutator│ │ByteFlip │ │Endianness │ │Structure   │ │Value      │ │NeoSerialization│
│             │ │Mutator  │ │Mutator    │ │Mutator     │ │Mutator    │ │Mutator      │
└─────────────┘ └─────────┘ └───────────┘ └────────────┘ └───────────┘ └─────────────┘
```

## Components

### IMutator Interface

The `IMutator` interface defines the contract for individual mutation strategies:

```csharp
public interface IMutator
{
    /// <summary>
    /// Mutates the input data
    /// </summary>
    /// <param name="data">The input data to mutate</param>
    /// <param name="random">The random number generator</param>
    /// <returns>The mutated data</returns>
    byte[] Mutate(byte[] data, Random random);
    
    /// <summary>
    /// Gets the name of the mutator
    /// </summary>
    string Name { get; }
}
```

### IMutationEngine Interface

The `IMutationEngine` interface defines the contract for mutation engines that orchestrate multiple mutators:

```csharp
public interface IMutationEngine
{
    /// <summary>
    /// Mutates the input data
    /// </summary>
    /// <param name="data">The input data to mutate</param>
    /// <returns>The mutated data</returns>
    byte[] Mutate(byte[] data);
    
    /// <summary>
    /// Adds a custom mutator to the engine
    /// </summary>
    /// <param name="mutator">The mutator to add</param>
    void AddMutator(IMutator mutator);
    
    /// <summary>
    /// Gets the list of registered mutators
    /// </summary>
    /// <returns>The list of mutators</returns>
    IReadOnlyList<IMutator> GetMutators();
}
```

### MutationEngine

The `MutationEngine` is a standard implementation of the `IMutationEngine` interface that applies random mutations to input data:

- Maintains a list of registered mutators
- Randomly selects mutators to apply to input data
- Supports multiple mutations in a single operation
- Provides factory methods for creating pre-configured engines

### GuidedMutationEngine

The `GuidedMutationEngine` is an advanced implementation of the `IMutationEngine` interface that uses coverage feedback to guide mutation strategies:

- Tracks statistics for each mutator (coverage, crashes, execution time)
- Calculates utility scores for mutators based on their effectiveness
- Selects mutators based on their utility scores
- Adapts mutation strategies over time to maximize code coverage

### BitFlipMutator

The `BitFlipMutator` is a basic mutator that flips random bits in the input data:

- Selects random bit positions in the input
- Inverts the selected bits (0→1, 1→0)
- Useful for exploring small variations in the input

### ByteFlipMutator

The `ByteFlipMutator` is a basic mutator that flips random bytes in the input data:

- Selects random byte positions in the input
- Inverts all bits in the selected bytes
- Useful for exploring larger variations in the input

### EndiannessMutator

The `EndiannessMutator` is a specialized mutator that swaps the byte order of multi-byte values:

- Identifies potential multi-byte values (2, 4, or 8 bytes)
- Reverses the byte order to simulate endianness issues
- Helps find bugs related to endianness handling in serialization code

### StructureMutator

The `StructureMutator` is a complex mutator that modifies the structure of the input data:

- Inserts random bytes at random positions
- Deletes random bytes from the input
- Duplicates segments of the input
- Swaps segments of the input
- Useful for finding bugs related to data structure parsing

### ValueMutator

The `ValueMutator` is a specialized mutator that replaces parts of the input with boundary values:

- Maintains a list of interesting values (min/max integers, boundary cases)
- Replaces segments of the input with these values
- Helps find bugs related to value handling and boundary conditions

### NeoSerializationMutator

The `NeoSerializationMutator` is a specialized mutator that targets Neo.IO-specific serialization formats:

- Recognizes and mutates VarInt values
- Recognizes and mutates VarString values
- Recognizes and mutates array structures
- Inserts known interesting values at random positions
- Deliberately corrupts format to test error handling

## Usage Examples

### Basic Mutation

```csharp
// Create a mutation engine with default mutators
var random = new Random();
var engine = MutationEngine.CreateWithDefaultMutators(random);

// Apply mutations to input data
byte[] input = GetSeedInput();
byte[] mutated = engine.Mutate(input);
```

### Guided Mutation

```csharp
// Create a coverage tracker
var coverageTracker = new EnhancedCoverageTracker();

// Create a guided mutation engine
var random = new Random();
var engine = GuidedMutationEngine.Create(random, coverageTracker);

// Execute a test with the mutated input
byte[] input = GetSeedInput();
byte[] mutated = engine.Mutate(input);
bool success = ExecuteTest(mutated);

// Provide feedback to the engine
IMutator usedMutator = engine.GetMutators().First();
engine.ProvideFeedback(
    usedMutator,
    newCoverage: coverageTracker.HasNewCoverage(),
    crashed: !success,
    executionTime: 10
);
```

## Integration with Other Components

The mutation components integrate with other parts of the Neo.IO Fuzzer:

1. **FuzzerEngine**: Uses mutation engines to generate test inputs
2. **EnhancedCoverageTracker**: Provides feedback to guide mutation strategies
3. **CorpusManager**: Stores interesting inputs discovered through mutation
4. **Fuzz Targets**: Execute the mutated inputs and report results

## Performance Considerations

The mutation components include optimizations to ensure efficient operation:

- Avoid unnecessary copying of data when possible
- Use efficient bit manipulation techniques
- Balance between exploration (trying diverse strategies) and exploitation (focusing on effective strategies)
- Implement statistical methods to identify promising mutation strategies

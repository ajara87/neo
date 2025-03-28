# Stateful Fuzz Target

## Overview
The Stateful Fuzz Target is an advanced fuzzing component that maintains state across multiple operations, enabling the testing of stateful behavior in Neo.IO components. Unlike traditional fuzzing which tests individual operations in isolation, stateful fuzzing can uncover bugs that only manifest after a specific sequence of operations has been performed.

## Features

### State Maintenance
- Maintains an internal state that persists across multiple fuzzing operations
- Supports testing of components with complex state machines
- Allows for the detection of state-dependent bugs and race conditions

### Operation Sequencing
- Interprets input data as a sequence of operations to be performed
- Supports variable-length operation sequences
- Provides mechanisms for replaying specific sequences that trigger bugs

### State Validation
- Performs invariant checks on the internal state after each operation
- Detects inconsistencies in state that may indicate bugs
- Supports custom validation logic for domain-specific state properties

### Comprehensive Logging
- Records the sequence of operations that led to a failure
- Tracks state changes throughout the execution
- Provides detailed context for debugging state-related issues

## Usage Example

```csharp
// Define operations that can be performed on the stateful component
var operations = new Dictionary<byte, Func<byte[], object, object>>
{
    { 0x01, (data, state) => PerformAdd(data, state) },
    { 0x02, (data, state) => PerformRemove(data, state) },
    { 0x03, (data, state) => PerformUpdate(data, state) },
    { 0x04, (data, state) => PerformQuery(data, state) }
};

// Define state validation logic
Func<object, bool> validateState = (state) => 
{
    var typedState = (MyState)state;
    return typedState.IsValid();
};

// Create a stateful fuzz target
var statefulTarget = new StatefulFuzzTarget(
    "MyStatefulComponent",
    operations,
    () => new MyState(), // Initial state factory
    validateState
);

// Execute the stateful target with test input
byte[] testInput = GetTestInput();
bool success = statefulTarget.Execute(testInput);

// Check results
if (!success)
{
    Console.WriteLine("Found a state-related bug!");
    Console.WriteLine(statefulTarget.GetLastOperationSequence());
}
```

## Integration with Other Components

The Stateful Fuzz Target works in conjunction with:

1. **FuzzerEngine**: Orchestrates the fuzzing process and provides inputs
2. **GuidedMutationEngine**: Uses state-related failures to guide mutation strategies
3. **EnhancedCoverageTracker**: Tracks coverage across different state transitions
4. **FileReporter**: Logs detailed information about discovered state-related bugs

## Implementation Details

The target implements several key mechanisms:

1. **Operation Parsing**: Interprets input bytes as operation codes and parameters
2. **State Management**: Maintains and updates internal state based on operations
3. **Invariant Checking**: Validates state consistency after each operation
4. **Sequence Recording**: Tracks the sequence of operations for reproducibility

## Performance Considerations

The Stateful Fuzz Target includes optimizations to ensure efficient operation:

- Limits maximum sequence length to prevent excessive resource usage
- Implements lightweight state copying for efficient state management
- Uses checksums to detect duplicate states and avoid redundant testing
- Provides mechanisms for pruning the state space to focus on interesting paths

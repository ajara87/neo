// Copyright (C) 2015-2025 The Neo Project.
//
// StatefulFuzzTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// A fuzz target that maintains state between operations to test stateful behavior
    /// </summary>
    /// <typeparam name="TState">The type of state to maintain</typeparam>
    public class StatefulFuzzTarget<TState> : IStatefulFuzzTarget where TState : class, new()
    {
        // Delegate types for operations and state verification
        public delegate bool OperationHandler(TState state, byte[] data, out string coveragePoint);
        public delegate bool StateVerifier(TState state, out string failureReason);

        private readonly List<(string Name, OperationHandler Handler)> _operations = new();
        private readonly List<(string Name, StateVerifier Verifier)> _stateVerifiers = new();
        private readonly Func<TState> _stateFactory;

        // Track coverage points
        private readonly HashSet<string> _coveragePoints = new();

        // Track operation sequences that led to failures
        private readonly List<(List<string> Operations, string FailureReason)> _failures = new();

        // Current state
        private TState _currentState;

        // Current operation sequence
        private readonly List<string> _currentSequence = new();

        /// <summary>
        /// Gets the name of the fuzz target
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the maximum number of operations to perform in a single test
        /// </summary>
        public int MaxOperationsPerTest { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of failures to track
        /// </summary>
        public int MaxFailuresToTrack { get; set; } = 20;

        /// <summary>
        /// Gets or sets whether to reset state between tests
        /// </summary>
        public bool ResetStateBetweenTests { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the StatefulFuzzTarget class
        /// </summary>
        /// <param name="name">The name of the fuzz target</param>
        /// <param name="stateFactory">Optional factory function to create the initial state</param>
        public StatefulFuzzTarget(string name, Func<TState>? stateFactory = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _stateFactory = stateFactory ?? (() => new TState());
            _currentState = _stateFactory();
        }

        /// <summary>
        /// Adds an operation to the target
        /// </summary>
        /// <param name="name">The name of the operation</param>
        /// <param name="handler">The handler for the operation</param>
        public void AddOperation(string name, OperationHandler handler)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Operation name cannot be null or empty", nameof(name));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_operations.Any(o => o.Name == name))
                throw new ArgumentException($"Operation with name '{name}' already exists", nameof(name));

            _operations.Add((name, handler));
        }

        /// <summary>
        /// Adds a state verifier to the target
        /// </summary>
        /// <param name="name">The name of the verifier</param>
        /// <param name="verifier">The verifier function</param>
        public void AddStateVerifier(string name, StateVerifier verifier)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Verifier name cannot be null or empty", nameof(name));

            if (verifier == null)
                throw new ArgumentNullException(nameof(verifier));

            if (_stateVerifiers.Any(v => v.Name == name))
                throw new ArgumentException($"Verifier with name '{name}' already exists", nameof(name));

            _stateVerifiers.Add((name, verifier));
        }

        /// <summary>
        /// Resets the target's state to its initial state
        /// </summary>
        public void ResetState()
        {
            _currentState = _stateFactory();
            _currentSequence.Clear();
        }

        /// <summary>
        /// Gets the current sequence of operations that have been performed
        /// </summary>
        /// <returns>The current operation sequence</returns>
        public IEnumerable<string> GetCurrentSequence()
        {
            return _currentSequence.ToList();
        }

        /// <summary>
        /// Gets the failures that have been detected during fuzzing
        /// </summary>
        /// <returns>The detected failures</returns>
        public IEnumerable<(IEnumerable<string> Operations, string FailureReason)> GetFailures()
        {
            return _failures.Select(f => ((IEnumerable<string>)f.Operations.ToList(), f.FailureReason));
        }

        /// <summary>
        /// Executes the fuzz test by performing a sequence of operations on the state
        /// </summary>
        /// <param name="data">The input data for the test</param>
        /// <returns>True if all operations and verifiers succeeded, false otherwise</returns>
        public bool Execute(byte[] data)
        {
            if (data == null || data.Length == 0 || _operations.Count == 0)
                return true;

            // Reset state if needed
            if (ResetStateBetweenTests)
            {
                ResetState();
            }

            // Determine the number of operations to perform
            int numOperations = Math.Min(MaxOperationsPerTest, data.Length / 4);
            if (numOperations == 0)
                numOperations = 1;

            // Split the input data into chunks for each operation
            int chunkSize = data.Length / numOperations;

            // Perform the operations
            for (int i = 0; i < numOperations && i * chunkSize < data.Length; i++)
            {
                // Get the chunk of data for this operation
                int startIndex = i * chunkSize;
                int endIndex = Math.Min((i + 1) * chunkSize, data.Length);
                int length = endIndex - startIndex;

                byte[] chunk = new byte[length];
                Array.Copy(data, startIndex, chunk, 0, length);

                // Select an operation based on the first byte of the chunk
                int operationIndex = chunk.Length > 0 ? chunk[0] % _operations.Count : 0;
                var (operationName, handler) = _operations[operationIndex];

                // Record the operation in the sequence
                _currentSequence.Add(operationName);

                // Execute the operation
                string coveragePoint;
                bool success = handler(_currentState, chunk, out coveragePoint);

                // Record coverage point if provided
                if (!string.IsNullOrEmpty(coveragePoint))
                {
                    _coveragePoints.Add($"{operationName}:{coveragePoint}");
                }

                // If the operation failed, record the failure and return false
                if (!success)
                {
                    RecordFailure($"Operation '{operationName}' failed");
                    return false;
                }

                // Verify the state after each operation
                foreach (var (verifierName, verifier) in _stateVerifiers)
                {
                    string failureReason;
                    bool valid = verifier(_currentState, out failureReason);

                    if (!valid)
                    {
                        RecordFailure($"Verifier '{verifierName}' failed: {failureReason}");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Records a failure with the current operation sequence
        /// </summary>
        /// <param name="reason">The reason for the failure</param>
        private void RecordFailure(string reason)
        {
            // Limit the number of failures we track
            if (_failures.Count >= MaxFailuresToTrack)
                return;

            // Record the failure
            _failures.Add((_currentSequence.ToList(), reason));
        }

        /// <summary>
        /// Gets the coverage points recorded during fuzzing
        /// </summary>
        /// <returns>The coverage points</returns>
        public object GetCoverage()
        {
            return new HashSet<string>(_coveragePoints);
        }

        /// <summary>
        /// Gets statistics about the fuzzing process
        /// </summary>
        /// <returns>A dictionary containing statistics</returns>
        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["OperationsCount"] = _operations.Count,
                ["VerifiersCount"] = _stateVerifiers.Count,
                ["CoveragePointsCount"] = _coveragePoints.Count,
                ["FailuresCount"] = _failures.Count,
                ["UniqueOperationsExecuted"] = _currentSequence.Distinct().Count()
            };
        }
    }
}

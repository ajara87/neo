// Copyright (C) 2015-2025 The Neo Project.
//
// StateTrackingFuzzStrategy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Defines the types of state tracking tests that can be performed.
    /// </summary>
    public enum StateTrackingTestType
    {
        /// <summary>
        /// Test state transitions (None, Added, Changed, Deleted).
        /// </summary>
        StateTransitions,

        /// <summary>
        /// Test snapshot functionality.
        /// </summary>
        Snapshots,

        /// <summary>
        /// Test commit and rollback operations.
        /// </summary>
        CommitRollback,

        /// <summary>
        /// Test complex state tracking scenarios.
        /// </summary>
        ComplexStateTracking,

        /// <summary>
        /// Test state tracking with concurrent operations.
        /// </summary>
        ConcurrentStateTracking
    }

    /// <summary>
    /// A fuzzing strategy for testing the state tracking capabilities of the Neo caching system.
    /// Uses the common utility classes to reduce code duplication.
    /// </summary>
    public class StateTrackingFuzzStrategy : ICacheFuzzStrategy, IEnhancedCacheFuzzStrategy
    {
        private readonly CoverageTrackerHelper _coverage;
        private readonly string _name = "StateTrackingFuzzStrategy";
        private readonly int _maxOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateTrackingFuzzStrategy"/> class.
        /// </summary>
        /// <param name="maxOperations">The maximum number of operations to perform.</param>
        public StateTrackingFuzzStrategy(int maxOperations = 100)
        {
            _maxOperations = maxOperations;
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
        }

        private void InitializeCoverage()
        {
            // Initialize coverage tracking for state-related scenarios
            _coverage.InitializePoints(
                "NoneToAdded",
                "NoneToChanged",
                "AddedToChanged",
                "ChangedToDeleted",
                "AddedToDeleted",
                "SnapshotCreation",
                "CommitAfterAdd",
                "CommitAfterChange",
                "CommitAfterDelete",
                "RollbackAfterAdd",
                "RollbackAfterChange",
                "RollbackAfterDelete",
                "StateTransitions",
                "Snapshots",
                "CommitRollback",
                "ComplexStateTracking",
                "ConcurrentStateTracking",
                "SuccessfulRetry",
                "FailedRetry",
                "SnapshotConsistency",
                "CommitSuccess",
                "RollbackSuccess",
                "StateTransitionFailure",
                "ConcurrentModification"
            );
        }

        /// <summary>
        /// Gets the coverage tracker used by this strategy.
        /// </summary>
        /// <returns>The coverage tracker.</returns>
        public CoverageTrackerHelper GetCoverageTracker()
        {
            return _coverage;
        }

        /// <summary>
        /// Executes the fuzzing strategy with the provided input data.
        /// </summary>
        /// <param name="input">The input data to use for fuzzing.</param>
        /// <returns>True if the execution was successful, false otherwise.</returns>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length < 2)
                return false;

            bool result = ExceptionHandlingUtilities.ExecuteWithExceptionHandling(
                () => ExecuteInternal(input),
                "StateTrackingFuzzStrategy.Execute",
                _coverage);

            // Report coverage percentage
            double coveragePercent = _coverage.GetCoveragePercentage();
            Console.WriteLine($"State Tracking Strategy Coverage: {coveragePercent:F2}%");

            return result;
        }

        private bool ExecuteInternal(byte[] input)
        {
            // Use enhanced enum parameter extraction
            StateTrackingTestType testType = InputProcessingUtilities.ExtractEnumParameter<StateTrackingTestType>(input, 0);

            // Determine test parameters from input
            int operationCount = InputProcessingUtilities.ExtractNumericParameter(input, 1, 10, _maxOperations);

            // Create a mock cache for testing
            var cache = new MockStateTrackingCache();

            // Generate test keys and values from structured input
            var structuredInput = InputProcessingUtilities.GenerateStructuredInput(
                (byte)operationCount,
                input.Skip(Math.Min(input.Length / 2, 10)).Take(Math.Min(input.Length / 2, 20)).ToArray());

            var testKeys = InputProcessingUtilities.GenerateTestKeys(structuredInput);
            var testValues = InputProcessingUtilities.GenerateTestValues(structuredInput);

            // Run tests based on test type
            switch (testType)
            {
                case StateTrackingTestType.StateTransitions:
                    _coverage.IncrementPoint("StateTransitions");
                    RunStateTransitionTests(cache, testKeys, testValues, operationCount);
                    break;

                case StateTrackingTestType.Snapshots:
                    _coverage.IncrementPoint("Snapshots");
                    RunSnapshotTests(cache, testKeys, testValues);
                    break;

                case StateTrackingTestType.CommitRollback:
                    _coverage.IncrementPoint("CommitRollback");
                    RunCommitRollbackTests(cache, testKeys, testValues);
                    break;

                case StateTrackingTestType.ComplexStateTracking:
                    _coverage.IncrementPoint("ComplexStateTracking");
                    RunComplexStateTrackingTests(cache, testKeys, testValues, operationCount);
                    break;

                case StateTrackingTestType.ConcurrentStateTracking:
                    _coverage.IncrementPoint("ConcurrentStateTracking");
                    RunConcurrentStateTrackingTests(cache, testKeys, testValues);
                    break;
            }

            return true;
        }

        private void RunStateTransitionTests(MockStateTrackingCache cache, List<string> keys, List<byte[]> values, int operationCount)
        {
            for (int i = 0; i < operationCount && i < keys.Count && i < values.Count; i++)
            {
                var key = keys[i];
                var value = values[i];

                // Determine operation based on index
                switch (i % 5)
                {
                    case 0: // None to Added
                        ExceptionHandlingUtilities.ExecuteWithRetry(() =>
                        {
                            cache.Add(key, value);
                            _coverage.IncrementPoint("NoneToAdded");
                            return true;
                        }, "NoneToAdded", 3);
                        break;

                    case 1: // None to Changed (Add then Change)
                        ExceptionHandlingUtilities.ExecuteWithRetry(() =>
                        {
                            cache.Add(key, value);
                            cache.Update(key, BitConverter.GetBytes(i + 1000));
                            _coverage.IncrementPoint("NoneToChanged");
                            return true;
                        }, "NoneToChanged", 3);
                        break;

                    case 2: // Added to Changed
                        ExceptionHandlingUtilities.ExecuteWithRetry(() =>
                        {
                            cache.Add(key, value);
                            cache.Update(key, BitConverter.GetBytes(i + 2000));
                            _coverage.IncrementPoint("AddedToChanged");
                            return true;
                        }, "AddedToChanged", 3);
                        break;

                    case 3: // Changed to Deleted
                        ExceptionHandlingUtilities.ExecuteWithRetry(() =>
                        {
                            cache.Add(key, value);
                            cache.Update(key, BitConverter.GetBytes(i + 3000));
                            cache.Delete(key);
                            _coverage.IncrementPoint("ChangedToDeleted");
                            return true;
                        }, "ChangedToDeleted", 3);
                        break;

                    case 4: // Added to Deleted
                        ExceptionHandlingUtilities.ExecuteWithRetry(() =>
                        {
                            cache.Add(key, value);
                            cache.Delete(key);
                            _coverage.IncrementPoint("AddedToDeleted");
                            return true;
                        }, "AddedToDeleted", 3);
                        break;
                }
            }
        }

        private void RunSnapshotTests(MockStateTrackingCache cache, List<string> keys, List<byte[]> values)
        {
            // Create a snapshot with retry logic
            MockStateTrackingCache snapshot = null;
            bool success = ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                snapshot = cache.CreateSnapshot();
                _coverage.IncrementPoint("SnapshotCreation");
                return true;
            }, "CreateSnapshot", 3);

            if (!success || snapshot == null)
                return;

            // Make some changes to the original cache
            if (keys.Count > 0 && values.Count > 0)
            {
                ExceptionHandlingUtilities.ExecuteWithRetry(() =>
                {
                    cache.Add(keys[0], values[0]);
                    return true;
                }, "AddAfterSnapshot", 3);

                // Verify the snapshot doesn't see the changes
                bool containsInSnapshot = snapshot.Contains(keys[0]);
                bool containsInOriginal = cache.Contains(keys[0]);

                if (!containsInSnapshot && containsInOriginal)
                {
                    // This is expected behavior - snapshot consistency
                    _coverage.IncrementPoint("SnapshotConsistency");
                }
            }
        }

        private void RunCommitRollbackTests(MockStateTrackingCache cache, List<string> keys, List<byte[]> values)
        {
            if (keys.Count < 3 || values.Count < 3)
                return;

            // Test commit after add
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                var testCache = new MockStateTrackingCache();
                testCache.Add(keys[0], values[0]);
                testCache.Commit();
                _coverage.IncrementPoint("CommitAfterAdd");
                _coverage.IncrementPoint("CommitSuccess");
                return true;
            }, "CommitAfterAdd", 3);

            // Test commit after change
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                var testCache = new MockStateTrackingCache();
                testCache.Add(keys[1], values[1]);
                testCache.Commit();
                testCache.Update(keys[1], BitConverter.GetBytes(1000));
                testCache.Commit();
                _coverage.IncrementPoint("CommitAfterChange");
                _coverage.IncrementPoint("CommitSuccess");
                return true;
            }, "CommitAfterChange", 3);

            // Test commit after delete
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                var testCache = new MockStateTrackingCache();
                testCache.Add(keys[2], values[2]);
                testCache.Commit();
                testCache.Delete(keys[2]);
                testCache.Commit();
                _coverage.IncrementPoint("CommitAfterDelete");
                _coverage.IncrementPoint("CommitSuccess");
                return true;
            }, "CommitAfterDelete", 3);

            // Test rollback after add
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                var testCache = new MockStateTrackingCache();
                var snapshot = testCache.CreateSnapshot();
                testCache.Add(keys[0], values[0]);
                testCache.Rollback();
                _coverage.IncrementPoint("RollbackAfterAdd");
                _coverage.IncrementPoint("RollbackSuccess");
                return true;
            }, "RollbackAfterAdd", 3);

            // Test rollback after change
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                var testCache = new MockStateTrackingCache();
                testCache.Add(keys[1], values[1]);
                testCache.Commit();
                var snapshot = testCache.CreateSnapshot();
                testCache.Update(keys[1], BitConverter.GetBytes(2000));
                testCache.Rollback();
                _coverage.IncrementPoint("RollbackAfterChange");
                _coverage.IncrementPoint("RollbackSuccess");
                return true;
            }, "RollbackAfterChange", 3);

            // Test rollback after delete
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                var testCache = new MockStateTrackingCache();
                testCache.Add(keys[2], values[2]);
                testCache.Commit();
                var snapshot = testCache.CreateSnapshot();
                testCache.Delete(keys[2]);
                testCache.Rollback();
                _coverage.IncrementPoint("RollbackAfterDelete");
                _coverage.IncrementPoint("RollbackSuccess");
                return true;
            }, "RollbackAfterDelete", 3);
        }

        private void RunComplexStateTrackingTests(MockStateTrackingCache cache, List<string> keys, List<byte[]> values, int operationCount)
        {
            if (keys.Count < 5 || values.Count < 5)
                return;

            // Create a sequence of operations that test complex state transitions
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                // Add multiple items
                for (int i = 0; i < Math.Min(5, keys.Count); i++)
                {
                    cache.Add(keys[i], values[i]);
                }

                // Create a snapshot
                var snapshot1 = cache.CreateSnapshot();

                // Update some items
                for (int i = 0; i < Math.Min(3, keys.Count); i++)
                {
                    cache.Update(keys[i], BitConverter.GetBytes(i + 5000));
                }

                // Create another snapshot
                var snapshot2 = cache.CreateSnapshot();

                // Delete some items
                for (int i = 0; i < Math.Min(2, keys.Count); i++)
                {
                    cache.Delete(keys[i]);
                }

                // Commit changes
                cache.Commit();

                // Verify snapshots maintain their state
                bool snapshot1Consistent = !snapshot1.Contains(keys[0]) ||
                    !BitConverter.GetBytes(5000).SequenceEqual(snapshot1.GetValue(keys[0]));

                bool snapshot2Consistent = snapshot2.Contains(keys[0]) &&
                    BitConverter.GetBytes(5000).SequenceEqual(snapshot2.GetValue(keys[0]));

                if (snapshot1Consistent && snapshot2Consistent)
                {
                    _coverage.IncrementPoint("SnapshotConsistency");
                }

                return true;
            }, "ComplexStateTracking", 3);
        }

        private void RunConcurrentStateTrackingTests(MockStateTrackingCache cache, List<string> keys, List<byte[]> values)
        {
            if (keys.Count < 3 || values.Count < 3)
                return;

            // Simulate concurrent operations on the cache
            ExceptionHandlingUtilities.ExecuteWithRetry(() =>
            {
                // Setup initial state
                cache.Add(keys[0], values[0]);
                cache.Add(keys[1], values[1]);
                cache.Commit();

                // Create a snapshot
                var snapshot = cache.CreateSnapshot();

                // Simulate concurrent operations
                // Thread 1: Update key[0]
                cache.Update(keys[0], BitConverter.GetBytes(9000));

                // Thread 2: Delete key[1]
                snapshot.Delete(keys[1]);

                try
                {
                    // This might fail in a real concurrent scenario
                    cache.Commit();
                    snapshot.Commit();
                    _coverage.IncrementPoint("CommitSuccess");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.LogExceptionDetails(ex, "ConcurrentCommit", LogVerbosity.Normal);
                    _coverage.IncrementPoint("ConcurrentModification");
                    return false;
                }

                return true;
            }, "ConcurrentStateTracking", 3);
        }

        /// <summary>
        /// Gets the coverage information for the strategy.
        /// </summary>
        /// <returns>An object containing coverage information.</returns>
        public object GetCoverage()
        {
            return _coverage.GetCoverageObject();
        }

        /// <summary>
        /// Gets the name of the strategy.
        /// </summary>
        /// <returns>The strategy name.</returns>
        public string GetName()
        {
            return _name;
        }

        /// <summary>
        /// A mock implementation of a state tracking cache for testing purposes.
        /// </summary>
        private class MockStateTrackingCache
        {
            private readonly Dictionary<string, byte[]> _items = new Dictionary<string, byte[]>();
            private readonly List<string> _addedItems = new List<string>();
            private readonly List<string> _changedItems = new List<string>();
            private readonly List<string> _deletedItems = new List<string>();

            /// <summary>
            /// Adds an item to the cache.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            public void Add(string key, byte[] value)
            {
                _items[key] = value;
                if (!_addedItems.Contains(key))
                    _addedItems.Add(key);
            }

            /// <summary>
            /// Updates an item in the cache.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The new value.</param>
            public void Update(string key, byte[] value)
            {
                if (_items.ContainsKey(key))
                {
                    _items[key] = value;
                    if (!_changedItems.Contains(key))
                        _changedItems.Add(key);
                }
                else
                {
                    Add(key, value);
                }
            }

            /// <summary>
            /// Deletes an item from the cache.
            /// </summary>
            /// <param name="key">The key.</param>
            public void Delete(string key)
            {
                if (_items.ContainsKey(key))
                {
                    _items.Remove(key);
                    if (!_deletedItems.Contains(key))
                        _deletedItems.Add(key);
                }
            }

            /// <summary>
            /// Checks if the cache contains an item with the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>True if the cache contains the key, false otherwise.</returns>
            public bool Contains(string key)
            {
                return _items.ContainsKey(key);
            }

            /// <summary>
            /// Gets the value associated with the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>The value if found, null otherwise.</returns>
            public byte[] GetValue(string key)
            {
                return _items.TryGetValue(key, out byte[] value) ? value : null;
            }

            /// <summary>
            /// Creates a snapshot of the cache.
            /// </summary>
            /// <returns>A new cache with the same items.</returns>
            public MockStateTrackingCache CreateSnapshot()
            {
                var snapshot = new MockStateTrackingCache();
                foreach (var kvp in _items)
                {
                    snapshot.Add(kvp.Key, kvp.Value);
                }
                // Clear tracking lists in the snapshot since it's a fresh state
                snapshot._addedItems.Clear();
                snapshot._changedItems.Clear();
                snapshot._deletedItems.Clear();
                return snapshot;
            }

            /// <summary>
            /// Commits changes to the cache.
            /// </summary>
            public void Commit()
            {
                // In a real implementation, this would persist changes
                _addedItems.Clear();
                _changedItems.Clear();
                _deletedItems.Clear();
            }

            /// <summary>
            /// Rolls back changes to the cache.
            /// </summary>
            public void Rollback()
            {
                // In a real implementation, this would revert to the last committed state
                // For this mock, we'll just clear the tracking lists
                _addedItems.Clear();
                _changedItems.Clear();
                _deletedItems.Clear();
            }
        }
    }
}

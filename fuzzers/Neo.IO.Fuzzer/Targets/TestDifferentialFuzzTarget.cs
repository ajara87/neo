// Copyright (C) 2015-2025 The Neo Project.
//
// TestDifferentialFuzzTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.TestImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// A custom implementation of the differential fuzzer target specifically designed for Neo.IO test implementations.
    /// This target supports both ISerializable and ISerializableSpan implementations.
    /// </summary>
    public class TestDifferentialFuzzTarget : IFuzzTarget
    {
        private readonly List<(string Name, ISerializableSpan Implementation)> _serializableSpanImplementations = new();
        private readonly List<(string Name, ISerializable Implementation)> _serializableImplementations = new();
        private readonly Dictionary<string, HashSet<string>> _coveragePoints = new();

        /// <summary>
        /// Gets the name of the target.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDifferentialFuzzTarget"/> class.
        /// </summary>
        /// <param name="name">The name of the target.</param>
        public TestDifferentialFuzzTarget(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Adds an ISerializableSpan implementation to the target.
        /// </summary>
        /// <param name="name">The name of the implementation.</param>
        /// <param name="implementation">The implementation to add.</param>
        public void AddSerializableSpanImplementation(string name, ISerializableSpan implementation)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Implementation name cannot be null or empty", nameof(name));

            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            if (_serializableSpanImplementations.Any(i => i.Name == name) ||
                _serializableImplementations.Any(i => i.Name == name))
                throw new ArgumentException($"Implementation with name '{name}' already exists", nameof(name));

            _serializableSpanImplementations.Add((name, implementation));
            _coveragePoints[name] = new HashSet<string>();
        }

        /// <summary>
        /// Adds an ISerializable implementation to the target.
        /// </summary>
        /// <param name="name">The name of the implementation.</param>
        /// <param name="implementation">The implementation to add.</param>
        public void AddSerializableImplementation(string name, ISerializable implementation)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Implementation name cannot be null or empty", nameof(name));

            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            if (_serializableSpanImplementations.Any(i => i.Name == name) ||
                _serializableImplementations.Any(i => i.Name == name))
                throw new ArgumentException($"Implementation with name '{name}' already exists", nameof(name));

            _serializableImplementations.Add((name, implementation));
            _coveragePoints[name] = new HashSet<string>();
        }

        /// <summary>
        /// Records a coverage point for an implementation.
        /// </summary>
        /// <param name="implementationName">The name of the implementation.</param>
        /// <param name="point">The coverage point to record.</param>
        private void RecordCoveragePoint(string implementationName, string point)
        {
            if (_coveragePoints.TryGetValue(implementationName, out var points))
            {
                points.Add(point);
            }
        }

        /// <summary>
        /// Executes the fuzzer target with the provided data.
        /// </summary>
        /// <param name="data">The input data to test with.</param>
        /// <returns>True if the test was successful, false otherwise.</returns>
        public bool Execute(byte[] data)
        {
            if (data == null || data.Length == 0)
                return true;

            bool hasSerializableSpan = _serializableSpanImplementations.Count > 0;
            bool hasSerializable = _serializableImplementations.Count > 0;

            if (!hasSerializableSpan && !hasSerializable)
                throw new InvalidOperationException("At least one implementation is required for differential fuzzing");

            bool success = true;

            // Test ISerializableSpan implementations
            if (hasSerializableSpan && _serializableSpanImplementations.Count >= 2)
            {
                success &= ExecuteSerializableSpanTest(data);
            }

            // Test ISerializable implementations
            if (hasSerializable && _serializableImplementations.Count >= 2)
            {
                success &= ExecuteSerializableTest(data);
            }

            return success;
        }

        /// <summary>
        /// Executes the test for ISerializableSpan implementations.
        /// </summary>
        /// <param name="data">The input data to test with.</param>
        /// <returns>True if the test was successful, false otherwise.</returns>
        private bool ExecuteSerializableSpanTest(byte[] data)
        {
            var results = new Dictionary<string, (int Size, string Result, Exception? Error)>();

            // Execute each implementation
            foreach (var (name, implementation) in _serializableSpanImplementations)
            {
                try
                {
                    // Create a clone of the implementation to avoid state interference
                    var clone = (ISerializableSpan)Activator.CreateInstance(implementation.GetType())!;

                    // Initialize the implementation with the data
                    if (clone is TestSerializableSpan testSpan)
                    {
                        testSpan.Initialize(data);
                    }
                    else if (clone is TestNestedSerializableSpan nestedSpan)
                    {
                        nestedSpan.Initialize(data);
                    }
                    else
                    {
                        // For other implementations, try to use reflection to call Initialize
                        var initializeMethod = clone.GetType().GetMethod("Initialize");
                        if (initializeMethod != null)
                        {
                            initializeMethod.Invoke(clone, new object[] { data });
                        }
                        else
                        {
                            // If no Initialize method is found, create a span and try to serialize
                            Span<byte> buffer = new byte[clone.Size];
                            if (data.Length >= clone.Size)
                            {
                                data.AsSpan(0, clone.Size).CopyTo(buffer);
                            }
                            clone.Serialize(buffer);
                        }
                    }

                    // Record the result
                    results[name] = (clone.Size, clone.ToString() ?? "null", null);

                    // Record coverage point
                    RecordCoveragePoint(name, $"Success:Size={clone.Size}");
                }
                catch (Exception ex)
                {
                    // Record the error
                    results[name] = (0, "Exception", ex);

                    // Record coverage point
                    RecordCoveragePoint(name, $"Exception:{ex.GetType().Name}");
                }
            }

            // Check if all implementations produced the same result
            bool consistent = true;
            var firstResult = results.First();

            foreach (var result in results.Skip(1))
            {
                if (result.Value.Error != null && firstResult.Value.Error != null)
                {
                    // Both threw exceptions, check if they're the same type
                    if (result.Value.Error.GetType() != firstResult.Value.Error.GetType())
                    {
                        RecordCoveragePoint(firstResult.Key, $"DifferentException:{firstResult.Value.Error.GetType().Name}");
                        RecordCoveragePoint(result.Key, $"DifferentException:{result.Value.Error.GetType().Name}");
                        consistent = false;
                    }
                }
                else if (result.Value.Error != null || firstResult.Value.Error != null)
                {
                    // One threw an exception, the other didn't
                    if (firstResult.Value.Error != null)
                    {
                        RecordCoveragePoint(firstResult.Key, $"ExceptionOnly:{firstResult.Value.Error.GetType().Name}");
                        RecordCoveragePoint(result.Key, "NoException");
                    }
                    else
                    {
                        RecordCoveragePoint(firstResult.Key, "NoException");
                        RecordCoveragePoint(result.Key, $"ExceptionOnly:{result.Value.Error.GetType().Name}");
                    }
                    consistent = false;
                }
                else
                {
                    // Neither threw an exception, compare the results
                    if (result.Value.Size != firstResult.Value.Size)
                    {
                        RecordCoveragePoint(firstResult.Key, $"DifferentSize:{firstResult.Value.Size}");
                        RecordCoveragePoint(result.Key, $"DifferentSize:{result.Value.Size}");
                        consistent = false;
                    }

                    if (result.Value.Result != firstResult.Value.Result)
                    {
                        RecordCoveragePoint(firstResult.Key, "DifferentResult");
                        RecordCoveragePoint(result.Key, "DifferentResult");
                        consistent = false;
                    }
                }
            }

            return consistent;
        }

        /// <summary>
        /// Executes the test for ISerializable implementations.
        /// </summary>
        /// <param name="data">The input data to test with.</param>
        /// <returns>True if the test was successful, false otherwise.</returns>
        private bool ExecuteSerializableTest(byte[] data)
        {
            var results = new Dictionary<string, (int Size, string Result, Exception? Error)>();

            // Execute each implementation
            foreach (var (name, implementation) in _serializableImplementations)
            {
                try
                {
                    // Create a memory reader from the input data
                    var reader = new MemoryReader(data);

                    // Create a clone of the implementation to avoid state interference
                    var clone = (ISerializable)Activator.CreateInstance(implementation.GetType())!;

                    // Deserialize the data
                    clone.Deserialize(ref reader);

                    // Record the result
                    results[name] = (clone.Size, clone.ToString() ?? "null", null);

                    // Record coverage point
                    RecordCoveragePoint(name, $"Success:Size={clone.Size}");
                }
                catch (Exception ex)
                {
                    // Record the error
                    results[name] = (0, "Exception", ex);

                    // Record coverage point
                    RecordCoveragePoint(name, $"Exception:{ex.GetType().Name}");
                }
            }

            // Check if all implementations produced the same result
            bool consistent = true;
            var firstResult = results.First();

            foreach (var result in results.Skip(1))
            {
                if (result.Value.Error != null && firstResult.Value.Error != null)
                {
                    // Both threw exceptions, check if they're the same type
                    if (result.Value.Error.GetType() != firstResult.Value.Error.GetType())
                    {
                        RecordCoveragePoint(firstResult.Key, $"DifferentException:{firstResult.Value.Error.GetType().Name}");
                        RecordCoveragePoint(result.Key, $"DifferentException:{result.Value.Error.GetType().Name}");
                        consistent = false;
                    }
                }
                else if (result.Value.Error != null || firstResult.Value.Error != null)
                {
                    // One threw an exception, the other didn't
                    if (firstResult.Value.Error != null)
                    {
                        RecordCoveragePoint(firstResult.Key, $"ExceptionOnly:{firstResult.Value.Error.GetType().Name}");
                        RecordCoveragePoint(result.Key, "NoException");
                    }
                    else
                    {
                        RecordCoveragePoint(firstResult.Key, "NoException");
                        RecordCoveragePoint(result.Key, $"ExceptionOnly:{result.Value.Error.GetType().Name}");
                    }
                    consistent = false;
                }
                else
                {
                    // Neither threw an exception, compare the results
                    if (result.Value.Size != firstResult.Value.Size)
                    {
                        RecordCoveragePoint(firstResult.Key, $"DifferentSize:{firstResult.Value.Size}");
                        RecordCoveragePoint(result.Key, $"DifferentSize:{result.Value.Size}");
                        consistent = false;
                    }

                    if (result.Value.Result != firstResult.Value.Result)
                    {
                        RecordCoveragePoint(firstResult.Key, "DifferentResult");
                        RecordCoveragePoint(result.Key, "DifferentResult");
                        consistent = false;
                    }
                }
            }

            return consistent;
        }

        /// <summary>
        /// Gets the total number of coverage points recorded for all implementations.
        /// </summary>
        /// <returns>The total number of coverage points.</returns>
        public int GetCoveragePointCount()
        {
            return _coveragePoints.Values.Sum(v => v.Count);
        }

        /// <summary>
        /// Gets the coverage points for a specific implementation.
        /// </summary>
        /// <param name="implementationName">The name of the implementation.</param>
        /// <returns>The coverage points for the implementation.</returns>
        public IEnumerable<string> GetCoveragePoints(string implementationName)
        {
            if (_coveragePoints.TryGetValue(implementationName, out var points))
            {
                return points;
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets all coverage points for all implementations.
        /// </summary>
        /// <returns>A dictionary of implementation names to coverage points.</returns>
        public Dictionary<string, HashSet<string>> GetAllCoveragePoints()
        {
            return _coveragePoints;
        }

        /// <summary>
        /// Gets the coverage information for this target.
        /// </summary>
        /// <returns>A dictionary containing coverage information.</returns>
        public object GetCoverage()
        {
            var coverage = new Dictionary<string, object>
            {
                ["TotalPoints"] = GetCoveragePointCount(),
                ["ImplementationCount"] = _serializableSpanImplementations.Count + _serializableImplementations.Count
            };

            // Add coverage points for each implementation
            foreach (var (name, _) in _coveragePoints)
            {
                coverage[name] = GetCoveragePoints(name).Count();
            }

            return coverage;
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// SerializableSpanTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.IO.Fuzzer.TestImplementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Target that tests ISerializableSpan implementations
    /// </summary>
    public class SerializableSpanTarget : IFuzzTarget
    {
        public string Name => "ISerializableSpan";

        // Sample ISerializableSpan implementations for testing
        private readonly ISerializableSpan[] _serializables;
        private readonly HashSet<string> _coveragePoints = new();

        /// <summary>
        /// Initializes a new instance of the SerializableSpanTarget class with default test implementations
        /// </summary>
        public SerializableSpanTarget()
        {
            // Initialize with sample ISerializableSpan implementations
            _serializables = new ISerializableSpan[]
            {
                new TestSerializableSpan(),
                new TestNestedSerializableSpan()
            };
        }

        /// <summary>
        /// Initializes a new instance of the SerializableSpanTarget class with a specific ISerializableSpan type
        /// </summary>
        /// <param name="type">The type that implements ISerializableSpan</param>
        /// <exception cref="ArgumentException">Thrown when the type does not implement ISerializableSpan or cannot be instantiated</exception>
        public SerializableSpanTarget(Type? type)
        {
            if (type == null)
            {
                // Use default constructor behavior
                _serializables = new ISerializableSpan[]
                {
                    new TestSerializableSpan(),
                    new TestNestedSerializableSpan()
                };
                return;
            }

            if (!typeof(ISerializableSpan).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type {type.FullName} does not implement ISerializableSpan", nameof(type));
            }

            try
            {
                // Create an instance of the specified type
                ISerializableSpan instance = (ISerializableSpan)Activator.CreateInstance(type);
                _serializables = new ISerializableSpan[] { instance };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to create instance of type {type.FullName}", nameof(type), ex);
            }
        }

        /// <summary>
        /// Executes the target with the provided input
        /// </summary>
        /// <param name="input">The input data to use</param>
        /// <returns>True if the execution was successful, false otherwise</returns>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return true;
            }

            try
            {
                // Test serialization of each ISerializableSpan implementation
                foreach (var serializable in _serializables)
                {
                    TestSerialization(serializable, input);
                }

                // Test GetSpan method
                TestGetSpan(input);

                return true;
            }
            catch (Exception ex)
            {
                // Track the exception as a coverage point
                _coveragePoints.Add($"Exception:{ex.GetType().Name}");
                return false;
            }
        }

        /// <summary>
        /// Gets the coverage information
        /// </summary>
        /// <returns>The coverage information</returns>
        public object GetCoverage()
        {
            return _coveragePoints;
        }

        /// <summary>
        /// Tests serialization of an ISerializableSpan implementation
        /// </summary>
        private void TestSerialization(ISerializableSpan serializable, byte[] input)
        {
            try
            {
                // Track the serializable type
                string serializableType = serializable.GetType().Name;
                _coveragePoints.Add($"Serializable:{serializableType}");

                // Test serialization
                int size = serializable.Size;
                _coveragePoints.Add($"Size:{serializableType}:{size}");

                if (input.Length >= size)
                {
                    // For test implementations, we can cast to our test classes to initialize them with input
                    if (serializable is TestSerializableSpan testSpan)
                    {
                        testSpan.Initialize(input);
                        _coveragePoints.Add($"Initialize:{serializableType}:Success");
                    }
                    else if (serializable is TestNestedSerializableSpan nestedSpan)
                    {
                        nestedSpan.Initialize(input);
                        _coveragePoints.Add($"Initialize:{serializableType}:Success");
                    }

                    // Test serialization
                    Span<byte> buffer = new byte[size];
                    serializable.Serialize(buffer);
                    _coveragePoints.Add($"Serialize:{serializableType}:Success");

                    // Test GetSpan
                    ReadOnlySpan<byte> getSpan = serializable.GetSpan();
                    _coveragePoints.Add($"GetSpan:{serializableType}:Success");

                    // Compare GetSpan and Serialize results
                    bool equal = getSpan.SequenceEqual(buffer);
                    _coveragePoints.Add($"GetSpan:Equals:Serialize:{equal}");
                }
                else
                {
                    _coveragePoints.Add($"Serialize:{serializableType}:InputTooSmall");
                }
            }
            catch (Exception ex)
            {
                // Track the exception
                _coveragePoints.Add($"Exception:{serializable.GetType().Name}:{ex.GetType().Name}");
                throw;
            }
        }

        /// <summary>
        /// Tests the GetSpan method
        /// </summary>
        private void TestGetSpan(byte[] input)
        {
            try
            {
                // Create a test span
                ReadOnlySpan<byte> span = input;

                // Track the input size
                _coveragePoints.Add($"GetSpan:InputSize:{input.Length}");

                // Test different slice operations
                if (input.Length > 1)
                {
                    ReadOnlySpan<byte> slice = span.Slice(1);
                    _coveragePoints.Add($"GetSpan:Slice:Start");
                }

                if (input.Length > 2)
                {
                    ReadOnlySpan<byte> slice = span.Slice(1, input.Length - 2);
                    _coveragePoints.Add($"GetSpan:Slice:StartLength");
                }

                // Test equality
                bool equal = span.SequenceEqual(input);
                _coveragePoints.Add($"GetSpan:SequenceEqual:{equal}");
            }
            catch (Exception ex)
            {
                // Track the exception
                _coveragePoints.Add($"Exception:GetSpan:{ex.GetType().Name}");
                throw;
            }
        }
    }

    /// <summary>
    /// Test implementation of ISerializableSpan
    /// </summary>
    public class TestSerializableSpan : ISerializableSpan
    {
        private readonly byte[] _data = new byte[4];

        public int Size => 4;

        public void Initialize(byte[] data)
        {
            if (data.Length >= 4)
            {
                Array.Copy(data, _data, 4);
            }
        }

        public void Serialize(Span<byte> span)
        {
            if (span.Length < Size)
                throw new ArgumentException("Span is too small", nameof(span));

            _data.CopyTo(span);
        }

        public ReadOnlySpan<byte> GetSpan()
        {
            return _data;
        }
    }

    /// <summary>
    /// Test implementation of ISerializableSpan with nested structure
    /// </summary>
    public class TestNestedSerializableSpan : ISerializableSpan
    {
        private readonly TestSerializableSpan _inner = new TestSerializableSpan();
        private readonly byte[] _header = new byte[2];

        public int Size => _inner.Size + _header.Length;

        public void Initialize(byte[] data)
        {
            if (data.Length >= 2)
            {
                Array.Copy(data, _header, 2);
            }

            if (data.Length >= 6)
            {
                byte[] innerData = new byte[4];
                Array.Copy(data, 2, innerData, 0, 4);
                _inner.Initialize(innerData);
            }
        }

        public void Serialize(Span<byte> span)
        {
            if (span.Length < Size)
                throw new ArgumentException("Span is too small", nameof(span));

            _header.CopyTo(span);
            _inner.Serialize(span.Slice(2));
        }

        public ReadOnlySpan<byte> GetSpan()
        {
            byte[] result = new byte[Size];
            _header.CopyTo(result, 0);
            _inner.GetSpan().CopyTo(new Span<byte>(result, 2, _inner.Size));
            return result;
        }
    }
}

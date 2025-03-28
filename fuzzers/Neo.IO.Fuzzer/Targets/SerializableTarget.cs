// Copyright (C) 2015-2025 The Neo Project.
//
// SerializableTarget.cs file belongs to the neo project and is free
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
using System.IO;
using System.Text;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Target that tests ISerializable implementations
    /// </summary>
    public class SerializableTarget : IFuzzTarget
    {
        public string Name => "ISerializable";

        // Sample ISerializable implementations for testing
        private readonly ISerializable[] _serializables;
        private readonly HashSet<string> _coveragePoints = new();

        /// <summary>
        /// Initializes a new instance of the SerializableTarget class with default test implementations
        /// </summary>
        public SerializableTarget()
        {
            // Initialize with sample ISerializable implementations
            _serializables = new ISerializable[]
            {
                new TestSerializable(),
                new TestNestedSerializable(),
                new TestSerializableImplementation1(),
                new TestSerializableImplementation2()
            };
        }

        /// <summary>
        /// Initializes a new instance of the SerializableTarget class with a specific ISerializable type
        /// </summary>
        /// <param name="type">The type that implements ISerializable</param>
        /// <exception cref="ArgumentException">Thrown when the type does not implement ISerializable or cannot be instantiated</exception>
        public SerializableTarget(Type? type)
        {
            if (type == null)
            {
                // Use default constructor behavior
                _serializables = new ISerializable[]
                {
                    new TestSerializable(),
                    new TestNestedSerializable(),
                    new TestSerializableImplementation1(),
                    new TestSerializableImplementation2()
                };
                return;
            }

            if (!typeof(ISerializable).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type {type.FullName} does not implement ISerializable", nameof(type));
            }

            try
            {
                // Create an instance of the specified type
                ISerializable instance = (ISerializable)Activator.CreateInstance(type);
                _serializables = new ISerializable[] { instance };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to create instance of type {type.FullName}", nameof(type), ex);
            }
        }

        /// <summary>
        /// Executes the target with the provided input
        /// </summary>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length == 0)
                return false;

            try
            {
                // Test serialization of each ISerializable implementation
                foreach (var serializable in _serializables)
                {
                    TestSerialization(serializable, input);
                }

                // Test MemoryReader deserialization
                TestMemoryReaderDeserialization(input);

                return true;
            }
            catch (Exception ex)
            {
                // Record the exception type for coverage tracking
                _coveragePoints.Add($"Exception:{ex.GetType().Name}");
                return false; // Return false to signal failure
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
        /// Tests serialization of an ISerializable implementation
        /// </summary>
        private void TestSerialization(ISerializable serializable, byte[] input)
        {
            try
            {
                // Track the serializable type
                string serializableType = serializable.GetType().Name;
                _coveragePoints.Add($"Serializable:{serializableType}");

                // Test deserialization
                var reader = new MemoryReader(input);

                // Track the size
                int size = serializable.Size;
                _coveragePoints.Add($"Size:{serializableType}:{size}");

                // Deserialize the object
                serializable.Deserialize(ref reader);
                _coveragePoints.Add($"Deserialize:{serializableType}:Success");

                // Test serialization
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    serializable.Serialize(writer);
                    _coveragePoints.Add($"Serialize:{serializableType}:Success");

                    // Verify the serialized data
                    byte[] serializedData = ms.ToArray();
                    _coveragePoints.Add($"SerializedLength:{serializableType}:{serializedData.Length}");
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
        /// Tests deserialization using MemoryReader
        /// </summary>
        private void TestMemoryReaderDeserialization(byte[] input)
        {
            try
            {
                // Create a MemoryReader from the input
                var reader = new MemoryReader(input);
                _coveragePoints.Add("MemoryReader:Created");

                // Test deserialization with different offsets
                for (int i = 0; i < Math.Min(input.Length, 5); i++)
                {
                    if (reader.Position + i < input.Length)
                    {
                        // Create a new test object
                        var test = new TestSerializable();

                        try
                        {
                            // Deserialize at the current position
                            test.Deserialize(ref reader);
                            _coveragePoints.Add($"MemoryReaderDeserialize:Offset:{i}:Success");
                        }
                        catch (Exception ex)
                        {
                            _coveragePoints.Add($"MemoryReaderDeserialize:Offset:{i}:Exception:{ex.GetType().Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"MemoryReaderDeserialize:Exception:{ex.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Test implementation of ISerializable
    /// </summary>
    internal class TestSerializable : ISerializable
    {
        private byte _value;

        public int Size => sizeof(byte);

        public void Deserialize(ref MemoryReader reader)
        {
            _value = reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_value);
        }
    }

    /// <summary>
    /// Test implementation of ISerializable with nested structure
    /// </summary>
    internal class TestNestedSerializable : ISerializable
    {
        private byte _value1;
        private ushort _value2;
        private uint _value3;

        public int Size => sizeof(byte) + sizeof(ushort) + sizeof(uint);

        public void Deserialize(ref MemoryReader reader)
        {
            _value1 = reader.ReadByte();
            _value2 = reader.ReadUInt16();
            _value3 = reader.ReadUInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_value1);
            writer.Write(_value2);
            writer.Write(_value3);
        }
    }
}

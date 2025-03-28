// Copyright (C) 2015-2025 The Neo Project.
//
// TestSerializableImplementation2.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;

namespace Neo.IO.Fuzzer.TestImplementations
{
    /// <summary>
    /// Alternative implementation of ISerializable for testing with numeric data
    /// </summary>
    public class TestSerializableImplementation2 : ISerializable
    {
        private int _intValue;
        private double _doubleValue;

        /// <summary>
        /// Initializes a new instance of the TestSerializableImplementation2 class
        /// </summary>
        public TestSerializableImplementation2()
        {
            _intValue = 0;
            _doubleValue = 0.0;
        }

        /// <summary>
        /// Gets the size of the object
        /// </summary>
        public int Size => sizeof(int) + sizeof(double);

        /// <summary>
        /// Serializes the object to a writer
        /// </summary>
        /// <param name="writer">The writer to serialize to</param>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_intValue);
            writer.Write(_doubleValue);
        }

        /// <summary>
        /// Deserializes the object from a reader
        /// </summary>
        /// <param name="reader">The reader to deserialize from</param>
        public void Deserialize(ref MemoryReader reader)
        {
            if (reader.Position + sizeof(int) <= reader.ReadToEnd().Length + reader.Position)
            {
                _intValue = reader.ReadInt32();

                if (reader.Position + sizeof(double) <= reader.ReadToEnd().Length + reader.Position)
                {
                    // MemoryReader doesn't have a ReadDouble method, so we read a long and convert it
                    long bits = reader.ReadInt64();
                    _doubleValue = BitConverter.Int64BitsToDouble(bits);
                }
                else
                {
                    _doubleValue = 0.0;
                }
            }
            else
            {
                _intValue = 0;
                _doubleValue = 0.0;
            }
        }
    }
}

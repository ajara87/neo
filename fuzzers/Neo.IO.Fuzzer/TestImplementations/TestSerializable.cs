// Copyright (C) 2015-2025 The Neo Project.
//
// TestSerializable.cs file belongs to the neo project and is free
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
    /// Simple implementation of ISerializable for testing
    /// </summary>
    public class TestSerializable : ISerializable
    {
        private byte[] _data;

        /// <summary>
        /// Initializes a new instance of the TestSerializable class
        /// </summary>
        public TestSerializable()
        {
            _data = Array.Empty<byte>();
        }

        /// <summary>
        /// Gets the size of the object
        /// </summary>
        public int Size => _data.Length + sizeof(int);

        /// <summary>
        /// Serializes the object to a writer
        /// </summary>
        /// <param name="writer">The writer to serialize to</param>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_data.Length);
            if (_data.Length > 0)
            {
                writer.Write(_data);
            }
        }

        /// <summary>
        /// Deserializes the object from a reader
        /// </summary>
        /// <param name="reader">The reader to deserialize from</param>
        public void Deserialize(ref MemoryReader reader)
        {
            int length = reader.ReadInt32();
            if (length > 0)
            {
                _data = reader.ReadMemory(length).ToArray();
            }
            else
            {
                _data = Array.Empty<byte>();
            }
        }
    }
}

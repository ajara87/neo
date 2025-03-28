// Copyright (C) 2015-2025 The Neo Project.
//
// TestNestedSerializable.cs file belongs to the neo project and is free
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
    /// Nested implementation of ISerializable for testing
    /// </summary>
    public class TestNestedSerializable : ISerializable
    {
        private readonly TestSerializable _inner;
        private int _value;

        /// <summary>
        /// Initializes a new instance of the TestNestedSerializable class
        /// </summary>
        public TestNestedSerializable()
        {
            _inner = new TestSerializable();
            _value = 0;
        }

        /// <summary>
        /// Gets the size of the object
        /// </summary>
        public int Size => _inner.Size + sizeof(int);

        /// <summary>
        /// Serializes the object to a writer
        /// </summary>
        /// <param name="writer">The writer to serialize to</param>
        public void Serialize(BinaryWriter writer)
        {
            _inner.Serialize(writer);
            writer.Write(_value);
        }

        /// <summary>
        /// Deserializes the object from a reader
        /// </summary>
        /// <param name="reader">The reader to deserialize from</param>
        public void Deserialize(ref MemoryReader reader)
        {
            _inner.Deserialize(ref reader);
            if (reader.Position < reader.ReadToEnd().Length)
            {
                _value = reader.ReadInt32();
            }
        }
    }
}

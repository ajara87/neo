// Copyright (C) 2015-2025 The Neo Project.
//
// TestSerializableImplementation1.cs file belongs to the neo project and is free
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
using System.Text;

namespace Neo.IO.Fuzzer.TestImplementations
{
    /// <summary>
    /// Alternative implementation of ISerializable for testing with string data
    /// </summary>
    public class TestSerializableImplementation1 : ISerializable
    {
        private string? _stringData;

        /// <summary>
        /// Initializes a new instance of the TestSerializableImplementation1 class
        /// </summary>
        public TestSerializableImplementation1()
        {
            _stringData = string.Empty;
        }

        /// <summary>
        /// Gets the size of the object
        /// </summary>
        public int Size
        {
            get
            {
                if (_stringData == null) return sizeof(bool);
                return sizeof(bool) + sizeof(int) + Encoding.UTF8.GetByteCount(_stringData);
            }
        }

        /// <summary>
        /// Serializes the object to a writer
        /// </summary>
        /// <param name="writer">The writer to serialize to</param>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_stringData != null);
            if (_stringData != null)
            {
                writer.Write(_stringData);
            }
        }

        /// <summary>
        /// Deserializes the object from a reader
        /// </summary>
        /// <param name="reader">The reader to deserialize from</param>
        public void Deserialize(ref MemoryReader reader)
        {
            bool hasString = reader.ReadBoolean();
            if (hasString && reader.Position < reader.ReadToEnd().Length)
            {
                _stringData = reader.ReadVarString();
            }
            else
            {
                _stringData = string.Empty;
            }
        }
    }
}

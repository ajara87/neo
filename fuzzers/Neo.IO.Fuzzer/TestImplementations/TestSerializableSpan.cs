// Copyright (C) 2015-2025 The Neo Project.
//
// TestSerializableSpan.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;

namespace Neo.IO.Fuzzer.TestImplementations
{
    /// <summary>
    /// Simple implementation of ISerializableSpan for testing
    /// </summary>
    public class TestSerializableSpan : ISerializableSpan
    {
        private byte[] _data;

        /// <summary>
        /// Initializes a new instance of the TestSerializableSpan class
        /// </summary>
        public TestSerializableSpan()
        {
            _data = Array.Empty<byte>();
        }

        /// <summary>
        /// Gets the size of the object
        /// </summary>
        public int Size => _data.Length;

        /// <summary>
        /// Gets a ReadOnlySpan that represents the current value
        /// </summary>
        /// <returns>A ReadOnlySpan containing the serialized data</returns>
        public ReadOnlySpan<byte> GetSpan() => _data;

        /// <summary>
        /// Deserializes the object from a span
        /// </summary>
        /// <param name="span">The span to deserialize from</param>
        public void Deserialize(ReadOnlySpan<byte> span)
        {
            _data = span.ToArray();
        }
    }
}

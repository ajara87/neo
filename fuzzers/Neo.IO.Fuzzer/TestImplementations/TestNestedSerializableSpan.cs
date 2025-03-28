// Copyright (C) 2015-2025 The Neo Project.
//
// TestNestedSerializableSpan.cs file belongs to the neo project and is free
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
    /// Nested implementation of ISerializableSpan for testing
    /// </summary>
    public class TestNestedSerializableSpan : ISerializableSpan
    {
        private readonly TestSerializableSpan _inner;
        private int _value;
        private byte[] _buffer;

        /// <summary>
        /// Initializes a new instance of the TestNestedSerializableSpan class
        /// </summary>
        public TestNestedSerializableSpan()
        {
            _inner = new TestSerializableSpan();
            _value = 0;
            _buffer = Array.Empty<byte>();
        }

        /// <summary>
        /// Gets the size of the object
        /// </summary>
        public int Size => _inner.Size + sizeof(int);

        /// <summary>
        /// Gets a ReadOnlySpan that represents the current value
        /// </summary>
        /// <returns>A ReadOnlySpan containing the serialized data</returns>
        public ReadOnlySpan<byte> GetSpan()
        {
            if (_buffer.Length < Size)
            {
                _buffer = new byte[Size];
            }

            var span = new Span<byte>(_buffer);
            _inner.GetSpan().CopyTo(span);
            BitConverter.TryWriteBytes(span.Slice(_inner.Size), _value);

            return _buffer;
        }

        /// <summary>
        /// Deserializes the object from a span
        /// </summary>
        /// <param name="span">The span to deserialize from</param>
        public void Deserialize(ReadOnlySpan<byte> span)
        {
            if (span.Length >= sizeof(int))
            {
                _inner.Deserialize(span.Slice(0, span.Length - sizeof(int)));
                _value = BitConverter.ToInt32(span.Slice(span.Length - sizeof(int)));
            }
            else
            {
                _inner.Deserialize(span);
                _value = 0;
            }
        }
    }
}

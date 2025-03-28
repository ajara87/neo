// Copyright (C) 2015-2025 The Neo Project.
//
// StructuredBinaryGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Text;

namespace Neo.IO.Fuzzer.Generators
{
    /// <summary>
    /// Generates binary data that follows Neo.IO serialization formats
    /// </summary>
    public class StructuredBinaryGenerator : IBinaryGenerator
    {
        private readonly Random _random;

        // Constants for limiting generation
        private const int MAX_DEPTH = 5;
        private const int MAX_STRING_LENGTH = 100;
        private const int MAX_ARRAY_LENGTH = 10;

        // Probability weights for different data types
        private readonly double[] _typeWeights = { 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.1 };
        private readonly Type[] _types = {
            typeof(bool),    // Boolean
            typeof(int),     // Integer
            typeof(long),    // Long
            typeof(string),  // String
            typeof(byte[]),  // Byte array
            typeof(VarInt),  // VarInt
            typeof(object[]) // Array (for nesting)
        };

        /// <summary>
        /// Initializes a new instance of the StructuredBinaryGenerator class
        /// </summary>
        /// <param name="random">The random number generator to use</param>
        public StructuredBinaryGenerator(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Generates a random binary array
        /// </summary>
        /// <param name="random">The random number generator</param>
        /// <param name="maxSize">The maximum size of the generated data</param>
        /// <returns>The generated binary data</returns>
        public byte[] Generate(Random random, int maxSize)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            if (maxSize <= 0)
                return Array.Empty<byte>();

            // Limit the maximum size to prevent excessive memory usage
            maxSize = Math.Min(maxSize, 1024 * 10); // 10KB max

            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            try
            {
                // Generate a structured object
                GenerateStructured(writer, random, 0, maxSize);

                // Return the generated data
                return ms.ToArray();
            }
            catch (Exception)
            {
                // If an error occurs during generation, return a simple array
                byte[] fallback = new byte[Math.Min(100, maxSize)];
                random.NextBytes(fallback);
                return fallback;
            }
        }

        /// <summary>
        /// Generates a structured object with the specified maximum size
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="random">The random number generator</param>
        /// <param name="depth">The current nesting depth</param>
        /// <param name="maxSize">The maximum size of the generated data</param>
        private void GenerateStructured(BinaryWriter writer, Random random, int depth, int maxSize)
        {
            // Prevent excessive recursion
            if (depth > MAX_DEPTH)
            {
                GenerateSimpleValue(writer, random);
                return;
            }

            // Limit maxSize to prevent excessive memory usage
            maxSize = Math.Min(maxSize, 1024);

            // Select a type to generate
            Type type = SelectRandomType(random, depth);

            // Generate a value of the selected type
            if (type == typeof(bool))
            {
                writer.Write(random.Next(2) == 1);
            }
            else if (type == typeof(int))
            {
                writer.Write(random.Next());
            }
            else if (type == typeof(long))
            {
                writer.Write(random.NextInt64());
            }
            else if (type == typeof(string))
            {
                // Limit string length more aggressively as depth increases
                int maxLength = MAX_STRING_LENGTH / (depth + 1);
                int length = random.Next(Math.Max(1, maxLength));
                writer.Write(GenerateRandomString(random, length));
            }
            else if (type == typeof(byte[]))
            {
                // Limit array size more aggressively as depth increases
                int maxLength = Math.Min(maxSize / 2, 100 / (depth + 1));
                int length = random.Next(Math.Max(1, maxLength));
                byte[] data = new byte[length];
                random.NextBytes(data);
                writer.Write(data.Length);
                writer.Write(data);
            }
            else if (type == typeof(VarInt))
            {
                WriteVarInt(writer, random.Next(1000)); // Limit to smaller values
            }
            else if (type == typeof(object[]))
            {
                // Generate an array of objects with limited length
                int maxArrayLength = Math.Min(MAX_ARRAY_LENGTH / (depth + 1), 5);
                int length = random.Next(1, Math.Max(2, maxArrayLength));
                writer.Write(length);

                // Calculate max size for each element
                int elementMaxSize = maxSize / (length + 1);

                for (int i = 0; i < length; i++)
                {
                    GenerateStructured(writer, random, depth + 1, elementMaxSize);
                }
            }
        }

        /// <summary>
        /// Generates a simple value (non-nested)
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="random">The random number generator</param>
        private void GenerateSimpleValue(BinaryWriter writer, Random random)
        {
            // Select a random type (excluding arrays)
            int typeIndex = random.Next(6); // Exclude the last type (object[])
            Type type = _types[typeIndex];

            // Generate a value of the selected type
            if (type == typeof(bool))
            {
                writer.Write(random.Next(2) == 1);
            }
            else if (type == typeof(int))
            {
                writer.Write(random.Next());
            }
            else if (type == typeof(long))
            {
                writer.Write(random.NextInt64());
            }
            else if (type == typeof(string))
            {
                int length = random.Next(100); // Shorter strings for simple values
                writer.Write(GenerateRandomString(random, length));
            }
            else if (type == typeof(byte[]))
            {
                int length = random.Next(100); // Shorter arrays for simple values
                byte[] data = new byte[length];
                random.NextBytes(data);
                writer.Write(data.Length);
                writer.Write(data);
            }
            else if (type == typeof(VarInt))
            {
                WriteVarInt(writer, random.Next());
            }
        }

        /// <summary>
        /// Selects a random type based on weights
        /// </summary>
        /// <param name="random">The random number generator</param>
        /// <param name="depth">The current nesting depth</param>
        /// <returns>The selected type</returns>
        private Type SelectRandomType(Random random, int depth)
        {
            double value = random.NextDouble();
            double sum = 0;

            for (int i = 0; i < _typeWeights.Length; i++)
            {
                sum += _typeWeights[i];
                if (value < sum)
                {
                    return _types[i];
                }
            }

            // Default to the last type
            return _types[_types.Length - 1];
        }

        /// <summary>
        /// Generates a random string of the specified length
        /// </summary>
        /// <param name="random">The random number generator</param>
        /// <param name="length">The length of the string</param>
        /// <returns>The generated string</returns>
        private string GenerateRandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes a variable-length integer to the binary writer
        /// </summary>
        /// <param name="writer">The binary writer to write to</param>
        /// <param name="value">The value to write</param>
        private void WriteVarInt(BinaryWriter writer, int value)
        {
            if (value < 0xFD)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
                writer.Write((byte)0xFD);
                writer.Write((ushort)value);
            }
            else
            {
                writer.Write((byte)0xFE);
                writer.Write((uint)value);
            }
        }

        /// <summary>
        /// Represents a variable-length integer
        /// </summary>
        private class VarInt
        {
            public int Value { get; set; }
        }
    }
}

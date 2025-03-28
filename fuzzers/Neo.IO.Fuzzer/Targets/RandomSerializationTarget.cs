// Copyright (C) 2015-2025 The Neo Project.
//
// RandomSerializationTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Target that tests serialization and deserialization of random data without specific class types.
    /// This target directly uses BinaryReader and BinaryWriter to test raw serialization operations.
    /// </summary>
    public class RandomSerializationTarget : IFuzzTarget
    {
        public string Name => "RandomSerialization";

        private readonly HashSet<string> _coveragePoints = new();

        /// <summary>
        /// Executes the target with the provided input
        /// </summary>
        /// <param name="input">The input data to test</param>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length == 0)
                return false;

            try
            {
                // Test raw serialization/deserialization
                TestRawSerialization(input);

                // Test primitive type serialization/deserialization
                TestPrimitiveTypes(input);

                // Test array serialization/deserialization
                TestArraySerialization(input);

                // Test string serialization/deserialization
                TestStringSerialization(input);

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
        /// Tests raw serialization and deserialization by writing and reading the input directly
        /// </summary>
        /// <param name="input">The input data to test</param>
        private void TestRawSerialization(byte[] input)
        {
            _coveragePoints.Add("RawSerialization");

            using (MemoryStream ms = new MemoryStream())
            {
                // Write the input to the stream
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    writer.Write(input);
                }

                // Reset the stream position
                ms.Position = 0;

                // Read the data back
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    byte[] output = reader.ReadBytes(input.Length);

                    // Verify the data was read correctly
                    if (output.Length == input.Length)
                    {
                        _coveragePoints.Add("RawSerialization_Success");
                    }
                }
            }
        }

        /// <summary>
        /// Tests serialization and deserialization of primitive types
        /// </summary>
        /// <param name="input">The input data to test</param>
        private void TestPrimitiveTypes(byte[] input)
        {
            _coveragePoints.Add("PrimitiveTypes");

            if (input.Length < 8)
            {
                return;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                // Write primitive types based on the input
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    // Use the input bytes to create primitive values
                    bool boolValue = (input[0] & 0x01) == 1;
                    byte byteValue = input[1];
                    short shortValue = BitConverter.ToInt16(input, 2);
                    int intValue = BitConverter.ToInt32(input, 4);

                    // Write the values
                    writer.Write(boolValue);
                    writer.Write(byteValue);
                    writer.Write(shortValue);
                    writer.Write(intValue);

                    _coveragePoints.Add("PrimitiveTypes_Write");
                }

                // Reset the stream position
                ms.Position = 0;

                // Read the values back
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    try
                    {
                        bool boolValue = reader.ReadBoolean();
                        byte byteValue = reader.ReadByte();
                        short shortValue = reader.ReadInt16();
                        int intValue = reader.ReadInt32();

                        _coveragePoints.Add("PrimitiveTypes_Read");
                    }
                    catch (EndOfStreamException)
                    {
                        _coveragePoints.Add("PrimitiveTypes_EndOfStream");
                    }
                }
            }
        }

        /// <summary>
        /// Tests serialization and deserialization of arrays
        /// </summary>
        /// <param name="input">The input data to test</param>
        private void TestArraySerialization(byte[] input)
        {
            _coveragePoints.Add("ArraySerialization");

            if (input.Length < 4)
            {
                return;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                // Write array data
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    // Determine array length from the first 4 bytes, but limit it to avoid excessive memory usage
                    int length = Math.Min(BitConverter.ToInt32(input, 0) & 0x7F, input.Length);
                    if (length < 0) length = 0;

                    // Write the array length
                    writer.Write(length);

                    // Write the array elements
                    for (int i = 0; i < length && i < input.Length; i++)
                    {
                        writer.Write(input[i]);
                    }

                    _coveragePoints.Add("ArraySerialization_Write");
                }

                // Reset the stream position
                ms.Position = 0;

                // Read the array back
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    try
                    {
                        int length = reader.ReadInt32();

                        // Read the array elements
                        for (int i = 0; i < length; i++)
                        {
                            byte value = reader.ReadByte();
                        }

                        _coveragePoints.Add("ArraySerialization_Read");
                    }
                    catch (EndOfStreamException)
                    {
                        _coveragePoints.Add("ArraySerialization_EndOfStream");
                    }
                }
            }
        }

        /// <summary>
        /// Tests serialization and deserialization of strings
        /// </summary>
        /// <param name="input">The input data to test</param>
        private void TestStringSerialization(byte[] input)
        {
            _coveragePoints.Add("StringSerialization");

            if (input.Length < 4)
            {
                return;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                // Write string data
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    // Create a string from the input bytes
                    // Limit the length to avoid excessive memory usage
                    int length = Math.Min(BitConverter.ToInt32(input, 0) & 0x3F, input.Length);
                    if (length < 0) length = 0;

                    // Convert bytes to a string
                    string value = "";
                    for (int i = 4; i < 4 + length && i < input.Length; i++)
                    {
                        // Use only printable ASCII characters
                        value += (char)(input[i] % 95 + 32);
                    }

                    // Write the string
                    writer.Write(value);

                    _coveragePoints.Add("StringSerialization_Write");
                }

                // Reset the stream position
                ms.Position = 0;

                // Read the string back
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    try
                    {
                        string value = reader.ReadString();
                        _coveragePoints.Add("StringSerialization_Read");
                    }
                    catch (EndOfStreamException)
                    {
                        _coveragePoints.Add("StringSerialization_EndOfStream");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the coverage information for this target
        /// </summary>
        /// <returns>The coverage information</returns>
        public object GetCoverage()
        {
            return _coveragePoints;
        }
    }
}

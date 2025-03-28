// Copyright (C) 2015-2025 The Neo Project.
//
// MemoryReaderTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Target that tests the MemoryReader struct
    /// </summary>
    public class MemoryReaderTarget : IFuzzTarget
    {
        public string Name => "MemoryReader";
        private readonly HashSet<string> _coveragePoints = new();

        /// <summary>
        /// Executes the target with the provided input
        /// </summary>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length == 0)
                return false;

            try
            {
                // Create a MemoryReader from the input
                var reader = new MemoryReader(input);
                _coveragePoints.Add("MemoryReader:Created");

                // Test various read methods based on the input length
                TestReaderMethods(reader, input.Length);
                return true;
            }
            catch (Exception ex)
            {
                // Track the exception as a coverage point
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
        /// Tests various MemoryReader methods based on the available data
        /// </summary>
        private void TestReaderMethods(MemoryReader reader, int inputLength)
        {
            // Get the initial position
            int initialPosition = reader.Position;
            _coveragePoints.Add($"Position:Initial:{initialPosition}");

            // Test reading methods based on available data
            TestReadByte(reader, inputLength);
            TestReadBoolean(reader, inputLength);
            TestReadInt16(reader, inputLength);
            TestReadUInt16(reader, inputLength);
            TestReadInt32(reader, inputLength);
            TestReadUInt32(reader, inputLength);
            TestReadInt64(reader, inputLength);
            TestReadUInt64(reader, inputLength);
            TestReadVarInt(reader, inputLength);
            TestReadVarBytes(reader, inputLength);
            TestReadString(reader, inputLength);
            TestPeek(reader, inputLength);
            TestReadFixedString(reader, inputLength);
        }

        private void TestReadByte(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read a byte
                if (reader.Position < inputLength)
                {
                    byte value = reader.ReadByte();
                    _coveragePoints.Add($"ReadByte:Success:{value}");
                }
                else
                {
                    _coveragePoints.Add("ReadByte:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadByte:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadBoolean(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read a boolean
                if (reader.Position < inputLength)
                {
                    bool value = reader.ReadBoolean();
                    _coveragePoints.Add($"ReadBoolean:Success:{value}");
                }
                else
                {
                    _coveragePoints.Add("ReadBoolean:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadBoolean:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadInt16(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read an Int16
                if (reader.Position + sizeof(short) <= inputLength)
                {
                    short value = reader.ReadInt16();
                    _coveragePoints.Add($"ReadInt16:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadInt16:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadInt16:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadUInt16(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read a UInt16
                if (reader.Position + sizeof(ushort) <= inputLength)
                {
                    ushort value = reader.ReadUInt16();
                    _coveragePoints.Add($"ReadUInt16:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadUInt16:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadUInt16:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadInt32(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read an Int32
                if (reader.Position + sizeof(int) <= inputLength)
                {
                    int value = reader.ReadInt32();
                    _coveragePoints.Add($"ReadInt32:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadInt32:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadInt32:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadUInt32(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read a UInt32
                if (reader.Position + sizeof(uint) <= inputLength)
                {
                    uint value = reader.ReadUInt32();
                    _coveragePoints.Add($"ReadUInt32:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadUInt32:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadUInt32:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadInt64(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read an Int64
                if (reader.Position + sizeof(long) <= inputLength)
                {
                    long value = reader.ReadInt64();
                    _coveragePoints.Add($"ReadInt64:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadInt64:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadInt64:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadUInt64(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's enough data to read a UInt64
                if (reader.Position + sizeof(ulong) <= inputLength)
                {
                    ulong value = reader.ReadUInt64();
                    _coveragePoints.Add($"ReadUInt64:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadUInt64:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadUInt64:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadVarInt(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's at least one byte to read
                if (reader.Position < inputLength)
                {
                    ulong value = reader.ReadVarInt();
                    _coveragePoints.Add($"ReadVarInt:Success");
                }
                else
                {
                    _coveragePoints.Add("ReadVarInt:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadVarInt:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadVarBytes(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's at least one byte to read
                if (reader.Position < inputLength)
                {
                    ReadOnlyMemory<byte> value = reader.ReadVarMemory();
                    _coveragePoints.Add($"ReadVarBytes:Success:Length:{value.Length}");
                }
                else
                {
                    _coveragePoints.Add("ReadVarBytes:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadVarBytes:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadString(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's at least one byte to read
                if (reader.Position < inputLength)
                {
                    string value = reader.ReadVarString();
                    _coveragePoints.Add($"ReadString:Success:Length:{value.Length}");
                }
                else
                {
                    _coveragePoints.Add("ReadString:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadString:Exception:{ex.GetType().Name}");
            }
        }

        private void TestPeek(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's at least one byte to peek
                if (reader.Position < inputLength)
                {
                    byte value = reader.Peek();
                    _coveragePoints.Add($"Peek:Success:{value}");
                }
                else
                {
                    _coveragePoints.Add("Peek:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"Peek:Exception:{ex.GetType().Name}");
            }
        }

        private void TestReadFixedString(MemoryReader reader, int inputLength)
        {
            try
            {
                // Check if there's at least one byte to read
                if (reader.Position < inputLength)
                {
                    int length = Math.Min(inputLength - reader.Position, 10);
                    string value = reader.ReadFixedString(length);
                    _coveragePoints.Add($"ReadFixedString:Success:Length:{value.Length}");
                }
                else
                {
                    _coveragePoints.Add("ReadFixedString:NotEnoughData");
                }
            }
            catch (Exception ex)
            {
                _coveragePoints.Add($"ReadFixedString:Exception:{ex.GetType().Name}");
            }
        }
    }
}

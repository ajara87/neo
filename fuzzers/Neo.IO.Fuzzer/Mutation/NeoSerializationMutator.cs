// Copyright (C) 2015-2025 The Neo Project.
//
// NeoSerializationMutator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.IO.Fuzzer.Mutation
{
    /// <summary>
    /// A specialized mutator that targets Neo.IO-specific serialization edge cases
    /// </summary>
    public class NeoSerializationMutator : IMutator
    {
        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        public string Name => "NeoSerialization";

        // Special values for boundary testing
        private static readonly byte[][] InterestingValues = new byte[][]
        {
            BitConverter.GetBytes(int.MaxValue),
            BitConverter.GetBytes(int.MinValue),
            BitConverter.GetBytes(long.MaxValue),
            BitConverter.GetBytes(long.MinValue),
            BitConverter.GetBytes(0),
            BitConverter.GetBytes(1),
            BitConverter.GetBytes(-1),
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, // VarInt max
            new byte[] { 0xFD, 0xFF, 0xFF }, // VarInt 0xFFFF
            new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF }, // VarInt 0xFFFFFFFF
            Encoding.UTF8.GetBytes("Neo"),
            Encoding.UTF8.GetBytes(""),
            new byte[] { 0xC0, 0xAF, 0xE0 } // Invalid UTF-8
        };

        // Mutation strategies
        private enum MutationStrategy
        {
            ModifyVarInt,
            ModifyVarString,
            ModifyArray,
            InsertInterestingValue,
            CorruptFormat
        }

        /// <summary>
        /// Mutates the input data with Neo.IO-specific mutations
        /// </summary>
        /// <param name="data">The input data to mutate</param>
        /// <param name="random">The random number generator</param>
        /// <returns>The mutated data</returns>
        public byte[] Mutate(byte[] data, Random random)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            // Make a copy of the data to mutate
            byte[] result = data.ToArray();

            // Select a mutation strategy
            MutationStrategy strategy = (MutationStrategy)random.Next(Enum.GetValues(typeof(MutationStrategy)).Length);

            switch (strategy)
            {
                case MutationStrategy.ModifyVarInt:
                    result = ModifyVarInt(result, random);
                    break;
                case MutationStrategy.ModifyVarString:
                    result = ModifyVarString(result, random);
                    break;
                case MutationStrategy.ModifyArray:
                    result = ModifyArray(result, random);
                    break;
                case MutationStrategy.InsertInterestingValue:
                    result = InsertInterestingValue(result, random);
                    break;
                case MutationStrategy.CorruptFormat:
                    result = CorruptFormat(result, random);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Attempts to find and modify a VarInt in the data
        /// </summary>
        private byte[] ModifyVarInt(byte[] data, Random random)
        {
            // Try to find a VarInt in the data
            for (int i = 0; i < data.Length; i++)
            {
                // Check for VarInt markers
                if (i + 1 < data.Length && (data[i] == 0xFD || data[i] == 0xFE || data[i] == 0xFF))
                {
                    byte[] result = data.ToArray();
                    int modification = random.Next(3);

                    switch (modification)
                    {
                        case 0: // Change the VarInt marker
                            result[i] = (byte)(0xFD + random.Next(3));
                            break;
                        case 1: // Modify the VarInt value
                            if (data[i] == 0xFD && i + 2 < data.Length) // 2-byte VarInt
                            {
                                result[i + 1] = (byte)random.Next(256);
                                result[i + 2] = (byte)random.Next(256);
                            }
                            else if (data[i] == 0xFE && i + 4 < data.Length) // 4-byte VarInt
                            {
                                for (int j = 1; j <= 4; j++)
                                    result[i + j] = (byte)random.Next(256);
                            }
                            else if (data[i] == 0xFF && i + 8 < data.Length) // 8-byte VarInt
                            {
                                for (int j = 1; j <= 8; j++)
                                    result[i + j] = (byte)random.Next(256);
                            }
                            break;
                        case 2: // Replace with a known interesting value
                            byte[] interestingValue = InterestingValues[random.Next(InterestingValues.Length)];
                            int maxCopy = Math.Min(interestingValue.Length, data.Length - i);
                            Array.Copy(interestingValue, 0, result, i, maxCopy);
                            break;
                    }

                    return result;
                }
                else if (data[i] < 0xFD) // Single-byte VarInt
                {
                    byte[] result = data.ToArray();
                    result[i] = (byte)random.Next(0xFD);
                    return result;
                }
            }

            // If no VarInt found, just modify a random byte
            return ModifyRandomBytes(data, random, 1);
        }

        /// <summary>
        /// Attempts to find and modify a VarString in the data
        /// </summary>
        private byte[] ModifyVarString(byte[] data, Random random)
        {
            // Try to find a VarString (VarInt followed by string data)
            for (int i = 0; i < data.Length; i++)
            {
                if (TryParseVarInt(data, i, out int length, out int bytesRead))
                {
                    int stringStart = i + bytesRead;
                    int stringEnd = stringStart + length;

                    if (stringEnd <= data.Length)
                    {
                        byte[] result = data.ToArray();
                        int modification = random.Next(3);

                        switch (modification)
                        {
                            case 0: // Modify the length
                                int newLength = random.Next(Math.Max(1, length - 5), length + 5);
                                newLength = Math.Max(0, Math.Min(newLength, data.Length - stringStart));
                                WriteVarInt(result, i, newLength);
                                break;
                            case 1: // Modify string content
                                for (int j = 0; j < Math.Min(length, 5); j++)
                                {
                                    int pos = stringStart + random.Next(length);
                                    if (pos < data.Length)
                                        result[pos] = (byte)random.Next(256);
                                }
                                break;
                            case 2: // Replace with invalid UTF-8
                                int invalidPos = stringStart + random.Next(Math.Max(1, length));
                                if (invalidPos < data.Length - 2)
                                {
                                    result[invalidPos] = 0xC0;
                                    result[invalidPos + 1] = 0xAF;
                                }
                                break;
                        }

                        return result;
                    }
                }
            }

            // If no VarString found, just modify a random byte
            return ModifyRandomBytes(data, random, 1);
        }

        /// <summary>
        /// Attempts to find and modify an array in the data
        /// </summary>
        private byte[] ModifyArray(byte[] data, Random random)
        {
            // Try to find an array (VarInt followed by data)
            for (int i = 0; i < data.Length; i++)
            {
                if (TryParseVarInt(data, i, out int length, out int bytesRead))
                {
                    int arrayStart = i + bytesRead;
                    int arrayEnd = arrayStart + length;

                    if (arrayEnd <= data.Length && length > 0)
                    {
                        byte[] result = data.ToArray();
                        int modification = random.Next(3);

                        switch (modification)
                        {
                            case 0: // Modify the length
                                int newLength = random.Next(Math.Max(1, length - 5), length + 5);
                                newLength = Math.Max(0, Math.Min(newLength, data.Length - arrayStart));
                                WriteVarInt(result, i, newLength);
                                break;
                            case 1: // Modify array content
                                for (int j = 0; j < Math.Min(length, 5); j++)
                                {
                                    int pos = arrayStart + random.Next(length);
                                    if (pos < data.Length)
                                        result[pos] = (byte)random.Next(256);
                                }
                                break;
                            case 2: // Swap elements
                                if (length >= 2)
                                {
                                    int pos1 = arrayStart + random.Next(length);
                                    int pos2 = arrayStart + random.Next(length);
                                    if (pos1 < data.Length && pos2 < data.Length)
                                    {
                                        byte temp = result[pos1];
                                        result[pos1] = result[pos2];
                                        result[pos2] = temp;
                                    }
                                }
                                break;
                        }

                        return result;
                    }
                }
            }

            // If no array found, just modify a random byte
            return ModifyRandomBytes(data, random, 1);
        }

        /// <summary>
        /// Inserts an interesting value at a random position
        /// </summary>
        private byte[] InsertInterestingValue(byte[] data, Random random)
        {
            byte[] interestingValue = InterestingValues[random.Next(InterestingValues.Length)];
            int position = random.Next(data.Length);

            // Either insert or overwrite
            if (random.Next(2) == 0 && data.Length + interestingValue.Length < 10000) // Insert
            {
                byte[] result = new byte[data.Length + interestingValue.Length];
                Array.Copy(data, 0, result, 0, position);
                Array.Copy(interestingValue, 0, result, position, interestingValue.Length);
                Array.Copy(data, position, result, position + interestingValue.Length, data.Length - position);
                return result;
            }
            else // Overwrite
            {
                byte[] result = data.ToArray();
                int maxCopy = Math.Min(interestingValue.Length, data.Length - position);
                Array.Copy(interestingValue, 0, result, position, maxCopy);
                return result;
            }
        }

        /// <summary>
        /// Deliberately corrupts the format to test error handling
        /// </summary>
        private byte[] CorruptFormat(byte[] data, Random random)
        {
            byte[] result = data.ToArray();
            int corruptionType = random.Next(3);

            switch (corruptionType)
            {
                case 0: // Truncate data
                    int newLength = random.Next(data.Length);
                    Array.Resize(ref result, newLength);
                    break;
                case 1: // Insert garbage in the middle
                    int position = random.Next(data.Length);
                    for (int i = 0; i < Math.Min(5, data.Length - position); i++)
                    {
                        result[position + i] = (byte)random.Next(256);
                    }
                    break;
                case 2: // Flip bits in multiple locations
                    int numFlips = random.Next(1, 5);
                    for (int i = 0; i < numFlips; i++)
                    {
                        int pos = random.Next(data.Length);
                        result[pos] ^= (byte)(1 << random.Next(8));
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Modifies a random number of bytes in the data
        /// </summary>
        private byte[] ModifyRandomBytes(byte[] data, Random random, int count)
        {
            byte[] result = data.ToArray();
            for (int i = 0; i < count; i++)
            {
                int position = random.Next(data.Length);
                result[position] = (byte)random.Next(256);
            }
            return result;
        }

        /// <summary>
        /// Tries to parse a VarInt at the specified position
        /// </summary>
        private bool TryParseVarInt(byte[] data, int position, out int value, out int bytesRead)
        {
            value = 0;
            bytesRead = 0;

            if (position >= data.Length)
                return false;

            if (data[position] < 0xFD)
            {
                value = data[position];
                bytesRead = 1;
                return true;
            }
            else if (data[position] == 0xFD)
            {
                if (position + 2 >= data.Length)
                    return false;

                value = data[position + 1] | (data[position + 2] << 8);
                bytesRead = 3;
                return true;
            }
            else if (data[position] == 0xFE)
            {
                if (position + 4 >= data.Length)
                    return false;

                value = data[position + 1] |
                       (data[position + 2] << 8) |
                       (data[position + 3] << 16) |
                       (data[position + 4] << 24);
                bytesRead = 5;
                return true;
            }

            // 0xFF (8-byte VarInt) not handled for simplicity
            return false;
        }

        /// <summary>
        /// Writes a VarInt at the specified position
        /// </summary>
        private void WriteVarInt(byte[] data, int position, int value)
        {
            if (position >= data.Length)
                return;

            if (value < 0xFD)
            {
                data[position] = (byte)value;
            }
            else if (value <= 0xFFFF && position + 2 < data.Length)
            {
                data[position] = 0xFD;
                data[position + 1] = (byte)(value & 0xFF);
                data[position + 2] = (byte)((value >> 8) & 0xFF);
            }
            else if (position + 4 < data.Length)
            {
                data[position] = 0xFE;
                data[position + 1] = (byte)(value & 0xFF);
                data[position + 2] = (byte)((value >> 8) & 0xFF);
                data[position + 3] = (byte)((value >> 16) & 0xFF);
                data[position + 4] = (byte)((value >> 24) & 0xFF);
            }
        }
    }
}

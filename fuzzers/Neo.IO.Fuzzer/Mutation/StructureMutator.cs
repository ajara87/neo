// Copyright (C) 2015-2025 The Neo Project.
//
// StructureMutator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.IO.Fuzzer.Mutation
{
    /// <summary>
    /// Mutator that modifies the structure of the input data
    /// </summary>
    public class StructureMutator : IMutator
    {
        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        public string Name => "Structure";

        /// <summary>
        /// Mutates the provided binary data by modifying its structure
        /// </summary>
        /// <param name="data">The binary data to mutate</param>
        /// <param name="random">The random number generator</param>
        /// <returns>The mutated binary data</returns>
        public byte[] Mutate(byte[] data, Random random)
        {
            if (data == null || data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            if (random == null)
                throw new ArgumentNullException(nameof(random));

            // Select a mutation strategy
            int strategy = random.Next(4);

            switch (strategy)
            {
                case 0:
                    // Insert random bytes
                    return InsertRandomBytes(data, random);

                case 1:
                    // Delete random bytes
                    return DeleteRandomBytes(data, random);

                case 2:
                    // Duplicate a segment
                    return DuplicateSegment(data, random);

                case 3:
                    // Swap segments
                    return SwapSegments(data, random);

                default:
                    return (byte[])data.Clone();
            }
        }

        /// <summary>
        /// Inserts random bytes into the data
        /// </summary>
        private byte[] InsertRandomBytes(byte[] data, Random random)
        {
            // Determine how many bytes to insert (1-10)
            int bytesToInsert = random.Next(1, 11);

            // Create a new array with the additional space
            byte[] result = new byte[data.Length + bytesToInsert];

            // Determine where to insert the bytes
            int insertPosition = random.Next(data.Length + 1);

            // Copy the first part of the original data
            Array.Copy(data, 0, result, 0, insertPosition);

            // Generate and insert random bytes
            for (int i = 0; i < bytesToInsert; i++)
            {
                result[insertPosition + i] = (byte)random.Next(256);
            }

            // Copy the rest of the original data
            Array.Copy(data, insertPosition, result, insertPosition + bytesToInsert, data.Length - insertPosition);

            return result;
        }

        /// <summary>
        /// Deletes random bytes from the data
        /// </summary>
        private byte[] DeleteRandomBytes(byte[] data, Random random)
        {
            if (data.Length <= 1)
            {
                return data;
            }

            // Determine how many bytes to delete (1 to half the data length)
            int bytesToDelete = random.Next(1, Math.Max(2, data.Length / 2));

            // Create a new array with the reduced space
            byte[] result = new byte[data.Length - bytesToDelete];

            // Determine where to delete the bytes from
            int deletePosition = random.Next(data.Length - bytesToDelete + 1);

            // Copy the first part of the original data
            Array.Copy(data, 0, result, 0, deletePosition);

            // Copy the rest of the original data, skipping the deleted bytes
            Array.Copy(data, deletePosition + bytesToDelete, result, deletePosition, data.Length - deletePosition - bytesToDelete);

            return result;
        }

        /// <summary>
        /// Duplicates a segment of the data
        /// </summary>
        private byte[] DuplicateSegment(byte[] data, Random random)
        {
            if (data.Length <= 1)
            {
                return data;
            }

            // Determine the segment size (1 to half the data length)
            int segmentSize = random.Next(1, Math.Max(2, data.Length / 2));

            // Determine the segment position
            int segmentPosition = random.Next(data.Length - segmentSize + 1);

            // Create a new array with the additional space
            byte[] result = new byte[data.Length + segmentSize];

            // Determine where to insert the duplicate
            int insertPosition = random.Next(data.Length + 1);

            // Copy the first part of the original data
            Array.Copy(data, 0, result, 0, insertPosition);

            // Copy the segment to duplicate
            Array.Copy(data, segmentPosition, result, insertPosition, segmentSize);

            // Copy the rest of the original data
            Array.Copy(data, insertPosition, result, insertPosition + segmentSize, data.Length - insertPosition);

            return result;
        }

        /// <summary>
        /// Swaps two segments of the data
        /// </summary>
        private byte[] SwapSegments(byte[] data, Random random)
        {
            // If data is too small, return it unchanged
            if (data.Length < 4)
            {
                return (byte[])data.Clone();
            }

            // Create a copy of the data
            byte[] result = (byte[])data.Clone();

            try
            {
                // Determine the first segment size and position (limit to 1/4 of the data)
                int segment1Size = random.Next(1, Math.Max(2, Math.Min(data.Length / 4, 10)));
                int segment1Position = random.Next(0, data.Length - segment1Size);

                // Determine the second segment size and position (limit to 1/4 of the data)
                int segment2Size = random.Next(1, Math.Max(2, Math.Min(data.Length / 4, 10)));
                int maxSegment2Position = data.Length - segment2Size;

                // Ensure the segments don't overlap
                int segment2Position;

                // Calculate valid ranges for segment2Position that don't overlap with segment1
                List<(int start, int end)> validRanges = new List<(int start, int end)>();

                // Range before segment1
                if (segment1Position > 0)
                {
                    validRanges.Add((0, Math.Max(0, segment1Position - segment2Size)));
                }

                // Range after segment1
                if (segment1Position + segment1Size < data.Length)
                {
                    validRanges.Add((segment1Position + segment1Size, Math.Max(segment1Position + segment1Size, data.Length - segment2Size)));
                }

                // If no valid ranges, return the original data
                if (validRanges.Count == 0)
                {
                    return result;
                }

                // Select a random valid range
                var selectedRange = validRanges[random.Next(validRanges.Count)];

                // If the range is invalid (start > end), return the original data
                if (selectedRange.start > selectedRange.end)
                {
                    return result;
                }

                // Select a random position within the range
                segment2Position = random.Next(selectedRange.start, selectedRange.end + 1);

                // Ensure segment2Position is within bounds
                segment2Position = Math.Min(segment2Position, data.Length - segment2Size);

                // Create temporary buffers for the segments
                byte[] segment1 = new byte[segment1Size];
                byte[] segment2 = new byte[segment2Size];

                // Copy the segments to the temporary buffers
                Array.Copy(data, segment1Position, segment1, 0, segment1Size);
                Array.Copy(data, segment2Position, segment2, 0, segment2Size);

                // Swap the segments
                Array.Copy(segment2, 0, result, segment1Position, segment2Size);
                Array.Copy(segment1, 0, result, segment2Position, segment1Size);
            }
            catch (Exception)
            {
                // If any error occurs, return the original data
                return (byte[])data.Clone();
            }

            return result;
        }
    }
}

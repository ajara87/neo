// Copyright (C) 2015-2025 The Neo Project.
//
// EndiannessMutator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.IO.Fuzzer.Mutation
{
    /// <summary>
    /// Mutator that swaps the endianness of multi-byte values in the input data
    /// </summary>
    public class EndiannessMutator : IMutator
    {
        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        public string Name => "Endianness";

        /// <summary>
        /// Mutates the provided binary data by swapping endianness of multi-byte values
        /// </summary>
        /// <param name="data">The binary data to mutate</param>
        /// <param name="random">The random number generator</param>
        /// <returns>The mutated binary data</returns>
        public byte[] Mutate(byte[] data, Random random)
        {
            if (data == null || data.Length < 2)
            {
                return Array.Empty<byte>();
            }

            if (random == null)
                throw new ArgumentNullException(nameof(random));

            // Create a copy of the data
            byte[] mutatedData = (byte[])data.Clone();

            // Determine how many swaps to perform (between 1 and 5)
            int swapCount = random.Next(1, Math.Min(6, data.Length / 2));

            for (int i = 0; i < swapCount; i++)
            {
                // Determine the size of the value to swap (2, 4, or 8 bytes)
                int valueSize = random.Next(3) switch
                {
                    0 => 2, // 16-bit value
                    1 => 4, // 32-bit value
                    _ => 8  // 64-bit value
                };

                // Ensure we have enough data for this value size
                if (data.Length < valueSize)
                {
                    valueSize = data.Length >= 2 ? 2 : 0;
                }

                if (valueSize == 0)
                {
                    continue; // Skip this swap
                }

                // Select a random position for the value (ensuring it fits)
                int position = random.Next(data.Length - valueSize + 1);

                // Swap the bytes
                for (int j = 0; j < valueSize / 2; j++)
                {
                    byte temp = mutatedData[position + j];
                    mutatedData[position + j] = mutatedData[position + valueSize - j - 1];
                    mutatedData[position + valueSize - j - 1] = temp;
                }
            }

            return mutatedData;
        }
    }
}

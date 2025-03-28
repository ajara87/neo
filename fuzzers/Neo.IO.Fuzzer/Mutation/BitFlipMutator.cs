// Copyright (C) 2015-2025 The Neo Project.
//
// BitFlipMutator.cs file belongs to the neo project and is free
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
    /// Mutator that flips random bits in the input data
    /// </summary>
    public class BitFlipMutator : IMutator
    {
        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        public string Name => "BitFlip";

        /// <summary>
        /// Mutates the provided binary data by flipping random bits
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

            // Create a copy of the data
            byte[] mutatedData = (byte[])data.Clone();

            // Determine how many bits to flip (between 1 and 5)
            int bitFlipCount = random.Next(1, Math.Min(6, data.Length * 8));

            for (int i = 0; i < bitFlipCount; i++)
            {
                // Select a random byte
                int byteIndex = random.Next(mutatedData.Length);

                // Select a random bit within the byte
                int bitIndex = random.Next(8);

                // Flip the bit
                mutatedData[byteIndex] ^= (byte)(1 << bitIndex);
            }

            return mutatedData;
        }
    }
}

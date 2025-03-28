// Copyright (C) 2015-2025 The Neo Project.
//
// ByteFlipMutator.cs file belongs to the neo project and is free
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
    /// Mutator that flips random bytes in the input data
    /// </summary>
    public class ByteFlipMutator : IMutator
    {
        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        public string Name => "ByteFlip";

        /// <summary>
        /// Mutates the provided binary data by flipping random bytes
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

            // Determine how many bytes to flip (between 1 and 5)
            int byteFlipCount = random.Next(1, Math.Min(6, data.Length));

            for (int i = 0; i < byteFlipCount; i++)
            {
                // Select a random byte
                int byteIndex = random.Next(mutatedData.Length);

                // Flip the byte (invert all bits)
                mutatedData[byteIndex] = (byte)~mutatedData[byteIndex];
            }

            return mutatedData;
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// ValueMutator.cs file belongs to the neo project and is free
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
    /// Mutator that replaces values with boundary cases or special values
    /// </summary>
    public class ValueMutator : IMutator
    {
        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        public string Name => "Value";

        // Special values to inject
        private static readonly byte[][] _specialValues = new byte[][]
        {
            new byte[] { 0x00 },                          // Null byte
            new byte[] { 0xFF },                          // Max byte
            new byte[] { 0x7F },                          // Max positive signed byte
            new byte[] { 0x80 },                          // Min negative signed byte
            new byte[] { 0x00, 0x00 },                    // Zero (2 bytes)
            new byte[] { 0xFF, 0xFF },                    // Max unsigned short
            new byte[] { 0x7F, 0xFF },                    // Max signed short
            new byte[] { 0x80, 0x00 },                    // Min signed short
            new byte[] { 0x00, 0x00, 0x00, 0x00 },        // Zero (4 bytes)
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },        // Max unsigned int
            new byte[] { 0x7F, 0xFF, 0xFF, 0xFF },        // Max signed int
            new byte[] { 0x80, 0x00, 0x00, 0x00 },        // Min signed int
            new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, // Zero (8 bytes)
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, // Max unsigned long
            new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, // Max signed long
            new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }  // Min signed long
        };

        /// <summary>
        /// Mutates the provided binary data by replacing parts with special values
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

            // Determine how many values to replace (between 1 and 3)
            int replaceCount = random.Next(1, 4);

            for (int i = 0; i < replaceCount; i++)
            {
                // Select a random special value
                byte[] specialValue = _specialValues[random.Next(_specialValues.Length)];

                // If the data is smaller than the special value, skip this iteration
                if (mutatedData.Length < specialValue.Length)
                {
                    continue;
                }

                // Select a random position to insert the special value
                int position = random.Next(0, mutatedData.Length - specialValue.Length + 1);

                // Replace the bytes at the selected position with the special value
                for (int j = 0; j < specialValue.Length; j++)
                {
                    mutatedData[position + j] = specialValue[j];
                }
            }

            return mutatedData;
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// IMutator.cs file belongs to the neo project and is free
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
    /// Interface for mutators that transform input data
    /// </summary>
    public interface IMutator
    {
        /// <summary>
        /// Mutates the input data
        /// </summary>
        /// <param name="data">The input data to mutate</param>
        /// <param name="random">The random number generator</param>
        /// <returns>The mutated data</returns>
        byte[] Mutate(byte[] data, Random random);

        /// <summary>
        /// Gets the name of the mutator
        /// </summary>
        string Name { get; }
    }
}

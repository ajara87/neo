// Copyright (C) 2015-2025 The Neo Project.
//
// IBinaryGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.IO.Fuzzer.Generators
{
    /// <summary>
    /// Interface for generating binary data for fuzzing
    /// </summary>
    public interface IBinaryGenerator
    {
        /// <summary>
        /// Generates a random binary array
        /// </summary>
        /// <param name="random">The random number generator</param>
        /// <param name="maxSize">The maximum size of the generated data</param>
        /// <returns>The generated binary data</returns>
        byte[] Generate(Random random, int maxSize);
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// ICacheFuzzStrategy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Interface for cache fuzzing strategies that implement different approaches to testing the Neo caching system.
    /// </summary>
    public interface ICacheFuzzStrategy
    {
        /// <summary>
        /// Executes the fuzzing strategy with the provided input data.
        /// </summary>
        /// <param name="input">The input data to use for fuzzing.</param>
        /// <returns>True if the execution was successful, false otherwise.</returns>
        bool Execute(byte[] input);

        /// <summary>
        /// Gets the coverage information for the strategy.
        /// </summary>
        /// <returns>An object containing coverage information.</returns>
        object GetCoverage();

        /// <summary>
        /// Gets the name of the strategy.
        /// </summary>
        /// <returns>The strategy name.</returns>
        string GetName();
    }
}

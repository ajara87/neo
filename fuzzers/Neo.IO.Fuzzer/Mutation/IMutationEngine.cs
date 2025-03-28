// Copyright (C) 2015-2025 The Neo Project.
//
// IMutationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Fuzzer.Mutation
{
    /// <summary>
    /// Interface for mutation engines that transform input data
    /// </summary>
    public interface IMutationEngine
    {
        /// <summary>
        /// Mutates the input data
        /// </summary>
        /// <param name="data">The input data to mutate</param>
        /// <returns>The mutated data</returns>
        byte[] Mutate(byte[] data);

        /// <summary>
        /// Adds a custom mutator to the engine
        /// </summary>
        /// <param name="mutator">The mutator to add</param>
        void AddMutator(IMutator mutator);

        /// <summary>
        /// Gets the list of registered mutators
        /// </summary>
        /// <returns>The list of mutators</returns>
        IReadOnlyList<IMutator> GetMutators();
    }
}

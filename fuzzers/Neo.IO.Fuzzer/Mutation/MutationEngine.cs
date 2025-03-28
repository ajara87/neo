// Copyright (C) 2015-2025 The Neo Project.
//
// MutationEngine.cs file belongs to the neo project and is free
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

namespace Neo.IO.Fuzzer.Mutation
{
    /// <summary>
    /// Engine that applies multiple mutation strategies to input data
    /// </summary>
    public class MutationEngine : IMutationEngine
    {
        private readonly List<IMutator> _mutators = new();
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the MutationEngine class
        /// </summary>
        /// <param name="random">The random number generator to use</param>
        public MutationEngine(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Adds a custom mutator to the engine
        /// </summary>
        /// <param name="mutator">The mutator to add</param>
        public void AddMutator(IMutator mutator)
        {
            if (mutator == null)
                throw new ArgumentNullException(nameof(mutator));

            _mutators.Add(mutator);
        }

        /// <summary>
        /// Gets all mutators in this engine
        /// </summary>
        /// <returns>The list of mutators</returns>
        public virtual IReadOnlyList<IMutator> GetMutators()
        {
            return _mutators.AsReadOnly();
        }

        /// <summary>
        /// Applies mutations to the input data
        /// </summary>
        /// <param name="data">The data to mutate</param>
        /// <returns>The mutated data</returns>
        public byte[] Mutate(byte[] data)
        {
            return Mutate(data, 1);
        }

        /// <summary>
        /// Applies multiple mutations to the input data
        /// </summary>
        /// <param name="data">The data to mutate</param>
        /// <param name="mutationCount">The number of mutations to apply</param>
        /// <returns>The mutated data</returns>
        public byte[] Mutate(byte[] data, int mutationCount)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (mutationCount <= 0 || _mutators.Count == 0)
                return data;

            byte[] result = data;

            for (int i = 0; i < mutationCount; i++)
            {
                // Select a random mutator
                IMutator mutator = _mutators[_random.Next(_mutators.Count)];

                // Apply the mutation
                result = mutator.Mutate(result, _random);
            }

            return result;
        }

        /// <summary>
        /// Creates a mutation engine with the default set of mutators
        /// </summary>
        /// <param name="random">The random number generator to use</param>
        /// <returns>A configured mutation engine</returns>
        public static MutationEngine CreateWithDefaultMutators(Random random)
        {
            var engine = new MutationEngine(random);

            // Add all the default mutators
            engine.AddMutator(new BitFlipMutator());
            engine.AddMutator(new ByteFlipMutator());
            engine.AddMutator(new EndiannessMutator());
            engine.AddMutator(new StructureMutator());
            engine.AddMutator(new ValueMutator());
            engine.AddMutator(new NeoSerializationMutator());

            return engine;
        }
    }
}

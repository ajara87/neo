// Copyright (C) 2015-2025 The Neo Project.
//
// GuidedMutationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.TestHarness;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Fuzzer.Mutation
{
    /// <summary>
    /// A mutation engine that selects and applies mutation strategies based on coverage feedback
    /// </summary>
    public class GuidedMutationEngine : MutationEngine
    {
        private readonly List<IMutator> _mutators;
        private readonly Random _random;
        private EnhancedCoverageTracker? _coverageTracker;

        // Statistics for each mutator
        private readonly Dictionary<IMutator, MutatorStats> _mutatorStats = new();

        // Minimum number of executions before considering statistics
        private const int MinExecutionsForStats = 10;

        // Weight factors for different aspects of mutator performance
        private const double NewCoverageWeight = 0.6;
        private const double CrashWeight = 0.3;
        private const double SpeedWeight = 0.1;

        /// <summary>
        /// Initializes a new instance of the GuidedMutationEngine class
        /// </summary>
        /// <param name="mutators">The mutators to use</param>
        /// <param name="random">The random number generator to use</param>
        /// <param name="coverageTracker">Optional coverage tracker to use</param>
        public GuidedMutationEngine(List<IMutator> mutators, Random random, EnhancedCoverageTracker? coverageTracker = null) : base(random)
        {
            _mutators = mutators ?? throw new ArgumentNullException(nameof(mutators));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _coverageTracker = coverageTracker;

            // Initialize statistics for each mutator
            foreach (var mutator in mutators)
            {
                _mutatorStats[mutator] = new MutatorStats();
            }
        }

        /// <summary>
        /// Sets the coverage tracker to use for guidance
        /// </summary>
        /// <param name="coverageTracker">The coverage tracker</param>
        public void SetCoverageTracker(EnhancedCoverageTracker? coverageTracker)
        {
            _coverageTracker = coverageTracker;
        }

        /// <summary>
        /// Adds a mutator to the engine
        /// </summary>
        /// <param name="mutator">The mutator to add</param>
        public new void AddMutator(IMutator mutator)
        {
            if (mutator == null)
                throw new ArgumentNullException(nameof(mutator));

            _mutators.Add(mutator);
            _mutatorStats[mutator] = new MutatorStats();
        }

        /// <summary>
        /// Gets the mutators in this engine
        /// </summary>
        /// <param name="includeDisabled">Whether to include disabled mutators</param>
        /// <returns>A list of all registered mutators</returns>
        public IReadOnlyList<IMutator> GetMutators(bool includeDisabled)
        {
            if (includeDisabled)
                return _mutators.AsReadOnly();
            else
                throw new NotImplementedException("Disabled mutators are not implemented");
        }

        /// <summary>
        /// Gets all mutators in this engine
        /// </summary>
        /// <returns>The list of mutators</returns>
        public override IReadOnlyList<IMutator> GetMutators()
        {
            return _mutators.AsReadOnly();
        }

        /// <summary>
        /// Records a successful mutation to guide future mutation strategies
        /// </summary>
        /// <param name="mutator">The mutator that produced the successful mutation</param>
        /// <param name="coverageIncrease">The amount of coverage increase</param>
        public void RecordSuccess(IMutator mutator, double coverageIncrease)
        {
            if (mutator == null)
                return;

            // Update mutator statistics
            if (!_mutatorStats.ContainsKey(mutator))
            {
                _mutatorStats[mutator] = new MutatorStats();
            }

            _mutatorStats[mutator].Successes++;
            _mutatorStats[mutator].TotalCoverageIncrease += coverageIncrease;
        }

        /// <summary>
        /// Mutates the input data using a coverage-guided approach
        /// </summary>
        /// <param name="data">The input data to mutate</param>
        /// <returns>The mutated data</returns>
        public new byte[] Mutate(byte[] data)
        {
            if (data == null)
                return new byte[0];

            // Select a mutator based on past performance
            IMutator mutator = SelectMutator();

            // Apply the mutation
            return mutator.Mutate(data, _random);
        }

        /// <summary>
        /// Mutates the input data using the most effective strategies based on coverage feedback
        /// </summary>
        /// <param name="input">The input data to mutate</param>
        /// <param name="iteration">The current iteration number</param>
        /// <returns>The mutated data</returns>
        public new byte[] Mutate(byte[] input, int iteration)
        {
            // Use iteration to potentially adjust mutation strategy
            var selectedMutator = SelectMutator(iteration);
            return selectedMutator.Mutate(input, _random);
        }

        /// <summary>
        /// Provides feedback on the effectiveness of the last mutation
        /// </summary>
        /// <param name="mutator">The mutator that was used</param>
        /// <param name="newCoverage">Whether the mutation led to new coverage</param>
        /// <param name="crashed">Whether the mutation led to a crash</param>
        /// <param name="executionTime">The execution time in milliseconds</param>
        public void ProvideFeedback(IMutator mutator, bool newCoverage, bool crashed, long executionTime)
        {
            if (!_mutatorStats.ContainsKey(mutator))
                return;

            var stats = _mutatorStats[mutator];
            stats.TotalExecutions++;

            if (newCoverage)
                stats.NewCoverageCount++;

            if (crashed)
                stats.CrashCount++;

            // Update running average of execution time
            stats.AverageExecutionTime =
                (stats.AverageExecutionTime * (stats.TotalExecutions - 1) + executionTime) /
                stats.TotalExecutions;
        }

        /// <summary>
        /// Selects a mutator based on its effectiveness
        /// </summary>
        /// <returns>The selected mutator</returns>
        private IMutator SelectMutator()
        {
            // If we don't have enough statistics, select randomly
            if (_mutators.All(m => _mutatorStats[m].TotalExecutions < MinExecutionsForStats))
            {
                return _mutators[_random.Next(_mutators.Count)];
            }

            // Calculate utility scores for each mutator
            var scores = new Dictionary<IMutator, double>();
            double totalScore = 0;

            foreach (var mutator in _mutators)
            {
                var stats = _mutatorStats[mutator];
                double score = CalculateUtilityScore(stats);
                scores[mutator] = score;
                totalScore += score;
            }

            // Select a mutator based on its score
            double randomValue = _random.NextDouble() * totalScore;
            double cumulativeScore = 0;

            foreach (var mutator in _mutators)
            {
                cumulativeScore += scores[mutator];
                if (randomValue <= cumulativeScore)
                    return mutator;
            }

            // Fallback to the last mutator if something went wrong
            return _mutators.Last();
        }

        private IMutator SelectMutator(int iteration)
        {
            return SelectMutator();
        }

        /// <summary>
        /// Calculates a utility score for a mutator based on its statistics
        /// </summary>
        /// <param name="stats">The mutator statistics</param>
        /// <returns>The utility score</returns>
        private double CalculateUtilityScore(MutatorStats stats)
        {
            if (stats.TotalExecutions == 0)
                return 1.0; // Default score for untested mutators

            // Calculate normalized metrics
            double coverageRate = (double)stats.NewCoverageCount / stats.TotalExecutions;
            double crashRate = (double)stats.CrashCount / stats.TotalExecutions;

            // Normalize execution time (lower is better)
            double normalizedSpeed = 1.0;
            if (stats.AverageExecutionTime > 0)
            {
                double maxTime = _mutatorStats.Values.Max(s => s.AverageExecutionTime);
                if (maxTime > 0)
                    normalizedSpeed = 1.0 - (stats.AverageExecutionTime / maxTime);
            }

            // Calculate weighted score
            double score = (coverageRate * NewCoverageWeight) +
                           (crashRate * CrashWeight) +
                           (normalizedSpeed * SpeedWeight);

            // Ensure minimum score to avoid starvation
            return Math.Max(0.1, score);
        }

        /// <summary>
        /// Resets the statistics for all mutators
        /// </summary>
        public void ResetStats()
        {
            foreach (var mutator in _mutators)
            {
                _mutatorStats[mutator] = new MutatorStats();
            }
        }

        /// <summary>
        /// Gets the current statistics for all mutators
        /// </summary>
        /// <returns>A dictionary mapping mutators to their statistics</returns>
        public Dictionary<string, MutatorStats> GetStats()
        {
            return _mutatorStats.ToDictionary(
                kvp => kvp.Key.GetType().Name,
                kvp => kvp.Value
            );
        }

        /// <summary>
        /// Creates a guided mutation engine with the specified mutators
        /// </summary>
        /// <param name="random">The random number generator</param>
        /// <param name="coverageTracker">The coverage tracker to use</param>
        /// <returns>A configured guided mutation engine</returns>
        public static GuidedMutationEngine Create(Random random, EnhancedCoverageTracker? coverageTracker)
        {
            var mutators = new List<IMutator>
            {
                new BitFlipMutator(),
                new ByteFlipMutator(),
                new EndiannessMutator(),
                new StructureMutator(),
                new ValueMutator(),
                new NeoSerializationMutator()
            };

            var engine = new GuidedMutationEngine(mutators, random, coverageTracker);

            return engine;
        }

        /// <summary>
        /// Statistics for a mutator
        /// </summary>
        public class MutatorStats
        {
            /// <summary>
            /// The total number of executions
            /// </summary>
            public int TotalExecutions { get; set; }

            /// <summary>
            /// The number of executions that led to new coverage
            /// </summary>
            public int NewCoverageCount { get; set; }

            /// <summary>
            /// The number of executions that led to crashes
            /// </summary>
            public int CrashCount { get; set; }

            /// <summary>
            /// The average execution time in milliseconds
            /// </summary>
            public double AverageExecutionTime { get; set; }

            /// <summary>
            /// The number of successful mutations
            /// </summary>
            public int Successes { get; set; }

            /// <summary>
            /// The total coverage increase
            /// </summary>
            public double TotalCoverageIncrease { get; set; }
        }
    }
}

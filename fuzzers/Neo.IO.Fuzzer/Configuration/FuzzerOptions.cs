// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzerOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.IO.Fuzzer.Configuration
{
    /// <summary>
    /// Options for configuring the fuzzer
    /// </summary>
    public class FuzzerOptions
    {
        [Option('t', "target-type", Required = true, HelpText = "Type of target to fuzz (SerializableSpan, Serializable, Composite, Differential, Stateful, Performance, Cache)")]
        public string TargetType { get; set; } = "SerializableSpan";

        /// <summary>
        /// Target class to fuzz (for SerializableSpan/Serializable) or comma-separated list of classes (for Composite) or strategy name (for Cache)
        /// </summary>
        [Option('c', "target-class", Required = false, HelpText = "Target class to fuzz (for SerializableSpan/Serializable), comma-separated list of classes (for Composite), or strategy name (for Cache: BasicCacheFuzzStrategy, KeyValueMutationStrategy, etc.)")]
        public string TargetClass { get; set; } = string.Empty;

        [Option('i', "iterations", Required = false, Default = 1000, HelpText = "Number of iterations to run")]
        public int Iterations { get; set; } = 1000;

        /// <summary>
        /// Maximum number of iterations to run (alias for Iterations for backward compatibility)
        /// </summary>
        public int MaxIterations => Iterations;

        [Option('s', "seed", Required = false, Default = 0, HelpText = "Random seed (0 for random)")]
        public int Seed { get; set; } = 0;

        [Option("seed-count", Required = false, Default = 100, HelpText = "Number of seed inputs to generate if corpus is empty")]
        public int SeedCount { get; set; } = 100;

        [Option("max-input-size", Required = false, Default = 10240, HelpText = "Maximum size of generated inputs in bytes")]
        public int MaxInputSize { get; set; } = 10240;

        [Option("max-mutations", Required = false, Default = 5, HelpText = "Maximum number of mutations to apply to an input")]
        public int MaxMutations { get; set; } = 5;

        [Option("corpus-selection-probability", Required = false, Default = 0.8, HelpText = "Probability of selecting an input from the corpus vs generating a new one")]
        public double CorpusSelectionProbability { get; set; } = 0.8;

        [Option("timeout", Required = false, Default = 5000, HelpText = "Timeout for test execution in milliseconds")]
        public int TimeoutMilliseconds { get; set; } = 5000;

        [Option("report-interval", Required = false, Default = 100, HelpText = "Interval for progress reporting")]
        public int ReportInterval { get; set; } = 100;

        [Option("corpus-dir", Required = false, HelpText = "Directory for storing corpus inputs")]
        public string CorpusDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "corpus");

        [Option("crash-dir", Required = false, HelpText = "Directory for storing crash inputs")]
        public string CrashDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "crashes");

        [Option("report-file", Required = false, HelpText = "File for detailed reporting")]
        public string ReportFile { get; set; } = string.Empty;

        // Enhanced coverage tracking options
        [Option("enhanced-coverage", Required = false, Default = false, HelpText = "Enable enhanced coverage tracking")]
        public bool EnableEnhancedCoverage { get; set; } = false;

        // Guided mutation options
        [Option("guided-mutation", Required = false, Default = false, HelpText = "Enable coverage-guided mutation")]
        public bool EnableGuidedMutation { get; set; } = false;

        // Neo.IO-specific mutator options
        [Option("neo-serialization-mutator", Required = false, Default = true, HelpText = "Enable Neo.IO-specific serialization mutator")]
        public bool EnableNeoSerializationMutator { get; set; } = true;

        /// <summary>
        /// Whether to use enhanced strategies for cache fuzzing
        /// </summary>
        [Option("use-enhanced", Required = false, Default = true, HelpText = "Whether to use enhanced strategies for cache fuzzing")]
        public bool UseEnhancedStrategies { get; set; } = true;

        // Differential fuzzing options
        [Option("diff-implementations", Required = false, HelpText = "Comma-separated list of implementation names for differential fuzzing")]
        public string DifferentialImplementations { get; set; } = string.Empty;

        // Stateful fuzzing options
        [Option("stateful-operations", Required = false, Default = 10, HelpText = "Maximum number of operations per test for stateful fuzzing")]
        public int MaxStatefulOperations { get; set; } = 10;

        [Option("stateful-reset", Required = false, Default = true, HelpText = "Reset state between tests for stateful fuzzing")]
        public bool ResetStateBetweenTests { get; set; } = true;

        // Performance fuzzing options
        [Option("perf-baseline-iterations", Required = false, Default = 100, HelpText = "Number of iterations to establish performance baseline")]
        public int PerformanceBaselineIterations { get; set; } = 100;

        [Option("perf-anomaly-threshold", Required = false, Default = 5.0, HelpText = "Threshold ratio for considering a measurement anomalous")]
        public double PerformanceAnomalyThreshold { get; set; } = 5.0;

        /// <summary>
        /// Validates the options
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(TargetType))
            {
                throw new ArgumentException("Target type is required");
            }

            if (Iterations <= 0)
            {
                throw new ArgumentException("Iterations must be greater than 0");
            }

            if (Seed == 0)
            {
                // Generate a random seed
                Seed = new Random().Next();
            }

            if (SeedCount <= 0)
            {
                throw new ArgumentException("Seed count must be greater than 0");
            }

            if (MaxInputSize <= 0)
            {
                throw new ArgumentException("Maximum input size must be greater than 0");
            }

            if (MaxMutations <= 0)
            {
                throw new ArgumentException("Maximum mutations must be greater than 0");
            }

            if (TimeoutMilliseconds <= 0)
            {
                throw new ArgumentException("Timeout must be greater than 0");
            }

            if (ReportInterval <= 0)
            {
                throw new ArgumentException("Report interval must be greater than 0");
            }

            // Validate enhanced options
            if (MaxStatefulOperations <= 0)
            {
                throw new ArgumentException("Maximum stateful operations must be greater than 0");
            }

            if (PerformanceBaselineIterations <= 0)
            {
                throw new ArgumentException("Performance baseline iterations must be greater than 0");
            }

            if (PerformanceAnomalyThreshold <= 0)
            {
                throw new ArgumentException("Performance anomaly threshold must be greater than 0");
            }
        }
    }

    /// <summary>
    /// Types of targets that can be fuzzed
    /// </summary>
    public enum TargetType
    {
        SerializableSpan,
        MemoryReader,
        Serializable,
        RandomSerialization,
        Composite,
        Differential,
        Stateful,
        Performance
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using CommandLine;
using Neo.IO;
using Neo.IO.Fuzzer.Configuration;
using Neo.IO.Fuzzer.Generators;
using Neo.IO.Fuzzer.Mutation;
using Neo.IO.Fuzzer.Reporting;
using Neo.IO.Fuzzer.Targets;
using Neo.IO.Fuzzer.TestHarness;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO.Fuzzer
{
    /// <summary>
    /// Main entry point for the Neo.IO.Fuzzer
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                // Parse command line arguments
                var result = Parser.Default.ParseArguments<FuzzerOptions>(args);

                return await result.MapResult(
                    async options => await RunFuzzerAsync(options),
                    errors => Task.FromResult(1));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        /// <summary>
        /// Runs the fuzzer with the specified options
        /// </summary>
        /// <param name="options">The fuzzer options</param>
        /// <returns>The exit code</returns>
        private static async Task<int> RunFuzzerAsync(FuzzerOptions options)
        {
            try
            {
                // Create directories if they don't exist
                Directory.CreateDirectory(options.CorpusDirectory);
                Directory.CreateDirectory(options.CrashDirectory);

                // Initialize the random number generator
                var random = new Random(options.Seed);

                // Initialize the target
                IFuzzTarget? target = FuzzerEngine.CreateTarget(options);
                if (target == null)
                {
                    Console.Error.WriteLine($"Error: Failed to create target of type {options.TargetType}");
                    return 1;
                }

                // Initialize the generator
                IBinaryGenerator generator = new StructuredBinaryGenerator(random);

                // Initialize the reporters
                var reporters = new List<IReporter>
                {
                    new ConsoleReporter()
                };

                if (!string.IsNullOrEmpty(options.ReportFile))
                {
                    reporters.Add(new FileReporter(options.ReportFile));
                }

                // Initialize the coverage tracker
                CoverageTracker coverageTracker;
                if (options.EnableEnhancedCoverage)
                {
                    Console.WriteLine("Using enhanced coverage tracking");
                    coverageTracker = new EnhancedCoverageTracker();
                }
                else
                {
                    coverageTracker = new CoverageTracker();
                }

                // Initialize the mutation engine
                MutationEngine mutationEngine;
                if (options.EnableGuidedMutation)
                {
                    Console.WriteLine("Using guided mutation engine");
                    if (coverageTracker is EnhancedCoverageTracker enhancedTracker)
                    {
                        mutationEngine = GuidedMutationEngine.Create(random, enhancedTracker);
                    }
                    else
                    {
                        // Fall back to regular mutation engine if enhanced coverage tracking is not enabled
                        Console.WriteLine("Warning: Guided mutation requires enhanced coverage tracking. Using regular mutation engine instead.");
                        mutationEngine = new MutationEngine(random);
                    }
                }
                else
                {
                    mutationEngine = new MutationEngine(random);
                }

                // Add Neo.IO-specific mutator if enabled
                if (options.EnableNeoSerializationMutator)
                {
                    Console.WriteLine("Adding Neo.IO-specific serialization mutator");
                    mutationEngine.AddMutator(new NeoSerializationMutator());
                }

                // Initialize the fuzzer engine
                var engine = new FuzzerEngine(
                    options,
                    target,
                    generator,
                    reporters.ToArray(),
                    coverageTracker,
                    mutationEngine);

                // Start the fuzzer
                await engine.StartAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        /// <summary>
        /// Finds the Neo.dll assembly in the specified directory or its parent directories
        /// </summary>
        /// <param name="startDir">The directory to start searching from</param>
        /// <returns>The path to the Neo.dll assembly, or null if not found</returns>
        private static string FindNeoAssembly(string startDir)
        {
            var dir = new DirectoryInfo(startDir);

            while (dir != null)
            {
                var neoPath = Path.Combine(dir.FullName, "Neo.dll");
                if (File.Exists(neoPath))
                {
                    return neoPath;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }

    /// <summary>
    /// Simple state class for stateful fuzzing
    /// </summary>
    public class StatefulFuzzerState
    {
        public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzerEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.Configuration;
using Neo.IO.Fuzzer.Generators;
using Neo.IO.Fuzzer.Mutation;
using Neo.IO.Fuzzer.Reporting;
using Neo.IO.Fuzzer.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO.Fuzzer.TestHarness
{
    /// <summary>
    /// Main engine for fuzzing Neo.IO components
    /// </summary>
    public class FuzzerEngine
    {
        private readonly FuzzerOptions _options;
        private readonly IFuzzTarget _target;
        private readonly IBinaryGenerator _generator;
        private readonly List<IMutator> _mutators;
        private readonly IMutationEngine _mutationEngine;
        private readonly CoverageTracker _coverageTracker;
        private readonly CorpusManager _corpusManager;
        private readonly TestExecutor _testExecutor;
        private readonly IReporter[] _reporters;
        private readonly Random _random;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private int _testsExecuted;
        private int _interestingInputs;
        private long _totalExecutionTime;
        private long _totalInputSize;

        /// <summary>
        /// Initializes a new instance of the FuzzerEngine class
        /// </summary>
        /// <param name="options">The fuzzer options</param>
        /// <param name="target">The target to fuzz</param>
        /// <param name="generator">The binary generator</param>
        /// <param name="reporters">The reporters to use</param>
        /// <param name="coverageTracker">Optional coverage tracker</param>
        /// <param name="mutationEngine">Optional mutation engine</param>
        public FuzzerEngine(
            FuzzerOptions options,
            IFuzzTarget target,
            IBinaryGenerator generator,
            IReporter[] reporters,
            CoverageTracker? coverageTracker = null,
            IMutationEngine? mutationEngine = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _reporters = reporters ?? throw new ArgumentNullException(nameof(reporters));

            // Initialize the random number generator
            _random = new Random(options.Seed);

            // Initialize the coverage tracker
            _coverageTracker = coverageTracker ?? (options.EnableEnhancedCoverage
                ? new EnhancedCoverageTracker()
                : new CoverageTracker());

            // Initialize the mutation engine
            if (mutationEngine != null)
            {
                _mutationEngine = mutationEngine;
            }
            else if (options.EnableGuidedMutation)
            {
                var enhancedTracker = _coverageTracker as EnhancedCoverageTracker;
                _mutationEngine = GuidedMutationEngine.Create(_random, enhancedTracker);
            }
            else
            {
                _mutationEngine = MutationEngine.CreateWithDefaultMutators(_random);
            }

            // Initialize the mutators list (for backward compatibility)
            _mutators = new List<IMutator>(_mutationEngine.GetMutators());

            // Initialize the corpus manager
            _corpusManager = new CorpusManager(options.CorpusDirectory, options.CrashDirectory, _coverageTracker);

            // Initialize the test executor
            _testExecutor = new TestExecutor(_target, options.TimeoutMilliseconds);

            // Initialize the cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the fuzzing process
        /// </summary>
        /// <returns>A task representing the fuzzing process</returns>
        public async Task StartAsync()
        {
            try
            {
                Console.WriteLine($"Starting fuzzing with target: {_target.GetType().Name}");
                Console.WriteLine($"Seed: {_options.Seed}");
                Console.WriteLine($"Max iterations: {_options.MaxIterations}");
                Console.WriteLine($"Timeout: {_options.TimeoutMilliseconds}ms");

                // Initialize the corpus
                await InitializeCorpusAsync();

                // For performance fuzzing, establish baseline first
                if (_target is PerformanceFuzzTarget performanceTarget)
                {
                    Console.WriteLine("Establishing performance baseline...");
                    await performanceTarget.EstablishBaselineAsync(_random);
                    Console.WriteLine("Performance baseline established.");
                }

                // Start the fuzzing loop
                await RunFuzzingLoopAsync();

                // Report final statistics
                ReportStatistics();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in fuzzing process: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                _cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Initializes the corpus
        /// </summary>
        /// <returns>A task representing the initialization process</returns>
        private async Task InitializeCorpusAsync()
        {
            // Load the corpus
            await _corpusManager.LoadCorpusAsync();

            // If the corpus is empty, generate seed inputs
            if (_corpusManager.CorpusCount == 0)
            {
                Console.WriteLine($"Generating {_options.SeedCount} seed inputs...");

                for (int i = 0; i < _options.SeedCount; i++)
                {
                    // Generate a random input
                    byte[] data = _generator.Generate(_random, _random.Next(1, _options.MaxInputSize));

                    // Execute the target with the input
                    var result = await _testExecutor.ExecuteTestAsync(data);

                    // If the execution was successful, add the input to the corpus
                    if (result.Outcome == TestOutcome.Success)
                    {
                        if (result.Coverage is HashSet<string> coveragePoints)
                        {
                            _corpusManager.AddToCorpus(data, coveragePoints);
                        }
                    }

                    // Report progress
                    if (i % 10 == 0)
                    {
                        Console.Write(".");
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Generated {_corpusManager.CorpusCount} seed inputs.");
            }
            else
            {
                Console.WriteLine($"Loaded {_corpusManager.CorpusCount} inputs from corpus.");
            }
        }

        /// <summary>
        /// Runs the fuzzing loop
        /// </summary>
        /// <returns>A task representing the fuzzing loop</returns>
        private async Task RunFuzzingLoopAsync()
        {
            Console.WriteLine("Starting fuzzing loop...");

            // Start the stopwatch
            var stopwatch = Stopwatch.StartNew();

            // Run the fuzzing loop
            for (int i = 0; i < _options.MaxIterations; i++)
            {
                // Check if cancellation was requested
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                // Generate an input for testing
                byte[] data = GenerateInput();

                // Reset state for stateful targets if configured
                if (_target is IStatefulFuzzTarget statefulTarget && _options.ResetStateBetweenTests)
                {
                    statefulTarget.ResetState();
                }

                // Execute the target with the input
                var result = await _testExecutor.ExecuteTestAsync(data);

                // Update statistics
                _testsExecuted++;
                _totalExecutionTime += result.ExecutionTime;
                _totalInputSize += data.Length;

                // If the test crashed, add it to the crashes directory
                if (result.Outcome == TestOutcome.Exception || result.Outcome == TestOutcome.Timeout)
                {
                    _corpusManager.AddToCrashes(data, result.Exception?.ToString() ?? "Unknown error");
                }

                // If the test produced interesting coverage, add the input to the corpus
                if (result.IsInteresting && result.Coverage is HashSet<string> coveragePoints)
                {
                    _corpusManager.AddToCorpus(data, coveragePoints);
                    _interestingInputs++;

                    // If using guided mutation, update the mutation engine
                    if (_mutationEngine is GuidedMutationEngine guidedEngine)
                    {
                        // Record the success with the last used mutator and a default coverage increase of 1.0
                        var lastMutator = guidedEngine.GetMutators().FirstOrDefault();
                        if (lastMutator != null)
                        {
                            guidedEngine.RecordSuccess(lastMutator, 1.0);
                        }
                    }
                }

                // Report progress periodically
                if (i % _options.ReportInterval == 0)
                {
                    ReportProgress(i, stopwatch.ElapsedMilliseconds);
                }
            }

            // Stop the stopwatch
            stopwatch.Stop();

            Console.WriteLine("Fuzzing loop completed.");
        }

        /// <summary>
        /// Generates an input for testing
        /// </summary>
        /// <returns>The generated input</returns>
        private byte[] GenerateInput()
        {
            // Decide whether to use the corpus or generate a new input
            if (_corpusManager.CorpusCount > 0 && _random.NextDouble() < _options.CorpusSelectionProbability)
            {
                // Select an input from the corpus
                byte[] input = _corpusManager.GetRandomCorpusInput(_random);

                // Determine how many mutations to apply
                int mutationCount = _random.Next(1, _options.MaxMutations + 1);

                // Apply mutations
                byte[] mutated = input;
                for (int i = 0; i < mutationCount; i++)
                {
                    mutated = _mutationEngine.Mutate(mutated);
                }

                return mutated;
            }
            else
            {
                // Generate a new input
                int size = _random.Next(1, _options.MaxInputSize);
                return _generator.Generate(_random, size);
            }
        }

        /// <summary>
        /// Reports progress during the fuzzing process
        /// </summary>
        /// <param name="iteration">The current iteration</param>
        /// <param name="elapsedMilliseconds">The elapsed time in milliseconds</param>
        private void ReportProgress(int iteration, long elapsedMilliseconds)
        {
            // Calculate statistics
            double testsPerSecond = _testsExecuted / (elapsedMilliseconds / 1000.0);
            double averageExecutionTime = _testsExecuted > 0 ? _totalExecutionTime / (double)_testsExecuted : 0;
            double averageInputSize = _testsExecuted > 0 ? _totalInputSize / (double)_testsExecuted : 0;

            // Report progress
            foreach (var reporter in _reporters)
            {
                reporter.ReportProgress(
                    iteration,
                    _options.MaxIterations,
                    _testsExecuted,
                    _interestingInputs,
                    _corpusManager.CorpusCount,
                    _corpusManager.CrashCount,
                    testsPerSecond,
                    averageExecutionTime,
                    averageInputSize,
                    _coverageTracker.GetCoverageCount());
            }
        }

        /// <summary>
        /// Reports a crash
        /// </summary>
        /// <param name="data">The input data that caused the crash</param>
        /// <param name="exception">The exception that was thrown</param>
        private void ReportCrash(byte[] data, Exception exception)
        {
            foreach (var reporter in _reporters)
            {
                reporter.ReportCrash(data, exception);
            }
        }

        /// <summary>
        /// Reports final statistics
        /// </summary>
        private void ReportStatistics()
        {
            foreach (var reporter in _reporters)
            {
                reporter.ReportStatistics(
                    _testsExecuted,
                    _interestingInputs,
                    _corpusManager.CorpusCount,
                    _corpusManager.CrashCount,
                    _coverageTracker.GetCoverageCount());
            }
        }

        /// <summary>
        /// Resets the fuzzer state
        /// </summary>
        public void Reset()
        {
            _testsExecuted = 0;
            _interestingInputs = 0;
            _totalExecutionTime = 0;
            _totalInputSize = 0;

            // Reset the coverage tracker
            _coverageTracker.Reset();

            // Reset the mutation engine if it's a guided engine
            if (_mutationEngine is GuidedMutationEngine guidedEngine)
            {
                guidedEngine.ResetStats();
            }
        }

        /// <summary>
        /// Creates a target based on the fuzzer options
        /// </summary>
        /// <param name="options">The fuzzer options</param>
        /// <returns>The created target, or null if no valid target could be created</returns>
        public static IFuzzTarget? CreateTarget(FuzzerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            switch (options.TargetType.ToLowerInvariant())
            {
                case "serializablespan":
                    // Create a serializable span target
                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // If no target class is specified, use our default implementation
                        return new SerializableSpanTarget();
                    }

                    Type? targetType = Type.GetType(options.TargetClass);
                    if (targetType != null)
                    {
                        return new SerializableSpanTarget(targetType);
                    }
                    return null;

                case "serializable":
                    // Create a serializable target
                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // If no target class is specified, use our default implementation
                        return new SerializableTarget();
                    }

                    Type? serializableTargetType = Type.GetType(options.TargetClass);
                    if (serializableTargetType != null)
                    {
                        return new SerializableTarget(serializableTargetType);
                    }
                    return null;

                case "composite":
                    // Create a composite target with multiple targets
                    var targets = new List<IFuzzTarget>();

                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // If no target classes are specified, use our default implementations
                        targets.Add(new SerializableSpanTarget());
                        targets.Add(new SerializableTarget());
                    }
                    else
                    {
                        var targetClasses = options.TargetClass.Split(',');

                        foreach (var className in targetClasses)
                        {
                            var type = Type.GetType(className.Trim());
                            if (type != null)
                            {
                                targets.Add(new SerializableSpanTarget(type));
                            }
                        }
                    }

                    return targets.Count > 0 ? new CompositeFuzzTarget(targets.ToArray()) : null;

                case "differential":
                    // Create a TestDifferentialFuzzTarget instead of the generic DifferentialFuzzTarget
                    var differentialTarget = new TestDifferentialFuzzTarget("DifferentialTarget");

                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // Add default implementations to the differential target

                        // Add ISerializableSpan implementations
                        differentialTarget.AddSerializableSpanImplementation("TestSerializableSpan1", new TestSerializableSpan());
                        differentialTarget.AddSerializableSpanImplementation("TestSerializableSpan2", new TestSerializableSpan());

                        // Add ISerializable implementations
                        differentialTarget.AddSerializableImplementation("TestSerializable1", new TestSerializable());
                        differentialTarget.AddSerializableImplementation("TestSerializable2", new TestSerializable());
                    }
                    else
                    {
                        // Parse the target class string to get multiple implementation types
                        var implementationClasses = options.TargetClass.Split(',');

                        int serializableSpanCount = 0;
                        int serializableCount = 0;

                        foreach (var ic in implementationClasses)
                        {
                            var parts = ic.Trim().Split(':');
                            if (parts.Length != 2)
                                throw new ArgumentException($"Invalid implementation class format: {ic}. Expected format: <name>:<class>");

                            var name = parts[0].Trim();
                            var className = parts[1].Trim();

                            var implementationType = Type.GetType(className);
                            if (implementationType == null)
                                throw new ArgumentException($"Type {className} not found");

                            var implementation = Activator.CreateInstance(implementationType);
                            if (implementation == null)
                                throw new ArgumentException($"Failed to create instance of {className}");

                            if (implementation is ISerializableSpan serializableSpan)
                            {
                                differentialTarget.AddSerializableSpanImplementation(name, serializableSpan);
                                serializableSpanCount++;
                            }
                            else if (implementation is ISerializable serializable)
                            {
                                differentialTarget.AddSerializableImplementation(name, serializable);
                                serializableCount++;
                            }
                            else
                            {
                                throw new ArgumentException($"Type {className} does not implement ISerializableSpan or ISerializable");
                            }
                        }

                        // Ensure we have at least two implementations of the same type
                        if (serializableSpanCount < 2 && serializableCount < 2)
                        {
                            throw new ArgumentException("At least two implementations of the same type (ISerializableSpan or ISerializable) are required for differential fuzzing");
                        }
                    }

                    return differentialTarget;

                case "stateful":
                    // Create a stateful target
                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // If no target class is specified, use our default implementation
                        return new StatefulFuzzTarget<object>("DefaultStatefulTarget");
                    }

                    return new StatefulFuzzTarget<object>(options.TargetClass);

                case "performance":
                    // Create a performance target
                    PerformanceFuzzTarget.OperationHandler handler = (data) => { /* Default empty handler */ };

                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // If no target class is specified, use a default name
                        return new PerformanceFuzzTarget(
                            "DefaultPerformanceTarget",
                            handler,
                            options.PerformanceBaselineIterations,
                            options.PerformanceAnomalyThreshold);
                    }

                    return new PerformanceFuzzTarget(
                        options.TargetClass,
                        handler,
                        options.PerformanceBaselineIterations,
                        options.PerformanceAnomalyThreshold);

                case "cache":
                    if (string.IsNullOrEmpty(options.TargetClass))
                    {
                        // Use the basic cache fuzzing target with default strategy
                        return new CacheFuzzTarget("BasicCacheFuzzing", options.UseEnhancedStrategies);
                    }
                    else
                    {
                        // Parse the strategy name
                        string strategyName = options.TargetClass;
                        bool useEnhanced = options.UseEnhancedStrategies;

                        // Check if the strategy name includes a specific type indicator
                        if (strategyName.Contains(":"))
                        {
                            string[] parts = strategyName.Split(':');
                            if (parts.Length == 2)
                            {
                                strategyName = parts[0];
                                useEnhanced = parts[1].Equals("enhanced", StringComparison.OrdinalIgnoreCase);
                            }
                        }

                        return new CacheFuzzTarget(strategyName, useEnhanced);
                    }

                default:
                    throw new ArgumentException($"Unknown target type: {options.TargetType}");
            }
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// CompositeCacheFuzzStrategy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Defines the types of composite strategy execution modes.
    /// </summary>
    public enum CompositeExecutionMode
    {
        /// <summary>
        /// Execute strategies sequentially.
        /// </summary>
        Sequential,

        /// <summary>
        /// Execute strategies in parallel.
        /// </summary>
        Parallel,

        /// <summary>
        /// Execute strategies with weighted distribution.
        /// </summary>
        Weighted,

        /// <summary>
        /// Execute strategies with adaptive selection based on previous results.
        /// </summary>
        Adaptive
    }

    /// <summary>
    /// A composite strategy that combines multiple cache fuzzing strategies.
    /// This allows for more comprehensive testing by running multiple strategies in sequence.
    /// Uses the common utility classes to reduce code duplication.
    /// </summary>
    public class CompositeCacheFuzzStrategy : ICacheFuzzStrategy, IEnhancedCacheFuzzStrategy
    {
        private readonly List<ICacheFuzzStrategy> _strategies = new List<ICacheFuzzStrategy>();
        private readonly CoverageTrackerHelper _coverage;
        private readonly string _name = "CompositeCacheFuzzStrategy";
        private readonly Dictionary<string, int> _strategyWeights = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _strategySuccesses = new Dictionary<string, int>();
        private readonly Dictionary<string, object> strategySpecificCoverage = new Dictionary<string, object>();
        private CompositeExecutionMode _executionMode = CompositeExecutionMode.Sequential;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeCacheFuzzStrategy"/> class with default enhanced strategies.
        /// </summary>
        public CompositeCacheFuzzStrategy()
        {
            // Add all available enhanced strategies by default
            _strategies.Add(new BasicCacheFuzzStrategy());
            _strategies.Add(new CapacityFuzzStrategy());
            _strategies.Add(new ConcurrencyFuzzStrategy());
            _strategies.Add(new KeyValueMutationStrategy());
            _strategies.Add(new StateTrackingFuzzStrategy());

            // Initialize coverage tracking
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
            InitializeWeights();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeCacheFuzzStrategy"/> class with specified strategies.
        /// </summary>
        /// <param name="strategies">The strategies to include in the composite.</param>
        public CompositeCacheFuzzStrategy(IEnumerable<ICacheFuzzStrategy> strategies)
        {
            if (strategies == null)
                throw new ArgumentNullException(nameof(strategies));

            _strategies.AddRange(strategies);

            if (_strategies.Count == 0)
            {
                // Add at least the basic strategy if none were provided
                _strategies.Add(new BasicCacheFuzzStrategy());
            }

            // Initialize coverage tracking
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
            InitializeWeights();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeCacheFuzzStrategy"/> class with specified strategies and execution mode.
        /// </summary>
        /// <param name="strategies">The strategies to include in the composite.</param>
        /// <param name="executionMode">The execution mode to use.</param>
        public CompositeCacheFuzzStrategy(IEnumerable<ICacheFuzzStrategy> strategies, CompositeExecutionMode executionMode)
            : this(strategies)
        {
            _executionMode = executionMode;
        }

        private void InitializeCoverage()
        {
            // Initialize basic coverage points
            _coverage.InitializePoints(
                "TotalExecutions",
                "SuccessfulExecutions",
                "FailedExecutions",
                "ParallelExecutions",
                "SequentialExecutions",
                "WeightedExecutions",
                "AdaptiveExecutions",
                "SuccessfulRetry",
                "FailedRetry",
                "TotalPoints"
            );

            // Track which strategies were executed
            foreach (var strategy in _strategies)
            {
                _coverage.InitializePoints(
                    $"Strategy_{strategy.GetName()}_Executed",
                    $"Strategy_{strategy.GetName()}_Succeeded",
                    $"Strategy_{strategy.GetName()}_Failed",
                    $"Strategy_{strategy.GetName()}_Weight"
                );
            }
        }

        private void InitializeWeights()
        {
            // Initialize default weights for strategies
            foreach (var strategy in _strategies)
            {
                string name = strategy.GetName();
                _strategyWeights[name] = 1; // Default equal weight
                _strategySuccesses[name] = 0; // No successes yet
            }
        }

        /// <summary>
        /// Gets the coverage tracker used by this strategy.
        /// </summary>
        /// <returns>The coverage tracker.</returns>
        public CoverageTrackerHelper GetCoverageTracker()
        {
            return _coverage;
        }

        /// <summary>
        /// Executes the composite strategy with the provided input data.
        /// </summary>
        /// <param name="input">The input data to use for fuzzing.</param>
        /// <returns>True if at least one strategy executed successfully, false otherwise.</returns>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length < 2)
                return false;

            bool result = ExceptionHandlingUtilities.ExecuteWithExceptionHandling(
                () => ExecuteInternal(input),
                "CompositeCacheFuzzStrategy.Execute",
                _coverage);

            // Report coverage percentage
            double coveragePercent = _coverage.GetCoveragePercentage();
            Console.WriteLine($"Composite Strategy Coverage: {coveragePercent:F2}%");

            return result;
        }

        private bool ExecuteInternal(byte[] input)
        {
            bool anySuccess = false;
            _coverage.IncrementPoint("TotalExecutions");

            // Extract execution mode from input if available
            if (input.Length > 0)
            {
                _executionMode = InputProcessingUtilities.ExtractEnumParameter<CompositeExecutionMode>(input, 0);
            }

            // Track which execution mode was used
            _coverage.IncrementPoint($"{_executionMode}Executions");

            // Divide the input among the strategies
            var chunks = InputProcessingUtilities.DivideInput(input, _strategies.Count);

            // Execute strategies based on execution mode
            switch (_executionMode)
            {
                case CompositeExecutionMode.Sequential:
                    anySuccess = ExecuteSequentially(chunks);
                    break;

                case CompositeExecutionMode.Parallel:
                    anySuccess = ExecuteInParallel(chunks);
                    break;

                case CompositeExecutionMode.Weighted:
                    anySuccess = ExecuteWeighted(chunks);
                    break;

                case CompositeExecutionMode.Adaptive:
                    anySuccess = ExecuteAdaptive(chunks);
                    break;
            }

            if (anySuccess)
            {
                _coverage.IncrementPoint("SuccessfulExecutions");
            }
            else
            {
                _coverage.IncrementPoint("FailedExecutions");
            }

            return anySuccess;
        }

        private bool ExecuteSequentially(List<byte[]> chunks)
        {
            bool anySuccess = false;

            // Execute each strategy with its chunk of the input
            for (int i = 0; i < _strategies.Count && i < chunks.Count; i++)
            {
                var strategy = _strategies[i];
                var chunk = chunks[i];

                bool success = ExecuteStrategy(strategy, chunk);
                if (success)
                {
                    anySuccess = true;
                }
            }

            return anySuccess;
        }

        private bool ExecuteInParallel(List<byte[]> chunks)
        {
            bool anySuccess = false;
            var tasks = new List<Task<bool>>();

            // Create tasks for each strategy
            for (int i = 0; i < _strategies.Count && i < chunks.Count; i++)
            {
                var strategy = _strategies[i];
                var chunk = chunks[i];

                tasks.Add(Task.Run(() => ExecuteStrategy(strategy, chunk)));
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks.ToArray());

            // Check results
            foreach (var task in tasks)
            {
                if (task.Result)
                {
                    anySuccess = true;
                }
            }

            return anySuccess;
        }

        private bool ExecuteWeighted(List<byte[]> chunks)
        {
            bool anySuccess = false;

            // Calculate total weight
            int totalWeight = _strategyWeights.Values.Sum();
            if (totalWeight == 0) totalWeight = _strategies.Count; // Fallback if all weights are 0

            // Execute strategies based on their weight
            for (int i = 0; i < _strategies.Count && i < chunks.Count; i++)
            {
                var strategy = _strategies[i];
                var chunk = chunks[i];
                string name = strategy.GetName();

                // Get weight for this strategy (default to 1 if not found)
                int weight = _strategyWeights.TryGetValue(name, out int w) ? w : 1;

                // Calculate how many times to execute based on weight
                int executions = Math.Max(1, (int)Math.Round((double)weight / totalWeight * 5));

                // Track weight in coverage
                _coverage.IncrementPoint($"Strategy_{name}_Weight", weight);

                // Execute the strategy multiple times based on weight
                bool strategySuccess = false;
                for (int j = 0; j < executions; j++)
                {
                    bool success = ExecuteStrategy(strategy, chunk);
                    if (success)
                    {
                        strategySuccess = true;
                    }
                }

                if (strategySuccess)
                {
                    anySuccess = true;
                }
            }

            return anySuccess;
        }

        private bool ExecuteAdaptive(List<byte[]> chunks)
        {
            bool anySuccess = false;

            // Execute strategies based on their success rate
            for (int i = 0; i < _strategies.Count && i < chunks.Count; i++)
            {
                var strategy = _strategies[i];
                var chunk = chunks[i];
                string name = strategy.GetName();

                // Get success count for this strategy
                int successCount = _strategySuccesses.TryGetValue(name, out int count) ? count : 0;

                // Calculate how many times to execute based on success rate
                // Strategies with more successes get more executions
                int executions = Math.Max(1, Math.Min(5, successCount + 1));

                // Execute the strategy multiple times based on success rate
                bool strategySuccess = false;
                for (int j = 0; j < executions; j++)
                {
                    bool success = ExecuteStrategy(strategy, chunk);
                    if (success)
                    {
                        strategySuccess = true;
                    }
                }

                // Update success count for adaptive learning
                if (strategySuccess)
                {
                    _strategySuccesses[name] = successCount + 1;
                    anySuccess = true;
                }
            }

            return anySuccess;
        }

        private bool ExecuteStrategy(ICacheFuzzStrategy strategy, byte[] chunk)
        {
            string strategyName = strategy.GetName();
            _coverage.IncrementPoint($"Strategy_{strategyName}_Executed");

            bool success = ExceptionHandlingUtilities.ExecuteWithRetry(
                () => strategy.Execute(chunk),
                $"CompositeStrategy_{strategyName}",
                3,
                100);

            if (success)
            {
                _coverage.IncrementPoint($"Strategy_{strategyName}_Succeeded");

                // If it's an enhanced strategy, collect its coverage data
                if (strategy is IEnhancedCacheFuzzStrategy enhancedStrategy)
                {
                    var tracker = enhancedStrategy.GetCoverageTracker();
                    if (tracker != null)
                    {
                        strategySpecificCoverage.Add(strategy.GetName(), tracker.GetCoverage());
                    }
                }
            }
            else
            {
                _coverage.IncrementPoint($"Strategy_{strategyName}_Failed");
            }

            return success;
        }

        /// <summary>
        /// Gets the coverage information for the composite strategy.
        /// </summary>
        /// <returns>An object containing coverage information from all strategies.</returns>
        public object GetCoverage()
        {
            // Combine coverage from all strategies
            var combinedCoverage = new Dictionary<string, object>(_coverage.GetCoverageObject() as Dictionary<string, object>);

            foreach (var strategy in _strategies)
            {
                var strategyCoverage = strategy.GetCoverage();

                if (strategyCoverage is Dictionary<string, int> strategyDict)
                {
                    foreach (var kvp in strategyDict)
                    {
                        combinedCoverage[$"{strategy.GetName()}_{kvp.Key}"] = kvp.Value;
                    }
                }
                else if (strategyCoverage is Dictionary<string, object> strategyObjDict)
                {
                    foreach (var kvp in strategyObjDict)
                    {
                        combinedCoverage[$"{strategy.GetName()}_{kvp.Key}"] = kvp.Value;
                    }
                }
                else
                {
                    // Just add the whole object if it's not a dictionary
                    combinedCoverage[$"{strategy.GetName()}_Coverage"] = strategyCoverage;
                }
            }

            // Calculate total coverage points
            int totalPoints = combinedCoverage.Count(kvp =>
                (kvp.Value is int intValue && intValue > 0) &&
                !kvp.Key.EndsWith("_Executed") &&
                !kvp.Key.EndsWith("_Succeeded") &&
                !kvp.Key.EndsWith("_Failed") &&
                !kvp.Key.EndsWith("_Weight"));

            combinedCoverage["TotalPoints"] = totalPoints;

            return combinedCoverage;
        }

        /// <summary>
        /// Gets the name of the composite strategy.
        /// </summary>
        /// <returns>The strategy name.</returns>
        public string GetName()
        {
            return _name;
        }

        /// <summary>
        /// Adds a strategy to the composite.
        /// </summary>
        /// <param name="strategy">The strategy to add.</param>
        public void AddStrategy(ICacheFuzzStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            _strategies.Add(strategy);

            // Update coverage tracking for the new strategy
            _coverage.InitializePoints(
                $"Strategy_{strategy.GetName()}_Executed",
                $"Strategy_{strategy.GetName()}_Succeeded",
                $"Strategy_{strategy.GetName()}_Failed",
                $"Strategy_{strategy.GetName()}_Weight"
            );

            // Initialize weight for the new strategy
            string name = strategy.GetName();
            _strategyWeights[name] = 1; // Default equal weight
            _strategySuccesses[name] = 0; // No successes yet
        }

        /// <summary>
        /// Sets the weight for a specific strategy.
        /// </summary>
        /// <param name="strategyName">The name of the strategy.</param>
        /// <param name="weight">The weight to assign (0 or greater).</param>
        public void SetStrategyWeight(string strategyName, int weight)
        {
            if (string.IsNullOrEmpty(strategyName))
                throw new ArgumentNullException(nameof(strategyName));

            if (weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be 0 or greater");

            // Check if the strategy exists
            if (_strategies.Any(s => s.GetName() == strategyName))
            {
                _strategyWeights[strategyName] = weight;
            }
        }

        /// <summary>
        /// Sets the execution mode for the composite strategy.
        /// </summary>
        /// <param name="mode">The execution mode to use.</param>
        public void SetExecutionMode(CompositeExecutionMode mode)
        {
            _executionMode = mode;
        }

        /// <summary>
        /// Gets the strategies in the composite.
        /// </summary>
        /// <returns>The list of strategies.</returns>
        public IReadOnlyList<ICacheFuzzStrategy> GetStrategies()
        {
            return _strategies.AsReadOnly();
        }
    }
}

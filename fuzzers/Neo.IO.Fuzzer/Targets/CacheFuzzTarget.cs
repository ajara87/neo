// Copyright (C) 2015-2025 The Neo Project.
//
// CacheFuzzTarget.cs file belongs to the neo project and is free
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

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// A fuzzing target for testing the Neo caching system.
    /// Supports both original and enhanced strategies.
    /// </summary>
    public class CacheFuzzTarget : IFuzzTarget
    {
        private readonly ICacheFuzzStrategy _strategy;
        private readonly Dictionary<string, object> _coverage = new Dictionary<string, object>();
        private readonly string _name;
        private readonly bool _useEnhancedStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheFuzzTarget"/> class with the default strategy.
        /// </summary>
        /// <param name="name">The name of the target.</param>
        /// <param name="useEnhancedStrategy">Whether to use enhanced strategies.</param>
        public CacheFuzzTarget(string name, bool useEnhancedStrategy = true)
        {
            _name = name;
            _useEnhancedStrategy = useEnhancedStrategy;
            _strategy = CreateStrategy("Basic", _useEnhancedStrategy);
            InitializeCoverage();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheFuzzTarget"/> class with a specific strategy.
        /// </summary>
        /// <param name="name">The name of the target.</param>
        /// <param name="strategyName">The name of the strategy to use.</param>
        /// <param name="useEnhancedStrategy">Whether to use enhanced strategies.</param>
        public CacheFuzzTarget(string name, string strategyName, bool useEnhancedStrategy = true)
        {
            _name = name;
            _useEnhancedStrategy = useEnhancedStrategy;
            _strategy = CreateStrategy(strategyName, _useEnhancedStrategy);
            InitializeCoverage();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheFuzzTarget"/> class with a specific strategy instance.
        /// </summary>
        /// <param name="name">The name of the target.</param>
        /// <param name="strategy">The fuzzing strategy to use.</param>
        public CacheFuzzTarget(string name, ICacheFuzzStrategy strategy)
        {
            _name = name;
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _useEnhancedStrategy = strategy is IEnhancedCacheFuzzStrategy;
            InitializeCoverage();
        }

        private void InitializeCoverage()
        {
            _coverage["StrategyName"] = _strategy.GetName();
            _coverage["StrategyType"] = _strategy.GetType().Name;
            _coverage["IsEnhanced"] = _useEnhancedStrategy;
            _coverage["SuccessfulExecutions"] = 0;
            _coverage["FailedExecutions"] = 0;
            _coverage["TotalExecutions"] = 0;
        }

        /// <summary>
        /// Executes the fuzzing target with the provided input.
        /// </summary>
        /// <param name="input">The input data for fuzzing.</param>
        /// <returns>True if the execution was successful, false otherwise.</returns>
        public bool Execute(byte[] input)
        {
            if (input == null)
                return false;

            try
            {
                bool result = _strategy.Execute(input);

                // Update coverage statistics
                _coverage["TotalExecutions"] = (int)_coverage["TotalExecutions"] + 1;

                if (result)
                {
                    _coverage["SuccessfulExecutions"] = (int)_coverage["SuccessfulExecutions"] + 1;
                }
                else
                {
                    _coverage["FailedExecutions"] = (int)_coverage["FailedExecutions"] + 1;
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the fuzzing process
                Console.WriteLine($"Exception in CacheFuzzTarget: {ex.Message}");
                _coverage["TotalExecutions"] = (int)_coverage["TotalExecutions"] + 1;
                _coverage["FailedExecutions"] = (int)_coverage["FailedExecutions"] + 1;
                _coverage["LastException"] = ex.GetType().Name;
                _coverage["LastExceptionMessage"] = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Gets the coverage information for the target.
        /// </summary>
        /// <returns>An object containing coverage information.</returns>
        public object GetCoverage()
        {
            // Combine target coverage with strategy coverage
            var strategyCoverage = _strategy.GetCoverage();

            // Add strategy coverage to target coverage
            if (strategyCoverage is Dictionary<string, int> strategyDict)
            {
                foreach (var kvp in strategyDict)
                {
                    _coverage[$"Strategy_{kvp.Key}"] = kvp.Value;
                }
            }
            else if (strategyCoverage is Dictionary<string, object> strategyObjDict)
            {
                foreach (var kvp in strategyObjDict)
                {
                    _coverage[$"Strategy_{kvp.Key}"] = kvp.Value;
                }
            }
            else
            {
                _coverage["StrategyCoverage"] = strategyCoverage;
            }

            return _coverage;
        }

        /// <summary>
        /// Gets the name of the fuzzing target.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Creates a cache fuzzing strategy based on the strategy name.
        /// </summary>
        /// <param name="strategyName">The name of the strategy to create.</param>
        /// <param name="useEnhanced">Whether to use enhanced strategies.</param>
        /// <returns>An instance of the specified strategy.</returns>
        public static ICacheFuzzStrategy CreateStrategy(string strategyName, bool useEnhanced = true)
        {
            // Check if the strategy name includes a specific type indicator
            if (strategyName.Contains(":"))
            {
                var parts = strategyName.Split(':');
                if (parts.Length == 2)
                {
                    strategyName = parts[0];
                    useEnhanced = parts[1].Equals("enhanced", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (useEnhanced)
            {
                return strategyName switch
                {
                    "Basic" => new BasicCacheFuzzStrategy(),
                    "Capacity" => new CapacityFuzzStrategy(),
                    "Concurrency" => new ConcurrencyFuzzStrategy(),
                    "KeyValueMutation" => new KeyValueMutationStrategy(),
                    "StateTracking" => new StateTrackingFuzzStrategy(),
                    "Composite" => new CompositeCacheFuzzStrategy(),
                    // Add other enhanced strategies as they are implemented
                    _ => new BasicCacheFuzzStrategy() // Default to enhanced basic strategy
                };
            }
            else
            {
                return strategyName switch
                {
                    "Basic" => new BasicCacheFuzzStrategy(),
                    "Capacity" => new CapacityFuzzStrategy(),
                    "Concurrency" => new ConcurrencyFuzzStrategy(),
                    "StateTracking" => new StateTrackingFuzzStrategy(),
                    "KeyValueMutation" => new KeyValueMutationStrategy(),
                    "Composite" => new CompositeCacheFuzzStrategy(),
                    // Add other strategies as they are implemented
                    _ => new BasicCacheFuzzStrategy() // Default to basic strategy
                };
            }
        }
    }
}

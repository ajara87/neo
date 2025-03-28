// Copyright (C) 2015-2025 The Neo Project.
//
// TestExecutor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO.Fuzzer.TestHarness
{
    /// <summary>
    /// Executes fuzzing tests and monitors execution
    /// </summary>
    public class TestExecutor
    {
        private readonly IFuzzTarget _target;
        private readonly int _timeoutMilliseconds;
        private readonly CoverageTracker? _coverageTracker;

        /// <summary>
        /// Gets or sets the results of differential testing
        /// </summary>
        public List<object> DifferentialResults { get; set; } = new();

        /// <summary>
        /// Gets or sets the inputs used for stateful testing
        /// </summary>
        public List<byte[]> StatefulInputs { get; set; } = new();

        /// <summary>
        /// Gets or sets the performance data for performance testing
        /// </summary>
        public PerformanceData PerformanceData { get; set; } = new();

        /// <summary>
        /// Gets or sets the execution times for performance testing
        /// </summary>
        public List<long> ExecutionTimes { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the TestExecutor class
        /// </summary>
        /// <param name="target">The target to test</param>
        /// <param name="timeoutMilliseconds">The timeout in milliseconds for each test</param>
        /// <param name="coverageTracker">Optional coverage tracker for recording coverage data</param>
        public TestExecutor(IFuzzTarget target, int timeoutMilliseconds, CoverageTracker? coverageTracker = null)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _timeoutMilliseconds = timeoutMilliseconds;
            _coverageTracker = coverageTracker;
        }

        /// <summary>
        /// Executes a test with the specified input
        /// </summary>
        /// <param name="input">The input data</param>
        /// <returns>The result of the test</returns>
        public async Task<TestResult> ExecuteTestAsync(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var result = new TestResult
            {
                Input = input,
                InputSize = input.Length
            };

            var stopwatch = new Stopwatch();
            var cancellationTokenSource = new CancellationTokenSource(_timeoutMilliseconds);

            try
            {
                // Start the timer
                stopwatch.Start();

                // Execute the test
                var testTask = Task.Run(() => _target.Execute(input), cancellationTokenSource.Token);

                // Wait for the test to complete or timeout
                await testTask;

                // Stop the timer
                stopwatch.Stop();

                // Record the execution time
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;

                // Record the coverage
                var coverage = _target.GetCoverage();
                result.Coverage = coverage;

                // Update the coverage tracker if provided
                if (_coverageTracker != null && coverage is HashSet<string> coverageSet)
                {
                    result.IsInteresting = _coverageTracker.UpdateCoverage(coverageSet);
                }

                // Set the outcome
                result.Outcome = TestOutcome.Success;
            }
            catch (OperationCanceledException)
            {
                // Test timed out
                stopwatch.Stop();
                result.ExecutionTime = _timeoutMilliseconds;
                result.Outcome = TestOutcome.Timeout;
                result.Exception = new TimeoutException($"Test execution timed out after {_timeoutMilliseconds}ms");
                result.Crashed = true;
            }
            catch (Exception ex)
            {
                // Test threw an exception
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;
                result.Exception = ex;
                result.Outcome = TestOutcome.Exception;
                result.Crashed = true;
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Executes a differential test comparing multiple implementations
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="implementations">The implementations to compare</param>
        /// <returns>The result of the test</returns>
        public async Task<TestResult> ExecuteDifferentialTestAsync(byte[] input, List<IFuzzTarget> implementations)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (implementations == null || implementations.Count < 2)
                throw new ArgumentException("At least two implementations are required for differential testing", nameof(implementations));

            var result = new TestResult
            {
                Input = input,
                InputSize = input.Length
            };

            var stopwatch = new Stopwatch();
            var results = new List<object>();

            try
            {
                // Start the timer
                stopwatch.Start();

                // Execute each implementation
                foreach (var implementation in implementations)
                {
                    var implementationResult = await ExecuteImplementationAsync(input, implementation);
                    results.Add(implementationResult);

                    // If any implementation crashes, record it
                    if (implementationResult is Exception)
                    {
                        result.Exception = implementationResult as Exception;
                        result.Outcome = TestOutcome.Exception;
                        result.Crashed = true;
                        break;
                    }
                }

                // Stop the timer
                stopwatch.Stop();

                // Record the execution time
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;

                // If no crashes, check for differences
                if (!result.Crashed && results.Count >= 2)
                {
                    bool allEqual = true;
                    var firstResult = results[0];

                    for (int i = 1; i < results.Count; i++)
                    {
                        if (!AreResultsEqual(firstResult, results[i]))
                        {
                            allEqual = false;
                            result.DifferentialResults = results;
                            result.Outcome = TestOutcome.Differential;
                            result.Crashed = true;
                            break;
                        }
                    }

                    if (allEqual)
                    {
                        result.Outcome = TestOutcome.Success;
                    }
                }
            }
            catch (Exception ex)
            {
                // Test threw an unexpected exception
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;
                result.Exception = ex;
                result.Outcome = TestOutcome.Exception;
                result.Crashed = true;
            }

            return result;
        }

        /// <summary>
        /// Executes a performance test to identify anomalies
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="baselineIterations">Number of iterations to establish a baseline</param>
        /// <param name="anomalyThreshold">Threshold for identifying performance anomalies</param>
        /// <returns>The result of the test</returns>
        public async Task<TestResult> ExecutePerformanceTestAsync(byte[] input, int baselineIterations, double anomalyThreshold)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var result = new TestResult
            {
                Input = input,
                InputSize = input.Length
            };

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();

            try
            {
                // Execute the test multiple times to establish a baseline
                for (int i = 0; i < baselineIterations; i++)
                {
                    stopwatch.Restart();
                    await Task.Run(() => _target.Execute(input));
                    stopwatch.Stop();
                    executionTimes.Add(stopwatch.ElapsedMilliseconds);
                }

                // Calculate statistics
                double averageTime = executionTimes.Count > 0 ? executionTimes.Average() : 0;
                double standardDeviation = CalculateStandardDeviation(executionTimes, averageTime);

                // Record the execution time (average)
                result.ExecutionTime = (long)averageTime;
                result.PerformanceData = new PerformanceData
                {
                    AverageExecutionTime = averageTime,
                    StandardDeviation = standardDeviation,
                    ExecutionTimes = executionTimes.ToArray()
                };

                // Check for performance anomalies
                if (standardDeviation > 0 && (standardDeviation / averageTime) > anomalyThreshold)
                {
                    result.Outcome = TestOutcome.PerformanceAnomaly;
                    result.Crashed = true;
                }
                else
                {
                    result.Outcome = TestOutcome.Success;
                }
            }
            catch (Exception ex)
            {
                // Test threw an exception
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;
                result.Exception = ex;
                result.Outcome = TestOutcome.Exception;
                result.Crashed = true;
            }

            return result;
        }

        /// <summary>
        /// Executes a stateful test with a sequence of operations
        /// </summary>
        /// <param name="inputs">The sequence of input data</param>
        /// <param name="resetStateBetweenTests">Whether to reset state between tests</param>
        /// <returns>The result of the test</returns>
        public async Task<TestResult> ExecuteStatefulTestAsync(List<byte[]> inputs, bool resetStateBetweenTests)
        {
            if (inputs == null || inputs.Count == 0)
                throw new ArgumentException("At least one input is required for stateful testing", nameof(inputs));

            var result = new TestResult
            {
                Input = inputs[0], // Store the first input for reference
                InputSize = inputs.Sum(i => i.Length),
                StatefulInputs = inputs
            };

            var stopwatch = new Stopwatch();
            var cancellationTokenSource = new CancellationTokenSource(_timeoutMilliseconds);

            try
            {
                // Start the timer
                stopwatch.Start();

                // Execute each input in sequence
                foreach (var input in inputs)
                {
                    if (resetStateBetweenTests)
                    {
                        // Reset the target's state if required
                        if (_target is IStatefulFuzzTarget statefulTarget)
                        {
                            statefulTarget.ResetState();
                        }
                    }

                    // Execute the test with this input
                    var testTask = Task.Run(() => _target.Execute(input), cancellationTokenSource.Token);
                    await testTask;
                }

                // Stop the timer
                stopwatch.Stop();

                // Record the execution time
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;

                // Record the coverage
                result.Coverage = _target.GetCoverage();

                // Set the outcome
                result.Outcome = TestOutcome.Success;
            }
            catch (OperationCanceledException)
            {
                // Test timed out
                stopwatch.Stop();
                result.ExecutionTime = _timeoutMilliseconds;
                result.Outcome = TestOutcome.Timeout;
                result.Exception = new TimeoutException($"Stateful test execution timed out after {_timeoutMilliseconds}ms");
                result.Crashed = true;
            }
            catch (Exception ex)
            {
                // Test threw an exception
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.ElapsedMilliseconds;
                result.Exception = ex;
                result.Outcome = TestOutcome.Exception;
                result.Crashed = true;
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Executes a single implementation and returns its result or exception
        /// </summary>
        private async Task<object> ExecuteImplementationAsync(byte[] input, IFuzzTarget implementation)
        {
            var cancellationTokenSource = new CancellationTokenSource(_timeoutMilliseconds);

            try
            {
                var result = await Task.Run(() => implementation.Execute(input), cancellationTokenSource.Token);
                return result;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Compares two results for equality
        /// </summary>
        private bool AreResultsEqual(object result1, object result2)
        {
            if (result1 == null && result2 == null)
                return true;

            if (result1 == null || result2 == null)
                return false;

            if (result1 is Exception || result2 is Exception)
                return false;

            // If both results are byte arrays, compare them
            if (result1 is byte[] bytes1 && result2 is byte[] bytes2)
                return bytes1.SequenceEqual(bytes2);

            // Otherwise, use the default equality comparison
            return result1.Equals(result2);
        }

        /// <summary>
        /// Calculates the standard deviation of a set of values
        /// </summary>
        private double CalculateStandardDeviation(List<long> values, double mean)
        {
            if (values.Count <= 1)
                return 0;

            double sumOfSquaredDifferences = values.Sum(value => Math.Pow(value - mean, 2));
            return Math.Sqrt(sumOfSquaredDifferences / (values.Count - 1));
        }
    }

    /// <summary>
    /// Represents the outcome of a test
    /// </summary>
    public enum TestOutcome
    {
        /// <summary>
        /// The test completed successfully
        /// </summary>
        Success,

        /// <summary>
        /// The test timed out
        /// </summary>
        Timeout,

        /// <summary>
        /// The test threw an exception
        /// </summary>
        Exception,

        /// <summary>
        /// The test found a differential between implementations
        /// </summary>
        Differential,

        /// <summary>
        /// The test found a performance anomaly
        /// </summary>
        PerformanceAnomaly
    }

    /// <summary>
    /// Represents the result of a test
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Gets or sets the input data
        /// </summary>
        public byte[] Input { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the size of the input data
        /// </summary>
        public int InputSize { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the outcome of the test
        /// </summary>
        public TestOutcome Outcome { get; set; }

        /// <summary>
        /// Gets or sets the exception that was thrown during the test, if any
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the test crashed
        /// </summary>
        public bool Crashed { get; set; }

        /// <summary>
        /// Gets or sets the coverage information for the test
        /// </summary>
        public object Coverage { get; set; } = new object();

        /// <summary>
        /// Gets or sets a value indicating whether the test produced interesting coverage
        /// </summary>
        public bool IsInteresting { get; set; }

        /// <summary>
        /// Gets or sets the results of differential testing
        /// </summary>
        public List<object> DifferentialResults { get; set; } = new();

        /// <summary>
        /// Gets or sets the inputs used for stateful testing
        /// </summary>
        public List<byte[]> StatefulInputs { get; set; } = new();

        /// <summary>
        /// Gets or sets the performance data for performance testing
        /// </summary>
        public PerformanceData PerformanceData { get; set; } = new();

        /// <summary>
        /// Gets or sets the execution times for performance testing
        /// </summary>
        public List<long> ExecutionTimes { get; set; } = new();
    }

    /// <summary>
    /// Represents performance data for a test
    /// </summary>
    public class PerformanceData
    {
        /// <summary>
        /// Gets or sets the average execution time in milliseconds
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the standard deviation of execution times
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// Gets or sets the individual execution times
        /// </summary>
        public long[] ExecutionTimes { get; set; }
    }
}

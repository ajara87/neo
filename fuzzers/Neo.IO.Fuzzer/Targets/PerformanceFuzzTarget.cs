// Copyright (C) 2015-2025 The Neo Project.
//
// PerformanceFuzzTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// A fuzz target that measures performance characteristics to identify anomalies
    /// </summary>
    public class PerformanceFuzzTarget : IFuzzTarget
    {
        // Delegate for the operation to measure
        public delegate void OperationHandler(byte[] data);

        private readonly OperationHandler _operation;
        private readonly int _baselineIterations;
        private readonly double _anomalyThreshold;

        // Performance metrics
        private readonly List<long> _executionTimes = new();
        private readonly Dictionary<string, List<long>> _taggedExecutionTimes = new();

        // Anomalies detected
        private readonly List<(byte[] Input, long ExecutionTime, double Ratio)> _anomalies = new();

        // Baseline statistics
        private double _baselineMean;
        private double _baselineStdDev;
        private bool _baselineEstablished = false;

        // Coverage tracking
        private readonly HashSet<string> _coveragePoints = new();

        /// <summary>
        /// Gets the name of the fuzz target
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the maximum number of anomalies to track
        /// </summary>
        public int MaxAnomalies { get; set; } = 20;

        /// <summary>
        /// Gets or sets the warmup iterations to perform before measuring performance
        /// </summary>
        public int WarmupIterations { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of iterations to perform for each measurement
        /// </summary>
        public int MeasurementIterations { get; set; } = 5;

        /// <summary>
        /// Initializes a new instance of the PerformanceFuzzTarget class
        /// </summary>
        /// <param name="name">The name of the fuzz target</param>
        /// <param name="operation">The operation to measure</param>
        /// <param name="baselineIterations">The number of iterations to perform to establish a baseline</param>
        /// <param name="anomalyThreshold">The threshold ratio for considering a measurement anomalous</param>
        public PerformanceFuzzTarget(
            string name,
            OperationHandler? operation,
            int baselineIterations = 100,
            double anomalyThreshold = 5.0)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _baselineIterations = baselineIterations;
            _anomalyThreshold = anomalyThreshold;
        }

        /// <summary>
        /// Establishes a performance baseline by running the operation with random inputs
        /// </summary>
        /// <param name="random">The random number generator to use</param>
        /// <returns>True if the baseline was successfully established</returns>
        public async Task<bool> EstablishBaselineAsync(Random random)
        {
            if (_baselineEstablished)
                return true;

            var baselineTimes = new List<long>(_baselineIterations);

            // Generate random inputs for baseline measurements
            for (int i = 0; i < _baselineIterations; i++)
            {
                // Generate a random input
                int size = random.Next(10, 1000);
                byte[] data = new byte[size];
                random.NextBytes(data);

                // Measure execution time
                long executionTime = MeasureExecutionTime(data);
                baselineTimes.Add(executionTime);

                // Add a small delay to avoid CPU throttling effects
                await Task.Delay(1);
            }

            // Calculate baseline statistics
            _baselineMean = baselineTimes.Average();
            double variance = baselineTimes.Sum(t => Math.Pow(t - _baselineMean, 2)) / baselineTimes.Count;
            _baselineStdDev = Math.Sqrt(variance);

            _baselineEstablished = true;

            // Record baseline as a coverage point
            _coveragePoints.Add($"Baseline:Mean:{_baselineMean:F2}:StdDev:{_baselineStdDev:F2}");

            return true;
        }

        /// <summary>
        /// Executes the fuzz test by measuring the performance of the operation
        /// </summary>
        /// <param name="data">The input data for the test</param>
        /// <returns>True if the performance is within expected parameters, false otherwise</returns>
        public bool Execute(byte[] data)
        {
            if (data == null || data.Length == 0)
                return true;

            if (!_baselineEstablished)
                throw new InvalidOperationException("Baseline must be established before executing performance tests");

            // Measure execution time
            long executionTime = MeasureExecutionTime(data);

            // Record the execution time
            _executionTimes.Add(executionTime);

            // Check if this is an anomaly
            double ratio = (double)executionTime / _baselineMean;
            bool isAnomaly = ratio > _anomalyThreshold;

            // Record coverage points
            string performanceCategory = GetPerformanceCategory(ratio);
            _coveragePoints.Add($"Performance:{performanceCategory}");

            // If this is an anomaly, record it
            if (isAnomaly && _anomalies.Count < MaxAnomalies)
            {
                _anomalies.Add((data, executionTime, ratio));
                _coveragePoints.Add($"Anomaly:Ratio:{ratio:F2}");
            }

            // The test "succeeds" even if we find an anomaly, since we're just measuring
            return true;
        }

        /// <summary>
        /// Measures the execution time of the operation
        /// </summary>
        /// <param name="data">The input data</param>
        /// <returns>The execution time in ticks</returns>
        private long MeasureExecutionTime(byte[] data)
        {
            // Perform warmup iterations
            for (int i = 0; i < WarmupIterations; i++)
            {
                _operation(data);
            }

            // Perform measurement iterations
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < MeasurementIterations; i++)
            {
                _operation(data);
            }

            stopwatch.Stop();

            // Return average time per iteration
            return stopwatch.ElapsedTicks / MeasurementIterations;
        }

        /// <summary>
        /// Tags an execution time with a specific label for more detailed analysis
        /// </summary>
        /// <param name="tag">The tag to apply</param>
        /// <param name="executionTime">The execution time to tag</param>
        public void TagExecutionTime(string tag, long executionTime)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            if (!_taggedExecutionTimes.ContainsKey(tag))
                _taggedExecutionTimes[tag] = new List<long>();

            _taggedExecutionTimes[tag].Add(executionTime);
        }

        /// <summary>
        /// Gets the performance category for a given ratio
        /// </summary>
        /// <param name="ratio">The ratio of execution time to baseline</param>
        /// <returns>The performance category</returns>
        private string GetPerformanceCategory(double ratio)
        {
            if (ratio <= 0.5) return "VeryFast";
            if (ratio <= 0.8) return "Fast";
            if (ratio <= 1.2) return "Normal";
            if (ratio <= 2.0) return "Slow";
            if (ratio <= 5.0) return "VerySlow";
            return "Anomalous";
        }

        /// <summary>
        /// Gets the coverage points for the target
        /// </summary>
        /// <returns>A set of all coverage points</returns>
        public HashSet<string> GetCoveragePoints()
        {
            return new HashSet<string>(_coveragePoints);
        }

        /// <summary>
        /// Gets the coverage information for this target
        /// </summary>
        /// <returns>A set of coverage points</returns>
        public object GetCoverage()
        {
            // Return the set of coverage points
            return new HashSet<string>(_coveragePoints);
        }

        /// <summary>
        /// Gets detailed information about the performance fuzzing results
        /// </summary>
        /// <returns>A string containing detailed information</returns>
        public string GetDetailedResults()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Performance Fuzzing Results for {Name}:");
            sb.AppendLine($"Baseline Mean: {_baselineMean:F2} ticks");
            sb.AppendLine($"Baseline StdDev: {_baselineStdDev:F2} ticks");
            sb.AppendLine($"Anomaly Threshold: {_anomalyThreshold:F2}x baseline");
            sb.AppendLine();

            if (_executionTimes.Count > 0)
            {
                double mean = _executionTimes.Average();
                double min = _executionTimes.Min();
                double max = _executionTimes.Max();

                sb.AppendLine($"Execution Times:");
                sb.AppendLine($"  Count: {_executionTimes.Count}");
                sb.AppendLine($"  Mean: {mean:F2} ticks ({mean / _baselineMean:F2}x baseline)");
                sb.AppendLine($"  Min: {min:F2} ticks ({min / _baselineMean:F2}x baseline)");
                sb.AppendLine($"  Max: {max:F2} ticks ({max / _baselineMean:F2}x baseline)");
                sb.AppendLine();
            }

            if (_taggedExecutionTimes.Count > 0)
            {
                sb.AppendLine("Tagged Execution Times:");
                foreach (var (tag, times) in _taggedExecutionTimes)
                {
                    double mean = times.Average();
                    sb.AppendLine($"  {tag}: {mean:F2} ticks ({mean / _baselineMean:F2}x baseline), Count: {times.Count}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"Anomalies Found: {_anomalies.Count}");
            if (_anomalies.Count > 0)
            {
                sb.AppendLine("Anomaly Details:");
                for (int i = 0; i < _anomalies.Count; i++)
                {
                    var (input, time, ratio) = _anomalies[i];
                    sb.AppendLine($"  Anomaly #{i + 1}:");
                    sb.AppendLine($"    Input: {BitConverter.ToString(input).Replace("-", "")} (Length: {input.Length})");
                    sb.AppendLine($"    Execution Time: {time:F2} ticks ({ratio:F2}x baseline)");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the anomalies found during fuzzing
        /// </summary>
        /// <returns>A list of anomalies</returns>
        public List<(byte[] Input, long ExecutionTime, double Ratio)> GetAnomalies()
        {
            return _anomalies;
        }

        /// <summary>
        /// Gets performance statistics for a specific tag
        /// </summary>
        /// <param name="tag">The tag to get statistics for</param>
        /// <returns>A dictionary containing performance statistics</returns>
        public Dictionary<string, double> GetTagStatistics(string tag)
        {
            if (!_taggedExecutionTimes.ContainsKey(tag) || _taggedExecutionTimes[tag].Count == 0)
                return new Dictionary<string, double>();

            var times = _taggedExecutionTimes[tag];
            double mean = times.Average();
            double min = times.Min();
            double max = times.Max();

            return new Dictionary<string, double>
            {
                ["Count"] = times.Count,
                ["Mean"] = mean,
                ["Min"] = min,
                ["Max"] = max,
                ["RelativeToBaseline"] = mean / _baselineMean
            };
        }
    }
}

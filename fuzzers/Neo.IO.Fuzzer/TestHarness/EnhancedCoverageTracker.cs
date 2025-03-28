// Copyright (C) 2015-2025 The Neo Project.
//
// EnhancedCoverageTracker.cs file belongs to the neo project and is free
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

namespace Neo.IO.Fuzzer.TestHarness
{
    /// <summary>
    /// Enhanced coverage tracker that provides more detailed insights into code coverage
    /// </summary>
    public class EnhancedCoverageTracker : CoverageTracker
    {
        // Track execution counts for each coverage point
        private readonly Dictionary<string, int> _executionCounts = new();

        // Track combinations of coverage points (path coverage)
        private readonly HashSet<string> _pathCoverage = new();

        // Track data-dependent branches
        private readonly Dictionary<string, HashSet<string>> _dataValues = new();

        // Track the current execution path
        private readonly List<string> _currentPath = new();

        // Track the maximum path length to prevent excessive memory usage
        private const int MaxPathLength = 100;

        /// <summary>
        /// Records a coverage point with its input value to track data-dependent branches
        /// </summary>
        /// <param name="point">The coverage point to record</param>
        /// <param name="value">The value that led to this coverage point</param>
        public void RecordWithValue(string point, object value)
        {
            // Call the base implementation to record the point
            base.Record(point);

            // Track execution count
            if (!_executionCounts.ContainsKey(point))
                _executionCounts[point] = 0;
            _executionCounts[point]++;

            // Track unique data values that reach this point
            if (value != null)
            {
                string valueStr = value.ToString() ?? "null";
                if (!_dataValues.ContainsKey(point))
                    _dataValues[point] = new HashSet<string>();
                _dataValues[point].Add(valueStr);
            }

            // Add to current path
            _currentPath.Add(point);
            if (_currentPath.Count > MaxPathLength)
                _currentPath.RemoveAt(0);

            // Record path if it's of reasonable length
            if (_currentPath.Count >= 2)
            {
                string path = string.Join("->", _currentPath.TakeLast(Math.Min(10, _currentPath.Count)));
                _pathCoverage.Add(path);
            }
        }

        /// <summary>
        /// Records a sequence of coverage points to track path coverage
        /// </summary>
        /// <param name="points">The sequence of coverage points to record</param>
        public void RecordPath(IEnumerable<string> points)
        {
            foreach (string point in points)
            {
                base.Record(point);

                if (!_executionCounts.ContainsKey(point))
                    _executionCounts[point] = 0;
                _executionCounts[point]++;
            }

            string path = string.Join("->", points);
            _pathCoverage.Add(path);
        }

        /// <summary>
        /// Clears the current execution path
        /// </summary>
        public void ClearPath()
        {
            _currentPath.Clear();
        }

        /// <summary>
        /// Calculates a more sophisticated interestingness score based on multiple factors
        /// </summary>
        /// <param name="newPoints">The new coverage points to evaluate</param>
        /// <returns>The interestingness score</returns>
        public override double CalculateInterestingnessScore(HashSet<string> newPoints)
        {
            if (newPoints == null || newPoints.Count == 0)
                return 0;

            // Base score from new coverage points
            double baseScore = base.CalculateInterestingnessScore(newPoints);

            // Additional score for points with low execution counts
            double rarityScore = newPoints.Sum(p => _executionCounts.ContainsKey(p) ?
                1.0 / Math.Log10(_executionCounts[p] + 2) : 1.0);

            // Additional score for points with high data diversity
            double diversityScore = newPoints.Sum(p => _dataValues.ContainsKey(p) ?
                Math.Log10(_dataValues[p].Count + 1) : 0);

            return baseScore + (rarityScore * 0.3) + (diversityScore * 0.2);
        }

        /// <summary>
        /// Gets detailed coverage statistics
        /// </summary>
        /// <returns>A dictionary containing coverage statistics</returns>
        public Dictionary<string, object> GetDetailedCoverageStats()
        {
            return new Dictionary<string, object>
            {
                ["TotalCoveragePoints"] = _coveragePoints.Count,
                ["UniquePathsCount"] = _pathCoverage.Count,
                ["PointsWithDataDiversity"] = _dataValues.Count,
                ["AverageExecutionCount"] = _executionCounts.Count > 0 ?
                    _executionCounts.Values.Average() : 0,
                ["MaxExecutionCount"] = _executionCounts.Count > 0 ?
                    _executionCounts.Values.Max() : 0,
                ["MaxDataDiversity"] = _dataValues.Count > 0 ?
                    _dataValues.Values.Max(v => v.Count) : 0
            };
        }
    }
}

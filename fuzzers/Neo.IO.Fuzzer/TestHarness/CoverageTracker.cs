// Copyright (C) 2015-2025 The Neo Project.
//
// CoverageTracker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.IO.Fuzzer.TestHarness
{
    /// <summary>
    /// Tracks code coverage during fuzzing
    /// </summary>
    public class CoverageTracker
    {
        protected readonly HashSet<string> _coveragePoints = new();
        private readonly Dictionary<string, int> _hitCounts = new();

        /// <summary>
        /// Updates coverage with the provided coverage information
        /// </summary>
        /// <param name="coverage">The coverage information</param>
        /// <returns>True if the coverage was increased, false otherwise</returns>
        public bool UpdateCoverage(object coverage)
        {
            if (coverage == null)
                return false;

            bool increased = false;

            // Handle different types of coverage information
            if (coverage is HashSet<string> coveragePoints)
            {
                // Add each coverage point
                foreach (var point in coveragePoints)
                {
                    if (_coveragePoints.Add(point))
                    {
                        increased = true;
                    }

                    // Update hit count
                    if (!_hitCounts.ContainsKey(point))
                    {
                        _hitCounts[point] = 1;
                    }
                    else
                    {
                        _hitCounts[point]++;
                    }
                }
            }
            else if (coverage is IEnumerable<string> coverageList)
            {
                // Add each coverage point
                foreach (var point in coverageList)
                {
                    if (_coveragePoints.Add(point))
                    {
                        increased = true;
                    }

                    // Update hit count
                    if (!_hitCounts.ContainsKey(point))
                    {
                        _hitCounts[point] = 1;
                    }
                    else
                    {
                        _hitCounts[point]++;
                    }
                }
            }

            return increased;
        }

        /// <summary>
        /// Gets the total number of coverage points
        /// </summary>
        /// <returns>The number of coverage points</returns>
        public int GetCoverageCount()
        {
            return _coveragePoints.Count;
        }

        /// <summary>
        /// Gets the coverage percentage (always returns 0 since we don't know the total possible coverage)
        /// </summary>
        /// <returns>The coverage percentage</returns>
        public double GetCoveragePercentage()
        {
            // We don't know the total possible coverage, so we can't calculate a percentage
            return 0.0;
        }

        /// <summary>
        /// Gets the hit counts for all coverage points
        /// </summary>
        /// <returns>A dictionary mapping coverage points to hit counts</returns>
        public Dictionary<string, int> GetHitCounts()
        {
            return new Dictionary<string, int>(_hitCounts);
        }

        /// <summary>
        /// Gets all coverage points
        /// </summary>
        /// <returns>A set of all coverage points</returns>
        public HashSet<string> GetCoveragePoints()
        {
            return new HashSet<string>(_coveragePoints);
        }

        /// <summary>
        /// Clears all coverage data
        /// </summary>
        public void Clear()
        {
            _coveragePoints.Clear();
            _hitCounts.Clear();
        }

        /// <summary>
        /// Calculates an interestingness score for new coverage points
        /// </summary>
        /// <param name="newPoints">The new coverage points to evaluate</param>
        /// <returns>The interestingness score</returns>
        public virtual double CalculateInterestingnessScore(HashSet<string> newPoints)
        {
            // Basic implementation: simply return 1.0 if there are any new points, 0.0 otherwise
            return newPoints?.Count > 0 ? 1.0 : 0.0;
        }

        /// <summary>
        /// Records a single coverage point
        /// </summary>
        /// <param name="point">The coverage point to record</param>
        /// <returns>True if this is a new coverage point, false otherwise</returns>
        public bool Record(string point)
        {
            if (string.IsNullOrEmpty(point))
                return false;

            bool isNew = _coveragePoints.Add(point);

            // Update hit count
            if (!_hitCounts.ContainsKey(point))
            {
                _hitCounts[point] = 1;
            }
            else
            {
                _hitCounts[point]++;
            }

            return isNew;
        }

        /// <summary>
        /// Resets the coverage tracker to its initial state
        /// </summary>
        public virtual void Reset()
        {
            Clear();
        }
    }
}

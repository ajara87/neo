// Copyright (C) 2015-2025 The Neo Project.
//
// CoverageTrackerHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Neo.IO.Fuzzer.Utilities
{
    /// <summary>
    /// Helper class for tracking coverage information in fuzzing strategies.
    /// Provides thread-safe operations for incrementing coverage points and retrieving coverage data.
    /// </summary>
    public class CoverageTrackerHelper
    {
        private readonly Dictionary<string, int> _coveragePoints = new Dictionary<string, int>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverageTrackerHelper"/> class.
        /// </summary>
        /// <param name="name">The name of the component being tracked</param>
        public CoverageTrackerHelper(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            InitializePoint("TotalOperations");
            InitializePoint("Exceptions");
        }

        /// <summary>
        /// Initializes a coverage point with a starting value of 0.
        /// </summary>
        /// <param name="pointName">The name of the coverage point</param>
        public void InitializePoint(string pointName)
        {
            if (string.IsNullOrEmpty(pointName))
                throw new ArgumentNullException(nameof(pointName));

            _lock.EnterWriteLock();
            try
            {
                _coveragePoints[pointName] = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Initializes multiple coverage points with starting values of 0.
        /// </summary>
        /// <param name="pointNames">The names of the coverage points</param>
        public void InitializePoints(params string[] pointNames)
        {
            if (pointNames == null)
                throw new ArgumentNullException(nameof(pointNames));

            _lock.EnterWriteLock();
            try
            {
                foreach (var pointName in pointNames)
                {
                    if (!string.IsNullOrEmpty(pointName))
                    {
                        _coveragePoints[pointName] = 0;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Increments a coverage point by 1.
        /// </summary>
        /// <param name="pointName">The name of the coverage point</param>
        /// <returns>The new value of the coverage point</returns>
        public int IncrementPoint(string pointName)
        {
            return IncrementPoint(pointName, 1);
        }

        /// <summary>
        /// Increments a coverage point by the specified amount.
        /// </summary>
        /// <param name="pointName">The name of the coverage point</param>
        /// <param name="increment">The amount to increment by</param>
        /// <returns>The new value of the coverage point</returns>
        public int IncrementPoint(string pointName, int increment)
        {
            if (string.IsNullOrEmpty(pointName))
                throw new ArgumentNullException(nameof(pointName));

            _lock.EnterUpgradeableReadLock();
            try
            {
                if (!_coveragePoints.ContainsKey(pointName))
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _coveragePoints[pointName] = 0;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                _lock.EnterWriteLock();
                try
                {
                    _coveragePoints[pointName] += increment;
                    return _coveragePoints[pointName];
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Gets the current value of a coverage point.
        /// </summary>
        /// <param name="pointName">The name of the coverage point</param>
        /// <returns>The current value of the coverage point, or 0 if it doesn't exist</returns>
        public int GetPointValue(string pointName)
        {
            if (string.IsNullOrEmpty(pointName))
                throw new ArgumentNullException(nameof(pointName));

            _lock.EnterReadLock();
            try
            {
                return _coveragePoints.TryGetValue(pointName, out int value) ? value : 0;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all coverage data as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all coverage points and their values</returns>
        public Dictionary<string, int> GetCoverage()
        {
            _lock.EnterReadLock();
            try
            {
                // Create a copy of the coverage data to avoid thread safety issues
                var result = new Dictionary<string, int>(_coveragePoints)
                {
                    ["StrategyName"] = 1 // Add strategy name as a coverage point
                };
                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets all coverage data as an object that can be used with the IFuzzTarget.GetCoverage method.
        /// </summary>
        /// <returns>An object containing all coverage points and their values</returns>
        public object GetCoverageObject()
        {
            return GetCoverage();
        }

        /// <summary>
        /// Merges coverage data from another CoverageTrackerHelper instance.
        /// </summary>
        /// <param name="other">The other CoverageTrackerHelper to merge from</param>
        public void MergeCoverage(CoverageTrackerHelper other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var otherCoverage = other.GetCoverage();

            _lock.EnterWriteLock();
            try
            {
                foreach (var kvp in otherCoverage)
                {
                    if (_coveragePoints.TryGetValue(kvp.Key, out int currentValue))
                    {
                        _coveragePoints[kvp.Key] = currentValue + kvp.Value;
                    }
                    else
                    {
                        _coveragePoints[kvp.Key] = kvp.Value;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the name of the component being tracked.
        /// </summary>
        /// <returns>The name of the component</returns>
        public string GetName()
        {
            return _name;
        }

        /// <summary>
        /// Gets the percentage of coverage points that have been hit.
        /// </summary>
        /// <returns>The percentage of coverage points hit</returns>
        public double GetCoveragePercentage()
        {
            _lock.EnterReadLock();
            try
            {
                if (_coveragePoints.Count == 0)
                    return 0.0;

                int hitPoints = 0;

                foreach (var kvp in _coveragePoints)
                {
                    if (kvp.Value > 0)
                        hitPoints++;
                }

                return (double)hitPoints / _coveragePoints.Count * 100.0;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Resets all coverage counters to zero.
        /// </summary>
        public void ResetCoverage()
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var key in _coveragePoints.Keys.ToList())
                {
                    _coveragePoints[key] = 0;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Disposes the CoverageTrackerHelper and releases any resources.
        /// </summary>
        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}

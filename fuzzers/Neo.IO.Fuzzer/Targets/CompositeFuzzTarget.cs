// Copyright (C) 2015-2025 The Neo Project.
//
// CompositeFuzzTarget.cs file belongs to the neo project and is free
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

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Target that combines multiple fuzz targets
    /// </summary>
    public class CompositeFuzzTarget : IFuzzTarget
    {
        private readonly IFuzzTarget[] _targets;
        private readonly HashSet<string> _coveragePoints = new();

        public string Name => "Composite";

        /// <summary>
        /// Initializes a new instance of the CompositeFuzzTarget class
        /// </summary>
        /// <param name="targets">The targets to combine</param>
        public CompositeFuzzTarget(IFuzzTarget[] targets)
        {
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));

            if (_targets.Length == 0)
            {
                throw new ArgumentException("At least one target must be provided", nameof(targets));
            }
        }

        /// <summary>
        /// Executes all targets with the provided input
        /// </summary>
        /// <param name="input">The input data to use</param>
        /// <returns>True if all targets executed successfully, false otherwise</returns>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return true;
            }

            var sb = new StringBuilder();
            bool allSuccessful = true;

            // Execute each target
            foreach (var target in _targets)
            {
                try
                {
                    sb.AppendLine($"Executing target: {target.Name}");
                    bool success = target.Execute(input);

                    if (!success)
                    {
                        allSuccessful = false;
                        sb.AppendLine($"Target {target.Name} failed");
                    }

                    // Collect coverage from the target
                    var coverage = target.GetCoverage();
                    if (coverage != null)
                    {
                        // Track the coverage points
                        if (coverage is HashSet<string> coverageSet)
                        {
                            foreach (var point in coverageSet)
                            {
                                _coveragePoints.Add($"{target.Name}:{point}");
                            }
                        }
                        else if (coverage is IEnumerable<string> coverageEnum)
                        {
                            foreach (var point in coverageEnum)
                            {
                                _coveragePoints.Add($"{target.Name}:{point}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    allSuccessful = false;
                    sb.AppendLine($"Exception in target {target.Name}: {ex.Message}");
                    _coveragePoints.Add($"Exception:{target.Name}:{ex.GetType().Name}");
                }
            }

            // Add the execution trace as a coverage point
            _coveragePoints.Add($"ExecutionTrace:{sb.ToString().GetHashCode()}");

            return allSuccessful;
        }

        /// <summary>
        /// Gets the coverage information
        /// </summary>
        /// <returns>The coverage information</returns>
        public object GetCoverage()
        {
            return _coveragePoints;
        }
    }
}

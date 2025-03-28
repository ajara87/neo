// Copyright (C) 2015-2025 The Neo Project.
//
// IReporter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.TestHarness;
using System;
using System.Collections.Generic;

namespace Neo.IO.Fuzzer.Reporting
{
    /// <summary>
    /// Interface for reporting fuzzing results
    /// </summary>
    public interface IReporter
    {
        /// <summary>
        /// Reports progress during the fuzzing process
        /// </summary>
        /// <param name="iteration">The current iteration</param>
        /// <param name="totalIterations">The total number of iterations</param>
        /// <param name="testsExecuted">The number of tests executed</param>
        /// <param name="interestingInputs">The number of interesting inputs found</param>
        /// <param name="corpusSize">The size of the corpus</param>
        /// <param name="crashCount">The number of crashes found</param>
        /// <param name="testsPerSecond">The number of tests executed per second</param>
        /// <param name="averageExecutionTime">The average execution time in milliseconds</param>
        /// <param name="averageInputSize">The average input size in bytes</param>
        /// <param name="coverageCount">The number of coverage points</param>
        void ReportProgress(
            int iteration,
            int totalIterations,
            int testsExecuted,
            int interestingInputs,
            int corpusSize,
            int crashCount,
            double testsPerSecond,
            double averageExecutionTime,
            double averageInputSize,
            int coverageCount);

        /// <summary>
        /// Reports a crash
        /// </summary>
        /// <param name="data">The input data that caused the crash</param>
        /// <param name="exception">The exception that was thrown</param>
        void ReportCrash(byte[] data, Exception exception);

        /// <summary>
        /// Reports final statistics
        /// </summary>
        /// <param name="testsExecuted">The number of tests executed</param>
        /// <param name="interestingInputs">The number of interesting inputs found</param>
        /// <param name="corpusSize">The size of the corpus</param>
        /// <param name="crashCount">The number of crashes found</param>
        /// <param name="coverageCount">The number of coverage points</param>
        void ReportStatistics(
            int testsExecuted,
            int interestingInputs,
            int corpusSize,
            int crashCount,
            int coverageCount);
    }

    /// <summary>
    /// Represents fuzzing statistics
    /// </summary>
    public class FuzzingStatistics
    {
        /// <summary>
        /// Gets or sets the total number of tests executed
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Gets or sets the number of successful tests
        /// </summary>
        public int SuccessfulTests { get; set; }

        /// <summary>
        /// Gets or sets the number of failed tests
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Gets or sets the number of tests that timed out
        /// </summary>
        public int TimeoutTests { get; set; }

        /// <summary>
        /// Gets or sets the number of tests that caused an exception
        /// </summary>
        public int ExceptionTests { get; set; }

        /// <summary>
        /// Gets or sets the number of tests that caused a target failure
        /// </summary>
        public int TargetFailureTests { get; set; }

        /// <summary>
        /// Gets or sets the number of tests that caused an executor exception
        /// </summary>
        public int ExecutorExceptionTests { get; set; }

        /// <summary>
        /// Gets or sets the number of interesting inputs found
        /// </summary>
        public int InterestingInputs { get; set; }

        /// <summary>
        /// Gets or sets the total execution time in milliseconds
        /// </summary>
        public long TotalExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the average execution time in milliseconds
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the total size of all inputs in bytes
        /// </summary>
        public long TotalInputSize { get; set; }

        /// <summary>
        /// Gets or sets the average size of inputs in bytes
        /// </summary>
        public double AverageInputSize { get; set; }

        /// <summary>
        /// Gets or sets the corpus size
        /// </summary>
        public int CorpusSize { get; set; }

        /// <summary>
        /// Gets or sets the number of crashes
        /// </summary>
        public int CrashesCount { get; set; }

        /// <summary>
        /// Gets or sets the coverage statistics
        /// </summary>
        public Dictionary<string, int> CoveragePoints { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the total number of coverage points
        /// </summary>
        public int TotalCoveragePoints { get; set; }
    }
}

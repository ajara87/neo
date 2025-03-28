// Copyright (C) 2015-2025 The Neo Project.
//
// FileReporter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.TestHarness;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Neo.IO.Fuzzer.Reporting
{
    /// <summary>
    /// Reports fuzzing results to files
    /// </summary>
    public class FileReporter : IReporter
    {
        private readonly string _outputDir;
        private readonly string _logFile;
        private readonly string _statsFile;
        private readonly string _crashesDir;
        private readonly object _logLock = new();

        /// <summary>
        /// Initializes a new instance of the FileReporter class
        /// </summary>
        /// <param name="outputFile">The output file for reports</param>
        public FileReporter(string outputFile)
        {
            if (string.IsNullOrEmpty(outputFile))
                throw new ArgumentNullException(nameof(outputFile));

            _outputDir = Path.GetDirectoryName(outputFile) ?? Directory.GetCurrentDirectory();
            _logFile = outputFile;
            _statsFile = Path.Combine(_outputDir, "fuzzer_stats.json");
            _crashesDir = Path.Combine(_outputDir, "crashes");

            // Create output directories
            Directory.CreateDirectory(_outputDir);
            Directory.CreateDirectory(_crashesDir);

            // Log the start of the fuzzing session
            LogMessage($"Fuzzing session started at {DateTime.Now}");
        }

        /// <summary>
        /// Reports progress during the fuzzing process
        /// </summary>
        public void ReportProgress(
            int iteration,
            int totalIterations,
            int testsExecuted,
            int interestingInputs,
            int corpusSize,
            int crashCount,
            double testsPerSecond,
            double averageExecutionTime,
            double averageInputSize,
            int coverageCount)
        {
            lock (_logLock)
            {
                var progress = new
                {
                    Timestamp = DateTime.Now,
                    Iteration = iteration,
                    TotalIterations = totalIterations,
                    TestsExecuted = testsExecuted,
                    InterestingInputs = interestingInputs,
                    CorpusSize = corpusSize,
                    CrashCount = crashCount,
                    TestsPerSecond = testsPerSecond,
                    AverageExecutionTime = averageExecutionTime,
                    AverageInputSize = averageInputSize,
                    CoverageCount = coverageCount
                };

                LogMessage($"Progress: {iteration}/{totalIterations} - Tests: {testsExecuted}, Corpus: {corpusSize}, Crashes: {crashCount}");
            }
        }

        /// <summary>
        /// Reports a crash
        /// </summary>
        public void ReportCrash(byte[] data, Exception exception)
        {
            lock (_logLock)
            {
                // Log the crash
                LogMessage($"CRASH: {exception.GetType().Name}: {exception.Message}");

                // Save the crash details to a file
                string crashId = Guid.NewGuid().ToString("N");
                string crashFile = Path.Combine(_crashesDir, $"crash_{crashId}.json");
                string inputFile = Path.Combine(_crashesDir, $"input_{crashId}.bin");

                var crashInfo = new
                {
                    Timestamp = DateTime.Now,
                    ExceptionType = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InputSize = data.Length,
                    InputFile = inputFile
                };

                // Save crash info
                File.WriteAllText(crashFile, JsonConvert.SerializeObject(crashInfo, Formatting.Indented));

                // Save input data
                File.WriteAllBytes(inputFile, data);
            }
        }

        /// <summary>
        /// Reports final statistics
        /// </summary>
        public void ReportStatistics(
            int testsExecuted,
            int interestingInputs,
            int corpusSize,
            int crashCount,
            int coverageCount)
        {
            lock (_logLock)
            {
                var statistics = new
                {
                    Timestamp = DateTime.Now,
                    TestsExecuted = testsExecuted,
                    InterestingInputs = interestingInputs,
                    CorpusSize = corpusSize,
                    CrashCount = crashCount,
                    CoverageCount = coverageCount,
                    InterestingRatio = testsExecuted > 0 ? (double)interestingInputs / testsExecuted : 0,
                    CrashRatio = testsExecuted > 0 ? (double)crashCount / testsExecuted : 0
                };

                // Log the statistics
                LogMessage($"Final statistics: Tests: {testsExecuted}, Interesting: {interestingInputs}, Corpus: {corpusSize}, Crashes: {crashCount}, Coverage: {coverageCount}");

                // Save the statistics to a file
                File.WriteAllText(_statsFile, JsonConvert.SerializeObject(statistics, Formatting.Indented));
            }
        }

        /// <summary>
        /// Logs a message to the log file
        /// </summary>
        /// <param name="message">The message to log</param>
        private void LogMessage(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(_logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}

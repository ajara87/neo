// Copyright (C) 2015-2025 The Neo Project.
//
// ConsoleReporter.cs file belongs to the neo project and is free
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
using System.Text;

namespace Neo.IO.Fuzzer.Reporting
{
    /// <summary>
    /// Reports fuzzing results to the console
    /// </summary>
    public class ConsoleReporter : IReporter
    {
        private readonly bool _verbose;
        private readonly object _lock = new object();
        private int _totalTests;
        private int _successfulTests;
        private int _timeoutTests;
        private int _exceptionTests;
        private int _interestingInputs;
        private long _totalExecutionTime;
        private long _totalInputSize;
        private readonly Dictionary<string, int> _exceptionTypes = new();

        /// <summary>
        /// Initializes a new instance of the ConsoleReporter class
        /// </summary>
        /// <param name="verbose">Whether to report verbose information</param>
        public ConsoleReporter(bool verbose = false)
        {
            _verbose = verbose;
        }

        /// <summary>
        /// Reports a test result
        /// </summary>
        /// <param name="result">The test result to report</param>
        public void ReportTestResult(TestResult result)
        {
            // Update statistics
            _totalTests++;
            _totalExecutionTime += result.ExecutionTime;
            _totalInputSize += result.InputSize;

            switch (result.Outcome)
            {
                case TestOutcome.Success:
                    _successfulTests++;
                    break;
                case TestOutcome.Timeout:
                    _timeoutTests++;
                    if (_verbose)
                    {
                        Console.WriteLine($"Timeout: Test execution timed out after {result.ExecutionTime}ms");
                    }
                    break;
                case TestOutcome.Exception:
                    _exceptionTests++;
                    if (_verbose)
                    {
                        Console.WriteLine($"Exception: {result.Exception?.GetType().Name}: {result.Exception?.Message}");
                    }

                    // Track exception types
                    if (result.Exception != null)
                    {
                        string exceptionType = result.Exception.GetType().Name;
                        if (!_exceptionTypes.ContainsKey(exceptionType))
                        {
                            _exceptionTypes[exceptionType] = 1;
                        }
                        else
                        {
                            _exceptionTypes[exceptionType]++;
                        }
                    }
                    break;
            }

            // Check if the input is interesting
            if (result.Coverage != null)
            {
                _interestingInputs++;
                if (_verbose)
                {
                    Console.WriteLine($"Interesting input found: {result.InputSize} bytes, execution time: {result.ExecutionTime}ms");
                }
            }
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
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine($"=== Progress Report: Iteration {iteration}/{totalIterations} ===");
                Console.WriteLine($"Tests executed: {testsExecuted}");
                Console.WriteLine($"Interesting inputs: {interestingInputs}");
                Console.WriteLine($"Corpus size: {corpusSize}");
                Console.WriteLine($"Crashes: {crashCount}");
                Console.WriteLine($"Tests per second: {testsPerSecond:F2}");
                Console.WriteLine($"Average execution time: {averageExecutionTime:F2}ms");
                Console.WriteLine($"Average input size: {averageInputSize:F2} bytes");
                Console.WriteLine($"Coverage points: {coverageCount}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Reports a crash
        /// </summary>
        public void ReportCrash(byte[] data, Exception exception)
        {
            lock (_lock)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("=== CRASH DETECTED ===");
                Console.WriteLine($"Exception: {exception.GetType().Name}");
                Console.WriteLine($"Message: {exception.Message}");

                if (_verbose)
                {
                    Console.WriteLine("Stack trace:");
                    Console.WriteLine(exception.StackTrace);

                    Console.WriteLine("Input data (hex):");
                    Console.WriteLine(BitConverter.ToString(data).Replace("-", ""));
                }

                Console.ResetColor();
                Console.WriteLine();
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
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("=== Final Statistics ===");
                Console.WriteLine($"Tests executed: {testsExecuted}");
                Console.WriteLine($"Interesting inputs: {interestingInputs}");
                Console.WriteLine($"Corpus size: {corpusSize}");
                Console.WriteLine($"Crashes: {crashCount}");
                Console.WriteLine($"Coverage points: {coverageCount}");

                if (testsExecuted > 0)
                {
                    double interestingRatio = (double)interestingInputs / testsExecuted * 100;
                    double crashRatio = (double)crashCount / testsExecuted * 100;

                    Console.WriteLine($"Interesting ratio: {interestingRatio:F2}%");
                    Console.WriteLine($"Crash ratio: {crashRatio:F2}%");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Flushes any buffered reports
        /// </summary>
        public void Flush()
        {
            // Nothing to flush for console reporter
        }
    }
}

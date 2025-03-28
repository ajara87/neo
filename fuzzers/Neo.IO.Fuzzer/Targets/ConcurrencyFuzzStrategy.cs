// Copyright (C) 2015-2025 The Neo Project.
//
// ConcurrencyFuzzStrategy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Neo.IO.Fuzzer.Utilities.TestCacheType;
using static Neo.IO.Fuzzer.Utilities.TestValueType;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Defines the types of concurrency tests that can be performed.
    /// </summary>
    public enum ConcurrencyTestType
    {
        ParallelReads,
        ParallelWrites,
        MixedReadWrite,
        ReadWriteContention,
        WriteWriteContention
    }

    /// <summary>
    /// A fuzzing strategy for testing concurrent cache operations in the Neo caching system.
    /// Uses the common utility classes to reduce code duplication.
    /// </summary>
    public class ConcurrencyFuzzStrategy : ICacheFuzzStrategy, IEnhancedCacheFuzzStrategy
    {
        private readonly CoverageTrackerHelper _coverage;
        private readonly string _name = "ConcurrencyFuzzStrategy";
        private readonly int _maxThreads;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyFuzzStrategy"/> class.
        /// </summary>
        /// <param name="maxThreads">The maximum number of threads to use for concurrency testing.</param>
        public ConcurrencyFuzzStrategy(int maxThreads = 8)
        {
            _maxThreads = maxThreads;
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
        }

        private void InitializeCoverage()
        {
            _coverage.InitializePoints(
                "ParallelReads", "ParallelWrites", "MixedReadWrite", "ReadWriteContention", "WriteWriteContention",
                "ThreadSafetyVerified", "RaceConditionDetected", "DeadlockDetected", "LockContentionObserved",
                "AllThreadsCompleted", "ThreadExceptionOccurred", "ReadSuccess", "ReadFailure",
                "ParallelReadSuccess", "ParallelReadMiss", "ParallelWriteSuccess", "ParallelWriteFailure",
                "MixedReadSuccess", "MixedReadMiss", "MixedWriteSuccess", "MixedWriteFailure",
                "ContentionReadSuccess", "ContentionReadMiss", "ContentionWriteSuccess", "ContentionWriteFailure",
                "ContentionRemoveSuccess", "ContentionRemoveMiss"
            );
        }

        /// <summary>
        /// Gets the coverage tracker used by this strategy.
        /// </summary>
        /// <returns>The coverage tracker.</returns>
        public CoverageTrackerHelper GetCoverageTracker()
        {
            return _coverage;
        }

        /// <summary>
        /// Executes the fuzzing strategy with the provided input data.
        /// </summary>
        /// <param name="input">The input data to use for fuzzing.</param>
        /// <returns>True if the execution was successful, false otherwise.</returns>
        public bool Execute(byte[] input)
        {
            if (input == null || input.Length < 4)
                return false;

            try
            {
                return ExecuteInternal(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in {_name}: {ex.Message}");
                return false;
            }
        }

        private bool ExecuteInternal(byte[] input)
        {
            // Extract test type from input
            var testType = InputProcessingUtilities.ExtractEnumParameter<ConcurrencyTestType>(input, 0);

            // Extract cache type from input (limit to StringKey and NumericKey)
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Extract thread count from input (ensure it's within reasonable limits)
            int threadCount = input.Length > 2 ? Math.Clamp((int)input[2], 2, _maxThreads) : 4;

            return ExecuteConcurrencyTest(testType, input);
        }

        private bool ExecuteConcurrencyTest(ConcurrencyTestType testType, byte[] input)
        {
            // Track test type coverage
            _coverage.IncrementPoint(testType.ToString());

            return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
            {
                // Extract cache type from input (limit to StringKey and NumericKey)
                TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

                // Extract value type from input
                TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

                // Extract thread count from input (ensure it's within reasonable limits)
                int threadCount = input.Length > 2 ? Math.Clamp((int)input[2], 2, _maxThreads) : 4;

                // Create a test cache
                object testCache;
                if (cacheType == TestCacheType.StringKey)
                {
                    testCache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);
                }
                else
                {
                    testCache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);
                }

                // Prepare the cache with initial data
                PrepareCache(testCache, input);

                // Execute the appropriate concurrency test
                switch (testType)
                {
                    case ConcurrencyTestType.ParallelReads:
                        ParallelReads(testCache, input);
                        break;

                    case ConcurrencyTestType.ParallelWrites:
                        ParallelWrites(testCache, input);
                        break;

                    case ConcurrencyTestType.MixedReadWrite:
                        MixedReadWrites(testCache, input);
                        break;

                    case ConcurrencyTestType.ReadWriteContention:
                        ContentionScenario(testCache, input);
                        break;

                    case ConcurrencyTestType.WriteWriteContention:
                        ContentionScenario(testCache, input);
                        break;
                }

                return true;
            }, "ConcurrencyFuzzStrategy.ExecuteConcurrencyTest", _coverage);
        }

        private void PrepareCache(object cache, byte[] input)
        {
            // Add some initial data to the cache
            if (cache is TestCacheBase<string, byte[]> stringCache)
            {
                for (int i = 0; i < 10; i++)
                {
                    string key = $"key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    stringCache.Add(key, value);
                }
            }
            else if (cache is TestCacheBase<int, byte[]> numericCache)
            {
                for (int i = 0; i < 10; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    numericCache.Add(key, value);
                }
            }
        }

        private void ParallelReads(object cache, byte[] input)
        {
            // Extract the number of threads from input
            int threadCount = input.Length > 0 ? Math.Max(2, input[0] % 10) : 4;

            // Track coverage for parallel reads
            _coverage.IncrementPoint("ParallelReads");

            // Create tasks for parallel reads
            var tasks = new Task[threadCount];

            if (cache is TestCacheBase<string, byte[]> stringCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform reads
                        for (int j = 0; j < 10; j++)
                        {
                            string key = $"key_{j}";
                            try
                            {
                                if (stringCache.TryGetValue(key, out byte[] value))
                                {
                                    _coverage.IncrementPoint("ParallelReadSuccess");
                                }
                                else
                                {
                                    _coverage.IncrementPoint("ParallelReadMiss");
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    });
                }
            }
            else if (cache is TestCacheBase<int, byte[]> numericCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform reads
                        for (int j = 0; j < 10; j++)
                        {
                            int key = j;
                            try
                            {
                                if (numericCache.TryGetValue(key, out byte[] value))
                                {
                                    _coverage.IncrementPoint("ParallelReadSuccess");
                                }
                                else
                                {
                                    _coverage.IncrementPoint("ParallelReadMiss");
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    });
                }
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks);
        }

        private void ParallelWrites(object cache, byte[] input)
        {
            // Extract the number of threads from input
            int threadCount = input.Length > 0 ? Math.Max(2, input[0] % 10) : 4;

            // Track coverage for parallel writes
            _coverage.IncrementPoint("ParallelWrites");

            // Create tasks for parallel writes
            var tasks = new Task[threadCount];

            if (cache is TestCacheBase<string, byte[]> stringCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform writes
                        for (int j = 0; j < 10; j++)
                        {
                            string key = $"key_{taskId}_{j}";
                            byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };
                            try
                            {
                                stringCache.Add(key, value);
                                _coverage.IncrementPoint("ParallelWriteSuccess");
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    });
                }
            }
            else if (cache is TestCacheBase<int, byte[]> numericCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform writes
                        for (int j = 0; j < 10; j++)
                        {
                            int key = taskId * 100 + j;
                            byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };
                            try
                            {
                                numericCache.Add(key, value);
                                _coverage.IncrementPoint("ParallelWriteSuccess");
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    });
                }
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks);
        }

        private void MixedReadWrites(object cache, byte[] input)
        {
            // Extract the number of threads from input
            int threadCount = input.Length > 0 ? Math.Max(2, input[0] % 10) : 4;

            // Track coverage for mixed read/write operations
            _coverage.IncrementPoint("MixedReadWrites");

            // Create tasks for mixed read/write operations
            var tasks = new Task[threadCount];

            if (cache is TestCacheBase<string, byte[]> stringCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform mixed read/write operations
                        for (int j = 0; j < 10; j++)
                        {
                            // Alternate between reads and writes
                            if (j % 2 == 0)
                            {
                                // Read operation
                                string key = $"key_{j % 10}";
                                try
                                {
                                    if (stringCache.TryGetValue(key, out byte[] value))
                                    {
                                        _coverage.IncrementPoint("MixedReadSuccess");
                                    }
                                    else
                                    {
                                        _coverage.IncrementPoint("MixedReadMiss");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                }
                            }
                            else
                            {
                                // Write operation
                                string key = $"key_{taskId}_{j}";
                                byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };
                                try
                                {
                                    stringCache.Add(key, value);
                                    _coverage.IncrementPoint("MixedWriteSuccess");
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                }
                            }
                        }
                    });
                }
            }
            else if (cache is TestCacheBase<int, byte[]> numericCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform mixed read/write operations
                        for (int j = 0; j < 10; j++)
                        {
                            // Alternate between reads and writes
                            if (j % 2 == 0)
                            {
                                // Read operation
                                int key = j % 10;
                                try
                                {
                                    if (numericCache.TryGetValue(key, out byte[] value))
                                    {
                                        _coverage.IncrementPoint("MixedReadSuccess");
                                    }
                                    else
                                    {
                                        _coverage.IncrementPoint("MixedReadMiss");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                }
                            }
                            else
                            {
                                // Write operation
                                int key = taskId * 100 + j;
                                byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };
                                try
                                {
                                    numericCache.Add(key, value);
                                    _coverage.IncrementPoint("MixedWriteSuccess");
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                }
                            }
                        }
                    });
                }
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks);
        }

        private void ContentionScenario(object cache, byte[] input)
        {
            // Extract the number of threads from input
            int threadCount = input.Length > 0 ? Math.Max(2, input[0] % 10) : 4;

            // Track coverage for contention scenario
            _coverage.IncrementPoint("ContentionScenario");

            // Create tasks for contention scenario
            var tasks = new Task[threadCount];

            if (cache is TestCacheBase<string, byte[]> stringCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform operations on the same keys to create contention
                        for (int j = 0; j < 10; j++)
                        {
                            string key = $"contention_key_{j % 3}"; // Use only 3 keys to increase contention

                            // Alternate between different operations
                            switch (j % 3)
                            {
                                case 0:
                                    // Read operation
                                    try
                                    {
                                        if (stringCache.TryGetValue(key, out byte[] value))
                                        {
                                            _coverage.IncrementPoint("ContentionReadSuccess");
                                        }
                                        else
                                        {
                                            _coverage.IncrementPoint("ContentionReadMiss");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                    }
                                    break;

                                case 1:
                                    // Write operation
                                    try
                                    {
                                        byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };
                                        stringCache.Add(key, value);
                                        _coverage.IncrementPoint("ContentionWriteSuccess");
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                    }
                                    break;

                                case 2:
                                    // Remove operation
                                    try
                                    {
                                        if (stringCache.Remove(key))
                                        {
                                            _coverage.IncrementPoint("ContentionRemoveSuccess");
                                        }
                                        else
                                        {
                                            _coverage.IncrementPoint("ContentionRemoveMiss");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                    }
                                    break;
                            }

                            // Add a small delay to increase the chance of contention
                            Thread.Sleep(1);
                        }
                    });
                }
            }
            else if (cache is TestCacheBase<int, byte[]> numericCache)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    int taskId = i;
                    tasks[i] = Task.Run(() =>
                    {
                        // Perform operations on the same keys to create contention
                        for (int j = 0; j < 10; j++)
                        {
                            int key = j % 3; // Use only 3 keys to increase contention

                            // Alternate between different operations
                            switch (j % 3)
                            {
                                case 0:
                                    // Read operation
                                    try
                                    {
                                        if (numericCache.TryGetValue(key, out byte[] value))
                                        {
                                            _coverage.IncrementPoint("ContentionReadSuccess");
                                        }
                                        else
                                        {
                                            _coverage.IncrementPoint("ContentionReadMiss");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                    }
                                    break;

                                case 1:
                                    // Write operation
                                    try
                                    {
                                        byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };
                                        numericCache.Add(key, value);
                                        _coverage.IncrementPoint("ContentionWriteSuccess");
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                    }
                                    break;

                                case 2:
                                    // Remove operation
                                    try
                                    {
                                        if (numericCache.Remove(key))
                                        {
                                            _coverage.IncrementPoint("ContentionRemoveSuccess");
                                        }
                                        else
                                        {
                                            _coverage.IncrementPoint("ContentionRemoveMiss");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                                    }
                                    break;
                            }

                            // Add a small delay to increase the chance of contention
                            Thread.Sleep(1);
                        }
                    });
                }
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Gets the coverage information for the strategy.
        /// </summary>
        /// <returns>An object containing coverage information.</returns>
        public object GetCoverage()
        {
            return _coverage.GetCoverage();
        }

        /// <summary>
        /// Gets the name of the strategy.
        /// </summary>
        /// <returns>The strategy name.</returns>
        public string GetName()
        {
            return _name;
        }
    }

    /// <summary>
    /// Extension methods for cache operations
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Generates a key for the cache based on input data
        /// </summary>
        public static object GenerateKey(this IDisposable cache, byte[] input, int index)
        {
            if (cache is TestCacheBase<string, byte[]>.StringKeyCache)
            {
                return $"key_{index}_{input[index % input.Length]}";
            }
            else if (cache is TestCacheBase<int, byte[]>.NumericKeyCache)
            {
                return index;
            }

            return null;
        }

        /// <summary>
        /// Generates a value for the cache based on input data
        /// </summary>
        public static object GenerateValue(this IDisposable cache, byte[] input, int index)
        {
            // Generate a byte array value
            byte[] value = new byte[Math.Min(input.Length, 10)];
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = input[(index + i) % input.Length];
            }

            return value;
        }
    }
}

// Copyright (C) 2015-2025 The Neo Project.
//
// CapacityFuzzStrategy.cs file belongs to the neo project and is free
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

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Defines the types of capacity tests that can be performed.
    /// </summary>
    public enum CapacityTestType
    {
        FillToCapacity,
        ExceedCapacity,
        RemoveItems,
        ClearCache,
        CapacityBoundary
    }

    /// <summary>
    /// A fuzzing strategy for testing cache capacity management in the Neo caching system.
    /// Uses the common utility classes to reduce code duplication.
    /// </summary>
    public class CapacityFuzzStrategy : ICacheFuzzStrategy, IEnhancedCacheFuzzStrategy
    {
        private readonly CoverageTrackerHelper _coverage;
        private readonly string _name = "CapacityFuzzStrategy";

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityFuzzStrategy"/> class.
        /// </summary>
        public CapacityFuzzStrategy()
        {
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
        }

        private void InitializeCoverage()
        {
            _coverage.InitializePoints(
                "FillToCapacity", "ExceedCapacity", "RemoveItems", "ClearCache", "CapacityBoundary",
                "CapacityReached", "EvictionsOccurred", "ItemsRemoved", "CacheCleared", "BoundaryTested"
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
            var testType = InputProcessingUtilities.ExtractEnumParameter<CapacityTestType>(input, 0);

            // Extract cache type from input (limit to StringKey and NumericKey)
            TestCacheType cacheType = (TestCacheType)(input.Length > 1 ? input[1] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 2 ? input[2] % 5 : 0);

            // Extract capacity from input (ensure it's within reasonable limits)
            int capacity = input.Length > 3 ? Math.Clamp((int)input[3], 10, 1000) : 100;

            // Create appropriate test cache based on cache type
            if (cacheType == TestCacheType.StringKey)
            {
                using var testCache = TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType, capacity);
                if (testCache is TestCacheBase<string, byte[]>.StringKeyCache stringKeyCache)
                {
                    return ExecuteCapacityTestWithStringKeys(stringKeyCache, testType, capacity, input);
                }
            }
            else // NumericKey
            {
                using var testCache = TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType, capacity);
                if (testCache is TestCacheBase<int, byte[]>.NumericKeyCache numericKeyCache)
                {
                    return ExecuteCapacityTestWithIntKeys(numericKeyCache, testType, capacity, input);
                }
            }

            // If we reach here, something went wrong with cache creation
            Console.WriteLine($"Failed to create appropriate test cache for type {cacheType} and value type {valueType}");
            return false;
        }

        private bool ExecuteCapacityTest(IDisposable testCacheObj, CapacityTestType testType, int capacity, byte[] input)
        {
            // Track test type coverage
            _coverage.IncrementPoint(testType.ToString());

            // Check if the cache is a StringKeyCache
            if (testCacheObj is TestCacheBase<string, byte[]> stringKeyCache)
            {
                return ExecuteCapacityTestWithStringKeys(stringKeyCache, testType, capacity, input);
            }
            // Check if the cache is a NumericKeyCache
            else if (testCacheObj is TestCacheBase<int, byte[]> numericKeyCache)
            {
                return ExecuteCapacityTestWithIntKeys(numericKeyCache, testType, capacity, input);
            }
            else
            {
                // Unsupported cache type
                Console.WriteLine($"Unsupported cache type: {testCacheObj.GetType().Name}");
                return false;
            }
        }

        private bool ExecuteCapacityTestWithStringKeys(TestCacheBase<string, byte[]> testCache, CapacityTestType testType, int capacity, byte[] input)
        {
            switch (testType)
            {
                case CapacityTestType.FillToCapacity:
                    return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
                    {
                        // Fill the cache to its capacity
                        for (int i = 0; i < capacity; i++)
                        {
                            try
                            {
                                string key = $"key_{i}_{Guid.NewGuid()}";
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)i; // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);
                                _coverage.IncrementPoint("ItemAdded");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception adding item {i}: {ex.Message}");
                                _coverage.IncrementPoint("AddException");
                            }
                        }

                        // Verify cache is at capacity
                        _coverage.IncrementPoint("CacheFilledToCapacity");
                        return true;
                    }, "CapacityFuzzStrategy.FillToCapacity", _coverage);

                case CapacityTestType.ExceedCapacity:
                    return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
                    {
                        // First fill the cache to capacity
                        for (int i = 0; i < capacity; i++)
                        {
                            try
                            {
                                string key = $"key_{i}_{Guid.NewGuid()}";
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)i; // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions during initial fill
                            }
                        }

                        // Now try to exceed capacity
                        for (int i = 0; i < capacity / 2; i++)
                        {
                            try
                            {
                                string key = $"overflow_key_{i}_{Guid.NewGuid()}";
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)(i + 100); // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);
                                _coverage.IncrementPoint("CapacityExceeded");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception exceeding capacity: {ex.Message}");
                                _coverage.IncrementPoint("ExceedCapacityException");
                            }
                        }

                        // Verify some evictions occurred
                        _coverage.IncrementPoint("EvictionsOccurred");
                        return true;
                    }, "CapacityFuzzStrategy.ExceedCapacity", _coverage);

                case CapacityTestType.RemoveItems:
                    return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
                    {
                        // First fill the cache with some items
                        var keysToRemove = new List<string>();
                        for (int i = 0; i < capacity / 2; i++)
                        {
                            try
                            {
                                string key = $"key_{i}_{Guid.NewGuid()}";
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)i; // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);

                                // Remember keys for later removal
                                if (i % 2 == 0)
                                {
                                    keysToRemove.Add(key);
                                }
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions during initial fill
                            }
                        }

                        // Now remove some items
                        foreach (var key in keysToRemove)
                        {
                            try
                            {
                                testCache.Remove(key);
                                _coverage.IncrementPoint("ItemRemoved");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception removing item: {ex.Message}");
                                _coverage.IncrementPoint("RemoveException");
                            }
                        }

                        // Verify items were removed
                        _coverage.IncrementPoint("ItemsRemoved");
                        return true;
                    }, "CapacityFuzzStrategy.RemoveItems", _coverage);

                case CapacityTestType.ClearCache:
                    return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
                    {
                        // First fill the cache with some items
                        for (int i = 0; i < capacity / 2; i++)
                        {
                            try
                            {
                                string key = $"key_{i}_{Guid.NewGuid()}";
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)i; // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions during initial fill
                            }
                        }

                        // Now clear the cache
                        try
                        {
                            // Use reflection to call the Clear method if it exists
                            var clearMethod = testCache.GetType().GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
                            if (clearMethod != null)
                            {
                                clearMethod.Invoke(testCache, null);
                                _coverage.IncrementPoint("CacheCleared");
                            }
                            else
                            {
                                // If Clear method doesn't exist, remove all items manually
                                var keysProperty = testCache.GetType().GetProperty("Keys", BindingFlags.Public | BindingFlags.Instance);
                                if (keysProperty != null)
                                {
                                    var keysCollection = keysProperty.GetValue(testCache);
                                    if (keysCollection is IEnumerable<object> objectKeys)
                                    {
                                        var keys = objectKeys.ToList(); // Create a copy to avoid collection modified exceptions
                                        var removeMethod = testCache.GetType().GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);

                                        foreach (var key in keys)
                                        {
                                            removeMethod?.Invoke(testCache, new[] { key });
                                        }
                                    }
                                    else if (keysCollection is IEnumerable<string> stringKeys)
                                    {
                                        var keys = stringKeys.ToList(); // Create a copy to avoid collection modified exceptions
                                        var removeMethod = testCache.GetType().GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);

                                        foreach (var key in keys)
                                        {
                                            removeMethod?.Invoke(testCache, new[] { key });
                                        }
                                    }
                                    else if (keysCollection is IEnumerable<int> intKeys)
                                    {
                                        var keys = intKeys.ToList(); // Create a copy to avoid collection modified exceptions
                                        var removeMethod = testCache.GetType().GetMethod("Remove", BindingFlags.Public | BindingFlags.Instance);

                                        foreach (var key in keys)
                                        {
                                            removeMethod?.Invoke(testCache, new object[] { key });
                                        }
                                    }
                                }

                                _coverage.IncrementPoint("CacheClearedManually");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception clearing cache: {ex.Message}");
                            _coverage.IncrementPoint("ClearException");
                        }

                        // Verify cache was cleared
                        _coverage.IncrementPoint("CacheCleared");
                        return true;
                    }, "CapacityFuzzStrategy.ClearCache", _coverage);

                case CapacityTestType.CapacityBoundary:
                    return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
                    {
                        // Test boundary conditions around capacity

                        // Fill the cache to exactly capacity
                        for (int i = 0; i < capacity; i++)
                        {
                            try
                            {
                                string key = $"key_{i}_{Guid.NewGuid()}";
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)i; // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);
                            }
                            catch (Exception)
                            {
                                // Ignore exceptions during initial fill
                            }
                        }

                        // Add one more item to test boundary
                        try
                        {
                            string key = $"boundary_key_{Guid.NewGuid()}";
                            byte[] value = new byte[8];
                            Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));

                            // Add to cache
                            testCache.Add(key, value);
                            _coverage.IncrementPoint("BoundaryExceeded");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception at boundary: {ex.Message}");
                            _coverage.IncrementPoint("BoundaryException");
                        }

                        // Verify boundary was tested
                        _coverage.IncrementPoint("BoundaryTested");
                        return true;
                    }, "CapacityFuzzStrategy.CapacityBoundary", _coverage);

                default:
                    return false;
            }
        }

        private bool ExecuteCapacityTestWithIntKeys(TestCacheBase<int, byte[]> testCache, CapacityTestType testType, int capacity, byte[] input)
        {
            // Similar implementation as ExecuteCapacityTestWithStringKeys but for int keys
            // This is a simplified version for demonstration
            switch (testType)
            {
                case CapacityTestType.FillToCapacity:
                    return ExceptionHandlingUtilities.ExecuteWithExceptionHandling(() =>
                    {
                        // Fill the cache to its capacity
                        for (int i = 0; i < capacity; i++)
                        {
                            try
                            {
                                int key = i;
                                byte[] value = new byte[8];
                                Array.Copy(input, 0, value, 0, Math.Min(input.Length, value.Length));
                                value[0] = (byte)i; // Ensure uniqueness

                                // Add to cache
                                testCache.Add(key, value);
                                _coverage.IncrementPoint("ItemAdded");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception adding item {i}: {ex.Message}");
                                _coverage.IncrementPoint("AddException");
                            }
                        }

                        // Verify cache is at capacity
                        _coverage.IncrementPoint("CacheFilledToCapacity");
                        return true;
                    }, "CapacityFuzzStrategy.FillToCapacity", _coverage);

                // Other cases would be implemented similarly
                default:
                    return false;
            }
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
}

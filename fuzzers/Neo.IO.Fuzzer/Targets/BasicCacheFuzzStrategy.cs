// Copyright (C) 2015-2025 The Neo Project.
//
// BasicCacheFuzzStrategy.cs file belongs to the neo project and is free
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
    /// Defines the types of cache operations that can be performed.
    /// </summary>
    public enum CacheOperation
    {
        /// <summary>
        /// Add an item to the cache.
        /// </summary>
        Add,

        /// <summary>
        /// Remove an item from the cache.
        /// </summary>
        Remove,

        /// <summary>
        /// Check if the cache contains a key.
        /// </summary>
        Contains,

        /// <summary>
        /// Access an item in the cache.
        /// </summary>
        Access,

        /// <summary>
        /// Clear all items from the cache.
        /// </summary>
        Clear,

        /// <summary>
        /// Enumerate all items in the cache.
        /// </summary>
        Enumeration
    }

    /// <summary>
    /// Defines the types of cache tests that can be performed.
    /// </summary>
    public enum CacheTestType
    {
        /// <summary>
        /// Tests basic cache operations like add, get, remove.
        /// </summary>
        BasicOperations,

        /// <summary>
        /// Tests cache capacity management.
        /// </summary>
        Capacity,

        /// <summary>
        /// Tests cache eviction policy.
        /// </summary>
        Eviction,

        /// <summary>
        /// Tests thread safety of the cache.
        /// </summary>
        ThreadSafety,

        /// <summary>
        /// Tests error handling in the cache.
        /// </summary>
        ErrorHandling,

        /// <summary>
        /// Tests cache expiration.
        /// </summary>
        Expiration
    }

    /// <summary>
    /// A disposable test object for cache testing
    /// </summary>
    public class DisposableTestObject : IDisposable
    {
        /// <summary>
        /// Gets or sets the ID of the object
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the data of the object
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets a value indicating whether the object has been disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableTestObject"/> class.
        /// </summary>
        public DisposableTestObject(int id, byte[] data)
        {
            Id = id;
            Data = data;
            IsDisposed = false;
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    /// <summary>
    /// A fuzzing strategy for testing basic cache operations in the Neo caching system.
    /// Uses the common utility classes to reduce code duplication.
    /// </summary>
    public class BasicCacheFuzzStrategy : ICacheFuzzStrategy, IEnhancedCacheFuzzStrategy
    {
        private readonly CoverageTrackerHelper _coverage;
        private readonly string _name = "BasicCacheFuzzStrategy";
        private readonly int _maxCapacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicCacheFuzzStrategy"/> class.
        /// </summary>
        /// <param name="maxCapacity">The maximum capacity for test caches.</param>
        public BasicCacheFuzzStrategy(int maxCapacity = 100)
        {
            _maxCapacity = maxCapacity;
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
        }

        private void InitializeCoverage()
        {
            // Initialize coverage tracking for basic operations
            _coverage.InitializePoints(
                "Add",
                "Remove",
                "Contains",
                "Access",
                "Clear",
                "Enumeration",
                "EmptyState",
                "WithItemsState",
                "SuccessfulRetries",
                "RetryAttempts",
                "RetryFailures",
                "CacheTypeHandling",
                "StringKeyHandling",
                "NumericKeyHandling",
                "AddOperation",
                "RemoveOperation",
                "ContainsOperation",
                "AccessOperation",
                "ClearOperation",
                "EnumerationOperation",
                "ContainsSuccess",
                "AccessSuccess",
                "ClearSuccess",
                "EnumerationSuccess",
                "TryGetValueSuccess",
                "TryGetValueFailure",
                "ContainsKeySuccess",
                "ContainsKeyFailure",
                "RemoveSuccess",
                "RemoveFailure",
                "CapacityAdd",
                "CapacityMaintained",
                "CapacityExceeded",
                "EvictionAccess",
                "EvictionAdd",
                "EvictionTrigger",
                "AccessedItemRetained",
                "UnAccessedItemRetained",
                "AccessedItemEvicted",
                "UnAccessedItemEvicted",
                "ThreadSafetyAdd",
                "ThreadSafetyGet",
                "AddSuccess",
                "AddFailure",
                "GetSuccess",
                "GetFailure",
                "ContainsKeySuccess",
                "ContainsKeyFailure",
                "RemoveSuccess",
                "RemoveFailure",
                "ExpirationAdd",
                "ExpirationBeforeGet",
                "ExpirationAfterGet",
                "ExpirationExpired",
                "DuplicateKeyNoError",
                "DuplicateKeyError",
                "NonExistentKeyFound",
                "NonExistentKeyNotFound",
                "RemoveNonExistentSuccess",
                "RemoveNonExistentFailure",
                "NullKeyNoError",
                "NullKeyError",
                "NullValueNoError",
                "NullValueError",
                "CountInRange",
                "CountOutOfRange",
                "DisposedOnRemove",
                "NotDisposedOnRemove"
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
            return TestCacheOperations(input);
        }

        private bool TestCacheOperations(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Extract test type from input
            CacheTestType testType = (CacheTestType)(input.Length > 2 ? input[2] % 6 : 0);

            // Execute the test based on the test type
            switch (testType)
            {
                case CacheTestType.BasicOperations:
                    return TestBasicCacheOperations(input);

                case CacheTestType.Capacity:
                    return TestCacheCapacity(input);

                case CacheTestType.Eviction:
                    return TestCacheEviction(input);

                case CacheTestType.ThreadSafety:
                    return TestCacheThreadSafety(input);

                case CacheTestType.ErrorHandling:
                    return TestErrorHandling(input);

                case CacheTestType.Expiration:
                    return TestCacheExpiration(input);

                default:
                    return TestBasicCacheOperations(input);
            }
        }

        private bool TestCacheCapacity(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Extract capacity from input
            int capacity = input.Length > 2 ? Math.Max(5, input[2] % 20) : 10;

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType, capacity);

                // Add items until capacity is exceeded
                for (int i = 0; i < capacity * 2; i++)
                {
                    string key = $"capacity_key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("CapacityAdd");

                        // Check if the cache size is maintained
                        if (i >= capacity && cache.Count <= capacity)
                        {
                            _coverage.IncrementPoint("CapacityMaintained");
                        }
                        else if (i < capacity && cache.Count > capacity)
                        {
                            _coverage.IncrementPoint("CapacityExceeded");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType, capacity);

                // Add items until capacity is exceeded
                for (int i = 0; i < capacity * 2; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("CapacityAdd");

                        // Check if the cache size is maintained
                        if (i >= capacity && cache.Count <= capacity)
                        {
                            _coverage.IncrementPoint("CapacityMaintained");
                        }
                        else if (i < capacity && cache.Count > capacity)
                        {
                            _coverage.IncrementPoint("CapacityExceeded");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        private bool TestCacheEviction(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Extract capacity from input
            int capacity = input.Length > 2 ? Math.Max(5, input[2] % 20) : 10;

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType, capacity);

                // Add items to fill the cache
                for (int i = 0; i < capacity; i++)
                {
                    string key = $"eviction_key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("EvictionAdd");
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Access some items to change their access order
                for (int i = 0; i < capacity / 2; i++)
                {
                    string key = $"eviction_key_{i}";

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("EvictionAccess");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Add more items to trigger eviction
                for (int i = capacity; i < capacity * 2; i++)
                {
                    string key = $"eviction_key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("EvictionTrigger");
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Check if the accessed items are still in the cache
                for (int i = 0; i < capacity; i++)
                {
                    string key = $"eviction_key_{i}";

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            if (i < capacity / 2)
                            {
                                _coverage.IncrementPoint("AccessedItemRetained");
                            }
                            else
                            {
                                _coverage.IncrementPoint("UnAccessedItemRetained");
                            }
                        }
                        else
                        {
                            if (i < capacity / 2)
                            {
                                _coverage.IncrementPoint("AccessedItemEvicted");
                            }
                            else
                            {
                                _coverage.IncrementPoint("UnAccessedItemEvicted");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType, capacity);

                // Add items to fill the cache
                for (int i = 0; i < capacity; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("EvictionAdd");
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Access some items to change their access order
                for (int i = 0; i < capacity / 2; i++)
                {
                    int key = i;

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("EvictionAccess");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Add more items to trigger eviction
                for (int i = capacity; i < capacity * 2; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("EvictionTrigger");
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Check if the accessed items are still in the cache
                for (int i = 0; i < capacity; i++)
                {
                    int key = i;

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            if (i < capacity / 2)
                            {
                                _coverage.IncrementPoint("AccessedItemRetained");
                            }
                            else
                            {
                                _coverage.IncrementPoint("UnAccessedItemRetained");
                            }
                        }
                        else
                        {
                            if (i < capacity / 2)
                            {
                                _coverage.IncrementPoint("AccessedItemEvicted");
                            }
                            else
                            {
                                _coverage.IncrementPoint("UnAccessedItemEvicted");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        private bool TestCacheThreadSafety(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Create a test cache
            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Test concurrent access to the cache
                var tasks = new List<Task>();

                // Add tasks for adding items
                for (int i = 0; i < 5; i++)
                {
                    int taskId = i;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            try
                            {
                                string key = $"thread_{taskId}_key_{j}";
                                byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };

                                cache.Add(key, value);
                                _coverage.IncrementPoint("ThreadSafetyAdd");
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    }));
                }

                // Add tasks for retrieving items
                for (int i = 0; i < 5; i++)
                {
                    int taskId = i;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            try
                            {
                                string key = $"thread_{taskId}_key_{j}";
                                if (cache.TryGetValue(key, out _))
                                {
                                    _coverage.IncrementPoint("ThreadSafetyGet");
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    }));
                }

                // Wait for all tasks to complete
                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Test concurrent access to the cache
                var tasks = new List<Task>();

                // Add tasks for adding items
                for (int i = 0; i < 5; i++)
                {
                    int taskId = i;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            try
                            {
                                int key = taskId * 100 + j;
                                byte[] value = new byte[] { (byte)j, (byte)(j >> 8), (byte)(j >> 16) };

                                cache.Add(key, value);
                                _coverage.IncrementPoint("ThreadSafetyAdd");
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    }));
                }

                // Add tasks for retrieving items
                for (int i = 0; i < 5; i++)
                {
                    int taskId = i;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            try
                            {
                                int key = taskId * 100 + j;
                                if (cache.TryGetValue(key, out _))
                                {
                                    _coverage.IncrementPoint("ThreadSafetyGet");
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandlingUtilities.TrackException(ex, _coverage);
                            }
                        }
                    }));
                }

                // Wait for all tasks to complete
                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        private bool TestCacheExpiration(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Extract expiration time from input
            int expirationMs = input.Length > 2 ? Math.Max(10, input[2] * 10) : 100;

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Add items with expiration
                for (int i = 0; i < 5; i++)
                {
                    string key = $"expiration_key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        // Use reflection to call the Add method with expiration
                        var addMethod = cache.GetType().GetMethod("Add", new[] { typeof(string), typeof(byte[]), typeof(TimeSpan) });
                        if (addMethod != null)
                        {
                            addMethod.Invoke(cache, new object[] { key, value, TimeSpan.FromMilliseconds(expirationMs) });
                        }
                        else
                        {
                            // Fallback to regular Add
                            cache.Add(key, value);
                        }
                        _coverage.IncrementPoint("ExpirationAdd");
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Verify items are in the cache
                for (int i = 0; i < 5; i++)
                {
                    string key = $"expiration_key_{i}";

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("ExpirationBeforeGet");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Wait for items to expire
                Thread.Sleep(expirationMs * 2);

                // Verify items are expired
                for (int i = 0; i < 5; i++)
                {
                    string key = $"expiration_key_{i}";

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("ExpirationAfterGet");
                        }
                        else
                        {
                            _coverage.IncrementPoint("ExpirationExpired");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Add items with expiration
                for (int i = 0; i < 5; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        // Use reflection to call the Add method with expiration
                        var addMethod = cache.GetType().GetMethod("Add", new[] { typeof(int), typeof(byte[]), typeof(TimeSpan) });
                        if (addMethod != null)
                        {
                            addMethod.Invoke(cache, new object[] { key, value, TimeSpan.FromMilliseconds(expirationMs) });
                        }
                        else
                        {
                            // Fallback to regular Add
                            cache.Add(key, value);
                        }
                        _coverage.IncrementPoint("ExpirationAdd");
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Verify items are in the cache
                for (int i = 0; i < 5; i++)
                {
                    int key = i;

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("ExpirationBeforeGet");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Wait for items to expire
                Thread.Sleep(expirationMs * 2);

                // Verify items are expired
                for (int i = 0; i < 5; i++)
                {
                    int key = i;

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("ExpirationAfterGet");
                        }
                        else
                        {
                            _coverage.IncrementPoint("ExpirationExpired");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        private bool TestBasicCacheOperations(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Create a test cache
            if (cacheType == TestCacheType.StringKey)
            {
                if (valueType == TestValueType.Disposable)
                {
                    try
                    {
                        using var cache = TestCacheBase<string, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(cacheType, valueType);

                        string key = "test_key";
                        var value = new DisposableTestObject(1, new byte[] { 1, 2, 3 });

                        // Use reflection to safely add the item
                        try
                        {
                            var addMethod = cache.GetType().GetMethod("Add", new[] { typeof(string), typeof(DisposableTestObject) });
                            if (addMethod != null)
                            {
                                addMethod.Invoke(cache, new object[] { key, value });
                                _coverage.IncrementPoint("AddSuccess");
                            }
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }

                        // Use reflection to safely get the item
                        try
                        {
                            var tryGetValueMethod = cache.GetType().GetMethod("TryGetValue", new[] { typeof(string), typeof(DisposableTestObject).MakeByRefType() });
                            if (tryGetValueMethod != null)
                            {
                                var parameters = new object[] { key, null };
                                var result = (bool)tryGetValueMethod.Invoke(cache, parameters);
                                if (result)
                                {
                                    _coverage.IncrementPoint("GetSuccess");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }

                        // Use reflection to safely remove the item
                        try
                        {
                            var removeMethod = cache.GetType().GetMethod("Remove", new[] { typeof(string) });
                            if (removeMethod != null)
                            {
                                var result = (bool)removeMethod.Invoke(cache, new object[] { key });
                                if (result)
                                {
                                    _coverage.IncrementPoint("RemoveSuccess");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }

                        // Check if the value was disposed
                        if (value.IsDisposed)
                        {
                            _coverage.IncrementPoint("DisposedOnRemove");
                        }
                        else
                        {
                            _coverage.IncrementPoint("NotDisposedOnRemove");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }
                else
                {
                    using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                    // Test adding an item
                    try
                    {
                        string key = "test_key";
                        byte[] value = new byte[] { 1, 2, 3 };

                        cache.Add(key, value);
                        _coverage.IncrementPoint("AddSuccess");

                        // Test retrieving an item
                        if (cache.TryGetValue(key, out byte[] retrievedValue))
                        {
                            _coverage.IncrementPoint("RetrieveSuccess");
                        }
                        else
                        {
                            _coverage.IncrementPoint("RetrieveFailure");
                        }

                        // Test removing an item
                        if (cache.Remove(key))
                        {
                            _coverage.IncrementPoint("RemoveSuccess");
                        }
                        else
                        {
                            _coverage.IncrementPoint("RemoveFailure");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }

                    try
                    {
                        // Get the count of items in the cache
                        int count = cache.Count;

                        // Check if the count is within expected range
                        if (count <= 5)
                        {
                            _coverage.IncrementPoint("CountInRange");
                        }
                        else if (count > 5)
                        {
                            _coverage.IncrementPoint("CountOutOfRange");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }
            }
            else
            {
                if (valueType == TestValueType.Disposable)
                {
                    try
                    {
                        using var cache = TestCacheBase<int, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(cacheType, valueType);

                        int key = 42;
                        var value = new DisposableTestObject(1, new byte[] { 1, 2, 3 });

                        // Use reflection to safely add the item
                        try
                        {
                            var addMethod = cache.GetType().GetMethod("Add", new[] { typeof(int), typeof(DisposableTestObject) });
                            if (addMethod != null)
                            {
                                addMethod.Invoke(cache, new object[] { key, value });
                                _coverage.IncrementPoint("AddSuccess");
                            }
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }

                        // Use reflection to safely get the item
                        try
                        {
                            var tryGetValueMethod = cache.GetType().GetMethod("TryGetValue", new[] { typeof(int), typeof(DisposableTestObject).MakeByRefType() });
                            if (tryGetValueMethod != null)
                            {
                                var parameters = new object[] { key, null };
                                var result = (bool)tryGetValueMethod.Invoke(cache, parameters);
                                if (result)
                                {
                                    _coverage.IncrementPoint("GetSuccess");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }

                        // Use reflection to safely remove the item
                        try
                        {
                            var removeMethod = cache.GetType().GetMethod("Remove", new[] { typeof(int) });
                            if (removeMethod != null)
                            {
                                var result = (bool)removeMethod.Invoke(cache, new object[] { key });
                                if (result)
                                {
                                    _coverage.IncrementPoint("RemoveSuccess");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }

                        // Check if the value was disposed
                        if (value.IsDisposed)
                        {
                            _coverage.IncrementPoint("DisposedOnRemove");
                        }
                        else
                        {
                            _coverage.IncrementPoint("NotDisposedOnRemove");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }
                else
                {
                    using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                    // Test adding an item
                    try
                    {
                        int key = 42;
                        byte[] value = new byte[] { 1, 2, 3 };

                        cache.Add(key, value);
                        _coverage.IncrementPoint("AddSuccess");

                        // Test retrieving an item
                        if (cache.TryGetValue(key, out byte[] retrievedValue))
                        {
                            _coverage.IncrementPoint("RetrieveSuccess");
                        }
                        else
                        {
                            _coverage.IncrementPoint("RetrieveFailure");
                        }

                        // Test removing an item
                        if (cache.Remove(key))
                        {
                            _coverage.IncrementPoint("RemoveSuccess");
                        }
                        else
                        {
                            _coverage.IncrementPoint("RemoveFailure");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }

                    try
                    {
                        // Get the count of items in the cache
                        int count = cache.Count;

                        // Check if the count is within expected range
                        if (count <= 5)
                        {
                            _coverage.IncrementPoint("CountInRange");
                        }
                        else if (count > 5)
                        {
                            _coverage.IncrementPoint("CountOutOfRange");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }
            }

            return true;
        }

        private bool TestErrorHandling(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Test adding duplicate keys
                for (int i = 0; i < 3; i++)
                {
                    string key = $"error_key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("AddSuccess");

                        // Try to add the same key again, should throw an exception
                        try
                        {
                            cache.Add(key, value);
                            _coverage.IncrementPoint("DuplicateKeyNoError");
                        }
                        catch (Exception ex)
                        {
                            _coverage.IncrementPoint("DuplicateKeyError");
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Test retrieving non-existent keys
                for (int i = 10; i < 13; i++)
                {
                    string key = $"error_key_{i}";

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("NonExistentKeyFound");
                        }
                        else
                        {
                            _coverage.IncrementPoint("NonExistentKeyNotFound");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Test removing non-existent keys
                for (int i = 20; i < 23; i++)
                {
                    string key = $"error_key_{i}";

                    try
                    {
                        bool removed = cache.Remove(key);
                        if (removed)
                        {
                            _coverage.IncrementPoint("RemoveNonExistentSuccess");
                        }
                        else
                        {
                            _coverage.IncrementPoint("RemoveNonExistentFailure");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Test null key handling
                try
                {
                    string nullKey = null;
                    byte[] value = new byte[] { 0, 1, 2 };

                    cache.Add(nullKey, value);
                    _coverage.IncrementPoint("NullKeyNoError");
                }
                catch (Exception ex)
                {
                    _coverage.IncrementPoint("NullKeyError");
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                // Test null value handling
                try
                {
                    string key = "null_value_key";
                    byte[] nullValue = null;

                    cache.Add(key, nullValue);
                    _coverage.IncrementPoint("NullValueNoError");
                }
                catch (Exception ex)
                {
                    _coverage.IncrementPoint("NullValueError");
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Test adding duplicate keys
                for (int i = 0; i < 3; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };

                    try
                    {
                        cache.Add(key, value);
                        _coverage.IncrementPoint("AddSuccess");

                        // Try to add the same key again, should throw an exception
                        try
                        {
                            cache.Add(key, value);
                            _coverage.IncrementPoint("DuplicateKeyNoError");
                        }
                        catch (Exception ex)
                        {
                            _coverage.IncrementPoint("DuplicateKeyError");
                            ExceptionHandlingUtilities.TrackException(ex, _coverage);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Test retrieving non-existent keys
                for (int i = 10; i < 13; i++)
                {
                    int key = i;

                    try
                    {
                        if (cache.TryGetValue(key, out _))
                        {
                            _coverage.IncrementPoint("NonExistentKeyFound");
                        }
                        else
                        {
                            _coverage.IncrementPoint("NonExistentKeyNotFound");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Test removing non-existent keys
                for (int i = 20; i < 23; i++)
                {
                    int key = i;

                    try
                    {
                        bool removed = cache.Remove(key);
                        if (removed)
                        {
                            _coverage.IncrementPoint("RemoveNonExistentSuccess");
                        }
                        else
                        {
                            _coverage.IncrementPoint("RemoveNonExistentFailure");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandlingUtilities.TrackException(ex, _coverage);
                    }
                }

                // Test null value handling
                try
                {
                    int key = 100;
                    byte[] nullValue = null;

                    cache.Add(key, nullValue);
                    _coverage.IncrementPoint("NullValueNoError");
                }
                catch (Exception ex)
                {
                    _coverage.IncrementPoint("NullValueError");
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                try
                {
                    // Get the count of items in the cache
                    int count = cache.Count;

                    // Check if the count is within expected range
                    if (count <= 5)
                    {
                        _coverage.IncrementPoint("CountInRange");
                    }
                    else if (count > 5)
                    {
                        _coverage.IncrementPoint("CountOutOfRange");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the coverage information for the strategy.
        /// </summary>
        /// <returns>An object containing coverage information.</returns>
        public object GetCoverage()
        {
            return _coverage.GetCoverageObject();
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

public static class TestCacheBaseExtensions
{
    // This extension method is no longer needed since we added a Count property
}

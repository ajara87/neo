// Copyright (C) 2015-2025 The Neo Project.
//
// KeyValueMutationStrategy.cs file belongs to the neo project and is free
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
using static Neo.IO.Fuzzer.Targets.DisposableTestObject;
using static Neo.IO.Fuzzer.Utilities.TestCacheType;
using static Neo.IO.Fuzzer.Utilities.TestValueType;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Defines the types of key-value mutation tests that can be performed.
    /// </summary>
    public enum TestType
    {
        /// <summary>
        /// Tests mutation of keys in the cache.
        /// </summary>
        KeyMutation,

        /// <summary>
        /// Tests mutation of values in the cache.
        /// </summary>
        ValueMutation,

        /// <summary>
        /// Tests mutation of key-value pairs in the cache.
        /// </summary>
        KeyValuePairMutation,

        /// <summary>
        /// Tests handling of null values in the cache.
        /// </summary>
        NullValueHandling,

        /// <summary>
        /// Tests handling of disposable values in the cache.
        /// </summary>
        DisposableValueHandling,

        /// <summary>
        /// Tests handling of duplicate keys in the cache.
        /// </summary>
        DuplicateKeyHandling
    }

    /// <summary>
    /// A fuzzing strategy for testing the handling of various key and value types in the Neo caching system.
    /// Uses the common utility classes to reduce code duplication.
    /// </summary>
    public class KeyValueMutationStrategy : ICacheFuzzStrategy, IEnhancedCacheFuzzStrategy
    {
        private readonly CoverageTrackerHelper _coverage;
        private readonly string _name = "KeyValueMutationStrategy";

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueMutationStrategy"/> class.
        /// </summary>
        public KeyValueMutationStrategy()
        {
            _coverage = new CoverageTrackerHelper(_name);
            InitializeCoverage();
        }

        private void InitializeCoverage()
        {
            _coverage.InitializePoints(
                "KeyMutation", "ValueMutation", "KeyValuePairMutation", "NullValueHandling", "DisposableValueHandling", "DuplicateKeyHandling",
                "StringKeyHandling", "NumericKeyHandling", "GuidKeyHandling", "ComplexKeyHandling",
                "StringValueHandling", "NumericValueHandling", "ByteArrayValueHandling", "ComplexValueHandling", "DisposableValueHandling",
                "CacheHits", "CacheMisses", "Exceptions", "NullValueAllowed", "NullValueRejected",
                "TestExecution", "TestType_KeyMutation", "TestType_ValueMutation", "TestType_KeyValuePairMutation", "TestType_NullValueHandling", "TestType_DuplicateKeyHandling",
                "CacheType_StringKey", "CacheType_NumericKey", "ValueType_String", "ValueType_Integer", "ValueType_ByteArray"
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

            // Extract test type, cache type, and value type from input
            TestType testType = (TestType)(input[0] % 6);
            TestCacheType cacheType = (TestCacheType)(input[1] % 2); // Only use StringKey and NumericKey
            TestValueType valueType = (TestValueType)(input[2] % 5);

            // Track test configuration coverage
            _coverage.IncrementPoint("TestExecution");
            _coverage.IncrementPoint($"TestType_{testType}");
            _coverage.IncrementPoint($"CacheType_{cacheType}");
            _coverage.IncrementPoint($"ValueType_{valueType}");

            // Execute the appropriate test based on test type
            switch (testType)
            {
                case TestType.KeyMutation:
                    return TestKeyMutation(input);

                case TestType.ValueMutation:
                    return TestValueMutation(input);

                case TestType.KeyValuePairMutation:
                    return TestKeyValuePairMutation(input);

                case TestType.NullValueHandling:
                    return TestNullValueHandling(input, cacheType);

                case TestType.DisposableValueHandling:
                    return TestDisposableValueHandling(input, cacheType);

                case TestType.DuplicateKeyHandling:
                    return TestDuplicateKeyHandling(input, cacheType);

                default:
                    // Default to key mutation test
                    return TestKeyMutation(input);
            }
        }

        private bool TestKeyMutation(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Create a test cache with the specified cache type and value type
            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Add some initial items
                for (int i = 0; i < 5; i++)
                {
                    string key = $"key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    cache.Add(key, value);
                }

                // Mutate keys
                for (int i = 0; i < 5; i++)
                {
                    string originalKey = $"key_{i}";
                    string mutatedKey = $"mutated_key_{i}";

                    // Get the value for the original key
                    if (cache.TryGetValue(originalKey, out byte[] value))
                    {
                        // Add the value with the mutated key
                        cache.Add(mutatedKey, value);

                        // Remove the original key
                        cache.Remove(originalKey);

                        _coverage.IncrementPoint("KeyMutationSuccess");
                    }
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Add some initial items
                for (int i = 0; i < 5; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    cache.Add(key, value);
                }

                // Mutate keys
                for (int i = 0; i < 5; i++)
                {
                    int originalKey = i;
                    int mutatedKey = i + 100;

                    // Get the value for the original key
                    if (cache.TryGetValue(originalKey, out byte[] value))
                    {
                        // Add the value with the mutated key
                        cache.Add(mutatedKey, value);

                        // Remove the original key
                        cache.Remove(originalKey);

                        _coverage.IncrementPoint("KeyMutationSuccess");
                    }
                }
            }

            return true;
        }

        private bool TestValueMutation(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Create a test cache with the specified cache type and value type
            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Add some initial items
                for (int i = 0; i < 5; i++)
                {
                    string key = $"key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    cache.Add(key, value);
                }

                // Mutate values
                for (int i = 0; i < 5; i++)
                {
                    string key = $"key_{i}";

                    // Get the value for the key
                    if (cache.TryGetValue(key, out byte[] originalValue))
                    {
                        // Create a mutated value
                        byte[] mutatedValue = new byte[originalValue.Length];
                        Array.Copy(originalValue, mutatedValue, originalValue.Length);

                        // Modify the value
                        for (int j = 0; j < mutatedValue.Length; j++)
                        {
                            mutatedValue[j] = (byte)(mutatedValue[j] ^ 0xFF); // Flip bits
                        }

                        // Update the value
                        cache.Remove(key);
                        cache.Add(key, mutatedValue);

                        _coverage.IncrementPoint("ValueMutationSuccess");
                    }
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Add some initial items
                for (int i = 0; i < 5; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    cache.Add(key, value);
                }

                // Mutate values
                for (int i = 0; i < 5; i++)
                {
                    int key = i;

                    // Get the value for the key
                    if (cache.TryGetValue(key, out byte[] originalValue))
                    {
                        // Create a mutated value
                        byte[] mutatedValue = new byte[originalValue.Length];
                        Array.Copy(originalValue, mutatedValue, originalValue.Length);

                        // Modify the value
                        for (int j = 0; j < mutatedValue.Length; j++)
                        {
                            mutatedValue[j] = (byte)(mutatedValue[j] ^ 0xFF); // Flip bits
                        }

                        // Update the value
                        cache.Remove(key);
                        cache.Add(key, mutatedValue);

                        _coverage.IncrementPoint("ValueMutationSuccess");
                    }
                }
            }

            return true;
        }

        private bool TestKeyValuePairMutation(byte[] input)
        {
            // Extract cache type from input
            TestCacheType cacheType = (TestCacheType)(input.Length > 0 ? input[0] % 2 : 0);

            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            // Create a test cache with the specified cache type and value type
            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Add some initial items
                for (int i = 0; i < 5; i++)
                {
                    string key = $"key_{i}";
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    cache.Add(key, value);
                }

                // Mutate key-value pairs
                for (int i = 0; i < 5; i++)
                {
                    string originalKey = $"key_{i}";
                    string mutatedKey = $"mutated_key_{i}";

                    // Get the value for the original key
                    if (cache.TryGetValue(originalKey, out byte[] originalValue))
                    {
                        // Create a mutated value
                        byte[] mutatedValue = new byte[originalValue.Length];
                        Array.Copy(originalValue, mutatedValue, originalValue.Length);

                        // Modify the value
                        for (int j = 0; j < mutatedValue.Length; j++)
                        {
                            mutatedValue[j] = (byte)(mutatedValue[j] ^ 0xFF); // Flip bits
                        }

                        // Add the mutated key-value pair
                        cache.Add(mutatedKey, mutatedValue);

                        // Remove the original key
                        cache.Remove(originalKey);

                        _coverage.IncrementPoint("KeyValuePairMutationSuccess");
                    }
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Add some initial items
                for (int i = 0; i < 5; i++)
                {
                    int key = i;
                    byte[] value = new byte[] { (byte)i, (byte)(i >> 8), (byte)(i >> 16) };
                    cache.Add(key, value);
                }

                // Mutate key-value pairs
                for (int i = 0; i < 5; i++)
                {
                    int originalKey = i;
                    int mutatedKey = i + 100;

                    // Get the value for the original key
                    if (cache.TryGetValue(originalKey, out byte[] originalValue))
                    {
                        // Create a mutated value
                        byte[] mutatedValue = new byte[originalValue.Length];
                        Array.Copy(originalValue, mutatedValue, originalValue.Length);

                        // Modify the value
                        for (int j = 0; j < mutatedValue.Length; j++)
                        {
                            mutatedValue[j] = (byte)(mutatedValue[j] ^ 0xFF); // Flip bits
                        }

                        // Add the mutated key-value pair
                        cache.Add(mutatedKey, mutatedValue);

                        // Remove the original key
                        cache.Remove(originalKey);

                        _coverage.IncrementPoint("KeyValuePairMutationSuccess");
                    }
                }
            }

            return true;
        }

        private bool TestNullValueHandling(byte[] input, TestCacheType cacheType)
        {
            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Test adding a null value
                try
                {
                    cache.Add("null_value_key", null!);
                    _coverage.IncrementPoint("NullValueAccepted");
                }
                catch (ArgumentNullException)
                {
                    _coverage.IncrementPoint("NullValueRejected");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                // Test adding a null key
                try
                {
                    cache.Add(null!, new byte[] { 1, 2, 3 });
                    _coverage.IncrementPoint("NullKeyAccepted");
                }
                catch (ArgumentNullException)
                {
                    _coverage.IncrementPoint("NullKeyRejected");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Test adding a null value
                try
                {
                    cache.Add(0, null!);
                    _coverage.IncrementPoint("NullValueAccepted");
                }
                catch (ArgumentNullException)
                {
                    _coverage.IncrementPoint("NullValueRejected");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        private bool TestDisposableValueHandling(byte[] input, TestCacheType cacheType)
        {
            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, DisposableTestObject>.StringKeyCache<DisposableTestObject>)TestCacheBase<string, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(cacheType, valueType);

                // Add a disposable value to the cache
                try
                {
                    string key = "disposable_key";
                    var value = new DisposableTestObject(1, new byte[] { 1, 2, 3 });

                    cache.Add(key, value);
                    _coverage.IncrementPoint("DisposableValueAdded");

                    // Verify value is in cache
                    if (cache.TryGetValue(key, out DisposableTestObject retrievedValue))
                    {
                        _coverage.IncrementPoint("DisposableValueRetrieved");
                    }

                    // Remove the value from the cache
                    if (cache.Remove(key))
                    {
                        _coverage.IncrementPoint("DisposableValueRemoved");
                    }

                    // Verify value is disposed
                    if (value.IsDisposed)
                    {
                        _coverage.IncrementPoint("DisposableValueDisposed");
                    }
                    else
                    {
                        _coverage.IncrementPoint("DisposableValueNotDisposed");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, DisposableTestObject>.NumericKeyCache<DisposableTestObject>)TestCacheBase<int, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(cacheType, valueType);

                // Add a disposable value to the cache
                try
                {
                    int key = 42;
                    var value = new DisposableTestObject(1, new byte[] { 1, 2, 3 });

                    cache.Add(key, value);
                    _coverage.IncrementPoint("DisposableValueAdded");

                    // Verify value is in cache
                    if (cache.TryGetValue(key, out DisposableTestObject retrievedValue))
                    {
                        _coverage.IncrementPoint("DisposableValueRetrieved");
                    }

                    // Remove the value from the cache
                    if (cache.Remove(key))
                    {
                        _coverage.IncrementPoint("DisposableValueRemoved");
                    }

                    // Verify value is disposed
                    if (value.IsDisposed)
                    {
                        _coverage.IncrementPoint("DisposableValueDisposed");
                    }
                    else
                    {
                        _coverage.IncrementPoint("DisposableValueNotDisposed");
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }

            return true;
        }

        private bool TestDuplicateKeyHandling(byte[] input, TestCacheType cacheType)
        {
            // Extract value type from input
            TestValueType valueType = (TestValueType)(input.Length > 1 ? input[1] % 5 : 0);

            if (cacheType == TestCacheType.StringKey)
            {
                using var cache = (TestCacheBase<string, byte[]>.StringKeyCache)TestCacheBase<string, byte[]>.CreateTestCache(cacheType, valueType);

                // Add an initial key-value pair
                string key = "duplicate_key";
                byte[] value1 = new byte[] { 1, 2, 3 };

                try
                {
                    cache.Add(key, value1);
                    _coverage.IncrementPoint("InitialKeyAdded");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                // Try to add the same key with a different value
                byte[] value2 = new byte[] { 4, 5, 6 };

                try
                {
                    cache.Add(key, value2);
                    _coverage.IncrementPoint("DuplicateKeyAccepted");

                    // Check which value is stored
                    if (cache.TryGetValue(key, out byte[] storedValue))
                    {
                        if (storedValue.SequenceEqual(value1))
                        {
                            _coverage.IncrementPoint("OriginalValueRetained");
                        }
                        else if (storedValue.SequenceEqual(value2))
                        {
                            _coverage.IncrementPoint("NewValueStored");
                        }
                    }
                }
                catch (ArgumentException)
                {
                    _coverage.IncrementPoint("DuplicateKeyRejected");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }
            }
            else
            {
                using var cache = (TestCacheBase<int, byte[]>.NumericKeyCache)TestCacheBase<int, byte[]>.CreateTestCache(cacheType, valueType);

                // Add an initial key-value pair
                int key = 42;
                byte[] value1 = new byte[] { 1, 2, 3 };

                try
                {
                    cache.Add(key, value1);
                    _coverage.IncrementPoint("InitialKeyAdded");
                }
                catch (Exception ex)
                {
                    ExceptionHandlingUtilities.TrackException(ex, _coverage);
                }

                // Try to add the same key with a different value
                byte[] value2 = new byte[] { 4, 5, 6 };

                try
                {
                    cache.Add(key, value2);
                    _coverage.IncrementPoint("DuplicateKeyAccepted");

                    // Check which value is stored
                    if (cache.TryGetValue(key, out byte[] storedValue))
                    {
                        if (storedValue.SequenceEqual(value1))
                        {
                            _coverage.IncrementPoint("OriginalValueRetained");
                        }
                        else if (storedValue.SequenceEqual(value2))
                        {
                            _coverage.IncrementPoint("NewValueStored");
                        }
                    }
                }
                catch (ArgumentException)
                {
                    _coverage.IncrementPoint("DuplicateKeyRejected");
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

        /// <summary>
        /// A disposable test value for testing disposal of cached values.
        /// </summary>
        private class DisposableTestObject : IDisposable
        {
            public int Id { get; }
            public byte[] Data { get; }
            public bool IsDisposed { get; private set; }

            public DisposableTestObject(int id, byte[] data)
            {
                Id = id;
                Data = data;
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}

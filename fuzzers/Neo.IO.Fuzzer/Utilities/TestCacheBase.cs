// Copyright (C) 2015-2025 The Neo Project.
//
// TestCacheBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Fuzzer.Utilities
{
    /// <summary>
    /// Extension methods for test caches.
    /// </summary>
    public static class TestCacheExtensions
    {
        /// <summary>
        /// Generates a key based on input data.
        /// </summary>
        /// <param name="cache">The cache to generate a key for</param>
        /// <param name="input">The input data to use for key generation</param>
        /// <param name="offset">The offset in the input data to start from</param>
        /// <returns>A key suitable for the cache type</returns>
        public static object GenerateKey(this IDisposable cache, byte[] input, int offset)
        {
            if (input == null || offset >= input.Length)
                return "default_key";

            // Extract a portion of the input to use as the key
            int length = Math.Min(8, input.Length - offset);
            byte[] keyData = new byte[length];
            Array.Copy(input, offset, keyData, 0, length);

            // Determine the type of cache and return an appropriate key
            if (cache is TestCacheBase<string, byte[]>)
            {
                // For string keys, convert to Base64
                return Convert.ToBase64String(keyData);
            }
            else if (cache is TestCacheBase<int, byte[]>)
            {
                // For int keys, convert to an integer
                return BitConverter.ToInt32(keyData.Length >= 4 ? keyData : new byte[] { 0, 0, 0, 0 }, 0);
            }
            else
            {
                // Default to string
                return Convert.ToBase64String(keyData);
            }
        }

        /// <summary>
        /// Generates a value based on input data.
        /// </summary>
        /// <param name="cache">The cache to generate a value for</param>
        /// <param name="input">The input data to use for value generation</param>
        /// <param name="offset">The offset in the input data to start from</param>
        /// <returns>A value suitable for the cache type</returns>
        public static object GenerateValue(this IDisposable cache, byte[] input, int offset)
        {
            if (input == null || offset >= input.Length)
                return new byte[] { 0, 1, 2, 3 };

            // Extract a portion of the input to use as the value
            int length = Math.Min(8, input.Length - offset);
            byte[] valueData = new byte[length];
            Array.Copy(input, offset, valueData, 0, length);

            return valueData;
        }
    }

    /// <summary>
    /// Enum representing different types of test caches to use in fuzzing.
    /// </summary>
    public enum TestCacheType
    {
        StringKey,
        NumericKey,
        GuidKey,
        ComplexKey
    }

    /// <summary>
    /// Enum representing different types of values to store in test caches.
    /// </summary>
    public enum TestValueType
    {
        String,
        Integer,
        ByteArray,
        ComplexObject,
        Disposable
    }

    /// <summary>
    /// Base class for test cache implementations
    /// </summary>
    public class TestCacheBase<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _cache;
        private readonly LinkedList<TKey> _accessOrder;
        private readonly int _maxCapacity;
        private readonly CoverageTrackerHelper _coverage;

        /// <summary>
        /// Gets the number of items in the cache
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// The coverage tracker
        /// </summary>
        protected CoverageTrackerHelper Coverage => _coverage;

        /// <summary>
        /// Creates a new TestCacheBase
        /// </summary>
        protected TestCacheBase(int maxCapacity, CoverageTrackerHelper coverage)
        {
            _maxCapacity = maxCapacity;
            _coverage = coverage;
            _cache = new Dictionary<TKey, TValue>();
            _accessOrder = new LinkedList<TKey>();
        }

        /// <summary>
        /// Gets the key for an item
        /// </summary>
        protected virtual TKey GetKeyForItem(TValue item)
        {
            // Default implementation - derived classes should override this
            return (TKey)(object)(item?.GetHashCode() ?? 0);
        }

        /// <summary>
        /// Tracks access to a key
        /// </summary>
        protected void OnAccess(TKey key)
        {
            // Remove the key if it exists
            _accessOrder.Remove(key);

            // Add the key to the end of the list
            _accessOrder.AddLast(key);
        }

        /// <summary>
        /// Adds a key-value pair to the cache
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            // Track coverage for add operation
            _coverage.IncrementPoint("Add");

            // Check if we need to evict an item
            if (_cache.Count >= _maxCapacity && !_cache.ContainsKey(key))
            {
                // Evict the oldest item
                if (_accessOrder.Count > 0)
                {
                    TKey oldestKey = _accessOrder.First.Value;
                    _cache.Remove(oldestKey);
                    _accessOrder.RemoveFirst();
                }
            }

            // Add or update the item
            _cache[key] = value;

            // Track access for the key
            OnAccess(key);
        }

        /// <summary>
        /// Tries to get a value from the cache
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            // Track coverage for TryGetValue operation
            _coverage.IncrementPoint("TryGetValue");

            // Try to get the value from the cache
            bool result = _cache.TryGetValue(key, out value);

            // Track access for the key if found
            if (result)
            {
                OnAccess(key);
            }

            return result;
        }

        /// <summary>
        /// Checks if the cache contains a key
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            // Track coverage for ContainsKey operation
            _coverage.IncrementPoint("ContainsKey");

            // Check if the cache contains the key
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// Removes a key-value pair from the cache
        /// </summary>
        public bool Remove(TKey key)
        {
            // Track coverage for Remove operation
            _coverage.IncrementPoint("Remove");

            // Remove the key from the access order
            _accessOrder.Remove(key);

            // Remove the key from the cache
            return _cache.Remove(key);
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            // Track coverage for clear operation
            _coverage.IncrementPoint("Clear");
            // Clear the cache
            _cache.Clear();
            // Reset access tracking
            _accessOrder.Clear();
        }

        /// <summary>
        /// Creates a test cache with the specified cache type and value type
        /// </summary>
        public static IDisposable CreateTestCache(TestCacheType cacheType, TestValueType valueType, int maxCapacity = 100)
        {
            // Create a coverage tracker
            var coverage = new CoverageTrackerHelper("CacheOperations");

            // Create the appropriate cache type
            switch (cacheType)
            {
                case TestCacheType.StringKey:
                    return new TestCacheBase<string, byte[]>.StringKeyCache(maxCapacity, coverage);

                case TestCacheType.NumericKey:
                    return new TestCacheBase<int, byte[]>.NumericKeyCache(maxCapacity, coverage);

                default:
                    throw new ArgumentException($"Unsupported cache type: {cacheType}");
            }
        }

        /// <summary>
        /// Creates a test cache with the specified cache type for IDisposable values
        /// </summary>
        public static IDisposable CreateDisposableTestCache<TDisposable>(TestCacheType cacheType, TestValueType valueType, int maxCapacity = 100) where TDisposable : IDisposable
        {
            // Create a coverage tracker
            var coverage = new CoverageTrackerHelper("CacheOperations");

            // Create the appropriate cache type
            switch (cacheType)
            {
                case TestCacheType.StringKey:
                    return new StringKeyCache<TDisposable>(maxCapacity, coverage);

                case TestCacheType.NumericKey:
                    return new NumericKeyCache<TDisposable>(maxCapacity, coverage);

                default:
                    throw new ArgumentException($"Unsupported cache type: {cacheType}");
            }
        }

        /// <summary>
        /// Disposes resources used by this cache.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by this cache.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose any IDisposable values in the cache
                foreach (var value in _cache.Values)
                {
                    if (value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                // Clear the cache
                _cache.Clear();
                _accessOrder.Clear();
            }
        }

        /// <summary>
        /// A cache implementation that uses string keys.
        /// </summary>
        public class StringKeyCache : TestCacheBase<string, byte[]>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StringKeyCache"/> class.
            /// </summary>
            public StringKeyCache(int maxCapacity, CoverageTrackerHelper coverage) : base(maxCapacity, coverage)
            {
            }

            /// <summary>
            /// Gets the key for an item
            /// </summary>
            protected override string GetKeyForItem(byte[] item)
            {
                return BitConverter.ToString(item);
            }

            /// <summary>
            /// Adds a byte array to the cache with the specified key.
            /// </summary>
            public new void Add(string key, byte[] value)
            {
                base.Add(key, value);
            }

            /// <summary>
            /// Tries to get a value from the cache.
            /// </summary>
            public bool TryGet(string key, out byte[] value)
            {
                return TryGetValue(key, out value!);
            }

            /// <summary>
            /// Clears all items from the cache.
            /// </summary>
            public new void Clear()
            {
                base.Clear();
            }

            /// <summary>
            /// Disposes resources used by this cache.
            /// </summary>
            public override void Dispose()
            {
                // Dispose logic
                base.Dispose();
            }
        }

        /// <summary>
        /// A generic cache implementation that uses string keys and disposable values.
        /// </summary>
        public class StringKeyCache<TValue> : TestCacheBase<string, TValue> where TValue : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StringKeyCache{TValue}"/> class.
            /// </summary>
            public StringKeyCache(int maxCapacity, CoverageTrackerHelper coverage) : base(maxCapacity, coverage)
            {
            }

            /// <summary>
            /// Gets the key for an item
            /// </summary>
            protected override string GetKeyForItem(TValue item)
            {
                return item.GetHashCode().ToString();
            }

            /// <summary>
            /// Adds a value to the cache with the specified key.
            /// </summary>
            public new void Add(string key, TValue value)
            {
                base.Add(key, value);
            }

            /// <summary>
            /// Tries to get a value from the cache.
            /// </summary>
            public bool TryGet(string key, out TValue value)
            {
                return TryGetValue(key, out value!);
            }

            /// <summary>
            /// Clears all items from the cache.
            /// </summary>
            public new void Clear()
            {
                base.Clear();
            }

            /// <summary>
            /// Disposes resources used by this cache.
            /// </summary>
            public override void Dispose()
            {
                // Dispose logic
                base.Dispose();
            }
        }

        /// <summary>
        /// A cache implementation that uses numeric keys.
        /// </summary>
        public class NumericKeyCache : TestCacheBase<int, byte[]>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NumericKeyCache"/> class.
            /// </summary>
            public NumericKeyCache(int maxCapacity, CoverageTrackerHelper coverage) : base(maxCapacity, coverage)
            {
            }

            /// <summary>
            /// Gets the key for an item
            /// </summary>
            protected override int GetKeyForItem(byte[] item)
            {
                return item.GetHashCode();
            }

            /// <summary>
            /// Adds a byte array to the cache with the specified key.
            /// </summary>
            public new void Add(int key, byte[] value)
            {
                base.Add(key, value);
            }

            /// <summary>
            /// Tries to get a value from the cache.
            /// </summary>
            public bool TryGet(int key, out byte[] value)
            {
                return TryGetValue(key, out value!);
            }

            /// <summary>
            /// Clears all items from the cache.
            /// </summary>
            public new void Clear()
            {
                base.Clear();
            }

            /// <summary>
            /// Disposes resources used by this cache.
            /// </summary>
            public override void Dispose()
            {
                // Dispose logic
                base.Dispose();
            }
        }

        /// <summary>
        /// A generic cache implementation that uses numeric keys and disposable values.
        /// </summary>
        public class NumericKeyCache<TValue> : TestCacheBase<int, TValue> where TValue : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NumericKeyCache{TValue}"/> class.
            /// </summary>
            public NumericKeyCache(int maxCapacity, CoverageTrackerHelper coverage) : base(maxCapacity, coverage)
            {
            }

            /// <summary>
            /// Gets the key for an item
            /// </summary>
            protected override int GetKeyForItem(TValue item)
            {
                return item.GetHashCode();
            }

            /// <summary>
            /// Adds a value to the cache with the specified key.
            /// </summary>
            public new void Add(int key, TValue value)
            {
                base.Add(key, value);
            }

            /// <summary>
            /// Tries to get a value from the cache.
            /// </summary>
            public bool TryGet(int key, out TValue value)
            {
                return TryGetValue(key, out value!);
            }

            /// <summary>
            /// Clears all items from the cache.
            /// </summary>
            public new void Clear()
            {
                base.Clear();
            }

            /// <summary>
            /// Disposes resources used by this cache.
            /// </summary>
            public override void Dispose()
            {
                // Dispose logic
                base.Dispose();
            }
        }
    }
}

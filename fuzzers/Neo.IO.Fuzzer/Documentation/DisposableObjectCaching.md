# Disposable Object Caching

This document describes the implementation and testing of disposable object caching in the Neo.IO.Fuzzer project.

## Overview

Proper resource management is critical for any caching system. When caching objects that implement `IDisposable`, the cache must ensure that these objects are properly disposed when they are removed from the cache or when the cache itself is disposed. The Neo.IO.Fuzzer includes specialized components to test this behavior.

## DisposableTestObject

The `DisposableTestObject` class is a test implementation of `IDisposable` used to verify proper disposal behavior:

```csharp
public class DisposableTestObject : IDisposable
{
    public int Id { get; }
    public byte[] Data { get; }
    public bool IsDisposed { get; private set; }

    public DisposableTestObject(int id, byte[] data)
    {
        Id = id;
        Data = data;
        IsDisposed = false;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
```

This class provides a simple way to track whether an object has been properly disposed by checking the `IsDisposed` property.

## TestCacheBase Enhancements

The `TestCacheBase<TKey, TValue>` class has been enhanced to support caching of disposable objects:

### Generic Type Parameters

To avoid naming conflicts and improve type safety, the specialized cache implementations use distinct type parameters:

```csharp
public class StringKeyCache<TDisposableValue> : TestCacheBase<string, TDisposableValue> 
    where TDisposableValue : IDisposable
{
    // Implementation
}

public class NumericKeyCache<TDisposableValue> : TestCacheBase<int, TDisposableValue> 
    where TDisposableValue : IDisposable
{
    // Implementation
}
```

### CreateDisposableTestCache Method

A specialized factory method creates caches for disposable objects:

```csharp
public static IDisposable CreateDisposableTestCache<TDisposable>(
    TestCacheType cacheType, 
    TestValueType valueType, 
    int maxCapacity = 100) 
    where TDisposable : IDisposable
{
    // Create a coverage tracker
    var coverage = new CoverageTrackerHelper("CacheOperations");
    
    // Create the appropriate cache type
    switch (cacheType)
    {
        case TestCacheType.StringKey:
            return new TestCacheBase<string, TDisposable>.StringKeyCache<TDisposable>(
                maxCapacity, coverage);
            
        case TestCacheType.NumericKey:
            return new TestCacheBase<int, TDisposable>.NumericKeyCache<TDisposable>(
                maxCapacity, coverage);
            
        default:
            throw new ArgumentException($"Unsupported cache type: {cacheType}");
    }
}
```

### Default GetKeyForItem Implementation

The base class now provides a default implementation for the `GetKeyForItem` method:

```csharp
protected virtual TKey GetKeyForItem(TValue item)
{
    // Default implementation - derived classes should override this
    return (TKey)(object)(item?.GetHashCode() ?? 0);
}
```

This eliminates the need for each derived class to implement this method if the default behavior is sufficient.

## Testing Disposable Object Caching

The fuzzing strategies test disposable object caching in several ways:

### Basic Cache Fuzzing Strategy

The `BasicCacheFuzzStrategy` tests the basic lifecycle of disposable objects in the cache:

```csharp
// Create a cache for disposable objects
using var cache = (TestCacheBase<string, DisposableTestObject>.StringKeyCache<DisposableTestObject>)
    TestCacheBase<string, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(
        cacheType, valueType);

// Add a disposable object to the cache
string key = "test_key";
var value = new DisposableTestObject(1, new byte[] { 1, 2, 3 });
cache.Add(key, value);

// Remove the object from the cache
cache.Remove(key);

// Verify the object was disposed
if (value.IsDisposed)
{
    _coverage.IncrementPoint("DisposedOnRemove");
}
else
{
    _coverage.IncrementPoint("NotDisposedOnRemove");
}
```

### Key-Value Mutation Strategy

The `KeyValueMutationStrategy` tests more complex scenarios with disposable objects:

```csharp
// Create a cache for disposable objects
using var cache = (TestCacheBase<string, DisposableTestObject>.StringKeyCache<DisposableTestObject>)
    TestCacheBase<string, DisposableTestObject>.CreateDisposableTestCache<DisposableTestObject>(
        cacheType, valueType);

// Add a disposable value to the cache
string key = "disposable_key";
var value = new DisposableTestObject(1, new byte[] { 1, 2, 3 });
cache.Add(key, value);

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
```

## Coverage Tracking

The fuzzing strategies track several coverage points related to disposable object caching:

- **DisposedOnRemove**: The object was properly disposed when removed from the cache
- **NotDisposedOnRemove**: The object was not disposed when removed from the cache
- **DisposableValueAdded**: A disposable value was successfully added to the cache
- **DisposableValueRetrieved**: A disposable value was successfully retrieved from the cache
- **DisposableValueRemoved**: A disposable value was successfully removed from the cache
- **DisposableValueDisposed**: A disposable value was properly disposed
- **DisposableValueNotDisposed**: A disposable value was not properly disposed

These coverage points help identify issues with disposable object handling in the cache implementations.

## Running Disposable Object Tests

To run tests specifically targeting disposable object caching, use the following command:

```bash
dotnet run -t cache -c BasicCacheFuzzStrategy -i 100 --report-interval 10
```

Or for the key-value mutation strategy:

```bash
dotnet run -t cache -c KeyValueMutationStrategy -i 100 --report-interval 10
```

## Best Practices for Disposable Object Caching

When implementing caches that store disposable objects, follow these best practices:

1. **Implement IDisposable**: The cache itself should implement `IDisposable`
2. **Dispose on Remove**: Objects should be disposed when removed from the cache
3. **Dispose on Clear**: All objects should be disposed when the cache is cleared
4. **Dispose on Cache Disposal**: All objects should be disposed when the cache is disposed
5. **Thread Safety**: Disposal operations should be thread-safe
6. **Null Handling**: The cache should handle null values gracefully

By following these practices and thoroughly testing with the fuzzing strategies, you can ensure that your cache implementation properly manages disposable resources.

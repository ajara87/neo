// Copyright (C) 2015-2025 The Neo Project.
//
// ExceptionHandlingUtilities.cs file belongs to the neo project and is free
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
using System.Threading;

namespace Neo.IO.Fuzzer.Utilities
{
    /// <summary>
    /// Categories of exceptions for standardized reporting and analysis.
    /// </summary>
    public enum ExceptionCategory
    {
        /// <summary>
        /// Unknown exception type
        /// </summary>
        Unknown,

        /// <summary>
        /// Argument-related exceptions (ArgumentException, ArgumentNullException, etc.)
        /// </summary>
        ArgumentError,

        /// <summary>
        /// Invalid operation exceptions
        /// </summary>
        InvalidOperation,

        /// <summary>
        /// Null reference exceptions
        /// </summary>
        NullReference,

        /// <summary>
        /// Index out of range exceptions
        /// </summary>
        IndexOutOfRange,

        /// <summary>
        /// Overflow exceptions
        /// </summary>
        Overflow,

        /// <summary>
        /// Out of memory exceptions
        /// </summary>
        OutOfMemory,

        /// <summary>
        /// Timeout exceptions
        /// </summary>
        Timeout,

        /// <summary>
        /// IO exceptions
        /// </summary>
        IO,

        /// <summary>
        /// Serialization exceptions
        /// </summary>
        Serialization,

        /// <summary>
        /// Threading exceptions
        /// </summary>
        Threading,

        /// <summary>
        /// Security exceptions
        /// </summary>
        Security
    }

    /// <summary>
    /// Verbosity levels for exception logging.
    /// </summary>
    public enum LogVerbosity
    {
        /// <summary>
        /// Minimal logging (just exception type and message)
        /// </summary>
        Minimal,

        /// <summary>
        /// Normal logging (exception type, message, and basic stack trace)
        /// </summary>
        Normal,

        /// <summary>
        /// Verbose logging (full exception details including inner exceptions)
        /// </summary>
        Verbose
    }

    /// <summary>
    /// Utility class for standardized exception handling in fuzzing operations.
    /// Provides methods for executing operations with consistent exception handling.
    /// </summary>
    public static class ExceptionHandlingUtilities
    {
        /// <summary>
        /// Executes an operation with standard exception handling.
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="operationName">The name of the operation (for logging)</param>
        /// <param name="coverageTracker">Optional coverage tracker to update</param>
        /// <returns>True if the operation completed successfully, false otherwise</returns>
        public static bool ExecuteWithExceptionHandling(
            Func<bool> operation,
            string operationName,
            CoverageTrackerHelper? coverageTracker = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                bool result = operation();

                // Track successful operation if coverage tracker is provided
                coverageTracker?.IncrementPoint("TotalOperations");

                return result;
            }
            catch (Exception ex)
            {
                // Log the exception
                LogException(ex, operationName);

                // Track exception if coverage tracker is provided
                coverageTracker?.IncrementPoint("Exceptions");

                return false;
            }
        }

        /// <summary>
        /// Executes an operation with custom exception handling.
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="exceptionHandler">Custom exception handler</param>
        /// <param name="operationName">The name of the operation (for logging)</param>
        /// <param name="coverageTracker">Optional coverage tracker to update</param>
        /// <returns>True if the operation completed successfully, false if an exception occurred and was handled</returns>
        public static bool ExecuteWithExceptionHandling(
            Func<bool> operation,
            Func<Exception, bool> exceptionHandler,
            string operationName,
            CoverageTrackerHelper? coverageTracker = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (exceptionHandler == null)
                throw new ArgumentNullException(nameof(exceptionHandler));

            try
            {
                bool result = operation();

                // Track successful operation if coverage tracker is provided
                coverageTracker?.IncrementPoint("TotalOperations");

                return result;
            }
            catch (Exception ex)
            {
                // Log the exception
                LogException(ex, operationName);

                // Track exception if coverage tracker is provided
                coverageTracker?.IncrementPoint("Exceptions");

                // Call the custom exception handler
                return exceptionHandler(ex);
            }
        }

        /// <summary>
        /// Executes an operation that returns a value with standard exception handling.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="defaultValue">The default value to return if an exception occurs</param>
        /// <param name="operationName">The name of the operation (for logging)</param>
        /// <param name="coverageTracker">Optional coverage tracker to update</param>
        /// <returns>The result of the operation, or the default value if an exception occurred</returns>
        public static T ExecuteWithExceptionHandling<T>(
            Func<T> operation,
            T defaultValue,
            string operationName,
            CoverageTrackerHelper? coverageTracker = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                T result = operation();

                // Track successful operation if coverage tracker is provided
                coverageTracker?.IncrementPoint("TotalOperations");

                return result;
            }
            catch (Exception ex)
            {
                // Log the exception
                LogException(ex, operationName);

                // Track exception if coverage tracker is provided
                coverageTracker?.IncrementPoint("Exceptions");

                return defaultValue;
            }
        }

        /// <summary>
        /// Executes an action with retry logic.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="maxRetries">The maximum number of retries</param>
        /// <param name="retryDelayMs">The delay between retries in milliseconds</param>
        /// <param name="coverageTracker">Optional coverage tracker to update</param>
        /// <returns>True if the action succeeded, false otherwise</returns>
        public static bool ExecuteWithRetry(
            Func<bool> action,
            string operationName,
            int maxRetries = 3,
            int retryDelayMs = 100,
            CoverageTrackerHelper? coverageTracker = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            int attempt = 0;

            while (attempt <= maxRetries)
            {
                try
                {
                    bool result = action();

                    // Track successful operation if coverage tracker is provided
                    coverageTracker?.IncrementPoint("TotalOperations");

                    if (attempt > 0)
                    {
                        // Track successful retry if coverage tracker is provided
                        coverageTracker?.IncrementPoint("SuccessfulRetries");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    attempt++;

                    // Log the exception
                    LogException(ex, $"{operationName} (Attempt {attempt}/{maxRetries + 1})");

                    // Track exception if coverage tracker is provided
                    coverageTracker?.IncrementPoint("Exceptions");
                    coverageTracker?.IncrementPoint("RetryAttempts");

                    if (attempt <= maxRetries)
                    {
                        // Wait before retrying
                        Thread.Sleep(retryDelayMs);
                    }
                }
            }

            // All attempts failed
            coverageTracker?.IncrementPoint("RetryFailures");
            return false;
        }

        /// <summary>
        /// Logs an exception with standard formatting.
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="operationName">The name of the operation that threw the exception</param>
        public static void LogException(Exception ex, string operationName)
        {
            Console.WriteLine($"Exception in {operationName}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Logs detailed information about an exception.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="operationName">The name of the operation that caused the exception</param>
        /// <param name="verbosity">The verbosity level for logging</param>
        public static void LogExceptionDetails(Exception exception, string operationName, LogVerbosity verbosity = LogVerbosity.Normal)
        {
            if (exception == null)
                return;

            ExceptionCategory category = CategorizeException(exception);

            // Basic information (always logged)
            Console.WriteLine($"Exception in {operationName}: [{category}] {exception.Message}");

            // Medium verbosity
            if (verbosity >= LogVerbosity.Normal)
            {
                if (exception.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {exception.InnerException.Message}");
                }

                Console.WriteLine($"Exception Type: {exception.GetType().FullName}");
            }

            // High verbosity
            if (verbosity >= LogVerbosity.Verbose)
            {
                Console.WriteLine($"Stack Trace: {exception.StackTrace}");

                if (exception.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception Stack Trace: {exception.InnerException.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Categorizes an exception into a standard category.
        /// </summary>
        /// <param name="exception">The exception to categorize</param>
        /// <returns>The category of the exception</returns>
        public static ExceptionCategory CategorizeException(Exception exception)
        {
            if (exception == null)
                return ExceptionCategory.Unknown;

            // Categorize by exception type
            if (exception is ArgumentException || exception is ArgumentNullException || exception is ArgumentOutOfRangeException)
                return ExceptionCategory.ArgumentError;
            if (exception is InvalidOperationException)
                return ExceptionCategory.InvalidOperation;
            if (exception is NullReferenceException)
                return ExceptionCategory.NullReference;
            if (exception is IndexOutOfRangeException)
                return ExceptionCategory.IndexOutOfRange;
            if (exception is OverflowException)
                return ExceptionCategory.Overflow;
            if (exception is OutOfMemoryException)
                return ExceptionCategory.OutOfMemory;
            if (exception is TimeoutException)
                return ExceptionCategory.Timeout;
            if (exception is System.IO.IOException)
                return ExceptionCategory.IO;
            if (exception is System.Runtime.Serialization.SerializationException)
                return ExceptionCategory.Serialization;
            if (exception is ThreadInterruptedException || exception is ThreadAbortException)
                return ExceptionCategory.Threading;
            if (exception is System.Security.SecurityException)
                return ExceptionCategory.Security;

            // Default to Unknown
            return ExceptionCategory.Unknown;
        }

        /// <summary>
        /// Tracks an exception in the coverage tracker with categorization.
        /// </summary>
        /// <param name="ex">The exception to track</param>
        /// <param name="coverageTracker">The coverage tracker to update</param>
        public static void TrackException(Exception ex, CoverageTrackerHelper coverageTracker)
        {
            if (coverageTracker == null || ex == null)
                return;

            ExceptionCategory category = CategorizeException(ex);

            // Track the exception category
            coverageTracker.IncrementPoint($"Exception_{category}");

            // Track the specific exception type
            coverageTracker.IncrementPoint($"ExceptionType_{ex.GetType().Name}");

            // Track if there was an inner exception
            if (ex.InnerException != null)
            {
                coverageTracker.IncrementPoint("InnerExceptions");
                coverageTracker.IncrementPoint($"InnerExceptionType_{ex.InnerException.GetType().Name}");
            }
        }
    }
}

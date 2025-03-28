// Copyright (C) 2015-2025 The Neo Project.
//
// IFuzzTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Interface for targets that can be fuzzed
    /// </summary>
    public interface IFuzzTarget
    {
        /// <summary>
        /// Gets the name of the target
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the target with the provided input
        /// </summary>
        /// <param name="input">The input data to use</param>
        /// <returns>True if the execution was successful, false otherwise</returns>
        bool Execute(byte[] input);

        /// <summary>
        /// Gets the coverage information for the target
        /// </summary>
        /// <returns>The coverage information</returns>
        object GetCoverage();
    }

    /// <summary>
    /// Represents the result of a fuzzing execution
    /// </summary>
    public class FuzzResult
    {
        /// <summary>
        /// Gets or sets whether the execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during execution, if any
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets additional information about the execution
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Gets or sets the coverage information for the execution
        /// </summary>
        public object? Coverage { get; set; }

        /// <summary>
        /// Gets or sets the result of the execution
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Gets or sets whether the execution produced interesting coverage
        /// </summary>
        public bool IsInteresting { get; set; }

        /// <summary>
        /// Creates a new instance of the FuzzResult class with default values
        /// </summary>
        public FuzzResult()
        {
            Success = true;
            ExecutionTimeMs = 0;
        }

        /// <summary>
        /// Creates a new instance of the FuzzResult class with the specified values
        /// </summary>
        /// <param name="success">Whether the execution was successful</param>
        /// <param name="exception">The exception that occurred during execution, if any</param>
        /// <param name="executionTimeMs">The execution time in milliseconds</param>
        public FuzzResult(bool success, Exception? exception = null, long executionTimeMs = 0)
        {
            Success = success;
            Exception = exception;
            ExecutionTimeMs = executionTimeMs;
        }
    }
}

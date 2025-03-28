// Copyright (C) 2015-2025 The Neo Project.
//
// InputProcessingUtilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.IO.Fuzzer.Utilities
{
    /// <summary>
    /// Utility class for processing fuzzing input data.
    /// Provides methods for dividing input, extracting parameters, and generating test data.
    /// </summary>
    public static class InputProcessingUtilities
    {
        /// <summary>
        /// Divides the input data into chunks for processing.
        /// </summary>
        /// <param name="input">The input data to divide</param>
        /// <param name="preferredChunkCount">The preferred number of chunks to create (if possible)</param>
        /// <returns>A list of byte arrays representing the chunks</returns>
        public static List<byte[]> DivideInput(byte[] input, int preferredChunkCount = 0)
        {
            var result = new List<byte[]>();

            // Handle null or too small inputs
            if (input == null || input.Length < 2)
                return result;

            // Determine chunk count based on input if not specified
            int chunkCount = preferredChunkCount > 0
                ? preferredChunkCount
                : Math.Max(1, input[1] % 10);

            int chunkSize = (input.Length - 2) / chunkCount;

            // Ensure minimum chunk size
            if (chunkSize < 1)
            {
                result.Add(input.Skip(2).ToArray());
                return result;
            }

            // Divide the input into chunks
            for (int i = 0; i < chunkCount && (i * chunkSize + 2) < input.Length; i++)
            {
                int start = 2 + (i * chunkSize);
                int length = Math.Min(chunkSize, input.Length - start);
                result.Add(input.Skip(start).Take(length).ToArray());
            }

            return result;
        }

        /// <summary>
        /// Extracts a numeric parameter from the input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="byteIndex">The index of the byte to use</param>
        /// <param name="minValue">The minimum value to return</param>
        /// <param name="maxValue">The maximum value to return</param>
        /// <returns>A numeric parameter between minValue and maxValue</returns>
        public static int ExtractNumericParameter(byte[] input, int byteIndex, int minValue, int maxValue)
        {
            if (input == null || input.Length <= byteIndex)
                return minValue;

            int range = maxValue - minValue + 1;
            return minValue + (input[byteIndex] % range);
        }

        /// <summary>
        /// Generates a list of test keys from the input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="minCount">The minimum number of keys to generate</param>
        /// <returns>A list of string keys</returns>
        public static List<string> GenerateTestKeys(byte[] input, int minCount = 10)
        {
            var result = new List<string>();

            // Skip the first byte (used for operation count)
            for (int i = 1; i < input.Length; i += 8)
            {
                if (i + 4 <= input.Length)
                {
                    // Create a key from the input
                    byte[] keyBytes = new byte[4];
                    Array.Copy(input, i, keyBytes, 0, 4);
                    var key = Convert.ToBase64String(keyBytes);
                    result.Add(key);
                }
            }

            // Ensure we have at least the minimum number of keys
            while (result.Count < minCount)
            {
                var key = $"key_{result.Count}";
                result.Add(key);
            }

            return result;
        }

        /// <summary>
        /// Generates a list of test values from the input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="minCount">The minimum number of values to generate</param>
        /// <returns>A list of byte arrays representing values</returns>
        public static List<byte[]> GenerateTestValues(byte[] input, int minCount = 10)
        {
            var result = new List<byte[]>();

            // Skip the first byte and use the rest for values
            for (int i = 1; i < input.Length; i += 8)
            {
                // Create a value from the input
                int size = Math.Min(8, input.Length - i);
                if (size > 0)
                {
                    byte[] valueBytes = new byte[size];
                    Array.Copy(input, i, valueBytes, 0, size);
                    result.Add(valueBytes);
                }
            }

            // Ensure we have at least the minimum number of values
            while (result.Count < minCount)
            {
                var value = BitConverter.GetBytes(result.Count);
                result.Add(value);
            }

            return result;
        }

        /// <summary>
        /// Creates a dictionary of test key-value pairs from the input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="minCount">The minimum number of pairs to generate</param>
        /// <returns>A dictionary of string keys and byte array values</returns>
        public static Dictionary<string, byte[]> CreateTestKeyValuePairs(byte[] input, int minCount = 10)
        {
            var result = new Dictionary<string, byte[]>();
            var keys = GenerateTestKeys(input, minCount);
            var values = GenerateTestValues(input, minCount);

            // Create pairs from the generated keys and values
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                result[keys[i]] = values[i];
            }

            return result;
        }

        /// <summary>
        /// Extracts a boolean parameter from the input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="byteIndex">The index of the byte to use</param>
        /// <returns>A boolean value based on the input byte</returns>
        public static bool ExtractBooleanParameter(byte[] input, int byteIndex)
        {
            if (input == null || input.Length <= byteIndex)
                return false;

            return (input[byteIndex] % 2) == 1;
        }

        /// <summary>
        /// Extracts a string parameter from the input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="startIndex">The starting index in the input</param>
        /// <param name="maxLength">The maximum length of the string</param>
        /// <returns>A string extracted from the input data</returns>
        public static string ExtractStringParameter(byte[] input, int startIndex, int maxLength)
        {
            if (input == null || input.Length <= startIndex)
                return string.Empty;

            int length = Math.Min(maxLength, input.Length - startIndex);
            byte[] stringBytes = new byte[length];
            Array.Copy(input, startIndex, stringBytes, 0, length);

            // Convert to a valid string (Base64 is safe)
            return Convert.ToBase64String(stringBytes);
        }

        /// <summary>
        /// Extracts an enum parameter from the input data.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <param name="input">The input data</param>
        /// <param name="byteIndex">The index of the byte to use</param>
        /// <returns>An enum value based on the input byte</returns>
        public static TEnum ExtractEnumParameter<TEnum>(byte[] input, int byteIndex) where TEnum : struct, Enum
        {
            if (input == null || input.Length <= byteIndex)
                return default;

            // Get all possible enum values
            var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();

            if (values.Length == 0)
                return default;

            // Select an enum value based on the input byte
            int index = input[byteIndex] % values.Length;
            return values[index];
        }

        /// <summary>
        /// Generates structured input data with specific sections for different parameters.
        /// </summary>
        /// <param name="operationType">Type of operation to encode</param>
        /// <param name="parameters">Parameter values to include</param>
        /// <returns>A byte array with structured input data</returns>
        public static byte[] GenerateStructuredInput(byte operationType, params byte[] parameters)
        {
            // Format: [OperationType][ParameterCount][Parameter1][Parameter2]...
            byte[] result = new byte[parameters.Length + 2];

            // Set operation type
            result[0] = operationType;

            // Set parameter count
            result[1] = (byte)parameters.Length;

            // Copy parameters
            Array.Copy(parameters, 0, result, 2, parameters.Length);

            return result;
        }

        /// <summary>
        /// Extracts structured parameters from input data.
        /// </summary>
        /// <param name="input">The input data</param>
        /// <returns>A tuple containing the operation type and parameters</returns>
        public static (byte OperationType, byte[] Parameters) ExtractStructuredParameters(byte[] input)
        {
            if (input == null || input.Length < 2)
                return (0, Array.Empty<byte>());

            // Extract operation type
            byte operationType = input[0];

            // Extract parameter count
            int parameterCount = input[1];

            // Validate parameter count
            if (parameterCount > input.Length - 2)
                parameterCount = input.Length - 2;

            // Extract parameters
            byte[] parameters = new byte[parameterCount];
            Array.Copy(input, 2, parameters, 0, parameterCount);

            return (operationType, parameters);
        }
    }
}

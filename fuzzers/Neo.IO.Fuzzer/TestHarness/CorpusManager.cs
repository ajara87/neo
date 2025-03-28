// Copyright (C) 2015-2025 The Neo Project.
//
// CorpusManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Neo.IO.Fuzzer.TestHarness
{
    /// <summary>
    /// Manages the corpus of inputs for fuzzing
    /// </summary>
    public class CorpusManager
    {
        private readonly string _corpusDir;
        private readonly string _crashesDir;
        private readonly CoverageTracker _coverageTracker;
        private readonly ConcurrentDictionary<string, byte[]> _corpus = new();
        private readonly ConcurrentDictionary<string, byte[]> _crashes = new();

        /// <summary>
        /// Initializes a new instance of the CorpusManager class
        /// </summary>
        /// <param name="corpusDir">The directory for storing corpus files</param>
        /// <param name="crashesDir">The directory for storing crash files</param>
        /// <param name="coverageTracker">The coverage tracker</param>
        public CorpusManager(string corpusDir, string crashesDir, CoverageTracker coverageTracker)
        {
            _corpusDir = corpusDir ?? throw new ArgumentNullException(nameof(corpusDir));
            _crashesDir = crashesDir ?? throw new ArgumentNullException(nameof(crashesDir));
            _coverageTracker = coverageTracker ?? throw new ArgumentNullException(nameof(coverageTracker));

            // Create directories if they don't exist
            Directory.CreateDirectory(_corpusDir);
            Directory.CreateDirectory(_crashesDir);
        }

        /// <summary>
        /// Gets the number of inputs in the corpus
        /// </summary>
        public int CorpusCount => _corpus.Count;

        /// <summary>
        /// Gets the number of crashes in the corpus
        /// </summary>
        public int CrashCount => _crashes.Count;

        /// <summary>
        /// Loads existing corpus files from the corpus directory
        /// </summary>
        /// <returns>A task representing the loading process</returns>
        public async Task LoadCorpusAsync()
        {
            // Load corpus files
            foreach (string filePath in Directory.GetFiles(_corpusDir, "*.bin"))
            {
                try
                {
                    byte[] input = await File.ReadAllBytesAsync(filePath);
                    string id = Path.GetFileNameWithoutExtension(filePath);
                    _corpus[id] = input;

                    // Try to load the coverage file if it exists
                    string coverageFilePath = Path.Combine(_corpusDir, $"{id}.coverage");
                    if (File.Exists(coverageFilePath))
                    {
                        string[] coveragePoints = await File.ReadAllLinesAsync(coverageFilePath);
                        var coverageSet = new HashSet<string>(coveragePoints);
                        _coverageTracker.UpdateCoverage(coverageSet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading corpus file {filePath}: {ex.Message}");
                }
            }

            // Load crash files
            foreach (string filePath in Directory.GetFiles(_crashesDir, "*.bin"))
            {
                try
                {
                    byte[] input = await File.ReadAllBytesAsync(filePath);
                    string id = Path.GetFileNameWithoutExtension(filePath);
                    _crashes[id] = input;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading crash file {filePath}: {ex.Message}");
                }
            }

            Console.WriteLine($"Loaded {_corpus.Count} corpus files and {_crashes.Count} crash files");
        }

        /// <summary>
        /// Adds an input to the corpus if it's interesting
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="testResult">The test result</param>
        /// <returns>True if the input was added to the corpus, false otherwise</returns>
        public bool AddToCorpusIfInteresting(byte[] input, TestResult testResult)
        {
            if (input == null || input.Length == 0)
            {
                return false;
            }

            // If the input caused a crash, add it to the crashes directory
            if (testResult.Outcome == TestOutcome.Exception ||
                testResult.Outcome == TestOutcome.Timeout)
            {
                return AddCrash(input, testResult);
            }

            // Check if the input increases coverage
            bool isInteresting = false;
            if (testResult.Coverage != null)
            {
                isInteresting = _coverageTracker.UpdateCoverage(testResult.Coverage);
            }

            // If the input is not interesting, don't add it to the corpus
            if (!isInteresting)
            {
                return false;
            }

            // Generate a unique ID for the input
            string id = GenerateInputId(input);

            // If the input is already in the corpus, don't add it again
            if (_corpus.ContainsKey(id))
            {
                return false;
            }

            // Add the input to the corpus
            _corpus[id] = input;

            // Save the input to a file
            string filePath = Path.Combine(_corpusDir, $"{id}.bin");
            File.WriteAllBytes(filePath, input);

            // Save the coverage points to a file
            if (testResult.Coverage != null)
            {
                string coverageFilePath = Path.Combine(_corpusDir, $"{id}.coverage");

                // Handle different types of coverage information
                if (testResult.Coverage is HashSet<string> coverageSet && coverageSet.Count > 0)
                {
                    File.WriteAllLines(coverageFilePath, coverageSet);
                }
                else if (testResult.Coverage is IEnumerable<string> coverageList)
                {
                    var coverageArray = coverageList.ToArray();
                    if (coverageArray.Length > 0)
                    {
                        File.WriteAllLines(coverageFilePath, coverageArray);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Adds an input to the corpus with its coverage points
        /// </summary>
        /// <param name="input">The input data</param>
        /// <param name="coveragePoints">The coverage points</param>
        /// <returns>True if the input was added to the corpus, false otherwise</returns>
        public bool AddToCorpus(byte[] input, HashSet<string> coveragePoints)
        {
            if (input == null || input.Length == 0)
            {
                return false;
            }

            // Check if the input increases coverage
            bool isInteresting = _coverageTracker.UpdateCoverage(coveragePoints);

            // If the input is not interesting, don't add it to the corpus
            if (!isInteresting)
            {
                return false;
            }

            // Generate a unique ID for the input
            string id = GenerateInputId(input);

            // If the input is already in the corpus, don't add it again
            if (_corpus.ContainsKey(id))
            {
                return false;
            }

            // Add the input to the corpus
            _corpus[id] = input;

            // Save the input to a file
            string filePath = Path.Combine(_corpusDir, $"{id}.bin");
            File.WriteAllBytes(filePath, input);

            // Save the coverage points to a file
            if (coveragePoints != null && coveragePoints.Count > 0)
            {
                string coverageFilePath = Path.Combine(_corpusDir, $"{id}.coverage");
                File.WriteAllLines(coverageFilePath, coveragePoints);
            }

            return true;
        }

        /// <summary>
        /// Adds a crash to the crashes directory
        /// </summary>
        /// <param name="input">The input data that caused the crash</param>
        /// <param name="testResult">The test result</param>
        /// <returns>True if the crash was added, false otherwise</returns>
        private bool AddCrash(byte[] input, TestResult testResult)
        {
            // Generate a unique ID for the crash
            string id = GenerateInputId(input);

            // If the crash is already in the crashes directory, don't add it again
            if (_crashes.ContainsKey(id))
            {
                return false;
            }

            // Add the crash to the crashes dictionary
            _crashes[id] = input;

            // Save the crash to a file
            string filePath = Path.Combine(_crashesDir, $"{id}.bin");
            File.WriteAllBytes(filePath, input);

            // Create a metadata file with information about the crash
            string metadataPath = Path.Combine(_crashesDir, $"{id}.txt");
            using (StreamWriter writer = new StreamWriter(metadataPath))
            {
                writer.WriteLine($"Crash ID: {id}");
                writer.WriteLine($"Time: {DateTime.UtcNow}");
                writer.WriteLine($"Input Size: {input.Length} bytes");
                writer.WriteLine($"Outcome: {testResult.Outcome}");

                if (testResult.Exception != null)
                {
                    writer.WriteLine($"Exception: {testResult.Exception.GetType().FullName}");
                    writer.WriteLine($"Message: {testResult.Exception.Message}");
                    writer.WriteLine($"Stack Trace: {testResult.Exception.StackTrace}");
                }
            }

            return true;
        }

        /// <summary>
        /// Adds a crash to the crashes directory with a custom error message
        /// </summary>
        /// <param name="input">The input data that caused the crash</param>
        /// <param name="errorMessage">The error message</param>
        /// <returns>True if the crash was added, false otherwise</returns>
        public bool AddToCrashes(byte[] input, string errorMessage)
        {
            if (input == null || input.Length == 0)
            {
                return false;
            }

            // Generate a unique ID for the crash
            string id = GenerateInputId(input);

            // If the crash is already in the crashes directory, don't add it again
            if (_crashes.ContainsKey(id))
            {
                return false;
            }

            // Add the crash to the crashes dictionary
            _crashes[id] = input;

            // Save the crash to a file
            string filePath = Path.Combine(_crashesDir, $"{id}.bin");
            File.WriteAllBytes(filePath, input);

            // Create a metadata file with information about the crash
            string metadataPath = Path.Combine(_crashesDir, $"{id}.txt");
            using (StreamWriter writer = new StreamWriter(metadataPath))
            {
                writer.WriteLine($"Crash ID: {id}");
                writer.WriteLine($"Time: {DateTime.UtcNow}");
                writer.WriteLine($"Input Size: {input.Length} bytes");
                writer.WriteLine($"Error: {errorMessage}");
            }

            return true;
        }

        /// <summary>
        /// Gets a random input from the corpus
        /// </summary>
        /// <param name="random">The random number generator</param>
        /// <returns>A random input from the corpus, or an empty array if the corpus is empty</returns>
        public byte[] GetRandomCorpusInput(Random random)
        {
            if (_corpus.Count == 0)
            {
                return Array.Empty<byte>();
            }

            // Get a random input from the corpus
            int index = random.Next(_corpus.Count);
            string id = _corpus.Keys.ElementAt(index);

            return _corpus[id];
        }

        /// <summary>
        /// Gets all inputs from the corpus
        /// </summary>
        /// <returns>All inputs from the corpus</returns>
        public IEnumerable<byte[]> GetAllCorpusInputs()
        {
            return _corpus.Values;
        }

        /// <summary>
        /// Gets all crashes from the corpus
        /// </summary>
        /// <returns>All crashes from the corpus</returns>
        public IEnumerable<byte[]> GetAllCrashes()
        {
            return _crashes.Values;
        }

        /// <summary>
        /// Generates a unique ID for an input
        /// </summary>
        /// <param name="input">The input data</param>
        /// <returns>A unique ID for the input</returns>
        private string GenerateInputId(byte[] input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(input);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            }
        }

        /// <summary>
        /// Minimizes the corpus by removing redundant inputs
        /// </summary>
        /// <returns>The number of inputs removed</returns>
        public int MinimizeCorpus()
        {
            // Get all inputs from the corpus
            var inputs = _corpus.ToList();

            // If the corpus is empty, there's nothing to minimize
            if (inputs.Count == 0)
            {
                return 0;
            }

            // Create a new coverage tracker
            var tempTracker = new CoverageTracker();

            // Sort inputs by size (smallest first)
            inputs.Sort((a, b) => a.Value.Length.CompareTo(b.Value.Length));

            // Keep track of which inputs to keep
            var keepInputs = new HashSet<string>();

            // Add inputs to the new corpus if they increase coverage
            foreach (var input in inputs)
            {
                // Get the coverage points for this input
                string coverageFilePath = Path.Combine(_corpusDir, $"{input.Key}.coverage");
                if (!File.Exists(coverageFilePath))
                {
                    // If there's no coverage file, keep the input
                    keepInputs.Add(input.Key);
                    continue;
                }

                // Load the coverage points
                string[] coveragePoints = File.ReadAllLines(coverageFilePath);
                var coverageSet = new HashSet<string>(coveragePoints);

                // Check if this input increases coverage
                bool isInteresting = tempTracker.UpdateCoverage(coverageSet);

                // If the input is interesting, keep it
                if (isInteresting)
                {
                    keepInputs.Add(input.Key);
                }
            }

            // Remove inputs that are not needed
            int removedCount = 0;
            foreach (var input in inputs)
            {
                if (!keepInputs.Contains(input.Key))
                {
                    // Remove the input from the corpus
                    _corpus.TryRemove(input.Key, out _);

                    // Delete the input file
                    string filePath = Path.Combine(_corpusDir, $"{input.Key}.bin");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // Delete the coverage file
                    string coverageFilePath = Path.Combine(_corpusDir, $"{input.Key}.coverage");
                    if (File.Exists(coverageFilePath))
                    {
                        File.Delete(coverageFilePath);
                    }

                    removedCount++;
                }
            }

            return removedCount;
        }
    }
}

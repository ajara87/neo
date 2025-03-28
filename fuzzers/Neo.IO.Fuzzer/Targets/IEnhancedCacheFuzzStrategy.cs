// Copyright (C) 2015-2025 The Neo Project.
//
// IEnhancedCacheFuzzStrategy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Fuzzer.Utilities;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Interface for enhanced cache fuzzing strategies that use the improved utilities.
    /// Extends the base ICacheFuzzStrategy interface with additional functionality.
    /// </summary>
    public interface IEnhancedCacheFuzzStrategy : ICacheFuzzStrategy
    {
        /// <summary>
        /// Gets the coverage tracker used by this strategy.
        /// </summary>
        /// <returns>The coverage tracker.</returns>
        CoverageTrackerHelper GetCoverageTracker();
    }
}

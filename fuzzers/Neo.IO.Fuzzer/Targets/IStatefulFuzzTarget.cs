// Copyright (C) 2015-2025 The Neo Project.
//
// IStatefulFuzzTarget.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.IO.Fuzzer.Targets
{
    /// <summary>
    /// Interface for fuzz targets that maintain state between operations
    /// </summary>
    public interface IStatefulFuzzTarget : IFuzzTarget
    {
        /// <summary>
        /// Resets the target's state to its initial state
        /// </summary>
        void ResetState();

        /// <summary>
        /// Gets the current sequence of operations that have been performed
        /// </summary>
        /// <returns>The current operation sequence</returns>
        IEnumerable<string> GetCurrentSequence();

        /// <summary>
        /// Gets the failures that have been detected during fuzzing
        /// </summary>
        /// <returns>The detected failures</returns>
        IEnumerable<(IEnumerable<string> Operations, string FailureReason)> GetFailures();
    }
}

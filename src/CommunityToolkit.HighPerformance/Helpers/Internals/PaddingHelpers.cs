// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace CommunityToolkit.HighPerformance.Helpers.Internals;

/// <summary>A placeholder class for common padding constants and eventually routines.</summary>
internal static class PaddingHelpers
{
    /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
    internal const int CACHE_LINE_SIZE = 128;
}

/// <summary>Padded reference to an object.</summary>
[StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE)]
internal struct PaddedReference
{
    [FieldOffset(0)]
    public object? Object;
}

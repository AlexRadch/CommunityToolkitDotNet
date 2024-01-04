// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// Provides processor-local storage of data.
/// </summary>
/// <typeparam name="T">Specifies the type of data stored per logical processor.</typeparam>
public class ProcessorLocal<T> : IDisposable
{
    /// <summary>
    /// Initializes the <see cref="ProcessorLocal{T}"/> instance.
    /// </summary>
    protected ProcessorLocal()
    {
    }

}

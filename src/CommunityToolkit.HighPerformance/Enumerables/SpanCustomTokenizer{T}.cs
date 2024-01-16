// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.HighPerformance.Enumerables;

#pragma warning disable IDE0057 // Use range operator

/// <summary>
/// Looks for the next token in the source.
/// </summary>
/// <typeparam name="T">The type of elements in <paramref name="source"/>.</typeparam>
/// <param name="source">The source <see cref="Span{T}"/> instance.</param>
/// <returns>
/// A tuple containing the starting and exclusive ending indexes of the next token.
/// If the token is not found then the starting index ending index should be equal to starting index.
/// If the search goes forward, then the starting index should be &lt; ending index.
/// If the search goes reverse, then the starting index should be > ending index.
/// </returns>
public delegate (int startIndex, int endIndex) GetNextTokenIndexesFunc<T>(Span<T> source);

/// <summary>
/// Span tokenizer that use <see cref="GetNextTokenIndexesFunc{T}"/> delegate to get all tokens in a source span.
/// Span tokenizer satisfies to <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-foreach-statement">foreach statement pattern</see>.
/// </summary>
/// <typeparam name="T">The type of elements in the span.</typeparam>
public ref struct SpanCustomTokenizer<T>
    where T : IEquatable<T>
{
    // The current source Span[T] instance.
    private Span<T> source;

    // Offset of the current source relative to the original
    private int sourceOffset;

    /// <summary>
    /// Range of the current token in the source.
    /// </summary>
    private Range range;

    /// <summary>
    /// The <see cref="GetNextTokenIndexesFunc{T}"/> delegate to get next token in the source.
    /// </summary>
    private readonly GetNextTokenIndexesFunc<T> getNextTokenFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanCustomTokenizer{T}"/> struct.
    /// </summary>
    /// <param name="source">The source <see cref="Span{T}"/> instance.</param>
    /// <param name="getNextTokenFunc">The <see cref="GetNextTokenIndexesFunc{T}"/> delegate to get next token in the <paramref name="source"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanCustomTokenizer(Span<T> source, GetNextTokenIndexesFunc<T> getNextTokenFunc)
    {
        this.source = source;
        this.sourceOffset = 0;
        this.range = default;
        this.getNextTokenFunc = getNextTokenFunc;
    }

    /// <summary>
    /// Implements the duck-typed <see cref="IEnumerable{T}.GetEnumerator"/> method.
    /// </summary>
    /// <returns>An <see cref="SpanTokenizer{T}"/> instance targeting the current <see cref="Span{T}"/> value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly SpanCustomTokenizer<T> GetEnumerator() => this;

    /// <summary>
    /// Implements the duck-typed <see cref="System.Collections.IEnumerator.MoveNext"/> method.
    /// </summary>
    /// <returns><see langword="true"/> whether a new element is available, <see langword="false"/> otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (this.source.Length <= 0)
        {
            return false;
        }

        (int startIndex, int endIndex) = this.getNextTokenFunc(this.source);
        int len = endIndex - startIndex;
        if (len > 0)
        {
            this.range = new Range(
                new Index(startIndex + this.sourceOffset),
                new Index(endIndex + this.sourceOffset));
            this.source = this.source.Slice(endIndex);
            this.sourceOffset += endIndex;

            return true;
        }
        else if (len > 0)
        {
            this.range = new Range(
                new Index(endIndex + this.sourceOffset + 1),
                new Index(startIndex + this.sourceOffset + 1));
            this.source = this.source.Slice(0, endIndex);

            return true; 
        }

        return false;
    }

    /// <summary>
    /// Gets the duck-typed <see cref="IEnumerator{T}.Current"/> property.
    /// </summary>
    public readonly Range Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.range;
    }
}

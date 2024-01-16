// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
public delegate (int startIndex, int endIndex) SpanGetNextTokenFunc<T>(Span<T> source);

/// <summary>
/// Trim token.
/// </summary>
/// <typeparam name="T">The type of elements in <paramref name="token"/>.</typeparam>
/// <param name="token">The <see cref="Span{T}"/> with token to trim.</param>
/// <returns>
/// A tuple containing the starting and exclusive ending indexes of trimmed token.
/// </returns>
public delegate (int startIndex, int endIndex) SpanTrimFunc<T>(Span<T> token);

/// <summary>
/// Span tokenizer that use <see cref="SpanGetNextTokenFunc{T}"/> delegate to get all tokens in a source span.
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

    // Range of the current token in the source.
#if NETSTANDARD2_1_OR_GREATER
    private Range range;
#else
    private (int Start, int End) range;
#endif

    // The SpanGetNextTokenFunc<T> delegate to get next token in the source.
    private readonly SpanGetNextTokenFunc<T> getNextTokenFunc;

    // The SpanTrimFunc<T> delegate to trim current token.
    private readonly SpanTrimFunc<T>? trimFunc;

    // The flag to skip empty tokens.
    private readonly bool skipEmpty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanCustomTokenizer{T}"/> struct.
    /// </summary>
    /// <param name="source">The source <see cref="Span{T}"/> instance.</param>
    /// <param name="getNextTokenFunc">The <see cref="SpanGetNextTokenFunc{T}"/> delegate to get next token in the <paramref name="source"/>.</param>
    /// <param name="trimFunc">The <see cref="SpanTrimFunc{T}"/>  delegate to trim current token.</param>
    /// <param name="skipEmpty">The flag to skip empty tokens.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanCustomTokenizer(Span<T> source, SpanGetNextTokenFunc<T> getNextTokenFunc, 
        SpanTrimFunc<T>? trimFunc = null, bool skipEmpty = false)
    {
        this.source = source;
        this.sourceOffset = 0;
        this.range = default;
        this.getNextTokenFunc = getNextTokenFunc;
        this.trimFunc = trimFunc;
        this.skipEmpty = skipEmpty;
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
        SkipEmpty:
        if (this.source.Length <= 0)
        {
            return false;
        }

        (int start, int end) = this.getNextTokenFunc(this.source);
        Debug.Assert(start >= -1 && start <= this.source.Length,
            $"SpanCustomTokenizer.getNextTokenFunc return Start {start} out of range [-1, {this.source.Length}]");
        Debug.Assert(end >= -1 && end <= this.source.Length,
            $"SpanCustomTokenizer.endIndex return End {end} out of range [-1, {this.source.Length}]");

        int len = end - start;
        if (len > 0)
        {
            if (this.trimFunc is not null)
            {
                (int startTrimmed, int endTrimmed) = this.getNextTokenFunc(this.source.Slice(start, len));
                Debug.Assert(startTrimmed >= 0 && startTrimmed <= len,
                    $"SpanCustomTokenizer.getNextTokenFunc return Start {startTrimmed} out of range [0, {len}].");
                Debug.Assert(endTrimmed >= startTrimmed && endTrimmed <= len,
                    $"SpanCustomTokenizer.getNextTokenFunc return End {endTrimmed} out of range [{startTrimmed}, {len}].");

                if (this.skipEmpty && endTrimmed <= startTrimmed)
                {
                    this.source = this.source.Slice(end);
                    this.sourceOffset += end;
                    goto SkipEmpty;
                }

#if NETSTANDARD2_1_OR_GREATER
                this.range = new Range(
                    new Index(start + startTrimmed + this.sourceOffset),
                    new Index(start + endTrimmed + this.sourceOffset));
#else
                this.range = (
                    start + startTrimmed + this.sourceOffset,
                    start + endTrimmed + this.sourceOffset);
#endif
            }
            else
            {
#if NETSTANDARD2_1_OR_GREATER
                this.range = new Range(
                    new Index(start + this.sourceOffset),
                    new Index(end + this.sourceOffset));
#else
                this.range = (
                    start + this.sourceOffset,
                    end + this.sourceOffset);
#endif
            }

            this.source = this.source.Slice(end);
            this.sourceOffset += end;
            return true;
        }
        else if (len > 0)
        {
            if (this.trimFunc is not null)
            {
                (int startTrimmed, int endTrimmed) = this.getNextTokenFunc(this.source.Slice(end + 1, len));
                Debug.Assert(startTrimmed >= 0 && startTrimmed <= len,
                    $"SpanCustomTokenizer.getNextTokenFunc return Start {startTrimmed} out of range [0, {len}].");
                Debug.Assert(endTrimmed >= startTrimmed && endTrimmed <= len,
                    $"SpanCustomTokenizer.getNextTokenFunc return End {endTrimmed} out of range [{startTrimmed}, {len}].");

                if (this.skipEmpty && endTrimmed <= startTrimmed)
                {
                    this.source = this.source.Slice(0, end);
                    goto SkipEmpty;
                }

#if NETSTANDARD2_1_OR_GREATER
                this.range = new Range(
                    new Index(end + startTrimmed + this.sourceOffset + 1),
                    new Index(end + endTrimmed + this.sourceOffset + 1));
#else
                this.range = (
                    end + startTrimmed + this.sourceOffset + 1,
                    end + endTrimmed + this.sourceOffset + 1);
#endif
            }
            else
            {
#if NETSTANDARD2_1_OR_GREATER
                this.range = new Range(
                    new Index(end + this.sourceOffset + 1),
                    new Index(start + this.sourceOffset + 1));
#else
                this.range = (
                    end + this.sourceOffset + 1,
                    start + this.sourceOffset + 1);
#endif
            }

            this.source = this.source.Slice(0, end);
            return true;
        }
        else
        {
            start = Math.Max(start + this.sourceOffset, 0);
#if NETSTANDARD2_1_OR_GREATER
            Index index = start;
            this.range = new Range(index, index);
#else
            this.range = (start, start);
#endif
            this.source = Span<T>.Empty;

            return false;
        }
    }

    /// <summary>
    /// Gets the duck-typed <see cref="IEnumerator{T}.Current"/> property.
    /// </summary>
#if NETSTANDARD2_1_OR_GREATER
    public readonly Range Current
#else
    public readonly (int Start, int End) Current
#endif
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.range;
    }
}

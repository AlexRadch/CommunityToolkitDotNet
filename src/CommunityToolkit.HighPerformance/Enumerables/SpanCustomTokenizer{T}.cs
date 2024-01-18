// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.HighPerformance.Enumerables;

#if NETSTANDARD2_1_OR_GREATER
#pragma warning disable IDE0057 // Use range operator
#endif

/// <summary>
/// <see langword="ref"/> <see langword="struct"/> that tokenizes a given <see cref="Span{T}"/> or <see cref="ReadOnlySpan{T}"/> source.
/// It should be used directly within a <see langword="foreach"/> loop.
/// It use <see cref="SpanCustomTokenizer.TokenizeFunc{T}"/> delegate to enumerate all tokens in a source.
/// It use <see cref="SpanCustomTokenizer.TrimFunc{T}"/> delegate to trim tokens.
/// It satisfies to <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-foreach-statement">foreach statement pattern</see>.
/// </summary>
/// <typeparam name="T">The type of elements in the span.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SpanCustomTokenizer{T}"/> struct.
/// </remarks>
/// <param name="source">The source <see cref="ReadOnlySpan{T}"/> instance.</param>
/// <param name="tokenizeFunc">The <see cref="SpanCustomTokenizer.TokenizeFunc{T}"/> delegate to get next token in the <paramref name="source"/>.</param>
/// <param name="trimFunc">The <see cref="SpanCustomTokenizer.TrimFunc{T}"/>  delegate to trim current token.</param>
/// <param name="skipEmpty">The flag to skip empty tokens.</param>
public ref struct SpanCustomTokenizer<T>(ReadOnlySpan<T> source, SpanCustomTokenizer.TokenizeFunc<T> tokenizeFunc,
    SpanCustomTokenizer.TrimFunc<T>? trimFunc = null, bool skipEmpty = false)
    where T : IEquatable<T>
{
    /// <summary>
    /// Not yet tokenized part of a source.
    /// </summary>
    public ReadOnlySpan<T> UntokenizedSourcePart
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    } = source;

    // Offset of the untokenized source relative to the original source
    private int sourceOffset = 0;

    // Range of the current token in the original source.
#if NETSTANDARD2_1_OR_GREATER
    private Range tokenRange = default;
#else
    private (int Start, int End) tokenRange = default;
#endif

    // The delegate to get next token in the source.
    private readonly SpanCustomTokenizer.TokenizeFunc<T> tokenizeFunc = tokenizeFunc;

    // The delegate to trim current token.
    private readonly SpanCustomTokenizer.TrimFunc<T>? trimFunc = trimFunc;

    // The flag to skip empty tokens.
    private readonly bool skipEmpty = skipEmpty;

    /// <summary>
    /// Implements the duck-typed <see cref="IEnumerable{T}.GetEnumerator"/> method.
    /// </summary>
    /// <returns>An <see cref="SpanCustomTokenizer{T}"/> instance targeting <see cref="ReadOnlySpan{T}"/> source.</returns>
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
        if (this.UntokenizedSourcePart.Length <= 0)
        {
            return false;
        }

        ((int Start, int End) token, (int Start, int End) nextSource) = this.tokenizeFunc(this.UntokenizedSourcePart);
        Debug.Assert(token.Start >= 0 && token.Start <= this.UntokenizedSourcePart.Length,
                $"SpanCustomTokenizer.TokenizeFunc return Token.Start {token.Start} out of range [0, {this.UntokenizedSourcePart.Length}]");
        Debug.Assert(token.End >= token.Start && token.End <= this.UntokenizedSourcePart.Length,
                $"SpanCustomTokenizer.TokenizeFunc return Token.End {token.End} out of range [{token.Start}, {this.UntokenizedSourcePart.Length}]");
        Debug.Assert(nextSource.Start >= 0 && nextSource.Start <= this.UntokenizedSourcePart.Length,
                $"SpanCustomTokenizer.TokenizeFunc return NextSource.Start {nextSource.Start} out of range [0, {this.UntokenizedSourcePart.Length}]");
        Debug.Assert(nextSource.End >= nextSource.Start && nextSource.End <= this.UntokenizedSourcePart.Length,
                $"SpanCustomTokenizer.TokenizeFunc return NextSource.End {nextSource.End} out of range [{nextSource.Start}, {this.UntokenizedSourcePart.Length}]");

        int tokenLen = token.End - token.Start;
        if (tokenLen > 0 && this.trimFunc is not null)
        {
            (token.Start, token.End) = this.trimFunc(this.UntokenizedSourcePart.Slice(token.Start, tokenLen));
            Debug.Assert(token.Start >= 0 && token.Start <= tokenLen,
                $"SpanCustomTokenizer.TrimFunc return Start {token.Start} out of range [0, {tokenLen}].");
            Debug.Assert(token.End >= token.Start && token.End <= tokenLen,
                $"SpanCustomTokenizer.TrimFunc return End {token.End} out of range [{token.Start}, {tokenLen}].");
            tokenLen = token.End - token.Start;
        }

        if (tokenLen <= 0 && this.skipEmpty)
        {
            this.UntokenizedSourcePart = this.UntokenizedSourcePart.Slice(nextSource.Start, nextSource.End - nextSource.Start);
            this.sourceOffset += nextSource.Start;
            goto SkipEmpty;
        }

#if NETSTANDARD2_1_OR_GREATER
        this.tokenRange = new Range(token.Start + this.sourceOffset, token.End + this.sourceOffset);
#else
        this.tokenRange = (token.Start + this.sourceOffset, token.End + this.sourceOffset);
#endif
        this.UntokenizedSourcePart = this.UntokenizedSourcePart.Slice(nextSource.Start, nextSource.End - nextSource.Start);
        this.sourceOffset += nextSource.Start;
        return true;
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
        get => this.tokenRange;
    }
}
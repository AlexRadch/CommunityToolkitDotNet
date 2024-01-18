// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace CommunityToolkit.HighPerformance.Enumerables;

//#pragma warning disable IDE0056 // Use index operator
#if NETSTANDARD2_1_OR_GREATER
#pragma warning disable IDE0057 // Use range operator
#endif

/// <summary>
/// Helper class for <see cref="SpanCustomTokenizer{T}"/> structure.
/// </summary>
public static class SpanCustomTokenizer
{
    /// <summary>
    /// Looks for the next token in the source.
    /// </summary>
    /// <typeparam name="T">The type of elements in <paramref name="source"/>.</typeparam>
    /// <param name="source">The source <see cref="ReadOnlySpan{T}"/> instance.</param>
    /// <returns>
    /// A tuple containing (A Range for the founded token, Range for untokenized part of source).
    /// </returns>
    public delegate ((int Start, int End) Token, (int Start, int End) Untokenized) TokenizeFunc<T>(ReadOnlySpan<T> source);

    /// <summary>
    /// Trim token.
    /// </summary>
    /// <typeparam name="T">The type of elements in <paramref name="token"/>.</typeparam>
    /// <param name="token">The <see cref="Span{T}"/> with token to trim.</param>
    /// <returns>A tuple containing the starting and exclusive ending indexes of trimmed token.</returns>
    public delegate (int Start, int End) TrimFunc<T>(ReadOnlySpan<T> token);

    /// <summary>
    /// Creates functions necessary to make the <see cref="SpanCustomTokenizer{T}"/> work with the specified parameters.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="ReadOnlySpan{T}"/> source.</typeparam>
    /// <param name="options">Specifies whether to remove empty tokens and trim tokens.</param>
    /// <param name="separator">An array of delimiting items.</param>
    /// <returns>
    /// Tuple with (<see cref="TokenizeFunc{T}"/>, <see cref="TrimFunc{T}"/>?) functions necessary 
    /// to make the <see cref="SpanCustomTokenizer{T}"/>  work with the specified parameters.
    /// </returns>
    public static (TokenizeFunc<T> tokenizeFunc, TrimFunc<T>? trimFunc)
        CreateTokenizationFunctions<T>(StringSplitOptions options, params T[] separator)
            where T : IEquatable<T>
    {
        return (CreateTokenizeFunc(options, separator), CreateTrimFunc<T>(options));
    }

    #region NextTokenFunc

    /// <summary>
    /// Creates a <see cref="TokenizeFunc{T}"/> function necessary to make the <see cref="SpanCustomTokenizer{T}"/> work with the specified parameters.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="ReadOnlySpan{T}"/> source.</typeparam>
    /// <param name="options">Specifies whether to remove empty tokens and trim tokens.</param>
    /// <param name="separator">An array of delimiting items.</param>
    /// <returns>A <see cref="TokenizeFunc{T}"/> function necessary to make the <see cref="SpanCustomTokenizer{T}"/> work with the specified parameters.</returns>
    /// <exception cref="NotImplementedException">When the required function is not yet implemented.</exception>
    public static TokenizeFunc<T> CreateTokenizeFunc<T>(StringSplitOptions options, params T[] separator)
        where T : IEquatable<T>
    {
        if (!options.HasFlag(StringSplitOptions.RemoveEmptyEntries))
        {
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
#if NET8_0_OR_GREATER
                SearchValues<char> searchValues = SearchValues.Create(Unsafe.As<T[], char[]>(ref separator));
                TokenizeFunc<char> func = (ReadOnlySpan<char> source) => TokenizeForwardSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<char>, TokenizeFunc<T>>(ref func);
#endif
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
#if NET8_0_OR_GREATER
                SearchValues<byte> searchValues = SearchValues.Create(Unsafe.As<T[], byte[]>(ref separator));
                TokenizeFunc<byte> func = (ReadOnlySpan<byte> source) => TokenizeForwardSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<byte>, TokenizeFunc<T>>(ref func);
#endif
            }
        }
        else
        {
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
#if NET8_0_OR_GREATER
                SearchValues<char> searchValues = SearchValues.Create(Unsafe.As<T[], char[]>(ref separator));
                TokenizeFunc<char> func = (ReadOnlySpan<char> source) => TokenizeForwardNotSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<char>, TokenizeFunc<T>>(ref func);
#endif
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
#if NET8_0_OR_GREATER
                SearchValues<byte> searchValues = SearchValues.Create(Unsafe.As<T[], byte[]>(ref separator));
                TokenizeFunc<byte> func = (ReadOnlySpan<byte> source) => TokenizeForwardNotSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<byte>, TokenizeFunc<T>>(ref func);
#endif
            }
        }

        throw new NotImplementedException();
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Looks for the next token in the source.
    /// </summary>
    /// <typeparam name="T">The type of elements in <paramref name="source"/>.</typeparam>
    /// <param name="source">The source <see cref="ReadOnlySpan{T}"/> instance.</param>
    /// <param name="separator">A <see cref="SearchValues{T}"/> of delimiting items.</param>
    /// <returns>
    /// A tuple containing (A Range for the founded token, Range for untokenized part of source).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ((int Start, int End) Token, (int Start, int End) Untokenized) 
        TokenizeForwardSkipEmpty<T>(ReadOnlySpan<T> source, SearchValues<T> separator)
            where T : IEquatable<T>
    {
        int start = source.IndexOfAnyExcept(separator);
        if (start < 0)
        {
            return ((source.Length, source.Length), (source.Length, source.Length));
        }

        ReadOnlySpan<T> source2 = source.Slice(start + 1);
        int end = source2.IndexOfAny(separator);
        if (end < 0)
        {
            return ((start, source.Length), (source.Length, source.Length));
        }

        return ((start, start + end), (start + end + 1, source.Length));
    }

    /// <summary>
    /// Looks for the next token in the source.
    /// </summary>
    /// <typeparam name="T">The type of elements in <paramref name="source"/>.</typeparam>
    /// <param name="source">The source <see cref="ReadOnlySpan{T}"/> instance.</param>
    /// <param name="separator">A <see cref="SearchValues{T}"/> of delimiting items.</param>
    /// <returns>
    /// A tuple containing (A Range for the founded token, Range for untokenized part of source).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ((int Start, int End) Token, (int Start, int End) Untokenized)
        TokenizeForwardNotSkipEmpty<T>(ReadOnlySpan<T> source, SearchValues<T> separator)
            where T : IEquatable<T>
    {
        int index = source.IndexOfAny(separator);
        if (index < 0)
        {
            return ((0, source.Length), (source.Length, source.Length));
        }

        return ((0, index), (index + 1, source.Length));
    }

#endif

    #endregion

    #region TrimFunct

    /// <summary>
    /// Creates a <see cref="TrimFunc{T}"/> function necessary to make the <see cref="SpanCustomTokenizer{T}"/> to trim tokens.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="ReadOnlySpan{T}"/> source.</typeparam>
    /// <param name="options">Specifies whether to remove empty tokens and trim tokens.</param>
    /// <returns>A <see cref="TrimFunc{T}"/> function necessary to make the <see cref="SpanCustomTokenizer{T}"/> to trim tokens.</returns>
    /// <exception cref="NotImplementedException">When the required function is not yet implemented.</exception>
    public static TrimFunc<T>? CreateTrimFunc<T>(StringSplitOptions options)
    {
#if NET5_0_OR_GREATER
        if (!options.HasFlag(StringSplitOptions.TrimEntries))
        {
            return null;
        }

        if (typeof(T) == typeof(char))
        {
            TrimFunc<char> func = Trim;
            return Unsafe.As<TrimFunc<char>, TrimFunc<T>>(ref func);
        }
#endif

        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Start, int End) Trim(ReadOnlySpan<char> token)
    {
        ReadOnlySpan<char> trimmed = token.Trim();
        if (trimmed.IsEmpty)
        {
            return (token.Length, token.Length);
        }

        int start = (int)Unsafe.ByteOffset(ref Unsafe.AsRef(in token.GetPinnableReference()), ref Unsafe.AsRef(in token.GetPinnableReference())) / sizeof(char);
        return (start, start + trimmed.Length);
    }

    //public static (int Start, int End) TrimDefault<T>(ReadOnlySpan<T> token)
    //{
    //    int start = token.IndexOf(default);
    //    if (start < 0)
    //        return (token.Length, token.Length);

    //    return (start, end);
    //}

#endregion
}
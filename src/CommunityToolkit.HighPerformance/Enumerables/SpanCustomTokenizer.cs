// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.HighPerformance.Enumerables;

//#pragma warning disable IDE0056 // Use index operator
#pragma warning disable IDE0057 // Use range operator

public static class SpanCustomTokenizer
{
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
    public delegate ((int Start, int End) Token, (int Start, int End) NextSource) TokenizeFunc<T>(ReadOnlySpan<T> source);

    /// <summary>
    /// Trim token.
    /// </summary>
    /// <typeparam name="T">The type of elements in <paramref name="token"/>.</typeparam>
    /// <param name="token">The <see cref="Span{T}"/> with token to trim.</param>
    /// <returns>
    /// A tuple containing the starting and exclusive ending indexes of trimmed token.
    /// </returns>
    public delegate (int Start, int End) TrimFunc<T>(ReadOnlySpan<T> token);

    public static (TokenizeFunc<T> tokenizeFunc, TrimFunc<T>? trimFunc) 
        CreateTokenizationFuncs<T>(StringSplitOptions options, params T[] separator)
            where T : IEquatable<T>
    {
        return (CreateTokenizeFunc(options, separator), CreateTrimFunc<T>(options));
    }

    #region NextTokenFunc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TokenizeFunc<T> CreateTokenizeFunc<T>(StringSplitOptions options, params T[] separator)
        where T : IEquatable<T>
    {
        if (!options.HasFlag(StringSplitOptions.RemoveEmptyEntries))
        {
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
                SearchValues<char> searchValues = SearchValues.Create(Unsafe.As<T[], char[]>(ref separator));
                TokenizeFunc<char> func = (ReadOnlySpan<char> source) => TokenizeForwardSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<char>, TokenizeFunc<T>>(ref func);
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                SearchValues<byte> searchValues = SearchValues.Create(Unsafe.As<T[], byte[]>(ref separator));
                TokenizeFunc<byte> func = (ReadOnlySpan<byte> source) => TokenizeForwardSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<byte>, TokenizeFunc<T>>(ref func);
            }
        }
        else
        {
            if (typeof(T) == typeof(char) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
                SearchValues<char> searchValues = SearchValues.Create(Unsafe.As<T[], char[]>(ref separator));
                TokenizeFunc<char> func = (ReadOnlySpan<char> source) => TokenizeForwardNotSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<char>, TokenizeFunc<T>>(ref func);
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                SearchValues<byte> searchValues = SearchValues.Create(Unsafe.As<T[], byte[]>(ref separator));
                TokenizeFunc<byte> func = (ReadOnlySpan<byte> source) => TokenizeForwardNotSkipEmpty(source, searchValues);
                return Unsafe.As<TokenizeFunc<byte>, TokenizeFunc<T>>(ref func);
            }
        }

        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ((int Start, int End) Token, (int Start, int End) NextSource) 
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ((int Start, int End) Token, (int Start, int End) NextSource)
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

    #endregion

    #region TrimFunct

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TrimFunc<T>? CreateTrimFunc<T>(StringSplitOptions options)
    {
        if (!options.HasFlag(StringSplitOptions.TrimEntries))
        {
            return null;
        }

        if (typeof(T) == typeof(char))
        {
            TrimFunc<char> func = Trim;
            return Unsafe.As<TrimFunc<char>, TrimFunc<T>>(ref func);
        }

        return null;
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
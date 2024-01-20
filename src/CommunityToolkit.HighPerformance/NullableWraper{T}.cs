// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CommunityToolkit.HighPerformance;

/// <summary>
/// A wrapper for the <see cref="Nullable{T}"/> structure that supports the <see cref="IEquatable{T}"/> and 
/// <see cref="IComparable{T}"/> interfaces.
/// 
/// The most useful methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> require that elements support 
/// the <see cref="IEquatable{T}"/> or <see cref="IComparable{T}"/>interfaces, but the <see cref="Nullable{T}"/> 
/// structure does not have them. Therefore, spans does not support nullable structures well. At the  same time, 
/// ordinary arrays perfectly support nullable structures.
/// 
/// To support span methods for nullable structures, we use this wrapper structure to cast spans with nullable elements 
/// to spans with nullable elements that support the required interfaces. After this, methods for spans become 
/// available.
/// </summary>
/// <typeparam name="T">The underlying value type of the this wrapper structure.</typeparam>
public sealed class NullableWrapper<T>
    where T : struct
{
#pragma warning disable IDE0051 // Remove unused private members
    private readonly bool hasValue; // Do not rename (binary serialization)
    private readonly T value; // Do not rename (binary serialization) or make readonly (can be mutated in ToString, etc.)
#pragma warning restore IDE0051 // Remove unused private members

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableWrapper{T}"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is never used, it is only declared in order to mark it with
    /// the <see langword="private"/> visibility modifier and prevent direct use.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NullableWrapper()
        => throw new InvalidOperationException(
            "The CommunityToolkit.HighPerformance.NullableWrapper<T> constructor should never be used.");

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableWrapper{T}"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is never used, it is only declared in order to mark it with
    /// the <see langword="private"/> visibility modifier and prevent direct use.
    /// </remarks>
#pragma warning disable IDE0051 // Remove unused private members
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NullableWrapper(T value)
        => throw new InvalidOperationException(
            "The CommunityToolkit.HighPerformance.NullableWrapper<T> constructor should never be used.");
#pragma warning restore IDE0051 // Remove unused private members

    /// <summary>
    /// <see cref="Nullable{T}.HasValue"/>
    /// </summary>
    public bool HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => NullableExtensions.AsReadonlyRefToNullable(this).HasValue;
    }

    /// <summary>
    /// <see cref="Nullable{T}.Value"/>
    /// </summary>
    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => NullableExtensions.AsReadonlyRefToNullable(this)!.Value;
    }

    /// <summary>
    /// <see cref="Nullable{T}.GetValueOrDefault()"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.GetValueOrDefault()"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault() => NullableExtensions.AsReadonlyRefToNullable(this).GetValueOrDefault();

    /// <summary>
    /// <see cref="Nullable{T}.GetValueOrDefault(T)"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.GetValueOrDefault(T)"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) 
        => NullableExtensions.AsReadonlyRefToNullable(this).GetValueOrDefault(defaultValue);

    /// <summary>
    /// <see cref="Nullable{T}.Equals"/>
    /// </summary>
    /// <param name="other"><see cref="Nullable{T}.Equals(object?)"/></param>
    /// <returns><see cref="Nullable{T}.Equals(object?)"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other) => NullableExtensions.AsReadonlyRefToNullable(this).Equals(other);

    /// <summary>
    /// <see cref="Nullable{T}.GetHashCode"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.GetHashCode()"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => NullableExtensions.AsReadonlyRefToNullable(this).GetHashCode();

    /// <summary>
    /// <see cref="Nullable{T}.ToString"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.ToString"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString() => NullableExtensions.AsReadonlyRefToNullable(this).ToString();

    /// <summary>
    /// Implicitly creates a new <see cref="NullableWrapper{T}"/> instance from a given <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NullableWrapper<T>(T? value) 
        => NullableExtensions.AsReadonlyRefToNullableWrapper(in value);

    /// <summary>
    /// Implicitly creates a new <see cref="Nullable{T}"/> instance from a given <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T?(NullableWrapper<T> value) => NullableExtensions.AsReadonlyRefToNullable(value);

    /// <summary>
    /// Implicitly creates a new <see cref="NullableWrapper{T}"/> instance from a given <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NullableWrapper<T>(T value) => (NullableWrapper<T>)(T?)value;

    /// <summary>
    /// Explicitly gets the <typeparamref name="T"/> value from a given <see cref="NullableWrapper{T}"/> instance.
    /// </summary>
    /// <param name="value">The input <see cref="NullableWrapper{T}"/> instance.</param>
    public static explicit operator T(NullableWrapper<T> value) => value.Value;
}

public static partial class NullableExtensions
{
    /// <summary>
    /// Reinterprets the given readonly reference to a <see cref="NullableWrapper{T}"/> value as a readonly reference 
    /// to a value of <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The readonly reference to <see cref="NullableWrapper{T}"/> instance to reinterpret.</param>
    /// <returns>A readonly reference to a value of <see cref="Nullable{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T? AsReadonlyRefToNullable<T>(in NullableWrapper<T> source) 
        where T : struct
    => ref Unsafe.As<NullableWrapper<T>, T?>(ref Unsafe.AsRef(in source));

    /// <summary>
    /// Reinterprets the given reference to a <see cref="NullableWrapper{T}"/> value as a reference to a value of 
    /// <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The reference to <see cref="NullableWrapper{T}"/> instance to reinterpret.</param>
    /// <returns>A readonly reference to a value of <see cref="Nullable{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T? AsRefToNullable<T>(ref NullableWrapper<T> source)
        where T : struct
    => ref Unsafe.As<NullableWrapper<T>, T?>(ref source);

    /// <summary>
    /// Reinterprets the given readonly reference to a <see cref="NullableWrapper{T}"/> value as a readonly reference 
    /// to a value of <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The readonly reference to <see cref="Nullable{T}"/> instance to reinterpret.</param>
    /// <returns>A readonly reference to a value of <see cref="NullableWrapper{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly NullableWrapper<T> AsReadonlyRefToNullableWrapper<T>(in T? source)
        where T : struct
    => ref Unsafe.As<T?, NullableWrapper<T>>(ref Unsafe.AsRef(in source));

    /// <summary>
    /// Reinterprets the given reference to a <see cref="NullableWrapper{T}"/> value as a reference to a value of 
    /// <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The reference to <see cref="Nullable{T}"/> instance to reinterpret.</param>
    /// <returns>A reference to a value of <see cref="NullableWrapper{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly NullableWrapper<T> AsRefToNullableWrapper<T>(ref T? source)
        where T : struct
    => ref Unsafe.As<T?, NullableWrapper<T>>(ref source);
}

public static partial class SpanExtensions
{
#if NETSTANDARD2_1_OR_GREATER

    /// <summary>
    /// Creates a <see cref="Span{T}"/> with <see cref="NullableWrapper{T}"/> elements that support the 
    /// <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/> interfaces. After this, many 
    /// <see cref="MemoryExtensions"/> methods for this span become available.
    /// </summary>
    /// <typeparam name="T">The underlying value type of span elements.</typeparam>
    /// <param name="source">A source with <see cref="Nullable{T}"/> elements.</param>
    /// <returns>
    /// A <see cref="Span{T}"/> with <see cref="NullableWrapper{T}"/> elements that support the 
    /// <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/> interfaces.
    /// </returns>
    public static Span<NullableWrapper<T>> WithNullableWrapper<T>(this Span<T?> source)
        where T : struct
    => MemoryMarshal.CreateSpan(ref Unsafe.As<T?, NullableWrapper<T>>(
        ref MemoryMarshal.GetReference(source)), source.Length);

    /// <summary>
    /// Creates a <see cref="ReadOnlySpan{T}"/> with <see cref="NullableWrapper{T}"/> elements that support the 
    /// <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/> interfaces. After this, many 
    /// <see cref="MemoryExtensions"/> methods for this span become available.
    /// </summary>
    /// <typeparam name="T">The underlying value type of span elements.</typeparam>
    /// <param name="source">A source with <see cref="Nullable{T}"/> elements.</param>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}"/> with <see cref="NullableWrapper{T}"/> elements that support the 
    /// <see cref="IEquatable{T}"/> and <see cref="IComparable{T}"/> interfaces.
    /// </returns>
    public static ReadOnlySpan<NullableWrapper<T>> WithNullableWrapper<T>(this ReadOnlySpan<T?> source)
        where T : struct
    => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T?, NullableWrapper<T>>(
        ref MemoryMarshal.GetReference(source)), source.Length);

    /// <summary>
    /// Creates a <see cref="Span{T}"/> with original <see cref="Nullable{T}"/> elements if you need Span with original 
    /// <see cref="Nullable{T}"/> elements after <see cref="WithNullableWrapper{T}(Span{T?})"/> method.
    /// </summary>
    /// <typeparam name="T">The underlying value type of span elements.</typeparam>
    /// <param name="source">A source with <see cref="NullableWrapper{T}"/> elements.</param>
    /// <returns><see cref="Span{T}"/> with <see cref="Nullable{T}"/> elements.</returns>
    public static Span<T?> WithNullable<T>(this Span<NullableWrapper<T>> source)
        where T : struct
    => MemoryMarshal.CreateSpan(ref Unsafe.As<NullableWrapper<T>, T?>(
        ref MemoryMarshal.GetReference(source)), source.Length);

    /// <summary>
    /// Creates a <see cref="Span{T}"/> with original <see cref="Nullable{T}"/> elements if you need Span with original 
    /// <see cref="Nullable{T}"/> elements after <see cref="WithNullableWrapper{T}(Span{T?})"/> method.
    /// </summary>
    /// <typeparam name="T">The underlying value type of span elements.</typeparam>
    /// <param name="source">A source with <see cref="NullableWrapper{T}"/> elements.</param>
    /// <returns><see cref="Span{T}"/> with <see cref="Nullable{T}"/> elements.</returns>
    public static ReadOnlySpan<T?> WithNullable<T>(this ReadOnlySpan<NullableWrapper<T>> source)
        where T : struct
    => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<NullableWrapper<T>, T?>(
        ref MemoryMarshal.GetReference(source)), source.Length);

#endif
}

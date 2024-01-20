// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

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
        get => NullableExtensions.AsNullableReadonlyRef(this).HasValue;
    }

    /// <summary>
    /// <see cref="Nullable{T}.Value"/>
    /// </summary>
    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => NullableExtensions.AsNullableReadonlyRef(this)!.Value;
    }

    /// <summary>
    /// <see cref="Nullable{T}.GetValueOrDefault()"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.GetValueOrDefault()"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault() => NullableExtensions.AsNullableReadonlyRef(this).GetValueOrDefault();

    /// <summary>
    /// <see cref="Nullable{T}.GetValueOrDefault(T)"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.GetValueOrDefault(T)"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) => NullableExtensions.AsNullableReadonlyRef(this).
        GetValueOrDefault(defaultValue);

    /// <summary>
    /// <see cref="Nullable{T}.Equals"/>
    /// </summary>
    /// <param name="other"><see cref="Nullable{T}.Equals(object?)"/></param>
    /// <returns><see cref="Nullable{T}.Equals(object?)"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? other) => NullableExtensions.AsNullableReadonlyRef(this).Equals(other);

    /// <summary>
    /// <see cref="Nullable{T}.GetHashCode"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.GetHashCode()"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => NullableExtensions.AsNullableReadonlyRef(this).GetHashCode();

    /// <summary>
    /// <see cref="Nullable{T}.ToString"/>
    /// </summary>
    /// <returns><see cref="Nullable{T}.ToString"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString() => NullableExtensions.AsNullableReadonlyRef(this).ToString();

    /// <summary>
    /// Implicitly creates a new <see cref="NullableWrapper{T}"/> instance from a given <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NullableWrapper<T>(T value) => NullableExtensions.AsNullableWrapperReadonlyRef(new T?(value));

    /// <summary>
    /// Explicitly gets the <typeparamref name="T"/> value from a given <see cref="NullableWrapper{T}"/> instance.
    /// </summary>
    /// <param name="value">The input <see cref="NullableWrapper{T}"/> instance.</param>
    public static explicit operator T(NullableWrapper<T> value) => NullableExtensions.AsNullableReadonlyRef(value)!.Value;

    /// <summary>
    /// Implicitly creates a new <see cref="NullableWrapper{T}"/> instance from a given <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NullableWrapper<T>(T? value) => NullableExtensions.AsNullableWrapperReadonlyRef(in value);

    /// <summary>
    /// Implicitly creates a new <see cref="Nullable{T}"/> instance from a given <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="value"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T?(NullableWrapper<T> value) => NullableExtensions.AsNullableReadonlyRef(in value);
}

/// <summary>
/// Helpers for working with the <see cref="NullableWrapper{T}"/> type.
/// </summary>
public static partial class NullableExtensions
{
    /// <summary>
    /// Reinterprets the given readonly reference to a <see cref="NullableWrapper{T}"/> value as a readonly reference 
    /// to a value of <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The readonly reference to <see cref="NullableWrapper{T}"/> instance.</param>
    /// <returns>A readonly reference to a value of <see cref="Nullable{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T? AsNullableReadonlyRef<T>(in NullableWrapper<T> source)
        where T : struct
    {
        // The reason why this method is an extension and is not part of
        // the NullableWrapper<T> type itself is that NullableWrapper<T> is
        // really just a mask used over Nullable<T> structure, but it is never
        // actually instantiated. Because of this, the method table of the
        // objects in the heap will be the one of type T created by the runtime,
        // and not the one of the Nullable<T> type. To avoid potential issues
        // when invoking this method on different runtimes, which might handle
        // that scenario differently, we use an extension method, which is just
        // syntactic sugar for a static method belonging to another class. This
        // isn't technically necessary, but it's just an extra precaution since
        // the syntax for users remains exactly the same anyway. Here we just
        // call the Unsafe.Unbox<T>(object) API, which is hidden away for users
        // of the type for simplicity. Note that this API will always actually
        // involve a conditional branch, which is introduced by the JIT compiler
        // to validate the object instance being unboxed. But since the
        // alternative of manually tracking the offset to the boxed data would be
        // both more error prone, and it would still introduce some overhead,
        // this doesn't really matter in this case anyway.
        return ref Unsafe.As<NullableWrapper<T>, T?>(ref Unsafe.AsRef(in source));
    }

    /// <summary>
    /// Reinterprets the given reference to a <see cref="NullableWrapper{T}"/> value as a reference to a value of 
    /// <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The reference to <see cref="NullableWrapper{T}"/> instance.</param>
    /// <returns>A readonly reference to a value of <see cref="Nullable{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T? AsNullableRef<T>(ref NullableWrapper<T> source)
        where T : struct
    {
        // The reason why this method is an extension and is not part of
        // the NullableWrapper<T> type itself is that NullableWrapper<T> is
        // really just a mask used over Nullable<T> structure, but it is never
        // actually instantiated. Because of this, the method table of the
        // objects in the heap will be the one of type T created by the runtime,
        // and not the one of the Nullable<T> type. To avoid potential issues
        // when invoking this method on different runtimes, which might handle
        // that scenario differently, we use an extension method, which is just
        // syntactic sugar for a static method belonging to another class. This
        // isn't technically necessary, but it's just an extra precaution since
        // the syntax for users remains exactly the same anyway. Here we just
        // call the Unsafe.Unbox<T>(object) API, which is hidden away for users
        // of the type for simplicity. Note that this API will always actually
        // involve a conditional branch, which is introduced by the JIT compiler
        // to validate the object instance being unboxed. But since the
        // alternative of manually tracking the offset to the boxed data would be
        // both more error prone, and it would still introduce some overhead,
        // this doesn't really matter in this case anyway.
        return ref Unsafe.As<NullableWrapper<T>, T?>(ref source);
    }

    /// <summary>
    /// Reinterprets the given readonly reference to a <see cref="NullableWrapper{T}"/> value as a readonly reference 
    /// to a value of <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The readonly reference to <see cref="Nullable{T}"/> instance.</param>
    /// <returns>A readonly reference to a value of <see cref="NullableWrapper{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly NullableWrapper<T> AsNullableWrapperReadonlyRef<T>(in T? source)
        where T : struct
    {
        // The reason why this method is an extension and is not part of
        // the NullableWrapper<T> type itself is that NullableWrapper<T> is
        // really just a mask used over Nullable<T> structure, but it is never
        // actually instantiated. Because of this, the method table of the
        // objects in the heap will be the one of type T created by the runtime,
        // and not the one of the Nullable<T> type. To avoid potential issues
        // when invoking this method on different runtimes, which might handle
        // that scenario differently, we use an extension method, which is just
        // syntactic sugar for a static method belonging to another class. This
        // isn't technically necessary, but it's just an extra precaution since
        // the syntax for users remains exactly the same anyway. Here we just
        // call the Unsafe.Unbox<T>(object) API, which is hidden away for users
        // of the type for simplicity. Note that this API will always actually
        // involve a conditional branch, which is introduced by the JIT compiler
        // to validate the object instance being unboxed. But since the
        // alternative of manually tracking the offset to the boxed data would be
        // both more error prone, and it would still introduce some overhead,
        // this doesn't really matter in this case anyway.
        return ref Unsafe.As<T?, NullableWrapper<T>>(ref Unsafe.AsRef(in source));
    }

    /// <summary>
    /// Reinterprets the given reference to a <see cref="NullableWrapper{T}"/> value as a reference to a value of 
    /// <see cref="Nullable{T}"/> type.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The reference to <see cref="Nullable{T}"/> instance.</param>
    /// <returns>A reference to a value of <see cref="NullableWrapper{T}"/> type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly NullableWrapper<T> AsNullableWrapperRef<T>(ref T? source)
        where T : struct
    {
        // The reason why this method is an extension and is not part of
        // the NullableWrapper<T> type itself is that NullableWrapper<T> is
        // really just a mask used over Nullable<T> structure, but it is never
        // actually instantiated. Because of this, the method table of the
        // objects in the heap will be the one of type T created by the runtime,
        // and not the one of the Nullable<T> type. To avoid potential issues
        // when invoking this method on different runtimes, which might handle
        // that scenario differently, we use an extension method, which is just
        // syntactic sugar for a static method belonging to another class. This
        // isn't technically necessary, but it's just an extra precaution since
        // the syntax for users remains exactly the same anyway. Here we just
        // call the Unsafe.Unbox<T>(object) API, which is hidden away for users
        // of the type for simplicity. Note that this API will always actually
        // involve a conditional branch, which is introduced by the JIT compiler
        // to validate the object instance being unboxed. But since the
        // alternative of manually tracking the offset to the boxed data would be
        // both more error prone, and it would still introduce some overhead,
        // this doesn't really matter in this case anyway.
        return ref Unsafe.As<T?, NullableWrapper<T>>(ref source);
    }
}

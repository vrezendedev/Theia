using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Theia.ECS.Blittables;
using Theia.ECS.Entities;

namespace Theia.ECS.Reflection.Types;

/// <summary>
/// Determines whether a type satisfies Theia ECS's stricter notion of <i>blittable</i>: a value
/// type whose entire field graph can be safely copied as raw memory, with no managed references,
/// no platform-dependent layouts, and no primitives whose in-memory representation depends on
/// the runtime (notably <see cref="bool"/> and <see cref="char"/>).
/// </summary>
/// <remarks>
/// <para>
/// This is stricter than the CLR's own definition of "blittable." In particular, <see cref="bool"/>,
/// <see cref="char"/>, <see cref="decimal"/>, <see cref="DateTime"/>, and <see cref="Nullable{T}"/>
/// are all rejected; structs declared with <see cref="LayoutKind.Auto"/> are rejected; and any
/// type containing a static field is rejected outright with an exception, since component types
/// must hold only instance data.
/// </para>
/// <para>
/// Use <see cref="BlittableChar"/> and <see cref="BlittableBoolean"/> in
/// place of <see cref="char"/> and <see cref="bool"/>.
/// </para>
/// <para>
/// Results are memoized in a process-wide <see cref="ConcurrentDictionary{TKey, TValue}"/> so
/// repeated checks against the same type are O(1).
/// </para>
/// </remarks>
internal static class BlittableMeta
{
    /// <summary>
    /// Binding flags used when reflecting over a type's instance fields. Captures public and
    /// non-public instance members; static members are inspected separately and rejected.
    /// </summary>
    internal const BindingFlags Flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly ConcurrentDictionary<Type, bool> s_cachedBlittables;

    static BlittableMeta()
    {
        s_cachedBlittables = new() { };
        s_cachedBlittables[typeof(decimal)] = false;
        s_cachedBlittables[typeof(DateTime)] = false;
        s_cachedBlittables[typeof(TimeSpan)] = true;
        s_cachedBlittables[typeof(BlittableBoolean)] = true;
        s_cachedBlittables[typeof(BlittableChar)] = true;
        s_cachedBlittables[typeof(BlittableRectangle)] = true;
        s_cachedBlittables[typeof(Entity)] = true;
        s_cachedBlittables[typeof(EntityMeta)] = true;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="type"/> is strictly blittable under
    /// Theia ECS's rules; otherwise <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check applies recursively to a struct's fields and to its generic arguments,
    /// short-circuiting on the first non-blittable component. A type is rejected if it is not
    /// a value type, is a generic definition, is a pointer or by-ref, has
    /// <see cref="LayoutKind.Auto"/>, is <see cref="Nullable{T}"/>, or contains any non-blittable field.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="type"/> declares any static field. Static state is incompatible
    /// with the per-instance layout assumptions of component storage.
    /// </exception>
    internal static bool IsStrictlyBlittable(Type type)
    {
        if (type.IsPrimitive)
            return type != typeof(bool) && type != typeof(char);

        if (type.IsEnum)
            return true;

        if (
            !type.IsValueType
            || type.IsGenericTypeDefinition
            || type.IsPointer
            || type.IsByRef
            || type.IsByRefLike
        )
            return false;

        if (s_cachedBlittables.TryGetValue(type, out bool cached))
            return cached;

        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return false;

            foreach (Type generic in type.GetGenericArguments())
            {
                if (!IsStrictlyBlittable(generic))
                {
                    s_cachedBlittables[type] = false;
                    return false;
                }
            }
        }

        if (type.StructLayoutAttribute?.Value is LayoutKind.Auto)
        {
            s_cachedBlittables[type] = false;
            return false;
        }

        if (
            type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            ).Length > 0
        )
            ThrowStaticFieldsNotAllowed(type);

        foreach (FieldInfo field in type.GetFields(Flags))
        {
            if (!IsStrictlyBlittable(field.FieldType))
            {
                s_cachedBlittables[type] = false;
                return false;
            }
        }

        s_cachedBlittables[type] = true;

        return true;
    }

    [DoesNotReturn]
    private static void ThrowStaticFieldsNotAllowed(Type type) =>
        throw new InvalidOperationException(
            $"Type '{type.Name}' cannot declare static fields. Blittable types must contain only instance data."
        );

    /// <summary>
    /// Throws a uniform <see cref="InvalidOperationException"/> describing why
    /// <typeparamref name="T"/> is not an acceptable blittable type. Used by component and
    /// relation registration to surface a consistent diagnostic.
    /// </summary>
    [DoesNotReturn]
    internal static void ThrowBlittableException<T>() =>
        throw new InvalidOperationException(
            $"'{typeof(T).Name}' must be a blittable struct. Ensure it contains only blittable value types and no bools, chars, strings, or reference types."
        );
}

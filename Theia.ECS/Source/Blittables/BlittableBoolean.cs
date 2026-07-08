using System;
using System.Runtime.CompilerServices;
using MessagePack;

namespace Theia.ECS.Blittables;

/// <summary>
/// A <b>strictly blittable</b> replacement for <see cref="bool"/>, suitable for use inside
/// component and relation structs.
/// </summary>
/// <remarks>
/// <para>
/// The CLR's <see cref="bool"/> is rejected by <see cref="Reflection.Types.BlittableMeta.IsStrictlyBlittable(Type)"/>
/// because its in-memory representation is not guaranteed to be a fixed single byte across
/// all runtimes and marshalling contexts.
/// <see cref="BlittableBoolean"/> sidesteps that by storing the value as a <see cref="byte"/>.
/// </para>
/// <para>
/// Implicit conversions to and from <see cref="bool"/> mean it can be used in expressions
/// as if it were a regular boolean, with no friction at the call site.
/// </para>
/// </remarks>
[MessagePackObject(AllowPrivate = true)]
public partial struct BlittableBoolean : IEquatable<BlittableBoolean>
{
    [Key(0)]
    private byte byteValue;

    /// <summary>Returns the underlying value as a <see cref="bool"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Get() => Convert.ToBoolean(byteValue);

    /// <summary>Sets the underlying value from a <see cref="bool"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(bool value) => byteValue = Convert.ToByte(value);

    public readonly bool Equals(BlittableBoolean other) => byteValue == other.byteValue;

    public override readonly bool Equals(object? obj) =>
        obj is BlittableBoolean other && Equals(other);

    public override readonly int GetHashCode() => byteValue.GetHashCode();

    public override readonly string ToString() =>
        $"{nameof(BlittableBoolean)}(byteValue: {byteValue})";

    public static bool operator ==(BlittableBoolean left, BlittableBoolean right) =>
        left.Equals(right);

    public static bool operator !=(BlittableBoolean left, BlittableBoolean right) =>
        !left.Equals(right);

    /// <summary>Implicitly wraps a <see cref="bool"/> so it can be assigned directly to a <see cref="BlittableBoolean"/>.</summary>
    public static implicit operator BlittableBoolean(bool value) =>
        new BlittableBoolean() { byteValue = Convert.ToByte(value) };

    /// <summary>Implicitly unwraps to a <see cref="bool"/> so the value can be used in any boolean context.</summary>
    public static implicit operator bool(BlittableBoolean value) => value.byteValue != 0;
}

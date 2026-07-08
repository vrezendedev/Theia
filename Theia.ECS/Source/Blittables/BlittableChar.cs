using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack;

namespace Theia.ECS.Blittables;

/// <summary>
/// A <b>strictly blittable</b> replacement for <see cref="char"/>, suitable for use inside
/// component and relation structs.
/// </summary>
/// <remarks>
/// <para>
/// The CLR's <see cref="char"/> is rejected by <see cref="Reflection.Types.BlittableMeta.IsStrictlyBlittable(Type)"/>
/// because its marshalled representation depends on the surrounding charset.
/// <see cref="BlittableChar"/> pins the layout via <see cref="LayoutKind.Sequential"/> and <see cref="CharSet.Unicode"/>, so
/// the value is always stored as a fixed two-byte UTF-16 code unit.
/// </para>
/// <para>
/// Implicit conversions to and from <see cref="char"/> mean it can be used in expressions
/// as if it were a regular character, with no friction at the call site.
/// </para>
/// </remarks>
[MessagePackObject(AllowPrivate = true)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public partial struct BlittableChar : IEquatable<BlittableChar>
{
    [Key(0)]
    private char _value;

    /// <summary>Returns the underlying value as a <see cref="char"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char Get() => _value;

    /// <summary>Sets the underlying value from a <see cref="char"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(char value) => _value = value;

    public readonly bool Equals(BlittableChar other) => _value == other._value;

    public override readonly bool Equals(object? obj) => obj is BlittableChar c && Equals(c);

    public override readonly int GetHashCode() => _value.GetHashCode();

    public override readonly string ToString() => $"{nameof(BlittableChar)}(Value: {_value})";

    public static bool operator ==(BlittableChar left, BlittableChar right) => left.Equals(right);

    public static bool operator !=(BlittableChar left, BlittableChar right) => !left.Equals(right);

    /// <summary>Implicitly wraps a <see cref="char"/> so it can be assigned directly to a <see cref="BlittableChar"/>.</summary>
    public static implicit operator BlittableChar(char value) =>
        new BlittableChar { _value = value };

    /// <summary>Implicitly unwraps to a <see cref="char"/> so the value can be used in any character context.</summary>
    public static implicit operator char(BlittableChar value) => value._value;
}

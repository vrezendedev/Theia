using System;
using System.Runtime.CompilerServices;

namespace Theia.ECS.Blittables;

/// <summary>
/// An axis-aligned rectangle defined by a top-left corner and a size, in integer coordinates.
/// </summary>
/// <remarks>
/// The API deliberately mirrors <c>Microsoft.Xna.Framework.Rectangle</c>. The difference is that this type stores
/// only blittable <see cref="int"/> fields (accordingly to Theia's rules) and carries no XNA dependency, so it can live in Theia's ECS
/// blittable storage where XNA <c>Rectangle</c> can't.
/// </remarks>
public struct BlittableRectangle : IEquatable<BlittableRectangle>
{
    /// <summary>X coordinate of the left edge.</summary>
    public int X;

    /// <summary>Y coordinate of the top edge.</summary>
    public int Y;

    /// <summary>Width of the rectangle.</summary>
    public int Width;

    /// <summary>Height of the rectangle.</summary>
    public int Height;

    /// <summary>Initializes a rectangle from a top-left corner and a size.</summary>
    /// <param name="x">X coordinate of the left edge.</param>
    /// <param name="y">Y coordinate of the top edge.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="height">Height of the rectangle.</param>
    public BlittableRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Returns the X coordinate of the left edge.</summary>
    /// <returns><see cref="X"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Left() => X;

    /// <summary>Returns the X coordinate of the right edge.</summary>
    /// <returns><see cref="X"/> plus <see cref="Width"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Right() => X + Width;

    /// <summary>Returns the Y coordinate of the top edge.</summary>
    /// <returns><see cref="Y"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Top() => Y;

    /// <summary>Returns the Y coordinate of the bottom edge.</summary>
    /// <returns><see cref="Y"/> plus <see cref="Height"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Bottom() => Y + Height;

    /// <summary>Returns the center point of the rectangle, using integer division.</summary>
    /// <returns>An <c>(x, y)</c> tuple of the center coordinates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ValueTuple<int, int> Center() => new(X + Width / 2, Y + Height / 2);

    /// <summary>Determines whether the given point lies within the rectangle.</summary>
    /// <param name="point">An <c>(x, y)</c> tuple to test.</param>
    /// <returns><c>true</c> when the point is inside the half-open bounds; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(ValueTuple<int, int> point) =>
        X <= point.Item1 && point.Item1 < X + Width && Y <= point.Item2 && point.Item2 < Y + Height;

    /// <summary>Determines whether the given rectangle lies entirely within this one.</summary>
    /// <param name="other">Rectangle to test for containment.</param>
    /// <returns><c>true</c> when <paramref name="other"/> is fully contained; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(BlittableRectangle other) =>
        other.X >= X
        && other.X + other.Width <= X + Width
        && other.Y >= Y
        && other.Y + other.Height <= Y + Height;

    /// <summary>Determines whether this rectangle overlaps another.</summary>
    /// <param name="other">Rectangle to test against.</param>
    /// <returns><c>true</c> when the two rectangles share any area; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Intersects(BlittableRectangle other) =>
        other.Left() < Right()
        && Left() < other.Right()
        && other.Top() < Bottom()
        && Top() < other.Bottom();

    /// <summary>Computes the overlapping region shared by this rectangle and another, if they intersect.</summary>
    /// <param name="other">Rectangle to intersect with.</param>
    /// <param name="overlap">
    /// When this method returns <c>true</c>, the shared region; otherwise the default (empty) rectangle.
    /// </param>
    /// <returns><c>true</c> when the rectangles overlap; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetOverlapping(BlittableRectangle other, out BlittableRectangle overlap)
    {
        if (Intersects(other))
        {
            int maxX = Math.Max(X, other.X);
            int minRight = Math.Min(X + Width, other.X + other.Width);
            int maxY = Math.Max(Y, other.Y);
            int minBottom = Math.Min(Y + Height, other.Y + other.Height);
            overlap = new BlittableRectangle(maxX, maxY, minRight - maxX, minBottom - maxY);
            return true;
        }
        else
        {
            overlap = default;
            return false;
        }
    }

    /// <summary>Computes the smallest rectangle that fully contains both this rectangle and another.</summary>
    /// <param name="other">Rectangle to union with.</param>
    /// <returns>The bounding rectangle enclosing both.</returns>
    public readonly BlittableRectangle GetUnified(BlittableRectangle other)
    {
        int minX = Math.Min(X, other.X);
        int minY = Math.Min(Y, other.Y);

        return new BlittableRectangle(
            minX,
            minY,
            Math.Max(Right(), other.Right()) - minX,
            Math.Max(Bottom(), other.Bottom()) - minY
        );
    }

    public readonly bool Equals(BlittableRectangle other) => this == other;

    public override readonly bool Equals(object? obj) =>
        obj is BlittableRectangle rectangle && this == rectangle;

    public override readonly int GetHashCode() =>
        (((17 * 23 + X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Width.GetHashCode()) * 23
        + Height.GetHashCode();

    public override readonly string ToString() =>
        $"{nameof(BlittableRectangle)}(X: {X} | Y: {Y} | Width: {Width} | Height: {Height})";

    public static bool operator ==(BlittableRectangle a, BlittableRectangle b) =>
        a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;

    public static bool operator !=(BlittableRectangle a, BlittableRectangle b) => !(a == b);
}

﻿using Newtonsoft.Json.Linq;
using System;
using System.Numerics;

namespace Walgelijk;

/// <summary>
/// Simple rectangle structure
/// </summary>
public struct Rect
{
    /// <summary>
    /// Minimum X point
    /// </summary>
    public float MinX;
    /// <summary>
    /// Minimum Y point
    /// </summary>
    public float MinY;
    /// <summary>
    /// Maximum X point
    /// </summary>
    public float MaxX;
    /// <summary>
    /// Maximum Y point
    /// </summary>
    public float MaxY;

    /// <summary>
    /// Width of rectangle
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public float Width
    {
        get => MaxX - MinX;
        set => MaxX = MinX + value;
    }

    /// <summary>
    /// Height of rectangle
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public float Height
    {
        get => MaxY - MinY;
        set => MaxY = MinY + value;
    }

    /// <summary>
    /// The top right of the rectangle
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public Vector2 TopRight
    {
        get => new(MaxX, MaxY);
        set { MaxX = value.X; MaxY = value.Y; }
    }

    /// <summary>
    /// The bottom right of the rectangle
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public Vector2 BottomRight
    {
        get => new(MaxX, MinY);
        set { MaxX = value.X; MinY = value.Y; }
    }

    /// <summary>
    /// The top left of the rectangle
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public Vector2 TopLeft
    {
        get => new(MinX, MaxY);
        set { MinX = value.X; MaxY = value.Y; }
    }

    /// <summary>
    /// The bottom left of the rectangle
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public Vector2 BottomLeft
    {
        get => new(MinX, MinY);
        set { MinX = value.X; MinY = value.Y; }
    }

    /// <summary>
    /// Returns the center of the rectangle. Calculated using (min + max) * 0.5
    /// </summary>
    public readonly Vector2 GetCenter() => new((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f);

    /// <summary>
    /// Offset the rectangle such that the center is the given point
    /// </summary>
    public void SetCenter(Vector2 point)
    {
        var center = GetCenter();
        var offset = point - center;
        MaxX += offset.X;
        MinX += offset.X;
        MaxY += offset.Y;
        MinY += offset.Y;
    }

    /// <summary>
    /// Returns the size of the rectangle. Calculated using (max - min)
    /// </summary>
    public readonly Vector2 GetSize() => new(MaxX - MinX, MaxY - MinY);

    /// <summary>
    /// Returns a random point inside this rectangle
    /// </summary>
    public readonly Vector2 GetRandomPoint() => new(Utilities.RandomFloat(MinX, MaxX), Utilities.RandomFloat(MinY, MaxY));

    /// <summary>
    /// Create a rectangle
    /// </summary>
    public Rect(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;

        MaxX = maxX;
        MaxY = maxY;
    }

    /// <summary>
    /// Create a rectangle given the center and the size
    /// </summary>
    public Rect(Vector2 center, Vector2 size)
    {
        var halfSize = size / 2;
        MinX = center.X - halfSize.X;
        MinY = center.Y - halfSize.Y;

        MaxX = center.X + halfSize.X;
        MaxY = center.Y + halfSize.Y;
    }

    /// <summary>
    /// Identical to <see cref="SDF.Rectangle(Vector2, Vector2, Vector2)"/>
    /// </summary>
    public readonly float SignedDistanceTo(Vector2 p) => SDF.Rectangle(p, GetCenter(), GetSize());

    /// <summary>
    /// Return a copy of the rectangle but translated by the given amount
    /// </summary>
    public readonly Rect Translate(Vector2 offset)
    {
        return new Rect(MinX + offset.X, MinY + offset.Y, MaxX + offset.X, MaxY + offset.Y);
    }

    /// <summary>
    /// Return a copy of the rectangle but translated by the given amount
    /// </summary>
    public readonly Rect Translate(float x, float y)
    {
        return new Rect(MinX + x, MinY + y, MaxX + x, MaxY + y);
    }

    /// <summary>
    /// Return a copy of the rectangle expanded in all directions by the given amount
    /// </summary>
    public readonly Rect Expand(float f)
    {
        return new Rect(MinX - f, MinY - f, MaxX + f, MaxY + f);
    }

    /// <summary>
    /// Does the rectangle contain the given point?
    /// </summary>
    public readonly bool ContainsPoint(Vector2 point) =>
        point.X > MinX && point.X < MaxX && point.Y > MinY && point.Y < MaxY;

    /// <summary>
    /// Does the rectangle, optionally expanded, contain the given point?
    /// </summary>
    public readonly bool ContainsPoint(Vector2 point, float expand) =>
        point.X > MinX - expand && point.X < MaxX + expand && point.Y > MinY - expand && point.Y < MaxY + expand;

    /// <summary>
    /// Does the rectangle overlap with the given rectangle?
    /// </summary>
    public readonly bool IntersectsRectangle(Rect b) => !(MaxX < b.MinX || MinX > b.MaxX || MinY > b.MaxY || MaxY < b.MinY);

    /// <summary>
    /// Returns the point on the rectangle that is closest to the given point
    /// </summary>
    public readonly Vector2 ClosestPoint(Vector2 point) => new(
            Utilities.Clamp(point.X, MinX, MaxX),
            Utilities.Clamp(point.Y, MinY, MaxY)
            );

    /// <summary>
    /// This will return a copy of this rectangle that is stretched just enough to contain the given point
    /// </summary>
    public readonly Rect StretchToContain(Vector2 point)
    {
        return new Rect(
            MathF.Min(MinX, point.X),
            MathF.Min(MinY, point.Y),
            MathF.Max(MaxX, point.X),
            MathF.Max(MaxY, point.Y));
    }

    /// <summary>
    /// This will return a copy of this rectangle that is stretched just enough to contain the given rect
    /// </summary>
    public readonly Rect StretchToContain(Rect rect)
    {
        return StretchToContain(rect.TopLeft).StretchToContain(rect.TopRight).StretchToContain(rect.BottomLeft).StretchToContain(rect.BottomRight);
    }
}

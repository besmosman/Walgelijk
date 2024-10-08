﻿using System.Collections.Generic;
using System.Numerics;

namespace Walgelijk;

/// <summary>
/// The default vertex used by a <see cref="VertexBuffer"/> if no type parameter is provided.
/// This vertex is generally useful for 2D and 3D objects but keeps it simple.
/// </summary>
public struct Vertex
{
    /// <summary>
    /// Size of an instance of this struct in bytes
    /// </summary>
    public const int Stride = sizeof(float) * (3 + 2 + 3) + Color.Stride;

    /// <summary>
    /// Vertex position
    /// </summary>
    public Vector3 Position;
    /// <summary>
    /// Vertex UV coordinates
    /// </summary>
    public Vector2 TexCoords;
    /// <summary>
    /// Normal vector
    /// </summary>
    public Vector3 Normal;
    /// <summary>
    /// Vertex colour
    /// </summary>
    public Color Color;

    /// <summary>
    /// Create a vertex with a position, UV, and colour
    /// </summary>
    public Vertex(Vector3 position, Vector2 texCoords, Color color)
    {
        Position = position;
        TexCoords = texCoords;
        Color = color;
        Normal = Vector3.Normalize(position);
    }

    /// <summary>
    /// Create a vertex with a position, UV, colour, and normal
    /// </summary>
    public Vertex(Vector3 position, Vector2 texCoords, Color color, Vector3 normal)
    {
        Position = position;
        TexCoords = texCoords;
        Color = color;
        Normal = normal;
    }

    /// <summary>
    /// Create a vertex with a position. White and zero UV by default
    /// </summary>
    /// <param name="position"></param>
    public Vertex(Vector3 position)
    {
        Position = position;
        TexCoords = Vector2.Zero;
        Color = Color.White;
        Normal = Vector3.Normalize(position);

    }

    /// <summary>
    /// Create a vertex with a position.  White and zero UV by default
    /// </summary>
    public Vertex(float x, float y, float z = 0)
    {
        Position = new Vector3(x, y, z);
        TexCoords = Vector2.Zero;
        Color = Color.White;
        Normal = Vector3.Normalize(Position);
    }

    /// <summary>
    /// Create a vertex with a position and a normal.  White and zero UV by default
    /// </summary>
    public Vertex(float x, float y, float z, Vector3 normal)
    {
        Position = new Vector3(x, y, z);
        TexCoords = Vector2.Zero;
        Color = Color.White;
        Normal = normal;
    }

    public readonly struct Descriptor : IVertexDescriptor
    {
        /// <inheritdoc/>
        public IEnumerable<VertexAttributeDescriptor> GetAttributes()
        {
            yield return new VertexAttributeDescriptor(AttributeType.Vector3); // position
            yield return new VertexAttributeDescriptor(AttributeType.Vector2); // uv
            yield return new VertexAttributeDescriptor(AttributeType.Vector3); // normal
            yield return new VertexAttributeDescriptor(AttributeType.Vector4); // color
        }
    }
}

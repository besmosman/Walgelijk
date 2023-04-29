﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace Walgelijk;

/// <summary>
/// Utility class that provides text mesh generation functions 
/// </summary>
public class TextMeshGenerator
{
    /// <summary>
    /// Font to render with
    /// </summary>
    public Font Font = Font.Default;

    /// <summary>
    /// Color to set the vertices with
    /// </summary>
    public Color Color = Color.White;

    /// <summary>
    /// Tracking multiplier
    /// </summary>
    public float TrackingMultiplier = 1;

    /// <summary>
    /// Kerning multiplier
    /// </summary>
    public float KerningMultiplier = 1;

    /// <summary>
    /// Line height multiplier
    /// </summary>
    public float LineHeightMultiplier = 1;

    /// <summary>
    /// The width at which the text needs to start wrapping. Infinity by default.
    /// </summary>
    public float WrappingWidth = float.PositiveInfinity;

    /// <summary>
    /// How to vertically align the text
    /// </summary>
    public HorizontalTextAlign HorizontalAlign = HorizontalTextAlign.Left;

    /// <summary>
    /// How to vertically align the text
    /// </summary>
    public VerticalTextAlign VerticalAlign = VerticalTextAlign.Top;

    /// <summary>
    /// Parse rich text tags
    /// </summary>
    public bool ParseRichText = false;

    /// <summary>
    /// The generator will consider newlines if this is true. If false, it'll just ignore them and stay on one line.
    /// </summary>
    public bool Multiline = true;

    private static ReadOnlySpan<char> GetTextUntilWhitespace(ReadOnlySpan<char> text)
    {
        if (text.Length > 1)
            for (int i = 1; i < text.Length; i++)
                if (char.IsWhiteSpace(text[i]) || text[i] == '\r' || text[i] == '\n')
                    return text[..(i)];
        return text;
    }

    /// <summary>
    /// Return the total predicted width for a line of text ignoring all textbox and alignment settings
    /// </summary>
    public float CalculateTextWidth(ReadOnlySpan<char> text)
    {
        float pos = 0;
        int tagStack = 0;
        char lastChar = default;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsControl(c))
                continue;

            if (ParseRichText)
            {
                if (c == '<' && !(i > 1 && text[i - 1] == '\\')) //check for escape slash //TODO niet alle dingen in een <..> moeten weg zijn. een vector string is bv <x, y> en die wordt dus niet gerendered
                    tagStack++;
                if (c == '>' && !(i > 1 && text[i - 1] == '\\'))
                {
                    tagStack--;
                    continue;
                }
                tagStack = Math.Max(0, tagStack);
            }

            if (tagStack > 0)
                continue;

            var glyph = Font.GetGlyph(c);
            Kerning kerning = i == 0 ? default : Font.GetKerning(lastChar, c);
            lastChar = c;

            pos += glyph.Advance * TrackingMultiplier + kerning.Amount * KerningMultiplier;
        }
        return pos;
    }

    /// <summary>
    /// Return the total predicted height of a line according to line count and line height, accounting for textbox and alignment settings
    /// </summary>
    public float CalculateTextHeight(ReadOnlySpan<char> displayString)
    {
        float cursor = 0;
        float width = Font.Width;
        float height = Font.Height;
        char lastChar = default;
        int line = 0;
        int glyphCountWithoutTags = 0;
        int lastLineLetterStartIndex = 0;
        for (int i = 0; i < displayString.Length; i++)
        {
            var c = displayString[i];

            switch (c)
            {
                case '<': //Possible tag
                    if (!ParseRichText)
                        break;
                    if (i > 0 && displayString[i - 1] == '\\')//check for escape slash
                        break;
                    int closingArrowDistance = displayString[i..].IndexOf('>');
                    if (closingArrowDistance == -1)//invalid syntax
                        break;
                    i += closingArrowDistance;
                    continue;
                case '\0':
                case '\r':
                    continue;
                case '\n':
                    if (Multiline)
                        startNewLine(i, displayString);
                    continue;
            }

            if (char.IsControl(c))
                continue;

            if (char.IsWhiteSpace(c))
            {
                var cursorPosUntilNextWord = cursor + CalculateTextWidth(GetTextUntilWhitespace(displayString[i..]));
                if (cursorPosUntilNextWord > WrappingWidth)
                {
                    startNewLine(i, displayString);
                    continue;
                }
            }
            else if (cursor > WrappingWidth)
            {
                startNewLine(i, displayString);
                //continue;
            }

            var glyph = Font.GetGlyph(c);
            Kerning kerning = i == 0 ? default : Font.GetKerning(lastChar, c);
            var pos = new Vector3(cursor + glyph.XOffset + kerning.Amount * KerningMultiplier, -glyph.YOffset - (line * Font.LineHeight * LineHeightMultiplier), 0);
            glyphCountWithoutTags++;
            cursor += glyph.Advance * TrackingMultiplier;
            lastChar = c;
        }

        if (displayString.Length > 1)
            startNewLine(displayString.Length, displayString);

        return Math.Max(0, line) * Font.LineHeight * LineHeightMultiplier;

        void startNewLine(int i, ReadOnlySpan<char> str)
        {
            line++;
            cursor = 0;
            lastLineLetterStartIndex = i + 1;
            lastChar = default;
        }
    }

    /// <summary>
    /// Generate 2D text mesh. Returns a structure containing some results
    /// </summary>
    public TextMeshResult Generate(string displayString, Vertex[] vertices, uint[] indices, IList<ColourInstruction>? colours = null) =>
        Generate(displayString.AsSpan(), vertices, indices, colours);

    /// <summary>
    /// Generate 2D text mesh.
    /// </summary>
    /// <param name="displayString">Text to render</param>
    /// <param name="vertices">Vertex array that will be populated. This needs to be the length of displayString * 4</param>
    /// <param name="indices">Index array that will be populated. This needs to be the length of displayString * 6</param>
    /// <param name="colours">Colours to set at indices</param>
    public TextMeshResult Generate(ReadOnlySpan<char> displayString, Vertex[] vertices, uint[] indices, IList<ColourInstruction>? colours = null)
    {
        float cursor = 0;
        float width = Font.Width;
        float height = Font.Height;

        var geometryBounds = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        var textBounds = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

        float skew = 0;

        uint vertexIndex = 0;
        uint indexIndex = 0;
        char lastChar = default;
        int line = 0;

        var colorToSet = Color;
        int glyphCountWithoutTags = 0;

        int lastLineLetterStartIndex = 0;
        int vertexCountSinceNewLine = 0;

        for (int i = 0; i < displayString.Length; i++)
        {
            var c = displayString[i];

            switch (c)
            {
                case '<': //Possible tag
                    if (!ParseRichText)
                        break;
                    if (i > 0 && displayString[i - 1] == '\\')//check for escape slash
                        break;
                    int closingArrowDistance = displayString[i..].IndexOf('>');
                    if (closingArrowDistance == -1)//invalid syntax
                        break;
                    executeTag(displayString[(i + 1)..(closingArrowDistance + i)]);
                    i += closingArrowDistance;
                    continue;
                case '\0':
                case '\r':
                    continue;
                case '\n':
                    startNewLine(i, displayString);
                    continue;
                    //case '\t':
                    //    float tabSize = Font.Size * 5;
                    //    cursor = MathF.Ceiling(cursor / tabSize) * tabSize;
                    //    lastChar = default;
                    //    continue;
                    //TODO andere escape character handlers
            }

            if (char.IsControl(c)) //if its a control character that hasnt already been handled then skip it completely
                continue;

            if (char.IsWhiteSpace(c))
            {
                var cursorPosUntilNextWord = cursor + CalculateTextWidth(GetTextUntilWhitespace(displayString[i..]));
                if (cursorPosUntilNextWord > WrappingWidth)
                {
                    startNewLine(i, displayString);
                    continue;
                }
            }
            else if (cursor > WrappingWidth)
            {
                startNewLine(i, displayString);
                //continue;
            }

            var glyph = Font.GetGlyph(c);
            Kerning kerning = i == 0 ? default : Font.GetKerning(lastChar, c);
            var rawCursor = new Vector2(cursor, -(line * Font.LineHeight * LineHeightMultiplier));
            var pos = new Vector3(
                rawCursor.X + glyph.XOffset + kerning.Amount * KerningMultiplier,
                -glyph.YOffset + rawCursor.Y,
                0);

            GlyphUVInfo uvInfo = new(glyph.X / width, glyph.Y / height, glyph.Width / width, glyph.Height / height);

            if (colours != null)
                foreach (var ce in colours)
                    if (ce.CharIndex == i)
                        colorToSet = ce.Colour * Color;

            textBounds = textBounds.StretchToContain(pos.XY() with { Y = rawCursor.Y });
            textBounds = textBounds.StretchToContain(pos.XY() with { Y = rawCursor.Y - Font.Base });
            textBounds = textBounds.StretchToContain(pos.XY() with { X = rawCursor.X + glyph.Width });

            vertices[vertexIndex + 0] = appendVertex(pos, glyph, uvInfo, colorToSet, 0, 0);
            vertices[vertexIndex + 1] = appendVertex(pos, glyph, uvInfo, colorToSet, 1, 0);
            vertices[vertexIndex + 2] = appendVertex(pos, glyph, uvInfo, colorToSet, 1, 1, skew);
            vertices[vertexIndex + 3] = appendVertex(pos, glyph, uvInfo, colorToSet, 0, 1, skew);

            indices[indexIndex + 0] = vertexIndex + 0;
            indices[indexIndex + 1] = vertexIndex + 1;
            indices[indexIndex + 2] = vertexIndex + 2;

            indices[indexIndex + 3] = vertexIndex + 0;
            indices[indexIndex + 4] = vertexIndex + 3;
            indices[indexIndex + 5] = vertexIndex + 2;

            vertexCountSinceNewLine += 4;
            glyphCountWithoutTags++;

            vertexIndex += 4;
            indexIndex += 6;
            cursor += glyph.Advance * TrackingMultiplier;

            lastChar = c;
        }

        if (displayString.Length > 1)
            startNewLine(displayString.Length, displayString);

        //TODO dit is niet goed
        if (VerticalAlign != VerticalTextAlign.Top)
        {
            var textBoundsOffset = textBounds.Height;

            switch (VerticalAlign)
            {
                case VerticalTextAlign.Middle:
                    textBoundsOffset /= 2;
                    break;
            }

            textBounds.MinY += textBoundsOffset;
            textBounds.MaxY += textBoundsOffset;

            for (int i = 0; i < vertexIndex; i++)
                vertices[i].Position.Y += textBoundsOffset;
        }

        if (HorizontalAlign != HorizontalTextAlign.Left)
        {
            var textBoundsOffset = textBounds.Width;

            switch (HorizontalAlign)
            {
                case HorizontalTextAlign.Center:
                    textBoundsOffset /= 2;
                    break;
            }

            textBounds.MinX -= textBoundsOffset;
            textBounds.MaxX -= textBoundsOffset;
        }

        geometryBounds = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
        for (int i = 0; i < vertexIndex; i++)
            geometryBounds = geometryBounds.StretchToContain(vertices[i].Position.XY());

        return new TextMeshResult
        {
            LocalBounds = geometryBounds,
            LocalTextBounds = textBounds,
            GlyphCount = glyphCountWithoutTags,
            VertexCount = glyphCountWithoutTags * 4,
            IndexCount = glyphCountWithoutTags * 6,
        };

        Vertex appendVertex(Vector3 pos, Glyph glyph, GlyphUVInfo uvInfo, Color color, float xFactor, float yFactor, float skew = 0)
        {
            var vertex = new Vertex(
                pos + new Vector3(glyph.Width * xFactor - skew, -glyph.Height * yFactor, 0),
                new Vector2(uvInfo.X + uvInfo.Width * xFactor, uvInfo.Y + uvInfo.Height * yFactor),
                color
                );

            return vertex;
        }

        void executeTag(ReadOnlySpan<char> tagContents)
        {
            bool isClosingTag = tagContents[0] == '/';
            if (isClosingTag)
                tagContents = tagContents[1..];

            if (tagContents.StartsWith(RichTextTags.Colour)) //Colour tag
            {
                colorToSet = isClosingTag ? Color : new Color(tagContents[(RichTextTags.Colour.Length + 1)..]);//+1 to remove the equal sign
                return;
            }

            if (tagContents.StartsWith(RichTextTags.Italic)) //Italics tag
            {
                skew = isClosingTag ? 0 : Font.Size * /*SkewIntensity*/ 0.1f;
                return;
            }
        }

        void startNewLine(int i, ReadOnlySpan<char> str)
        {
            if (HorizontalAlign != HorizontalTextAlign.Left) //no need to do alignment if its left aligned
            {
                int start = lastLineLetterStartIndex;
                int end = i;
                var part = str[start..end];
                var lengthOfLastLine = (int)CalculateTextWidth(part);
                var verticesInLine = vertices.AsSpan()[((int)vertexIndex - vertexCountSinceNewLine)..(int)vertexIndex];

                var offset = HorizontalAlign switch
                {
                    HorizontalTextAlign.Right => lengthOfLastLine,
                    HorizontalTextAlign.Center => lengthOfLastLine / 2,
                    _ => 0
                };

                for (int v = 0; v < verticesInLine.Length; v++)
                {
                    verticesInLine[v].Position.X -= offset;
                }
            }

            line++;
            cursor = 0;
            lastLineLetterStartIndex = i + 1;
            vertexCountSinceNewLine = 0;
            lastChar = default;
        }
    }

    /// <summary>
    /// Instruction that tells the generator when to set a colour
    /// </summary>
    public readonly struct ColourInstruction : IEquatable<ColourInstruction>
    {
        /// <summary>
        /// Character index at which to set the colour
        /// </summary>
        public readonly int CharIndex;

        /// <summary>
        /// Colour to set when we reach <see cref="CharIndex"/>
        /// </summary>
        public readonly Color Colour;

        public ColourInstruction(int charIndex, Color colour)
        {
            CharIndex = charIndex;
            Colour = colour;
        }

        public override bool Equals(object? obj)
        {
            return obj is ColourInstruction instruction && Equals(instruction);
        }

        public bool Equals(ColourInstruction other)
        {
            return CharIndex == other.CharIndex &&
                   Colour.Equals(other.Colour);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CharIndex, Colour);
        }

        public static bool operator ==(ColourInstruction left, ColourInstruction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColourInstruction left, ColourInstruction right)
        {
            return !(left == right);
        }
    }
}

internal struct GlyphUVInfo
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public GlyphUVInfo(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    public override bool Equals(object? obj)
    {
        return obj is GlyphUVInfo info &&
               X == info.X &&
               Y == info.Y &&
               Width == info.Width &&
               Height == info.Height;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    public static bool operator ==(GlyphUVInfo left, GlyphUVInfo right)
    {
        return left.X == right.X &&
               left.Y == right.Y &&
               left.Width == right.Width &&
               left.Height == right.Height;
    }

    public static bool operator !=(GlyphUVInfo left, GlyphUVInfo right)
    {
        return !(left == right);
    }
}

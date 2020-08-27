﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Walgelijk.Text
{
    internal struct FontLoader
    {
        public static Font LoadFromMetadata(string metadataPath)
        {
            string[] text = File.ReadAllLines(metadataPath);

            Font font = new Font();

            var info = GetInfo(text);

            font.Name = info.Name;
            font.Size = info.Size;
            font.Bold = info.Bold;
            font.Italic = info.Italic;
            font.Width = info.Width;
            font.Height = info.Height;
            font.LineHeight = info.LineHeight;

            ParsePages(metadataPath, font, info);
            ParseGlyphs(text, font, info);
            ParseKernings(text, font, info);

            font.Material = GetSDFFontMaterial(font);

            return font;
        }

        public static Material GetSDFFontMaterial(Font font)
        {
            string vert = ShaderConstants.DefaultVertex;
            string frag =
@"#version 460

in vec2 uv;
in vec4 vertexColor;

out vec4 color;

uniform sampler2D mainTex;
uniform float thickness;
uniform float softness;

void main()
{
    vec4 tex = texture(mainTex, uv);
    float t = clamp(1-thickness, 0, 1);
    tex.a = smoothstep(t-softness, t, tex.a);
    color = vertexColor * tex;
}";
            Material mat = new Material(new Shader(vert, frag));
            mat.SetUniform("mainTex", font.Pages[0]);
            mat.SetUniform("thickness", 0.5f);
            mat.SetUniform("softness", 0.02f);
            return mat;
        }

        private static void ParsePages(string metadataPath, in Font font, FontInfo info)
        {
            font.Pages = new Texture[info.PageCount];
            for (int i = 0; i < info.PageCount; i++)
            {
                string path = Path.Combine(Path.GetDirectoryName(metadataPath), info.PagePaths[i]);
                font.Pages[i] = Texture.Load(path, false);
            }
        }

        private static void ParseKernings(string[] text, in Font font, FontInfo info)
        {
            font.Kernings = new Dictionary<KerningPair, Kerning>();

            var kerningLines = GetLines("kerning ", text);
            for (int i = 0; i < info.KerningCount; i++)
            {
                var line = kerningLines[i];
                Kerning kerning;

                kerning.FirstChar = (char)GetIntFrom("first", line);
                kerning.SecondChar = (char)GetIntFrom("second", line);
                kerning.Amount = GetIntFrom("amount", line);

                font.Kernings.Add(new KerningPair {CurrentChar = kerning.FirstChar, PreviousChar = kerning.SecondChar}, kerning);
            }
        }

        private static void ParseGlyphs(string[] text, in Font font, FontInfo info)
        {
            font.Glyphs = new Dictionary<char, Glyph>();
            var charLines = GetLines("char ", text);
            for (int i = 0; i < info.GlyphCount; i++)
            {
                var line = charLines[i];
                Glyph glyph;

                glyph.Identity = (char)GetIntFrom("id", line);
                glyph.X = GetIntFrom("x", line);
                glyph.Y = GetIntFrom("y", line);
                glyph.Width = GetIntFrom("width", line);
                glyph.Height = GetIntFrom("height", line);
                glyph.XOffset = GetIntFrom("xoffset", line);
                glyph.YOffset = GetIntFrom("yoffset", line);
                glyph.Advance = GetIntFrom("xadvance", line);
                glyph.Page = GetIntFrom("page", line);

                font.Glyphs.Add(glyph.Identity, glyph);
            }
        }

        private static string[] GetLines(string name, string[] text)
        {
            return text.Where(s => s.StartsWith(name)).ToArray();
        }

        private static string GetStringFrom(string name, string line)
        {
            string target = name + "=";

            int index = line.IndexOf(target);
            if (index == -1)
                throw new ArgumentException($"Cannot find variable by name \"{name}\"");
            index += target.Length;

            char delimiter = ' ';
            if (line[index] == '\"')
            {
                delimiter = '\"';
                index++;
            }

            int endIndex = line.IndexOf(delimiter, index);
            if (endIndex == -1)
                endIndex = line.Length;

            //Wat smeert C# mij nou weer aan? js looking ass
            return line[index..endIndex];
        }

        private static int GetIntFrom(string name, string line) => int.Parse(GetStringFrom(name, line));
        private static bool GetBoolFrom(string name, string line) => GetIntFrom(name, line) == 1;

        private static FontInfo GetInfo(string[] text)
        {
            FontInfo data;
            string info = GetLine("info");
            string common = GetLine("common");
            if (info == null) throw new ArgumentException($"Invalid font metadata file: cannot find \"info\" block");

            data.Name = GetStringFrom("face", info);
            data.Size = GetIntFrom("size", info);
            data.Bold = GetBoolFrom("bold", info);
            data.Italic = GetBoolFrom("italic", info);

            data.Width = GetIntFrom("scaleW", common);
            data.Height = GetIntFrom("scaleH", common);
            data.PageCount = GetIntFrom("pages", common);
            data.LineHeight = GetIntFrom("lineHeight", common);
            data.GlyphCount = GetIntFrom("count", GetLine("chars"));

            var kerningsLine = GetLine("kernings");
            if (kerningsLine == null)
                data.KerningCount = 0;
            else
                data.KerningCount = GetIntFrom("count", GetLine("kernings"));

            try
            {
                data.PagePaths = new string[data.PageCount];
                var pageLines = GetLines("page", text);
                for (int i = 0; i < data.PageCount; i++)
                {
                    var line = pageLines[i];
                    int id = GetIntFrom("id", line);
                    data.PagePaths[id] = GetStringFrom("file", line);
                }
            }
            catch (Exception)
            {
                throw new ArgumentException("Malformed font file: incorrect page ID");
            }

            return data;

            string GetLine(string name)
            {
                return text.FirstOrDefault(s => s.StartsWith(name));
            }
        }
    }
}

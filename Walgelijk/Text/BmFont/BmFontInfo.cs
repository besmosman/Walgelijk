﻿namespace Walgelijk.BmFont;

internal struct BmFontInfo
{
    public string Name;
    public int Size;
    public bool Bold;
    public bool Italic;
    public bool Smooth;

    public int PageCount;
    public int Width;
    public int Height;
    public int LineHeight;
    public int Base;

    public string[] PagePaths;

    public int GlyphCount;
    public int KerningCount;
}

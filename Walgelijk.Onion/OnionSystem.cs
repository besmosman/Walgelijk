﻿using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace Walgelijk.Onion;

public class OnionSystem : Walgelijk.System
{
    public bool DebugOverlay = true;

    public override void Initialise()
    {
    }

    public override void Update()
    {
        // end previous frame
        Onion.Tree.End();

        // process
        Onion.Tree.Process(Time.DeltaTime);
        Onion.Input.Update(Input, Time.DeltaTime);
        Onion.Navigator.Process(Onion.Input);

        // next frame
        Onion.Layout.Position(0, 0);
        Onion.Layout.Size(Window.Width, Window.Height);
        Onion.Tree.Start(0, new Dummy()); //Root node
    }

    public override void Render()
    {
        Onion.Tree.Render();

        if (DebugOverlay)
        {
            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Order = RenderOrder.DebugUI;

            DrawCaptures();
            DrawControlTree();
        }
    }

    private void DrawCaptures()
    {
        var p = new Vector2(Window.Width - 32, 32);
        Draw.Colour = Colors.Yellow;
        Draw.Text($"HOVER:  {((Onion.Navigator.HoverControl?.ToString()) ?? "none")}", p, Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
        Draw.Text($"SCROLL: {((Onion.Navigator.ScrollControl?.ToString()) ?? "none")}", p += new Vector2(0, 14), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
        Draw.Text($"FOCUS:  {((Onion.Navigator.FocusedControl?.ToString()) ?? "none")}", p += new Vector2(0, 14), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
        Draw.Text($"ACTIVE: {((Onion.Navigator.ActiveControl?.ToString()) ?? "none")}", p += new Vector2(0, 14), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
    }

    private void DrawControlTree()
    {
        float h = 0.5f;
        var offset = new Vector2(32, 32);
        draw(Onion.Tree.Root);
        void draw(Node node)
        {
            var inst = Onion.Tree.EnsureInstance(node.Identity);
            Draw.Colour = Colors.Gray;
            if (node.AliveLastFrame)
                Draw.Colour = Colors.Yellow;
            if (node.Alive)
                Draw.Colour = Color.FromHsv(h, 0.6f, 1);

            if (Input.IsKeyHeld(Key.Space))
                Draw.Text($"Scroll: {inst.InnerScrollOffset.X}, {inst.InnerScrollOffset.Y}, Y: {inst.Rects.ComputedGlobal.MinY}", offset, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);
            else
                Draw.Text((node.ToString() ?? "[untitled]") + " D: " + node.ComputedGlobalOrder, offset, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);

            Draw.Colour = Colors.Purple.WithAlpha(0.1f);
            Draw.OutlineColour = Colors.Purple;
            Draw.OutlineWidth = 4;
            Draw.Quad(inst.Rects.ChildContent);
            Draw.OutlineWidth = 0;

            offset.X += 32;
            h += 0.15f;
            foreach (var item in node.GetChildren())
            {
                offset.Y += 16;
                draw(item);
            }
            h -= 0.15f;
            offset.X -= 32;
        }
    }
}

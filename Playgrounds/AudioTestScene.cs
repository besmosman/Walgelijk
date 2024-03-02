﻿using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace Playgrounds;

public struct OggStreamerTestScene : ISceneCreator
{
    public Scene Load(Game game)
    {
        var scene = new Scene(game);
        scene.AddSystem(new TestSystem());
        scene.AddSystem(new OnionSystem());

        game.UpdateRate = 120;
        game.FixedUpdateRate = 50;

        return scene;
    }

    public class TestSystem : Walgelijk.System
    {
        public Sound Stream = new Sound(Resources.Load<StreamAudioData>("perfect-loop.ogg"), loops: true);

        public override void Update()
        {
           // Stream.ForceUpdate();

            Ui.Layout.Size(200, 32).Move(0, 0);
            if (Ui.Button("Play"))
                Audio.Play(Stream);

            Ui.Layout.Size(200, 32).Move(200, 0);
            if (Ui.Button("Pause"))
                Audio.Pause(Stream);

            Ui.Layout.Size(200, 32).Move(400, 0);
            if (Ui.Button("Stop"))
                Audio.Stop(Stream);

            Ui.Layout.Size(200,200).Center();
            Ui.Label($"{(Audio.IsPlaying(Stream) ? "playing" : "stopped")}\n{Audio.GetTime(Stream)}" );

            RenderQueue.Add(new ClearRenderTask());
        }
    }
}

public struct AudioTestScene : ISceneCreator
{
    public Scene Load(Game game)
    {
        var scene = new Scene(game);
        scene.AddSystem(new AudioTestSystem());

        game.UpdateRate = 120;
        game.FixedUpdateRate = 50;

        return scene;
    }

    public class AudioTestSystem : Walgelijk.System
    {
        private Sound OneShot = new Sound(Resources.Load<FixedAudioData>("mono_hit.wav"), false, new SpatialParams(1, float.PositiveInfinity, 1));
        private Sound Streaming = new Sound(Resources.Load<StreamAudioData>("perfect-loop.ogg"), true, new SpatialParams(1, float.PositiveInfinity, 1));

        public override void FixedUpdate()
        {
            Audio.DistanceModel = AudioDistanceModel.InverseSquare;
            Audio.SpatialMultiplier = 0.1f;
            if (Input.IsKeyHeld(Key.Space))
            {
                Audio.PlayOnce(OneShot, new Vector3(Utilities.RandomFloat(-100,100),0, 0), 0.1f);
            }
        }

        public override void Update()
        {
            var th = Time.SecondsSinceLoad;
            Audio.ListenerOrientation = (new Vector3(1 * MathF.Cos(th), 0,  1 * MathF.Sin(th)), Vector3.UnitY);
            Audio.SetPosition(Streaming, new Vector3(0, 0, 100));

            if (Input.IsKeyPressed(Key.Q))
                if (Audio.IsPlaying(Streaming))
                    Audio.Pause(Streaming);
                else
                    Audio.Play(Streaming);

            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Colour = Colors.Black;
            Draw.Quad(new Rect(0, 0, Window.Width, Window.Height));

            Draw.Colour = Colors.White;
            if (Audio is Walgelijk.OpenTK.OpenALAudioRenderer audio)
            {
                Draw.Text("Sources in use: " + audio.TemporarySourceBuffer.Count(), new Vector2(30, 30), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                Draw.Text("Sources created: " + audio.CreatedTemporarySourceCount, new Vector2(30, 40), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);
            }

            var p = Audio.GetTime(Streaming) / (float)Streaming.Data.Duration.TotalSeconds;

            Draw.Colour = Colors.Gray.Brightness(0.5f);
            Draw.Quad(new Rect(30, 100, 30 + 500, 120));
            Draw.Colour = Colors.Cyan;
            Draw.Quad(new Rect(30, 100, 30 + 500 * p, 120).Expand(-2));
        }
    }
}
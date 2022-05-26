﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Imgui;
using Walgelijk.OpenTK;
using Walgelijk.ParticleSystem;
using Walgelijk.SimpleDrawing;
using Walgelijk.UI;
using Walgelijk.Video;

namespace Test
{
    class Program
    {
        private static Game game;

        static void Main(string[] args)
        {
            game = new Game(
                new OpenTKWindow("hallo daar", new Vector2(-1, -1), new Vector2(800, 600)),
                new OpenALAudioRenderer()
                );

            game.Window.TargetUpdateRate = 0;
            game.Console.DrawConsoleNotification = true;
            game.Window.VSync = false;

            Resources.RegisterType(typeof(Gif), path => Gif.Load(path));

            Resources.SetBasePathForType<AudioData>("audio");
            Resources.SetBasePathForType<Prefab>("prefabs");
            Resources.SetBasePathForType<Texture>("textures");
            Resources.SetBasePathForType<Font>("fonts");

            //game.Scene = SplashScreen.CreateScene(new[]{
            //    new SplashScreen.Logo(Resources.Load<Texture>("walgelijk.png"), 0.5f),
            //    },
            //() =>
            //{
            //});
            game.Scene = LoadScene2(game);

#if DEBUG
            game.DevelopmentMode = true;
#else
            game.DevelopmentMode = false;
#endif
            game.Window.SetIcon(Resources.Load<Texture>("icon.png"));
            game.Profiling.DrawQuickProfiler = false;

            game.Start();
        }

        private static Scene LoadScene2(Game game)
        {
            Scene scene = new Scene(game);

            var camera = scene.CreateEntity();
            scene.AttachComponent(camera, new TransformComponent());
            scene.AttachComponent(camera, new CameraComponent { PixelsPerUnit = 1, OrthographicSize = 1 });

            var text = scene.CreateEntity();
            scene.SetTag(text, new Tag(234));
            scene.AttachComponent(text, new TransformComponent());
            var generator = scene.AttachComponent(text, new TextComponent(Resources.Load<string>("lorem ipsum.txt")));

            scene.AddSystem(new Walgelijk.Imgui.GuiSystem());
            scene.AddSystem(new TransformSystem());
            scene.AddSystem(new CameraSystem() { ExecutionOrder = -1 });
            scene.AddSystem(new TextWrappingWidthSystem() { ExecutionOrder = -1 });
            scene.AddSystem(new ShapeRendererSystem());
            scene.AddSystem(new DebugCameraSystem());
            scene.AddSystem(new Scene2System());

           // AudioData costolotData = game.AudioRenderer.LoadSound("resources\\audio\\Costolot.ogg");
           // var music = new Sound(costolotData, true, false);
           // game.AudioRenderer.Play(music);

            game.Console.DrawConsoleNotification = false;

            return scene;
        }

        public class Scene2System : Walgelijk.System
        {
            private string[] audioDevices = null;

            public override void Initialise()
            {
                audioDevices = game.AudioRenderer.EnumerateAvailableAudioDevices().ToArray();
            }

            public override void Render()
            {
                Draw.Reset();
                Draw.Order = RenderOrder.Top;
                Draw.Image(Resources.Load<Texture>("qoitest.qoi"), new Rect(Input.WorldMousePosition+ new Vector2(0, 256), new Vector2(512)), ImageContainmentMode.Contain);

                if (Gui.ClickButton("R", new Vector2(100 + 256 + 5, 100), new Vector2(32)))
                    Initialise();

                if (Gui.ClickButton("P", new Vector2(100 + 256 + 5 + 32 + 5, 100), new Vector2(32)))
                {
                    AudioData costolotData = Resources.Load<AudioData>("Costolot.ogg");
                    var music = new Sound(costolotData, true, false);
                    game.AudioRenderer.Play(music);
                }


                Gui.Label(game.AudioRenderer.GetCurrentAudioDevice(), new Vector2(256));

                int i = 0;
                foreach (var item in audioDevices)
                {
                    if (Gui.ClickButton(item, new Vector2(100, 100 + i * (32 + 5)), 
                        new Vector2(256, 32), optionalId: i))
                        game.AudioRenderer.SetAudioDevice(item);
                    i++;
                }
            }
        }

        class TextWrappingWidthSystem : Walgelijk.System
        {
            public override void Update()
            {
                if (Input.IsKeyReleased(Key.F8))
                {
                    using var o = File.Create("test.png");
                    Graphics.Upload(Resources.Load<Texture>("pride.png"));
                    Graphics.SaveTexture(o, Resources.Load<Texture>("pride.png"));
                    o.Close();
                    o.Dispose();
                }

                if (Scene.TryGetEntityWithTag(new Tag(234), out var entity))
                {
                    var pos = Scene.GetComponentFast<TransformComponent>(entity).Position.X;
                    var text = Scene.GetComponentFast<TextComponent>(entity);
                    text.WrappingWidth = MathF.Max(10, Input.WorldMousePosition.X - pos);
                    DebugDraw.Rectangle(text.LocalBoundingBox, 0, Colors.Magenta);
                    //DebugDraw.Text(text.LocalBoundingBox.TopLeft + new Vector2(0, 30), text.TextMeshGenerator.CalculateTextHeight(text.String).ToString(), 3, Colors.Red);
                    DebugDraw.Line(text.LocalBoundingBox.TopLeft + new Vector2(-10 ,0), text.LocalBoundingBox.TopLeft + new Vector2(-10, -text.TextMeshGenerator.CalculateTextHeight(text.String)));
                    if (Input.IsKeyPressed(Key.D1))
                    {
                        switch (text.HorizontalAlignment)
                        {
                            case HorizontalTextAlign.Left:
                                text.HorizontalAlignment = HorizontalTextAlign.Center;
                                break;
                            case HorizontalTextAlign.Center:
                                text.HorizontalAlignment = HorizontalTextAlign.Right;
                                break;
                            case HorizontalTextAlign.Right:
                                text.HorizontalAlignment = HorizontalTextAlign.Left;
                                break;
                        }
                    }

                    if (Input.IsKeyPressed(Key.D2))
                    {
                        switch (text.VerticalAlignment)
                        {
                            case VerticalTextAlign.Top:
                                text.VerticalAlignment = VerticalTextAlign.Middle;
                                break;
                            case VerticalTextAlign.Middle:
                                text.VerticalAlignment = VerticalTextAlign.Bottom;
                                break;
                            case VerticalTextAlign.Bottom:
                                text.VerticalAlignment = VerticalTextAlign.Top;
                                break;
                        }
                    }
                }
            }
        }

        private static Scene LoadScene1(Game game)
        {
            Scene scene = new Scene(game);

            RenderTexture gaming = new RenderTexture(128, 128);
            game.Window.Graphics.CurrentTarget = gaming;
            game.Window.Graphics.Clear(Colors.Purple);
            game.Window.Graphics.Draw(PrimitiveMeshes.Circle, Material.DefaultTextured);

            var camera = scene.CreateEntity();
            scene.AttachComponent(camera, new TransformComponent());
            scene.AttachComponent(camera, new CameraComponent { PixelsPerUnit = 1, OrthographicSize = 0.02f });

            scene.AttachComponent(scene.CreateEntity(), new UiDataComponent());

            scene.AddSystem(new TransformSystem());
            scene.AddSystem(new CameraSystem() { ExecutionOrder = -1 });
            scene.AddSystem(new ShapeRendererSystem());
            scene.AddSystem(new WaveMovementSystem());
            scene.AddSystem(new DebugCameraSystem());
            scene.AddSystem(new ParticleSystem());
            scene.AddSystem(new ElementTransformSystem());
            scene.AddSystem(new ElementEventSystem());
            scene.AddSystem(new ElementRenderingSystem());
            scene.AddSystem(new VideoSystem());
            //     scene.AddSystem(new PostProcessingSystem() { ExecutionOrder = -2 });

            //Background.CreateBackground(scene, gaming);

            scene.AttachComponent(scene.CreateEntity(), new PostProcessingComponent
            {
                Effects = new List<IPostProcessingEffect>()
                {
                    new ShaderPostProcessor(Resources.Load<string>("shaders\\noise.frag")),
                },
                //End = new RenderOrder(100, 0)
            });

            scene.AttachComponent(scene.CreateEntity(), new PostProcessingComponent
            {
                Effects = new List<IPostProcessingEffect>()
                {
                    new ShaderPostProcessor(Resources.Load<string>("shaders\\chromatic_aberration.frag")),
                },
                //Begin = new RenderOrder(100, 1),
            });

            var orgin = scene.CreateEntity();
            scene.AttachComponent(orgin, new TransformComponent { Position = new Vector2(0, 0) });
            scene.AttachComponent(orgin, new RectangleShapeComponent { Color = Colors.Red });
            scene.AttachComponent(orgin, new WaveMovementComponent());

            var x100 = scene.CreateEntity();
            scene.AttachComponent(x100, new TransformComponent { Position = new Vector2(100, 0) });
            scene.AttachComponent(x100, new RectangleShapeComponent { Color = Colors.GreenYellow });
            scene.AttachComponent(x100, new WaveMovementComponent());

            var x50y50 = scene.CreateEntity();
            scene.AttachComponent(x50y50, new TransformComponent { Position = new Vector2(50, 50) });
            scene.AttachComponent(x50y50, new RectangleShapeComponent { Color = Colors.Blue });
            scene.AttachComponent(x50y50, new WaveMovementComponent());

            var agag = scene.CreateEntity();
            scene.AttachComponent(agag, new TransformComponent());
            var ccc = scene.AttachComponent(agag, new ElementComponent
            {
                Anchors = new IAnchor[]
                 {
                    new HorizontalCenterAnchor(),
                    new TopAnchor{Offset = 15},
                 },
                Size = new Vector2(128),
                StyleOverride = new Walgelijk.UI.Style
                {
                    BackgroundColour = new Property<Color>(Colors.Gray, Colors.Magenta, Colors.Magenta * 0.5f)
                },
                Overflow = OverflowBehaviour.Hide
            });

            ccc.SetTextString("hoe <color=#ff0000>is het </color><i>vandaag\nhawiudhauwdhawduawhdiauwhdaiuwhd");
            ccc.Text.Generator.ParseRichText = true;
            ccc.Text.Rebuild();

            var particles = scene.CreateEntity();
            scene.AttachComponent(particles, new TransformComponent());
            scene.AttachComponent(particles, new WaveMovementComponent());

            //var opening = new Sound(Resources.Load<AudioData>("cannot-build.wav"));
            //game.AudioRenderer.PlayOnce(opening);

            // game.AudioRenderer.Play(new Sound(Resources.Load<AudioData>("dexter.ogg"), true));

            var particleComponent = new ParticlesComponent(10000)
            {
                // Dampening = new FloatRange(0.9f),
                RotationalDampening = new FloatRange(0.95f),
                // Gravity = new Vec2Range(Vector2.Zero),
                StartVelocity = new Vec2Range(Vector2.One * -15, Vector2.One * 15),
                EmissionRate = 15,
                StartColor = new ColorRange(Colors.White),
                ColorOverLife = new ColorCurve(new Curve<Color>.Key(Colors.Green, 0), new Curve<Color>.Key(Color.Red, 1f)),
                SizeOverLife = new FloatCurve(new Curve<float>.Key(0, 0), new Curve<float>.Key(1, 0.1f), new Curve<float>.Key(0, 1)),
                LifeRange = new FloatRange(0.3f, .5f),
                WorldSpace = false,
                SimulationSpeed = 1,
                FloorLevel = -4
            };
            //Sound particleHitSound = new Sound(Resources.Load<AudioData>("bounce.ogg"));
            // particleComponent.OnHitFloor.AddListener(p => game.AudioRenderer.PlayOnce(particleHitSound, p.Velocity.Length() * 0.05f));

            scene.AttachComponent(particles, particleComponent);

            //particleComponent.Material.SetUniform(ShaderDefaults.MainTextureUniform, gaming);

            {
                var videoEntity = scene.CreateEntity();
                scene.AttachComponent(videoEntity, new TransformComponent { Scale = new Vector2(9, 16) * 3 });
                var renderer = scene.AttachComponent(videoEntity, new QuadShapeComponent(true));
                var videoComponent = scene.AttachComponent(videoEntity, new GifComponent
                {
                    Gif = Resources.Load<Gif>("test.gif"),
                    IsPlaying = true
                });

                renderer.Material = new Material(Material.DefaultTextured);

                videoComponent.OnTextureInitialised.AddListener(t =>
                {
                    renderer.Material.SetUniform(ShaderDefaults.MainTextureUniform, t);
                });
            }

            {
                Video vid = new Video("resources\\test.mp4");
            }

            return scene;
        }
    }

    [RequiresComponents(typeof(TransformComponent))]
    public class WaveMovementComponent
    {
        public float Amplitude = 25;
        public float Frequency = 2;
        public float Phase;
    }

    public class WaveMovementSystem : Walgelijk.System
    {
        Sound ogg = new Sound(Resources.Load<AudioData>("dexter.ogg"), true);
        Sound wav = new Sound(Resources.Load<AudioData>("dexter.wav"), true);
        //Sound opening = new Sound(Resources.Load<AudioData>("walgelijk.wav"));
        public override void Update()
        {
            var components = Scene.GetAllComponentsOfType<WaveMovementComponent>();
            foreach (var item in components)
            {
                var transform = Scene.GetComponentFast<TransformComponent>(item.Entity);
                var wave = item.Component;

                // transform.Rotation += Time.UpdateDeltaTime * 64;
                DebugDraw.Circle(transform.Position, 3.4f, Colors.Green);
                //transform.Position = new Vector2(MathF.Sin(Time.SecondsSinceLoad * wave.Frequency + wave.Phase) * wave.Amplitude, 0);
                DebugDraw.Text(transform.Position, transform.Position.ToString(), 0.1f);
            }

            if (Input.IsKeyPressed(Key.Q))
            {
                Audio.StopAll();
                Audio.Play(ogg);
            }

            if (Input.IsKeyPressed(Key.E))
            {
                Audio.StopAll();
                Audio.Play(wav);
            }

            Rect a = new Rect(0, 0, 100, 100);
            DebugDraw.Rectangle(a, 0, Colors.Cyan);
            DebugDraw.Circle(Input.WorldMousePosition, 10, Colors.Green);
            DebugDraw.Circle(a.ClosestPoint(Input.WorldMousePosition), 20, a.ContainsPoint(Input.WorldMousePosition) ? Colors.White : Colors.Green);
        }

        public override void Render()
        {
            //DebugDraw.Circle(Vector2.One, .4f, Colors.Green, renderOrder: new RenderOrder(101));

            //if (Utilities.RandomByte() != 200)
            //    return;

            //float l = 10;
            //var end = new Vector2(MathF.Cos(Time.SecondsSinceLoad), MathF.Sin(Time.SecondsSinceLoad)) * l;
            //DebugDraw.Line(default, end, duration: 1, renderOrder: new RenderOrder(101));
        }
    }
}

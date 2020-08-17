﻿namespace Walgelijk
{
    /// <summary>
    /// System that renders basic shapes. Supports <see cref="IBasicShapeComponent"/>
    /// </summary>
    public class BasicRendererSystem : System
    {
        private Game game => Scene.Game;

        public override void Initialise()
        {

        }

        public override void Execute()
        {
            var basicShapes = Scene.GetAllComponentsOfType<IBasicShapeComponent>();

            foreach (var pair in basicShapes)
            {
                var shape = pair.Component;

                if (shape.RenderTask.VertexBuffer != null)
                {
                    var transform = Scene.GetComponentFrom<TransformComponent>(pair.Entity);
                    var task = shape.RenderTask;
                    task.ModelMatrix = transform.LocalToWorldMatrix;
                    game.RenderQueue.Enqueue(task);
                }
            }
        }
    }
}

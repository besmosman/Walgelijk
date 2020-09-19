﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Walgelijk
{

    /// <summary>
    /// Provides performance information
    /// </summary>
    public sealed class Profiler
    {
        /// <summary>
        /// Amount of updates in the last second
        /// </summary>
        public float UpdatesPerSecond => upsCounter.Frequency;
        /// <summary>
        /// Amount of frames rendered in the last second
        /// </summary>
        public float FramesPerSecond => fpsCounter.Frequency;
        /// <summary>
        /// Enables or disables a small debug performance information display
        /// </summary>
        public bool DrawQuickProfiler { get; set; } = true;

        private readonly Game game;
        private readonly QuickProfiler quickProfiler;
        private readonly Stopwatch stopwatch;

        private readonly TickRateCounter upsCounter = new TickRateCounter();
        private readonly TickRateCounter fpsCounter = new TickRateCounter();

        private readonly Stack<ProfiledTask> profiledTaskStack = new Stack<ProfiledTask>();
        private readonly List<ProfiledTask> profiledTasks = new List<ProfiledTask>();

        /// <summary>
        /// Create a profiler for the given game
        /// </summary>
        /// <param name="game"></param>
        public Profiler(Game game)
        {
            this.game = game;
            quickProfiler = new QuickProfiler(this);

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        /// <summary>
        /// Force the profiler to update. Should be handled by the window.
        /// </summary>
        public void Update()
        {
            CalculateUPS();
        }

        /// <summary>
        /// Force the profiler to calculate render information. Should be handled by the window.
        /// </summary>
        public void Render()
        {
            CalculateFPS();

            if (DrawQuickProfiler)
                quickProfiler.Render(game.RenderQueue);

            profiledTasks.Clear();
        }

        /// <summary>
        /// Start a profiled task with a name
        /// </summary>
        public void StartTask(string name)
        {
            profiledTaskStack.Push(new ProfiledTask { Name = name, StartTick = stopwatch.ElapsedTicks });
        }

        /// <summary>
        /// End the ongoing profiled task
        /// </summary>
        public void EndTask()
        {
            if (!profiledTaskStack.TryPop(out var result)) return;
            result.EndTick = stopwatch.ElapsedTicks;
            profiledTasks.Add(result);
        }

        /// <summary>
        /// Get all profiled tasks for this frame
        /// </summary>
        public IEnumerable<ProfiledTask> GetProfiledTasks()
        {
            foreach (var p in profiledTasks)
                yield return p;
        }

        private void CalculateUPS()
        {
            upsCounter.Tick(game.Time.SecondsSinceLoad);
        }

        private void CalculateFPS()
        {
            fpsCounter.Tick(game.Time.SecondsSinceLoad);
        }
    }

    /// <summary>
    /// Structure that holds a task name and relevant time data
    /// </summary>
    public struct ProfiledTask
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name;

        internal long StartTick;
        internal long EndTick;

        /// <summary>
        /// How long the task took
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromTicks(EndTick - StartTick);
    }

    internal class TickRateCounter
    {
        public float Frequency { get; private set; }

        public float MeasureInterval { get; set; } = 1f;

        private int counter;
        private float lastMeasurementTime;

        public void Tick(float currentTime)
        {
            counter++;
            if ((currentTime - lastMeasurementTime) > MeasureInterval)
            {
                lastMeasurementTime = currentTime;
                Frequency = counter / MeasureInterval;
                counter = 0;
            }
        }
    }
}

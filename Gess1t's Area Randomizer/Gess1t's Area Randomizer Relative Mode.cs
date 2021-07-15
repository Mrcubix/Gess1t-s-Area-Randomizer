﻿using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin;
using System;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Area_Randomizer
{
    /*
        PluginName() : Define a plugin name.
    */
    [PluginName("Gess1t's Area Randomizer (Relative Mode Edition)")]
    public class Gess1ts_Area_Randomizer_Relative_Mode : IFilter
    {
        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        Stopwatch timer = new Stopwatch();
        Stopwatch transitionTimer = new Stopwatch();
        Stopwatch updateIntervalTimer = new Stopwatch();
        float lpmm = Info.Driver.OutputMode.Tablet.Digitizer.MaxX / Info.Driver.OutputMode.Tablet.Digitizer.Width;
        Vector2 fullArea = new Vector2(Info.Driver.OutputMode.Tablet.Digitizer.Width, Info.Driver.OutputMode.Tablet.Digitizer.Height);
        Area userDefinedArea;
        Area targetArea;
        Area area;
        Vector2 positionUpdateVector;
        Vector2 sizeUpdateVector;
        Server server;
        bool isRunningAfterSave = true;
        public Vector2 Filter(Vector2 point) 
        {
            if (Info.Driver.OutputMode is RelativeOutputMode relativeOutputMode)
            {   
                if (isRunningAfterSave)
                {
                    isRunningAfterSave = false;
                    userDefinedArea = new Area(new Vector2(Info.Driver.Tablet.Digitizer.Width, Info.Driver.Tablet.Digitizer.Height), new Vector2(Info.Driver.Tablet.Digitizer.Width / 2, Info.Driver.Tablet.Digitizer.Height / 2));
                    _ = Task.Run(GenerateArea);
                    resetEvent.WaitOne();
                }
                Vector2 generatedareaPos = (((point / lpmm) - area.toTopLeft()) / area.size);
                return (userDefinedArea.toTopLeft() + (generatedareaPos * userDefinedArea.size)) * lpmm;
            }
            else
            {
                return new Vector2(point.X,point.Y);
            }
        }
        public void GenerateArea()
        {
            while(true)
            {
                if (Info.Driver.OutputMode is RelativeOutputMode relativeOutputMode)
                {
                    float? aspectRatio = null;
                    /*
                         NOTE: 
                            - Generate a new area when user enable the plugin
                            - Generate a new area when timer >= generationInterval, also stop the timer & start the transition timer
                     */
                    if (!timer.IsRunning & !updateIntervalTimer.IsRunning)
                    {
                        timer.Start();
                        area = new Area(fullArea, false, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
                        //Log.Debug("Area Randomizer", $"New area: {area.toString()}");
                        resetEvent.WaitOne();
                    }
                    if (timer.ElapsedMilliseconds >= generationInterval)
                    {
                        timer.Reset();
                        targetArea = new Area(fullArea, false, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
                        sizeUpdateVector = (targetArea.size - area.size) / (float)(transitionDuration / areaTransitionUpdateInterval);
                        positionUpdateVector = (targetArea.position - area.position) / (float)(transitionDuration / areaTransitionUpdateInterval);
                        //Log.Debug("Area Randomizer", $"New area: {targetArea.toString()}");
                        updateIntervalTimer.Start();
                        transitionTimer.Start();
                    }
                    if (updateIntervalTimer.ElapsedMilliseconds >= areaTransitionUpdateInterval)
                    {
                        updateIntervalTimer.Restart();
                        area.Update(sizeUpdateVector, positionUpdateVector);
                        if (transitionTimer.ElapsedMilliseconds >= transitionDuration)
                        {
                            updateIntervalTimer.Reset();
                            transitionTimer.Reset();
                            timer.Start();
                            //Log.Debug("Area Randomizer", $"Transition complete: {area.toString()}");
                        }
                    }
                }
            }
        }
        public Task<Area> GetAreaAsync()
        {
            return Task.FromResult(area);
        }
        public Task<Area> GetTargetAreaAsync()
        {
            return Task.FromResult(targetArea);
        }
        public Task<Area> GetFullAreaAsync()
        {
            return Task.FromResult(new Area(fullArea, fullArea / 2));
        }
        // Get Pen pos
        public FilterStage FilterStage => FilterStage.PostTranspose;
        /*
            Property() : Define input box property text. OR BooleanProperty() : Define checkbox property text.
            Unit() : Define a Unit.
            DefaultPropertyValue() : Define a default value for the property.
            ToolTip() : Define a description, usually used on a prperty to describe it's function.
        */
        [BooleanProperty("Enable Independant Size Randomization", ""),
         DefaultPropertyValue(false),
         ToolTip("Area Randomizer:\n\n" +
                 "Width and Height will be generated using their own multiplier.\n\n" +
                 "This will override \"Keep Aspect Ration\" when enabled.")
        ]
        public bool enableIndependantRandomization { set; get; }
        [Property("Area Generation Interval"),
         Unit("ms"),
         DefaultPropertyValue(10000),
         ToolTip("Area Randomizer:\n\n" +
                 "Time after which, an new area will be generated.\n\n" +
                 "Higher value mean it will take longer before a new area is generated.")
        ]
        public int generationInterval { set; get; }
        [Property("Transition duration"),
         Unit("ms"),
         DefaultPropertyValue(5000),
         ToolTip("Area Randomizer:\n\n" +
                 "Time taken to transition from an area to another.")
        ]
        public int transitionDuration { set; get; }
        [Property("Area Transition Update Interval"),
         Unit("ms"),
         DefaultPropertyValue(50),
         ToolTip("Area Randomizer:\n\n" +
                 "Amount of time between each update during the transition from an area to another.\n\n" +
                 "A smaller value will result in a smoother transition.\n" +
                 "A value of 0 mean the area will be updated on the next report.\n" +
                 "(not recommended on higher report rate tablets)")
        ]
        public int areaTransitionUpdateInterval { set; get; }
        [Property("Minimum Width"),
         Unit("%"),
         DefaultPropertyValue(0),
         ToolTip("Area Randomizer:\n\n" +
                 "Minimum width the generated area will have, in pourcentages.")
        ]
        public int area_MinX { set; get; }
        [Property("Minimum Heigth"),
        Unit("%"),
         DefaultPropertyValue(0),
         ToolTip("Area Randomizer:\n\n" +
                 "Minimum height the generated area will have, in pourcentages.")
        ]
        public int area_MinY { set; get; }
        [Property("Maximum Width"),
         Unit("%"),
         DefaultPropertyValue(100),
         ToolTip("Area Randomizer:\n\n" +
                 "Maximum width the generated area will have, in pourcentages.")
        ]
        public int area_MaxX { set; get; }
        [Property("Maximum Height"),
         Unit("%"),
         DefaultPropertyValue(100),
         ToolTip("Area Randomizer:\n\n" +
                 "Maximum height the generated area will have, in pourcentages.")
        ]
        public int area_MaxY { set; get; }
    }
}
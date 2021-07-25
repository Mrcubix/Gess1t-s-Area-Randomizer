using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin;
using System;
using EventTimer = System.Timers.Timer;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Area_Randomizer
{
    /*
        PluginName() : Define a plugin name.
    */
    [PluginName("Gess1t's Area Randomizer (Relative Mode Edition)")]
    public class Gess1ts_Area_Randomizer_Relative_Mode : IFilter
    {
        private readonly ManualResetEvent firstAreaGeneration = new ManualResetEvent(false);
        public event EventHandler<Vector2> positionChanged;
        public event EventHandler<Area> AreaChanged;
        public event EventHandler<Area> TargetAreaHasGenerated;
        EventTimer ttimer = new EventTimer();
        EventTimer ttransitionTimer = new EventTimer();
        EventTimer uupdateIntervalTimer = new EventTimer();
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
        Client client;
        Vector2 generatedareaPos;
        private bool runAfterSave = true;
        public bool isFirstRequest = true;
        private bool firstUse = false;
        private string overlayDir;
        private string pluginOverlayDir;
        public Gess1ts_Area_Randomizer_Relative_Mode()
        {
            if (Info.Driver.OutputMode is RelativeOutputMode relativeOutputMode)
            { 
                overlayDir = Path.Combine(Directory.GetCurrentDirectory(), "Overlays");
                if (!Directory.Exists(overlayDir))
                {
                    Directory.CreateDirectory(overlayDir);
                    firstUse = true;
                }
                pluginOverlayDir = Path.Combine(overlayDir, "AreaRandomizer");
                if (!Directory.Exists(pluginOverlayDir))
                {
                    Directory.CreateDirectory(pluginOverlayDir);
                    firstUse = true;
                }

                client = new Client("API");
                Log.Debug("Area Randomizer", "Starting Client");
                _ = Task.Run(client.StartAsync);

                positionChanged += (_, input) =>
                {
                    _ = client.rpc.NotifyAsync("SendDataAsync", "AreaRandomizer", "Position", input / lpmm);
                };
                AreaChanged += (_, area) =>
                {
                    _ = client.rpc.NotifyAsync("SendDataAsync", "AreaRandomizer", "Area", area);
                };
                TargetAreaHasGenerated += (_, targetArea) =>
                {
                    _ = client.rpc.NotifyAsync("SendDataAsync", "AreaRandomizer", "TargetArea", targetArea);
                };
                userDefinedArea = new Area(fullArea, fullArea / 2);
            }
        }
        public Vector2 Filter(Vector2 input) 
        {
            if (Info.Driver.OutputMode is RelativeOutputMode relativeOutputMode)
            {   
                if (firstUse)
                {
                    firstUse = false;
                    new Thread(new ThreadStart(CopyFiles)).Start();
                }
                if (runAfterSave)
                {
                    runAfterSave = false;
                    ttimer.Interval = _generationInterval;
                    ttransitionTimer.Interval = _transitionDuration;
                    uupdateIntervalTimer.Interval = _areaTransitionUpdateInterval;
                    ttimer.Elapsed += (_, _) =>
                    {
                        if (Info.Driver.OutputMode is RelativeOutputMode relativeOutputMode)
                        {   
                            float? aspectRatio = null;
                            targetArea = new Area(fullArea, false, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
                            TargetAreaHasGenerated?.Invoke(this, targetArea);
                            ttimer.Enabled = false;
                            sizeUpdateVector = (targetArea.size - area.size) / (float)(_transitionDuration / _areaTransitionUpdateInterval);
                            positionUpdateVector = (targetArea.position - area.position) / (float)(_transitionDuration / _areaTransitionUpdateInterval);
                            ttransitionTimer.Enabled = true;
                            uupdateIntervalTimer.Enabled = true;
                        }
                    };
                    uupdateIntervalTimer.Elapsed += (_, _) =>
                    {
                        area.Update(sizeUpdateVector, positionUpdateVector);
                        AreaChanged?.Invoke(this, area);
                    };
                    ttransitionTimer.Elapsed += (_, _) =>
                    {
                        uupdateIntervalTimer.Enabled = false;
                        ttransitionTimer.Enabled = false;
                        ttimer.Enabled = true;
                    };
                    ttimer.Start();
                    GenerateFirstArea();
                    firstAreaGeneration.WaitOne();
                }
                generatedareaPos = (((input / lpmm) - area.toTopLeft()) / area.size);
                positionChanged?.Invoke(this, input);
                return (userDefinedArea.toTopLeft() + (generatedareaPos * userDefinedArea.size)) * lpmm;
            }
            else
            {
                return new Vector2(input.X, input.Y);
            }
        }
        public void GenerateFirstArea()
        {
            if (Info.Driver.OutputMode is RelativeOutputMode relativeOutputMode)
            {
                float? aspectRatio = null;
                /*
                    NOTE: 
                        - Generate a new area when user enable the plugin
                        - Generate a new area when timer >= generationInterval, also stop the timer & start the transition timer
                */
                timer.Start();
                area = new Area(fullArea, false, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
                AreaChanged?.Invoke(this, area);
                firstAreaGeneration.Set();
            }
        }
        // http://msdn.microsoft.com/en-us/library/cc148994.aspx
        public void CopyFiles()
        {
            if (_pluginsPath != null)
            {
                DirectoryInfo source = new DirectoryInfo(Path.Combine(_pluginsPath, "AreaRandomizer"));
                if (source.Exists)
                {
                    foreach (FileInfo file in source.GetFiles())
                    {
                        file.CopyTo(Path.Combine(pluginOverlayDir, file.Name), true);
                    }
                    foreach(DirectoryInfo directory in source.GetDirectories())
                    {
                        string targetFolder = Path.Combine(pluginOverlayDir, directory.Name);
                        Directory.CreateDirectory(targetFolder);
                        DirectoryInfo directoryTarget = new DirectoryInfo(targetFolder);
                        foreach (FileInfo file in directory.GetFiles())
                        {
                            file.CopyTo(Path.Combine(targetFolder, file.Name), true);
                        }
                    }
                }
                else
                {
                    Log.Write("Area Randomizer", "Overlay Unavailable: Plugin folder path is missing, make sure to set it up properly and save to try again.", LogLevel.Info);
                }
            }
            else
            {
                Log.Write("Area Randomizer", "Overlay Unavailable: Plugin folder path is missing, make sure to set it up properly and save to try again.", LogLevel.Info);
            }
        }
        // Get Pen pos
        public FilterStage FilterStage => FilterStage.PostTranspose;
        /*
            Property() : Define input box property text. OR BooleanProperty() : Define checkbox property text.
            Unit() : Define a Unit.
            DefaultPropertyValue() : Define a default value for the property.
            ToolTip() : Define a description, usually used on a prperty to describe it's function.
        */
        [Property("Plugin folder path"),
         ToolTip("Proxy API:\n\n" +
                 "Folder where this plugin is located in.\n\n" +
                 "E.g: 'C:\\Users\\{user}\\AppData\\Local\\OpenTabletDriver\\Plugins\\Area Visualizer' on windows.")
        ]
        public string pluginsPath 
        {
            get => @_pluginsPath; 
            set
            {
                _pluginsPath = @value;
            }
        }
        public string _pluginsPath;
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
        public int generationInterval 
        { 
            set
            {
                _generationInterval = Math.Max(value, 15);
            } 
            get => _generationInterval;
        }
        private int _generationInterval;
        [Property("Transition duration"),
         Unit("ms"),
         DefaultPropertyValue(5000),
         ToolTip("Area Randomizer:\n\n" +
                 "Time taken to transition from an area to another.")
        ]
        public int transitionDuration 
        { 
            set
            {
                _transitionDuration = Math.Max(value, 15);
            }
            get => _transitionDuration;
        }
        private int _transitionDuration;
        [Property("Area Transition Update Interval"),
         Unit("ms"),
         DefaultPropertyValue(50),
         ToolTip("Area Randomizer:\n\n" +
                 "Amount of time between each update during the transition from an area to another.\n\n" +
                 "A smaller value will result in a smoother transition.\n" +
                 "A value of 0 mean the area will be updated on the next report.\n" +
                 "(not recommended on higher report rate tablets)")
        ]
        public int areaTransitionUpdateInterval 
        { 
            set
            {
                _areaTransitionUpdateInterval = Math.Max(value, 15);
            }
            get => _areaTransitionUpdateInterval; 
        }
        private int _areaTransitionUpdateInterval;
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
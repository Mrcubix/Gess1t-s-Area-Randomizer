using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin;
using System;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;

namespace Area_Randomizer
{
    /*
        PluginName() : Define a plugin name.
    */
    [PluginName("Gess1t's Area Randomizer (Absolute Mode Edition)")]
    public class Gess1ts_Area_Randomizer_Absolute_Mode : IFilter
    {
        private readonly ManualResetEvent PositionEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent TargetGenerationEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent AreaUpdateEvent = new ManualResetEvent(false);
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
        Vector2 generatedareaPos;
        bool isRunningAfterSave = true;
        bool serverIsRunning;
        public bool isFirstRequest = true;
        private bool firstUse = false;
        private string overlayDir;
        private string pluginOverlayDir;
        public Gess1ts_Area_Randomizer_Absolute_Mode()
        {
            if (Info.Driver.OutputMode is AbsoluteOutputMode absoluteOutputMode)
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
                if (!serverIsRunning) 
                {
                    serverIsRunning = true;
                    server = new Server("AreaRandomizer", this);
                    Log.Debug("Area Randomizer", "Starting server");
                    _ = Task.Run(server.StartAsync);
                }
            }
        }
        public Vector2 Filter(Vector2 point) 
        {
            if (Info.Driver.OutputMode is AbsoluteOutputMode absoluteOutputMode)
            {
                if (firstUse)
                {
                    firstUse = false;
                    new Thread(new ThreadStart(CopyFiles)).Start();
                }
                if (isRunningAfterSave)
                {
                    isRunningAfterSave = false;
                    userDefinedArea = new Area(new Vector2(absoluteOutputMode.Input.Width, absoluteOutputMode.Input.Height), absoluteOutputMode.Input.Position);
                    _ = Task.Run(GenerateArea);
                    AreaUpdateEvent.WaitOne();
                }
                generatedareaPos = (((point / lpmm) - area.toTopLeft()) / area.size);
                PositionEvent.Set();
                PositionEvent.Reset();
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
                if (Info.Driver.OutputMode is AbsoluteOutputMode absoluteOutputMode)
                {
                    float aspectRatio = (float)Math.Round((absoluteOutputMode.Output.Width / absoluteOutputMode.Output.Height), 4);
                    /*
                        NOTE: 
                            - Generate a new area when user enable the plugin
                            - Generate a new area when timer >= generationInterval, also stop the timer & start the transition timer
                    */ 
                    if (!timer.IsRunning & !updateIntervalTimer.IsRunning)
                    {
                        timer.Start();
                        area = new Area(fullArea, EnableAspectRatio, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
                        //Log.Debug("Area Randomizer", $"New area: {area.toString()}");
                        AreaUpdateEvent.Set();
                        AreaUpdateEvent.Reset();
                        // Area first generated here
                        // Area first updated here
                    }
                    if (timer.ElapsedMilliseconds >= generationInterval)
                    {
                        timer.Reset();
                        targetArea = new Area(fullArea, EnableAspectRatio, enableIndependantRandomization, area_MinX, area_MaxX, area_MinY, area_MaxY, aspectRatio);
                        TargetGenerationEvent.Set();
                        TargetGenerationEvent.Reset();
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
                        // update event here
                        AreaUpdateEvent.Set();
                        AreaUpdateEvent.Reset();
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
        public Task<string> GetMethodsAsync()
        {
            string[] methods = new string[4] {"GetFullAreaAsync", "GetPositionAsync", "GetAreaAsync", "GetTargetAreaAsync"};
            return Task.FromResult(JsonSerializer.Serialize(methods));
        }
        public async Task<Vector2> GetPositionAsync()
        {
            await Task.Run(() => PositionEvent.WaitOne());
            Vector2 Position = area.toTopLeft() + generatedareaPos * area.size;
            return Position;
        }
        public async Task<Area> GetAreaAsync()
        {
            await Task.Run(() => AreaUpdateEvent.WaitOne());
            return area;
        }
        public async Task<Area> GetTargetAreaAsync()
        {
            await Task.Run(() => TargetGenerationEvent.WaitOne());
            return targetArea;
        }
        public async Task<Area> GetFullAreaAsync()
        {
            if (isFirstRequest)
            {
                isFirstRequest = false;
            }
            else
            {
                await Task.Delay(10000);
            }
            return new Area(fullArea, fullArea / 2);
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
        public FilterStage FilterStage => FilterStage.PreTranspose;
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
        [BooleanProperty("Keep Aspect ratio", ""),
         DefaultPropertyValue(true),
         ToolTip("Area Randomizer:\n\n" +
                 "Generated area will have the display aspect ratio.\n\n" +
                 "This will not work when \"Enable Independant Size Randomization\" is enabled.")
        ]
        public bool EnableAspectRatio { set; get; }
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
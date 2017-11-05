using System;

namespace Samarium {

    using PluginFramework;
    using PluginFramework.Command;
    using PluginFramework.Config;
    using PluginFramework.Logger;
    using PluginFramework.Plugin;
    using PluginFramework.UI;
    using static PluginFramework.UI.ConsoleUI;

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.Reflection;

    public static partial class Samarium {

        #region Const and effectively const fields
        const string ApplicationName = nameof(Samarium);
        static readonly Version Version = typeof(Samarium).Assembly.GetName().Version;
        static string Prompt { get; } = $"{ nameof(Samarium) } >";

        static readonly List<(string Argument, string Description, Action Handler)?> Arguments = new List<(string Argument, string Description, Action Handler)?> {
            ("--help", "Prints this help menu.", PrintHelp),
            ("--no-plugins", "Stops automatic plugin loading. Useful for debugging.", SetNoPlugins),
            ("--exec=\"<cmd>\"", "Executes a command immediately after successful booting.", SetAutoExec),
            ("--init", "Re-initialization \"wizard\"", () => InitWizard()),
            ("--logdir=\"<dir>\"", "Set the logging directory for this instance.", SetLogDir),
            ("--cnfdir=\"<dir>\"", "Set the config directory for this instance.", SetConfigDir),
            ("--plugindir=\"<dir>\"", "Set the plugins directory for this instance.", SetPluginDir)
        };

        static readonly List<ICommand> SystemCommands = new List<ICommand> {
            new Command {
                Arguments = new[] { "--quiet", "--q" },
                CommandTag = "help",
                Description = 
                    "Prints a list of all commands and the plugins they belong to,\n" +
                    "along with a short description of their jobs.\n\n" +
                    "Usage:\n" +
                    "\thelp [arguments] [switch=setting]\n\n" +
                    "Description:\n" +
                    "\tArguments:\n" +
                    "\t\t--quiet\n" +
                    "\t\t--q\t\t\tNo console output\n" +
                    "\tSwitches:\n" +
                    "\t\t-plugin=\t\tOutput only commands for this plugin. Plugins may be chained.",
                ShortDescription = "Prints a list of all commands and their parent plugins.",
                Handler = Command_Help,
                Switches = new Dictionary<string, string[]> { { "-plugin=", null } }
            }
        };
        #endregion

        #region Runtime Options
        /// <summary>
        /// Gets or sets a value indicating whether to automatically load 
        /// plugins on boot.
        /// </summary>
        public static bool AutoLoadPlugins { get; set; }

        /// <summary>
        /// Gets or sets a list of commands to execute after successful boot.
        /// </summary>
        public static List<string[]> StartupCommands { get; set; }

        public static string LogDirectory { get; set; } = "./logs/";

        public static string ConfigDirectory { get; set; } = "./.conf/";

        public static string PluginsDirectory { get; set; } = "./plugins/";

        public static bool KeepAlive { get; set; } = true; // Only set to false when application should terminate
        #endregion

        public static IConfig SystemConfig { get; private set; }

        public static Logger SystemLogger { get; private set; }

        public static PluginRegistry Registry { get; private set; }

        public static int Main(string[] args) {

            // Start by parsing arguments
            foreach (var arg1 in args) {
                var splitArg = arg1.Split('=')[0];
                var argTuple = Arguments.FirstOrDefault(x => (bool)x?.Argument.StartsWith(splitArg));
                if (argTuple is null)
                    continue;
                argTuple?.Handler();
            }

            // Application still alive; boot.
            InitDirectories();

            // Instantiate logger
            SystemLogger = Logger.CreateInstance(nameof(Samarium), LogDirectory);

            // Begin logging from here
            Info("Samarium is booting... please be patient...");
            Ok("Samarium arguments...\t\thandled!");
            Ok("Samarium configs...\t\tloaded!");
            Ok("Samarium logger...\t\t\tinitialised!");

            // Initialise plugin registry
            Registry = PluginRegistry.CreateInstance(SystemConfig);
            Ok("Samarium plugin registry...\tinitialised!");

            Info("Loading Samarium plugins... please be patient...");
            LoadPlugins();

            try {

                var inputCommand = default(string);

                do {

                    Console.WriteLine();
                    PrintPrompt();
                    inputCommand = Console.ReadLine().Trim();


                } while (KeepAlive);
            } catch {
                // TERMINATE
            }

            Console.ReadKey();
            return 0;
        }

        #region Init code
        static void InitDirectories() {
            // Start things off by initialising the config directory.
            InitConfigDir();

            // Initialise configurable directories
            SystemConfig.SetConfig("log_directory", LogDirectory);
            SystemConfig.SetConfig("plugin_directory", PluginsDirectory);

            // Get all configs ending with "directory" and attempt to create them
            // Directories will only be created if they do not already exist
            foreach (var cfg in SystemConfig.Where<string>(x => x.ToLowerInvariant().EndsWith("directory")))
                Directory.CreateDirectory(cfg);
        }

        static void InitConfigDir() {
            var cfgFilePath = Path.Combine(ConfigDirectory, "samarium.def.yml");
            if (!Directory.Exists(ConfigDirectory)) {
                Directory.CreateDirectory(ConfigDirectory);
                using (var cfgFile = File.Create(cfgFilePath))
                using (var resourceStream = typeof(Samarium).Assembly.GetManifestResourceStream("Samarium.Resources.ConfigDefaults.samarium.yml")) { 
                    resourceStream.CopyTo(cfgFile);
                }
            }

            SystemConfig = new DynamicConfig(ConfigDirectory, "samarium.yml", new FileInfo(cfgFilePath));

        }
        
        static void LoadPlugins() {
            var ignoreList = new List<string>();
            var includeList = new List<string>();

            // Plugins to ignore
            if (!File.Exists($"{ PluginsDirectory }/plugins.ignore"))
                File.Create($"{ PluginsDirectory }/plugins.ignore").Close();
            else
                using (var fReader = new StreamReader(File.OpenRead($"{ PluginsDirectory }/plugins.ignore")))
                    while (fReader.Peek() != -1) {
                        var line = fReader.ReadLine();
                        if (line.StartsWith("#", StringComparison.InvariantCulture))
                            continue;
                        ignoreList.Add(line);
                    }

            // Plugins to include from elsewhere
            if (!File.Exists($"{ PluginsDirectory }/plugins.include"))
                File.Create($"{ PluginsDirectory }/plugins.include").Close();
            else {
                using (var fReader = new StreamReader(File.OpenRead($"{ PluginsDirectory }/plugins.include"))) {
                    while (fReader.Peek() != -1) {
                        var line = fReader.ReadLine();
                        if (line.StartsWith("#", StringComparison.InvariantCulture))
                            continue;
                        includeList.Add(line);
                    }
                }
            }

            foreach (var dll in Directory.GetFiles(PluginsDirectory, "*.dll")) {
                if (ignoreList.Contains(dll)) continue;

                var assembly = Assembly.LoadFrom(dll);
                foreach (Type t in assembly.GetTypes()) {
                    if (t.IsSubclassOf(typeof(Plugin))) {
                        Registry.RegisterPlugin(assembly, t);
                    }
                }
            }

            if (includeList.Where(x => !string.IsNullOrEmpty(x.Trim())).Count() == 0) {
                Warn("No plugins selected for loading! I'm essentially a virtual paperweight now.");
                return;
            }

            foreach (var dll in includeList.Where(x => !string.IsNullOrEmpty(x.Trim()))) {

                try {
                    Info($"Loading plugin { dll }...");

                    if (!File.Exists(dll)) {
                        Error($@"The file { dll } could not be found on the local hard drive!");
                        Error("Please make sure that the file exists and can be found by the application!");
                        dll.Replace('\\', '/');
                    }

                    var assembly = Assembly.LoadFrom(dll);
                    foreach (Type t in assembly.GetTypes()) {
                        if (t.IsSubclassOf(typeof(Plugin))) {
                            Registry.RegisterPlugin(assembly, t);
                        }
                    }
                    Ok($"Plugin { dll } LOADED!");
                } catch (Exception ex) {
                    Error($"An error occurred while loading plugin { Path.GetFileName(dll) }!");
                    Error($"Error message: { ex.Message }");
                    Trace($"Error stacktrace: { ex.StackTrace }");
                    continue;
                }
            }
        }
        #endregion

        #region Logging shortcuts
        static void Fatal(string fmt, params object[] args) => SystemLogger.Fatal(fmt, args);

        static void Error(string fmt, params object[] args) => SystemLogger.Error(fmt, args);

        static void Info(string fmt, params object[] args) => SystemLogger.Info(fmt, args);

        static void Ok(string fmt, params object[] args) => SystemLogger.Ok(fmt, args);

        static void Warn(string fmt, params object[] args) => SystemLogger.Warn(fmt, args);

        static void Trace(string fmt, params object[] args) => SystemLogger.Trace(fmt, args);

        static void Debug(string fmt, params object[] args) => SystemLogger.Debug(fmt, args);
        #endregion

        #region Prototypes

        static partial void SetNoPlugins();

        static partial void SetLogDir();

        static partial void SetAutoExec();

        static partial void SetConfigDir();

        static partial void SetPluginDir();
        #endregion

        #region For a later date
        public static void PrintHelp() {
            var boxWidth = 35;
            var controlBoxHeight = 11;

            PrintFullScreenBorder(title: ApplicationName, version: $"v{ Version.Major }.{ Version.MajorRevision }.{ Version.Minor }.{ Version.MinorRevision }");
            PrintBox(0, 0, (uint)boxWidth, (uint)ConsoleHeight, BorderType.DoubleBorder, topRightCorner: CharType.TopT, bottomRightCorner: CharType.BottomT, title: " Commands ");
            PrintBox((uint)boxWidth, (uint)ConsoleHeight - ((uint)controlBoxHeight), (uint)ConsoleWidth - (uint)boxWidth - 1, (uint)controlBoxHeight,
                     BorderType.DoubleBorder, CharType.LeftT, CharType.RightT, CharType.BottomT, CharType.BottomRightCorner, " Legend ");

            Print(boxWidth + 5, ConsoleHeight - (controlBoxHeight - 2), "\u241B\t==>\tEscape (Exit) application");
            Print(boxWidth + 5, ConsoleHeight - (controlBoxHeight - 4), "↑\t==>\tMove selection upwards");
            Print(boxWidth + 5, ConsoleHeight - (controlBoxHeight - 6), "↓\t==>\tMove selection downwards");
            Print(boxWidth + 5, ConsoleHeight - (controlBoxHeight - 8), "⏎\t==>\tConfirm selection");
            // ␛↑↓⏎

            var wipText = "WIP - This is a Work in Progress!";

            PrintCentre(wipText, ConsoleWidth + boxWidth - 5, ConsoleHeight / 2 - wipText.Split('\n').Length, clearArea: false);

            Console.ReadKey();
            Environment.Exit(0x0);
        }

        public static int InitWizard() {

            var warningSign =
                (".i;;;;i.\n" +
                "iYcviii;vXY:\n" +
                ".YXi       .i1c.\n" +
                ".YC.     .    in7.\n" +
                ".vc.   ......   ;1c.\n" +
                "i7,   ..        .;1;\n" +
                "i7,   .. ...      .Y1i\n" +
                ",7v     .6MMM@;     .YX,\n" +
                ".7;.   ..IMMMMMM1     :t7.\n" +
                ".;Y.     ;$MMMMMM9.     :tc.\n" +
                "vY.   .. .nMMM@MMU.      ;1v.\n" +
                "i7i   ...  .#MM@M@C. .....:71i\n" +
                "it:   ....   $MMM@9;.,i;;;i,;tti\n" +
                ":t7.  .....   0MMMWv.,iii:::,,;St.\n" +
                ".nC.   .....   IMMMQ..,::::::,.,czX.\n" +
                ".ct:   ....... .ZMMMI..,:::::::,,:76Y.\n" +
                "c2:   ......,i..Y$M@t..:::::::,,..inZY\n" +
                "vov   ......:ii..c$MBc..,,,,,,,,,,..iI9i\n" +
                "i9Y   ......iii:..7@MA,..,,,,,,,,,....;AA:\n" +
                "iIS.  ......:ii::..;@MI....,............;Ez.\n" +
                ".I9.  ......:i::::...8M1..................C0z.\n" +
                ".z9;  ......:i::::,.. .i:...................zWX.\n" +
                "vbv  ......,i::::,,.      ................. :AQY\n" +
                "c6Y.  .,...,::::,,..:t0@@QY. ................ :8bi\n" +
                ":6S. ..,,...,:::,,,..EMMMMMMI. ............... .;bZ,\n" +
                ":6o,  .,,,,..:::,,,..i#MMMMMM#v.................  YW2.\n" +
                ".n8i ..,,,,,,,::,,,,.. tMMMMM@C:.................. .1Wn\n" +
                "7Uc. .:::,,,,,::,,,,..   i1t;,..................... .UEi\n" +
                "7C...::::::::::::,,,,..        ....................  vSi.\n" +
                ";1;...,,::::::,.........       ..................    Yz:\n" +
                "v97,.........                                     .voC.\n" +
                "izAotX7777777777777777777777777777777777777777Y7n92:\n" +
                ".;CoIIIIIUAA666666699999ZZZZZZZZZZZZZZZZZZZZ6ov.").Split('\n');

            Console.WindowHeight = warningSign.Length + 5;
            PrintFullScreenBorder(title: string.Concat(" ", ApplicationName, " - Init Wizard "), version: $"v{ Version.Major }.{ Version.MajorRevision }.{ Version.Minor }.{ Version.MinorRevision }");

            var startLine = ConsoleHeight / 2 - warningSign.Length / 2;
            for (var i = 0; i < warningSign.Length; i++) {
                PrintCentre(warningSign[i], startLine + i);
            }

            return 0;
        }
        #endregion

    }
}

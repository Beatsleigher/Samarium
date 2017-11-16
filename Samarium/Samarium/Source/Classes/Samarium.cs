using System;

namespace Samarium {

    using PluginFramework;
    using PluginFramework.Command;
    using PluginFramework.Config;
    using PluginFramework.Logger;
    using PluginFramework.Plugin;
    using PluginFramework.Rest;
    using PluginFramework.UI;
    using static PluginFramework.UI.ConsoleUI;

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Net;
    using System.Threading;
    using Newtonsoft.Json;

    public static partial class Samarium {

        #region Const and effectively const fields
        const string ApplicationName = nameof(Samarium);
        static Version Version { get; } = typeof(Samarium).Assembly.GetName().Version;
        static string Prompt { get; } = $"{ nameof(Samarium) } >";
        static Regex commandChainRegex { get; } = new Regex("([&]{2})", RegexOptions.Compiled);
        static string ApplicationCopyright { get; } = $"{ ApplicationName } © Simon Cahill & contributors";

        static readonly List<(string Argument, string Description, Action Handler)?> Arguments = new List<(string Argument, string Description, Action Handler)?> {
            ("--help", "Prints this help menu.", PrintHelp),
            ("--no-plugins", "Stops automatic plugin loading. Useful for debugging.", SetNoPlugins),
            ("--exec=\"<cmd>\"", "Executes a command immediately after successful booting.", SetAutoExec),
            ("--init", "Re-initialization \"wizard\"", () => InitWizard()),
            ("--logdir=\"<dir>\"", "Set the logging directory for this instance.", SetLogDir),
            ("--cnfdir=\"<dir>\"", "Set the config directory for this instance.", SetConfigDir),
            ("--plugindir=\"<dir>\"", "Set the plugins directory for this instance.", SetPluginDir)
        };

        static List<ICommand> SystemCommands { get; } = new List<ICommand> {
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
            },
            new Command {
                Arguments = null,
                CommandTag = "load",
                Description =
                    "Loads one or more selected plugins (*.dll/*.exe) in to Samarium.\n" +
                    "Plugins can be loaded during runtime, as to ensure a consistent user\n" +
                    "experience.\n\n" +
                    "Usage:\n" +
                    "\tload </path/to/plugin> [</path/to/another/plugin>...]\n",
                ShortDescription = "Loads a new plugin in to Samarium.",
                Handler = Command_LoadPlugin
            },
            new Command {
                Arguments = null,
                CommandTag = "unload",
                Description =
                    "Unloads one or more plugins from Samarium.\n" +
                    "Plugins can be unloaded during runtime, although it is not recommended!\n" +
                    "Unloading plugins during runtime can cause application instability!\n\n" +
                    "Usage:\n" +
                    "\tunload <plugin_name> [<plugin_name> ...]",
                ShortDescription = "Unloads a plugin from Samarium.",
                Handler = Command_UnloadPlugin
            },
            new Command {
                CommandTag = "exit",
                Description =
                    "Disrupts the looper and causes the application to\n" +
                    "terminate in a controlled fashion.",
                Handler = (plugin, command, args) => { KeepAlive = false; return null; },
                ShortDescription = "Terminates Samarium in a controller manor."
            },
            new Command {
                Arguments = new[] { "--load", "--save", "--load-defaults", "--get", "--set", "--list" },
                CommandTag = "config",
                Description =
                    "Allows for basic configuration management.\n" +
                    "Using this command, configurations can be retrieved and set\n" +
                    "while the application is running.\n" +
                    "Generally new configurations are available and usable system-wide.\n" +
                    "This may vary depending on the loaded plugins and their respective\n" +
                    "configurations.\n\n" +
                    "Usage:\n" +
                    "\tconfig <argument>\n" +
                    "\tconfig <\"--set key=newvalue\">\n" +
                    "\tconfig <[--list] [\"--list filter\"]>\n\n" +
                    "Description:\n" +
                    "\tArguments:\n" +
                    "\t\t--load\t\t\tReload the configurations from the config file.\n" +
                    "\t\t--save\t\t\tSave the current configuration to the config file.\n" +
                    "\t\t--load-defaults\t\tLoad the default configurations.\n" +
                    "\t\t--get\t\t\tGet a specific configuration",
                Handler = Command_Config,
                ShortDescription = "Basic config management."
            },
            new Command {
                CommandTag = "clear",
                Description = "Clears the console.",
                Handler = (plugin, cmd, args) => { Console.Clear(); PrintTopTitle(ApplicationCopyright, Version.ToString()); return default; },
                ShortDescription = "Clears the console."
            },
            new Command {
                Arguments = new[] { "--all-plugins", "--loaded-plugins", "--ignored-plugins" },
                CommandTag = "list",
                Description = 
                    "Lists the desired information in the console.\n\n" +
                    "Usage:\n" +
                    "\tlist <argument>\n\n" +
                    "Description:\n" +
                    "\tArguments:\n" +
                    "\t\t--all-plugins\t\t\tList all plugins known to Samarium.\n" +
                    "\t\t--loaded-plugins\t\tList all plugins loaded in to memory.\n" +
                    "\t\t--ignored-plugins\t\tList all plugins ignored by Samarium on startup.\n",
                Handler = Command_List,
                ShortDescription = "Lists the desired information"                
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

        public static RestService RestService { get; private set; }

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
            InitConsole();

            // Instantiate logger
            SystemLogger = Logger.CreateInstance(nameof(Samarium), LogDirectory).SetConfig(SystemConfig);

            // Begin logging from here
            Info("Samarium is booting... please be patient...");
            Ok("Samarium arguments...\t\thandled!");
            Ok("Samarium configs...\t\tloaded!");
            Ok("Samarium logger...\t\t\tinitialised!");

            // Initialise plugin registry
            Registry = PluginRegistry.CreateInstance(SystemConfig);
            Registry.CommandExecutionRequested += Registry_CommandExecutionRequested;
            Registry.AsyncCommandExecutionRequested += Registry_AsyncCommandExecutionRequested;
            Ok("Samarium plugin registry...\tinitialised!");

            // Initialise REST service
            Info("Initialising RESTful services... please be patient...");
            //RestService = RestService.CreateInstance(IPAddress.Parse(SystemConfig.GetString("rest_base_ip")));

            Info("Loading Samarium plugins... please be patient...");
            LoadPlugins();

            Info("Processing hooks...");
            Console.CancelKeyPress += (s, evt) => { evt.Cancel = true; };
            Console.CancelKeyPress += Console_CancelKeyPress;

            Ok("Samarium has successfully booted!");
            Thread.Sleep(500);
            ExecuteCommand("clear");
            try {

                var inputCommand = default(string);

                do {

                    try {
                        Console.WriteLine();
                        PrintPrompt();
                        inputCommand = Console.ReadLine()?.Trim();
                    } catch { continue; }

                    if (string.IsNullOrEmpty(inputCommand))
                        continue;

                    //////////////////////////////////////////////////
                    //                  Cheat Sheet                 //
                    //////////////////////////////////////////////////
                    //                                              //
                    //      &&      =>      Command chaining        //
                    //      ??      =>      Asynchronous execution  //
                    //      !!      =>      no idea                 //
                    //      ""      =>      multiple args in one    //
                    //                                              //
                    //////////////////////////////////////////////////

                    var asyncCalls = new List<Task<ICommandResult>>();
                    // First check for chained commands
                    try {
                        foreach (var command in commandChainRegex.Split(inputCommand).Where(x => x != "&&")) {
                            var tmp = default(string[]);
                            // Weed out asynchronous calls
                            if (command.StartsWith("??")) {
                                tmp = command.TrimStart('?').SplitCommandLine().ToArray();
                                asyncCalls.Add(ExecuteCommandAsync(tmp[0], tmp.Where(x => x != tmp[0]).ToArray()));
                                continue;
                            }

                            tmp = command.SplitCommandLine().ToArray();
                            ExecuteCommand(tmp[0], tmp.Where(x => x != tmp[0]).ToArray());

                        }
                        Task.WaitAll(asyncCalls.ToArray());
                    } catch (Exception ex) {
                        Error("An error occurred while executing the command {0}!", inputCommand);
                        Error("Error description: {0}", ex.Message);
                        Trace("Error source: {0}", ex.Source);
                        Trace("Error stacktrace: {0}", ex.StackTrace);
                        Error("If this error persists, please contact your vendor or submit an issue on GitHub!");
                    }

                } while (KeepAlive);
            } catch (Exception ex) {
                // TERMINATE
                Fatal("A fatal error occurred within Samarium!");
                Fatal("Error message: {0}", ex.Message);
                Trace("Error source: {0}", ex.Source);
                Trace("Error stacktrace: {0}", ex.StackTrace);
                Fatal("This error has forced Samarium to abort! I will attempt to cleanly shut down all plugins!");
            } finally {
                // UNLOAD PLUGINS
                ExecuteCommand("unload", "*");
            }

            return 0;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = false;
            Warn("Attempted to terminate application with CTRL+C!");
            Warn("Application cannot be terminated this way; to cleanly exit the application, type \"exit\"");
        }

        private static async Task<ICommandResult> Registry_AsyncCommandExecutionRequested(IPlugin sender, string requestedCommand, params string[] execArgs) {
            return await ExecuteCommandAsync(requestedCommand, execArgs);
        }

        private static ICommandResult Registry_CommandExecutionRequested(IPlugin sender, string requestedCommand, params string[] execArgs) {
            // Maybe at some point log which plugin requested execution?
            // Don't know if that makes sense; but better think ahead.
            // In any case, it's good for debugging.
            // Yes, I could've made a one-liner, but then this comment wouldn't be a thing!
            return ExecuteCommand(requestedCommand, execArgs);
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
#if USE_YAMLDOTNET
            var cfgFilePath = Path.Combine(ConfigDirectory, "samarium.def.yml");
            if (!Directory.Exists(ConfigDirectory)) {
                Directory.CreateDirectory(ConfigDirectory);
                using (var cfgFile = File.Create(cfgFilePath))
                using (var resourceStream = typeof(Samarium).Assembly.GetManifestResourceStream("Samarium.Resources.ConfigDefaults.samarium.yml")) { 
                    resourceStream.CopyTo(cfgFile);
                }
            }

            SystemConfig = new DynamicConfig(ConfigDirectory, "samarium.yml", new FileInfo(cfgFilePath));
#else
            var cfgFilePath = Path.Combine(ConfigDirectory, "samarium.def.json");
            if (!Directory.Exists(ConfigDirectory) || !File.Exists(cfgFilePath)) {
                Directory.CreateDirectory(ConfigDirectory);
                using (var cfgFile = File.Create(cfgFilePath))
                using (var resourceStream = typeof(Samarium).Assembly.GetManifestResourceStream("Samarium.Resources.ConfigDefaults.samarium.json")) {
                    resourceStream.CopyTo(cfgFile);
                }
            }

            SystemConfig = new DynamicConfig(ConfigDirectory, "samarium.json", new FileInfo(cfgFilePath));
#endif

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
                        includeList.Add(line.Replace("$(plugin_dir)", PluginsDirectory));
                    }
                }
            }

            var initialResults = ExecuteCommand("load", Directory.GetFiles(PluginsDirectory, "*.dll")
                .Where(x => !ignoreList.Contains(x)).ToArray());

            if (includeList.Where(x => !string.IsNullOrEmpty(x.Trim())).Count() == 0 && ((dynamic)initialResults).Result.Count == 0) {
                Warn("No plugins selected for loading! I'm essentially a virtual paperweight now.");
                return;
            }

            var includeListResults = ExecuteCommand("load", includeList.Where(x => !string.IsNullOrEmpty(x.Trim()))
                                                                                  .Where(File.Exists).ToArray());
        }
        
        static void InitConsole() {
            var spaces = new[] { " ", " ", " ", " ", " ", " ", " ", " ", " " };
            Console.Title = string.Format("{0} {1} Version {2}", ApplicationCopyright, string.Join("", spaces), Version);
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

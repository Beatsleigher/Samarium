using System;

namespace Samarium {

    using PluginFramework;
    using PluginFramework.Command;
    using PluginFramework.Plugin;

    using static System.Console;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.Reflection;
    using YamlDotNet.Serialization;

    partial class Samarium {

        static ICommandResult ExecuteCommand(string commandTag, params string[] args) {
            // Check for Samarium commands first.
            // If default, check in plugins.
            var command = SystemCommands.FirstOrDefault(x => x.CommandTag == commandTag) ??
                          Registry.PluginInstances.FirstOrDefault(plugin => plugin.HasCommand(commandTag))?.GetCommand(commandTag);

            if (command is default(ICommand)) {
                Error("Attempted to execute command {0} with params {1}; command was not found!", commandTag, string.Join(", ", args));
                return default;
            }

            return command.Execute(args);
        }

        static async Task<ICommandResult> ExecuteCommandAsync(string commandTag, params string[] args) {
            // Check for Samarium commands first.
            // If default, check in plugins.
            var command = SystemCommands.FirstOrDefault(x => x.CommandTag == commandTag) ??
                          Registry.PluginInstances.FirstOrDefault(plugin => plugin.HasCommand(commandTag))?.GetCommand(commandTag);

            if (command is default(ICommand)) {
                Error("Attempted to execute command {0} with params {1}; command was not found!", commandTag, string.Join(", ", args));
                return default;
            }

            return await command.ExecuteAsync(args);
        }

        static ICommandResult Command_Help(IPlugin sender, ICommand _, params string[] args) {
            var fColour = ForegroundColor;
            var loadedCommands = new List<ICommand>();
            var printToConsole = (!args.Contains("--q") && !args.Contains("--quiet"));

            args = args.Where(x => x.ToLowerInvariant() != "--q" && x.ToLowerInvariant() != "--quiet").ToArray();

            // Arguments should now be empty.
            // If not, loop through them and show the help menu for specific command
            if (args.Length > 0) {
                var knownCommands = SystemCommands.Concat(Registry.GetAllCommands());

                foreach (var tag in args) {
                    WriteLine(
                        "\n\t{0}",
                        knownCommands.FirstOrDefault(x => x.CommandTag == tag)?.Description?.Replace("\n", "\n\t") ??
                        string.Format("The command {0} doesn't exist. Type \"help\" for all commands.", tag)
                    );
                }
                return null; // BEWARE
            }

            if (printToConsole) {
                ForegroundColor = ConsoleColor.DarkYellow;
                WriteLine("\t{0,-25}\t{1,-65} {2,-14}", "[ Command Tag ]", "[ Short Description ]", " [ Plugin ]");
                ForegroundColor = fColour;
            }

            foreach (var command in SystemCommands.Where(x => x != null && !string.IsNullOrEmpty(x.CommandTag))) {
                if (printToConsole) {
                    Write("\t{0,-30}\t{1,-65}", command.CommandTag, command.ShortDescription);
                    ForegroundColor = ConsoleColor.DarkYellow;
                    Write("{0,-3}", " [");
                    ForegroundColor = ConsoleColor.Cyan;
                    Write($"{{0,-{ nameof(Samarium).Length + 1 }}}", nameof(Samarium));
                    ForegroundColor = ConsoleColor.DarkYellow;
                    WriteLine("{0}", "]");
                    ForegroundColor = fColour;
                }
                loadedCommands.Add(command);
            }
            if (printToConsole)
                WriteLine();

            foreach (var pluginClass in Registry.PluginInstances) {
                if (pluginClass == null) continue;
                if (pluginClass.PluginCommands == null) continue;

                loadedCommands.AddRange(pluginClass.PluginCommands);

                if (printToConsole) {
                    foreach (var command in pluginClass.PluginCommands.Where(x => (x != null && !string.IsNullOrEmpty(x.CommandTag)))) {
                        Write("\t{0,-30}\t{1,-65}", command.CommandTag, command.ShortDescription);

                        ForegroundColor = ConsoleColor.DarkYellow;
                        Write("{0,-3}", " [");
                        ForegroundColor = ConsoleColor.Cyan;
                        Write($"{{0,-{ pluginClass.PluginName.Length }}}", pluginClass.PluginName);
                        ForegroundColor = ConsoleColor.DarkYellow;
                        WriteLine("{0,2}", "]");
                        ForegroundColor = fColour;
                    }
                    WriteLine();
                }

            }
            if (printToConsole)
                WriteLine("To find out more about a command, type help <command>...");

            return null;
        }

        static ICommandResult Command_LoadPlugin(IPlugin sender, ICommand command, params string[] args) {

            var loadedPlugins = new List<string>();

            foreach (var file in args) {
                if (File.Exists(file) && (file.ToLowerInvariant().EndsWith(".exe") || file.ToLowerInvariant().EndsWith(".dll"))) {
                    var assembly = Assembly.LoadFrom(file);
                    foreach (var t in assembly.GetTypes()) {
                        if (t.IsSubclassOf(typeof(IPlugin)) || t.IsSubclassOf(typeof(Plugin))) {
                            Info("Attempting to load plugin {0}! Plugin name: {1}", file, t.Name);
                            try {
                                Registry.RegisterPlugin(assembly, t);
                                loadedPlugins.Add(file);
                            } catch (Exception ex) {
                                Fatal("FAILED to load plugin {0}! Reason: {1}", file, ex.Message);
                                Trace(ex.StackTrace);
                            }
                        }
                    }
                } else {
                    Error("Attempted to load invalid plugin {0}!", file);
                }
            }

            return new CommandResult<List<string>> { Message = string.Format("Loaded {0} plugins!", loadedPlugins.Count), Result = loadedPlugins };
        }

        static ICommandResult Command_UnloadPlugin(IPlugin sender, ICommand command, params string[] args) {
            var unloadedPlugins = new Dictionary<string, bool>();

            if (args.Length == 1 && args[0] == "*") {
                args = (from plugin in Registry.PluginInstances
                        select plugin.PluginName).ToArray();
            }

            foreach (var plugin in args) {
                Warn("Attempting to unload plugin {0}...", plugin);
                var instance = Registry.GetInstance(plugin.ToLowerInvariant());
                if (instance is default(IPlugin)) {
                    Error("Could not locate plugin {0}!", plugin);
                    continue;
                }

                var unloaded = Registry.RemovePlugin(instance);
                if (!unloaded)
                    Fatal("Could not unload plugin {0}!", plugin);
                else
                    Ok("Successfully unloaded plugin {0}!", plugin);
                unloadedPlugins.Add(plugin, unloaded);

            }

            return new CommandResult<Dictionary<string, bool>> {
                Message = $"Successfully unloaded { unloadedPlugins.Where(x => x.Value).Count() } plugins!",
                Result = unloadedPlugins
            };
        }

        static ICommandResult Command_PrintPrompt(IPlugin sender, ICommand command, params string[] args) {
            PrintPrompt();
            //return ComandResult.Empty; TODO
            return null;
        }

        static ICommandResult Command_Config(IPlugin sender, ICommand command, params string[] args) {

            command.SortArgs(out _, out var arguments, out _, args);

            foreach (var arg in arguments) {
                var trimmedArg = arg.Trim();
                switch (trimmedArg) {
                    case "--list":
                        Output(SystemConfig.ToString(ConfigSerializationType.Yaml));
                        return default; // No point in returning anything here. System configs are available system-wide.
                    case "--load":
                        SystemConfig.LoadConfigs();
                        return default; // Again, no point here. Events will be called accordingly.
                    case "--save":
                        SystemConfig.SaveConfigs();
                        return default;
                    case "--load-defaults":
                        SystemConfig.LoadDefaults();
                        return default;
                    default:
                        if (arg.StartsWith("--set")) {
                            foreach (var cfg in arg.Split(' ').Where(x => x != "--set")) {
                                var split = cfg.Split(new[] { '=' }, 2);
                                if (!SystemConfig.HasKey(split[0])) {
                                    Warn("Adding new configuration to Samarium...");
                                    var pseudoYaml = string.Join(": ", split);
                                    var kvPair = new DeserializerBuilder().Build().Deserialize<Dictionary<string, object>>(pseudoYaml);
                                    Info(
                                        "New configuration has following properties:\n\tname: {0}\n\tvalue: {1}\n\ttype: {2}",
                                        kvPair.First().Key, kvPair.First().Value, kvPair.First().Value.GetType().Name
                                    );
                                    SystemConfig.SetConfig(kvPair.First().Key, kvPair.First().Value);
                                    Ok("Added new configuration \"{0}\" to Samarium!", kvPair.First().Key);
                                }
                            }
                        }
                        if (trimmedArg.StartsWith("--list")) {
                            Output(SystemConfig.Where<object>(x => x.Contains(trimmedArg.Split(' ')[1])).Serialize());
                        }
                        break;
                }
            }

            return default;

            void Output(string serializedData) => WriteLine("Current Samarium configuration:\n{0}", serializedData);
        }

        static ICommandResult Command_List(IPlugin sender, ICommand command, params string[] args) {

            command.SortArgs(out _, out var arguments, out _, args);

            foreach (var arg in arguments) {
                switch (arg.ToLowerInvariant()) {
                    case "--all-plugins": {
                            var plugins = GetAllPlugins();
                            WriteLine("All known plugins:");
                            plugins.ForEach(x => WriteLine("\t{0}", x));
                            return new CommandResult<List<string>>("Listing all known plugins", plugins);
                        }
                    case "--loaded-plugins": {
                            var plugins = Registry.PluginInstances;
                            WriteLine("Loaded plugins:");
                            foreach (var plugin in plugins)
                                WriteLine("\t{0}", plugin.PluginName);
                            WriteLine("Total count: {0}", plugins.Count);
                            return new CommandResult<List<IPlugin>>("Listing all loaded plugins", plugins);
                        }
                    case "--ignored-plugins": {
                            var plugins = File.ReadLines($"{ PluginsDirectory }/plugins.ignore").ToList();
                            WriteLine("All ignored plugins:");
                            plugins.ForEach(x => WriteLine("\t{0}", x));
                            return new CommandResult<List<string>>("Listing all ignored plugins", plugins);
                        }
                }
            }

            return default;

        }

        static List<string> GetAllPlugins() {
            var plugins = new List<string>(File.ReadLines($"{ PluginsDirectory }/plugins.ignore"));
            plugins.AddRange(File.ReadLines($"{ PluginsDirectory }/plugins.include"));

            return plugins;
        }

        static void PrintPrompt() {
            WriteLine();
            Write(Prompt);
        }

    }
}

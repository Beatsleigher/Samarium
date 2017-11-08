using System;

namespace Samarium {

    using PluginFramework.Command;
    using PluginFramework.Plugin;

    using static System.Console;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    partial class Samarium {

        static ICommandResult ExecuteCommand(string commandTag, params string[] args) {
            // Check for Samarium commands first.
            // If default, check in plugins.
            var command = SystemCommands.FirstOrDefault(x => x.CommandTag == commandTag) ?? 
                          Registry.PluginInstances.FirstOrDefault(plugin => plugin.HasCommand(commandTag))?.GetCommand(commandTag);

            if (command is default) {
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

            if (command is default) {
                Error("Attempted to execute command {0} with params {1}; command was not found!", commandTag, string.Join(", ", args));
                return default;
            }

            return await command.ExecuteAsync(args);
        }

        static ICommandResult Command_Help(IPlugin sender, ICommand _, params string[] args) {
            var fColour = ForegroundColor;
            var loadedCommands = new List<ICommand>();
            var printToConsole = (!args.Contains("--q") && !args.Contains("--quiet"));

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
                        Write("\t{0,-25}\t{1,-130}", command.CommandTag, command.ShortDescription);

                        ForegroundColor = ConsoleColor.DarkYellow;
                        Write("{0,-2}", "[");
                        ForegroundColor = ConsoleColor.Cyan;
                        Write("{0,-" + pluginClass.PluginName.Length + "}", pluginClass.PluginName);
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
            throw new NotImplementedException();
        }

        static ICommandResult Command_UnloadPlugin(IPlugin sender, ICommand command, params string[] args) {
            throw new NotImplementedException();
        }

        static ICommandResult Command_PrintPrompt(IPlugin sender, ICommand command, params string[] args) {
            PrintPrompt();
            //return ComandResult.Empty; TODO
            return null;
        }

        static void PrintPrompt() {
            WriteLine();
            Write(Prompt);
        }

    }
}

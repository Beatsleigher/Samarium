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

        static ICommandResult Command_Help(IPlugin sender, ICommand command, params string[] args) {
            return null;
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

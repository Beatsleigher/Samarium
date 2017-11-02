using System;

using Samarium.PluginFramework;

namespace Samarium {

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    partial class Samarium {

        static partial void SetNoPlugins() => AutoLoadPlugins = false;

        static partial void SetLogDir() {
            var tmp = Environment.GetCommandLineArgs()
                .Where(x => x.StartsWith("--logdir=", StringComparison.InvariantCultureIgnoreCase))?
                .FirstOrDefault();
            LogDirectory = tmp?.Substring((int)tmp?.IndexOf('=') + 1) ?? default;
        }

        static partial void SetAutoExec() {
            StartupCommands.AddRange(
                from arg in Environment.GetCommandLineArgs()
                where arg.StartsWith("--exec=")
                select arg.Substring(arg.IndexOf('=') + 1)
                          .TrimMatchingQuotes()
                          .Split(' ')
            );
        }

    }
}

using System;

namespace Samarium {
    using static global::Samarium.PluginFramework.UI.ConsoleUI;
    using PluginFramework;

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::Samarium.PluginFramework.UI;

    public static partial class Samarium {

        #region Const and effectively const fields
        const string ApplicationName = nameof(Samarium);
        static readonly Version Version = typeof(Samarium).Assembly.GetName().Version;

        static readonly List<(string Argument, string Description, Action Handler)> Arguments = new List<(string Argument, string Description, Action Handler)> {
            ("--help", "Prints this help menu.", PrintHelp),
            ("--no-plugins", "Stops automatic plugin loading. Useful for debugging.", SetNoPlugins),
            ("--exec=\"<cmd>\"", "Executes a command immediately after successful booting.", SetAutoExec),
            ("--init", "Re-initialization \"wizard\"", () => InitWizard()),
            ("--logdir=\"<dir>\"", "Set the logging directory for this instance.", SetLogDir)
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

        public static string LogDirectory { get; set; }
        #endregion

        public static int Main(string[] args) {

            // Start by parsing arguments

            

            return 0;
        }

        #region Prototypes

        static partial void SetNoPlugins();

        static partial void SetLogDir();

        static partial void SetAutoExec();
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

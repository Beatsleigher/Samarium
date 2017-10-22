using System;

namespace Samarium {

    using log4net;
    using log4net.Core;
    using log4net.Config;

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

public class Program {

        static void Main(string[] args) {
            BasicConfigurator.Configure();
            var log = LogManager.GetLogger("Samarium");
            log.Logger.IsEnabledFor(log4net.Core.Level.All);
            log.Info("Test");
            log.Info(typeof(Program).Assembly.Location);
            Console.Read();
        }

    }
}

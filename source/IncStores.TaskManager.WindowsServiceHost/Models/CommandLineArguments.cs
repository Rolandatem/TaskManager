using Incstores.Common.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace IncStores.TaskManager.WindowsServiceHost.Models
{
    public class CommandLineArguments
    {
        #region "Properties"
        public string[] Args { get; set; } = null;
        public string Environment { get; set; } = null;
        public bool RunAsConsole { get; set; } = false;
        public LogLevel? OverrideMinLogLevel { get; set; } = null;
        #endregion

        public CommandLineArguments(string[] args)
        {
            this.Args = args;

            #region "Convert all arguments to lowercase"
            args
                .ToList()
                .ConvertAll(arg => arg.ToLower())
                .ToArray();
            #endregion

            #region "Check for Environment argument"
            Action<string> commonEnvironmentSet = (arg) =>
            {
                this.Environment = args[Array.IndexOf(args, arg) + 1];
            };

            if (args.Contains("--environment")) { commonEnvironmentSet("--environment"); }
            if (args.Contains("--e")) { commonEnvironmentSet("--e"); }
            #endregion

            #region "Check for Console"
            if (args.Contains("--console") || args.Contains("--c"))
            { this.RunAsConsole = true; }
            #endregion

            #region "Check Product LogLevel override"
            Func<string, string> commonOverrideMinLogLevel = (arg) =>
            {
                return args[Array.IndexOf(args, arg) + 1];
            };

            string overrideStringValue = args.Contains("--minloglevel") ? commonOverrideMinLogLevel("--minloglevel") :
                args.Contains("--mll") ? commonOverrideMinLogLevel("--mll") :
                null;

            if (overrideStringValue.Exists())
            {
                bool exists = Enum.TryParse(overrideStringValue, true, out LogLevel overrideValue);
                if (exists)
                { this.OverrideMinLogLevel = overrideValue; }
                else
                {
                    Console.WriteLine("Unknown minimum log level supplied.");
                    throw new Exception("Unknown minimum log level supplied.");
                }
            }
            #endregion
        }
    }
}

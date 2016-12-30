using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Tools.WindowsInstallerXml;

namespace SetupCustomize
{
    public class FileVersionPreprocessor : PreprocessorExtension
    {
        public override string[] Prefixes { get; } = { "FileVersion" };

        public override string EvaluateFunction(string prefix, string function, string[] args)
        {
            string result = null;

            switch (prefix)
            {
                case "FileVersion":
                    {
                        if (args.Length == 0) throw new ArgumentException("File name parameter was not specified.");
                        if (!File.Exists(args[0])) throw new ArgumentException($"Cannot find file \"{args[0]}\".");

                        string filename = Path.IsPathRooted(args[0]) ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), args[0]);
                        FileVersionInfo vi = FileVersionInfo.GetVersionInfo(filename);
                        PropertyInfo pi = vi.GetType().GetProperty(function);
                        if (pi == null) throw new ArgumentException($"Unable to find property {function} in FileVersionInfo.");

                        result = pi.GetValue(vi, null).ToString();
                        break;
                    }
            }

            return result;
        }
    }
}

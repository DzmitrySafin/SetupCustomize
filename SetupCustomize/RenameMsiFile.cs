using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SetupCustomize
{
    /*
     * Insert block like this into .wixproj file:
     *
     * <UsingTask TaskName="RenameMsiFile" AssemblyFile="$(SolutionDir)Dependencies\SetupCustomize.dll" />
     * <Target Name="RenameMsiTarget">
     *   <Message Text="MSI build renaming..." />
     *   <RenameMsiFile MsiName="$(TargetDir)\$(OutputName).msi" WxiName="$(ProjectDir)AssemblyInfo.wxi" TargetName="$(TargetName)" Platform="$(Platform)" />
     * </Target>
     * <Target Name="AfterBuild">
     *   <CallTarget Targets="RenameMsiTarget" />
     * </Target>
     *
     * AssemblyInfo.wxi example:
     *
     * <?xml version="1.0" encoding="utf-8"?>
     * <Include>
     *   <?define MajorVersion = "1" ?>
     *   <?define MinorVersion = "2" ?>
     *   <?define BuildVersion = "3" ?>
     *   <?define ProductVersion = "$(var.MajorVersion).$(var.MinorVersion).$(var.BuildVersion)" ?>
     *   <?define ProductName = "$(var.TargetName) $(var.ProductVersion) $(var.Platform)" ?>
     * </Include>
     */
    public class RenameMsiFile : Task
    {
        #region Properties

        [Required]
        public string MsiName { get; set; }

        [Required]
        public string WxiName { get; set; }

        public string TargetName { get; set; }

        public string Platform { get; set; }

        #endregion

        public override bool Execute()
        {
            string productName = ParseProductName();
            if (string.IsNullOrEmpty(productName))
            {
                Log.LogError("Cannot accuire ProductName parameter.");
                return false;
            }

            if (!File.Exists(MsiName))
            {
                Log.LogError($"Cannot find MSI file \"{MsiName}\".");
                return false;
            }

            string name = $"{Path.GetDirectoryName(MsiName)}\\{productName}{Path.GetExtension(MsiName)}";
            try
            {
                if (File.Exists(name)) File.Delete(name);
                File.Move(MsiName, name);
            }
            catch (Exception ex)
            {
                Log.LogError(ex.Message);
                return false;
            }

            return true;
        }

        private string ParseProductName()
        {
            var lines = LoadWxiContent();
            if (lines == null) return null;

            var dictionary = new StringDictionary
            {
                {"TargetName", TargetName},
                {"Platform", Platform},
                {"ProductName", string.Empty}
            };

            const string pattern = @"\$\(var\.(?<name>\w+)\)";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            foreach (string line in lines)
            {
                var pair = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length == 2)
                {
                    string line0 = pair[0].Trim();
                    string line1 = pair[1].Trim().Trim('\"', '\'');

                    Match m = regex.Match(line1);
                    while (m.Success)
                    {
                        var g = m.Groups["name"];
                        if (g.Success) line1 = line1.Replace(m.Value, dictionary[g.Value]);

                        m = m.NextMatch();
                    }

                    if (dictionary.ContainsKey(line0)) dictionary[line0] = line1;
                    else dictionary.Add(line0, line1);
                }
            }

            return dictionary["ProductName"];
        }

        private IEnumerable<string> LoadWxiContent()
        {
            if (!File.Exists(WxiName))
            {
                Log.LogError($"Cannot find WXI file \"{WxiName}\".");
                return null;
            }

            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(File.ReadAllText(WxiName));
            }
            catch (Exception ex)
            {
                Log.LogError(ex.Message);
                return null;
            }

            var content = doc.GetElementsByTagName("Include");
            if (content.Count == 0)
            {
                Log.LogError("Error parsing WXI file.");
                return null;
            }

            return (from XmlNode childNode in content[0].ChildNodes where childNode.Name == "define" select childNode.Value).ToList();
        }
    }
}

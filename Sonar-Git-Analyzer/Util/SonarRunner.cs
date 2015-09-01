// -----------------------------------------------------------------------
// <copyright file="SonarRunner.cs"
//           project="Sonar-Git-Analyzer"
//           company="Mairegger Michael"
//           webpage="http://michaelmairegger.wordpress.com">
//     Copyright © Mairegger Michael, 2015 
//     All rights reserved
// </copyright>
// -----------------------------------------------------------------------
namespace Sonar_Git_Analyzer.Util
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    internal class SonarRunner
    {
        private readonly Lazy<string> _sonarRunnerPath;

        private SonarRunner(ArgumentHelper helper)
        {
            _sonarRunnerPath = new Lazy<string>(() => GetSonarRunnerPath(helper));
        }

        public bool Execute(Configuration configuration, CommitHelper commitHelper)
        {
            if (!string.IsNullOrEmpty(_sonarRunnerPath.Value))
            {
                DirectoryInfo dropDirectory = new DirectoryInfo(configuration.DropLocation);
                dropDirectory = dropDirectory.GetDirectories().SingleOrDefault(i => i.FullName.Contains(commitHelper.SHA));

                if (dropDirectory == null)
                {
                    return false;
                }

                var sourceDirectory = dropDirectory.GetDirectories().SingleOrDefault(i => i.FullName.Contains(commitHelper.SHA));
                if (sourceDirectory == null)
                {
                    return false;
                }

                var userName = string.Format(
                    "-D project.settings={0} " +
                    "-D sonar.projectBaseDir={1} " +
                    "-D sonar.sources={1} " +
                    "-D sonar.projectVersion={2} " +
                    "-D sonar.projectDate={3:yyyy-MM-dd}",
                    configuration.SonarProperties,
                    sourceDirectory.FullName,
                    commitHelper.Version,
                    commitHelper.CommitDateTime);

                var process = new Process
                              {
                                  StartInfo =
                                  {
                                      FileName = _sonarRunnerPath.Value,
                                      UseShellExecute = false,
                                      Arguments = userName
                                  }
                              };
                process.Start();
                process.WaitForExit();

                commitHelper.IsAnalyzed = process.ExitCode == 0;

                return commitHelper.IsAnalyzed;
            }

            Console.WriteLine("sonar-runner not found in location {0}", _sonarRunnerPath.Value);

            return false;
        }

        private static string GetSonarRunnerPath(ArgumentHelper helper)
        {
            if (helper.SonarRunnerPath.Exists)
            {
                return helper.SonarRunnerPath.FullName;
            }

            string executable = "sonar-runner.bat";
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                executable = "sonar-runner";
            }

            var directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory.Parent != null)
            {
                var enumeration = directory.Parent.EnumerateDirectories("sonar-runner*").ToList();
                if (enumeration.Any())
                {
                    var folder = enumeration.First();
                    var file = new FileInfo(Path.Combine(folder.FullName, "bin", executable));
                    if (file.Exists)
                    {
                        return file.FullName;
                    }
                }

                directory = directory.Parent;
            }
            return null;
        }

        public static SonarRunner Instance { get; private set; }
        public static void CreateInstance(ArgumentHelper config)
        {
            Instance = new SonarRunner(config);
        }
    }
}
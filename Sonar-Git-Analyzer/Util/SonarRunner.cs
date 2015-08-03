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
        public static bool Execute(Configuration configuration, CommitHelper commitHelper)
        {
            if (File.Exists(configuration.SonarRunnerPath))
            {
                DirectoryInfo d = new DirectoryInfo(configuration.DropLocation);
                d = d.GetDirectories().SingleOrDefault(i => i.FullName.Contains(commitHelper.SHA));
                var finalDirectory = d.GetDirectories().SingleOrDefault(i => i.FullName.Contains(commitHelper.SHA));

                var userName = string.Format("-D project.settings={0} -D sonar.projectBaseDir={1} -D sonar.projectVersion={2} -D sonar.sources={1}", configuration.SonarProperties, finalDirectory.FullName, commitHelper.Version);
                var process = new Process
                              {
                                  StartInfo =
                                  {
                                      FileName = configuration.SonarRunnerPath, 
                                      UseShellExecute = false, 
                                      Arguments = userName
                                  }
                              };
                process.Start();
                process.WaitForExit();
                return true;
            }

            Console.WriteLine("sonar-runner not found in location {0}", configuration.SonarRunnerPath);

            return false;
        }
    }
}
// -----------------------------------------------------------------------
// <copyright file="GitHubHelper.cs"
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
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    internal class GitHubHelper
    {
        private Lazy<HttpClient> _httpClient;
        private bool _firstRun = true;

        public async Task<bool> Download(Configuration configuration, CommitHelper applicationState)
        {
            var client = GetLazyClient(configuration);

            var destinationDirectoryName = Path.Combine(configuration.DropLocation, applicationState.SHA);
            if (!Directory.Exists(destinationDirectoryName))
            {
                string zipFile = string.Format("https://github.com/{0}/archive/{1}.zip", configuration.GitHubRepository, applicationState.SHA);

                var request = await client.GetAsync(zipFile);
                request.EnsureSuccessStatusCode();

                ZipArchive zip = new ZipArchive(await request.Content.ReadAsStreamAsync());
                zip.ExtractToDirectory(destinationDirectoryName);

                return true;
            }

            if (_firstRun)
            {
                Console.WriteLine("Skip downloading: {0} (v{1})", applicationState.SHA, applicationState.Version);
            }

            return false;
        }

        public async Task<string> Download(Configuration configuration)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
                client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", configuration.GitHubRepository));

                string shaKey;
                if (!Regex.IsMatch(configuration.Branch, @"\A\b[0-9a-fA-F]+\b\Z"))
                {
                    string head = string.Format("git/refs/heads/{0}", configuration.Branch);
                    var result = await client.GetStringAsync(head);
                    JObject obj = JObject.Parse(result);

                    shaKey = (string)obj["object"]["sha"];
                }
                else
                {
                    shaKey = configuration.Branch;
                }

                await Download(configuration, null);
                return shaKey;
            }
        }

        internal async Task<IList<CommitHelper>> FetchHistory(Configuration configuration, bool fetch)
        {
            var httpClient = GetLazyClient(configuration);

            var result1 = await httpClient.GetStringAsync("tags");

            var obj = JArray.Parse(result1);

            var tempList = from commit in obj.Children()
                                        select new CommitHelper
                                               {
                                                   Version = commit["name"].Value<string>(),
                                                   SHA = commit["commit"]["sha"].Value<string>()
                                               };

            var commitList = tempList.Reverse().ToList();

            int commitCount = 0;

            if (fetch)
            {
                foreach (var commit in commitList)
                {
                    commitCount++;
                    Console.Write("Downloading {0} out of {1}\r", commitCount, commitList.Count());
                    if (await Download(configuration, commit))
                    {
                        Console.WriteLine("{0} out of {1} commits downloaded", commitCount, commitList.Count());
                    }
                }

                _firstRun = false;
            }

            return commitList;
        }

        private HttpClient GetLazyClient(Configuration configuration)
        {
            if (_httpClient == null)
            {
                _httpClient = new Lazy<HttpClient>(() =>
                                                   {
                                                       var client = new HttpClient();
                                                       client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
                                                       client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", configuration.GitHubRepository));

                                                       return client;
                                                   });
            }

            return _httpClient.Value;
        }
    }
}
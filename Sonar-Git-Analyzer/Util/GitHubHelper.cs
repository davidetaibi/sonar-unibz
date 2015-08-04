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
        private readonly Configuration _configuration;
        private Lazy<HttpClient> _httpClient;
        private bool _firstRun = true;

        public GitHubHelper(Configuration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> Download(CommitHelper applicationState)
        {
            var client = GetLazyClient();

            var destinationDirectoryName = Path.Combine(_configuration.DropLocation, applicationState.SHA);
            if (!Directory.Exists(destinationDirectoryName))
            {
                string zipFile = string.Format("https://github.com/{0}/archive/{1}.zip", _configuration.GitHubRepository, applicationState.SHA);

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

        public async Task<string> Download()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
                client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", _configuration.GitHubRepository));

                string shaKey;
                if (!Regex.IsMatch(_configuration.Branch, @"\A\b[0-9a-fA-F]+\b\Z"))
                {
                    string head = string.Format("git/refs/heads/{0}", _configuration.Branch);
                    var result = await client.GetStringAsync(head);
                    JObject obj = JObject.Parse(result);

                    shaKey = (string)obj["object"]["sha"];
                }
                else
                {
                    shaKey = _configuration.Branch;
                }

                await Download(null);
                return shaKey;
            }
        }

        public async Task SetCommitDate(CommitHelper commit)
        {
            if (commit.CommitDateTime > DateTime.MinValue)
            {
                return;
            }
            var httpClient = GetLazyClient();
            var commitDetail = await httpClient.GetStringAsync(commit.Url);
            var jobject = JObject.Parse(commitDetail);

            DateTime dateOfCommit = jobject["commit"]["author"]["date"].Value<DateTime>();
            commit.CommitDateTime = dateOfCommit;
        }

        internal async Task<IList<CommitHelper>> FetchHistory(bool fetch)
        {
            var httpClient = GetLazyClient();

            var result1 = await httpClient.GetStringAsync("tags");

            var obj = JArray.Parse(result1);

            var tempList = from commit in obj.Children()
                           select new CommitHelper
                                  {
                                      Version = commit["name"].Value<string>(),
                                      SHA = commit["commit"]["sha"].Value<string>(),
                                      Url = commit["commit"]["url"].Value<string>()
                                  };

            var commitList = tempList.Reverse().ToList();

            int commitCount = 0;

            if (fetch)
            {
                foreach (var commit in commitList)
                {
                    commitCount++;
                    Console.Write("Downloading {0} out of {1}\r", commitCount, commitList.Count());
                    if (await Download(commit))
                    {
                        Console.WriteLine("{0} out of {1} commits downloaded", commitCount, commitList.Count());
                    }
                }

                _firstRun = false;
            }

            return commitList;
        }

        private HttpClient GetLazyClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new Lazy<HttpClient>(() =>
                                                   {
                                                       var client = new HttpClient();
                                                       client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
                                                       client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", _configuration.GitHubRepository));

                                                       return client;
                                                   });
            }

            return _httpClient.Value;
        }
    }
}
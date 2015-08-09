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
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Octokit;

    internal class GitHubHelper
    {
        private readonly Configuration _configuration;
        private const string SoanarAnalyzer = "Sonar-Analyzer";
        readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue(SoanarAnalyzer));
        private Lazy<HttpClient> _httpClient;
        private bool _firstRun = true;

        public GitHubHelper(Configuration configuration, ArgumentHelper helper)
        {
            _configuration = configuration;

            if (!string.IsNullOrEmpty(helper.Token))
            {
                _client.Credentials = new Credentials(helper.Token);
            }
            else if(!string.IsNullOrEmpty(helper.GitHubUserName) && !string.IsNullOrEmpty(helper.Password))
            {
                _client.Credentials = new Credentials(helper.GitHubUserName, helper.Password);
            }
        }

        public async Task<bool> Download(CommitHelper applicationState)
        {
            var destinationDirectoryName = Path.Combine(_configuration.DropLocation, applicationState.SHA);
            if (!Directory.Exists(destinationDirectoryName))
            {
                string zipFile = string.Format("https://github.com/{0}/archive/{1}.zip", _configuration.GitHubRepository, applicationState.SHA);

                var request = await GetLazyClient().GetAsync(zipFile);
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

        //public async Task<string> Download()
        //{
        //    using (var client = new HttpClient())
        //    {
        //        client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
        //        client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", _configuration.GitHubRepository));

        //        string shaKey;
        //        if (!Regex.IsMatch(_configuration.Branch, @"\A\b[0-9a-fA-F]+\b\Z"))
        //        {
        //            string head = string.Format("git/refs/heads/{0}", _configuration.Branch);
        //            var result = await client.GetStringAsync(head);
        //            JObject obj = JObject.Parse(result);

        //            shaKey = (string)obj["object"]["sha"];
        //        }
        //        else
        //        {
        //            shaKey = _configuration.Branch;
        //        }

        //        await Download(null);
        //        return shaKey;
        //    }
        //}

        public async Task SetCommitDate(CommitHelper commit)
        {
            if (commit.CommitDateTime > DateTime.MinValue)
            {
                return;
            }
            var commitDetail = await _client.Repository.Commits.Get(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository, commit.SHA);
            commit.CommitDateTime = commitDetail.Commit.Author.Date.Date;
        }

        internal async Task<IList<CommitHelper>> FetchHistory(bool fetch)
        {
            var result1 = await _client.Repository.GetAllTags(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository);

            //var obj = JArray.Parse(result1);

            var tempList = from commit in result1
                           select new CommitHelper
                                  {
                                      Version = commit.Name,
                                      SHA = commit.Commit.Sha,
                                      Url = commit.Commit.Url
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
                                                       client.DefaultRequestHeaders.Add("User-Agent", SoanarAnalyzer);
                                                       client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", _configuration.GitHubRepository));

                                                       return client;
                                                   });
            }

            return _httpClient.Value;
        }

        /// <summary>
        /// Returs the next time the API-Search rate is reset, or null when the amount of remaining request is not zero.
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime?> GetNextAPIResetTime()
        {
            Console.Write("Loading API LIMITS");
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", SoanarAnalyzer);
            var result = await _client.Connection.Get<string>(new Uri("https://api.github.com/rate_limit"), new TimeSpan(0, 0, 15));
            var obj = JObject.Parse(result.HttpResponse.Body.ToString());

            var helper = new
                         {
                             Remaining = obj["resources"]["core"]["remaining"].Value<int>(),
                             Limit = obj["resources"]["core"]["limit"].Value<int>(),
                             ResetTime = (
                                 new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) +
                                 TimeSpan.FromSeconds(obj["resources"]["core"]["reset"].Value<long>())
                                 ).ToLocalTime()
                         };
            
            Console.WriteLine("\rRequest Limit: {0}/{1}\tReset counter on: {2}", helper.Remaining, helper.Limit, helper.ResetTime);

            Console.ResetColor();
            if (helper.Remaining == 0)
            {
                return helper.ResetTime;
            }
            return null;
        }
    }
}
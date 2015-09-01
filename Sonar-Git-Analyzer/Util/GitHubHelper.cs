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
            if (helper.Anonymous)
            {
                _client.Credentials = Credentials.Anonymous;
            }
            if (!string.IsNullOrEmpty(helper.Token))
            {
                _client.Credentials = new Credentials(helper.Token);
            }
            else if(!string.IsNullOrEmpty(helper.GitHubUserName) && !string.IsNullOrEmpty(helper.Password))
            {
                _client.Credentials = new Credentials(helper.GitHubUserName, helper.Password);
            }
            Console.WriteLine("Authentication Type: {0}", _client.Credentials.AuthenticationType);
        }

        private async Task<bool> Download(CommitHelper applicationState)
        {
            var temp = _configuration.SHAs.SingleOrDefault(i => i.SHA == applicationState.SHA);
            if (temp == null)
            {
                _configuration.SHAs.Add(applicationState);
                temp = applicationState;
            }
            if (temp.IsAnalyzed)
            {
                return true;
            }

            var dropLocation = new DirectoryInfo(_configuration.DropLocation);
            if (!dropLocation.Exists)
            {
                dropLocation.Create();
            }
            var destinationDirectoryName = Path.Combine(_configuration.DropLocation, temp.SHA);

            if (Directory.Exists(destinationDirectoryName))
            {
                Directory.Delete(destinationDirectoryName, true);
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                dropLocation.CreateSubdirectory(temp.SHA);
                string zipFile = string.Format("https://github.com/{0}/{1}/archive/{2}.zip", _configuration.GitHubRepositoryOwner, _configuration.GitHubRepository, temp.SHA);

                var request = await GetLazyClient().GetAsync(zipFile);
                request.EnsureSuccessStatusCode();

                ZipArchive zip = new ZipArchive(await request.Content.ReadAsStreamAsync());
                

                zip.ExtractToDirectory(destinationDirectoryName);

                temp.IsAnalyzed = true;

                _configuration.Save();

                return true;
            }

            if (_firstRun)
            {
                Console.WriteLine("Skip downloading: {0} (v{1})", temp.SHA, temp.Version);
            }

            return false;
        }

        public async Task SetCommitDate(CommitHelper commit)
        {
            if (commit.CommitDateTime == DateTimeOffset.MinValue)
            {
                var commitDetail = await _client.Repository.Commits.Get(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository, commit.SHA);
                commit.CommitDateTime = commitDetail.Commit.Author.Date.Date;
            }
        }

        internal async Task<IList<CommitHelper>> FetchHistory(bool fetch)
        {
            IEnumerable<CommitHelper> tempList;
            if (_configuration.CommitAnalyzer == null)
            {
                var result = await _client.Repository.GetAllTags(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository);
                tempList = from commit in result
                           select new CommitHelper
                           {
                               Version = commit.Name,
                               SHA = commit.Commit.Sha,
                               Url = commit.Commit.Url
                           };
            }
            else
            {
                var commitRequest = new CommitRequest
                        {
                            Since = _configuration.CommitAnalyzer.LastCommitDate
                        };

                var result = await _client.Repository.Commits.GetAll(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository, commitRequest);

                if ((_configuration.CommitAnalyzer.LastCommitDate == DateTimeOffset.MinValue && _configuration.CommitAnalyzer.AnalyzationBehavior == AnalyzationBehavior.FirstAllThenNewest) || 
                    _configuration.CommitAnalyzer.AnalyzationBehavior == AnalyzationBehavior.All)
                {
                }
                else if(_configuration.CommitAnalyzer.AnalyzationBehavior == AnalyzationBehavior.Newest || _configuration.CommitAnalyzer.AnalyzationBehavior == AnalyzationBehavior.FirstAllThenNewest)
                {
                    result = result.Take(1).ToList();
                }

                _configuration.CommitAnalyzer.LastCommitDate = DateTime.Now;

                tempList = from commit in result
                           select new CommitHelper
                           {
                               Version = "0.0",
                               SHA = commit.Sha,
                               Url = commit.Url,
                               CommitDateTime = commit.Commit.Author.Date
                           };
            }

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
        public async Task<DateTime?> GetNextApiResetTime()
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
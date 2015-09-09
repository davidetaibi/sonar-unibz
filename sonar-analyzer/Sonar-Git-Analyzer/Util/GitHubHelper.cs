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
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Octokit;
    using Octokit.Internal;

    internal class GitHubHelper
    {
        private const string SoanarAnalyzer = "Sonar-Analyzer";
        private readonly Configuration _configuration;
        private readonly GitHubClient _client;
        private Lazy<HttpClient> _httpClient;
        private bool _firstRun = true;
        private HttpClientHandler httpClientHandler = new HttpClientHandler();

        public GitHubHelper(Configuration configuration, ArgumentHelper helper)
        {
            if (!string.IsNullOrEmpty(helper.Proxy))
            {
                Console.WriteLine("Use Proxy {0}", helper.Proxy);
                httpClientHandler = new HttpClientHandler
                {
                    Proxy = new WebProxy(helper.Proxy, false),
                    UseProxy = true
                };
                var httpClientAdapter = new HttpClientAdapter(() => httpClientHandler);
                _client = new GitHubClient(new Connection(new ProductHeaderValue(SoanarAnalyzer), httpClientAdapter));
            }
            else
            {
                _client = new GitHubClient(new ProductHeaderValue(SoanarAnalyzer));
            }



            _configuration = configuration;
            if (helper.Anonymous)
            {
                _client.Credentials = Credentials.Anonymous;
            }
            if (!string.IsNullOrEmpty(helper.Token))
            {
                _client.Credentials = new Credentials(helper.Token);
            }
            else if (!string.IsNullOrEmpty(helper.GitHubUserName) && !string.IsNullOrEmpty(helper.Password))
            {
                _client.Credentials = new Credentials(helper.GitHubUserName, helper.Password);
            }
            Console.WriteLine("Authentication Type: {0}", _client.Credentials.AuthenticationType);
        }

        /// <summary>
        ///     Returs the next time the API-Search rate is reset, or null when the amount of remaining request is not zero.
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime?> GetNextApiResetTime()
        {
            Console.Write("Loading API LIMITS");
            Console.ForegroundColor = ConsoleColor.Yellow;

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

        public async Task SetCommitDate(CommitHelper commit)
        {
            if (commit.CommitDateTime == DateTimeOffset.MinValue)
            {
                var commitDetail = await _client.Repository.Commits.Get(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository, commit.SHA);
                commit.CommitDateTime = commitDetail.Commit.Author.Date.Date;
            }
        }

        internal async Task<IList<CommitHelper>> FetchHistory()
        {
            IEnumerable<CommitHelper> tempList;
            if (_configuration.AnalysationBehavior == AnalysationBehavior.Tags)
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
                                        Since = _configuration.LastSuccessfulAnalyzedCommit
                                    };

                var result = await _client.Repository.Commits.GetAll(_configuration.GitHubRepositoryOwner, _configuration.GitHubRepository, commitRequest);

                if ((_configuration.LastSuccessfulAnalyzedCommit == DateTimeOffset.MinValue && _configuration.AnalysationBehavior == AnalysationBehavior.FirstAllThenNewest) ||
                    _configuration.AnalysationBehavior == AnalysationBehavior.All)
                {
                }
                else if (_configuration.AnalysationBehavior == AnalysationBehavior.Newest || _configuration.AnalysationBehavior == AnalysationBehavior.FirstAllThenNewest)
                {
                    result = result.Take(1).ToList();
                }

                tempList = from commit in result
                           select new CommitHelper
                                  {
                                      Version = "0.0",
                                      SHA = commit.Sha,
                                      Url = commit.Url,
                                      CommitDateTime = commit.Commit.Author.Date
                                  };
            }

            var commitList = tempList.OrderBy(i => i.CommitDateTime).ToList();

            RemoveDuplicateDates(commitList);

            int commitCount = 0;

            foreach (var commit in commitList)
            {
                commitCount++;
                Console.Write("\rDownloading {0} out of {1}", commitCount, commitList.Count());
                await Download(commit);
            }
            Console.WriteLine("\rDownloading {0} out of {1} completed.", commitCount, commitList.Count());

            _firstRun = false;

            return commitList;
        }

        private async Task<bool> Download(CommitHelper applicationState)
        {
            var temp = _configuration.CommitList.SingleOrDefault(i => i.SHA == applicationState.SHA);
            if (temp == null)
            {
                _configuration.CommitList.Add(applicationState);
                temp = applicationState;
            }
            if (temp.IsAnalyzed)
            {
                return true;
            }

            var dropLocation = new DirectoryInfo(_configuration.DropLocation);
            if (!dropLocation.Exists)
            {
                try
                {
                    dropLocation.Create();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
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

                _configuration.Save();

                return true;
            }

            if (_firstRun)
            {
                Console.WriteLine("Skip downloading: {0} (v{1})", temp.SHA, temp.Version);
            }

            return false;
        }

        private HttpClient GetLazyClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new Lazy<HttpClient>(() =>
                                                   {
                                                       var client = new HttpClient(httpClientHandler);
                                                       client.DefaultRequestHeaders.Add("User-Agent", SoanarAnalyzer);
                                                       client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", _configuration.GitHubRepository));

                                                       return client;
                                                   });
            }

            return _httpClient.Value;
        }

        private void RemoveDuplicateDates(IList<CommitHelper> result)
        {
            var groupedList = from i in result
                              group i by i.CommitDateTime.Date
                              into g
                              select g;

            foreach (var group in groupedList)
            {
                var dt = group.Max(i => i.CommitDateTime);

                foreach (var item in group.Where(i => i.CommitDateTime != dt))
                {
                    result.Remove(item);
                }
            }
        }
    }
}
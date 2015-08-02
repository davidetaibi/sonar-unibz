// -----------------------------------------------------------------------
// <copyright file="GitHubHelper.cs"
//           project="Sonar-Git-Analyzer"
//           company="Mairegger Michael"
//           webpage="http://michaelmairegger.wordpress.com">
//     Copyright © Mairegger Michael, 2015 
//     All rights reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Sonar_Git_Analyzer
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Collections.Generic;

    internal class GitHubHelper
    {
        private static Lazy<HttpClient> HttpClient;

        private static HttpClient GetLazyClient(Configuration configuration)
        {
            if(HttpClient == null)

            {


                HttpClient = new Lazy<System.Net.Http.HttpClient>(() =>
                {
                    var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
                    client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", configuration.GitHubRepository));

                    return client;
                });


            }

            return HttpClient.Value;
                
        }
        public static async Task<string> Download(Configuration configuration)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Sonar-Analyzer");
                client.BaseAddress = new Uri(string.Format("https://api.github.com/repos/{0}/", configuration.GitHubRepository));

                string shaKey;
                if (!System.Text.RegularExpressions.Regex.IsMatch(configuration.Branch, @"\A\b[0-9a-fA-F]+\b\Z"))
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

                await Analyze(configuration, null);
                return shaKey;
            }
        }

        public static async Task Analyze(Configuration configuration, CommitHelper commitHelper)
        {
            var client = GetLazyClient(configuration);

            var destinationDirectoryName = Path.Combine(configuration.DropLocation, commitHelper.SHA);
            if (!Directory.Exists(destinationDirectoryName))
            {
                string zipFile = string.Format("https://github.com/{0}/archive/{1}.zip", configuration.GitHubRepository, commitHelper.SHA);

                var request = await client.GetAsync(zipFile);
                request.EnsureSuccessStatusCode();

                ZipArchive zip = new ZipArchive(await request.Content.ReadAsStreamAsync());
                zip.ExtractToDirectory(destinationDirectoryName);
            }
            else
            {
                Console.WriteLine("I will skip this commit because the commit {0} with version {1} was already analyzed", commitHelper.SHA, commitHelper.Version);
            }
        }

        internal static async Task<IEnumerable<CommitHelper>> FetchHistory(Configuration configuration, bool fetch)
        {
            var httpClient = GetLazyClient(configuration);

            var result1 = await httpClient.GetStringAsync("tags");

            var obj = JArray.Parse(result1);


                var r = from commit in obj.Children()
                             select new CommitHelper(){
                                 Version = commit["name"].Value<string>(),
                                 SHA = commit["commit"]["sha"].Value<string>()
                             }
                             ;

            int commitCount = 1;

            if (fetch)
            {
                foreach (var commit in r.ToList())
                {
                    await Analyze(configuration, commit);

                    Console.WriteLine("{0} out of {1} commits downloaded", commitCount++, r.Count());
                }
            }

            return r;
        }
    }
}

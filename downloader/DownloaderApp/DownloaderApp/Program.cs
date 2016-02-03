using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Newtonsoft.Json;

namespace DownloaderApp
{
    class Program
    {
        private static string _token;
        private static string _repo;
        private static string _targetPath;
        private static GitHubClient _gitHub;
        private static string _owner;

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                GetRepoData();
                GetSaveLocation();
                Connect();
                await DownloadData();
            }).Wait();
        }

        private static void GetRepoData()
        {
            bool inputIsCorrect;
            do
            {
                Console.WriteLine("GitHub token: ");
                _token = Console.ReadLine();
                Console.WriteLine("GitHub repo owner: ");
                _owner = Console.ReadLine();
                Console.WriteLine("GitHub repo name: ");
                _repo = Console.ReadLine();
                Console.WriteLine("Press [Y] if this information is correct:");
                Console.WriteLine($"{_token}, {_owner}/{_repo}");
                var key = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (key.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    inputIsCorrect = true;
                }
                else
                {
                    inputIsCorrect = false;
                }
            } while (!inputIsCorrect);
        }

        private static void GetSaveLocation()
        {
            bool inputIsCorrect;
            do
            {
                var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Console.WriteLine("Target directory: ");
                var fileName = Console.ReadLine();
                Console.WriteLine("Press [Y] if this target path is correct:");
                _targetPath = Path.Combine(docsPath, fileName);
                Console.WriteLine($"{_targetPath}");
                var key = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (key.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    inputIsCorrect = true;
                }
                else
                {
                    inputIsCorrect = false;
                }
            } while (!inputIsCorrect);
        }

        private static void Connect()
        {
            var credentials = new Credentials(_token);
            var connection = new Connection(new Octokit.ProductHeaderValue("GitTimeline"))
            {
                Credentials = credentials,
            };
            _gitHub = new GitHubClient(connection);
        }

        private async static Task DownloadData()
        {
            bool downloadComplete = false;
            do
            {
                Console.WriteLine("Press a key to download [P]ulls, [I]ssues, [C]ommits or [Q]uit.");
                var key = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (key.Equals("Q", StringComparison.InvariantCultureIgnoreCase))
                {
                    downloadComplete = true;
                }
                else if (key.Equals("P", StringComparison.InvariantCultureIgnoreCase))
                {
                    await downloadPulls();
                }
                else if (key.Equals("I", StringComparison.InvariantCultureIgnoreCase))
                {
                    await downloadIssues();
                }
                else if (key.Equals("C", StringComparison.InvariantCultureIgnoreCase))
                {
                    await downloadCommits();
                }
            } while (!downloadComplete);
        }

        private async static Task downloadPulls()
        {
            var pulls = await _gitHub.PullRequest.GetAllForRepository(_owner, _repo);
            Console.WriteLine("Pulls:");
            foreach (var pull in pulls)
            {
                Console.WriteLine($"{pull.CreatedAt} - {pull.Title}");
            }
            var json = JsonConvert.SerializeObject(pulls);
            var outputPath = Path.Combine(_targetPath, "pulls.json");
            File.WriteAllText(outputPath, json);
        }

        private async static Task downloadIssues()
        {
            var issues = await _gitHub.Issue.GetAllForRepository(_owner, _repo);
            Console.WriteLine("Issues:");
            foreach (var issue in issues)
            {
                Console.WriteLine($"{issue.CreatedAt} - {issue.Title}");
            }
            var json = JsonConvert.SerializeObject(issues);
            var outputPath = Path.Combine(_targetPath, "issues.json");
            File.WriteAllText(outputPath, json);
        }

        private async static Task downloadCommits()
        {

        }

    }
}

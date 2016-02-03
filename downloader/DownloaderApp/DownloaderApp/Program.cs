using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace DownloaderApp
{
    class Program
    {
        private static string _token;
        private static string _repo;
        private static string _targetPath;
        private static GitHubClient _gitHub;
        private static string _owner;
        private static Regex _issueRegex = new Regex("#(\\d+)", RegexOptions.Compiled);
        
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
                    Directory.CreateDirectory(_targetPath);
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
                    try
                    {
                        await downloadPulls();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                else if (key.Equals("I", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    { 
                        await downloadIssues();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            } while (!downloadComplete);
        }

        private async static Task downloadPulls()
        {
            var prRequest = new PullRequestRequest
            {
                State = ItemState.All,
                SortDirection = SortDirection.Ascending,
                SortProperty = PullRequestSort.Created,
            };
            var pulls = await _gitHub.PullRequest.GetAllForRepository(_owner, _repo, prRequest);
            Console.WriteLine("Pulls:");
            List<ExpandoObject> myPullData = new List<ExpandoObject>();
            foreach (var pull in pulls)
            {
                dynamic pullData = new ExpandoObject();
                pullData.htmlUrl = pull.HtmlUrl;
                pullData.number = pull.Number;
                pullData.createdAt = pull.CreatedAt;
                pullData.closedAt = pull.ClosedAt;
                pullData.submitter = pull.User.Login;
                pullData.sha = pull.Head.Sha;
                pullData.branch = pull.Head.Label;
                Console.WriteLine(pullData);
                var prCommits = await _gitHub.PullRequest.Commits(_owner, _repo, pull.Number);
                List<ExpandoObject> myCommitData = new List<ExpandoObject>();
                foreach (var prCommit in prCommits)
                {
                    dynamic commitData = new ExpandoObject();
                    var commit = prCommit.Commit;
                    commitData.sha = prCommit.Sha;
                    commitData.message = commit.Message;
                    commitData.createdAt = commit.Committer.Date;
                    commitData.committer = commit.Committer.Name;
                    myCommitData.Add(commitData);
                }
                pullData.commits = myCommitData;
                myPullData.Add(pullData);
            }
            var json = JsonConvert.SerializeObject(myPullData);
            var outputPath = Path.Combine(_targetPath, "pulls.json");
            File.WriteAllText(outputPath, json);
        }

        private async static Task downloadIssues()
        {
            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemState.All,
                SortDirection = SortDirection.Ascending,
                SortProperty = IssueSort.Created,
            };
            var issues = await _gitHub.Issue.GetAllForRepository(_owner, _repo, issueRequest);
            List<ExpandoObject> myIssueData = new List<ExpandoObject>();
            Console.WriteLine("Issues:");
            foreach (var issue in issues)
            {
                dynamic issueData = new ExpandoObject();
                issueData.htmlUrl = issue.HtmlUrl;
                issueData.number = issue.Number;
                issueData.createdAt = issue.CreatedAt;
                issueData.title = issue.Title;
                issueData.submitter = issue.User.Login;
                Console.WriteLine(issueData);
                var relatedIssues = new List<string>();
                var issueDiscussion = await _gitHub.Issue.Comment.GetAllForIssue(_owner, _repo, issue.Number);
                foreach (var discussion in issueDiscussion)
                {
                    var matchingIssues = _issueRegex.Match(discussion.Body);
                    if (matchingIssues?.Groups?.Count >= 2)
                    {
                        foreach (var issueNumber in matchingIssues.Groups[1].Captures)
                        {
                            relatedIssues.Add(issueNumber.ToString());
                        }
                    }
                }
                issueData.relatedIssues = relatedIssues;
                myIssueData.Add(issueData);
            }
            var json = JsonConvert.SerializeObject(myIssueData);
            var outputPath = Path.Combine(_targetPath, "issues.json");
            File.WriteAllText(outputPath, json);
        }

    }
}

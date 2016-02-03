using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetPath = GetSaveLocation();
            var json = Process(targetPath);
            SaveProcessedData(targetPath, json);
        }

        private static string GetSaveLocation()
        {
            bool inputIsCorrect;
            string targetPath;

            do
            {
                var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Console.WriteLine("Target directory: ");
                var fileName = Console.ReadLine();
                Console.WriteLine("Press [Y] if this target path is correct:");
                targetPath = Path.Combine(docsPath, fileName);
                Console.WriteLine($"{targetPath}");
                var key = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine();
                if (key.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!Directory.Exists(targetPath))
                    {
                        Console.WriteLine("This directory doesn't exist.");
                        inputIsCorrect = false;
                    }
                    else
                    {
                        inputIsCorrect = true;
                    }
                }
                else
                {
                    inputIsCorrect = false;
                }
            } while (!inputIsCorrect);
            return targetPath;
        }

        private static string Process(string targetPath)
        {
            var pullPath = Path.Combine(targetPath, "pulls.json");
            var rawPulls = File.ReadAllText(pullPath);
            var issuePath = Path.Combine(targetPath, "issues.json");
            var rawIssues = File.ReadAllText(issuePath);

            var pulls = JsonConvert.DeserializeObject(rawPulls) as JArray;
            var issues = JsonConvert.DeserializeObject(rawPulls) as JArray;

            var dataItems = new List<ExpandoObject>();

            foreach (var pull in pulls.Take(10))
            {
                dynamic pullItem = new ExpandoObject();
                var startDate = pull.Value<DateTime>("createdAt");
                var endDate = pull.Value<DateTime>("closedAt");
                pullItem.start = $"new Date({startDate.Year},{startDate.Month},{startDate.Day})";
                pullItem.end = $"new Date({startDate.Year},{startDate.Month},{startDate.Day})";
                pullItem.group = pull.Value<string>("number");
                pullItem.content = $"<a href=\"{pull.Value<string>("htmlUrl")}\">#{pull.Value<string>("number")}</a>";
                pullItem.className = pull.Value<string>("submitter");
                pullItem.type = "range";
                dataItems.Add(pullItem);

                foreach (var commit in pull["commits"] as JArray)
                {
                    dynamic commitItem = new ExpandoObject();
                    var commitDate = pull.Value<DateTime>("createdAt");
                    commitItem.start = $"new Date({commitDate})";
                    commitItem.className = commit.Value<string>("committer");
                    commitItem.content = commit.Value<string>("message");
                    commitItem.group = pull.Value<string>("number");
                    commitItem.type = "point";
                    dataItems.Add(commitItem);
                }
            }

            return JsonConvert.SerializeObject(dataItems);
        }

        private static void SaveProcessedData(string targetPath, string json)
        {
            var filePath = Path.Combine(targetPath, "graphData.json");
            File.WriteAllText(filePath, json);
        }
    }
}

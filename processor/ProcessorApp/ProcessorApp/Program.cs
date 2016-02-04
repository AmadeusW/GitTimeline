using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace ProcessorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetPath = GetSaveLocation();
            var items = Process(targetPath);
            SaveProcessedData(targetPath, items);
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

        private static List<WorkItem> Process(string targetPath)
        {
            var pullPath = Path.Combine(targetPath, "pulls.json");
            var rawPulls = File.ReadAllText(pullPath);
            var issuePath = Path.Combine(targetPath, "issues.json");
            var rawIssues = File.ReadAllText(issuePath);

            var pulls = JsonConvert.DeserializeObject(rawPulls) as JArray;
            var issues = JsonConvert.DeserializeObject(rawIssues) as JArray;

            var allItems = new List<WorkItem>(issues.Count);

            MultiValueDictionary<int, int> issueRelations = new MultiValueDictionary<int, int>();

            foreach (var issue in issues)
            {
                var id = issue.Value<int>("number");
                var rawRelatedItems = issue.Value<JArray>("relatedIssues");
                var relatedItems = rawRelatedItems.Count == 0 ? new List<int>() : rawRelatedItems.Select(n => n.Value<int>()).ToList();
                foreach (var relatedItem in relatedItems)
                {
                    issueRelations.Add(id, relatedItem);
                    issueRelations.Add(relatedItem, id);
                }

                var newItem = new WorkItem()
                {
                    Id = id,
                    Url = issue.Value<string>("htmlUrl"),
                    Title = issue.Value<string>("title"),
                    Body = issue.Value<string>("body"),
                    Author = issue.Value<string>("submitter"),
                    CreationDate = issue.Value<DateTime>("createdAt"),
                };
                allItems.Add(newItem);
            }

            foreach (var item in allItems)
            {
                IReadOnlyCollection<int> knownRelations;
                if (!issueRelations.TryGetValue(item.Id, out knownRelations))
                    continue;
                item.RelatedItems = knownRelations;
            }


            foreach (var pull in pulls)
            {
                var id = pull.Value<int>("number");

                var earliestCommit = DateTime.MaxValue;
                var latestCommit = DateTime.MinValue;
                var commits = new List<Commit>();
                foreach (var commit in pull["commits"] as JArray)
                {
                    var commitDate = commit.Value<DateTime>("createdAt");

                    if (commitDate < earliestCommit)
                        earliestCommit = commitDate;
                    if (commitDate > latestCommit)
                        latestCommit = commitDate;

                    var commitItem = new Commit()
                    {
                        Date = commitDate,
                        Sha = commit.Value<string>("sha"),
                        Message = commit.Value<string>("message"),
                        Author = commit.Value<string>("committer"),
                    };
                    commits.Add(commitItem);
                }

                var closedAtString = pull.Value<string>("closedAt");
                DateTime closedAt;
                if (!(DateTime.TryParse(closedAtString, out closedAt)))
                {
                    closedAt = default(DateTime);
                }

                var originalItem = allItems.Single(n => n.Id == id);
                var pullRequest = new PullRequest(originalItem, commits)
                {
                    FirstCommit = earliestCommit,
                    LastCommit = latestCommit,
                    CloseDate = closedAt,
                    Sha = pull.Value<string>("sha"),
                    Branch = pull.Value<string>("branch"),
                };
                allItems[allItems.IndexOf(originalItem)] = pullRequest;
            }
            return allItems;
        }

        private static void SaveProcessedData(string targetPath, List<WorkItem> items)
        {
            var filePath = Path.Combine(targetPath, "data.html");
            var calendar = DateTimeFormatInfo.CurrentInfo.Calendar;
            
            using (var stringWriter = new StringWriter())
            using (var writer = new HtmlTextWriter(stringWriter))
            {
                stringWriter.WriteLine($@"
<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">

  <title>{targetPath}</title>

  <link rel=""stylesheet"" href=""styles.css"">
  </head>
  <body>
  ");
                /*
  <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css"" integrity=""sha384-1q8mTJOASx8j1Au+a5WDVnPi2lkFfwwEAa8hDDdjZlpLegxhjVME1fgjWPGmkzs7"" crossorigin=""anonymous"">
  <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap-theme.min.css"" integrity = ""sha384-fLW2N01lMqjakBkx3l/M9EahuwpSfeNvV63J5ezn3uZzapT0u7EYsXMjQV+0En5r"" crossorigin = ""anonymous"" >
  <script src=""https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/js/bootstrap.min.js"" integrity = ""sha384-0mSbJDEHialfmuBBQP6A4Qrprq5OVfW37PRR3j5ELqxss1yVqOtnepnHVP9aJ7xS"" crossorigin = ""anonymous"" ></ script >
    */
                writer.Indent++;

                foreach (var grouping in items.Where(i => i.CreationDate.Year == 2015).GroupBy(i => calendar.GetWeekOfYear(i.CreationDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday)))
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.H1);
                    writer.Write($"Week {grouping.Key}");
                    writer.RenderEndTag(); // h2

                    foreach (var item in grouping)
                    {
                        var pr = item as PullRequest;

                        writer.AddAttribute(HtmlTextWriterAttribute.Class, item.Author);
                        writer.AddAttribute(HtmlTextWriterAttribute.Id, item.Id.ToString());
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.Indent++;

                        writer.RenderBeginTag("p");
                        writer.Indent++;

                        writer.RenderBeginTag(HtmlTextWriterTag.H3);
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, item.Url);
                        writer.RenderBeginTag(HtmlTextWriterTag.A);
                        writer.Write((pr != null) ? "Pull request " : "Issue ");
                        writer.Write("#" + item.Id);
                        writer.RenderEndTag(); //a
                        writer.RenderEndTag(); //h3

                        writer.RenderBeginTag(HtmlTextWriterTag.H2);
                        writer.Write(item.Title);
                        writer.RenderEndTag(); // h2

                        writer.RenderEndTag(); // p
                        writer.Indent--;

                        if (pr != null)
                        {
                            writer.RenderBeginTag(HtmlTextWriterTag.P);
                            writer.Indent++;

                            if (pr.CloseDate == default(DateTime))
                            {
                                writer.Write($"Opened at <strong>{pr.CreationDate.ToString("dddd MMMM d, HH:mm")}, never closed</strong>. ");
                            }
                            else
                            {
                                writer.Write($"Opened at <strong>{pr.CreationDate.ToString("dddd MMMM d, HH:mm")}</strong>, closed at <strong>{pr.CloseDate.ToString("dddd MMMM d, HH:mm")}</strong>. ");
                                writer.RenderBeginTag(HtmlTextWriterTag.Strong);
                                writer.WriteBreak();
                                writer.Write($"Under review for {(pr.CloseDate - pr.CreationDate).TotalHours.ToString("F")} hours");
                                writer.RenderEndTag();
                            }
                            writer.RenderEndTag(); // p
                            writer.Indent--;

                            writer.RenderBeginTag(HtmlTextWriterTag.P);
                            writer.Indent++;

                            writer.Write($"First commit on <strong>{pr.FirstCommit.ToString("dddd MMMM d, HH:mm")}</strong>, last at <strong>{pr.LastCommit.ToString("dddd MMMM d, HH:mm")}</strong>.");
                            writer.WriteBreak();
                            writer.Write("Worked on for ");
                            writer.RenderBeginTag(HtmlTextWriterTag.Strong);
                            writer.Write($"{(pr.LastCommit - pr.FirstCommit).TotalHours.ToString("F")} hours");
                            writer.RenderEndTag();

                            writer.RenderEndTag(); // p
                            writer.Indent--;

                            writer.RenderBeginTag(HtmlTextWriterTag.P);
                            writer.Indent++;

                            foreach (var commit in pr.Commits)
                            {
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, commit.Author);
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.WriteLine($"{commit.Author} on <strong>{commit.Date.ToString("dddd MMMM d, HH:mm")}</strong>: {commit.Message}");
                                writer.WriteBreak();
                                writer.RenderEndTag();//span
                            }

                            writer.RenderEndTag(); // p
                            writer.Indent--;
                        }
                        else
                        {
                            writer.RenderBeginTag(HtmlTextWriterTag.P);
                            writer.Indent++;

                            writer.Write($"Opened at {item.CreationDate}");

                            writer.RenderEndTag(); // p
                            writer.Indent--;
                        }

                        if (item.RelatedItems.Any())
                        {
                            writer.RenderBeginTag(HtmlTextWriterTag.H4);
                            writer.Write("Related items:");
                            writer.RenderEndTag(); // h4

                            writer.RenderBeginTag(HtmlTextWriterTag.P);
                            writer.Indent++;

                            foreach (var related in item.RelatedItems)
                            {
                                writer.AddAttribute(HtmlTextWriterAttribute.Href, "#" + related.ToString());
                                writer.RenderBeginTag(HtmlTextWriterTag.A);
                                writer.Write("#" + related.ToString() + " ");
                                writer.RenderEndTag(); // a
                            }

                            writer.RenderEndTag(); // p
                            writer.Indent--;
                        }

                        writer.RenderEndTag(); // div
                        writer.Indent--;
                    }
                }

                writer.Indent--;
                stringWriter.WriteLine(@"
  </body ></html>
  ");

                File.WriteAllText(filePath, stringWriter.ToString());
            }
        }
    }
}

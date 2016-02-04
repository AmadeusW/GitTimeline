using System;
using System.Collections.Generic;

namespace ProcessorApp
{
    internal class WorkItem
    {
        public int Id {get;set;}
        public string Url { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Author { get; set; }
        public DateTime CreationDate { get; set; }
        public IEnumerable<int> RelatedItems { get; set; }

        public WorkItem()
        {
            RelatedItems = new List<int>();
        }
    }

    internal class PullRequest : WorkItem
    {
        public DateTime FirstCommit { get; set; }
        public DateTime LastCommit { get; set; }
        public DateTime CloseDate { get; set; }
        public string Sha { get; set; }
        public string Branch { get; set; }
        public List<Commit> Commits { get; set; }

        public PullRequest(WorkItem fromItem, List<Commit> commits) : base()
        {
            this.Id = fromItem.Id;
            this.Url = fromItem.Url;
            this.Title = fromItem.Title;
            this.Author = fromItem.Author;
            this.CreationDate = fromItem.CreationDate;
            this.RelatedItems = fromItem.RelatedItems;
            this.Commits = commits;
        }
    }

    internal class Commit
    {
        public DateTime Date { get; set; }
        public string Sha { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
    }
}
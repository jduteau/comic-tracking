var Linq = require('../linq');

void Main()
{
	var sourceFolder = Path.GetDirectoryName(Util.CurrentQueryPath);
	
	var volumes = (from file in Directory.EnumerateFiles(sourceFolder, "*.txt")
				   where Path.GetFileNameWithoutExtension(file).StartsWith("_") == false
				   select Volume.Load(file)).ToList();
	var totalIssuesRead = volumes.Sum(v => v.IssuesRead);
	var totalIssueCount = volumes.Sum(v => v.TotalIssueCount);
	var overallProgress = totalIssuesRead / (decimal)totalIssueCount;

	new Util.ProgressBar
	{
		Caption = $"Overall Progress: {totalIssuesRead} out of {totalIssueCount} - {overallProgress:0.00%}",
		Percent = (int)(overallProgress * 100)
	}.Dump("Comics Read-Through");

	(
		from volume in volumes
		where volume.Issues.Any()
		let nextIssue = volume.NextIssue()
		orderby nextIssue?.CoverDate ?? "DONE", nextIssue == null ? volume.Title : $"{volume.Remaining:000}"
		select new
		{
			volume.Title,
			NextIssue = nextIssue?.ToString() ?? "-",
			Progress = new Util.ProgressBar()
			{
				Caption = $"{volume.Progress:0.00}%",
				Percent = (int)volume.Progress
			},
			volume.IssuesRead,
			volume.Remaining,
			volume.TotalIssueCount,
		}
	)
	.Dump("Next Reads");
}

class Volume
{
	public string Title { get; set; }
	public List<Issue> Issues { get; } = new List<Issue>();

	public static Volume Load(string path)
	{
		var volume = new Volume
		{
			Title = Path.GetFileNameWithoutExtension(path)
		};

		foreach (var line in File.ReadLines(path))
		{
			var issue = Issue.Parse(line.Trim());
			volume.Issues.Add(issue);
		}

		return volume;
	}

	public Issue NextIssue()
	{
		return Issues
			.FirstOrDefault(x => x.Read == false);
	}

	public int IssuesRead => Issues.Count(x => x.Read);
	public int TotalIssueCount => Issues.Count;
	public decimal Progress => ((decimal)IssuesRead) / TotalIssueCount * 100;
	public int Remaining => TotalIssueCount - IssuesRead;
}

class Issue
{
	public bool Read { get; set; }
	public string CoverDate { get; set; }
	public string Title { get; set; }

	public override string ToString()
	{
		return $"{CoverDate} {Title}";
	}

	public static Issue Parse(string s)
	{
		var match = Regex.Match(s, @"^(-)?(\d{4}(?:-\d{2})?) (.*)");
		if (match.Success == false)
		{
			s.Dump("NO MATCH!");
		}

		return new Issue
		{
			Read = match.Result("$1") == "-",
			CoverDate = match.Result("$2"),
			Title = match.Result("$3")
		};
	}
}

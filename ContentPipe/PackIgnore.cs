using System.Text.RegularExpressions;

namespace ContentPipe;

internal class PackIgnore
{
	private readonly List<string[]> matches = new List<string[]>();

	public readonly string Path;
    
	public PackIgnore(string data, string path)
	{
		Path = path;
		string[] lines = data.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		
		foreach (var line in lines)
		{
			if(line.StartsWith("#"))
				continue;
			matches.Add(line.Split("/"));
		}
	}

	public bool Disallows(string file)
	{
		file = file[(Path.Length + 1)..];
		string[] parts = file.Split("/");
		if (parts.Length < 1)
			return true;
		foreach (var match in matches)
		{
			// Matching logic.
			
			// If it's a single statement we can assume it to be functional.
			if (match.Length == 1 && parts[0] == match[0])
				return true;

			if (match.Length == 1 && match[0].StartsWith("*"))
			{
				if (parts[^1].EndsWith(match[0][1..]))
					return true;
			}
		}
		return false;
	}
}

using System.Text;

namespace ContentPipe;

/// <summary>
/// Utility class to build paths to content
/// </summary>
public readonly struct ContentPath
{
	public readonly string[] Parts;
	public readonly string? DirectoryIdentifier;

	public ContentPath(params string[] parts)
	{
		List<string> partsList = new List<string>();
		for (int i = 0; i < parts.Length; i++)
		{
			// The first component may be a "Directory Identifier", prefixed with an ampersand.
			// This can be used by the loader to find the appropriate content directory quickly.
			if (i == 0 && parts[i].StartsWith("$"))
				DirectoryIdentifier = parts[i][1..];
			else
				partsList.Add(parts[i]);
		}
		Parts = partsList.ToArray();
	}
	
	public ContentPath(string formatted) : this(formatted.Split("/", StringSplitOptions.RemoveEmptyEntries))
	{
		
	}

	public ContentPath()
	{
		Parts = Array.Empty<string>();
		DirectoryIdentifier = null;
	}

	private ContentPath(string[] parts, string? directoryIdentifier)
	{
		Parts = parts;
		DirectoryIdentifier = directoryIdentifier;
	}

	public override string ToString()
	{
		// It's basically just a regular old path.
		StringBuilder stringBuilder = new StringBuilder();
		if (DirectoryIdentifier != null)
			stringBuilder.Append("$").Append(DirectoryIdentifier);
		foreach (var part in Parts)
		{
			// We don't want to start paths with "/". It's okay if the user does but formatted ones shouldn't
			if (stringBuilder.Length != 0)
				stringBuilder.Append("/");
			stringBuilder.Append(part);
		}
		return stringBuilder.ToString();
	}

	public ContentPath Append(string part)
	{
		return new ContentPath(Parts.Concat(part.Split("/", StringSplitOptions.TrimEntries)).ToArray(), DirectoryIdentifier);
	}

	public ContentPath NoDirectoryIdentifier()
	{	
		return new ContentPath(Parts.ToArray(), null);
	}

	public ContentPath MoveUp()
	{
		return new ContentPath(Parts[..^1], DirectoryIdentifier);
	}
	
	public static implicit operator string(ContentPath path)
	{
		return path.ToString();
	}

	public static implicit operator ContentPath(string path)
	{
		return new ContentPath(path);
	}
}

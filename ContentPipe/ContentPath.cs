using System.Text;

namespace ContentPipe;

/// <summary>
/// Utility class to build paths to content
/// </summary>
public readonly struct ContentPath
{
	public readonly string[] Parts;
	public readonly string? MountPoint;
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
			// It can also be a mount point, specified with the "@" symbol.
			else if (i <= 1 && parts[i].StartsWith("@"))
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

	private ContentPath(string[] parts, string? directoryIdentifier, string? mountPoint)
	{
		Parts = parts;
		DirectoryIdentifier = directoryIdentifier;
		MountPoint = mountPoint;
	}

	public override string ToString()
	{
		// It's basically just a regular old path.;
		List<string> pathParts = new List<string>();
		if (DirectoryIdentifier != null)
			pathParts.Add("$" + DirectoryIdentifier);
		if (MountPoint != null && !String.IsNullOrEmpty(MountPoint))
			pathParts.Add("@" + DirectoryIdentifier);
		pathParts.AddRange(Parts);
		return string.Join("/", pathParts);
	}

	public ContentPath Append(string part)
	{
		return new ContentPath(Parts.Concat(part.Split("/", StringSplitOptions.TrimEntries)).ToArray(), DirectoryIdentifier, MountPoint);
	}

	public ContentPath NoDirectoryIdentifier()
	{	
		return new ContentPath(Parts.ToArray(), null, MountPoint);
	}

	public ContentPath MoveUp()
	{
		return new ContentPath(Parts[..^1], DirectoryIdentifier, MountPoint);
	}
	
	public static implicit operator string(ContentPath path)
	{
		return path.ToString();
	}

	public static implicit operator ContentPath(string path)
	{
		return new ContentPath(path);
	}
	public ContentPath NoMount()
	{
		return new ContentPath(Parts, DirectoryIdentifier, null);
	}

	public ContentPath RemoveMount(ContentMount directory)
	{
		if (directory.MountPoint == null)
			return NoMount();

		if (MountPoint != null)
			return NoMount();

		if (Parts.Length < 2)
			return this;

		if (Parts[0] == directory.MountPoint)
		{
			return new ContentPath(Parts[1..], DirectoryIdentifier, null);
		}
		
		return this;
	}

	public ContentPath AddMount(string mount)
	{
		return new ContentPath(Parts, DirectoryIdentifier, mount);
	}
	
	public bool IsExtension(string ext)
	{
		return Parts.LastOrDefault()?.EndsWith(ext) ?? false;
	}
	
	public string GetExtension()
	{
		string? last = Parts.LastOrDefault();
		return last == null ? "" : Path.GetExtension(last);
	}

	private ContentPath Clone()
	{
		return new ContentPath(Parts.ToArray(), DirectoryIdentifier, MountPoint);
	}

	public ContentPath SetExtension(string ext)
	{
		if (Parts.Length == 0)
			return Clone();
		ContentPath path = Clone();

		string last = path.Parts[^1];
		last = Path.GetFileNameWithoutExtension(last) + ext;
		path.Parts[^1] = last;

		return path;
	}
}

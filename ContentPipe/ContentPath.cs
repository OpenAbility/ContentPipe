using Microsoft.VisualBasic.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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
	public readonly bool Rooted;

	public ContentPath(params string[] parts) : this(parts, false)
	{
	}
	
	public ContentPath(string formatted) : this(formatted.Split("/"), formatted.StartsWith("/"))
	{
		
	}
	
	private ContentPath(string[] parts, bool knownRooted)
	{
		List<string> partsList = new List<string>();
		Rooted = knownRooted;
		for (int i = 0; i < parts.Length; i++)
		{
			// The first component may be a "Directory Identifier", prefixed with an ampersand.
			// This can be used by the loader to find the appropriate content directory quickly.
			if (i == 0 && parts[i].StartsWith("$"))
				DirectoryIdentifier = parts[i][1..];
			else if (i == 0 && parts[i] == "")
			{
				Rooted = true;
			}
			// It can also be a mount point, specified with the "@" symbol.
			else if (i <= 1 && parts[i].StartsWith("@"))
				DirectoryIdentifier = parts[i][1..];
			else
				partsList.Add(parts[i]);
			
		}
		Parts = partsList.ToArray();
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

	[Pure]
	public override string ToString()
	{
		// It's basically just a regular old path.
		List<string> pathParts = new List<string>();
		if (DirectoryIdentifier != null)
			pathParts.Add("$" + DirectoryIdentifier);
		if (MountPoint != null && !String.IsNullOrEmpty(MountPoint))
			pathParts.Add("@" + DirectoryIdentifier);
		pathParts.AddRange(Parts);
		return (Rooted ? "/" : "") + String.Join("/", pathParts);
	}

	[Pure]
	public ContentPath Append(string part)
	{
		return new ContentPath(Parts.Concat(part.Split("/", StringSplitOptions.TrimEntries)).ToArray(), DirectoryIdentifier, MountPoint);
	}

	[Pure]
	public ContentPath NoDirectoryIdentifier()
	{	
		return new ContentPath(Parts.ToArray(), null, MountPoint);
	}

	[Pure]
	public ContentPath MoveUp()
	{
		return new ContentPath(Parts[..^1], DirectoryIdentifier, MountPoint);
	}
	
	[Pure]
	public ContentPath MoveIn()
	{
		return new ContentPath(Parts[1..], DirectoryIdentifier, MountPoint);
	}
	
	[Pure]
	public static implicit operator string(ContentPath path)
	{
		return path.ToString();
	}

	[Pure]
	public static implicit operator ContentPath(string path)
	{
		return new ContentPath(path);
	}
	
	[Pure]
	public static ContentPath operator +(ContentPath first, ContentPath second)
	{
		return first.Join(second);
	}
	
	[Pure]
	public ContentPath NoMount()
	{
		return new ContentPath(Parts, DirectoryIdentifier, null);
	}

	[Pure]
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
	

	[Pure]
	public ContentPath AddMount(string mount)
	{
		return new ContentPath(Parts, DirectoryIdentifier, mount);
	}
	
	[Pure]
	public bool IsExtension(string ext)
	{
		return Parts.LastOrDefault()?.EndsWith(ext) ?? false;
	}
	
	[Pure]
	public string GetExtension()
	{
		string? last = Parts.LastOrDefault();
		return last == null ? "" : Path.GetExtension(last);
	}

	[Pure]
	private ContentPath Clone()
	{
		return new ContentPath(Parts.ToArray(), DirectoryIdentifier, MountPoint);
	}

	[Pure]
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

	/// <summary>
	/// Appends another path onto this path
	/// </summary>
	/// <param name="path">The other path</param>
	/// <returns>The joined paths</returns>
	[Pure]
	public ContentPath Join(ContentPath path)
	{
		if (path.MountPoint != MountPoint)
			throw new Exception("Incompatible mount points found!");
		if (path.DirectoryIdentifier != DirectoryIdentifier)
			throw new Exception("Incompatible mount points found!");
		if (path.Rooted)
			return path;
		return new ContentPath(Parts.Concat(path.Parts).ToArray(), DirectoryIdentifier, MountPoint);
	}

	/// <summary>
	/// Simplifies and resolved the path
	/// </summary>
	/// <returns>The attempted and simplified path</returns>
	[Pure]
	public ContentPath Resolve()
	{
		Stack<string> partsStack = new Stack<string>();
		for (int i = 0; i < Parts.Length; i++)
		{
			// The "." sign basically means "do nothing"
			if (Parts[i] == ".")
				continue;
			// Step up
			else if (Parts[i] == "..")
			{
				if (partsStack.Count < 1)
					throw new Exception("Cannot step up beyond root directory!");
				partsStack.Pop();
			}
			else
			{
				partsStack.Push(Parts[i]);
			}
		}

		return new ContentPath(partsStack.Reverse().ToArray(), DirectoryIdentifier, MountPoint);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		ContentPath other;
		if (obj is string s)
			other = s;
		else if (obj is ContentPath path)
			other = path;
		else
			return false;

		other = other.Resolve();
		ContentPath local = Resolve();

		if (other.MountPoint != local.MountPoint)
			return false;
		if (other.DirectoryIdentifier != local.DirectoryIdentifier)
			return false;
		if (other.Parts.Length != local.Parts.Length)
			return false;

		return !local.Parts.Where((t, i) => other.Parts[i] != t).Any();
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}

	public static bool operator ==(ContentPath a, ContentPath b)
	{
		return a.Equals(b);
	}
	
	public static bool operator !=(ContentPath a, ContentPath b)
	{
		return !(a == b);
	}
}

using System.IO.Compression;

namespace ContentPipe;

/// <summary>
/// A packed content directory(a .cpkg file), used by the Content class.
/// </summary>
public struct ContentDirectory
{

	private ZipArchive archive;
	
	/// <summary>
	/// Fetch a content lump, returns null if it is not available
	/// </summary>
	/// <param name="name">The path to the resource</param>
	public ContentLump? this[string name]
	{
		get
		{
			ZipArchiveEntry? entry = archive.GetEntry(name);
			if (entry == null)
				return null;
			
			ContentLump contentLump = new()
			{
				Name = name,
				Stream = new ContentStream(entry.Length, entry.Open())
			};

			return contentLump;
		}
	}
	

	/// <summary>
	/// Compress a directory into a .cpkg file. It will be stored in the same directory as the source directory,
	/// if path is "Content/TextData", it will output a file called "Content/TextData.cpkg". This file can then be loaded in the Content class with
	/// <code>Content.LoadDirectory("Content/TextData");</code>
	/// </summary>
	/// <param name="path">The path to the directory to compress</param>
	public static void CompressDirectory(string path)
	{
		if(File.Exists(path + ".cpkg"))
			File.Delete(path + ".cpkg");

		FileStream fileStream = File.OpenWrite(path + ".cpkg");
		ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
		
		PushFiles(path, path, ref archive, new Stack<PackIgnore>());

		archive.Comment = "Built automatically by CompressDirectory";
		archive.Dispose();
		fileStream.Close();
	}

	private static void PushFiles(string root, string path, ref ZipArchive archive, Stack<PackIgnore> ignores)
	{
		string[] files = Directory.GetFiles(path);

		// packignores are wonderful files.
		string ignorePath = Path.Combine(path, ".packignore");
		if (File.Exists(ignorePath))
		{
			ignores.Push(new PackIgnore(File.ReadAllText(ignorePath), path));
		}

		foreach (var file in files)
		{
			if(ignores.Any(i => i.Disallows(file)))
				continue;
			archive.CreateEntryFromFile(file, Path.GetRelativePath(root, file));
		}

		string[] dirs = Directory.GetDirectories(path);
		foreach (var directory in dirs)
		{
			PushFiles(root, directory, ref archive, ignores);
		}
	}

	/// <summary>
	/// Load a packed content directory(a .cpkg file) from a path
	/// </summary>
	/// <param name="path">The path to the file, excluding the extension</param>
	public ContentDirectory(string path)
	{
		string fileName = path + ".cpkg";
		archive = ZipFile.OpenRead(fileName);
		Content = new string[archive.Entries.Count];
		for (int i = 0; i < archive.Entries.Count; i++)
		{
			Content[i] = archive.Entries[i].FullName;
		}
	}

	public readonly string[] Content;
}

/// <summary>
/// A content lump, or in other words, a named byte array. Used to store data in a cpkg file.
/// </summary>
public struct ContentLump
{
	/// <summary>
	/// The name of the content lump
	/// </summary>
	public string Name;

	/// <summary>
	/// The data of the ContentLump
	/// </summary>
	public byte[]? Data;
	
	/// <summary>
	/// The stream to the data of the lump
	/// </summary>
	public Stream? Stream;

	/// <summary>
	/// Create a content lump with 0:ed fields.
	/// </summary>
	public ContentLump()
	{
		Name = "";
	}
}
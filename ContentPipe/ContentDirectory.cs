using MessagePack;
using System.IO.Compression;

namespace ContentPipe;

/// <summary>
/// A packed content directory(a .cpkg file), used by the Content class.
/// </summary>
public struct ContentDirectory
{
	private Dictionary<string, ContentLump> contentLumps = new Dictionary<string, ContentLump>();

	/// <summary>
	/// Fetch a content lump, returns null if it is not available
	/// </summary>
	/// <param name="name">The path to the resource</param>
	public ContentLump? this[string name]
	{
		get
		{
			if (contentLumps.ContainsKey(name))
				return contentLumps[name];
			return null;
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

		if (!Directory.Exists(path))
			return;

		string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
		string filePath = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileName(path)) + ".cpkg";

		List<ContentLump> data = new List<ContentLump>();

		foreach (var file in files)
		{
			string localPath = Path.GetRelativePath(path, file);
			ContentLump lump = new ContentLump();
			lump.Name = localPath;
			lump.Data = File.ReadAllBytes(file);
			data.Add(lump);
		}

		byte[] compressedData = MessagePackSerializer.Serialize(data.ToArray());

		FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate);
		DeflateStream gZipStream = new DeflateStream(fileStream, CompressionLevel.SmallestSize, false);
		gZipStream.Write(compressedData);
		gZipStream.Close();
		fileStream.Close();

	}

	/// <summary>
	/// Load a packed content directory(a .cpkg file) from a path
	/// </summary>
	/// <param name="path">The path to the file, excluding the extension</param>
	public ContentDirectory(string path)
	{

		FileStream fileStream = File.Open(path + ".cpkg", FileMode.Open);
		DeflateStream gZipStream = new DeflateStream(fileStream, CompressionMode.Decompress, false);

		MemoryStream memoryStream = new MemoryStream();
		gZipStream.CopyTo(memoryStream);
		gZipStream.Close();
		fileStream.Close();

		ContentLump[] data = MessagePackSerializer.Deserialize<ContentLump[]>(memoryStream.ToArray());

		foreach (var lump in data)
		{
			contentLumps.Add(lump.Name, lump);
		}

	}
}

/// <summary>
/// A content lump, or in other words, a named byte array. Used to store data in a cpkg file.
/// </summary>
[MessagePackObject]
public struct ContentLump
{
	/// <summary>
	/// The name of the content lump
	/// </summary>
	[Key(0)] public string Name;

	/// <summary>
	/// The data of the ContentLump
	/// </summary>
	[Key(1)] public byte[] Data = Array.Empty<byte>();

	/// <summary>
	/// Create a content lump with 0:ed fields.
	/// </summary>
	public ContentLump()
	{
		Name = "";
	}
}
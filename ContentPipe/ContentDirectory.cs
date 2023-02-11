using MessagePack;
using System.IO.Compression;

namespace ContentPipe;

public struct ContentDirectory
{
	private Dictionary<string, ContentLump> contentLumps = new Dictionary<string, ContentLump>();

	public ContentLump? this[string name]
	{
		get
		{
			if (contentLumps.ContainsKey(name))
				return contentLumps[name];
			return null;
		}
	}

	public static void CompressDirectory(string path)
	{
		
		if(!Directory.Exists(path))
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

[MessagePackObject]
public struct ContentLump
{
	[Key(0)]
	public string Name;
	
	[Key(1)]
	public byte[] Data = Array.Empty<byte>();


	public ContentLump()
	{
		Name = "";
	}
}
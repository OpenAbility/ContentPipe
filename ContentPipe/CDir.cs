using System.Text;

using FilePair = System.Collections.Generic.KeyValuePair<string, string>;

namespace ContentPipe;

public class CDIRFile
{
	private readonly Dictionary<uint, CDIRFileDefinition> fileDefinitions = new Dictionary<uint, CDIRFileDefinition>();
	public readonly string Path;
	private readonly string Origin;
	private readonly ulong[] segmentOffsets;

	public CDIRFile(string path) : this(File.OpenRead(path), path)
	{
		
	}

	public static uint Hash(string str)
	{
		const uint multiplier = 37;
		
		return str.Aggregate<char, uint>(0, (current, c) => multiplier * current + c);
	}
	
	public CDirReadHandle? ReadFile(uint hash)
	{
		if (!fileDefinitions.TryGetValue(hash, out CDIRFileDefinition file))
			return null;

		// Find the last file with an offset less than the file requested.
		// This segment will contain the file we want!
		int seg = -1;
		for (int i = 0; i < segmentOffsets.Length; i++)
		{
			if(segmentOffsets[i] > file.Offset)
				break;
			seg = i;
		}

		// We couldn't find our segment
		if (seg == -1)
			return null;
		
		// TODO: Research if File.OpenRead is slow, it could be, and we DON'T want that!!!!
		FileStream stream = File.OpenRead(Path + "_" + seg);

		ulong readOffset = file.Offset - segmentOffsets[seg] + 4;

		return new CDirReadHandle(readOffset, file.Size, stream);
	}

	public CDirReadHandle? ReadFile(string file)
	{
		return ReadFile(Hash(file));
	}

	public CDIRFile(Stream stream, string path)
	{
		BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, false);
		if (new string(reader.ReadChars(4)) != "CDIR")
			throw new InvalidFileException("Invalid file header!");

		Path = path;
		
		uint length = reader.ReadUInt32();
		for (int i = 0; i < length; i++)
		{
			uint hash = reader.ReadUInt32();
			ulong offset = reader.ReadUInt64();
			uint fileLength = reader.ReadUInt32();
			
			fileDefinitions.Add(hash, new CDIRFileDefinition()
			{
				Offset = offset,
				Size = fileLength
			});
		}
		
		length = reader.ReadUInt32();
		segmentOffsets = new ulong[length];
		for (int i = 0; i < length; i++)
		{
			segmentOffsets[i] = reader.ReadUInt64();
		}
	}
	
	public string[] GetContent()
	{
		CDirReadHandle? handle = ReadFile("__content_listing");
		if (handle == null)
			return Array.Empty<string>();

		return Encoding.Default.GetString(handle.Read()).Split("\n");
	}

	private static void PushFiles(string root, string path, ref List<FilePair> fileListing, Stack<PackIgnore> ignores)
	{
		string[] files = Directory.GetFiles(path);

		// packignores are wonderful files.
		string ignorePath = System.IO.Path.Combine(path, ".packignore");
		if (File.Exists(ignorePath))
		{
			ignores.Push(new PackIgnore(File.ReadAllText(ignorePath), path));
		}

		foreach (var file in files)
		{
			if(file.EndsWith(".packignore"))
				continue;
			if (ignores.Any(i => i.Disallows(file)))
				continue;
			fileListing.Add(new FilePair(file, file[(root.Length + 1)..]));
		}

		string[] dirs = Directory.GetDirectories(path);
		foreach (var directory in dirs)
		{
			PushFiles(root, directory, ref fileListing, ignores);
		}
		if (File.Exists(ignorePath))
		{
			ignores.Pop();
		}
	}

	public static void Pack(string input, string output, bool listing = true)
	{
		if (input == "")
			input = ".";
		// Files can be 1GB max.
		const ulong targetLength = 1024 * 1024 * 1024;
		
		List<FilePair> files = new ();
		PushFiles(input, input, ref files, new Stack<PackIgnore>());
		string temp = "__content_listing";
		if (listing)
		{
			File.WriteAllLines(temp, files.Select(f => f.Value));
			files.Add(new KeyValuePair<string, string>(temp, temp));
		}

		List<ulong> offsets = new List<ulong>();
		Dictionary<uint, CDIRFileDefinition> fileDefinitions = new Dictionary<uint, CDIRFileDefinition>();

		ulong offset = 0;
		ulong currentPartLength = 0;
		ulong currentPart = 0;
		offsets.Add(0);
		BinaryWriter currentPartWriter = new BinaryWriter(File.OpenWrite(output + "_0"), Encoding.ASCII, false);
		currentPartWriter.Write("CSEG".ToCharArray());
		foreach (FilePair file in files)
		{
			// Ugly hack
			uint hash = Hash(file.Value);
			byte[] data = File.ReadAllBytes(file.Key);

			fileDefinitions[hash] = new CDIRFileDefinition()
			{
				Offset = offset,
				Size = (uint)data.Length
			};
			currentPartWriter.Write(data);

			currentPartLength += (uint)data.Length;
			offset += (uint)data.Length;

			if (currentPartLength <= targetLength)
				continue;
			currentPart++;
			currentPartLength = 0;
			currentPartWriter.Flush();
			currentPartWriter.Close();
			currentPartWriter = new BinaryWriter(File.OpenWrite(output + "_" + currentPart), Encoding.ASCII, false);
			currentPartWriter.Write("CSEG".ToCharArray());
			offsets.Add(offset);
		}
		
		currentPartWriter.Flush();
		currentPartWriter.Close();

		if(listing)
			File.Delete(temp);
		
		BinaryWriter directoryWriter = new BinaryWriter(File.OpenWrite(output), Encoding.ASCII, false);
		directoryWriter.Write("CDIR".ToCharArray());
		directoryWriter.Write((uint)fileDefinitions.Count);

		foreach (var cdef in fileDefinitions)
		{
			directoryWriter.Write(cdef.Key);
			directoryWriter.Write(cdef.Value.Offset);
			directoryWriter.Write(cdef.Value.Size);
		}
		
		directoryWriter.Write(offsets.Count);
		foreach (var o in offsets)
		{
			directoryWriter.Write(o);
		}
		
		directoryWriter.Flush();
		directoryWriter.Close();
	}
}

public class CDirReadHandle : IDisposable
{
	public readonly Stream ReadStream;
	public readonly ulong Length;
	public readonly ulong ReadOffset;
	
	public CDirReadHandle(ulong readOffset, ulong length, Stream readStream)
	{
		ReadOffset = readOffset;
		Length = length;
		ReadStream = readStream;
	}

	public byte[] Read()
	{
		byte[] read = new byte[Length];
		ReadStream.Position = (long)ReadOffset;
		ReadStream.Read(read);
		return read;
	}


	public void Dispose()
	{
		ReadStream.Dispose();
	}
}

public struct CDIRFileDefinition
{
	public ulong Offset;
	public uint Size;
}
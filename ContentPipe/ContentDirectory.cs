using System.Security.Cryptography;
using System.Text;

namespace ContentPipe;

public class ContentDirectory
{
	private readonly DirectoryDefinition RootDirectory;
	public readonly string Path;
	private readonly string Origin;
	private ulong[] segmentOffsets;

	public ContentDirectory(string path) : this(File.OpenRead(path), path)
	{
		
	}

	public static uint Hash(string str)
	{
		if (str.StartsWith("%h%"))
		{
			return UInt32.Parse(str[3..]);
		}
		const uint multiplier = 37;
		
		return str.Aggregate<char, uint>(0, (current, c) => multiplier * current + c);
	}

	private FileDefinition? GetFile(ContentPath path, DirectoryDefinition current)
	{
		// It's the last part, or in other words, the file.
		if (path.Parts.Length == 1)
		{
			if (current.Files.TryGetValue(path.Parts[0], out FileDefinition definition))
				return definition;
			return null;
		}

		if (current.Directories.TryGetValue(path.Parts[0], out DirectoryDefinition directory))
			return GetFile(path.MoveIn(), directory);
		return null;
	}
	
	public CDirReadHandle? ReadFile(ContentPath path)
	{
		// If there are no parts, return null.
		if (path.Parts.Length == 0)
			return null;

		// If the root name ain't empty, and we ain't giving the proper
		// path to root.
		if (RootDirectory.Name != "" && RootDirectory.Name != path.Parts[0])
			return null;
		
		FileDefinition? file = null;
		if (RootDirectory.Name != "")
			file = GetFile(path.MoveIn(), RootDirectory);
		else
			file = GetFile(path, RootDirectory);


		if (file == null)
			return null;
		
		

		// Find the last file with an offset less than the file requested.
		// This segment will contain the file we want!
		int seg = -1;
		for (int i = 0; i < segmentOffsets.Length; i++)
		{
			if(segmentOffsets[i] > file!.Value.Offset)
				break;
			seg = i;
		}

		// We couldn't find our segment
		if (seg == -1)
			return null;
		
		// TODO: Research if File.OpenRead is slow, it could be, and we DON'T want that!!!!
		FileStream stream = File.OpenRead(Path + "_" + seg);

		ulong readOffset = file.Value.Offset - segmentOffsets[seg] + 4;

		return new CDirReadHandle(readOffset, file.Value.Size, stream, path, file.Value.Checksum);
	}

	public ContentDirectory(Stream stream, string path)
	{
		BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, false);
		if (new string(reader.ReadChars(4)) != "CDR2")
			throw new InvalidFileException("Invalid file header!");

		Path = path;
		
		uint version = reader.ReadUInt32();
		if(version != 2)
			throw new InvalidFileException("Unsupported version " + version);

		RootDirectory = LoadDirectory(reader);
			
		uint segments = reader.ReadUInt32();
		segmentOffsets = new ulong[segments];
		for (uint i = 0; i < segments; i++)
		{
			segmentOffsets[i] = reader.ReadUInt64();
		}
	}

	private static DirectoryDefinition LoadDirectory(BinaryReader reader)
	{
		DirectoryDefinition directoryDefinition = new DirectoryDefinition();
		directoryDefinition.Name = reader.ReadTerminatedString();
		
		uint subdirectories = reader.ReadUInt32();
		for (uint i = 0; i < subdirectories; i++)
		{
			DirectoryDefinition subdirectory = LoadDirectory(reader);
			directoryDefinition.Directories[subdirectory.Name] = subdirectory;
		}
		
		uint files = reader.ReadUInt32();
		for (uint i = 0; i < files; i++)
		{
			FileDefinition fileDefinition = new FileDefinition();
			fileDefinition.Name = reader.ReadTerminatedString();
			fileDefinition.Checksum = reader.ReadUInt64();
			fileDefinition.Offset = reader.ReadUInt64();
			fileDefinition.Size = reader.ReadUInt32();
			directoryDefinition.Files[fileDefinition.Name] = fileDefinition;
		}

		return directoryDefinition;
	}

	private static DirectoryDefinition BuildDirectory(DirectoryInfo info, Stack<PackIgnore> ignoreStack)
	{
		// Much, much nicer directory searching that uses DirectoryInfo.
		// Makes everything much cleaner and less hacky.
		DirectoryDefinition directoryDefinition = new DirectoryDefinition();
		directoryDefinition.Name = info.Name;
		directoryDefinition.DirectoryInfo = info;
		
		string ignorePath = System.IO.Path.Join(info.FullName, ".packignore");
		if (File.Exists(ignorePath))
		{
			ignoreStack.Push(new PackIgnore(File.ReadAllText(ignorePath), info.FullName));
		}

		foreach (var subInfo in info.GetDirectories())
		{
			DirectoryDefinition directory = BuildDirectory(subInfo, ignoreStack);
			directoryDefinition.Directories[directory.Name] = directory;
		}
		
		foreach (var file in info.GetFiles())
		{
			if (file.Name == ".packignore")
				continue;
			if(ignoreStack.Any(s => s.Disallows(file.FullName)))
				continue;
			
			FileDefinition definition = new FileDefinition
			{
				Name = file.Name,
				FileInfo = file
			};
			directoryDefinition.Files[definition.Name] = definition;
		}


		return directoryDefinition;
	}

	private static void Write(DirectoryDefinition definition, WriteContext writeContext)
	{
		writeContext.Writer.Write(definition.Name.ToCharArray());
		writeContext.Writer.Write((byte)0);
		
		writeContext.Writer.Write((uint)definition.Directories.Count);
		foreach (DirectoryDefinition dir in definition.Directories.Values)
		{
			Write(dir, writeContext);
		}
		
		writeContext.Writer.Write((uint)definition.Files.Count);
		foreach (FileDefinition file in definition.Files.Values)
		{
			using FileStream fs = file.FileInfo!.OpenRead();
			
			writeContext.Writer.Write(file.Name.ToCharArray());
			writeContext.Writer.Write((byte)0);
			
			writeContext.Writer.Write(0ul);
			writeContext.Writer.Write(writeContext.WriteFile(fs, (ulong)fs.Length));
			writeContext.Writer.Write((uint)fs.Length);
		}
	}

	public static void Pack(string input, string output, bool listing = true, bool globalRoot = true)
	{
		using MD5 md5 = MD5.Create();
		
		if (input == "")
			input = ".";

		
		DirectoryInfo readDirectory = new DirectoryInfo(input);
		DirectoryDefinition definition = BuildDirectory(readDirectory, new Stack<PackIgnore>());
		// If the root dir has an empty name that means it is "global", and mustn't
		// be directly addressed. If it is non-empty, it will always take it into account
		// when searching.
		if (globalRoot)
			definition.Name = "";

		using FileStream outputStream = File.OpenWrite(output);
		using BinaryWriter writer = new BinaryWriter(outputStream);
		
		writer.Write("CDR2".ToCharArray());
		writer.Write(2u);
		
		WriteContext writeContext = new WriteContext(output, writer);
		Write(definition, writeContext);
		writeContext.Finish();

		writer.Write((ulong)writeContext.SegmentOffset.Count);
		foreach (ulong segment in writeContext.SegmentOffset)
		{
			writer.Write(segment);
		}
	}

	// General container class for writing CDIR files
	private class WriteContext
	{
		
		// Valve has the VPK format which is quite simmilar to CDIR.
		// Now, they use somewhere from 100-200 megs in TF2 for archive
		// file size limits. I'm going for the upper limit as my personal
		// use case might include slightly larger files, plus I'd prefer not
		// to have a gazillion parts.
		// Now for the bigger question: How many bytes in a kilobyte?
		// I don't fucking know and no matter what I choose people will be mad.
		// So, my solution is to just say fuck you to everyone and use both:
		public const ulong TargetLength = 1024 * 1000 * 200;

		private ulong totalOffset;
		private ulong currentSegmentSize = 0;
		private FileStream? writeStream;
		private string baseFileName;

		public List<ulong> SegmentOffset = new List<ulong>();


		public readonly BinaryWriter Writer;

		public WriteContext(string baseFile, BinaryWriter writer)
		{
			baseFileName = baseFile;
			Writer = writer;
		}


		private void NewFile()
		{
			currentSegmentSize = 0;
			writeStream?.Flush();
			writeStream?.Close();
			writeStream?.Dispose();
			writeStream = File.OpenWrite(baseFileName + "_" + SegmentOffset.Count);
			SegmentOffset.Add(totalOffset);

			BinaryWriter writer = new BinaryWriter(writeStream, Encoding.UTF8, false);
			writer.Write("CSEG".ToCharArray());
		}
		
		public ulong WriteFile(Stream stream, ulong size)
		{
			if(writeStream == null)
				NewFile();
			// We'll try to split early(I think?)
			// idk, seem to get mixed results, but it
			// might also be the good ol' 1024v1000 debate.
			if (currentSegmentSize + size > TargetLength)
			{
				NewFile();
			}
			ulong fileOffset = totalOffset;
			
			stream.CopyTo(writeStream!);

			totalOffset += size;
			currentSegmentSize += size;
			return fileOffset;
		}

		public void Finish()
		{
			writeStream?.Flush();
			writeStream?.Close();
			writeStream?.Dispose();
		}

	}
}

public class CDirReadHandle : IDisposable
{
	public readonly Stream ReadStream;
	public readonly ulong Length;
	public readonly ulong ReadOffset;
	public readonly ContentPath Path;
	public readonly ulong Checksum;
	
	public CDirReadHandle(ulong readOffset, ulong length, Stream readStream, ContentPath path, ulong checksum)
	{
		ReadOffset = readOffset;
		Length = length;
		ReadStream = readStream;
		Path = path;
		Checksum = checksum;
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

internal struct FileDefinition
{
	public string Name;
	public ulong Checksum;
	public ulong Offset;
	public uint Size;
	public FileInfo? FileInfo;
}

internal struct DirectoryDefinition
{
	public string Name;
	public DirectoryInfo? DirectoryInfo;
	public Dictionary<string, DirectoryDefinition> Directories = new Dictionary<string, DirectoryDefinition>();
	public Dictionary<string, FileDefinition> Files = new Dictionary<string, FileDefinition>();
	
	public DirectoryDefinition()
	{
		Name = "";
	}
}
using System.Collections.Specialized;
using System.Diagnostics;

namespace ContentPipe;

public class CpkgFile
{


	private string[] expectedParts = Array.Empty<string>();
	private CprtFile?[] parts = Array.Empty<CprtFile?>();
	private readonly Queue<int> readStack = new Queue<int>();
	private readonly string filePath;
	private Node[] nodes = Array.Empty<Node>();
	
	public CpkgFile(string path) : this(File.OpenRead(path), path)
	{
		
	}
	
	public CpkgFile(Stream stream, string filePath)
	{
		this.filePath = filePath;
		LittleEndianBinaryReader binaryReader = new (stream);
		
		string header = new (binaryReader.ReadChars(4));
		Assert(header == "CPKG", "Invalid Header!");
		
		ushort major = binaryReader.ReadUInt16();
		ushort minor = binaryReader.ReadUInt16();

		if (major == 1 && minor == 0)
		{
			Load10(binaryReader);
		}
		else
		{
			Assert(false, "Unsupported CPKG version " + major + "." + minor);
		}
	}

	private void Load10(LittleEndianBinaryReader binaryReader)
	{
		string partsHeader = new (binaryReader.ReadChars(4));
		Assert(partsHeader == "PTRS", "Invalid Parts Header!");
		uint partCount = binaryReader.ReadUInt32();

		expectedParts = new string[partCount];
		parts = new CprtFile[partCount];
		for (int i = 0; i < partCount; i++)
		{
			expectedParts[i] = binaryReader.ReadNullTermString();
		}

		parts[0] = new CprtFile(File.OpenRead(Path.ChangeExtension(filePath, ".0.cprt")));
		nodes = new Node[parts[0]!.ChunkCount];
		for (uint i = 0; i < parts[0]!.ChunkCount; i++)
		{
			nodes[i] = new Node(i, parts[0]!, this);
		}
	}

	private void LoadPart(uint id)
	{
		if(parts[id] != null)
			return;
		if (readStack.Count > 10)
		{
			parts[readStack.Dequeue()] = null;
		}
		parts[id] = new CprtFile(File.OpenRead(Path.ChangeExtension(filePath, "." + id + ".cprt")));
	}

	public Stream? ReadFile(string path)
	{
		string[] pathParts = path.Split("/");

		Node current = nodes[0];
		int currentPartIndex = 0;

		while (true)
		{
			if (currentPartIndex + 1 != pathParts.Length)
			{
				Assert(current.IsDirectory);

				List<uint> searchedIndices = new List<uint>();
				while (current.IsLink)
				{
					// Prevent looping links!
					Assert(!searchedIndices.Contains(current.Linked));
					searchedIndices.Add(current.Linked);
					current = nodes[current.Linked];
				}
				
				current = nodes[current.Directory[pathParts[currentPartIndex]]];
				currentPartIndex++;

			}
			else
			{
				Assert(!current.IsDirectory);
				
				List<uint> searchedIndices = new List<uint>();
				while (current.IsLink)
				{
					// Prevent looping links!
					Assert(!searchedIndices.Contains(current.Linked));
					searchedIndices.Add(current.Linked);
					current = nodes[current.Linked];
				}
				
				LoadPart(current.FilePart);
				return parts[current.FilePart]?.ReadChunk(current.FileChunk);
			}
		}
	}

	private void Assert(bool statement, string? message = null)
	{
		if (!statement)
		{
			throw new InvalidFileException(message);
		}
	}
	
	
	private class Node
	{
		public readonly uint ID;

		public readonly CpkgFile File;
		public readonly BitVector8 Flags;
		public readonly LittleEndianBinaryReader Reader;

		public readonly bool IsLink;
		public readonly bool IsDirectory;

		public uint Linked;

		public uint FilePart;
		public uint FileChunk;

		public readonly Dictionary<string, uint> Directory = new Dictionary<string, uint>();

		public Node(uint id, CprtFile part, CpkgFile file)
		{
			this.ID = id;
			this.File = file;
			Reader = new LittleEndianBinaryReader(part.ReadChunk(id)!);
			Flags = Reader.ReadByte();

			IsDirectory = Flags[0];
			IsLink = Flags[1];

			if (IsLink)
			{
				Linked = Reader.ReadUInt32();
			}
			else
			{
				if (IsDirectory)
				{
					uint children = Reader.ReadUInt32();
					for (int i = 0; i < children; i++)
					{
						string name = Reader.ReadNullTermString();
						uint child = Reader.ReadUInt32();
						Directory[name] = child;
					}
				}
				else
				{
					FilePart = Reader.ReadUInt32();
					FileChunk = Reader.ReadUInt32();
				}
			}
		}
	}
}

internal class CprtFile
{
	private Stream stream;
	private Chunk[] chunks;

	public readonly uint ChunkCount;

	private byte[] readBuffer;
	
	public CprtFile(Stream stream)
	{
		this.stream = stream;

		LittleEndianBinaryReader binaryReader = new LittleEndianBinaryReader(stream);
		
		string chars = new (binaryReader.ReadChars(4));
		Assert(chars == "CPRT", "Invalid Part File Header!");
		
		ushort major = binaryReader.ReadUInt16();
		ushort minor = binaryReader.ReadUInt16();
		Assert(major == 1 && minor == 0, "Unsupported CPRT version " + major + "." + minor);

		ChunkCount = binaryReader.ReadUInt32();
		chunks = new Chunk[ChunkCount];
		uint maxSize = 0;
		for (int i = 0; i < chunks.Length; i++)
		{
			chunks[i] = new Chunk(binaryReader.ReadUInt32(), binaryReader.ReadUInt32());
			uint size = chunks[i].End - chunks[i].Start;
			if (size > maxSize)
				maxSize = size;
		}
		readBuffer = new byte[maxSize];

		uint _ = binaryReader.ReadUInt32(); // We don't need the data size.
	}

	public Stream? ReadChunk(uint id)
	{
		if (id > chunks.Length)
			return null;
		Chunk chunk = chunks[id];

		stream.Position = chunk.Start;
		stream.ReadExactly(readBuffer, 0, (int)(chunk.End - chunk.Start));

		MemoryStream memory = new MemoryStream();
		memory.Write(readBuffer, 0, (int)(chunk.End - chunk.Start));
		return memory;
	}


	private void Assert(bool statement, string? message = null)
	{
		if (!statement)
		{
			throw new InvalidFileException(message);
		}
	}
	
	private struct Chunk
	{
		public uint Start;
		public uint End;

		public Chunk(uint start, uint end)
		{
			Start = start;
			End = end;
		}
	}
}
using System.Text;

namespace ContentPipe;

internal interface IContentProvider
{
	public ContentLump? Load(string name);
	public string[] GetContent();
}

internal readonly struct PrefixedContentProvider : IContentProvider
{
	private readonly IContentProvider provider;
	private readonly string prefix;

	public PrefixedContentProvider(string prefix, IContentProvider provider)
	{
		this.prefix = prefix;
		this.provider = provider;
	}
	
	public ContentLump? Load(string name)
	{
		if (!name.StartsWith(prefix))
			return null;
		
		string deAliased = name[prefix.Length..];
		return provider.Load(deAliased);
	}
	
	public string[] GetContent()
	{
		string[] content = provider.GetContent();
		for (int i = 0; i < content.Length; i++)
		{
			content[i] = prefix + content[i];
		}
		return content;
	}
	
}

internal readonly struct CDirContentProvider : IContentProvider
{
	private readonly CDIRFile directory;
	
	public CDirContentProvider(CDIRFile directory)
	{
		this.directory = directory;
	}

	public ContentLump? Load(string name)
	{
		CDirReadHandle? handle = directory.ReadFile(name);
		if (handle == null)
			return null;

		// TODO: Wrap stuff instead!
		return new ContentLump
		{
			Data = handle.Read()
		};
	}
	public string[] GetContent()
	{
		CDirReadHandle? handle = directory.ReadFile("__content_listing");
		if (handle == null)
			return Array.Empty<string>();

		return Encoding.Default.GetString(handle.Read()).Split("\n");
	}
}

internal readonly struct PacketContentProvider : IContentProvider
{
	private readonly ContentDirectory directory;
	public PacketContentProvider(ContentDirectory directory)
	{
		this.directory = directory;
	}
	
	public ContentLump? Load(string name)
	{
		return directory[name];
	}
	
	public string[] GetContent()
	{
		return directory.Content;
	}
}

internal readonly struct PhysicalContentProvider : IContentProvider
{
	private readonly string directory;
	public PhysicalContentProvider(string directory)
	{
		this.directory = directory;
	}
	
	public ContentLump? Load(string name)
	{
		string path = Path.Combine(directory, name);
		if (File.Exists(path))
		{
			return new ContentLump()
			{
				Stream = File.OpenRead(path), Name = name
			};
		}
		return null;
	}
	
	public string[] GetContent()
	{
		string[] files =  Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

		for (int i = 0; i < files.Length; i++)
		{
			files[i] = Path.GetRelativePath(directory, files[i]);
		}
		
		return files;
	}
}
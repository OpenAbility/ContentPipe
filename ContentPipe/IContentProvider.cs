namespace ContentPipe;

internal interface IContentProvider
{
	public ContentLump? Load(string name);
}

internal struct PacketContentProvider : IContentProvider
{
	private ContentDirectory directory;
	public PacketContentProvider(ContentDirectory directory)
	{
		this.directory = directory;
	}
	
	public ContentLump? Load(string name)
	{
		return directory[name];
	}
}

internal struct PhysicalContentProvider : IContentProvider
{
	private string directory;
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
				Data = File.ReadAllBytes(path), Name = name
			};
		}
		return null;
	}
}
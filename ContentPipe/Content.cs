using System.Text;

namespace ContentPipe;

/// <summary>
/// The main content pipeline accessor.
/// </summary>
public static class Content
{
	private static readonly Dictionary<string, IContentProvider> Providers = new Dictionary<string, IContentProvider>();

	/// <summary>
	/// Load a packed content directory(.cpkg file)
	/// </summary>
	/// <param name="path">The path to the directory without extension</param>
	public static void LoadDirectory(string path)
	{
		if(Providers.ContainsKey(path))
			return;
		Providers.Add(path, new PacketContentProvider(new ContentDirectory(path)));
	}
	
	/// <summary>
	/// Unload a packed content directory
	/// </summary>
	/// <param name="path">The same as you used when you loaded it(path without extension)</param>
	public static void Unload(string path)
	{
		if (Providers.ContainsKey(path))
			Providers.Remove(path);
	}
	
	/// <summary>
	/// Unload all content directories
	/// </summary>
	public static void UnloadAll()
	{
		Providers.Clear();
	}
	
	/// <summary>
	/// Load a physical directory(a directory on disk)
	/// </summary>
	/// <param name="path">The path to the directory</param>
	public static void LoadPhysicalDirectory(string path)
	{
		if(Providers.ContainsKey(path))
			return;
		if(!Directory.Exists(path))
			return;
		
		Providers.Add(path, new PhysicalContentProvider(path));
	}

	/// <summary>
	/// Load/Fetch a content lump from loaded directories. Newer loaded directories are fetched from before other directories
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The content lump loaded, or null if it is not available</returns>
	public static ContentLump? Load(string resource)
	{
		foreach (var provider in Providers.Values)
		{
			ContentLump? lump = provider.Load(resource);

			if (lump != null)
				return lump;
		}
		return null;
	}
	
	/// <summary>
	/// Load/Fetch a content lump from all loaded directories. Newer loaded directories are fetched from before other directories
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The content lumps loaded</returns>
	public static ContentLump[] LoadAll(string resource)
	{
		List<ContentLump> contentLumps = new List<ContentLump>();
		foreach (var provider in Providers.Values.Reverse())
		{
			ContentLump? lump = provider.Load(resource);

			if (lump != null)
				contentLumps.Add(lump.Value);
		}
		
		return contentLumps.ToArray();
	}

	/// <summary>
	/// Read the binary data from a resource. This is simply a wrapper for
	/// <code>
	/// Content.Load(resource).Data
	/// </code>
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The binary data loaded, or an empty byte array if resource isn't available</returns>
	public static byte[] LoadBytes(string resource)
	{
		ContentLump? lump = Load(resource);
		if (lump == null)
			return Array.Empty<byte>();
		return lump.Value.Data;
	}
	
	/// <summary>
	/// Read a string from a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The string loaded, or an empty string if resource doesn't exist</returns>
	public static string LoadString(string resource)
	{
		return Encoding.UTF8.GetString(LoadBytes(resource));
	}
	
	/// <summary>
	/// Read a stream from a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static Stream LoadStream(string resource)
	{
		return new MemoryStream(LoadBytes(resource));
	}
	
	/// <summary>
	/// Load all bytes available for a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static byte[][] LoadAllBytes(string resource)
	{
		List<byte[]> data = new List<byte[]>();
		ContentLump[] lumps = LoadAll(resource);

		foreach (var lump in lumps)
		{
			data.Add(lump.Data);
		}

		return data.ToArray();
	}
	
	/// <summary>
	/// Read all streams available for a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static Stream[] LoadAllStreams(string resource)
	{
		var allBytes = LoadAllBytes(resource);
		List<Stream> streams = new List<Stream>();
		
		foreach (var lump in allBytes)
		{
			streams.Add(new MemoryStream(lump));
		}

		return streams.ToArray();
	}
	
		
	/// <summary>
	/// Read all strings available for a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static string[] LoadAllStrings(string resource)
	{
		var allBytes = LoadAllBytes(resource);
		List<string> strings = new List<string>();
		
		foreach (var lump in allBytes)
		{
			strings.Add(Encoding.UTF8.GetString(lump));
		}

		return strings.ToArray();
	}
}

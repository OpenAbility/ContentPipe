using System.Text;

namespace ContentPipe;

/// <summary>
/// The main content pipeline accessor.
/// </summary>
public static class Content
{
	private static readonly Dictionary<string, ContentDirectory> Directories = new Dictionary<string, ContentDirectory>();
	private static readonly List<string> PhysicalDirectories = new List<string>();
	
	/// <summary>
	/// Load a packed content directory(.cpkg file)
	/// </summary>
	/// <param name="path">The path to the directory without extension</param>
	public static void LoadDirectory(string path)
	{
		if(Directories.ContainsKey(path))
			return;
		Directories.Add(path, new ContentDirectory(path));
	}
	
	/// <summary>
	/// Unload a packed content directory
	/// </summary>
	/// <param name="path">The same as you used when you loaded it(path without extension)</param>
	public static void UnloadDirectory(string path)
	{
		if (Directories.ContainsKey(path))
			Directories.Remove(path);
	}
	
	/// <summary>
	/// Unload all content directories
	/// </summary>
	public static void UnloadAll()
	{
		Directories.Clear();
		PhysicalDirectories.Clear();
	}
	
	/// <summary>
	/// Load a physical directory(a directory on disk)
	/// </summary>
	/// <param name="path">The path to the directory</param>
	public static void LoadPhysicalDirectory(string path)
	{
		if(PhysicalDirectories.Contains(path))
			return;
		if(!Directory.Exists(path))
			return;
		
		PhysicalDirectories.Add(path);
	}
	
	/// <summary>
	/// Unload a physical directory
	/// </summary>
	/// <param name="path">The path to the directory</param>
	public static void UnloadPhysicalDirectory(string path)
	{
		if (PhysicalDirectories.Contains(path))
			PhysicalDirectories.Remove(path);
	}
	
	/// <summary>
	/// Load/Fetch a content lump from loaded directories. Newer loaded directories are fetched from before other directories
	/// <remarks>Physical directories will always be loaded from before packed directories</remarks>
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The content lump loaded, or null if it is not available</returns>
	public static ContentLump? Load(string resource)
	{

		foreach (var directory in PhysicalDirectories)
		{
			string path = Path.Combine(directory, resource);
			if (File.Exists(path))
			{
				return new ContentLump()
				{
					Data = File.ReadAllBytes(path),
					Name = resource
				};
			}
		}
		
		foreach (var directory in Directories.Values.Reverse())
		{
			ContentLump? lump = directory[resource];

			if (lump != null)
				return lump;
		}
		return null;
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
}

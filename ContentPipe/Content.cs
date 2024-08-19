using System.Text;
using System.Text.RegularExpressions;

namespace ContentPipe;

/// <summary>
/// The main content pipeline accessor.
/// </summary>
public static class Content
{
	private static readonly List<ContentMount> Mounts = new List<ContentMount>();
	
	private static readonly Dictionary<string, int> LoadedContent = new Dictionary<string, int>();
	
	/// <summary>
	/// If Loads should be logged. This will slow down performance of loading by some amount!
	/// </summary>
	public static bool ShouldLogLoads = false;

	public static string IdentifierPrefix = "";
	
	/// <summary>
	/// A filter to run on all log load registrations, in case you want to ignore something.
	/// This only affects the output log!
	/// </summary>
	public static Regex LogLoadIgnoreFilter = new Regex("");

	/// <summary>
	/// Mount a ContentMount
	/// </summary>
	/// <param name="mount">The mount to add</param>
	/// <returns>True if it was mounted, otherwise false</returns>
	public static bool Mount(ContentMount mount)
	{
		if (Mounts.Contains(mount))
			return false;
		Mounts.Add(mount);
		return true;
	}

	/// <summary>
	/// Unmount a ContentMount
	/// </summary>
	/// <param name="mount">The mount to unmount</param>
	/// <returns>True if it was unmounted, otherwise false</returns>
	public static bool Unmount(ContentMount mount)
	{
		return Mounts.Remove(mount);
	}

	/// <summary>
	/// Searches for a ContentMount
	/// </summary>
	/// <param name="id">The mount ID</param>
	/// <returns>The mount, or null if not found</returns>
	public static ContentMount? GetMount(string id)
	{
		return Mounts.Find(m => m.ID == id);
	}
	
	public static void UnmountAll()
	{
		Mounts.Clear();
	}

	private static void RegisterLoad(string resource)
	{
		if(!ShouldLogLoads)
			return;
		LoadedContent.TryAdd(resource, 0);
		LoadedContent[resource]++;
	}

	/// <summary>
	/// Load/Fetch a content lump from loaded directories. Newer loaded directories are fetched from before other directories
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The content lump loaded, or null if it is not available</returns>
	public static ContentLump? Load(ContentPath resource)
	{
		ContentPath noDir = resource.NoDirectoryIdentifier();
		RegisterLoad(noDir);

		if (resource.DirectoryIdentifier != null)
		{
			ContentMount? mount = Mounts.Find(m => m.ID == resource.DirectoryIdentifier);
			if(mount != null)
				return mount.Load(noDir);
		}
		
		foreach (var mount in Mounts)
		{
			ContentLump? lump = mount.Load(resource);

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
	public static ContentLump[] LoadAll(ContentPath resource)
	{
		// Directory identifiers basically nullify LoadAll
		if (resource.DirectoryIdentifier != null)
		{
			ContentMount? mount = Mounts.Find(m => m.ID == resource.DirectoryIdentifier);
			ContentPath noDir = resource.NoDirectoryIdentifier();
			RegisterLoad(noDir);
			ContentLump? lump = mount.Load(noDir);
			if (lump != null)
			{
				return new ContentLump[1]
				{
					lump.Value!
				};
			}
			return Array.Empty<ContentLump>();
		}
		
		resource = resource.NoDirectoryIdentifier();
		RegisterLoad(resource);
		List<ContentLump> contentLumps = new List<ContentLump>();
		foreach (var mount in Mounts)
		{
			ContentLump? lump = mount.Load(resource);

			if (lump != null)
				contentLumps.Add(lump.Value);
		}
		
		return contentLumps.ToArray();
	}

	public static void RequestCompiles(ContentPath resource)
	{
		if (resource.DirectoryIdentifier != null)
		{
			ContentMount? mount = Mounts.Find(m => m.ID == resource.DirectoryIdentifier);
			if (mount != null)
			{
				ContentPath noDir = resource.NoDirectoryIdentifier();
				mount.Compile(noDir);
				return;
			}
		}
		
		resource = resource.NoDirectoryIdentifier();
		foreach (var mount in Mounts)
		{
			mount.Compile(resource);
		}
	}

	/// <summary>
	/// Read the binary data from a resource.
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The binary data loaded, or an empty byte array if resource isn't available</returns>
	public static byte[] LoadBytes(ContentPath resource)
	{
		return LoadBytes(Load(resource));
	}
	/// <summary>
	/// Read the binary data from a lump
	/// </summary>
	/// <param name="lump">The lump to load from</param>
	/// <returns>The binary data loaded, or an empty byte array if none is available</returns>
	public static byte[] LoadBytes(ContentLump? lump)
	{
		if (lump == null)
			return Array.Empty<byte>();
		if (lump.Value.Data == null)
		{
			if(lump.Value.Stream == null)
				return Array.Empty<byte>();

			byte[] readBuffer = new byte[lump.Value.Stream.Length];
			if (lump.Value.Stream.Read(readBuffer) != readBuffer.Length)
			{
				
			}
			return readBuffer;
		}
		return lump.Value.Data;
	}
	
	/// <summary>
	/// Read a string from a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The string loaded, or an empty string if resource doesn't exist</returns>
	public static string LoadString(ContentPath resource)
	{
		return Encoding.UTF8.GetString(LoadBytes(resource));
	}
	
	/// <summary>
	/// Read a string from a lump
	/// </summary>
	/// <param name="lump">The lump to read from</param>
	/// <returns>The string loaded, or an empty string if no data was found</returns>
	public static string LoadString(ContentLump? lump)
	{
		return Encoding.UTF8.GetString(LoadBytes(lump));
	}
	
	/// <summary>
	/// Read a stream from a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static Stream LoadStream(ContentPath resource)
	{
		return LoadStream(Load(resource));
	}

	/// <summary>
	/// Read a stream from a lump
	/// </summary>
	/// <param name="lump">The lump to read from</param>
	/// <returns>The stream, or Stream.Null if no data could be loaded from the lump</returns>
	public static Stream LoadStream(ContentLump? lump)
	{
		if(lump == null)
			return Stream.Null;
		
		if (lump.Value.Stream == null)
		{
			return lump.Value.Data == null ? Stream.Null : new MemoryStream(lump.Value.Data);
		}
		return lump.Value.Stream;
	}
	
	/// <summary>
	/// Load all bytes available for a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static byte[][] LoadAllBytes(ContentPath resource)
	{
		List<byte[]> data = new List<byte[]>();
		ContentLump[] lumps = LoadAll(resource);

		foreach (var lump in lumps)
		{
			data.Add(LoadBytes(lump));
		}

		return data.ToArray();
	}
	
	/// <summary>
	/// Read all streams available for a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static Stream[] LoadAllStreams(ContentPath resource)
	{
		var allBytes = LoadAll(resource);
		List<Stream> streams = new List<Stream>();
		
		foreach (var lump in allBytes)
		{
			streams.Add(LoadStream(lump));
		}

		return streams.ToArray();
	}
	
		
	/// <summary>
	/// Read all strings available for a resource
	/// </summary>
	/// <param name="resource">The resource path to load, relative to the directory</param>
	/// <returns>The stream to load, as a MemoryStream, points towards the binary data, or an empty byte array if it is not available</returns>
	public static string[] LoadAllStrings(ContentPath resource)
	{
		var allBytes = LoadAll(resource);
		List<string> strings = new List<string>();
		
		foreach (var lump in allBytes)
		{
			strings.Add(LoadString(lump));
		}

		return strings.ToArray();
	}
	
	/// <summary>
	/// Get the load log if one is collected, formatted as CSV
	/// </summary>
	/// <returns>The load log</returns>
	public static string WriteLoadLog(bool includeDeadResources = false)
	{
		StringWriter stringWriter = new StringWriter();
		stringWriter.WriteLine("File,Loads");

		if (includeDeadResources)
		{
			foreach (var mount in Mounts)
			{
				foreach (var res in mount.GetFiles())
				{
					if(!String.IsNullOrWhiteSpace(res))
						LoadedContent.TryAdd(res, 0);
				}
			}
		}
		var lc = LoadedContent.OrderByDescending(l => l.Value);
		foreach (var load in lc)
		{
			if(!LogLoadIgnoreFilter.IsMatch(load.Key))
				stringWriter.WriteLine(load.Key + "," + load.Value);
		}
		
		return stringWriter.ToString();
	}
	
	/// <summary>
	/// Get all content, including duplicates
	/// </summary>
	/// <returns>An enumerable returning each file available, including duplicates.</returns>
	public static IEnumerable<ContentPath> GetAllContent(bool packable = false)
	{
		foreach (var mount in Mounts)
		{
			foreach (var v in mount.GetFiles(packable))
			{
				yield return v;
			}
		}
	}

	public static ulong GetContentID(string path)
	{
		ContentLump? lump = Load(path);
		if (lump == null)
			return 0;
		return lump.Value.UniqueID ?? 0;
	}
}

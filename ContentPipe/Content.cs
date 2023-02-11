using System.Text;

namespace ContentPipe;

public static class Content
{
	private static readonly Dictionary<string, ContentDirectory> Directories = new Dictionary<string, ContentDirectory>();
	private static readonly List<string> PhysicalDirectories = new List<string>();

	public static void LoadDirectory(string path)
	{
		if(Directories.ContainsKey(path))
			return;
		Directories.Add(path, new ContentDirectory(path));
	}

	public static void UnloadDirectory(string path)
	{
		if (Directories.ContainsKey(path))
			Directories.Remove(path);
	}
	
	public static void LoadPhysicalDirectory(string path)
	{
		if(PhysicalDirectories.Contains(path))
			return;
		if(!Directory.Exists(path))
			return;
		
		PhysicalDirectories.Add(path);
	}

	public static void UnloadPhysicalDirectory(string path)
	{
		if (PhysicalDirectories.Contains(path))
			PhysicalDirectories.Remove(path);
	}

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

	public static byte[] LoadBytes(string resource)
	{
		ContentLump? lump = Load(resource);
		if (lump == null)
			return Array.Empty<byte>();
		return lump.Value.Data;
	}

	public static string LoadString(string resource)
	{
		return Encoding.UTF8.GetString(LoadBytes(resource));
	}

	public static Stream LoadStream(string resource)
	{
		return new MemoryStream(LoadBytes(resource));
	}
}

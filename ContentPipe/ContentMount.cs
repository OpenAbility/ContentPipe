namespace ContentPipe;

/// <summary>
/// A content mounting point
/// </summary>
public class ContentMount
{
	public readonly string ID;
	public readonly string? MountPoint;
	public readonly string PhysicalPath;
	public readonly ContentMountType Type;

	private readonly ContentDirectory? contentDirectory;
	
	internal ContentMount(string id, string? mountPoint, string physicalPath, ContentDirectory? directory, ContentMountType type)
	{
		ID = id;
		MountPoint = mountPoint;
		PhysicalPath = physicalPath;
		contentDirectory = directory;
		Type = type;
	}

	public static ContentMount Create(ContentMountData data)
	{
		ContentDirectory? directory = null;
		if (data.MountType == ContentMountType.PackedDirectory)
		{
			directory = new ContentDirectory(data.Path + ".cdir");
		}

		return new ContentMount(data.ID ?? data.Path, data.MountPoint, data.Path, directory, data.MountType);
	}

	public void RequestCompile(ContentPath path)
	{
		if (Type != ContentMountType.PhysicalDirectory)
			return;

		ContentPath loadablePath = path.NoDirectoryIdentifier().RemoveMount(this);
		string loadPath = Path.Combine(PhysicalPath, loadablePath);

		if (Content.ContentCompiler == null)
			return;
		
		if (!Content.ContentCompiler.ShouldRecompile(path, this, !File.Exists(loadPath)))
			return;

		if (!Directory.Exists(Path.GetDirectoryName(loadPath) ?? ""))
			Directory.CreateDirectory(Path.GetDirectoryName(loadPath) ?? "");

		FileStream fileStream = File.OpenWrite(loadPath);
		Content.ContentCompiler.RecompileContent(path, fileStream, this, File.Exists(loadPath));
		fileStream.Close();

	}

	public ContentLump? Load(ContentPath path)
	{
		if (MountPoint != null && !(MountPoint == path.Parts.FirstOrDefault() || MountPoint != path.MountPoint))
			return null;

		ContentPath loadablePath = path.NoDirectoryIdentifier().RemoveMount(this);
		
		// CDIR loading
		if (Type == ContentMountType.PackedDirectory)
		{
			CDirReadHandle? handle = contentDirectory!.ReadFile(loadablePath);
			if (handle == null)
				return null;

			// TODO: Wrap stuff instead!
			return new ContentLump
			{
				Data = handle.Read(),
				Name = path,
				UniqueID = handle.Hash
			};
		} else if (Type == ContentMountType.PhysicalDirectory)
		{
			string loadPath = Path.Combine(PhysicalPath, loadablePath);
			if (Content.ContentCompiler != null)
			{
				bool shouldCompile = true;
				shouldCompile = Content.ContentCompiler.ShouldRecompile(path, this, !File.Exists(loadPath));
				if (shouldCompile)
				{
					if (!Directory.Exists(Path.GetDirectoryName(loadPath) ?? ""))
						Directory.CreateDirectory(Path.GetDirectoryName(loadPath) ?? "");
					try
					{
						FileStream fileStream = File.OpenWrite(loadPath);
						Content.ContentCompiler.RecompileContent(path, fileStream, this, File.Exists(loadPath));
						fileStream.Close();
					}
					catch (Exception _)
					{
						// ignored
					}

				}
			}
		
			if (File.Exists(loadPath))
			{
				DateTime lastWrite = File.GetLastWriteTimeUtc(loadPath);
				try
				{
					return new ContentLump
					{
						Stream = File.OpenRead(loadPath), Name = path, UniqueID = (ulong)(lastWrite - DateTime.UnixEpoch).TotalSeconds
					};
				}
				catch (Exception _)
				{
					return null;
				}

			}
		}

		return null;
	}
	
	public ContentPath[] GetContent(bool packable = false)
	{
		if (Type == ContentMountType.PackedDirectory)
		{
			return contentDirectory!.GetContent().Select(c => new ContentPath(c)).ToArray();
		} else if (Type == ContentMountType.PhysicalDirectory)
		{
			List<ContentPath> paths = new List<ContentPath>();
			ContentPath root = new ContentPath();
			if (MountPoint != null)
				root = root.AddMount(MountPoint);
			Stack<PackIgnore>? ignores = packable ? new Stack<PackIgnore>() : null;
			EnumerateDirectory(new DirectoryInfo(PhysicalPath), ref paths, root, ignores);
			return paths.ToArray();
		}
		return Array.Empty<ContentPath>();
	}
	
	
	private void EnumerateDirectory(DirectoryInfo directory, ref List<ContentPath> paths, ContentPath parentPath, Stack<PackIgnore>? ignoreStack)
	{
		if (ignoreStack != null)
		{
			string ignorePath = Path.Combine(directory.FullName, ".packignore");
			if (File.Exists(ignorePath))
			{
				ignoreStack.Push(new PackIgnore(File.ReadAllText(ignorePath), directory.FullName));
			}
		}

		foreach (var file in directory.GetFiles())
		{
			if (ignoreStack != null)
			{
				if (file.Name.EndsWith(".packignore"))
					continue;
				if (ignoreStack.Any(i => i.Disallows(file.FullName)))
					continue;
			}


			paths.Add(parentPath.Append(file.Name));
		}

		foreach (var subdir in directory.GetDirectories())
		{
			// We push the path here. If we did it earlier it'd break.
			// If we mount the dir "Content", all paths would be "Content/...." when we want
			// "...." without the "Content". You get it?
			EnumerateDirectory(subdir, ref paths, parentPath.Append(subdir.Name), ignoreStack);
		}
	}
}

public struct ContentMountData
{
	public ContentMountType MountType;
	public string Path;
	public string? ID; // Defaults to Path
	public string? MountPoint; // AKA prefix.

	public readonly ContentMountData SetID(string id)
	{
		ContentMountData mountData = this;
		mountData.ID = id;
		return mountData;
	}
	
	public static ContentMountData Physical(string path)
	{
		return new ContentMountData()
		{
			MountType = ContentMountType.PhysicalDirectory, Path = path
		};
	}
	
	public static ContentMountData PhysicalPoint(string path, string point)
	{
		return new ContentMountData()
		{
			MountType = ContentMountType.PhysicalDirectory, Path = path, MountPoint = point
		};
	}
	
	
	public static ContentMountData Packed(string path)
	{
		return new ContentMountData()
		{
			MountType = ContentMountType.PackedDirectory, Path = path
		};
	}
	
	public static ContentMountData PackedPoint(string path, string point)
	{
		return new ContentMountData()
		{
			MountType = ContentMountType.PackedDirectory, Path = path, MountPoint = point
		};
	}
}

public enum ContentMountType
{
	/// <summary>
	/// A packed content directory
	/// </summary>
	PackedDirectory,
	/// <summary>
	/// A physical file-system directory
	/// </summary>
	PhysicalDirectory
}

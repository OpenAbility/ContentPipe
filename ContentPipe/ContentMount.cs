using ContentPipe.ContentCompilation;

namespace ContentPipe;

public class ContentMount
{
	public readonly ContentMountType MountType;
	public readonly string ID;
	public readonly string? MountPoint;
	public readonly string LoadPath;
	public readonly string? SourcePath;
	public readonly ContentCompileContext? CompileContext;
	private readonly ContentDirectory? contentDirectory;

	internal ContentMount(ContentMountType mountType, string id, string? mountPoint, string loadPath, string? sourcePath, ContentCompileContext? compileContext, ContentDirectory? contentDirectory)
	{
		MountType = mountType;
		ID = id;
		MountPoint = mountPoint;
		LoadPath = loadPath;
		SourcePath = sourcePath;
		CompileContext = compileContext;
		this.contentDirectory = contentDirectory;
	}


	public IEnumerable<ContentPath> GetFiles(bool packable = true)
	{
		if (MountType == ContentMountType.PackedDirectory)
			return contentDirectory!.GetContent(MountPoint);
		
		ContentPath root = new ContentPath();
		if (MountPoint != null)
			root = root.AddMount(MountPoint);
		Stack<PackIgnore>? ignores = packable ? new Stack<PackIgnore>() : null;

		if(SourcePath == null || CompileContext == null)
			return EnumerateDirectory(new DirectoryInfo(LoadPath), root, ignores);
		return EnumerateDirectory(new DirectoryInfo(SourcePath), root, ignores);
	}
	
	private IEnumerable<ContentPath> EnumerateDirectory(DirectoryInfo directory, ContentPath parentPath, Stack<PackIgnore>? ignoreStack)
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

			
			ContentPath filePath = parentPath.Append(file.Name);
			if(CompileContext != null && SourcePath != null)
				filePath = filePath.SetExtension(CompileContext.GetTargetExtension(filePath));
			
			yield return filePath;
		}

		foreach (var subdir in directory.GetDirectories())
		{
			// We push the path here. If we did it earlier it'd break.
			// If we mount the dir "Content", all paths would be "Content/...." when we want
			// "...." without the "Content". You get it?

			foreach (var path in EnumerateDirectory(subdir, parentPath.Append(subdir.Name), ignoreStack))
			{
				yield return path;
			}
		}
	}

	/// <summary>
	/// Compiles a resource
	/// </summary>
	/// <param name="path">The resource path to compile</param>
	/// <returns>True if the compiled resource now exists</returns>
	public bool Compile(ContentPath path)
	{
		// We cannot compile it
		if (CompileContext == null || SourcePath == null)
			return false;
		
		ContentPath loadablePath = path.NoDirectoryIdentifier().RemoveMount(this);
		
		// Get the source extension
		string sourceExtension = CompileContext.GetSourceExtension(path);
		string targetPath = Path.Join(LoadPath, loadablePath);
		string sourcePath = Path.Join(SourcePath, loadablePath.SetExtension(sourceExtension));
		
		return CompileContext.PerformCompile(sourcePath, targetPath, path.GetExtension());
	}

	public ContentLump? Load(ContentPath path)
	{
		if (MountPoint != null && !(MountPoint == path.Parts.FirstOrDefault() || MountPoint != path.MountPoint))
			return null;
		
		ContentPath loadablePath = path.NoDirectoryIdentifier().RemoveMount(this);
		
		// CDIR loading
		if (MountType == ContentMountType.PackedDirectory)
		{
			CDirReadHandle? handle = contentDirectory!.ReadFile(loadablePath);
			if (handle == null)
				return null;

			// TODO: Wrap stuff instead!
			return new ContentLump
			{
				Data = handle.Read(),
				Name = path,
				UniqueID = handle.Checksum
			};
		} else if (MountType == ContentMountType.PhysicalDirectory)
		{
			string loadPath = Path.Combine(LoadPath, loadablePath);
			bool mayCompile = CompileContext != null && SourcePath != null;
			bool mayLoad = mayCompile ? Compile(loadablePath) : File.Exists(loadPath);
			
			if (mayLoad)
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
}

public enum ContentMountType
{
	PhysicalDirectory,
	PackedDirectory
}

/// <summary>
/// Builds a content mount
/// </summary>
public class MountBuilder
{
	private ContentMountType type;
	private string loadPath;
	private string? sourceDirectory;
	private string id;
	private string? mountPoint;
	private ContentCompileContext? compileContext;

	protected MountBuilder(ContentMountType mountType, string path)
	{
		type = mountType;
		loadPath = path;
		id = path;
	}

	public static MountBuilder BeginPacked(string path)
	{
		return new MountBuilder(ContentMountType.PackedDirectory, path);
	}
	
	public static MountBuilder Begin(string path)
	{
		return new MountBuilder(ContentMountType.PhysicalDirectory, path);
	}
	
	public MountBuilder Compilable(ContentCompileContext context, string path)
	{
		if (type == ContentMountType.PackedDirectory)
			return this;
		sourceDirectory = path;
		compileContext = context;
		return this;
	}

	public MountBuilder ID(string id)
	{
		this.id = id;
		return this;
	}
	
	public MountBuilder MountPoint(string point)
	{
		mountPoint = point;
		return this;
	}

	public ContentMount Finish()
	{
		return new ContentMount(
			type, id, mountPoint, loadPath, sourceDirectory, compileContext,
			type == ContentMountType.PackedDirectory ? new ContentDirectory(loadPath + ".cdir") : null);
	}
}
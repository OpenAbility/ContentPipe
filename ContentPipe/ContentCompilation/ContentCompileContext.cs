namespace ContentPipe.ContentCompilation;

public class ContentCompileContext
{
	private readonly Dictionary<string, string> extensionMappingST = new Dictionary<string, string>();
	private readonly Dictionary<string, string> extensionMappingTS = new Dictionary<string, string>();
	private readonly Dictionary<string, ICompileHandler> compilers = new Dictionary<string, ICompileHandler>();

	public ICompileHandler? DefaultHandler;
	
	/// <summary>
	/// Register a content compile handler
	/// </summary>
	/// <param name="source"></param>
	/// <param name="target"></param>
	/// <param name="handler"></param>
	/// <returns></returns>
	public ContentCompileContext Register(string source, string target, ICompileHandler handler)
	{
		extensionMappingTS[target] = source;
		extensionMappingST[source] = target;
		compilers[target] = handler;
		return this;
	}

	public string GetSourceExtension(ContentPath target)
	{
		string ext = target.GetExtension();
		return extensionMappingTS.TryGetValue(ext, out string? source) ? source : target.GetExtension();
	}
	
	public string GetTargetExtension(ContentPath source)
	{
		string ext = source.GetExtension();
		return extensionMappingST.TryGetValue(ext, out string? target) ? target : source.GetExtension();
	}

	/// <summary>
	/// Performs a content compilation
	/// </summary>
	/// <param name="sourceFile">The source file path</param>
	/// <param name="targetFile">The target file path</param>
	/// <param name="targetExtension">The target file extension</param>
	/// <returns>True, if <c>targetFile</c> can be loaded, otherwise false</returns>
	public bool PerformCompile(string sourceFile, string targetFile, string targetExtension)
	{
		if (!File.Exists(sourceFile))
			return false;
		
		if (File.Exists(targetFile))
		{
			// If the target file is newer than the source file, don't recompile!
			if (File.GetLastWriteTimeUtc(sourceFile) <= File.GetLastWriteTimeUtc(targetFile))
				return true;
		}
		
		
		ICompileHandler? handler = compilers!.GetValueOrDefault(targetExtension, DefaultHandler);

		if (handler == null)
		{
			File.Copy(sourceFile, targetFile);
			return true;
		}
		
		return handler.Compile(sourceFile, targetFile);
	}
}

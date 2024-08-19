namespace ContentPipe.ContentCompilation;

public interface ICompileHandler
{
	/// <summary>
	/// Performs a compilation
	/// </summary>
	/// <param name="source">The source file path to load</param>
	/// <param name="target">The target file path to write to(might already exist)</param>
	/// <returns>True if <c>target</c> was successfully written, otherwise false</returns>
	public bool Compile(string source, string target);
}

namespace ContentPipe;

public interface IContentCompiler
{
	public bool ShouldRecompile(ContentPath path, ContentMount mount, bool firstCompile);
	public void RecompileContent(ContentPath path, Stream output, ContentMount mount, bool firstCompile);
}

using ContentPipe.ContentCompilation;

namespace ContentPipe.Dev;


public static class Program
{
	public static void Main(string[] args)
	{

		ContentCompileContext compileContext = new ContentCompileContext();

		compileContext.Register(".src", ".trg", new CopyCompiler());


		Content.Mount(MountBuilder.Begin("data/Compiled").Compilable(compileContext, "data/Source").ID("Compiled").Finish());
		
		Console.WriteLine("Listing");
		foreach (ContentPath file in Content.GetAllContent(true))
		{
			Console.WriteLine(file);
			ContentLump? lump = Content.Load(file);
			if (lump == null)
			{
				Console.WriteLine("Cannot load!");
				continue;
			}
			TextReader reader = new StreamReader(lump.Value.Stream!);
			Console.WriteLine("Content: " + reader.ReadToEnd());
		}
		
		Content.UnmountAll();
		
		ContentDirectory.Pack("data/Compiled", "data/content.cdir");

		Content.Mount(MountBuilder.BeginPacked("data/content").Finish());

		Console.WriteLine("ListingP");
		foreach (ContentPath file in Content.GetAllContent(true))
		{
			Console.WriteLine(file);
			ContentLump? lump = Content.Load(file);
			if (lump == null)
			{
				Console.WriteLine("Cannot load!");
				continue;
			}
			TextReader reader = new StreamReader(new MemoryStream(lump.Value.Data!));
			Console.WriteLine("Content: " + reader.ReadToEnd());
		}
		
	}

	private class CopyCompiler : ICompileHandler
	{

		public bool Compile(string source, string target)
		{
			Console.WriteLine("Compiling!");	
			string src = File.ReadAllText(source);
			File.WriteAllText(target, string.Concat(src.Reverse()));
			
			return true;
		}
	}
}

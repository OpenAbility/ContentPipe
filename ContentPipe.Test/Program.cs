// See https://aka.ms/new-console-template for more information

using ContentPipe;

ContentDirectory.CompressDirectory("Content/Content1");
ContentDirectory.CompressDirectory("Content/Content2");

Content.LoadDirectory("Content/Content1");
Content.LoadDirectory("Content/Content2");

Content.UnloadDirectory("Content/Content2");

Console.WriteLine(Content.LoadString("test.txt"));
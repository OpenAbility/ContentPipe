# ContentPipe
ContentPipe is a very basic content pipeline inspired by a series of things such as
the MonoGame Content Pipeline, GMod and its game mounting and more.

The usage is extensively documented using doxygen comments, so don't expect the best
documentation outside of your IDE and the source code.

Basic example:
```csharp
using ContentPipe;

ContentDirectory.CompressDirectory("Content");
Content.LoadDirectory("Content");

string someJsonData = Content.LoadString("file.json");

// This step is far from mandatory, but can be used to free up memory or
// if you want to do something like having consistent file names between parts
// of the game(use content directories for e.g levels etc)

Content.UnloadDirectory("Content");
```
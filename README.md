# ContentPipe
ContentPipe is a very basic content pipeline inspired by a series of things such as 
the MonoGame Content Pipeline, GMod and its game mounting and more.

The usage is extensively documented using doxygen comments, so don't expect the best
documentation outside your IDE and the source code.

Basic example:
```csharp
using ContentPipe;

// Pack the contents of directory "Content" into file "Content.cdir"
ContentDirectory.Pack("Content", "Content.cdir");

// Build MountData pointing to the packed directory "Content.cdir"
// We omit the extension.
ContentMountData mountData = ContentMountData.Packed("Content");

// Build a mount from the mount data
ContentMount mount = ContentMount.Create(mountData);
// Mount the mount onto the global content scope.
Content.Mount(mount);

string someJsonData = Content.LoadString("file.json");

// If you for whatever reason wish, you can unmount your mounts.
// This could be useful if you want to switch models based on region
// etc. Up to you.
Content.Unmount(mount);
```
namespace ContentPipe;

/// <summary>
/// A content lump, or in other words, a named byte array. Used to store data in a cpkg file.
/// </summary>
public struct ContentLump
{
	/// <summary>
	/// The path of the content lump
	/// </summary>
	public ContentPath Name;

	/// <summary>
	/// The data of the ContentLump
	/// </summary>
	public byte[]? Data;
	
	/// <summary>
	/// The stream to the data of the lump
	/// </summary>
	public Stream? Stream;

	/// <summary>
	/// A unique ID for this content lump
	/// </summary>
	public ulong? UniqueID;

	/// <summary>
	/// Create a content lump with 0:ed fields.
	/// </summary>
	public ContentLump()
	{
		Name = "";
	}

}
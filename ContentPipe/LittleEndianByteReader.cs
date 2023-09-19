using System.Text;

namespace ContentPipe;

internal class LittleEndianBinaryReader : BinaryReader
{

	public LittleEndianBinaryReader(Stream input) : base(input)
	{
	}
	public LittleEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
	{
	}
	public LittleEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
	{
	}
	

	public override short ReadInt16() => BitConverter.ToInt16(ReadForEndianness(sizeof(short)));

	public override int ReadInt32() => BitConverter.ToInt32(ReadForEndianness(sizeof(int)));

	public override long ReadInt64() => BitConverter.ToInt64(ReadForEndianness(sizeof(long)));

	public override ushort ReadUInt16() => BitConverter.ToUInt16(ReadForEndianness(sizeof(ushort)));

	public override uint ReadUInt32() => BitConverter.ToUInt32(ReadForEndianness(sizeof(uint)));

	public override ulong ReadUInt64() => BitConverter.ToUInt64(ReadForEndianness(sizeof(ulong)));

	private byte[] ReadForEndianness(int bytesToRead)
	{
		var bytesRead = ReadBytes(bytesToRead);

		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytesRead);
		}

		return bytesRead;
	}

	public string ReadNullTermString()
	{
		StringBuilder stringBuilder = new StringBuilder();

		char c;
		while ((c = ReadChar()) != '\0')
		{
			stringBuilder.Append(c);
		}
		
		return stringBuilder.ToString();
	}
}

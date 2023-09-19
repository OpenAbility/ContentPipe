using System.IO.Compression;

namespace ContentPipe;

internal class ContentStream : Stream
{

	private readonly Stream underlying;
	
	internal ContentStream(long length, Stream underlying)
	{
		this.underlying = underlying;
		Length = length;
	}

	public override void Flush()
	{
		underlying.Flush();
	}
	public override int Read(byte[] buffer, int offset, int count)
	{
		return underlying.Read(buffer, offset, count);
	}
	public override long Seek(long offset, SeekOrigin origin)
	{
		return underlying.Seek(offset, origin);
	}
	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}
	public override void Write(byte[] buffer, int offset, int count)
	{
		underlying.Write(buffer, offset, count);
	}
	public override bool CanRead
	{
		get
		{
			return underlying.CanRead;
		}
	}
	public override bool CanSeek
	{
		get
		{
			return underlying.CanSeek;
		}
	}
	public override bool CanWrite
	{
		get
		{
			return underlying.CanWrite;
		}
	}
	public override long Length { get; }
	public override long Position
	{
		get
		{
			return underlying.Position;
		}
		set
		{
			underlying.Position = value;
		}
	}
}

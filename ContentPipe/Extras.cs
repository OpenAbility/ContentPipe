using System.Security.Cryptography;
using System.Text;

namespace ContentPipe;

internal static class Extras
{
	public static string ReadTerminatedString(this BinaryReader reader)
	{
		string s = "";
		char c;
		while ((c = reader.ReadChar()) != '\0')
			s += c;
		return s;
	}
	
	public static byte[] GetHash(this string inputString)
	{
		using (HashAlgorithm algorithm = SHA256.Create())
			return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
	}

	public static string GetHashString(this string inputString)
	{
		StringBuilder sb = new StringBuilder();
		foreach (byte b in GetHash(inputString))
			sb.Append(b.ToString("X2"));

		return sb.ToString();
	}
}

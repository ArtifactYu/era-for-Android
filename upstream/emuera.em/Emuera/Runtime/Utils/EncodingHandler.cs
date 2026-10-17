using System;
using System.IO;
using System.Text;

namespace MinorShift.Emuera.Runtime.Utils;

public static class EncodingHandler
{
	public static Encoding UTF8Encoding = new UTF8Encoding(false, true);
	public static Encoding shiftjisEncoding = GetEncoding(932);
	public static Encoding UTF8BOMEncoding = new UTF8Encoding(true, true);


	public static Encoding DetectEncoding(string filePath)
	{
		try
		{
			using var file = File.Open(filePath, FileMode.Open);
			Span<byte> bom = stackalloc byte[3];
			_ = file.Read(bom);
			file.Close();
			if (bom.SequenceEqual<byte>([0xEF, 0xBB, 0xBF]))
			{
				return UTF8BOMEncoding;
			}
			//read using UTF8
			using var sr = new StreamReader(MinorShift.Emuera.Compat.CasePath.Resolve(filePath), UTF8Encoding);
			//Peek for detecting any BOM encoding
			sr.Peek();
			//If any BOM was detected durig Peek(), sr.CurrentEncoding won't be the same 
			//as the encoding we passed as a argument
			if (!UTF8Encoding.Equals(sr.CurrentEncoding))
			{
				return sr.CurrentEncoding;
			}
			//Here the used encoding is still UTF8, if it detects any 
			//invalid byte, it means its not UTF8 in which case we assume its SHIFT-JIS
			sr.ReadToEnd();
			sr.Dispose();
			return UTF8Encoding;
		}
		catch
		{
			using var legacy = File.OpenRead(filePath);
			return DetectLegacyEncoding(legacy, 0);
		}
	}

	public static Encoding DetectEncoding(Stream stream)
	{
		var pos = stream.Position;
		try
		{
			using var sr = new StreamReader(stream, UTF8Encoding, true, -1, true);
			sr.Peek();
			if (!UTF8Encoding.Equals(sr.CurrentEncoding))
			{
				stream.Seek(pos, SeekOrigin.Begin);
				return sr.CurrentEncoding;
			}
			sr.ReadToEnd();
			sr.Dispose();
			stream.Seek(pos, SeekOrigin.Begin);
			return UTF8Encoding;
		}
		catch
		{
			stream.Seek(pos, SeekOrigin.Begin);
			return DetectLegacyEncoding(stream, pos);
		}
	}
	// Some Chinese distributions contain GB18030 files alongside UTF-8 and SJIS.
	// Validate fallbacks strictly; never silently turn script identifiers into '?'.
	static Encoding DetectLegacyEncoding(Stream stream, long position)
	{
		foreach (var encoding in new[] { shiftjisEncoding, GetEncoding(54936), GetEncoding(950) })
		{
			try
			{
				stream.Position = position;
				using var reader = new StreamReader(stream, encoding, true, 4096, true);
				reader.ReadToEnd();
				return encoding;
			}
			catch (DecoderFallbackException) { }
			finally { stream.Position = position; }
		}
		throw new DecoderFallbackException("Unsupported or damaged script text encoding.");
	}
	public static Encoding GetEncoding(int codePage)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
	}
}

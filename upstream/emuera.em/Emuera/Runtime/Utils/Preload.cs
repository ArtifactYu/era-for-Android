using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Utils;
static partial class Preload
{
	static Dictionary<string, string[]> files = new(StringComparer.OrdinalIgnoreCase);

	public static string[] GetFileLines(string path)
	{
		return files[path];
	}

	// Opens as UTF8BOM if starts with BOM, else use DetectEncoding
	private static string[] readAllLinesDetectEncoding(string path)
	{
		try
		{
			using var file = File.Open(path, FileMode.Open);
			Span<byte> bom = stackalloc byte[3];
			_ = file.Read(bom);
			file.Close();
			try
			{
				if (bom.SequenceEqual<byte>([0xEF, 0xBB, 0xBF]))
				{
					return File.ReadAllLines(path, EncodingHandler.UTF8BOMEncoding);
				}
				else
				{
					return File.ReadAllLines(path, EncodingHandler.DetectEncoding(path));
				}
			}
			catch
			{
				ParserMediator.Warn(trerror.AbnormalEncode.Text, new ScriptPosition(path, 0), 0, "");
				return null;
			}
		}
		catch (IOException)
		{
			ParserMediator.Warn(string.Format(trerror.FileUsingOtherProcess.Text, path), new ScriptPosition(path, 0), 0, "");
			return File.ReadAllLines(path, EncodingHandler.UTF8BOMEncoding);
		}
	}

	public static async Task Load(string path)
	{
		var startTime = DateTime.Now;
		Debug.WriteLine($"Load: {path} : Start");

		var dir = new DirectoryInfo(MinorShift.Emuera.Compat.CasePath.Resolve(path));
		if (dir.Exists)
		{
			await Task.Run(() =>
			{
				dir.EnumerateFiles("*", SearchOption.AllDirectories)
				.AsParallel()
				.Where(x =>
				{
					var ext = x.Extension;
					return ext.Equals(".csv", StringComparison.OrdinalIgnoreCase) ||
							ext.Equals(".erb", StringComparison.OrdinalIgnoreCase) ||
							ext.Equals(".erh", StringComparison.OrdinalIgnoreCase) ||
							ext.Equals(".erd", StringComparison.OrdinalIgnoreCase) ||
							ext.Equals(".als", StringComparison.OrdinalIgnoreCase);
				}).ForAll((childPath) =>
				{
					var key = childPath;
					var value = readAllLinesDetectEncoding(childPath.ToString());
					lock (files)
					{
						files[key.ToString()] = value;
					}

				});
			});
		}
		else
		{
			var key = path;
			var value = readAllLinesDetectEncoding(path);
			lock (files)
			{
				files[key] = value;
			}
		};

		Debug.WriteLine($"Load: {path} : End in {(DateTime.Now - startTime).TotalMilliseconds}ms");
	}

	public static async Task Load(IEnumerable<string> paths)
	{
		foreach (var path in paths)
		{
			await Load(path);
		}
	}

	public static void Clear()
	{
		files.Clear();
	}
}

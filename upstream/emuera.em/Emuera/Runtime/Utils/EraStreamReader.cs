using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Parser;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Sub;

internal sealed partial class EraStreamReader : IDisposable
{
	public EraStreamReader(bool useRename)
	{
		this.useRename = useRename;
	}

	string filepath;
	string filename;
	readonly bool useRename;
	int curNo;
	int nextNo = 1;
	string[] _fileLines;
	public bool Open(string path)
	{
		return Open(path, Path.GetFileName(path));
	}

	public bool Open(string path, string name)
	{
		//そんなお行儀の悪いことはしていない
		//if (disposed)
		//    throw new ExeEE("破棄したオブジェクトを再利用しようとした");
		//if ((reader != null) || (stream != null) || (filepath != null))
		//    throw new ExeEE("使用中のオブジェクトを別用途に再利用しようとした");
		filepath = path;
		filename = name;
		curNo = 0;
		nextNo = 0;
		try
		{
			_fileLines = File.ReadAllLines(filepath, EncodingHandler.DetectEncoding(path));
		}
		catch
		{
			Dispose();
			return false;
		}
		return true;
	}
	public bool OpenOnCache(string path)
	{

		return OpenOnCache(path, Path.GetFileName(path));
	}

	public bool OpenOnCache(string path, string name)
	{
		filepath = path.ToString();
		filename = name.ToString();
		curNo = 0;
		nextNo = 0;
		_fileLines = Preload.GetFileLines(path);
		return _fileLines != null;
	}


	public string ReadLine()
	{
		string ret = null;
		curNo = nextNo;
		if (_fileLines.Length > curNo)
		{
			ret = _fileLines[curNo];
			nextNo++;
		}
		return ret;
	}

	[GeneratedRegex(@"\[\[.*?\]\]")]
	private static partial Regex regexRenameIdentifer();

	/// <summary>
	/// 次の有効な行を読む。LexicalAnalyzer経由でConfigを参照するのでConfig完成までつかわないこと。
	/// </summary>
	public CharStream ReadEnabledLine(bool disabled = false)
	{
		string line;
		CharStream st;
		while (true)
		{
			line = ReadLine();
			if (line == null)
				return null;
			if (line.Length == 0)
				continue;

			//Ordinal消して大丈夫なのかわからないのでコメントアウト
			//if (useRename && (line.IndexOf("[[", StringComparison.Ordinal) >= 0) && (line.IndexOf("]]", StringComparison.Ordinal) >= 0))
			// andEmuera: 正規表現は @"\[\[.*?\]\]" なので "[[" を含まない行は絶対にマッチしない。
			// _Rename.csv を使う構成では ERB の全行 (実データで 845,461 行) に Match が走るため、
			// 先に弾く。判定は Ordinal 固定なのでカルチャの影響も受けない
			if (useRename && line.Contains("[[", StringComparison.Ordinal))
			{
				var match = regexRenameIdentifer().Match(line);
				while (match.Success)
				{
					//この段階でマッチしないパターンもある
					if (ParserMediator.RenameDic.TryGetValue(match.Value, out var targetStr))
					{
						line = line.Replace(match.Value, targetStr);
					}

					match = match.NextMatch();
				}
			}
			st = new CharStream(line);
			LexicalAnalyzer.SkipWhiteSpace(st);
			if (st.EOS)
				continue;
			//[SKIPSTART]～[SKIPEND]中にここが誤爆するので無効化
			if (!disabled)
			{
				if (st.Current == '}')
					throw new CodeEE(trerror.UnexpectedContinuationEnd.Text, new ScriptPosition(filename, curNo));
				if (st.Current == '{')
				{
					if (line.Trim() != "{")
						throw new CodeEE(trerror.CharacterAfterContinuation.Text, new ScriptPosition(filename, curNo));
					break;
				}
			}
			return st;
		}
		//curNoはこの後加算しない(始端記号の行を行番号とする)
		StringBuilder b = new();
		while (true)
		{
			line = ReadLine();
			if (line == null)
			{
				throw new CodeEE(trerror.NotCloseLineContinuation.Text, new ScriptPosition(filename, curNo));
			}

			//if (useRename && (line.IndexOf("[[", StringComparison.Ordinal) >= 0) && (line.IndexOf("]]", StringComparison.Ordinal) >= 0))
			// andEmuera: 上に同じ (事前ふるい)
			if (useRename && line.Contains("[[", StringComparison.Ordinal))
			{
				//この段階でマッチしないパターンもある
				var match = regexRenameIdentifer().Match(line);
				while (match.Success)
				{
					if (ParserMediator.RenameDic.TryGetValue(match.Value, out var targetStr))
					{
						line = line.Replace(match.Value, targetStr);
					}

					match = match.NextMatch();
				}
			}
			var test = line.AsSpan().TrimStart();
			if (test.Length > 0)
			{
				if (test[0] == '}')
				{
					if (!test.TrimEnd().SequenceEqual("}"))
						throw new CodeEE(trerror.CharacterAfterContinuationEnd.Text, new ScriptPosition(filename, curNo));
					break;
				}
				//行連結文字なら1字でないとおかしい、というか、こうしないとFORMの数値変数処理が誤爆する。
				//{
				//A}
				//みたいなどうしようもないコードは知ったこっちゃない
				if (test.SequenceEqual("{"))
					throw new CodeEE(trerror.UnexpectedContinuation.Text, new ScriptPosition(filename, curNo));
			}
			b.Append(line);
			b.Append(Config.ReplaceContinuationBR.Replace("\"", ""));
		}
		st = new CharStream(b.ToString());
		LexicalAnalyzer.SkipWhiteSpace(st);
		return st;
	}

	/// <summary>
	/// 直前に読んだ行の行番号
	/// </summary>
	public int LineNo
	{ get { return curNo; } }
	public string Filename
	{
		get
		{
			return filename;
		}
	}
	//public string Filepath
	//{
	//    get
	//    {
	//        return filepath;
	//    }
	//}

	public void Close() { Dispose(); }
	bool disposed;
	#region IDisposable メンバ

	public void Dispose()
	{
		if (disposed)
			return;
		filepath = null;
		filename = null;
		disposed = true;
		_fileLines = null;
	}

	#endregion
}

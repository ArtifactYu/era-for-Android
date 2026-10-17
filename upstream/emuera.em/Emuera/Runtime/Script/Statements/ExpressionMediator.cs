using Microsoft.VisualBasic;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System.Text;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Script.Statements;

//1756 元ExpressionEvaluator。GetValueの仕事はなくなったので改名。
//IOperandTerm間での通信や共通の処理に使う。
//変数が絡む仕事はVariableEvaluatorへ。
internal sealed class ExpressionMediator
{
	public ExpressionMediator(Process proc, VariableEvaluator vev, EmueraConsole console)
	{
		VEvaluator = vev;
		Process = proc;
		Console = console;
	}
	public readonly VariableEvaluator VEvaluator;
	public readonly Process Process;
	public readonly EmueraConsole Console;



	private bool forceHiragana;
	private bool forceKatakana;
	private bool halftoFull;

	public void ForceKana(long flag)
	{
		if (flag < 0 || flag > 3)
			throw new CodeEE(trerror.OoRForcekanaArg.Text);
		forceKatakana = flag == 1;
		forceHiragana = flag > 1;
		halftoFull = flag == 3;
	}

	public bool ForceKana()
	{
		return forceHiragana | forceKatakana | halftoFull;
	}

	public void OutputToConsole(string str, FunctionIdentifier func, bool lineEnd)
	{
		if (func.IsPrintSingle())
			Console.PrintSingleLine(str, false);
		else
		{
			Console.Print(str, lineEnd);
			if (func.IsNewLine() || func.IsWaitInput())
			{
				Console.NewLine();
				if (func.IsWaitInput())
					Console.ReadAnyKey();
			}
		}
		Console.UseSetColorStyle = true;
	}

	public string ConvertStringType(string str)
	{
		if (!(forceHiragana | forceKatakana | halftoFull))
			return str;
		//andEmuera: Strings.StrConv は Windows の NLS API 依存で Android では例外になるため自前実装に置き換え
		if (forceKatakana)
			return JapaneseText.ToKatakana(str);
		else if (forceHiragana)
		{
			if (halftoFull)
				return JapaneseText.ToHiragana(JapaneseText.ToWide(str));
			else
				return JapaneseText.ToHiragana(str);
		}
		return str;
	}

	public static string CheckEscape(string str)
	{
		CharStream st = new(str);
		StringBuilder buffer = new();

		while (!st.EOS)
		{
			//エスケープ文字の使用
			if (st.Current == '\\')
			{
				st.ShiftNext();
				switch (st.Current)
				{
					case '\\':
						buffer.Append('\\');
						buffer.Append('\\');
						break;
					case '{':
					case '}':
					case '%':
					case '@':
						buffer.Append('\\');
						buffer.Append(st.Current);
						break;
					default:
						buffer.Append("\\\\");
						buffer.Append(st.Current);
						break;
				}
				st.ShiftNext();
				continue;
			}
			buffer.Append(st.Current);
			st.ShiftNext();
		}
		return buffer.ToString();
	}

	public static string CreateBar(long var, long max, long length)
	{
		if (max <= 0)
			throw new CodeEE(trerror.MaxBarNotPositive.Text);
		if (length <= 0)
			throw new CodeEE(trerror.BarNotPositive.Text);
		if (length >= 100)//暴走を防ぐため。
			throw new CodeEE(trerror.TooLongBar.Text);
		StringBuilder builder = new();
		builder.Append('[');
		int count;
		unchecked
		{
			count = (int)(var * length / max);
		}
		if (count < 0)
			count = 0;
		if (count > length)
			count = (int)length;
		builder.Append(Config.Config.BarChar1, count);
		builder.Append(Config.Config.BarChar2, (int)length - count);
		builder.Append(']');
		return builder.ToString();
	}
}

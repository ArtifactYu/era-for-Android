//統合_netEmuera: .netEmuera (gitlab.com/alnatiyan/EmueraDotNet) の MATCHALL 命令を移植したもの。
//上流の Instraction.Child.cs を触らずに済むよう、partial の別ファイルとして置いている。
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Runtime.Script;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System;

namespace MinorShift.Emuera.GameProc.Function;

internal sealed partial class FunctionIdentifier
{
	/// <summary>
	/// MATCHALL &lt;変数&gt;（, &lt;キャラ変数の添字&gt;）, &lt;探す値&gt;（, &lt;開始&gt;（, &lt;終了&gt;））
	/// 一致した要素数を RESULT:0 に、その添字を RESULT:1 以降に格納する。
	/// RESULT の長さを超えた分は件数にだけ数えて捨てる。
	/// </summary>
	private sealed class MATCHALL_Instruction : AInstruction
	{
		public MATCHALL_Instruction()
		{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_MATCHALL);
			flag = EXTENDED | METHOD_SAFE;
		}

		public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
		{
			SpMatchAllArgument arg = (SpMatchAllArgument)func.Argument;
			VariableToken token = arg.Token;
			AExpression valExpr = arg.Value;
			Type type = valExpr.GetOperandType();

			//キャラクタ変数はキャラ番号を、それ以外は配列の添字を走査する
			long len;
			if (token.IsCharacterData)
			{
				if (token.IsArray2D || token.IsArray3D)
					throw new ExeEE("type error");
				len = exm.VEvaluator.CHARANUM;
			}
			else
			{
				if (!token.IsArray1D)
					throw new ExeEE("type error");
				len = token.GetLength(0);
			}

			long beg = arg.Beg?.GetIntValue(exm) ?? 0;
			long end = arg.End?.GetIntValue(exm) ?? len;
			if (beg < 0 || end < 0)
				throw new CodeEE("検索範囲に負の値が渡されました");
			if (beg > end)
				throw new CodeEE("検索範囲の指定が不正です");
			if (Math.Max(beg, end) > len)
				throw new CodeEE("検索範囲が変数のサイズを超えています");

			long[] arr = exm.VEvaluator.RESULT_ARRAY;
			Span<long> output = arr.AsSpan()[1..];

			//idxs[0] が走査対象。1次元キャラクタ変数のときだけ idxs[1] に変数側の添字を固定する
			long[] idxs = new long[2];
			if (arg.Index.HasValue)
				idxs[1] = arg.Index.Value;

			int count = 0;
			if (type == typeof(long))
			{
				long val = valExpr.GetIntValue(exm);
				for (long i = beg; i < end; i++)
				{
					idxs[0] = i;
					if (val == token.GetIntValue(exm, idxs))
					{
						if (output.Length > count)
							output[count] = i;
						++count;
					}
				}
			}
			else if (type == typeof(string))
			{
				string val = valExpr.GetStrValue(exm);
				for (long i = beg; i < end; i++)
				{
					idxs[0] = i;
					if (val == token.GetStrValue(exm, idxs))
					{
						if (output.Length > count)
							output[count] = i;
						++count;
					}
				}
			}
			else
				throw new ExeEE("unknown type");

			arr[0] = count;
		}
	}
}

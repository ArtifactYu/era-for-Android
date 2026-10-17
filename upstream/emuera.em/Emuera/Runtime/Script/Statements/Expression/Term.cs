using MinorShift.Emuera.Runtime.Script.Data;
using System;

namespace MinorShift.Emuera.Runtime.Script.Statements.Expression;

internal sealed class NullTerm : AExpression
{
	public NullTerm(long i)
		: base(typeof(long))
	{
	}

	public NullTerm(string s)
		: base(typeof(string))
	{
	}
}

/// <summary>
/// 項。一単語だけ。
/// </summary>
internal class SingleTerm : AExpression
{
	protected SingleTerm(Type type)
		: base(type)
	{

	}

	public override AExpression Restructure(ExpressionMediator exm)
	{
		return this;
	}
}

internal sealed class SingleStrTerm : SingleTerm
{
	public SingleStrTerm(string s)
		: base(typeof(string))
	{
		sValue = s;
	}
	string sValue;
	public override string GetStrValue(ExpressionMediator exm)
	{
		return sValue;
	}
	public override SingleTerm GetValue(ExpressionMediator exm)
	{
		return this;
	}
	public string Str
	{
		get
		{
			//チェック済みの上での呼び出し
			//if (type != typeof(string))
			//    throw new ExeEE("項の種別が異常");
			return sValue;
		}
	}
	public override string ToString()
	{
		return sValue.ToString();
	}
}


internal sealed class SingleLongTerm : SingleTerm
{
	public SingleLongTerm(long i)
		: base(typeof(long))
	{
		iValue = i;
	}
	readonly long iValue;

	public override long GetIntValue(ExpressionMediator exm)
	{
		return iValue;
	}
	public override SingleTerm GetValue(ExpressionMediator exm)
	{
		return this;
	}

	public long Int
	{
		get
		{
			//チェック済みの上での呼び出し
			//if (type != typeof(Int64))
			//    throw new ExeEE("項の種別が異常");
			return iValue;
		}
	}
	public override string ToString()
	{
		return iValue.ToString();
	}

	public override AExpression Restructure(ExpressionMediator exm)
	{
		return this;
	}
}


/// <summary>
/// 項。一単語だけ。
/// </summary>
internal sealed class StrFormTerm : AExpression
{
	public StrFormTerm(StrForm sf)
		: base(typeof(string))
	{
		sfValue = sf;
	}
	readonly StrForm sfValue;

	public StrForm StrForm
	{
		get
		{
			return sfValue;
		}
	}

	public override string GetStrValue(ExpressionMediator exm)
	{
		return sfValue.GetString(exm);
	}
	public override SingleTerm GetValue(ExpressionMediator exm)
	{
		return new SingleStrTerm(sfValue.GetString(exm));
	}

	public override AExpression Restructure(ExpressionMediator exm)
	{
		sfValue.Restructure(exm);
		if (sfValue.IsConst)
			return new SingleStrTerm(sfValue.GetString(exm));
		AExpression term = sfValue.GetAExpression();
		if (term != null)
			return term;
		return this;
	}
}


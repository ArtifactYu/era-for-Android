using System;

namespace MinorShift.Emuera.Runtime.Script.Statements.Expression;

internal abstract class AExpression
{
	public AExpression(Type t)
	{
		type = t;
	}
	public Type GetOperandType()
	{
		return type;
	}

	public virtual long GetIntValue(ExpressionMediator exm)
	{
		return 0;
	}
	public virtual string GetStrValue(ExpressionMediator exm)
	{
		return "";
	}
	public virtual SingleTerm GetValue(ExpressionMediator exm)
	{
		if (type == typeof(long))
			return new SingleLongTerm(0);
		else
			return new SingleStrTerm("");
	}
	public bool IsInteger
	{
		get { return type == typeof(long); }
	}
	public bool IsString
	{
		get { return type == typeof(string); }
	}
	readonly Type type;

	/// <summary>
	/// 定数を解体して可能ならSingleTerm化する
	/// defineの都合上、2回以上呼ばれる可能性がある
	/// </summary>
	public virtual AExpression Restructure(ExpressionMediator exm)
	{
		return this;
	}
}

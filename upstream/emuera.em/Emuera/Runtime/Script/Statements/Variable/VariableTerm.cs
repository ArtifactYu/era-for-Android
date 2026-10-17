using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Utils;
using System;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Script.Statements.Variable;

internal class VariableTerm : AExpression
{
	protected VariableTerm(VariableToken token) : base(token.VariableType) { }
	public VariableTerm(VariableToken token, AExpression[] args)
		: base(token.VariableType)
	{
		Identifier = token;
		arguments = args;
		transporter = new long[arguments.Length];

		allArgIsConst = true;
		for (int i = 0; i < arguments.Length; i++)
		{
			if (arguments[i] is SingleLongTerm singleTerm)
			{
				transporter[i] = singleTerm.Int;
			}
			else
			{
				allArgIsConst = false;
			}
		}
	}
	public VariableToken Identifier;
	private readonly AExpression[] arguments;
	protected long[] transporter;
	protected bool allArgIsConst;

	public long GetElementInt(int i, ExpressionMediator exm)
	{
		if (allArgIsConst)
			return transporter[i];
		return arguments[i].GetIntValue(exm);
	}

	public bool isAllConst { get { return allArgIsConst; } }
	public int getEl1forArg { get { return (int)transporter[0]; } }

	public override long GetIntValue(ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			return Identifier.GetIntValue(exm, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}
	public override string GetStrValue(ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			string ret = Identifier.GetStrValue(exm, transporter);
			if (ret == null)
				return "";
			return ret;
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}
	public virtual void SetValue(long value, ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			Identifier.SetValue(value, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}
	public virtual void SetValue(string value, ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			Identifier.SetValue(value, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}

	public virtual void SetValue(long[] array, ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			Identifier.SetValue(array, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
			{
				Identifier.CheckElement(transporter);
				throw new CodeEE(string.Format(trerror.AssignToVarOoR.Text, Identifier.Name));
			}
			throw;
		}
	}
	public virtual void SetValue(string[] array, ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			Identifier.SetValue(array, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
			{
				Identifier.CheckElement(transporter);
				throw new CodeEE(string.Format(trerror.AssignToVarOoR.Text, Identifier.Name));
			}
			throw;
		}
	}

	public virtual long ChangeValue(long value, ExpressionMediator exm)
	{
		try
		{
			if (!allArgIsConst)
				for (int i = 0; i < arguments.Length; i++)
					transporter[i] = arguments[i].GetIntValue(exm);
			return Identifier.PlusValue(value, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}
	public override SingleTerm GetValue(ExpressionMediator exm)
	{
		if (Identifier.VariableType == typeof(long))
			return new SingleLongTerm(GetIntValue(exm));
		else
			return new SingleStrTerm(GetStrValue(exm));
	}
	public virtual void SetValue(SingleTerm value, ExpressionMediator exm)
	{
		if (value is SingleLongTerm singleLongTerm)
			SetValue(singleLongTerm.Int, exm);
		else
			SetValue(((SingleStrTerm)value).Str, exm);
	}
	public virtual void SetValue(AExpression value, ExpressionMediator exm)
	{
		if (Identifier.VariableType == typeof(long))
			SetValue(value.GetIntValue(exm), exm);
		else
			SetValue(value.GetStrValue(exm), exm);
	}
	public int GetLength()
	{
		return Identifier.GetLength();
	}
	public int GetLength(int dimension)
	{
		return Identifier.GetLength(dimension);
	}
	public int GetLastLength()
	{
		if (Identifier.IsArray1D)
			return Identifier.GetLength();
		else if (Identifier.IsArray2D)
			return Identifier.GetLength(1);
		else if (Identifier.IsArray3D)
			return Identifier.GetLength(2);
		return 0;
	}

	public virtual FixedVariableTerm GetFixedVariableTerm(ExpressionMediator exm)
	{
		if (!allArgIsConst)
			for (int i = 0; i < arguments.Length; i++)
				transporter[i] = arguments[i].GetIntValue(exm);
		FixedVariableTerm fp = new(Identifier);
		if (transporter.Length >= 1)
			fp.Index1 = transporter[0];
		if (transporter.Length >= 2)
			fp.Index2 = transporter[1];
		if (transporter.Length >= 3)
			fp.Index3 = transporter[2];
		return fp;
	}

	public override AExpression Restructure(ExpressionMediator exm)
	{
		bool[] canCheck = new bool[arguments.Length];
		allArgIsConst = true;
		for (int i = 0; i < arguments.Length; i++)
		{
			arguments[i] = arguments[i].Restructure(exm);
			if (!(arguments[i] is SingleTerm))
			{
				allArgIsConst = false;
				canCheck[i] = false;
			}
			else
			{
				//キャラクターデータの第1引数はこの時点でチェックしても意味がないのと
				//ARG系は限界超えてても必要な数に拡張されるのでチェックしなくていい
				if (i == 0 && Identifier.IsCharacterData || Identifier.Name == "ARG" || Identifier.Name == "ARGS")
					canCheck[i] = false;
				else
					canCheck[i] = true;
				//if (allArgIsConst)
				//チェックのために値が必要
				transporter[i] = arguments[i].GetIntValue(exm);
			}
		}
		if (!Identifier.IsReference)
			Identifier.CheckElement(transporter, canCheck);
		if (Identifier.CanRestructure && allArgIsConst)
			return GetValue(exm);
		else if (allArgIsConst)
			return new FixedVariableTerm(Identifier, transporter);
		return this;
	}

	//以下添え字解析用の追加関数
	public bool checkSameTerm(VariableTerm term)
	{
		//添え字が全部定数があることがこの関数の前提(そもそもそうでないと使い道がない)
		if (!allArgIsConst)
			return false;
		if (Identifier.Name != term.Identifier.Name)
			return false;
		else
		{
			for (int i = 0; i < transporter.Length; i++)
			{
				if (transporter[i] != term.transporter[i])
					return false;
			}
		}
		return true;
	}

	public string GetFullString()
	{
		//添え字が全部定数があることがこの関数の前提(IOperandTermから変数名を取れないため)
		if (!allArgIsConst)
			return "";
		if (Identifier.IsArray1D)
			return Identifier.Name + ":" + transporter[0].ToString();
		else if (Identifier.IsArray2D)
			return Identifier.Name + ":" + transporter[0].ToString() + ":" + transporter[1].ToString();
		else if (Identifier.IsArray3D)
			return Identifier.Name + ":" + transporter[0].ToString() + ":" + transporter[1].ToString() + ":" + transporter[2].ToString();
		else
			return Identifier.Name;
	}
}


internal sealed class FixedVariableTerm : VariableTerm
{
	public FixedVariableTerm(VariableToken token)
		: base(token)
	{
		Identifier = token;
		transporter = new long[3];
		allArgIsConst = true;
	}
	public FixedVariableTerm(VariableToken token, long[] args)
		: base(token)
	{
		allArgIsConst = true;
		Identifier = token;
		transporter = new long[3];
		for (int i = 0; i < args.Length; i++)
			transporter[i] = args[i];
	}
	public long Index1 { get { return transporter[0]; } set { transporter[0] = value; } }
	public long Index2 { get { return transporter[1]; } set { transporter[1] = value; } }
	public long Index3 { get { return transporter[2]; } set { transporter[2] = value; } }


	public override long GetIntValue(ExpressionMediator exm)
	{
		try
		{
			return Identifier.GetIntValue(exm, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}
	public override string GetStrValue(ExpressionMediator exm)
	{
		try
		{
			string ret = Identifier.GetStrValue(exm, transporter);
			if (ret == null)
				return "";
			return ret;
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}

	public override void SetValue(long value, ExpressionMediator exm)
	{
		try
		{
			Identifier.SetValue(value, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}
	public override void SetValue(string value, ExpressionMediator exm)
	{
		try
		{
			Identifier.SetValue(value, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}

	public override long ChangeValue(long value, ExpressionMediator exm)
	{
		try
		{
			return Identifier.PlusValue(value, transporter);
		}
		catch (Exception e)
		{
			if (e is IndexOutOfRangeException || e is ArgumentOutOfRangeException || e is OverflowException)
				Identifier.CheckElement(transporter);
			throw;
		}
	}

	public override AExpression Restructure(ExpressionMediator exm)
	{
		if (Identifier.CanRestructure)
			return GetValue(exm);
		return this;
	}

	public void IsArrayRangeValid(long index1, long index2, string funcName, long i1, long i2)
	{
		Identifier.IsArrayRangeValid(transporter, index1, index2, funcName, i1, i2);
	}
}


/// <summary>
/// 引数がない変数。値を参照、代入できない
/// </summary>
internal sealed class VariableNoArgTerm : VariableTerm
{
	public VariableNoArgTerm(VariableToken token)
		: base(token)
	{
		Identifier = token;
		allArgIsConst = true;
	}
	public override long GetIntValue(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override string GetStrValue(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override void SetValue(long value, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override void SetValue(string value, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override void SetValue(long[] array, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override void SetValue(string[] array, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override long ChangeValue(long value, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override SingleTerm GetValue(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override void SetValue(SingleTerm value, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override void SetValue(AExpression value, ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }
	public override FixedVariableTerm GetFixedVariableTerm(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.MissingVarArg.Text, Identifier.Name)); }

	public override AExpression Restructure(ExpressionMediator exm)
	{
		return this;
	}
}

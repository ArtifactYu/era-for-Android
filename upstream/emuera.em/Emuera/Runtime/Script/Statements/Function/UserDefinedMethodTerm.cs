using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Script.Statements.Function;

internal abstract class SuperUserDefinedMethodTerm : AExpression
{
	protected SuperUserDefinedMethodTerm(Type returnType)
		: base(returnType)
	{
	}
	public abstract UserDefinedFunctionArgument Argument { get; }
	public abstract CalledFunction Call { get; }
	public override long GetIntValue(ExpressionMediator exm)
	{
		if (exm.Process.GetValue(this) is not SingleLongTerm term)
			return 0;
		return term.Int;
	}
	public override string GetStrValue(ExpressionMediator exm)
	{
		if (exm.Process.GetValue(this) is not SingleStrTerm term)
			return "";
		return term.Str;
	}
	public override SingleTerm GetValue(ExpressionMediator exm)
	{
		SingleTerm term = exm.Process.GetValue(this);
		if (term == null)
		{
			if (GetOperandType() == typeof(long))
				return new SingleLongTerm(0);
			else
				return new SingleStrTerm("");
		}
		return term;
	}
}

internal sealed class UserDefinedMethodTerm : SuperUserDefinedMethodTerm
{

	/// <summary>
	/// エラーならnullを返す。
	/// </summary>
	public static UserDefinedMethodTerm Create(FunctionLabelLine targetLabel, List<AExpression> srcArgs, out string errMes)
	{
		CalledFunction call = CalledFunction.CreateCalledFunctionMethod(targetLabel, targetLabel.LabelName);
		UserDefinedFunctionArgument arg = call.ConvertArg(srcArgs, out errMes);
		if (arg == null)
			return null;
		return new UserDefinedMethodTerm(arg, call.TopLabel.MethodType, call);
	}

	private UserDefinedMethodTerm(UserDefinedFunctionArgument arg, Type returnType, CalledFunction call)
		: base(returnType)
	{
		argment = arg;
		called = call;
	}
	public override UserDefinedFunctionArgument Argument { get { return argment; } }
	public override CalledFunction Call { get { return called; } }
	private readonly UserDefinedFunctionArgument argment;
	private readonly CalledFunction called;

	public override AExpression Restructure(ExpressionMediator exm)
	{
		Argument.Restructure(exm);
		return this;
	}



}
internal sealed class UserDefinedRefMethodTerm : SuperUserDefinedMethodTerm
{
	public UserDefinedRefMethodTerm(UserDefinedRefMethod reffunc, List<AExpression> srcArgs)
		: base(reffunc.RetType)
	{
		this.srcArgs = srcArgs;
		this.reffunc = reffunc;
	}
	List<AExpression> srcArgs;
	readonly UserDefinedRefMethod reffunc;
	public override UserDefinedFunctionArgument Argument
	{
		get
		{
			if (reffunc.CalledFunction == null)
				throw new CodeEE(string.Format(trerror.EmptyRefFunc.Text, reffunc.Name));
			UserDefinedFunctionArgument arg = reffunc.CalledFunction.ConvertArg(srcArgs, out string errMes);
			if (arg == null)
				throw new CodeEE(errMes);
			return arg;
		}
	}
	public override CalledFunction Call
	{
		get
		{
			if (reffunc.CalledFunction == null)
				throw new CodeEE(string.Format(trerror.EmptyRefFunc.Text, reffunc.Name));
			return reffunc.CalledFunction;
		}
	}

	public override AExpression Restructure(ExpressionMediator exm)
	{
		for (int i = 0; i < srcArgs.Count; i++)
		{
			if ((reffunc.ArgTypeList[i] & UserDifinedFunctionDataArgType.__Ref) == UserDifinedFunctionDataArgType.__Ref)
				srcArgs[i].Restructure(exm);
			else
				srcArgs[i] = srcArgs[i].Restructure(exm);
		}
		return this;
	}


}

internal sealed class UserDefinedRefMethodNoArgTerm : SuperUserDefinedMethodTerm
{
	public UserDefinedRefMethodNoArgTerm(UserDefinedRefMethod reffunc)
		: base(reffunc.RetType)
	{
		this.reffunc = reffunc;
	}
	readonly UserDefinedRefMethod reffunc;
	public override UserDefinedFunctionArgument Argument
	{ get { throw new CodeEE(string.Format(trerror.RefFuncHasNotArg.Text, reffunc.Name)); } }
	public override CalledFunction Call
	{ get { throw new CodeEE(string.Format(trerror.RefFuncHasNotArg.Text, reffunc.Name)); } }
	public string GetRefName()
	{
		if (reffunc.CalledFunction == null)
			return "";
		return reffunc.CalledFunction.TopLabel.LabelName;
	}
	public override long GetIntValue(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.RefFuncHasNotArg.Text, reffunc.Name)); }
	public override string GetStrValue(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.RefFuncHasNotArg.Text, reffunc.Name)); }
	public override SingleTerm GetValue(ExpressionMediator exm)
	{ throw new CodeEE(string.Format(trerror.RefFuncHasNotArg.Text, reffunc.Name)); }
	public override AExpression Restructure(ExpressionMediator exm)
	{
		return this;
	}
}

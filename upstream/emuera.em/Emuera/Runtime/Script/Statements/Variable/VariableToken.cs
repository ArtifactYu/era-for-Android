using EnumsNET;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.GameData.Variable;

//IndexOutOfRangeException, ArgumentOutOfRangeExceptionを投げることがある。VariableTermの方で処理すること。
//引数は整数しか受け付けない。*.csvを利用した置換はVariableTermの方で処理すること
internal abstract class VariableToken
{
	protected VariableToken(VariableCode varCode, VariableData varData)
	{
		Code = varCode;
		VariableType = ((varCode & VariableCode.__INTEGER__) == VariableCode.__INTEGER__) ? typeof(long) : typeof(string);
		VarCodeInt = (int)(varCode & VariableCode.__LOWERCASE__);
		varName = Enums.AsString(varCode);
		this.varData = varData;
		IsForbid = false;
		IsPrivate = false;
		IsReference = false;
		Dimension = 0;
		IsGlobal = (Code == VariableCode.GLOBAL) || (Code == VariableCode.GLOBALS);
		if ((Code & VariableCode.__ARRAY_1D__) == VariableCode.__ARRAY_1D__)
			Dimension = 1;
		if ((Code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__)
			Dimension = 2;
		if ((Code & VariableCode.__ARRAY_3D__) == VariableCode.__ARRAY_3D__)
			Dimension = 3;


		IsSavedata = false;
		if ((Code == VariableCode.GLOBAL) || (Code == VariableCode.GLOBALS))
			IsSavedata = true;
		else if ((Code & VariableCode.__SAVE_EXTENDED__) == VariableCode.__SAVE_EXTENDED__)
		{
			IsSavedata = true;
		}
		else if (((Code & VariableCode.__EXTENDED__) != VariableCode.__EXTENDED__)
			&& ((Code & VariableCode.__CALC__) != VariableCode.__CALC__)
			&& ((Code & VariableCode.__UNCHANGEABLE__) != VariableCode.__UNCHANGEABLE__)
			&& ((Code & VariableCode.__LOCAL__) != VariableCode.__LOCAL__)
			&& (!varName.StartsWith("NOTUSE_")))
		{
			VariableCode flag = Code & (VariableCode.__ARRAY_1D__ | VariableCode.__ARRAY_2D__ | VariableCode.__ARRAY_3D__ | VariableCode.__STRING__ | VariableCode.__INTEGER__ | VariableCode.__CHARACTER_DATA__);
			switch (flag)
			{
				case VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_INTEGER__)
						IsSavedata = true;
					break;
				case VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_STRING__)
						IsSavedata = true;
					break;
				case VariableCode.__CHARACTER_DATA__ | VariableCode.__INTEGER__ | VariableCode.__ARRAY_1D__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_INTEGER_ARRAY__)
						IsSavedata = true;
					break;
				case VariableCode.__CHARACTER_DATA__ | VariableCode.__STRING__ | VariableCode.__ARRAY_1D__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_CHARACTER_STRING_ARRAY__)
						IsSavedata = true;
					break;
				case VariableCode.__INTEGER__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_INTEGER__)
						IsSavedata = true;
					break;
				case VariableCode.__STRING__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_STRING__)
						IsSavedata = true;
					break;
				case VariableCode.__INTEGER__ | VariableCode.__ARRAY_1D__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_INTEGER_ARRAY__)
						IsSavedata = true;
					break;
				case VariableCode.__STRING__ | VariableCode.__ARRAY_1D__:
					if (VarCodeInt < (int)VariableCode.__COUNT_SAVE_STRING_ARRAY__)
						IsSavedata = true;
					break;
			}
		}
	}

	public readonly VariableCode Code;
	public readonly int VarCodeInt;
	protected readonly VariableData varData;
	protected string varName;
	public Type VariableType { get; protected set; }
	public bool CanRestructure { get; protected set; }
	public string Name { get { return varName; } }


	//CodeEEにしているけど実際はExeEEかもしれない
	public virtual long GetIntValue(ExpressionMediator exm, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallStrAsInt.Text, varName)); }
	public virtual string GetStrValue(ExpressionMediator exm, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallIntAsStr.Text, varName)); }
	public virtual void SetValue(long value, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallStrAsInt.Text, varName)); }
	public virtual void SetValue(string value, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallIntAsStr.Text, varName)); }
	public virtual void SetValue(long[] values, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallNDStrAsInt.Text, varName)); }
	public virtual void SetValue(string[] values, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallNDIntAsStr.Text, varName)); }
	public virtual void SetValueAll(long value, int start, int end, int charaPos)
	{ throw new CodeEE(string.Format(trerror.CallNDStrAsInt.Text, varName)); }
	public virtual void SetValueAll(string value, int start, int end, int charaPos)
	{ throw new CodeEE(string.Format(trerror.CallNDIntAsStr.Text, varName)); }
	public virtual long PlusValue(long value, long[] arguments)
	{ throw new CodeEE(string.Format(trerror.CallStrAsInt.Text, varName)); }
	public virtual int GetLength()
	{ throw new CodeEE(string.Format(trerror.GetSize0DVar.Text, varName)); }
	public virtual int GetLength(int dimension)
	{ throw new CodeEE(string.Format(trerror.GetSize0DVar.Text, varName)); }
	public virtual object GetArray()
	{
		if (IsCharacterData)
			throw new CodeEE(string.Format(trerror.CallCharaVarAsVar.Text, varName));
		throw new CodeEE(string.Format(trerror.GetSize0DVar.Text, varName));
	}
	public virtual object GetArrayChara(int charano)
	{
		if (!IsCharacterData)
			throw new CodeEE(string.Format(trerror.CallVarAsCharaVar.Text, varName));
		throw new CodeEE(string.Format(trerror.GetSize0DVar.Text, varName));
	}

	//andEmuera: 元は呼び出しのたびに [true, true, true] を作っていた。
	//CheckElement は変数の要素アクセスごとに通るエンジン最深部で、
	//プロファイラはここの配列初期化に 530ms / 3,090ms を積んでいた
	//(壁時計は動かなかった＝サンプラの見かけだが、確保が減るぶん端末の GC には効く)。
	//doCheck は読むだけ (どの実装も書き換えない) なので 1 本を共有してよい
	static readonly bool[] checkAllElements = [true, true, true];

	public void throwOutOfRangeException(long[] arguments, Exception e)
	{
		CheckElement(arguments, checkAllElements);
		throw e;
	}
	public virtual void CheckElement(long[] arguments, bool[] doCheck) { }
	public void CheckElement(long[] arguments)
	{
		CheckElement(arguments, checkAllElements);
	}
	public virtual void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
	{
		CheckElement(arguments, checkAllElements);
		return;
	}

	public int CodeInt
	{ get { return VarCodeInt; } }
	public VariableCode CodeFlag
	{ get { return Code & VariableCode.__UPPERCASE__; } }

	public bool IsNull
	{
		get
		{
			return Code == VariableCode.__NULL__;
		}
	}
	public bool IsCharacterData
	{
		get
		{
			return (Code & VariableCode.__CHARACTER_DATA__) == VariableCode.__CHARACTER_DATA__;
		}
	}
	public bool IsInteger
	{
		get
		{
			return (Code & VariableCode.__INTEGER__) == VariableCode.__INTEGER__;
		}
	}
	public bool IsString
	{
		get
		{
			return (Code & VariableCode.__STRING__) == VariableCode.__STRING__;
		}
	}
	public bool IsArray1D
	{
		get
		{
			return (Code & VariableCode.__ARRAY_1D__) == VariableCode.__ARRAY_1D__;
		}
	}
	public bool IsArray2D
	{
		get
		{
			return (Code & VariableCode.__ARRAY_2D__) == VariableCode.__ARRAY_2D__;
		}
	}
	public bool IsArray3D
	{
		get
		{
			return (Code & VariableCode.__ARRAY_3D__) == VariableCode.__ARRAY_3D__;
		}
	}
	/// <summary>
	/// 1810alpha007 諸事情によりReadOnlyからIsConstに改名。
	/// </summary>
	public virtual bool IsConst
	{
		get
		{
			return (Code & VariableCode.__UNCHANGEABLE__) == VariableCode.__UNCHANGEABLE__;
		}
	}
	public bool IsCalc
	{
		get
		{
			return (Code & VariableCode.__CALC__) == VariableCode.__CALC__;
		}
	}
	public bool IsLocal
	{
		get
		{
			return (Code & VariableCode.__LOCAL__) == VariableCode.__LOCAL__;
		}
	}
	public bool CanForbid
	{
		get
		{
			return (Code & VariableCode.__CAN_FORBID__) == VariableCode.__CAN_FORBID__;
		}
	}
	public bool IsForbid { get; protected set; }
	public bool IsPrivate { get; protected set; }
	public bool IsGlobal { get; protected set; }
	public bool IsSavedata { get; protected set; }
	public bool IsReference { get; protected set; }
	public int Dimension { get; protected set; }

}

internal abstract class CharaVariableToken : VariableToken
{
	protected CharaVariableToken(VariableCode varCode, VariableData varData)
		: base(varCode, varData)
	{
		sizes = CharacterData.CharacterVarLength(varCode, varData.Constant);
		if (sizes != null)
		{
			totalSize = 1;
			for (int i = 0; i < sizes.Length; i++)
				totalSize *= sizes[i];
			IsForbid = totalSize == 0;
		}
		IsPrivate = false;
		CanRestructure = false;
	}
	protected int[] sizes;
	protected int totalSize;
	public override int GetLength()
	{
		if (sizes.Length == 1)
			return sizes[0];
		if (sizes.Length == 0)
			throw new CodeEE(string.Format(trerror.GetSize0DCharaVar.Text, varName));
		throw new CodeEE(string.Format(trerror.GetSizeCharaVarWithoutDim.Text, Dimension.ToString(), varName));
	}
	public override int GetLength(int dimension)
	{
		if (sizes.Length == 0)
			throw new CodeEE(string.Format(trerror.GetSize0DCharaVar.Text, varName));
		if (dimension < sizes.Length)
			return sizes[dimension];
		throw new CodeEE(string.Format(trerror.GetSizeCharaVarNonExistDim.Text, varName));
	}
	public override void CheckElement(long[] arguments, bool[] doCheck)
	{
		if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= varData.CharacterList.Count)))
			throw new CodeEE(string.Format(trerror.OoRCharaVarArg.Text, varName, "1", arguments[0].ToString()));
		if (doCheck.Length > 1 && sizes.Length > 0 && doCheck[1] && ((arguments[1] < 0) || (arguments[1] >= sizes[0])))
			throw new CodeEE(string.Format(trerror.OoRCharaVarArg.Text, varName, "2", arguments[1].ToString()));
		if (doCheck.Length > 2 && sizes.Length > 1 && doCheck[2] && ((arguments[2] < 0) || (arguments[2] >= sizes[1])))
			throw new CodeEE(string.Format(trerror.OoRCharaVarArg.Text, varName, "3", arguments[2].ToString()));
	}

	public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
	{
		CheckElement(arguments);
		//CharacterData chara = varData.CharacterList[(int)arguments[0]];
		if ((index1 < 0) || (index1 > sizes[0]))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
		if ((index2 < 0) || (index2 > sizes[0]))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
	}
}

internal abstract class UserDefinedVariableToken : VariableToken
{
	protected UserDefinedVariableToken(VariableCode varCode, UserDefinedVariableData data)
		: base(varCode, null)
	{
		varName = data.Name;
		IsPrivate = data.Private;
		isConst = data.Const;
		sizes = data.Lengths;
		IsGlobal = data.Global;
		IsSavedata = data.Save;
		//Dimension = sizes.Length;
		totalSize = 1;
		for (int i = 0; i < sizes.Length; i++)
			totalSize *= sizes[i];
		IsForbid = totalSize == 0;
		CanRestructure = isConst;
	}

	public abstract void SetDefault();
	protected bool isConst;
	protected int[] sizes;
	protected int totalSize;
	//public bool IsGlobal { get; protected set; }
	//public bool IsSavedata { get; protected set; }
	public override bool IsConst
	{
		get
		{
			return isConst;
		}
	}

	public override int GetLength()
	{
		if (Dimension == 1)
			return sizes[0];
		throw new CodeEE(string.Format(trerror.GetSizeDimError.Text, Dimension.ToString(), varName));
	}

	public override int GetLength(int dimension)
	{
		if (dimension < Dimension)
			return sizes[dimension];
		throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
	}
	public override void CheckElement(long[] arguments, bool[] doCheck)
	{
		//if (array == null)
		//	throw new ExeEE("プライベート変数" + varName + "の配列が用意されていない");

		if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= sizes[0])))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
		if (sizes.Length >= 2 && ((arguments[1] < 0) || (arguments[1] >= sizes[1])))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "2", arguments[1].ToString()));
		if (sizes.Length >= 3 && ((arguments[2] < 0) || (arguments[2] >= sizes[2])))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "3", arguments[2].ToString()));
	}
	void CheckBounds(long i1, long i2, long i3)
	{
		//if (array == null)
		//	throw new ExeEE("プライベート変数" + varName + "の配列が用意されていない");

		if ((i1 < 0) || (i1 >= sizes[0]))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", i1.ToString()));
		if (sizes.Length >= 2 && ((i2 < 0) || (i2 >= sizes[1])))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "2", i2.ToString()));
		if (sizes.Length >= 3 && ((i3 < 0) || (i3 >= sizes[2])))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "3", i3.ToString()));
	}
	public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
	{
		CheckBounds(arguments[0], arguments[1], arguments[2]);
		if ((index1 < 0) || (index1 > sizes[Dimension - 1]))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
		if ((index2 < 0) || (index2 > sizes[Dimension - 1]))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
	}
	public abstract void ScopeIn();
	public abstract void ScopeOut();
	public bool IsStatic { get; protected set; }
}

internal abstract class UserDefinedCharaVariableToken : CharaVariableToken
{
	protected UserDefinedCharaVariableToken(VariableCode varCode, UserDefinedVariableData data, VariableData varData, int arrayIndex)
		: base(varCode, varData)
	{
		ArrayIndex = arrayIndex;
		DimData = data;
		varName = data.Name;
		sizes = data.Lengths;
		IsGlobal = data.Global;
		IsSavedata = data.Save;
		//Dimension = sizes.Length;
		totalSize = 1;
		for (int i = 0; i < sizes.Length; i++)
			totalSize *= sizes[i];
		IsForbid = totalSize == 0;
	}
	readonly public UserDefinedVariableData DimData;
	readonly public int ArrayIndex;
	public override object GetArrayChara(int charano)
	{
		return varData.CharacterList[charano].UserDefCVarDataList[ArrayIndex];
	}

}

//1808beta009 廃止 UserDefinedVariableTokenで一括して扱う
//internal abstract class PrivateVariableToken : UserDefinedVariableToken
//{
//    protected PrivateVariableToken(VariableCode varCode, UserDefinedVariableData data)
//        : base(varCode, data)
//    {
//        IsPrivate = true;
//    }
//}

/// <summary>
/// 1808beta009 追加
/// 参照型。public もあるよ
/// </summary>
internal abstract class ReferenceToken : UserDefinedVariableToken
{
	protected ReferenceToken(VariableCode varCode, UserDefinedVariableData data)
		: base(varCode, data)
	{
		CanRestructure = false;
		IsStatic = !data.Private;
		IsReference = true;
		arrayList = [];
		IsForbid = false;
	}
	protected List<Array> arrayList;
	protected Array array;

	public override void SetDefault()
	{//Defaultのセットは参照元がやるべき
	}
	public override int GetLength()
	{
		if (array == null)
			throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
		if (Dimension != 1)
			throw new CodeEE(string.Format(trerror.GetSizeDimError.Text, Dimension.ToString(), varName));
		return array.Length;
	}

	public override int GetLength(int dimension)
	{
		if (array == null)
			throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
		if (dimension < Dimension)
			return array.GetLength(dimension);
		throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
	}
	public override void CheckElement(long[] arguments, bool[] doCheck)
	{
		if (array == null)
			throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
		if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.GetLength(0))))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
		if (Dimension >= 2 && ((arguments[1] < 0) || (arguments[1] >= array.GetLength(1))))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "2", arguments[1].ToString()));
		if (Dimension >= 3 && ((arguments[2] < 0) || (arguments[2] >= array.GetLength(2))))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "3", arguments[2].ToString()));
	}
	public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
	{
		CheckElement(arguments);
		if ((index1 < 0) || (index1 > array.GetLength(Dimension - 1)))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
		if ((index2 < 0) || (index2 > array.GetLength(Dimension - 1)))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
	}

	int counter;
	public override void ScopeIn()
	{
		if (counter > 0)
			arrayList.Add(array);
		counter++;
		array = null;
	}

	public override void ScopeOut()
	{
		//arrayList.RemoveAt(arrayList.Count - 1);
		if (arrayList.Count > 0)
		{
			array = arrayList[^1];
			arrayList.RemoveAt(arrayList.Count - 1);
		}
		else
			array = null;
		counter--;
	}
	public override object GetArray()
	{
		if (array == null)
			throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
		return array;
	}

	public void SetRef(Array refArray)
	{
		array = refArray;
	}

	/// <summary>
	/// 型が一致するかどうか（参照可能かどうか）
	/// </summary>
	/// <param name="rother"></param>
	/// <returns></returns>
	public bool MatchType(VariableToken rother, bool allowChara, out string errMes)
	{
		errMes = "";
		if (rother == null)
		{ errMes = trerror.CanNotOmitRefToVar.Text; return false; }
		if (rother.IsCalc)
		{ errMes = trerror.CanNotRefPseudoVar.Text; return false; }
		//TODO constの参照
		//if (rother.IsConst != this.isConst)
		if (rother.IsConst)
		{ errMes = trerror.CanNotRefConstVar.Text; return false; }
		//1812 ローカル参照の条件変更
		//ローカルかつDYNAMICなREFはローカル参照できる
		if ((!IsPrivate) && (rother.IsPrivate || rother.IsLocal))
		{ errMes = trerror.CanNotGlobalRefLocalVar.Text; return false; }
		////1810beta002 ローカル参照禁止
		//if ((!rother.IsReference) && (rother.IsPrivate || rother.IsLocal))
		//{ errMes = "ローカル変数は参照できません"; return false; }
		if (rother.IsCharacterData && !allowChara)
		{ errMes = trerror.CanNotRefCharaVar.Text; return false; }
		if (IsInteger != rother.IsInteger)
		{ errMes = trerror.CanNotRefDifferentType.Text; return false; }
		if (Dimension != rother.Dimension)
		{ errMes = trerror.CanNotRefDifferentDim.Text; return false; }
		return true;
	}
}

internal abstract class LocalVariableToken : VariableToken
{
	public LocalVariableToken(VariableCode varCode, VariableData varData, string subId, int size)
		: base(varCode, varData)
	{
		CanRestructure = false;
		subID = subId;
		this.size = size;
	}
	public abstract void SetDefault();
	public abstract void resize(int newSize);
	protected string subID;
	protected int size;
	public override int GetLength()
	{
		return size;
	}
	public override int GetLength(int dimension)
	{
		if (dimension == 0)
			return size;
		throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
	}
	public override void CheckElement(long[] arguments, bool[] doCheck)
	{
		//if (array == null)
		//	throw new ExeEE("プライベート変数" + varName + "の配列が用意されていない");
		if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= size)))
			throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
	}
	public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
	{
		CheckElement(arguments);
		if ((index1 < 0) || (index1 > size))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
		if ((index2 < 0) || (index2 > size))
			throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
	}
}



//サブクラスの詳細はVariableData以外は知らなくてよい
internal sealed partial class VariableData
{
	#region 変数
	private sealed class Int1DVariableToken : VariableToken
	{
		public Int1DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
			array = varData.DataIntegerArray[VarCodeInt];
			IsForbid = array.Length == 0;
		}
		long[] array;
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			array[arguments[0]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			for (int i = start; i < end; i++)
				array[i] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			array[arguments[0]] += value;
			return array[arguments[0]];
		}
		public override int GetLength()
		{ return array.Length; }
		public override int GetLength(int dimension)
		{
			if (dimension == 0)
				return array.Length;
			throw new CodeEE(string.Format(trerror.GetSizeDimError.Text, varName));
		}
		public override object GetArray() { return array; }

		public override void CheckElement(long[] arguments, bool[] doCheck)
		{
			if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.Length)))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
		}
		public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
		{
			CheckElement(arguments);
			if ((index1 < 0) || (index1 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
			if ((index2 < 0) || (index2 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
		}
	}

	private sealed class Int2DVariableToken : VariableToken
	{
		public Int2DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
			array = varData.DataIntegerArray2D[VarCodeInt];
			IsForbid = array.Length == 0;
		}
		long[,] array;
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0], arguments[1]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}
		public override long PlusValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1]] += value;
			return array[arguments[0], arguments[1]];
		}
		public override int GetLength()
		{ throw new CodeEE(string.Format(trerror.GetSizeDimError.Text, "2", varName)); }
		public override int GetLength(int dimension)
		{
			if ((dimension == 0) || (dimension == 1))
				return array.GetLength(dimension);
			throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
		}
		public override object GetArray() { return array; }

		public override void CheckElement(long[] arguments, bool[] doCheck)
		{
			if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.GetLength(0))))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
			if (doCheck[1] && ((arguments[1] < 0) || (arguments[1] >= array.GetLength(1))))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "2", arguments[1].ToString()));
		}
		public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
		{
			CheckElement(arguments);
			if ((index1 < 0) || (index1 > array.GetLength(1)))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
			if ((index2 < 0) || (index2 > array.GetLength(1)))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
		}
	}

	private sealed class Int3DVariableToken : VariableToken
	{
		public Int3DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
			array = varData.DataIntegerArray3D[VarCodeInt];
			IsForbid = array.Length == 0;
		}
		long[,,] array;
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1], arguments[2]] = value;
		}
		public override void SetValue(long[] values, long[] arguments)
		{
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], arguments[1], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						array[i, j, k] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1], arguments[2]] += value;
			return array[arguments[0], arguments[1], arguments[2]];
		}
		public override int GetLength()
		{ throw new CodeEE(string.Format(trerror.GetSizeDimError.Text, "3", varName)); }
		public override int GetLength(int dimension)
		{
			if ((dimension == 0) || (dimension == 1) || (dimension == 2))
				return array.GetLength(dimension);
			throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
		}
		public override object GetArray() { return array; }

		public override void CheckElement(long[] arguments, bool[] doCheck)
		{
			if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.GetLength(0))))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
			if (doCheck[1] && ((arguments[1] < 0) || (arguments[1] >= array.GetLength(1))))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "2", arguments[1].ToString()));
			if (doCheck[2] && ((arguments[2] < 0) || (arguments[2] >= array.GetLength(2))))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "3", arguments[2].ToString()));
		}
		public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
		{
			CheckElement(arguments);
			if ((index1 < 0) || (index1 > array.GetLength(2)))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
			if ((index2 < 0) || (index2 > array.GetLength(2)))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
		}
	}

	private sealed class StrVariableToken : VariableToken
	{
		public StrVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
			array = varData.DataString;
			IsForbid = array.Length == 0;
		}
		string[] array;
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return array[VarCodeInt];
		}

		public override void SetValue(string value, long[] arguments)
		{
			array[VarCodeInt] = value;
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			array[VarCodeInt] = value;
		}

	}

	private sealed class Str1DVariableToken : VariableToken
	{
		public Str1DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
			array = varData.DataStringArray[VarCodeInt];
			IsForbid = array.Length == 0;
		}
		string[] array;
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			array[arguments[0]] = value;
		}
		public override void SetValue(string[] values, long[] arguments)
		{
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			for (int i = start; i < end; i++)
				array[i] = value;
		}
		public override int GetLength()
		{ return array.Length; }
		public override int GetLength(int dimension)
		{
			if (dimension == 0)
				return array.Length;
			throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
		}
		public override object GetArray() { return array; }

		public override void CheckElement(long[] arguments, bool[] doCheck)
		{
			if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.Length)))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
		}
		public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
		{
			CheckElement(arguments);
			if ((index1 < 0) || (index1 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
			if ((index2 < 0) || (index2 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
		}
	}

	private sealed class CharaIntVariableToken : CharaVariableToken
	{
		public CharaIntVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			return chara.DataInteger[VarCodeInt];
		}

		public override void SetValue(long value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataInteger[VarCodeInt] = value;
		}


		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			varData.CharacterList[charaPos].setValueAll(VarCodeInt, value);
			//CharacterData chara = varData.CharacterList[charaPos];
			//chara.DataInteger[VarCodeInt] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataInteger[VarCodeInt] += value;
			return chara.DataInteger[VarCodeInt];
		}
	}

	private sealed class CharaInt1DVariableToken : CharaVariableToken
	{
		public CharaInt1DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			return chara.DataIntegerArray[VarCodeInt][arguments[1]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataIntegerArray[VarCodeInt][arguments[1]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			long[] array = chara.DataIntegerArray[VarCodeInt];
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			varData.CharacterList[charaPos].setValueAll1D(VarCodeInt, value, start, end);
			//CharacterData chara = varData.CharacterList[charaPos];
			//Int64[] array = chara.DataIntegerArray[VarCodeInt];
			//for (int i = start; i < end; i++)
			//    array[i] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataIntegerArray[VarCodeInt][arguments[1]] += value;
			return chara.DataIntegerArray[VarCodeInt][arguments[1]];
		}

		public override object GetArrayChara(int charano)
		{
			CharacterData chara = varData.CharacterList[charano];
			return chara.DataIntegerArray[VarCodeInt];
		}

	}


	private sealed class CharaStrVariableToken : CharaVariableToken
	{
		public CharaStrVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			return chara.DataString[VarCodeInt];
		}

		public override void SetValue(string value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataString[VarCodeInt] = value;
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			varData.CharacterList[charaPos].setValueAll(VarCodeInt, value);
			//CharacterData chara = varData.CharacterList[charaPos];
			//chara.DataString[VarCodeInt] = value;
		}


	}

	private sealed class CharaStr1DVariableToken : CharaVariableToken
	{
		public CharaStr1DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			return chara.DataStringArray[VarCodeInt][arguments[1]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataStringArray[VarCodeInt][arguments[1]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			string[] array = chara.DataStringArray[VarCodeInt];
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			varData.CharacterList[charaPos].setValueAll1D(VarCodeInt, value, start, end);
			//CharacterData chara = varData.CharacterList[charaPos];
			//String[] array = chara.DataStringArray[VarCodeInt];
			//for (int i = start; i < end; i++)
			//    array[i] = value;
		}

		public override object GetArrayChara(int charano)
		{
			CharacterData chara = varData.CharacterList[charano];
			return chara.DataStringArray[VarCodeInt];
		}

	}

	private sealed class CharaInt2DVariableToken : CharaVariableToken
	{
		public CharaInt2DVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}

		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			return chara.DataIntegerArray2D[VarCodeInt][arguments[1], arguments[2]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataIntegerArray2D[VarCodeInt][arguments[1], arguments[2]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			long[,] array = chara.DataIntegerArray2D[VarCodeInt];
			int start = (int)arguments[2];
			int end = start + values.Length;
			int index1 = (int)arguments[1];
			for (int i = start; i < end; i++)
				array[index1, i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			varData.characterList[charaPos].setValueAll2D(VarCodeInt, value);
			//CharacterData chara = varData.CharacterList[charaPos];
			//Int64[,] array = chara.DataIntegerArray2D[VarCodeInt];
			//int a1 = array.GetLength(0);
			//int a2 = array.GetLength(1);
			//for (int i = 0; i < a1; i++)
			//    for (int j = 0; j < a2; j++)
			//        array[i, j] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			CharacterData chara = varData.CharacterList[(int)arguments[0]];
			chara.DataIntegerArray2D[VarCodeInt][arguments[1], arguments[2]] += value;
			return chara.DataIntegerArray2D[VarCodeInt][arguments[1], arguments[2]];
		}

		public override object GetArrayChara(int charano)
		{
			CharacterData chara = varData.CharacterList[charano];
			return chara.DataIntegerArray2D[VarCodeInt];
		}

	}

	#endregion
	#region 定数
	private abstract class ConstantToken : VariableToken
	{
		public ConstantToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override void SetValue(long value, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToConst.Text, varName)); }
		public override void SetValue(string value, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToConst.Text, varName)); }
		public override void SetValue(long[] values, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToConst.Text, varName)); }
		public override void SetValue(string[] values, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToConst.Text, varName)); }
		public override long PlusValue(long value, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToConst.Text, varName)); }
	}

	private sealed class IntConstantToken : ConstantToken
	{
		public IntConstantToken(VariableCode varCode, VariableData varData, long i)
			: base(varCode, varData)
		{
			this.i = i;
		}
		long i;
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return i;
		}
	}
	private sealed class StrConstantToken : ConstantToken
	{
		public StrConstantToken(VariableCode varCode, VariableData varData, string s)
			: base(varCode, varData)
		{
			this.s = s;
		}
		string s;
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return s;
		}
	}
	private sealed class Int1DConstantToken : ConstantToken
	{
		public Int1DConstantToken(VariableCode varCode, VariableData varData, long[] array)
			: base(varCode, varData)
		{
			this.array = array;
			IsForbid = array.Length == 0;
		}
		long[] array;
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0]];
		}
		public override int GetLength()
		{ return array.Length; }
		public override int GetLength(int dimension)
		{
			if (dimension == 0)
				return array.Length;
			throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
		}
		public override object GetArray() { return array; }

		public override void CheckElement(long[] arguments, bool[] doCheck)
		{
			if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.Length)))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
		}
		public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
		{
			CheckElement(arguments);
			if ((index1 < 0) || (index1 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
			if ((index2 < 0) || (index2 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
		}
	}

	private sealed class Str1DConstantToken : ConstantToken
	{
		public Str1DConstantToken(VariableCode varCode, VariableData varData, string[] array)
			: base(varCode, varData)
		{
			this.array = array;
			IsForbid = array.Length == 0;
		}
		public Str1DConstantToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			array = varData.constant.GetCsvNameList(varCode);
			IsForbid = array.Length == 0;
		}

		string[] array;
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0]];
		}
		public override int GetLength()
		{ return array.Length; }
		public override int GetLength(int dimension)
		{
			if (dimension == 0)
				return array.Length;
			throw new CodeEE(string.Format(trerror.GetSizeNonExistDim.Text, varName));
		}
		public override object GetArray() { return array; }

		public override void CheckElement(long[] arguments, bool[] doCheck)
		{
			if (doCheck[0] && ((arguments[0] < 0) || (arguments[0] >= array.Length)))
				throw new CodeEE(string.Format(trerror.OoRVarArg.Text, varName, "1", arguments[0].ToString()));
		}
		public override void IsArrayRangeValid(long[] arguments, long index1, long index2, string funcName, long i1, long i2)
		{
			CheckElement(arguments);
			if ((index1 < 0) || (index1 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i1.ToString(), index1.ToString(), varName));
			if ((index2 < 0) || (index2 > array.Length))
				throw new CodeEE(string.Format(trerror.OoRInstructionArg.Text, funcName, i2.ToString(), index2.ToString(), varName));
		}
	}

	#endregion
	#region 特殊処理

	private abstract class PseudoVariableToken : VariableToken
	{
		protected PseudoVariableToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override void SetValue(long value, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToPseudoVar.Text, varName)); }
		public override void SetValue(string value, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToPseudoVar.Text, varName)); }
		public override void SetValue(long[] values, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToPseudoVar.Text, varName)); }
		public override void SetValue(string[] values, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToPseudoVar.Text, varName)); }
		public override long PlusValue(long value, long[] arguments)
		{ throw new CodeEE(string.Format(trerror.AssignToPseudoVar.Text, varName)); }
		public override int GetLength()
		{ throw new CodeEE(string.Format(trerror.GetSizePseudoVar.Text, varName)); }
		public override int GetLength(int dimension)
		{ throw new CodeEE(string.Format(trerror.GetSizePseudoVar.Text, varName)); }
		public override object GetArray()
		{ throw new CodeEE(string.Format(trerror.GetDimPseudoVar.Text, varName)); }
	}


	private sealed class RandToken : PseudoVariableToken
	{
		public RandToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			long i = arguments[0];
			if (i <= 0)
				throw new CodeEE(string.Format(trerror.RandArgIsNegative.Text, i.ToString()));
			return exm.VEvaluator.GetNextRand(i);
		}
	}
	private sealed class CompatiRandToken : PseudoVariableToken
	{
		public CompatiRandToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			long i = arguments[0];
			if (i == 0)
				return 0L;
			else if (i < 0)
				i = -i;
			return exm.VEvaluator.GetNextRand(32768) % i;//0-32767の乱数を引数で除算した余り
		}
	}

	private sealed class CHARANUM_Token : PseudoVariableToken
	{
		public CHARANUM_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return varData.CharacterList.Count;
		}
	}

	private sealed class LASTLOAD_TEXT_Token : PseudoVariableToken
	{
		public LASTLOAD_TEXT_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return varData.LastLoadText;
		}
	}

	private sealed class LASTLOAD_VERSION_Token : PseudoVariableToken
	{
		public LASTLOAD_VERSION_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return varData.LastLoadVersion;
		}
	}

	private sealed class LASTLOAD_NO_Token : PseudoVariableToken
	{
		public LASTLOAD_NO_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return varData.LastLoadNo;
		}
	}
	private sealed class LINECOUNT_Token : PseudoVariableToken
	{
		public LINECOUNT_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return exm.Console.LineCount;
		}
	}

	private sealed class WINDOW_TITLE_Token : VariableToken
	{
		public WINDOW_TITLE_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return GlobalStatic.Console.GetWindowTitle();
		}
		public override void SetValue(string value, long[] arguments)
		{
			GlobalStatic.Console.SetWindowTitle(value);
		}
	}

	private sealed class MONEYLABEL_Token : VariableToken
	{
		public MONEYLABEL_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return Config.MoneyLabel;
		}
	}

	private sealed class DRAWLINESTR_Token : VariableToken
	{
		public DRAWLINESTR_Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return exm.Console.getDefStBar();
		}
	}

	private sealed class EmptyStrToken : PseudoVariableToken
	{
		public EmptyStrToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return "";
		}
	}
	private sealed class EmptyIntToken : PseudoVariableToken
	{
		public EmptyIntToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return 0L;
		}
	}

	private sealed class Debug__FILE__Token : PseudoVariableToken
	{
		public Debug__FILE__Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			LogicalLine line = exm.Process.GetScaningLine();
			if ((line == null) || (line.Position == null))
				return "";
			return line.Position.Value.Filename;
		}
	}

	private sealed class Debug__FUNCTION__Token : PseudoVariableToken
	{
		public Debug__FUNCTION__Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			LogicalLine line = exm.Process.GetScaningLine();
			if ((line == null) || (line.ParentLabelLine == null))
				return "";//システム待機中のデバッグモードから呼び出し
			return line.ParentLabelLine.LabelName;
		}
	}
	private sealed class Debug__LINE__Token : PseudoVariableToken
	{
		public Debug__LINE__Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			LogicalLine line = exm.Process.GetScaningLine();
			if ((line == null) || (line.Position == null))
				return -1L;
			return line.Position.Value.LineNo;
		}
	}

	private sealed class ISTIMEOUTToken : PseudoVariableToken
	{
		public ISTIMEOUTToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = false;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return Convert.ToInt64(GlobalStatic.Console.IsTimeOut);
		}
	}

	private sealed class __INT_MAX__Token : PseudoVariableToken
	{
		public __INT_MAX__Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return long.MaxValue;
		}
	}
	private sealed class __INT_MIN__Token : PseudoVariableToken
	{
		public __INT_MIN__Token(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return long.MinValue;
		}
	}

	private sealed class EMUERA_VERSIONToken : PseudoVariableToken
	{
		public EMUERA_VERSIONToken(VariableCode varCode, VariableData varData)
			: base(varCode, varData)
		{
			CanRestructure = true;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			//return AssemblyData.emueraVer.ToString();
			//互換性維持のため
			return "1.824.0.0";
		}

	}

	#endregion
	#region LOCAL


	private sealed class LocalInt1DVariableToken : LocalVariableToken
	{
		public LocalInt1DVariableToken(VariableCode varCode, VariableData varData, string subId, int size)
			: base(varCode, varData, subId, size)
		{
		}
		long[] array;

		public override void SetDefault()
		{
			if (array != null)
				Array.Clear(array, 0, size);
		}

		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				array = new long[size];
			return array[arguments[0]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			if (array == null)
				array = new long[size];
			array[arguments[0]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			if (array == null)
				array = new long[size];
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			if (array == null)
				array = new long[size];
			for (int i = start; i < end; i++)
				array[i] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			if (array == null)
				array = new long[size];
			array[arguments[0]] += value;
			return array[arguments[0]];
		}

		public override object GetArray()
		{
			if (array == null)
				array = new long[size];
			return array;
		}

		public override void resize(int newSize)
		{
			size = newSize;
			array = null;
		}
	}

	private sealed class LocalStr1DVariableToken : LocalVariableToken
	{
		public LocalStr1DVariableToken(VariableCode varCode, VariableData varData, string subId, int size)
			: base(varCode, varData, subId, size)
		{
		}
		string[] array;
		public override void SetDefault()
		{
			if (array != null)
				Array.Clear(array, 0, size);
		}

		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				array = new string[size];
			return array[arguments[0]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			if (array == null)
				array = new string[size];
			array[arguments[0]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			if (array == null)
				array = new string[size];
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			if (array == null)
				array = new string[size];
			for (int i = start; i < end; i++)
				array[i] = value;
		}

		public override object GetArray()
		{
			if (array == null)
				array = new string[size];
			return array;
		}

		public override void resize(int newSize)
		{
			size = newSize;
			array = null;
		}

	}

	#endregion
	#region userdef

	//1808beta009 廃止 private static と統合
	//private sealed class UserDefinedInt1DVariableToken : UserDefinedVariableToken

	#region static (広域変数とprivate static の両方を含む)
	private sealed class StaticInt1DVariableToken : UserDefinedVariableToken
	{
		public StaticInt1DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VAR, data)
		{
			length = data.Lengths[0];
			IsStatic = true;
			//array = new Int64[length[0]];
			defArray = data.DefaultInt;
			//if (defArray != null)
			//	Array.Copy(defArray, array, defArray.Length);
		}
		int length;
		long[] array;
		long[] defArray;
		//andEmuera: 変数を 1 個読むたびに通る。実機のセーブロードではこの遅延初期化の判定に
		//1,387ms / 22,900ms が乗っていた。中身は null 判定だけなので必ず埋め込ませる
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		void IfNullInitArray()
		{
			if (array == null)
			{
				array = new long[length];
				if (defArray != null)
				{
					defArray.AsSpan().CopyTo(array.AsSpan());
				}
			}
		}

		public override void SetDefault()
		{
			IfNullInitArray();
			var span = array.AsSpan();
			span.Clear();
			if (defArray != null)
				defArray.AsSpan().CopyTo(span);
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			IfNullInitArray();
			return array[arguments[0]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			IfNullInitArray();
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			IfNullInitArray();
			for (int i = start; i < end; i++)
				array[i] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0]] += value;
			return array[arguments[0]];
		}
		public override object GetArray()
		{
			IfNullInitArray();
			return array;
		}

		public override void ScopeIn() { }
		public override void ScopeOut() { }

	}

	private sealed class StaticInt2DVariableToken : UserDefinedVariableToken
	{
		public StaticInt2DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VAR2D, data)
		{
			size.x = data.Lengths[0];
			size.y = data.Lengths[1];
			IsStatic = true;
			//array = new Int64[sizes[0], sizes[1]];
		}
		(int x, int y) size = (0, 0);
		long[,] array;

		//andEmuera: 変数を 1 個読むたびに通る。実機のセーブロードではこの遅延初期化の判定に
		//1,387ms / 22,900ms が乗っていた。中身は null 判定だけなので必ず埋め込ませる
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		void IfNullInitArray()
		{
			array ??= new long[sizes[0], sizes[1]];
		}
		public override void SetDefault()
		{
			IfNullInitArray();
			Array.Clear(array, 0, totalSize);
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			IfNullInitArray();
			return array[arguments[0], arguments[1]];
		}
		public override void SetValue(long value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			IfNullInitArray();
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			IfNullInitArray();
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}
		public override long PlusValue(long value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0], arguments[1]] += value;
			return array[arguments[0], arguments[1]];
		}
		public override object GetArray()
		{
			IfNullInitArray();
			return array;
		}

		public override void ScopeIn() { }
		public override void ScopeOut() { }

	}
	private sealed class StaticInt3DVariableToken : UserDefinedVariableToken
	{
		public StaticInt3DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VAR3D, data)
		{
			size.x = data.Lengths[0];
			size.y = data.Lengths[1];
			size.z = data.Lengths[2];
			IsStatic = true;
			//array = new Int64[sizes[0], sizes[1], sizes[2]];
		}
		(int x, int y, int z) size = (0, 0, 0);
		long[,,] array;
		//andEmuera: 変数を 1 個読むたびに通る。実機のセーブロードではこの遅延初期化の判定に
		//1,387ms / 22,900ms が乗っていた。中身は null 判定だけなので必ず埋め込ませる
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		void IfNullInitArray()
		{
			array ??= new long[sizes[0], sizes[1], sizes[2]];
		}
		public override void SetDefault()
		{
			IfNullInitArray();
			Array.Clear(array, 0, totalSize);
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			IfNullInitArray();
			return array[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0], arguments[1], arguments[2]] = value;
		}
		public override void SetValue(long[] values, long[] arguments)
		{
			IfNullInitArray();
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], arguments[1], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			IfNullInitArray();
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						array[i, j, k] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0], arguments[1], arguments[2]] += value;
			return array[arguments[0], arguments[1], arguments[2]];
		}
		public override object GetArray()
		{
			IfNullInitArray();
			return array;
		}
		public override void ScopeIn() { }
		public override void ScopeOut() { }

	}
	private sealed class StaticStr1DVariableToken : UserDefinedVariableToken
	{
		public StaticStr1DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VARS, data)
		{
			length = data.Lengths[0];
			IsStatic = true;
			//array = new string[sizes[0]];
			defArray = data.DefaultStr;
			//if (defArray != null)
			//	Array.Copy(defArray, array, defArray.Length);
		}
		int length;
		string[] array;
		string[] defArray;

		//andEmuera: 変数を 1 個読むたびに通る。実機のセーブロードではこの遅延初期化の判定に
		//1,387ms / 22,900ms が乗っていた。中身は null 判定だけなので必ず埋め込ませる
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		void IfNullInitArray()
		{
			if (array == null)
			{
				array = new string[length];
				if (defArray != null)
					Array.Copy(defArray, array, defArray.Length);
			}
		}

		public override void SetDefault()
		{
			IfNullInitArray();
			if (defArray == null)
			{
				Array.Clear(array, 0, totalSize);
			}
			else
			{
				Array.Copy(defArray, array, defArray.Length);
			}
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			IfNullInitArray();
			return array[arguments[0]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			IfNullInitArray();
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			IfNullInitArray();
			for (int i = start; i < end; i++)
				array[i] = value;
		}
		public override object GetArray()
		{
			IfNullInitArray();
			return array;
		}

		public override void ScopeIn() { }
		public override void ScopeOut() { }
	}
	private sealed class StaticStr2DVariableToken : UserDefinedVariableToken
	{
		public StaticStr2DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VARS2D, data)
		{
			size.x = data.Lengths[0];
			size.y = data.Lengths[1];
			IsStatic = true;
			//array = new string[sizes[0], sizes[1]];
		}
		string[,] array;
		(int x, int y) size;

		//andEmuera: 変数を 1 個読むたびに通る。実機のセーブロードではこの遅延初期化の判定に
		//1,387ms / 22,900ms が乗っていた。中身は null 判定だけなので必ず埋め込ませる
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		void IfNullInitArray()
		{
			if (array == null)
			{
				array = new string[size.x, size.y];
			}
		}

		public override void SetDefault()
		{
			IfNullInitArray();
			Array.Clear(array, 0, totalSize);
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			IfNullInitArray();
			return array[arguments[0], arguments[1]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			IfNullInitArray();
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			IfNullInitArray();
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}
		public override object GetArray()
		{
			IfNullInitArray();
			return array;
		}
		public override void ScopeIn() { }
		public override void ScopeOut() { }
	}

	private sealed class StaticStr3DVariableToken : UserDefinedVariableToken
	{
		public StaticStr3DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VARS3D, data)
		{
			size.x = data.Lengths[0];
			size.y = data.Lengths[1];
			size.z = data.Lengths[2];
			IsStatic = true;
			//array = new string[sizes[0], sizes[1], sizes[2]];
		}
		string[,,] array;
		(int x, int y, int z) size;

		//andEmuera: 変数を 1 個読むたびに通る。実機のセーブロードではこの遅延初期化の判定に
		//1,387ms / 22,900ms が乗っていた。中身は null 判定だけなので必ず埋め込ませる
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		void IfNullInitArray()
		{
			if (array == null)
			{
				array = new string[size.x, size.y, size.z];
			}
		}

		public override void SetDefault()
		{
			IfNullInitArray();
			Array.Clear(array, 0, totalSize);
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			IfNullInitArray();
			return array[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			IfNullInitArray();
			array[arguments[0], arguments[1], arguments[2]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			IfNullInitArray();
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], arguments[1], i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			IfNullInitArray();
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						array[i, j, k] = value;
		}
		public override object GetArray()
		{
			IfNullInitArray();
			return array;
		}
		public override void ScopeIn() { }
		public override void ScopeOut() { }
	}
	#endregion
	#region private dynamic

	private sealed class PrivateInt1DVariableToken : UserDefinedVariableToken
	{
		public PrivateInt1DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VAR, data)
		{
			IsStatic = false;
			arrayStack = [];
			defArray = data.DefaultInt;
		}
		readonly Stack<long[]> arrayStack;
		long[] array;
		long[] defArray;
		//int counter = 0;
		public override void SetDefault()
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			array[arguments[0]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			var span = array.AsSpan()[start..end];
			span.Fill(value);
		}

		public override long PlusValue(long value, long[] arguments)
		{
			array[arguments[0]] += value;
			return array[arguments[0]];
		}
		public override object GetArray() { return array; }

		public override void ScopeIn()
		{
			if (array != null)
				arrayStack.Push(array);
			//counter++;
			array = new long[sizes[0]];
			if (defArray != null)
				defArray.AsSpan().CopyTo(array.AsSpan());
		}

		public override void ScopeOut()
		{
			if (arrayStack.Count > 0)
			{
				array = arrayStack.Pop();
			}
			else
				array = null;
		}
	}
	private sealed class PrivateInt2DVariableToken : UserDefinedVariableToken
	{
		public PrivateInt2DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VAR2D, data)
		{
			IsStatic = false;
			arrayStack = [];
		}
		readonly Stack<long[,]> arrayStack;
		long[,] array;
		//int counter = 0;
		public override void SetDefault() { }
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0], arguments[1]];
		}
		public override void SetValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}
		public override long PlusValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1]] += value;
			return array[arguments[0], arguments[1]];
		}
		public override object GetArray() { return array; }

		public override void ScopeIn()
		{
			if (array != null)
				arrayStack.Push(array);
			//counter++;
			array = new long[sizes[0], sizes[1]];
		}

		public override void ScopeOut()
		{
			//counter--;
			//arrayList.RemoveAt(arrayList.Count - 1);
			if (arrayStack.Count > 0)
			{
				array = arrayStack.Pop();
			}
			else
				array = null;
		}
	}
	private sealed class PrivateInt3DVariableToken : UserDefinedVariableToken
	{
		public PrivateInt3DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VAR3D, data)
		{
			IsStatic = false;
			arrayStack = [];
		}
		readonly Stack<long[,,]> arrayStack;
		long[,,] array;
		//int counter = 0;
		public override void SetDefault() { }
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1], arguments[2]] = value;
		}
		public override void SetValue(long[] values, long[] arguments)
		{
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], arguments[1], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						array[i, j, k] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			array[arguments[0], arguments[1], arguments[2]] += value;
			return array[arguments[0], arguments[1], arguments[2]];
		}
		public override object GetArray() { return array; }

		public override void ScopeIn()
		{
			if (array != null)
				arrayStack.Push(array);
			//counter++;
			array = new long[sizes[0], sizes[1], sizes[2]];
		}

		public override void ScopeOut()
		{
			//counter--;
			//arrayList.RemoveAt(arrayList.Count - 1);
			if (arrayStack.Count > 0)
			{
				array = arrayStack.Pop();
			}
			else
				array = null;
		}
	}

	private sealed class PrivateStr1DVariableToken : UserDefinedVariableToken
	{
		public PrivateStr1DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VARS, data)
		{
			sizes = data.Lengths;
			IsStatic = false;
			arrayStack = [];
			defArray = data.DefaultStr;
		}
		//int counter = 0;
		readonly Stack<string[]> arrayStack;
		string[] array;
		string[] defArray;
		public override void SetDefault()
		{
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			array[arguments[0]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			if (value == default)
			{
				Array.Clear(array, start, end - start);
			}
			else
			{
				Array.Fill(array, value, start, end - start);
			}
		}
		public override object GetArray() { return array; }
		public override void ScopeIn()
		{
			//counter++;
			if (array != null)
				arrayStack.Push(array);
			array = new string[sizes[0]];
			if (defArray != null)
				Array.Copy(defArray, array, defArray.Length);
			//arrayList.Add(array);
		}

		public override void ScopeOut()
		{
			//counter--;
			//arrayList.RemoveAt(arrayList.Count - 1);
			if (arrayStack.Count > 0)
			{
				array = arrayStack.Pop();
			}
			else
				array = null;
		}
	}

	private sealed class PrivateStr2DVariableToken : UserDefinedVariableToken
	{
		public PrivateStr2DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VARS2D, data)
		{
			IsStatic = false;
			arrayStack = [];
		}
		//int counter = 0;
		readonly Stack<string[,]> arrayStack;
		string[,] array;
		public override void SetDefault()
		{
		}

		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0], arguments[1]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			array[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}
		public override object GetArray() { return array; }
		public override void ScopeIn()
		{
			//counter++;
			if (array != null)
				arrayStack.Push(array);
			array = new string[sizes[0], sizes[1]];
			//arrayList.Add(array);
		}

		public override void ScopeOut()
		{
			//counter--;
			//arrayList.RemoveAt(arrayList.Count - 1);
			if (arrayStack.Count > 0)
			{
				array = arrayStack.Pop();
			}
			else
				array = null;
		}
	}

	private sealed class PrivateStr3DVariableToken : UserDefinedVariableToken
	{
		public PrivateStr3DVariableToken(UserDefinedVariableData data)
			: base(VariableCode.VARS3D, data)
		{
			int[] sizes = data.Lengths;
			IsStatic = false;
			arrayStack = [];
		}
		//int counter = 0;
		readonly Stack<string[,,]> arrayStack;
		string[,,] array;
		public override void SetDefault() { }

		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			return array[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			array[arguments[0], arguments[1], arguments[2]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[arguments[0], arguments[1], i] = values[i - start];
		}
		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						array[i, j, k] = value;
		}
		public override object GetArray() { return array; }
		public override void ScopeIn()
		{
			//counter++;
			if (array != null)
				arrayStack.Push(array);
			array = new string[sizes[0], sizes[1], sizes[2]];
			//arrayList.Add(array);
		}

		public override void ScopeOut()
		{
			//counter--;
			//arrayList.RemoveAt(arrayList.Count - 1);
			if (arrayStack.Count > 0)
			{
				array = arrayStack.Pop();
			}
			else
				array = null;
		}
	}


	#endregion
	#region ref
	//1808beta009で追加
	/// <summary>
	/// public staticとprivate dynamicをクラスレベルでは区別しない
	/// 1808beta009時点ではprivate dynamicのみ
	/// </summary>
	private sealed class ReferenceInt1DToken : ReferenceToken
	{
		public ReferenceInt1DToken(UserDefinedVariableData data)
			: base(VariableCode.REF, data)
		{
			CanRestructure = false;
			IsStatic = !data.Private;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			return ((long[])array)[arguments[0]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((long[])array)[arguments[0]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				((long[])array)[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			for (int i = start; i < end; i++)
				((long[])array)[i] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((long[])array)[arguments[0]] += value;
			return ((long[])array)[arguments[0]];
		}

	}

	private sealed class ReferenceInt2DToken : ReferenceToken
	{
		public ReferenceInt2DToken(UserDefinedVariableData data)
			: base(VariableCode.REF2D, data)
		{
			CanRestructure = false;
			IsStatic = !data.Private;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			return ((long[,])array)[arguments[0], arguments[1]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((long[,])array)[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				((long[,])array)[arguments[0], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					((long[,])array)[i, j] = value;
		}


		public override long PlusValue(long value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((long[,])array)[arguments[0], arguments[1]] += value;
			return ((long[,])array)[arguments[0], arguments[1]];
		}
	}

	private sealed class ReferenceInt3DToken : ReferenceToken
	{
		public ReferenceInt3DToken(UserDefinedVariableData data)
			: base(VariableCode.REF3D, data)
		{
			CanRestructure = false;
			IsStatic = !data.Private;
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			return ((long[,,])array)[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((long[,,])array)[arguments[0], arguments[1], arguments[2]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				((long[,,])array)[arguments[0], arguments[1], i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						((long[,,])array)[i, j, k] = value;
		}


		public override long PlusValue(long value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((long[,,])array)[arguments[0], arguments[1], arguments[2]] += value;
			return ((long[,,])array)[arguments[0], arguments[1], arguments[2]];
		}

	}
	private sealed class ReferenceStr1DToken : ReferenceToken
	{
		public ReferenceStr1DToken(UserDefinedVariableData data)
			: base(VariableCode.REFS, data)
		{
			CanRestructure = false;
			IsStatic = !data.Private;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			return ((string[])array)[arguments[0]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((string[])array)[arguments[0]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int start = (int)arguments[0];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				((string[])array)[i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			for (int i = start; i < end; i++)
				((string[])array)[i] = value;
		}
	}

	private sealed class ReferenceStr2DToken : ReferenceToken
	{
		public ReferenceStr2DToken(UserDefinedVariableData data)
			: base(VariableCode.REFS2D, data)
		{
			CanRestructure = false;
			IsStatic = !data.Private;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			return ((string[,])array)[arguments[0], arguments[1]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((string[,])array)[arguments[0], arguments[1]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				((string[,])array)[arguments[0], i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					((string[,])array)[i, j] = value;
		}
	}

	private sealed class ReferenceStr3DToken : ReferenceToken
	{
		public ReferenceStr3DToken(UserDefinedVariableData data)
			: base(VariableCode.REFS3D, data)
		{
			CanRestructure = false;
			IsStatic = !data.Private;
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			return ((string[,,])array)[arguments[0], arguments[1], arguments[2]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			((string[,,])array)[arguments[0], arguments[1], arguments[2]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int start = (int)arguments[2];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				((string[,,])array)[arguments[0], arguments[1], i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			if (array == null)
				throw new CodeEE(string.Format(trerror.EmptyRefVar.Text, varName));
			int a1 = array.GetLength(0);
			int a2 = array.GetLength(1);
			int a3 = array.GetLength(2);
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					for (int k = 0; k < a3; k++)
						((string[,,])array)[i, j, k] = value;
		}

	}
	#endregion
	#region chara (広域のみ)

	private sealed class UserDefinedCharaInt1DVariableToken : UserDefinedCharaVariableToken
	{
		public UserDefinedCharaInt1DVariableToken(UserDefinedVariableData data, VariableData varData, int arrayIndex)
			: base(VariableCode.CVAR, data, varData, arrayIndex)
		{
		}
		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			long[] array = (long[])GetArrayChara((int)arguments[0]);
			return array[arguments[1]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			long[] array = (long[])GetArrayChara((int)arguments[0]);
			array[arguments[1]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			long[] array = (long[])GetArrayChara((int)arguments[0]);
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			long[] array = (long[])GetArrayChara(charaPos);
			for (int i = start; i < end; i++)
				array[i] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			long[] array = (long[])GetArrayChara((int)arguments[0]);
			array[arguments[1]] += value;
			return array[arguments[1]];
		}
	}

	private sealed class UserDefinedCharaStr1DVariableToken : UserDefinedCharaVariableToken
	{
		public UserDefinedCharaStr1DVariableToken(UserDefinedVariableData data, VariableData varData, int arrayIndex)
			: base(VariableCode.CVARS, data, varData, arrayIndex)
		{
		}
		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			string[] array = (string[])GetArrayChara((int)arguments[0]);
			return array[arguments[1]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			string[] array = (string[])GetArrayChara((int)arguments[0]);
			array[arguments[1]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			string[] array = (string[])GetArrayChara((int)arguments[0]);
			int start = (int)arguments[1];
			int end = start + values.Length;
			for (int i = start; i < end; i++)
				array[i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			string[] array = (string[])GetArrayChara(charaPos);
			for (int i = start; i < end; i++)
				array[i] = value;
		}
	}

	private sealed class UserDefinedCharaInt2DVariableToken : UserDefinedCharaVariableToken
	{
		public UserDefinedCharaInt2DVariableToken(UserDefinedVariableData data, VariableData varData, int arrayIndex)
			: base(VariableCode.CVAR2D, data, varData, arrayIndex)
		{
		}

		public override long GetIntValue(ExpressionMediator exm, long[] arguments)
		{
			long[,] array = (long[,])GetArrayChara((int)arguments[0]);
			return array[arguments[1], arguments[2]];
		}

		public override void SetValue(long value, long[] arguments)
		{
			long[,] array = (long[,])GetArrayChara((int)arguments[0]);
			array[arguments[1], arguments[2]] = value;
		}

		public override void SetValue(long[] values, long[] arguments)
		{
			long[,] array = (long[,])GetArrayChara((int)arguments[0]);
			int start = (int)arguments[2];
			int end = start + values.Length;
			int index1 = (int)arguments[1];
			for (int i = start; i < end; i++)
				array[index1, i] = values[i - start];
		}

		public override void SetValueAll(long value, int start, int end, int charaPos)
		{
			long[,] array = (long[,])GetArrayChara(charaPos);
			int a1 = sizes[0];
			int a2 = sizes[1];
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}

		public override long PlusValue(long value, long[] arguments)
		{
			long[,] array = (long[,])GetArrayChara((int)arguments[0]);
			array[arguments[1], arguments[2]] += value;
			return array[arguments[1], arguments[2]];
		}
	}

	private sealed class UserDefinedCharaStr2DVariableToken : UserDefinedCharaVariableToken
	{
		public UserDefinedCharaStr2DVariableToken(UserDefinedVariableData data, VariableData varData, int arrayIndex)
			: base(VariableCode.CVARS2D, data, varData, arrayIndex)
		{
		}

		public override string GetStrValue(ExpressionMediator exm, long[] arguments)
		{
			string[,] array = (string[,])GetArrayChara((int)arguments[0]);
			return array[arguments[1], arguments[2]];
		}

		public override void SetValue(string value, long[] arguments)
		{
			string[,] array = (string[,])GetArrayChara((int)arguments[0]);
			array[arguments[1], arguments[2]] = value;
		}

		public override void SetValue(string[] values, long[] arguments)
		{
			string[,] array = (string[,])GetArrayChara((int)arguments[0]);
			int start = (int)arguments[2];
			int end = start + values.Length;
			int index1 = (int)arguments[1];
			for (int i = start; i < end; i++)
				array[index1, i] = values[i - start];
		}

		public override void SetValueAll(string value, int start, int end, int charaPos)
		{
			string[,] array = (string[,])GetArrayChara(charaPos);
			int a1 = sizes[0];
			int a2 = sizes[1];
			for (int i = 0; i < a1; i++)
				for (int j = 0; j < a2; j++)
					array[i, j] = value;
		}

	}
	#endregion
	#endregion
}

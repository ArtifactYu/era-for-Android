using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using System.Collections.Generic;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem
{
	//Wrapper for easier character data access
	public class PluginAPICharContext
	{
		internal static void CacheVariables(ExpressionMediator exm)
		{
			_BASE = exm.VEvaluator.VariableData.GetVarTokenDic()["BASE"];
			_MAXBASE = exm.VEvaluator.VariableData.GetVarTokenDic()["MAXBASE"];
			_ABL = exm.VEvaluator.VariableData.GetVarTokenDic()["ABL"];
			_TALENT = exm.VEvaluator.VariableData.GetVarTokenDic()["TALENT"];
			_EXP = exm.VEvaluator.VariableData.GetVarTokenDic()["EXP"];
			_MARK = exm.VEvaluator.VariableData.GetVarTokenDic()["MARK"];
			_PALAM = exm.VEvaluator.VariableData.GetVarTokenDic()["PALAM"];
			_SOURCE = exm.VEvaluator.VariableData.GetVarTokenDic()["SOURCE"];
			_EX = exm.VEvaluator.VariableData.GetVarTokenDic()["EX"];
			_CFLAG = exm.VEvaluator.VariableData.GetVarTokenDic()["CFLAG"];
			_JUEL = exm.VEvaluator.VariableData.GetVarTokenDic()["JUEL"];
			_RELATION = exm.VEvaluator.VariableData.GetVarTokenDic()["RELATION"];
			_EQUIP = exm.VEvaluator.VariableData.GetVarTokenDic()["EQUIP"];
			_TEQUIP = exm.VEvaluator.VariableData.GetVarTokenDic()["TEQUIP"];
			_STAIN = exm.VEvaluator.VariableData.GetVarTokenDic()["STAIN"];
			_GOTJUEL = exm.VEvaluator.VariableData.GetVarTokenDic()["GOTJUEL"];
			_NOWEX = exm.VEvaluator.VariableData.GetVarTokenDic()["NOWEX"];
			_DOWNBASE = exm.VEvaluator.VariableData.GetVarTokenDic()["DOWNBASE"];
			_CUP = exm.VEvaluator.VariableData.GetVarTokenDic()["CUP"];
			_CDOWN = exm.VEvaluator.VariableData.GetVarTokenDic()["CDOWN"];
			_TCVAR = exm.VEvaluator.VariableData.GetVarTokenDic()["TCVAR"];
			_NAME = exm.VEvaluator.VariableData.GetVarTokenDic()["NAME"];
			_CALLNAME = exm.VEvaluator.VariableData.GetVarTokenDic()["CALLNAME"];
			_NICKNAME = exm.VEvaluator.VariableData.GetVarTokenDic()["NICKNAME"];
			_MASTERNAME = exm.VEvaluator.VariableData.GetVarTokenDic()["MASTERNAME"];
			_CSTR = exm.VEvaluator.VariableData.GetVarTokenDic()["CSTR"];
			_CDFLAG = exm.VEvaluator.VariableData.GetVarTokenDic()["CDFLAG"];

			_UserDefinedVars.Clear();
			var userCharaVars = exm.VEvaluator.VariableData.UserDefinedCharaVarList;
			foreach (var userCharVar in userCharaVars)
			{
				var key = userCharVar.Name;
				_UserDefinedVars.Add(key, userCharVar);
			}

			PluginAPICharContext.exm = exm;
		}

		//Cached Int[] variables
		static VariableToken _BASE;
		static VariableToken _MAXBASE;
		static VariableToken _ABL;
		static VariableToken _TALENT;
		static VariableToken _EXP;
		static VariableToken _MARK;
		static VariableToken _PALAM;
		static VariableToken _SOURCE;
		static VariableToken _EX;
		static VariableToken _CFLAG;
		static VariableToken _JUEL;
		static VariableToken _RELATION;
		static VariableToken _EQUIP;
		static VariableToken _TEQUIP;
		static VariableToken _STAIN;
		static VariableToken _GOTJUEL;
		static VariableToken _NOWEX;
		static VariableToken _DOWNBASE;
		static VariableToken _CUP;
		static VariableToken _CDOWN;
		static VariableToken _TCVAR;

		//Cached string variables
		static VariableToken _NAME;
		static VariableToken _CALLNAME;
		static VariableToken _NICKNAME;
		static VariableToken _MASTERNAME;

		//Cached string[] variables
		static VariableToken _CSTR;

		//Cached int[][] variables
		static VariableToken _CDFLAG;

		static Dictionary<string, VariableToken> _UserDefinedVars = [];

		static ExpressionMediator exm;
		internal PluginAPICharContext(long charId)
		{
			this.charId = charId;
			BASE = new CharInt1dWrapper(charId, _BASE, exm, VariableCode.BASE);
			MAXBASE = new CharInt1dWrapper(charId, _MAXBASE, exm, VariableCode.MAXBASE);
			ABL = new CharInt1dWrapper(charId, _ABL, exm, VariableCode.ABL);
			TALENT = new CharInt1dWrapper(charId, _TALENT, exm, VariableCode.TALENT);
			EXP = new CharInt1dWrapper(charId, _EXP, exm, VariableCode.EXP);
			MARK = new CharInt1dWrapper(charId, _MARK, exm, VariableCode.MARK);
			PALAM = new CharInt1dWrapper(charId, _PALAM, exm, VariableCode.PALAM);
			SOURCE = new CharInt1dWrapper(charId, _SOURCE, exm, VariableCode.SOURCE);
			EX = new CharInt1dWrapper(charId, _EX, exm, VariableCode.EX);
			CFLAG = new CharInt1dWrapper(charId, _CFLAG, exm, VariableCode.CFLAG);
			JUEL = new CharInt1dWrapper(charId, _JUEL, exm, VariableCode.JUEL);
			RELATION = new CharInt1dWrapper(charId, _RELATION, exm, VariableCode.RELATION);
			EQUIP = new CharInt1dWrapper(charId, _EQUIP, exm, VariableCode.EQUIP);
			TEQUIP = new CharInt1dWrapper(charId, _TEQUIP, exm, VariableCode.TEQUIP);
			STAIN = new CharInt1dWrapper(charId, _STAIN, exm, VariableCode.STAIN);
			GOTJUEL = new CharInt1dWrapper(charId, _GOTJUEL, exm, VariableCode.GOTJUEL);
			NOWEX = new CharInt1dWrapper(charId, _NOWEX, exm, VariableCode.NOWEX);
			DOWNBASE = new CharInt1dWrapper(charId, _DOWNBASE, exm, VariableCode.DOWNBASE);
			CUP = new CharInt1dWrapper(charId, _CUP, exm, VariableCode.CUP);
			CDOWN = new CharInt1dWrapper(charId, _CDOWN, exm, VariableCode.CDOWN);
			TCVAR = new CharInt1dWrapper(charId, _TCVAR, exm, VariableCode.TCVAR);

			NAME = new CharStringWrapper(charId, _NAME, exm, VariableCode.NAME);
			CALLNAME = new CharStringWrapper(charId, _CALLNAME, exm, VariableCode.CALLNAME);
			NICKNAME = new CharStringWrapper(charId, _NICKNAME, exm, VariableCode.NICKNAME);
			MASTERNAME = new CharStringWrapper(charId, _MASTERNAME, exm, VariableCode.MASTERNAME);

			CSTR = new CharString1dWrapper(charId, _CSTR, exm, VariableCode.CSTR);
			CDFLAG = new CharInt2dWrapper(charId, _CDFLAG, exm, VariableCode.CDFLAG);

			UserDefined = new CharUserdefinedInt1dWrapper(charId, _UserDefinedVars, exm, VariableCode.VAR);
		}

		public CharUserdefinedInt1dWrapper UserDefined;

		public CharInt1dWrapper BASE;
		public CharInt1dWrapper MAXBASE;
		public CharInt1dWrapper ABL;
		public CharInt1dWrapper TALENT;
		public CharInt1dWrapper EXP;
		public CharInt1dWrapper MARK;
		public CharInt1dWrapper PALAM;
		public CharInt1dWrapper SOURCE;
		public CharInt1dWrapper EX;
		public CharInt1dWrapper CFLAG;
		public CharInt1dWrapper JUEL;
		public CharInt1dWrapper RELATION;
		public CharInt1dWrapper EQUIP;
		public CharInt1dWrapper TEQUIP;
		public CharInt1dWrapper STAIN;
		public CharInt1dWrapper GOTJUEL;
		public CharInt1dWrapper NOWEX;
		public CharInt1dWrapper DOWNBASE;
		public CharInt1dWrapper CUP;
		public CharInt1dWrapper CDOWN;
		public CharInt1dWrapper TCVAR;

		public CharStringWrapper NAME;
		public CharStringWrapper CALLNAME;
		public CharStringWrapper NICKNAME;
		public CharStringWrapper MASTERNAME;

		public CharString1dWrapper CSTR;

		public CharInt2dWrapper CDFLAG;

		long charId;
	}
}

using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Expression;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem
{
	public class PluginMethodParameter
	{
		public PluginMethodParameter(string initialValue)
		{
			isString = true;
			strValue = initialValue;
		}

		public PluginMethodParameter(long initialValue)
		{
			isString = false;
			intValue = initialValue;
		}

		public bool isString;
		public string strValue;
		public long intValue;
	}

	internal static class PluginMethodParameterBuilder
	{
		internal static PluginMethodParameter ConvertTerm(AExpression term, ExpressionMediator exm)
		{
			if (term.IsString)
			{
				return new PluginMethodParameter(term.GetStrValue(exm));
			}
			else
			{
				return new PluginMethodParameter(term.GetIntValue(exm));

			}
		}
	}
}

using MinorShift.Emuera.Runtime.Script.Parser;

namespace MinorShift.Emuera.Runtime.Script.Data;

internal sealed class DefineMacro
{
	public DefineMacro(string key, WordCollection wc, int argcount)
	{
		Keyword = key;
		Statement = wc;
		ArgCount = argcount;
		Statement.PointerReset();;
		HasArguments = argcount != 0;
		if (Statement.Collection.Count == 1)
			IDWord = Statement.Current as IdentifierWord;
		IsNull = wc.Collection.Count == 0;
	}
	public readonly string Keyword;
	public readonly int ArgCount;
	public readonly WordCollection Statement;
	public readonly IdentifierWord IDWord;
	public readonly bool HasArguments;
	public readonly bool IsNull;

}

using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MinorShift.Emuera.Runtime.Config;

internal abstract class AConfigItem
{
	#region EM_私家版_Emuera多言語化改造
	// public AConfigItem(ConfigCode code, string text)
	public AConfigItem(ConfigCode code, string text, string etext)
	{
		Code = code;
		Name = EnumsNET.Enums.AsString(code);
		// this.Text = text;
		Text = text.ToUpper();
		// this.EngText = etext;
		EngText = etext.ToUpper();
	}
	#endregion

	#region EM_私家版_Emuera多言語化改造
	public static ConfigItem<T> Copy<T>(ConfigItem<T> other)
	{
		if (other == null)
			return null;
		//ConfigItem<T> ret = new ConfigItem<T>(other.Code, other.Text, other.Value);
		ConfigItem<T> ret = new(other.Code, other.Text, other.EngText, other.Value)
		{
			Fixed = other.Fixed
		};
		return ret;
	}
	#endregion

	public abstract void CopyTo(AConfigItem other);
	public abstract bool TryParse(string tokens);
	public abstract void SetValue<U>(U p);
	public abstract U GetValue<U>();
	public abstract string ValueToString();
	public readonly ConfigCode Code;
	public readonly string Name;
	public readonly string Text;
	#region EM_私家版_Emuera多言語化改造
	public readonly string EngText;
	#endregion
	public bool Fixed;
}

internal sealed class ConfigItem<T> : AConfigItem
{
	#region EM_私家版_Emuera多言語化改造
	public ConfigItem(ConfigCode code, string text, string etext, T t) : base(code, text, etext)
	#endregion
	{
		val = t;
	}
	private T val;
	public T Value
	{
		get { return val; }
		set
		{
			if (Fixed)
				return;
			val = value;
		}
	}

	public override void CopyTo(AConfigItem other)
	{

		ConfigItem<T> item = (ConfigItem<T>)other;
		item.Fixed = false;
		item.Value = Value;
		item.Fixed = Fixed;
	}

	public override void SetValue<U>(U p)
	{
		////if (this is ConfigItem<U>)
		//	((ConfigItem<U>)(AConfigItem)this).Value = p;
		////else
		////	throw new ExeEE("型が一致しない");

		((ConfigItem<U>)(AConfigItem)this).Value = p;
	}

	public override U GetValue<U>()
	{
		//////if (this is ConfigItem<U>)
		//	return ((ConfigItem<U>)(AConfigItem)this).Value;
		////throw new ExeEE("型が一致しない");

		return ((ConfigItem<U>)(AConfigItem)this).Value;
	}

	public override string ValueToString()
	{
		if (this is ConfigItem<bool>)
		{
			//ConfigItem<T>をConfigItem<bool>に直接キャストすることはできない
			bool b = ((ConfigItem<bool>)(AConfigItem)this).Value;
			if (b)
				return "YES";
			return "NO";
		}
		if (this is ConfigItem<Color>)
		{
			Color c = ((ConfigItem<Color>)(AConfigItem)this).Value;
			return string.Format("{0},{1},{2}", c.R, c.G, c.B);
		}

		#region EM_私家版_LoadText＆SaveText機能拡張
		if (this is ConfigItem<List<string>>)
		{
			var sb = new StringBuilder();
			var v = ((ConfigItem<List<string>>)(AConfigItem)this).Value;
			foreach (var str in v)
			{
				if (sb.Length > 0)
					sb.Append(",");
				sb.Append(str);
			}
			return sb.ToString();
		}
		#endregion
		return val.ToString();
	}


	public override string ToString()
	{
		#region EM_私家版_Emuera多言語化改造
		return (Config.EnglishConfigOutput ? EngText : Text) + ":" + ValueToString();
		#endregion
	}



	/// ジェネリック化大失敗。なんかうまい方法ないかな～
	public override bool TryParse(string param)
	{
		bool ret = false;
		if (param == null || param.Length == 0)
			return false;
		if (Fixed)
			return false;
		string str = param.Trim();
		if (this is ConfigItem<bool>)
		{
			bool b = false;
			ret = tryStringToBool(str, ref b);
			if (ret)//ConfigItem<T>をConfigItem<bool>に直接キャストすることはできない
				((ConfigItem<bool>)(AConfigItem)this).Value = b;
		}
		else if (this is ConfigItem<Color>)
		{
			Color c;
			ret = tryStringsToColor(str, out c);
			if (ret)
				((ConfigItem<Color>)(AConfigItem)this).Value = c;
			else
				throw new CodeEE(Lang.Error.NotExistColorSpecifier.Text);
		}
		else if (this is ConfigItem<char>)
		{
			char c;
			ret = char.TryParse(str, out c);
			if (ret)
				((ConfigItem<char>)(AConfigItem)this).Value = c;
		}
		else if (this is ConfigItem<int>)
		{
			int i;
			ret = int.TryParse(str, out i);
			if (ret)
				((ConfigItem<int>)(AConfigItem)this).Value = i;
			else
				throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
		}
		else if (this is ConfigItem<long>)
		{
			long i;
			ret = long.TryParse(str, out i);
			if (ret)
				((ConfigItem<long>)(AConfigItem)this).Value = i;
			else
				throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
		}
		else if (this is ConfigItem<List<long>>)
		{
			((ConfigItem<List<long>>)(AConfigItem)this).Value.Clear();
			long i;
			string[] strs = str.Split('/');
			foreach (string st in strs)
			{
				ret = long.TryParse(st.Trim(), out i);
				if (ret)
					((ConfigItem<List<long>>)(AConfigItem)this).Value.Add(i);
				else
				{
					throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
				}
			}
		}
		else if (this is ConfigItem<string>)
		{
			ret = true;
			((ConfigItem<string>)(AConfigItem)this).Value = str;
		}
		else if (this is ConfigItem<List<string>>)
		{
			#region EM_私家版_LoadText＆SaveText機能拡張
			// ret = true;
			// ((ConfigItem<List<string>>)(AConfigItem)this).Value.Add(str);
			ret = true;
			var list = ((ConfigItem<List<string>>)(AConfigItem)this).Value;
			tryStringToStringList(str, ref list);
			((ConfigItem<List<string>>)(AConfigItem)this).Value = list;
			#endregion
		}
		else if (this is ConfigItem<TextDrawingMode>)
		{
			if (Enum.TryParse<TextDrawingMode>(str, true, out var result))
			{
				((ConfigItem<TextDrawingMode>)(AConfigItem)this).Value = result;
			}
			else
				throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}
		else if (this is ConfigItem<ReduceArgumentOnLoadFlag>)
		{
			if (Enum.TryParse<ReduceArgumentOnLoadFlag>(str, true, out var result))
			{
				((ConfigItem<ReduceArgumentOnLoadFlag>)(AConfigItem)this).Value = result;
			}
			else
				throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}
		else if (this is ConfigItem<DisplayWarningFlag>)
		{
			if (Enum.TryParse<DisplayWarningFlag>(str, true, out var result))
			{
				((ConfigItem<DisplayWarningFlag>)(AConfigItem)this).Value = result;
			}
			else
				throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}
		else if (this is ConfigItem<UseLanguage>)
		{
			if (Enum.TryParse<UseLanguage>(str, true, out var result))
			{
				((ConfigItem<UseLanguage>)(AConfigItem)this).Value = result;
			}
			else
				throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}
		else if (this is ConfigItem<TextEditorType>)
		{
			if (Enum.TryParse<TextEditorType>(str, true, out var result))
			{
				((ConfigItem<TextEditorType>)(AConfigItem)this).Value = result;
			}
			else
				throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}
		//else
		//    throw new ExeEE("型不明なコンフィグ");
		return ret;
	}


	#region EM_私家版_LoadText＆SaveText機能拡張
	private static bool tryStringToStringList(string arg, ref List<string> vs)
	{
		string[] tokens = arg.Split(',');
		vs.Clear();
		foreach (var token in tokens)
		{
			vs.Add(token.Trim());
		}
		return true;
	}
	#endregion

	private static bool tryStringToBool(string arg, ref bool p)
	{
		if (arg == null)
			return false;
		string str = arg.Trim();
		if (int.TryParse(str, out int i))
		{
			p = i != 0;
			return true;
		}
		if (str.Equals("NO", StringComparison.OrdinalIgnoreCase)
			|| str.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
			|| str.Equals("後", StringComparison.Ordinal))//"単位の位置"用
		{
			p = false;
			return true;
		}
		if (str.Equals("YES", StringComparison.OrdinalIgnoreCase)
			|| str.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
			|| str.Equals("前", StringComparison.Ordinal))
		{
			p = true;
			return true;
		}
		throw new CodeEE(Lang.Error.InvalidSpecification.Text);
	}

	private static bool tryStringsToColor(string str, out Color c)
	{
		string[] tokens = str.Split(',');
		c = Color.Black;
		int r, g, b;
		if (tokens.Length < 3)
			return false;
		if (!int.TryParse(tokens[0].Trim(), out r) || r < 0 || r > 255)
			return false;
		if (!int.TryParse(tokens[1].Trim(), out g) || g < 0 || g > 255)
			return false;
		if (!int.TryParse(tokens[2].Trim(), out b) || b < 0 || b > 255)
			return false;
		c = Color.FromArgb(r, g, b);
		return true;
	}
}

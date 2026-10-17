using MinorShift.Emuera.UI.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace MinorShift.Emuera.Runtime.Utils.EvilMask;

internal sealed class Utils
{
	internal sealed class MixedNum
	{
		public static int ToPixel(MixedNum num, int def)
		{
			return num != null ? num.isPx ? num.num : num.num * Config.Config.FontSize / 100 : def;
		}
		public static int ToPixel(MixedNum num)
		{
			return num != null ? num.isPx ? num.num : num.num * Config.Config.FontSize / 100 : 0;
		}
		public static float ToPixelf(MixedNum num, float def)
		{
			return num != null ? num.isPx ? num.num : num.num * Config.Config.FontSize / 100f : def;
		}
		public static float ToPixelf(MixedNum num)
		{
			return num != null ? num.isPx ? num.num : num.num * Config.Config.FontSize / 100f : 0f;
		}
		public int num;
		public bool isPx;
		public override string ToString()
		{
			var sb = new StringBuilder();
			if (isPx) sb.Append(num).Append("px");
			else sb.Append(num * Config.Config.FontSize / 100);
			return sb.ToString();
		}
		public StringBuilder BuilderString(StringBuilder sb)
		{
			if (isPx) sb.Append(num).Append("px");
			else sb.Append(num * Config.Config.FontSize / 100);
			return sb;
		}
	}
	internal sealed class StyledBoxModel
	{
		public MixedNum[] border, margin, padding, radius;
		public int[] color;
	}
	static Stopwatch stopwatch = new();
	static long stopwatch_base = DateTime.Now.Ticks;
	public static void ParseParam4MixedNum(ref MixedNum[] nums, string tag, string word, string attrValue)
	{
		string[] tokens = attrValue.Split(',');
		switch (tokens.Length)
		{
			case 1: // all
				ParseMixedNum(ref nums[0], tag, word, tokens[0].Trim());
				nums[1] = nums[0];
				nums[2] = nums[0];
				nums[3] = nums[0];
				break;
			case 2: // top and bottom | left and right
				ParseMixedNum(ref nums[0], tag, word, tokens[0].Trim());
				ParseMixedNum(ref nums[1], tag, word, tokens[1].Trim());
				nums[2] = nums[0];
				nums[3] = nums[1];
				break;
			case 3: //top | left and right | bottom
				ParseMixedNum(ref nums[0], tag, word, tokens[0].Trim());
				ParseMixedNum(ref nums[1], tag, word, tokens[1].Trim());
				ParseMixedNum(ref nums[2], tag, word, tokens[2].Trim());
				nums[3] = nums[1];
				break;
			case 4: // top | right | bottom | left
				for (int i = 0; i < 4; i++)
					ParseMixedNum(ref nums[i], tag, word, tokens[i].Trim());
				break;
			default:
				throw new CodeEE(string.Format(Lang.Error.CanNotInterpretAttribute.Text, attrValue));
		}
	}
	public static T CreateIfNull<T>(ref T obj) where T : new()
	{
		if (obj == null) obj = new T();
		return obj;
	}
	public static int[] CreateIfNull(ref int[] obj, int len = 4)
	{
		if (obj == null) obj = new int[len];
		return obj;
	}
	public static bool TryParseStyledBoxModel(ref StyledBoxModel box, string tag, string word, string attrValue)
	{
		switch (word.ToLower())
		{
			case "border":
				if (CreateIfNull(ref box).border == null) box.border = new MixedNum[4];
				else throw new CodeEE(string.Format(Lang.Error.DuplicateAttribute.Text, tag, word));
				ParseParam4MixedNum(ref box.border, tag, word, attrValue);
				return true;
			case "radius":
				if (CreateIfNull(ref box).radius == null) box.radius = new MixedNum[4];
				else throw new CodeEE(string.Format(Lang.Error.DuplicateAttribute.Text, tag, word));
				ParseParam4MixedNum(ref box.radius, tag, word, attrValue);
				return true;
			case "margin":
				if (CreateIfNull(ref box).margin == null) box.margin = new MixedNum[4];
				else throw new CodeEE(string.Format(Lang.Error.DuplicateAttribute.Text, tag, word));
				ParseParam4MixedNum(ref box.margin, tag, word, attrValue);
				return true;
			case "padding":
				if (CreateIfNull(ref box).padding == null) box.padding = new MixedNum[4];
				else throw new CodeEE(string.Format(Lang.Error.DuplicateAttribute.Text, tag, word));
				ParseParam4MixedNum(ref box.padding, tag, word, attrValue);
				return true;
		}
		return false;
	}
	public static void ParseMixedNum(ref MixedNum num, string tag, string word, string attrValue)
	{
		if (num == null) num = new MixedNum();
		else
			throw new CodeEE(string.Format(Lang.Error.DuplicateAttribute.Text, tag, word));
		if (attrValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
		{
			if (!int.TryParse(attrValue.Substring(0, attrValue.Length - 2), out num.num))
				throw new CodeEE(string.Format(Lang.Error.AttributeCanNotInterpretNum.Text, tag, word));
			num.isPx = true;
		}
		else if (!int.TryParse(attrValue, out num.num))
			throw new CodeEE(string.Format(Lang.Error.AttributeCanNotInterpretNum.Text, tag, word));
	}
	public static void AddTagMixedNumArg(StringBuilder sb, string name, MixedNum num)
	{
		if (num != null && num.num != 0)
		{
			sb.Append(' ').Append(name).Append("='");
			num.BuilderString(sb).Append('\'');
		}
	}
	public static void AddTagMixedParam(StringBuilder sb, string name, MixedNum[] nums)
	{
		if (nums != null)
		{
			sb.Append(' ').Append(name).Append("='");
			if (ReferenceEquals(nums[0], nums[1]) && ReferenceEquals(nums[0], nums[2]) && ReferenceEquals(nums[0], nums[3]))
				nums[0].BuilderString(sb);
			else if (ReferenceEquals(nums[0], nums[2]) && ReferenceEquals(nums[1], nums[3]))
			{
				nums[0].BuilderString(sb);
				sb.Append(',');
				nums[1].BuilderString(sb);
			}
			else if (ReferenceEquals(nums[1], nums[3]))
			{
				nums[0].BuilderString(sb);
				sb.Append(',');
				nums[1].BuilderString(sb);
				sb.Append(',');
				nums[2].BuilderString(sb);
			}
			else
				for (int i = 0; i < nums.Length; i++)
				{
					nums[i].BuilderString(sb);
					if (i + 1 < nums.Length) sb.Append(',');
				}
			sb.Append('\'');
		}
	}
	public static void AddColorParam(StringBuilder sb, string name, Color color)
	{
		if (color != Color.Transparent)
		{
			sb.Append(' ').Append(name).Append("='").Append(HtmlManager.GetColorToString(color)).Append('\'');
		}
	}
	public static void AddColorParam4(StringBuilder sb, string name, Color[] colors)
	{
		if (colors != null)
		{
			sb.Append(' ').Append(name).Append("='");
			if (colors[0] == colors[1] && colors[0] == colors[2] && colors[0] == colors[3])
				sb.Append(HtmlManager.GetColorToString(colors[0]));
			else if (colors[0] == colors[2] && colors[1] == colors[3])
				sb.Append(HtmlManager.GetColorToString(colors[0])).Append(',').Append(HtmlManager.GetColorToString(colors[1]));
			else if (colors[1] == colors[3])
				sb.Append(HtmlManager.GetColorToString(colors[0])).Append(',').Append(HtmlManager.GetColorToString(colors[1]))
					.Append(',').Append(HtmlManager.GetColorToString(colors[2]));
			else
				for (int i = 0; i < colors.Length; i++)
				{
					sb.Append(HtmlManager.GetColorToString(colors[i]));
					if (i + 1 < colors.Length) sb.Append(',');
				}
			sb.Append('\'');
		}
	}
	public static void AddTagArg(StringBuilder sb, string name, string value)
	{
		sb.Append(' ').Append(name).Append("='").Append(value).Append('\'');
	}
	public static long TimePoint()
	{
		if (!stopwatch.IsRunning) stopwatch.Start();
		return stopwatch_base + stopwatch.Elapsed.Ticks;
	}
	/// <summary>
	///  ※重要※絶対パスで返すので返り値でEmueraの互換性を保つならGetRelativePathを通すこと！
	/// </summary>
	public static string GetValidPath(string path)
	{
		//andEmuera: '/'→'\' の固定置換は Android(ext4) では区切りにならず、
		//"resources/1001ペコリーヌ/顔_デフォルト.webp" が丸ごと 1 個のファイル名になってしまう。
		//区切りの正規化と ".." によるゲームフォルダ脱出の防止は PortablePath にまとめた
		//(上流の Replace("..\\","") は String.Replace が再走査しないため "....//" で抜けられた)。
		return PortablePath.CombineUnderRoot(Program.ExeDir, path);
	}
	public static void MixedNum4ToInt4(MixedNum[] mnums, ref int[] nums)
	{
		if (mnums != null)
		{
			CreateIfNull(ref nums);
			for (int i = 0; i < 4; i++)
				nums[i] = MixedNum.ToPixel(mnums[i]);
		}
	}
	// filepathの安全性(ゲームフォルダ以外のフォルダか)を確認しない
	static public Bitmap LoadImage(string filepath)
	{
		Bitmap bmp = null;
		//FileStream fs = null;
		if (!File.Exists(filepath)) return null;
		try
		{
			/*				fs = new FileStream(filepath, FileMode.Open);
							var factory = new ImageProcessor.ImageFactory();
							factory.Load(fs);
							bmp = (Bitmap)factory.Image;*/
			if (Path.GetExtension(filepath).ToLower() == ".webp")
			{
				using WebP webp = new();
				bmp = webp.Load(filepath);
			}
			else
			{
				bmp = new Bitmap(filepath);
			}
		}
		catch { }
		return bmp;

	}
	// ビットマップファイルからアイコンファイルをつくる
	public static Icon MakeIconFromBmpFile(Bitmap bmp)
	{
		Image img = bmp;

		Bitmap bitmap = new(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		Graphics g = Graphics.FromImage(bitmap);
		g.DrawImage(img, new Rectangle(0, 0, 256, 256));
		g.Dispose();

		Icon icon = Icon.FromHandle(bitmap.GetHicon());

		img.Dispose();
		bitmap.Dispose();
		return icon;
	}

	public sealed class DataTable
	{
		static Type[] builtInDTTypes = [typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(string)];
		static Dictionary<Type, string> builtInDictDTTypeNames = new()
		{
				{ typeof(sbyte), "int8" },
				{ typeof(short), "int16" },
				{ typeof(int), "int32" },
				{ typeof(long), "int64" },
				{ typeof(string), "string" },
			};
		static Dictionary<string, Type> builtInDictDTTypeNames_R = new()
		{
				{ "int8", typeof(sbyte) },
				{ "int16", typeof(short) },
				{ "int32", typeof(int) },
				{ "int64", typeof(long) },
				{ "string", typeof(string) },
			};
		public static long TypeToInt(Type t)
		{
			if (t == typeof(sbyte)) return 1;
			if (t == typeof(short)) return 2;
			if (t == typeof(int)) return 3;
			if (t == typeof(long)) return 4;
			if (t == typeof(string)) return 5;
			return long.MaxValue;
		}
		public static Type IntToType(long i)
		{
			if (i > 0 && i <= builtInDTTypes.Length) return builtInDTTypes[i - 1];
			return null;
		}
		public static Type NameToType(string n)
		{
			if (builtInDictDTTypeNames_R.ContainsKey(n)) return builtInDictDTTypeNames_R[n];
			return null;
		}
		public static string TypeToName(Type t)
		{
			if (builtInDictDTTypeNames.ContainsKey(t)) return builtInDictDTTypeNames[t];
			return null;
		}
		public static object ConvertInt(long v, Type t)
		{
			if (t == typeof(sbyte)) return (sbyte)Math.Min(Math.Max(v, sbyte.MinValue), sbyte.MaxValue);
			if (t == typeof(short)) return (short)Math.Min(Math.Max(v, short.MinValue), short.MaxValue);
			if (t == typeof(int)) return (int)Math.Min(Math.Max(v, int.MinValue), int.MaxValue);
			return v;
		}
	}
}

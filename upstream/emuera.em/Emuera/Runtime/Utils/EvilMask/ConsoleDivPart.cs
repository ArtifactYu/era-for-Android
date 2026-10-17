using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.UI.Game;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using static MinorShift.Emuera.Runtime.Utils.EvilMask.Shape;
using static MinorShift.Emuera.Runtime.Utils.EvilMask.Utils;

namespace MinorShift.Emuera.Runtime.Utils.EvilMask;

#region 統合_netEmuera
/// <summary>
/// divの配置方法。Relative / Absolute は EM+EE 従来のもの。
/// AbsoluteLeftTop / AbsoluteLeftBottom は .netEmuera 由来で、原点の取り方が Absolute と違う。
/// </summary>
enum DivDisplayMode
{
	Relative,
	Absolute,
	AbsoluteLeftTop,
	AbsoluteLeftBottom,
}
#endregion

class ConsoleDivPart : AConsoleDisplayNode
{
	public ConsoleDivPart(MixedNum xPos, MixedNum yPos, MixedNum width, MixedNum height, int depth, int color, StyledBoxModel box, DivDisplayMode display, ConsoleDisplayLine[] childs, bool contentBox = false)
	{
		//統合_netEmuera_contentbox: width/height で書かれた大きさは CSS どおり中身のぶん
		this.contentBox = contentBox;
		backgroundColor = color >= 0 ? Color.FromArgb((int)(color | 0xff000000)) : Color.Transparent;
		StringBuilder sb = new();
		//統合_netEmuera: width/height は省略可。省略時は中身から算出する
		if (width != null) width.num = Math.Abs(width.num);
		if (height != null) height.num = Math.Abs(height.num);
		sb.Append("<div");
		AddTagMixedNumArg(sb, "xpos", xPos);
		AddTagMixedNumArg(sb, "ypos", yPos);
		AddTagMixedNumArg(sb, "width", width);
		AddColorParam(sb, "color", backgroundColor);
		AddTagMixedNumArg(sb, "height", height);
		if (box != null)
		{
			AddTagMixedParam(sb, "margin", box.margin);
			MixedNum4ToInt4(box.margin, ref margin);
			AddTagMixedParam(sb, "padding", box.padding);
			MixedNum4ToInt4(box.padding, ref padding);
			AddTagMixedParam(sb, "border", box.border);
			MixedNum4ToInt4(box.border, ref border);
			AddTagMixedParam(sb, "radius", box.radius);
			MixedNum4ToInt4(box.radius, ref radius);
			if (box.color != null)
			{
				borderColors = new Color[4];
				for (int i = 0; i < 4; i++)
					borderColors[i] = box.color[i] >= 0 ? Color.FromArgb((int)(box.color[i] | 0xff000000)) : Color.Transparent;
				AddColorParam4(sb, "bcolor", borderColors);
			}
		}
		sb.Append(">");
		altHeadTag = sb.ToString();
		Text = string.Empty;
		xOffset = MixedNum.ToPixel(xPos, 0);
		#region EE_div各要素の修正
		if (margin != null) divXOffset += margin[Direction.Left];
		if (padding != null) divXOffset += padding[Direction.Left];
		//統合_netEmuera_contentbox: .netEmuera の border は箱の縁に引く線で、中身の原点はずらさない
		if (border != null && !contentBox) divXOffset += border[Direction.Left];
		#endregion
		PointY = MixedNum.ToPixel(yPos, 0);

		#region EE_div各要素の修正
		if (margin != null) yOffset += margin[Direction.Top];
		if (padding != null) yOffset += padding[Direction.Top];
		if (border != null && !contentBox) yOffset += border[Direction.Top];
		#endregion

		//統合_netEmuera: 省略時は中身に合わせる。
		//レイアウト上の大きさは .netEmuera と同じ「行数×行高」「中身の幅」にする。
		//画像のように行高を超える中身はこれだとクリップで消えてしまうので、
		//描画時のクリップだけ実寸まで広げる（contentWidth / contentHeight）
		autoSized = width == null || height == null;
		this.width = width != null ? MixedNum.ToPixel(width, 0) : AutoWidth(childs);
		Height = height != null ? MixedNum.ToPixel(height, 0) : childs.Length * Config.Config.LineHeight + EdgeSize(Direction.Top) + EdgeSize(Direction.Bottom);
		if (autoSized)
		{
			contentWidth = Math.Max(this.width, AutoWidth(childs));
			contentHeight = Math.Max(Height, ContentBottom(childs) + EdgeSize(Direction.Top) + EdgeSize(Direction.Bottom));
		}
		children = childs;
		LinkNestedDivParents(childs);
		Depth = depth;
		Display = display;

		ShiftChildrenX(PointX + xOffset + divXOffset);
	}

	#region 統合_netEmuera
	private int AutoWidth(ConsoleDisplayLine[] childs)
	{
		int content = 0;
		foreach (var line in childs)
			foreach (var button in line.Buttons)
			{
				if (button == null)
					continue;
				content = Math.Max(content, button.PointX + button.Width);
				//入れ子の div は表示ノードとしての Width が 0 なので、自分で右端を出す
				if (button.StrArray != null)
					foreach (var part in button.StrArray)
						if (part is ConsoleDivPart nested)
							content = Math.Max(content, nested.LayoutRight);
			}
		return content + EdgeSize(Direction.Left) + EdgeSize(Direction.Right);
	}

	/// <summary>親の自動サイズ計算に使う、この div の右端</summary>
	private int LayoutRight => PointX + xOffset + width;

	#region 統合_netEmuera_button内div
	/// <summary>
	/// 入れ子の div に、それを包んでいるボタンを覚えさせる。
	/// 当たり判定と描画が「この div ごとボタンの中身」であることを知るのに使う。
	/// 最上位の行なら addDisplayLine が同じことをしているが、div の中の行はそこを通らない
	/// </summary>
	private static void LinkNestedDivParents(ConsoleDisplayLine[] childs)
	{
		foreach (var line in childs)
			foreach (var button in line.Buttons)
			{
				if (button?.StrArray == null)
					continue;
				foreach (var part in button.StrArray)
					if (part is ConsoleDivPart nested)
						nested.Parent = button;
			}
	}
	#endregion

	//画像は1行に収まらないので、各ノードの下端から中身の実高さを求める
	private static int ContentBottom(ConsoleDisplayLine[] childs)
	{
		int lineHeight = Config.Config.LineHeight;
		int bottom = 0;
		for (int i = 0; i < childs.Length; i++)
		{
			int lineY = i * lineHeight;
			foreach (var button in childs[i].Buttons)
			{
				if (button?.StrArray == null)
					continue;
				foreach (var part in button.StrArray)
					if (part != null)
						bottom = Math.Max(bottom, lineY + PartBottom(part));
			}
		}
		return bottom;
	}

	#region 統合_netEmuera_fontサイズ
	/// <summary>
	/// ノードの下端。&lt;font size&gt; で大きくした文字は AConsoleDisplayPart.Bottom が
	/// 設定フォントサイズを返すので、そのままだと自動サイズの div が中身より低くなり
	/// 大きな文字が途中で切れる（ShinEraTenseiP のエンカウント表示がこれ）
	/// </summary>
	private static int PartBottom(AConsoleDisplayNode part)
	{
		if (part is ConsoleStyledString css && css.StringStyle.FontSize > 0)
			return Math.Max(part.Bottom, css.StringStyle.FontSize);
		return part.Bottom;
	}
	#endregion

	private int EdgeSize(int direction)
	{
		int size = 0;
		if (margin != null) size += margin[direction];
		if (border != null) size += border[direction];
		if (padding != null) size += padding[direction];
		return size;
	}
	#endregion
	int pointX;
	int xOffset;
	#region EE_div各要素の修正
	int divXOffset;
	int yOffset;
	#endregion
	int width;
	#region 統合_netEmuera
	readonly bool autoSized;
	readonly int contentWidth;
	readonly int contentHeight;
	#region 統合_netEmuera_contentbox
	/// <summary>width/height で大きさを書いた div。枠(margin/border/padding)は中身を削らない</summary>
	readonly bool contentBox;
	#endregion
	#endregion
	public override int PointX
	{
		get { return pointX; }
		set
		{
			var diff = value - pointX;
			pointX = value;
			#region EE_div各要素の修正
			//foreach (var child in children)
			//    child.ShiftPositionX(value + xOffset + divXOffset);
			ShiftChildrenX(diff);
			#endregion
		}
	}
	int PointY;
	int Height;
	int[] margin, padding, radius, border;
	Color[] borderColors;
	Color backgroundColor;
	string altHeadTag;
	readonly ConsoleDisplayLine[] children;
	public bool IsEscaped { get; set; }
	public override int Top { get { return PointY; } }
	public override int Bottom { get { return PointY + Height; } }
	public DivDisplayMode Display { get; private set; }
	public bool IsRelative => Display == DivDisplayMode.Relative;
	public ConsoleDisplayLine[] Children { get { return children; } }

	public override bool CanDivide => false;
	public ConsoleButtonString TestChildHitbox(int pointX, int pointY, int relPointY)
	{
		ConsoleButtonString pointing = null;
		#region 統合_netEmuera_div当たり判定
		//当たり判定の矩形を DrawTo と揃える。外枠がそのまま当たり判定で、
		//枠(margin/border/padding)の内側が中身の原点。
		//従来は Y だけ枠のぶん下げて高さは外枠のままだったので、枠の厚みぶん下にずれていた
		//絶対配置は DrawTo と同じ原点の取り方をする。相対配置の式で出すと縦位置がまるごとずれる
		int boxLeft = IsRelative ? PointX + xOffset : xOffset;
		int boxTop;
		if (IsRelative)
			boxTop = relPointY + PointY;
		else
		{
			int clientHeight = GlobalStatic.EMediator.Console.Window.MainPicBox.Height;
			boxTop = Display switch
			{
				DivDisplayMode.AbsoluteLeftTop => PointY,
				DivDisplayMode.AbsoluteLeftBottom => clientHeight + PointY,
				_ => clientHeight - PointY - Height,
			};
		}
		int contentTop = boxTop + yOffset;
		var rect = new Rectangle(boxLeft, boxTop, width, Height);
		//自動サイズのときは DrawTo が中身の実寸までクリップを広げるので、判定もそこまで広げる。
		//行高を超える画像を入れた div は、これが無いとはみ出した部分を押せない
		if (autoSized)
			rect = Rectangle.Union(rect, new Rectangle(boxLeft + divXOffset, contentTop, contentWidth, contentHeight));
		#endregion
		if (!rect.Contains(pointX, pointY)) return null;
		relPointY = contentTop;
		foreach (var line in children)
		{
			for (int b = 0; b < line.Buttons.Length; b++)
			{
				ConsoleButtonString button = line.Buttons[line.Buttons.Length - b - 1];
				if (button == null || button.StrArray == null)
					continue;
				#region 統合_netEmuera_button内div
				//入れ子の div は表示ノードとしての Width が 0 なので、中身が div だけのボタンは
				//X 範囲も 0 幅になる。下の X 判定では入れないので、先に div の中を見る
				foreach (AConsoleDisplayNode part in button.StrArray)
				{
					if (part is not ConsoleDivPart nested)
						continue;
					ConsoleButtonString childPointing = nested.TestChildHitbox(pointX, pointY, relPointY);
					if (childPointing == null)
						continue;
					pointing = childPointing;
					if (pointing.IsButton)
						return pointing;
				}
				#endregion
				if (button.PointX <= pointX && button.PointX + button.Width >= pointX)
				{
					//if (relPointY >= 0 && relPointY <= Config.Config.FontSize)
					//{
					//	pointing = button;
					//	if(pointing.IsButton)
					//		goto breakfor;
					//}
					foreach (AConsoleDisplayNode part in button.StrArray)
					{
						if (part == null)
							continue;
						if (part.PointX <= pointX && part.PointX + part.Width >= pointX
							&& relPointY + part.Top <= pointY && relPointY + part.Bottom >= pointY)
						{
							pointing = button;
							if (pointing.IsButton)
								return pointing;
						}
					}
				}
			}
			relPointY += Config.Config.LineHeight;
		}
		#region 統合_netEmuera_button内div
		//中にボタンが無くても、この div ごとボタンの中身なら div の矩形を当たり判定にする。
		//ここまで来ているのは矩形の中を指しているとき。
		//<button><div>…</div></button>（ShinEraTenseiP の SET_BUTTON_TAG がこの形）では
		//div は表示ノードとしての Width が 0 なので、親ボタン側の判定では拾えない
		if ((pointing == null || !pointing.IsButton) && Parent != null && Parent.IsButton)
			pointing = Parent;
		#endregion
		return pointing;
	}
	public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, bool isFocus, TextDrawingMode mode, bool isButton = false)
	{
		if (GlobalStatic.EMediator.Console.Window == null) return;
		#region 統合_netEmuera_button内div
		//この div ごとボタンの中身なら、そのボタンの選択状態で中身を描く。
		//ボタン自身（ConsoleButtonString.DrawTo）は div を読み飛ばすので、ここで伝えないと色が変わらない
		if (Parent != null && Parent.IsButton)
		{
			isSelecting |= GlobalStatic.EMediator.Console.ButtonIsPointing(Parent);
			isFocus |= GlobalStatic.EMediator.Console.ButtonIsSelected(Parent);
		}
		#endregion
		//統合_netEmuera: AbsoluteLeftTop / AbsoluteLeftBottom は .netEmuera の原点の取り方に合わせる
		//（LeftBottom は画面下端 + ypos。ypos に負値を渡して下から積む使い方をする）
		int clientHeight = GlobalStatic.EMediator.Console.Window.MainPicBox.Height;
		var rect = Display switch // 何故か+2pxが必要，なぞ
		{
			DivDisplayMode.Relative => new Rectangle(PointX + xOffset, pointY + PointY, width + 2, Height),
			DivDisplayMode.AbsoluteLeftTop => new Rectangle(xOffset, PointY, width + 2, Height),
			DivDisplayMode.AbsoluteLeftBottom => new Rectangle(xOffset, clientHeight + PointY, width + 2, Height),
			_ => new Rectangle(xOffset, clientHeight - PointY - Height, width + 2, Height),
		};

		if (margin != null)
			rect = new Rectangle(rect.X + margin[Direction.Left], rect.Y + margin[Direction.Top],
				 rect.Width - margin[Direction.Left] - margin[Direction.Right], rect.Height - margin[Direction.Top] - margin[Direction.Bottom]);
		graph.SetClip(rect, CombineMode.Replace);
		//統合_netEmuera_contentbox: 枠と背景を描く矩形。content-box のときは中身のクリップもここまで許す
		var boxRect = rect;

		var pxMode = graph.PixelOffsetMode;
		graph.PixelOffsetMode = PixelOffsetMode.HighQuality; // ここを高品質にしておく、全体的高品質してもいいかな？
		BoxBorder.DrawBorder(graph, rect, border, radius, borderColors, backgroundColor);
		graph.PixelOffsetMode = pxMode;

		//統合_netEmuera_contentbox: .netEmuera の border は箱の縁に引く線で、中身の場所には効かない
		if (border != null && !contentBox)
			rect = new Rectangle(rect.X + border[Direction.Left], rect.Y + border[Direction.Top],
				 rect.Width - border[Direction.Left] - border[Direction.Right], rect.Height - border[Direction.Top] - border[Direction.Bottom]);

		if (padding != null)
			rect = new Rectangle(rect.X + padding[Direction.Left], rect.Y + padding[Direction.Top],
				 rect.Width - padding[Direction.Left] - padding[Direction.Right], rect.Height - padding[Direction.Top] - padding[Direction.Bottom]);

		//中身の原点は枠(border/padding)の内側。クリップだけをこの後で広げる
		int contentTop = rect.Y;

		//統合_netEmuera: 自動サイズのときは中身をクリップで削らない。
		//枠と背景は行数ベースの大きさ（=.netEmuera と同じ）で描き、中身だけ実寸まで許す
		if (autoSized)
			rect = new Rectangle(rect.X, rect.Y, Math.Max(rect.Width, contentWidth), Math.Max(rect.Height, contentHeight));

		#region 統合_netEmuera_contentbox
		//width/height で書かれた大きさは CSS どおり中身のぶんなので、枠のぶんだけ中身を削らない。
		//.netEmuera は div の中身をクリップしないので、箱いっぱいまで出す
		if (contentBox)
			rect = Rectangle.Union(rect, boxRect);
		#endregion

		graph.SetClip(rect, CombineMode.Replace);

		pointY = contentTop;
		foreach (var child in children)
		{
			child.DrawTo(graph, pointY, isBackLog, true, mode, isSelecting, isFocus);
			//統合_netEmuera_div入れ子: ConsoleButtonString.DrawTo は div を読み飛ばすので、
			//入れ子の div はここで自分で描く
			DrawNestedDivs(graph, child, pointY, isSelecting, isFocus, isBackLog, mode);
			pointY += Config.Config.LineHeight;
		}
		graph.ResetClip();
	}

	#region 統合_netEmuera_island内div
	/// <summary>行が直接持っている div を描く。HTML_PRINT_ISLAND のように
	/// 奥行きごとの描画（ConsoleEscapedParts）を通らない行のためのもの</summary>
	internal static void DrawDivsIn(Graphics graph, ConsoleDisplayLine line, int pointY, TextDrawingMode mode)
	{
		DrawNestedDivs(graph, line, pointY, false, false, false, mode);
	}
	#endregion

	#region 統合_netEmuera_div入れ子
	private static void DrawNestedDivs(Graphics graph, ConsoleDisplayLine line, int pointY, bool isSelecting, bool isFocus, bool isBackLog, TextDrawingMode mode)
	{
		foreach (var button in line.Buttons)
		{
			if (button?.StrArray == null)
				continue;
			foreach (var part in button.StrArray)
				if (part is ConsoleDivPart nested)
					nested.DrawTo(graph, pointY, isSelecting, isFocus, isBackLog, mode);
		}
	}
	#endregion

    private void ShiftChildrenX(int diff)
	{
		foreach (var child in children)
			child.ShiftPositionX(diff);
	}

	public override void SetWidth(StringMeasure sm, float subPixel)
	{
	}

	public override string ToString()
	{
		var sb = new StringBuilder();
		sb.Append(altHeadTag);
		foreach (var line in children)
		{
			line.BuildString(sb);
			sb.Append("\r\n");
		}
		sb.Append("</div>");
		return sb.ToString();
	}
	public override StringBuilder BuildString(StringBuilder sb)
	{
		sb.Append(altHeadTag);
		foreach (var line in children)
		{
			line.BuildString(sb);
			sb.Append("\r\n");
		}
		sb.Append("</div>");
		return sb;
	}
}

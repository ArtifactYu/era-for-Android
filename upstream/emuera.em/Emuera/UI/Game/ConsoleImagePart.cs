using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.UI.Game.Image;
using System;
using System.Drawing;
using System.Text;
using static MinorShift.Emuera.Runtime.Utils.EvilMask.Utils;

namespace MinorShift.Emuera.UI.Game;

sealed class ConsoleImagePart : AConsoleDisplayNode
{
	#region EM_私家版_HTMLパラメータ拡張
	//public ConsoleImagePart(string resName, string resNameb, int raw_height, int raw_width, int raw_ypos)
	public ConsoleImagePart(string resName, string resNameb, string resNamem, MixedNum raw_height, MixedNum raw_width, MixedNum raw_ypos)
	{
		top = 0;
		bottom = Config.FontSize;
		Text = "";
		ResourceName = resName ?? "";
		ButtonResourceName = resNameb;
		MappingGraphName = resNamem;
		StringBuilder sb = new();
		sb.Append("<img src='").Append(ResourceName).Append('\'');
		if (ButtonResourceName != null)
			AddTagArg(sb, "srcb", ButtonResourceName);
		//{
		//	sb.Append("' srcb='");
		//	sb.Append(ButtonResourceName);
		//}
		if (!string.IsNullOrEmpty(MappingGraphName))
			AddTagArg(sb, "srcm", MappingGraphName);
		AddTagMixedNumArg(sb, "height", raw_height);
		AddTagMixedNumArg(sb, "width", raw_width);
		AddTagMixedNumArg(sb, "ypos", raw_ypos);
		//{
		//	sb.Append("' srcm='");
		//	sb.Append(MappingGraphName);
		//}
		//if(raw_height != 0)
		//	if (raw_height != null && raw_height.num != 0)
		//	{
		//	sb.Append("' height='");
		//	sb.Append(raw_height.num.ToString());
		//	if (raw_height.isPx) sb.Append("px");
		//}
		//if(raw_width != 0)
		//if (raw_width != null && raw_width.num != 0)
		//	{
		//	sb.Append("' width='");
		//	sb.Append(raw_width.num.ToString());
		//	if (raw_width.isPx) sb.Append("px");
		//}
		////if(raw_ypos != 0)
		//if(raw_ypos != null && raw_ypos.num != 0)
		//	{
		//	sb.Append("' ypos='");
		//	sb.Append(raw_ypos.num.ToString());
		//	if (raw_ypos.isPx) sb.Append("px");
		//}
		// sb.Append("'>");
		sb.Append(">");
		AltText = sb.ToString();
		cImage = AppContents.GetSprite(ResourceName);
		//if (cImage != null && !cImage.IsCreated)
		//	cImage = null;
		if (cImage == null)
		{
			Text = AltText;
			return;
		}
		int height;
		//if (raw_height == 0)//HTMLで高さが指定されていない又は0が指定された場合、フォントサイズをそのまま高さ(px単位)として使用する。
		if (raw_height == null || raw_height.num == 0)//HTMLで高さが指定されていない又は0が指定された場合、フォントサイズをそのまま高さ(px単位)として使用する。
			height = Config.FontSize;
		// else//HTMLで高さが指定された場合、フォントサイズの100分率と解釈する。
		//	height = Config.Config.FontSize * raw_height / 100;
		else if (raw_height.isPx)//HTMLで高さがpx指定された場合、そのまま使う。
			height = raw_height.num;
		else // フォントサイズの100分率と解釈する。
			height = Config.FontSize * raw_height.num / 100;
		//幅が指定されていない又は0が指定された場合、元画像の縦横比を維持するように幅(px単位)を設定する。1未満は端数としてXsubpixelに記録。
		//負の値が指定される可能性があるが、最終的なWidthは正の値になるようにあとで調整する。
		//if (raw_width == 0)
		if (raw_width == null || raw_width.num == 0)
		{
			Width = cImage.DestBaseSize.Width * height / cImage.DestBaseSize.Height;
			XsubPixel = (float)cImage.DestBaseSize.Width * height / cImage.DestBaseSize.Height - Width;
		}
		else if (raw_width.isPx)
		{
			Width = raw_width.num;
		}
		else
		{
			// Width = Config.Config.FontSize * raw_width / 100;
			// XsubPixel = ((float)Config.Config.FontSize * raw_width / 100f) - Width;
			Width = Config.FontSize * raw_width.num / 100;
			XsubPixel = (float)Config.FontSize * raw_width.num / 100f - Width;
		}
		//top = raw_ypos * Config.Config.FontSize / 100;
		top = raw_ypos != null ? raw_ypos.isPx ? raw_ypos.num : raw_ypos.num * Config.FontSize / 100 : 0;
		destRect = new Rectangle(0, top, Width, height);
		if (destRect.Width < 0)
		{
			destRect.X = -destRect.Width;
			Width = -destRect.Width;
		}
		if (destRect.Height < 0)
		{
			destRect.Y = destRect.Y - destRect.Height;
			height = -destRect.Height;
		}
		bottom = top + height;
		//if(top > 0)
		//	top = 0;
		//if(bottom < Config.Config.FontSize)
		//	bottom = Config.Config.FontSize;
		if (ButtonResourceName != null)
		{
			cImageB = AppContents.GetSprite(ButtonResourceName);
			//if (cImageB != null && !cImageB.IsCreated)
			//	cImageB = null;
		}
		if (MappingGraphName != null)
		{
			cImageM = AppContents.GetSprite(MappingGraphName);
		}
	}
	public readonly string MappingGraphName;
	private readonly ASprite cImageM;
	#endregion
	private readonly ASprite cImage;
	private readonly ASprite cImageB;
	private readonly int top;
	private readonly int bottom;
	private readonly Rectangle destRect;
	//#pragma warning disable CS0649 // フィールド 'ConsoleImagePart.ia' は割り当てられません。常に既定値 null を使用します。
	//		private readonly ImageAttributes ia;
	//#pragma warning restore CS0649 // フィールド 'ConsoleImagePart.ia' は割り当てられません。常に既定値 null を使用します。
	public readonly string ResourceName;
	public readonly string ButtonResourceName;
	public override int Top { get { return top; } }
	public override int Bottom { get { return bottom; } }

	public override bool CanDivide { get { return false; } }
	public override void SetWidth(StringMeasure sm, float subPixel)
	{
		if (Error)
		{
			Width = 0;
			return;
		}
		if (cImage != null)
			return;
		Width = sm.GetDisplayLength(Text, Config.DefaultFont);
		XsubPixel = subPixel;
	}

	public override string ToString()
	{
		if (AltText == null)
			return "";
		return AltText;
	}
	#region EM_私家版_描画拡張
	public override StringBuilder BuildString(StringBuilder sb)
	{
		if (AltText != null) sb.Append(AltText);
		return sb;
	}
	#endregion
	#region EM_私家版_imgマースク
	public long GetMappingColor(int pointX, int pointY)
	{
		if (cImageM != null && cImageM.IsCreated)
		{
			Size spriteSize;
			if (cImageM is SpriteF sf)
			{
				spriteSize = sf.DestBaseSize;
			}
			else if (cImageM is SpriteG sg)
			{
				spriteSize = sg.DestBaseSize;
			}
			else return 0;
			pointX = pointX * spriteSize.Width / destRect.Width;
			pointY = pointY * spriteSize.Height / destRect.Height;
			var c = cImageM.SpriteGetColor(pointX, pointY);
			return c.ToArgb() & 0xFFFFFF;
		}
		return 0;
	}
	#endregion
	public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isFocus, bool isBackLog, TextDrawingMode mode, bool isButton = false)
	{
		if (!MinorShift.Emuera.Api.DisplayOptions.ShowImages) return;
		if (Error)
			return;
		ASprite img = cImage;
		if ((isSelecting || isFocus) && cImageB != null)
			img = cImageB;

		if (img != null && img.IsCreated)
		{
			Rectangle rect = destRect;
			//PointX微調整
			rect.X = destRect.X + PointX + Config.DrawingParam_ShapePositionShift;
			rect.Y = destRect.Y + pointY;
			img.GraphicsDraw(graph, rect);
		}
		else
		{
			if (mode == TextDrawingMode.GRAPHICS)
				graph.DrawString(AltText, Config.DefaultFont, new SolidBrush(Config.ForeColor), new Point(PointX, pointY));
			else
				System.Windows.Forms.TextRenderer.DrawText(graph, AltText.AsSpan(), Config.DefaultFont, new Point(PointX, pointY), Config.ForeColor, System.Windows.Forms.TextFormatFlags.NoPrefix);
		}
	}
}

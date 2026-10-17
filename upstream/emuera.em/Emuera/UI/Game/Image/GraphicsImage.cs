using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.UI.Game.Image;

internal sealed class GraphicsImage : AbstractImage
{
	//public Bitmap Bitmap;
	//public IntPtr GDIhDC { get; protected set; }
	//protected Graphics g;
	//protected IntPtr hBitmap;
	//protected IntPtr hDefaultImg;

	public GraphicsImage(int id)
	{
		ID = id;
		g = null;
		Bitmap = null;
		//created = false;
		//locked = false;
	}
	public readonly int ID;
	Size size;
	Brush brush;
	Pen pen;
	Font font;
	#region EE_GDRAWTEXT
	FontStyle style;
	#endregion

	//Bitmap b;
	//Graphics g;
	// 当GraphicsImage是完全由图像拼接而成时，此处记录拼接图案的列表。
	// 可清理图片来减少内存使用。在使用时按照此列表组合
	public bool useImgList { get { return drawImgList != null; } }
	public List<Tuple<ASprite, Rectangle>> drawImgList;


	////bool created;
	////bool locked;
	//public void LockGraphics()
	//{
	//	//if (locked)
	//	//	return;
	//	//g = Graphics.FromImage(b);
	//	//locked = true;
	//}
	//public void UnlockGraphics()
	//{
	//	//if (!locked)
	//	//	return;
	//	//g.Dispose();
	//	//g = null;
	//	//locked = false;
	//}

	public Bitmap RealBitmap;
	public override Bitmap Bitmap
	{
		set { RealBitmap = value; }
		get
		{
			Load();
			return RealBitmap;
		}
	}

	#region Bitmap書き込み・作成

	/// <summary>
	/// GCREATE(int ID, int width, int height)
	/// Graphicsの基礎となるBitmapを作成する。エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GCreate(int x, int y, bool useGDI)
	{
		if (useGDI)
			throw new NotImplementedException();
		GDispose();
		RealBitmap = new Bitmap(x, y, PixelFormat.Format32bppArgb);
		size = new Size(x, y);
		g = Graphics.FromImage(RealBitmap);
		drawImgList = [];
		lock (AppContents.tempLoadedGraphicsImages)
			AppContents.tempLoadedGraphicsImages.Add(this);
	}
	internal void GCreateFromF(Bitmap bmp, bool useGDI)
	{
		if (useGDI)
			throw new NotImplementedException();
		GDispose();
		RealBitmap = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
		size = new Size(bmp.Width, bmp.Height);
		g = Graphics.FromImage(RealBitmap);
		g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
	}

	/// <summary>
	/// GCLEAR(int ID, int cARGB)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GClear(Color c)
	{
		#region 統合_GCLEARのアンロード対応
		//範囲指定版（下の GClear）と同じ手順にする。
		//UnLoad 済みなら描く前に読み直し、塗ったあとは「スプライトから組み直せる画像」ではなくなるので
		//drawImgList を落とす。落とさないと UnloadTempLoadedGraphicsImageNames に
		//また外され、次の GCLEAR が g == null で落ちる
		Load();
		#endregion
		if (g == null)
			throw new NullReferenceException();
		g.Clear(c);
		#region 統合_GCLEARのアンロード対応
		drawImgList = null;
		#endregion
	}

	#region EM_私家版_GCLEAR拡張
	public void GClear(Color c, int x, int y, int w, int h)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		g.SetClip(new Rectangle(x, y, w, h), CombineMode.Replace);
		g.Clear(c);
		g.ResetClip();
		drawImgList = null;
	}
	#endregion

	/// <summary>
	/// GDRAWTEXTGDRAWTEXT int ID, str text, int x, int y
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	#region EE_GDRAWTEXT 元のソースコードにあったものを改良
	public void GDrawString(string text, int x, int y)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		Font usingFont = font;
		var format = new StringFormat(StringFormat.GenericTypographic);
		if (usingFont == null)
			usingFont = new(Config.FontName, 100, GlobalStatic.Console.StringStyle.FontStyle, GraphicsUnit.Pixel);
		GraphicsPath gp =
			new();
		//一部のフォントで描画がずれる問題修正
		float emSize = (float)usingFont.Height * usingFont.FontFamily.GetEmHeight(usingFont.Style) / usingFont.FontFamily.GetLineSpacing(usingFont.Style);
		gp.AddString(text, usingFont.FontFamily, (int)usingFont.Style, emSize, new Point(x, y), format);
		g.SmoothingMode = SmoothingMode.AntiAlias;
		if (brush != null)
			g.FillPath(brush, gp);
		else
			g.FillPath(new SolidBrush(Config.ForeColor), gp);

		if (pen != null)
			g.DrawPath(pen, gp);
		else
			g.DrawPath(new Pen(Config.ForeColor), gp);

	}
	#endregion

	/// <summary>
	/// GDRAWTEXT int ID, str text, int x, int y, int width, int height
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawString(string text, int x, int y, int width, int height)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		Font usingFont = font;
		if (usingFont == null)
			usingFont = Config.DefaultFont;
		if (brush != null)
		{
			g.DrawString(text, usingFont, brush, new RectangleF(x, y, width, height));
		}
		else
		{
			using var b = new SolidBrush(Config.ForeColor);
			g.DrawString(text, usingFont, b, x, y);
		}
	}

	/// <summary>
	/// GDRAWRECTANGLE(int ID, int x, int y, int width, int height)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawRectangle(Rectangle rect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		if (pen != null)
		{
			g.DrawRectangle(pen, rect);
		}
		else
		{
			using var p = new Pen(Config.ForeColor);
			g.DrawRectangle(p, rect);
		}
	}

	/// <summary>
	/// GFILLRECTANGLE(int ID, int x, int y, int width, int height)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GFillRectangle(Rectangle rect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		if (brush != null)
		{
			g.FillRectangle(brush, rect);
		}
		else
		{
			using var b = new SolidBrush(Config.BackColor);
			g.FillRectangle(b, rect);
		}
	}

	/// <summary>
	/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawCImg(ASprite img, Rectangle destRect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		if (useImgList)
		{
			if (img as SpriteG != null)
			{
				SpriteG imgG = img as SpriteG;
				if (imgG.useImgList)
				{
					foreach (Tuple<ASprite, Rectangle> img_element in imgG.drawImgList)
					{
						if (imgG.isBaseImage(this))
						{
							drawImgList = null;
							break;
						}
						drawImgList.Add(new Tuple<ASprite, Rectangle>(
							img_element.Item1,
							new Rectangle(
								(img_element.Item2.X + destRect.X) * destRect.Width / imgG.DestBaseSize.Width,
								(img_element.Item2.Y + destRect.Y) * destRect.Height / imgG.DestBaseSize.Height,
								img_element.Item2.Width * destRect.Width / imgG.DestBaseSize.Width,
								img_element.Item2.Height * destRect.Height / imgG.DestBaseSize.Height
							)
						));
					}
				}
				else
				{
					drawImgList = null;
				}
			}
			else if (img as SpriteF != null)
			{
				drawImgList.Add(
					new Tuple<ASprite, Rectangle>(img, destRect)
				);

				if (drawImgList.Count > 50)
				{
					drawImgList = null;
				}
			}
			else
			{
				drawImgList = null;
			}
		}
		img.GraphicsDraw(g, destRect);
	}

	/// <summary>
	/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight, float[][] cm)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawCImg(ASprite img, Rectangle destRect, float[][] cm)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		ImageAttributes imageAttributes = new();
		ColorMatrix colorMatrix = new(cm);
		imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

		img.GraphicsDraw(g, destRect, imageAttributes);
	}

	/// <summary>
	/// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		Bitmap src = srcGra.GetBitmap();
		g.DrawImage(src, destRect, srcRect, GraphicsUnit.Pixel);
	}


	/// <summary>
	/// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight, float[][] cm)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect, float[][] cm)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		Bitmap src = srcGra.GetBitmap();
		ImageAttributes imageAttributes = new();
		ColorMatrix colorMatrix = new(cm);
		imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
		//g.DrawImage(img.Bitmap, destRect, srcRect, GraphicsUnit.Pixel, imageAttributes);なんでこのパターンないのさ
		g.DrawImage(src, destRect, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, imageAttributes);

	}


	/// <summary>
	/// GDRAWGWITHMASK(int ID, int srcID, int maskID, int destX, int destY)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GDrawGWithMask(GraphicsImage srcGra, GraphicsImage maskGra, Point destPoint)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();

		drawImgList = null;

		Bitmap destImg = GetBitmap();
		byte[] srcBytes = BytesFromBitmap(srcGra.GetBitmap());
		byte[] srcMaskBytes = BytesFromBitmap(maskGra.GetBitmap());
		Rectangle destRect = new(destPoint.X, destPoint.Y, srcGra.Width, srcGra.Height);

		BitmapData bmpData =
			destImg.LockBits(new Rectangle(0, 0, destImg.Width, destImg.Height),
			ImageLockMode.ReadWrite,
			PixelFormat.Format32bppArgb);
		try
		{
			nint ptr = bmpData.Scan0;
			byte[] pixels = new byte[bmpData.Stride * destImg.Height];
			Marshal.Copy(ptr, pixels, 0, pixels.Length);


			for (int y = 0; y < srcGra.Height; y++)
			{

				int destIndex = ((destPoint.Y + y) * destImg.Width + destPoint.X) * 4;
				int srcIndex = ((0 + y) * srcGra.Width + 0) * 4;
				for (int x = 0; x < srcGra.Width; x++)
				{
					if (srcMaskBytes[srcIndex] == 255)//完全不透明
					{
						pixels[destIndex++] = srcBytes[srcIndex++];
						pixels[destIndex++] = srcBytes[srcIndex++];
						pixels[destIndex++] = srcBytes[srcIndex++];
						pixels[destIndex++] = srcBytes[srcIndex++];
					}
					else if (srcMaskBytes[srcIndex] == 0)//完全透明
					{
						destIndex += 4;
						srcIndex += 4;
					}
					else//半透明 alpha/255ではなく（alpha+1）/256で計算しているがたぶん誤差
					{
						int mask = srcMaskBytes[srcIndex]; mask++;
						pixels[destIndex] = (byte)(srcBytes[srcIndex] * mask + pixels[destIndex] * (256 - mask) >> 8); srcIndex++; destIndex++;
						pixels[destIndex] = (byte)(srcBytes[srcIndex] * mask + pixels[destIndex] * (256 - mask) >> 8); srcIndex++; destIndex++;
						pixels[destIndex] = (byte)(srcBytes[srcIndex] * mask + pixels[destIndex] * (256 - mask) >> 8); srcIndex++; destIndex++;
						pixels[destIndex] = (byte)(srcBytes[srcIndex] * mask + pixels[destIndex] * (256 - mask) >> 8); srcIndex++; destIndex++;
					}
				}
			}

			// Bitmapへコピー
			Marshal.Copy(pixels, 0, ptr, pixels.Length);
		}
		finally
		{
			destImg.UnlockBits(bmpData);
		}
	}

	#region EE_GDRAWGWITHROTATE
	/// <summary>
	/// GROTATE(int ID, int angle, int x, int y)
	/// </summary>
	public void GRotate(long a, int x, int y)
	{
		if (g == null)
			throw new NullReferenceException();
		float angle = a;
		g.TranslateTransform(-x, -y, MatrixOrder.Append);
		g.RotateTransform(angle, MatrixOrder.Append);
		g.TranslateTransform(x, y, MatrixOrder.Append);

		g.DrawImageUnscaled(Bitmap, 0, 0);
		//g.DrawImage(Bitmap, new Rectangle(Bitmap.Width, Bitmap.Height, Bitmap.Width, Bitmap.Height));
	}
	/// <summary>
	/// GDRAWGWITHROTATE
	/// </summary>
	public void GDrawGWithRotate(GraphicsImage srcGra, long a, int x, int y)
	{
		if (g == null || srcGra == null)
			throw new NullReferenceException();
		float angle = a;
		g.TranslateTransform(-x, -y, MatrixOrder.Append);
		g.RotateTransform(angle, MatrixOrder.Append);
		g.TranslateTransform(x, y, MatrixOrder.Append);
		Bitmap src = srcGra.GetBitmap();
		g.DrawImage(src, 0, 0);
	}
	#endregion
	#region 統合_netEmuera_G_POLYGON
	//G_POLYGON_POINT_ADD で貯めた頂点を G_POLYGON_DRAW / _FILL で使う。
	//頂点は描画しても消えない。消すには G_POLYGON_POINT_CLEAR を呼ぶ
	List<Point> polygonPoints;

	public void GDrawPolygonAddPoint(int x, int y)
	{
		if (g == null)
			throw new NullReferenceException();
		polygonPoints ??= [];
		polygonPoints.Add(new Point(x, y));
	}

	public void GDrawPolygonClearPoint()
	{
		if (g == null)
			throw new NullReferenceException();
		polygonPoints?.Clear();
	}

	public void GDrawPolygon()
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		//2点だと線分にしかならないので、多角形として意味を持つ3点から
		if (polygonPoints == null || polygonPoints.Count < 3)
			throw new CodeEE(trerror.PolygonNeedsPoints.Text);

		drawImgList = null;
		if (pen != null)
			g.DrawPolygon(pen, [.. polygonPoints]);
		else
		{
			using Pen p = new(Config.ForeColor);
			g.DrawPolygon(p, [.. polygonPoints]);
		}
	}

	public void GFillPolygon()
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		if (polygonPoints == null || polygonPoints.Count < 3)
			throw new CodeEE(trerror.PolygonNeedsPoints.Text);

		drawImgList = null;
		if (brush != null)
			g.FillPolygon(brush, [.. polygonPoints]);
		else
		{
			using SolidBrush b = new(Config.ForeColor);
			g.FillPolygon(b, [.. polygonPoints]);
		}
	}
	#endregion

	#region EE_GDRAWLINE
	public void GDrawLine(int fromX, int fromY, int forX, int forY)
	{
		if (g == null)
			throw new NullReferenceException();

		if (pen != null)
		{
			g.DrawLine(pen, fromX, fromY, forX, forY);
		}
		else
		{
			using (Pen p = new(Config.ForeColor))
				g.DrawLine(p, fromX, fromY, forX, forY);
		}
	}
	#endregion

	#region EE_GDASHSTYLE
	public void GDashStyle(long style, long cap)
	{
		if (g == null)
			throw new NullReferenceException();
		if (pen == null)
			pen = new Pen(Config.ForeColor);

		pen.DashStyle = (DashStyle)style;
		pen.DashCap = (DashCap)cap;
	}
	#endregion

	#region EE_GDRAWTEXT フォントスタイルも指定できるように
	// public void GSetFont(Font r)
	public void GSetFont(Font r, FontStyle fs)
	{
		if (font != null)
			font.Dispose();
		font = r;
		style = fs;
	}
	#endregion
	public void GSetBrush(Brush r)
	{
		if (brush != null)
			brush.Dispose();
		brush = r;
	}
	public void GSetPen(Pen r)
	{
		DashStyle style = DashStyle.Solid;
		DashCap cap = DashCap.Flat;

		if (pen != null)
		{
			style = pen.DashStyle;
			cap = pen.DashCap;
			pen.Dispose();
		}
		pen = r;
		pen.DashStyle = style;
		pen.DashCap = cap;
	}

	#region Bitmap読み込み・削除
	/// <summary>
	/// 未作成ならエラー
	/// </summary>
	public Bitmap GetBitmap()
	{
		if (Bitmap == null)
			throw new NullReferenceException();
		//UnlockGraphics();
		return Bitmap;
	}
	/// <summary>
	/// GSETCOLOR(int ID, int cARGB, int x, int y)
	/// エラーチェックは呼び出し元でのみ行う
	/// </summary>
	public void GSetColor(Color c, int x, int y)
	{
		if (Bitmap == null)
			throw new NullReferenceException();
		//UnlockGraphics();
		Bitmap.SetPixel(x, y, c);
	}

	/// <summary>
	/// GGETCOLOR(int ID, int x, int y)
	/// エラーチェックは呼び出し元でのみ行う。特に画像範囲内であるかどうかチェックすること
	/// </summary>
	public Color GGetColor(int x, int y)
	{
		if (Bitmap == null)
			throw new NullReferenceException();
		//UnlockGraphics();
		return Bitmap.GetPixel(x, y);
	}

	public void UnLoad()
	{
		if (RealBitmap == null)
			return;

		if (g != null)
			g.Dispose();
		if (RealBitmap != null)
			RealBitmap.Dispose();
		g = null;
		RealBitmap = null;
	}

	/// <summary>
	/// GDISPOSE(int ID)
	/// </summary>
	public void GDispose()
	{
		size = new Size(0, 0);
		drawImgList = null;
		if (RealBitmap == null)
			return;
		if (g != null)
			g.Dispose();
		if (RealBitmap != null)
			RealBitmap.Dispose();
		if (brush != null)
			brush.Dispose();
		if (pen != null)
			pen.Dispose();
		if (font != null)
			font.Dispose();
		g = null;
		RealBitmap = null;
		brush = null;
		pen = null;
		font = null;
	}

	public override void Dispose()
	{
		GDispose();
		GC.SuppressFinalize(this);
	}

	~GraphicsImage()
	{
		Dispose();
	}
	#endregion

	#region 状態判定（Bitmap読み書きを伴わない）
	public override bool IsCreated { get { return g != null || useImgList; } }
	/// <summary>
	/// int GWIDTH(int ID)
	/// </summary>
	public int Width { get { return size.Width; } }
	/// <summary>
	/// int GHEIGHT(int ID)
	/// </summary>
	public int Height { get { return size.Height; } }

	#region EE_GDRAWTEXTに付随する様々な要素
	public string Fontname { get { return font.Name; } }
	public int Fontsize { get { return (int)font.Size; } }

	public int Fontstyle
	{
		get
		{
			int ret = 0;
			if ((style & FontStyle.Bold) == FontStyle.Bold)
				ret |= 1;
			if ((style & FontStyle.Italic) == FontStyle.Italic)
				ret |= 2;
			if ((style & FontStyle.Strikeout) == FontStyle.Strikeout)
				ret |= 4;
			if ((style & FontStyle.Underline) == FontStyle.Underline)
				ret |= 8;
			return ret;
		}
	}

	public Font Fnt { get { return font; } }
	public Pen Pen { get { return pen; } }
	public Brush Brush { get { return brush; } }
	#endregion

	#endregion


	private static byte[] BytesFromBitmap(Bitmap bmp)
	{
		BitmapData bmpData = bmp.LockBits(
		  new Rectangle(0, 0, bmp.Width, bmp.Height),
		  ImageLockMode.ReadOnly,  // 書き込むときはReadAndWriteで
		  PixelFormat.Format32bppArgb
		);
		if (bmpData.Stride < 0)
			throw new Exception();//変な形式のが送られてくることはありえないはずだが一応
		byte[] pixels = new byte[bmpData.Stride * bmp.Height];
		try
		{
			nint ptr = bmpData.Scan0;
			Marshal.Copy(ptr, pixels, 0, pixels.Length);
		}
		finally
		{
			bmp.UnlockBits(bmpData);

		}
		return pixels;
	}

	/// <summary>
	/// GTOARRAY int ID, var array
	/// エラーチェックは呼び出し元でのみ行う
	/// <returns></returns>
	public bool GBitmapToInt64Array(long[,] array, int xstart, int ystart)
	{
		if (g == null || Bitmap == null)
			throw new NullReferenceException();
		int w = Bitmap.Width;
		int h = Bitmap.Height;
		if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
			return false;
		Rectangle rect = new(0, 0, w, h);
		BitmapData bmpData =
			Bitmap.LockBits(rect, ImageLockMode.ReadOnly,
			PixelFormat.Format32bppArgb);
		nint ptr = bmpData.Scan0;
		byte[] rgbValues = new byte[w * h * 4];
		Marshal.Copy(ptr, rgbValues, 0, rgbValues.Length);
		Bitmap.UnlockBits(bmpData);
		int i = 0;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				array[x + xstart, y + ystart] =
				rgbValues[i++] + //B
				((long)rgbValues[i++] << 8) + //G
				((long)rgbValues[i++] << 16) + //R
				((long)rgbValues[i++] << 24);  //A
			}
		}
		return true;
	}


	/// <summary>
	/// GFROMARRAY int ID, var array
	/// エラーチェックは呼び出し元でのみ行う
	/// <returns></returns>
	public bool GByteArrayToBitmap(long[,] array, int xstart, int ystart)
	{
		if (g == null || Bitmap == null)
			throw new NullReferenceException();
		int w = Bitmap.Width;
		int h = Bitmap.Height;
		if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
			return false;

		byte[] rgbValues = new byte[w * h * 4];
		int i = 0;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				long c = array[x + xstart, y + ystart];
				rgbValues[i++] = (byte)(c & 0xFF);//B
				rgbValues[i++] = (byte)(c >> 8 & 0xFF);//G
				rgbValues[i++] = (byte)(c >> 16 & 0xFF);//R
				rgbValues[i++] = (byte)(c >> 24 & 0xFF);//A
			}
		}
		Rectangle rect = new(0, 0, w, h);
		BitmapData bmpData =
			Bitmap.LockBits(rect, ImageLockMode.WriteOnly,
			PixelFormat.Format32bppArgb);
		nint ptr = bmpData.Scan0;
		Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
		Bitmap.UnlockBits(bmpData);
		return true;
	}
	#endregion

	public void Load()
	{
		if (RealBitmap != null)
			return;

		if (drawImgList == null)
			return;

		RealBitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
		g = Graphics.FromImage(RealBitmap);

		foreach (Tuple<ASprite, Rectangle> tuple in drawImgList)
			tuple.Item1.GraphicsDraw(g, tuple.Item2);

		lock (AppContents.tempLoadedGraphicsImages)
			AppContents.tempLoadedGraphicsImages.Add(this);
		return;
	}

}

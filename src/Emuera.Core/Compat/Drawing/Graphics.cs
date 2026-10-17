// andEmuera: System.Drawing.Graphics / Brush / Pen / Region の SkiaSharp 実装。
//
// Emuera がこの層を使うのは (1) GCREATE/SPRITECREATE の画像合成 (2) 文字列の幅計測 の 2 つ。
// WinForms のコンソール描画経路は Android では通らない（WebView が描画する）が、
// 上流ソースを無改変でコンパイルするためにメソッドは一通り用意してある。

using SkiaSharp;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace System.Drawing
{
	public abstract class Brush : IDisposable
	{
		internal abstract SKPaint CreatePaint();
		public abstract void Dispose();
	}

	public sealed class SolidBrush : Brush
	{
		public SolidBrush(Color color) => Color = color;

		public Color Color { get; set; }

		internal override SKPaint CreatePaint() => new()
		{
			Color = Color.ToSk(),
			Style = SKPaintStyle.Fill,
			IsAntialias = true,
		};

		// 色ごとに SKPaint を共有する案は計測して見送った (docs/porting-notes.md)。
		// フル描画 4.5ms に対して差が出ず、共有した可変オブジェクトを持ち回るぶん危ないだけだった

		public override void Dispose() { }
	}

	public sealed class Pen : IDisposable
	{
		public Pen(Color color) : this(color, 1f) { }

		public Pen(Color color, float width)
		{
			Color = color;
			Width = width;
		}

		public Pen(Brush brush) : this(brush, 1f) { }

		public Pen(Brush brush, float width)
		{
			Brush = brush;
			Width = width;
			if (brush is SolidBrush sb)
				Color = sb.Color;
		}

		public Color Color { get; set; }
		public float Width { get; set; }
		public Brush Brush { get; set; }
		public DashStyle DashStyle { get; set; }
		public DashCap DashCap { get; set; }
		public LineCap StartCap { get; set; }
		public LineCap EndCap { get; set; }
		public LineJoin LineJoin { get; set; }

		internal SKPaint CreatePaint()
		{
			var paint = new SKPaint
			{
				Color = Color.ToSk(),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = Width,
				IsAntialias = true,
			};
			if (DashStyle == DashStyle.Dash)
				paint.PathEffect = SKPathEffect.CreateDash([Width * 3, Width * 3], 0);
			else if (DashStyle == DashStyle.Dot)
				paint.PathEffect = SKPathEffect.CreateDash([Width, Width * 2], 0);
			return paint;
		}

		public void Dispose() { }
	}

	public static class Brushes
	{
		public static Brush Black => new SolidBrush(Color.Black);
		public static Brush White => new SolidBrush(Color.White);
		public static Brush Red => new SolidBrush(Color.Red);
		public static Brush Transparent => new SolidBrush(Color.Transparent);
	}

	public static class Pens
	{
		public static Pen Black => new(Color.Black);
		public static Pen White => new(Color.White);
		public static Pen Red => new(Color.Red);
	}

	public sealed class GraphicsState
	{
		internal int SaveCount;
	}

	public sealed class Graphics : IDisposable
	{
		SKCanvas canvas;
		readonly bool ownsCanvas;
		readonly Image backing;

		Graphics(SKCanvas canvas, bool ownsCanvas, Image backing)
		{
			this.canvas = canvas;
			this.ownsCanvas = ownsCanvas;
			this.backing = backing;
		}

		internal SKCanvas Canvas => canvas;

		public static Graphics FromImage(Image image)
		{
			// これから画素を書き換える。参照で作ってある SKImage はここで捨てる
			image?.InvalidateDrawable();
			return new(new SKCanvas(image.SkBitmap), true, image);
		}

		/// <summary>
		/// 描画元として渡す SKImage。<see cref="Image.Drawable"/> は画素を参照するだけなので
		/// コピーが起きないが、<b>描画先と同じビットマップ</b>のときだけは従来どおり複製する
		/// (Skia の raster は転送元と転送先が重なる場合を保証しない)。
		/// 戻り値の <c>owned</c> が true なら呼び出し側が Dispose する。
		/// </summary>
		(SKImage Image, bool Owned) SourceImage(Image image)
		{
			if (image != backing)
			{
				var cached = image.Drawable;
				if (cached != null)
					return (cached, false);
			}
			return (SKImage.FromBitmap(image.SkBitmap), true);
		}

		internal static Graphics FromCanvas(SKCanvas canvas)
			=> new(canvas, false, null);

		public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.Default;
		public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Default;
		public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.Default;
		public CompositingMode CompositingMode { get; set; } = CompositingMode.SourceOver;
		public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.Default;
		public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;

		public float DpiX => 96f;
		public float DpiY => 96f;

		public Region Clip
		{
			get => new(ClipBounds);
			set { }
		}

		public RectangleF ClipBounds
		{
			get
			{
				var r = canvas.LocalClipBounds;
				return new RectangleF(r.Left, r.Top, r.Width, r.Height);
			}
		}

		public RectangleF VisibleClipBounds => ClipBounds;

		SKSamplingOptions Sampling => InterpolationMode == InterpolationMode.NearestNeighbor
			? new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None)
			: new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

		public void Clear(Color color) => canvas.Clear(color.ToSk());

		#region 画像描画

		public void DrawImage(Image image, int x, int y) => DrawImage(image, (float)x, y);

		public void DrawImage(Image image, float x, float y)
		{
			if (image?.SkBitmap == null)
				return;
			var (skImage, owned) = SourceImage(image);
			canvas.DrawImage(skImage, x, y);
			if (owned)
				skImage.Dispose();
		}

		public void DrawImage(Image image, int x, int y, int width, int height)
			=> DrawImageCore(image, new RectangleF(x, y, width, height), new RectangleF(0, 0, image.Width, image.Height), null);

		public void DrawImage(Image image, Point point) => DrawImage(image, point.X, point.Y);

		public void DrawImage(Image image, PointF point) => DrawImage(image, point.X, point.Y);

		public void DrawImage(Image image, Rectangle destRect)
			=> DrawImage(image, destRect, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

		public void DrawImage(Image image, RectangleF destRect)
			=> DrawImageCore(image, destRect, new RectangleF(0, 0, image.Width, image.Height), null);

		public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit unit)
			=> DrawImageCore(image,
				new RectangleF(destRect.X, destRect.Y, destRect.Width, destRect.Height),
				new RectangleF(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height), null);

		public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit unit)
			=> DrawImageCore(image, destRect, srcRect, null);

		public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit unit)
			=> DrawImageCore(image,
				new RectangleF(destRect.X, destRect.Y, destRect.Width, destRect.Height),
				new RectangleF(srcX, srcY, srcWidth, srcHeight), null);

		public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit unit, ImageAttributes attr)
			=> DrawImageCore(image,
				new RectangleF(destRect.X, destRect.Y, destRect.Width, destRect.Height),
				new RectangleF(srcX, srcY, srcWidth, srcHeight), attr);

		public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit unit, ImageAttributes attr)
			=> DrawImageCore(image,
				new RectangleF(destRect.X, destRect.Y, destRect.Width, destRect.Height),
				new RectangleF(srcX, srcY, srcWidth, srcHeight), attr);

		public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit unit, ImageAttributes attr)
			=> DrawImageCore(image, destRect, srcRect, attr);

		public void DrawImageUnscaled(Image image, int x, int y) => DrawImage(image, (float)x, y);

		public void DrawImageUnscaled(Image image, Point point) => DrawImage(image, point.X, point.Y);

		void DrawImageCore(Image image, RectangleF dest, RectangleF src, ImageAttributes attr)
		{
			if (image?.SkBitmap == null)
				return;

			using var paint = new SKPaint { IsAntialias = SmoothingMode != SmoothingMode.None };
			if (attr?.ColorMatrix != null)
				paint.ColorFilter = SKColorFilter.CreateColorMatrix(attr.ColorMatrix.ToSkiaColorMatrix());
			if (CompositingMode == CompositingMode.SourceCopy)
				paint.BlendMode = SKBlendMode.Src;

			var (skImage, owned) = SourceImage(image);
			canvas.DrawImage(skImage,
				new SKRect(src.Left, src.Top, src.Right, src.Bottom),
				new SKRect(dest.Left, dest.Top, dest.Right, dest.Bottom),
				Sampling, paint);
			if (owned)
				skImage.Dispose();
		}

		#endregion

		#region 図形描画

		public void FillRectangle(Brush brush, Rectangle rect)
			=> FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);

		public void FillRectangle(Brush brush, RectangleF rect)
			=> FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);

		public void FillRectangle(Brush brush, int x, int y, int width, int height)
			=> FillRectangle(brush, (float)x, y, width, height);

		public void FillRectangle(Brush brush, float x, float y, float width, float height)
		{
			using var paint = brush.CreatePaint();
			if (CompositingMode == CompositingMode.SourceCopy)
				paint.BlendMode = SKBlendMode.Src;
			canvas.DrawRect(x, y, width, height, paint);
		}

		public void DrawRectangle(Pen pen, Rectangle rect)
			=> DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

		public void DrawRectangle(Pen pen, int x, int y, int width, int height)
			=> DrawRectangle(pen, (float)x, y, width, height);

		public void DrawRectangle(Pen pen, float x, float y, float width, float height)
		{
			using var paint = pen.CreatePaint();
			canvas.DrawRect(x, y, width, height, paint);
		}

		public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
			=> DrawLine(pen, (float)x1, y1, x2, y2);

		public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
		{
			using var paint = pen.CreatePaint();
			canvas.DrawLine(x1, y1, x2, y2, paint);
		}

		public void DrawLine(Pen pen, Point p1, Point p2) => DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);

		public void DrawLine(Pen pen, PointF p1, PointF p2) => DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);

		public void FillEllipse(Brush brush, RectangleF rect)
		{
			using var paint = brush.CreatePaint();
			canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), paint);
		}

		public void FillEllipse(Brush brush, int x, int y, int width, int height)
			=> FillEllipse(brush, new RectangleF(x, y, width, height));

		public void DrawEllipse(Pen pen, RectangleF rect)
		{
			using var paint = pen.CreatePaint();
			canvas.DrawOval(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), paint);
		}

		public void DrawEllipse(Pen pen, int x, int y, int width, int height)
			=> DrawEllipse(pen, new RectangleF(x, y, width, height));

		public void FillRegion(Brush brush, Region region)
		{
			using var paint = brush.CreatePaint();
			canvas.DrawPath(region.Path, paint);
		}

		public void FillPath(Brush brush, GraphicsPath path)
		{
			using var paint = brush.CreatePaint();
			canvas.DrawPath(path.Path, paint);
		}

		public void DrawPath(Pen pen, GraphicsPath path)
		{
			using var paint = pen.CreatePaint();
			canvas.DrawPath(path.Path, paint);
		}

		public void FillPolygon(Brush brush, PointF[] points)
		{
			using var path = new GraphicsPath();
			path.AddPolygon(points);
			FillPath(brush, path);
		}

		public void DrawPolygon(Pen pen, PointF[] points)
		{
			using var path = new GraphicsPath();
			path.AddPolygon(points);
			DrawPath(pen, path);
		}

		public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
		{
			using var paint = pen.CreatePaint();
			canvas.DrawArc(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), startAngle, sweepAngle, false, paint);
		}

		public void FillPie(Brush brush, RectangleF rect, float startAngle, float sweepAngle)
		{
			using var paint = brush.CreatePaint();
			canvas.DrawArc(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), startAngle, sweepAngle, true, paint);
		}

		#endregion

		#region 文字列

		public void DrawString(string s, Font font, Brush brush, float x, float y)
			=> DrawString(s, font, brush, new PointF(x, y), null);

		public void DrawString(string s, Font font, Brush brush, PointF point)
			=> DrawString(s, font, brush, point, null);

		public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
		{
			if (string.IsNullOrEmpty(s))
				return;
			using var paint = brush.CreatePaint();
			// GDI+ は指定座標を文字の左上として扱うので、ベースラインへ補正する
			var baseline = point.Y - font.SkFont.Metrics.Ascent;
			// 主フォントで全部描けるなら従来どおり 1 発で。
			// 欠けている字があるときだけ、代替フォントを混ぜて描く経路へ回す
			if (MinorShift.Emuera.GameView.PuchiGraphics.FontEdge)
			{
				using var edge = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 3.5f, StrokeJoin = SKStrokeJoin.Round, IsAntialias = true };
				GlyphFallback.Draw(canvas, s, font, point.X, baseline, edge);
			}
			if (GlyphFallback.AllCovered(font, s))
				canvas.DrawText(s, point.X, baseline, font.SkFont, paint);
			else
				GlyphFallback.Draw(canvas, s, font, point.X, baseline, paint);
		}

		public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle)
			=> DrawString(s, font, brush, new PointF(layoutRectangle.X, layoutRectangle.Y), null);

		public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
			=> DrawString(s, font, brush, new PointF(layoutRectangle.X, layoutRectangle.Y), format);

		public SizeF MeasureString(string text, Font font)
			=> MeasureString(text, font, int.MaxValue);

		public SizeF MeasureString(string text, Font font, int width)
		{
			if (string.IsNullOrEmpty(text))
				return new SizeF(0, font.Height);
			var w = GlyphFallback.Measure(font, text);
			return new SizeF(w, font.Height);
		}

		public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat format)
			=> MeasureString(text, font, (int)layoutArea.Width);

		public SizeF MeasureString(string text, Font font, int width, StringFormat format)
			=> MeasureString(text, font, width);

		public SizeF MeasureString(string text, Font font, PointF origin, StringFormat format)
			=> MeasureString(text, font, int.MaxValue);

		public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
		{
			var ranges = stringFormat?.MeasurableCharacterRanges ?? [];
			var result = new Region[ranges.Length];
			for (int i = 0; i < ranges.Length; i++)
			{
				var r = ranges[i];
				int first = Math.Clamp(r.First, 0, text.Length);
				int length = Math.Clamp(r.Length, 0, text.Length - first);
				float before = first == 0 ? 0 : GlyphFallback.Measure(font, text.AsSpan(0, first));
				float w = length == 0 ? 0 : GlyphFallback.Measure(font, text.AsSpan(first, length));
				result[i] = new Region(new RectangleF(layoutRect.X + before, layoutRect.Y, w, font.Height));
			}
			return result;
		}

		#endregion

		#region クリップ・変換

		public void SetClip(Rectangle rect)
			=> canvas.ClipRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), SKClipOperation.Intersect);

		public void SetClip(RectangleF rect)
			=> canvas.ClipRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), SKClipOperation.Intersect);

		public void SetClip(Region region) => SetClip(region.GetBounds(this));

		public void SetClip(Rectangle rect, CombineMode mode) => ClipCore(RectPath(rect), mode);

		public void SetClip(RectangleF rect, CombineMode mode) => ClipCore(RectPath(rect), mode);

		public void SetClip(Region region, CombineMode mode) => ClipCore(region.Path, mode);

		public void SetClip(GraphicsPath path, CombineMode mode) => ClipCore(path.Path, mode);

		void ClipCore(SKPath path, CombineMode mode)
		{
			// Skia のクリップは差分適用のみ。Replace は「一旦解除してから設定」で近似する。
			if (mode == CombineMode.Replace)
				ClearClip();
			var op = mode == CombineMode.Exclude || mode == CombineMode.Complement
				? SKClipOperation.Difference
				: SKClipOperation.Intersect;
			canvas.ClipPath(path, op, antialias: SmoothingMode == SmoothingMode.AntiAlias);
		}

		static SKPath RectPath(RectangleF rect)
		{
			var p = new SKPath();
			p.AddRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));
			return p;
		}

		static SKPath RectPath(Rectangle rect)
			=> RectPath(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));

		public void IntersectClip(Rectangle rect) => SetClip(rect);

		public void IntersectClip(RectangleF rect) => SetClip(rect);

		/// <summary>
		/// クリップを解除する。
		///
		/// 空実装にしてはいけない。上流の <c>ConsoleDivPart.DrawTo</c> は
		/// 「div の矩形にクリップして中身を描き、最後に ResetClip で戻す」という手順で、
		/// 戻し忘れると<b>その後に描かれるものすべてが直前の div の矩形に閉じ込められる</b>。
		/// EmueraConsole.OnPaint は奥行きの深いパーツ → 通常の行テキスト → 手前のパーツ
		/// の順に描くので、奥に div がある画面では**行テキストが丸ごと消える**
		/// (era のコマンド一覧が出ない、という形で出た)。
		/// </summary>
		public void ResetClip() => ClearClip();

		/// <summary>
		/// クリップを掛ける前の状態へ戻す。
		/// Skia のクリップは差分適用しかできないので、保存レベルを 1 段戻して掛け直す。
		/// 呼んだあとは必ず「クリップ用の 1 段」が積まれた状態になる (何度呼んでも増えない)。
		/// </summary>
		void ClearClip()
		{
			canvas.Restore();
			canvas.Save();
		}

		public void TranslateTransform(float dx, float dy) => canvas.Translate(dx, dy);

		public void TranslateTransform(float dx, float dy, MatrixOrder order) => canvas.Translate(dx, dy);

		public void ScaleTransform(float sx, float sy) => canvas.Scale(sx, sy);

		public void ScaleTransform(float sx, float sy, MatrixOrder order) => canvas.Scale(sx, sy);

		public void RotateTransform(float angle) => canvas.RotateDegrees(angle);

		public void RotateTransform(float angle, MatrixOrder order) => canvas.RotateDegrees(angle);

		public void ResetTransform() => canvas.ResetMatrix();

		public GraphicsState Save() => new() { SaveCount = canvas.Save() };

		public void Restore(GraphicsState state) => canvas.RestoreToCount(state.SaveCount);

		#endregion

		public void Flush() => canvas.Flush();

		public void Flush(FlushIntention intention) => canvas.Flush();

		public IntPtr GetHdc() => IntPtr.Zero;

		public void ReleaseHdc() { }

		public void ReleaseHdc(IntPtr hdc) { }

		public void Dispose()
		{
			// 描き込んだ結果を次に描画元として使うときのために、参照の SKImage を捨てておく
			backing?.InvalidateDrawable();
			if (ownsCanvas)
				canvas?.Dispose();
			canvas = null;
		}
	}

	public enum FlushIntention
	{
		Flush = 0,
		Sync = 1,
	}

	internal static class SkColorExtensions
	{
		internal static SKColor ToSk(this Color c) => new(c.R, c.G, c.B, c.A);
	}
}

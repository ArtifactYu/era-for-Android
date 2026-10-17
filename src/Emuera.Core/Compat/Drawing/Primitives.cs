// andEmuera: Android 移植用の System.Drawing 互換シム（列挙型・補助型）。
//
// System.Drawing.Primitives (Color / Point / Size / Rectangle) は Android でもそのまま
// 使えるため再定義しない。ここで補うのは System.Drawing.Common 側にしかない型だけ。

using System.Collections.Generic;

namespace System.Drawing
{
	public enum GraphicsUnit
	{
		World = 0,
		Display = 1,
		Pixel = 2,
		Point = 3,
		Inch = 4,
		Document = 5,
		Millimeter = 6,
	}

	[Flags]
	public enum FontStyle
	{
		Regular = 0,
		Bold = 1,
		Italic = 2,
		Underline = 4,
		Strikeout = 8,
	}

	[Flags]
	public enum StringFormatFlags
	{
		DirectionRightToLeft = 0x0001,
		DirectionVertical = 0x0002,
		FitBlackBox = 0x0004,
		DisplayFormatControl = 0x0020,
		NoFontFallback = 0x0400,
		MeasureTrailingSpaces = 0x0800,
		NoWrap = 0x1000,
		LineLimit = 0x2000,
		NoClip = 0x4000,
	}

	public enum StringAlignment
	{
		Near = 0,
		Center = 1,
		Far = 2,
	}

	public enum StringTrimming
	{
		None = 0,
		Character = 1,
		Word = 2,
		EllipsisCharacter = 3,
		EllipsisWord = 4,
		EllipsisPath = 5,
	}

	public struct CharacterRange
	{
		public CharacterRange(int first, int length)
		{
			First = first;
			Length = length;
		}

		public int First { get; set; }
		public int Length { get; set; }
	}

	public sealed class StringFormat : IDisposable
	{
		public StringFormat() { }

		public StringFormat(StringFormatFlags options)
		{
			FormatFlags = options;
		}

		public StringFormat(StringFormat format)
		{
			if (format == null)
				return;
			FormatFlags = format.FormatFlags;
			Alignment = format.Alignment;
			LineAlignment = format.LineAlignment;
			Trimming = format.Trimming;
			MeasurableCharacterRanges = format.MeasurableCharacterRanges;
		}

		public StringFormatFlags FormatFlags { get; set; }
		public StringAlignment Alignment { get; set; }
		public StringAlignment LineAlignment { get; set; }
		public StringTrimming Trimming { get; set; }

		/// <summary>字送りに余白を入れない書式 (GDI+ の GenericTypographic 相当)。</summary>
		public static StringFormat GenericTypographic => new(StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip);

		public static StringFormat GenericDefault => new();

		internal CharacterRange[] MeasurableCharacterRanges { get; private set; } = [];

		public void SetMeasurableCharacterRanges(CharacterRange[] ranges) => MeasurableCharacterRanges = ranges;

		public void Dispose() { }
	}
}

namespace System.Drawing.Imaging
{
	public enum PixelFormat
	{
		Undefined = 0,
		Format24bppRgb = 137224,
		Format32bppRgb = 139273,
		Format32bppArgb = 2498570,
		Format32bppPArgb = 925707,
	}

	public enum ColorMatrixFlag
	{
		Default = 0,
		SkipGrays = 1,
		AltGrays = 2,
	}

	public enum ColorAdjustType
	{
		Default = 0,
		Bitmap = 1,
		Brush = 2,
		Pen = 3,
		Text = 4,
		Any = 5,
	}

	/// <summary>
	/// GDI+ の 5x5 カラーマトリクス。SkiaSharp の SKColorFilter へ変換して使う。
	/// </summary>
	public sealed class ColorMatrix
	{
		readonly float[][] matrix;

		public ColorMatrix()
		{
			matrix =
			[
				[1f, 0f, 0f, 0f, 0f],
				[0f, 1f, 0f, 0f, 0f],
				[0f, 0f, 1f, 0f, 0f],
				[0f, 0f, 0f, 1f, 0f],
				[0f, 0f, 0f, 0f, 1f],
			];
		}

		public ColorMatrix(float[][] newColorMatrix)
		{
			matrix = newColorMatrix;
		}

		public float this[int row, int column]
		{
			get => matrix[row][column];
			set => matrix[row][column] = value;
		}

		public float Matrix00 { get => matrix[0][0]; set => matrix[0][0] = value; }
		public float Matrix11 { get => matrix[1][1]; set => matrix[1][1] = value; }
		public float Matrix22 { get => matrix[2][2]; set => matrix[2][2] = value; }
		public float Matrix33 { get => matrix[3][3]; set => matrix[3][3] = value; }
		public float Matrix44 { get => matrix[4][4]; set => matrix[4][4] = value; }

		/// <summary>
		/// SkiaSharp の SKColorFilter.CreateColorMatrix が要求する 4x5 (行優先) 配列に変換する。
		/// GDI+ は行ベクトル×行列、Skia は列ベクトルなので転置して渡す。
		/// </summary>
		internal float[] ToSkiaColorMatrix()
		{
			return
			[
				matrix[0][0], matrix[1][0], matrix[2][0], matrix[3][0], matrix[4][0] * 255f,
				matrix[0][1], matrix[1][1], matrix[2][1], matrix[3][1], matrix[4][1] * 255f,
				matrix[0][2], matrix[1][2], matrix[2][2], matrix[3][2], matrix[4][2] * 255f,
				matrix[0][3], matrix[1][3], matrix[2][3], matrix[3][3], matrix[4][3] * 255f,
			];
		}
	}

	public sealed class ImageAttributes : IDisposable
	{
		internal ColorMatrix ColorMatrix { get; private set; }

		public void SetColorMatrix(ColorMatrix newColorMatrix) => ColorMatrix = newColorMatrix;

		public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag flags) => ColorMatrix = newColorMatrix;

		public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag mode, ColorAdjustType type) => ColorMatrix = newColorMatrix;

		public void ClearColorMatrix() => ColorMatrix = null;

		public void Dispose() { }
	}
}

namespace System.Drawing.Drawing2D
{
	public enum SmoothingMode
	{
		Invalid = -1,
		Default = 0,
		HighSpeed = 1,
		HighQuality = 2,
		None = 3,
		AntiAlias = 4,
	}

	public enum InterpolationMode
	{
		Invalid = -1,
		Default = 0,
		Low = 1,
		High = 2,
		Bilinear = 3,
		Bicubic = 4,
		NearestNeighbor = 5,
		HighQualityBilinear = 6,
		HighQualityBicubic = 7,
	}

	public enum PixelOffsetMode
	{
		Invalid = -1,
		Default = 0,
		HighSpeed = 1,
		HighQuality = 2,
		None = 3,
		Half = 4,
	}

	public enum CompositingMode
	{
		SourceOver = 0,
		SourceCopy = 1,
	}

	public enum CompositingQuality
	{
		Invalid = -1,
		Default = 0,
		HighSpeed = 1,
		HighQuality = 2,
		GammaCorrected = 3,
		AssumeLinear = 4,
	}

	public enum FillMode
	{
		Alternate = 0,
		Winding = 1,
	}

	public enum LineCap
	{
		Flat = 0,
		Square = 1,
		Round = 2,
		Triangle = 3,
	}

	public enum LineJoin
	{
		Miter = 0,
		Bevel = 1,
		Round = 2,
		MiterClipped = 3,
	}

	public enum DashStyle
	{
		Solid = 0,
		Dash = 1,
		Dot = 2,
		DashDot = 3,
		DashDotDot = 4,
		Custom = 5,
	}

	/// <summary>
	/// GDI+ の GraphicsPath 相当。SkiaSharp の SKPath を保持する。
	/// </summary>
	public sealed class GraphicsPath : IDisposable
	{
		internal SkiaSharp.SKPath Path { get; } = new SkiaSharp.SKPath();

		public GraphicsPath() { }

		public GraphicsPath(FillMode fillMode)
		{
			Path.FillType = fillMode == FillMode.Winding
				? SkiaSharp.SKPathFillType.Winding
				: SkiaSharp.SKPathFillType.EvenOdd;
		}

		public void AddRectangle(RectangleF rect) => Path.AddRect(new SkiaSharp.SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));

		public void AddRectangle(Rectangle rect) => Path.AddRect(new SkiaSharp.SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));

		public void AddEllipse(RectangleF rect) => Path.AddOval(new SkiaSharp.SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));

		public void AddLine(float x1, float y1, float x2, float y2)
		{
			if (Path.PointCount == 0)
				Path.MoveTo(x1, y1);
			else
				Path.LineTo(x1, y1);
			Path.LineTo(x2, y2);
		}

		public void AddPolygon(PointF[] points)
		{
			if (points == null || points.Length == 0)
				return;
			var pts = new SkiaSharp.SKPoint[points.Length];
			for (int i = 0; i < points.Length; i++)
				pts[i] = new SkiaSharp.SKPoint(points[i].X, points[i].Y);
			Path.AddPoly(pts, true);
		}

		public void AddPolygon(Point[] points)
		{
			if (points == null || points.Length == 0)
				return;
			var pts = new PointF[points.Length];
			for (int i = 0; i < points.Length; i++)
				pts[i] = new PointF(points[i].X, points[i].Y);
			AddPolygon(pts);
		}

		public void AddPath(GraphicsPath addingPath, bool connect) => Path.AddPath(addingPath.Path);

		public void AddPath(GraphicsPath addingPath) => Path.AddPath(addingPath.Path);

		public void AddString(string s, FontFamily family, int style, float emSize, PointF origin, StringFormat format)
		{
			if (string.IsNullOrEmpty(s))
				return;
			using var font = new SkiaSharp.SKFont(family.Typeface, emSize);
			using var textPath = font.GetTextPath(s, new SkiaSharp.SKPoint(origin.X, origin.Y - font.Metrics.Ascent));
			if (textPath != null)
				Path.AddPath(textPath);
		}

		public void AddString(string s, FontFamily family, int style, float emSize, Point origin, StringFormat format)
			=> AddString(s, family, style, emSize, new PointF(origin.X, origin.Y), format);

		public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
			=> Path.AddArc(new SkiaSharp.SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), startAngle, sweepAngle);

		public void CloseFigure() => Path.Close();

		public void StartFigure() { }

		public void Reset() => Path.Reset();

		public RectangleF GetBounds()
		{
			var b = Path.Bounds;
			return new RectangleF(b.Left, b.Top, b.Width, b.Height);
		}

		public bool IsVisible(PointF point) => Path.Contains(point.X, point.Y);

		public void Dispose() => Path.Dispose();
	}

	public sealed class Matrix : IDisposable
	{
		internal SkiaSharp.SKMatrix Value = SkiaSharp.SKMatrix.Identity;

		public Matrix() { }

		public void Translate(float offsetX, float offsetY)
			=> Value = Value.PreConcat(SkiaSharp.SKMatrix.CreateTranslation(offsetX, offsetY));

		public void Scale(float scaleX, float scaleY)
			=> Value = Value.PreConcat(SkiaSharp.SKMatrix.CreateScale(scaleX, scaleY));

		public void Rotate(float angle)
			=> Value = Value.PreConcat(SkiaSharp.SKMatrix.CreateRotationDegrees(angle));

		public void Reset() => Value = SkiaSharp.SKMatrix.Identity;

		public void Dispose() { }
	}
}

namespace System.Drawing.Text
{
	public enum TextRenderingHint
	{
		SystemDefault = 0,
		SingleBitPerPixelGridFit = 1,
		SingleBitPerPixel = 2,
		AntiAliasGridFit = 3,
		AntiAlias = 4,
		ClearTypeGridFit = 5,
	}

	/// <summary>
	/// フォントファイルを直接読み込むためのコレクション (EE のフォントファイル対応で使われる)。
	/// </summary>
	public sealed class PrivateFontCollection : IDisposable
	{
		readonly List<FontFamily> families = [];

		public FontFamily[] Families => families.ToArray();

		public void AddFontFile(string filename)
		{
			var typeface = SkiaSharp.SKTypeface.FromFile(filename);
			if (typeface == null)
				return;
			families.Add(new FontFamily(typeface));
			// 名前からの解決 (new Font("BIZ UDGothic", ...)) でも引けるようにする。
			// 上流は Pfc 経由でしか参照しないが、移植側の描画は FontResolver を通る
			FontResolver.Register(typeface.FamilyName, typeface);
		}

		public void AddMemoryFont(IntPtr memory, int length) { }

		public void Dispose()
		{
			foreach (var f in families)
				f.Dispose();
			families.Clear();
		}
	}
}

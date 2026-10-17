// andEmuera: System.Drawing 互換シムの追加分。
// 領域演算 (Region) はシェイプ描画 (EM の CUSTOMDRAWLINE / 図形パーツ) で使われるため、
// SkiaSharp の SKPath による実装を用意する。

using SkiaSharp;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

namespace System.Drawing.Imaging
{
	[Flags]
	public enum ImageLockMode
	{
		ReadOnly = 1,
		WriteOnly = 2,
		ReadWrite = 3,
		UserInputBuffer = 4,
	}

	/// <summary>
	/// LockBits の戻り値。Scan0 は SKBitmap のピクセルバッファを直接指す。
	/// </summary>
	public sealed class BitmapData
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public int Stride { get; set; }
		public PixelFormat PixelFormat { get; set; }
		public IntPtr Scan0 { get; set; }
		public int Reserved { get; set; }
	}
}

namespace System.Drawing.Drawing2D
{
	public enum CombineMode
	{
		Replace = 0,
		Intersect = 1,
		Union = 2,
		Xor = 3,
		Exclude = 4,
		Complement = 5,
	}

	public enum DashCap
	{
		Flat = 0,
		Round = 2,
		Triangle = 3,
	}

	public enum MatrixOrder
	{
		Prepend = 0,
		Append = 1,
	}
}

namespace System.Drawing.Text
{
	/// <summary>
	/// 端末にインストール済みのフォント一覧。Android では Skia のフォントマネージャから引く。
	/// </summary>
	public sealed class InstalledFontCollection : IDisposable
	{
		public FontFamily[] Families
		{
			get
			{
				var list = new List<FontFamily>();
				using var manager = SKFontManager.Default;
				foreach (var name in manager.FontFamilies)
				{
					var typeface = SKTypeface.FromFamilyName(name);
					if (typeface != null)
						list.Add(new FontFamily(typeface));
				}
				return list.ToArray();
			}
		}

		public void Dispose() { }
	}
}

namespace System.Drawing
{
	/// <summary>
	/// GDI+ の Region 相当。SKPath で領域を保持し、論理演算は SKPathOp で行う。
	/// </summary>
	public sealed class Region : IDisposable
	{
		SKPath path;

		public Region()
		{
			// 既定は「無限領域」。実用上は十分大きな矩形で代用する。
			path = new SKPath();
			path.AddRect(new SKRect(-1e6f, -1e6f, 1e6f, 1e6f));
		}

		public Region(RectangleF rect)
		{
			path = new SKPath();
			path.AddRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));
		}

		public Region(Rectangle rect)
			: this(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height)) { }

		public Region(GraphicsPath graphicsPath)
		{
			path = new SKPath(graphicsPath.Path);
		}

		internal SKPath Path => path;

		public RectangleF GetBounds(Graphics g)
		{
			var b = path.Bounds;
			return new RectangleF(b.Left, b.Top, b.Width, b.Height);
		}

		public bool IsEmpty(Graphics g) => path.IsEmpty || path.Bounds.Width <= 0 || path.Bounds.Height <= 0;

		public bool IsVisible(PointF point) => path.Contains(point.X, point.Y);

		public bool IsVisible(PointF point, Graphics g) => IsVisible(point);

		public bool IsVisible(RectangleF rect) => path.Bounds.IntersectsWith(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));

		public void Union(Region region) => Combine(region.path, SKPathOp.Union);

		public void Union(RectangleF rect) => Combine(RectPath(rect), SKPathOp.Union);

		public void Union(GraphicsPath p) => Combine(p.Path, SKPathOp.Union);

		public void Intersect(Region region) => Combine(region.path, SKPathOp.Intersect);

		public void Intersect(RectangleF rect) => Combine(RectPath(rect), SKPathOp.Intersect);

		public void Intersect(GraphicsPath p) => Combine(p.Path, SKPathOp.Intersect);

		public void Exclude(Region region) => Combine(region.path, SKPathOp.Difference);

		public void Exclude(RectangleF rect) => Combine(RectPath(rect), SKPathOp.Difference);

		public void Exclude(GraphicsPath p) => Combine(p.Path, SKPathOp.Difference);

		public void Xor(Region region) => Combine(region.path, SKPathOp.Xor);

		public void Complement(Region region)
		{
			var other = new SKPath(region.path);
			other.Op(path, SKPathOp.Difference, other);
			path.Dispose();
			path = other;
		}

		public void MakeEmpty()
		{
			path.Dispose();
			path = new SKPath();
		}

		Region(SKPath source) => path = new SKPath(source);

		public Region Clone() => new(path);

		void Combine(SKPath other, SKPathOp op)
		{
			var result = new SKPath();
			if (path.Op(other, op, result))
			{
				path.Dispose();
				path = result;
			}
			else
			{
				result.Dispose();
			}
		}

		static SKPath RectPath(RectangleF rect)
		{
			var p = new SKPath();
			p.AddRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));
			return p;
		}

		public void Dispose() => path?.Dispose();
	}
}

// andEmuera: System.Drawing.Image / Bitmap の SkiaSharp 実装。

using SkiaSharp;
using System.Drawing.Imaging;
using System.IO;

namespace System.Drawing
{
	public abstract class Image : IDisposable
	{
		internal abstract SKBitmap SkBitmap { get; }

		/// <summary>
		/// <c>ANDEMUERA_NO_IMAGE_CACHE=1</c> で毎回 <see cref="SKImage.FromBitmap"/> に戻す
		/// (＝従来どおり描画のたびに全画素をコピーする)。A/B 計測と退避用。
		/// </summary>
		static readonly bool noImageCache =
			Environment.GetEnvironmentVariable("ANDEMUERA_NO_IMAGE_CACHE") == "1";

		SKImage drawable;

		/// <summary>
		/// 描画元として渡す <see cref="SKImage"/>。
		///
		/// <see cref="SKImage.FromBitmap"/> は mutable な <see cref="SKBitmap"/> を<b>丸ごと複製する</b>。
		/// 上流の OnPaint は毎回 <c>DrawImage(bakedBackground, 0, 0)</c> を通るので、
		/// 画面と同じ大きさ (1600x2691 なら約 17MB) のコピーが 1 描画ごとに走っていた。
		/// スプライトや CBG も同じ経路。
		///
		/// <c>FromPixels</c> はバッファを<b>参照するだけ</b>なので、作った SKImage を
		/// この Image と一緒に持ち回れば、書き換えが無い限り作り直しも要らない。
		/// 参照している以上、<b>画素を書き換えたら必ず <see cref="InvalidateDrawable"/> を呼ぶこと</b>。
		/// </summary>
		internal SKImage Drawable
		{
			get
			{
				var bmp = SkBitmap;
				if (bmp == null)
					return null;
				if (noImageCache)
					return null;   // 呼び出し側が FromBitmap へ落とす
				if (drawable != null)
					return drawable;

				// PeekPixels はバッファを指すだけ。SKImage は情報を写し取るので pixmap は捨ててよい
				using var pixmap = bmp.PeekPixels();
				drawable = pixmap != null ? SKImage.FromPixels(pixmap) : SKImage.FromBitmap(bmp);
				return drawable;
			}
		}

		/// <summary>画素を書き換えたので、参照で作った <see cref="Drawable"/> を捨てる。</summary>
		internal void InvalidateDrawable()
		{
			drawable?.Dispose();
			drawable = null;
		}

		public int Width => SkBitmap?.Width ?? 0;
		public int Height => SkBitmap?.Height ?? 0;
		public Size Size => new(Width, Height);

		public float HorizontalResolution => 96f;
		public float VerticalResolution => 96f;

		public PixelFormat PixelFormat => PixelFormat.Format32bppArgb;

		public static Image FromFile(string filename) => Bitmap.FromFileCore(filename);

		public static Image FromStream(Stream stream) => new Bitmap(stream);

		public void Save(string filename) => Save(filename, ImageFormat.Png);

		public void Save(string filename, ImageFormat format)
		{
			using var fs = File.Create(filename);
			Save(fs, format);
		}

		public void Save(Stream stream, ImageFormat format)
		{
			using var image = SKImage.FromBitmap(SkBitmap);
			using var data = image.Encode(format.EncodedFormat, 100);
			data.SaveTo(stream);
		}

		/// <summary>
		/// PNG を可逆のままバイト列にする。画面転送用の経路。
		///
		/// <see cref="Save(Stream, ImageFormat)"/> との違いは 3 点で、いずれも出力画素は変わらない:
		/// <list type="bullet">
		/// <item>SKImage を作らない。mutable な SKBitmap から SKImage を作ると
		///   ピクセルが丸ごとコピーされる (1600x3456 なら 22MB)</item>
		/// <item>フィルタと zlib レベルを指定できる。既定は「毎行 5 種のフィルタを試す +
		///   zlib 6」という一番遅い設定になっている</item>
		/// <item>MemoryStream を経由しないのでコピーが 1 回減る</item>
		/// </list>
		/// </summary>
		public byte[] EncodePng(SKPngEncoderFilterFlags filter, int zlibLevel)
		{
			var bmp = SkBitmap;
			if (bmp == null)
				return null;

			// PeekPixels はピクセルバッファを指すだけでコピーしない
			using var pixmap = bmp.PeekPixels();
			if (pixmap == null)
			{
				// 取れない実装のための保険。従来どおりの経路に落とす
				using var image = SKImage.FromBitmap(bmp);
				using var fallback = image.Encode(SKEncodedImageFormat.Png, 100);
				return fallback?.ToArray();
			}

			using var data = pixmap.Encode(new SKPngEncoderOptions(filter, zlibLevel));
			return data?.ToArray();
		}

		public abstract void Dispose();
	}

	public sealed class ImageFormat
	{
		ImageFormat(SKEncodedImageFormat format) => EncodedFormat = format;

		internal SKEncodedImageFormat EncodedFormat { get; }

		public static ImageFormat Png { get; } = new(SKEncodedImageFormat.Png);
		public static ImageFormat Jpeg { get; } = new(SKEncodedImageFormat.Jpeg);
		public static ImageFormat Bmp { get; } = new(SKEncodedImageFormat.Bmp);
		public static ImageFormat Webp { get; } = new(SKEncodedImageFormat.Webp);
		public static ImageFormat Gif { get; } = new(SKEncodedImageFormat.Gif);
	}

	public sealed class Bitmap : Image
	{
		SKBitmap bitmap;

		internal override SKBitmap SkBitmap => bitmap;

		internal Bitmap(SKBitmap source) => bitmap = source;

		public Bitmap(int width, int height)
			: this(width, height, PixelFormat.Format32bppArgb) { }

		public Bitmap(int width, int height, PixelFormat format)
		{
			// Emuera は常に 32bit ARGB を前提にしているので format は無視して構わない
			bitmap = new SKBitmap(Math.Max(width, 1), Math.Max(height, 1), SKColorType.Bgra8888, SKAlphaType.Premul);
			bitmap.Erase(SKColors.Transparent);
		}

		public Bitmap(Stream stream)
		{
			bitmap = SKBitmap.Decode(stream) ?? new SKBitmap(1, 1);
		}

		public Bitmap(string filename)
		{
			bitmap = SKBitmap.Decode(MinorShift.Emuera.Compat.CasePath.Resolve(filename)) ?? new SKBitmap(1, 1);
		}

		public Bitmap(Image original)
			: this(original, original.Width, original.Height) { }

		public Bitmap(Image original, int width, int height)
		{
			bitmap = new SKBitmap(Math.Max(width, 1), Math.Max(height, 1), SKColorType.Bgra8888, SKAlphaType.Premul);
			using var canvas = new SKCanvas(bitmap);
			canvas.Clear(SKColors.Transparent);
			using var src = SKImage.FromBitmap(original.SkBitmap);
			canvas.DrawImage(src, new SKRect(0, 0, width, height));
		}

		public Bitmap(Image original, Size newSize)
			: this(original, newSize.Width, newSize.Height) { }

		internal static Bitmap FromFileCore(string filename)
		{
			var decoded = SKBitmap.Decode(MinorShift.Emuera.Compat.CasePath.Resolve(filename));
			return decoded == null ? null : new Bitmap(decoded);
		}

		public Color GetPixel(int x, int y)
		{
			var c = bitmap.GetPixel(x, y);
			return Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
		}

		public void SetPixel(int x, int y, Color color)
		{
			InvalidateDrawable();
			bitmap.SetPixel(x, y, new SKColor(color.R, color.G, color.B, color.A));
		}

		public void MakeTransparent(Color transparentColor)
		{
			InvalidateDrawable();
			var target = new SKColor(transparentColor.R, transparentColor.G, transparentColor.B);
			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					var c = bitmap.GetPixel(x, y);
					if (c.Red == target.Red && c.Green == target.Green && c.Blue == target.Blue)
						bitmap.SetPixel(x, y, SKColors.Transparent);
				}
			}
		}

		/// <summary>
		/// GDI+ の LockBits 相当。SKBitmap のピクセルバッファをそのまま指すため、
		/// 呼び出し側は Scan0 / Stride で直接読み書きできる。
		/// </summary>
		public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format)
		{
			// 書き込み用に渡した先で何が起きるか分からないので、取られた時点で捨てる
			InvalidateDrawable();
			return new BitmapData
			{
				Width = rect.Width,
				Height = rect.Height,
				Stride = bitmap.RowBytes,
				PixelFormat = format,
				// 部分矩形でも先頭からのオフセットで指せるようにする (4 bytes/pixel 前提)
				Scan0 = bitmap.GetPixels() + rect.Top * bitmap.RowBytes + rect.Left * 4,
			};
		}

		public void UnlockBits(BitmapData bitmapdata)
		{
			InvalidateDrawable();
			bitmap.NotifyPixelsChanged();
		}

		public IntPtr GetHicon() => IntPtr.Zero;

		public Bitmap Clone() => new(bitmap.Copy());

		public Bitmap Clone(Rectangle rect, PixelFormat format)
		{
			var dst = new SKBitmap(Math.Max(rect.Width, 1), Math.Max(rect.Height, 1), SKColorType.Bgra8888, SKAlphaType.Premul);
			using var canvas = new SKCanvas(dst);
			canvas.Clear(SKColors.Transparent);
			using var src = SKImage.FromBitmap(bitmap);
			canvas.DrawImage(src, new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), new SKRect(0, 0, rect.Width, rect.Height));
			return new Bitmap(dst);
		}

		public override void Dispose()
		{
			// SKImage はこのビットマップの画素を参照しているので、先に手放す
			InvalidateDrawable();
			bitmap?.Dispose();
			bitmap = null;
		}
	}

	/// <summary>
	/// アイコンは Android では使わないためスタブ。
	/// </summary>
	public sealed class Icon : IDisposable
	{
		public Icon(string fileName) { }
		public Icon(Stream stream) { }
		public static Icon ExtractAssociatedIcon(string filePath) => null;
		public static Icon FromHandle(IntPtr handle) => null;
		public Bitmap ToBitmap() => new(1, 1);
		public void Dispose() { }
	}
}

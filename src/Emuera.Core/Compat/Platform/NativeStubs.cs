// andEmuera: Win32 依存の置き換え (キー状態取得・WebP デコード)。

using SkiaSharp;
using System;
using System.Drawing;
using System.IO;

namespace MinorShift.Emuera.Runtime.Utils
{
	/// <summary>
	/// 上流は user32.dll の GetKeyState でシフト/コントロールの押下状態を見ている。
	/// Android には対応する概念が無いため、UI 層から <see cref="ModifierState"/> を更新して代用する。
	/// </summary>
	internal sealed class WinInput
	{
		/// <summary>仮想キーコード → 押下中かどうか。Android の UI 層が更新する。</summary>
		public static Func<int, bool> ModifierState { get; set; }

		public static short GetKeyState(int nVirtKey)
			=> (ModifierState?.Invoke(nVirtKey) ?? false) ? unchecked((short)0x8000) : (short)0;
	}

	/// <summary>
	/// 上流の libwebp P/Invoke ラッパーの置き換え。SkiaSharp が WebP を直接扱えるので薄い。
	/// </summary>
	internal sealed class WebP : IDisposable
	{
		public Bitmap Load(string pathFileName)
		{
			using var stream = File.OpenRead(pathFileName);
			var decoded = SKBitmap.Decode(stream);
			return decoded == null ? null : new Bitmap(decoded);
		}

		public Bitmap Decode(byte[] rawWebP)
		{
			var decoded = SKBitmap.Decode(rawWebP);
			return decoded == null ? null : new Bitmap(decoded);
		}

		public Bitmap Decode(byte[] rawWebP, object options) => Decode(rawWebP);

		public Bitmap GetThumbnailQuality(byte[] rawWebP, int width, int height)
		{
			using var src = Decode(rawWebP);
			return src == null ? null : new Bitmap(src, width, height);
		}

		public Bitmap GetThumbnailFast(byte[] rawWebP, int width, int height)
			=> GetThumbnailQuality(rawWebP, width, height);

		public byte[] EncodeLossy(Bitmap bmp, int quality = 75)
		{
			using var image = SKImage.FromBitmap(bmp.SkBitmap);
			using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
			return data.ToArray();
		}

		public byte[] EncodeLossless(Bitmap bmp)
		{
			using var image = SKImage.FromBitmap(bmp.SkBitmap);
			using var data = image.Encode(SKEncodedImageFormat.Webp, 100);
			return data.ToArray();
		}

		public void Save(Bitmap bmp, string pathFileName, int quality = 75)
			=> File.WriteAllBytes(pathFileName, EncodeLossy(bmp, quality));

		public void GetInfo(byte[] rawWebP, out int width, out int height, out bool has_alpha, out bool has_animation, out string format)
		{
			using var codec = SKCodec.Create(new MemoryStream(rawWebP));
			width = codec?.Info.Width ?? 0;
			height = codec?.Info.Height ?? 0;
			has_alpha = codec?.Info.AlphaType != SKAlphaType.Opaque;
			has_animation = (codec?.FrameCount ?? 0) > 1;
			format = "WebP";
		}

		public string GetVersion() => "SkiaSharp";

		public void Dispose() { }
	}
}

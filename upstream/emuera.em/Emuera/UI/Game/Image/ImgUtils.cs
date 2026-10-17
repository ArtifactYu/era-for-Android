using MinorShift.Emuera.Runtime.Utils;
using System.Drawing;
using System.IO;

namespace MinorShift.Emuera.UI.Game.Image;

static class ImgUtils
{
	public static Bitmap LoadImage(string filepath)
	{
		if (!File.Exists(filepath))
		{
			return null;
		}
		Bitmap bmp = null;

		if (Path.GetExtension(filepath).ToUpperInvariant() == ".WEBP")
		{
			using (WebP webp = new())
				bmp = webp.Load(filepath);

			if (bmp == null)
			{
				return null;
			}
		}
		else
		{
			bmp = new Bitmap(filepath);
			if (bmp == null)
			{
				return null;
			}
		}

		return bmp;
	}
}

using System.Drawing;
using System.Drawing.Drawing2D;

namespace MinorShift.Emuera.Runtime.Utils.EvilMask;

internal sealed class Shape
{
	internal struct Direction { public const int Top = 0, Right = 1, Bottom = 2, Left = 3; };
	internal struct Corner { public const int LeftTop = 0, RightTop = 1, RightBottom = 2, LeftBottom = 3; };

	internal sealed class BoxBorder
	{
		public static void DrawBorder(Graphics graph, Rectangle rect, int[] border, int[] radius, Color[] colors, Color bgColor)
		{
			if (radius == null || radius[Corner.LeftTop] == 0)
				drawNormalCornerBorderLeftTop(graph, rect, border, bgColor, colors, false, false);
			else
				drawRoundedCornerBorderLeftTop(graph, rect, border, radius, bgColor, colors, false, false);

			if (radius == null || radius[Corner.RightTop] == 0)
				drawNormalCornerBorderLeftTop(graph, rect, border, bgColor, colors, true, false);
			else
				drawRoundedCornerBorderLeftTop(graph, rect, border, radius, bgColor, colors, true, false);

			if (radius == null || radius[Corner.LeftBottom] == 0)
				drawNormalCornerBorderLeftTop(graph, rect, border, bgColor, colors, false, true);
			else
				drawRoundedCornerBorderLeftTop(graph, rect, border, radius, bgColor, colors, false, true);

			if (radius == null || radius[Corner.RightBottom] == 0)
				drawNormalCornerBorderLeftTop(graph, rect, border, bgColor, colors, true, true);
			else
				drawRoundedCornerBorderLeftTop(graph, rect, border, radius, bgColor, colors, true, true);
		}
		static void drawNormalCornerBorderLeftTop(Graphics graph, Rectangle rect, int[] border, Color bgColor, Color[] colors, bool flipX, bool flipY)
		{
			var backUp = graph.SmoothingMode;
			graph.SmoothingMode = SmoothingMode.AntiAlias;
			if (bgColor != Color.Transparent)
			{
				// 1/4の背景を描画
				var bWidth = rect.Width - (border != null ? border[Direction.Left] - border[Direction.Right] : 0);
				var bHalfWidth = bWidth / 2;
				var bHeigh = rect.Height - (border != null ? border[Direction.Top] - border[Direction.Bottom] : 0);
				var bHalfHeigh = bHeigh / 2;
				if ((flipX ? bWidth - bHalfWidth : bHalfWidth) > 0 && (flipY ? bHeigh - bHalfHeigh : bHalfHeigh) > 0)
					using (var brush = new SolidBrush(bgColor))
					{
						graph.FillRectangle(brush, new Rectangle(
							rect.X + (border != null ? border[Direction.Left] : 0) + (flipX ? bHalfWidth : 0),
							rect.Y + (border != null ? border[Direction.Top] : 0) + (flipY ? bHalfHeigh : 0),
							flipX ? bWidth - bHalfWidth : bHalfWidth + 1,
							flipY ? bHeigh - bHalfHeigh : bHalfHeigh + 1));
					}
			}
			if (border == null) return;
			if (border[flipX ? Direction.Right : Direction.Left] > 0)
			{
				// 左枠の半分
				Color color = colors == null ? Config.Config.ForeColor : colors[flipX ? Direction.Right : Direction.Left];
				if (color != Color.Transparent)
					using (var path = new GraphicsPath())
					{
						path.AddPolygon(new Point[] {
								new(flipX ? rect.X+rect.Width : rect.X,
									flipY ? rect.Y+rect.Height : rect.Y),

								new(flipX ? rect.X+rect.Width-border[Direction.Right] : rect.X+border[Direction.Left],
									flipY ? rect.Y+rect.Height-border[Direction.Bottom] : rect.Y+border[Direction.Top]),

								new(flipX ? rect.X+rect.Width-border[Direction.Right] : rect.X+border[Direction.Left],
									rect.Y+rect.Height/2),

								new(flipX ? rect.X+rect.Width : rect.X,
									rect.Y+rect.Height/2)
							});
						using (var brush = new SolidBrush(color))
						{
							graph.FillPath(brush, path);
						}
					}
			}
			if (border[flipY ? Direction.Bottom : Direction.Top] > 0)
			{
				// 上枠の半分
				Color color = colors == null ? Config.Config.ForeColor : colors[flipY ? Direction.Bottom : Direction.Top];
				if (color != Color.Transparent)
					using (var path = new GraphicsPath())
					{
						path.AddPolygon(new Point[] {
								new(flipX ? rect.X+rect.Width : rect.X,
									flipY ? rect.Y+rect.Height : rect.Y),

								new(flipX ? rect.X+rect.Width-border[Direction.Right] : rect.X+border[Direction.Left],
									flipY ? rect.Y+rect.Height-border[Direction.Bottom] : rect.Y+border[Direction.Top]),

								new(rect.X+rect.Width/2,
									flipY ? rect.Y+rect.Height-border[Direction.Bottom] : rect.Y+border[Direction.Top]),

								new(rect.X+rect.Width/2,
									flipY ? rect.Y+rect.Height : rect.Y)
							});
						using (var brush = new SolidBrush(color))
						{
							graph.FillPath(brush, path);
						}
					}
			}
			graph.SmoothingMode = backUp;
		}
		static void drawRoundedCornerBorderLeftTop(Graphics graph, Rectangle rect, int[] border, int[] radius, Color bgColor, Color[] colors, bool flipX, bool flipY)
		{
			var backUp = graph.SmoothingMode;
			graph.SmoothingMode = SmoothingMode.AntiAlias;
			GraphicsPath innerEllipse = null;
			int corner = flipX ? flipY ? Corner.RightBottom : Corner.RightTop : flipY ? Corner.LeftBottom : Corner.LeftTop;
			Rectangle cornerRect = new(
				flipX ? rect.X + rect.Width - radius[corner] : rect.X,
				flipY ? rect.Y + rect.Height - radius[corner] : rect.Y,
				radius[corner],
				radius[corner]);
			using (var cornerEllipse = new GraphicsPath())
			{
				cornerEllipse.AddEllipse(new Rectangle(
					flipX ? cornerRect.X - cornerRect.Width : cornerRect.X,
					flipY ? cornerRect.Y - cornerRect.Height : cornerRect.Y,
					cornerRect.Width * 2 - 1,
					cornerRect.Height * 2 - 1));
				bool hasInnerEllipse = (border != null ? border[flipX ? Direction.Left : Direction.Right] : 0) < radius[corner]
					&& (border != null ? border[flipY ? Direction.Bottom : Direction.Top] : 0) < radius[corner];
				Rectangle innerEllipseRect = radius != null ? new Rectangle(
					flipX ? rect.X + rect.Width - radius[corner] : rect.X + (border != null ? border[Direction.Left] : 0),
					flipY ? rect.Y + rect.Height - radius[corner] : rect.Y + (border != null ? border[Direction.Top] : 0),
					radius[corner] - (border != null ? border[flipX ? Direction.Left : Direction.Right] : 0),
					radius[corner] - (border != null ? border[flipY ? Direction.Bottom : Direction.Top] : 0)) : new Rectangle();
				if (hasInnerEllipse)
				{
					innerEllipse = new GraphicsPath();
					innerEllipse.AddEllipse(new Rectangle(
						flipX ? innerEllipseRect.X - innerEllipseRect.Width : innerEllipseRect.X,
						flipY ? innerEllipseRect.Y - innerEllipseRect.Height : innerEllipseRect.Y,
						innerEllipseRect.Width * 2 - 1,
						innerEllipseRect.Height * 2 - 1));
				}
				if (bgColor != Color.Transparent)
				{
					// 1/4の背景を描画
					var bWidth = rect.Width - (border != null ? border[Direction.Left] + border[Direction.Right] : 0);
					var bHalfWidth = bWidth / 2;
					var bHeigh = rect.Height - (border != null ? border[Direction.Top] + border[Direction.Bottom] : 0);
					var bHalfHeigh = bHeigh / 2;
					if ((flipX ? bWidth - bHalfWidth : bHalfWidth) > 0 && (flipY ? bHeigh - bHalfHeigh : bHalfHeigh) > 0)
						using (var brush = new SolidBrush(bgColor))
						{
							var bgRect = new Rectangle(
								flipX ? rect.X + (border != null ? border[Direction.Left] : 0) + bHalfWidth : rect.X + (border != null ? border[Direction.Left] : 0),
								flipY ? rect.Y + (border != null ? border[Direction.Top] : 0) + bHalfHeigh : rect.Y + (border != null ? border[Direction.Top] : 0),
								flipX ? bWidth - bHalfWidth : bHalfWidth + 1,
								flipY ? bHeigh - bHalfHeigh : bHalfHeigh + 1);
							if (hasInnerEllipse)
								using (var region = new Region(bgRect))
								{
									region.Exclude(innerEllipseRect);
									graph.FillRegion(brush, region);
									graph.SetClip(innerEllipseRect, CombineMode.Replace);
									graph.FillPath(brush, innerEllipse);
									graph.SetClip(rect, CombineMode.Replace);
								}
							else
								graph.FillRectangle(brush, bgRect);
						}
				}
				if (border != null)
				{
					if (hasInnerEllipse) cornerEllipse.AddPath(innerEllipse, false);
					if (border[flipX ? Direction.Right : Direction.Left] > 0)
					{
						// 左枠の半分
						Color color = colors == null ? Config.Config.ForeColor : colors[flipX ? Direction.Right : Direction.Left];
						if (color != Color.Transparent)
							using (var path = new GraphicsPath())
							using (var brush = new SolidBrush(color))
							{
								path.AddPolygon(new Point[] {
										new(flipX ? rect.X+rect.Width : rect.X,
											flipY ? rect.Y+rect.Height : rect.Y),

										new(flipX ? rect.X+rect.Width-cornerRect.Width : rect.X+cornerRect.Width,
											flipY ? rect.Y+rect.Height-cornerRect.Height : rect.Y+cornerRect.Height),

										new(flipX ? rect.X+rect.Width : rect.X,
											flipY ? rect.Y+rect.Height-cornerRect.Height : rect.Y+cornerRect.Height),});
								graph.SetClip(path, CombineMode.Intersect);
								graph.FillPath(brush, cornerEllipse);
								graph.SetClip(rect, CombineMode.Replace);
								graph.SmoothingMode = backUp;
								graph.FillRectangle(brush, new Rectangle(
									flipX ? rect.X + rect.Width - border[Direction.Right] : rect.X,
									flipY ? rect.Y + rect.Height / 2 : rect.Y + radius[corner],
									border[flipX ? Direction.Right : Direction.Left],
									rect.Height / 2 - radius[corner]));
								graph.SmoothingMode = SmoothingMode.AntiAlias;
							}
					}
					if (border[flipY ? Direction.Bottom : Direction.Top] > 0)
					{
						// 上枠の半分
						Color color = colors == null ? Config.Config.ForeColor : colors[flipY ? Direction.Bottom : Direction.Top];
						if (color != Color.Transparent)
							using (var path = new GraphicsPath())
							using (var brush = new SolidBrush(color))
							{
								path.AddPolygon(new Point[] {
										new(flipX ? rect.X+rect.Width : rect.X,
											flipY ? rect.Y+rect.Height : rect.Y),
										new(flipX ? rect.X+rect.Width-cornerRect.Width : rect.X+cornerRect.Width,
											flipY ? rect.Y+rect.Height-cornerRect.Height : rect.Y+cornerRect.Height),


										new(flipX ? rect.X+rect.Width-cornerRect.Width : rect.X+cornerRect.Width,
											flipY ? rect.Y+rect.Height : rect.Y)});

								graph.SetClip(path, CombineMode.Intersect);
								graph.FillPath(brush, cornerEllipse);
								graph.SetClip(rect, CombineMode.Replace);
								graph.SmoothingMode = backUp;
								graph.FillRectangle(brush, new Rectangle(
									flipX ? rect.X + rect.Width / 2 : rect.X + radius[corner],
									flipY ? rect.Y + rect.Height - border[Direction.Bottom] : rect.Y,
									rect.Width / 2 - (flipX ? radius[corner] : 0),
									border[flipY ? Direction.Bottom : Direction.Top]));
								graph.SmoothingMode = SmoothingMode.AntiAlias;
							}
					}
				}
				if (innerEllipse != null) innerEllipse.Dispose();
			}
			graph.SmoothingMode = backUp;
		}
	}
}

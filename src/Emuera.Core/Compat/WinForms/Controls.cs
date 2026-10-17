// andEmuera: System.Windows.Forms のコントロール類の互換シム。
//
// Android 版では実際の描画・入力は WebView が担当するため、ここにあるのは
// 「上流の EmueraConsole が触る状態を保持し、変更を Android 側へ通知する」ための器。
// 実際の橋渡しは MinorShift.Emuera.UI.Framework.Forms.MainWindow が行う。

using System.Collections.Generic;
using System.Drawing;

namespace System.Windows.Forms
{
	public class Control : IDisposable
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public int Left { get; set; }
		public int Top { get; set; }
		public string Text { get; set; } = string.Empty;
		public bool Enabled { get; set; } = true;
		public bool Visible { get; set; } = true;
		public bool Created { get; set; } = true;
		public bool IsDisposed { get; private set; }
		public Color BackColor { get; set; } = Color.Black;
		public Color ForeColor { get; set; } = Color.White;
		public Font Font { get; set; }
		public IntPtr Handle => IntPtr.Zero;
		public bool InvokeRequired => false;

		public Size Size
		{
			get => new(Width, Height);
			set { Width = value.Width; Height = value.Height; }
		}

		public Point Location
		{
			get => new(Left, Top);
			set { Left = value.X; Top = value.Y; }
		}

		public Rectangle Bounds
		{
			get => new(Left, Top, Width, Height);
			set { Left = value.X; Top = value.Y; Width = value.Width; Height = value.Height; }
		}

		public Rectangle ClientRectangle => new(0, 0, Width, Height);
		public Size ClientSize => new(Width, Height);

		/// <summary>画面座標をコントロール内座標へ変換する。Android では座標系が同一なのでそのまま返す。</summary>
		public Point PointToClient(Point p) => p;

		public Point PointToScreen(Point p) => p;

		/// <summary>オフスクリーン描画用の Graphics。WebView 版では実描画に使わない。</summary>
		public Graphics CreateGraphics()
		{
			var bmp = new Bitmap(Math.Max(Width, 1), Math.Max(Height, 1));
			return Graphics.FromImage(bmp);
		}

		public object Invoke(Delegate method) => method?.DynamicInvoke();

		public object Invoke(Delegate method, params object[] args) => method?.DynamicInvoke(args);

		public IAsyncResult BeginInvoke(Delegate method) { method?.DynamicInvoke(); return null; }

		public virtual void Refresh() { }

		public virtual void Invalidate() { }

		public virtual void Update() { }

		public bool Focus() => true;

		public void Show() { }

		public void Hide() { }

		/// <summary>現在のポインタ位置。Android の UI 層がタッチ位置を書き込む。</summary>
		public static Point MousePosition { get; set; }

		public virtual void Dispose() => IsDisposed = true;
	}

	/// <summary>ウィンドウ。Android では画面が 1 枚しかないため器だけ用意する。</summary>
	public class Form : Control
	{
		/// <summary>Android では常にアプリが前面にあるものとして扱う。</summary>
		public static Form ActiveForm { get; set; } = new();

		public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
		public DialogResult DialogResult { get; set; }
		public Form Owner { get; set; }
		public bool TopMost { get; set; }

		public DialogResult ShowDialog() => DialogResult.OK;

		public void Activate() { }

		public void Close() { }
	}

	public class Cursor
	{
		/// <summary>ポインタ画像のサイズ。ツールチップ位置の計算にのみ使われる。</summary>
		public Size Size { get; set; } = new(16, 16);

		public static Cursor Current { get; set; } = new();

		public static Point Position { get; set; }
	}

	public sealed class Screen
	{
		public Rectangle Bounds { get; internal set; }
		public Rectangle WorkingArea { get; internal set; }
		public bool Primary => true;
		public string DeviceName => "Android";

		/// <summary>Android の UI 層が実画面サイズを書き込む。</summary>
		public static Screen PrimaryScreen { get; } = new();

		public static Screen[] AllScreens => [PrimaryScreen];

		public static Screen FromControl(Control control) => PrimaryScreen;

		public static Screen FromPoint(Point point) => PrimaryScreen;
	}

	public enum MessageBoxDefaultButton
	{
		Button1 = 0,
		Button2 = 256,
		Button3 = 512,
	}

	public class PictureBox : Control
	{
		public Image Image { get; set; }
	}

	/// <summary>
	/// 上流はここに本物の <c>VScrollBar</c> を置き、<b>代入の副作用</b>に頼っている。
	///
	/// EmueraConsole.verticalScrollBarUpdate は表示行が減ったとき
	/// <c>ScrollBar.Maximum = 行数;</c> と書くだけで済ませているが、
	/// 本物の WinForms は「Maximum を Value より小さくしたら Value も引き下げる」ので、
	/// これだけで「最下行を表示中」(<c>Value == Maximum</c>) が保たれる。
	/// 素の自動プロパティにするとここが崩れ、<c>Value &gt; Maximum</c> のまま残る。
	/// 描画は <c>bottomLineNo = Value - 1</c> を最下行として使うので、
	/// 超過ぶんだけ画面全体が上へずれ、下側が背景色のまま空く。しかも
	/// <c>Value != Maximum</c> は「履歴表示中」と見なされるため、
	/// 一度ずれると入力のたびに悪化して戻らなくなる。
	///
	/// 本物は範囲外の <c>Value</c> 代入で例外を投げるが、上流はその例外に依存していないので
	/// ここではクランプで受ける (投げても誰も捌かない)。
	/// </summary>
	public class ScrollBar : Control
	{
		int value;
		int minimum;
		int maximum;

		public int Value
		{
			get => value;
			set
			{
				int next = Math.Clamp(value, minimum, maximum);
				if (next == this.value)
					return;
				this.value = next;
				OnValueChanged();
			}
		}

		public int Minimum
		{
			get => minimum;
			set
			{
				minimum = value;
				if (maximum < minimum)
					maximum = minimum;
				Value = this.value;   // 新しい範囲へ締め直す
			}
		}

		public int Maximum
		{
			get => maximum;
			set
			{
				maximum = value;
				if (minimum > maximum)
					minimum = maximum;
				Value = this.value;   // 同上。Value > Maximum ならここで引き下がる
			}
		}

		/// <summary>
		/// 上流の Designer は 1 を設定している (MainWindow.Designer.cs)。
		/// EmueraConsole は <c>Value == Maximum</c> を「最下行」と判定するので、
		/// WinForms の既定値 (10) ではなく上流に合わせる。
		/// </summary>
		public int LargeChange { get; set; } = 1;

		public int SmallChange { get; set; } = 1;

		public event EventHandler ValueChanged;

		protected void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
	}

	public class VScrollBar : ScrollBar { }

	public class HScrollBar : ScrollBar { }

	public class TextBox : Control
	{
		public int SelectionStart { get; set; }
		public int SelectionLength { get; set; }
		public bool ReadOnly { get; set; }
		public bool Multiline { get; set; }
		public int MaxLength { get; set; }

		public event EventHandler TextChanged;

		public void Clear() => Text = string.Empty;

		public void SelectAll() { }

		public void Select(int start, int length) { SelectionStart = start; SelectionLength = length; }

		protected void OnTextChanged() => TextChanged?.Invoke(this, EventArgs.Empty);
	}

	public class RichTextBox : TextBox
	{
		public Color SelectionColor { get; set; } = Color.White;
		public Color SelectionBackColor { get; set; } = Color.Black;
		public Font SelectionFont { get; set; }
		public bool DetectUrls { get; set; }
		public string Rtf { get; set; } = string.Empty;

		public void AppendText(string text) => Text += text;

		public void ScrollToCaret() { }
	}

	public class Label : Control { }

	/// <summary>
	/// HotkeyState が入れ子型 Link を using static で参照しているため、形だけ用意する。
	/// </summary>
	public class LinkLabel : Label
	{
		public class Link
		{
			public Link() { }
			public Link(int start, int length) { Start = start; Length = length; }
			public int Start { get; set; }
			public int Length { get; set; }
			public object LinkData { get; set; }
			public bool Enabled { get; set; } = true;
			public bool Visited { get; set; }
		}
	}

	/// <summary>
	/// WinForms の Timer 互換。UI スレッド前提ではなくスレッドプールで発火する。
	/// </summary>
	public class Timer : IDisposable
	{
		readonly Timers.Timer inner = new();

		public Timer()
		{
			inner.Elapsed += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
			inner.AutoReset = true;
		}

		public int Interval
		{
			get => (int)inner.Interval;
			set => inner.Interval = Math.Max(1, value);
		}

		public bool Enabled
		{
			get => inner.Enabled;
			set => inner.Enabled = value;
		}

		public object Tag { get; set; }

		public event EventHandler Tick;

		public void Start() => inner.Start();

		public void Stop() => inner.Stop();

		public void Dispose() => inner.Dispose();
	}

	public class ToolTip : IDisposable
	{
		readonly Dictionary<Control, string> texts = [];

		public bool Active { get; set; } = true;
		public bool OwnerDraw { get; set; }
		public int InitialDelay { get; set; } = 500;
		public int AutoPopDelay { get; set; } = 5000;
		public int ReshowDelay { get; set; } = 100;
		public bool ShowAlways { get; set; }
		public bool UseAnimation { get; set; }
		public bool UseFading { get; set; }
		public Color ForeColor { get; set; } = Color.Black;
		public Color BackColor { get; set; } = Color.LightYellow;
		public string ToolTipTitle { get; set; }

		/// <summary>現在表示中のツールチップ本文。Android 側はこれを読んで長押し表示に使う。</summary>
		public string CurrentText { get; private set; }

		public event DrawToolTipEventHandler Draw;
		public event PopupEventHandler Popup;

		public string GetToolTip(Control control)
			=> control != null && texts.TryGetValue(control, out var s) ? s : string.Empty;

		public void SetToolTip(Control control, string caption)
		{
			if (control == null)
				return;
			if (string.IsNullOrEmpty(caption))
				texts.Remove(control);
			else
				texts[control] = caption;
		}

		public void Show(string text, Control window) => CurrentText = text;

		public void Show(string text, Control window, int duration) => CurrentText = text;

		public void Show(string text, Control window, Point point) => CurrentText = text;

		public void Show(string text, Control window, Point point, int duration) => CurrentText = text;

		public void Show(string text, Control window, int x, int y) => CurrentText = text;

		public void Show(string text, Control window, int x, int y, int duration) => CurrentText = text;

		public void Hide(Control win) => CurrentText = null;

		public void RemoveAll()
		{
			texts.Clear();
			CurrentText = null;
		}

		protected void OnDraw(DrawToolTipEventArgs e) => Draw?.Invoke(this, e);

		protected void OnPopup(PopupEventArgs e) => Popup?.Invoke(this, e);

		public void Dispose() => texts.Clear();
	}

	public delegate void DrawToolTipEventHandler(object sender, DrawToolTipEventArgs e);

	public delegate void PopupEventHandler(object sender, PopupEventArgs e);

	public class DrawToolTipEventArgs : EventArgs
	{
		public DrawToolTipEventArgs(Graphics graphics, Control associatedWindow, Control associatedControl,
			Rectangle bounds, string toolTipText, Color backColor, Color foreColor, Font font)
		{
			Graphics = graphics;
			AssociatedWindow = associatedWindow;
			AssociatedControl = associatedControl;
			Bounds = bounds;
			ToolTipText = toolTipText;
			BackColor = backColor;
			ForeColor = foreColor;
			Font = font;
		}

		public Graphics Graphics { get; }
		public Control AssociatedWindow { get; }
		public Control AssociatedControl { get; }
		public Rectangle Bounds { get; }
		public string ToolTipText { get; }
		public Color BackColor { get; }
		public Color ForeColor { get; }
		public Font Font { get; }

		public void DrawBackground() { }

		public void DrawBorder() { }

		public void DrawText() { }

		public void DrawText(TextFormatFlags flags) { }
	}

	public class PopupEventArgs : CancelEventArgsBase
	{
		public PopupEventArgs(Control associatedWindow, Control associatedControl, bool isBalloon, Size size)
		{
			AssociatedWindow = associatedWindow;
			AssociatedControl = associatedControl;
			IsBalloon = isBalloon;
			ToolTipSize = size;
		}

		public Control AssociatedWindow { get; }
		public Control AssociatedControl { get; }
		public bool IsBalloon { get; }
		public Size ToolTipSize { get; set; }
	}

	public class CancelEventArgsBase : EventArgs
	{
		public bool Cancel { get; set; }
	}

	public class KeyEventArgs : EventArgs
	{
		public KeyEventArgs(Keys keyData) => KeyData = keyData;

		public Keys KeyData { get; }
		public Keys KeyCode => KeyData & Keys.KeyCode;
		public Keys Modifiers => KeyData & Keys.Modifiers;
		public int KeyValue => (int)KeyCode;
		public bool Control => (KeyData & Keys.Control) != 0;
		public bool Shift => (KeyData & Keys.Shift) != 0;
		public bool Alt => (KeyData & Keys.Alt) != 0;
		public bool Handled { get; set; }
		public bool SuppressKeyPress { get; set; }
	}

	public class KeyPressEventArgs : EventArgs
	{
		public KeyPressEventArgs(char keyChar) => KeyChar = keyChar;
		public char KeyChar { get; set; }
		public bool Handled { get; set; }
	}

	public class MouseEventArgs : EventArgs
	{
		public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
		{
			Button = button;
			Clicks = clicks;
			X = x;
			Y = y;
			Delta = delta;
		}

		public MouseButtons Button { get; }
		public int Clicks { get; }
		public int X { get; }
		public int Y { get; }
		public int Delta { get; }
		public Point Location => new(X, Y);
	}

	public enum DialogResult
	{
		None = 0,
		OK = 1,
		Cancel = 2,
		Abort = 3,
		Retry = 4,
		Ignore = 5,
		Yes = 6,
		No = 7,
	}

	public enum MessageBoxButtons
	{
		OK = 0,
		OKCancel = 1,
		AbortRetryIgnore = 2,
		YesNoCancel = 3,
		YesNo = 4,
		RetryCancel = 5,
	}

	public enum MessageBoxIcon
	{
		None = 0,
		Error = 16,
		Question = 32,
		Warning = 48,
		Information = 64,
	}

	/// <summary>
	/// メッセージボックス。Android 側でダイアログを出すため <see cref="Handler"/> に委譲する。
	/// </summary>
	public static class MessageBox
	{
		/// <summary>(本文, タイトル, ボタン) を受け取ってユーザーの選択を返すハンドラ。</summary>
		public static Func<string, string, MessageBoxButtons, DialogResult> Handler { get; set; }

		public static DialogResult Show(string text) => Show(text, string.Empty, MessageBoxButtons.OK);

		public static DialogResult Show(string text, string caption) => Show(text, caption, MessageBoxButtons.OK);

		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
			=> Handler?.Invoke(text, caption, buttons) ?? DialogResult.OK;

		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
			=> Show(text, caption, buttons);

		public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
			=> Show(text, caption, buttons);

		public static DialogResult Show(Control owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
			=> Show(text, caption, buttons);
	}

	/// <summary>
	/// クリップボード。Android の ClipboardManager へ委譲する。
	/// </summary>
	public static class Clipboard
	{
		public static Func<string> Getter { get; set; }
		public static Action<string> Setter { get; set; }

		public static string GetText() => Getter?.Invoke() ?? string.Empty;

		public static void SetText(string text) => Setter?.Invoke(text);

		public static bool ContainsText() => !string.IsNullOrEmpty(GetText());

		public static void Clear() => Setter?.Invoke(string.Empty);

		public static void SetDataObject(object data) => Setter?.Invoke(data?.ToString());

		public static void SetDataObject(object data, bool copy) => SetDataObject(data);

		public static void SetDataObject(object data, bool copy, int retryTimes, int retryDelay) => SetDataObject(data);
	}

	public static class Application
	{
		public static string ExecutablePath { get; set; } = string.Empty;
		public static string StartupPath { get; set; } = string.Empty;
		public static string ProductVersion { get; set; } = "1.824";

		public static void DoEvents() { }

		public static void Exit() { }

		public static void Restart() { }
	}

	public static class Cursors
	{
		public static object Default => null;
		public static object Hand => null;
		public static object Arrow => null;
		public static object IBeam => null;
		public static object WaitCursor => null;
	}
}

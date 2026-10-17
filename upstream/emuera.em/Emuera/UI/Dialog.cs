using System.Windows.Forms;
static class Dialog
{
	public enum Result
	{
		Yes,
		No
	}
	public static void Show(string text)
	{
		MessageBox.Show(text);
	}
	public static void Show(string title, string text)
	{
		MessageBox.Show(text, title);
	}
	public static bool ShowPrompt(string title, string text)
	{
		var result = MessageBox.Show(text, title, MessageBoxButtons.YesNo);
		return result switch
		{
			DialogResult.Yes => true,
			_ => false
		};
	}
}

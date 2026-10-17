namespace MinorShift.Emuera.Runtime.Utils;

internal sealed class WinInput
{
	[System.Runtime.InteropServices.DllImport("user32.dll")]
	public static extern short GetKeyState(int nVirtKey);
}

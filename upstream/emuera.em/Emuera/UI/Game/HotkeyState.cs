using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;

namespace MinorShift.Emuera.UI.Game;

//HOTKEY STATE

// Q: What is this?
// A: This allows setting context sensitive hotkeys for games through the use of
// HOTKEY_STATE_INIT and HOTKEY_STATE commands, and HOTKEY.ERB file.

// Q: How do I use this?
// A: *) Take HOTKEY.ERB.example.txt from same folder as this file,
// rename it to HOTKEY.ERB, and put it in in the same folder as exe file.
// Do not put it in ERB folder.
// *) Place HOTKEY_STATE_INIT int somewhere in title, before any HOTKEY_STATE get called.
// int sets the size of state array.
// *) Place HOTKEY_STATE int int wherever you want, this is the same as doing state[int] = [int] in C#.
// *) Edit HOTKEY.ERB to your liking.
// *) Press ctrl-d in Emuera to enable hotkeys.

// Q: Anything else I should know?
// A: *) HOTKEY.ERB isn't an actual erabasic file, it only supports a very limited subset of erabasic right now.
// It supports IF, ENDIF, SIF, RETURN statements.
// It doesn't support ELSEIF and ELSE statements or anything else.
// If you find it too limiting, and have visual studio installed,
// you can temporarily replace keyToNumberRunInterpreter with keyToNumberHardcoded.
// *) DebugPrintBytecode function might be useful for debugging, it prints bytecode
// in human readable form, in file HOTKEY.ERB.bytecode.txt.
// *) KEYS:S in HOTKEY.ERB is same as Keys.S in C#. Everything C# supports, KEYS: supports too.

internal class HotkeyState
{
	private bool enabled = false;
	private bool availableStateArray = false;
	private bool availableHotkeyFile = false;
	private nint[] state;
	private List<int> bytecode = new(0x200);

	public void HotkeyStateInit(nint size)
	{
		state = new nint[size];
		availableStateArray = true;
		//Toggle();
	}

	public void HotkeyStateSet(nint index, nint value)
	{
		if (!availableStateArray)
		{
			throw new Exception("use HOTKEY_STATE_INIT before using HOTKEY_STATE");
		}
		state[index] = value;
	}

	private enum Eval
	{
		NUMBER = 0,
		KEYCOMPARE = 1,
		STATECOMPARE = 2,
		STATEGET = 3,
	}

	private enum Command
	{
		RETURN = 0,
		GOTOIFNOT = 1,
	}

	//private void DebugPrintBytecodeEval(StreamWriter writetext, ref int i)
	//{
	//	int b = bytecode[i++];
	//	if (b == (int)Eval.NUMBER)
	//	{
	//		writetext.WriteLine("NUMBER");
	//		b = bytecode[i++];
	//		writetext.WriteLine(b);
	//	} else if (b == (int)Eval.KEYCOMPARE)
	//	{
	//		writetext.WriteLine("KEYCOMPARE");
	//		b = bytecode[i++];
	//		writetext.WriteLine(b);
	//	} else if (b == (int)Eval.STATECOMPARE)
	//	{
	//		writetext.WriteLine("STATECOMPARE");
	//		b = bytecode[i++];
	//		writetext.WriteLine(b);
	//		b = bytecode[i++];
	//		writetext.WriteLine(b);
	//	} else if (b == (int)Eval.STATEGET)
	//	{
	//		writetext.WriteLine("STATEGET");
	//		b = bytecode[i++];
	//		writetext.WriteLine(b);
	//	} else
	//	{
	//		throw new Exception();
	//	}
	//}

	//private void DebugPrintBytecode()
	//{
	//	var filename = Program.ExeDir + "HOTKEY.ERB.bytecode.txt";
	//	StreamWriter writetext = new StreamWriter(filename);
	//	for (int i = 0; i < bytecode.Count; )
	//	{
	//		int b = bytecode[i++];
	//		if (b == (int)Command.GOTOIFNOT)
	//		{
	//			writetext.WriteLine("GOTOIFNOT");
	//			b = bytecode[i++];
	//			writetext.WriteLine($"LINE {b+1}");
	//			DebugPrintBytecodeEval(writetext, ref i);
	//		} else if (b == (int)Command.RETURN)
	//		{
	//			writetext.WriteLine("RETURN");
	//			DebugPrintBytecodeEval(writetext, ref i);
	//		} else
	//		{
	//			throw new Exception();
	//		}
	//	}
	//	writetext.Dispose();
	//}

	//returns 0 on success
	private int ParseEval(string line)
	{
		if (line.StartsWith("KEY == KEYS:"))
		{
			line = line.Substring("KEY == KEYS:".Length);
			Keys keys;
			//bool success = Enum.TryParse<Keys>("S", out keys);
			bool success = Enum.TryParse(line, out keys);
			if (!success) return 1;
			bytecode.Add((int)Eval.KEYCOMPARE);
			bytecode.Add((int)keys);
			return 0;
		}
		if (line.StartsWith("STATE:"))
		{
			line = line.Substring("STATE:".Length);
			int index = line.IndexOf(" == ");
			if (index != -1)
			{
				var line1 = line.Substring(0, index);
				var line2 = line.Substring(index + " == ".Length);
				bytecode.Add((int)Eval.STATECOMPARE);
				bytecode.Add(int.Parse(line1));
				bytecode.Add(int.Parse(line2));
				//throw new Exception("not implemented yet");
				return 0;
			}
			bytecode.Add((int)Eval.STATEGET);
			bytecode.Add(int.Parse(line));
			//throw new Exception("not implemented yet");
			return 0;
		}
		bytecode.Add((int)Eval.NUMBER);
		bytecode.Add(int.Parse(line));
		//throw new Exception("not implemented yet");
		return 0;
	}

	//returns 0 on success
	private int Parse(string[] lines)
	{
		if (lines.Length < 2) return 1;
		if (lines[0] != "@HOTKEY(KEY)") return 1;

		bool lastLineWasSIF = false;
		var gotoStack = new Stack<int>();
		
		for (int i = 1; i < lines.Length; i++)
		{
			var line = lines[i];
			line = line.TrimStart();
			int semicolonIndex = line.IndexOf(';');
			if (semicolonIndex != -1)
			{
				line = line.Substring(0, semicolonIndex);
			}
			line = line.TrimEnd();
			if (line.Length == 0) continue;

			if (line.StartsWith("IF "))
			{
				line = line.Substring("IF ".Length);
				bytecode.Add((int)Command.GOTOIFNOT);
				gotoStack.Push(bytecode.Count);
				bytecode.Add(777); //placeholder, will be overwritten
				int res = ParseEval(line);
				if (res != 0) return res;
			} else if (line.StartsWith("RETURN "))
			{
				line = line.Substring("RETURN ".Length);
				bytecode.Add((int)Command.RETURN);
				int res = ParseEval(line);
				if (res != 0) return res;
			} else if (line.StartsWith("SIF "))
			{
				lastLineWasSIF = true;
				line = line.Substring("SIF ".Length);
				bytecode.Add((int)Command.GOTOIFNOT);
				gotoStack.Push(bytecode.Count);
				bytecode.Add(777); //placeholder, will be overwritten
				int res = ParseEval(line);
				if (res != 0) return res;
				continue;
			} else if (line == "ENDIF")
			{
				int lastGoto = gotoStack.Pop();
				bytecode[lastGoto] = bytecode.Count;
			} else
			{
				throw new Exception("invalid line");
			}

			if (lastLineWasSIF)
			{
				int lastGoto = gotoStack.Pop();
				bytecode[lastGoto] = bytecode.Count;
				lastLineWasSIF = false;
			}

		}

		//DebugPrintBytecode();
		
		return 0;
	}

	public void Toggle()
	{
		if (enabled)
		{
			enabled = false;
			MessageBox.Show("hotkeys OFF");
			return;
		}

		if (!enabled && availableHotkeyFile)
		{
			enabled = true;
			MessageBox.Show("hotkeys ON");
			return;
		}

		var filename = Program.ExeDir + "HOTKEY.ERB";
		if (!File.Exists(filename))
		{
			MessageBox.Show("HOTKEY.ERB not found.");
			return;
		}

		string[] lines = File.ReadAllLines(filename, EncodingHandler.DetectEncoding(filename));
		int res = Parse(lines);
		if (res != 0)
		{
			MessageBox.Show("HOTKEY.ERB parsing failed.");
			return;
		}

		//MessageBox.Show("Not implemented yet.");
		availableHotkeyFile = true;
		enabled = true;
		if (availableStateArray)
		{
			MessageBox.Show("hotkeys ON");
		} else
		{
			MessageBox.Show("hotkeys ON, HOTKEY_STATE_INIT not called.");
		}
		
	}

	private int InterpreterEval(ref int i, int key)
	{
		int b = bytecode[i++];
		if (b == (int)Eval.NUMBER)
		{
			b = bytecode[i++];
			return b;
		} else if (b == (int)Eval.KEYCOMPARE)
		{
			b = bytecode[i++];
			//return (int)(key == b); //Cannot convert type bool to int
			return Convert.ToInt32(key == b);
		} else if (b == (int)Eval.STATECOMPARE)
		{
			b = bytecode[i++];
			int stateIndex = b;
			b = bytecode[i++];
			int valueToCompare = b;
			return Convert.ToInt32(state[stateIndex] == valueToCompare);
		} else if (b == (int)Eval.STATEGET)
		{
			b = bytecode[i++];
			return (int)state[b]; //Maybe should use int instead of nint.
		}

		throw new Exception("something is wrong");
	}

	public int keyToNumberRunInterpreter(KeyEventArgs e)
	{
		if (!enabled) return -1;
		if (!availableStateArray) return -1;
		if (!availableHotkeyFile) return -1;

		int key = (int)e.KeyData;

		for (int i = 0; i < bytecode.Count; )
		{
			int b = bytecode[i++];
			if (b == (int)Command.GOTOIFNOT)
			{
				b = bytecode[i++];
				int res = InterpreterEval(ref i, key);
				if (res == 0)
				{
					i = b;
					if (i < 0 || i + 1 > bytecode.Count) throw new Exception("something is wrong");
				}
			} else if (b == (int)Command.RETURN)
			{
				int res = InterpreterEval(ref i, key);
				return res;
			} else
			{
				throw new Exception("something is wrong");
			}
		}
		return -1;
	}

	//public int keyToNumberHardcoded(System.Windows.Forms.KeyEventArgs e)
	//{
	//	Keys key = e.KeyData;
	//	//if (e.Modifiers == Keys.Control && !(e.KeyData & Keys.ControlKey))
	//	//{
	//	//	enable();
	//	//}
	//	//if (e.Modifiers == Keys.Control && input == Keys.Oem3)
	//	//{
	//	//	enable();
	//	//}
	//	//else if (input == Keys.Oem3) //tilda
	//	//{
	//	//	enabled = !enabled;
	//	//}
	//	if (!enabled) return -1;
	//	if (!availableStateArray) return -1;
	//	//if (input == Keys.Q)
	//	//{
	//	//	return 1;
	//	//}
	//	if (state[0] == 0) //other things
	//	{
	//		if (key == Keys.A)
	//		{
	//			return 0;
	//		}
	//		if (key == Keys.S)
	//		{
	//			return 1;
	//		}
	//		if (key == Keys.D)
	//		{
	//			return 2;
	//		}
	//		if (key == Keys.F)
	//		{
	//			return 3;
	//		}
	//		if (key == Keys.W)
	//		{
	//			return 4;
	//		}
	//		if (key == Keys.E)
	//		{
	//			return 5;
	//		}
	//		if (key == Keys.R)
	//		{
	//			return 6;
	//		}
	//		if (key == Keys.X)
	//		{
	//			return 7;
	//		}
	//		if (key == Keys.C)
	//		{
	//			return 8;
	//		}
	//		if (key == Keys.V)
	//		{
	//			return 9;
	//		}
	//	}
	//	if (state[0] == 1) //looking at character, normal view
	//	{
	//		if (key == Keys.D)
	//		{
	//			return 811; //where everyone is
	//		}
	//		else if (key == Keys.F)
	//		{
	//			return 400; //map
	//		}
	//		else if (key == Keys.G)
	//		{
	//			return 405; //go out
	//		}
	//		else if (key == Keys.V)
	//		{
	//			if (state[1] == 0)
	//			{
	//				return 351; //time not stopped, follow me
	//			}
	//			else
	//			{
	//				return 825; //time stopped, gather pants
	//			}
	//		}
	//		else if (key == Keys.C)
	//		{
	//			return 410; //cleaning
	//		}
	//		else if (key == Keys.E)
	//		{
	//			if (state[1] == 0)
	//			{
	//				return 300; //time not stopped, talk
	//			}
	//			else
	//			{
	//				return 817; //time stopped, gather
	//			}
	//		}
	//		if (key == Keys.R)
	//		{
	//			return 312; //kiss
	//		}
	//	}
	//	if (state[0] == 2) //where everyone
	//	{
	//		if (key == Keys.A)
	//		{
	//			return 0; //where everyone stop
	//		}
	//	}
	//	if (state[0] == 3) //map display
	//	{
	//		if (key == Keys.A)
	//		{
	//			return 283; //map display stop
	//		}
	//		if (key == Keys.R)
	//		{
	//			return 286; //next map
	//		}
	//		if (key == Keys.W)
	//		{
	//			return 284; //previou map
	//		}
	//	}

	//	if (state[0] == 4) //sex receiving
	//	{
	//		if (key == Keys.F)
	//		{
	//			return (int)state[2]; //do nothing/partner initialized sex
	//		}
	//		if (key == Keys.R)
	//		{
	//			return 312; //kiss
	//		}
	//	}

	//	else if (key == Keys.T)
	//	{
	//		if (state[1] == 0)
	//		{
	//			return 355; //time stop
	//		}
	//		else
	//		{
	//			return 999; //time resume
	//		}
	//	}

	//	//else if (input == Keys.V)
	//	//{
	//	//	return 283; //map stop
	//	//}

	//	//else if (input == Keys.C)
	//	//{
	//	//	return 0; //where everyone is stop
	//	//}




	//	else if (key == Keys.B)
	//	{
	//		return 100; //go out stop
	//	}
	//	//else if (input == Keys.A)
	//	//{
	//	//	return -1; //put breakpoint here for fast edit and continue
	//	//}
	//	return -1;
	//}
}

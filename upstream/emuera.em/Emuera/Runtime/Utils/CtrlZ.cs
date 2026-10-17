//This is used by ctrl-z shortcut to undo last input.
//It does this by loading last save, and repeating all inputs except last one.
//TONOTDO:
//* Maybe add ctrl-shift-z hotkey that will undo AND refresh RNG at the end.
//Wouldn't work, need to store new rng separately, too much work.

using System.Collections.Generic;

namespace MinorShift.Emuera.Runtime.Utils;

internal class CtrlZ
{
	public int mLastSave = -1;
	private int mLastSaveExpected = -1;
	//^ only exists because save can have a "confirm" dialogue.
	public List<string> mInputs = new(0x40);

	public long[] mRandomSeed = new long[MTRandom.N32 + 1];

	public bool mRewindInProgress = false;
	public bool mRepeatedUndoRequested = false;

	public void Add(string s)
	{
		if (!Config.Config.Ctrl_Z_Enabled) return;
		if (mRewindInProgress) return;
		mInputs.Add(s);
	}

	// Called on clicking save file.
	public void OnSavePrepare(int aLastSaveExpected)
	{
		if (!Config.Config.Ctrl_Z_Enabled) return;
		mLastSaveExpected = aLastSaveExpected;
	}

	// Called on confirming save overwrite
	public void OnSave()
	{
		if (!Config.Config.Ctrl_Z_Enabled) return;
		mLastSave = mLastSaveExpected;
		mInputs.Clear();
		GlobalStatic.VEvaluator.Rand.GetRand(mRandomSeed);
	}

	// Called on clicking on a file to load.
	public void OnLoad(int aSaveFile)
	{
		if (!Config.Config.Ctrl_Z_Enabled) return;
		if (mRewindInProgress) return;
		mLastSave = aSaveFile;
		mInputs.Clear();
		GlobalStatic.VEvaluator.Rand.GetRand(mRandomSeed);
	}
}
using MinorShift.Emuera.Runtime.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MinorShift.Emuera.Forms;

public partial class RikaiDialog : Form
{
	private byte[] mEdict;
	private byte[] mEdictIndex;
	public delegate void RikaiSendIndex(byte[] edictind);
	private RikaiSendIndex rikaiSendIndex;
	public Encoding eucjp = Encoding.GetEncoding(20932);
	private List<string> dialogLines = new(16);
	DateTime start = DateTime.Now;

	public RikaiDialog(byte[] edict, RikaiSendIndex rikaiSendIndex)
	{
		this.mEdict = edict;
		this.rikaiSendIndex = rikaiSendIndex;

		dialogLines.Add("");
		dialogLines.Add("");
		dialogLines.Add("");
		dialogLines.Add("");
		dialogLines.Add("");
		dialogLines.Add("");

		InitializeComponent();

		backgroundWorker.DoWork += new DoWorkEventHandler(bwDoWork);
		backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwRunWorkerCompleted);
		backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(bwProgressChanged);

		label.Text = "starting";

		backgroundWorker.RunWorkerAsync();
	}

	private void bwRunWorkerCompleted(
		object sender, RunWorkerCompletedEventArgs e)
	{
		rikaiSendIndex(mEdictIndex);
	}

	private void bwProgressChanged(object sender, ProgressChangedEventArgs e)
	{
		var per = e.ProgressPercentage;
		dialogLines[0] = $"Generating {Config.RikaiFilename}.ind, {e.ProgressPercentage}% done.";
		if (per >= 20 && dialogLines[1].Length == 0)
		{
			var timeSpan = DateTime.Now - start;
			dialogLines[1] = $"20% in {(int)timeSpan.TotalSeconds} seconds";
		}
		if (per >= 40 && dialogLines[2].Length == 0)
		{
			var timeSpan = DateTime.Now - start;
			dialogLines[2] = $"40% in {(int)timeSpan.TotalSeconds} seconds";
		}
		if (per >= 60 && dialogLines[3].Length == 0)
		{
			var timeSpan = DateTime.Now - start;
			dialogLines[3] = $"60% in {(int)timeSpan.TotalSeconds} seconds";
		}
		if (per >= 80 && dialogLines[4].Length == 0)
		{
			var timeSpan = DateTime.Now - start;
			dialogLines[4] = $"80% in {(int)timeSpan.TotalSeconds} seconds";
		}
		if (per >= 100 && dialogLines[5].Length == 0)
		{
			var timeSpan = DateTime.Now - start;
			dialogLines[5] = $"100% in {(int)timeSpan.TotalSeconds} seconds";
		}
		label.Text = string.Join("\n", dialogLines);
	}

	class EdictParser
	{
		public byte[] mEdict;
		public ReadOnlyMemory<byte> mEdictMemory;
		public int mStart, mEnd;
		public bool mFinished;
		public const long mTickDelta = 4000;
		public long mTickNext = DateTime.Now.Ticks + mTickDelta;
		public ReadOnlyMemory<byte> mWord;
		public ReadOnlyMemory<byte> mPronunciation;

		public EdictParser(byte[] aEdict)
		{
			mEdict = aEdict;

			mEdictMemory = mEdict.AsMemory();

			for (; mEnd < mEdict.Length; ++mEnd)
			{
				if (mEdict[mEnd] == '\n')
				{
					mEnd--;
					mStart = mEnd;
					break;
				}
			}
		}

		public void Step()
		{
			mWord = null;
			mPronunciation = null;
			mEnd += 2;
			mStart = mEnd;

			if (mEnd >= mEdict.Length)
			{
				mFinished = true;
				return;
			}

			for (; mEnd < mEdict.Length; ++mEnd)
			{
				if (mEdict[mEnd] == '\n')
				{
					break;
				}
			}

			mEnd--;

			int c = mStart;
			for (; c < mEnd; c++)
			{
				if (mEdict[c] == ',')
				{
					throw new Exception();
				}
				else if (mEdict[c] == '/')
				{
					int b = c;
					b--;
					mWord = mEdictMemory.Slice(mStart, b - mStart);
					break;
				}
				else if (mEdict[c] == '[')
				{
					int b = c;
					b--;
					mWord = mEdictMemory.Slice(mStart, b - mStart);
					c++;
					int pronoun_start = c;
					for (; c < mEnd; c++)
					{
						if (mEdict[c] == ']')
						{
							mPronunciation = mEdictMemory.Slice(pronoun_start, c - pronoun_start);
							c = mEnd; //superbreak
							break;
						}
						else if (mEdict[c] == ',')
						{
							throw new Exception();
						}
					}
				}
			}
			return;
		}
	}

	class IndexEntryList
	{
		public List<IndexEntry> mList = new(0x10000); //TODO: get final size and add it here, rounded up just in case

		public void Add(ReadOnlyMemory<byte> aWord, int offset)
		{
			//Do binary search.
			//If found, add another offset to an existing entry.
			//If not found, add a new entry.
			//mList.Insert(0, aIndexEntry);
			// IndexEntry aIndexEntry

			var ie = new IndexEntry(aWord);

			int index = mList.BinarySearch(ie);
			if (index >= 0)
			{
				mList[index].offsets.Add(offset);
			} else
			{
				ie.offsets.Add(offset);
				mList.Insert(~index, ie);
			}
		}

		public void AddFirst(ReadOnlyMemory<byte> aWord, int offset)
		{
			var indexEntry = new IndexEntry(aWord);
			indexEntry.offsets.Add(offset);
			//TODO: either modify constructor to accept offset, or do fancy {x = 1, y = 2} creation thing.
			mList.Add(indexEntry);
		}

		public void AddSecond(ReadOnlyMemory<byte> aWord, int offset)
		{
			var indexEntry = new IndexEntry(aWord);
			indexEntry.offsets.Add(offset);
			//TODO: either modify constructor to accept offset, or do fancy {x = 1, y = 2} creation thing.
			mList.Add(indexEntry);
		}
	}

	class IndexEntry : IComparable<IndexEntry>
	{
		public ReadOnlyMemory<byte> mWord;
		public List<int> offsets;

		public IndexEntry(ReadOnlyMemory<byte> aWord)
		{
			mWord = aWord;
			offsets = new List<int>(4);
		}

		public int CompareTo(IndexEntry ie)
		{
			return Compare(this.mWord, ie.mWord);
		}
	}

	// Returns 0 when equal, 1 when first bigger, -1 when first smaller
	private static int Compare(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte> second)
	{
		var res = first.Span.SequenceCompareTo(second.Span);
		return res;
	}

	private void bwDoWork(object sender, DoWorkEventArgs e)
	{
		//GenerateIndex

		// So, let me describe how the search index looks like.
		// 0x00 search_word 0x01 offset 0x00 search_word 0x01 offset 0x00
		// search_word is the word we are searching for
		// offset is the position of this word in the dictionary
		// In case the word appears more than once, there will be multiple offsets.
		// 0x00 search_word 0x01 offset 0x01 offset 0x00
		// I can guarantee that 0x00 0x01 and 0x02 will never be used for encoding search_words, but since I can't be so sure about offsets, this is how offsets are encoded:
		// 1) Offsets are stored byte by byte, from most significant to least significant
		// 2) If there is ever a need to encode 0x00 in the offset itself, you use 0x02 ((0 << 2) + 2) instead. If you need 0x01, you use 0x02 ((1 << 2) + 2).
		// If you need 0x02, you use 0x02 ((2 << 2) + 2). If you need 0x03, you use ((3 << 2) + 2).
		// Just using plain text with '\n' and ',' as separators probably would've been simplier and only 20% bigger, but whatever, let's suffer.

		// The whole point of this is, this index is sorted, so you do binary search with byte sequence.
		// When you need to search for search_word, you search for 0x00 search_word 0x01, and it's guaranteed that there is only one such byte sequence in the index.


		BackgroundWorker worker = sender as BackgroundWorker;

		var indexEntryList = new IndexEntryList();

		int oldPercentage = -1;

		var edictParser = new EdictParser(mEdict);

		edictParser.Step();
		indexEntryList.AddFirst(edictParser.mWord, edictParser.mStart);

		edictParser.Step();
		indexEntryList.AddSecond(edictParser.mWord, edictParser.mStart);

		while (true)
		{
			edictParser.Step();
			if (edictParser.mFinished)
			{
				worker.ReportProgress(100);
				break;
			}
			
			{
				long tickNew = DateTime.Now.Ticks;
				if (tickNew > edictParser.mTickNext)
				{
					edictParser.mTickNext = tickNew + EdictParser.mTickDelta;
					int percentage = (int)(edictParser.mEnd / (float)mEdict.Length * 100);
					if (oldPercentage != percentage)
					{
						oldPercentage = percentage;
						worker.ReportProgress(percentage);
					}
				}
			}

			indexEntryList.Add(edictParser.mWord, edictParser.mStart);


			if (edictParser.mPronunciation.Equals(null)) continue;
			
			indexEntryList.Add(edictParser.mPronunciation, edictParser.mStart);
		}

		var memory = new MemoryStream(0x10000);
		memory.WriteByte(0);

		var bytesToWrite = new List<byte>(64);

		foreach (var ie in indexEntryList.mList)
		
		{
			memory.Write(ie.mWord.Span);
			foreach (var offset in ie.offsets)
			{
				int num = offset;
				int rem;

				memory.WriteByte(1);

				bytesToWrite.Clear();

				while (true) //single offset
				{
					num = Math.DivRem(num, 0x100, out rem);

					//if (rem >= 0 && rem <= 2)
					//if (rem >= 0 && rem <= 3)
					if ((rem >> 2) == 0)
					{
						bytesToWrite.Insert(0, 2);
						rem = (rem << 2) + 2;
						bytesToWrite.Insert(1, (byte)rem);
					}
					else
					{
						bytesToWrite.Insert(0, (byte)rem);
					}

					if (num == 0) break;
				} //end single offset

				foreach (byte b in bytesToWrite)
				{
					memory.WriteByte(b);
				}

			}

			memory.WriteByte(0);

		}

		mEdictIndex = memory.ToArray();

		using (FileStream fileStream = new FileStream(Program.ExeDir + Config.RikaiFilename + ".ind", FileMode.Create, FileAccess.Write))
		{
			memory.Position = 0;
			memory.CopyTo(fileStream);
		}
	}
}


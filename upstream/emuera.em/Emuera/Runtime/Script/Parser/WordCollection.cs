using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MinorShift.Emuera.Runtime.Script.Parser;

/// <summary>
/// 字句解析結果の保存場所。Listとその現在位置を結びつけるためのもの。
/// 基本的に全てpublicで
/// </summary>
internal sealed class WordCollection
{
	public WordCollection()
	{
		Collection = new();
		Pointer = Collection.First;
	}

	public LinkedList<Word> Collection;
	public LinkedListNode<Word> Pointer;
	int index = 0;
	private static Word nullToken = new NullWord();

	public void PointerReset()
	{
		index = 0;
		Pointer = Collection.First;
	}

	public void Add(Word token)
	{
		Collection.AddLast(token);
		Pointer ??= Collection.First;
	}
	public void Add(WordCollection wc)
	{
		foreach (var word in wc.Collection)
		{
			Collection.AddLast(word);
		}
		Pointer ??= Collection.First;
	}

	public void Clear()
	{
		Collection.Clear();
	}

	public void ShiftNext()
	{
		if (Pointer == null)
		{
			if (index == 0)
			{
				Pointer = Collection.First;
			}
			else
			{
			}
		}
		else
		{

			Pointer = Pointer.Next;
		}

		index++;
	}
	public Word Current
	{
		get
		{
			if (Pointer == null)
				return nullToken;
			return Pointer.Value;
		}
	}
	public bool EOL
	{
		get
		{
			return Pointer == null;
		}
	}
	#region EM_私家版_HTMLパラメータ拡張
	public Word Next
	{
		get
		{
			if (index + 1 >= Collection.Count)
				return nullToken;
			var nextPoint = Pointer.Next;
			return nextPoint.Value;
		}
	}
	#endregion

	public void Insert(Word w)
	{
		if (Pointer == null)
		{
			if (Collection.Count == 0)
			{
				Collection.AddFirst(w);
				Pointer = Collection.First;
			}
			else
			{
				Collection.AddLast(w);
				Pointer = Collection.Last;
			}
		}
		else
		{
			Collection.AddAfter(Pointer, w);
		}
	}
	public void InsertRange(WordCollection wc)
	{
		var pointer = Pointer?.Previous;
		LinkedListNode<Word> lastPointer = null;
		foreach (var word in wc.Collection)
		{
			if (pointer == null)
			{
				if (index == 0)
				{
					pointer = Collection.AddFirst(word);
				}
				else
				{

					pointer = Collection.AddLast(word);
				}
				Pointer = pointer;
			}
			else
			{

				pointer = Collection.AddAfter(pointer, word);
			}

			lastPointer ??= pointer;
		}

		Pointer = lastPointer;
	}
	public void Remove()
	{
		var next = Pointer.Next;
		Collection.Remove(Pointer);
		Pointer = next;
	}

	public void SetIsMacro()
	{
		foreach (Word word in Collection)
		{
			word.SetIsMacro();
		}
	}

	public WordCollection Clone()
	{
		var ret = new WordCollection();
		ret.Add(this);
		return ret;
	}

	// public WordCollection Clone(int start, int count)
	// {
	//     WordCollection ret = new();
	//     if (start > Collection.Count)
	//         return ret;
	//     int end = start + count;
	//     if (end > Collection.Count)
	//         end = Collection.Count;
	//     for (int i = start; i < end; i++)
	//     {
	//         ret.Collection.Add(Collection[i]);
	//     }
	//     return ret;
	// }
}

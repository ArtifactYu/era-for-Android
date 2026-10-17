using MinorShift.Emuera.UI.Game;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MinorShift.Emuera.Runtime.Utils.EvilMask;

internal sealed class ConsoleEscapedParts
{
	const int ROW_LINE = 0, ROW_DEPTH = 1, ROW_TOP = 2, ROW_BOTTOM = 3, ROW_ID = 4, ROW_DIVTYPE = 5;
	static readonly DataTable dt = new(); // divの情報を保存する場所
	static readonly Dictionary<long, AConsoleDisplayNode> parts = []; //　実際にdivデータを保存する場所

	/// <summary>
	/// dt と parts を触る間の排他。
	///
	/// 書き込み (Add / Remove / RemoveAt / Clear) は PRINT 処理から、
	/// 読み出し (GetPartsInRange) は描画から呼ばれる。両者は別スレッドになりうるが、
	/// DataTable も Dictionary もスレッドセーフではない。並行に触ると行の値が壊れ、
	/// (int)r[ROW_LINE] のようなキャストが InvalidCastException で落ちる。
	/// </summary>
	static readonly object sync = new();
	/// <summary>
	/// 最近GetPartsInRangeを行ったか
	/// </summary>
	static bool getOnce;
	/// <summary>
	/// 前回GetPartsInRangeを行った時の状態
	/// </summary>
	static int lastTop, lastBottom, lastGeneration;
	/// <summary>
	/// 前回のGetPartsInRangeから変動があるか
	/// </summary>
	public static bool Changed { get; private set; }
	/// <summary>
	/// 前回GetPartsInRangeを行った時と状況が変わったらfalseを返す。
	/// 重複計算防止用。
	/// </summary>
	public static bool TestedInRange(int top, int bottom, int gen)
	{
		lock (sync)
			return getOnce && top == lastTop && bottom == lastBottom && gen == lastGeneration;
	}
	static ConsoleEscapedParts()
	{
		// データベースを構築
		dt.Columns.Add("line", typeof(int)); // div所属のline
		dt.Columns.Add("depth", typeof(int)); // divの奥行き
		dt.Columns.Add("top", typeof(int)); // divの一行目がこの行より下回る
		dt.Columns.Add("bottom", typeof(int)); // divの一行目がこの行より上回る
		dt.Columns.Add("id", typeof(long)); // ユニークID（保存時点のTimeStamp）
		dt.Columns.Add("div", typeof(sbyte)); //　divの種類（display:absoluteかdisplay:relativeか）
	}
	public static void Clear()
	{
		lock (sync)
		{
			getOnce = false;
			dt.Clear();
			parts.Clear();
			Changed = false;
		}
	}
	public static void Add(AConsoleDisplayNode part, int line, int depth, int top, int bottom)
	{
		lock (sync)
		{
			var id = Utils.TimePoint();
			var row = dt.NewRow();
			row[ROW_LINE] = line;
			row[ROW_DEPTH] = depth;
			row[ROW_TOP] = top;
			row[ROW_BOTTOM] = bottom;
			row[ROW_ID] = id;
			if (part is ConsoleDivPart div)
				row[ROW_DIVTYPE] = (!div.IsRelative ? 2 : 0) | 1; // bit1 : divであるなら1　bit2 : divがdisplay:absoluteなら1
			else
				row[ROW_DIVTYPE] = 0;
			dt.Rows.Add(row);
			// id は TimePoint なので、同じ刻みで 2 回呼ばれると衝突する。
			// Add が投げると行だけ残って parts に無い状態になるので、上書きで揃える
			parts[id] = part;
			Changed = true;
		}
	}
	/// <summary>
	/// 所在行のlineNoがlineの行とその以降の行のdivデータを全部削除
	/// </summary>
	public static void Remove(int line)
	{
		lock (sync)
			RemoveWhere(l => l >= line);
	}
	/// <summary>
	/// 所在行のlineNoがlineの行のdivデータを削除
	/// </summary>
	public static void RemoveAt(int line)
	{
		lock (sync)
			RemoveWhere(l => l == line);
	}

	/// <summary>
	/// 所在行が条件に合う div データを消す。呼び出し元が sync を持っていること。
	///
	/// 行の値は <c>is int</c> で確かめてから使う。<c>(int)r[ROW_LINE]</c> と直に書くと、
	/// 値が DBNull や別の型になっていたときに InvalidCastException で
	/// PRINT ごと落ちてしまう (MaxLog 超えの RemoveAt で実際に踏んだ)。
	/// 型が合わない行はもう使えないので、この機会に一緒に捨てる。
	/// </summary>
	static void RemoveWhere(Func<int, bool> match)
	{
		foreach (var row in dt.AsEnumerable()
			.Where(r => r[ROW_LINE] is not int rowLine || match(rowLine)).ToArray())
		{
			if (row[ROW_ID] is long id)
				parts.Remove(id);
			dt.Rows.Remove(row);
			Changed = true;
		}
	}
	/// <summary>
	/// top行からbottom行までの範囲内表示内容があったdivをrmapで保存。
	/// genは今のbuttonGeneration，重複計算防止用。
	/// </summary>
	public static void GetPartsInRange(int top, int bottom, int gen, Dictionary<int, List<AConsoleDisplayNode>> rmap)
	{
		if (GlobalStatic.Console?.GetLineNo > Config.Config.MaxLog)
		{
			var correction = GlobalStatic.Console.GetLineNo - Config.Config.MaxLog;
			top += correction;
			bottom += correction;
		}
		if (rmap == null) return;
		rmap.Clear();
		lock (sync)
		{
			// すべてのdisplay:absoluteのdiv
			// とtop行からbottom行までの範囲内表示内容があったdiv
			// と所在行がtop行からbottom行までの範囲内のdiv
			// に対し
			// (行の値は is で確かめてから使う。理由は RemoveWhere と同じ)
			foreach (var row in dt.AsEnumerable()
				.Where(r => r[ROW_DIVTYPE] is sbyte divType
					&& ((divType & 2) != 0
						|| r[ROW_TOP] is int rowTop && rowTop <= bottom + 1
						&& r[ROW_BOTTOM] is int rowBottom && rowBottom >= top
						&& r[ROW_LINE] is int line
						&& (divType != 0 || top > line || line > bottom + 1))))
			{
				// parts に無い行は Add が途中で落ちた残骸。描けないので飛ばす
				if (row[ROW_DEPTH] is not int depth
					|| row[ROW_ID] is not long id
					|| !parts.TryGetValue(id, out var part))
					continue;
				if (!rmap.TryGetValue(depth, out var list))
				{
					list = [];
					rmap.Add(depth, list);
				}
				list.Add(part);
			}
			getOnce = true;
			lastTop = top; lastBottom = bottom; lastGeneration = gen;
			Changed = false;
		}
	}
}

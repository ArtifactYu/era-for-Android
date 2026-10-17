//統合_netEmuera: SQL_* 用の SQLite バックエンド。
//DB の実体は sav/temp_db/ に置き、セーブ時にセーブデータの隣へコピー、ロード時に書き戻す。
//起動時に temp_db を作り直すので、セーブせずに終了した変更は残らない。
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using trerror = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.Error;

namespace MinorShift.Emuera.Runtime.Utils;

internal static class ErbSql
{
	static SqliteConnection connection;
	//SqliteCommand を破棄すると配下のリーダーも閉じられるので、コマンドはリーダーと同じ寿命で持つ
	static readonly Dictionary<long, (SqliteCommand Command, SqliteDataReader Reader)> readers = [];

	static string TempDir => Config.Config.SavDir + "temp_db" + Path.DirectorySeparatorChar;

	public static void ConnectionOpen(string name)
	{
		Directory.CreateDirectory(TempDir);

		CloseAll();
		SqliteConnectionStringBuilder builder = new() { DataSource = TempDir + name + ".db" };
		connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		//ゲームのセーブ単位でしか永続化しないので、journal と fsync は落とす
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "PRAGMA journal_mode = OFF;PRAGMA synchronous = OFF;";
		command.ExecuteNonQuery();
	}

	/// <summary>
	/// 起動時に前回の作業用DBを捨てる。
	/// ここは SQL_* を使わないゲームでも必ず通るので、失敗しても起動は止めない
	/// （SQLite のアセンブリが無い構成や、DBファイルが掴まれている場合がある）。
	/// </summary>
	public static void SetUpTempDB()
	{
		if (!Directory.Exists(TempDir))
			return;
		try
		{
			CloseAll();
			Directory.Delete(TempDir, true);
			Directory.CreateDirectory(TempDir);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException or FileNotFoundException or TypeLoadException)
		{
		}
	}

	static void CloseAll()
	{
		foreach (long id in new List<long>(readers.Keys))
			CloseReader(id);
		//一度も開いていなければ SQLite に触らない
		if (connection == null)
			return;
		connection.Close();
		connection = null;
		SqliteConnection.ClearAllPools();
	}

	public static void Save(string destDirPath)
	{
		if (!Directory.Exists(TempDir))
			return;
		Directory.CreateDirectory(destDirPath);
		foreach (FileInfo dbFile in new DirectoryInfo(TempDir).EnumerateFiles())
			dbFile.CopyTo(Path.Combine(destDirPath, dbFile.Name), true);
	}

	public static void Load(string srcDirPath)
	{
		SetUpTempDB();
		if (!Directory.Exists(srcDirPath))
			return;
		Directory.CreateDirectory(TempDir);
		foreach (FileInfo dbFile in new DirectoryInfo(srcDirPath).EnumerateFiles())
			dbFile.CopyTo(Path.Combine(TempDir, dbFile.Name), true);
	}

	static SqliteConnection RequireConnection(string methodName)
	{
		return connection ?? throw new CodeEE(string.Format(trerror.SqlNoConnection.Text, methodName));
	}

	static SqliteDataReader RequireReader(string methodName, long readerID)
	{
		if (!readers.TryGetValue(readerID, out var entry))
			throw new CodeEE(string.Format(trerror.SqlNoReader.Text, methodName, readerID));
		return entry.Reader;
	}

	static void CloseReader(long readerID)
	{
		if (!readers.TryGetValue(readerID, out var old))
			return;
		old.Reader.Dispose();
		old.Command.Dispose();
		readers.Remove(readerID);
	}

	public static void ExecuteReader(string methodName, long readerID, string sql)
	{
		SqliteCommand command = RequireConnection(methodName).CreateCommand();
		command.CommandText = sql;
		//前のリーダーは開きっぱなしにしない
		CloseReader(readerID);
		readers[readerID] = (command, command.ExecuteReader());
	}

	public static object ExecuteScalar(string methodName, string sql)
	{
		using SqliteCommand command = RequireConnection(methodName).CreateCommand();
		command.CommandText = sql;
		return command.ExecuteScalar();
	}

	public static void ExecuteNonQuery(string methodName, string sql)
	{
		using SqliteCommand command = RequireConnection(methodName).CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	public static bool ReaderRead(string methodName, long readerID) => RequireReader(methodName, readerID).Read();

	public static long ReaderGetLong(string methodName, long readerID, int index) => RequireReader(methodName, readerID).GetInt64(index);

	public static string ReaderGetString(string methodName, long readerID, int index) => RequireReader(methodName, readerID).GetString(index);

	public static bool ReaderIsNull(string methodName, long readerID, int index)
	{
		SqliteDataReader reader = RequireReader(methodName, readerID);
		//列が無い場合も NULL 扱いにする（.netEmuera と同じ）
		return reader.FieldCount <= index || reader.IsDBNull(index);
	}
}

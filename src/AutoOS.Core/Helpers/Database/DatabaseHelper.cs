using System.Text;
using System.Text.Json.Nodes;
using LevelDB;
using Microsoft.Data.Sqlite;

namespace AutoOS.Core.Helpers.Database;

public static partial class DatabaseHelper
{
	public static JsonNode? Read(string databasePath, string domain, string keyName)
	{
		if (string.IsNullOrEmpty(databasePath))
			return null;

		if (databasePath.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
		{
			return ReadSqlite(databasePath, keyName);
		}

		if (!Directory.Exists(databasePath))
			return null;

		byte[] prefixBytes = Encoding.UTF8.GetBytes(domain);
		byte[] separatorBytes = [0x00, 0x01];
		byte[] keyNameBytes = Encoding.UTF8.GetBytes(keyName);
		byte[] finalKeyBytes = new byte[prefixBytes.Length + separatorBytes.Length + keyNameBytes.Length];

		Buffer.BlockCopy(prefixBytes, 0, finalKeyBytes, 0, prefixBytes.Length);
		Buffer.BlockCopy(separatorBytes, 0, finalKeyBytes, prefixBytes.Length, separatorBytes.Length);
		Buffer.BlockCopy(keyNameBytes, 0, finalKeyBytes, prefixBytes.Length + separatorBytes.Length, keyNameBytes.Length);

		JsonNode? result = null;
		try
		{
			result = ReadFromDatabase(databasePath, finalKeyBytes);
		}
		catch
		{
			string tempDatabasePath = databasePath + " - Copy";

			try
			{
				Directory.CreateDirectory(tempDatabasePath);

				foreach (string file in Directory.GetFiles(databasePath))
				{
					if (Path.GetFileName(file).Equals("LOCK", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					using var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					using var destStream = new FileStream(Path.Combine(tempDatabasePath, Path.GetFileName(file)), FileMode.Create, FileAccess.Write);
					sourceStream.CopyTo(destStream);
				}

				result = ReadFromDatabase(tempDatabasePath, finalKeyBytes);
			}
			catch { }
			finally
			{
				if (Directory.Exists(tempDatabasePath))
				{
					try
					{
						Directory.Delete(tempDatabasePath, true);
					}
					catch { }
				}
			}
		}

		return result;
	}

	private static JsonNode? ReadFromDatabase(string databasePath, byte[] finalKeyBytes)
	{
		using var database = new DB(new Options(), databasePath);
		byte[] valueBytes = database.Get(finalKeyBytes);

		if (valueBytes == null || valueBytes.Length == 0)
			return null;

		string value = Encoding.UTF8.GetString(valueBytes);

		if (value.Length > 0 && value[0] == '\x01')
			value = value[1..];

		return JsonNode.Parse(value);
	}

	public static bool Write(string databasePath, string domain, string keyName, JsonNode? jsonContent)
	{
		byte[] prefixBytes = Encoding.UTF8.GetBytes(domain);
		byte[] separatorBytes = [0x00, 0x01];
		byte[] keyNameBytes = Encoding.UTF8.GetBytes(keyName);
		byte[] finalKeyBytes = new byte[prefixBytes.Length + separatorBytes.Length + keyNameBytes.Length];

		Buffer.BlockCopy(prefixBytes, 0, finalKeyBytes, 0, prefixBytes.Length);
		Buffer.BlockCopy(separatorBytes, 0, finalKeyBytes, prefixBytes.Length, separatorBytes.Length);
		Buffer.BlockCopy(keyNameBytes, 0, finalKeyBytes, prefixBytes.Length + separatorBytes.Length, keyNameBytes.Length);

		byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContent!.ToJsonString());

		byte[] finalValueBytes = new byte[1 + jsonBytes.Length];
		finalValueBytes[0] = 0x01;
		Buffer.BlockCopy(jsonBytes, 0, finalValueBytes, 1, jsonBytes.Length);
		var options = new Options { CreateIfMissing = true };
		using var database = new DB(options, databasePath);
		database.Put(finalKeyBytes, finalValueBytes);
		return true;
	}

	public static bool Delete(string databasePath, string domain, string keyName)
	{
		byte[] prefixBytes = Encoding.UTF8.GetBytes(domain);
		byte[] separatorBytes = [0x00, 0x01];
		byte[] keyNameBytes = Encoding.UTF8.GetBytes(keyName);
		byte[] finalKeyBytes = new byte[prefixBytes.Length + separatorBytes.Length + keyNameBytes.Length];

		Buffer.BlockCopy(prefixBytes, 0, finalKeyBytes, 0, prefixBytes.Length);
		Buffer.BlockCopy(separatorBytes, 0, finalKeyBytes, prefixBytes.Length, separatorBytes.Length);
		Buffer.BlockCopy(keyNameBytes, 0, finalKeyBytes, prefixBytes.Length + separatorBytes.Length, keyNameBytes.Length);

		var options = new Options { CreateIfMissing = true };
		using var database = new DB(options, databasePath);
		database.Delete(finalKeyBytes);
		return true;
	}

	private static JsonNode? ReadSqlite(string sqlitePath, string keyName)
	{
		using var connection = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = sqlitePath, Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly }.ToString());
		connection.Open();

		using var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT value, compression_type, conversion_type FROM data WHERE key = @keyName;", connection);
		cmd.Parameters.AddWithValue("@keyName", keyName);

		using SqliteDataReader reader = cmd.ExecuteReader();
		if (reader.Read())
		{
			byte[] rawBytes = (byte[])reader.GetValue(0);
			int compressionType = reader.GetInt32(1);
			int conversionType = reader.GetInt32(2);

			byte[] decompressedBytes = rawBytes;
			if (compressionType == 1)
			{
				try
				{
					decompressedBytes = Snappier.Snappy.DecompressToArray(rawBytes);
				}
				catch
				{
					decompressedBytes = rawBytes;
				}
			}

			string decodedValue = conversionType switch
			{
				1 => Encoding.UTF8.GetString(decompressedBytes),
				0 => Encoding.Unicode.GetString(decompressedBytes),
				_ => Encoding.UTF8.GetString(decompressedBytes)
			};

			return JsonNode.Parse(decodedValue);
		}

		return null;
	}
}

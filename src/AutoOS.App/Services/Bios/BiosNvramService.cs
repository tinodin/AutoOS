using System.Text;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;
using AutoOS.Core.Helpers.ReadWrite;

namespace AutoOS.App.Services.Bios;

public sealed class BiosNvramService : IBiosNvramService
{
	private Dictionary<string, byte[]>? _nvarStore;

	public void LoadCurrentValues(List<Setting> settings, Dictionary<ushort, QidTarget> qidMap)
	{
		var blobs = new Dictionary<(string Name, Guid Guid), byte[]>();

		foreach (Setting setting in settings)
		{
			if (blobs.ContainsKey((setting.VariableName, setting.VariableGuid)))
				continue;

			if (TryGetCurrentBlob(setting, out byte[]? blob, out _) && blob != null)
				blobs[(setting.VariableName, setting.VariableGuid)] = blob;
		}

		HiiHelper.ApplySuppression(settings, blobs, qidMap);

		settings.RemoveAll(setting =>
			!blobs.TryGetValue((setting.VariableName, setting.VariableGuid), out byte[]? blob)
			|| setting.Width < 1
			|| setting.Offset + setting.Width > blob.Length);

		foreach (Setting setting in settings)
		{
			byte[] blob = blobs[(setting.VariableName, setting.VariableGuid)];

			if (HiiHelper.TryDecodeStringValue(setting, blob, out Option? stringMatched))
			{
				setting.Value = stringMatched != null ? stringMatched.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
				continue;
			}

			if (setting.Width > 8)
			{
				int byteLen = (int)setting.Width;
				int start = (int)setting.Offset;
				int end = start + byteLen;
				int nullPos = end;
				for (int j = start; j + 1 < end; j += 2)
				{
					if (blob[j] == 0 && blob[j + 1] == 0)
					{
						nullPos = j;
						break;
					}
				}
				string raw = Encoding.Unicode.GetString(blob, start, nullPos - start);
				setting.Value = HiiHelper.StripAnsi(raw);
			}
			else
			{
				ulong raw = 0;
				for (int i = 0; i < setting.Width; i++)
					raw |= (ulong)blob[setting.Offset + i] << (8 * i);

				setting.Value = setting.NumericFormat == IfrNumericFormat.Dec
					? HiiHelper.FormatValue(raw, setting.Options)
					: HiiHelper.FormatNumericValue(raw, setting.NumericFormat);
			}
		}
	}

	public bool PatchVariable(IEnumerable<KeyValuePair<Setting, SettingState>> settings, out byte[]? patched, out uint attributes)
	{
		patched = null;
		attributes = 0;

		Setting first = settings.First().Key;
		if (!TryGetCurrentBlob(first, out byte[]? blob, out attributes) || blob == null)
			return false;

		if (HiiHelper.TryDecodeStringValue(first, blob, out _))
		{
			KeyValuePair<Setting, SettingState> stringPair = settings.FirstOrDefault(pair => pair.Key.Offset == first.Offset);
			if (stringPair.Value.Value == null)
				return false;

			Option? option = SettingState.ResolveOption(first, stringPair.Value.Value);
			if (option == null)
				return false;

			patched = Encoding.ASCII.GetBytes((!string.IsNullOrEmpty(option.StoredValue) ? option.StoredValue : option.Label) + "\0");
			return true;
		}

		byte[] buffer = (byte[])blob.Clone();

		foreach ((Setting setting, SettingState state) in settings)
		{
			if (state.Value == null)
				return false;

			if (setting.Offset + setting.Width > buffer.Length)
				return false;

			byte[]? field;
			if (setting.Width > 8)
			{
				field = HiiHelper.EncodeStringValue(ReadStringPrefix(blob, setting), state.Value, setting.Width);
			}
			else
			{
				if (setting.Width < 1)
					return false;

				Option? option = SettingState.ResolveOption(setting, state.Value);
				if (!HiiHelper.TryParseNumericValue(state.Value, setting.NumericFormat, out ulong numeric) && option == null)
					return false;

				field = HiiHelper.EncodeNumericValue(option?.Value ?? numeric, setting.Width);
			}

			if (field == null)
				return false;

			Buffer.BlockCopy(field, 0, buffer, (int)setting.Offset, field.Length);
		}

		patched = buffer;
		return true;
	}

	public bool TryGetCurrentBlob(Setting setting, out byte[]? blob, out uint attributes)
	{
		if (HiiHelper.TryGetVariable(setting.VariableName, setting.VariableGuid, out blob, out attributes) && blob != null)
		{
			if (setting.VarStoreSize > 0 && blob.Length > setting.VarStoreSize && !HiiHelper.TryDecodeStringValue(setting, blob, out _))
				blob = blob.AsSpan(0, (int)setting.VarStoreSize).ToArray();

			return true;
		}

		_nvarStore ??= BuildNvarStore();
		blob = _nvarStore.TryGetValue(setting.VariableName, out byte[]? nvarBlob) ? nvarBlob : null;
		return blob != null;
	}

	private static Dictionary<string, byte[]> BuildNvarStore()
	{
		const int CHUNK_SIZE = 0x400000;
		const int SCAN_LIMIT = 96 * 1024 * 1024;
		const int OVERLAP = 0x10000;
		const int STOP_GAP_CHUNKS = 2;

		var store = new Dictionary<string, byte[]>(StringComparer.Ordinal);

		if (!HiiHelper.TryGetVariable("HiiDB", new Guid("1B838190-4625-4EAD-ABC9-CD5E6AF18FE0"), out byte[]? hii) || hii == null || hii.Length < 8)
			return store;

		uint dbsize = BitConverter.ToUInt32(hii, 0);
		uint address = BitConverter.ToUInt32(hii, 4);
		if (dbsize == 0 || dbsize > 64 * 1024 * 1024)
			return store;

		ulong baseAddress = address & ~0xFFFUL;
		uint totalSpan = dbsize + SCAN_LIMIT;
		int lastFoundChunk = -1;
		bool anyFound = false;

		using ReadWriteHelper read = new();
		byte[] chunk = new byte[CHUNK_SIZE];
		byte[] window = new byte[CHUNK_SIZE + OVERLAP];
		int windowLen = 0;
		ReadOnlySpan<byte> magic = "NVAR"u8;

		for (uint offset = 0; offset < totalSpan; offset += CHUNK_SIZE)
		{
			int chunkIndex = (int)(offset / CHUNK_SIZE);
			if (anyFound && lastFoundChunk >= 0 && chunkIndex - lastFoundChunk > STOP_GAP_CHUNKS)
				break;

			uint length = Math.Min(CHUNK_SIZE, totalSpan - offset);
			if (!read.ReadMemory(baseAddress + offset, length, chunk))
			{
				windowLen = 0;
				continue;
			}

			int keep = Math.Min(OVERLAP, windowLen);
			Buffer.BlockCopy(window, windowLen - keep, window, 0, keep);
			chunk.AsSpan(0, (int)length).CopyTo(window.AsSpan(keep));
			int scanLen = keep + (int)length;

			bool foundInChunk = false;
			int pos = 0;
			while (pos < scanLen)
			{
				int match = window.AsSpan(0, scanLen).Slice(pos).IndexOf(magic);
				if (match < 0)
					break;
				int entry = pos + match;
				pos = entry + 1;

				if (entry + 11 > scanLen)
					continue;
				ushort entrySize = BitConverter.ToUInt16(window, entry + 4);
				if (entrySize < 12 || entry + entrySize > scanLen)
					continue;

				int nameOffset = entry + 11;
				int nameEnd = Array.IndexOf(window, (byte)0, nameOffset, entrySize - 11);
				if (nameEnd < 0)
					continue;

				string variableName = Encoding.ASCII.GetString(window, nameOffset, nameEnd - nameOffset);
				if (variableName.Length == 0 || variableName != variableName.Trim())
					continue;

				store[variableName] = window.AsSpan(nameEnd + 1, entry + entrySize - nameEnd - 1).ToArray();
				foundInChunk = true;
			}

			if (foundInChunk)
			{
				anyFound = true;
				lastFoundChunk = chunkIndex;
			}
			windowLen = scanLen;
		}

		return store;
	}

	private static string ReadStringPrefix(byte[] blob, Setting setting)
	{
		int start = (int)setting.Offset;
		int end = start + (int)setting.Width;
		int nullPos = end;
		for (int j = start; j + 1 < end; j += 2)
		{
			if (blob[j] == 0 && blob[j + 1] == 0)
			{
				nullPos = j;
				break;
			}
		}
		return HiiHelper.GetAnsiPrefix(Encoding.Unicode.GetString(blob, start, nullPos - start));
	}
}

using System.Text;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Services.Bios;

public sealed class BiosNvramService : IBiosNvramService
{
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
		if (!HiiHelper.TryGetVariable(setting.VariableName, setting.VariableGuid, out blob, out attributes) || blob == null)
			return false;

		if (setting.VarStoreSize > 0 && blob.Length > setting.VarStoreSize && !HiiHelper.TryDecodeStringValue(setting, blob, out _))
			blob = blob.AsSpan(0, (int)setting.VarStoreSize).ToArray();

		return true;
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

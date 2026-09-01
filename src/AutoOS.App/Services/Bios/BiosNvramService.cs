using System.Text;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Services.Bios;

public sealed class BiosNvramService : IBiosNvramService
{
	private static AmiSmmTransport CreateTransport()
	{
		return new AmiSmmTransport();
	}

	public void LoadCurrentValues(List<Setting> settings, Dictionary<ushort, QidTarget> qidMap, AmiSmmTransport? transport = null)
	{
		bool created = transport == null;
		if (created)
		{
			transport = CreateTransport();
			if (!transport.TryLoadAndInit())
				return;
		}

		try
		{
			Dictionary<(string Name, Guid Guid), (byte[] Blob, uint Attributes, uint Status)> cache = new(settings.Count);

			foreach (Setting setting in settings)
			{
				(string Name, Guid Guid) key = (setting.VariableName, setting.VariableGuid);
				if (cache.ContainsKey(key))
					continue;

				if (transport!.TryGetVariable(setting.VariableName, setting.VariableGuid, out byte[]? blob, out uint attrs, out uint status) && blob != null)
				{
					if (setting.VarStoreSize > 0 && blob.Length > setting.VarStoreSize && !HiiHelper.TryDecodeStringValue(setting, blob, out _))
						blob = blob.AsSpan(0, (int)setting.VarStoreSize).ToArray();

					cache[key] = (blob, attrs, status);
				}
				else
				{
					cache[key] = (Array.Empty<byte>(), 0, status);
				}
			}

			foreach (Setting setting in settings)
			{
				if (cache.TryGetValue((setting.VariableName, setting.VariableGuid), out (byte[] Blob, uint Attributes, uint Status) entry) && entry.Blob.Length != 0)
				{
					setting.VarAttributes = entry.Attributes;
					setting.VarReadStatus = entry.Status;
				}
				else if (cache.TryGetValue((setting.VariableName, setting.VariableGuid), out (byte[] Blob, uint Attributes, uint Status) failed))
				{
					setting.VarReadStatus = failed.Status;
				}
			}

			Dictionary<(string Name, Guid Guid), byte[]> blobs = new(cache.Count);
			foreach (KeyValuePair<(string Name, Guid Guid), (byte[] Blob, uint Attributes, uint Status)> kv in cache)
			{
				if (kv.Value.Blob.Length != 0)
					blobs[kv.Key] = kv.Value.Blob;
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
					setting.Value = HiiHelper.ReadStringValue(blob, setting);
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
		finally
		{
			if (created)
				transport!.Dispose();
		}
	}

	public bool PatchVariable(IEnumerable<KeyValuePair<Setting, SettingState>> settings, out byte[]? patched, out uint attributes, AmiSmmTransport? transport = null)
	{
		bool created = transport == null;
		if (created)
		{
			transport = CreateTransport();
			if (!transport.TryLoadAndInit())
			{
				patched = null;
				attributes = 0;
				return false;
			}
		}

		try
		{
			patched = null;
			attributes = 0;

			using IEnumerator<KeyValuePair<Setting, SettingState>> enumerator = settings.GetEnumerator();
			if (!enumerator.MoveNext())
				return false;

			Setting first = enumerator.Current.Key;

			if (!transport!.TryGetVariable(first.VariableName, first.VariableGuid, out byte[]? blob, out uint attrs, out _) || blob == null)
				return false;

			attributes = attrs;

			if (HiiHelper.TryDecodeStringValue(first, blob, out _))
			{
				KeyValuePair<Setting, SettingState> stringPair = settings.FirstOrDefault(pair => pair.Key.Offset == first.Offset);
				if (stringPair.Value?.Value == null)
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
					field = HiiHelper.EncodeStringValue(HiiHelper.GetStringPrefix(blob, setting), state.Value, setting.Width);
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
		finally
		{
			if (created)
				transport!.Dispose();
		}
	}

	public bool TryGetCurrentBlob(Setting setting, out byte[]? blob, out uint attributes, AmiSmmTransport? transport = null)
	{
		bool created = transport == null;
		if (created)
		{
			transport = CreateTransport();
			if (!transport.TryLoadAndInit())
			{
				blob = null;
				attributes = 0;
				return false;
			}
		}

		try
		{
			if (transport!.TryGetVariable(setting.VariableName, setting.VariableGuid, out byte[]? data, out uint attrs, out _) && data != null)
			{
				if (setting.VarStoreSize > 0 && data.Length > setting.VarStoreSize && !HiiHelper.TryDecodeStringValue(setting, data, out _))
					data = data.AsSpan(0, (int)setting.VarStoreSize).ToArray();

				blob = data;
				attributes = attrs;
				return true;
			}

			blob = null;
			attributes = 0;
			return false;
		}
		finally
		{
			if (created)
				transport!.Dispose();
		}
	}
}

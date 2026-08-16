using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.ReadWrite;
using AutoOS.Core.Helpers.Registry;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.Core.Helpers.Bios;

public static partial class HiiHelper
{
	[GeneratedRegex("\u001b\\[[0-9;?]*(?:[ -/]*[@-~])")]
	private static partial Regex AnsiCsiRegex();

	private static readonly (byte Bit, string Name)[] FlagNamesMap =
	[
		(0x01, "READ_ONLY"),
		(0x02, "BUFFER_OVERFLOW"),
		(0x04, "CALLBACK"),
		(0x10, "RESET_REQUIRED"),
		(0x20, "REST_STYLE"),
		(0x40, "RECONNECT_REQUIRED"),
		(0x80, "OPTIONS_ONLY")
	];

	private const int QUESTION_PROMPT_OFFSET = 2;
	private const int QUESTION_HELP_OFFSET = 4;
	private const int QUESTION_TOKEN_OFFSET = 6;
	private const int QUESTION_VAR_STORE_ID_OFFSET = 8;
	private const int QUESTION_VAR_STORE_OFFSET = 10;
	private const int QUESTION_FLAGS_OFFSET = 12;
	private const int NUMERIC_FLAGS_OFFSET = 13;
	private const int STRING_MAX_SIZE_OFFSET = 14;
	private const int LANGUAGE_OFFSET = 42;

	public static bool TryReadHiiDb(out byte[]? data)
	{
		data = null;

		if (!TryGetVariable("HiiDB", new Guid("1B838190-4625-4EAD-ABC9-CD5E6AF18FE0"), out byte[]? variable) || variable == null)
			return false;

		if (variable.Length < 8)
			return false;

		uint size = BitConverter.ToUInt32(variable, 0);
		uint physicalAddress = BitConverter.ToUInt32(variable, 4);

		if (size == 0 || size > 64 * 1024 * 1024)
			return false;

		byte[] buffer = new byte[size];
		using ReadWriteHelper read = new();
		if (!read.ReadMemory(physicalAddress, size, buffer))
			return false;

		if (buffer.Length < 20)
			return false;

		data = buffer;
		return true;
	}

	public static bool TryGetBiosLanguage(out string language)
	{
		if (!TryGetVariable("PlatformLang", new Guid("8BE4DF61-93CA-11D2-AA0D-00E098032B8C"), out byte[]? data) || data == null)
		{
			language = string.Empty;
			return false;
		}

		int end = Array.IndexOf(data, (byte)0);
		if (end < 0)
			end = data.Length;

		language = Encoding.ASCII.GetString(data, 0, end).Split(';', 2)[0];
		return !string.IsNullOrEmpty(language);
	}

	public static bool TryGetVariable(string name, Guid guid, out byte[]? data)
	{
		return TryGetVariable(name, guid, out data, out _);
	}

	public static bool TryGetVariable(string name, Guid guid, out byte[]? data, out uint attributes)
	{
		data = null;
		attributes = 0;

		RegistryHelper.EnablePrivilege("SeSystemEnvironmentPrivilege");

		string guidString = GetGuidString(guid);

		for (int capacity = 0x4000; capacity <= 0x20000; capacity *= 2)
		{
			byte[] buffer = new byte[capacity];
			uint attrs = 0;

			uint size;
			unsafe
			{
				fixed (char* namePtr = name)
				fixed (char* guidPtr = guidString)
				fixed (byte* pointer = buffer)
				{
					size = PInvoke.GetFirmwareEnvironmentVariableEx(new PCWSTR(namePtr), new PCWSTR(guidPtr), pointer, (uint)buffer.Length, &attrs);
				}
			}

			if (size > 0)
			{
				data = buffer.AsSpan(0, (int)size).ToArray();
				attributes = attrs;
				return true;
			}

			int error = Marshal.GetLastWin32Error();
			if (error != (int)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
				return false;
		}

		return false;
	}

	public static bool TrySetVariable(string name, Guid guid, byte[] data)
	{
		return TrySetVariable(name, guid, data, 0);
	}

	public static bool TrySetVariable(string name, Guid guid, byte[] data, uint attributes)
	{
		RegistryHelper.EnablePrivilege("SeSystemEnvironmentPrivilege");

		string guidString = GetGuidString(guid);

		try
		{
			unsafe
			{
				fixed (char* namePtr = name)
				fixed (char* guidPtr = guidString)
				fixed (byte* pointer = data)
				{
					return PInvoke.SetFirmwareEnvironmentVariableEx(new PCWSTR(namePtr), new PCWSTR(guidPtr), pointer, (uint)data.Length, attributes) != 0;
				}
			}
		}
		catch
		{
			return false;
		}
	}

	public static string GetGuidString(Guid guid) => $"{{{guid.ToString().ToUpperInvariant()}}}";

	public static List<Setting> ParseDatabase(byte[] data, string lang, out Dictionary<ushort, QidTarget> qidMap)
	{
		List<ParsedPackageList> parsedPackageLists = ParseAll(data, lang);
		List<Setting> flattenedSettings = [];

		foreach (ParsedPackageList packageList in parsedPackageLists)
		{
			var byteDefaults = new Dictionary<(ushort VarStoreId, ushort Offset), ulong>();
			foreach (Question question in packageList.Questions)
			{
				if (question.DefaultValue.HasValue)
				{
					byteDefaults[(question.VarStoreId, question.Offset)] = question.DefaultValue.Value;
				}
			}

			foreach (Question question in packageList.Questions)
			{
				if (!packageList.VarStores.TryGetValue(question.VarStoreId, out VarStore? varStore))
					continue;

				if (string.IsNullOrWhiteSpace(question.Prompt) || question.Prompt.Contains("%d", StringComparison.OrdinalIgnoreCase))
					continue;

				List<Option> options = question.Options;
				ulong? defaultValue = byteDefaults.TryGetValue((question.VarStoreId, question.Offset), out ulong value) ? value : question.DefaultValue;

				if (packageList.Languages.Count > 0 && options.Count > 0 && options.All(option => string.IsNullOrEmpty(option.Label)))
				{
					options = [];
					for (int languageIndex = 0; languageIndex < packageList.Languages.Count; languageIndex++)
					{
						HiiLanguage language = packageList.Languages[languageIndex];
						ulong asciiValue = !string.IsNullOrEmpty(language.Tag) ? (ulong)language.Tag[0] : (ulong)languageIndex;

						options.Add(new Option
						{
							Index = asciiValue.ToString(),
							Label = language.DisplayName,
							StoredValue = language.Tag,
							Value = (ulong)languageIndex,
							IsDefault = languageIndex == 0
						});
					}
				}

				string defaultLabel = string.Empty;
				if (defaultValue.HasValue)
				{
					ulong resolvedDefault = defaultValue.Value;
					Option? matchedOption = options.FirstOrDefault(option => option.Value == resolvedDefault);
					defaultLabel = matchedOption != null ? matchedOption.Label : FormatNumericValue(resolvedDefault, question.NumericFormat);
				}
				else
				{
					Option? defaultOption = options.FirstOrDefault(option => option.IsDefault);
					if (defaultOption != null)
					{
						defaultLabel = defaultOption.Label;
					}
					else if (options.Count > 0)
					{
						defaultLabel = options[0].Label;
					}
					else if (question.Minimum.HasValue)
					{
						defaultLabel = question.Minimum.Value.ToString();
					}
				}

				flattenedSettings.Add(new Setting
				{
					VariableName = varStore.Name,
					VariableGuid = varStore.Guid,
					Offset = question.Offset,
					Width = question.Width,
					VarStoreSize = varStore.Size,
					Value = string.Empty,
					Name = question.Prompt,
					Description = question.Help,
					Path = question.Path,
					Token = question.Token,
					Minimum = question.Minimum ?? 0,
					Maximum = question.Maximum ?? 0,
					Default = defaultLabel,
					Increment = (uint)(question.Step ?? 1),
					NumericFormat = question.NumericFormat,
					Options = options,
					SuppressionBlocks = question.SuppressionBlocks,
					Flags = GetFlagNames(question.Flags)
				});
			}
		}

		var resultList = new List<Setting>();
		var uniqueByKey = new Dictionary<(string VariableName, uint Offset, uint Width), Setting>();

		foreach (Setting setting in flattenedSettings)
		{
			(string VariableName, uint Offset, uint Width) key = (setting.VariableName, setting.Offset, setting.Width);
			if (uniqueByKey.TryAdd(key, setting))
			{
				resultList.Add(setting);
			}
			else if (string.IsNullOrWhiteSpace(uniqueByKey[key].Name) && !string.IsNullOrWhiteSpace(setting.Name))
			{
				int index = resultList.IndexOf(uniqueByKey[key]);
				uniqueByKey[key] = setting;
				if (index >= 0)
					resultList[index] = setting;
			}
		}

		qidMap = BuildQidMap(parsedPackageLists, resultList);

		return resultList;
	}

	private static Dictionary<ushort, QidTarget> BuildQidMap(List<ParsedPackageList> parsedPackageLists, List<Setting> settings)
	{
		var rawQids = new Dictionary<ushort, (ushort VarStoreId, ushort Offset)>();
		foreach (ParsedPackageList packageList in parsedPackageLists)
		{
			foreach (Question question in packageList.Questions)
			{
				if (question.Opcode is not (IfrOpcode.OneOf or IfrOpcode.CheckBox or IfrOpcode.Numeric))
					continue;

				if (ushort.TryParse(question.Token, out ushort qid))
					rawQids.TryAdd(qid, (question.VarStoreId, question.Offset));
			}
		}

		var resolvableVarStores = new Dictionary<ushort, (string Name, Guid Guid)>();
		foreach (Setting setting in settings)
		{
			if (!ushort.TryParse(setting.Token, out ushort qid) || !rawQids.TryGetValue(qid, out (ushort VarStoreId, ushort Offset) target))
				continue;

			resolvableVarStores.TryAdd(target.VarStoreId, (setting.VariableName, setting.VariableGuid));
		}

		var qidMap = new Dictionary<ushort, QidTarget>();
		foreach (KeyValuePair<ushort, (ushort VarStoreId, ushort Offset)> entry in rawQids)
		{
			if (resolvableVarStores.TryGetValue(entry.Value.VarStoreId, out (string Name, Guid Guid) varStore))
				qidMap.Add(entry.Key, new QidTarget(varStore.Name, varStore.Guid, entry.Value.Offset));
		}

		return qidMap;
	}

	public static void ApplySuppression(List<Setting> settings, Dictionary<(string Name, Guid Guid), byte[]> variableBlobs, Dictionary<ushort, QidTarget> qidMap)
	{
		byte? CurrentValue(ushort qid)
		{
			if (!qidMap.TryGetValue(qid, out QidTarget target))
				return null;

			if (!variableBlobs.TryGetValue((target.VariableName, target.VariableGuid), out byte[]? blob))
				return null;

			if (target.Offset >= blob.Length)
				return null;

			return blob[target.Offset];
		}

		foreach (Setting setting in settings)
		{
			List<List<SuppressionBlock>?>? blocks = setting.SuppressionBlocks;
			if (blocks == null || blocks.Count != setting.Options.Count)
				continue;

			for (int i = blocks.Count - 1; i >= 0; i--)
			{
				if (BlockHides(blocks[i], CurrentValue))
				{
					setting.Options.RemoveAt(i);
					blocks.RemoveAt(i);
				}
			}

			for (int i = setting.Options.Count - 1; i >= 1; i--)
			{
				if (setting.Options.Take(i).Any(option => option.Value == setting.Options[i].Value))
				{
					setting.Options.RemoveAt(i);
					blocks.RemoveAt(i);
				}
			}
		}
	}

	private static List<ParsedPackageList> ParseAll(byte[] database, string lang)
	{
		var results = new List<ParsedPackageList>();

		foreach (PackageList packageList in GetPackageLists(database))
		{
			var packages = GetPackages(packageList.Payload).ToList();
			var languageTables = new Dictionary<string, Dictionary<ushort, string>>();
			var languages = new List<HiiLanguage>();
			var uniqueLanguageTags = new HashSet<string>();
			Dictionary<ushort, string>? englishStrings = null;

			foreach ((HiiPackageType type, ReadOnlyMemory<byte> payload) in packages)
			{
				if (type != HiiPackageType.Strings)
					continue;
				string? languageTag = GetPackageLanguage(payload.Span);
				Dictionary<ushort, string> strings = new HiiStringTable(payload.Span).StringsById;
				if (languageTag == "en-US" && englishStrings == null)
				{
					englishStrings = strings;
				}
				if (!string.IsNullOrEmpty(languageTag))
				{
					languageTables[languageTag] = strings;
					if (uniqueLanguageTags.Add(languageTag))
					{
						string displayName = strings.TryGetValue(1, out string? name) && !string.IsNullOrWhiteSpace(name) ? name : languageTag;
						languages.Add(new HiiLanguage(languageTag, displayName));
					}
				}
			}

			Dictionary<ushort, string> stringTable = englishStrings != null ? [with(englishStrings)] : [];
			if (languageTables.TryGetValue(lang, out Dictionary<ushort, string>? selectedLanguageTable))
			{
				foreach ((ushort stringId, string text) in selectedLanguageTable)
				{
					if (!string.IsNullOrEmpty(text))
						stringTable[stringId] = text;
				}
			}

			var varStores = new Dictionary<ushort, VarStore>();
			var formTitles = new Dictionary<ushort, string>();
			var formReferences = new List<(ushort Parent, ushort Target, string Label)>();
			var formQuestionLists = new List<List<Question>>();
			var formOrder = new List<ushort>();
			var itemsByForm = new Dictionary<ushort, List<FormItem>>();

			foreach ((HiiPackageType type, ReadOnlyMemory<byte> payload) in packages)
			{
				if (type == HiiPackageType.Strings)
					continue;
				if (type == HiiPackageType.Forms)
				{
					ParsedForms parsedForms = ParseForms(payload.Span, stringTable);
					foreach (KeyValuePair<ushort, VarStore> pair in parsedForms.VarStores)
						varStores[pair.Key] = pair.Value;
					foreach (KeyValuePair<ushort, string> pair in parsedForms.FormTitles)
						formTitles[pair.Key] = pair.Value;
					formReferences.AddRange(parsedForms.FormReferences);
					formQuestionLists.Add(parsedForms.Questions);
					foreach (ushort fid in parsedForms.FormOrder)
					{
						if (!formOrder.Contains(fid))
							formOrder.Add(fid);
					}
					foreach ((ushort fid, List<FormItem> items) in parsedForms.FormItems)
					{
						if (!itemsByForm.TryGetValue(fid, out List<FormItem>? existing))
						{
							existing = [];
							itemsByForm[fid] = existing;
						}
						existing.AddRange(items);
					}
				}
			}

			var parentByForm = new Dictionary<ushort, (ushort Parent, string Label)>();
			foreach ((ushort parent, ushort child, string label) in formReferences)
			{
				parentByForm.TryAdd(child, (parent, label));
			}

			string PathFor(ushort formId)
			{
				var pathParts = new List<string>();
				ushort currentFormId = formId;
				var visited = new HashSet<ushort>();
				int guard = 0;

				while (currentFormId != 0 && parentByForm.TryGetValue(currentFormId, out (ushort Parent, string Label) parentInfo) && guard < 32)
				{
					guard++;
					if (!visited.Add(currentFormId))
						break;

					string labelOrTitle = !string.IsNullOrEmpty(parentInfo.Label)
						? parentInfo.Label
						: (formTitles.TryGetValue(currentFormId, out string? formTitle) ? formTitle : string.Empty);

					pathParts.Insert(0, labelOrTitle);
					currentFormId = parentInfo.Parent;
				}

				string rootTitle = formTitles.TryGetValue(currentFormId, out string? title) ? title : string.Empty;
				if (!string.IsNullOrEmpty(rootTitle) && (pathParts.Count == 0 || pathParts[0] != rootTitle))
				{
					pathParts.Insert(0, rootTitle);
				}

				string path = string.Join(" / ", pathParts.Where(part => !string.IsNullOrEmpty(part)));
				if (path.StartsWith("Setup / ", StringComparison.Ordinal))
				{
					path = path[8..];
				}
				return path;
			}

			var pathByForm = formTitles.Keys.ToDictionary(formId => formId, formId => PathFor(formId));
			var menuOrder = new Dictionary<Question, int>();
			var visitedForms = new HashSet<ushort>();
			var pendingItems = new Stack<(ushort FormId, FormItem Item)>();
			int counter = 0;

			foreach (ushort fid in formOrder)
			{
				if (!visitedForms.Add(fid))
					continue;

				if (itemsByForm.TryGetValue(fid, out List<FormItem>? seedItems))
				{
					for (int i = seedItems.Count - 1; i >= 0; i--)
						pendingItems.Push((fid, seedItems[i]));
				}

				while (pendingItems.Count > 0)
				{
					(ushort formId, FormItem item) = pendingItems.Pop();

					if (item.Question != null)
					{
						menuOrder[item.Question] = counter++;
					}
					else if (parentByForm.TryGetValue(item.RefTarget, out (ushort Parent, string Label) targetInfo) && targetInfo.Parent == formId && visitedForms.Add(item.RefTarget) && itemsByForm.TryGetValue(item.RefTarget, out List<FormItem>? subItems))
					{
						for (int i = subItems.Count - 1; i >= 0; i--)
							pendingItems.Push((item.RefTarget, subItems[i]));
					}
				}
			}

			List<Question> allQuestions = [];
			foreach (List<Question> questionList in formQuestionLists)
			{
				foreach (Question question in questionList)
				{
					question.Path = pathByForm.TryGetValue(question.FormId, out string? path) ? path : string.Empty;
					allQuestions.Add(question);
				}
			}

			allQuestions = [.. allQuestions.OrderBy(question => menuOrder.GetValueOrDefault(question, int.MaxValue))];

			results.Add(new ParsedPackageList
			{
				Guid = packageList.Guid,
				VarStores = varStores,
				Questions = allQuestions,
				Languages = languages
			});
		}

		return results;
	}

	private static IEnumerable<PackageList> GetPackageLists(ReadOnlyMemory<byte> database)
	{
		int offset = 0;
		while (TryReadPackageList(database, offset, out PackageList packageList, out int nextOffset))
		{
			yield return packageList;
			offset = nextOffset;
		}
	}

	private static bool TryReadPackageList(ReadOnlyMemory<byte> database, int offset, out PackageList packageList, out int nextOffset)
	{
		ReadOnlySpan<byte> span = database.Span;
		if (offset + 20 > span.Length)
		{
			packageList = null!;
			nextOffset = offset;
			return false;
		}

		Guid guid = new(span.Slice(offset, 16));
		uint length = BinaryPrimitives.ReadUInt32LittleEndian(span[(offset + 16)..]);
		if (length < 24 || offset + length > span.Length)
		{
			packageList = null!;
			nextOffset = offset;
			return false;
		}

		packageList = new PackageList { Guid = guid, Payload = database.Slice(offset + 20, (int)length - 20) };
		nextOffset = offset + (int)length;
		return true;
	}

	private static IEnumerable<(HiiPackageType Type, ReadOnlyMemory<byte> Payload)> GetPackages(ReadOnlyMemory<byte> payload)
	{
		int offset = 0;
		while (TryReadPackage(payload, offset, out (HiiPackageType Type, ReadOnlyMemory<byte> Payload) package, out int nextOffset))
		{
			yield return package;
			offset = nextOffset;
		}
	}

	private static bool TryReadPackage(ReadOnlyMemory<byte> payload, int offset, out (HiiPackageType Type, ReadOnlyMemory<byte> Payload) package, out int nextOffset)
	{
		ReadOnlySpan<byte> span = payload.Span;
		if (offset + 4 > span.Length)
		{
			package = default;
			nextOffset = offset;
			return false;
		}

		uint length = (uint)(span[offset] | (span[offset + 1] << 8) | (span[offset + 2] << 16));
		var type = (HiiPackageType)span[offset + 3];
		if (length < 4 || offset + length > span.Length)
		{
			package = default;
			nextOffset = offset;
			return false;
		}

		package = (type, payload.Slice(offset + 4, (int)length - 4));
		nextOffset = offset + (int)length;
		return true;
	}

	private static string? GetPackageLanguage(ReadOnlySpan<byte> payload)
	{
		if (payload.Length <= LANGUAGE_OFFSET)
			return null;
		int nullIndex = payload[LANGUAGE_OFFSET..].IndexOf((byte)0);
		if (nullIndex < 0)
			return null;
		string languageTag = Encoding.ASCII.GetString(payload.Slice(LANGUAGE_OFFSET, nullIndex));
		return string.IsNullOrEmpty(languageTag) ? null : languageTag;
	}

	private static ParsedForms ParseForms(ReadOnlySpan<byte> formPackage, Dictionary<ushort, string> stringTable)
	{
		var varStores = new Dictionary<ushort, VarStore>();
		var questions = new List<Question>();
		var formTitles = new Dictionary<ushort, string>();
		var formReferences = new List<(ushort Parent, ushort Target, string Label)>();
		var formOrder = new List<ushort>();
		var formItems = new Dictionary<ushort, List<FormItem>>();

		void AddFormItem(ushort fid, FormItem item)
		{
			if (!formItems.TryGetValue(fid, out List<FormItem>? items))
			{
				items = [];
				formItems[fid] = items;
			}
			items.Add(item);
		}

		Question? currentQuestion = null;
		bool awaitingUInt64Default = false;
		IfrNumericFormat numericFormat = IfrNumericFormat.Dec;
		var scopes = new List<ParseScope>();

		int questionScopeIndex = -1;
		ushort formId = 0;
		int offset = 0;
		int end = formPackage.Length;

		while (offset + 2 <= end)
		{
			var opcode = (IfrOpcode)formPackage[offset];
			int length = formPackage[offset + 1] & 0x7F;
			if (length < 2 || offset + length > end)
				break;

			bool hasScope = (formPackage[offset + 1] & 0x80) != 0;
			if (hasScope)
				scopes.Add(new ParseScope());

			try
			{
				if (opcode == IfrOpcode.VarStore && length >= 22)
				{
					ushort varStoreId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 18)..]);
					ushort varStoreSize = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 20)..]);
					Guid guid = new(formPackage.Slice(offset + 2, 16));
					string name = ReadAsciiNullTerminated(formPackage, offset + 22);
					varStores[varStoreId] = new VarStore { Id = varStoreId, Name = name, Guid = guid, Size = varStoreSize };
				}
				else if (opcode == IfrOpcode.VarStoreEfi && length >= 26)
				{
					ushort varStoreId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 2)..]);
					Guid guid = new(formPackage.Slice(offset + 4, 16));
					ushort varStoreSize = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 24)..]);
					string name = ReadAsciiNullTerminated(formPackage, offset + 26);
					varStores[varStoreId] = new VarStore { Id = varStoreId, Name = name, Guid = guid, Size = varStoreSize };
				}
				else if (opcode == IfrOpcode.Form && length >= 6)
				{
					ushort newFormId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 2)..]);
					ushort titleStringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 4)..]);
					formTitles[newFormId] = GetString(stringTable, titleStringId);
					if (!formOrder.Contains(newFormId))
						formOrder.Add(newFormId);
					formId = newFormId;
					currentQuestion = null;
					awaitingUInt64Default = false;
				}
				else if (opcode == IfrOpcode.FormReference && length >= 15)
				{
					ushort promptStringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 2)..]);
					ushort targetFormId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 13)..]);
					string label = GetString(stringTable, promptStringId);
					formReferences.Add((formId, targetFormId, label));
					AddFormItem(formId, new FormItem(null, targetFormId));
					if (!formTitles.ContainsKey(targetFormId))
					{
						formTitles[targetFormId] = label;
						formOrder.Add(targetFormId);
					}
				}
				else if (opcode is IfrOpcode.OneOf or IfrOpcode.CheckBox or IfrOpcode.Numeric)
				{
					if (length >= 13)
					{
						ushort promptStringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_PROMPT_OFFSET)..]);
						ushort helpStringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_HELP_OFFSET)..]);
						ushort token = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_TOKEN_OFFSET)..]);
						ushort varStoreId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_VAR_STORE_ID_OFFSET)..]);
						ushort varStoreOffset = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_VAR_STORE_OFFSET)..]);
						byte questionFlags = formPackage[offset + QUESTION_FLAGS_OFFSET];
						uint width = 1;

						if (opcode is IfrOpcode.OneOf or IfrOpcode.Numeric && length >= 14)
						{
							byte numericFlags = formPackage[offset + NUMERIC_FLAGS_OFFSET];
							width = (uint)((numericFlags & 3) switch
							{
								0 => 1,
								1 => 2,
								2 => 4,
								3 => 8,
								_ => 1
							});
							numericFormat = (numericFlags & 0x80) != 0 ? IfrNumericFormat.None
								: (numericFlags & 0x40) != 0 ? IfrNumericFormat.Bin
								: (numericFlags & 0x20) != 0 ? IfrNumericFormat.Hex
								: IfrNumericFormat.Dec;
						}

						ulong? minimum = null, maximum = null, increment = null;
						if (opcode is IfrOpcode.OneOf or IfrOpcode.Numeric && length >= 17)
						{
							int dataSize = length - 14;
							if (dataSize == 3 && offset + 16 < offset + length)
							{
								minimum = formPackage[offset + 14];
								maximum = formPackage[offset + 15];
								increment = formPackage[offset + 16];
							}
							else if (dataSize == 6 && offset + 19 < offset + length)
							{
								minimum = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 14)..]);
								maximum = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 16)..]);
								increment = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 18)..]);
							}
							else if (dataSize == 12 && offset + 25 < offset + length)
							{
								minimum = BinaryPrimitives.ReadUInt32LittleEndian(formPackage[(offset + 14)..]);
								maximum = BinaryPrimitives.ReadUInt32LittleEndian(formPackage[(offset + 18)..]);
								increment = BinaryPrimitives.ReadUInt32LittleEndian(formPackage[(offset + 22)..]);
							}
							else if (dataSize == 24 && offset + 37 < offset + length)
							{
								minimum = BinaryPrimitives.ReadUInt64LittleEndian(formPackage[(offset + 14)..]);
								maximum = BinaryPrimitives.ReadUInt64LittleEndian(formPackage[(offset + 22)..]);
								increment = BinaryPrimitives.ReadUInt64LittleEndian(formPackage[(offset + 30)..]);
							}
						}

						currentQuestion = new Question
						{
							Opcode = opcode,
							VarStoreId = varStoreId,
							Offset = varStoreOffset,
							Width = width,
							Prompt = GetString(stringTable, promptStringId),
							Help = GetString(stringTable, helpStringId),
							Flags = questionFlags,
							FormId = formId,
							Token = token.ToString(),
							Minimum = minimum,
							Maximum = maximum,
							Step = increment,
							NumericFormat = numericFormat
						};
					questions.Add(currentQuestion);
					AddFormItem(formId, new FormItem(currentQuestion, 0));
					awaitingUInt64Default = false;
					questionScopeIndex = scopes.Count - 1;
					}
				}
				else if (opcode == IfrOpcode.String && length >= 15)
				{
					ushort promptStringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_PROMPT_OFFSET)..]);
					ushort helpStringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_HELP_OFFSET)..]);
					ushort token = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_TOKEN_OFFSET)..]);
					ushort varStoreId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_VAR_STORE_ID_OFFSET)..]);
					ushort varStoreOffset = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + QUESTION_VAR_STORE_OFFSET)..]);
					byte questionFlags = formPackage[offset + QUESTION_FLAGS_OFFSET];
					uint maxSize = formPackage[offset + STRING_MAX_SIZE_OFFSET];
					uint width = maxSize * 2;

					if (width == 0)
						width = 40;

					currentQuestion = new Question
					{
						Opcode = opcode,
						VarStoreId = varStoreId,
						Offset = varStoreOffset,
						Width = width,
						Prompt = GetString(stringTable, promptStringId),
						Help = GetString(stringTable, helpStringId),
						Flags = questionFlags,
						FormId = formId,
						Token = token.ToString()
					};
					questions.Add(currentQuestion);
					AddFormItem(formId, new FormItem(currentQuestion, 0));
					questionScopeIndex = scopes.Count - 1;
				}
				else if (opcode == IfrOpcode.SuppressIf && length == 2 && hasScope)
				{
					scopes[^1] = new ParseScope { Block = new SuppressionBlock() };
				}
				else if (opcode == IfrOpcode.End && length == 2)
				{
					if (scopes.Count > 0)
					{
						ParseScope closed = scopes[^1];
						scopes.RemoveAt(scopes.Count - 1);

						if (closed.DeferredOperator != 0)
						{
							SuppressionBlock? block = InnermostSuppressBlock(scopes);
							if (block != null && block.Tokens.Count > closed.TokensAtOpen)
								block.Tokens.Add(new SuppressionToken(closed.DeferredOperator, 0, 0));
						}
					}
				}
				else if (opcode == IfrOpcode.Default && currentQuestion != null && length >= 5)
				{
					byte valueType = formPackage[offset + 4];
					if (valueType <= 3 && offset + 5 + (1 << valueType) <= offset + length)
					{
						int byteWidth = 1 << valueType;
						currentQuestion.DefaultValue = ReadInteger(formPackage, offset + 5, byteWidth);
						awaitingUInt64Default = false;
					}
					else if (valueType == 8)
					{
						awaitingUInt64Default = true;
					}
				}
				else if (opcode == IfrOpcode.UInt64 && currentQuestion != null && awaitingUInt64Default && length >= 10)
				{
					currentQuestion.DefaultValue = BinaryPrimitives.ReadUInt64LittleEndian(formPackage[(offset + 2)..]);
					awaitingUInt64Default = false;
				}
				else if (IsExpressionOpcode(opcode))
				{
					SuppressionBlock? block = InnermostSuppressBlock(scopes);

					if (IsExpressionOperator(opcode) && hasScope)
					{
						scopes[^1] = new ParseScope
						{
							DeferredOperator = NormalizeOperator(opcode),
							TokensAtOpen = block?.Tokens.Count ?? 0
						};
					}
					else
					{
						block?.Tokens.Add(CreateSuppressionToken(opcode, formPackage, offset, length));
					}
				}
				else if (opcode == IfrOpcode.OneOfOption && currentQuestion != null && currentQuestion.Opcode == IfrOpcode.OneOf && length >= 6)
				{
					ushort stringId = BinaryPrimitives.ReadUInt16LittleEndian(formPackage[(offset + 2)..]);
					byte optionFlags = formPackage[offset + 4];
					byte valueType = length >= 6 ? formPackage[offset + 5] : (byte)0;
					ulong? optionValue = null;
					if (valueType <= 3 && offset + 6 + (1 << valueType) <= offset + length)
					{
						int byteWidth = 1 << valueType;
						optionValue = ReadInteger(formPackage, offset + 6, byteWidth);
					}

					currentQuestion.Options.Add(new Option
					{
						Index = (optionValue ?? 0).ToString(),
						Label = GetString(stringTable, stringId),
						Value = optionValue ?? 0,
						IsDefault = (optionFlags & 0x10) != 0
					});
					if ((optionFlags & 0x10) != 0 && !currentQuestion.DefaultValue.HasValue)
					{
						currentQuestion.DefaultValue = optionValue ?? 0;
					}
					currentQuestion.SuppressionBlocks ??= [];
					List<SuppressionBlock>? optionBlocks = null;
					for (int i = questionScopeIndex + 1; i < scopes.Count; i++)
					{
						SuppressionBlock? block = scopes[i].Block;
						if (block != null)
							(optionBlocks ??= []).Add(block);
					}
					currentQuestion.SuppressionBlocks.Add(optionBlocks);
				}
			}
			catch
			{
			}

			offset += length;
		}

		return new ParsedForms
		{
			VarStores = varStores,
			Questions = questions,
			FormTitles = formTitles,
			FormReferences = formReferences,
			FormOrder = formOrder,
			FormItems = formItems
		};
	}

	private sealed class ParseScope
	{
		public SuppressionBlock? Block { get; init; }

		public byte DeferredOperator { get; init; }

		public int TokensAtOpen { get; init; }
	}

	private static bool IsExpressionOpcode(IfrOpcode opcode) =>
		opcode is IfrOpcode.EqIdVal or IfrOpcode.EqIdId or IfrOpcode.EqIdValList or IfrOpcode.QuestionRef1
			or IfrOpcode.UInt8 or IfrOpcode.UInt16 or IfrOpcode.UInt32 or IfrOpcode.UInt64
			or IfrOpcode.True or IfrOpcode.False
			or IfrOpcode.And or IfrOpcode.Or or IfrOpcode.Not
			or IfrOpcode.ScopedAnd or IfrOpcode.ScopedOr or IfrOpcode.ScopedNot
			or IfrOpcode.Equal or IfrOpcode.NotEqual or IfrOpcode.GreaterThan
			or IfrOpcode.GreaterEqual or IfrOpcode.LessThan or IfrOpcode.LessEqual;

	private static bool IsExpressionOperator(IfrOpcode opcode) =>
		opcode is IfrOpcode.And or IfrOpcode.Or or IfrOpcode.Not
			or IfrOpcode.ScopedAnd or IfrOpcode.ScopedOr or IfrOpcode.ScopedNot
			or IfrOpcode.Equal or IfrOpcode.NotEqual or IfrOpcode.GreaterThan
			or IfrOpcode.GreaterEqual or IfrOpcode.LessThan or IfrOpcode.LessEqual;

	private static byte NormalizeOperator(IfrOpcode opcode) => opcode switch
	{
		IfrOpcode.ScopedAnd => (byte)IfrOpcode.And,
		IfrOpcode.ScopedOr => (byte)IfrOpcode.Or,
		IfrOpcode.ScopedNot => (byte)IfrOpcode.Not,
		_ => (byte)opcode
	};

	private static SuppressionBlock? InnermostSuppressBlock(List<ParseScope> scopes)
	{
		for (int i = scopes.Count - 1; i >= 0; i--)
		{
			if (scopes[i].Block != null)
				return scopes[i].Block;
		}

		return null;
	}

	private static SuppressionToken CreateSuppressionToken(IfrOpcode opcode, ReadOnlySpan<byte> data, int offset, int length)
	{
		switch (opcode)
		{
			case IfrOpcode.EqIdVal:
			case IfrOpcode.EqIdId:
				return new SuppressionToken((byte)opcode,
					BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..]),
					BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 4)..]));
			case IfrOpcode.EqIdValList:
			{
				ushort qid = BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..]);
				ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 4)..]);
				var values = new List<ushort>();
				for (int i = 0; i < count && offset + 6 + 2 * i + 2 <= offset + length; i++)
					values.Add(BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 6 + 2 * i)..]));
				return new SuppressionToken((byte)opcode, qid, 0, values);
			}
			case IfrOpcode.QuestionRef1:
				return new SuppressionToken((byte)opcode, BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..]), 0);
			case IfrOpcode.UInt8:
				return new SuppressionToken((byte)opcode, 0, data[offset + 2]);
			case IfrOpcode.UInt16:
				return new SuppressionToken((byte)opcode, 0, BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..]));
			case IfrOpcode.UInt32:
				return new SuppressionToken((byte)opcode, 0, BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 2)..]));
			case IfrOpcode.UInt64:
				return new SuppressionToken((byte)opcode, 0, BinaryPrimitives.ReadUInt64LittleEndian(data[(offset + 2)..]));
			default:
				return new SuppressionToken((byte)opcode, 0, 0);
		}
	}

	private static string ReadAsciiNullTerminated(ReadOnlySpan<byte> data, int start)
	{
		int length = data[start..].IndexOf((byte)0);
		if (length < 0)
			length = data.Length - start;
		return Encoding.ASCII.GetString(data.Slice(start, length));
	}

	private static string GetString(Dictionary<ushort, string> stringTable, ushort stringId) =>
		stringTable.TryGetValue(stringId, out string? value) ? value : string.Empty;

	private static ulong ReadInteger(ReadOnlySpan<byte> data, int offset, int width)
	{
		ulong result = 0;
		for (int byteIndex = 0; byteIndex < width && offset + byteIndex < data.Length; byteIndex++)
		{
			result |= (ulong)data[offset + byteIndex] << (8 * byteIndex);
		}
		return result;
	}

	private static List<string> GetFlagNames(byte flags)
	{
		var result = new List<string>();
		foreach ((byte bit, string name) in FlagNamesMap)
		{
			if ((flags & bit) != 0)
				result.Add(name);
		}
		return result;
	}

	public static string FormatValue(ulong rawValue, IEnumerable<Option> options)
	{
		string valueText = rawValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
		IReadOnlyList<Option> optionList = options as IReadOnlyList<Option> ?? options.ToList();
		if (optionList.Count > 0 && !optionList.Any(option => option.Value == rawValue || string.Equals(option.Index, valueText, StringComparison.OrdinalIgnoreCase)))
		{
			return string.Empty;
		}
		return valueText;
	}

	public static string FormatNumericValue(ulong rawValue, IfrNumericFormat numericFormat) =>
		numericFormat switch
		{
			IfrNumericFormat.Hex => rawValue.ToString("X", System.Globalization.CultureInfo.InvariantCulture),
			IfrNumericFormat.Bin => Convert.ToString((long)rawValue, 2),
			_ => rawValue.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};

	public static bool TryParseNumericValue(string? value, IfrNumericFormat numericFormat, out ulong result)
	{
		result = 0;
		if (string.IsNullOrWhiteSpace(value))
			return false;

		return numericFormat switch
		{
			IfrNumericFormat.Hex => ulong.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out result),
			IfrNumericFormat.Bin => TryParseBinary(value, out result),
			_ => ulong.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out result)
		};
	}

	private static bool TryParseBinary(string value, out ulong result)
	{
		result = 0;
		foreach (char c in value)
		{
			if (c is not '0' and not '1')
				return false;

			result = (result << 1) | (uint)(c - '0');
		}
		return true;
	}

	public static bool TryDecodeStringValue(Setting setting, byte[] blob, out Option? matched)
	{
		matched = null;
		if (setting.Options.Count == 0)
			return false;

		int start = (int)setting.Offset;
		if (start >= blob.Length)
			return false;

		int nullPos = Array.IndexOf(blob, (byte)0, start);
		int end = nullPos >= 0 ? nullPos : blob.Length;
		if (end <= start + (int)setting.Width)
			return false;

		string text = Encoding.ASCII.GetString(blob, start, end - start);
		matched = setting.Options.FirstOrDefault(option => string.Equals(option.StoredValue, text, StringComparison.OrdinalIgnoreCase))
			?? setting.Options.FirstOrDefault(option => string.Equals(option.Label, text, StringComparison.OrdinalIgnoreCase));
		return matched != null;
	}

	public static string StripAnsi(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		return AnsiCsiRegex().Replace(text, string.Empty);
	}

	public static string GetAnsiPrefix(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		var builder = new StringBuilder();
		int index = 0;
		Match match = AnsiCsiRegex().Match(text, index);
		while (match.Success && match.Index == index)
		{
			builder.Append(match.Value);
			index = match.Index + match.Length;
			match = AnsiCsiRegex().Match(text, index);
		}
		return builder.ToString();
	}

	public static byte[]? EncodeStringValue(string prefix, string value, uint width)
	{
		if (width < 2)
			return null;

		byte[] encoded = Encoding.Unicode.GetBytes(prefix + value);
		if (encoded.Length + 2 > width)
			return null;

		byte[] field = new byte[width];
		encoded.CopyTo(field, 0);
		return field;
	}

	public static byte[] EncodeNumericValue(ulong value, uint width)
	{
		byte[] field = new byte[width];
		for (int byteIndex = 0; byteIndex < width; byteIndex++)
			field[byteIndex] = (byte)(value >> (8 * byteIndex));
		return field;
	}
}

using System.Runtime.InteropServices;
using AutoOS.Core.Helpers.CPU.Models;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AutoOS.Core.Helpers.CPU;

public static partial class CpuHelper
{
	public static CpuArchitecture GetCpuArchitecture()
	{
		using RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

		string vendorId = key?.GetValue("VendorIdentifier")?.ToString() ?? "";
		string processorName = key?.GetValue("ProcessorNameString")?.ToString() ?? "";
		string identifier = key?.GetValue("Identifier")?.ToString() ?? "";

		CpuVendor vendor = CpuVendor.Unknown;
		if (vendorId.Contains("GenuineIntel", StringComparison.OrdinalIgnoreCase))
			vendor = CpuVendor.Intel;
		else if (vendorId.Contains("AuthenticAMD", StringComparison.OrdinalIgnoreCase))
			vendor = CpuVendor.AMD;

		uint family = 0, model = 0, stepping = 0;

		string[] parts = identifier.Split(' ');
		for (int i = 0; i < parts.Length; i++)
		{
			if (parts[i] == "Family" && i + 1 < parts.Length && uint.TryParse(parts[i + 1], out uint f))
				family = f;
			else if (parts[i] == "Model" && i + 1 < parts.Length && uint.TryParse(parts[i + 1], out uint m))
				model = m;
			else if (parts[i] == "Stepping" && i + 1 < parts.Length && uint.TryParse(parts[i + 1], out uint s))
				stepping = s;
		}

		uint displayFamily = family;
		uint displayModel = model;

		var arch = new CpuArchitecture
		{
			Vendor = vendor,
			Family = family,
			Model = model,
			Stepping = stepping,
			DisplayFamily = displayFamily,
			DisplayModel = displayModel
		};

		if (vendor == CpuVendor.Intel)
		{
			arch.DisplayName = processorName;
			arch.ArchitectureName = GetIntelArchName(displayFamily, displayModel);
			arch.DisplayFamily = 0x06;
		}
		else if (vendor == CpuVendor.AMD)
		{
			arch.DisplayName = processorName;
			arch.ArchitectureName = GetAmdArchName(displayFamily, displayModel);
		}

		return arch;
	}

	private static string GetIntelArchName(uint family, uint model)
	{
		if (family != 0x06) return "Unknown Intel";

		return model switch
		{
			0x97 or 0x9A => "Alder Lake",
			0xBA or 0xB7 or 0xBF => "Raptor Lake",
			0xAA => "Meteor Lake",
			0xBD => "Lunar Lake",
			0xAC or 0xAE => "Granite Rapids",
			0xAF => "Sierra Forest",
			0xCF => "Emerald Rapids",
			0x8F => "Sapphire Rapids",
			0x8C or 0x8D => "Tiger Lake",
			0xA7 => "Rocket Lake",
			0x7E => "Ice Lake",
			0xA5 or 0xA6 => "Comet Lake",
			0x66 => "Cannon Lake",
			0x8E or 0x9E => "Kaby Lake / Coffee Lake",
			0x55 => "Skylake-X / Cascade Lake",
			0x4E or 0x5E => "Skylake",
			0x3D or 0x47 or 0x4F or 0x56 => "Broadwell",
			0x3C or 0x45 or 0x46 or 0x3F => "Haswell",
			0x3A or 0x3E => "Ivy Bridge",
			0x2A or 0x2D => "Sandy Bridge",
			0xBE => "Gracemont (N-series)",
			_ => $"Intel Model {model:X2}H"
		};
	}

	private static string GetAmdArchName(uint family, uint model)
	{
		if (family == 0x17)
		{
			if (model <= 0x0F || (model >= 0x10 && model <= 0x1F) || model == 0x20)
			{
				if (model == 0x08 || model == 0x18) return "Zen+";
				return "Zen 1";
			}
			if ((model >= 0x30 && model <= 0x3F) || (model >= 0x60 && model <= 0x6F) || (model >= 0x70 && model <= 0x7F) || (model >= 0x90 && model <= 0x9F))
				return "Zen 2";
		}
		else if (family == 0x19)
		{
			if ((model >= 0x00 && model <= 0x0F) || (model >= 0x20 && model <= 0x2F) || (model >= 0x50 && model <= 0x5F))
				return "Zen 3";
			if ((model >= 0x10 && model <= 0x1F) || (model >= 0x60 && model <= 0x6F) || (model >= 0x70 && model <= 0x7F))
				return "Zen 4";
		}
		else if (family == 0x1A)
		{
			return "Zen 5";
		}

		return $"AMD Family {family:X2}H Model {model:X2}H";
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct SYSTEM_CPU_SET_INFORMATION
	{
		public uint Size;
		public uint Type;
		public SYSTEM_CPU_SET_INFORMATION_ANONYMOUS Anonymous;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct SYSTEM_CPU_SET_INFORMATION_ANONYMOUS
	{
		[FieldOffset(0)]
		public SYSTEM_CPU_SET_INFORMATION_CPU_SET CpuSet;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct SYSTEM_CPU_SET_INFORMATION_CPU_SET
	{
		public uint Id;
		public ushort Group;
		public byte LogicalProcessorIndex;
		public byte CoreIndex;
		public byte LastLevelCacheIndex;
		public byte NumaNodeIndex;
		public byte EfficiencyClass;
		public byte AllFlags;
		public byte SchedulingClass;
		public byte Reserved;
		public ulong AllocationTag;
	}

	public unsafe static CpuSetsInfo GetCpuSets()
	{
		var info = new CpuSetsInfo();

		List<CpuSet>? fakeCpuSets = CpuSetInformationFake.FakeCpuSets;
		if (fakeCpuSets is { Count: > 0 })
		{
			info.CpuSets = [.. fakeCpuSets.OrderBy(c => c.EfficiencyClass).ThenBy(c => c.CoreIndex).ThenBy(c => c.LogicalProcessorIndex)];
			ProcessCpuSets(info.CpuSets, info);
			return info;
		}

		var cpuSets = new List<CpuSet>();

		uint bufferSize = 0;
		PInvoke.GetSystemCpuSetInformation(null, 0, &bufferSize, HANDLE.Null, 0);

		if (bufferSize == 0) return info;

		IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
		try
		{
			uint returnedLength = 0;
			if (PInvoke.GetSystemCpuSetInformation(
				(Windows.Win32.System.SystemInformation.SYSTEM_CPU_SET_INFORMATION*)buffer,
				bufferSize,
				&returnedLength,
				HANDLE.Null,
				0))
			{
				int offset = 0;
				while (offset < (int)returnedLength)
				{
					SYSTEM_CPU_SET_INFORMATION cpuSetInfo = Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(IntPtr.Add(buffer, offset));
					if (cpuSetInfo.Size == 0) break;

					SYSTEM_CPU_SET_INFORMATION_CPU_SET cpuSet = cpuSetInfo.Anonymous.CpuSet;
					cpuSets.Add(new CpuSet
					{
						Id = cpuSet.Id,
						CoreIndex = cpuSet.CoreIndex,
						LogicalProcessorIndex = cpuSet.LogicalProcessorIndex,
						EfficiencyClass = cpuSet.EfficiencyClass,
						LastLevelCacheIndex = cpuSet.LastLevelCacheIndex,
						NumaNodeIndex = cpuSet.NumaNodeIndex
					});

					offset += (int)cpuSetInfo.Size;
				}
			}

			info.CpuSets = [.. cpuSets.OrderBy(c => c.EfficiencyClass).ThenBy(c => c.CoreIndex).ThenBy(c => c.LogicalProcessorIndex)];
			ProcessCpuSets(info.CpuSets, info);
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}

		return info;
	}

	private static void ProcessCpuSets(List<CpuSet> cpuSets, CpuSetsInfo info)
	{
		if (cpuSets.Count == 0) return;

		info.CoreCount = cpuSets.Count;
		byte firstEfficiencyClass = cpuSets[0].EfficiencyClass;
		byte firstLevelCache = cpuSets[0].LastLevelCacheIndex;
		byte firstNumaNodeIndex = cpuSets[0].NumaNodeIndex;

		foreach (CpuSet cpuSet in cpuSets)
		{
			if (cpuSet.CoreIndex != cpuSet.LogicalProcessorIndex)
			{
				info.HyperThreading = true;
				int threadsDiff = Math.Abs(cpuSet.LogicalProcessorIndex - cpuSet.CoreIndex);
				if (info.MaxThreadsPerCore < threadsDiff)
					info.MaxThreadsPerCore = threadsDiff + 1;
			}

			if (cpuSet.EfficiencyClass != firstEfficiencyClass)
				info.EfficiencyClass = true;

			if (cpuSet.LastLevelCacheIndex != firstLevelCache)
				info.LastLevelCache = true;

			if (cpuSet.NumaNodeIndex != firstNumaNodeIndex)
				info.NumaNode = true;
		}
	}

	public static (List<CpuCore> PCores, List<CpuCore> ECores) GroupCpuSetsByEfficiencyClass(CpuSetsInfo cpuSetsInfo)
	{
		var pCores = new List<CpuCore>();
		var eCores = new List<CpuCore>();

		if (!cpuSetsInfo.EfficiencyClass)
		{
			pCores.AddRange(GroupCpuSetsByCore(cpuSetsInfo.CpuSets));
			return (pCores, eCores);
		}

		var groupedByEfficiency = cpuSetsInfo.CpuSets
			.GroupBy(c => c.EfficiencyClass)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (IGrouping<byte, CpuSet>? group in groupedByEfficiency)
		{
			List<CpuCore> cores = GroupCpuSetsByCore([.. group]);
			if (GetCpuArchitecture().Vendor == CpuVendor.Intel)
			{
				if (group.Key == 0) eCores.AddRange(cores);
				else pCores.AddRange(cores);
			}
			else
			{
				pCores.AddRange(cores);
			}
		}

		return (pCores, eCores);
	}

	public static List<CpuCoreGroup> GroupCpuSetsSequentially(CpuSetsInfo cpuSetsInfo)
	{
		var groups = new List<CpuCoreGroup>();
		var cpuSets = cpuSetsInfo.CpuSets.OrderBy(c => c.LogicalProcessorIndex).ToList();
		if (cpuSets.Count == 0) return groups;

		var currentSets = new List<CpuSet> { cpuSets[0] };
		int groupIndex = 0;
		int coreOffset = 0;

		for (int i = 1; i < cpuSets.Count; i++)
		{
			CpuSet previous = cpuSets[i - 1];
			CpuSet current = cpuSets[i];

			if (previous.EfficiencyClass != current.EfficiencyClass || previous.LastLevelCacheIndex != current.LastLevelCacheIndex || previous.NumaNodeIndex != current.NumaNodeIndex)
			{
				CpuCoreGroup group = CreateGroup(currentSets, previous, cpuSetsInfo, groupIndex++, coreOffset);
				groups.Add(group);
				coreOffset += group.Cores.Count;
				currentSets = [];
			}
			currentSets.Add(current);
		}

		if (currentSets.Count > 0)
		{
			groups.Add(CreateGroup(currentSets, currentSets[0], cpuSetsInfo, groupIndex, coreOffset));
		}

		return groups;
	}

	private static CpuCoreGroup CreateGroup(List<CpuSet> cpuSets, CpuSet sample, CpuSetsInfo info, int groupIndex, int coreOffset)
	{
		string name = "P-Cores";

		if (info.EfficiencyClass)
		{
			name = (sample.EfficiencyClass == 0) ? "E-Cores" : "P-Cores";
		}
		else if (info.LastLevelCache || info.NumaNode)
		{
			name = $"CCD {groupIndex}";
		}

		return new CpuCoreGroup
		{
			Name = name,
			Cores = GroupCpuSetsByCore(cpuSets, coreOffset)
		};
	}

	public static List<CpuCore> GroupCpuSetsByCore(List<CpuSet> cpuSets, int offset = 0)
	{
		var cores = new Dictionary<byte, CpuCore>();
		int sequentialNumber = offset;

		foreach (CpuSet? cpuSet in cpuSets.OrderBy(c => c.LogicalProcessorIndex))
		{
			if (!cores.TryGetValue(cpuSet.CoreIndex, out CpuCore? core))
			{
				core = new CpuCore
				{
					CoreIndex = cpuSet.CoreIndex,
					Name = $"Core {sequentialNumber++}"
				};
				cores[cpuSet.CoreIndex] = core;
			}

			core.Threads.Add(new CpuThread
			{
				CpuId = cpuSet.Id,
				Name = $"Thread {cpuSet.LogicalProcessorIndex}",
				BitMask = 1UL << cpuSet.LogicalProcessorIndex
			});
		}

		return [.. cores.Values];
	}

	public static class CpuSetInformationFake
	{
		private static List<CpuSet>? _fakeCpuSets;

		public static List<CpuSet>? FakeCpuSets
		{
			get => _fakeCpuSets;
			set => _fakeCpuSets = value;
		}

		// 12 cores, 24 threads
		public static void Fake5900x()
		{
			var cpuSets = new List<CpuSet>();
			byte lastCoreIndex = 0;
			int count = 24;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i
				};

				if (i % 2 != 0)
				{
					cpuSet.CoreIndex = lastCoreIndex;
				}
				else
				{
					cpuSet.CoreIndex = (byte)(i / 2);
					lastCoreIndex = cpuSet.CoreIndex;
				}

				if (i > 11)
				{
					cpuSet.LastLevelCacheIndex = 12;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 24 cores (8 P-cores + 16 E-cores), 32 threads
		public static void Fake13900()
		{
			var cpuSets = new List<CpuSet>();
			byte lastCoreIndex = 0;
			int count = 32;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i
				};

				if (i < 16 && i % 2 != 0)
				{
					cpuSet.CoreIndex = lastCoreIndex;
				}
				else
				{
					cpuSet.CoreIndex = (byte)(i < 16 ? i / 2 : i - 8);
					lastCoreIndex = cpuSet.CoreIndex;
				}

				if (i < 16)
				{
					cpuSet.EfficiencyClass = 1;
				}
				else
				{
					cpuSet.EfficiencyClass = 0;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 24 cores (8 P-cores + 16 E-cores), 24 threads
		public static void Fake13900WithoutHT()
		{
			var cpuSets = new List<CpuSet>();
			int count = 24;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i,
					CoreIndex = (byte)i
				};

				if (i < 8)
				{
					cpuSet.EfficiencyClass = 1;
				}
				else
				{
					cpuSet.EfficiencyClass = 0;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 8 cores, 8 threads
		public static void Fake8Threads()
		{
			var cpuSets = new List<CpuSet>();
			int count = 8;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i,
					CoreIndex = (byte)i
				};

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 12 cores
		public static void FakeNumaCCD12Core()
		{
			var cpuSets = new List<CpuSet>();
			int count = 12;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i,
					CoreIndex = (byte)i
				};

				if (i > 5)
				{
					cpuSet.LastLevelCacheIndex = 6;
					cpuSet.NumaNodeIndex = 6;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 12 cores with hyperthreading, 2 CCDs
		public static void Fake2CCD12CoreHT()
		{
			var cpuSets = new List<CpuSet>();
			byte lastCoreIndex = 0;
			int count = 24;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i
				};

				if (i % 2 != 0)
				{
					cpuSet.CoreIndex = lastCoreIndex;
				}
				else
				{
					cpuSet.CoreIndex = (byte)(i / 2);
					lastCoreIndex = cpuSet.CoreIndex;
				}

				if (i > 11)
				{
					cpuSet.LastLevelCacheIndex = 12;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 14 cores (6 P-cores + 8 E-cores), 20 threads
		public static void Fake13600KF()
		{
			var cpuSets = new List<CpuSet>();
			byte lastCoreIndex = 0;
			int count = 20;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i
				};

				if (i < 12 && i % 2 != 0)
				{
					cpuSet.CoreIndex = lastCoreIndex;
				}
				else
				{
					if (i < 12)
					{
						cpuSet.CoreIndex = (byte)(i / 2);
					}
					else
					{
						cpuSet.CoreIndex = (byte)(6 + (i - 12));
					}
					lastCoreIndex = cpuSet.CoreIndex;
				}

				if (i < 12)
				{
					cpuSet.EfficiencyClass = 1;
				}
				else
				{
					cpuSet.EfficiencyClass = 0;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 10 cores (6 P-cores + 4 E-cores), 10 threads
		public static void Fake225F()
		{
			var cpuSets = new List<CpuSet>();
			int count = 10;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i,
					CoreIndex = (byte)i
				};

				if (i >= 2 && i <= 5)
				{
					cpuSet.EfficiencyClass = 0;
				}
				else
				{
					cpuSet.EfficiencyClass = 1;
				}

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// 16 cores (8 cores per CCD), 32 threads
		public static void Fake9950X3D()
		{
			var cpuSets = new List<CpuSet>();
			byte lastCoreIndex = 0;
			int count = 32;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i
				};

				if (i % 2 != 0)
				{
					cpuSet.CoreIndex = lastCoreIndex;
				}
				else
				{
					cpuSet.CoreIndex = (byte)(i / 2);
					lastCoreIndex = cpuSet.CoreIndex;
				}

				cpuSet.LastLevelCacheIndex = (byte)(i < 16 ? 1 : 16);

				cpuSets.Add(cpuSet);
			}

			_fakeCpuSets = cpuSets;
		}

		// Intel i5 11400H (6 cores, 12 threads)
		public static void Fake11400()
		{
			var cpuSets = new List<CpuSet>();
			byte lastCoreIndex = 0;
			int count = 12;
			uint index = 0x100;

			for (int i = 0; i < count; i++)
			{
				var cpuSet = new CpuSet
				{
					Id = index + (uint)i,
					LogicalProcessorIndex = (byte)i
				};

				if (i % 2 != 0) cpuSet.CoreIndex = lastCoreIndex;
				else { cpuSet.CoreIndex = (byte)i; lastCoreIndex = (byte)i; }

				cpuSets.Add(cpuSet);
			}
			_fakeCpuSets = cpuSets;
		}
	}
}

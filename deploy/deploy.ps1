$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class TrustedInstaller
{
	const uint SC_MANAGER_CONNECT = 0x1;
	const uint SERVICE_QUERY_STATUS = 0x4;
	const uint SERVICE_START = 0x10;
	const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
	const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
	const uint CREATE_NO_WINDOW = 0x08000000;
	const int PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;
	const int STARTF_USESHOWWINDOW = 0x00000001;
	const short SW_HIDE = 0;

	const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
	const uint TOKEN_ADJUST_SESSIONID = 0x100;
	const uint TOKEN_QUERY = 0x8;
	const uint SE_PRIVILEGE_ENABLED = 0x2;

	[StructLayout(LayoutKind.Sequential)]
	struct LUID { public uint LowPart; public int HighPart; }

	[StructLayout(LayoutKind.Sequential)]
	struct LUID_AND_ATTRIBUTES
	{
		public LUID Luid;
		public uint Attributes;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct TOKEN_PRIVILEGES
	{
		public uint PrivilegeCount;
		public LUID_AND_ATTRIBUTES Privileges;
	}

	static readonly string[] AllPrivileges = {
		"SeAssignPrimaryTokenPrivilege","SeLockMemoryPrivilege","SeIncreaseQuotaPrivilege",
		"SeTcbPrivilege","SeSecurityPrivilege","SeTakeOwnershipPrivilege","SeLoadDriverPrivilege",
		"SeSystemProfilePrivilege","SeSystemtimePrivilege","SeProfileSingleProcessPrivilege",
		"SeIncreaseBasePriorityPrivilege","SeCreatePagefilePrivilege","SeCreatePermanentPrivilege",
		"SeBackupPrivilege","SeRestorePrivilege","SeShutdownPrivilege","SeDebugPrivilege",
		"SeAuditPrivilege","SeSystemEnvironmentPrivilege","SeChangeNotifyPrivilege","SeUndockPrivilege",
		"SeManageVolumePrivilege","SeImpersonatePrivilege","SeCreateGlobalPrivilege",
		"SeIncreaseWorkingSetPrivilege","SeTimeZonePrivilege","SeCreateSymbolicLinkPrivilege",
		"SeDelegateSessionUserImpersonatePrivilege"
	};

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool OpenProcessToken(IntPtr h, uint a, out IntPtr t);

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool LookupPrivilegeValue(string s, string n, out LUID l);

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool AdjustTokenPrivileges(
		IntPtr t, bool d, ref TOKEN_PRIVILEGES n, int l, IntPtr p, IntPtr r);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern IntPtr OpenSCManager(string m, string d, uint a);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern IntPtr OpenService(IntPtr scm, string name, uint access);

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool QueryServiceStatusEx(
		IntPtr h, int i, out SERVICE_STATUS_PROCESS s, int sz, out int b);

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool StartService(IntPtr h, int a, IntPtr v);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern IntPtr OpenProcess(uint a, bool i, int p);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool InitializeProcThreadAttributeList(
		IntPtr l, int c, int f, ref IntPtr s);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool UpdateProcThreadAttribute(
		IntPtr l, uint f, IntPtr a, IntPtr v, IntPtr s, IntPtr p, IntPtr r);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern bool CreateProcessW(
		string a, string c, IntPtr p1, IntPtr p2, bool i, uint f,
		IntPtr e, string d, ref STARTUPINFOEX s, out PROCESS_INFORMATION p);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

	const uint INFINITE = 0xFFFFFFFF;

	static void EnableAllPrivileges(IntPtr hProcess)
	{
		IntPtr hTok;
		if (!OpenProcessToken(hProcess, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY | TOKEN_ADJUST_SESSIONID, out hTok))
			return;

		foreach (string p in AllPrivileges)
		{
			LUID luid;
			if (!LookupPrivilegeValue(null, p, out luid))
				continue;

			TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
			tp.PrivilegeCount = 1;
			tp.Privileges.Luid = luid;
			tp.Privileges.Attributes = SE_PRIVILEGE_ENABLED;

			AdjustTokenPrivileges(hTok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
		}
	}

	public static void Spawn(string commandLine)
	{
		var scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
		var svc = OpenService(scm, "TrustedInstaller", SERVICE_START | SERVICE_QUERY_STATUS);

		SERVICE_STATUS_PROCESS ssp;
		int bytesNeeded;

		do
		{
			QueryServiceStatusEx(
				svc, 0, out ssp,
				Marshal.SizeOf(typeof(SERVICE_STATUS_PROCESS)),
				out bytesNeeded);

			if (ssp.dwCurrentState == 1)
				StartService(svc, 0, IntPtr.Zero);
		}
		while (ssp.dwCurrentState != 4);

		IntPtr hTI = OpenProcess(PROCESS_ALL_ACCESS, false, ssp.dwProcessId);

		IntPtr size = IntPtr.Zero;
		InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);

		IntPtr list = Marshal.AllocHGlobal(size);
		InitializeProcThreadAttributeList(list, 1, 0, ref size);

		IntPtr parent = Marshal.AllocHGlobal(IntPtr.Size);
		Marshal.WriteIntPtr(parent, hTI);

		UpdateProcThreadAttribute(
			list, 0,
			(IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
			parent, (IntPtr)IntPtr.Size,
			IntPtr.Zero, IntPtr.Zero);

		STARTUPINFOEX si = new STARTUPINFOEX();
		si.StartupInfo.cb = Marshal.SizeOf(typeof(STARTUPINFOEX));
		si.StartupInfo.dwFlags = STARTF_USESHOWWINDOW;
		si.StartupInfo.wShowWindow = SW_HIDE;
		si.lpAttributeList = list;

		PROCESS_INFORMATION pi;

		CreateProcessW(
			null,
			commandLine,
			IntPtr.Zero, IntPtr.Zero,
			false,
			EXTENDED_STARTUPINFO_PRESENT | CREATE_NO_WINDOW,
			IntPtr.Zero, null,
			ref si, out pi);

		EnableAllPrivileges(pi.hProcess);

		WaitForSingleObject(pi.hProcess, INFINITE);
	}

	struct SERVICE_STATUS_PROCESS
	{
		public int dwServiceType, dwCurrentState, dwControlsAccepted;
		public int dwWin32ExitCode, dwServiceSpecificExitCode;
		public int dwCheckPoint, dwWaitHint, dwProcessId, dwServiceFlags;
	}

	struct PROCESS_INFORMATION
	{
		public IntPtr hProcess, hThread;
		public int dwProcessId, dwThreadId;
	}

	struct STARTUPINFO
	{
		public int cb;
		public IntPtr lpReserved, lpDesktop, lpTitle;
		public int dwX, dwY, dwXSize, dwYSize;
		public int dwXCountChars, dwYCountChars;
		public int dwFillAttribute, dwFlags;
		public short wShowWindow, cbReserved2;
		public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
	}

	struct STARTUPINFOEX
	{
		public STARTUPINFO StartupInfo;
		public IntPtr lpAttributeList;
	}
}
"@

Write-Host "`n===== Prerequisites =====`n"  -ForegroundColor Yellow

$admin = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
	Write-Host "This script must be run as Administrator." -ForegroundColor Red
	return
}

if (-not [Environment]::Is64BitProcess) {
	Write-Host "This script must be run in 64-bit PowerShell." -ForegroundColor Red
	return
}

$restartRequired = $false

$services = @(
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\cdrom"; Name = "Start"; Value = 1 },
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\CompositeBus"; Name = "Start"; Value = 3 },
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\defragsvc"; Name = "Start"; Value = 3 },
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\spaceport"; Name = "Start"; Value = 0 },
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\umbus"; Name = "Start"; Value = 3 },
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\vdrvroot"; Name = "Start"; Value = 0 },
	@{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\vds"; Name = "Start"; Value = 3 }
)

foreach ($service in $services) {
	$serviceName = Split-Path $service.Path -Leaf
	if (-not (Test-Path $service.Path)) {
		Write-Host "Your OS has the $serviceName removed. Either use an existing dual boot or install a default Windows and run the script there." -ForegroundColor Red
		return
	}
	$current = (Get-ItemProperty -Path $service.Path -Name $service.Name -ErrorAction SilentlyContinue).$($service.Name)
	if ($current -ne $service.Value) {
		Write-Host "Updating $serviceName startup..."
		Set-ItemProperty -Path $service.Path -Name $service.Name -Value $service.Value -ErrorAction SilentlyContinue
		$restartRequired = $true
	}
}

$pnpDeviceNames = @(
	'Microsoft Virtual Drive Enumerator',
	'Composite Bus Enumerator',
	'UMBus Root Bus Enumerator'
)

foreach ($deviceName in $pnpDeviceNames) {
	$device = Get-PnpDevice -FriendlyName $deviceName -ErrorAction SilentlyContinue
	if ($device -and $device.Status -ne 'OK') {
		Write-Host "Enabling $deviceName..."
		$device | Enable-PnpDevice -Confirm:$false | Out-Null
		$restartRequired = $true
	}
}

if ($restartRequired) {
	Write-Host "Restart your PC and rerun this script."
	return
}

Write-Host "Please select the 25H2.iso file you downloaded in Step 2..."
Add-Type -AssemblyName System.Windows.Forms
$IsoPicker = New-Object System.Windows.Forms.OpenFileDialog
$IsoPicker.Filter = "ISO Files (*.iso)|*.iso"
$IsoPicker.Title = "Select the Windows ISO file"
$IsoPicker.Multiselect = $false
if ($IsoPicker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
	Write-Host "No ISO selected. Exiting." -ForegroundColor Red
	return
}
$fileName = [System.IO.Path]::GetFileName($IsoPicker.FileName)
if ($fileName -notin @("25H2.iso", "25H2-001.iso")) {
    Write-Host "Invalid file. Please select 25H2.iso downloaded in Step 2." -ForegroundColor Red
    return
}

Write-Host "Please select your drivers folder you created in Step 3..."
$DriverPicker = New-Object System.Windows.Forms.FolderBrowserDialog
$DriverPicker.Description = "Select the drivers folder"
if ($DriverPicker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
	Write-Host "No folder selected. Exiting." -ForegroundColor Red
	return
}
$DriversDir = $DriverPicker.SelectedPath
if ((Get-ChildItem -Path $DriversDir -Filter "*.zip" -Recurse -File).Count -gt 0) {
	Write-Host "The selected folder contains .zip files. Please extract all drivers first." -ForegroundColor Red
	return
}
if ((Get-ChildItem -Path $DriversDir -Filter "*.inf" -Recurse -File).Count -eq 0) {
	Write-Host "The selected folder does not contain any .inf files. Please extract all drivers first." -ForegroundColor Red
	return
}

$physicalDisks = Get-PhysicalDisk | Where-Object { $_.BusType -ne 'USB' -and $_.MediaType -ne 'Removable' }
if ($physicalDisks.Count -gt 1) {
	Write-Host "Getting disk information and measuring speed..."
	$diskList = $physicalDisks | ForEach-Object {
		$media = $_.MediaType
		$bus = $_.BusType

		$calculatedType =
			if ($media -eq 'SSD') { "SSD ($bus)" }
			else { "HDD ($bus)" }

		$disk = Get-Disk | Where-Object FriendlyName -eq $_.FriendlyName | Select-Object -First 1

		$partitions = if ($disk) {
			Get-Partition -DiskNumber $disk.Number -ErrorAction SilentlyContinue
		}

		$volumes = $partitions | Get-Volume -ErrorAction SilentlyContinue
		$freeBytes = ($volumes.SizeRemaining | Measure-Object -Sum).Sum

		$driveLetter = ($partitions | Where-Object DriveLetter).DriveLetter | Select-Object -First 1

		$readSpeed = "N/A"
		$writeSpeed = "N/A"

		if ($driveLetter) {
			$readOutput = winsat disk -drive $driveLetter -seq -read 2>$null
			$readMatch = $readOutput | Select-String 'Sequential 64\.0 Read\s+([\d\.]+)\s+MB/s'

			if ($readMatch) {
				$readSpeed = "$($readMatch.Matches[0].Groups[1].Value) MB/s"
			}

			$writeOutput = winsat disk -drive $driveLetter -seq -write 2>$null
			$writeMatch = $writeOutput | Select-String 'Sequential 64\.0 Write\s+([\d\.]+)\s+MB/s'

			if ($writeMatch) {
				$writeSpeed = "$($writeMatch.Matches[0].Groups[1].Value) MB/s"
			}
		}

		[PSCustomObject]@{
			Disk  = if ($disk) { $disk.Number } else { $_.DeviceId }
			Name  = $_.FriendlyName
			Type  = $calculatedType
			Size  = "$([Math]::Round($_.Size / 1GB, 2)) GB"
			Free  = "$([Math]::Round($freeBytes / 1GB, 2)) GB"
			Read  = $readSpeed
			Write = $writeSpeed
		}
	}
	$diskList | Sort-Object Disk | Format-Table -AutoSize

	$recommendedDisk = $diskList | ForEach-Object {
		$speedVal = 0.0
		if ($_.Read -match '([\d\.]+)') { $speedVal = [double]$Matches[1] }
		[PSCustomObject]@{
			Disk     = $_.Disk
			SpeedVal = $speedVal
		}
	} | Sort-Object SpeedVal -Descending | Select-Object -First 1

	Write-Host "Enter the disk number that will be checked for available space for AutoOS (Recommendation: $($recommendedDisk.Disk)): " -NoNewline
	$validDisks = $diskList.Disk | ForEach-Object { [string]$_ }
	do {
		$key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
		$selectedDisk = $key.Character
	} while ($selectedDisk -notin $validDisks)
	Write-Host $selectedDisk
	$DiskNumber = [int][string]$selectedDisk
} else {
	$DiskNumber = (Get-Partition -DriveLetter C | Get-Disk).Number
}

Write-Host "`n===== Step 1: Check Partition Style =====`n" -ForegroundColor Yellow
if ((Get-Disk -Number $DiskNumber).PartitionStyle -eq 'MBR') {
	Write-Host "Partition style is MBR"
	
	$windows = $false
	$partitions = Get-Partition -DiskNumber $DiskNumber -ErrorAction SilentlyContinue
	foreach ($partition in $partitions) {
		if ($partition.DriveLetter) {
			$drivePath = "$($partition.DriveLetter):\"
			if (Test-Path "$drivePath\Windows") {
				$windows = $true
				break
			}
		}
	}
	
	if ($windows) {
		Write-Host "Converting disk to GPT..."
		mbr2gpt /validate /disk:$DiskNumber /allowFullOS
		mbr2gpt /convert /disk:$DiskNumber /allowFullOS
		Write-Host "Please set Boot Mode to UEFI in BIOS, then rerun this script." -ForegroundColor Yellow
		return
	} else {
		Write-Host "Please follow this guide to convert the disk to GPT manually:" -ForegroundColor Red
		Write-Host "https://www.diskgenius.com/resource/change-disk-format-from-mbr-to-gpt.html"
		Write-Host "After converting the disk to GPT, rerun this script." -ForegroundColor Yellow
		return
	}
} else {
	Write-Host "Partition style is GPT"
}

Write-Host "`n===== Step 2: Check BitLocker State =====`n" -ForegroundColor Yellow
try {
	if ((Get-BitLockerVolume -MountPoint C:).VolumeStatus -ne "FullyDecrypted") {
		Write-Host "BitLocker is enabled"
		Clear-BitLockerAutoUnlock -ErrorAction SilentlyContinue | Out-Null
		Disable-BitLocker -MountPoint "C:" | Out-Null

		while ($true) {
			$status = Get-BitLockerVolume -MountPoint "C:"
			
			if ($status.VolumeStatus -eq "FullyDecrypted") {
				Write-Host "`r                                              " -NoNewline
				Write-Host "`rBitLocker is fully disabled."
				break
			}
			
			$progressPercentage = 100 - $status.EncryptionPercentage
			Write-Host "`rDisabling BitLocker ($progressPercentage%)..." -NoNewline
			
			Start-Sleep -Seconds 5
		}
		Write-Host "BitLocker is disabled"
	} else {
		Write-Host "BitLocker is disabled"
	}
} catch {
	Write-Host "BitLocker is disabled"
}

Write-Host "`n===== Step 3: Check Partitions =====`n" -ForegroundColor Yellow
$Partitions = Get-Partition -DiskNumber $DiskNumber | Where-Object { $_.Type -eq 'Basic' -and $_.Size -gt 0 }
$ShrinkTargetsMB = @(1048576, 524288, 262144, 131072, 65536)

$ShrinkablePartition = $null
$ShrinkAmountMB = 0
$ExistingPartition = $null
$TargetDrive = $null

$ExistingPartitions = @()

foreach ($p in $Partitions) {
	try {
		$items = @(Get-ChildItem "$($p.DriveLetter):\" -Force)
		$userItems = $items | Where-Object { $_.Name -notin @('System Volume Information', '$RECYCLE.BIN', 'desktop.ini') }
		if ($userItems.Count -eq 0) {
			$ExistingPartitions += $p
		}
	} catch { }
}

if ($ExistingPartitions.Count -gt 0) {
	$ExistingPartition = $ExistingPartitions | Sort-Object { (Get-Volume -DriveLetter $_.DriveLetter).SizeRemaining } -Descending | Select-Object -First 1
	$TargetDrive = $ExistingPartition.DriveLetter + ":"
	$FreeGB = [math]::Round((Get-Volume -DriveLetter $ExistingPartition.DriveLetter).SizeRemaining / 1GB, 2)
	Write-Host "Using empty partition $TargetDrive with $FreeGB GB free"
}
else {
	foreach ($Partition in $Partitions) {
		if (-not $Partition.DriveLetter) { continue }
		try {
			$Supported = Get-PartitionSupportedSize -DriveLetter $Partition.DriveLetter
		} catch {
			Write-Host "Failed to query supported partition sizes.`nDownload Minitool Partition Wizard from here (https://cdn2.minitool.com/?p=pw&e=pw-free-offline) and use the 'Split' function with at least 64GB then rerun this script." -ForegroundColor Red
			if ($Host.Name -eq 'ConsoleHost') {
				[void][System.Console]::ReadLine()
			}
			return
		}
		$MaxShrinkMB = [math]::Floor(($Partition.Size - $Supported.SizeMin) / 1MB)
		Write-Host "Partition $($Partition.DriveLetter): $MaxShrinkMB MB shrinkable"
		$Partition | Add-Member -NotePropertyName MaxShrinkMB -NotePropertyValue $MaxShrinkMB
	}

	foreach ($Partition in $Partitions) {
		foreach ($Target in $ShrinkTargetsMB) {
			if ($Partition.MaxShrinkMB -ge $Target) {
				$ShrinkablePartition = $Partition
				$ShrinkAmountMB = $Target
				break
			}
		}
		if ($ShrinkablePartition) { break }
	}

	if ($ShrinkablePartition) {
		Write-Host "Shrinking partition $($ShrinkablePartition.DriveLetter): by $ShrinkAmountMB MB..."
		$NewSize = $ShrinkablePartition.Size - ($ShrinkAmountMB * 1MB)
		Resize-Partition -DriveLetter $ShrinkablePartition.DriveLetter -Size $NewSize
		$NewPartition = New-Partition -DiskNumber $DiskNumber -UseMaximumSize -AssignDriveLetter
		$TargetDrive  = "$($NewPartition.DriveLetter):"
	}
	else {
		Write-Host "No partition with at least 64GB of free space or shrinkable space found.`nDownload Minitool Partition Wizard from here (https://cdn2.minitool.com/?p=pw&e=pw-free-offline) and use the 'Split' function with at least 64GB then rerun this script." -ForegroundColor Red
		Write-Host "Press Enter to exit..."
		if ($Host.Name -eq 'ConsoleHost') {
			[void][System.Console]::ReadLine()
		}
		return
	}
}

Write-Host "`n===== Step 4: Prepare Target Partition =====`n" -ForegroundColor Yellow
Write-Host "Formatting partition $TargetDrive..."
Start-Process -FilePath "cmd.exe" -ArgumentList "/c ""format $TargetDrive /fs:ntfs /q /y /v:AutoOS > nul 2> nul""" -NoNewWindow -Wait

Write-Host "`n===== Step 5: Apply Windows Image =====`n" -ForegroundColor Yellow
try {
	Write-Host "Mounting ISO..." 
	$MountedIso = (Mount-DiskImage -ImagePath $IsoPicker.FileName -PassThru | Get-Volume).DriveLetter + ":"
	
	$WimPath = Join-Path $MountedIso "sources\install.wim"
	$Images = Get-WindowsImage -ImagePath $WimPath
	if ($Images.Count -ne 1 -or $Images[0].ImageName -notmatch "Pro") {
		Write-Host "The selected ISO is not supported, please use the ISO linked in Step 2." -ForegroundColor Red
		return
	}

	Write-Host "Copying install.wim..."
	$TempWim = "$env:TEMP\install.wim"
	Copy-Item -Path $WimPath -Destination $TempWim -Force
	& "$env:SystemRoot\System32\attrib.exe" -r $TempWim
} finally {
	Write-Host "Unmounting ISO..."
	Dismount-DiskImage -ImagePath $IsoPicker.FileName | Out-Null
}

Write-Host "Mounting install.wim..."
$MountDirectory = "C:\mnt"
New-Item -Path $MountDirectory -ItemType Directory -Force | Out-Null

$Images = Get-WindowsImage -ImagePath $TempWim
foreach ($Image in $Images) {
	try {
		Mount-WindowsImage -Path $MountDirectory -ImagePath $TempWim -Name $Image.ImageName -ErrorAction Stop | Out-Null
		Write-Host "Stripping 8.3 filenames..."
		[TrustedInstaller]::Spawn(
			"cmd /c fsutil 8dot3name strip /f /s `"$MountDirectory`""
		)
		reg load HKLM\Mount "$MountDirectory\Windows\System32\config\SYSTEM" | Out-Null
		Set-ItemProperty -Path "HKLM:\Mount\ControlSet001\Control\FileSystem" -Name "NtfsDisable8dot3NameCreation" -Value 1 -Type DWord -Force
		reg unload HKLM\Mount | Out-Null
	} finally {
		Write-Host "Unmounting install.wim..."
		try {
			Dismount-WindowsImage -Path $MountDirectory -Save | Out-Null
		} catch {
			Dismount-WindowsImage -Path $MountDirectory -Discard | Out-Null
		}
		[TrustedInstaller]::Spawn(
			"cmd /c rmdir /s /q `"$MountDirectory`""
		)
	}
}

Write-Host "Applying Windows image to $TargetDrive..."
DISM /Apply-Image /ImageFile:$TempWim /Index:1 /ApplyDir:$TargetDrive
Remove-Item $TempWim -Force

Write-Host "`n===== Step 6: Install Drivers =====`n" -ForegroundColor Yellow
Write-Host "Installing drivers from $DriversDir..."
DISM /Image:$TargetDrive /Add-Driver /Driver:$DriversDir /Recurse

Write-Host "`n===== Step 7: Add unattend.xml =====`n" -ForegroundColor Yellow
Write-Host "Adding unattend.xml..."
New-Item -ItemType Directory -Path $TargetDrive\Windows\Panther -Force | Out-Null
try { Add-MpPreference -ExclusionPath "$TargetDrive\Windows\Panther" | Out-Null } catch {	}
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/unattend.xml" -OutFile $TargetDrive\Windows\Panther\unattend.xml

Write-Host "`n===== Step 8: Create Boot Entry =====`n" -ForegroundColor Yellow
Write-Host "Creating boot entry..."
bcdedit /set "{current}" bootmenupolicy legacy
bcdboot $TargetDrive\Windows
bcdedit /set "{default}" description "AutoOS"
bcdedit /set "{default}" bootmenupolicy legacy
bcdedit /timeout 6
Write-Host "`n===== AutoOS Deployment Completed Successfully! =====" -ForegroundColor Green
Write-Host "Open the installation guide on your phone and continue with Step 5." -ForegroundColor Yellow

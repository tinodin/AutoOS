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

$admin = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "This script must be run as Administrator."
    return
}

if (-not [Environment]::Is64BitProcess) {
    Write-Host "This script must be run in 64-bit PowerShell."
    return
}

$restartRequired = $false

$services = @(
    @{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\cdrom"; Name = "Start"; Value = 1 },
    @{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\spaceport"; Name = "Start"; Value = 0 },
    @{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\vdrvroot"; Name = "Start"; Value = 0 },
    @{ Path = "HKLM:\SYSTEM\CurrentControlSet\Services\vds"; Name = "Start"; Value = 3 }
)

foreach ($service in $services) {
    if (-not (Test-Path $service.Path)) {
        Write-Host "Your OS has the $serviceName removed. Either use an existing dual boot or install a default Windows and run the script there."
        return
    }
    $serviceName = Split-Path $service.Path -Leaf
    $current = (Get-ItemProperty -Path $service.Path -Name $service.Name -ErrorAction SilentlyContinue).$($service.Name)
    if ($current -ne $service.Value) {
        Write-Host "Updating $serviceName startup..."
        Set-ItemProperty -Path $service.Path -Name $service.Name -Value $service.Value -ErrorAction SilentlyContinue
        $restartRequired = $true
    }
}

$virtualDriveEnumerator = Get-PnpDevice -FriendlyName 'Microsoft Virtual Drive Enumerator' -ErrorAction SilentlyContinue
if ($virtualDriveEnumerator -and $virtualDriveEnumerator.Status -ne 'OK') {
    Write-Host "Enabling Microsoft Virtual Drive Enumerator..."
    $virtualDriveEnumerator | Enable-PnpDevice -Confirm:$false | Out-Null
    $restartRequired = $true
}

if ($restartRequired) {
    Write-Host "Restart your PC and rerun this script."
    return
}

Write-Host "Please select the Windows ISO..."
$IsoPicker = New-Object System.Windows.Forms.OpenFileDialog
$IsoPicker.Filter = "ISO Files (*.iso)|*.iso"
$IsoPicker.Title = "Select the Windows ISO file"
$IsoPicker.Multiselect = $false
if ($IsoPicker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
    Write-Host "No ISO selected. Exiting."
    return
}

Write-Host "Do you want to install drivers? (y/n) " -NoNewline
do {
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    $InstallDrivers = $key.Character
} while ($InstallDrivers -notmatch '^[YyNn]$')
Write-Host $InstallDrivers

if ($InstallDrivers -match '^[Yy]') {
    Write-Host "Please select your drivers folder..."
    $DriverPicker = New-Object System.Windows.Forms.FolderBrowserDialog
    $DriverPicker.Description = "Select the drivers folder"
    if ($DriverPicker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return }
    $DriversDir = $DriverPicker.SelectedPath
}

Write-Host "`n===== Step 1: Check Partition Style =====`n"
$DiskNumber = (Get-Partition -DriveLetter C | Get-Disk).Number
if ((Get-Partition -DriveLetter "C" | Get-Disk).PartitionStyle -eq 'MBR') {
    Write-Host "Partition style is MBR. Converting to GPT..."
    mbr2gpt /convert /disk:$DiskNumber /allowFullOS
    Write-Host "Please set Boot Mode to UEFI in BIOS after conversion, then rerun this script."
    return
} else {
    Write-Host "Partition style is GPT"
}

Write-Host "`n===== Step 2: Check BitLocker State =====`n"
try {
    if ((Get-BitLockerVolume -MountPoint C:).VolumeStatus -ne "FullyDecrypted") {
        Write-Host "BitLocker is enabled. Disabling..."
        Disable-BitLocker -MountPoint C:
        Write-Host "Wait until decryption finishes, then rerun this script."
        return
    } else {
        Write-Host "BitLocker is disabled"
    }
} catch {
    Write-Host "BitLocker is disabled"
}

Write-Host "`n===== Step 3: Check Partitions =====`n"
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
        $Supported = Get-PartitionSupportedSize -DriveLetter $Partition.DriveLetter
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
        Write-Host "No partition with at least 64GB of free space or shrinkable space found. Use the 'Split' function in Minitool Partition Wizard Free and rerun this script."
        Write-Host "Press Enter to exit..."
        if ($Host.Name -eq 'ConsoleHost') {
            [void][System.Console]::ReadLine()
        }
        return
    }
}

Write-Host "`n===== Step 4: Prepare Target Partition =====`n"
Write-Host "Formatting partition $TargetDrive..."
Start-Process -FilePath "cmd.exe" -ArgumentList "/c ""format $TargetDrive /fs:ntfs /q /y /v:AutoOS > nul 2> nul""" -NoNewWindow -Wait

Write-Host "`n===== Step 5: Apply Windows Image =====`n"
try {
    Write-Host "Mounting ISO..." 
    $MountedIso = (Mount-DiskImage -ImagePath $IsoPicker.FileName -PassThru | Get-Volume).DriveLetter + ":"
    
    $WimPath = Join-Path $MountedIso "sources\install.wim"
    $Images = Get-WindowsImage -ImagePath $WimPath
    if ($Images.Count -ne 1 -or $Images[0].ImageName -notmatch "Pro") {
        throw "The selected ISO is not supported, please use the ISO linked in Step 2."
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

Write-Host "`n===== Step 6: Install Drivers =====`n"
if ($InstallDrivers -match '^[Yy]') {
    Write-Host "Installing drivers from $DriversDir..."
    DISM /Image:$TargetDrive /Add-Driver /Driver:$DriversDir /Recurse
}
else {
    Write-Host "Skipping driver installation..."
}

Write-Host "`n===== Step 7: Add unattend.xml =====`n"
Write-Host "Adding unattend.xml..."
New-Item -ItemType Directory -Path $TargetDrive\Windows\Panther -Force | Out-Null
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/unattend.xml" -OutFile $TargetDrive\Windows\Panther\unattend.xml

Write-Host "`n===== Step 8: Create Boot Entry =====`n"
Write-Host "Creating boot entry..."
bcdedit /set "{current}" bootmenupolicy legacy
bcdboot $TargetDrive\Windows
bcdedit /set "{default}" description "AutoOS"
bcdedit /set "{default}" bootmenupolicy legacy
bcdedit /timeout 6
Write-Host "`n===== AutoOS Deployment Completed Successfully! ====="
Write-Host "Open the installation guide on your phone and continue with Step 4."
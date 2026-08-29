# 🚀 Installation Guide

> [!NOTE]  
> This guide does **NOT** require a USB drive.<br/>
> AutoOS will automatically be installed as a **dual boot**, meaning your **old data** and Windows installation **stay accessible** after installing.
> Don't do anything the guide does not tell you to do (e.g. stuff related to the ISO, drivers or partitions).

### Step 1: Join Discord Server
Join my [Discord Server](https://discord.gg/bZU4dMMWpg) to receive **installation support** and stay informed about **future updates or changes**.

### Step 2: Downloading the ISO
Download the latest Windows `25H2.iso` file from [here](https://drive.google.com/drive/folders/1e4dwuWYwu2i-NTPunT_zFRZYBG5Cgnpa?usp=sharing) (Log into your Google Account if you get an error).<br/>
Other ISOs are not supported (will not work) to guarantee consistency and the latest features.

### Step 3: Downloading Drivers
Open **Network Connections** (`ncpa.cpl`) and look for the brand of your Ethernet, Wi-Fi and Bluetooth adapters (Usually same brand as Wi-Fi).<br/>
If you have one of the following, download the drivers from the links below.<br/>

**INTEL:** [Ethernet](https://www.intel.com/content/www/us/en/download/727998/intel-network-adapter-driver-for-microsoft-windows-11.html) · [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/Bluetooth.zip)

**Realtek:** [Ethernet](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Realtek/Ethernet.zip) · [Wi-Fi and Bluetooth](https://www.realtek.com/Download/Index?cate_id=203&menu_id=297)

**MediaTek:** [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/MediaTek/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/MediaTek/Bluetooth.zip)

**Qualcomm:** [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Qualcomm/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Qualcomm/Bluetooth.zip)

**Killer:** [Ethernet, Wi-Fi and Bluetooth](https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/Killer.zip)

**Marvell:** [Ethernet](https://www.marvell.com/content/dam/marvell/en/drivers/Marvell_AQtion_Win_v3.1.11_10-17-2025.zip)

---

<details>
<summary>If you have a <b>Laptop</b>, click to expand</summary>
<br/>

Open **System Information** (`msinfo32`) and look for **System Model**.<br/>
Search for "**{System Model} drivers**" in your browser.<br/>
Click on the support page, head to drivers and select Windows 11 (if available), then download the **Audio and Touchpad** drivers (if available).
<br/>
</details>

---

<details>
<summary>If you have an Intel <b>10th Generation or newer Prebuilt PC</b> or <b>Laptop</b>, click to expand</summary>
<br/>

Open Device Manager (`devmgmt.msc`) and look for anything containing "**Intel**" under the `Storage controllers` section.<br/>
If there are none, continue with Step 4.<br/>

If there is an "**Intel**" entry, you have **2 options**:

**Option 1 (Preferred):**
- Disable `VMD Controller` in your BIOS.
- On DELL/Alienware go to `Storage -> SATA/NVMe Operation` and change it from `Disabled` to `AHCI/NVMe`.

**Option 2:**
- Download the Intel® Rapid Storage Technology Driver for your CPU Generation from below:
  - [10th and 11th](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/10th%20and%2011th/RST.zip)
  - [12th to 15th](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/12th%20to%2015th/RST.zip)
  - [Intel® Core™ Ultra Series 3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/Intel%C2%AE%20Core%E2%84%A2%20Ultra%20Series%203/RST.zip)

**Notes:**
- **Option 1** will result in **better disk speeds**.
- If you get a **BSOD** on your **old Windows** after **disabling VMD Controller**, boot into **safe mode** once, then restart.
- If you keep `VMD Controller` **enabled** and **didn't download the RST driver**, you will get `Inaccessible boot device` **BSOD**.
</details>

---

After downloading your drivers, extract all `.zip` files.<br/>
For `.exe` files, run them and select the `Extract` option (if available).<br/>
If they don't have that option, use `7-Zip, NanaZip, or WinRAR` to extract them.

Finally, create a **New Folder** and move all extracted folders into it.

### Step 4: Run the deployment script
Open PowerShell **as Administrator**.<br/>
Paste this into the PowerShell window to run the deployment script.<br/>
This will automatically handle everything related to the ISO, the drivers and your partitions.<br/>

```ps1
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
irm https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/deploy.ps1 | iex
```
Select the `25H2.iso` you downloaded in Step 2 and your **drivers folder** you created in Step 3.<br/>
If you get any errors during the script, please leave a message on my [Discord Server](https://discord.gg/bZU4dMMWpg).

### Step 5: Restarting into AutoOS
Once the script has successfully finished, restart your PC.<br/>
Boot into `AutoOS` by pressing `Enter` after every restart.<br/>

> [!WARNING]
> Make sure to **keep your ethernet cable connected** or **connect to your WiFi in the setup**.<br/>
> **DO NOT BYPASS THE NETWORK REQUIREMENT!**

### Step 6: AutoOS Installer
Once the OOBE has finished, wait for Windows to restart and the `AutoOS Installer` to open up.<br/>

> [!IMPORTANT]
> Click on the `AutoOS User Guide` tile on the Home tab and follow the **instructions** for the `AutoOS Installer`.
# 📘 AutoOS Settings User Guide

<details>
<summary>What <b>NOT</b> to do on AutoOS, click to expand</summary>
<br/>

- Run any other `tweaks` or `optimizers` for obvious reasons.
- Use `timer resolution` or `memory cleaners` because they do more harm than good.
- Use `external frame rate limiters` like `NVCP` or `RTSS` because they trade **better 1% lows** for **added latency** (unless non-competitive).
- Set `visual effects` to `Best Performance`.
- Disable `animations`, `transparency` or `paging file`.
- **Uninstall** `MSI Afterburner, OBS, Everything, Windhawk, StartAllBack` or any of the `runtimes`.
- **Install** `7-Zip` or `WinRAR` because `NanaZip` is already installed.
- **Uninstall** more AppX Packages like `Xbox Game Bar` or `Microsoft Edge`.

</details>

---

<details>
<summary>If you want to merge the space of your old Windows partition with the AutoOS partition, click to expand</summary>
<br/>

**Step 1:**
- Copy your Games to the same path but on the AutoOS partition.
- Open all the manifest files from the game launchers, then use `Ctrl + H` to replace the drive letters, click **Replace All** and save the file.
  - Epic Games
    - `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat`
    - `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests`
    - `C:\ProgramData\Epic\EpicOnlineServicesShared\InstallHelper\InstalledItems`
  - Riot Games
    - `C:\ProgramData\Riot Games\Metadata\<your game>\<your game>.live.product_settings.yaml`
  - Steam
    - `C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf`

**Step 2:**
- Install `Minitool Partition Wizard` from the `Applications` tab.
- Right click on the **old Windows partition** and select **Delete**.
- Right click on the **AutoOS partition** and select **Extend**, select the **old Windows partition** and **max out the slider**.
- Click **Apply** and then **Restart Now**.

  ---

  <details>
  <summary>If you are on an <b>ASUS Motherboard</b> and get <b>GPT header corruption has been detected</b> message, click to expand</summary>
  <br/>

  - Press `F1` to get into `BIOS`.
  - Press `F7` to get into `advanced mode`.
  - Go to `Boot` tab, then select `Boot Configuration`.
  - Change `Next Boot Recovery Action` to `Recovery` (if available).
  - Change `Boot Sector (MBR/GPT) Recovery Policy` to `Auto Recovery` (if available).

  </details>

  ---

  <details>
  <summary>If the <b>old Windows entry</b> is still <b>showing up</b> on boot, click to expand</summary>
  <br/>

  - Open Command Prompt and paste:
  ```
  bcdedit /enum
  ```

  - Find the entry of your old Windows partition, copy its `identifier` value and then run:

  ```
  bcdedit /delete {identifier}
  ```
  </details>
</details>

---

### Home
Quick access for links:
- Open AutoOS User Guide
- Open AutoOS Contribution Guide
- Join AutoOS Discord Server
- Donate

![Home](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Home.png)

### Sound
Adjust Volume, Format and Buffer Size of your current input and output device:
- If your audio output device supports a lower `buffer size`, you can lower it in exchange for **higher CPU usage**.

![Sound](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Sound.png)

### Displays
Manually adjust or import a Custom Resolution Utility (CRU) profile:

  <details>
  <summary>For <b>NVIDIA</b> GPUs, click to expand</summary>
  <br/>

  - Open NVIDIA Control Panel, go to `Display` -> `Change resolution` and select your desired `monitor`.
  - Make sure your desired resolution and refresh rate is active.
  - Click on `Customize...` -> `Create Custom Resolution`, and click `Accept` on the disclaimer.
  - Change the `Timing` -> `Standard` to `Manual`.
  
    **Testing Loop:**
    Whenever you change a value below, click `Test` and follow these rules:
    - **If the display works normally:** Click `No` and continue adjusting.
    - **If you see glitches/flickering:** Click `No`. Use smaller steps to find the exact working limit.
    - **If you get a Black Screen / No Signal:** Press `Enter` to cancel. Use smaller steps to find the exact working limit.
    
    **Phase 1: Maximize Vertical Total Pixels**
    - **Increase** the **Vertical** `Total pixels` in increments of 20.
    - Click `Test` after each change using the rules above until you find the maximum working value.

    **Phase 2: Minimize Horizontal Total Pixels**
    - Take note of the default value.
    - **Decrease** the **Horizontal** `Total pixels` in increments of 20.
    - Click `Test` after each change using the rules above until you find the minimum working value.

    **Phase 3: Final Vertical Total Pixels**
    - **Increase** the **Vertical** `Total pixels` again, in increments of 1.
    - Click `Test` after each change using the rules above until you find the absolute maximum working value.
    - If you were not able to increase it further than what you had from **Phase 1**, set **Horizontal** `Total pixels` back to the default value and **skip Phase 4**.

    **Phase 4: Final Horizontal Total Pixels**
    - **Increase** the **Horizontal** `Total pixels` again, in increments of 10.
    - Click `Test` after each change using the rules above until you find the absolute maximum working value.

    **Save changes using Custom Resolution Utility (CRU):**
    - Click `Launch` under `Create a custom resolution` in AutoOS.
    - Click on the entry under `Extension blocks`, then click on `Edit` below.
    - Change the `Type` to `DisplayID 2.0`, if it has no data, change it back to `CTA-861`.
    - Click on the resolution entry with the same `refresh rate` you modified in NVIDIA Control Panel under `Detailed resolutions`, then click `Edit` below.
    - Enter all values from NVIDIA Control Panel under the corresponding fields in CRU.
      - Front porch (pixels) -> Front porch
      - Sync width (pixels) -> Sync width
      - In Custom Resolution Utility (CRU), click on `Total` and enter your values.
      - Polarity -> Sync polarity
      - Refresh rate -> Refresh rate
    - If the `Actual` refresh rate doesn't end in `.000 Hz`, increase or decrease the Horizontal Total pixels by 1 until it ends with `.000 Hz`.
    - Click `OK` and then `Export`, change Save as type to `EXE File (*.exe)` and save it an external location like a USB or HDD.
    - Click `OK` to exit Custom Resolution Utility (CRU).
    - Click on `Restart` under `Restart the graphics driver` in AutoOS.

    **Future usage:**
    - Click `Import profile` in AutoOS and select the `.exe` file you exported to instantly reapply the custom resolution.

  </details>

![Displays](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Displays.png)

### Graphics Cards
Select your Graphics Card preferences:
- Click `Update` if available, which updates your GPU driver while keeping your current settings.
- Enable `High-Bandwidth Digital Content Protection (HDCP)` if you watch **DRM protected content** like `Netflix` etc.
- Disable `High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio` if you don't use headphones or speakers connected to your monitor or audio receiver.
- Manually adjust or import an `MSI Afterburner Profile`.
- Enable the `OBS Studio` toggle if you want to have `clips` (`30sec`, `Alt + F10`).

![Graphics Cards](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Graphics%20Cards.png)
![Graphics Cards2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Graphics%20Cards2.png)

### Per-CPU Scheduling
Manually adjust or automatically optimize Audio, GPU, XHCI and NIC Affinities:
- Click `Optimize Affinities` to reapply Affinities to all devices after **manual driver reinstalls** or after toggling `Hyper-Threading` / `SMT`.

![Per-CPU Scheduling](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Per-CPU%20Scheduling.png)
![Per-CPU Scheduling2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Per-CPU%20Scheduling2.png)

### Bluetooth & Devices
Toggle Bluetooth Services & Drivers and XHCI Interrupt Moderation (IMOD) per controller:
- Keep XHCI Interrupt Moderation (IMOD) disabled for all USB controllers.

![Bluetooth & Devices](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Bluetooth%20&%20Devices.png)

### Network & Internet
Manually adjust or automatically optimize advanced network adapter settings:
- Click `Optimize Adapter` to reapply settings after **driver reinstalls**.

![Network & Internet](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Network%20&%20Internet.png)

### Energy & Power
Adjust, Edit, Duplicate, Delete, Restore, Export, Import Power plans and compare them:
- Keep using the AutoOS Power Plan.
- If you have issues with the AutoOS Power Plan, leave a message on the [Discord Server](https://discord.gg/bZU4dMMWpg).
- Select another power plan in the 2nd combobox to **compare** it against the active power plan.
- Right click on the active power plan combobox to `Edit, Duplicate, Delete or Export` it.

![Energy & Power](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Energy%20&%20Power.png)
![Energy & Power2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Energy%20&%20Power2.png)

### Services & Drivers
Toggle Services & Drivers states with configured functionality:
- **Enable** the `WiFi Support` checkbox if you are using **WiFi while Gaming**.
- **Enable** the `Bluetooth Support` checkbox if you are using **Bluetooth while Gaming**.
- **Disable** the toggle at the top and restart whenever you are **Gaming** competitively.
- **Enable** it again and restart if you need `functionality` for **Work** or installing applications / drivers.

![Services & Drivers](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Services%20&%20Drivers.png)

### BIOS Settings
Manually adjust or merge recommended BIOS Settings:
- In the toolbar, click `Merge` and `Import to NVRAM`, then restart your PC.

  **If no internet:**
  - Enable the **toggle** in the `Services & Drivers` tab and restart your PC.
  - Click `Optimize` in the `Per-CPU Scheduling` and in the `Network & Internet` tab.

  **If not booting:**
  - Reset CMOS using the **button** on your **motherboard** (if yours has one) or by **removing the CMOS battery** for 5 minutes.
  - After that, **lower** the **Merge Count** until you **find the setting** that causes your PC to not boot.
  - Once you find the setting, please leave a message on the [Discord Server](https://discord.gg/bZU4dMMWpg).

  **If crashing, freezing or worse performance:**
  - Option 1 (Easier): Click `Restore` and select the oldest or previous NVRAM backup.
  - Option 2 (Harder):
    - **Intel:**
      - Lower `Max Turbo Ratios`
      - Enable `Hyper-Threading`
      - Disable `E-Cores` (Active E-Cores, Active Efficient Cores, Active Efficient-cores, No. of CPU E-Cores Enabled)
    - **AMD:**
      - Lower `All Core Curve Optimizer Magnitude`

![BIOS Settings](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/BIOS%20Settings.png)

### Disk Cleanup
Clean up your drives:
- Click `Clean up disks` to run disk cleanup to free up some space.

![Disk Cleanup](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Disk%20Cleanup.png)

### Windows Security
Toggle Windows Security Options:
- Enable `HVCI` and `VBS` if you play `FACEIT` or `Valorant`.

![Windows Security](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Windows%20Security.png)

### Windows Update
Toggle Windows Updates and set target version:
- Enable `Windows Updates` to get the **latest features** and **security updates**.
- All **optimizations** will be **kept**.
- **Drivers** are **excluded by default**.

![Windows Update](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Windows%20Update.png)

### Browsers
Post-install your Browsers and Browser Extensions:
- Use `Thorium` or `Helium` over `Chrome`.
- Use `Zen` for best **productivity** and **design**.

![Browsers](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Browsers.png)

### Applications
Post-install your Applications:
- Select `Discord`, which comes `preconfigured` with `Vencord`, `OpenAsar` and `all settings`.
- Use `Logitech Onboard Memory Manager` over `Logitech G HUB` if you have a **Logitech Mouse**.

![Applications](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications.png)
![Applications2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications2.png)
![Applications3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications3.png)
![Applications4](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications4.png)

### Benchmarks
Analyze your Benchmarks:
- Add existing Benchmarks by clicking `Add`.
- Select a process, adjust delay, duration and hotkey. Then click `Record`.
- Go to `Analysis` tab to view charts.
  - Choose between **Bar, Column, Line, Scatter** and **Pie** charts.
  - Toggle **Statistics** and switch **Metrics**. 
  - Adjust **Low Fps** and **Stuttering factor**.
  - Adjust **Colors**
  - Toggle visibility by clicking on the **Legend items**.
- Click `Statistics` tab to view stats.
  - Change to **Baseline mode**.
  - Switch between **absolute** and **relative delta**.
  - Adjust **Low Fps** and **Stuttering factor**.

![Benchmarks](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Benchmarks.png)
![Benchmarks2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Benchmarks2.png)
![Benchmarks3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Benchmarks3.png)
![Benchmarks4](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Benchmarks4.png)
![Benchmarks5](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Benchmarks5.png)
![Benchmarks6](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Benchmarks6.png)

### Games
View your Game Library (Supports Epic Games, Riot Games, EA, Ubisoft Connect, Steam, Eden, Citron and Ryujinx):
- Switch between Epic Games and Steam Accounts on the top right.
- Press the `Play` button to launch any Game.

  **If Services & Drivers toggle disabled:**
  - Once you are in the `Game`, press the `Stop Processes` button.
  - Use `Alt+Tab` to switch between apps in this state.
  - Press the `Restart Processes` button to restore the taskbar etc.

  **Adding Games:**
  - For `Riot Games` titles to show up in the `Games` tab, install them through the `Epic Games Launcher` as well.
  - For `EA` or `Ubisoft Connect` titles to show up in the `Games` tab, add them to your `Epic Games Launcher` library.
  - To add custom games, add the game as a `non-steam game` in `Steam`.
  - The name has to be the same as found on [IGDB](https://www.igdb.com/).

  **Notes:**
  - Cap your Game's `frame rate limit` to `a multiple` of your monitor's `refresh rate` (eg. 144hz -> 72/144/288fps).

![Games](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Games.png)

### Settings
Configure AutoOS window and theme preferences:
- Enable `Hide AutoOS Startup` if you don't want to see the `AutoOS Startup`.
- Adjust `App theme`, `Material` and `Tint color` to your liking.
- Select paths for **Switch Emulator** data to make them show up in the `Games` tab.

![Settings](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Settings.png)
# 📘 คู่มือการตั้งค่าสำหรับผู้ใช้ AutoOS (AutoOS Settings User Guide)

<details>
<summary>สิ่งที่คุณ <b>ไม่ควรทำ</b> บน AutoOS (คลิกเพื่อขยาย)</summary>
<br/>

- ห้ามใช้โปรแกรม `ปรับแต่ง (tweaks)` หรือ `เพิ่มประสิทธิภาพ (optimizers)` อื่น ๆ เพิ่มเติม (เนื่องจากจะตีกันกับระบบของ AutoOS)
- ห้ามใช้โปรแกรม `ล็อกเวลาการตอบสนอง (timer resolution)` หรือ `ตัวเคลียร์แรม (memory cleaners)` เพราะส่งผลเสียมากกว่าผลดี
- ห้ามใช้ `โปรแกรมจำกัดเฟรมเรตภายนอก (external frame rate limiters)` เช่น ในแผงควบคุม `NVCP` หรือ `RTSS` เนื่องจากโปรแกรมเหล่านี้จะช่วยให้ค่า **1% lows** ดีขึ้น แต่ต้องแลกมาด้วย **ค่าความหน่วง (latency) ที่เพิ่มขึ้น** (ยกเว้นกรณีที่ไม่ได้ใช้เล่นเกมแนวแข่งกันจริงจัง)
- ห้ามตั้งค่า `เอฟเฟกต์ภาพ (visual effects)` เป็น `Best Performance` (ประสิทธิภาพดีที่สุด)
- ห้ามปิดการใช้งาน `แอนิเมชัน (animations)`, `เอฟเฟกต์ความโปร่งใส (transparency)` หรือ `ไฟล์สลับหน่วยความจำเสมือน (paging file)`
- ห้าม **ถอนการติดตั้ง (Uninstall)** `MSI Afterburner, OBS, Everything, Windhawk, StartAllBack` หรือบรรดาตัวรันไทม์ (`runtimes`) ต่าง ๆ
- ห้าม **ติดตั้ง** `7-Zip` หรือ `WinRAR` เนื่องจากในระบบมีโปรแกรม `NanaZip` ติดตั้งมาให้เรียบร้อยแล้ว
- ห้าม **ถอนการติดตั้ง** แพ็กเกจแอปพลิเคชันระบบ (AppX Packages) ตัวอื่น ๆ เพิ่มเติม เช่น `Xbox Game Bar` หรือ `Microsoft Edge`

</details>

---

<details>
<summary>หากคุณต้องการยุบรวมพื้นที่ของพาร์ติชัน Windows ตัวเก่า เข้ากับพาร์ติชัน AutoOS (คลิกเพื่อขยาย)</summary>
<br/>

**ขั้นตอนที่ 1:**
- คัดลอกโฟลเดอร์เกมของคุณไปยังเส้นทางเดิม (path) แต่อยู่บนพาร์ติชันของ AutoOS แทน
- เปิดไฟล์รายการติดตั้ง (manifest files) จากตัวเปิดเกมต่าง ๆ จากนั้นกดปุ่ม `Ctrl + H` เพื่อแทนที่ตัวอักษรไดรฟ์ คลิก **Replace All** (แทนที่ทั้งหมด) แล้วกดบันทึกไฟล์
  - Epic Games
    - `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat`
    - `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests`
    - `C:\ProgramData\Epic\EpicOnlineServicesShared\InstallHelper\InstalledItems`
  - Riot Games
    - `C:\ProgramData\Riot Games\Metadata\<ชื่อเกมของคุณ>\<ชื่อเกมของคุณ>.live.product_settings.yaml`
  - Steam
    - `C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf`

**ขั้นตอนที่ 2:**
- ติดตั้งโปรแกรม `Minitool Partition Wizard` จากแท็บ `Applications` ในหน้าแอป AutoOS
- คลิกขวาที่ **พาร์ติชัน Windows ตัวเก่า** แล้วเลือก **Delete** (ลบ)
- คลิกขวาที่ **พาร์ติชัน AutoOS** แล้วเลือก **Extend** (ขยายพื้นที่) จากนั้นเลือกพื้นที่ของ **พาร์ติชัน Windows ตัวเก่า** และ **ลากแถบเลื่อนให้สุด** เพื่อใช้พื้นที่ทั้งหมด
- คลิก **Apply** (นำไปใช้) แล้วเลือก **Restart Now** (รีสตาร์ททันที)

  ---

  <details>
  <summary>หากคุณใช้บอร์ด <b>ASUS</b> แล้วเจอกล่องข้อความเตือน <b>GPT header corruption has been detected</b> (คลิกเพื่อขยาย)</summary>
  <br/>

  - กดปุ่ม `F1` เพื่อเข้าสู่หน้าจอ `BIOS`
  - กดปุ่ม `F7` เพื่อเข้าสู่ `Advanced Mode` (โหมดขั้นสูง)
  - ไปที่แท็บ `Boot` จากนั้นเลือก `Boot Configuration`
  - เปลี่ยนหัวข้อ `Next Boot Recovery Action` เป็น `Recovery` (หากมี)
  - เปลี่ยนหัวข้อ `Boot Sector (MBR/GPT) Recovery Policy` เป็น `Auto Recovery` (หากมี)

  </details>

  ---

  <details>
  <summary>หากยังมี <b>เมนูบูต Windows ตัวเก่า</b> แสดงขึ้นมาในตอนเปิดเครื่อง (คลิกเพื่อขยาย)</summary>
  <br/>

  - เปิด Command Prompt (CMD) ขึ้นมา แล้ววางคำสั่งด้านล่างนี้:
  ```
  bcdedit /enum
  ```

  - ค้นหาเมนูที่เป็นของพาร์ติชัน Windows ตัวเก่า จากนั้นคัดลอกค่ารหัสเฉพาะ `identifier` ของมันมา แล้วรันคำสั่ง:

  ```
  bcdedit /delete {คัดลอกรหัส identifier มาวางตรงนี้}
  ```
  </details>
</details>

---

### หน้าแรก (Home)
ลิงก์สำหรับเข้าถึงด่วน:
- เปิดคู่มือผู้ใช้ AutoOS (AutoOS User Guide)
- เปิดคู่มือการมีส่วนร่วมพัฒนา AutoOS (AutoOS Contribution Guide)
- เข้าร่วม AutoOS Discord Server
- บริจาคสนับสนุนโครงการ (Donate)

![Home](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Home.png)

### ระบบเสียง (Sound)
ปรับระดับเสียง, ฟอร์แมตสัญญาณเสียง และขนาดบัฟเฟอร์ (Buffer Size) ของอุปกรณ์นำเข้าและส่งออกเสียงปัจจุบันของคุณ:
- หากอุปกรณ์เสียงปลายทางของคุณรองรับระดับบัฟเฟอร์ (buffer size) ที่ต่ำลง คุณสามารถปรับลดระดับลงเพื่อแลกกับการประมวลผลของ CPU ที่เพิ่มขึ้นเล็กน้อยได้

![Sound](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Sound.png)

### หน้าจอแสดงผล (Displays)
ปรับเปลี่ยนหรือนำเข้าโปรไฟล์ Custom Resolution Utility (CRU) ด้วยตัวเอง:

  <details>
  <summary>สำหรับผู้ใช้งานการ์ดจอ <b>NVIDIA</b> (คลิกเพื่อขยาย)</summary>
  <br/>

  - เปิดแผงควบคุม NVIDIA Control Panel ไปที่เมนู `Display` -> `Change resolution` แล้วคลิกเลือก `หน้าจอ` ที่คุณต้องการตั้งค่า
  - ตรวจสอบให้แน่ใจว่าได้เลือกความละเอียด (Resolution) และอัตรารีเฟรชเรต (Refresh rate) ที่คุณต้องการใช้งานอยู่จริง
  - คลิกปุ่ม `Customize...` -> `Create Custom Resolution` และคลิก `Accept` ในหน้านโยบายคำเตือน
  - เปลี่ยนหัวข้อ `Timing` -> `Standard` ให้เป็น `Manual`
  
    **วงจรการทดสอบปรับแต่งค่า (Testing Loop):**
    ทุกครั้งที่คุณปรับเปลี่ยนตัวเลขใด ๆ ด้านล่างนี้ ให้กดปุ่ม `Test` และทำตามกฎดังนี้:
    - **หากจอแสดงผลใช้งานได้ปกติ:** ให้คลิก `No` แล้วทำการปรับลด/ปรับเพิ่มค่าต่อไป
    - **หากพบอาการภาพล้ม จอกระพริบ หรือผิดปกติ:** ให้คลิก `No` และทำการค่อย ๆ ปรับเปลี่ยนทีละนิดเพื่อหาขีดจำกัดสูงสุดของการทำงาน
    - **หากหน้าจอมืดสนิท / ไม่มีสัญญาณอินพุต (Black Screen / No Signal):** ให้กดปุ่ม `Enter` เพื่อยกเลิกขั้นตอน แล้วทำการปรับหาขีดจำกัดด้วยสเกลที่เล็กลงอีกครั้ง
    
    **เฟส 1: เพิ่มค่า Vertical Total Pixels (พิกเซลแนวตั้งรวม) ให้มากที่สุด**
    - ค่อย ๆ **เพิ่ม** ค่า **Vertical** `Total pixels` ทีละ 20
    - กดปุ่ม `Test` ทุกครั้งหลังเปลี่ยนค่าตามกฎด้านบน จนกว่าจะเจอรันไทม์สูงสุดที่ระบบรับได้

    **เฟส 2: ลดค่า Horizontal Total Pixels (พิกเซลแนวนอนรวม) ให้เหลือน้อยที่สุด**
    - จดบันทึกค่าตั้งต้น (default) ของเดิมไว้ก่อน
    - ค่อย ๆ **ลด** ค่า **Horizontal** `Total pixels` ลงทีละ 20
    - กดปุ่ม `Test` ทุกครั้งหลังเปลี่ยนค่าตามกฎด้านบน จนกว่าจะเจอระดับต่ำสุดที่ระบบยังเสถียรอยู่

    **เฟส 3: ปรับแต่งละเอียดค่า Vertical Total Pixels ขั้นสุดท้าย**
    - ค่อย ๆ **เพิ่ม** ค่า **Vertical** `Total pixels` อีกครั้งในสเกลที่ละเอียดขึ้น โดยเพิ่มขึ้นทีละ 1
    - กดปุ่ม `Test` ทุกครั้งหลังเปลี่ยนค่าตามกฎด้านบน จนกว่าจะได้ค่าสูงสุดที่เสถียรที่สุดจริง ๆ
    - หากไม่สามารถปรับเพิ่มจากขั้นตอนใน **เฟส 1** ได้เลย ให้ตั้งค่า **Horizontal** `Total pixels` กลับไปเป็นค่าตั้งต้น และ **ข้ามเฟส 4 ไปได้เลย**

    **เฟส 4: ปรับแต่งละเอียดค่า Horizontal Total Pixels ขั้นสุดท้าย**
    - ค่อย ๆ **เพิ่ม** ค่า **Horizontal** `Total pixels` อีกครั้ง โดยเพิ่มขึ้นทีละ 10
    - กดปุ่ม `Test` ทุกครั้งหลังเปลี่ยนค่าตามกฎด้านบน จนกว่าจะได้ค่าสูงสุดที่เสถียรที่สุด

    **บันทึกค่าการตั้งค่าโดยใช้โปรแกรม Custom Resolution Utility (CRU):**
    - ในหน้าแอป AutoOS ให้คลิกปุ่ม `Launch` ภายใต้หัวข้อ `Create a custom resolution`
    - คลิกเลือกรายการใต้หัวข้อ `Extension blocks` จากนั้นคลิกเลือกปุ่ม `Edit` ด้านล่าง
    - เปลี่ยนค่า `Type` เป็น `DisplayID 2.0` (หากไม่มีข้อมูลอะไรเลย ให้เปลี่ยนกลับไปเป็น `CTA-861`)
    - คลิกที่รายการความละเอียดที่มี **อัตรารีเฟรชเรตเดียวกัน** กับที่คุณได้ตั้งไว้ใน NVIDIA Control Panel ใต้หัวข้อ `Detailed resolutions` จากนั้นคลิกปุ่ม `Edit` ด้านล่าง
    - กรอกตัวเลขทั้งหมดที่ได้จากใน NVIDIA Control Panel ลงในช่องที่ตรงกันของโปรแกรม CRU:
      - Front porch (pixels) -> กรอกช่อง Front porch
      - Sync width (pixels) -> กรอกช่อง Sync width
      - ในโปรแกรม CRU ให้คลิกที่ช่อง `Total` แล้วใส่ตัวเลขที่บันทึกมาลงไป
      - Polarity -> กรอกช่อง Sync polarity
      - Refresh rate -> กรอกช่อง Refresh rate
    - หากค่ารีเฟรชเรตจริง (`Actual` refresh rate) ไม่ได้ลงท้ายด้วย `.000 Hz` พอดี ให้ลองปรับเพิ่มหรือลดตัวเลข Horizontal Total pixels ทีละ 1 จนกว่าค่าจริงจะลงท้ายด้วย `.000 Hz`
    - คลิกปุ่ม `OK` จากนั้นคลิกปุ่ม `Export` และเปลี่ยนประเภทการเซฟ (Save as type) ให้เป็นแบบ `EXE File (*.exe)` แล้วเซฟเก็บไว้ในหน่วยความจำภายนอก เช่น USB หรือ External HDD
    - คลิก `OK` เพื่อปิดโปรแกรม CRU
    - ในแอป AutoOS ให้คลิกปุ่ม `Restart` ใต้หัวข้อ `Restart the graphics driver`

    **การใช้งานครั้งต่อไป:**
    - คุณสามารถกดปุ่ม `Import profile` ในแอป AutoOS แล้วเลือกไฟล์ `.exe` ที่เคยเซฟไว้เพื่อนำประวัติการตั้งค่ากลับมาใช้งานใหม่ได้ทันที

  </details>

![Displays](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Displays.png)

### การ์ดจอ (Graphics Cards)
เลือกการตั้งค่าตามความต้องการของคุณ:
- คลิกปุ่ม `Update` (หากมี) เพื่ออัปเดตไดรเวอร์การ์ดจอโดยระบบจะยังคงรักษาการตั้งค่าเดิมของคุณไว้
- เปิดใช้งาน `High-Bandwidth Digital Content Protection (HDCP)` หากคุณต้องรับชม **เนื้อหาที่ติดลิขสิทธิ์ DRM** เช่น `Netflix` เป็นต้น
- ปิดใช้งาน `High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio` หากคุณไม่จำเป็นต้องใช้หูฟังหรือลำโพงที่ต่อตรงผ่านจอภาพของคุณ
- ปรับแต่งการตั้งค่าการ์ดจอด้วยตนเอง หรือนำเข้าไฟล์โปรไฟล์ `MSI Afterburner Profile`
- เปิดใช้งานตัวเลือก `OBS Studio` หากคุณต้องการใช้ระบบบันทึก **คลิปวิดีโอย้อนหลัง** (ความยาว `30 วินาที` โดยใช้ปุ่มลัด `Alt + F10`)

![Graphics Cards](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Graphics%20Cards.png)
![Graphics Cards2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Graphics%20Cards2.png)

### การจัดลำดับการทำงานของ CPU รายตัว (Per-CPU Scheduling)
ปรับแต่งหรือตั้งค่าความผูกพันของ CPU (Affinities) สำหรับอุปกรณ์เสียง (Audio), การ์ดจอ (GPU), XHCI (USB) และการ์ดควบคุมเครือข่าย (NIC):
- คลิกปุ่ม `Optimize Affinities` เพื่อทำการตั้งค่าระบบ Affinities ใหม่อีกครั้งหลังจากที่ทำการ **ติดตั้งไดรเวอร์การ์ดจอหรืออุปกรณ์เครือข่ายใหม่** หรือหลังจากเปิด/ปิดระบบ `Hyper-Threading` / `SMT` บนคอมพิวเตอร์ของคุณ

![Per-CPU Scheduling](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Per-CPU%20Scheduling.png)
![Per-CPU Scheduling2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Per-CPU%20Scheduling2.png)

### อุปกรณ์และบลูทูธ (Bluetooth & Devices)
สลับการเปิด/ปิดบริการบลูทูธ, ไดรเวอร์ และตั้งค่าระบบควบคุมจังหวะสัญญาณ XHCI Interrupt Moderation (IMOD) ของตัวควบคุมแต่ละชุด:
- แนะนำให้คงค่า IMOD ของอุปกรณ์ USB ให้ปิดการทำงานไว้ (disabled) สำหรับคอนโทรลเลอร์ USB ทั้งหมด

![Bluetooth & Devices](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Bluetooth%20&%20Devices.png)

### เครือข่ายและอินเทอร์เน็ต (Network & Internet)
ปรับตั้งค่าอะแดปเตอร์เครือข่ายขั้นสูงด้วยตนเอง หรือใช้การตั้งค่าเพิ่มประสิทธิภาพอัตโนมัติ:
- คลิกที่ปุ่ม `Optimize Adapter` เพื่อนำการตั้งค่าเครือข่ายกลับมาใช้งานอีกครั้งหลังจากที่คุณได้ **ลงไดรเวอร์การ์ดเครือข่ายใหม่**

![Network & Internet](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Network%20&%20Internet.png)

### พลังงานและแบตเตอรี่ (Energy & Power)
ปรับแต่ง, แก้ไข, โคลน, ลบ, คืนค่า, ส่งออก หรือนำเข้าแผนพลังงาน (Power plans) พร้อมเปรียบเทียบค่าความต่าง:
- แนะนำให้ใช้แผนพลังงาน `AutoOS Power Plan` ต่อไปเพื่อผลลัพธ์ที่ดีที่สุด
- หากคุณพบปัญหาเกี่ยวกับการใช้งานแผนพลังงาน AutoOS สามารถแจ้งปัญหานี้ได้ในระบบ [Discord Server](https://discord.gg/bZU4dMMWpg)
- สามารถเลือกแผนพลังงานอื่นในกล่องคำสั่งที่ 2 เพื่อทำการ **เปรียบเทียบการตั้งค่า** กับแผนปัจจุบันที่เปิดใช้งานอยู่
- คลิกขวาบนแผนพลังงานที่กำลังเปิดใช้งานอยู่ในกล่องเลือกเพื่อแสดงเมนูการจัดการ `Edit, Duplicate, Delete หรือ Export` (แก้ไข, โคลน, ลบ หรือส่งออกข้อมูล)

![Energy & Power](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Energy%20&%20Power.png)
![Energy & Power2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Energy%20&%20Power2.png)
![Energy & Power3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Energy%20&%20Power3.png)

### บริการและการทำงานของระบบ (Services & Drivers)
สลับการเปิด/ปิดเซอร์วิสต่าง ๆ ตามลักษณะการใช้งาน:
- **เลือกติ๊กถูก** ในหัวข้อ `WiFi Support` หากคุณกำลังใช้งานอินเทอร์เน็ตไร้สาย **WiFi ในระหว่างการเล่นเกม**
- **เลือกติ๊กถูก** ในหัวข้อ `Bluetooth Support` หากคุณมีการเชื่อมต่ออุปกรณ์ **บลูทูธในระหว่างการเล่นเกม**
- **ปิดใช้งานตัวเลือกนี้** ที่ด้านบนสุดของหน้าและทำการรีสตาร์ทเครื่องทุกครั้งในวันที่ต้องการ **เล่นเกมเชิงแข่งขันอย่างจริงจัง** (Competitive Gaming)
- **เปิดใช้งานตัวเลือกนี้อีกครั้ง** และรีสตาร์ทหากคุณต้องการเรียกใช้ฟังก์ชันเหล่านี้สำหรับ **การทำงานทั่วไป** หรือสำหรับลงติดตั้งโปรแกรม/ไดรเวอร์อื่น ๆ

![Services & Drivers](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Services%20&%20Drivers.png)

### การตั้งค่า BIOS (BIOS Settings)
ปรับแต่งหรือผสานรวมค่าแนะนำสำหรับ BIOS:
- บนแถบเครื่องมือ ให้คลิกปุ่ม `Merge` และ `Import to NVRAM` จากนั้นทำการรีสตาร์ทคอมพิวเตอร์ของคุณ

  **หากคุณไม่มีการเชื่อมต่ออินเทอร์เน็ต:**
  - ติ๊กถูกเปิดสวิตช์ในแท็บ `Services & Drivers` แล้วทำการรีสตาร์ทเครื่อง
  - คลิกปุ่ม `Optimize` ในแท็บ `Per-CPU Scheduling` และแท็บ `Network & Internet` อีกครั้ง

  **ในกรณีที่คอมพิวเตอร์บูตไม่ขึ้น (เปิดไม่ติด):**
  - ทำการรีเซ็ตค่า BIOS (Reset CMOS) โดยการกดปุ่มเคลียร์ค่าบน **เมนบอร์ด** (หากเมนบอร์ดของคุณมีปุ่มนี้) หรือโดยการ **ถอดถ่านกระดุม CMOS** บนเมนบอร์ดออกทิ้งไว้ประมาณ 5 นาที
  - หลังจากเปิดติดแล้ว ให้เลือก **ลดระดับ Merge Count (จำนวนการผสาน)** ลงมาเรื่อย ๆ จนกว่าคุณจะ **เจอตัวเลือกที่ทำให้เครื่องมีปัญหา** บูตไม่ขึ้น
  - เมื่อพบค่าตัวเลือกนั้นแล้ว กรุณาแจ้งข้อมูลนี้เข้ามาที่ [Discord Server](https://discord.gg/bZU4dMMWpg) เพื่อทางผู้พัฒนาจะนำไปปรับปรุงแก้ไขต่อไปครับ

  **ในกรณีที่เกิดอาการเครื่องค้าง, จอฟ้า หรือความสามารถในการประมวลผลลดลง:**
  - ทางเลือกที่ 1 (ง่ายสุด): คลิกที่ปุ่ม `Restore` เพื่อเลือกคืนค่า NVRAM ที่สำรองไว้ในประวัติรุ่นเก่าสุดหรือรุ่นก่อนหน้า
  - ทางเลือกที่ 2 (ยากกว่า):
    - **ระบบ Intel:**
      - ลดระดับตัวคูณคล็อกเทอร์โบ (`Max Turbo Ratios`) ลง
      - เปิดใช้งานระบบ `Hyper-Threading`
      - ปิดการทำงานของคอร์ประหยัดพลังงาน (`E-Cores`) (หัวข้อ Active E-Cores, Active Efficient Cores, Active Efficient-cores, หรือ No. of CPU E-Cores Enabled)
    - **ระบบ AMD:**
      - ปรับลดค่าระดับ `All Core Curve Optimizer Magnitude` ลง

![BIOS Settings](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/BIOS%20Settings.png)

### การล้างไฟล์ขยะบนดิสก์ (Disk Cleanup)
การเคลียร์ไฟล์ขยะและเพิ่มพื้นที่ไดรฟ์ต่าง ๆ:
- คลิกที่ปุ่ม `Clean up disks` เพื่อเริ่มทำความสะอาดดิสก์และกู้คืนพื้นที่ที่ถูกใช้งานโดยไม่จำเป็น

![Disk Cleanup](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Disk%20Cleanup.png)

### ความปลอดภัยของ Windows (Windows Security)
สลับการเปิดใช้งานความปลอดภัยต่าง ๆ ของระบบ:
- เปิดใช้งาน `HVCI` และ `VBS` หากคุณมีการรันหรือเข้าเล่นเกมที่มีระบบป้องกันการโกงขั้นสูง เช่น `FACEIT` หรือ `Valorant`

![Windows Security](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Windows%20Security.png)

### การอัปเดตระบบปฏิบัติการ (Windows Update)
เปิดหรือปิดระบบอัปเดตระบบปฏิบัติการและล็อกรุ่นเป้าหมาย:
- เปิดใช้งาน `Windows Updates` เพื่อรับการแก้ไขระบบและ **ระบบความปลอดภัยรุ่นล่าสุด**
- **การตั้งค่าปรับแต่งเพื่อประสิทธิภาพสูงสุด (Optimizations) ทั้งหมดจะถูกรักษาไว้ตามเดิม**
- ตัวไดรเวอร์เสริมต่าง ๆ **จะถูกยกเว้นออกจากการดาวน์โหลดโดยอัตโนมัติ (Excluded by default)** เพื่อป้องกันระบบรวน

![Windows Update](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Windows%20Update.png)

### เว็บเบราว์เซอร์ (Browsers)
การติดตั้งเพิ่มเติมภายหลังสำหรับเว็บเบราว์เซอร์และส่วนขยาย (Extensions):
- แนะนำให้หันมาใช้งาน `Thorium` หรือ `Helium` แทนการใช้ `Chrome` แบบปกติ
- แนะนำให้ใช้เบราว์เซอร์ `Zen` เพื่อความลื่นไหลในการทำงาน (**productivity**) และความเพลิดเพลินกับ **ดีไซน์ที่หรูหราทันสมัย**

![Browsers](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Browsers.png)

### แอปพลิเคชัน (Applications)
การติดตั้งเพิ่มเติมภายหลังสำหรับแอปพลิเคชันระบบ:
- เลือกติดตั้ง `Discord` ซึ่งจะมาพร้อมกับโครงสร้างการตั้งค่าล่วงหน้า (`preconfigured`) ร่วมกับ `Vencord`, `OpenAsar` และ **การตั้งค่าที่มีประสิทธิภาพสูงสุด**
- แนะนำใช้โปรแกรม `Logitech Onboard Memory Manager` แทนตัวแอปใหญ่ `Logitech G HUB` หากอุปกรณ์เมาส์ของคุณคือ **เมาส์ Logitech**

![Applications](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications.png)
![Applications2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications2.png)
![Applications3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications3.png)
![Applications4](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Applications4.png)

### แท็บหน้าจอเกม (Games)
สำรวจและเข้าเล่นคลังเกมของคุณ (รองรับความเชื่อมโยงกับ Epic Games, Riot Games, EA, Ubisoft Connect, Steam, Eden, Citron และ Ryujinx):
- สามารถเปลี่ยนสลับบัญชี Epic Games และบัญชี Steam ได้ง่ายดายที่เมนูตรงมุมบนขวา
- กดปุ่ม `Play` (เล่นเกม) เพื่อเริ่มรันเกมต่าง ๆ

  **กรณีปิดการใช้งานสวิตช์ Services & Drivers ไว้:**
  - เมื่อตัวเกมโหลดเข้าหน้าเกมเรียบร้อยแล้ว ให้กดปุ่ม `Stop Processes`
  - ในโหมดนี้ ให้ใช้ปุ่มลัด `Alt+Tab` ในการเปลี่ยนสลับเพื่อสลับไปมาระหว่างโปรแกรมอื่น ๆ
  - กดปุ่ม `Restart Processes` อีกครั้งเพื่อทำการเรียกคืนแถบงาน (Taskbar) และส่วนควบคุมระบบปฏิบัติการหลักกลับมาดังเดิม

  **การเพิ่มเกมในรายการ:**
  - สำหรับเกมจากค่าย `Riot Games` หากต้องการให้แสดงขึ้นมาในหน้านี้ ให้ติดตั้งเกมเหล่านี้ผ่าน `Epic Games Launcher` ร่วมด้วย
  - สำหรับเกมค่าย `EA` หรือค่าย `Ubisoft Connect` ให้เลือกเพิ่มเกมเหล่านี้ไว้ในคลังเกมของโปรแกรม `Epic Games Launcher` ของคุณ
  - สำหรับเกมอื่น ๆ ทั่วไป: ให้เลือกกดเพิ่มเกมเป็น **เกมที่ไม่ใช่ของ Steam (non-steam game)** ภายในโปรแกรม `Steam`
  - ตรวจสอบให้แน่ใจว่าชื่อเกมตรงกันกับฐานข้อมูลบนเว็บไซต์ [IGDB](https://www.igdb.com/)

  **หมายเหตุเพิ่มเติม:**
  - แนะนำให้ **จำกัดเฟรมเรตสูงสุด (frame rate limit)** ในตัวเกมให้มีค่าสัมพันธ์กับตัวเลข **อัตรารีเฟรชเรตหน้าจอ** ของคุณเป็นสัดส่วนลงตัว (เช่น จอ 144hz -> ล็อกเฟรมเรตที่ 72, 144 หรือ 288fps)

![Games](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Games.png)

### ตั้งค่าโปรแกรม (Settings)
การปรับแต่งการตั้งค่าต่าง ๆ ของหน้าต่างแอปและธีมของ AutoOS:
- ติ๊กถูกเปิดใช้ `Hide AutoOS Startup` หากคุณไม่ต้องการเห็นหน้าต่างแอนิเมชันเปิดใช้งานแอปในตอนเริ่มรัน
- ปรับเปลี่ยนหัวข้อ `Material` เพื่อเปลี่ยนวัสดุและภาพสะท้อนพื้นผิวของหน้าต่างแอปตามใจชอบ
- เลือกที่อยู่โฟลเดอร์สำหรับข้อมูล **ตัวจำลองเครื่องเล่นเกม Nintendo Switch (Switch Emulator)** เพื่อดึงเกมในอีมูเลเตอร์มาแสดงผลในหน้ารวมเกมในแท็บ `Games`

![Settings](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Settings/Settings.png)

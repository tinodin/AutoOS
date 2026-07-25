# 🚀 คู่มือการติดตั้ง (Installation Guide)

> [!NOTE]  
> การติดตั้งตามคู่มือนี้ **ไม่จำเป็น** ต้องใช้แฟลชไดรฟ์ (USB drive) แต่อย่างใด<br/>
> AutoOS จะถูกติดตั้งในรูปแบบ **Dual Boot (ระบบปฏิบัติการคู่)** โดยอัตโนมัติ ซึ่งหมายความว่า **ข้อมูลเดิม** และ Windows ตัวเก่าของคุณ **จะยังคงอยู่และสามารถเข้าถึงได้ตามปกติ** หลังการติดตั้งเสร็จสิ้น
> โปรดอย่าทำขั้นตอนใด ๆ นอกเหนือจากที่คู่มือระบุไว้ (เช่น การจัดการเกี่ยวกับไฟล์ ISO, ไดรเวอร์ หรือการแบ่งพาร์ติชันด้วยตนเอง)

### ขั้นตอนที่ 1: เข้าร่วม Discord Server
เข้าร่วม [Discord Server](https://discord.gg/bZU4dMMWpg) เพื่อขอ **ความช่วยเหลือระหว่างการติดตั้ง** และติดตามข่าวสารเกี่ยวกับ **การอัปเดตและการเปลี่ยนแปลงในอนาคต**

### ขั้นตอนที่ 2: ดาวน์โหลดไฟล์ ISO
ดาวน์โหลดไฟล์ Windows `25H2.iso` ล่าสุดได้จาก [ลิงก์นี้](https://drive.google.com/drive/folders/1BlAYofjlW1bU-WPG3jXygO1ezoJ4gPs7?usp=sharing) (หากพบข้อผิดพลาด ให้เข้าสู่ระบบด้วยบัญชี Google ของคุณก่อนทำการดาวน์โหลด)<br/>
ระบบไม่รองรับการใช้งานร่วมกับ ISO เวอร์ชันอื่น ๆ (อาจเปิดใช้งานไม่ได้) เพื่อให้มั่นใจในเรื่องความเสถียรและฟังก์ชันการใช้งานล่าสุด

### ขั้นตอนที่ 3: ดาวน์โหลดไดรเวอร์
เปิดหน้าต่าง **Network Connections** (โดยการพิมพ์คำสั่ง `ncpa.cpl` ในช่อง Run/ค้นหา) จากนั้นดูแบรนด์/ยี่ห้อของอะแดปเตอร์สายแลน (Ethernet), Wi-Fi และบลูทูธ (Bluetooth) ของคุณ (ส่วนใหญ่บลูทูธจะใช้ยี่ห้อเดียวกับ Wi-Fi)<br/>
หากคุณใช้งานอุปกรณ์ตามยี่ห้อด้านล่างนี้ ให้ดาวน์โหลดไดรเวอร์ที่ต้องการจากลิงก์ที่เตรียมไว้ให้:<br/>

**INTEL:** [Ethernet (สายแลน)](https://www.intel.com/content/www/us/en/download/727998/intel-network-adapter-driver-for-microsoft-windows-11.html) · [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/Bluetooth.zip)

**Realtek:** [Ethernet (สายแลน)](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Realtek/Ethernet.zip) · [Wi-Fi และ Bluetooth](https://www.realtek.com/Download/Index?cate_id=203&menu_id=297)

**MediaTek:** [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/MediaTek/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/MediaTek/Bluetooth.zip)

**Qualcomm:** [Wi-Fi](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Qualcomm/WiFi.zip) · [Bluetooth](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Qualcomm/Bluetooth.zip)

**Killer:** [Ethernet, Wi-Fi และ Bluetooth](https://github.com/tinodin/AutoOS-Resources/releases/download/v1.0.0.0/Killer.zip)

**Marvell:** [Ethernet (สายแลน)](https://www.marvell.com/content/dam/marvell/en/drivers/Marvell_AQtion_Win_v3.1.11_10-17-2025.zip)

---

<details>
<summary>หากคุณใช้งาน <b>โน้ตบุ๊ก (Laptop)</b> (คลิกเพื่อขยาย)</summary>
<br/>

เปิดโปรแกรม **System Information** (โดยพิมพ์ `msinfo32` ในช่อง Run/ค้นหา) แล้วดูที่หัวข้อ **System Model**<br/>
ค้นหาคำว่า "**{System Model} drivers**" ในเบราว์เซอร์ของคุณ<br/>
คลิกเข้าไปที่หน้าสนับสนุนผู้ใช้ (Support page) ไปที่หัวข้อไดรเวอร์ แล้วเลือก Windows 11 (หากมีตัวเลือก) จากนั้นดาวน์โหลดไดรเวอร์ **เสียง (Audio) และทัชแพด (Touchpad)** (หากมี)
<br/>
</details>

---

<details>
<summary>หากคุณใช้คอมพิวเตอร์แบรนด์สำเร็จรูป (Prebuilt PC) หรือโน้ตบุ๊ก (Laptop) ที่ใช้ซีพียู Intel <b>Gen 10 ขึ้นไป</b> (คลิกเพื่อขยาย)</summary>
<br/>

เปิด Device Manager (โดยพิมพ์ `devmgmt.msc` ในช่อง Run/ค้นหา) แล้วตรวจสอบว่ามีรายการที่มีคำว่า "**Intel**" ปรากฏอยู่ภายใต้หัวข้อ `Storage controllers` (ตัวควบคุมพื้นที่เก็บข้อมูล) หรือไม่<br/>
หากไม่มี ให้ข้ามไปทำขั้นตอนที่ 4 ได้เลย<br/>

แต่หากมีรายการที่เป็น "**Intel**" ปรากฏอยู่ คุณจะมี **2 ทางเลือก**:

**ทางเลือกที่ 1 (แนะนำ):**
- ทำการปิดระบบ `VMD Controller` ในหน้า BIOS ของเมนบอร์ด
- สำหรับแบรนด์ DELL/Alienware ให้ไปที่หัวข้อ `Storage -> SATA/NVMe Operation` และเปลี่ยนจาก `Disabled` เป็น `AHCI/NVMe`

**ทางเลือกที่ 2:**
- ดาวน์โหลดไดรเวอร์ Intel® Rapid Storage Technology (RST) ที่เหมาะสมกับรุ่นซีพียูของคุณจากลิงก์ด้านล่างนี้:
  - [ซีพียู Intel Gen 10 และ 11](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/10th%20and%2011th/RST.zip)
  - [ซีพียู Intel Gen 12 ถึง 15](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/12th%20to%2015th/RST.zip)
  - [ซีพียู Intel® Core™ Ultra Series 3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/Files/Intel/RST/Intel%C2%AE%20Core%E2%84%A2%20Ultra%20Series%203/RST.zip)

**หมายเหตุเพิ่มเติม:**
- **ทางเลือกที่ 1** จะช่วยให้ระบบส่งผ่านข้อมูลของฮาร์ดดิสก์/SSD ได้ **รวดเร็วกว่า**
- หากคุณปิด `VMD Controller` แล้วพบอาการจอฟ้า (**BSOD**) ในระบบปฏิบัติการ Windows ตัวเก่า ให้ทดลองบูตระบบเข้าสู่ Safe Mode สักหนึ่งครั้ง จากนั้นจึงทำการรีสตาร์ทเครื่องตามปกติ
- หากคุณเปิดใช้งาน `VMD Controller` ทิ้งไว้ และ **ไม่ได้ดาวน์โหลดไดรเวอร์ RST สำรองเตรียมไว้** จะพบอาการจอฟ้าแจ้งข้อความ `Inaccessible boot device` ในขั้นตอนถัดไป
</details>

---

เมื่อทำการดาวน์โหลดไดรเวอร์มาเสร็จสิ้นแล้ว ให้ทำการแตกไฟล์ `.zip` ทั้งหมดออกมาก่อน<br/>
สำหรับไฟล์นามสกุล `.exe` บางตัว ให้เปิดรันโปรแกรมแล้วคลิกเลือกคำสั่ง `Extract` (หากมีตัวเลือก) เพื่อแตกไฟล์ออกมา<br/>
หากไม่มีตัวเลือกดังกล่าว ให้ใช้โปรแกรมแตกไฟล์ เช่น `7-Zip, NanaZip หรือ WinRAR` เพื่อดึงข้อมูลออกมาแทน

สุดท้าย ให้ทำการสร้าง **โฟลเดอร์ใหม่ (New Folder)** ขึ้นมาโฟลเดอร์หนึ่ง แล้วย้ายโฟลเดอร์ไดรเวอร์ที่แตกไฟล์ทั้งหมดข้างต้นมารวมกันไว้ในนี้

### ขั้นตอนที่ 4: รันสคริปต์เพื่อติดตั้ง (Run the deployment script)
เปิดโปรแกรม PowerShell ด้วยสิทธิ์ผู้ดูแลระบบ (**Run as Administrator**)<br/>
คัดลอกโค้ดด้านล่างนี้ไปวางในหน้าต่าง PowerShell เพื่อเริ่มดำเนินการรันสคริปต์ตัวช่วยติดตั้ง<br/>
ระบบจะทำการตั้งค่าจัดการเกี่ยวกับไฟล์ ISO, ไดรเวอร์ และแบ่งพื้นที่พาร์ติชันที่เตรียมไว้ให้โดยอัตโนมัติ<br/>

```ps1
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
irm https://raw.githubusercontent.com/tinodin/AutoOS/master/deploy/deploy.ps1 | iex
```
เลือกไฟล์ `25H2.iso` ที่คุณดาวน์โหลดมาในขั้นตอนที่ 2 และเลือก **โฟลเดอร์ไดรเวอร์** ที่คุณรวบรวมไว้ในขั้นตอนที่ 3<br/>
หากพบข้อผิดพลาดหรือรันไม่ผ่านในส่วนใดของสคริปต์นี้ สามารถส่งคำถามไว้ได้ที่ [Discord Server](https://discord.gg/bZU4dMMWpg)

### ขั้นตอนที่ 5: รีสตาร์ทเข้าสู่ AutoOS
เมื่อสคริปต์ติดตั้งดำเนินการสำเร็จแล้ว ให้กดรีสตาร์ทเครื่องคอมพิวเตอร์ของคุณ<br/>
บูตระบบเข้าสู่ `AutoOS` โดยการเลือกเมนูและกดปุ่ม `Enter` ในหน้าจอตอนเริ่มต้นระบบใหม่<br/>

> [!WARNING]
> ตรวจสอบให้แน่ใจว่าได้ **ต่อสายแลน (Ethernet) ไว้** หรือทำการ **เชื่อมต่ออินเทอร์เน็ตผ่าน Wi-Fi ในขั้นตอนตั้งค่า**<br/>
> **ห้ามกดข้ามขั้นตอนการบังคับต่ออินเทอร์เน็ตของระบบติดตั้งโดยเด็ดขาด!**

### ขั้นตอนที่ 6: ตัวติดตั้ง AutoOS Installer
หลังจากขั้นตอนตั้งค่าระบบแรกเริ่ม (OOBE) สำเร็จแล้ว ให้รอเครื่องรีสตาร์ทอีกครั้งและรอจนกระทั่งโปรแกรม `AutoOS Installer` เปิดขึ้นมา<br/>

> [!IMPORTANT]
> คลิกที่แถบข้อความ `AutoOS User Guide` ในส่วนแท็บ Home ของแอป และปฏิบัติตาม **คำแนะนำการตั้งค่า** ของตัวโปรแกรม `AutoOS Installer`

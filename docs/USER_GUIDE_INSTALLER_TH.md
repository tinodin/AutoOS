# 📘 คู่มือการใช้งานตัวติดตั้ง AutoOS (AutoOS Installer User Guide)

### หน้าแรก (Home)
ลิงก์สำหรับเข้าถึงเมนูต่าง ๆ อย่างรวดเร็ว:
- เปิดคู่มือผู้ใช้ AutoOS (AutoOS User Guide)
- เปิดคู่มือการมีส่วนร่วมพัฒนา AutoOS (AutoOS Contribution Guide)
- เข้าร่วม AutoOS Discord Server
- บริจาคสนับสนุนโครงการ (Donate)

![Home](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Home.png)

### การปรับแต่งส่วนบุคคล (Personalization)
เลือกธีมและตั้งค่าแถบงาน (Taskbar) ตามความชอบของคุณ:
- เปลี่ยนโหมดธีมเป็น `Always Light` (สว่างตลอดเวลา) หรือ `Always Dark` (มืดตลอดเวลา) ตามต้องการ หากปล่อยไว้ ระบบจะสลับธีมให้โดยอัตโนมัติตามช่วงเวลาของวัน
- เปลี่ยนโหมดเป็น `Custom` เพื่อตั้งเวลาการเปลี่ยนธีมด้วยตัวเอง
- ปิดใช้งานหัวข้อ `Always show all tray icons in the notification area` (แสดงไอคอนถาดระบบทั้งหมดในพื้นที่แจ้งเตือนเสมอ) หากคุณต้องการให้แถบงานดูสะอาดตาและเป็นระเบียบยิ่งขึ้น

![Personalization](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Personalization.png)

### เว็บเบราว์เซอร์ (Browsers)
เลือกเว็บเบราว์เซอร์และส่วนขยาย (Extensions) ที่ต้องการติดตั้ง:
- แนะนำให้ใช้เบราว์เซอร์อย่าง `Thorium` หรือ `Helium` แทน `Chrome` เพื่อความรวดเร็วและประสิทธิภาพที่ดีกว่า
- เลือกใช้ `Zen` เบราว์เซอร์ที่ตอบโจทย์ทั้งด้าน **การทำงาน (Productivity)** และ **ดีไซน์ที่สวยงาม**
- เลือกติดตั้ง `uBlock Origin` และส่วนขยายอื่น ๆ ตามที่ระบบ **คัดสรรมาให้ (my selection)**

![Browsers](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Browsers.png)

### แอปพลิเคชัน (Applications)
เลือกแอปพลิเคชันที่คุณต้องการใช้งาน:
- เลือก `Discord` ซึ่งจะมาพร้อมการตั้งค่าล่วงหน้า (**preconfigured**) ร่วมกับ `Vencord`, `OpenAsar` และ **การตั้งค่าทั้งหมด**
- เลือก `Epic Games, Steam หรือ Riot Games` เพื่อระบบจะทำการ **นำเข้าบัญชีและตัวเกม** จากระบบ Windows ตัวเก่าของคุณโดยอัตโนมัติ
- แนะนำให้ใช้ `Logitech Onboard Memory Manager` แทน `Logitech G HUB` หากคุณใช้ **เมาส์ของ Logitech**

![Applications](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Applications.png)
![Applications2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Applications2.png)
![Applications3](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Applications3.png)
![Applications4](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Applications4.png)

### หน้าจอแสดงผล (Displays)
นำเข้าโปรไฟล์โปรแกรม Custom Resolution Utility (CRU) ที่ตั้งค่าไว้ล่วงหน้า (ระบุหรือไม่ก็ได้):

![Displays](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Displays.png)

### การ์ดจอ (Graphics Cards)
ตั้งค่าเกี่ยวกับการทำงานของการ์ดจอของคุณ:
- สำหรับ **โน้ตบุ๊ก (Laptops)**: ควรตรวจสอบให้แน่ใจว่าได้ติดตั้งไดรเวอร์การ์ดจอ (GPU) **ทั้งหมด** ที่มีอยู่
- สำหรับ **คอมพิวเตอร์ตั้งโต๊ะ (PCs)**: คุณสามารถเลือกที่จะไม่ติดตั้งไดรเวอร์การ์ดจอออนบอร์ด (iGPU) ได้ หากคุณใช้การ์ดจอแยก (dGPU) เป็นหลักอยู่แล้ว
- เปิดใช้งาน `High-Bandwidth Digital Content Protection (HDCP)` หากคุณใช้รับชม **เนื้อหาที่ติดลิขสิทธิ์ DRM** เช่น `Netflix` เป็นต้น
- ปิดใช้งาน `High-Definition Multimedia Interface (HDMI)/DisplayPort (DP) Audio` หากคุณไม่ได้เชื่อมต่อหูฟังหรือลำโพงผ่านหน้าจอโดยตรง
- นำเข้าโปรไฟล์ `MSI Afterburner Profile` ที่ตั้งค่าไว้ล่วงหน้า (ระบุหรือไม่ก็ได้)
- เปิดใช้งานหัวข้อ `OBS Studio` หากต้องการใช้ระบบบันทึก **คลิปวิดีโอย้อนหลัง** (ความยาว `30 วินาที` โดยกดปุ่ม `Alt + F10`)

![Graphics Cards](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Graphics%20Cards.png)
![Graphics Cards2](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Graphics%20Cards2.png)

### ความปลอดภัยของ Windows (Windows Security)
ตั้งค่าเกี่ยวกับระบบความปลอดภัยของ Windows:
- เปิดใช้งาน `HVCI` และ `VBS` หากคุณเป็นผู้เล่นเกมที่ใช้ระบบป้องกันการโกงขั้นสูงอย่าง `FACEIT` หรือ `Valorant`

![Windows Security](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Windows%20Security.png)

### ติดตั้ง AutoOS (Install AutoOS)
เริ่มต้นขั้นตอนการติดตั้ง:
- คลิกที่ `ลิงก์เชื่อมโยง (hyperlink)` ในหน้าต่างแอปเพื่อเปิด `Windows Security` ขึ้นมา จากนั้นทำการปิดใช้งานระบบ `Real-time protection` และ `Tamper protection`
- หลังจากนั้นทุกอย่างจะถูกกำหนดค่าและติดตั้ง **โดยอัตโนมัติ**
- ขั้นตอนนี้มักจะใช้เวลาประมาณ **15-45 นาที** ขึ้นอยู่กับความเร็วอินเทอร์เน็ตของคุณ
- ในบางแอปพลิเคชันอาจมีหน้าต่างเด้งขึ้นมาให้คุณล็อกอิน หากต้องการข้ามสามารถปิดหน้าต่างของแอปนั้น ๆ ไปได้เลย

> [!NOTE]  
> คุณอาจพบอาการหน้าต่างของแอปแสดงผลเป็นสีดำหรือว่างเปล่าหลังจากทำการติดตั้งไดรเวอร์การ์ดจอเสร็จสิ้น<br/>
> วิธีการแก้ไข: ให้ทำการปรับขนาดหน้าต่างแอป (Resize), คลิกเปิด-ปิดปุ่มแถบนำทางสัก 2-3 ครั้ง หรือปล่อยทิ้งไว้สักครู่เพื่อให้ระบบทำการเรนเดอร์หน้าจอใหม่อีกครั้ง

> [!IMPORTANT]
> หลังจากที่ตัวติดตั้ง AutoOS Installer ทำการติดตั้งจนเสร็จสิ้นและรีสตาร์ทคอมพิวเตอร์ของคุณแล้ว ให้เปิดแอป `AutoOS` ขึ้นมา จากนั้นคลิกเลือกที่กล่องข้อความ `AutoOS User Guide` บนแท็บ Home และทำตาม **คำแนะนำการตั้งค่า** ในส่วนของ `AutoOS Settings`

![Install AutoOS](https://raw.githubusercontent.com/tinodin/AutoOS-Resources/main/AutoOS%20Installer/Install%20AutoOS.png)

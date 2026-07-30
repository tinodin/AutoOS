# 📜 License & Credits

## 🙏 Credits

**ghost1372 (Mahdi Hosseini):**  
Thank you for creating [DevWinUI](https://github.com/ghost1372/DevWinUI). It inspired me to learn C# and rewrite this project in WinUI 3. I appreciate your quick responses, fixes to issues, and the helpful [workflow file](https://github.com/ghost1372/DevWinUI/blob/main/.github/workflows/publish-release.yml), which I adapted for this project.

---

**rgl (Rui Lopes):**  
Thank you for creating [uup-dump-get-windows-iso](https://github.com/rgl/uup-dump-get-windows-iso), which I adapted to automatically build the latest Windows release in order to speed up and simplify AutoOS installation.

---

**cschneegans (Christoph Schneegans):**  
Thank you for creating [unattend-generator](https://github.com/cschneegans/unattend-generator), which helps AutoOS installation to be seamless.

---

**m417z (Michael Maltsev):**  
Thank you for creating [Windhawk](https://github.com/ramensoftware/windhawk) and for helping me to publish my mod [Auto Theme Switcher](https://windhawk.net/mods/auto-theme-switcher).

---

**Imribiy:**  
Thank you for creating [AMD GPU Tweaks](https://github.com/imribiy/amd-gpu-tweaks).  

## 📜 License

This project is licensed under the **GNU General Public License v3.0**. See the `LICENSE` file for details.

### Third-Party Components

#### Reference code used:
1. **LowAudioLatency**
   - Licensed under the **MIT License**.
   - Source: [spddl/LowAudioLatency](https://github.com/spddl/LowAudioLatency)

2. **GoInterruptPolicy**
   - Licensed under the **MIT License**.
   - Source: [spddl/GoInterruptPolicy](https://github.com/spddl/GoInterruptPolicy)

#### Binaries included:
1. **nvidiaProfileInspector**
   - Licensed under the **MIT License**.
   - Source: [Orbmu2k/nvidiaProfileInspector](https://github.com/Orbmu2k/nvidiaProfileInspector)

2. **RadeonSoftwareSlimmer**
   - Licensed under the **GNU General Public License v3.0**.
   - Source: [GSDragoon/RadeonSoftwareSlimmer](https://github.com/GSDragoon/RadeonSoftwareSlimmer)
   - Changes: Added command line options for preinstall
   - Fork: [tinodin/RadeonSoftwareSlimmer](https://github.com/tinodin/RadeonSoftwareSlimmer)

3. **PresentMon**
   - Licensed under the **MIT License**.
   - Source: [GameTechDev/PresentMon](https://github.com/GameTechDev/PresentMon)

4. **Service List Builder**
   - Licensed under the **GNU General Public License v3.0**.
   - Source: [valleyofdoom/service-list-builder](https://github.com/valleyofdoom/service-list-builder)
   - Changes: Removed `shutdown /r /t 0` from created lists, added `--output-dir` switch because of MSIX restrictions.
   - Fork: [tinodin/service-list-builder](https://github.com/tinodin/service-list-builder)

5. **ClassicWindowSwitcher**
   - Licensed under the **GNU General Public License v2.0**.
   - Source: [Ingan121/ClassicWindowSwitcher](https://github.com/Ingan121/ClassicWindowSwitcher)

6. **Custom Resolution Utility (CRU)**
```
Copyright (C) 2012-2022 ToastyX
https://monitortests.com/custom-resolution-utility

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software the rights to use, copy, and/or distribute copies of the
software subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies of the software.

THE SOFTWARE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY CLAIM, DAMAGES, OR
OTHER LIABILITY IN CONNECTION WITH THE USE OF THE SOFTWARE.
```
- Source: [Custom Resolution Utility (CRU)](https://monitortests.com/custom-resolution-utility)

7. **7-Zip**
```
  7-Zip
  ~~~~~
  License for use and distribution
  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

  7-Zip Copyright (C) 1999-2025 Igor Pavlov.

  The licenses for files are:

    - 7z.dll:
         - The "GNU LGPL" as main license for most of the code
         - The "GNU LGPL" with "unRAR license restriction" for some code
         - The "BSD 3-clause License" for some code
         - The "BSD 2-clause License" for some code
    - All other files: the "GNU LGPL".

  Redistributions in binary form must reproduce related license information from this file.

  Note:
    You can use 7-Zip on any computer, including a computer in a commercial
    organization. You don't need to register or pay for 7-Zip.


GNU LGPL information
--------------------

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You can receive a copy of the GNU Lesser General Public License from
    http://www.gnu.org/




BSD 3-clause License in 7-Zip code
----------------------------------

  The "BSD 3-clause License" is used for the following code in 7z.dll
    1) LZFSE data decompression.
       That code was derived from the code in the "LZFSE compression library" developed by Apple Inc,
       that also uses the "BSD 3-clause License".
    2) ZSTD data decompression.
       that code was developed using original zstd decoder code as reference code.
       The original zstd decoder code was developed by Facebook Inc,
       that also uses the "BSD 3-clause License".

  Copyright (c) 2015-2016, Apple Inc. All rights reserved.
  Copyright (c) Facebook, Inc. All rights reserved.
  Copyright (c) 2023-2025 Igor Pavlov.

Text of the "BSD 3-clause License"
----------------------------------

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors may
   be used to endorse or promote products derived from this software without
   specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

---




BSD 2-clause License in 7-Zip code
----------------------------------

  The "BSD 2-clause License" is used for the XXH64 code in 7-Zip.

  XXH64 code in 7-Zip was derived from the original XXH64 code developed by Yann Collet.

  Copyright (c) 2012-2021 Yann Collet.
  Copyright (c) 2023-2025 Igor Pavlov.

Text of the "BSD 2-clause License"
----------------------------------

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

---




unRAR license restriction
-------------------------

The decompression engine for RAR archives was developed using source
code of unRAR program.
All copyrights to original unRAR code are owned by Alexander Roshal.

The license for original unRAR code has the following restriction:

  The unRAR sources cannot be used to re-create the RAR compression algorithm,
  which is proprietary. Distribution of modified unRAR sources in separate form
  or as a part of other software is permitted, provided that it is clearly
  stated in the documentation and source comments that the code may
  not be used to develop a RAR (WinRAR) compatible archiver.

--
```
- Source: [7-Zip](https://www.7-zip.org)

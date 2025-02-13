@echo off
setlocal ENABLEDELAYEDEXPANSION
if "%INTERFACE%"=="" for /f "tokens=3,*" %%i in ('netsh int show interface ^| find "Connected"') do set INTERFACE=%%j
if "%IP%"=="" for /f "tokens=3 delims=: " %%i in ('netsh int ip show config name^="%INTERFACE%" ^| findstr "IP Address" ^| findstr [0-9]') do set IP=%%i
if "%MASK%"=="" for /f "tokens=2 delims=()" %%i in ('netsh int ip show config name^="%INTERFACE%" ^| findstr /r "(.*)"') do for %%j in (%%i) do set MASK=%%j
if "%GATEWAY%"=="" for /f "tokens=3 delims=: " %%i in ('netsh int ip show config name^="%INTERFACE%" ^| findstr "Default" ^| findstr [0-9]') do set GATEWAY=%%i
set DNS1=%GATEWAY%
set DNS2=8.8.8.8
netsh int ipv4 set address name="%INTERFACE%" static %IP% %MASK% %GATEWAY% >nul 2>&1
netsh int ipv4 set dns name="%INTERFACE%" static %DNS1% primary >nul 2>&1
netsh int ipv4 add dns name="%INTERFACE%" %DNS2% index=2 >nul 2>&1

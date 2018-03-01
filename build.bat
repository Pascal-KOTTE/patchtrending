
@echo off

set ans=Altiris.NS
set acm=Altiris.Common
set adb=Altiris.Database

if "%1"=="8.1" goto build-8.1
if "%1"=="8.0" goto build-8.0
if "%1"=="7.6" goto build-7.6
if "%1"=="7.5" goto build-7.5
if "%1"=="7.1" goto build-7.1


:default build path

:build-8.1

set build=8.1
set gac=C:\Windows\Microsoft.NET\assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
set ver1=v4.0_8.1.4528.0__d516cb311cfb6e4f

goto build


:build-8.0

set build=8.0
set gac=C:\Windows\Microsoft.NET\assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
set ver1=v4.0_8.0.2298.0__d516cb311cfb6e4f

goto build


:build-7.6

set build=7.6
set gac=C:\Windows\Microsoft.NET\assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
set ver1=v4.0_7.6.1383.0__d516cb311cfb6e4f

goto build


:build-7.5

set build=7.5
set gac=C:\Windows\Assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe
set ver1=7.5.3153.0__d516cb311cfb6e4f

goto build


:build-7.1

set build=7.1
set gac=C:\Windows\Assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe
set ver1=7.1.8400.0__d516cb311cfb6e4f


:build
cmd /c %csc% /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /out:PatchTrending-%build%.exe *.cs


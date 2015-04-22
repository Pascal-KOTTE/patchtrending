
@echo off

set ans=Altiris.NS
set acm=Altiris.Common
set adb=Altiris.Database


if "%1"=="7.1" goto build-7.1
if "%1"=="7.6" goto build-7.6


:default build path
:build-7.5
set build=7.5

set gac=C:\Windows\Assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe

set ver1=7.5.3153.0__d516cb311cfb6e4f
set ver2=7.5.3219.0__d516cb311cfb6e4f
set ver3=7.5.3153.0__99b1e4cc0d03f223


goto build

:build-7.1
set build=7.1

set gac=C:\Windows\Assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe

set ver1=7.1.8400.0__d516cb311cfb6e4f
set ver2=7.5.3219.0__d516cb311cfb6e4f
set ver3=7.5.3153.0__99b1e4cc0d03f223


goto build


:build-7.6
set build=7.6

set gac=C:\Windows\Microsoft.NET\assembly\GAC_MSIL
set csc=@c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

set ver1=v4.0_7.6.1383.0__d516cb311cfb6e4f
set ver2=v4.0_7.6.1395.0__d516cb311cfb6e4f
set ver3=v4.0_7.6.1383.0__99b1e4cc0d03f223



:build
cmd /c %csc% /reference:%gac%\%ans%\%ver1%\%ans%.dll /reference:%gac%\%acm%\%ver1%\%acm%.dll /reference:%gac%\%adb%\%ver1%\%adb%.dll /out:PatchTrending-%build%.exe *.cs

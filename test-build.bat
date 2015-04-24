@echo off
cmd /c build.bat

if not exist "test-site" (mkdir "test-site")

copy /y siteconfig.txt test-site
copy /y site-layout.txt test-site
copy /y patchtrending-7.5.exe test-site

cd test-site

cmd /c patchtrending-7.5.exe /collectdata
cmd /c patchtrending-7.5.exe
cd ..

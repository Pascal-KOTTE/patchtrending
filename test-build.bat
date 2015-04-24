@echo off
cmd /c build.bat
copy /y siteconfig.txt test-site
copy /y patchtrending-7.5.exe test-site

if not exist test-site then mkdir test-site
cd test-site

cd
cmd /c patchtrending-7.5.exe /collectdata
cmd /c patchtrending-7.5.exe

cd ..

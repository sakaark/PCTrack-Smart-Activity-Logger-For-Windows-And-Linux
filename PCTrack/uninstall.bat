@echo off
del "C:\Users\%username%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\PCTrack.lnk"
Registry\deregistry.exe
echo Apllication successfully uninstalled from your computer.
PAUSE

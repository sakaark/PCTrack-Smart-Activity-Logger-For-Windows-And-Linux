@echo off
Registry\registry.exe
IF ERRORLEVEL 1 (
echo This application is already installed on your computer.
PAUSE
EXIT
)
echo Set oWS = WScript.CreateObject("WScript.Shell") > CreateShortcut.vbs
echo sLinkFile = "C:\Users\%username%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\PCTrack.lnk" >> CreateShortcut.vbs
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> CreateShortcut.vbs
echo oLink.TargetPath = "%CD%\AppFiles\PCTrack.exe" >> CreateShortcut.vbs
echo oLink.WorkingDirectory = "%CD%\AppFiles" >> CreateShortcut.vbs
echo oLink.Save >> CreateShortcut.vbs
cscript CreateShortcut.vbs
del CreateShortcut.vbs

cd AppFiles
start PCTrack.exe
echo Apllication successfully installed on your computer.
PAUSE
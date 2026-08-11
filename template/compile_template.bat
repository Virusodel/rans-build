@echo off
echo Compiling locker_payload.exe...
cl /EHsc /O2 /MT locker_payload.cpp user32.lib kernel32.lib advapi32.lib
if exist locker_payload.exe (
    move locker_payload.exe ..\builder\Resources\template.exe
    echo Done! Template saved to builder\Resources\template.exe
) else (
    echo Compilation failed!
)
pause

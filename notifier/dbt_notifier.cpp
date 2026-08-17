#include <windows.h>
#include <winioctl.h>
#include <stdio.h>
#include <string>
#include <thread>
#include <chrono>
#include <tlhelp32.h>
#include <psapi.h>
#include <shlwapi.h>

#pragma comment(lib, "psapi.lib")
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "user32.lib")

#define IOCTL_GET_ATTEMPTS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_GET_BLOCK_INFO CTL_CODE(FILE_DEVICE_UNKNOWN, 0x802, METHOD_BUFFERED, FILE_ANY_ACCESS)

typedef struct _BLOCK_INFO {
    ULONG64 BlockedCount;
    ULONG PID;
    CHAR ProcessName[256];
} BLOCK_INFO;

HANDLE FindFirstDevice() {
    WCHAR deviceName[64];
    HANDLE hDevice;
    
    for (int i = 0; i < 64; i++) {
        swprintf(deviceName, 64, L"\\\\.\\DbtMbrProtector_%d", i);
        hDevice = CreateFileW(deviceName, GENERIC_READ | GENERIC_WRITE,
                              0, NULL, OPEN_EXISTING, 0, NULL);
        if (hDevice != INVALID_HANDLE_VALUE) {
            wprintf(L"Connected to: %s\n", deviceName);
            return hDevice;
        }
    }
    
    return INVALID_HANDLE_VALUE;
}

DWORD WINAPI MonitorThread(LPVOID param) {
    HANDLE hDevice = FindFirstDevice();
    
    if (hDevice == INVALID_HANDLE_VALUE) {
        MessageBoxW(NULL,
            L"Failed to open DBT MBR Protector device!\n\n"
            L"Make sure the driver is installed and running.\n"
            L"Try running as Administrator.",
            L"Notifier Error", MB_ICONERROR | MB_OK);
        return 1;
    }
    
    ULONG64 lastAttempts = 0;
    ULONG64 currentAttempts = 0;
    DWORD bytesReturned;
    
    while (true) {
        if (DeviceIoControl(hDevice, IOCTL_GET_ATTEMPTS, NULL, 0,
                            &currentAttempts, sizeof(ULONG64), &bytesReturned, NULL)) {
            
            if (currentAttempts > lastAttempts) {
                BLOCK_INFO blockInfo = {0};
                
                if (DeviceIoControl(hDevice, IOCTL_GET_BLOCK_INFO, NULL, 0,
                                    &blockInfo, sizeof(BLOCK_INFO), &bytesReturned, NULL)) {
                    
                    std::string processNameStr(blockInfo.ProcessName);
                    std::wstring processNameW(processNameStr.begin(), processNameStr.end());
                    
                    WCHAR msg[1024];
                    wsprintfW(msg,
                        L"DBT MBR Protector\n\n"
                        L"BLOCKED MBR WRITE ATTEMPT #%llu\n\n"
                        L"Process: %s (PID: %lu)\n"
                        L"Drive: PhysicalDrive\n"
                        L"Action: Denied (STATUS_ACCESS_DENIED)\n\n"
                        L"Total blocked attempts: %llu\n\n"
                        L"This attempt was intercepted and blocked at kernel level.",
                        currentAttempts - lastAttempts,
                        processNameW.c_str(), blockInfo.PID,
                        currentAttempts);
                    
                    MessageBoxW(NULL, msg, L"DBT MBR Protector Alert",
                               MB_OK | MB_ICONWARNING | MB_SYSTEMMODAL | MB_TOPMOST);
                }
                
                lastAttempts = currentAttempts;
            }
        }
        Sleep(1000);
    }
    
    CloseHandle(hDevice);
    return 0;
}

BOOL IsElevated() {
    BOOL isElevated = FALSE;
    HANDLE hToken = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
        TOKEN_ELEVATION elevation = {0};
        DWORD size = sizeof(TOKEN_ELEVATION);
        if (GetTokenInformation(hToken, TokenElevation, &elevation, size, &size)) {
            isElevated = elevation.TokenIsElevated;
        }
        CloseHandle(hToken);
    }
    return isElevated;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    if (!IsElevated()) {
        MessageBoxW(NULL,
            L"DBT MBR Protector Notifier requires Administrator privileges.\n"
            L"Please run as Administrator.",
            L"Notifier", MB_ICONERROR | MB_OK);
        return 1;
    }
    
    MessageBoxW(NULL,
        L"DBT MBR Protector Notifier\n\n"
        L"Monitoring MBR write attempts...\n"
        L"Will show alert on any block.\n"
        L"Running in background.",
        L"DBT Monitor", MB_OK | MB_ICONINFORMATION);
    
    HANDLE hThread = CreateThread(NULL, 0, MonitorThread, NULL, 0, NULL);
    if (hThread) {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }
    
    return 0;
}

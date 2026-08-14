#include <windows.h>
#include <winioctl.h>
#include <stdio.h>
#include <string>
#include <vector>
#include <thread>
#include <chrono>

#define IOCTL_GET_ATTEMPTS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

std::wstring GetProcessNameFromId(DWORD pid) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (!hProcess) return L"Unknown";
    
    WCHAR processName[MAX_PATH] = {0};
    DWORD size = MAX_PATH;
    if (QueryFullProcessImageNameW(hProcess, 0, processName, &size)) {
        CloseHandle(hProcess);
        return std::wstring(processName);
    }
    
    // Fallback: get from PEB
    HMODULE hMod;
    DWORD cbNeeded;
    if (EnumProcessModules(hProcess, &hMod, sizeof(hMod), &cbNeeded)) {
        GetModuleBaseNameW(hProcess, hMod, processName, MAX_PATH);
    }
    
    CloseHandle(hProcess);
    return std::wstring(processName);
}

DWORD WINAPI MonitorThread(LPVOID param) {
    HANDLE hDevice = CreateFileW(L"\\\\.\\DbtMbrProtector", GENERIC_READ | GENERIC_WRITE,
                                 0, NULL, OPEN_EXISTING, 0, NULL);
    
    if (hDevice == INVALID_HANDLE_VALUE) {
        MessageBoxA(NULL, "Failed to open DBT MBR Protector device!", "Error", MB_ICONERROR);
        return 1;
    }
    
    ULONG64 lastAttempts = 0;
    ULONG64 currentAttempts = 0;
    DWORD bytesReturned;
    
    while (true) {
        if (DeviceIoControl(hDevice, IOCTL_GET_ATTEMPTS, NULL, 0,
                            &currentAttempts, sizeof(ULONG64), &bytesReturned, NULL)) {
            
            if (currentAttempts > lastAttempts) {
                // Блокировка произошла
                ULONG64 blockedCount = currentAttempts - lastAttempts;
                
                // Получение текущего процесса (упрощенно)
                HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                if (hSnapshot != INVALID_HANDLE_VALUE) {
                    PROCESSENTRY32 pe32 = { sizeof(PROCESSENTRY32) };
                    if (Process32First(hSnapshot, &pe32)) {
                        do {
                            if (pe32.th32ProcessID == GetCurrentProcessId()) {
                                // Информация о блокировке
                                WCHAR msg[512];
                                wsprintfW(msg, 
                                    L"DBT MBR Protector\n\n"
                                    L"Blocked MBR write attempt #%llu\n"
                                    L"Process: %s (PID: %lu)\n"
                                    L"Drive: PhysicalDrive\n"
                                    L"Action: Denied\n\n"
                                    L"Total blocked attempts: %llu",
                                    blockedCount, pe32.szExeFile, pe32.th32ProcessID, currentAttempts);
                                
                                MessageBoxW(NULL, msg, L"DBT MBR Protector Alert", 
                                           MB_OK | MB_ICONWARNING | MB_SYSTEMMODAL);
                                break;
                            }
                        } while (Process32Next(hSnapshot, &pe32));
                    }
                    CloseHandle(hSnapshot);
                }
                
                lastAttempts = currentAttempts;
            }
        }
        Sleep(1000); // Проверка каждую секунду
    }
    
    CloseHandle(hDevice);
    return 0;
}

int main() {
    MessageBoxW(NULL, L"DBT MBR Protector Notifier started.\nMonitoring MBR write attempts...", 
                L"DBT Monitor", MB_OK | MB_ICONINFORMATION);
    
    HANDLE hThread = CreateThread(NULL, 0, MonitorThread, NULL, 0, NULL);
    if (hThread) {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }
    
    return 0;
}

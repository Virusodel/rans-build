#include <windows.h>
#include <winioctl.h>
#include <stdio.h>
#include <string>
#include <vector>
#include <thread>
#include <chrono>
#include <tlhelp32.h>      // Для CreateToolhelp32Snapshot
#include <psapi.h>         // Для EnumProcessModules, GetModuleBaseNameW
#include <shlwapi.h>       // Для PathFindFileNameW

#pragma comment(lib, "psapi.lib")
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "user32.lib")

#define IOCTL_GET_ATTEMPTS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Получение имени процесса по PID
std::wstring GetProcessNameFromId(DWORD pid) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (!hProcess) return L"Unknown";
    
    WCHAR processName[MAX_PATH] = {0};
    DWORD size = MAX_PATH;
    
    // Способ 1: через QueryFullProcessImageName
    if (QueryFullProcessImageNameW(hProcess, 0, processName, &size)) {
        CloseHandle(hProcess);
        // Извлекаем только имя файла (без пути)
        return std::wstring(PathFindFileNameW(processName));
    }
    
    // Способ 2: через GetModuleBaseName (для старых Windows)
    HMODULE hMod;
    DWORD cbNeeded;
    if (EnumProcessModules(hProcess, &hMod, sizeof(hMod), &cbNeeded)) {
        if (GetModuleBaseNameW(hProcess, hMod, processName, MAX_PATH)) {
            CloseHandle(hProcess);
            return std::wstring(processName);
        }
    }
    
    CloseHandle(hProcess);
    return L"Unknown";
}

// Получение текущего процесса из Toolhelp32Snapshot
std::wstring GetCurrentProcessNameFromSnapshot() {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) return L"Unknown";
    
    PROCESSENTRY32W pe32;
    pe32.dwSize = sizeof(PROCESSENTRY32W);
    DWORD currentPid = GetCurrentProcessId();
    
    if (Process32FirstW(hSnapshot, &pe32)) {
        do {
            if (pe32.th32ProcessID == currentPid) {
                CloseHandle(hSnapshot);
                return std::wstring(pe32.szExeFile);
            }
        } while (Process32NextW(hSnapshot, &pe32));
    }
    
    CloseHandle(hSnapshot);
    return L"Unknown";
}

DWORD WINAPI MonitorThread(LPVOID param) {
    // Попытка открыть устройство драйвера
    HANDLE hDevice = CreateFileW(L"\\\\.\\DbtMbrProtector", 
                                 GENERIC_READ | GENERIC_WRITE,
                                 0, NULL, OPEN_EXISTING, 0, NULL);
    
    if (hDevice == INVALID_HANDLE_VALUE) {
        MessageBoxA(NULL, "Failed to open DBT MBR Protector device!\n"
                         "Make sure the driver is installed and running.", 
                    "DBT Notifier Error", MB_ICONERROR | MB_OK);
        return 1;
    }
    
    ULONG64 lastAttempts = 0;
    ULONG64 currentAttempts = 0;
    DWORD bytesReturned;
    
    while (true) {
        if (DeviceIoControl(hDevice, IOCTL_GET_ATTEMPTS, NULL, 0,
                            &currentAttempts, sizeof(ULONG64), &bytesReturned, NULL)) {
            
            if (currentAttempts > lastAttempts) {
                // Новая блокировка произошла
                ULONG64 blockedCount = currentAttempts - lastAttempts;
                
                // Получаем имя текущего процесса (который вызвал блокировку)
                std::wstring processName = GetCurrentProcessNameFromSnapshot();
                DWORD pid = GetCurrentProcessId();
                
                // Формируем сообщение
                WCHAR msg[1024];
                wsprintfW(msg, 
                    L"DBT MBR Protector\n\n"
                    L"⚠ BLOCKED MBR WRITE ATTEMPT #%llu\n\n"
                    L"Process: %s (PID: %lu)\n"
                    L"Drive: PhysicalDrive\n"
                    L"Action: Denied (STATUS_ACCESS_DENIED)\n\n"
                    L"Total blocked attempts: %llu\n\n"
                    L"This attempt was intercepted and blocked at kernel level.",
                    blockedCount, 
                    processName.c_str(), 
                    pid, 
                    currentAttempts);
                
                // Показываем MessageBox (поверх всех окон)
                MessageBoxW(NULL, msg, L"DBT MBR Protector Alert", 
                           MB_OK | MB_ICONWARNING | MB_SYSTEMMODAL | MB_TOPMOST);
                
                lastAttempts = currentAttempts;
            }
        } else {
            // Если IOCTL не сработал, возможно драйвер выгружен
            DWORD err = GetLastError();
            if (err != ERROR_SUCCESS) {
                // Не спамим ошибками, просто ждем
            }
        }
        
        Sleep(1000); // Проверка каждую секунду
    }
    
    CloseHandle(hDevice);
    return 0;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    // Проверка прав администратора
    BOOL isAdmin = FALSE;
    PSID adminGroup = NULL;
    SID_IDENTIFIER_AUTHORITY ntAuthority = SECURITY_NT_AUTHORITY;
    if (AllocateAndInitializeSid(&ntAuthority, 2, SECURITY_BUILTIN_DOMAIN_RID,
                                 DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &adminGroup)) {
        CheckTokenMembership(NULL, adminGroup, &isAdmin);
        FreeSid(adminGroup);
    }
    
    if (!isAdmin) {
        MessageBoxA(NULL, "DBT MBR Protector Notifier\n\n"
                         "This application requires Administrator privileges.\n"
                         "Please run as Administrator.", 
                    "DBT Notifier", MB_ICONWARNING | MB_OK);
        return 1;
    }
    
    // Показываем стартовое сообщение
    MessageBoxW(NULL, 
        L"DBT MBR Protector Notifier\n\n"
        L"✓ Monitoring MBR write attempts\n"
        L"✓ Will show alert on any block\n"
        L"✓ Running in background\n\n"
        L"Click OK to minimize to system tray.",
        L"DBT Monitor", MB_OK | MB_ICONINFORMATION);
    
    // Запускаем поток мониторинга
    HANDLE hThread = CreateThread(NULL, 0, MonitorThread, NULL, 0, NULL);
    if (hThread) {
        // Ждем бесконечно (поток сам работает)
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }
    
    return 0;
}

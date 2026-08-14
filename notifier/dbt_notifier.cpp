#include <windows.h>
#include <winioctl.h>
#include <stdio.h>
#include <string>
#include <vector>
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

// Языковые ресурсы
struct LanguageStrings {
    const wchar_t* Title;
    const wchar_t* ErrorMsg;
    const wchar_t* StartMsg;
    const wchar_t* AlertTitle;
    const wchar_t* AlertHeader;
    const wchar_t* AlertProcess;
    const wchar_t* AlertDrive;
    const wchar_t* AlertAction;
    const wchar_t* AlertTotal;
    const wchar_t* AlertNote;
    const wchar_t* NotAdminMsg;
};

LanguageStrings GetStrings(int lang) {
    LanguageStrings eng = {
        L"DBT Notifier Error",
        L"Failed to open DBT MBR Protector device!\n\nMake sure the driver is installed and running.",
        L"DBT MBR Protector Notifier\n\nMonitoring MBR write attempts\nWill show alert on any block\nRunning in background\n\nClick OK to minimize to system tray.",
        L"DBT MBR Protector Alert",
        L"BLOCKED MBR WRITE ATTEMPT #%llu",
        L"Process: %s (PID: %lu)",
        L"Drive: PhysicalDrive",
        L"Action: Denied (STATUS_ACCESS_DENIED)",
        L"Total blocked attempts: %llu",
        L"This attempt was intercepted and blocked at kernel level.",
        L"This application requires Administrator privileges.\nPlease run as Administrator."
    };
    
    LanguageStrings rus = {
        L"Ошибка DBT Notifier",
        L"Не удалось открыть устройство DBT MBR Protector!\n\nУбедитесь, что драйвер установлен и запущен.",
        L"DBT MBR Protector Notifier\n\nМониторинг попыток записи в MBR\nПоказывает предупреждения при блокировке\nРаботает в фоновом режиме\n\nНажмите OK для сверки в системный трей.",
        L"DBT MBR Protector Предупреждение",
        L"БЛОКИРОВКА ЗАПИСИ В MBR #%llu",
        L"Процесс: %s (PID: %lu)",
        L"Диск: PhysicalDrive",
        L"Действие: Отказано (STATUS_ACCESS_DENIED)",
        L"Всего заблокировано: %llu",
        L"Эта попытка была перехвачена на уровне ядра.",
        L"Это приложение требует прав администратора.\nЗапустите от имени администратора."
    };
    
    return (lang == 1) ? rus : eng;
}

int GetSystemLanguage() {
    LANGID lang = GetUserDefaultUILanguage();
    if (lang == 0x0419 || lang == 0x041A || lang == 0x0422 || lang == 0x0822) {
        return 1; // Russian
    }
    return 0; // English
}

std::wstring GetProcessNameFromId(DWORD pid) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, pid);
    if (!hProcess) return L"Unknown";
    
    WCHAR processName[MAX_PATH] = {0};
    DWORD size = MAX_PATH;
    
    if (QueryFullProcessImageNameW(hProcess, 0, processName, &size)) {
        CloseHandle(hProcess);
        return std::wstring(PathFindFileNameW(processName));
    }
    
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
    int lang = *(int*)param;
    LanguageStrings str = GetStrings(lang);
    
    HANDLE hDevice = CreateFileW(L"\\\\.\\DbtMbrProtector", 
                                 GENERIC_READ | GENERIC_WRITE,
                                 0, NULL, OPEN_EXISTING, 0, NULL);
    
    if (hDevice == INVALID_HANDLE_VALUE) {
        DWORD err = GetLastError();
        WCHAR msg[512];
        wsprintfW(msg, L"%s\n\nError code: 0x%08X (%lu)", 
                  str.ErrorMsg, err, err);
        MessageBoxW(NULL, msg, str.Title, MB_ICONERROR | MB_OK);
        return 1;
    }
    
    ULONG64 lastAttempts = 0;
    ULONG64 currentAttempts = 0;
    DWORD bytesReturned;
    
    while (true) {
        if (DeviceIoControl(hDevice, IOCTL_GET_ATTEMPTS, NULL, 0,
                            &currentAttempts, sizeof(ULONG64), &bytesReturned, NULL)) {
            
            if (currentAttempts > lastAttempts) {
                ULONG64 blockedCount = currentAttempts - lastAttempts;
                std::wstring processName = GetCurrentProcessNameFromSnapshot();
                DWORD pid = GetCurrentProcessId();
                
                WCHAR msg[1024];
                wsprintfW(msg, 
                    L"DBT MBR Protector\n\n"
                    L"%s\n\n"
                    L"%s\n"
                    L"%s\n"
                    L"   %s\n"
                    L"%s\n\n"
                    L"%s",
                    str.AlertHeader, blockedCount,
                    str.AlertProcess, processName.c_str(), pid,
                    str.AlertDrive,
                    str.AlertAction,
                    str.AlertTotal, currentAttempts,
                    str.AlertNote);
                
                MessageBoxW(NULL, msg, str.AlertTitle, 
                           MB_OK | MB_ICONWARNING | MB_SYSTEMMODAL | MB_TOPMOST);
                
                lastAttempts = currentAttempts;
            }
        }
        Sleep(1000);
    }
    
    CloseHandle(hDevice);
    return 0;
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    // Определение языка
    int lang = GetSystemLanguage();
    LanguageStrings str = GetStrings(lang);
    
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
    
    MessageBoxW(NULL, str.StartMsg, L"DBT Monitor", MB_OK | MB_ICONINFORMATION);
    
    // Запуск мониторинга
    HANDLE hThread = CreateThread(NULL, 0, MonitorThread, &lang, 0, NULL);
    if (hThread) {
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);
    }
    
    return 0;
}

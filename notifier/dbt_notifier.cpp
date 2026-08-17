#include <windows.h>

#define NOTIFY_EVENT_NAME L"Global\\DbtMbrProtectorEvent"

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    BOOL isAdmin = FALSE;
    HANDLE hToken = NULL;
    
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
        TOKEN_ELEVATION elevation = {0};
        DWORD size = sizeof(TOKEN_ELEVATION);
        if (GetTokenInformation(hToken, TokenElevation, &elevation, size, &size)) {
            isAdmin = elevation.TokenIsElevated;
        }
        CloseHandle(hToken);
    }
    
    if (!isAdmin) {
        MessageBoxW(NULL, 
            L"DBT MBR Protector Notifier requires Administrator privileges.\n"
            L"Please run as Administrator.",
            L"Notifier", MB_ICONERROR | MB_OK);
        return 1;
    }
    
    HANDLE hEvent = OpenEventW(SYNCHRONIZE, FALSE, NOTIFY_EVENT_NAME);
    if (!hEvent) {
        MessageBoxW(NULL, 
            L"Failed to open notification event!\n\n"
            L"Make sure the driver is installed and running.",
            L"Notifier Error", MB_ICONERROR | MB_OK);
        return 1;
    }
    
    MessageBoxW(NULL, 
        L"DBT MBR Protector Notifier\n\n"
        L"Monitoring MBR write attempts...\n"
        L"Will show alert on any block.\n"
        L"Running in background.",
        L"DBT Monitor", MB_OK | MB_ICONINFORMATION);
    
    while (true) {
        DWORD result = WaitForSingleObject(hEvent, INFINITE);
        
        if (result == WAIT_OBJECT_0) {
            MessageBoxW(NULL, 
                L"DBT MBR Protector\n\n"
                L"BLOCKED MBR WRITE ATTEMPT!\n\n"
                L"A program attempted to overwrite the MBR.\n"
                L"This attempt was intercepted and blocked at kernel level.",
                L"DBT MBR Protector Alert",
                MB_OK | MB_ICONWARNING | MB_SYSTEMMODAL | MB_TOPMOST);
            
            ResetEvent(hEvent);
        }
    }
    
    CloseHandle(hEvent);
    return 0;
}

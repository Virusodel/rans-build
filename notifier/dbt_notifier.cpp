#include <windows.h>
#include <wtsapi32.h>
#include <stdio.h>

#pragma comment(lib, "wtsapi32.lib")

#define SERVICE_NAME L"DBTNotifierService"
#define NOTIFY_EVENT_NAME L"Global\\DbtMbrProtectorEvent"

SERVICE_STATUS g_ServiceStatus = {0};
SERVICE_STATUS_HANDLE g_StatusHandle = NULL;
HANDLE g_StopEvent = NULL;
HANDLE g_NotifyEvent = NULL;

// Объявления функций
void WINAPI ServiceMain(DWORD argc, LPWSTR* argv);
void WINAPI ServiceCtrlHandler(DWORD ctrlCode);

void ShowPopupMessage(const wchar_t* title, const wchar_t* message) {
    DWORD sessionId = WTSGetActiveConsoleSessionId();
    if (sessionId == 0xFFFFFFFF) return;

    HANDLE hUserToken = NULL;
    if (!WTSQueryUserToken(sessionId, &hUserToken)) {
        hUserToken = NULL;
    }

    DWORD response = 0;
    WTSSendMessageW(
        WTS_CURRENT_SERVER_HANDLE,
        sessionId,
        NULL,
        0,
        (LPWSTR)title,
        wcslen(title) + 1,
        (LPWSTR)message,
        wcslen(message) + 1,
        MB_OK | MB_ICONWARNING | MB_SYSTEMMODAL | MB_TOPMOST,
        0,
        &response,
        FALSE
    );

    if (hUserToken) CloseHandle(hUserToken);
}

void ReportServiceStatus(DWORD state, DWORD exitCode, DWORD waitHint) {
    g_ServiceStatus.dwCurrentState = state;
    g_ServiceStatus.dwWin32ExitCode = exitCode;
    g_ServiceStatus.dwWaitHint = waitHint;
    SetServiceStatus(g_StatusHandle, &g_ServiceStatus);
}

void WINAPI ServiceCtrlHandler(DWORD ctrlCode) {
    switch (ctrlCode) {
        case SERVICE_CONTROL_STOP:
        case SERVICE_CONTROL_SHUTDOWN:
            ReportServiceStatus(SERVICE_STOP_PENDING, NO_ERROR, 5000);
            if (g_StopEvent) SetEvent(g_StopEvent);
            break;
        default:
            break;
    }
}

void WINAPI ServiceMain(DWORD argc, LPWSTR* argv) {
    g_StatusHandle = RegisterServiceCtrlHandlerW(SERVICE_NAME, ServiceCtrlHandler);
    if (!g_StatusHandle) return;

    g_ServiceStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
    g_ServiceStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_SHUTDOWN;
    ReportServiceStatus(SERVICE_RUNNING, NO_ERROR, 0);

    g_StopEvent = CreateEventW(NULL, TRUE, FALSE, NULL);

    g_NotifyEvent = OpenEventW(SYNCHRONIZE | EVENT_MODIFY_STATE, FALSE, NOTIFY_EVENT_NAME);
    if (!g_NotifyEvent) {
        ShowPopupMessage(L"DBT Service Error", L"Failed to open notification event!");
        ReportServiceStatus(SERVICE_STOPPED, 1, 0);
        return;
    }

    ShowPopupMessage(L"DBT Service", L"DBT MBR Protector Service started.\nMonitoring MBR write attempts...");

    HANDLE events[2] = { g_StopEvent, g_NotifyEvent };

    while (true) {
        DWORD result = WaitForMultipleObjects(2, events, FALSE, INFINITE);

        if (result == WAIT_OBJECT_0) break;
        if (result == WAIT_OBJECT_0 + 1) {
            ShowPopupMessage(L"DBT MBR Protector Alert",
                L"BLOCKED MBR WRITE ATTEMPT!\n\n"
                L"A program attempted to overwrite the MBR.\n"
                L"This attempt was intercepted and blocked at kernel level.");
            ResetEvent(g_NotifyEvent);
        }
    }

    if (g_NotifyEvent) CloseHandle(g_NotifyEvent);
    if (g_StopEvent) CloseHandle(g_StopEvent);

    ReportServiceStatus(SERVICE_STOPPED, NO_ERROR, 0);
}

void InstallService() {
    SC_HANDLE scm = OpenSCManagerW(NULL, NULL, SC_MANAGER_CREATE_SERVICE);
    if (!scm) return;

    WCHAR path[MAX_PATH];
    GetModuleFileNameW(NULL, path, MAX_PATH);

    SC_HANDLE service = CreateServiceW(
        scm,
        SERVICE_NAME,
        L"DBT MBR Protector Notifier",
        SERVICE_ALL_ACCESS,
        SERVICE_WIN32_OWN_PROCESS,
        SERVICE_AUTO_START,
        SERVICE_ERROR_NORMAL,
        path,
        NULL, NULL, NULL, NULL, NULL
    );

    if (service) {
        CloseServiceHandle(service);
        MessageBoxW(NULL, L"Service installed successfully!", L"DBT Service", MB_OK);
    } else {
        DWORD err = GetLastError();
        if (err == ERROR_SERVICE_EXISTS) {
            MessageBoxW(NULL, L"Service already exists.", L"DBT Service", MB_OK);
        } else {
            wchar_t msg[256];
            wsprintfW(msg, L"Failed to install service. Error: %lu", err);
            MessageBoxW(NULL, msg, L"DBT Service", MB_ICONERROR | MB_OK);
        }
    }

    CloseServiceHandle(scm);
}

void UninstallService() {
    SC_HANDLE scm = OpenSCManagerW(NULL, NULL, SC_MANAGER_ALL_ACCESS);
    if (!scm) return;

    SC_HANDLE service = OpenServiceW(scm, SERVICE_NAME, SERVICE_ALL_ACCESS);
    if (service) {
        SERVICE_STATUS status;
        ControlService(service, SERVICE_CONTROL_STOP, &status);

        if (DeleteService(service)) {
            MessageBoxW(NULL, L"Service uninstalled successfully!", L"DBT Service", MB_OK);
        } else {
            MessageBoxW(NULL, L"Failed to uninstall service.", L"DBT Service", MB_ICONERROR | MB_OK);
        }
        CloseServiceHandle(service);
    } else {
        MessageBoxW(NULL, L"Service not found.", L"DBT Service", MB_ICONERROR | MB_OK);
    }

    CloseServiceHandle(scm);
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    if (__argc > 1) {
        if (wcscmp(__wargv[1], L"--install") == 0) {
            InstallService();
            return 0;
        }
        if (wcscmp(__wargv[1], L"--uninstall") == 0) {
            UninstallService();
            return 0;
        }
    }

    SERVICE_TABLE_ENTRYW serviceTable[] = {
        { SERVICE_NAME, ServiceMain },
        { NULL, NULL }
    };

    StartServiceCtrlDispatcherW(serviceTable);
    return 0;
}

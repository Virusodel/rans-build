#include <ntddk.h>
#include <ntdddisk.h>
#include <wdm.h>
#include <stdlib.h>
#include <ntstrsafe.h>

#define DEVICE_NAME L"\\Device\\DbtMbrProtector"
#define SYM_LINK_NAME L"\\DosDevices\\DbtMbrProtector"
#define SECTOR_SIZE 512
#define MAX_PROCESS_NAME 256
#define MAX_DRIVE_COUNT 64
#define PROTECTED_SECTOR_START 0
#define PROTECTED_SECTOR_COUNT 10  // Защита секторов 0-9

#define IOCTL_GET_ATTEMPTS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_GET_CONFIG CTL_CODE(FILE_DEVICE_UNKNOWN, 0x802, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Структура фильтра
typedef struct _FILTER_EXTENSION {
    PDEVICE_OBJECT FilterDeviceObject;
    PDEVICE_OBJECT AttachedToDevice;
    ULONG DeviceNumber;
    ULONG64 TotalAttempts;
    KSPIN_LOCK Lock;
    BOOLEAN IsProtected;
} FILTER_EXTENSION, *PFILTER_EXTENSION;

// Глобальные переменные
ULONG64 g_GlobalAttempts = 0;
KSPIN_LOCK g_GlobalLock;
BOOLEAN g_EnableLogging = TRUE;
BOOLEAN g_EnableNotification = TRUE;

// Определение языка (0 = ENG, 1 = RUS)
ULONG g_Language = 0;

// Функции локализации
const char* GetString(ULONG StringId) {
    static const char* StringsENG[] = {
        "UNKNOWN",
        "[DBT] DBT MBR Protector loading...",
        "[DBT] RegistryPath: %wZ\n",
        "[DBT] DBT MBR Protector loaded successfully.",
        "[DBT] Protecting all PhysicalDrive devices.",
        "[DBT] Filter attached to PhysicalDrive%lu\n",
        "[DBT] Driver unloaded. Total blocked: %llu\n",
        "[DBT] BLOCKED MBR WRITE",
        "[DBT] Process %S (PID %lu) attempted to write to MBR on PhysicalDrive%lu. Blocked.",
        "[DBT] BLOCKED MBR WRITE (Enhanced Protection)",
        "Device: PhysicalDrive%lu",
        "Process: %s (PID: %lu)",
        "Offset: 0x%llX, Length: 0x%X",
        "Status: ACCESS_DENIED",
        "Enhanced MBR Protection Active",
        "Protection Level: Full"
    };
    
    static const char* StringsRUS[] = {
        "НЕИЗВЕСТНО",
        "[DBT] Загрузка DBT MBR Protector...",
        "[DBT] Путь реестра: %wZ\n",
        "[DBT] DBT MBR Protector успешно загружен.",
        "[DBT] Защита всех PhysicalDrive устройств.",
        "[DBT] Фильтр подключен к PhysicalDrive%lu\n",
        "[DBT] Драйвер выгружен. Всего блокировок: %llu\n",
        "[DBT] БЛОКИРОВКА ЗАПИСИ В MBR",
        "[DBT] Процесс %S (PID %lu) попытался записать в MBR на PhysicalDrive%lu. Заблокировано.",
        "[DBT] БЛОКИРОВКА MBR (Расширенная защита)",
        "Устройство: PhysicalDrive%lu",
        "Процесс: %s (PID: %lu)",
        "Смещение: 0x%llX, Длина: 0x%X",
        "Статус: ДОСТУП ЗАПРЕЩЕН",
        "Расширенная защита MBR активна",
        "Уровень защиты: Полный"
    };
    
    if (g_Language == 1 && StringId < sizeof(StringsRUS)/sizeof(StringsRUS[0])) {
        return StringsRUS[StringId];
    }
    if (StringId < sizeof(StringsENG)/sizeof(StringsENG[0])) {
        return StringsENG[StringId];
    }
    return "UNKNOWN";
}

// Получение имени процесса (улучшено)
NTSTATUS GetProcessName(PCHAR ProcessName, SIZE_T Size, PULONG pPid) {
    PEPROCESS CurrentProcess = PsGetCurrentProcess();
    ULONG pid = (ULONG)PsGetCurrentProcessId();
    
    if (pPid) *pPid = pid;
    
    __try {
        PCHAR pName = PsGetProcessImageFileName(CurrentProcess);
        if (pName) {
            RtlStringCbPrintfA(ProcessName, Size, "%s", pName);
            return STATUS_SUCCESS;
        }
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        RtlStringCbPrintfA(ProcessName, Size, "%s", GetString(0));
        return STATUS_UNSUCCESSFUL;
    }
    
    RtlStringCbPrintfA(ProcessName, Size, "%s", GetString(0));
    return STATUS_SUCCESS;
}

// Проверка защиты сектора (расширено)
BOOLEAN IsProtectedSector(ULONGLONG ByteOffset, ULONG Length) {
    // Защита сектора 0 (MBR)
    if (ByteOffset == 0 && Length >= SECTOR_SIZE) {
        return TRUE;
    }
    
    // Защита секторов 1-9 (GPT Header, Backup MBR, etc.)
    for (ULONG i = 1; i < PROTECTED_SECTOR_COUNT; i++) {
        if (ByteOffset == (ULONGLONG)i * SECTOR_SIZE) {
            return TRUE;
        }
    }
    
    // Защита первых 10 секторов от частичной перезаписи
    if (ByteOffset < (ULONGLONG)PROTECTED_SECTOR_COUNT * SECTOR_SIZE && 
        ByteOffset + Length > (ULONGLONG)PROTECTED_SECTOR_COUNT * SECTOR_SIZE) {
        return TRUE;
    }
    
    return FALSE;
}

// Обработчик IRP_MJ_WRITE (улучшен)
NTSTATUS DispatchWrite(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONGLONG byteOffset = irpSp->Parameters.Write.ByteOffset.QuadPart;
    ULONG length = irpSp->Parameters.Write.Length;
    
    // Расширенная проверка
    if (IsProtectedSector(byteOffset, length)) {
        CHAR processName[MAX_PROCESS_NAME];
        ULONG pid = 0;
        
        GetProcessName(processName, sizeof(processName), &pid);
        
        KIRQL oldIrql;
        KeAcquireSpinLock(&g_GlobalLock, &oldIrql);
        g_GlobalAttempts++;
        ext->TotalAttempts++;
        ULONG64 attemptNumber = g_GlobalAttempts;
        KeReleaseSpinLock(&g_GlobalLock, oldIrql);
        
        // Логирование на двух языках
        if (g_EnableLogging) {
            DbgPrint(
                "[DBT] %s #%llu\n"
                "  [ENG] %s\n"
                "  [RUS] %s\n"
                "  [ENG] %s: %s\n"
                "  [RUS] %s: %s\n"
                "  [ENG] %s\n"
                "  [RUS] %s\n",
                GetString(7), attemptNumber,
                GetString(8), GetString(8),
                GetString(10), GetString(9),
                GetString(11), GetString(11),
                GetString(12), GetString(12),
                GetString(13), GetString(13)
            );
            
            WCHAR msgBuffer[512];
            swprintf(msgBuffer, 512, 
                L"[DBT] [ENG] Process %S (PID %lu) attempted to write MBR on PhysicalDrive%lu. Blocked.\n"
                L"[DBT] [RUS] Процесс %S (PID %lu) попытался записать MBR на PhysicalDrive%lu. Заблокировано.",
                processName, pid, ext->DeviceNumber,
                processName, pid, ext->DeviceNumber);
            DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "%S\n", msgBuffer);
        }
        
        // Отмена IRP
        Irp->IoStatus.Status = STATUS_ACCESS_DENIED;
        Irp->IoStatus.Information = 0;
        IoCompleteRequest(Irp, IO_NO_INCREMENT);
        return STATUS_ACCESS_DENIED;
    }
    
    // Пропускаем запрос
    IoSkipCurrentIrpStackLocation(Irp);
    return IoCallDriver(ext->AttachedToDevice, Irp);
}

// Pass-through для всех других IRP
NTSTATUS DispatchPassThrough(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    IoSkipCurrentIrpStackLocation(Irp);
    PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
    return IoCallDriver(ext->AttachedToDevice, Irp);
}

// Создание фильтра для устройства
NTSTATUS CreateFilterDevice(PDRIVER_OBJECT DriverObject, PDEVICE_OBJECT PhysicalDevice, ULONG DeviceNumber) {
    NTSTATUS status;
    PDEVICE_OBJECT filterDevice = NULL;
    PFILTER_EXTENSION ext;
    UNICODE_STRING deviceName;
    WCHAR nameBuffer[64];
    
    swprintf(nameBuffer, 64, L"\\Device\\DbtMbrProtector_%lu", DeviceNumber);
    RtlInitUnicodeString(&deviceName, nameBuffer);
    
    status = IoCreateDevice(DriverObject, sizeof(FILTER_EXTENSION),
                            &deviceName, FILE_DEVICE_DISK,
                            0, FALSE, &filterDevice);
    if (!NT_SUCCESS(status)) return status;
    
    ext = (PFILTER_EXTENSION)filterDevice->DeviceExtension;
    ext->FilterDeviceObject = filterDevice;
    ext->AttachedToDevice = IoAttachDeviceToDeviceStack(filterDevice, PhysicalDevice);
    ext->DeviceNumber = DeviceNumber;
    ext->TotalAttempts = 0;
    ext->IsProtected = TRUE;
    KeInitializeSpinLock(&ext->Lock);
    
    if (!ext->AttachedToDevice) {
        IoDeleteDevice(filterDevice);
        return STATUS_UNSUCCESSFUL;
    }
    
    filterDevice->Flags |= DO_BUFFERED_IO;
    filterDevice->Flags &= ~DO_DEVICE_INITIALIZING;
    
    DbgPrint("[DBT] %s PhysicalDrive%lu\n", GetString(5), DeviceNumber);
    return STATUS_SUCCESS;
}

// Получение номера диска
ULONG GetDiskNumber(PDEVICE_OBJECT DeviceObject) {
    WCHAR* name = DeviceObject->DriverObject->DriverName.Buffer;
    ULONG number = 0xFFFFFFFF;
    
    if (wcsstr(name, L"Harddisk")) {
        WCHAR* ptr = wcsstr(name, L"Harddisk");
        if (ptr) {
            ptr += 8;
            number = wcstoul(ptr, NULL, 10);
        }
    }
    return number;
}

// Обход всех дисковых устройств
NTSTATUS AttachToAllDisks(PDRIVER_OBJECT DriverObject) {
    NTSTATUS status;
    UNICODE_STRING deviceName;
    PFILE_OBJECT fileObject;
    PDEVICE_OBJECT diskDevice;
    WCHAR buffer[64];
    ULONG i;
    
    for (i = 0; i < MAX_DRIVE_COUNT; i++) {
        // Физические диски
        swprintf(buffer, 64, L"\\Device\\Harddisk%lu\\DR0", i);
        RtlInitUnicodeString(&deviceName, buffer);
        
        status = IoGetDeviceObjectPointer(&deviceName, FILE_ANY_ACCESS, 
                                          &fileObject, &diskDevice);
        if (NT_SUCCESS(status)) {
            CreateFilterDevice(DriverObject, diskDevice, i);
            ObDereferenceObject(fileObject);
        }
        
        // Тома
        swprintf(buffer, 64, L"\\Device\\HarddiskVolume%lu", i);
        RtlInitUnicodeString(&deviceName, buffer);
        
        status = IoGetDeviceObjectPointer(&deviceName, FILE_ANY_ACCESS, 
                                          &fileObject, &diskDevice);
        if (NT_SUCCESS(status)) {
            ULONG diskNum = GetDiskNumber(diskDevice);
            if (diskNum != 0xFFFFFFFF) {
                CreateFilterDevice(DriverObject, diskDevice, diskNum);
            }
            ObDereferenceObject(fileObject);
        }
    }
    return STATUS_SUCCESS;
}

// IOCTL обработчик
NTSTATUS DispatchDeviceControl(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONG code = irpSp->Parameters.DeviceIoControl.IoControlCode;
    NTSTATUS status = STATUS_INVALID_DEVICE_REQUEST;
    ULONG info = 0;
    
    switch (code) {
        case IOCTL_GET_ATTEMPTS:
        {
            if (Irp->AssociatedIrp.SystemBuffer && 
                irpSp->Parameters.DeviceIoControl.OutputBufferLength >= sizeof(ULONG64)) {
                *(PULONG64)Irp->AssociatedIrp.SystemBuffer = g_GlobalAttempts;
                info = sizeof(ULONG64);
                status = STATUS_SUCCESS;
            }
            break;
        }
        case IOCTL_GET_CONFIG:
        {
            if (Irp->AssociatedIrp.SystemBuffer && 
                irpSp->Parameters.DeviceIoControl.OutputBufferLength >= 4) {
                *(PULONG)Irp->AssociatedIrp.SystemBuffer = g_Language;
                info = 4;
                status = STATUS_SUCCESS;
            }
            break;
        }
        default:
            status = STATUS_INVALID_DEVICE_REQUEST;
            break;
    }
    
    Irp->IoStatus.Status = status;
    Irp->IoStatus.Information = info;
    IoCompleteRequest(Irp, IO_NO_INCREMENT);
    return status;
}

// Выгрузка драйвера
VOID DriverUnload(PDRIVER_OBJECT DriverObject) {
    PDEVICE_OBJECT device = DriverObject->DeviceObject;
    UNICODE_STRING symLinkName;
    
    while (device) {
        PFILTER_EXTENSION ext = (PFILTER_EXTENSION)device->DeviceExtension;
        if (ext->AttachedToDevice) {
            IoDetachDevice(ext->AttachedToDevice);
        }
        PDEVICE_OBJECT nextDevice = device->NextDevice;
        IoDeleteDevice(device);
        device = nextDevice;
    }
    
    RtlInitUnicodeString(&symLinkName, SYM_LINK_NAME);
    IoDeleteSymbolicLink(&symLinkName);
    
    DbgPrint("[DBT] %s %llu\n", GetString(6), g_GlobalAttempts);
}

// Entry point
NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath) {
    ULONG i;
    UNICODE_STRING symLinkName;
    
    // Определение языка системы
    g_Language = 0; // По умолчанию английский
    // Можно определить по реестру или параметрам
    
    DbgPrint("[DBT] %s\n", GetString(1));
    DbgPrint("[DBT] %s", GetString(2));
    
    KeInitializeSpinLock(&g_GlobalLock);
    g_GlobalAttempts = 0;
    
    for (i = 0; i < IRP_MJ_MAXIMUM_FUNCTION; i++) {
        DriverObject->MajorFunction[i] = DispatchPassThrough;
    }
    
    DriverObject->MajorFunction[IRP_MJ_WRITE] = DispatchWrite;
    DriverObject->MajorFunction[IRP_MJ_DEVICE_CONTROL] = DispatchDeviceControl;
    DriverObject->DriverUnload = DriverUnload;
    
    RtlInitUnicodeString(&symLinkName, SYM_LINK_NAME);
    IoCreateSymbolicLink(&symLinkName, &symLinkName);
    
    AttachToAllDisks(DriverObject);
    
    DbgPrint("[DBT] %s\n", GetString(3));
    DbgPrint("[DBT] %s\n", GetString(4));
    DbgPrint("[DBT] %s\n", GetString(14));
    DbgPrint("[DBT] %s\n", GetString(15));
    
    return STATUS_SUCCESS;
}

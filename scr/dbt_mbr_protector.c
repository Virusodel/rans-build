#include <ntddk.h>
#include <ntdddisk.h>
#include <wdm.h>
#include <stdlib.h>  // для wcstoul

#define DEVICE_NAME L"\\Device\\DbtMbrProtector"
#define SYM_LINK_NAME L"\\DosDevices\\DbtMbrProtector"
#define PROTECTED_SECTOR 0
#define SECTOR_SIZE 512
#define MAX_PROCESS_NAME 256

typedef struct _FILTER_EXTENSION {
    PDEVICE_OBJECT FilterDeviceObject;
    PDEVICE_OBJECT AttachedToDevice;
    ULONG DeviceNumber;
    ULONG64 TotalAttempts;
    KSPIN_LOCK Lock;
} FILTER_EXTENSION, *PFILTER_EXTENSION;

ULONG64 g_GlobalAttempts = 0;
KSPIN_LOCK g_GlobalLock;

// Получение имени процесса
NTSTATUS GetProcessName(PCHAR ProcessName, SIZE_T Size) {
    PEPROCESS CurrentProcess = PsGetCurrentProcess();
    
    __try {
        PCHAR pName = PsGetProcessImageFileName(CurrentProcess);
        if (pName) {
            sprintf(ProcessName, "%s", pName);
            return STATUS_SUCCESS;
        }
    } __except(EXCEPTION_EXECUTE_HANDLER) {
        sprintf(ProcessName, "UNKNOWN");
        return STATUS_UNSUCCESSFUL;
    }
    
    sprintf(ProcessName, "UNKNOWN");
    return STATUS_SUCCESS;
}

// Обработчик IRP_MJ_WRITE
NTSTATUS DispatchWrite(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONGLONG byteOffset = irpSp->Parameters.Write.ByteOffset.QuadPart;
    ULONG length = irpSp->Parameters.Write.Length;
    
    // Проверка: запись в сектор 0?
    if (byteOffset == 0 && length >= SECTOR_SIZE) {
        CHAR processName[MAX_PROCESS_NAME];
        ULONG pid = (ULONG)PsGetCurrentProcessId();
        
        GetProcessName(processName, sizeof(processName));
        
        // Блокировка для атомарного обновления
        KIRQL oldIrql;
        KeAcquireSpinLock(&g_GlobalLock, &oldIrql);
        g_GlobalAttempts++;
        ext->TotalAttempts++;
        ULONG64 attemptNumber = g_GlobalAttempts;
        KeReleaseSpinLock(&g_GlobalLock, oldIrql);
        
        // Логирование с информацией о процессе
        DbgPrint(
            "[DBT] BLOCKED MBR WRITE #%llu\n"
            "  Device: PhysicalDrive%lu\n"
            "  Process: %s (PID: %lu)\n"
            "  Offset: 0x%llX, Length: 0x%X\n"
            "  Status: ACCESS_DENIED\n",
            attemptNumber, ext->DeviceNumber, processName, pid, byteOffset, length
        );
        
        // Вывод в системный журнал
        WCHAR msgBuffer[512];
        swprintf(msgBuffer, 512, 
            L"[DBT] Process %S (PID %lu) attempted to write to MBR on PhysicalDrive%lu. Blocked.",
            processName, pid, ext->DeviceNumber);
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "%S\n", msgBuffer);
        
        // Отменяем IRP с ошибкой доступа
        Irp->IoStatus.Status = STATUS_ACCESS_DENIED;
        Irp->IoStatus.Information = 0;
        IoCompleteRequest(Irp, IO_NO_INCREMENT);
        return STATUS_ACCESS_DENIED;
    }
    
    // Пропускаем запрос ниже по стеку
    IoSkipCurrentIrpStackLocation(Irp);
    return IoCallDriver(((PFILTER_EXTENSION)DeviceObject->DeviceExtension)->AttachedToDevice, Irp);
}

// Pass-through для всех других IRP
NTSTATUS DispatchPassThrough(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    IoSkipCurrentIrpStackLocation(Irp);
    return IoCallDriver(((PFILTER_EXTENSION)DeviceObject->DeviceExtension)->AttachedToDevice, Irp);
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
    KeInitializeSpinLock(&ext->Lock);
    
    if (!ext->AttachedToDevice) {
        IoDeleteDevice(filterDevice);
        return STATUS_UNSUCCESSFUL;
    }
    
    filterDevice->Flags |= DO_BUFFERED_IO;
    filterDevice->Flags &= ~DO_DEVICE_INITIALIZING;
    
    DbgPrint("[DBT] Filter attached to PhysicalDrive%lu\n", DeviceNumber);
    return STATUS_SUCCESS;
}

// Получение номера диска из имени устройства
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
    
    for (i = 0; i < 64; i++) {
        swprintf(buffer, 64, L"\\Device\\Harddisk%lu\\DR0", i);
        RtlInitUnicodeString(&deviceName, buffer);
        
        status = IoGetDeviceObjectPointer(&deviceName, FILE_ANY_ACCESS, 
                                          &fileObject, &diskDevice);
        if (NT_SUCCESS(status)) {
            CreateFilterDevice(DriverObject, diskDevice, i);
            ObDereferenceObject(fileObject);
        }
        
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

// IOCTL для получения статистики
NTSTATUS DispatchDeviceControl(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONG code = irpSp->Parameters.DeviceIoControl.IoControlCode;
    NTSTATUS status = STATUS_INVALID_DEVICE_REQUEST;
    ULONG info = 0;
    
    switch (code) {
        case IOCTL_STORAGE_GET_DEVICE_NUMBER:
        case 0x80000000:
        {
            PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
            if (Irp->AssociatedIrp.SystemBuffer && 
                irpSp->Parameters.DeviceIoControl.OutputBufferLength >= sizeof(ULONG64)) {
                *(PULONG64)Irp->AssociatedIrp.SystemBuffer = g_GlobalAttempts;
                info = sizeof(ULONG64);
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
    
    DbgPrint("[DBT] Driver unloaded. Total MBR write attempts blocked: %llu\n", g_GlobalAttempts);
}

// Entry point
NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath) {
    ULONG i;
    UNICODE_STRING symLinkName;
    
    DbgPrint("[DBT] DBT MBR Protector loading...\n");
    DbgPrint("[DBT] RegistryPath: %wZ\n", RegistryPath);
    
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
    
    DbgPrint("[DBT] DBT MBR Protector loaded successfully.\n");
    DbgPrint("[DBT] Protecting all PhysicalDrive devices.\n");
    
    return STATUS_SUCCESS;
}

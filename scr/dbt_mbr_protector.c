#include <ntddk.h>
#include <ntdddisk.h>
#include <wdm.h>
#include <stdlib.h>

#define DEVICE_NAME L"\\Device\\DbtMbrProtector"
#define SYM_LINK_NAME L"\\DosDevices\\DbtMbrProtector"
#define SECTOR_SIZE 512
#define MAX_PROCESS_NAME 256
#define MAX_DRIVE_COUNT 64

#define IOCTL_GET_ATTEMPTS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Структура GPT заголовка
typedef struct _GPT_HEADER {
    UCHAR Signature[8];
    UCHAR Revision[4];
    ULONG HeaderSize;
    ULONG HeaderCRC32;
    ULONG Reserved;
    ULONGLONG MyLBA;
    ULONGLONG AlternateLBA;
    ULONGLONG FirstUsableLBA;
    ULONGLONG LastUsableLBA;
    UCHAR DiskGUID[16];
    ULONGLONG PartitionEntryLBA;
    ULONG NumberOfPartitionEntries;
    ULONG SizeOfPartitionEntry;
    ULONG PartitionEntryArrayCRC32;
} GPT_HEADER, *PGPT_HEADER;

typedef struct _FILTER_EXTENSION {
    PDEVICE_OBJECT FilterDeviceObject;
    PDEVICE_OBJECT AttachedToDevice;
    ULONG DeviceNumber;
    ULONG64 TotalAttempts;
    KSPIN_LOCK Lock;
    BOOLEAN IsProtected;
    ULONGLONG GPTStartLBA;
    ULONG GPTSectorCount;
    BOOLEAN GPTDetected;
} FILTER_EXTENSION, *PFILTER_EXTENSION;

ULONG64 g_GlobalAttempts = 0;
KSPIN_LOCK g_GlobalLock;
BOOLEAN g_EnableLogging = TRUE;

const char* SuspiciousProcesses[] = {
    "petya", "goldeneye", "misha", "satana",
    "annabelle", "gdi", "ransom", "wannacry",
    "locky", "cryptolocker", "badrabbit"
};
#define SUSPICIOUS_COUNT (sizeof(SuspiciousProcesses) / sizeof(SuspiciousProcesses[0]))

NTSTATUS GetProcessName(PCHAR ProcessName, SIZE_T Size, PULONG pPid) {
    PEPROCESS CurrentProcess = PsGetCurrentProcess();
    ULONG pid = (ULONG)PsGetCurrentProcessId();
    
    if (pPid) *pPid = pid;
    
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

BOOLEAN IsSuspiciousProcess(PCHAR ProcessName) {
    if (!ProcessName) return FALSE;
    
    for (ULONG i = 0; i < SUSPICIOUS_COUNT; i++) {
        if (strstr(ProcessName, SuspiciousProcesses[i])) {
            return TRUE;
        }
    }
    return FALSE;
}

// Проверка, является ли диск GPT
BOOLEAN IsGPTDisk(PDEVICE_OBJECT PhysicalDevice, PULONGLONG pPartitionEntryLBA, PULONG pEntryCount) {
    NTSTATUS status;
    PIRP irp;
    IO_STATUS_BLOCK ioStatus;
    KEVENT event;
    LARGE_INTEGER byteOffset;
    GPT_HEADER gptHeader;
    PFILE_OBJECT fileObject = NULL;
    PDEVICE_OBJECT targetDevice = NULL;
    BOOLEAN result = FALSE;
    
    // Открываем устройство
    status = IoGetDeviceObjectPointer(&PhysicalDevice->DriverObject->DriverName, 
                                      FILE_READ_DATA, &fileObject, &targetDevice);
    if (!NT_SUCCESS(status)) {
        return FALSE;
    }
    
    // Читаем сектор 1 (GPT заголовок)
    KeInitializeEvent(&event, NotificationEvent, FALSE);
    byteOffset.QuadPart = SECTOR_SIZE;
    
    irp = IoBuildSynchronousFsdRequest(IRP_MJ_READ, targetDevice,
                                       &gptHeader, sizeof(GPT_HEADER),
                                       &byteOffset, &event, &ioStatus);
    if (!irp) {
        ObDereferenceObject(fileObject);
        return FALSE;
    }
    
    status = IoCallDriver(targetDevice, irp);
    if (status == STATUS_PENDING) {
        KeWaitForSingleObject(&event, Executive, KernelMode, FALSE, NULL);
        status = ioStatus.Status;
    }
    
    ObDereferenceObject(fileObject);
    
    if (!NT_SUCCESS(status)) {
        return FALSE;
    }
    
    // Проверяем сигнатуру GPT
    if (gptHeader.Signature[0] == 'E' &&
        gptHeader.Signature[1] == 'F' &&
        gptHeader.Signature[2] == 'I' &&
        gptHeader.Signature[3] == ' ' &&
        gptHeader.Signature[4] == 'P' &&
        gptHeader.Signature[5] == 'A' &&
        gptHeader.Signature[6] == 'R' &&
        gptHeader.Signature[7] == 'T') {
        
        if (pPartitionEntryLBA) {
            *pPartitionEntryLBA = gptHeader.PartitionEntryLBA;
        }
        if (pEntryCount) {
            *pEntryCount = gptHeader.NumberOfPartitionEntries * 
                           gptHeader.SizeOfPartitionEntry / SECTOR_SIZE + 1;
        }
        result = TRUE;
    }
    
    return result;
}

// Проверка защиты сектора (динамическая)
BOOLEAN IsProtectedSector(PFILTER_EXTENSION ext, ULONGLONG ByteOffset, ULONG Length) {
    ULONGLONG sector = ByteOffset / SECTOR_SIZE;
    ULONGLONG endSector = (ByteOffset + Length - 1) / SECTOR_SIZE;
    
    // Защита MBR (сектор 0)
    if (sector == 0) {
        return TRUE;
    }
    
    // Защита GPT (если обнаружена)
    if (ext->GPTDetected) {
        // Защита GPT заголовка (сектор 1)
        if (sector == 1) {
            return TRUE;
        }
        
        // Защита таблицы разделов
        if (ext->GPTStartLBA > 0) {
            ULONGLONG gptEnd = ext->GPTStartLBA + ext->GPTSectorCount;
            if (sector >= ext->GPTStartLBA && sector < gptEnd) {
                return TRUE;
            }
            if (endSector >= ext->GPTStartLBA && endSector < gptEnd) {
                return TRUE;
            }
        }
    }
    
    return FALSE;
}

NTSTATUS DispatchWrite(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONGLONG byteOffset = irpSp->Parameters.Write.ByteOffset.QuadPart;
    ULONG length = irpSp->Parameters.Write.Length;
    
    if (IsProtectedSector(ext, byteOffset, length)) {
        CHAR processName[MAX_PROCESS_NAME];
        ULONG pid = 0;
        
        GetProcessName(processName, sizeof(processName), &pid);
        
        KIRQL oldIrql;
        KeAcquireSpinLock(&g_GlobalLock, &oldIrql);
        g_GlobalAttempts++;
        ext->TotalAttempts++;
        ULONG64 attemptNumber = g_GlobalAttempts;
        KeReleaseSpinLock(&g_GlobalLock, oldIrql);
        
        if (g_EnableLogging) {
            DbgPrint(
                "[DBT] BLOCKED WRITE #%llu\n"
                "  Device: PhysicalDrive%lu\n"
                "  Process: %s (PID: %lu)\n"
                "  Offset: 0x%llX, Length: 0x%X\n"
                "  Status: ACCESS_DENIED\n",
                attemptNumber, ext->DeviceNumber, processName, pid, byteOffset, length
            );
            
            if (IsSuspiciousProcess(processName)) {
                DbgPrint(
                    "[DBT] SUSPICIOUS PROCESS: %s (PID: %lu)\n",
                    processName, pid
                );
            }
        }
        
        Irp->IoStatus.Status = STATUS_ACCESS_DENIED;
        Irp->IoStatus.Information = 0;
        IoCompleteRequest(Irp, IO_NO_INCREMENT);
        return STATUS_ACCESS_DENIED;
    }
    
    IoSkipCurrentIrpStackLocation(Irp);
    return IoCallDriver(ext->AttachedToDevice, Irp);
}

NTSTATUS DispatchPassThrough(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    IoSkipCurrentIrpStackLocation(Irp);
    PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
    return IoCallDriver(ext->AttachedToDevice, Irp);
}

NTSTATUS CreateFilterDevice(PDRIVER_OBJECT DriverObject, PDEVICE_OBJECT PhysicalDevice, ULONG DeviceNumber) {
    NTSTATUS status;
    PDEVICE_OBJECT filterDevice = NULL;
    PFILTER_EXTENSION ext;
    UNICODE_STRING deviceName;
    WCHAR nameBuffer[64];
    ULONGLONG partitionEntryLBA = 0;
    ULONG entryCount = 0;
    BOOLEAN isGPT = FALSE;
    
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
    ext->GPTDetected = FALSE;
    ext->GPTStartLBA = 0;
    ext->GPTSectorCount = 0;
    KeInitializeSpinLock(&ext->Lock);
    
    if (!ext->AttachedToDevice) {
        IoDeleteDevice(filterDevice);
        return STATUS_UNSUCCESSFUL;
    }
    
    // Проверяем, является ли диск GPT
    isGPT = IsGPTDisk(PhysicalDevice, &partitionEntryLBA, &entryCount);
    if (isGPT) {
        ext->GPTDetected = TRUE;
        ext->GPTStartLBA = partitionEntryLBA;
        ext->GPTSectorCount = entryCount;
        DbgPrint("[DBT] PhysicalDrive%lu: GPT detected, protecting LBA %llu-%llu\n",
                 DeviceNumber, partitionEntryLBA, partitionEntryLBA + entryCount);
    } else {
        DbgPrint("[DBT] PhysicalDrive%lu: MBR detected\n", DeviceNumber);
    }
    
    filterDevice->Flags |= DO_BUFFERED_IO;
    filterDevice->Flags &= ~DO_DEVICE_INITIALIZING;
    
    DbgPrint("[DBT] Filter attached to PhysicalDrive%lu\n", DeviceNumber);
    return STATUS_SUCCESS;
}

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

NTSTATUS AttachToAllDisks(PDRIVER_OBJECT DriverObject) {
    NTSTATUS status;
    UNICODE_STRING deviceName;
    PFILE_OBJECT fileObject;
    PDEVICE_OBJECT diskDevice;
    WCHAR buffer[64];
    ULONG i;
    
    for (i = 0; i < MAX_DRIVE_COUNT; i++) {
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

NTSTATUS DispatchDeviceControl(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONG code = irpSp->Parameters.DeviceIoControl.IoControlCode;
    NTSTATUS status = STATUS_INVALID_DEVICE_REQUEST;
    ULONG info = 0;
    
    switch (code) {
        case IOCTL_STORAGE_GET_DEVICE_NUMBER:
        case 0x80000000:
        {
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
    
    DbgPrint("[DBT] Driver unloaded. Total blocked: %llu\n", g_GlobalAttempts);
}

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

#include <ntddk.h>
#include <ntdddisk.h>
#include <wdm.h>
#include <stdlib.h>

#define DEVICE_NAME L"\\Device\\DbtMbrProtector"
#define SYM_LINK_NAME L"\\DosDevices\\DbtMbrProtector"
#define SECTOR_SIZE 512
#define MAX_PROCESS_NAME 256
#define MAX_DRIVE_COUNT 64
#define PROTECTED_SECTOR_COUNT 10

#define IOCTL_GET_ATTEMPTS CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

typedef struct _FILTER_EXTENSION {
    PDEVICE_OBJECT FilterDeviceObject;
    PDEVICE_OBJECT AttachedToDevice;
    ULONG DeviceNumber;
    ULONG64 TotalAttempts;
    KSPIN_LOCK Lock;
    BOOLEAN IsProtected;
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

BOOLEAN IsProtectedSector(ULONGLONG ByteOffset, ULONG Length) {
    if (ByteOffset == 0 && Length >= SECTOR_SIZE) {
        return TRUE;
    }
    
    for (ULONG i = 1; i < PROTECTED_SECTOR_COUNT; i++) {
        if (ByteOffset == (ULONGLONG)i * SECTOR_SIZE) {
            return TRUE;
        }
    }
    
    if (ByteOffset < (ULONGLONG)PROTECTED_SECTOR_COUNT * SECTOR_SIZE && 
        ByteOffset + Length > (ULONGLONG)PROTECTED_SECTOR_COUNT * SECTOR_SIZE) {
        return TRUE;
    }
    
    return FALSE;
}

NTSTATUS DispatchWrite(PDEVICE_OBJECT DeviceObject, PIRP Irp) {
    PFILTER_EXTENSION ext = (PFILTER_EXTENSION)DeviceObject->DeviceExtension;
    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    ULONGLONG byteOffset = irpSp->Parameters.Write.ByteOffset.QuadPart;
    ULONG length = irpSp->Parameters.Write.Length;
    
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
        
        if (g_EnableLogging) {
            DbgPrint(
                "[DBT] BLOCKED MBR WRITE #%llu\n"
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

BITS 16
ORG 0x7C00

start:
    cli
    cld
    xor ax, ax
    mov ds, ax
    mov es, ax
    mov ss, ax
    mov sp, 0x7C00
    sti

    ; Сохраняем оригинальный MBR в сектор 2
    call save_mbr

    ; Загружаем основной код из сектора 3 в память по адресу 0x8000
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x8000
    mov ah, 0x02
    mov al, 8          ; Загружаем 8 секторов (4 КБ) -> достаточно для всего
    mov ch, 0
    mov cl, 3          ; Начинаем с сектора 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    ; Передаём управление основному коду
    jmp 0x0000:0x8000

load_error:
    ; Если не удалось загрузить — показываем ошибку
    mov si, msg_error
    call print
    jmp hang

save_mbr:
    pusha
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x7E00
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 1
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc .error
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x7E00
    mov ah, 0x03
    mov al, 1
    mov ch, 0
    mov cl, 2
    mov dh, 0
    mov dl, 0x80
    int 0x13
.error:
    popa
    ret

print:
    lodsb
    or al, al
    jz .done
    mov ah, 0x0E
    int 0x10
    jmp print
.done:
    ret

hang:
    cli
    hlt
    jmp hang

msg_error:
    db 'Load error!', 0

times 510 - ($ - $$) db 0
dw 0xAA55

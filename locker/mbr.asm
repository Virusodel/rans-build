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

    ; ЧИТАЕМ STAGE2 (СЕКТОР 13)
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x8000
    mov ah, 0x02
    mov al, 2
    mov ch, 0
    mov cl, 13
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    ; ПЕРЕХОД НА STAGE2
    jmp 0x0000:0x8000

load_error:
    mov si, msg_error
    call print
    jmp hang

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
    db 'Load error!',0

times 510 - ($ - $$) db 0
dw 0xAA55

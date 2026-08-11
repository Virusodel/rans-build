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

    ; Загружаем Stage2 из сектора 3 (6 секторов: 3-8)
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x8000
    mov ah, 0x02
    mov al, 6
    mov ch, 0
    mov cl, 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

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
    db 'Load error!', 0

times 510 - ($ - $$) db 0
dw 0xAA55

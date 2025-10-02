namespace BolidEmulator
{
    partial class BolidEmulatorGUI
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.s2000ppMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.portGroupBox = new System.Windows.Forms.GroupBox();
            this.portComboBox = new System.Windows.Forms.ComboBox();
            this.refreshPortsButton = new System.Windows.Forms.Button();
            this.connectButton = new System.Windows.Forms.Button();
            this.baudrateComboBox = new System.Windows.Forms.ComboBox();
            this.parityComboBox = new System.Windows.Forms.ComboBox();
            this.stopbitsComboBox = new System.Windows.Forms.ComboBox();
            this.deviceGroupBox = new System.Windows.Forms.GroupBox();
            this.addrNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.pollDeviceButton = new System.Windows.Forms.Button();
            this.scanButton = new System.Windows.Forms.Button();
            this.stopScanButton = new System.Windows.Forms.Button();
            this.scanStartNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.scanEndNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.scanTimeoutNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.devicesGroupBox = new System.Windows.Forms.GroupBox();
            this.devicesListBox = new System.Windows.Forms.ListBox();
            this.clearDevicesButton = new System.Windows.Forms.Button();
            this.infoGroupBox = new System.Windows.Forms.GroupBox();
            this.infoLabel = new System.Windows.Forms.Label();
            this.logGroupBox = new System.Windows.Forms.GroupBox();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.portLabel = new System.Windows.Forms.Label();
            this.baudrateLabel = new System.Windows.Forms.Label();
            this.parityLabel = new System.Windows.Forms.Label();
            this.stopbitsLabel = new System.Windows.Forms.Label();
            this.addrLabel = new System.Windows.Forms.Label();
            this.scanRangeLabel = new System.Windows.Forms.Label();
            this.scanDashLabel = new System.Windows.Forms.Label();
            this.scanTimeoutLabel = new System.Windows.Forms.Label();
            this.scanTimeoutUnitLabel = new System.Windows.Forms.Label();
            this.scanHintLabel = new System.Windows.Forms.Label();
            
            this.menuStrip.SuspendLayout();
            this.portGroupBox.SuspendLayout();
            this.deviceGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.addrNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scanStartNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scanEndNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scanTimeoutNumericUpDown)).BeginInit();
            this.devicesGroupBox.SuspendLayout();
            this.infoGroupBox.SuspendLayout();
            this.logGroupBox.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.s2000ppMenuItem,
            this.helpMenu});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1200, 24);
            this.menuStrip.TabIndex = 0;
            
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitMenuItem});
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Size = new System.Drawing.Size(48, 20);
            this.fileMenu.Text = "Файл";
            
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(108, 22);
            this.exitMenuItem.Text = "Выход";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            
            this.s2000ppMenuItem.Name = "s2000ppMenuItem";
            this.s2000ppMenuItem.Size = new System.Drawing.Size(145, 20);
            this.s2000ppMenuItem.Text = "Управление С2000ПП";
            this.s2000ppMenuItem.Click += new System.EventHandler(this.s2000ppMenuItem_Click);
            
            this.helpMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutMenuItem});
            this.helpMenu.Name = "helpMenu";
            this.helpMenu.Size = new System.Drawing.Size(65, 20);
            this.helpMenu.Text = "Помощь";
            
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(149, 22);
            this.aboutMenuItem.Text = "О программе";
            this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
            
            this.portGroupBox.Controls.Add(this.portLabel);
            this.portGroupBox.Controls.Add(this.portComboBox);
            this.portGroupBox.Controls.Add(this.refreshPortsButton);
            this.portGroupBox.Controls.Add(this.connectButton);
            this.portGroupBox.Controls.Add(this.baudrateLabel);
            this.portGroupBox.Controls.Add(this.baudrateComboBox);
            this.portGroupBox.Controls.Add(this.parityLabel);
            this.portGroupBox.Controls.Add(this.parityComboBox);
            this.portGroupBox.Controls.Add(this.stopbitsLabel);
            this.portGroupBox.Controls.Add(this.stopbitsComboBox);
            this.portGroupBox.Location = new System.Drawing.Point(10, 27);
            this.portGroupBox.Name = "portGroupBox";
            this.portGroupBox.Size = new System.Drawing.Size(1180, 90);
            this.portGroupBox.TabIndex = 1;
            this.portGroupBox.TabStop = false;
            this.portGroupBox.Text = "COM-порт";
            
            this.portLabel.Location = new System.Drawing.Point(10, 25);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(50, 20);
            this.portLabel.TabIndex = 0;
            this.portLabel.Text = "Порт:";
            this.portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.portComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.portComboBox.FormattingEnabled = true;
            this.portComboBox.Location = new System.Drawing.Point(60, 23);
            this.portComboBox.Name = "portComboBox";
            this.portComboBox.Size = new System.Drawing.Size(300, 21);
            this.portComboBox.TabIndex = 1;
            
            this.refreshPortsButton.Location = new System.Drawing.Point(370, 21);
            this.refreshPortsButton.Name = "refreshPortsButton";
            this.refreshPortsButton.Size = new System.Drawing.Size(90, 25);
            this.refreshPortsButton.TabIndex = 2;
            this.refreshPortsButton.Text = "Обновить";
            this.refreshPortsButton.UseVisualStyleBackColor = true;
            this.refreshPortsButton.Click += new System.EventHandler(this.refreshPortsButton_Click);
            
            this.connectButton.Location = new System.Drawing.Point(470, 21);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(120, 25);
            this.connectButton.TabIndex = 3;
            this.connectButton.Text = "Подключиться";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
            
            this.baudrateLabel.Location = new System.Drawing.Point(10, 55);
            this.baudrateLabel.Name = "baudrateLabel";
            this.baudrateLabel.Size = new System.Drawing.Size(70, 20);
            this.baudrateLabel.TabIndex = 4;
            this.baudrateLabel.Text = "Скорость:";
            this.baudrateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.baudrateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.baudrateComboBox.FormattingEnabled = true;
            this.baudrateComboBox.Items.AddRange(new object[] {
            "4800",
            "9600",
            "19200",
            "38400",
            "57600",
            "115200"});
            this.baudrateComboBox.Location = new System.Drawing.Point(85, 53);
            this.baudrateComboBox.Name = "baudrateComboBox";
            this.baudrateComboBox.Size = new System.Drawing.Size(80, 21);
            this.baudrateComboBox.TabIndex = 5;
            
            this.parityLabel.Location = new System.Drawing.Point(180, 55);
            this.parityLabel.Name = "parityLabel";
            this.parityLabel.Size = new System.Drawing.Size(70, 20);
            this.parityLabel.TabIndex = 6;
            this.parityLabel.Text = "Четность:";
            this.parityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.parityComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.parityComboBox.FormattingEnabled = true;
            this.parityComboBox.Items.AddRange(new object[] {
            "None",
            "Odd",
            "Even"});
            this.parityComboBox.Location = new System.Drawing.Point(250, 53);
            this.parityComboBox.Name = "parityComboBox";
            this.parityComboBox.Size = new System.Drawing.Size(70, 21);
            this.parityComboBox.TabIndex = 7;
            
            this.stopbitsLabel.Location = new System.Drawing.Point(340, 55);
            this.stopbitsLabel.Name = "stopbitsLabel";
            this.stopbitsLabel.Size = new System.Drawing.Size(75, 20);
            this.stopbitsLabel.TabIndex = 8;
            this.stopbitsLabel.Text = "Стоп-биты:";
            this.stopbitsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.stopbitsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.stopbitsComboBox.FormattingEnabled = true;
            this.stopbitsComboBox.Items.AddRange(new object[] {
            "1",
            "2"});
            this.stopbitsComboBox.Location = new System.Drawing.Point(420, 53);
            this.stopbitsComboBox.Name = "stopbitsComboBox";
            this.stopbitsComboBox.Size = new System.Drawing.Size(50, 21);
            this.stopbitsComboBox.TabIndex = 9;
            
            
            this.deviceGroupBox.Controls.Add(this.addrLabel);
            this.deviceGroupBox.Controls.Add(this.addrNumericUpDown);
            this.deviceGroupBox.Controls.Add(this.pollDeviceButton);
            this.deviceGroupBox.Controls.Add(this.scanButton);
            this.deviceGroupBox.Controls.Add(this.stopScanButton);
            this.deviceGroupBox.Controls.Add(this.scanRangeLabel);
            this.deviceGroupBox.Controls.Add(this.scanStartNumericUpDown);
            this.deviceGroupBox.Controls.Add(this.scanDashLabel);
            this.deviceGroupBox.Controls.Add(this.scanEndNumericUpDown);
            this.deviceGroupBox.Controls.Add(this.scanTimeoutLabel);
            this.deviceGroupBox.Controls.Add(this.scanTimeoutNumericUpDown);
            this.deviceGroupBox.Controls.Add(this.scanTimeoutUnitLabel);
            this.deviceGroupBox.Controls.Add(this.scanHintLabel);
            this.deviceGroupBox.Location = new System.Drawing.Point(10, 125);
            this.deviceGroupBox.Name = "deviceGroupBox";
            this.deviceGroupBox.Size = new System.Drawing.Size(1180, 100);
            this.deviceGroupBox.TabIndex = 2;
            this.deviceGroupBox.TabStop = false;
            this.deviceGroupBox.Text = "Устройство";
            
            this.addrLabel.Location = new System.Drawing.Point(10, 25);
            this.addrLabel.Name = "addrLabel";
            this.addrLabel.Size = new System.Drawing.Size(50, 20);
            this.addrLabel.TabIndex = 0;
            this.addrLabel.Text = "Адрес:";
            this.addrLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.addrNumericUpDown.Location = new System.Drawing.Point(60, 23);
            this.addrNumericUpDown.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.addrNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.addrNumericUpDown.Name = "addrNumericUpDown";
            this.addrNumericUpDown.Size = new System.Drawing.Size(80, 20);
            this.addrNumericUpDown.TabIndex = 1;
            this.addrNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            
            this.pollDeviceButton.Location = new System.Drawing.Point(150, 21);
            this.pollDeviceButton.Name = "pollDeviceButton";
            this.pollDeviceButton.Size = new System.Drawing.Size(120, 25);
            this.pollDeviceButton.TabIndex = 2;
            this.pollDeviceButton.Text = "Подключиться";
            this.pollDeviceButton.UseVisualStyleBackColor = true;
            this.pollDeviceButton.Click += new System.EventHandler(this.pollDeviceButton_Click);
            
            this.scanButton.Location = new System.Drawing.Point(280, 21);
            this.scanButton.Name = "scanButton";
            this.scanButton.Size = new System.Drawing.Size(100, 25);
            this.scanButton.TabIndex = 3;
            this.scanButton.Text = "Сканировать";
            this.scanButton.UseVisualStyleBackColor = true;
            this.scanButton.Click += new System.EventHandler(this.scanButton_Click);
            
            this.stopScanButton.Enabled = false;
            this.stopScanButton.Location = new System.Drawing.Point(390, 21);
            this.stopScanButton.Name = "stopScanButton";
            this.stopScanButton.Size = new System.Drawing.Size(100, 25);
            this.stopScanButton.TabIndex = 4;
            this.stopScanButton.Text = "Остановить";
            this.stopScanButton.UseVisualStyleBackColor = true;
            this.stopScanButton.Click += new System.EventHandler(this.stopScanButton_Click);
            
            this.scanRangeLabel.Location = new System.Drawing.Point(10, 55);
            this.scanRangeLabel.Name = "scanRangeLabel";
            this.scanRangeLabel.Size = new System.Drawing.Size(140, 20);
            this.scanRangeLabel.TabIndex = 5;
            this.scanRangeLabel.Text = "Сканировать адреса:";
            this.scanRangeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.scanStartNumericUpDown.Location = new System.Drawing.Point(155, 53);
            this.scanStartNumericUpDown.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.scanStartNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.scanStartNumericUpDown.Name = "scanStartNumericUpDown";
            this.scanStartNumericUpDown.Size = new System.Drawing.Size(70, 20);
            this.scanStartNumericUpDown.TabIndex = 6;
            this.scanStartNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            
            this.scanDashLabel.Location = new System.Drawing.Point(230, 55);
            this.scanDashLabel.Name = "scanDashLabel";
            this.scanDashLabel.Size = new System.Drawing.Size(15, 20);
            this.scanDashLabel.TabIndex = 7;
            this.scanDashLabel.Text = "-";
            this.scanDashLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            this.scanEndNumericUpDown.Location = new System.Drawing.Point(250, 53);
            this.scanEndNumericUpDown.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.scanEndNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.scanEndNumericUpDown.Name = "scanEndNumericUpDown";
            this.scanEndNumericUpDown.Size = new System.Drawing.Size(70, 20);
            this.scanEndNumericUpDown.TabIndex = 8;
            this.scanEndNumericUpDown.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            
            this.scanTimeoutLabel.Location = new System.Drawing.Point(340, 55);
            this.scanTimeoutLabel.Name = "scanTimeoutLabel";
            this.scanTimeoutLabel.Size = new System.Drawing.Size(60, 20);
            this.scanTimeoutLabel.TabIndex = 9;
            this.scanTimeoutLabel.Text = "Таймаут:";
            this.scanTimeoutLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.scanTimeoutNumericUpDown.DecimalPlaces = 1;
            this.scanTimeoutNumericUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.scanTimeoutNumericUpDown.Location = new System.Drawing.Point(405, 53);
            this.scanTimeoutNumericUpDown.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.scanTimeoutNumericUpDown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.scanTimeoutNumericUpDown.Name = "scanTimeoutNumericUpDown";
            this.scanTimeoutNumericUpDown.Size = new System.Drawing.Size(55, 20);
            this.scanTimeoutNumericUpDown.TabIndex = 10;
            this.scanTimeoutNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            
            this.scanTimeoutUnitLabel.Location = new System.Drawing.Point(465, 55);
            this.scanTimeoutUnitLabel.Name = "scanTimeoutUnitLabel";
            this.scanTimeoutUnitLabel.Size = new System.Drawing.Size(30, 20);
            this.scanTimeoutUnitLabel.TabIndex = 11;
            this.scanTimeoutUnitLabel.Text = "сек";
            this.scanTimeoutUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.scanHintLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.scanHintLabel.ForeColor = System.Drawing.Color.Gray;
            this.scanHintLabel.Location = new System.Drawing.Point(10, 77);
            this.scanHintLabel.Name = "scanHintLabel";
            this.scanHintLabel.Size = new System.Drawing.Size(1160, 15);
            this.scanHintLabel.TabIndex = 12;
            this.scanHintLabel.Text = "Рекомендации: Быстро (1-50) для тестирования, Полное (1-127) для поиска всех устройств";
            this.scanHintLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.devicesGroupBox.Controls.Add(this.devicesListBox);
            this.devicesGroupBox.Controls.Add(this.clearDevicesButton);
            this.devicesGroupBox.Location = new System.Drawing.Point(10, 233);
            this.devicesGroupBox.Name = "devicesGroupBox";
            this.devicesGroupBox.Size = new System.Drawing.Size(1180, 220);
            this.devicesGroupBox.TabIndex = 3;
            this.devicesGroupBox.TabStop = false;
            this.devicesGroupBox.Text = "Найденные устройства";
            
            this.devicesListBox.FormattingEnabled = true;
            this.devicesListBox.Location = new System.Drawing.Point(10, 20);
            this.devicesListBox.Name = "devicesListBox";
            this.devicesListBox.Size = new System.Drawing.Size(1160, 160);
            this.devicesListBox.TabIndex = 0;
            this.devicesListBox.DoubleClick += new System.EventHandler(this.devicesListBox_DoubleClick);
            
            this.clearDevicesButton.Location = new System.Drawing.Point(10, 185);
            this.clearDevicesButton.Name = "clearDevicesButton";
            this.clearDevicesButton.Size = new System.Drawing.Size(90, 25);
            this.clearDevicesButton.TabIndex = 1;
            this.clearDevicesButton.Text = "Очистить";
            this.clearDevicesButton.UseVisualStyleBackColor = true;
            this.clearDevicesButton.Click += new System.EventHandler(this.clearDevicesButton_Click);
            
            this.infoGroupBox.Controls.Add(this.infoLabel);
            this.infoGroupBox.Location = new System.Drawing.Point(10, 461);
            this.infoGroupBox.Name = "infoGroupBox";
            this.infoGroupBox.Size = new System.Drawing.Size(1180, 50);
            this.infoGroupBox.TabIndex = 4;
            this.infoGroupBox.TabStop = false;
            this.infoGroupBox.Text = "Информация";
            
            this.infoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.infoLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(139)))), ((int)(((byte)(87)))));
            this.infoLabel.Location = new System.Drawing.Point(10, 18);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(1160, 25);
            this.infoLabel.TabIndex = 0;
            this.infoLabel.Text = "💡 Дважды кликните по устройству в списке для открытия окна управления";
            this.infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            this.logGroupBox.Controls.Add(this.logTextBox);
            this.logGroupBox.Location = new System.Drawing.Point(10, 519);
            this.logGroupBox.Name = "logGroupBox";
            this.logGroupBox.Size = new System.Drawing.Size(1180, 230);
            this.logGroupBox.TabIndex = 5;
            this.logGroupBox.TabStop = false;
            this.logGroupBox.Text = "Лог";
            
            this.logTextBox.Location = new System.Drawing.Point(10, 20);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(1160, 200);
            this.logTextBox.TabIndex = 0;
            
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 756);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip.TabIndex = 6;
            
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(104, 17);
            this.statusLabel.Text = "Готов к работе";
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 778);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.logGroupBox);
            this.Controls.Add(this.infoGroupBox);
            this.Controls.Add(this.devicesGroupBox);
            this.Controls.Add(this.deviceGroupBox);
            this.Controls.Add(this.portGroupBox);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "BolidEmulatorGUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Эмулятор пульта С2000/С2000М v1.0";
            
            // Устанавливаем иконку
            try
            {
                if (System.IO.File.Exists("icon.ico"))
                {
                    this.Icon = new System.Drawing.Icon("icon.ico");
                }
            }
            catch (System.Exception)
            {
                // Игнорируем ошибки загрузки иконки
            }
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BolidEmulatorGUI_FormClosing);
            this.Load += new System.EventHandler(this.BolidEmulatorGUI_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.portGroupBox.ResumeLayout(false);
            this.portGroupBox.PerformLayout();
            this.deviceGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.addrNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scanStartNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scanEndNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scanTimeoutNumericUpDown)).EndInit();
            this.devicesGroupBox.ResumeLayout(false);
            this.infoGroupBox.ResumeLayout(false);
            this.logGroupBox.ResumeLayout(false);
            this.logGroupBox.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem s2000ppMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpMenu;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.GroupBox portGroupBox;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.ComboBox portComboBox;
        private System.Windows.Forms.Button refreshPortsButton;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Label baudrateLabel;
        private System.Windows.Forms.ComboBox baudrateComboBox;
        private System.Windows.Forms.Label parityLabel;
        private System.Windows.Forms.ComboBox parityComboBox;
        private System.Windows.Forms.Label stopbitsLabel;
        private System.Windows.Forms.ComboBox stopbitsComboBox;
        private System.Windows.Forms.GroupBox deviceGroupBox;
        private System.Windows.Forms.Label addrLabel;
        private System.Windows.Forms.NumericUpDown addrNumericUpDown;
        private System.Windows.Forms.Button pollDeviceButton;
        private System.Windows.Forms.Button scanButton;
        private System.Windows.Forms.Button stopScanButton;
        private System.Windows.Forms.Label scanRangeLabel;
        private System.Windows.Forms.NumericUpDown scanStartNumericUpDown;
        private System.Windows.Forms.Label scanDashLabel;
        private System.Windows.Forms.NumericUpDown scanEndNumericUpDown;
        private System.Windows.Forms.Label scanTimeoutLabel;
        private System.Windows.Forms.NumericUpDown scanTimeoutNumericUpDown;
        private System.Windows.Forms.Label scanTimeoutUnitLabel;
        private System.Windows.Forms.Label scanHintLabel;
        private System.Windows.Forms.GroupBox devicesGroupBox;
        private System.Windows.Forms.ListBox devicesListBox;
        private System.Windows.Forms.Button clearDevicesButton;
        private System.Windows.Forms.GroupBox infoGroupBox;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.GroupBox logGroupBox;
        private System.Windows.Forms.TextBox logTextBox;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
    }
}

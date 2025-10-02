using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BolidEmulator
{
    public partial class S2000PPWindow : Form
    {
        private Form parent;
        private S2000PPManager manager;
        private bool isConnected = false;
        private bool isScanning = false;
        private bool isClosing = false;
        private Dictionary<int, string> foundDevices = new Dictionary<int, string>();
        private CancellationTokenSource scanStopEvent;

        private TabControl notebook;
        private ComboBox portCombo;
        private ComboBox baudrateCombo;
        private TextBox addressTextBox;
        private ComboBox parityCombo;
        private ComboBox stopbitsCombo;
        private Button connectButton;
        private Button scanButton;
        private Button stopScanButton;
        private Button readConfigButton;
        private ProgressBar progressBar;
        private Label progressLabel;
        private ListBox devicesListBox;
        private Label statusLabel;
        private TextBox logText;

        private ListView zonesTable;
        private ListView relaysTable;
        private ListView divisionsTable;
        private ListView analogTable;

        public S2000PPWindow(Form parent)
        {
            this.parent = parent;
            this.manager = new S2000PPManager(Log);

            InitializeComponent();
            CreateInterface();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Управление С2000-ПП";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            
            // Устанавливаем иконку
            try
            {
                if (File.Exists("icon.ico"))
                {
                    Icon = new Icon("icon.ico");
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки загрузки иконки
            }
        }

        private void CreateInterface()
        {
            // Главная панель с отступами
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // Настройка сетки
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Вкладки
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // Лог
            Controls.Add(mainPanel);

            // Вкладки
            notebook = new TabControl 
            { 
                Dock = DockStyle.Fill
            };
            mainPanel.Controls.Add(notebook, 0, 0);

            CreateConnectionTab();
            CreateInstructionTab();
            CreateZonesTab();
            CreateRelaysTab();
            CreateDivisionsTab();
            CreateAnalogTab();

            CreateLogSection(mainPanel);
        }

        private void CreateConnectionTab()
        {
            var connectionFrame = new TabPage("Подключение");
            notebook.TabPages.Add(connectionFrame);

            // Создаем главную панель с правильной структурой
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(5)
            };

            // Настройка строк
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F)); // Настройки подключения
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // Кнопки управления
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Прогресс-бар
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Список устройств
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));   // Статус

            connectionFrame.Controls.Add(mainPanel);

            // Настройки подключения
            var settingsFrame = new GroupBox
            {
                Text = "Параметры подключения",
                Dock = DockStyle.Fill
            };

            // COM-порт
            var portLabel = new Label { Text = "COM-порт:", Location = new Point(20, 30), Size = new Size(80, 20) };
            portCombo = new ComboBox 
            { 
                Location = new Point(120, 27), 
                Width = 200, 
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var updatePortsButton = new Button { Text = "Обновить", Location = new Point(330, 25), Size = new Size(80, 25) };
            updatePortsButton.Click += (s, e) => UpdatePorts();

            // Скорость
            var baudrateLabel = new Label { Text = "Скорость:", Location = new Point(20, 60), Size = new Size(80, 20) };
            baudrateCombo = new ComboBox
            {
                Location = new Point(120, 57),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            baudrateCombo.Items.AddRange(new[] { "1200", "2400", "9600", "19200", "38400", "57600", "115200" });
            baudrateCombo.SelectedIndex = 2;

            // Адрес
            var addressLabel = new Label { Text = "Адрес С2000-ПП:", Location = new Point(20, 90), Size = new Size(100, 20) };
            addressTextBox = new TextBox { Location = new Point(120, 87), Width = 100, Text = "1" };

            // Четность
            var parityLabel = new Label { Text = "Четность:", Location = new Point(20, 120), Size = new Size(80, 20) };
            parityCombo = new ComboBox
            {
                Location = new Point(120, 117),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            parityCombo.Items.AddRange(new[] { "Нет", "Чет", "Нечет" });
            parityCombo.SelectedIndex = 0;

            // Стоповые биты
            var stopbitsLabel = new Label { Text = "Стоповые биты:", Location = new Point(20, 150), Size = new Size(100, 20) };
            stopbitsCombo = new ComboBox
            {
                Location = new Point(120, 147),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            stopbitsCombo.Items.AddRange(new[] { "1", "2" });
            stopbitsCombo.SelectedIndex = 0;

            settingsFrame.Controls.AddRange(new Control[]
            {
                portLabel, portCombo, updatePortsButton,
                baudrateLabel, baudrateCombo,
                addressLabel, addressTextBox,
                parityLabel, parityCombo,
                stopbitsLabel, stopbitsCombo
            });

            // Кнопки управления
            var buttonsFrame = new Panel 
            { 
                Dock = DockStyle.Fill
            };
            
            connectButton = new Button { Text = "Подключиться", Location = new Point(10, 15), Size = new Size(120, 30) };
            connectButton.Click += (s, e) => ToggleConnection();

            scanButton = new Button { Text = "Сканировать устройства", Location = new Point(140, 15), Size = new Size(150, 30) };
            scanButton.Click += (s, e) => ScanDevices();

            stopScanButton = new Button { Text = "Остановить сканирование", Location = new Point(300, 15), Size = new Size(150, 30), Enabled = false };
            stopScanButton.Click += (s, e) => StopScanning();

            readConfigButton = new Button { Text = "Считать конфигурацию", Location = new Point(460, 15), Size = new Size(150, 30), Enabled = false };
            readConfigButton.Click += (s, e) => ReadConfiguration();

            buttonsFrame.Controls.AddRange(new Control[] { connectButton, scanButton, stopScanButton, readConfigButton });

            // Прогресс-бар для сканирования
            var progressFrame = new Panel 
            { 
                Dock = DockStyle.Fill
            };
            
            progressLabel = new Label { Text = "Готов к сканированию", Location = new Point(10, 10), Size = new Size(200, 20) };
            progressBar = new ProgressBar { Location = new Point(220, 8), Size = new Size(200, 20), Maximum = 127 };
            progressFrame.Controls.AddRange(new Control[] { progressLabel, progressBar });

            // Список найденных устройств
            var devicesFrame = new GroupBox
            {
                Text = "Найденные устройства",
                Dock = DockStyle.Fill
            };

            devicesListBox = new ListBox 
            { 
                Dock = DockStyle.Fill
            };
            devicesListBox.DoubleClick += (s, e) => OnDeviceDoubleClick();
            devicesFrame.Controls.Add(devicesListBox);

            // Статус подключения
            statusLabel = new Label 
            { 
                Text = "Не подключено", 
                Dock = DockStyle.Fill,
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Добавляем все элементы в главную панель
            mainPanel.Controls.Add(settingsFrame, 0, 0);
            mainPanel.Controls.Add(buttonsFrame, 0, 1);
            mainPanel.Controls.Add(progressFrame, 0, 2);
            mainPanel.Controls.Add(devicesFrame, 0, 3);
            mainPanel.Controls.Add(statusLabel, 0, 4);

            UpdatePorts();
        }

        private void CreateInstructionTab()
        {
            var instructionFrame = new TabPage("Инструкция");
            notebook.TabPages.Add(instructionFrame);

            // Создаем прокручиваемую панель
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            instructionFrame.Controls.Add(scrollPanel);

            // Заголовок инструкции
            var titleLabel = new Label
            {
                Text = "Инструкция по подключению преобразователя протокола С2000-ПП",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            scrollPanel.Controls.Add(titleLabel);

            // Переменная для отслеживания текущей позиции Y
            int currentY = 50;

            // Загружаем и отображаем изображение
            try
            {
                // Определяем путь к изображению
                string[] candidatePaths = {
                    "c2000pp.png",
                    Path.Combine(Application.StartupPath, "c2000pp.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "c2000pp.png")
                };

                string imagePath = null;
                foreach (string path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        imagePath = path;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    // Загружаем изображение
                    var originalImage = Image.FromFile(imagePath);
                    
                    // Масштабируем изображение для лучшего отображения
                    int maxWidth = 600;
                    int newWidth = originalImage.Width;
                    int newHeight = originalImage.Height;
                    
                    if (originalImage.Width > maxWidth)
                    {
                        double ratio = (double)maxWidth / originalImage.Width;
                        newWidth = maxWidth;
                        newHeight = (int)(originalImage.Height * ratio);
                    }
                    
                    var scaledImage = new Bitmap(originalImage, newWidth, newHeight);
                    
                    // Создаем PictureBox для изображения
                    var imagePictureBox = new PictureBox
                    {
                        Image = scaledImage,
                        Location = new Point(10, currentY),
                        Size = new Size(newWidth, newHeight),
                        SizeMode = PictureBoxSizeMode.StretchImage
                    };
                    scrollPanel.Controls.Add(imagePictureBox);
                    
                    // Обновляем позицию Y после изображения
                    currentY += newHeight + 10;
                    
                    // Подпись к изображению
                    var captionLabel = new Label
                    {
                        Text = "Схема подключения преобразователя протокола С2000-ПП",
                        Font = new Font("Arial", 10, FontStyle.Italic),
                        Location = new Point(10, currentY),
                        AutoSize = true
                    };
                    scrollPanel.Controls.Add(captionLabel);
                    
                    // Обновляем позицию Y после подписи
                    currentY += 30;
                    
                    // Освобождаем ресурсы
                    originalImage.Dispose();
                }
                else
                {
                    // Если изображение не найдено
                    var errorLabel = new Label
                    {
                        Text = "Изображение не найдено. Ожидалось 'c2000pp.png' рядом с программой",
                        Font = new Font("Arial", 10),
                        ForeColor = Color.Red,
                        Location = new Point(10, currentY),
                        AutoSize = true
                    };
                    scrollPanel.Controls.Add(errorLabel);
                    currentY += 30;
                }
            }
            catch (Exception ex)
            {
                // Если ошибка при загрузке изображения
                var errorLabel = new Label
                {
                    Text = $"Ошибка загрузки изображения: {ex.Message}",
                    Font = new Font("Arial", 10),
                    ForeColor = Color.Red,
                    Location = new Point(10, currentY),
                    AutoSize = true
                };
                scrollPanel.Controls.Add(errorLabel);
                currentY += 30;
            }

            // Текст инструкции
            var instructionText = @"
Преобразователь протокола С2000-ПП предназначен для подключения устройств системы ""Орион"" к системам автоматизации по протоколу Modbus RTU.

ПОДКЛЮЧЕНИЕ:

1. ПИТАНИЕ:
   • Подключите питание 12В к клеммам ""12B"" и ""0B"" на блоке ""Орион""
   • Соблюдайте полярность: ""+"" к ""12B"", ""-"" к ""0B""

2. ПОДКЛЮЧЕНИЕ К С2000-ПП:
   • Подключите линию RS-485 от С2000-ПП к клеммам ""A"" и ""B"" на блоке ""Орион""
   • Клемма ""A"" - положительный сигнал RS-485
   • Клемма ""B"" - отрицательный сигнал RS-485

3. ПОДКЛЮЧЕНИЕ К СИСТЕМЕ АВТОМАТИЗАЦИИ:
   • Подключите линию Modbus RTU к клеммам ""A"" и ""B"" на блоке ""Modbus""
   • Клемма ""A"" - положительный сигнал Modbus
   • Клемма ""B"" - отрицательный сигнал Modbus

4. НАСТРОЙКИ ПРОТОКОЛА:
   • Скорость: 9600 бод (по умолчанию)
   • Четность: Нет
   • Стоповые биты: 1
   • Адрес С2000-ПП: 1 (по умолчанию)

5. ПРОВЕРКА ПОДКЛЮЧЕНИЯ:
   • Убедитесь, что все соединения надежны
   • Проверьте правильность подключения питания
   • Убедитесь в отсутствии коротких замыканий

ВАЖНО:
• Не подключайте питание при подключении проводов
• Используйте экранированный кабель для RS-485 и Modbus
• Соблюдайте максимальную длину линии (до 1200м)
• При необходимости используйте терминаторы на концах линии";

            var instructionLabel = new Label
            {
                Text = instructionText,
                Location = new Point(10, currentY), // Размещаем после изображения с правильным отступом
                Font = new Font("Arial", 10),
                AutoSize = true,
                MaximumSize = new Size(800, 0) // Ограничиваем ширину для переноса текста
            };
            scrollPanel.Controls.Add(instructionLabel);
        }

        private void CreateZonesTab()
        {
            var zonesFrame = new TabPage("Зоны");
            notebook.TabPages.Add(zonesFrame);

            // Таблица зон
            zonesTable = new ListView 
            { 
                Dock = DockStyle.Fill,
                View = View.Details, 
                FullRowSelect = true,
                GridLines = true
            };
            zonesTable.Columns.Add("№ Зоны", 80);
            zonesTable.Columns.Add("Адрес", 80);
            zonesTable.Columns.Add("№ ШС", 80);
            zonesTable.Columns.Add("Тип", 120);
            zonesTable.Columns.Add("Состояние", 200);

            // Кнопки управления зонами
            var zonesButtonsFrame = new Panel 
            { 
                Dock = DockStyle.Bottom,
                Height = 200
            };

            // Основные кнопки
            var mainButtonsFrame = new Panel 
            { 
                Dock = DockStyle.Top,
                Height = 40
            };
            
            var updateZonesButton = new Button { Text = "Обновить состояния", Location = new Point(10, 5), Size = new Size(150, 30) };
            updateZonesButton.Click += (s, e) => UpdateZonesStates();

            var armZoneButton = new Button { Text = "Взять на охрану", Location = new Point(170, 5), Size = new Size(120, 30) };
            armZoneButton.Click += (s, e) => ArmZone();

            var disarmZoneButton = new Button { Text = "Снять с охраны", Location = new Point(300, 5), Size = new Size(120, 30) };
            disarmZoneButton.Click += (s, e) => DisarmZone();

            mainButtonsFrame.Controls.AddRange(new Control[] { updateZonesButton, armZoneButton, disarmZoneButton });

            // Группа управления контролем ШС
            var controlFrame = new GroupBox
            {
                Text = "Управление контролем ШС",
                Dock = DockStyle.Top,
                Height = 50
            };

            var enableControlButton = new Button { Text = "Включить контроль ШС (111)", Location = new Point(10, 20), Size = new Size(180, 25) };
            enableControlButton.Click += (s, e) => EnableZoneControl();

            var disableControlButton = new Button { Text = "Выключить контроль ШС (112)", Location = new Point(200, 20), Size = new Size(180, 25) };
            disableControlButton.Click += (s, e) => DisableZoneControl();

            controlFrame.Controls.AddRange(new Control[] { enableControlButton, disableControlButton });

            // Группа управления автоматикой
            var automationFrame = new GroupBox
            {
                Text = "Управление автоматикой",
                Dock = DockStyle.Top,
                Height = 50
            };

            var enableAutomationButton = new Button { Text = "Включить автоматику (148)", Location = new Point(10, 20), Size = new Size(180, 25) };
            enableAutomationButton.Click += (s, e) => EnableAutomation();

            var disableAutomationButton = new Button { Text = "Отключить автоматику (142)", Location = new Point(200, 20), Size = new Size(180, 25) };
            disableAutomationButton.Click += (s, e) => DisableAutomation();

            automationFrame.Controls.AddRange(new Control[] { enableAutomationButton, disableAutomationButton });

            // Группа управления АСПТ
            var asptFrame = new GroupBox
            {
                Text = "Управление АСПТ",
                Dock = DockStyle.Top,
                Height = 50
            };

            var startAsptButton = new Button { Text = "Пуск АСПТ (146)", Location = new Point(10, 20), Size = new Size(150, 25) };
            startAsptButton.Click += (s, e) => StartAspt();

            var resetAsptButton = new Button { Text = "Сброс пуска АСПТ (143)", Location = new Point(170, 20), Size = new Size(150, 25) };
            resetAsptButton.Click += (s, e) => ResetAspt();

            asptFrame.Controls.AddRange(new Control[] { startAsptButton, resetAsptButton });

            // Группа тестирования
            var testFrame = new GroupBox
            {
                Text = "Тестирование",
                Dock = DockStyle.Top,
                Height = 50
            };

            var testZoneButton = new Button { Text = "Тест вход (19)", Location = new Point(10, 20), Size = new Size(120, 25) };
            testZoneButton.Click += (s, e) => TestZone();

            var enterTestButton = new Button { Text = "Вход в тест (20)", Location = new Point(140, 20), Size = new Size(120, 25) };
            enterTestButton.Click += (s, e) => EnterTestMode();

            var exitTestButton = new Button { Text = "Выход из теста (21)", Location = new Point(270, 20), Size = new Size(120, 25) };
            exitTestButton.Click += (s, e) => ExitTestMode();

            testFrame.Controls.AddRange(new Control[] { testZoneButton, enterTestButton, exitTestButton });

            zonesButtonsFrame.Controls.AddRange(new Control[] { mainButtonsFrame, controlFrame, automationFrame, asptFrame, testFrame });
            zonesFrame.Controls.AddRange(new Control[] { zonesTable, zonesButtonsFrame });
        }

        private void CreateRelaysTab()
        {
            var relaysFrame = new TabPage("Реле");
            notebook.TabPages.Add(relaysFrame);

            // Таблица реле
            relaysTable = new ListView 
            { 
                Dock = DockStyle.Fill,
                View = View.Details, 
                FullRowSelect = true,
                GridLines = true
            };
            relaysTable.Columns.Add("№ Реле", 80);
            relaysTable.Columns.Add("Адрес", 80);
            relaysTable.Columns.Add("№ в приборе", 100);
            relaysTable.Columns.Add("Состояние", 120);

            // Кнопки управления реле
            var relaysButtonsFrame = new Panel 
            { 
                Dock = DockStyle.Bottom,
                Height = 50
            };

            var updateRelaysButton = new Button { Text = "Обновить состояния", Location = new Point(10, 10), Size = new Size(150, 30) };
            updateRelaysButton.Click += (s, e) => UpdateRelaysStates();

            var turnOnRelayButton = new Button { Text = "Включить", Location = new Point(170, 10), Size = new Size(100, 30) };
            turnOnRelayButton.Click += (s, e) => TurnOnRelay();

            var turnOffRelayButton = new Button { Text = "Выключить", Location = new Point(280, 10), Size = new Size(100, 30) };
            turnOffRelayButton.Click += (s, e) => TurnOffRelay();

            relaysButtonsFrame.Controls.AddRange(new Control[] { updateRelaysButton, turnOnRelayButton, turnOffRelayButton });
            relaysFrame.Controls.AddRange(new Control[] { relaysTable, relaysButtonsFrame });
        }

        private void CreateDivisionsTab()
        {
            var divisionsFrame = new TabPage("Разделы");
            notebook.TabPages.Add(divisionsFrame);

            // Таблица разделов
            divisionsTable = new ListView 
            { 
                Dock = DockStyle.Fill,
                View = View.Details, 
                FullRowSelect = true,
                GridLines = true
            };
            divisionsTable.Columns.Add("№ Раздела", 100);
            divisionsTable.Columns.Add("Адрес", 80);
            divisionsTable.Columns.Add("Состояние", 150);
            divisionsTable.Columns.Add("Описание", 200);

            // Кнопки управления разделами
            var divisionsButtonsFrame = new Panel 
            { 
                Dock = DockStyle.Bottom,
                Height = 50
            };

            var updateDivisionsButton = new Button { Text = "Обновить состояния", Location = new Point(10, 10), Size = new Size(150, 30) };
            updateDivisionsButton.Click += (s, e) => UpdateDivisionsStates();

            var armDivisionButton = new Button { Text = "Взять на охрану", Location = new Point(170, 10), Size = new Size(120, 30) };
            armDivisionButton.Click += (s, e) => ArmDivision();

            var disarmDivisionButton = new Button { Text = "Снять с охраны", Location = new Point(300, 10), Size = new Size(120, 30) };
            disarmDivisionButton.Click += (s, e) => DisarmDivision();

            divisionsButtonsFrame.Controls.AddRange(new Control[] { updateDivisionsButton, armDivisionButton, disarmDivisionButton });
            divisionsFrame.Controls.AddRange(new Control[] { divisionsTable, divisionsButtonsFrame });
        }

        private void CreateAnalogTab()
        {
            var analogFrame = new TabPage("Аналоговые значения");
            notebook.TabPages.Add(analogFrame);

            // Таблица аналоговых значений
            analogTable = new ListView 
            { 
                Dock = DockStyle.Fill,
                View = View.Details, 
                FullRowSelect = true,
                GridLines = true
            };
            analogTable.Columns.Add("№ Зоны", 80);
            analogTable.Columns.Add("Тип", 150);
            analogTable.Columns.Add("Температура (°C)", 120);
            analogTable.Columns.Add("Влажность (%)", 120);
            analogTable.Columns.Add("CO (ppm)", 100);

            // Кнопки управления
            var analogButtonsFrame = new Panel 
            { 
                Dock = DockStyle.Bottom,
                Height = 50
            };
            
            var updateAnalogButton = new Button { Text = "Обновить значения", Location = new Point(10, 10), Size = new Size(150, 30) };
            updateAnalogButton.Click += (s, e) => UpdateAnalogValues();

            analogButtonsFrame.Controls.Add(updateAnalogButton);
            analogFrame.Controls.AddRange(new Control[] { analogTable, analogButtonsFrame });
        }

        private void CreateLogSection(TableLayoutPanel mainPanel)
        {
            var logFrame = new GroupBox
            {
                Text = "Лог",
                Dock = DockStyle.Fill
            };

            logText = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9)
            };

            logFrame.Controls.Add(logText);
            mainPanel.Controls.Add(logFrame, 0, 1);
        }

        private void Log(string message)
        {
            if (isClosing) return;

            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}\r\n";

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        logText.AppendText(logMessage);
                        logText.SelectionStart = logText.Text.Length;
                        logText.ScrollToCaret();
                    }));
                }
                else
                {
                    logText.AppendText(logMessage);
                    logText.SelectionStart = logText.Text.Length;
                    logText.ScrollToCaret();
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при закрытии окна
            }
        }

        private void UpdatePorts()
        {
            var ports = manager.GetAvailablePorts();
            portCombo.Items.Clear();
            portCombo.Items.AddRange(ports.ToArray());
            if (ports.Count > 0)
            {
                portCombo.SelectedIndex = 0;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists("s2000pp_settings.json"))
                {
                    var json = File.ReadAllText("s2000pp_settings.json");
                    var settings = Newtonsoft.Json.Linq.JObject.Parse(json);

                    if (settings["com_port"] != null)
                    {
                        string portValue = settings["com_port"].ToString();
                        for (int i = 0; i < portCombo.Items.Count; i++)
                        {
                            if (portCombo.Items[i].ToString().StartsWith(portValue))
                            {
                                portCombo.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    if (settings["baudrate"] != null)
                        baudrateCombo.Text = settings["baudrate"].ToString();
                    if (settings["pp_address"] != null)
                        addressTextBox.Text = settings["pp_address"].ToString();
                    if (settings["parity"] != null)
                        parityCombo.Text = settings["parity"].ToString();
                    if (settings["stopbits"] != null)
                        stopbitsCombo.Text = settings["stopbits"].ToString();

                    Log($"Настройки подключения загружены: {settings["com_port"]}, {settings["baudrate"]} бод, адрес {settings["pp_address"]}");
                }
            }
            catch (Exception e)
            {
                Log($"Ошибка загрузки настроек: {e.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new
                {
                    com_port = portCombo.Text,
                    baudrate = baudrateCombo.Text,
                    pp_address = addressTextBox.Text,
                    parity = parityCombo.Text,
                    stopbits = stopbitsCombo.Text,
                    last_saved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText("s2000pp_settings.json", json);

                Log("Настройки подключения сохранены");
            }
            catch (Exception e)
            {
                Log($"Ошибка сохранения настроек: {e.Message}");
            }
        }

        private void OnDeviceDoubleClick()
        {
            if (isClosing) return;

            if (devicesListBox.SelectedItem == null) return;

            var deviceText = devicesListBox.SelectedItem.ToString();
            try
            {
                if (deviceText.Contains("адрес"))
                {
                    var addressPart = deviceText.Split(new[] { "адрес" }, StringSplitOptions.None)[1].Split(',')[0].Trim();
                    int address = int.Parse(addressPart);

                    if (isScanning)
                    {
                        Log("Остановка сканирования для подключения к устройству...");
                        StopScanning();
                        Task.Delay(1000).ContinueWith(_ => ConnectToDevice(address));
                    }
                    else
                    {
                        ConnectToDevice(address);
                    }
                }
            }
            catch (Exception e)
            {
                Log($"Ошибка обработки выбранного устройства: {e.Message}");
            }
        }

        private void ConnectToDevice(int address)
        {
            addressTextBox.Text = address.ToString();
            Log($"Автоматическое подключение к устройству на адресе {address}");

            if (Connect())
            {
                Log("Автоматическое считывание конфигурации...");
                Task.Delay(100).ContinueWith(_ => ReadConfiguration());
            }
        }

        private void UpdateDevicesList()
        {
            if (isClosing) return;

            try
            {
                devicesListBox.Items.Clear();
                foreach (var kvp in foundDevices)
                {
                    devicesListBox.Items.Add(kvp.Value);
                }

                if (foundDevices.Count > 0)
                {
                    Log($"Обновлен список устройств: найдено {foundDevices.Count} устройств");
                }
                else
                {
                    Log("Список устройств пуст");
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки при закрытии окна
            }
        }

        private void ToggleConnection()
        {
            if (isConnected)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        private bool Connect()
        {
            string portText = portCombo.Text;
            if (string.IsNullOrEmpty(portText))
            {
                MessageBox.Show("Выберите COM-порт", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string port = portText.Contains(" - ") ? portText.Split(new[] { " - " }, StringSplitOptions.None)[0] : portText;
            int baudrate = int.Parse(baudrateCombo.Text);
            int address = int.Parse(addressTextBox.Text);

            string parity = parityCombo.Text == "Чет" ? "E" : parityCombo.Text == "Нечет" ? "O" : "N";
            int stopbits = int.Parse(stopbitsCombo.Text);

            if (manager.Connect(port, baudrate, parity, stopbits, address))
            {
                isConnected = true;
                connectButton.Text = "Отключиться";
                readConfigButton.Enabled = true;
                statusLabel.Text = "Подключено";
                Log($"Подключено к {port} (адрес {address})");
                SaveSettings();
                return true;
            }
            else
            {
                Log("Не удалось подключиться к устройству");
                return false;
            }
        }

        private void Disconnect()
        {
            manager.Disconnect();
            isConnected = false;
            connectButton.Text = "Подключиться";
            readConfigButton.Enabled = false;
            statusLabel.Text = "Не подключено";
            Log("Отключено");
        }

        private void ScanDevices()
        {
            if (isScanning) return;

            string portText = portCombo.Text;
            if (string.IsNullOrEmpty(portText))
            {
                Log("Выберите COM-порт для сканирования");
                return;
            }

            string port = portText.Contains(" - ") ? portText.Split(new[] { " - " }, StringSplitOptions.None)[0] : portText;
            int baudrate = int.Parse(baudrateCombo.Text);
            string parity = parityCombo.Text == "Чет" ? "E" : parityCombo.Text == "Нечет" ? "O" : "N";
            int stopbits = int.Parse(stopbitsCombo.Text);

            isScanning = true;
            scanStopEvent = new CancellationTokenSource();
            scanButton.Enabled = false;
            stopScanButton.Enabled = true;
            progressBar.Value = 0;
            progressLabel.Text = "Сканирование устройств...";

            foundDevices.Clear();
            UpdateDevicesList();

            Task.Run(() =>
            {
                try
                {
                    Log("Начинаем сканирование устройств С2000-ПП...");
                    var foundDevicesResult = manager.ScanDevices(port, baudrate, parity, stopbits,
                        (address, description, currentAddress, totalAddresses) =>
                        {
                            if (isClosing) return;

                            foundDevices[address] = description;
                            Invoke(new Action(() => UpdateDevicesList()));
                            Log($"Найдено устройство на адресе {address}: {description}");

                            double progress = (currentAddress / (double)totalAddresses) * 100;
                            Invoke(new Action(() =>
                            {
                                progressBar.Value = currentAddress;
                                progressLabel.Text = $"Найдено устройство на адресе {address} ({progress:F1}%)";
                            }));
                        },
                        scanStopEvent,
                        (currentAddress, totalAddresses) =>
                        {
                            if (isClosing) return;

                            double progress = (currentAddress / (double)totalAddresses) * 100;
                            Invoke(new Action(() =>
                            {
                                progressBar.Value = currentAddress;
                                progressLabel.Text = $"Опрашивается адрес {currentAddress} ({progress:F1}%)";
                            }));
                            Log($"Опрашивается адрес {currentAddress}/{totalAddresses}");
                        });

                    Invoke(new Action(() => FinishScanning()));

                    if (foundDevicesResult.Count > 0)
                    {
                        Log($"Сканирование завершено. Найдено устройств: {foundDevicesResult.Count}");
                        foreach (var kvp in foundDevicesResult)
                        {
                            Log($"Найдено: {kvp.Value}");
                        }
                    }
                    else
                    {
                        Log("Сканирование завершено. Устройства не найдены");
                    }
                }
                catch (Exception e)
                {
                    Log($"Ошибка при сканировании: {e.Message}");
                    Invoke(new Action(() => FinishScanning()));
                }
            });
        }

        private void FinishScanning()
        {
            isScanning = false;
            scanButton.Enabled = true;
            stopScanButton.Enabled = false;
            progressBar.Value = 127;
            progressLabel.Text = "Сканирование завершено (100%)";
        }

        private void StopScanning()
        {
            if (!isScanning) return;

            Log("Остановка сканирования...");
            scanStopEvent?.Cancel();
            isScanning = false;
            scanButton.Enabled = true;
            stopScanButton.Enabled = false;
            progressBar.Value = 127;
            progressLabel.Text = "Сканирование остановлено (100%)";
        }

        private void ReadConfiguration()
        {
            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Task.Run(() =>
            {
                if (manager.ReadConfiguration())
                {
                    Invoke(new Action(() => PopulateTables()));
                }
                else
                {
                    Invoke(new Action(() => MessageBox.Show("Не удалось считать конфигурацию", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
            });
        }

        private void PopulateTables()
        {
            // Очищаем таблицы
            zonesTable.Items.Clear();
            relaysTable.Items.Clear();
            divisionsTable.Items.Clear();

            // Заполняем таблицу зон
            foreach (var kvp in manager.ZonesConfig)
            {
                var zoneInfo = kvp.Value;
                var item = new ListViewItem(zoneInfo.Number.ToString());
                item.SubItems.Add(zoneInfo.Address.ToString());
                item.SubItems.Add(zoneInfo.Shs.ToString());
                item.SubItems.Add($"Тип {zoneInfo.ZoneType}");
                item.SubItems.Add(zoneInfo.CurrentState.ToString());
                zonesTable.Items.Add(item);
            }

            // Заполняем таблицу реле
            foreach (var kvp in manager.RelaysConfig)
            {
                var relayInfo = kvp.Value;
                string stateText = relayInfo.IsOn ? "Включено" : "Выключено";
                var item = new ListViewItem(relayInfo.Number.ToString());
                item.SubItems.Add(relayInfo.Address.ToString());
                item.SubItems.Add(relayInfo.RelayInDevice.ToString());
                item.SubItems.Add(stateText);
                relaysTable.Items.Add(item);
            }

            // Заполняем таблицу разделов
            foreach (var kvp in manager.DivisionsConfig)
            {
                var divisionInfo = kvp.Value;
                var item = new ListViewItem(divisionInfo.Number.ToString());
                item.SubItems.Add(divisionInfo.Address.ToString());
                item.SubItems.Add(divisionInfo.CurrentState.ToString());
                item.SubItems.Add(divisionInfo.Description);
                divisionsTable.Items.Add(item);
            }

            Log("Таблицы обновлены");
        }

        private void UpdateZonesStates()
        {
            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            manager.UpdateZonesStates((success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void ArmZone()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.ArmZone(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void DisarmZone()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.DisarmZone(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void RefreshZonesTable()
        {
            zonesTable.Items.Clear();
            foreach (var kvp in manager.ZonesConfig)
            {
                var zoneInfo = kvp.Value;
                string stateDescription = zoneInfo.CurrentState.ToString();
                if (zoneInfo.RawStateCode.HasValue)
                {
                    stateDescription = $"{stateDescription} (0x{zoneInfo.RawStateCode.Value:X4})";
                }

                var item = new ListViewItem(zoneInfo.Number.ToString());
                item.SubItems.Add(zoneInfo.Address.ToString());
                item.SubItems.Add(zoneInfo.Shs.ToString());
                item.SubItems.Add($"Тип {zoneInfo.ZoneType}");
                item.SubItems.Add(stateDescription);
                zonesTable.Items.Add(item);
            }
        }

        private void UpdateRelaysStates()
        {
            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            manager.UpdateRelaysStates((success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshRelaysTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void TurnOnRelay()
        {
            if (relaysTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите реле", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int relayNum = int.Parse(relaysTable.SelectedItems[0].Text);
            manager.TurnOnRelay(relayNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshRelaysTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void TurnOffRelay()
        {
            if (relaysTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите реле", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int relayNum = int.Parse(relaysTable.SelectedItems[0].Text);
            manager.TurnOffRelay(relayNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshRelaysTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void RefreshRelaysTable()
        {
            relaysTable.Items.Clear();
            foreach (var kvp in manager.RelaysConfig)
            {
                var relayInfo = kvp.Value;
                string stateText = relayInfo.IsOn ? "Включено" : "Выключено";
                var item = new ListViewItem(relayInfo.Number.ToString());
                item.SubItems.Add(relayInfo.Address.ToString());
                item.SubItems.Add(relayInfo.RelayInDevice.ToString());
                item.SubItems.Add(stateText);
                relaysTable.Items.Add(item);
            }
        }

        private void UpdateDivisionsStates()
        {
            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            manager.UpdateDivisionsStates((success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshDivisionsTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void ArmDivision()
        {
            if (divisionsTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите раздел", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int divisionNum = int.Parse(divisionsTable.SelectedItems[0].Text);
            manager.ArmDivision(divisionNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshDivisionsTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void DisarmDivision()
        {
            if (divisionsTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите раздел", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int divisionNum = int.Parse(divisionsTable.SelectedItems[0].Text);
            manager.DisarmDivision(divisionNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshDivisionsTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void RefreshDivisionsTable()
        {
            divisionsTable.Items.Clear();
            foreach (var kvp in manager.DivisionsConfig)
            {
                var divisionInfo = kvp.Value;
                var item = new ListViewItem(divisionInfo.Number.ToString());
                item.SubItems.Add(divisionInfo.Address.ToString());
                item.SubItems.Add(divisionInfo.CurrentState.ToString());
                item.SubItems.Add(divisionInfo.Description);
                divisionsTable.Items.Add(item);
            }
        }

        private void UpdateAnalogValues()
        {
            Log("Обновление аналоговых значений...");
            // TODO: Реализовать обновление аналоговых значений
        }

        private void EnableZoneControl()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.EnableZoneControl(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void DisableZoneControl()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.DisableZoneControl(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void EnableAutomation()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.EnableAutomation(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void DisableAutomation()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.DisableAutomation(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void StartAspt()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.StartAspt(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void ResetAspt()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.ResetAspt(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void TestZone()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.TestZone(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void EnterTestMode()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.EnterTestMode(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void ExitTestMode()
        {
            if (zonesTable.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите зону", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isConnected)
            {
                MessageBox.Show("Нет подключения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int zoneNum = int.Parse(zonesTable.SelectedItems[0].Text);
            manager.ExitTestMode(zoneNum, (success, message) =>
            {
                if (success)
                {
                    Log(message);
                    RefreshZonesTable();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isClosing = true;

            if (isScanning)
            {
                StopScanning();
            }

            if (isConnected)
            {
                Disconnect();
            }

            try
            {
                Log("Окно управления С2000-ПП закрыто. Теперь можно подключиться к COM-порту в основной программе.");
            }
            catch
            {
                // Игнорируем ошибки при закрытии
            }

            base.OnFormClosing(e);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BolidEmulator
{
    public class BranchButton
    {
        public int BranchNum { get; set; }
        public BranchState State { get; set; }
        public Action<int> OnClick { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public Button Button { get; set; }

        public BranchButton(Control parent, int branchNum, BranchState state, Action<int> onClick, DeviceInfo deviceInfo = null)
        {
            BranchNum = branchNum;
            State = state;
            OnClick = onClick;
            DeviceInfo = deviceInfo;

            Button = new Button
            {
                Text = GetButtonText(),
                Size = new Size(100, 50),
                Font = new Font("Arial", 9, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };
            
            // Создаем контекстное меню для кнопки
            Button.ContextMenuStrip = CreateContextMenu();

            UpdateButtonStyle();
            Button.Click += (s, e) => OnClick(branchNum);
            
            // Убираем дублирующий обработчик MouseDown, так как ContextMenuStrip уже обрабатывает правый клик
            // Button.MouseDown += (s, e) => { ... };
        }

        private string GetButtonText()
        {
            return $"ШС {BranchNum}\n{GetStateText(State)}";
        }

        private string GetStateText(BranchState state)
        {
            switch (state)
            {
                case BranchState.ARMED: return "Взят";
                case BranchState.DISARMED: return "Снят";
                case BranchState.ALARM: return "Тревога";
                case BranchState.FAULT: return "Неисправность";
                case BranchState.BYPASS: return "Обход";
                default: return "Неизвестно";
            }
        }

        private void UpdateButtonStyle()
        {
            switch (State)
            {
                case BranchState.ARMED:
                    Button.BackColor = Color.Green;
                    Button.ForeColor = Color.White;
                    break;
                case BranchState.DISARMED:
                    Button.BackColor = Color.Red;
                    Button.ForeColor = Color.White;
                    break;
                case BranchState.ALARM:
                    Button.BackColor = Color.Orange;
                    Button.ForeColor = Color.White;
                    break;
                case BranchState.FAULT:
                    Button.BackColor = Color.Purple;
                    Button.ForeColor = Color.White;
                    break;
                case BranchState.BYPASS:
                    Button.BackColor = Color.Blue;
                    Button.ForeColor = Color.White;
                    break;
                default:
                    Button.BackColor = Color.LightGray;
                    Button.ForeColor = Color.Black;
                    break;
            }
        }

        public void UpdateState(BranchState newState)
        {
            State = newState;
            Button.Text = GetButtonText();
            UpdateButtonStyle();
            
            // Обновляем контекстное меню при изменении состояния
            Button.ContextMenuStrip = CreateContextMenu();
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            if (DeviceInfo == null) return contextMenu;

            int adcValue = DeviceInfo.AdcValues.ContainsKey(BranchNum) ? DeviceInfo.AdcValues[BranchNum] : 0;
            float resistance = DeviceInfo.Resistances.ContainsKey(BranchNum) ? DeviceInfo.Resistances[BranchNum] : 0.0f;

            string resistanceDisplay = (resistance == float.PositiveInfinity || resistance <= 0) ? "Обрыв" : $"{resistance:F2} кОм";

            var protocol = new BolidProtocol();
            int? currentStateCode = GetStateCode(State);
            
            string adcInterpretation = protocol.InterpretAdcForDevice(
                adcValue,
                DeviceInfo.DeviceType.DeviceCode,
                currentStateCode,
                BranchNum
            );

            string sensorType = "Неизвестен";
            if (new[] { 9, 41, 61, 81 }.Contains(DeviceInfo.DeviceType.DeviceCode) && currentStateCode.HasValue)
            {
                sensorType = protocol.DetectSensorType(adcValue, currentStateCode.Value, BranchNum);
            }

            // Добавляем элементы в контекстное меню
            var titleItem = new ToolStripMenuItem($"Шлейф {BranchNum}");
            titleItem.Enabled = false;
            contextMenu.Items.Add(titleItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var stateItem = new ToolStripMenuItem($"Состояние: {GetStateText(State)}");
            stateItem.Enabled = false;
            contextMenu.Items.Add(stateItem);
            
            var adcItem = new ToolStripMenuItem($"ADC код: {adcValue}");
            adcItem.Enabled = false;
            contextMenu.Items.Add(adcItem);
            
            var deviceItem = new ToolStripMenuItem($"Устройство: {DeviceInfo.DeviceType.Name}");
            deviceItem.Enabled = false;
            contextMenu.Items.Add(deviceItem);

            if (new[] { 1, 2, 11, 15, 26, 32, 34 }.Contains(DeviceInfo.DeviceType.DeviceCode))
            {
                var resistanceItem = new ToolStripMenuItem($"Сопротивление: {resistanceDisplay}");
                resistanceItem.Enabled = false;
                contextMenu.Items.Add(resistanceItem);
            }
            else
            {
                var sensorItem = new ToolStripMenuItem($"Тип датчика: {sensorType}");
                sensorItem.Enabled = false;
                contextMenu.Items.Add(sensorItem);
                
                var paramItem = new ToolStripMenuItem($"Параметр: {adcInterpretation}");
                paramItem.Enabled = false;
                contextMenu.Items.Add(paramItem);
            }

            return contextMenu;
        }


        private int? GetStateCode(BranchState state)
        {
            switch (state)
            {
                case BranchState.ARMED: return 24;
                case BranchState.DISARMED: return 109;
                case BranchState.ALARM: return 3;
                case BranchState.FAULT: return 45;
                case BranchState.BYPASS: return 111;
                default: return null;
            }
        }
    }

    public class RelayButton
    {
        public int RelayNum { get; set; }
        public bool State { get; set; }
        public Action<int> OnClick { get; set; }
        public Button Button { get; set; }

        public RelayButton(Control parent, int relayNum, bool state, Action<int> onClick)
        {
            RelayNum = relayNum;
            State = state;
            OnClick = onClick;

            Button = new Button
            {
                Text = GetButtonText(),
                Size = new Size(80, 50),
                Font = new Font("Arial", 9, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };

            UpdateButtonStyle();
            Button.Click += (s, e) => OnClick(relayNum);
        }

        private string GetButtonText()
        {
            string status = State ? "ВКЛ" : "ВЫКЛ";
            return $"Реле {RelayNum}\n{status}";
        }

        private void UpdateButtonStyle()
        {
            if (State)
            {
                Button.BackColor = Color.Green;
                Button.ForeColor = Color.White;
            }
            else
            {
                Button.BackColor = Color.Red;
                Button.ForeColor = Color.White;
            }
        }

        public void UpdateState(bool newState)
        {
            State = newState;
            Button.Text = GetButtonText();
            UpdateButtonStyle();
        }
    }

    public class RelayButtonWithType
    {
        public int RelayNum { get; set; }
        public bool State { get; set; }
        public string RelayType { get; set; }
        public Action<int> OnClick { get; set; }
        public Button Button { get; set; }

        public RelayButtonWithType(Control parent, int relayNum, bool state, string relayType, Action<int> onClick)
        {
            RelayNum = relayNum;
            State = state;
            RelayType = relayType;
            OnClick = onClick;

            Button = new Button
            {
                Text = GetButtonText(),
                Size = new Size(100, 60),
                Font = new Font("Arial", 8, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };

            UpdateButtonStyle();
            Button.Click += (s, e) => OnClick(relayNum);
        }

        private string GetButtonText()
        {
            string status = State ? "ВКЛ" : "ВЫКЛ";
            return $"Реле {RelayNum}\n{status}\n{RelayType}";
        }

        private void UpdateButtonStyle()
        {
            if (State)
            {
                Button.BackColor = Color.Green;
                Button.ForeColor = Color.White;
            }
            else
            {
                Button.BackColor = Color.Red;
                Button.ForeColor = Color.White;
            }
        }

        public void UpdateState(bool newState)
        {
            State = newState;
            Button.Text = GetButtonText();
            UpdateButtonStyle();
        }
    }

    public partial class DeviceControlPanel : Form
    {
        private Form parent;
        private DeviceManager deviceManager;
        private DeviceInfo deviceInfo;
        private Action<string> logCallback;
        private Control uiControl; // Для безопасного вызова UI операций
        private Dictionary<int, BranchButton> branchButtons;
        private Dictionary<int, object> relayButtons;
        private bool isClosing = false;
        private Dictionary<int, BranchState> previousBranchStates;
        private bool alarmSoundEnabled = true;
        private int updateCallbackId;
        private bool pollingInProgress = false;
        private bool pollingStopped = false;
        private int currentPolledCount = 0;
        private bool autoPollingActive = false;
        private ProgressBar progressBar;
        private Label progressStatusLabel;
        private CheckBox autoPollCheckBox;
        private Button soundAlarmButton;
        private SoundAlarm soundAlarm;
        
        // Метки для отображения параметров питания МИП/РИП
        private Label outputVoltageValueLabel;
        private Label loadCurrentValueLabel;
        private Label batteryVoltageValueLabel;
        private Label chargerStatusValueLabel;
        private Label networkVoltageValueLabel;
        private Label lastUpdateValueLabel;

        // Безопасный вызов logCallback из любого потока
        private void SafeLogCallback(string message)
        {
            if (uiControl != null && uiControl.InvokeRequired)
            {
                uiControl.Invoke(new Action<string>(SafeLogCallback), message);
            }
            else
            {
                logCallback?.Invoke(message);
            }
        }

        // Безопасное обновление UI элементов из любого потока
        private void SafeUpdateUI(Action action)
        {
            if (uiControl != null && uiControl.InvokeRequired)
            {
                uiControl.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public DeviceControlPanel(Form parent, DeviceManager deviceManager, DeviceInfo deviceInfo, Action<string> logCallback = null)
        {
            this.parent = parent;
            this.deviceManager = deviceManager;
            this.deviceInfo = deviceInfo;
            this.logCallback = logCallback ?? (msg => Console.WriteLine(msg));
            this.uiControl = this; // Используем форму как UI control

            branchButtons = new Dictionary<int, BranchButton>();
            relayButtons = new Dictionary<int, object>();
            previousBranchStates = new Dictionary<int, BranchState>();
            soundAlarm = new SoundAlarm();
            
            // Пытаемся найти файл alarm.wav в текущей директории
            string currentDir = Directory.GetCurrentDirectory();
            string alarmFile = Path.Combine(currentDir, "alarm.wav");
            if (File.Exists(alarmFile))
            {
                soundAlarm.SetAlarmFile(alarmFile);
                SafeLogCallback($"Найден файл звуковой сигнализации: {alarmFile}");
            }
            else
            {
                SafeLogCallback($"Файл alarm.wav не найден в {currentDir}");
            }

            InitializeComponent();
            CreateInterface();
            updateCallbackId = deviceManager.AddUpdateCallback(OnDeviceUpdate);

            if (deviceInfo.DeviceType.MaxBranches > 0)
            {
                UpdateBranchStates();
                CheckActiveAlarms();
            }
        }

        private void InitializeComponent()
        {
            Text = $"Управление {deviceInfo.DeviceType.Name} (адрес {deviceInfo.Address})";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            
            // Добавляем обработчик изменения размера окна
            this.Resize += OnFormResize;
        }

        private void CreateInterface()
        {
            // Специальная обработка для С2000-ПП
            if (deviceInfo.DeviceType.DeviceCode == 36)
            {
                var mainContainer = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10)
                };
                CreateS2000ppInfoInterface(mainContainer);
                Controls.Add(mainContainer);
                return;
            }

            // Специальная обработка для МИП/РИП устройств
            int[] powerDeviceCodes = { 33, 38, 39, 48, 49, 54, 55, 79, 80 };
            SafeLogCallback($"Проверка устройства: код={deviceInfo.DeviceType.DeviceCode}, название={deviceInfo.DeviceType.Name}");
            if (powerDeviceCodes.Contains(deviceInfo.DeviceType.DeviceCode))
            {
                var mainContainer = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10)
                };
                CreatePowerDeviceInterface(mainContainer);
                Controls.Add(mainContainer);
                return;
            }

            // Переменные для отслеживания прогресса
            pollingInProgress = false;
            pollingStopped = false;
            currentPolledCount = 0;
            autoPollingActive = false;

            // Создаем основную таблицу макета
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4, // Заголовок + управление + прогресс + TabControl
                Padding = new Padding(10)
            };

            // Настраиваем строки
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Заголовок
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Управление
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Прогресс
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // TabControl

            // 1. Заголовок устройства
            var titlePanel = new Panel 
            { 
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 5)
            };
            
            var titleLabel = new Label
            {
                Text = $"Устройство: {deviceInfo.DeviceType.Name}",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(0, 5),
                AutoSize = true
            };
            
            var addrLabel = new Label
            {
                Text = $"Адрес: {deviceInfo.Address}",
                Font = new Font("Arial", 10),
                Location = new Point(200, 5),
                AutoSize = true
            };
            
            var versionLabel = new Label
            {
                Text = $"Версия: {deviceInfo.Version:F2}",
                Font = new Font("Arial", 10),
                Location = new Point(350, 5),
                AutoSize = true
            };

            titlePanel.Controls.AddRange(new Control[] { titleLabel, addrLabel, versionLabel });
            mainTable.Controls.Add(titlePanel, 0, 0);

            // 2. Панель управления
            var controlPanel = new Panel 
            { 
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 2)
            };
            
            bool hasBranches = deviceInfo.DeviceType.MaxBranches > 0 || deviceInfo.DeviceType.DeviceCode == 9;

            if (hasBranches)
            {
                autoPollCheckBox = new CheckBox
                {
                    Text = "Автоопрос шлейфов",
                    Location = new Point(0, 8),
                    AutoSize = true
                };
                autoPollCheckBox.CheckedChanged += OnAutoPollToggle;
                controlPanel.Controls.Add(autoPollCheckBox);
            }

            if (deviceInfo.DeviceType.MaxRelays > 0)
            {
                var updateRelaysButton = new Button
                {
                    Text = "Обновить реле",
                    Location = new Point(200, 5),
                    Size = new Size(120, 25)
                };
                updateRelaysButton.Click += (s, e) => UpdateRelayStates();
                controlPanel.Controls.Add(updateRelaysButton);
            }

            // Добавляем кнопку звуковой сигнализации в панель управления
            if (hasBranches)
            {
                soundAlarmButton = new Button
                {
                    Text = "🔊 Звуковая сигнализация: ВКЛ",
                    Location = new Point(350, 5),
                    Size = new Size(200, 25)
                };
                soundAlarmButton.Click += ToggleSoundAlarm;
                controlPanel.Controls.Add(soundAlarmButton);
            }

            mainTable.Controls.Add(controlPanel, 0, 1);

            // 3. Прогресс-бар для опроса шлейфов
            var progressPanel = new Panel 
            { 
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 2, 0, 2)
            };
            
            if (hasBranches)
            {
                progressStatusLabel = new Label
                {
                    Text = "Готов к опросу шлейфов",
                    Location = new Point(0, 8),
                    AutoSize = true
                };
                
                progressBar = new ProgressBar
                {
                    Location = new Point(200, 8),
                    Size = new Size(300, 18),
                    Maximum = deviceInfo.DeviceType.MaxBranches
                };
                
                progressPanel.Controls.AddRange(new Control[] { progressStatusLabel, progressBar });
            }

            mainTable.Controls.Add(progressPanel, 0, 2);

            // 4. TabControl
            var tabControl = new TabControl 
            { 
                Dock = DockStyle.Fill
            };
            mainTable.Controls.Add(tabControl, 0, 3);

            // Вкладка шлейфов
            if (hasBranches)
            {
                CreateBranchesTab(tabControl);
            }

            // Вкладка реле
            if (deviceInfo.DeviceType.MaxRelays > 0)
            {
                CreateRelaysTab(tabControl);
            }
            else
            {
                var infoTab = new TabPage("Реле");
                var infoLabel = new Label
                {
                    Text = deviceInfo.DeviceType.DeviceCode == 9
                        ? "С2000-КДЛ не имеет встроенных реле\nРеле подключаются через адресные модули"
                        : "Данное устройство не поддерживает управление реле",
                    Font = new Font("Arial", 12),
                    ForeColor = deviceInfo.DeviceType.DeviceCode == 9 ? Color.Blue : Color.Gray,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                infoTab.Controls.Add(infoLabel);
                tabControl.TabPages.Add(infoTab);
            }

            Controls.Add(mainTable);
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            // Пересчитываем сетку шлейфов при изменении размера окна
            if (branchButtons != null && branchButtons.Count > 0)
            {
                // Находим TableLayoutPanel с шлейфами и пересчитываем колонки
                var branchesGrid = FindBranchesGrid();
                if (branchesGrid != null)
                {
                    UpdateBranchesGridLayout(branchesGrid);
                }
            }
            
            // Пересчитываем сетку реле при изменении размера окна
            if (relayButtons != null && relayButtons.Count > 0)
            {
                // Находим TableLayoutPanel с реле и пересчитываем колонки
                var relaysGrid = FindRelaysGrid();
                if (relaysGrid != null)
                {
                    UpdateRelaysGridLayout(relaysGrid);
                }
            }
        }

        private TableLayoutPanel FindBranchesGrid()
        {
            // Ищем TableLayoutPanel с шлейфами в дереве контролов
            foreach (Control control in this.Controls)
            {
                var tableLayout = FindTableLayoutPanel(control);
                if (tableLayout != null)
                    return tableLayout;
            }
            return null;
        }

        private TableLayoutPanel FindTableLayoutPanel(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TableLayoutPanel tableLayout && 
                    tableLayout.Controls.Count > 0 && 
                    tableLayout.Controls[0] is Button)
                {
                    return tableLayout;
                }
                
                var found = FindTableLayoutPanel(control);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void UpdateBranchesGridLayout(TableLayoutPanel branchesGrid)
        {
            // Рассчитываем новое количество колонок
            int padding = 40; // Отступы слева и справа
            int availableWidth = this.Width - padding;
            
            // Рассчитываем количество колонок более точно
            int buttonWidth = 90; // Реальная ширина кнопки шлейфа
            int buttonMargin = 8; // Отступы между кнопками
            int totalButtonWidth = buttonWidth + buttonMargin;
            
            int columnsCount = Math.Max(1, availableWidth / totalButtonWidth);
            columnsCount = Math.Max(1, Math.Min(20, columnsCount));

            if (branchesGrid.ColumnCount != columnsCount)
            {
                // Обновляем количество колонок
                branchesGrid.ColumnCount = columnsCount;
                branchesGrid.ColumnStyles.Clear();
                
                for (int i = 0; i < columnsCount; i++)
                {
                    branchesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / columnsCount));
                }

                // Пересчитываем позиции всех кнопок
                int buttonIndex = 0;
                foreach (Control control in branchesGrid.Controls)
                {
                    if (control is Button)
                    {
                        int row = buttonIndex / columnsCount;
                        int col = buttonIndex % columnsCount;
                        branchesGrid.SetRow(control, row);
                        branchesGrid.SetColumn(control, col);
                        buttonIndex++;
                    }
                }
            }
        }

        private TableLayoutPanel FindRelaysGrid()
        {
            // Ищем TableLayoutPanel с реле в дереве контролов
            foreach (Control control in this.Controls)
            {
                var tableLayout = FindRelaysTableLayoutPanel(control);
                if (tableLayout != null)
                    return tableLayout;
            }
            return null;
        }

        private TableLayoutPanel FindRelaysTableLayoutPanel(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TableLayoutPanel tableLayout && 
                    tableLayout.Controls.Count > 0 && 
                    tableLayout.Controls[0] is Button)
                {
                    // Проверяем, что это сетка реле (ищем кнопки с текстом "Реле")
                    bool isRelaysGrid = false;
                    foreach (Control btn in tableLayout.Controls)
                    {
                        if (btn is Button button && button.Text.Contains("Реле"))
                        {
                            isRelaysGrid = true;
                            break;
                        }
                    }
                    if (isRelaysGrid)
                        return tableLayout;
                }
                
                var found = FindRelaysTableLayoutPanel(control);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void UpdateRelaysGridLayout(TableLayoutPanel relaysGrid)
        {
            // Рассчитываем новое количество колонок
            int padding = 40; // Отступы слева и справа
            int availableWidth = this.Width - padding;
            
            // Рассчитываем количество колонок более точно
            int buttonWidth = 90; // Реальная ширина кнопки реле
            int buttonMargin = 8; // Отступы между кнопками
            int totalButtonWidth = buttonWidth + buttonMargin;
            
            int columnsCount = Math.Max(1, availableWidth / totalButtonWidth);
            columnsCount = Math.Max(1, Math.Min(20, columnsCount));

            if (relaysGrid.ColumnCount != columnsCount)
            {
                // Обновляем количество колонок
                relaysGrid.ColumnCount = columnsCount;
                relaysGrid.ColumnStyles.Clear();
                
                for (int i = 0; i < columnsCount; i++)
                {
                    relaysGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / columnsCount));
                }

                // Пересчитываем позиции всех кнопок
                int buttonIndex = 0;
                foreach (Control control in relaysGrid.Controls)
                {
                    if (control is Button)
                    {
                        int row = buttonIndex / columnsCount;
                        int col = buttonIndex % columnsCount;
                        relaysGrid.SetRow(control, row);
                        relaysGrid.SetColumn(control, col);
                        buttonIndex++;
                    }
                }
            }
        }

        private void CreateBranchesTab(TabControl tabControl)
        {
            var branchesTab = new TabPage("Шлейфы");
            
            // Основной контейнер для вкладки
            var branchesContainer = new Panel { Dock = DockStyle.Fill };

            // Создаем панель с прокруткой для кнопок шлейфов
            var scrollPanel = new Panel { Dock = DockStyle.Fill };
            
            // Создаем TableLayoutPanel для сетки кнопок с адаптивным количеством колонок
            var branchesGrid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(10, 10), // Добавляем отступы
                AutoScroll = false
            };

            // Рассчитываем оптимальное количество колонок на основе ширины окна
            // Учитываем реальные размеры: ширина окна минус отступы, деленная на ширину кнопки
            int padding = 40; // Отступы слева и справа
            int availableWidth = this.Width - padding;
            
            // Рассчитываем количество колонок более точно
            // Используем формулу: доступная_ширина / (ширина_кнопки + отступы)
            int buttonWidth = 90; // Реальная ширина кнопки шлейфа
            int buttonMargin = 8; // Отступы между кнопками
            int totalButtonWidth = buttonWidth + buttonMargin;
            
            int columnsCount = Math.Max(1, availableWidth / totalButtonWidth);
            
            // Отладочная информация
            SafeLogCallback($"Расчет колонок: ширина окна={this.Width}, доступная ширина={availableWidth}, ширина кнопки={totalButtonWidth}, колонок={columnsCount}");
            
            // Ограничиваем количество колонок (минимум 1, максимум 20)
            columnsCount = Math.Max(1, Math.Min(20, columnsCount));
            
            branchesGrid.ColumnCount = columnsCount;

            // Настраиваем колонки равномерно
            for (int i = 0; i < columnsCount; i++)
            {
                branchesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / columnsCount));
            }

            int maxBranches = deviceInfo.DeviceType.MaxBranches;
            if (deviceInfo.DeviceType.DeviceCode == 9) // С2000-КДЛ
            {
                maxBranches = 127;
            }

            // Создаем кнопки шлейфов
            for (int branchNum = 1; branchNum <= maxBranches; branchNum++)
            {
                int row = (branchNum - 1) / columnsCount;
                int col = (branchNum - 1) % columnsCount;

                var state = deviceInfo.Branches.ContainsKey(branchNum) ? deviceInfo.Branches[branchNum] : BranchState.UNKNOWN;
                var button = new BranchButton(branchesGrid, branchNum, state, OnBranchClick, deviceInfo);
                button.Button.Dock = DockStyle.Fill;
                button.Button.Margin = new Padding(2);
                branchesGrid.Controls.Add(button.Button, col, row);
                branchButtons[branchNum] = button;
            }

            // Создаем прокручиваемую панель
            var scrollablePanel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            scrollablePanel.Controls.Add(branchesGrid);
            scrollPanel.Controls.Add(scrollablePanel);

            // Добавляем элементы в правильном порядке
            branchesContainer.Controls.Add(scrollPanel);
            branchesTab.Controls.Add(branchesContainer);
            tabControl.TabPages.Add(branchesTab);
        }

        private void CreateRelaysTab(TabControl tabControl)
        {
            var relaysTab = new TabPage("Реле");
            
            // Основной контейнер для вкладки
            var relaysContainer = new Panel { Dock = DockStyle.Fill };

            // Создаем панель с прокруткой для кнопок реле
            var scrollPanel = new Panel { Dock = DockStyle.Fill };
            
            // Создаем TableLayoutPanel для сетки кнопок реле с адаптивным количеством колонок
            var relaysGrid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(10, 10), // Добавляем отступы
                AutoScroll = false
            };

            // Рассчитываем оптимальное количество колонок на основе ширины окна
            // Учитываем реальные размеры: ширина окна минус отступы, деленная на ширину кнопки
            int padding = 40; // Отступы слева и справа
            int availableWidth = this.Width - padding;
            
            // Рассчитываем количество колонок более точно
            // Используем формулу: доступная_ширина / (ширина_кнопки + отступы)
            int buttonWidth = 90; // Реальная ширина кнопки реле
            int buttonMargin = 8; // Отступы между кнопками
            int totalButtonWidth = buttonWidth + buttonMargin;
            
            int columnsCount = Math.Max(1, availableWidth / totalButtonWidth);
            
            // Ограничиваем количество колонок (минимум 1, максимум 20)
            columnsCount = Math.Max(1, Math.Min(20, columnsCount));
            
            relaysGrid.ColumnCount = columnsCount;

            // Настраиваем колонки равномерно
            for (int i = 0; i < columnsCount; i++)
            {
                relaysGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / columnsCount));
            }

            // Создаем кнопки реле
            for (int relayNum = 1; relayNum <= deviceInfo.DeviceType.MaxRelays; relayNum++)
            {
                int row = (relayNum - 1) / columnsCount;
                int col = (relayNum - 1) % columnsCount;

                bool state = deviceInfo.Relays.ContainsKey(relayNum) ? deviceInfo.Relays[relayNum] : false;

                object relayButton = null;

                if (deviceInfo.DeviceType.DeviceCode == 2) // Сигнал-20П
                {
                    string relayType = relayNum <= 3 ? "Сухой контакт" : "С контролем";
                    var buttonWithType = new RelayButtonWithType(relaysGrid, relayNum, state, relayType, OnRelayClick);
                    buttonWithType.Button.Dock = DockStyle.Fill;
                    buttonWithType.Button.Margin = new Padding(2);
                    relaysGrid.Controls.Add(buttonWithType.Button, col, row);
                    relayButton = buttonWithType; // Используем RelayButtonWithType как RelayButton
                }
                else if (deviceInfo.DeviceType.DeviceCode == 32) // Сигнал-10
                {
                    string relayType = relayNum <= 2 ? "Оптореле" : "С контролем";
                    var buttonWithType = new RelayButtonWithType(relaysGrid, relayNum, state, relayType, OnRelayClick);
                    buttonWithType.Button.Dock = DockStyle.Fill;
                    buttonWithType.Button.Margin = new Padding(2);
                    relaysGrid.Controls.Add(buttonWithType.Button, col, row);
                    relayButton = buttonWithType; // Используем RelayButtonWithType как RelayButton
                }
                else if (deviceInfo.DeviceType.DeviceCode == 15) // С2000-КПБ
                {
                    string relayType = relayNum <= 3 ? "Исполнительное" : "С контролем";
                    var buttonWithType = new RelayButtonWithType(relaysGrid, relayNum, state, relayType, OnRelayClick);
                    buttonWithType.Button.Dock = DockStyle.Fill;
                    buttonWithType.Button.Margin = new Padding(2);
                    relaysGrid.Controls.Add(buttonWithType.Button, col, row);
                    relayButton = buttonWithType; // Используем RelayButtonWithType как RelayButton
                }
                else
                {
                    var regularButton = new RelayButton(relaysGrid, relayNum, state, OnRelayClick);
                    regularButton.Button.Dock = DockStyle.Fill;
                    regularButton.Button.Margin = new Padding(2);
                    relaysGrid.Controls.Add(regularButton.Button, col, row);
                    relayButton = regularButton;
                }

                relayButtons[relayNum] = relayButton;
            }

            // Создаем прокручиваемую панель
            var scrollablePanel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            scrollablePanel.Controls.Add(relaysGrid);
            scrollPanel.Controls.Add(scrollablePanel);

            // Создаем фрейм для расширенного управления реле
            var advancedFrame = new GroupBox
            {
                Text = "Расширенное управление реле",
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(10)
            };
            relaysContainer.Controls.Add(advancedFrame);

            // Выбор реле
            var relaySelectFrame = new Panel { Dock = DockStyle.Top, Height = 30 };
            
            var relayLabel = new Label
            {
                Text = "Реле:",
                Location = new Point(0, 5),
                AutoSize = true
            };
            relaySelectFrame.Controls.Add(relayLabel);

            var selectedRelayVar = new NumericUpDown
            {
                Minimum = 1,
                Maximum = deviceInfo.DeviceType.MaxRelays,
                Value = 1,
                Location = new Point(50, 3),
                Width = 50
            };
            relaySelectFrame.Controls.Add(selectedRelayVar);

            // Выбор программы
            var programLabel = new Label
            {
                Text = "Программа:",
                Location = new Point(120, 5),
                AutoSize = true
            };
            relaySelectFrame.Controls.Add(programLabel);

            var selectedProgramVar = new ComboBox
            {
                Location = new Point(200, 3),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            relaySelectFrame.Controls.Add(selectedProgramVar);

            // Заполняем список программ
            foreach (var kvp in BolidConstants.RELAY_PROGRAMS)
            {
                selectedProgramVar.Items.Add($"{kvp.Key}: {kvp.Value}");
            }
            if (selectedProgramVar.Items.Count > 0)
            {
                selectedProgramVar.SelectedIndex = 0;
            }

            // Кнопка выполнения
            var executeButton = new Button
            {
                Text = "Выполнить",
                Location = new Point(520, 2),
                Size = new Size(80, 25)
            };
            relaySelectFrame.Controls.Add(executeButton);

            advancedFrame.Controls.Add(relaySelectFrame);

            // Информационная панель
            var infoFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) };
            
            var relayInfoVar = new Label
            {
                Text = "Выберите реле и программу для управления",
                Location = new Point(0, 5),
                AutoSize = true,
                ForeColor = Color.Blue
            };
            infoFrame.Controls.Add(relayInfoVar);

            advancedFrame.Controls.Add(infoFrame);

            // Обработчики событий
            executeButton.Click += (s, e) => ExecuteRelayProgram(selectedRelayVar, selectedProgramVar, relayInfoVar);
            selectedRelayVar.ValueChanged += (s, e) => UpdateRelayInfo(selectedRelayVar, selectedProgramVar, relayInfoVar);
            selectedProgramVar.SelectedIndexChanged += (s, e) => UpdateRelayInfo(selectedRelayVar, selectedProgramVar, relayInfoVar);

            // Добавляем информационную панель о реле для разных устройств ПЕРЕД кнопками
            CreateRelayInfoPanel(relaysContainer);

            // Добавляем элементы в правильном порядке
            relaysContainer.Controls.Add(scrollPanel);
            relaysTab.Controls.Add(relaysContainer);
            tabControl.TabPages.Add(relaysTab);
        }

        private void OnBranchClick(int branchNum)
        {
            if (isClosing) return;

            if (pollingInProgress)
            {
                SafeLogCallback($"Остановка опроса шлейфов для выполнения команды на шлейфе {branchNum}");
                pollingStopped = true;
                SafeUpdateUI(() => progressStatusLabel.Text = "Остановка опроса...");
                Thread.Sleep(100);
            }

            deviceManager.ToggleBranch(deviceInfo.Address, branchNum, (success, message) =>
            {
                if (isClosing) return;

                if (success)
                {
                    SafeLogCallback($"Шлейф {branchNum}: {message}");

                    if (!autoPollingActive)
                    {
                        if (pollingStopped)
                        {
                            SafeLogCallback("Перезапуск опроса шлейфов...");
                            Task.Delay(1000).ContinueWith(_ => RestartPolling());
                        }
                        else
                        {
                            SafeLogCallback("Запуск нового опроса шлейфов...");
                            Task.Delay(1000).ContinueWith(_ => UpdateBranchStates());
                        }
                    }
                    else
                    {
                        SafeLogCallback("Автоматический опрос продолжит работу");
                    }
                }
                else
                {
                    SafeLogCallback($"Ошибка шлейфа {branchNum}: {message}");
                    MessageBox.Show($"Ошибка", $"Шлейф {branchNum}: {message}", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    if (!autoPollingActive)
                    {
                        if (pollingStopped)
                        {
                            SafeLogCallback("Перезапуск опроса шлейфов после ошибки...");
                            Task.Delay(1000).ContinueWith(_ => RestartPolling());
                        }
                        else
                        {
                            SafeLogCallback("Запуск нового опроса шлейфов после ошибки...");
                            Task.Delay(1000).ContinueWith(_ => UpdateBranchStates());
                        }
                    }
                    else
                    {
                        SafeLogCallback("Автоматический опрос продолжит работу после ошибки");
                    }
                }
            });
        }

        private void OnRelayClick(int relayNum)
        {
            if (isClosing) return;

            SafeLogCallback($"Попытка управления реле {relayNum} на устройстве {deviceInfo.DeviceType.Name} (код {deviceInfo.DeviceType.DeviceCode})");
            
            deviceManager.ToggleRelay(deviceInfo.Address, relayNum, (success, message) =>
            {
                if (isClosing) return;

                if (success)
                {
                    SafeLogCallback($"Реле {relayNum}: {message}");
                }
                else
                {
                    SafeLogCallback($"Ошибка реле {relayNum}: {message}");
                    MessageBox.Show($"Ошибка", $"Реле {relayNum}: {message}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void OnAutoPollToggle(object sender, EventArgs e)
        {
            if (isClosing) return;

            if (autoPollCheckBox.Checked)
            {
                autoPollingActive = true;
                SafeLogCallback("Автоматический опрос шлейфов включен");
                StartAutoPolling();
            }
            else
            {
                autoPollingActive = false;
                SafeLogCallback("Автоматический опрос шлейфов выключен");
                StopAutoPolling();
            }
        }

        private void StartAutoPolling()
        {
            SafeLogCallback($"StartAutoPolling: autoPollingActive={autoPollingActive}, isClosing={isClosing}");
            
            if (isClosing || !autoPollingActive) 
            {
                SafeLogCallback("Автоматический опрос шлейфов отменен: autoPollingActive=false или isClosing=true");
                return;
            }

            if (pollingInProgress)
            {
                SafeLogCallback("Опрос шлейфов уже выполняется, планируем следующий через 3 секунды...");
                SafeUpdateUI(() =>
                {
                    Task.Delay(3000).ContinueWith(t =>
                    {
                        if (autoPollingActive && !isClosing)
                        {
                            SafeLogCallback("Task.Delay сработал - повторный запуск опроса шлейфов...");
                            StartAutoPolling();
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                });
                return;
            }

            SafeLogCallback("Запуск автоматического опроса шлейфов...");
            UpdateBranchStatesWithAutoRestart();
        }

        private void UpdateBranchStatesWithAutoRestart()
        {
            if (!autoPollingActive || isClosing) return;

            // updatedBranchesCount = 0; // Удалено как неиспользуемое
            currentPolledCount = 0;

            int totalBranches = deviceInfo.DeviceType.MaxBranches;
            if (deviceInfo.DeviceType.DeviceCode == 9) // С2000-КДЛ
            {
                totalBranches = 127;
            }

            pollingInProgress = true;
            pollingStopped = false;

            SafeUpdateUI(() =>
            {
                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }
                if (progressStatusLabel != null)
                {
                    progressStatusLabel.Text = "Начинается опрос...";
                }
            });

            deviceManager.UpdateBranchStates(deviceInfo.Address, (success, message) =>
            {
                if (isClosing) return;

                pollingInProgress = false;

                if (success)
                {
                    SafeLogCallback($"Автоматическое обновление шлейфов завершено: {message}");
                    SafeUpdateUI(() =>
                    {
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = "Опрос завершен";
                        }
                    });
                }
                else
                {
                    SafeLogCallback($"Ошибка автоматического обновления шлейфов: {message}");
                    SafeUpdateUI(() =>
                    {
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = "Ошибка опроса";
                        }
                    });
                }

                SafeUpdateUI(() =>
                {
                    if (autoPollingActive && !isClosing)
                    {
                        SafeLogCallback("Планирование следующего опроса шлейфов через 5 секунд...");
                        Task.Delay(5000).ContinueWith(t =>
                        {
                            if (autoPollingActive && !isClosing)
                            {
                                SafeLogCallback("Task.Delay сработал - запуск следующего опроса шлейфов...");
                                StartAutoPolling();
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        SafeLogCallback("Task.Delay для шлейфов запущен");
                    }
                    else
                    {
                        SafeLogCallback($"Опрос шлейфов остановлен: autoPollingActive={autoPollingActive}, isClosing={isClosing}");
                    }
                });
            }, (polledCount, totalCount) =>
            {
                if (isClosing || pollingStopped || !autoPollingActive)
                    return false;

                currentPolledCount = polledCount;
                SafeUpdateUI(() =>
                {
                    if (progressBar != null)
                    {
                        progressBar.Value = polledCount;
                    }

                    if (totalCount > 0)
                    {
                        double percentage = (polledCount / (double)totalCount) * 100;
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = $"Автоопрос: {polledCount}/{totalCount} ({percentage:F1}%)";
                        }
                    }
                    else
                    {
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = $"Автоопрос: {polledCount}/{totalCount}";
                        }
                    }
                });

                return true;
            });

            SafeLogCallback($"Начинается автоматический опрос {totalBranches} шлейфов...");
        }

        private void StopAutoPolling()
        {
        }

        private void UpdateBranchStates()
        {
            if (isClosing) return;

            if (pollingInProgress)
            {
                SafeLogCallback("Опрос шлейфов уже выполняется");
                return;
            }

            // updatedBranchesCount = 0; // Удалено как неиспользуемое
            currentPolledCount = 0;

            int totalBranches = deviceInfo.DeviceType.MaxBranches;
            if (deviceInfo.DeviceType.DeviceCode == 9) // С2000-КДЛ
            {
                totalBranches = 127;
            }

            pollingInProgress = true;
            pollingStopped = false;

            SafeUpdateUI(() =>
            {
                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }
                if (progressStatusLabel != null)
                {
                    progressStatusLabel.Text = $"Опрос шлейфов: 0/{totalBranches}";
                }
            });

            deviceManager.UpdateBranchStates(deviceInfo.Address, (success, message) =>
            {
                if (isClosing) return;

                pollingInProgress = false;

                if (success)
                {
                    SafeLogCallback($"Обновление шлейфов завершено: {message}");
                    SafeUpdateUI(() =>
                    {
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = "Опрос завершен";
                        }
                    });
                }
                else
                {
                    SafeLogCallback($"Ошибка обновления шлейфов: {message}");
                    SafeUpdateUI(() =>
                    {
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = "Ошибка опроса";
                        }
                    });
                    MessageBox.Show($"Ошибка", $"Ошибка обновления шлейфов: {message}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }, (polledCount, totalCount) =>
            {
                if (isClosing || pollingStopped)
                    return false;

                currentPolledCount = polledCount;
                SafeUpdateUI(() =>
                {
                    if (progressBar != null)
                    {
                        progressBar.Value = polledCount;
                    }

                    if (totalCount > 0)
                    {
                        double percentage = (polledCount / (double)totalCount) * 100;
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = $"Опрос шлейфов: {polledCount}/{totalCount} ({percentage:F1}%)";
                        }
                    }
                    else
                    {
                        if (progressStatusLabel != null)
                        {
                            progressStatusLabel.Text = $"Опрос шлейфов: {polledCount}/{totalCount}";
                        }
                    }
                });

                return true;
            });

            SafeLogCallback($"Начинается опрос {totalBranches} шлейфов...");
        }

        private void RestartPolling()
        {
            if (isClosing) return;

            pollingStopped = false;
            SafeLogCallback("Перезапуск опроса шлейфов...");
            UpdateBranchStates();
        }

        private void UpdateRelayStates()
        {
            if (isClosing) return;

            deviceManager.UpdateRelayStates(deviceInfo.Address, (success, message) =>
            {
                if (isClosing) return;

                if (success)
                {
                    SafeLogCallback($"Обновление реле: {message}");
                }
                else
                {
                    SafeLogCallback($"Ошибка обновления реле: {message}");
                    MessageBox.Show($"Ошибка", $"Ошибка обновления реле: {message}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void OnDeviceUpdate(int address, DeviceInfo updatedDeviceInfo)
        {
            if (address != deviceInfo.Address || isClosing) return;

            int updatedCount = 0;

            foreach (var kvp in branchButtons)
            {
                int branchNum = kvp.Key;
                var button = kvp.Value;
                var newState = updatedDeviceInfo.Branches.ContainsKey(branchNum) ? updatedDeviceInfo.Branches[branchNum] : BranchState.UNKNOWN;
                var oldState = button.State;
                button.UpdateState(newState);

                if (oldState != newState)
                {
                    updatedCount++;

                    if (newState == BranchState.ALARM)
                    {
                        // alarmDetected = true; // Удалено как неиспользуемое
                        SafeLogCallback($"🚨 ТРЕВОГА на шлейфе {branchNum}!");

                        if (alarmSoundEnabled)
                        {
                            // Воспроизводим звук тревоги
                            soundAlarm.PlayAlarm();
                        }
                    }
                    else if (oldState == BranchState.ALARM && newState != BranchState.ALARM)
                    {
                        SafeLogCallback($"✅ Тревога на шлейфе {branchNum} снята");
                    }
                }

                previousBranchStates[branchNum] = newState;
            }

            bool hasAnyAlarm = branchButtons.Values.Any(b => b.State == BranchState.ALARM);

            // Если нет активных тревог, останавливаем звук
            if (!hasAnyAlarm && soundAlarm != null)
            {
                soundAlarm.StopAlarm();
                SafeLogCallback("🔇 Все тревоги сняты, звук остановлен");
            }

            if (updatedCount > 0)
            {
                int totalBranches = deviceInfo.DeviceType.MaxBranches;
                SafeLogCallback($"Обновлено шлейфов: {updatedCount}/{totalBranches}");
            }

            foreach (var kvp in relayButtons)
            {
                int relayNum = kvp.Key;
                var button = kvp.Value;
                if (button != null)
                {
                    bool newState = updatedDeviceInfo.Relays.ContainsKey(relayNum) ? updatedDeviceInfo.Relays[relayNum] : false;
                    
                    // Обновляем состояние в зависимости от типа кнопки
                    if (button is RelayButton relayButton)
                    {
                        relayButton.UpdateState(newState);
                    }
                    else if (button is RelayButtonWithType relayButtonWithType)
                    {
                        relayButtonWithType.UpdateState(newState);
                    }
                }
            }
        }

        private void ToggleSoundAlarm(object sender, EventArgs e)
        {
            alarmSoundEnabled = !alarmSoundEnabled;

            if (alarmSoundEnabled)
            {
                SafeUpdateUI(() => soundAlarmButton.Text = "🔊 Звуковая сигнализация: ВКЛ");
                SafeLogCallback("Звуковая сигнализация включена");
                if (HasActiveAlarm())
                {
                    soundAlarm.PlayAlarm();
                }
            }
            else
            {
                SafeUpdateUI(() => soundAlarmButton.Text = "🔇 Звуковая сигнализация: ВЫКЛ");
                SafeLogCallback("Звуковая сигнализация выключена");
                // Останавливаем звук при выключении
                soundAlarm.StopAlarm();
            }
        }

        private bool HasActiveAlarm()
        {
            return branchButtons.Values.Any(b => b.State == BranchState.ALARM);
        }

        private void CheckActiveAlarms()
        {
            if (!alarmSoundEnabled) return;

            if (HasActiveAlarm())
            {
                soundAlarm.PlayAlarm();
                SafeLogCallback("🔊 Запущена звуковая сигнализация для активной тревоги");
            }
        }

        private void CreateS2000ppInfoInterface(Panel mainContainer)
        {
            var infoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            // Основное сообщение
            var infoLabel = new Label
            {
                Text = @"🔧 С2000-ПП - это специализированное устройство управления

Данное устройство не поддерживает стандартное управление через
панель управления устройствами.

Для управления С2000-ПП используйте специальный инструмент
""Управление С2000-ПП"" в разделе ""Устройства"" главного окна.",
                Font = new Font("Arial", 12),
                ForeColor = Color.FromArgb(46, 139, 87),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Панель кнопок
            var buttonPanel = new Panel { Height = 60, Dock = DockStyle.Bottom, Padding = new Padding(0, 10, 0, 10) };
            
            var openS2000ppButton = new Button
            {
                Text = "Открыть управление С2000-ПП",
                Location = new Point(0, 15),
                Size = new Size(200, 30)
            };
            openS2000ppButton.Click += (s, e) => OpenS2000ppTool();

            var closeButton = new Button
            {
                Text = "Закрыть",
                Location = new Point(220, 15),
                Size = new Size(100, 30)
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Controls.AddRange(new Control[] { openS2000ppButton, closeButton });
            infoPanel.Controls.Add(infoLabel);
            infoPanel.Controls.Add(buttonPanel);
            mainContainer.Controls.Add(infoPanel);
        }

        private void OpenS2000ppTool()
        {
            try
            {
                Close();
                MessageBox.Show("Управление С2000-ПП будет реализовано в отдельном файле.", "С2000-ПП", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна С2000-ПП: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreatePowerDeviceInterface(Panel mainContainer)
        {
            // Создаем интерфейс для МИП/РИП устройств
            var powerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            
            // Заголовок
            var titleLabel = new Label
            {
                Text = "Параметры питания",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true
            };
            powerPanel.Controls.Add(titleLabel);
            
            // Прогресс-бар для опроса параметров питания
            var progressFrame = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 10, 0, 5) };
            
            progressStatusLabel = new Label
            {
                Text = "Запуск автоматического опроса...",
                Location = new Point(0, 8),
                AutoSize = true
            };
            progressFrame.Controls.Add(progressStatusLabel);
            
            progressBar = new ProgressBar
            {
                Location = new Point(200, 8),
                Size = new Size(300, 18),
                Maximum = 100
            };
            progressFrame.Controls.Add(progressBar);
            
            powerPanel.Controls.Add(progressFrame);
            
            // Создаем TabControl для вкладок с отступом
            var tabControl = new TabControl { 
                Location = new Point(0, 80), 
                Size = new Size(powerPanel.Width, powerPanel.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(200, 40)
            };
            powerPanel.Controls.Add(tabControl);
            
            // Вкладка параметров питания
            CreatePowerParametersTab(tabControl);
            
            // Вкладка реле (если есть)
            if (deviceInfo.DeviceType.MaxRelays > 0)
            {
                CreateRelaysTab(tabControl);
            }
            else
            {
                // Для МИП устройств показываем информационное сообщение
                var infoTab = new TabPage("Реле");
                var infoLabel = new Label
                {
                    Text = "МИП устройства не имеют реле\nУправление осуществляется через параметры питания",
                    Font = new Font("Arial", 12),
                    ForeColor = Color.Blue,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                infoTab.Controls.Add(infoLabel);
                tabControl.TabPages.Add(infoTab);
            }
            
            // Переменные для отслеживания прогресса
            pollingInProgress = false;
            pollingStopped = false;
            currentPolledCount = 0;
            
            // Переменные для автоматического опроса
            autoPollingActive = true;
            
            // Принудительный запуск через небольшую задержку
            var startupTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            startupTimer.Tick += (s, e) => {
                startupTimer.Stop();
                startupTimer.Dispose();
                SafeLogCallback("Принудительный запуск опроса параметров питания...");
                StartAutoPowerPolling();
            };
            startupTimer.Start();
            
            mainContainer.Controls.Add(powerPanel);
        }

        private void CreatePowerParametersTab(TabControl tabControl)
        {
            var powerTab = new TabPage("Параметры питания");
            tabControl.TabPages.Add(powerTab);
            
            // Создаем прокручиваемую панель
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            powerTab.Controls.Add(scrollPanel);
            
            // Заголовок
            var titleLabel = new Label
            {
                Text = "Параметры питания",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            scrollPanel.Controls.Add(titleLabel);
            
            // Создаем фреймы для параметров
            var paramsFrame = new Panel { Location = new Point(20, 50), Size = new Size(520, 500) };
            scrollPanel.Controls.Add(paramsFrame);
            
            // Выходное напряжение
            var outputFrame = new GroupBox
            {
                Text = "Выходное напряжение",
                Location = new Point(0, 0),
                Size = new Size(500, 80)
            };
            
            var outputVoltageLabel = new Label
            {
                Text = "Напряжение:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            outputFrame.Controls.Add(outputVoltageLabel);
            
            outputVoltageValueLabel = new Label
            {
                Text = "Неизвестно",
                Location = new Point(150, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true
            };
            outputFrame.Controls.Add(outputVoltageValueLabel);
            paramsFrame.Controls.Add(outputFrame);
            
            // Ток нагрузки
            var currentFrame = new GroupBox
            {
                Text = "Ток нагрузки",
                Location = new Point(0, 90),
                Size = new Size(500, 80)
            };
            
            var loadCurrentLabel = new Label
            {
                Text = "Ток:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            currentFrame.Controls.Add(loadCurrentLabel);
            
            loadCurrentValueLabel = new Label
            {
                Text = "Неизвестно",
                Location = new Point(150, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true
            };
            currentFrame.Controls.Add(loadCurrentValueLabel);
            paramsFrame.Controls.Add(currentFrame);
            
            // Напряжение АКБ
            var batteryFrame = new GroupBox
            {
                Text = "Аккумуляторная батарея",
                Location = new Point(0, 180),
                Size = new Size(500, 80)
            };
            
            var batteryVoltageLabel = new Label
            {
                Text = "Напряжение АКБ:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            batteryFrame.Controls.Add(batteryVoltageLabel);
            
            batteryVoltageValueLabel = new Label
            {
                Text = "Неизвестно",
                Location = new Point(150, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true
            };
            batteryFrame.Controls.Add(batteryVoltageValueLabel);
            paramsFrame.Controls.Add(batteryFrame);
            
            // Состояние ЗУ
            var chargerFrame = new GroupBox
            {
                Text = "Зарядное устройство",
                Location = new Point(0, 270),
                Size = new Size(500, 80)
            };
            
            var chargerStatusLabel = new Label
            {
                Text = "Состояние ЗУ:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            chargerFrame.Controls.Add(chargerStatusLabel);
            
            chargerStatusValueLabel = new Label
            {
                Text = "Неизвестно",
                Location = new Point(150, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true
            };
            chargerFrame.Controls.Add(chargerStatusValueLabel);
            paramsFrame.Controls.Add(chargerFrame);
            
            // Сетевое напряжение
            var networkFrame = new GroupBox
            {
                Text = "Сетевое питание",
                Location = new Point(0, 360),
                Size = new Size(500, 80)
            };
            
            var networkVoltageLabel = new Label
            {
                Text = "Напряжение сети:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            networkFrame.Controls.Add(networkVoltageLabel);
            
            networkVoltageValueLabel = new Label
            {
                Text = "Неизвестно",
                Location = new Point(150, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true
            };
            networkFrame.Controls.Add(networkVoltageValueLabel);
            paramsFrame.Controls.Add(networkFrame);
            
            // Время последнего обновления
            var updateFrame = new GroupBox
            {
                Text = "Информация",
                Location = new Point(0, 450),
                Size = new Size(500, 80)
            };
            
            var lastUpdateLabel = new Label
            {
                Text = "Последнее обновление:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            updateFrame.Controls.Add(lastUpdateLabel);
            
            lastUpdateValueLabel = new Label
            {
                Text = "Данные не обновлялись",
                Location = new Point(150, 30),
                Font = new Font("Arial", 9),
                AutoSize = true
            };
            updateFrame.Controls.Add(lastUpdateValueLabel);
            paramsFrame.Controls.Add(updateFrame);
            
            // Обновляем отображение параметров
            UpdatePowerDisplay();
        }

        private void StartAutoPowerPolling()
        {
            SafeLogCallback($"StartAutoPowerPolling: autoPollingActive={autoPollingActive}, isClosing={isClosing}");
            
            if (!autoPollingActive || isClosing)
            {
                SafeLogCallback("Автоматический опрос отменен: autoPollingActive=false или isClosing=true");
                return;
            }
            
            if (pollingInProgress)
            {
                // Если опрос уже идет, планируем следующий через 2 секунды
                SafeLogCallback("Опрос уже выполняется, планируем следующий через 2 секунды...");
                Task.Delay(2000).ContinueWith(t =>
                {
                    if (autoPollingActive && !isClosing)
                    {
                        SafeLogCallback("Task.Delay сработал - повторный запуск опроса...");
                        StartAutoPowerPolling();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
                return;
            }
            
            SafeLogCallback("Запуск автоматического опроса параметров питания...");
            
            // Запускаем опрос
            UpdatePowerParametersWithAutoRestart();
        }

        private void UpdatePowerParametersWithAutoRestart()
        {
            if (!autoPollingActive || isClosing)
                return;
            
            pollingInProgress = true;
            pollingStopped = false;
            progressBar.Value = 0;
            progressStatusLabel.Text = "Опрос параметров питания...";
            
            deviceManager.UpdateBranchStates(deviceInfo.Address, (success, message) =>
            {
                if (isClosing)
                    return;
                
                pollingInProgress = false;
                
                if (success)
                {
                    SafeLogCallback("Параметры питания обновлены");
                    UpdatePowerDisplay();
                    progressStatusLabel.Text = "Параметры обновлены";
                }
                else
                {
                    SafeLogCallback($"Ошибка обновления параметров питания: {message}");
                    progressStatusLabel.Text = "Ошибка обновления";
                }
                
                // Планируем следующий опрос через 3 секунды
                SafeUpdateUI(() =>
                {
                    if (autoPollingActive && !isClosing)
                    {
                        SafeLogCallback("Планирование следующего опроса через 3 секунды...");
                        
                        // Используем Task.Delay как альтернативу таймеру
                        Task.Delay(3000).ContinueWith(t =>
                        {
                            if (autoPollingActive && !isClosing)
                            {
                                SafeLogCallback("Task.Delay сработал - запуск следующего опроса...");
                                StartAutoPowerPolling();
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        
                        SafeLogCallback("Task.Delay запущен");
                    }
                    else
                    {
                        SafeLogCallback($"Опрос остановлен: autoPollingActive={autoPollingActive}, isClosing={isClosing}");
                    }
                });
            }, (polledCount, totalCount) =>
            {
                if (isClosing || !autoPollingActive)
                    return false;
                
                SafeUpdateUI(() =>
                {
                    if (!isClosing && autoPollingActive)
                    {
                        double percentage = (polledCount / (double)totalCount) * 100;
                        progressBar.Value = (int)percentage;
                        progressStatusLabel.Text = $"Опрос параметров: {polledCount}/{totalCount} ({percentage:F1}%)";
                    }
                });
                
                return true;
            });
        }

        private void UpdatePowerDisplay()
        {
            // Получаем ADC значения из deviceInfo
            if (deviceInfo.AdcValues == null || deviceInfo.AdcValues.Count == 0)
                return;
            
            // Обновляем значения на основе ADC
            if (deviceInfo.AdcValues.ContainsKey(1))
            {
                int adc = deviceInfo.AdcValues[1];
                double voltage = adc * 0.125;
                outputVoltageValueLabel.Text = $"{voltage:F2} В";
            }
            
            if (deviceInfo.AdcValues.ContainsKey(2))
            {
                int adc = deviceInfo.AdcValues[2];
                double current = adc * 0.035;
                loadCurrentValueLabel.Text = $"{current:F2} А";
            }
            
            if (deviceInfo.AdcValues.ContainsKey(3))
            {
                int adc = deviceInfo.AdcValues[3];
                if (adc == 0)
                {
                    batteryVoltageValueLabel.Text = "АКБ не подключена";
                }
                else
                {
                    double voltage = adc * 0.125;
                    batteryVoltageValueLabel.Text = $"{voltage:F2} В";
                }
            }
            
            if (deviceInfo.AdcValues.ContainsKey(4))
            {
                int adc = deviceInfo.AdcValues[4];
                if (adc >= 200)
                {
                    chargerStatusValueLabel.Text = "Норма";
                }
                else if (adc >= 100)
                {
                    chargerStatusValueLabel.Text = "Предупреждение";
                }
                else
                {
                    chargerStatusValueLabel.Text = "Неисправность";
                }
            }
            
            if (deviceInfo.AdcValues.ContainsKey(5))
            {
                int adc = deviceInfo.AdcValues[5];
                double voltage = adc * 2.0;
                networkVoltageValueLabel.Text = $"{voltage:F1} В";
            }
            
            // Время последнего обновления
            lastUpdateValueLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void ExecuteRelayProgram(NumericUpDown relaySelector, ComboBox programSelector, Label infoLabel)
        {
            if (isClosing) return;

            int relayNum = (int)relaySelector.Value;
            string programText = programSelector.SelectedItem?.ToString();
            
            if (string.IsNullOrEmpty(programText))
            {
                MessageBox.Show("Предупреждение", "Выберите программу управления", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Извлекаем код программы
            try
            {
                int programCode = int.Parse(programText.Split(':')[0]);
                
                SafeLogCallback($"Выполнение программы реле {programCode} на реле {relayNum}");
                
                deviceManager.ToggleRelay(deviceInfo.Address, relayNum, (success, message) =>
                {
                    if (isClosing) return;

                    if (success)
                    {
                        SafeLogCallback($"Реле {relayNum}: {message}");
                    }
                    else
                    {
                        SafeLogCallback($"Ошибка реле {relayNum}: {message}");
                        MessageBox.Show("Ошибка", $"Реле {relayNum}: {message}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }, programCode);
            }
            catch (Exception ex)
            {
                SafeLogCallback($"Ошибка выполнения программы реле: {ex.Message}");
                MessageBox.Show("Ошибка", "Неверный формат программы", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateRelayInfo(NumericUpDown relaySelector, ComboBox programSelector, Label infoLabel)
        {
            if (isClosing) return;

            int relayNum = (int)relaySelector.Value;
            string programText = programSelector.SelectedItem?.ToString();
            
            if (!string.IsNullOrEmpty(programText))
            {
                try
                {
                    int programCode = int.Parse(programText.Split(':')[0]);
                    string programDescription = programText.Split(new char[] { ':' }, 2)[1].Trim();
                    
                    // Получаем текущее состояние реле
                    bool currentState = deviceInfo.Relays.ContainsKey(relayNum) ? deviceInfo.Relays[relayNum] : false;
                    string stateText = currentState ? "включено" : "выключено";
                    
                    string infoText = $"Реле {relayNum} (текущее состояние: {stateText}) → Программа {programCode}: {programDescription}";
                    infoLabel.Text = infoText;
                }
                catch (Exception)
                {
                    infoLabel.Text = "Неверный формат программы";
                }
            }
            else
            {
                infoLabel.Text = "Выберите реле и программу для управления";
            }
        }

        private void CreateRelayInfoPanel(Panel container)
        {
            // Создаем информационную панель о реле
            var infoFrame = new GroupBox
            {
                Text = "Информация о реле",
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(10)
            };
            container.Controls.Add(infoFrame);

            var infoLabel = new Label
            {
                Text = GetRelayInfoText(),
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.Blue,
                Font = new Font("Arial", 9)
            };
            infoFrame.Controls.Add(infoLabel);
        }

        private string GetRelayInfoText()
        {
            switch (deviceInfo.DeviceType.DeviceCode)
            {
                case 2: // Сигнал-20П
                    return "Реле 1-3: Сухие контакты (до 28В/2А или до 80В/50мА)\nРеле 4-5: Выходы с контролем исправности (до 28В/0.8А)";
                
                case 32: // Сигнал-10
                    return "Реле 1-2: Оптореле (до 350В/0.1А)\nРеле 3-4: Выходы с контролем исправности (до 28В/1А)";
                
                case 15: // С2000-КПБ
                    return "Реле 1-3: Исполнительные реле (управление устройствами)\nРеле 4-6: Выходы с контролем исправности (обрыв/КЗ)";
                
                default:
                    return "Информация о типах реле для данного устройства недоступна";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isClosing = true;

            if (autoPollingActive)
            {
                autoPollingActive = false;
                StopAutoPolling();
                SafeLogCallback("Остановка автоматического опроса при закрытии окна");
            }

            if (pollingInProgress)
            {
                pollingStopped = true;
                SafeLogCallback("Остановка опроса шлейфов при закрытии окна");
            }

            if (deviceManager != null)
            {
                deviceManager.RemoveUpdateCallback(updateCallbackId);
            }

            // Останавливаем звуковую сигнализацию при закрытии окна
            if (soundAlarm != null)
            {
                SafeLogCallback("Остановка звуковой сигнализации при закрытии окна");
                soundAlarm.StopAlarm();
                
                // Принудительная остановка всех звуков
                soundAlarm.ForceStopAllSounds();
                
                // Принудительно ждем завершения остановки
                Thread.Sleep(200);
                
                SafeLogCallback("Звуковая сигнализация остановлена");
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Принудительно останавливаем звук при освобождении ресурсов
                if (soundAlarm != null)
                {
                    Console.WriteLine("Принудительная остановка звука в Dispose");
                    soundAlarm.StopAlarm();
                    soundAlarm.ForceStopAllSounds();
                    
                    // Принудительно ждем завершения остановки
                    Thread.Sleep(100);
                    
                    soundAlarm = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}

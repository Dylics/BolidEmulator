using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BolidEmulator
{
    public partial class BolidEmulatorGUI : Form
    {
        private const string SETTINGS_FILE = "settings.json";
        
        private BolidProtocol protocol;
        private SerialPort serialPort;
        private bool isConnected = false;
        private List<Tuple<byte, string, float>> foundDevices = new List<Tuple<byte, string, float>>();
        private bool scanning = false;
        private Thread scanThread;
        private bool polling = false;

        public BolidEmulatorGUI()
        {
            InitializeComponent();
            protocol = new BolidProtocol();
            protocol.SetDebug(true);
            
            baudrateComboBox.SelectedIndex = 1;
            parityComboBox.SelectedIndex = 0;
            stopbitsComboBox.SelectedIndex = 0;
            // По умолчанию быстрый режим без проверок
        }

        private void BolidEmulatorGUI_Load(object sender, EventArgs e)
        {
            UpdatePorts();
            LoadSettings();
            protocol.SetFastMode(true); // Всегда быстрый режим
        }

        private void BolidEmulatorGUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectPort();
        }

        private void UpdatePorts()
        {
            try
            {
                portComboBox.Items.Clear();
                string[] ports = SerialPort.GetPortNames();
                
                if (ports.Length > 0)
                {
                    foreach (string port in ports)
                    {
                        portComboBox.Items.Add($"🔵 {port} - COM порт");
                    }
                    portComboBox.SelectedIndex = 0;
                    Log($"COM ports updated: {ports.Length} total");
                }
                else
                {
                    portComboBox.Items.Add("COM-портов не обнаружено");
                    portComboBox.SelectedIndex = 0;
                    Log("COM-портов не найдено");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка обновления портов: {ex.Message}");
                portComboBox.Items.Add("Ошибка получения портов");
                portComboBox.SelectedIndex = 0;
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            logTextBox.ScrollToCaret();
        }

        private bool ConnectPort()
        {
            if (isConnected)
                return true;

            string portName = portComboBox.Text;
            if (string.IsNullOrEmpty(portName) || portName.Contains("COM-портов не обнаружено"))
            {
                MessageBox.Show("Выберите COM-порт", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (portName.Contains(" - "))
                portName = portName.Split(new[] { " - " }, StringSplitOptions.None)[0];
            portName = portName.Replace("🔵", "").Replace("🟢", "").Replace("🔴", "").Replace("⚪", "").Trim();

            try
            {
                int baudrate = int.Parse(baudrateComboBox.Text);
                Parity parity = parityComboBox.Text == "None" ? Parity.None : 
                               parityComboBox.Text == "Odd" ? Parity.Odd : Parity.Even;
                StopBits stopbits = stopbitsComboBox.Text == "1" ? StopBits.One : StopBits.Two;

                Log($"Подключение к порту {portName}...");

                int readTimeout = 300; // Быстрый режим
                int writeTimeout = 300; // Быстрый режим

                serialPort = new SerialPort(portName, baudrate, parity, 8, stopbits)
                {
                    ReadTimeout = readTimeout,
                    WriteTimeout = writeTimeout
                };

                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();


                protocol.StartMonitoring(serialPort);

                isConnected = true;
                connectButton.Text = "Отключиться";

                SaveSettings();

                Log($"Успешно подключен к порту {portName}");
                return true;
            }
            catch (Exception ex)
            {
                CleanupPort();
                Log($"Ошибка порта {portName}: {ex.Message}");
                MessageBox.Show(
                    $"Не удалось открыть порт {portName}:\n{ex.Message}\n\n" +
                    $"Возможные причины:\n" +
                    $"• Порт уже используется другой программой\n" +
                    $"• Порт не существует\n" +
                    $"• Недостаточно прав доступа",
                    "Ошибка порта", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void CleanupPort()
        {
            try
            {
                if (serialPort != null)
                {
                    try
                    {
                        if (serialPort.IsOpen)
                            serialPort.Close();
                    }
                    catch { }
                    serialPort = null;
                }
                isConnected = false;
                connectButton.Text = "Подключиться";
            }
            catch { }
        }

        private void DisconnectPort()
        {
            try
            {
                string portName = "неизвестный";

                if (protocol != null)
                {
                    try
                    {
                        protocol.StopMonitoring();
                    }
                    catch (Exception ex)
                    {
                        Log($"Ошибка остановки мониторинга: {ex.Message}");
                    }
                }

                if (serialPort != null)
                {
                    try
                    {
                        portName = serialPort.PortName;
                        if (serialPort.IsOpen)
                        {
                            serialPort.Close();
                            try
                            {
                                serialPort.DiscardInBuffer();
                                serialPort.DiscardOutBuffer();
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Ошибка закрытия порта: {ex.Message}");
                    }
                    finally
                    {
                        serialPort = null;
                    }
                }

                isConnected = false;

                if (scanning)
                {
                    scanning = false;
                    if (scanThread != null && scanThread.IsAlive)
                    {
                        try
                        {
                            scanThread.Join(1000);
                        }
                        catch { }
                    }
                }

                if (polling)
                {
                    polling = false;
                }

                try
                {
                    connectButton.Text = "Подключиться";
                }
                catch { }

                Log("Отключен от порта");
            }
            catch (Exception ex)
            {
                Log($"Ошибка при отключении: {ex.Message}");
                try
                {
                    serialPort = null;
                    isConnected = false;
                }
                catch { }
            }
        }

        private void PollDevice()
        {
            if (!ConnectPort())
                return;

            if (polling)
            {
                Log("Опрос устройства уже выполняется");
                return;
            }

            byte addr = (byte)addrNumericUpDown.Value;
            Log($"Запрос типа устройства на адрес {addr}");

            polling = true;

            Thread pollThread = new Thread(() =>
            {
                try
                {
                    var success = protocol.RequestDeviceTypeVersion(addr, serialPort);
                    if (!success.Item1)
                    {
                        Invoke(new Action(() => Log($"Ошибка отправки: {success.Item2}")));
                        return;
                    }

                    double responseTimeout = 3.0; // Быстрый режим

                    var response = protocol.GetResponse(responseTimeout, addr);
                    if (response != null)
                    {
                        Invoke(new Action(() => ParseDeviceVersionResponse(response)));
                    }
                    else
                    {
                        Invoke(new Action(() => Log("Таймаут ответа")));
                    }
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => Log($"Ошибка опроса: {ex.Message}")));
                }
                finally
                {
                    polling = false;
                }
            });
            pollThread.IsBackground = true;
            pollThread.Start();
        }

        private void ParseDeviceVersionResponse(object response)
        {
            byte magic = 0;
            byte deviceType = 0;
            byte addr = 0;
            byte deviceVersion = 0;

            if (response is ResponseDeviceTypeVersionShort shortResp)
            {
                magic = shortResp.Magic;
                deviceType = shortResp.DeviceType;
                addr = shortResp.Addr;
                deviceVersion = shortResp.DeviceVersion;
            }
            else if (response is ResponseDeviceTypeVersionMiddle midResp)
            {
                magic = midResp.Magic;
                deviceType = midResp.DeviceType;
                addr = midResp.Addr;
                deviceVersion = midResp.DeviceVersion;
            }
            else if (response is ResponseDeviceTypeVersionLong longResp)
            {
                magic = longResp.Magic;
                deviceType = longResp.DeviceType;
                addr = longResp.Addr;
                deviceVersion = longResp.DeviceVersion;
            }
            else
            {
                Log("Неожиданный тип ответа");
                return;
            }

            if (magic != 0)
            {
                Log($"Ошибка (код 0x{magic:X2})");
                return;
            }

            string deviceName = BolidConstants.DEVICES.ContainsKey(deviceType) 
                ? BolidConstants.DEVICES[deviceType] 
                : $"Неизвестный код типа устройства: {deviceType}";

            float versionFloat = deviceVersion / 100.0f;

            bool deviceExists = foundDevices.Any(d => d.Item1 == addr);
            if (!deviceExists)
            {
                foundDevices.Add(Tuple.Create(addr, deviceName, versionFloat));
                UpdateDevicesList();
            }

            string result = $"Адрес: {addr}\nТип устройства: {deviceName}\nВерсия: {versionFloat:F2}\nРезультат: OK";
            Log(result);
        }

        private void ScanDevices()
        {
            if (!ConnectPort())
                return;

            if (scanning)
            {
                Log("Сканирование уже выполняется");
                return;
            }

            byte startAddr = (byte)scanStartNumericUpDown.Value;
            byte endAddr = (byte)scanEndNumericUpDown.Value;
            double timeout = (double)scanTimeoutNumericUpDown.Value;

            if (startAddr > endAddr)
            {
                MessageBox.Show("Начальный адрес не может быть больше конечного", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (endAddr - startAddr > 127)
            {
                var result = MessageBox.Show(
                    $"Сканирование {endAddr - startAddr + 1} адресов может занять много времени. Продолжить?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;
            }

            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show("Порт не подключен", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            scanning = true;
            scanButton.Enabled = false;
            stopScanButton.Enabled = true;

            Log($"Начинаем сканирование адресов {startAddr}-{endAddr}");

            scanThread = new Thread(() =>
            {
                try
                {
                    bool ProgressCallback(byte addr, int foundCount, int total)
                    {
                        if (!scanning)
                            return false;

                        float progress = (addr - startAddr + 1) / (float)total * 100;
                        Invoke(new Action(() => statusLabel.Text = 
                            $"Сканирование... {addr}/{endAddr} ({progress:F1}%) - найдено: {foundCount}"));
                        return true;
                    }

                    void DeviceFoundCallback(byte addr, string deviceName, float deviceVersion)
                    {
                        if (!scanning)
                            return;

                        bool deviceExists = foundDevices.Any(d => d.Item1 == addr);
                        if (!deviceExists)
                        {
                            foundDevices.Add(Tuple.Create(addr, deviceName, deviceVersion));
                            Invoke(new Action(() => UpdateDevicesList()));
                            Invoke(new Action(() => Log($"Найдено устройство: адрес {addr}, тип: {deviceName}, версия: {deviceVersion:F2}")));
                        }
                    }

                    var devices = protocol.ScanDevices(serialPort, startAddr, endAddr, timeout, 
                        ProgressCallback, DeviceFoundCallback);

                    if (scanning)
                    {
                        Invoke(new Action(() => ShowScanResults(devices)));
                    }
                    else
                    {
                        Invoke(new Action(() => Log("Сканирование остановлено пользователем")));
                    }
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => Log($"Ошибка сканирования: {ex.Message}")));
                    Invoke(new Action(() => MessageBox.Show($"Ошибка сканирования: {ex.Message}", 
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
                finally
                {
                    scanning = false;
                    Invoke(new Action(() => scanButton.Enabled = true));
                    Invoke(new Action(() => stopScanButton.Enabled = false));
                    Invoke(new Action(() => statusLabel.Text = "Готов к работе"));
                }
            });
            scanThread.IsBackground = true;
            scanThread.Start();
        }

        private void ShowScanResults(List<Tuple<byte, string, float>> devices)
        {
            if (devices.Count == 0)
            {
                Log("Устройства не найдены");
                MessageBox.Show("Устройства не найдены в указанном диапазоне адресов", 
                    "Результат сканирования", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Log($"Найдено устройств: {devices.Count}");
            foreach (var device in devices)
            {
                Log($"Адрес {device.Item1}: {device.Item2} v{device.Item3:F2}");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Найдено устройств: {devices.Count}\n");
            foreach (var device in devices)
            {
                sb.AppendLine($"Адрес {device.Item1}: {device.Item2} v{device.Item3:F2}");
            }

            MessageBox.Show(sb.ToString(), "Результат сканирования", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateDevicesList()
        {
            devicesListBox.Items.Clear();

            foreach (var device in foundDevices)
            {
                string deviceText = $"Адрес {device.Item1}: {device.Item2} v{device.Item3:F2}";
                devicesListBox.Items.Add(deviceText);
            }

            if (foundDevices.Count == 0)
            {
                devicesListBox.Items.Add("Устройства не найдены");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    string json = File.ReadAllText(SETTINGS_FILE);
                    JObject settings = JObject.Parse(json);

                    if (settings["com_port"] != null)
                    {
                        string portName = settings["com_port"].ToString();
                        for (int i = 0; i < portComboBox.Items.Count; i++)
                        {
                            if (portComboBox.Items[i].ToString().Contains(portName))
                            {
                                portComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    if (settings["baudrate"] != null)
                        baudrateComboBox.Text = settings["baudrate"].ToString();
                    if (settings["parity"] != null)
                        parityComboBox.Text = settings["parity"].ToString();
                    if (settings["stopbits"] != null)
                        stopbitsComboBox.Text = settings["stopbits"].ToString();
                    if (settings["device_address"] != null)
                        addrNumericUpDown.Value = (int)settings["device_address"];
                    // Проверки порта отключены - всегда быстрый режим
                    protocol.SetFastMode(true);

                    Log("Настройки подключения загружены");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string portName = portComboBox.Text;
                if (portName.Contains(" - "))
                    portName = portName.Split(new[] { " - " }, StringSplitOptions.None)[0];
                portName = portName.Replace("🔵", "").Replace("🟢", "").Replace("🔴", "").Replace("⚪", "").Trim();

                var settings = new
                {
                    com_port = portName,
                    baudrate = baudrateComboBox.Text,
                    parity = parityComboBox.Text,
                    stopbits = stopbitsComboBox.Text,
                    device_address = (int)addrNumericUpDown.Value,
                    fast_port = false, // Всегда быстрый режим
                    last_saved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SETTINGS_FILE, json);

                Log("Настройки подключения сохранены");
            }
            catch (Exception ex)
            {
                Log($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void refreshPortsButton_Click(object sender, EventArgs e)
        {
            UpdatePorts();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                DisconnectPort();
            }
            else
            {
                if (ConnectPort())
                {
                    connectButton.Text = "Отключиться";
                }
            }
        }


        private void pollDeviceButton_Click(object sender, EventArgs e)
        {
            PollDevice();
        }

        private void scanButton_Click(object sender, EventArgs e)
        {
            ScanDevices();
        }

        private void stopScanButton_Click(object sender, EventArgs e)
        {
            if (scanning)
            {
                scanning = false;
                Log("Остановка сканирования...");
                statusLabel.Text = "Сканирование остановлено";
            }
        }

        private void clearDevicesButton_Click(object sender, EventArgs e)
        {
            foundDevices.Clear();
            devicesListBox.Items.Clear();
            devicesListBox.Items.Add("Устройства не найдены");
            Log("Список устройств очищен");
        }

        private void devicesListBox_DoubleClick(object sender, EventArgs e)
        {
            if (scanning)
            {
                Log("Остановка сканирования для открытия управления устройством");
                scanning = false;
                if (scanThread != null && scanThread.IsAlive)
                {
                    scanThread.Join(1000);
                }
            }

            if (polling)
            {
                Log("Остановка опроса устройства для открытия управления");
                polling = false;
            }

            OpenDeviceControl();
        }

        private void OpenDeviceControl()
        {
            if (devicesListBox.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите устройство из списка", "Предупреждение", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (foundDevices.Count == 0)
            {
                MessageBox.Show("Нет найденных устройств", "Предупреждение", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int deviceIndex = devicesListBox.SelectedIndex;
            if (deviceIndex >= foundDevices.Count)
            {
                MessageBox.Show("Неверный индекс устройства", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var device = foundDevices[deviceIndex];

            if (!isConnected)
            {
                string deviceInfoText = $"Информация об устройстве:\n\n" +
                    $"Адрес: {device.Item1}\n" +
                    $"Тип: {device.Item2}\n" +
                    $"Версия: {device.Item3:F2}\n\n" +
                    $"Для управления устройством необходимо подключение к COM-порту.\n" +
                    $"Подключиться сейчас?";

                if (MessageBox.Show(deviceInfoText, "Информация об устройстве", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                if (!ConnectPort())
                    return;
            }

            protocol.ClearResponses();

            try
            {
                // Создаем DeviceInfo для устройства
                int deviceCode = GetDeviceCodeFromName(device.Item2);
                int maxBranches = GetMaxBranchesForDevice(device.Item1, device.Item2);
                int maxRelays = GetMaxRelaysForDevice(device.Item1, device.Item2);
                
                Log($"Создание DeviceInfo: название={device.Item2}, код={deviceCode}, шлейфы={maxBranches}, реле={maxRelays}");
                
                var deviceType = new DeviceType(deviceCode, device.Item2, maxBranches, maxRelays);
                var deviceInfo = new DeviceInfo(device.Item1, deviceType, device.Item3);

                // Создаем DeviceManager
                var deviceManager = new DeviceManager(protocol, serialPort);
                
                // Регистрируем устройство в DeviceManager
                deviceManager.RegisterDevice(deviceInfo);

                // Создаем и показываем панель управления
                var controlPanel = new DeviceControlPanel(this, deviceManager, deviceInfo, Log);
                controlPanel.StartPosition = FormStartPosition.CenterScreen;
                controlPanel.Show();
                
                Log($"Открыто управление устройством: {device.Item2} (адрес {device.Item1})");
            }
            catch (Exception ex)
            {
                Log($"Ошибка открытия панели управления: {ex.Message}");
                MessageBox.Show($"Ошибка открытия панели управления: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void s2000ppMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (isConnected)
                {
                    DisconnectPort();
                    Log("Отключен от COM-порта для работы с С2000-ПП");
                }

                var s2000ppWindow = new S2000PPWindow(this);
                s2000ppWindow.StartPosition = FormStartPosition.CenterScreen;
                s2000ppWindow.Show();
                Log("Открыто окно управления С2000-ПП");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна С2000-ПП: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            Form aboutForm = new Form
            {
                Text = "О программе",
                Size = new Size(500, 500),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label titleLabel = new Label
            {
                Text = "Эмулятор пульта С2000/С2000М v1.0",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(460, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label descLabel = new Label
            {
                Text = "Программа для управления устройствами охранно-пожарной сигнализации Bolid через последовательный интерфейс.\n\n" +
                       "Функции:\n" +
                       "• Опрос типа и версии устройства\n" +
                       "• Управление шлейфами (взятие/снятие)\n" +
                       "• Запрос состояния шлейфов и АЦП\n" +
                       "• Управление реле с различными программами\n" +
                       "• Управление С2000-ПП через Modbus RTU\n\n" +
                       "Протокол: Bolid RS-485/RS-232\n" +
                       "Таймаут: 3000 мс",
                Font = new Font("Arial", 10),
                Location = new Point(20, 60),
                Size = new Size(460, 200),
                TextAlign = ContentAlignment.TopLeft
            };

            GroupBox authorGroup = new GroupBox
            {
                Text = "Автор программы",
                Location = new Point(20, 270),
                Size = new Size(460, 60)
            };

            Label authorLabel = new Label
            {
                Text = "Зайнуллин Тимур Сергеевич",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 20),
                Size = new Size(440, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            authorGroup.Controls.Add(authorLabel);

            GroupBox contactsGroup = new GroupBox
            {
                Text = "Контакты",
                Location = new Point(20, 340),
                Size = new Size(460, 80)
            };

            Label contactsLabel = new Label
            {
                Text = "Сайт: https://dylics.online/\n" +
                       "Email: dylics@gmail.com\n" +
                       "Telegram: @Dylics_STR",
                Font = new Font("Arial", 10),
                Location = new Point(10, 20),
                Size = new Size(440, 55),
                TextAlign = ContentAlignment.TopLeft
            };
            contactsGroup.Controls.Add(contactsLabel);

            Button closeButton = new Button
            {
                Text = "Закрыть",
                Location = new Point(200, 430),
                Size = new Size(100, 30)
            };
            closeButton.Click += (s, ev) => aboutForm.Close();

            aboutForm.Controls.Add(titleLabel);
            aboutForm.Controls.Add(descLabel);
            aboutForm.Controls.Add(authorGroup);
            aboutForm.Controls.Add(contactsGroup);
            aboutForm.Controls.Add(closeButton);

            aboutForm.ShowDialog(this);
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private int GetDeviceCodeFromName(string deviceName)
        {
            // Определяем код устройства по названию
            if (deviceName.Contains("С2000-КДЛ")) return 9;
            if (deviceName.Contains("С2000-КПБ")) return 15;
            if (deviceName.Contains("Сигнал-20П")) return 2;
            if (deviceName.Contains("Сигнал-10")) return 32;
            if (deviceName.Contains("С2000-ПП")) return 36;
            if (deviceName.Contains("С2000-СП1")) return 1;
            if (deviceName.Contains("С2000-СП2")) return 11;
            if (deviceName.Contains("С2000-СП1 ИСП.01")) return 26;
            if (deviceName.Contains("С2000-СП2 ИСП.01")) return 34;
            if (deviceName.Contains("МИП-12")) return 48;
            if (deviceName.Contains("МИП-24")) return 49;
            if (deviceName.Contains("РИП-12")) return 38;
            if (deviceName.Contains("РИП-12 ИСП.01")) return 39;
            if (deviceName.Contains("РИП-12 ИСП.02")) return 54;
            if (deviceName.Contains("РИП-12 ИСП.03")) return 55;
            if (deviceName.Contains("РИП-12 ИСП.04")) return 79;
            if (deviceName.Contains("РИП-12 ИСП.05")) return 80;
            if (deviceName.Contains("С2000-АР1")) return 41;
            if (deviceName.Contains("С2000-АР2")) return 61;
            if (deviceName.Contains("С2000-АР8")) return 81;
            
            return 0; // Неизвестное устройство
        }

        private int GetMaxBranchesForDevice(byte address, string deviceName)
        {
            int deviceCode = GetDeviceCodeFromName(deviceName);
            
            switch (deviceCode)
            {
                case 9: return 127; // С2000-КДЛ
                case 1: return 1;   // С2000-СП1
                case 2: return 20;   // Сигнал-20П (исправлено с 2 на 20)
                case 11: return 2; // С2000-СП2
                case 15: return 2; // С2000-КПБ
                case 26: return 1; // С2000-СП1 ИСП.01
                case 32: return 10; // Сигнал-10 (исправлено с 1 на 10)
                case 34: return 2; // С2000-СП2 ИСП.01
                case 41: return 1; // С2000-АР1
                case 61: return 2; // С2000-АР2
                case 81: return 8; // С2000-АР8
                // МИП устройства (модули источников питания)
                case 48: return 5; // МИП-12
                case 49: return 5; // МИП-24
                // РИП устройства (резервированные источники питания)
                case 33: return 5; // РИП-12
                case 38: return 5; // РИП-12 ИСП.01
                case 39: return 5; // РИП-24 ИСП.01
                case 54: return 5; // РИП-12 ИСП.04
                case 55: return 5; // РИП-24 ИСП.05
                case 79: return 5; // РИП-12 ИСП.06
                case 80: return 5; // РИП-24 ИСП.07
                default: return 0;
            }
        }

        private int GetMaxRelaysForDevice(byte address, string deviceName)
        {
            int deviceCode = GetDeviceCodeFromName(deviceName);
            
            switch (deviceCode)
            {
                case 2: return 5;  // Сигнал-20П (исправлено с 6 на 5)
                case 15: return 6; // С2000-КПБ (исправлено с 4 на 6)
                case 32: return 4; // Сигнал-10
                // МИП устройства (модули источников питания) - НЕ ИМЕЮТ РЕЛЕ
                case 48: return 0; // МИП-12
                case 49: return 0; // МИП-24
                // РИП устройства (резервированные источники питания)
                case 33: return 2; // РИП-12
                case 38: return 2; // РИП-12 ИСП.01
                case 39: return 4; // РИП-24 ИСП.01
                case 54: return 2; // РИП-12 ИСП.04
                case 55: return 4; // РИП-24 ИСП.05
                case 79: return 2; // РИП-12 ИСП.06
                case 80: return 4; // РИП-24 ИСП.07
                default: return 0;
            }
        }
    }
}

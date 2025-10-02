using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BolidEmulator
{
    public enum ZoneState
    {
        UNKNOWN,
        ARMED,
        DISARMED,
        ALARM,
        FAULT,
        BYPASS
    }

    public enum DivisionState
    {
        UNKNOWN,
        ARMED,
        DISARMED,
        ARMED_PROCESS,
        DISARMED_PROCESS,
        ALARM,
        FIRE,
        FAULT,
        TEST,
        BLOCKED,
        AUTO_ON,
        AUTO_OFF
    }

    public class ZoneInfo
    {
        public int Number { get; set; }
        public int Address { get; set; }
        public int Shs { get; set; }
        public int ZoneType { get; set; }
        public ZoneState CurrentState { get; set; }
        public int? RawStateCode { get; set; }
        public List<string> ExtendedStates { get; set; }

        public ZoneInfo()
        {
            ExtendedStates = new List<string>();
        }
    }

    public class RelayInfo
    {
        public int Number { get; set; }
        public int Address { get; set; }
        public int RelayInDevice { get; set; }
        public bool IsOn { get; set; }
    }

    public class DivisionInfo
    {
        public int Number { get; set; }
        public int Address { get; set; }
        public DivisionState CurrentState { get; set; }
        public string Description { get; set; }
    }

    public class AnalogValue
    {
        public int ZoneNumber { get; set; }
        public float? Temperature { get; set; }
        public float? Humidity { get; set; }
        public float? CoConcentration { get; set; }
    }

    public class S2000PPManager
    {
        private Action<string> logCallback;
        private SerialPort client;
        private bool isConnected;
        private int ppAddress;

        public Dictionary<int, ZoneInfo> ZonesConfig { get; set; }
        public Dictionary<int, RelayInfo> RelaysConfig { get; set; }
        public Dictionary<int, DivisionInfo> DivisionsConfig { get; set; }
        public Dictionary<int, AnalogValue> AnalogValues { get; set; }

        private List<Action<string, object>> updateCallbacks;

        private Dictionary<int, string> zoneStateDecoder;

        public S2000PPManager(Action<string> logCallback = null)
        {
            this.logCallback = logCallback ?? (msg => Console.WriteLine(msg));
            client = null;
            isConnected = false;
            ppAddress = 1;

            ZonesConfig = new Dictionary<int, ZoneInfo>();
            RelaysConfig = new Dictionary<int, RelayInfo>();
            DivisionsConfig = new Dictionary<int, DivisionInfo>();
            AnalogValues = new Dictionary<int, AnalogValue>();

            updateCallbacks = new List<Action<string, object>>();

            InitializeZoneStateDecoder();
        }

        private void InitializeZoneStateDecoder()
        {
            zoneStateDecoder = new Dictionary<int, string>
            {
                { 0x6d2f, "Снят с охраны, восстановлена работа ДПЛС" },
                { 0x182f, "Взят на охрану" },
                { 0xfa00, "Взят на охрану" },
                { 0x6d00, "Шлейф снят" },
                { 0x2f00, "Восстановлена работа ДПЛС" },
                { 0xffff, "Неисправность" },
                { 0xff00, "Обрыв" },
                { 0x00ff, "Короткое замыкание" },
                { 0x0001, "Норма" },
                { 0x0002, "Нарушение" },
                { 0x0004, "Тревога" },
                { 0x0008, "Пожар" },
                { 0x0010, "Внимание" },
                { 0x0020, "Предупреждение" },
                { 0x0040, "Неисправность" },
                { 0x0080, "Блокировка" },
                { 0x0100, "Автоматика отключена" },
                { 0x0200, "Автоматика включена" },
                { 0x0400, "Тест" },
                { 0x0800, "Программирование" },
                { 0x1000, "Связь потеряна" },
                { 0x2000, "Связь восстановлена" },
                { 0x4000, "Питание в норме" },
                { 0x8000, "Питание не в норме" },
                { 0x0000, "Неопределенное состояние" },
                { 0x0003, "Нарушение + Норма" },
                { 0x0005, "Тревога + Норма" },
                { 0x0006, "Тревога + Нарушение" },
                { 0x0007, "Тревога + Нарушение + Норма" },
                { 0x0009, "Пожар + Норма" },
                { 0x000A, "Пожар + Нарушение" },
                { 0x000B, "Пожар + Нарушение + Норма" },
                { 0x000C, "Пожар + Тревога" },
                { 0x000D, "Пожар + Тревога + Норма" },
                { 0x000E, "Пожар + Тревога + Нарушение" },
                { 0x000F, "Пожар + Тревога + Нарушение + Норма" }
            };
        }

        public void AddUpdateCallback(Action<string, object> callback)
        {
            updateCallbacks.Add(callback);
        }

        private void NotifyUpdate(string updateType, object data)
        {
            foreach (var callback in updateCallbacks)
            {
                try
                {
                    callback(updateType, data);
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка в callback обновления: {e.Message}");
                }
            }
        }

        public bool Connect(string port, int baudrate = 9600, string parity = "N", int stopbits = 1, int address = 1)
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                }

                client = new SerialPort(port, baudrate)
                {
                    Parity = parity == "E" ? Parity.Even : parity == "O" ? Parity.Odd : Parity.None,
                    StopBits = stopbits == 1 ? StopBits.One : StopBits.Two,
                    DataBits = 8,
                    ReadTimeout = 2000,
                    WriteTimeout = 2000
                };

                ppAddress = address;

                client.Open();
                isConnected = true;
                logCallback($"Подключено к С2000-ПП на {port} (адрес {address})");
                return true;
            }
            catch (Exception e)
            {
                logCallback($"Ошибка подключения: {e.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (client != null)
                {
                    try
                    {
                        client.Close();
                    }
                    catch (Exception e)
                    {
                        logCallback($"Ошибка закрытия соединения: {e.Message}");
                    }
                    finally
                    {
                        client = null;
                    }
                }
                isConnected = false;
                logCallback("Отключено от С2000-ПП");
            }
            catch (Exception e)
            {
                logCallback($"Ошибка при отключении: {e.Message}");
                client = null;
                isConnected = false;
            }
        }

        public string DecodeZoneState(int code)
        {
            if (zoneStateDecoder.ContainsKey(code))
            {
                return zoneStateDecoder[code];
            }

            if (code == 6191)
                return "Взят на охрану";
            if (code == 64000)
                return "Взят на охрану";
            if (code == 27951)
                return "Снят с охраны, восстановлена работа ДПЛС";
            if (code == 0x6d00)
                return "Шлейф снят";
            if (code == 0x2f00)
                return "Восстановлена работа ДПЛС";
            if (code == 0xffff)
                return "Неисправность";
            if (code == 0xff00)
                return "Обрыв";
            if (code == 0x00ff)
                return "Короткое замыкание";

            var states = new List<string>();
            if ((code & 0x0001) != 0) states.Add("Норма");
            if ((code & 0x0002) != 0) states.Add("Нарушение");
            if ((code & 0x0004) != 0) states.Add("Тревога");
            if ((code & 0x0008) != 0) states.Add("Пожар");
            if ((code & 0x0010) != 0) states.Add("Внимание");
            if ((code & 0x0020) != 0) states.Add("Предупреждение");
            if ((code & 0x0040) != 0) states.Add("Неисправность");
            if ((code & 0x0080) != 0) states.Add("Блокировка");
            if ((code & 0x0100) != 0) states.Add("Автоматика отключена");
            if ((code & 0x0200) != 0) states.Add("Автоматика включена");
            if ((code & 0x0400) != 0) states.Add("Тест");
            if ((code & 0x0800) != 0) states.Add("Программирование");
            if ((code & 0x1000) != 0) states.Add("Связь потеряна");
            if ((code & 0x2000) != 0) states.Add("Связь восстановлена");
            if ((code & 0x4000) != 0) states.Add("Питание в норме");
            if ((code & 0x8000) != 0) states.Add("Питание не в норме");

            if (states.Count > 0)
            {
                return string.Join(" | ", states);
            }
            else
            {
                return $"Неизвестное состояние (0x{code:X4}, {code})";
            }
        }

        public string DecodeDivisionState(int stateCode)
        {
            var divisionStates = new Dictionary<int, string>
            {
                { 0x0000, "Снят с охраны" },
                { 0x0001, "Взят на охрану" },
                { 0x0002, "Взятие" },
                { 0x0003, "Снятие" },
                { 0x0004, "Тревога" },
                { 0x0005, "Пожар" },
                { 0x0006, "Неисправность" },
                { 0x0007, "Тест" },
                { 0x0008, "Блокировка" },
                { 0x0009, "Автоматика включена" },
                { 0x000A, "Автоматика отключена" }
            };

            return divisionStates.ContainsKey(stateCode) 
                ? divisionStates[stateCode] 
                : $"Неизвестное состояние (0x{stateCode:X4})";
        }

        public bool ReadConfiguration()
        {
            if (!isConnected || client == null)
            {
                logCallback("Нет подключения к С2000-ПП");
                return false;
            }

            try
            {
                ZonesConfig.Clear();
                for (int zoneNum = 1; zoneNum <= 32; zoneNum++)
                {
                    int startAddr = (zoneNum - 1) * 4;
                    // Симуляция чтения регистров
                    var zoneInfo = new ZoneInfo
                    {
                        Number = zoneNum,
                        Address = startAddr,
                        Shs = zoneNum,
                        ZoneType = 1,
                        CurrentState = ZoneState.UNKNOWN
                    };
                    ZonesConfig[zoneNum] = zoneInfo;
                }

                RelaysConfig.Clear();
                for (int relayNum = 1; relayNum <= 16; relayNum++)
                {
                    int startAddr = 2048 + (relayNum - 1) * 2;
                    var relayInfo = new RelayInfo
                    {
                        Number = relayNum,
                        Address = startAddr,
                        RelayInDevice = relayNum
                    };
                    RelaysConfig[relayNum] = relayInfo;
                }

                DivisionsConfig.Clear();
                for (int divisionNum = 1; divisionNum <= 32; divisionNum++)
                {
                    int startAddr = 2560 + (divisionNum - 1) * 4;
                    var divisionInfo = new DivisionInfo
                    {
                        Number = divisionNum,
                        Address = startAddr,
                        CurrentState = DivisionState.UNKNOWN,
                        Description = $"Раздел {divisionNum}"
                    };
                    DivisionsConfig[divisionNum] = divisionInfo;
                }

                logCallback("Конфигурация успешно считана");
                NotifyUpdate("configuration_loaded", new
                {
                    zones = ZonesConfig.Count,
                    relays = RelaysConfig.Count,
                    divisions = DivisionsConfig.Count
                });

                return true;
            }
            catch (Exception e)
            {
                logCallback($"Ошибка при чтении конфигурации: {e.Message}");
                return false;
            }
        }

        public List<string> GetAvailablePorts()
        {
            var ports = new List<string>();
            try
            {
                foreach (string portName in SerialPort.GetPortNames())
                {
                    ports.Add($"{portName} - {portName}");
                }
            }
            catch (Exception e)
            {
                logCallback($"Ошибка получения списка портов: {e.Message}");
            }
            return ports;
        }

        public Dictionary<int, string> ScanDevices(string port, int baudrate = 9600, string parity = "N", 
            int stopbits = 1, Action<int, string, int, int> callback = null, 
            CancellationTokenSource stopEvent = null, Action<int, int> progressCallback = null)
        {
            var foundDevices = new Dictionary<int, string>();

            Task.Run(() =>
            {
                SerialPort testClient = null;
                try
                {
                    testClient = new SerialPort(port, baudrate)
                    {
                        Parity = parity == "E" ? Parity.Even : parity == "O" ? Parity.Odd : Parity.None,
                        StopBits = stopbits == 1 ? StopBits.One : StopBits.Two,
                        DataBits = 8,
                        ReadTimeout = 200
                    };

                    testClient.Open();

                    int totalAddresses = 127;
                    for (int address = 1; address <= 127; address++)
                    {
                        if (stopEvent != null && stopEvent.Token.IsCancellationRequested)
                        {
                            logCallback("Сканирование прервано пользователем");
                            break;
                        }

                        if (progressCallback != null)
                        {
                            progressCallback(address, totalAddresses);
                        }

                        try
                        {
                            // Симуляция сканирования - в реальности здесь был бы Modbus запрос
                            if (address == 1) // Симулируем найденное устройство
                            {
                                string desc = $"С2000-ПП (адрес {address}, версия 123)";
                                foundDevices[address] = desc;
                                if (callback != null)
                                {
                                    callback(address, desc, address, totalAddresses);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Игнорируем ошибки при сканировании отдельных адресов
                        }
                        Thread.Sleep(10);
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка сканирования: {e.Message}");
                }
                finally
                {
                    if (testClient != null)
                    {
                        try
                        {
                            testClient.Close();
                        }
                        catch (Exception)
                        {
                            // Игнорируем ошибки при закрытии
                        }
                    }
                }
            });

            return foundDevices;
        }

        public void UpdateZonesStates(Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    int updatedCount = 0;

                    foreach (var kvp in ZonesConfig)
                    {
                        int zoneNum = kvp.Key;
                        var zoneInfo = kvp.Value;

                        try
                        {
                            // Симуляция чтения состояния зоны
                            int modbusAddr = 40000 + (zoneNum - 1);
                            int stateCode = 0x182f; // Симулируем состояние "Взят на охрану"
                            string stateStr = DecodeZoneState(stateCode);

                            zoneInfo.RawStateCode = stateCode;

                            if (stateCode == 0x182f || stateCode == 0xfa00 || stateCode == 6191 || stateCode == 64000)
                            {
                                zoneInfo.CurrentState = ZoneState.ARMED;
                            }
                            else if (stateCode == 0x6d2f || stateCode == 0x6d00 || stateCode == 27951)
                            {
                                zoneInfo.CurrentState = ZoneState.DISARMED;
                            }
                            else if (stateCode == 0x0004 || stateCode == 0x0008)
                            {
                                zoneInfo.CurrentState = ZoneState.ALARM;
                            }
                            else if (stateCode == 0x0040 || stateCode == 0xff00 || stateCode == 0x00ff)
                            {
                                zoneInfo.CurrentState = ZoneState.FAULT;
                            }
                            else
                            {
                                zoneInfo.CurrentState = ZoneState.UNKNOWN;
                            }

                            updatedCount++;
                            logCallback($"Зона {zoneNum}: {stateStr} (0x{stateCode:X4})");
                        }
                        catch (Exception e)
                        {
                            logCallback($"Ошибка обновления зоны {zoneNum}: {e.Message}");
                            continue;
                        }
                    }

                    NotifyUpdate("zones_updated", new { count = updatedCount });

                    if (callback != null)
                    {
                        callback(true, $"Обновлено {updatedCount} зон");
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка обновления состояний зон: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void ArmZone(int zoneNum, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!ZonesConfig.ContainsKey(zoneNum))
            {
                callback?.Invoke(false, $"Зона {zoneNum} не найдена в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var zoneInfo = ZonesConfig[zoneNum];
                    int modbusAddr = 40000 + (zoneNum - 1);

                    // Симуляция отправки команды взятия на охрану (код 24)
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    int newState = 0x182f; // Симулируем успешное взятие на охрану
                    if (newState == 0x182f || newState == 0xfa00 || newState == 6191 || newState == 64000)
                    {
                        zoneInfo.CurrentState = ZoneState.ARMED;
                        NotifyUpdate("zone_updated", new { zone = zoneNum, state = "armed" });
                        logCallback($"Зона {zoneNum} взята на охрану");
                        if (callback != null)
                        {
                            callback(true, $"Зона {zoneNum} взята на охрану");
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(false, $"Зона {zoneNum} не изменила состояние");
                        }
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка взятия зоны на охрану: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void DisarmZone(int zoneNum, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!ZonesConfig.ContainsKey(zoneNum))
            {
                callback?.Invoke(false, $"Зона {zoneNum} не найдена в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var zoneInfo = ZonesConfig[zoneNum];
                    int modbusAddr = 40000 + (zoneNum - 1);

                    // Симуляция отправки команды снятия с охраны (код 109)
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    int newState = 0x6d2f; // Симулируем успешное снятие с охраны
                    if (newState == 0x6d2f || newState == 0x6d00 || newState == 27951)
                    {
                        zoneInfo.CurrentState = ZoneState.DISARMED;
                        NotifyUpdate("zone_updated", new { zone = zoneNum, state = "disarmed" });
                        logCallback($"Зона {zoneNum} снята с охраны");
                        if (callback != null)
                        {
                            callback(true, $"Зона {zoneNum} снята с охраны");
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(false, $"Зона {zoneNum} не изменила состояние");
                        }
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка снятия зоны с охраны: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void EnableZoneControl(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 111, "включить контроль ШС", callback);
        }

        public void DisableZoneControl(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 112, "выключить контроль ШС", callback);
        }

        public void EnableAutomation(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 148, "включить автоматику", callback);
        }

        public void DisableAutomation(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 142, "отключить автоматику", callback);
        }

        public void StartAspt(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 146, "пуск АСПТ", callback);
        }

        public void ResetAspt(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 143, "сброс пуска АСПТ", callback);
        }

        public void TestZone(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 19, "тест вход", callback);
        }

        public void EnterTestMode(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 20, "вход в режим тестирования", callback);
        }

        public void ExitTestMode(int zoneNum, Action<bool, string> callback = null)
        {
            SendZoneCommand(zoneNum, 21, "выход из режима тестирования", callback);
        }

        private void SendZoneCommand(int zoneNum, int commandCode, string commandName, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!ZonesConfig.ContainsKey(zoneNum))
            {
                callback?.Invoke(false, $"Зона {zoneNum} не найдена в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var zoneInfo = ZonesConfig[zoneNum];
                    int modbusAddr = 40000 + (zoneNum - 1);

                    // Симуляция отправки команды
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    int newState = 0x182f;
                    zoneInfo.CurrentState = DecodeZoneStateEnum(newState);
                    NotifyUpdate("zone_updated", new { zone = zoneNum, state = commandName });
                    logCallback($"Зона {zoneNum}: {commandName} (код {commandCode})");
                    if (callback != null)
                    {
                        callback(true, $"Зона {zoneNum}: {commandName}");
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка выполнения команды '{commandName}' для зоны {zoneNum}: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        private ZoneState DecodeZoneStateEnum(int stateCode)
        {
            if (stateCode == 0x182f || stateCode == 0xfa00 || stateCode == 6191 || stateCode == 64000)
                return ZoneState.ARMED;
            if (stateCode == 0x6d2f || stateCode == 0x6d00 || stateCode == 27951)
                return ZoneState.DISARMED;
            if (stateCode == 0xffff)
                return ZoneState.FAULT;
            if (stateCode == 0xff00)
                return ZoneState.FAULT;
            if (stateCode == 0x00ff)
                return ZoneState.FAULT;
            return ZoneState.UNKNOWN;
        }

        public void UpdateRelaysStates(Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    int updatedCount = 0;

                    foreach (var kvp in RelaysConfig)
                    {
                        int relayNum = kvp.Key;
                        var relayInfo = kvp.Value;

                        try
                        {
                            // Симуляция чтения состояния реле
                            int modbusAddr = 10000 + (relayNum - 1);
                            bool isOn = relayNum % 2 == 0; // Симулируем чередующиеся состояния
                            relayInfo.IsOn = isOn;
                            string stateStr = isOn ? "Включено" : "Выключено";
                            updatedCount++;
                            logCallback($"Реле {relayNum}: {stateStr}");
                        }
                        catch (Exception e)
                        {
                            logCallback($"Ошибка обновления реле {relayNum}: {e.Message}");
                            continue;
                        }
                    }

                    NotifyUpdate("relays_updated", new { count = updatedCount });

                    if (callback != null)
                    {
                        callback(true, $"Обновлено {updatedCount} реле");
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка обновления состояний реле: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void TurnOnRelay(int relayNum, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!RelaysConfig.ContainsKey(relayNum))
            {
                callback?.Invoke(false, $"Реле {relayNum} не найдено в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var relayInfo = RelaysConfig[relayNum];
                    int modbusAddr = 10000 + (relayNum - 1);

                    // Симуляция отправки команды включения реле
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    bool isOn = true;
                    if (isOn)
                    {
                        relayInfo.IsOn = true;
                        NotifyUpdate("relay_updated", new { relay = relayNum, state = "on" });
                        logCallback($"Реле {relayNum} включено");
                        if (callback != null)
                        {
                            callback(true, $"Реле {relayNum} включено");
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(false, $"Реле {relayNum} не изменило состояние");
                        }
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка включения реле: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void TurnOffRelay(int relayNum, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!RelaysConfig.ContainsKey(relayNum))
            {
                callback?.Invoke(false, $"Реле {relayNum} не найдено в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var relayInfo = RelaysConfig[relayNum];
                    int modbusAddr = 10000 + (relayNum - 1);

                    // Симуляция отправки команды выключения реле
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    bool isOn = false;
                    if (!isOn)
                    {
                        relayInfo.IsOn = false;
                        NotifyUpdate("relay_updated", new { relay = relayNum, state = "off" });
                        logCallback($"Реле {relayNum} выключено");
                        if (callback != null)
                        {
                            callback(true, $"Реле {relayNum} выключено");
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(false, $"Реле {relayNum} не изменило состояние");
                        }
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка выключения реле: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void UpdateDivisionsStates(Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    int updatedCount = 0;

                    foreach (var kvp in DivisionsConfig)
                    {
                        int divisionNum = kvp.Key;
                        var divisionInfo = kvp.Value;

                        try
                        {
                            // Симуляция чтения состояния раздела
                            int modbusAddr = 44096 + (divisionNum - 1);
                            int stateCode = 0x0000; // Симулируем состояние "Снят с охраны"
                            string stateStr = DecodeDivisionState(stateCode);

                            if (stateCode == 0x0000)
                                divisionInfo.CurrentState = DivisionState.DISARMED;
                            else if (stateCode == 0x0001)
                                divisionInfo.CurrentState = DivisionState.ARMED;
                            else if (stateCode == 0x0002)
                                divisionInfo.CurrentState = DivisionState.ARMED_PROCESS;
                            else if (stateCode == 0x0003)
                                divisionInfo.CurrentState = DivisionState.DISARMED_PROCESS;
                            else if (stateCode == 0x0004)
                                divisionInfo.CurrentState = DivisionState.ALARM;
                            else if (stateCode == 0x0005)
                                divisionInfo.CurrentState = DivisionState.FIRE;
                            else if (stateCode == 0x0006)
                                divisionInfo.CurrentState = DivisionState.FAULT;
                            else if (stateCode == 0x0007)
                                divisionInfo.CurrentState = DivisionState.TEST;
                            else if (stateCode == 0x0008)
                                divisionInfo.CurrentState = DivisionState.BLOCKED;
                            else if (stateCode == 0x0009)
                                divisionInfo.CurrentState = DivisionState.AUTO_ON;
                            else if (stateCode == 0x000A)
                                divisionInfo.CurrentState = DivisionState.AUTO_OFF;
                            else
                                divisionInfo.CurrentState = DivisionState.UNKNOWN;

                            updatedCount++;
                            logCallback($"Раздел {divisionNum}: {stateStr}");
                        }
                        catch (Exception e)
                        {
                            logCallback($"Ошибка обновления раздела {divisionNum}: {e.Message}");
                            continue;
                        }
                    }

                    NotifyUpdate("divisions_updated", new { count = updatedCount });

                    if (callback != null)
                    {
                        callback(true, $"Обновлено {updatedCount} разделов");
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка обновления состояний разделов: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void ArmDivision(int divisionNum, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!DivisionsConfig.ContainsKey(divisionNum))
            {
                callback?.Invoke(false, $"Раздел {divisionNum} не найден в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var divisionInfo = DivisionsConfig[divisionNum];
                    int modbusAddr = 44096 + (divisionNum - 1);

                    // Симуляция отправки команды взятия раздела на охрану (код 1)
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    int newState = 0x0001;
                    if (newState == 0x0001)
                    {
                        divisionInfo.CurrentState = DivisionState.ARMED;
                        NotifyUpdate("division_updated", new { division = divisionNum, state = "armed" });
                        logCallback($"Раздел {divisionNum} взят на охрану");
                        if (callback != null)
                        {
                            callback(true, $"Раздел {divisionNum} взят на охрану");
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(false, $"Раздел {divisionNum} не изменил состояние");
                        }
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка взятия раздела на охрану: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }

        public void DisarmDivision(int divisionNum, Action<bool, string> callback = null)
        {
            if (!isConnected || client == null)
            {
                callback?.Invoke(false, "Нет подключения к С2000-ПП");
                return;
            }

            if (!DivisionsConfig.ContainsKey(divisionNum))
            {
                callback?.Invoke(false, $"Раздел {divisionNum} не найден в конфигурации");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var divisionInfo = DivisionsConfig[divisionNum];
                    int modbusAddr = 44096 + (divisionNum - 1);

                    // Симуляция отправки команды снятия раздела с охраны (код 0)
                    Thread.Sleep(500);

                    // Симуляция проверки нового состояния
                    int newState = 0x0000;
                    if (newState == 0x0000)
                    {
                        divisionInfo.CurrentState = DivisionState.DISARMED;
                        NotifyUpdate("division_updated", new { division = divisionNum, state = "disarmed" });
                        logCallback($"Раздел {divisionNum} снят с охраны");
                        if (callback != null)
                        {
                            callback(true, $"Раздел {divisionNum} снят с охраны");
                        }
                    }
                    else
                    {
                        if (callback != null)
                        {
                            callback(false, $"Раздел {divisionNum} не изменил состояние");
                        }
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка снятия раздела с охраны: {e.Message}");
                    if (callback != null)
                    {
                        callback(false, $"Ошибка: {e.Message}");
                    }
                }
            });
        }
    }
}

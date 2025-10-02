using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BolidEmulator
{
    public enum BranchState
    {
        UNKNOWN,
        ARMED,
        DISARMED,
        ALARM,
        FAULT,
        BYPASS
    }

    public class DeviceType
    {
        public int DeviceCode { get; set; }
        public string Name { get; set; }
        public int MaxBranches { get; set; }
        public int MaxRelays { get; set; }

        public DeviceType(int deviceCode, string name, int maxBranches = 0, int maxRelays = 0)
        {
            DeviceCode = deviceCode;
            Name = name;
            MaxBranches = maxBranches;
            MaxRelays = maxRelays;
        }

        public static DeviceType FromCode(int deviceCode)
        {
            string name = BolidConstants.DEVICES.ContainsKey(deviceCode) 
                ? BolidConstants.DEVICES[deviceCode] 
                : $"Неизвестный тип {deviceCode}";

            switch (deviceCode)
            {
                case 32: // Сигнал-10
                    return new DeviceType(deviceCode, name, maxBranches: 10, maxRelays: 4);
                case 0: // С2000/С2000М
                    return new DeviceType(deviceCode, name, maxBranches: 20, maxRelays: 8);
                case 4: // С2000-4
                    return new DeviceType(deviceCode, name, maxBranches: 4, maxRelays: 4);
                case 16: // С2000-2
                    return new DeviceType(deviceCode, name, maxBranches: 2, maxRelays: 2);
                case 1: // Сигнал-20
                    return new DeviceType(deviceCode, name, maxBranches: 20, maxRelays: 0);
                case 2: // Сигнал-20П
                    return new DeviceType(deviceCode, name, maxBranches: 20, maxRelays: 5);
                case 26: // Сигнал-20М
                    return new DeviceType(deviceCode, name, maxBranches: 20, maxRelays: 0);
                case 9: // С2000-КДЛ
                    return new DeviceType(deviceCode, name, maxBranches: 127, maxRelays: 0);
                case 41: // С2000-КДЛ-2И
                    return new DeviceType(deviceCode, name, maxBranches: 127, maxRelays: 0);
                case 61: // С2000-КДЛ-Modbus
                    return new DeviceType(deviceCode, name, maxBranches: 127, maxRelays: 0);
                case 81: // С2000-КДЛ-2И исп.01
                    return new DeviceType(deviceCode, name, maxBranches: 127, maxRelays: 0);
                case 15: // С2000-КПБ
                    return new DeviceType(deviceCode, name, maxBranches: 2, maxRelays: 6);
                case 48: // МИП-12
                    return new DeviceType(deviceCode, name, maxBranches: 5, maxRelays: 0);
                case 49: // МИП-24
                    return new DeviceType(deviceCode, name, maxBranches: 5, maxRelays: 0);
                case 33:
                case 38:
                case 54:
                case 79: // РИП-12 (разные исполнения)
                    return new DeviceType(deviceCode, name, maxBranches: 5, maxRelays: 2);
                case 39:
                case 55:
                case 80: // РИП-24 (разные исполнения)
                    return new DeviceType(deviceCode, name, maxBranches: 5, maxRelays: 4);
                default:
                    return new DeviceType(deviceCode, name, maxBranches: 20, maxRelays: 8);
            }
        }

        public int GetRelayBranchMapping(int relayNum)
        {
            return MaxBranches + relayNum;
        }
    }

    public class DeviceInfo
    {
        public int Address { get; set; }
        public DeviceType DeviceType { get; set; }
        public float Version { get; set; }
        public Dictionary<int, BranchState> Branches { get; set; }
        public Dictionary<int, bool> Relays { get; set; }
        public Dictionary<int, int> AdcValues { get; set; }
        public Dictionary<int, float> Resistances { get; set; }

        public DeviceInfo(int address, DeviceType deviceType, float version)
        {
            Address = address;
            DeviceType = deviceType;
            Version = version;
            Branches = new Dictionary<int, BranchState>();
            Relays = new Dictionary<int, bool>();
            AdcValues = new Dictionary<int, int>();
            Resistances = new Dictionary<int, float>();
        }
    }

    public class DeviceManager
    {
        private BolidProtocol protocol;
        private SerialPort port;
        private Action<string> logCallback;
        private Dictionary<int, DeviceInfo> devices;
        private List<Action<int, DeviceInfo>> updateCallbacks;
        private readonly object relayLock = new object();

        public DeviceManager(BolidProtocol protocol, SerialPort port, Action<string> logCallback = null)
        {
            this.protocol = protocol;
            this.port = port;
            this.logCallback = logCallback ?? (msg => Console.WriteLine(msg));
            devices = new Dictionary<int, DeviceInfo>();
            updateCallbacks = new List<Action<int, DeviceInfo>>();
        }

        public SerialPort Port
        {
            get { return port; }
            set { port = value; }
        }

        public DeviceInfo AddDevice(int address, int deviceCode, float version)
        {
            var deviceType = DeviceType.FromCode(deviceCode);
            var deviceInfo = new DeviceInfo(address, deviceType, version);

            for (int relayNum = 1; relayNum <= deviceType.MaxRelays; relayNum++)
            {
                deviceInfo.Relays[relayNum] = false;
            }

            devices[address] = deviceInfo;
            logCallback($"Добавлено устройство: {deviceType.Name} на адресе {address}");
            return deviceInfo;
        }

        public void RegisterDevice(DeviceInfo deviceInfo)
        {
            // Инициализируем состояния реле (все выключены по умолчанию)
            for (int relayNum = 1; relayNum <= deviceInfo.DeviceType.MaxRelays; relayNum++)
            {
                if (!deviceInfo.Relays.ContainsKey(relayNum))
                {
                    deviceInfo.Relays[relayNum] = false;
                }
            }

            devices[deviceInfo.Address] = deviceInfo;
            logCallback($"Зарегистрировано устройство: {deviceInfo.DeviceType.Name} на адресе {deviceInfo.Address}");
        }

        public DeviceInfo GetDevice(int address)
        {
            return devices.ContainsKey(address) ? devices[address] : null;
        }

        public List<DeviceInfo> GetAllDevices()
        {
            return devices.Values.ToList();
        }

        public int AddUpdateCallback(Action<int, DeviceInfo> callback)
        {
            updateCallbacks.Add(callback);
            return updateCallbacks.Count - 1;
        }

        public void RemoveUpdateCallback(int callbackId)
        {
            if (callbackId >= 0 && callbackId < updateCallbacks.Count)
            {
                updateCallbacks[callbackId] = null;
            }
        }

        private void NotifyUpdate(int address, DeviceInfo deviceInfo)
        {
            foreach (var callback in updateCallbacks)
            {
                if (callback != null)
                {
                    try
                    {
                        callback(address, deviceInfo);
                    }
                    catch (Exception e)
                    {
                        logCallback($"Ошибка в callback обновления: {e.Message}");
                    }
                }
            }
        }

        public void UpdateBranchStates(int address, Action<bool, string> callback = null, 
            Func<int, int, bool> progressCallback = null)
        {
            var deviceInfo = GetDevice(address);
            if (deviceInfo == null)
            {
                callback?.Invoke(false, "Устройство не найдено");
                return;
            }

            if (port == null || !port.IsOpen)
            {
                callback?.Invoke(false, "Порт не подключен");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var updatedBranches = new Dictionary<int, BranchState>();
                    int totalBranches = deviceInfo.DeviceType.MaxBranches;
                    int polledCount = 0;

                    if (progressCallback != null)
                    {
                        if (!progressCallback(0, totalBranches))
                        {
                            logCallback("Опрос остановлен в начале");
                            return;
                        }
                    }

                    for (int branchNum = 1; branchNum <= totalBranches; branchNum++)
                    {
                        try
                        {
                            var success = protocol.RequestAdc((byte)address, (byte)branchNum, port);
                            if (!success.Item1)
                            {
                                polledCount++;
                                continue;
                            }

                            var adcResponse = protocol.GetResponse(0.3, (byte)address);
                            if (adcResponse == null || !(adcResponse is ResponseADC))
                            {
                                polledCount++;
                                continue;
                            }

                            var adcResp = (ResponseADC)adcResponse;
                            if (adcResp.Magic != 28)
                            {
                                logCallback($"Шлейф {branchNum}: ошибка АЦП (код 0x{adcResp.Magic:X2})");
                                polledCount++;
                                continue;
                            }

                            deviceInfo.AdcValues[branchNum] = adcResp.ADC;

                            int? currentStateCode = null;
                            if (deviceInfo.Branches.ContainsKey(branchNum))
                            {
                                // Получаем код состояния из последнего ответа
                                currentStateCode = GetStateCode(deviceInfo.Branches[branchNum]);
                            }

                            var adcInterpretation = protocol.InterpretAdcForDevice(
                                adcResp.ADC,
                                deviceInfo.DeviceType.DeviceCode,
                                currentStateCode,
                                branchNum
                            );

                            if (new[] { 1, 2, 11, 26, 32, 34 }.Contains(deviceInfo.DeviceType.DeviceCode))
                            {
                                float resistance = protocol.CalculateResistanceFromAdc(adcResp.ADC);
                                deviceInfo.Resistances[branchNum] = resistance;
                                logCallback($"Шлейф {branchNum}: ADC={adcResp.ADC}, {adcInterpretation}");
                            }
                            else
                            {
                                deviceInfo.Resistances[branchNum] = 0.0f;
                                logCallback($"Шлейф {branchNum}: ADC={adcResp.ADC}, {adcInterpretation}");
                            }

                            var stateSuccess = protocol.RequestBranchState((byte)address, (byte)branchNum, port);
                            if (!stateSuccess.Item1)
                            {
                                polledCount++;
                                continue;
                            }

                            var stateResponse = protocol.GetResponse(0.3, (byte)address);
                            if (stateResponse == null)
                            {
                                polledCount++;
                                continue;
                            }

                            if (stateResponse is ResponseBranchStateShort shortResp)
                            {
                                if (shortResp.Magic == 26)
                                {
                                    int stateCode = shortResp.State & 0xFF;
                                    var branchState = ParseBranchState(stateCode);
                                    updatedBranches[branchNum] = branchState;
                                    deviceInfo.Branches[branchNum] = branchState;

                                    string stateDescription = BolidConstants.BRANCH_STATES.ContainsKey(stateCode)
                                        ? BolidConstants.BRANCH_STATES[stateCode]
                                        : $"Неизвестное состояние {stateCode}";
                                    logCallback($"Шлейф {branchNum}: код {stateCode} ({stateDescription})");

                                    NotifyUpdate(address, deviceInfo);
                                }
                                else
                                {
                                    logCallback($"Шлейф {branchNum}: ошибка ответа (код 0x{shortResp.Magic:X2})");
                                    polledCount++;
                                }
                            }
                            else if (stateResponse is ResponseBranchStateLong longResp)
                            {
                                if (longResp.Magic == 26)
                                {
                                    int stateCodeLow = (int)(longResp.State & 0xFF);
                                    int stateCodeHigh = (int)((longResp.State & 0xFF00) >> 8);

                                    if (stateCodeLow != 0)
                                    {
                                        var branchState = ParseBranchState(stateCodeLow);
                                        updatedBranches[branchNum] = branchState;
                                        deviceInfo.Branches[branchNum] = branchState;

                                        string stateDescription = BolidConstants.BRANCH_STATES.ContainsKey(stateCodeLow)
                                            ? BolidConstants.BRANCH_STATES[stateCodeLow]
                                            : $"Неизвестное состояние {stateCodeLow}";
                                        logCallback($"Шлейф {branchNum}: код {stateCodeLow} ({stateDescription})");

                                        NotifyUpdate(address, deviceInfo);
                                    }

                                    if (stateCodeHigh != 0)
                                    {
                                        string stateDescriptionHigh = BolidConstants.BRANCH_STATES.ContainsKey(stateCodeHigh)
                                            ? BolidConstants.BRANCH_STATES[stateCodeHigh]
                                            : $"Неизвестное состояние {stateCodeHigh}";
                                        logCallback($"Шлейф {branchNum}: дополнительный код {stateCodeHigh} ({stateDescriptionHigh})");
                                    }
                                }
                                else
                                {
                                    logCallback($"Шлейф {branchNum}: ошибка ответа (код 0x{longResp.Magic:X2})");
                                    polledCount++;
                                }
                            }

                            Thread.Sleep(100);
                            polledCount++;

                            if (progressCallback != null)
                            {
                                if (!progressCallback(polledCount, totalBranches))
                                {
                                    logCallback($"Опрос остановлен после шлейфа {branchNum}");
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logCallback($"Ошибка обновления шлейфа {branchNum}: {e.Message}");
                            polledCount++;
                            continue;
                        }
                    }

                    deviceInfo.Branches = updatedBranches;

                    if (progressCallback != null)
                    {
                        progressCallback(polledCount, totalBranches);
                    }

                    callback?.Invoke(true, $"Обновлено {updatedBranches.Count} шлейфов из {polledCount} опрошенных");
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка обновления состояний: {e.Message}");
                    callback?.Invoke(false, $"Ошибка: {e.Message}");
                }
            });
        }

        private int? GetStateCode(BranchState state)
        {
            // Возвращаем код состояния для более точной интерпретации ADC
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

        private BranchState ParseBranchState(int stateCode)
        {
            string stateDescription = BolidConstants.BRANCH_STATES.ContainsKey(stateCode)
                ? BolidConstants.BRANCH_STATES[stateCode]
                : $"Неизвестное состояние {stateCode}";

            switch (stateCode)
            {
                case 24: return BranchState.ARMED;
                case 109: return BranchState.DISARMED;
                case 3:
                case 58: return BranchState.ALARM;
                case 45:
                case 46:
                case 47:
                case 41:
                case 42:
                case 214:
                case 215: return BranchState.FAULT;
                case 111: return BranchState.BYPASS;
                case 37:
                case 40: return BranchState.ALARM;
                case 17: return BranchState.FAULT;
                case 23: return BranchState.UNKNOWN;
                case 18: return BranchState.ALARM;
                case 28: return BranchState.UNKNOWN;
                case 187: return BranchState.FAULT;
                case 204: return BranchState.FAULT;
                default: return BranchState.UNKNOWN;
            }
        }

        public void ToggleBranch(int address, int branchNum, Action<bool, string> callback = null)
        {
            var deviceInfo = GetDevice(address);
            if (deviceInfo == null)
            {
                callback?.Invoke(false, "Устройство не найдено");
                return;
            }

            if (port == null || !port.IsOpen)
            {
                callback?.Invoke(false, "Порт не подключен");
                return;
            }

            var currentState = deviceInfo.Branches.ContainsKey(branchNum) 
                ? deviceInfo.Branches[branchNum] 
                : BranchState.UNKNOWN;

            bool newSecure;
            string actionText;

            if (currentState == BranchState.ARMED)
            {
                newSecure = false;
                actionText = "снятие";
            }
            else
            {
                newSecure = true;
                actionText = "взятие";
            }

            Task.Run(() =>
            {
                try
                {
                    logCallback($"{actionText.Capitalize()} шлейфа {branchNum} на адресе {address}");

                    var success = protocol.ManageBranch((byte)address, (byte)branchNum, newSecure, port);
                    if (!success.Item1)
                    {
                        callback?.Invoke(false, $"Ошибка отправки команды: {success.Item2}");
                        return;
                    }

                    var response = protocol.GetResponse(0.5, (byte)address);
                    if (response == null || !(response is ResponseManageBranch))
                    {
                        callback?.Invoke(false, "Нет ответа от устройства");
                        return;
                    }

                    var manageResp = (ResponseManageBranch)response;
                    if (manageResp.Magic == 20)
                    {
                        var newState = newSecure ? BranchState.ARMED : BranchState.DISARMED;
                        deviceInfo.Branches[branchNum] = newState;
                        NotifyUpdate(address, deviceInfo);

                        callback?.Invoke(true, $"Шлейф {branchNum} успешно {actionText}");
                    }
                    else
                    {
                        callback?.Invoke(false, $"Ошибка выполнения команды (код 0x{manageResp.Magic:X2})");
                    }
                }
                catch (Exception e)
                {
                    logCallback($"Ошибка переключения шлейфа: {e.Message}");
                    callback?.Invoke(false, $"Ошибка: {e.Message}");
                }
            });
        }

        public void ToggleRelay(int address, int relayNum, Action<bool, string> callback = null, int? programCode = null)
        {
            var deviceInfo = GetDevice(address);
            if (deviceInfo == null)
            {
                callback?.Invoke(false, "Устройство не найдено");
                return;
            }

            if (port == null || !port.IsOpen)
            {
                callback?.Invoke(false, "Порт не подключен");
                return;
            }

            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(relayLock, 0, ref lockTaken);
                if (!lockTaken)
                {
                    callback?.Invoke(false, "Операция с реле уже выполняется, подождите");
                    return;
                }

                int actualProgramCode;
                string actionText;

                if (programCode == null)
                {
                    bool currentState = deviceInfo.Relays.ContainsKey(relayNum) ? deviceInfo.Relays[relayNum] : false;
                    actualProgramCode = currentState ? 2 : 1;
                    actionText = currentState ? "выключение" : "включение";
                }
                else
                {
                    actualProgramCode = programCode.Value;
                    string programDescription = BolidConstants.RELAY_PROGRAMS.ContainsKey(actualProgramCode)
                        ? BolidConstants.RELAY_PROGRAMS[actualProgramCode]
                        : $"Программа {actualProgramCode}";
                    actionText = $"выполнение программы {actualProgramCode} ({programDescription})";
                }

                Task.Run(() =>
                {
                    try
                    {
                        logCallback($"{actionText.Capitalize()} реле {relayNum} на адресе {address}");

                        var success = protocol.ManageRelay((byte)address, (byte)relayNum, (byte)actualProgramCode, port);
                        if (!success.Item1)
                        {
                            callback?.Invoke(false, $"Ошибка отправки команды: {success.Item2}");
                            return;
                        }

                        var response = protocol.GetResponse(0.5, (byte)address);
                        if (response != null && response is ResponseManageRelay)
                        {
                            var relayResp = (ResponseManageRelay)response;
                            if (relayResp.Magic == 22)
                            {
                                bool programKnown = BolidConstants.RELAY_PROGRAMS.ContainsKey(actualProgramCode);
                                if (!programKnown)
                                {
                                    logCallback($"Предупреждение: неизвестная программа реле {actualProgramCode}");
                                }

                                if (actualProgramCode == 1 || actualProgramCode == 2)
                                {
                                    bool currentRelayState = deviceInfo.Relays.ContainsKey(relayNum) ? deviceInfo.Relays[relayNum] : false;
                                    deviceInfo.Relays[relayNum] = !currentRelayState;
                                    NotifyUpdate(address, deviceInfo);
                                }

                                callback?.Invoke(true, $"Реле {relayNum} успешно {actionText}");
                            }
                            else
                            {
                                callback?.Invoke(false, $"Ошибка выполнения команды (код 0x{relayResp.Magic:X2})");
                            }
                        }
                        else
                        {
                            callback?.Invoke(false, "Нет ответа от устройства");
                        }
                    }
                    catch (Exception e)
                    {
                        logCallback($"Ошибка переключения реле: {e.Message}");
                        callback?.Invoke(false, $"Ошибка: {e.Message}");
                    }
                });
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(relayLock);
                }
            }
        }

        public void UpdateRelayStates(int address, Action<bool, string> callback = null)
        {
            var deviceInfo = GetDevice(address);
            if (deviceInfo == null)
            {
                callback?.Invoke(false, "Устройство не найдено");
                return;
            }

            if (deviceInfo.DeviceType.MaxRelays == 0)
            {
                callback?.Invoke(false, "Устройство не поддерживает реле");
                return;
            }

            if (port == null || !port.IsOpen)
            {
                callback?.Invoke(false, "Порт не подключен");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    int updatedCount = 0;
                    var relayStates = new List<string>();

                    for (int relayNum = 1; relayNum <= deviceInfo.DeviceType.MaxRelays; relayNum++)
                    {
                        try
                        {
                            var success = protocol.RequestAdc((byte)address, (byte)(deviceInfo.DeviceType.GetRelayBranchMapping(relayNum)), port);
                            if (!success.Item1)
                            {
                                logCallback($"Ошибка запроса ADC для реле {relayNum}: {success.Item2}");
                                continue;
                            }

                            var adcResponse = protocol.GetResponse(0.3, (byte)address);
                            if (adcResponse == null || !(adcResponse is ResponseADC))
                            {
                                logCallback($"Реле {relayNum}: нет ответа ADC");
                                continue;
                            }

                            var adcResp = (ResponseADC)adcResponse;
                            if (adcResp.Magic != 28)
                            {
                                logCallback($"Реле {relayNum}: ошибка ADC (код 0x{adcResp.Magic:X2})");
                                continue;
                            }

                            bool relayState = protocol.InterpretRelayAdc(adcResp.ADC, deviceInfo.DeviceType.DeviceCode);
                            deviceInfo.Relays[relayNum] = relayState;

                            string stateText = relayState ? "Включено" : "Выключено";
                            relayStates.Add($"Реле {relayNum}: {stateText} (ADC: {adcResp.ADC})");
                            updatedCount++;
                        }
                        catch (Exception e)
                        {
                            logCallback($"Ошибка обновления реле {relayNum}: {e.Message}");
                            continue;
                        }
                    }

                    NotifyUpdate(address, deviceInfo);

                    string statesText = string.Join("\n", relayStates);
                    logCallback($"Обновлены состояния реле на адресе {address} ({updatedCount}/{deviceInfo.DeviceType.MaxRelays}):\n{statesText}");

                    callback?.Invoke(true, $"Обновлены состояния {updatedCount} реле");
                }
                catch (Exception e)
                {
                    string errorMsg = $"Ошибка обновления реле: {e.Message}";
                    logCallback(errorMsg);
                    callback?.Invoke(false, errorMsg);
                }
            });
        }
    }

    public static class StringExtensions
    {
        public static string Capitalize(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}

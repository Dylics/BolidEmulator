using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace BolidEmulator
{
    public enum CommandType : byte
    {
        DEVICE_TYPE_VERSION = 0x0D,
        MANAGE_BRANCH = 0x13,
        REQUEST_ADC = 0x1B,
        BRANCH_STATE = 0x19,
        MANAGE_RELAY = 0x15
    }

    public enum BranchAction : byte
    {
        DISARM = 0x00,
        ARM = 0x02
    }

    public enum RelayProgram : byte
    {
        NO_CONTROL = 0,
        TURN_ON = 1,
        TURN_OFF = 2,
        TURN_ON_TIME = 3,
        TURN_OFF_TIME = 4,
        BLINK_FROM_OFF = 5,
        BLINK_FROM_ON = 6,
        BLINK_FROM_OFF_TIME = 7,
        BLINK_FROM_ON_TIME = 8,
        LAMP = 9,
        PCN = 10,
        ASPT = 11,
        SIREN = 12,
        FIRE_PCN = 13,
        FAULT_OUTPUT = 14,
        FIRE_LAMP = 15,
        OLD_PCN_TACTICS = 16,
        TURN_ON_BEFORE_ARM = 17,
        TURN_OFF_BEFORE_ARM = 18,
        TURN_ON_AT_ARM = 19,
        TURN_OFF_AT_ARM = 20,
        TURN_ON_AT_DISARM = 21,
        TURN_OFF_AT_DISARM = 22,
        TURN_ON_AT_NO_ARM = 23,
        TURN_OFF_AT_NO_ARM = 24,
        TURN_ON_TECH_VIOLATION = 25,
        TURN_OFF_TECH_VIOLATION = 26,
        TURN_ON_AT_DISARM_2 = 27,
        TURN_OFF_AT_DISARM_2 = 28,
        TURN_ON_AT_ARM_2 = 29,
        TURN_OFF_AT_ARM_2 = 30,
        TURN_ON_TECH_VIOLATION_2 = 31,
        TURN_OFF_TECH_VIOLATION_2 = 32,
        ASPT_1 = 33,
        ASPT_A = 34,
        ASPT_A1 = 35,
        TURN_ON_TEMP_RISE = 36,
        TURN_ON_TEMP_FALL = 37,
        TURN_ON_START_DELAY = 38,
        TURN_ON_PT_START = 39,
        TURN_ON_EXTINGUISH = 40,
        TURN_ON_FAILED_START = 41,
        TURN_ON_AUTO_ON = 42,
        TURN_OFF_AUTO_ON = 43,
        TURN_ON_AUTO_OFF = 44,
        TURN_OFF_AUTO_OFF = 45,
        TURN_ON_IU_WORKING = 46,
        TURN_OFF_IU_WORKING = 47,
        TURN_ON_IU_IDLE = 48,
        TURN_OFF_IU_IDLE = 49,
        TURN_ON_FIRE2 = 50,
        TURN_OFF_FIRE2 = 51,
        BLINK_FIRE2_FROM_OFF = 52,
        BLINK_FIRE2_FROM_ON = 53,
        TURN_ON_ATTACK = 54,
        TURN_OFF_ATTACK = 55,
        LAMP_2 = 56,
        SIREN_2 = 57
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommonHeader
    {
        public byte Addr;
        public byte Len;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestDeviceTypeVersion
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Func;
        public byte Spare1;
        public byte Spare2;

        public RequestDeviceTypeVersion(byte addr)
        {
            Addr = addr;
            Len = 6;
            Magic = 0;
            Func = (byte)CommandType.DEVICE_TYPE_VERSION;
            Spare1 = 0;
            Spare2 = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestManageBranch
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Func;
        public byte Branch;
        public byte Action;

        public RequestManageBranch(byte addr, byte branch, byte action)
        {
            Addr = addr;
            Len = 6;
            Magic = 0;
            Func = (byte)CommandType.MANAGE_BRANCH;
            Branch = branch;
            Action = action;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestADC
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Func;
        public byte Branch;
        public byte Spare;

        public RequestADC(byte addr, byte branch)
        {
            Addr = addr;
            Len = 6;
            Magic = 0;
            Func = (byte)CommandType.REQUEST_ADC;
            Branch = branch;
            Spare = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestBranchState
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Func;
        public byte Branch;
        public byte Spare;

        public RequestBranchState(byte addr, byte branch)
        {
            Addr = addr;
            Len = 6;
            Magic = 0;
            Func = (byte)CommandType.BRANCH_STATE;
            Branch = branch;
            Spare = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestManageRelay
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Func;
        public byte Relay;
        public byte Program;

        public RequestManageRelay(byte addr, byte relay, byte program)
        {
            Addr = addr;
            Len = 6;
            Magic = 0;
            Func = (byte)CommandType.MANAGE_RELAY;
            Relay = relay;
            Program = program;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseDeviceTypeVersionShort
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte DeviceType;
        public byte DeviceVersion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseDeviceTypeVersionMiddle
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte DeviceType;
        public byte DeviceVersion;
        public byte Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseDeviceTypeVersionLong
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte DeviceType;
        public byte DeviceVersion;
        public byte Spare1;
        public byte Spare2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseManageBranch
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Branch;
        public byte Action;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseADC
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Branch;
        public byte ADC;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseBranchStateShort
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Branch;
        public byte State;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseBranchStateLong
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Branch;
        public ushort State;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResponseManageRelay
    {
        public byte Addr;
        public byte Len;
        public byte Magic;
        public byte Relay;
        public byte Program;
    }

    public static class BolidConstants
    {
        public static readonly byte[] CRC8_TABLE = {
            0, 94, 188, 226, 97, 63, 221, 131, 194, 156,
            126, 32, 163, 253, 31, 65, 157, 195, 33, 127,
            252, 162, 64, 30, 95, 1, 227, 189, 62, 96,
            130, 220, 35, 125, 159, 193, 66, 28, 254, 160,
            225, 191, 93, 3, 128, 222, 60, 98, 190, 224,
            2, 92, 223, 129, 99, 61, 124, 34, 192, 158,
            29, 67, 161, 255, 70, 24, 250, 164, 39, 121,
            155, 197, 132, 218, 56, 102, 229, 187, 89, 7,
            219, 133, 103, 57, 186, 228, 6, 88, 25, 71,
            165, 251, 120, 38, 196, 154, 101, 59, 217, 135,
            4, 90, 184, 230, 167, 249, 27, 69, 198, 152,
            122, 36, 248, 166, 68, 26, 153, 199, 37, 123,
            58, 100, 134, 216, 91, 5, 231, 185, 140, 210,
            48, 110, 237, 179, 81, 15, 78, 16, 242, 172,
            47, 113, 147, 205, 17, 79, 173, 243, 112, 46,
            204, 146, 211, 141, 111, 49, 178, 236, 14, 80,
            175, 241, 19, 77, 206, 144, 114, 44, 109, 51,
            209, 143, 12, 82, 176, 238, 50, 108, 142, 208,
            83, 13, 239, 177, 240, 174, 76, 18, 145, 207,
            45, 115, 202, 148, 118, 40, 171, 245, 23, 73,
            8, 86, 180, 234, 105, 55, 213, 139, 87, 9,
            235, 181, 54, 104, 138, 212, 149, 203, 41, 119,
            244, 170, 72, 22, 233, 183, 85, 11, 136, 214,
            52, 106, 43, 117, 151, 201, 74, 20, 246, 168,
            116, 42, 200, 150, 21, 75, 169, 247, 182, 252,
            10, 84, 215, 137, 107, 53
        };

        public static readonly Dictionary<int, string> DEVICES = new Dictionary<int, string>
        {
            {0, "С2000/С2000М"},
            {1, "Сигнал-20"},
            {2, "Сигнал-20П"},
            {3, "С2000-СП1"},
            {4, "С2000-4"},
            {7, "С2000-К"},
            {8, "С2000-ИТ"},
            {9, "С2000-КДЛ"},
            {10, "С2000-БИ/БКИ"},
            {11, "Сигнал-20(вер. 02)"},
            {13, "С2000-КС"},
            {14, "С2000-АСПТ"},
            {15, "С2000-КПБ"},
            {16, "С2000-2"},
            {19, "УО-ОРИОН"},
            {20, "Рупор"},
            {21, "Рупор-Диспетчер исп.01"},
            {22, "С2000-ПТ"},
            {24, "УО-4С"},
            {25, "Поток-3Н"},
            {26, "Сигнал-20М"},
            {28, "С2000-БИ-01"},
            {30, "Рупор исп.01"},
            {31, "С2000-Adem"},
            {32, "Сигнал-10"},
            {33, "РИП-12 исп.50, исп.51, без исполнения"},
            {34, "Сигнал-10"},
            {36, "С2000-ПП"},
            {38, "РИП-12 исп.54"},
            {39, "РИП-24 исп.50, исп.51"},
            {41, "С2000-КДЛ-2И"},
            {43, "С2000-PGE"},
            {44, "С2000-БКИ"},
            {45, "Поток-БКИ"},
            {46, "Рупор-200"},
            {47, "С2000-Периметр"},
            {48, "МИП-12"},
            {49, "МИП-24"},
            {53, "РИП-48 исп.01"},
            {54, "РИП-12 исп.56"},
            {55, "РИП-24 исп.56"},
            {59, "Рупор исп.02"},
            {61, "С2000-КДЛ-Modbus"},
            {66, "Рупор исп.03"},
            {67, "Рупор-300"},
            {76, "С2000-PGE исп.01"},
            {79, "ПКВ-РИП-12 исп.56"},
            {80, "ПКВ-РИП-24 исп.56"},
            {81, "С2000-КДЛ-2И исп.01"},
            {82, "ШКП-RS"}
        };

        public static readonly Dictionary<int, string> BRANCH_ACTIONS = new Dictionary<int, string>
        {
            {0, "Снятие"},
            {2, "Взятие"}
        };

        public static readonly Dictionary<int, string> RELAY_PROGRAMS = new Dictionary<int, string>
        {
            {0, "Не управлять"},
            {1, "Включить"},
            {2, "Выключить"},
            {3, "Включить на время"},
            {4, "Выключить на время"},
            {5, "Мигать из состояния выключено"},
            {6, "Мигать из состояния включено"},
            {7, "Мигать из состояния выключено на время"},
            {8, "Мигать из состояния включено на время"},
            {9, "Лампа"},
            {10, "ПЦН"},
            {11, "АСПТ"},
            {12, "Сирена"},
            {13, "Пожарный ПЦН"},
            {14, "Выход неисправность"},
            {15, "Пожарная лампа"},
            {16, "Старая тактика ПЦН"},
            {17, "Включить на время перед взятием"},
            {18, "Выключить на время перед взятием"},
            {19, "Включить на время при взятии"},
            {20, "Выключить на время при взятии"},
            {21, "Включить на время при снятии"},
            {22, "Выключить на время при снятии"},
            {23, "Включить на время при невзятии"},
            {24, "Выключить на время при невзятии"},
            {25, "Включить на время при нарушении технологического ШС"},
            {26, "Выключить на время при нарушении технологического ШС"},
            {27, "Включить при снятии"},
            {28, "Выключить при снятии (выход взят-снят)"},
            {29, "Включить при взятии"},
            {30, "Выключить при взятии"},
            {31, "Включить при нарушении технологического ШС"},
            {32, "Выключить при нарушении технологического ШС"},
            {33, "АСПТ-1"},
            {34, "АСПТ-А"},
            {35, "АСПТ-А1"},
            {36, "Включить при повышении температуры"},
            {37, "Включить при понижении температуры"},
            {38, "Включить при задержке пуска"},
            {39, "Включить при пуске ПТ"},
            {40, "Включить при тушении"},
            {41, "Включить при неудачном пуске"},
            {42, "Включить при включении автоматики"},
            {43, "Выключить при включении автоматики"},
            {44, "Включить при выключении автоматики"},
            {45, "Выключить при выключении автоматики"},
            {46, "Включить если ИУ в рабочем состоянии"},
            {47, "Выключить если ИУ в рабочем состоянии"},
            {48, "Включить если ИУ в исходном состоянии"},
            {49, "Выключить если ИУ в исходном состоянии"},
            {50, "Включить при Пожар2"},
            {51, "Выключить при Пожар2"},
            {52, "Мигать при Пожар2 из состояния выключено"},
            {53, "Мигать при Пожар2 из состояния включено"},
            {54, "Включить при нападении"},
            {55, "Выключить при нападении"},
            {56, "Лампа 2"},
            {57, "Сирена 2"}
        };

        public static readonly Dictionary<int, string> BRANCH_STATES = new Dictionary<int, string>
        {
            {1, "Восстановление сети 220 В"},
            {2, "Авария сети 220 В"},
            {3, "Тревога проникновения"},
            {4, "Помеха"},
            {6, "Помеха устранена"},
            {7, "Ручное включение"},
            {8, "Ручное выключение"},
            {9, "Активация УДП"},
            {10, "Восстановление УДП"},
            {14, "Подбор кода"},
            {15, "Дверь открыта"},
            {17, "Неудачное взятие"},
            {18, "Предъявлен код принуждения"},
            {19, "Тест (код 19)"},
            {20, "Вход в режим тестирования"},
            {21, "Выход из режима тестирования"},
            {22, "Восстановление контроля"},
            {23, "Задержка взятия"},
            {24, "Взят под охрану"},
            {25, "Доступ закрыт"},
            {26, "Доступ отклонен"},
            {27, "Дверь взломана"},
            {28, "Доступ предоставлен"},
            {29, "Запрет доступа"},
            {30, "Восстановление доступа"},
            {31, "Дверь закрыта"},
            {32, "Проход"},
            {33, "Дверь заблокирована"},
            {34, "Идентификация"},
            {35, "Восстановление технологического входа"},
            {36, "Нарушение технологического входа"},
            {37, "Пожар"},
            {38, "Нарушение 2-го технологического входа"},
            {39, "Восстановление нормы оборудования"},
            {40, "Пожар 2"},
            {41, "Неисправность оборудования"},
            {42, "Неизвестное устройство"},
            {44, "Внимание!"},
            {45, "Обрыв входа"},
            {46, "Обрыв ДПЛС"},
            {47, "Восстановление ДПЛС"},
            {58, "Тихая тревога"},
            {71, "Понижение уровня"},
            {72, "Норма уровня"},
            {74, "Повышение уровня"},
            {75, "Аварийное повышение уровня"},
            {76, "Повышение температуры"},
            {77, "Аварийное понижение уровня"},
            {78, "Температура в норме"},
            {79, "Тревога затопления"},
            {80, "Восстановление датчика затопления"},
            {82, "Неисправность термометра"},
            {83, "Восстановление термометра"},
            {84, "Начало локального программирования"},
            {109, "Снят с охраны"},
            {111, "Включение ШС"},
            {112, "Отключение ШС"},
            {113, "Включение выхода"},
            {114, "Отключение выхода"},
            {117, "Восстановление снятого входа"},
            {118, "Тревога входа"},
            {119, "Нарушение снятого входа"},
            {121, "Обрыв выхода"},
            {122, "КЗ выхода"},
            {123, "Восстановление выхода"},
            {126, "Потеря связи с выходом"},
            {127, "Восстановление связи с выходом"},
            {128, "Изменение состояния выхода"},
            {130, "Включение насоса"},
            {131, "Выключение насоса"},
            {135, "Ошибка при автоматическом тестировании"},
            {137, "Пуск"},
            {138, "Неудачный пуск"},
            {139, "Неудачный пуск пожаротушения"},
            {140, "Тест (код 140)"},
            {141, "Задержка пуска АУП"},
            {142, "Автоматика АУП выключена"},
            {143, "Отмена пуска АУП"},
            {144, "Тушение"},
            {145, "Аварийный пуск АУП"},
            {146, "Пуск АУП"},
            {147, "Блокировка пуска АУП"},
            {148, "Автоматика АУП включена"},
            {149, "Взлом корпуса прибора"},
            {150, "Пуск речевого оповещения"},
            {151, "Отмена пуска речевого оповещения"},
            {152, "Восстановление корпуса прибора"},
            {153, "ИУ в рабочем состоянии"},
            {154, "ИУ в исходном состоянии"},
            {155, "Отказ ИУ"},
            {156, "Ошибка ИУ"},
            {158, "Восстановление внутренней зоны"},
            {159, "Задержка пуска речевого оповещения"},
            {161, "Останов задержки пуска АУП"},
            {165, "Ошибка параметров входа"},
            {187, "Неизвестное состояние адресного устройства"},
            {188, "Восстановление связи со входом"},
            {189, "Потеря связи по ДПЛС1"},
            {190, "Потеря связи по ДПЛС2"},
            {191, "Восстановление связи по ДПЛС1"},
            {192, "Отключение выходного напряжения"},
            {193, "Подключение выходного напряжения"},
            {194, "Перегрузка источника питания"},
            {195, "Перегрузка источника питания устранена"},
            {196, "Неисправность зарядного устройства"},
            {197, "Восстановление зарядного устройства"},
            {198, "Неисправность источника питания"},
            {199, "Восстановление источника питания"},
            {200, "Восстановление батареи"},
            {201, "Восстановление связи по ДПЛС2"},
            {202, "Неисправность батареи"},
            {203, "Перезапуск прибора"},
            {204, "Требуется обслуживание"},
            {205, "Ошибка теста АКБ"},
            {206, "Понижение температуры"},
            {211, "Батарея разряжена"},
            {212, "Разряд резервной батареи"},
            {213, "Восстановление резервной батареи"},
            {214, "КЗ входа"},
            {215, "Короткое замыкание ДПЛС"},
            {216, "Сработка датчика"},
            {217, "Отключение ветви RS-485"},
            {218, "Восстановление ветви RS-485"},
            {220, "Срабатывание СДУ"},
            {221, "Отказ СДУ"},
            {222, "Повышение напряжения ДПЛС"},
            {223, "Отметка наряда"},
            {237, "Раздел снят по принуждению"},
            {241, "Раздел взят"},
            {242, "Раздел снят"},
            {250, "Потеряна связь с прибором"},
            {251, "Восстановлена связь с прибором"},
            {253, "Включение пульта С2000М"}
        };
    }

    public class BolidProtocol
    {
        private bool _debug = false;
        private bool _fastMode = false;
        private Type _lastRequestType;
        private byte _lastRequestAddr = 0;
        private byte[] _buffer = new byte[270];
        private int _bufferLen = 0;
        private List<object> _responseQueue = new List<object>();
        private object _queueLock = new object();
        private SerialPort _port;
        private bool _monitoring = false;
        private Thread _monitorThread;
        private bool _stopMonitoringFlag = false;

        private byte Crc8(byte[] data)
        {
            byte crc = 0;
            foreach (byte b in data)
            {
                crc = BolidConstants.CRC8_TABLE[crc ^ b];
            }
            return crc;
        }

        public Tuple<bool, string> IsPortResponsive(SerialPort port, byte testAddr = 1, double timeout = 1.0)
        {
            if (port == null || !port.IsOpen)
            {
                return Tuple.Create(false, "Порт не открыт");
            }

            try
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();

                var request = new RequestDeviceTypeVersion(testAddr);
                byte[] data = PackRequest(request);

                if (_fastMode)
                {
                    port.WriteTimeout = 300;
                }
                else
                {
                    port.WriteTimeout = 1000;
                }

                port.Write(data, 0, data.Length);

                DateTime startTime = DateTime.Now;
                bool responseReceived = false;

                while ((DateTime.Now - startTime).TotalSeconds < timeout)
                {
                    if (port.BytesToRead > 0)
                    {
                        byte[] responseData = new byte[port.BytesToRead];
                        port.Read(responseData, 0, responseData.Length);
                        if (responseData.Length > 0)
                        {
                            responseReceived = true;
                            break;
                        }
                    }
                    Thread.Sleep(10);
                }

                if (responseReceived)
                {
                    return Tuple.Create(true, "Порт отвечает на запросы");
                }
                else
                {
                    return Tuple.Create(false, $"Порт не отвечает в течение {timeout} сек");
                }
            }
            catch (TimeoutException)
            {
                return Tuple.Create(false, "Таймаут записи в порт");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, $"Ошибка порта: {e.Message}");
            }
        }

        private byte[] StructureToByteArray(object obj)
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        private byte[] PackRequest(object request)
        {
            byte[] data = StructureToByteArray(request);
            byte crc = Crc8(data);
            byte[] result = new byte[data.Length + 1];
            Array.Copy(data, result, data.Length);
            result[data.Length] = crc;
            return result;
        }

        private T ByteArrayToStructure<T>(byte[] data) where T : struct
        {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);
            T structure = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return structure;
        }

        private object UnpackResponse(byte[] data, Type responseClass)
        {
            if (data.Length < 2)
                return null;

            byte[] payload = new byte[data.Length - 1];
            Array.Copy(data, payload, payload.Length);
            byte crc = data[data.Length - 1];

            if (Crc8(payload) != crc)
                return null;

            if (responseClass == typeof(ResponseDeviceTypeVersionShort))
            {
                if (payload.Length >= 5)
                    return ByteArrayToStructure<ResponseDeviceTypeVersionShort>(payload);
            }
            else if (responseClass == typeof(ResponseDeviceTypeVersionMiddle))
            {
                if (payload.Length >= 6)
                    return ByteArrayToStructure<ResponseDeviceTypeVersionMiddle>(payload);
            }
            else if (responseClass == typeof(ResponseDeviceTypeVersionLong))
            {
                if (payload.Length >= 7)
                    return ByteArrayToStructure<ResponseDeviceTypeVersionLong>(payload);
            }
            else if (responseClass == typeof(ResponseManageBranch))
            {
                if (payload.Length >= 5)
                    return ByteArrayToStructure<ResponseManageBranch>(payload);
            }
            else if (responseClass == typeof(ResponseADC))
            {
                if (payload.Length >= 5)
                    return ByteArrayToStructure<ResponseADC>(payload);
            }
            else if (responseClass == typeof(ResponseBranchStateShort))
            {
                if (payload.Length >= 5)
                    return ByteArrayToStructure<ResponseBranchStateShort>(payload);
            }
            else if (responseClass == typeof(ResponseBranchStateLong))
            {
                if (payload.Length >= 6)
                    return ByteArrayToStructure<ResponseBranchStateLong>(payload);
            }
            else if (responseClass == typeof(ResponseManageRelay))
            {
                if (payload.Length >= 5)
                    return ByteArrayToStructure<ResponseManageRelay>(payload);
            }

            return null;
        }

        private bool SendRequest(object request, SerialPort port)
        {
            try
            {
                if (port == null || !port.IsOpen)
                    return false;

                byte[] data = PackRequest(request);
                int originalWriteTimeout = port.WriteTimeout;

                if (_fastMode)
                {
                    port.WriteTimeout = 300;
                }
                else
                {
                    port.WriteTimeout = 1000;
                }

                try
                {
                    port.Write(data, 0, data.Length);

                    var addrProperty = request.GetType().GetField("Addr");
                    if (addrProperty != null)
                    {
                        _lastRequestAddr = (byte)addrProperty.GetValue(request);
                    }
                    _lastRequestType = request.GetType();

                    return true;
                }
                finally
                {
                    port.WriteTimeout = originalWriteTimeout;
                }
            }
            catch
            {
                return false;
            }
        }

        private void DataReceived(byte[] data)
        {
            if (data.Length == 0)
                return;

            if (_bufferLen + data.Length > _buffer.Length)
            {
                _bufferLen = 0;
            }

            Array.Copy(data, 0, _buffer, _bufferLen, data.Length);
            _bufferLen += data.Length;

            while (_bufferLen >= 2)
            {
                byte addr = _buffer[0];
                byte length = _buffer[1];

                if (length < 2 || length > 255)
                {
                    Array.Copy(_buffer, 1, _buffer, 0, _bufferLen - 1);
                    _bufferLen--;
                    continue;
                }

                if (_bufferLen < length + 1)
                    break;

                if (addr != _lastRequestAddr)
                {
                    Array.Copy(_buffer, 1, _buffer, 0, _bufferLen - 1);
                    _bufferLen--;
                    continue;
                }

                byte[] packet = new byte[length + 1];
                Array.Copy(_buffer, packet, packet.Length);

                object response = null;
                if (_lastRequestType == typeof(RequestDeviceTypeVersion))
                {
                    if (length == 5)
                        response = UnpackResponse(packet, typeof(ResponseDeviceTypeVersionShort));
                    else if (length == 6)
                        response = UnpackResponse(packet, typeof(ResponseDeviceTypeVersionMiddle));
                    else if (length == 7)
                        response = UnpackResponse(packet, typeof(ResponseDeviceTypeVersionLong));
                }
                else if (_lastRequestType == typeof(RequestManageBranch))
                {
                    if (length == 5)
                        response = UnpackResponse(packet, typeof(ResponseManageBranch));
                }
                else if (_lastRequestType == typeof(RequestADC))
                {
                    if (length == 5)
                        response = UnpackResponse(packet, typeof(ResponseADC));
                }
                else if (_lastRequestType == typeof(RequestBranchState))
                {
                    if (length == 5)
                        response = UnpackResponse(packet, typeof(ResponseBranchStateShort));
                    else if (length == 6)
                        response = UnpackResponse(packet, typeof(ResponseBranchStateLong));
                }
                else if (_lastRequestType == typeof(RequestManageRelay))
                {
                    if (length == 5)
                        response = UnpackResponse(packet, typeof(ResponseManageRelay));
                }

                if (response != null)
                {
                    lock (_queueLock)
                    {
                        _responseQueue.Add(response);
                    }
                }

                Array.Copy(_buffer, length + 1, _buffer, 0, _bufferLen - length - 1);
                _bufferLen -= length + 1;
            }
        }

        public Tuple<bool, string> RequestDeviceTypeVersion(byte addr, SerialPort port)
        {
            if (addr < 1 || addr > 255)
                return Tuple.Create(false, "Некорректный адрес устройства");

            StartMonitoring(port);

            var request = new RequestDeviceTypeVersion(addr);
            return Tuple.Create(SendRequest(request, port), "");
        }

        public Tuple<bool, string> ManageBranch(byte addr, byte branch, bool secure, SerialPort port)
        {
            if (addr < 1 || addr > 255)
                return Tuple.Create(false, "Некорректный адрес устройства");
            if (branch < 1 || branch > 255)
                return Tuple.Create(false, "Некорректный номер шлейфа");

            byte action = secure ? (byte)BranchAction.ARM : (byte)BranchAction.DISARM;
            var request = new RequestManageBranch(addr, branch, action);
            return Tuple.Create(SendRequest(request, port), "");
        }

        public Tuple<bool, string> RequestAdc(byte addr, byte branch, SerialPort port)
        {
            if (addr < 1 || addr > 255)
                return Tuple.Create(false, "Некорректный адрес устройства");
            if (branch < 1 || branch > 255)
                return Tuple.Create(false, "Некорректный номер шлейфа");

            var request = new RequestADC(addr, branch);
            return Tuple.Create(SendRequest(request, port), "");
        }

        public Tuple<bool, string> RequestBranchState(byte addr, byte branch, SerialPort port)
        {
            if (addr < 1 || addr > 255)
                return Tuple.Create(false, "Некорректный адрес устройства");
            if (branch < 1 || branch > 255)
                return Tuple.Create(false, "Некорректный номер шлейфа");

            var request = new RequestBranchState(addr, branch);
            return Tuple.Create(SendRequest(request, port), "");
        }

        public Tuple<bool, string> ManageRelay(byte addr, byte relay, byte program, SerialPort port)
        {
            if (addr < 1 || addr > 255)
                return Tuple.Create(false, "Некорректный адрес устройства");
            if (relay < 1 || relay > 255)
                return Tuple.Create(false, "Некорректный номер реле");
            if (program > 255)
                return Tuple.Create(false, "Некорректная программа управления");

            var request = new RequestManageRelay(addr, relay, program);
            return Tuple.Create(SendRequest(request, port), "");
        }

        public object GetResponse(double timeout = 1.0, byte? expectedAddr = null)
        {
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeout)
            {
                lock (_queueLock)
                {
                    if (_responseQueue.Count > 0)
                    {
                        if (expectedAddr.HasValue)
                        {
                            for (int i = 0; i < _responseQueue.Count; i++)
                            {
                                var response = _responseQueue[i];
                                var addrProperty = response.GetType().GetField("Addr");
                                if (addrProperty != null)
                                {
                                    byte addr = (byte)addrProperty.GetValue(response);
                                    if (addr == expectedAddr.Value)
                                    {
                                        _responseQueue.RemoveAt(i);
                                        return response;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var response = _responseQueue[0];
                            _responseQueue.RemoveAt(0);
                            return response;
                        }
                    }
                }
                Thread.Sleep(10);
            }
            return null;
        }

        public void ClearResponses()
        {
            lock (_queueLock)
            {
                _responseQueue.Clear();
            }
        }

        public void SetDebug(bool debug)
        {
            _debug = debug;
        }

        public void SetFastMode(bool fastMode)
        {
            _fastMode = fastMode;
        }

        public void StartMonitoring(SerialPort port)
        {
            if (_monitoring)
                StopMonitoring();

            _port = port;
            _monitoring = true;
            _stopMonitoringFlag = false;
            _monitorThread = new Thread(MonitorPort);
            _monitorThread.IsBackground = true;
            _monitorThread.Start();
        }

        public void StopMonitoring()
        {
            try
            {
                _stopMonitoringFlag = true;
                _monitoring = false;

                if (_monitorThread != null && _monitorThread.IsAlive)
                {
                    try
                    {
                        _monitorThread.Join(1000);
                    }
                    catch { }
                }

                _monitorThread = null;
                _port = null;

                try
                {
                    lock (_queueLock)
                    {
                        _responseQueue.Clear();
                        _bufferLen = 0;
                    }
                }
                catch { }
            }
            catch
            {
                _monitoring = false;
                _stopMonitoringFlag = true;
                _monitorThread = null;
                _port = null;
            }
        }

        private void MonitorPort()
        {
            while (_monitoring && !_stopMonitoringFlag && _port != null && _port.IsOpen)
            {
                try
                {
                    if (_stopMonitoringFlag)
                        break;

                    if (_port.BytesToRead > 0)
                    {
                        try
                        {
                            byte[] data = new byte[_port.BytesToRead];
                            _port.Read(data, 0, data.Length);
                            if (data.Length > 0)
                            {
                                DataReceived(data);
                            }
                        }
                        catch (TimeoutException)
                        {
                        }
                        catch
                        {
                            break;
                        }
                    }

                    Thread.Sleep(10);
                }
                catch
                {
                    break;
                }
            }
        }

        public delegate bool ProgressCallback(byte addr, int foundCount, int total);
        public delegate void DeviceFoundCallback(byte addr, string deviceName, float deviceVersion);

        public List<Tuple<byte, string, float>> ScanDevices(SerialPort port, byte startAddr = 1, byte endAddr = 255, 
            double timeout = 0.3, ProgressCallback progressCallback = null, DeviceFoundCallback deviceFoundCallback = null)
        {
            var foundDevices = new List<Tuple<byte, string, float>>();
            int totalAddresses = endAddr - startAddr + 1;

            if (port == null || !port.IsOpen)
                return foundDevices;

            if (!_fastMode)
            {
                var portCheck = IsPortResponsive(port, startAddr, timeout);
                if (!portCheck.Item1)
                    return foundDevices;
            }

            for (byte addr = startAddr; addr <= endAddr; addr++)
            {
                if (progressCallback != null)
                {
                    if (progressCallback(addr, foundDevices.Count, totalAddresses) == false)
                        break;
                }

                if (!port.IsOpen)
                    break;

                try
                {
                    var success = RequestDeviceTypeVersion(addr, port);
                    if (!success.Item1)
                        continue;

                    var response = GetResponse(timeout);
                    if (response != null)
                    {
                        byte deviceType = 0;
                        byte deviceVersion = 0;
                        byte magic = 0;

                        if (response is ResponseDeviceTypeVersionShort shortResp)
                        {
                            magic = shortResp.Magic;
                            deviceType = shortResp.DeviceType;
                            deviceVersion = shortResp.DeviceVersion;
                        }
                        else if (response is ResponseDeviceTypeVersionMiddle midResp)
                        {
                            magic = midResp.Magic;
                            deviceType = midResp.DeviceType;
                            deviceVersion = midResp.DeviceVersion;
                        }
                        else if (response is ResponseDeviceTypeVersionLong longResp)
                        {
                            magic = longResp.Magic;
                            deviceType = longResp.DeviceType;
                            deviceVersion = longResp.DeviceVersion;
                        }

                        if (magic == 0)
                        {
                            float version = deviceVersion / 100.0f;
                            string deviceName = BolidConstants.DEVICES.ContainsKey(deviceType) 
                                ? BolidConstants.DEVICES[deviceType] 
                                : $"Неизвестный тип {deviceType}";

                            foundDevices.Add(Tuple.Create(addr, deviceName, version));

                            deviceFoundCallback?.Invoke(addr, deviceName, version);
                        }
                    }

                    Thread.Sleep(100);
                }
                catch
                {
                    continue;
                }
            }

            return foundDevices;
        }

        public float CalculateResistanceFromAdc(int adcValue)
        {
            if (adcValue <= 0)
                return float.PositiveInfinity;

            float baseResistance = 238.0f / adcValue - 0.8f;

            float correction;
            if (adcValue >= 80)
            {
                correction = -0.185f + (adcValue - 80) * 0.077f;
            }
            else if (adcValue >= 40)
            {
                correction = -0.31f + (adcValue - 40) * 0.003125f;
            }
            else
            {
                correction = -0.31f - (40 - adcValue) * 0.01f;
            }

            float resistance = baseResistance + correction;
            return (float)Math.Round(resistance, 2);
        }

        public string DetectSensorType(int adcValue, int stateCode, int branchNum)
        {
            if (adcValue >= 30 && adcValue <= 120)
            {
                if (stateCode == 37 || stateCode == 40)
                    return "Дымовой датчик (ДИП-34А)";
                else if (stateCode == 204 || stateCode == 187)
                    return "Дымовой датчик (ДИП-34А) - требует обслуживания";
                else
                    return "Дымовой датчик (ДИП-34А)";
            }
            else if (adcValue < 30)
            {
                if (stateCode == 37 || stateCode == 40)
                    return "Температурный датчик (С2000-ИП)";
                else if (stateCode == 206)
                    return "Температурный датчик (С2000-ИП)";
                else
                    return "Температурный датчик (С2000-ИП)";
            }
            else if (adcValue > 120)
            {
                return "Датчик влажности (С2000-ВТ)";
            }
            else if (adcValue >= 50 && adcValue <= 100)
            {
                if (stateCode == 3 || stateCode == 24 || stateCode == 109)
                    return "Охранный датчик";
                else
                    return "Охранный датчик";
            }
            else
            {
                return "Адресный датчик (тип неизвестен)";
            }
        }

        public string InterpretAdcForDevice(int adcValue, int deviceCode, int? stateCode = null, int? branchNum = null)
        {
            if (adcValue <= 0)
                return "Обрыв";

            int[] analogDevices = { 1, 2, 11, 15, 26, 32, 34 };
            if (analogDevices.Contains(deviceCode))
            {
                float resistance = CalculateResistanceFromAdc(adcValue);
                if (float.IsPositiveInfinity(resistance))
                    return "Обрыв";
                return $"Сопротивление: {resistance} кОм";
            }

            int[] addressableDevices = { 9, 41, 61, 81 };
            if (addressableDevices.Contains(deviceCode))
            {
                if (stateCode.HasValue && branchNum.HasValue)
                {
                    string sensorType = DetectSensorType(adcValue, stateCode.Value, branchNum.Value);

                    if (sensorType.Contains("Дымовой"))
                    {
                        if (adcValue < 50)
                            return $"Задымленность: низкая (ADC: {adcValue})";
                        else if (adcValue < 100)
                            return $"Задымленность: средняя (ADC: {adcValue})";
                        else
                            return $"Задымленность: высокая (ADC: {adcValue})";
                    }
                    else if (sensorType.Contains("Температурный"))
                    {
                        return $"Температура: {adcValue:F1}°C (ADC: {adcValue})";
                    }
                    else if (sensorType.Contains("Влажность"))
                    {
                        return $"Влажность: {adcValue:F1}% (ADC: {adcValue})";
                    }
                    else if (sensorType.Contains("Охранный"))
                    {
                        return $"Состояние: норма (ADC: {adcValue})";
                    }
                    else
                    {
                        return $"{sensorType} (ADC: {adcValue})";
                    }
                }
                else
                {
                    if (adcValue < 50)
                        return $"Низкий уровень (ADC: {adcValue})";
                    else if (adcValue < 100)
                        return $"Средний уровень (ADC: {adcValue})";
                    else if (adcValue < 150)
                        return $"Высокий уровень (ADC: {adcValue})";
                    else
                        return $"Очень высокий уровень (ADC: {adcValue})";
                }
            }

            int[] powerDevices = { 33, 38, 39, 48, 49, 54, 55, 79, 80 };
            if (powerDevices.Contains(deviceCode))
            {
                return InterpretPowerDeviceAdc(adcValue, branchNum ?? 0);
            }

            return $"ADC код: {adcValue}";
        }

        public string InterpretPowerDeviceAdc(int adcValue, int branchNum)
        {
            if (adcValue <= 0)
                return "Нет данных";

            if (branchNum == 1)
            {
                float voltage = adcValue * 0.125f;
                string status = voltage < 20 ? "Низкое" : voltage > 30 ? "Высокое" : "Норма";
                return $"Выходное напряжение: {voltage:F2} В ({status})";
            }
            else if (branchNum == 2)
            {
                float current = adcValue * 0.035f;
                string status = current < 0.1f ? "Низкий" : current > 2.0f ? "Высокий" : "Норма";
                return $"Ток нагрузки: {current:F2} А ({status})";
            }
            else if (branchNum == 3)
            {
                if (adcValue == 0)
                    return "АКБ не подключена";
                float voltage = adcValue * 0.125f;
                string status = voltage < 10 ? "Разряжена" : voltage > 14 ? "Перезаряд" : "Норма";
                return $"Напряжение АКБ: {voltage:F2} В ({status})";
            }
            else if (branchNum == 4)
            {
                if (adcValue >= 200)
                    return "ЗУ: норма";
                else if (adcValue >= 100)
                    return "ЗУ: предупреждение";
                else if (adcValue >= 50)
                    return "ЗУ: неисправность";
                else
                    return "ЗУ: отключено";
            }
            else if (branchNum == 5)
            {
                float voltage = adcValue * 2.0f;
                string status = voltage < 180 ? "Низкое" : voltage > 250 ? "Высокое" : "Норма";
                return $"Сетевое напряжение: {voltage:F1} В ({status})";
            }
            else
            {
                return $"Неизвестный параметр (ADC: {adcValue})";
            }
        }

        public string GetResistanceStatus(float resistance)
        {
            if (resistance < 0.1f)
                return "Короткое замыкание";
            else if (resistance < 1.8f)
                return "Нарушение (низкое сопротивление)";
            else if (resistance >= 2.2f && resistance <= 5.4f)
                return "Норма";
            else if (resistance <= 6.6f)
                return "Нарушение (высокое сопротивление)";
            else if (resistance <= 25)
                return "Нарушение (высокое сопротивление)";
            else
                return "Обрыв";
        }

        public bool InterpretRelayAdc(int adcValue, int? deviceCode = null)
        {
            if (deviceCode == 15)
            {
                return adcValue != 0;
            }
            else
            {
                if (adcValue == 0)
                    return false;
                else if (adcValue == 37)
                    return true;
                else
                    return adcValue > 0;
            }
        }
    }
}




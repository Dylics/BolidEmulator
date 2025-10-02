using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BolidEmulator
{
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
        CRITICAL
    }

    public enum LogCategory
    {
        PROTOCOL,
        DEVICE,
        COMMUNICATION,
        USER_ACTION,
        SYSTEM,
        ERROR
    }

    public class LogEntry
    {
        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public int? DeviceAddr { get; set; }
        public string Command { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class AdvancedLogger
    {
        private readonly string logDir;
        private readonly int maxFileSize;
        private readonly int backupCount;
        private readonly bool enableConsole;

        private readonly object statsLock = new object();
        private readonly object cacheLock = new object();

        private readonly Dictionary<string, int> stats = new Dictionary<string, int>
        {
            { "total_logs", 0 },
            { "DEBUG", 0 },
            { "INFO", 0 },
            { "WARNING", 0 },
            { "ERROR", 0 },
            { "CRITICAL", 0 },
            { "PROTOCOL", 0 },
            { "DEVICE", 0 },
            { "COMMUNICATION", 0 },
            { "USER_ACTION", 0 },
            { "SYSTEM", 0 },
            { "ERROR", 0 }
        };

        private readonly DateTime startTime = DateTime.Now;
        private readonly List<LogEntry> logCache = new List<LogEntry>();
        private readonly int cacheSize = 1000;

        public AdvancedLogger(string logDir = "logs", int maxFileSize = 10 * 1024 * 1024, // 10MB
                             int backupCount = 5, bool enableConsole = true)
        {
            this.logDir = logDir;
            this.maxFileSize = maxFileSize;
            this.backupCount = backupCount;
            this.enableConsole = enableConsole;

            // Создаем директорию для логов
            Directory.CreateDirectory(logDir);
        }

        public void Log(LogLevel level, LogCategory category, string message,
                        int? deviceAddr = null, string command = null,
                        Dictionary<string, object> data = null)
        {
            // Создаем запись лога
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Level = level.ToString(),
                Category = category.ToString(),
                Message = message,
                DeviceAddr = deviceAddr,
                Command = command,
                Data = data
            };

            // Добавляем в кэш
            AddToCache(logEntry);

            // Формируем сообщение для логгера
            string logMessage = message;
            if (deviceAddr.HasValue)
                logMessage = $"[Device {deviceAddr}] {logMessage}";
            if (!string.IsNullOrEmpty(command))
                logMessage = $"[{command}] {logMessage}";
            if (data != null)
                logMessage += $" | Data: {JsonConvert.SerializeObject(data)}";

            // Выводим в консоль если включено
            if (enableConsole)
            {
                Console.WriteLine($"{logEntry.Timestamp} | {level.ToString().PadRight(8)} | {category.ToString().PadRight(12)} | {logMessage}");
            }

            // Записываем в файл
            WriteToFile(logEntry, logMessage);

            // Обновляем статистику
            UpdateStats(level, category);
        }

        public void Debug(LogCategory category, string message, int? deviceAddr = null, 
                         string command = null, Dictionary<string, object> data = null)
        {
            Log(LogLevel.DEBUG, category, message, deviceAddr, command, data);
        }

        public void Info(LogCategory category, string message, int? deviceAddr = null,
                        string command = null, Dictionary<string, object> data = null)
        {
            Log(LogLevel.INFO, category, message, deviceAddr, command, data);
        }

        public void Warning(LogCategory category, string message, int? deviceAddr = null,
                           string command = null, Dictionary<string, object> data = null)
        {
            Log(LogLevel.WARNING, category, message, deviceAddr, command, data);
        }

        public void Error(LogCategory category, string message, int? deviceAddr = null,
                         string command = null, Dictionary<string, object> data = null)
        {
            Log(LogLevel.ERROR, category, message, deviceAddr, command, data);
        }

        public void Critical(LogCategory category, string message, int? deviceAddr = null,
                             string command = null, Dictionary<string, object> data = null)
        {
            Log(LogLevel.CRITICAL, category, message, deviceAddr, command, data);
        }

        public void LogProtocolCommand(int deviceAddr, string command, byte[] data, string direction = "OUT")
        {
            string hexData = string.Join(" ", data.Select(b => b.ToString("X2")));
            string message = $"{direction}: {command} -> {hexData}";
            var dataDict = new Dictionary<string, object>
            {
                { "hex", hexData },
                { "direction", direction }
            };
            Debug(LogCategory.PROTOCOL, message, deviceAddr, command, dataDict);
        }

        public void LogDeviceResponse(int deviceAddr, string responseType, byte[] data, 
                                    Dictionary<string, object> parsedData = null)
        {
            string hexData = string.Join(" ", data.Select(b => b.ToString("X2")));
            string message = $"IN: {responseType} <- {hexData}";
            var dataDict = new Dictionary<string, object>
            {
                { "hex", hexData },
                { "parsed", parsedData }
            };
            Debug(LogCategory.PROTOCOL, message, deviceAddr, responseType, dataDict);
        }

        public void LogDeviceAction(int deviceAddr, string action, int? branch = null, 
                                   int? relay = null, bool? result = null)
        {
            string message = $"Action: {action}";
            if (branch.HasValue)
                message += $" (Branch {branch})";
            if (relay.HasValue)
                message += $" (Relay {relay})";
            if (result.HasValue)
                message += $" -> {(result.Value ? "Success" : "Failed")}";

            var dataDict = new Dictionary<string, object>
            {
                { "action", action },
                { "branch", branch },
                { "relay", relay },
                { "result", result }
            };
            Info(LogCategory.DEVICE, message, deviceAddr, data: dataDict);
        }

        public void LogUserAction(string action, Dictionary<string, object> details = null)
        {
            Info(LogCategory.USER_ACTION, $"User action: {action}", data: details);
        }

        public void LogSystemEvent(string eventName, Dictionary<string, object> details = null)
        {
            Info(LogCategory.SYSTEM, $"System event: {eventName}", data: details);
        }

        public Dictionary<string, object> GetStats()
        {
            lock (statsLock)
            {
                var result = new Dictionary<string, object>();
                foreach (var kvp in stats)
                {
                    result[kvp.Key] = kvp.Value;
                }
                result["uptime"] = (DateTime.Now - startTime).TotalSeconds;
                result["logs_per_minute"] = stats["total_logs"] / Math.Max((DateTime.Now - startTime).TotalMinutes, 1);
                return result;
            }
        }

        public List<LogEntry> GetRecentLogs(int count = 100)
        {
            lock (cacheLock)
            {
                if (count <= 0)
                    return new List<LogEntry>(logCache);
                
                return logCache.Skip(Math.Max(0, logCache.Count - count)).ToList();
            }
        }

        public bool ExportLogs(string filename, DateTime? startTime = null, DateTime? endTime = null,
                              List<string> categories = null, List<string> levels = null)
        {
            try
            {
                var logsToExport = new List<LogEntry>();

                lock (cacheLock)
                {
                    foreach (var logEntry in logCache)
                    {
                        // Фильтруем по времени
                        var entryTime = DateTime.Parse(logEntry.Timestamp);
                        if (startTime.HasValue && entryTime < startTime.Value)
                            continue;
                        if (endTime.HasValue && entryTime > endTime.Value)
                            continue;

                        // Фильтруем по категориям
                        if (categories != null && !categories.Contains(logEntry.Category))
                            continue;

                        // Фильтруем по уровням
                        if (levels != null && !levels.Contains(logEntry.Level))
                            continue;

                        logsToExport.Add(logEntry);
                    }
                }

                // Экспортируем в JSON
                string json = JsonConvert.SerializeObject(logsToExport, Formatting.Indented);
                File.WriteAllText(filename, json, Encoding.UTF8);

                Info(LogCategory.SYSTEM, $"Exported {logsToExport.Count} log entries to {filename}");
                return true;
            }
            catch (Exception e)
            {
                Error(LogCategory.SYSTEM, $"Failed to export logs: {e.Message}");
                return false;
            }
        }

        public void ClearOldLogs(int days = 30)
        {
            try
            {
                var cutoffTime = DateTime.Now.AddDays(-days);

                lock (cacheLock)
                {
                    logCache.RemoveAll(log => DateTime.Parse(log.Timestamp) < cutoffTime);
                }

                Info(LogCategory.SYSTEM, $"Cleared logs older than {days} days");
            }
            catch (Exception e)
            {
                Error(LogCategory.SYSTEM, $"Failed to clear old logs: {e.Message}");
            }
        }

        private void AddToCache(LogEntry logEntry)
        {
            lock (cacheLock)
            {
                logCache.Add(logEntry);
                if (logCache.Count > cacheSize)
                {
                    logCache.RemoveAt(0);
                }
            }
        }

        private void UpdateStats(LogLevel level, LogCategory category)
        {
            lock (statsLock)
            {
                stats["total_logs"]++;
                stats[level.ToString()]++;
                stats[category.ToString()]++;
            }
        }

        private void WriteToFile(LogEntry logEntry, string logMessage)
        {
            try
            {
                // Основной лог файл
                string mainLogFile = Path.Combine(logDir, "bolid_emulator.log");
                string logLine = $"{logEntry.Timestamp} | {logEntry.Level.PadRight(8)} | {logEntry.Category.PadRight(12)} | {logMessage}";
                File.AppendAllText(mainLogFile, logLine + Environment.NewLine, Encoding.UTF8);

                // Лог ошибок
                if (logEntry.Level == "ERROR" || logEntry.Level == "CRITICAL")
                {
                    string errorLogFile = Path.Combine(logDir, "errors.log");
                    File.AppendAllText(errorLogFile, logLine + Environment.NewLine, Encoding.UTF8);
                }

                // Лог протокола
                if (logEntry.Category == "PROTOCOL")
                {
                    string protocolLogFile = Path.Combine(logDir, "protocol.log");
                    File.AppendAllText(protocolLogFile, logLine + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Игнорируем ошибки записи в файл
            }
        }
    }

    // Глобальный экземпляр логгера
    public static class GlobalLogger
    {
        public static readonly AdvancedLogger Instance = new AdvancedLogger(enableConsole: false);
    }
}

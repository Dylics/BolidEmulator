using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BolidEmulator
{
    public class SoundAlarm
    {
        private bool isPlaying = false;
        private bool stopFlag = false;
        private Task alarmTask;
        private CancellationTokenSource cancellationTokenSource;
        private string alarmFile;
        private double volume = 0.02; // Громкость 2%
        private SoundPlayer currentPlayer = null;
        private object playerLock = new object();

        // Windows API для управления звуком
        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr phwo, uint uDeviceID, IntPtr pwfx, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

        public SoundAlarm()
        {
            alarmFile = FindAlarmFile();
        }

        public void PlayAlarm()
        {
            if (isPlaying)
                return; // Уже играет

            isPlaying = true;
            stopFlag = false;
            cancellationTokenSource = new CancellationTokenSource();

            // Устанавливаем системную громкость перед воспроизведением
            SetSystemVolume(volume);

            // Запускаем в отдельном потоке
            alarmTask = Task.Run(() => PlayAlarmThread(cancellationTokenSource.Token));
        }

        public void StopAlarm()
        {
            Console.WriteLine("Остановка звуковой сигнализации...");
            stopFlag = true;
            cancellationTokenSource?.Cancel();

            // Принудительно останавливаем текущий плеер
            lock (playerLock)
            {
                if (currentPlayer != null)
                {
                    try
                    {
                        Console.WriteLine("Принудительная остановка SoundPlayer");
                        currentPlayer.Stop();
                        currentPlayer.Dispose();
                        currentPlayer = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при остановке SoundPlayer: {ex.Message}");
                    }
                }
            }

            // Останавливаем системные звуки через Windows API
            try
            {
                Console.WriteLine("Остановка системных звуков через Windows API");
                waveOutReset(IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при остановке системных звуков: {ex.Message}");
            }

            if (alarmTask != null && !alarmTask.IsCompleted)
            {
                try
                {
                    Console.WriteLine("Ожидание завершения задачи воспроизведения...");
                    alarmTask.Wait(100); // Минимальное время ожидания
                    Console.WriteLine("Задача воспроизведения завершена");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при ожидании завершения задачи: {ex.Message}");
                }
            }
            
            isPlaying = false;
            Console.WriteLine("Звуковая сигнализация остановлена");
        }

        private async Task PlayAlarmThread(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Начало воспроизведения звуковой сигнализации");
                while (!stopFlag && !cancellationToken.IsCancellationRequested)
                {
                    // Проверяем флаг остановки перед каждым воспроизведением
                    if (stopFlag || cancellationToken.IsCancellationRequested)
                        break;

                    // Воспроизводим аудио файл (PlaySync() блокирует выполнение до завершения)
                    PlayAudioFile();

                    // Проверяем флаг остановки после воспроизведения
                    if (stopFlag || cancellationToken.IsCancellationRequested)
                        break;

                    // Пауза между повторениями (увеличена для предотвращения наложения)
                    await Task.Delay(1000, cancellationToken);
                }
                Console.WriteLine("Воспроизведение звуковой сигнализации завершено");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Воспроизведение отменено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка воспроизведения: {ex.Message}");
            }
            finally
            {
                isPlaying = false;
                Console.WriteLine("Флаг воспроизведения сброшен");
            }
        }

        private void PlayAudioFile()
        {
            try
            {
                // Проверяем флаг остановки перед воспроизведением
                if (stopFlag)
                {
                    Console.WriteLine("Воспроизведение прервано - установлен флаг остановки");
                    return;
                }

                // Проверяем, найден ли аудио файл
                if (!string.IsNullOrEmpty(alarmFile) && File.Exists(alarmFile))
                {
                    Console.WriteLine($"Воспроизводим файл: {alarmFile}");
                    
                    // Создаем новый SoundPlayer
                    lock (playerLock)
                    {
                        if (currentPlayer != null)
                        {
                            currentPlayer.Stop();
                            currentPlayer.Dispose();
                        }
                        
                        currentPlayer = new SoundPlayer(alarmFile);
                        
                        // Используем Play() для асинхронного воспроизведения
                        // Это предотвращает блокировку UI потока
                        currentPlayer.Play();
                    }
                }
                else
                {
                    Console.WriteLine($"Файл не найден или пустой: '{alarmFile}'");
                    // Используем системный звук как резерв
                    SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка воспроизведения: {ex.Message}");
                // Используем системный звук как резерв
                SystemSounds.Exclamation.Play();
            }
        }

        public void SetVolume(double newVolume)
        {
            volume = Math.Max(0.0, Math.Min(1.0, newVolume));
            Console.WriteLine($"Установлена громкость: {volume * 100:F1}%");
            
            // Если звук уже играет, обновляем системную громкость
            if (isPlaying)
            {
                SetSystemVolume(volume);
            }
        }

        // Установка громкости через Windows API
        private void SetSystemVolume(double volumeLevel)
        {
            try
            {
                // Конвертируем громкость в формат Windows (0-65535)
                // Младшие 16 бит - левый канал, старшие 16 бит - правый канал
                uint volumeValue = (uint)(volumeLevel * 65535);
                uint leftVolume = volumeValue;
                uint rightVolume = volumeValue;
                uint combinedVolume = (rightVolume << 16) | leftVolume;

                // Устанавливаем громкость для всех устройств
                waveOutSetVolume(IntPtr.Zero, combinedVolume);
                Console.WriteLine($"Системная громкость установлена: {volumeLevel * 100:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка установки системной громкости: {ex.Message}");
            }
        }

        // Принудительная остановка всех звуков
        public void ForceStopAllSounds()
        {
            Console.WriteLine("Принудительная остановка всех звуков");
            
            // Останавливаем текущий плеер
            lock (playerLock)
            {
                if (currentPlayer != null)
                {
                    try
                    {
                        currentPlayer.Stop();
                        currentPlayer.Dispose();
                        currentPlayer = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при принудительной остановке: {ex.Message}");
                    }
                }
            }

            // Останавливаем все звуки через Windows API
            try
            {
                waveOutReset(IntPtr.Zero);
                Console.WriteLine("Все звуки остановлены через Windows API");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при остановке через Windows API: {ex.Message}");
            }
        }

        public void SetAlarmFile(string filePath)
        {
            alarmFile = filePath;
        }

        private string FindAlarmFile()
        {
            // Получаем папку проекта
            string projectDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            string assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";

            // Возможные расположения
            string[] candidates = {
                // В текущей рабочей директории
                Path.Combine(currentDir, "alarm.wav"),
                Path.Combine(currentDir, "alarm.mp3"),
                // В папке сборки
                Path.Combine(assemblyDir, "alarm.wav"),
                Path.Combine(assemblyDir, "alarm.mp3"),
                // В папке приложения
                Path.Combine(projectDir, "alarm.wav"),
                Path.Combine(projectDir, "alarm.mp3"),
                // В подпапках
                Path.Combine(currentDir, "data", "alarm.wav"),
                Path.Combine(currentDir, "data", "alarm.mp3"),
                Path.Combine(currentDir, "sounds", "alarm.wav"),
                Path.Combine(currentDir, "sounds", "alarm.mp3"),
                Path.Combine(currentDir, "audio", "alarm.wav"),
                Path.Combine(currentDir, "audio", "alarm.mp3"),
                // В родительской папке
                Path.Combine(Directory.GetParent(currentDir)?.FullName ?? "", "alarm.wav"),
                Path.Combine(Directory.GetParent(currentDir)?.FullName ?? "", "alarm.mp3"),
                Path.Combine(Directory.GetParent(projectDir)?.FullName ?? "", "alarm.wav"),
                Path.Combine(Directory.GetParent(projectDir)?.FullName ?? "", "alarm.mp3")
            };

            // Выводим отладочную информацию
            Console.WriteLine($"Поиск файла alarm.wav:");
            Console.WriteLine($"  Текущая директория: {currentDir}");
            Console.WriteLine($"  Папка приложения: {projectDir}");
            Console.WriteLine($"  Папка сборки: {assemblyDir}");
            
            foreach (string path in candidates)
            {
                Console.WriteLine($"Проверяем: {path} - {File.Exists(path)}");
                if (File.Exists(path))
                {
                    Console.WriteLine($"Найден файл: {path}");
                    return path;
                }
            }

            // Последняя попытка — рекурсивный поиск
            try
            {
                string searchRoot = Directory.GetParent(Directory.GetParent(projectDir)?.FullName ?? "")?.FullName ?? "";
                Console.WriteLine($"Рекурсивный поиск в: {searchRoot}");
                if (Directory.Exists(searchRoot))
                {
                    string[] foundFiles = Directory.GetFiles(searchRoot, "alarm.*", SearchOption.AllDirectories);
                    Console.WriteLine($"Найдено файлов: {foundFiles.Length}");
                    if (foundFiles.Length > 0)
                    {
                        Console.WriteLine($"Первый найденный файл: {foundFiles[0]}");
                        return foundFiles[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка рекурсивного поиска: {ex.Message}");
            }

            Console.WriteLine("Файл alarm.wav не найден!");
            return null;
        }
    }

    // Глобальный экземпляр для использования в приложении
    public static class GlobalSoundAlarm
    {
        public static readonly SoundAlarm Instance = new SoundAlarm();
    }
}

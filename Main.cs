using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Wox.Plugin;
using Serilog;
using Serilog.Events;

namespace WoxTimerPlugin
{
    public class Main : IPlugin
    {
        private PluginInitContext _context;
        private Dictionary<string, TimerInfo> _activeTimers = new Dictionary<string, TimerInfo>();
        private static readonly Regex TimeRegex = new Regex(@"^(?:(?<hours>\d+):)?(?<minutes>\d+):(?<seconds>\d+)$", RegexOptions.Compiled);
        // 新增鬧鐘格式的正則表達式 (HH:mm)
        private static readonly Regex AlarmTimeRegex = new Regex(@"^(?<hours>\d{1,2}):(?<minutes>\d{2})$", RegexOptions.Compiled);

        // Serilog logger 實例
        private static ILogger _logger;

        // 計時器資訊類別，用於記錄計時器相關資訊
        private class TimerInfo
        {
            public System.Threading.Timer Timer { get; set; }
            public string Title { get; set; }
            public DateTime EndTime { get; set; }
            public int TotalSeconds { get; set; }
            public bool IsAlarm { get; set; }

            public string GetRemainingTime()
            {
                TimeSpan remaining = EndTime - DateTime.Now;
                if (remaining.TotalSeconds <= 0)
                    return "即將完成";

                int hours = remaining.Hours;
                int minutes = remaining.Minutes;
                int seconds = remaining.Seconds;

                return FormatTimeDisplay(hours, minutes, seconds);
            }
        }
        public void Init(PluginInitContext context)
        {
            _context = context;

            // 建立日誌目錄
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WoxTimerPlugin", "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }            // 設定 Serilog
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(logDir, "wox-timer-plugin-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5, // 保留最近5天的日誌
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _logger.Information("Wox 計時器插件已初始化");
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            if (string.IsNullOrEmpty(query.Search))
            {
                // 顯示基本使用方法
                results.Add(new Result
                {
                    Title = "計時器與鬧鐘",
                    SubTitle = "倒數計時: timer HH:mm:ss [標題] | 鬧鐘: timer HH:mm [標題]",
                    IcoPath = "Images\\timer.png"
                });

                // 查看所有計時器選項
                if (_activeTimers.Count > 0)
                {
                    results.Add(new Result
                    {
                        Title = "查看所有計時器與鬧鐘",
                        SubTitle = $"目前有 {_activeTimers.Count} 個活動計時器或鬧鐘",
                        IcoPath = "Images\\timer.png",
                        Action = c =>
                        {
                            // 原始程式碼:
                            // _context.API.ChangeQuery("timer 列表");
                            // return false;
                            Thread.Sleep(300);
                            _logger.Debug("收到查詢 - 關鍵字: {SearchTerm}", query.Search);
                            _context.API.ChangeQuery("timer 列表");
                            return false;
                        }
                    });

                    results.Add(new Result
                    {
                        Title = "取消所有計時器與鬧鐘",
                        SubTitle = $"一次取消所有 {_activeTimers.Count} 個活動計時器與鬧鐘",
                        IcoPath = "Images\\timer.png",
                        Action = c =>
                        {
                            CancelAllTimers();
                            return true;
                        }
                    });
                }

                return results;
            }

            // 檢查是否為列表命令
            if (query.Search.Trim().ToLower() == "列表" || query.Search.Trim().ToLower() == "list")
            {
                if (_activeTimers.Count > 0)
                {
                    foreach (var pair in _activeTimers)
                    {
                        var timerId = pair.Key;
                        var timerInfo = pair.Value;
                        string itemType = timerInfo.IsAlarm ? "鬧鐘" : "計時器";

                        results.Add(new Result
                        {
                            Title = $"{itemType}: {timerInfo.Title} - 剩餘 {timerInfo.GetRemainingTime()}",
                            SubTitle = $"觸發時間: {timerInfo.EndTime:HH:mm:ss} | 按 Enter 取消",
                            IcoPath = "Images\\timer.png",
                            Action = c =>
                            {
                                CancelTimer(timerId);
                                return true;
                            }
                        });
                    }
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "目前沒有活動的計時器或鬧鐘",
                        SubTitle = "計時器: timer HH:mm:ss [標題] | 鬧鐘: timer HH:mm [標題]",
                        IcoPath = "Images\\timer.png"
                    });
                }

                return results;
            }

            // 檢查是否為取消命令
            if (query.Search.Trim().ToLower() == "cancel" || query.Search.Trim().ToLower() == "取消")
            {
                if (_activeTimers.Count > 0)
                {
                    results.Add(new Result
                    {
                        Title = "取消所有計時器與鬧鐘",
                        SubTitle = $"目前有 {_activeTimers.Count} 個活動計時器或鬧鐘",
                        IcoPath = "Images\\timer.png",
                        Action = c =>
                        {
                            CancelAllTimers();
                            return true;
                        }
                    });

                    // 提供查看列表的選項
                    results.Add(new Result
                    {
                        Title = "查看計時器與鬧鐘列表",
                        SubTitle = "選擇要取消的特定計時器或鬧鐘",
                        IcoPath = "Images\\timer.png",
                        Action = c =>
                        {
                            _context.API.ChangeQuery("timer 列表");
                            return false;
                        }
                    });
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "目前沒有活動的計時器或鬧鐘",
                        SubTitle = "請先設定一個計時器或鬧鐘",
                        IcoPath = "Images\\timer.png"
                    });
                }

                return results;
            }

            // 解析參數
            var parts = query.Search.Trim().Split(new[] { ' ' }, 2);
            var timeStr = parts[0];
            string title = parts.Length > 1 ? parts[1] : "計時器";

            // 先嘗試匹配倒數計時格式 (HH:mm:ss)
            var match = TimeRegex.Match(timeStr);
            if (match.Success)
            {
                int hours = string.IsNullOrEmpty(match.Groups["hours"].Value) ? 0 : int.Parse(match.Groups["hours"].Value);
                int minutes = int.Parse(match.Groups["minutes"].Value);
                int seconds = int.Parse(match.Groups["seconds"].Value);

                // 確保時間格式合理
                if (hours >= 0 && minutes >= 0 && minutes < 60 && seconds >= 0 && seconds < 60)
                {
                    int totalSeconds = hours * 3600 + minutes * 60 + seconds;
                    if (totalSeconds > 0)
                    {
                        string displayTime = FormatTimeDisplay(hours, minutes, seconds);

                        results.Add(new Result
                        {
                            Title = $"設定倒數計時 {displayTime}",
                            SubTitle = $"標題: {title}",
                            IcoPath = "Images\\timer.png",
                            Action = c =>
                            {
                                StartTimer(totalSeconds, title, false);
                                return true;
                            }
                        });
                    }
                    else
                    {
                        results.Add(new Result
                        {
                            Title = "時間必須大於 0",
                            SubTitle = "請輸入有效的時間，例如：timer 00:01:30 休息",
                            IcoPath = "Images\\timer.png"
                        });
                    }
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "無效的時間格式",
                        SubTitle = "請使用 HH:mm:ss 格式，例如：timer 00:05:00 喝水",
                        IcoPath = "Images\\timer.png"
                    });
                }

                return results;
            }

            // 如果不是倒數計時格式，嘗試匹配鬧鐘格式 (HH:mm)
            var alarmMatch = AlarmTimeRegex.Match(timeStr);
            if (alarmMatch.Success)
            {
                int hours = int.Parse(alarmMatch.Groups["hours"].Value);
                int minutes = int.Parse(alarmMatch.Groups["minutes"].Value);

                // 確保時間格式合理
                if (hours >= 0 && hours < 24 && minutes >= 0 && minutes < 60)
                {
                    // 計算鬧鐘時間
                    DateTime now = DateTime.Now;
                    DateTime alarmTime = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

                    // 如果鬧鐘時間已經過去，設定為明天同一時間
                    if (alarmTime <= now)
                    {
                        alarmTime = alarmTime.AddDays(1);
                    }

                    // 計算秒數差異
                    TimeSpan timeSpan = alarmTime - now;
                    int totalSeconds = (int)timeSpan.TotalSeconds;
                    string displayTime = $"{alarmTime:HH:mm}";
                    string remainingTime = FormatTimeDisplay((int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);

                    results.Add(new Result
                    {
                        Title = $"設定鬧鐘 {displayTime} ({remainingTime}後)",
                        SubTitle = $"標題: {title}",
                        IcoPath = "Images\\timer.png",
                        Action = c =>
                        {
                            StartTimer(totalSeconds, title, true, alarmTime);
                            return true;
                        }
                    });
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "無效的時間格式",
                        SubTitle = "鬧鐘時間格式應為 HH:mm，例如：timer 14:30 開會",
                        IcoPath = "Images\\timer.png"
                    });
                }

                return results;
            }

            // 如果都不匹配，顯示格式提示
            results.Add(new Result
            {
                Title = "無效的時間格式",
                SubTitle = "計時器: HH:mm:ss (例如 00:05:00) | 鬧鐘: HH:mm (例如 14:30)",
                IcoPath = "Images\\timer.png"
            });

            return results;
        }

        private static string FormatTimeDisplay(int hours, int minutes, int seconds)
        {
            if (hours > 0)
            {
                return $"{hours}小時{minutes}分{seconds}秒";
            }
            else if (minutes > 0)
            {
                return $"{minutes}分{seconds}秒";
            }
            else
            {
                return $"{seconds}秒";
            }
        }
        private void StartTimer(int seconds, string title, bool isAlarm, DateTime? specificEndTime = null)
        {
            // 建立唯一ID
            string id = Guid.NewGuid().ToString();

            // 轉換為毫秒
            int milliseconds = seconds * 1000;

            // 計算結束時間
            DateTime endTime = specificEndTime ?? DateTime.Now.AddSeconds(seconds);

            // 顯示通知：計時器/鬧鐘已啟動
            string notificationType = isAlarm ? "鬧鐘" : "計時器";
            string notification = isAlarm
                ? $"{endTime:HH:mm} - {title}"
                : $"{FormatTimeDisplay(seconds / 3600, (seconds / 60) % 60, seconds % 60)} - {title}";

            // 記錄啟動資訊
            _logger.Information(
                "{TimerType} 已啟動 - ID: {ID}, 標題: {Title}, 持續時間: {Duration}秒, 結束時間: {EndTime}",
                notificationType,
                id,
                title,
                seconds,
                endTime);

            ShowNotification($"{notificationType}已啟動", notification);

            // 建立 Timer 實例
            var timer = new System.Threading.Timer(state =>
            {
                // 通知使用者時間到
                string completeMessage = isAlarm ? "鬧鐘時間到！" : "倒數計時完成！";
                ShowNotification(title, completeMessage);

                // 記錄完成資訊
                _logger.Information(
                    "{TimerType} 已完成 - ID: {ID}, 標題: {Title}",
                    isAlarm ? "鬧鐘" : "計時器",
                    id,
                    title);

                // 從活動計時器中移除
                _activeTimers.Remove(id);
            }, null, milliseconds, Timeout.Infinite);
            // 將計時器資訊儲存到字典
            _activeTimers[id] = new TimerInfo
            {
                Timer = timer,
                Title = title,
                EndTime = endTime,
                TotalSeconds = seconds,
                IsAlarm = isAlarm
            };
        }
        private void CancelAllTimers()
        {
            int timerCount = 0;
            int alarmCount = 0;

            foreach (var timerInfo in _activeTimers.Values)
            {
                timerInfo.Timer.Dispose();
                if (timerInfo.IsAlarm)
                    alarmCount++;
                else
                    timerCount++;
            }

            int totalCount = _activeTimers.Count;

            // 記錄取消資訊
            _logger.Information("批次取消所有計時器及鬧鐘 - 數量: {TotalCount}（計時器: {TimerCount}，鬧鐘: {AlarmCount}）",
                totalCount, timerCount, alarmCount);

            _activeTimers.Clear();

            if (timerCount > 0 && alarmCount > 0)
                ShowNotification("已取消", $"已取消 {timerCount} 個計時器和 {alarmCount} 個鬧鐘");
            else if (timerCount > 0)
                ShowNotification("計時器已取消", $"已取消 {timerCount} 個計時器");
            else
                ShowNotification("鬧鐘已取消", $"已取消 {alarmCount} 個鬧鐘");
        }
        private void CancelTimer(string timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timerInfo))
            {
                timerInfo.Timer.Dispose();
                _activeTimers.Remove(timerId);

                string itemType = timerInfo.IsAlarm ? "鬧鐘" : "計時器";

                // 記錄取消資訊
                _logger.Information(
                    "{TimerType} 已手動取消 - ID: {ID}, 標題: {Title}, 原定結束時間: {EndTime}",
                    itemType,
                    timerId,
                    timerInfo.Title,
                    timerInfo.EndTime);

                ShowNotification($"{itemType}已取消", $"已取消{itemType}: {timerInfo.Title}");
            }
        }
        
        private void ShowNotification(string title, string message)
        {
            using (var notifyIcon = new System.Windows.Forms.NotifyIcon())
            {
                string exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string iconPath = System.IO.Path.Combine(exeDir, "Images", "timer.png");
            
                // 修改後的代碼，僅使用 .ico 檔案作為自訂圖示
                if (System.IO.File.Exists(iconPath) && System.IO.Path.GetExtension(iconPath).ToLower() == ".ico")
                {
                    notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    notifyIcon.Icon = System.Drawing.SystemIcons.Information;
                }
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(5000, title, message, System.Windows.Forms.ToolTipIcon.Info);
                System.Threading.Thread.Sleep(5000);
                notifyIcon.Visible = false;
            }
        }

        // 處理計時器完成時的操作，包括釋放計時器、更新記錄以及顯示桌面通知
        private void HandleTimerFinish(string timerId, TimerInfo info)
        {
            info.Timer.Dispose();
            _activeTimers.Remove(timerId);
            ShowNotification("計時完成", $"{info.Title} 已完成倒數計時。");
            _logger.Information("{Title} 的計時器已完成", info.Title);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Wox.Plugin;

namespace WoxTimerPlugin
{
    public class Main : IPlugin
    {
        private PluginInitContext _context;
        private Dictionary<string, Timer> _activeTimers = new Dictionary<string, Timer>();
        private static readonly Regex TimeRegex = new Regex(@"^(?:(?<hours>\d+):)?(?<minutes>\d+):(?<seconds>\d+)$", RegexOptions.Compiled);

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            
            if (string.IsNullOrEmpty(query.Search))
            {
                // 顯示基本使用方法
                results.Add(new Result
                {
                    Title = "計時器",
                    SubTitle = "使用方式: timer HH:mm:ss [<標題>] - 設定倒數計時，例如 timer 00:05:00 喝水",
                    IcoPath = "Images\\timer.png"
                });
                
                // 顯示取消選項（如果有活動的計時器）
                if (_activeTimers.Count > 0)
                {
                    results.Add(new Result
                    {
                        Title = "取消所有計時器",
                        SubTitle = $"目前有 {_activeTimers.Count} 個活動計時器",
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

            // 檢查是否為取消命令
            if (query.Search.Trim().ToLower() == "cancel" || query.Search.Trim().ToLower() == "取消")
            {
                if (_activeTimers.Count > 0)
                {
                    results.Add(new Result
                    {
                        Title = "取消所有計時器",
                        SubTitle = $"目前有 {_activeTimers.Count} 個活動計時器",
                        IcoPath = "Images\\timer.png",
                        Action = c =>
                        {
                            CancelAllTimers();
                            return true;
                        }
                    });
                }
                else
                {
                    results.Add(new Result
                    {
                        Title = "目前沒有活動的計時器",
                        SubTitle = "請先設定一個計時器",
                        IcoPath = "Images\\timer.png"
                    });
                }
                
                return results;
            }

            // 解析參數
            var parts = query.Search.Trim().Split(new[] { ' ' }, 2);
            var timeStr = parts[0];
            string title = parts.Length > 1 ? parts[1] : "計時器";

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
                                StartTimer(totalSeconds, title);
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

        private string FormatTimeDisplay(int hours, int minutes, int seconds)
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

        private void StartTimer(int seconds, string title)
        {
            // 建立唯一ID
            string id = Guid.NewGuid().ToString();
            
            // 轉換為毫秒
            int milliseconds = seconds * 1000;
            
            // 顯示通知：計時器已啟動
            ShowNotification($"計時器已啟動", $"{FormatTimeDisplay(seconds/3600, (seconds/60)%60, seconds%60)} - {title}");
            
            // 建立並啟動 Timer
            var timer = new Timer(state =>
            {
                // 通知使用者時間到
                ShowNotification(title, $"倒數計時完成！");
                
                // 從活動計時器中移除
                _activeTimers.Remove(id);
            }, null, milliseconds, Timeout.Infinite);
            
            // 將計時器加入活動計時器列表
            _activeTimers[id] = timer;
        }

        private void CancelAllTimers()
        {
            foreach (var timer in _activeTimers.Values)
            {
                timer.Dispose();
            }
            
            int count = _activeTimers.Count;
            _activeTimers.Clear();
            
            ShowNotification("計時器已取消", $"已取消 {count} 個計時器");
        }

        private void ShowNotification(string title, string message)
        {
            // 顯示 Windows 通知
            var notification = new NotifyIcon
            {
                Visible = true,
                Icon = System.Drawing.SystemIcons.Information,
                BalloonTipTitle = title,
                BalloonTipText = message
            };
            
            notification.ShowBalloonTip(5000); // 顯示 5 秒
            
            // 設定自動清理通知
            var disposeTimer = new Timer(obj =>
            {
                notification.Dispose();
            }, null, 6000, Timeout.Infinite);
        }
    }
}

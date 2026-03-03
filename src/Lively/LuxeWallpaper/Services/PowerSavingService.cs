using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;

namespace LuxeWallpaper.Services
{
    public class PowerSavingService
    {
        private readonly System.Timers.Timer _monitorTimer;
        private readonly string[] _gameProcessNames = new[]
        {
            "game", "steam", "origin", "uplay", "epicgames", "battlenet",
            "league of legends", "dota2", "csgo", "valorant", "pubg",
            "genshin impact", "cyberpunk", "witcher", "skyrim", "fallout"
        };

        private readonly string[] _officeProcessNames = new[]
        {
            "winword", "excel", "powerpnt", "outlook", "onenote",
            "photoshop", "illustrator", "premiere", "aftereffects",
            "autocad", "solidworks", "catia", "proe", "ug"
        };

        private bool _isInGameMode;
        private bool _isInOfficeMode;
        private bool _isPowerSavingEnabled;
        private int _powerThreshold = 80;
        private bool _shouldStopRendering;

        public event EventHandler<bool> PowerSavingStateChanged;
        public event EventHandler<bool> RenderingStateChanged;

        public bool IsInGameMode => _isInGameMode;
        public bool IsInOfficeMode => _isInOfficeMode;
        public bool IsPowerSavingActive => _isPowerSavingEnabled && (_isInGameMode || _isInOfficeMode);
        public bool ShouldStopRendering => _shouldStopRendering;
        public int PowerThreshold => _powerThreshold;

        public PowerSavingService()
        {
            _monitorTimer = new System.Timers.Timer(5000);
            _monitorTimer.Elapsed += OnMonitorTimerElapsed;
            _monitorTimer.AutoReset = true;
        }

        public void Start(bool enablePowerSaving)
        {
            _isPowerSavingEnabled = enablePowerSaving;
            if (enablePowerSaving)
            {
                _monitorTimer.Start();
            }
            else
            {
                _monitorTimer.Stop();
            }
        }

        public void Stop()
        {
            _monitorTimer.Stop();
        }

        public void SetPowerSavingEnabled(bool enabled)
        {
            _isPowerSavingEnabled = enabled;
            if (enabled)
            {
                if (!_monitorTimer.Enabled)
                {
                    _monitorTimer.Start();
                }
            }
            else
            {
                _monitorTimer.Stop();
                _isInGameMode = false;
                _isInOfficeMode = false;
                _shouldStopRendering = false;
                PowerSavingStateChanged?.Invoke(this, false);
                RenderingStateChanged?.Invoke(this, false);
            }
        }

        public void SetPowerThreshold(int threshold)
        {
            _powerThreshold = threshold;
        }

        public void CheckShouldStopRendering(int currentCoveragePercentage)
        {
            bool shouldStop = currentCoveragePercentage >= _powerThreshold;
            if (_shouldStopRendering != shouldStop)
            {
                _shouldStopRendering = shouldStop;
                RenderingStateChanged?.Invoke(this, shouldStop);
            }
        }

        private void OnMonitorTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isPowerSavingEnabled)
                return;

            CheckRunningProcesses();
        }

        private void CheckRunningProcesses()
        {
            try
            {
                var processes = Process.GetProcesses();
                var processNames = processes.Select(p => p.ProcessName.ToLower()).ToList();

                bool wasInGameMode = _isInGameMode;
                bool wasInOfficeMode = _isInOfficeMode;

                _isInGameMode = processNames.Any(name => 
                    _gameProcessNames.Any(gameName => name.Contains(gameName)));

                _isInOfficeMode = processNames.Any(name => 
                    _officeProcessNames.Any(officeName => name.Contains(officeName)));

                bool wasPowerSavingActive = wasInGameMode || wasInOfficeMode;
                bool isPowerSavingActive = _isInGameMode || _isInOfficeMode;

                if (wasPowerSavingActive != isPowerSavingActive)
                {
                    PowerSavingStateChanged?.Invoke(this, isPowerSavingActive);
                }
            }
            catch (Exception)
            {
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        public string GetActiveWindowTitle()
        {
            const int nChars = 256;
            System.Text.StringBuilder buffer = new System.Text.StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buffer, nChars) > 0)
            {
                return buffer.ToString();
            }
            return string.Empty;
        }

        public void Dispose()
        {
            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
        }
    }
}

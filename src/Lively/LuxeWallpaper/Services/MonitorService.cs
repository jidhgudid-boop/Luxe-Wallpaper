using LuxeWallpaper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LuxeWallpaper.Services
{
    public class MonitorService
    {
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public uint cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        private const uint MONITORINFOF_PRIMARY = 0x00000001;

        public List<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo>();
            uint devNum = 0;
            DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE();
            displayDevice.cb = (uint)Marshal.SizeOf(displayDevice);

            while (EnumDisplayDevices(null, devNum, ref displayDevice, 0))
            {
                if ((displayDevice.StateFlags & 0x00000008) != 0)
                {
                    var monitorInfo = new MonitorInfo
                    {
                        DeviceName = displayDevice.DeviceName,
                        IsPrimary = (displayDevice.StateFlags & 0x00000004) != 0
                    };
                    monitors.Add(monitorInfo);
                }
                devNum++;
            }

            // 获取显示器边界信息
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = (uint)Marshal.SizeOf(mi);
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    var monitor = monitors.FirstOrDefault(m => monitors.IndexOf(m) == monitors.Count - 1);
                    if (monitor != null)
                    {
                        monitor.ScreenBounds = new Rect(
                            mi.rcMonitor.left,
                            mi.rcMonitor.top,
                            mi.rcMonitor.right - mi.rcMonitor.left,
                            mi.rcMonitor.bottom - mi.rcMonitor.top);

                        monitor.WorkingArea = new Rect(
                            mi.rcWork.left,
                            mi.rcWork.top,
                            mi.rcWork.right - mi.rcWork.left,
                            mi.rcWork.bottom - mi.rcWork.top);
                    }
                }
                return true;
            }, IntPtr.Zero);

            return monitors;
        }

        public MonitorInfo GetPrimaryMonitor()
        {
            return GetAllMonitors().FirstOrDefault(m => m.IsPrimary);
        }
    }
}

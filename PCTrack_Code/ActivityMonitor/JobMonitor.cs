using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Data = ActivityMonitor.XmlDataLayer;

namespace ActivityMonitor
{
    /// <summary>
    /// Interface to be implemented by and Class implementing DataLayer
    /// </summary>
    interface Datalayer
    {
        void SendScreenChange(string processName, string processTitle, TimeSpan duration);
    }

    /// <summary>
    /// This monitors foreground window and idle time
    /// </summary>
    public static class JobMonitor
    {
        private static volatile bool isIdle = false;
        private const int idleThresholdMinutes = 15;
        private const double logThresholdTime = 2;
        public static volatile bool Pause = false;
        /// <summary>
        /// Monitors the idle time
        /// </summary>
        public static void MonitorIdleTime()
        {
            while (true)
            {
                var idleTime = IdleTimeDetector.GetIdleTimeInfo();
                if (idleTime.IdleTime.TotalMinutes >= idleThresholdMinutes)
                {
                    isIdle = true;
                }
                else
                {
                    isIdle = false;
                }
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Monitor foreground window changes
        /// </summary>
        public static void MonitorForegroundChange()
        {
            string processTitleOld = null, processTitleNew = null, processNameOld = null, processNameNew = null;
            DateTime timeOld = DateTime.Now, timeNew = DateTime.Now;
            bool isIdleLogged = false;
            while (true)
            {
                Datalayer data = new Data();
                if (isIdle == false && Pause == false)
                {
                    isIdleLogged = false;
                    IntPtr hwnd = GetForegroundWindow();
                    uint pid;
                    Process process = null;
                    GetWindowThreadProcessId(hwnd, out pid);
                    try
                    {
                        process = Process.GetProcessById((int)pid);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    processNameNew = process.ProcessName;
                    processTitleNew = process.MainWindowTitle;
                    timeNew = DateTime.Now;
                    processNameOld = processNameOld == null ? processNameNew : processNameOld;
                    processTitleOld = processTitleOld == null ? processTitleNew : processTitleOld;
                    if (processTitleOld != processTitleNew && processNameOld != processNameNew)
                    {
                        if (processTitleOld != "" && processTitleOld != null)
                        {
                            data.SendScreenChange(processNameOld, processTitleOld, timeNew.Subtract(timeOld));
                        }
                        processNameOld = processNameNew;
                        processTitleOld = processTitleNew;
                        timeOld = timeNew;
                    }
                    else if (timeNew.Subtract(timeOld).TotalMinutes >= logThresholdTime)
                    {
                        if (processTitleNew != "" && processTitleNew != null)
                        {
                            data.SendScreenChange(processNameNew, processTitleNew, timeNew.Subtract(timeOld));
                        }
                        timeOld = timeNew;
                    }
                }
                else
                {
                    if (!isIdleLogged)
                    {
                        timeNew = DateTime.Now;
                        if (processTitleOld != "" && processTitleOld != null)
                        {
                            data.SendScreenChange(processNameOld, processTitleOld, timeNew.Subtract(timeOld));
                        }
                        isIdleLogged = true;
                    }
                    timeOld = DateTime.Now;
                }
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Detects if PC is idle
        /// </summary>
        public static class IdleTimeDetector
        {
            [DllImport("user32.dll")]
            static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

            public static IdleTimeInfo GetIdleTimeInfo()
            {
                int systemUptime = Environment.TickCount,
                    lastInputTicks = 0,
                    idleTicks = 0;

                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
                lastInputInfo.dwTime = 0;

                if (GetLastInputInfo(ref lastInputInfo))
                {
                    lastInputTicks = (int)lastInputInfo.dwTime;

                    idleTicks = systemUptime - lastInputTicks;
                }

                return new IdleTimeInfo
                {
                    LastInputTime = DateTime.Now.AddMilliseconds(-1 * idleTicks),
                    IdleTime = new TimeSpan(0, 0, 0, 0, idleTicks),
                    SystemUptimeMilliseconds = systemUptime,
                };
            }
        }

        public class IdleTimeInfo
        {
            public DateTime LastInputTime { get; internal set; }

            public TimeSpan IdleTime { get; internal set; }

            public int SystemUptimeMilliseconds { get; internal set; }
        }

        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    /// <summary>
    /// This class spawns 4 threads to do following jobs:
    /// 1. Detect and log idle time
    /// 2. Detect foreground window and log it
    /// 3. Synthesise logs periodically to output required information
    /// 4. Update process statistics for downloaded temp files
    /// </summary>
    public static class MainManager
    {
        public static void SpawnProcesses()
        {
            Thread idleTimeMonitoring = new Thread(JobMonitor.MonitorIdleTime);
            Thread monitorForegroundChanges = new Thread(JobMonitor.MonitorForegroundChange);
            Thread combineTemp = new Thread(new XmlDataLayer().CombineResults);
            Thread statsFiles = new Thread(XmlDataLayer.UpdateStatsFiles);
            idleTimeMonitoring.Start();
            monitorForegroundChanges.Start();
            combineTemp.Start();
            statsFiles.Start();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    /// <summary>
    /// data strucutre to store days statistics
    /// </summary>
    public class DayProcessStats
    {
        public DateTime Date { get; set; }
        public List<FinalProcessStat> FinalProcessStatList { get; set; }
        public DayProcessStats(DateTime date)
        {
            Date = date;
            FinalProcessStatList = XmlDataLayer.getDayProcessStats(date);
        }

        public class FinalProcessStat
        {
            public string Name { get; set; }
            public TimeSpan Period { get; set; }

            public FinalProcessStat(string name, TimeSpan period)
            {
                Name = name;
                Period = period;
            }

        }
    }
}

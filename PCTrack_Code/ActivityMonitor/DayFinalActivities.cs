using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    /// <summary>
    /// data structure to store day's activities
    /// </summary>
    public class DayFinalActivities
    {
        public DateTime Date { get; set; }
        public List<FinalActivities> FinalActivityList { get; set; }
        public DayFinalActivities(DateTime date)
        {
            Date = date;
            FinalActivityList = XmlDataLayer.getFinalActivities(date);
        }
        public class FinalActivities
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public List<Process> Processes { get; set; }
            public FinalActivities() { }
            public FinalActivities(TimeSpan startTime, TimeSpan endTime, List<Process> processes)
            {
                StartTime = startTime;
                EndTime = endTime;
                Processes = processes;
            }
            public class Process
            {
                public string Name { get; set; }
                public TimeSpan Duration { get; set; }
                public List<Title> Titles { get; set; }
                public Process() { }
                public Process(string name, List<Title> titles)
                {
                    Name = name;
                    Titles = titles;
                }
                public class Title
                {
                    public string Heading { get; set; }
                    public TimeSpan Duration { get; set; }
                    public Title(string heading, TimeSpan duration)
                    {
                        Heading = heading;
                        Duration = duration;
                    }
                }
            }
        }
    }
}

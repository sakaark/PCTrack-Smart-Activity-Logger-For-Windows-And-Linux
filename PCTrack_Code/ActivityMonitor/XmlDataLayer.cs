// This file contains class and methods to handle all xml manipulations of data and configuration files
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Globalization;
using System.Threading;

namespace ActivityMonitor
{
    public class XmlDataLayer : Datalayer
    {
        public static string TempDirectory = null;
        public string TempFile = null;
        public static string SecondaryOutputDirectory = null;
        public string SecondaryOutputFile = null;
        public static string FinalOutputDir = null;
        private const string tempProcInfoFolder = "Info";
        private const string tempInfoFileName = "info";
        private const string tempFolderName = "Temp";
        private const string secondaryTempFolderName = "SecondaryTemp";
        private const string finalOutputFolderName = "Final";
        private const string processStatsFolderName = "ProcessStats";
        private const string configFile = "config.xml";
        private static string downloadsFile = "downloads.xml";
        private static string uploadsFile = "uploads.xml";
        private const double minTimePerHalfHour = 300;
        private const double rejectionRatioInv = 2;
        private static IFormatProvider culture = CultureInfo.InvariantCulture;
        private static object currentFinalDateFileAccess = new Object();
        private static object currentProcessDateFileAccess = new Object();

        /// <summary>
        /// combine the results of temporary files
        /// </summary>
        public void CombineResults()
        {
            while (true)
            {
                Thread.Sleep(10 * 1000);
                CombineRecentResults();
                Thread.Sleep(2 * 60 * 1000);
            }
        }

        /// <summary>
        /// clear generated files
        /// </summary>
        private void clearGenMainFiles()
        {
            try
            {
                string mainDir = GetUserApplicationDirectory() + "\\Final";
                string secDir = GetUserApplicationDirectory() + "\\SecondaryTemp";
                var main = new DirectoryInfo(mainDir);
                main.Delete(true);
                var sec = new DirectoryInfo(secDir);
                sec.Delete(true);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// update process statistics files
        /// </summary>
        public static void UpdateStatsFiles()
        {
            Thread.Sleep(10 * 1000);
            string statsDir = GetUserApplicationDirectory() + "\\ProcessStats";
            while (true)
            {
                try
                {
                    var main = new DirectoryInfo(statsDir);
                    main.Delete(true);
                    break;
                }
                catch { }
            }
            createDirectoryIfNotExists(statsDir);
            string dir = getTempDirectory();
            string[] tempFiles = Directory.GetFiles(dir);
            foreach (string tempFile in tempFiles)
            {
                XElement temp = loadTempFile(tempFile);
                
                string date = (string)temp.Attribute("date");
                List<XElement> processes = (from p in temp.Element("entries").Elements()
                                      select p).ToList();
                foreach (XElement p in processes)
                {
                    addToProcessStatistics((string)p.Attribute("processName"), TimeSpan.ParseExact((string)p.Attribute("period"), "g", culture), date);
                }
            }
            Thread.Sleep(20 * 60 * 1000);
        }

        /// <summary>
        /// combine recent results not yet processed
        /// </summary>
        public void CombineRecentResults()
        {
            clearGenMainFiles();
            SecondaryOutputDirectory = SecondaryOutputDirectory == null ? getSecondaryOutputDirectory() : SecondaryOutputDirectory;
            TempDirectory = TempDirectory == null ? getTempDirectory() : TempDirectory;
            string[] tempFiles = Directory.GetFiles(TempDirectory);
            foreach (string tempFile in tempFiles)
            {
                if (isTempProcessed(tempFile))
                {
                    continue;
                }
                if (tempFile == GetCurrentTempFileName())
                {
                    continue;
                }
                XElement xelement = loadTempFile(tempFile);
                XElement[] topProcesses = getTopTwoTimes(xelement.Element("entries"));
                XElement[] topTitles = topProcesses[0] != null ? getTopTwoTimes(topProcesses[0], "record") : new XElement[2] { null, null };
                XElement[] secondTitles = topProcesses[1] != null ? getTopTwoTimes(topProcesses[1], "record") : new XElement[2] { null, null };
                XElement requiredElement = constructSecondaryElement(xelement, topProcesses, topTitles, secondTitles);
                string secFileName = createSecondaryFileIfNotExists((string)xelement.Attribute("date"));
                XElement finalElement = loadSecFile(secFileName);
                XElement entries = finalElement.Element("entries");
                entries.Add(requiredElement);
                saveXmlFile(finalElement, secFileName);
                this.SecondaryOutputFile = secFileName;
                FinalOutputDir = GetUserApplicationDirectory() + "\\" + finalOutputFolderName;
                createDirectoryIfNotExists(FinalOutputDir);
                this.FinalizeResults();
            }
        }

        /// <summary>
        /// load temp files from xml
        /// </summary>
        /// <param name="tempFile"></param>
        /// <returns></returns>
        private static XElement loadTempFile(string tempFile)
        {
            XElement temp = null;
            while (true)
            {
                try
                {
                    temp = XElement.Load(tempFile);
                    break;
                }
                catch (System.Xml.XmlException)
                {
                    try
                    {
                        System.IO.File.Delete(tempFile);
                    }
                    catch (Exception) { }
                    string date = tempFile.Split('\\')[tempFile.Split('\\').Length - 1].Split(' ')[0].Replace("_", ":");
                    string time = tempFile.Split('\\')[tempFile.Split('\\').Length - 1].Split(' ')[1].Replace("_", ":");
                    createTempFileIfNotExists(tempFile, date, time);
                }
                catch (IOException)
                {
                }
            }
            return temp;
        }

        /// <summary>
        /// load secondary files from xml
        /// </summary>
        /// <param name="secFile"></param>
        /// <returns></returns>
        private static XElement loadSecFile(string secFile)
        {
            XElement sec = null;
            while (true)
            {
                try
                {
                    sec = XElement.Load(secFile);
                    break;
                }
                catch (System.Xml.XmlException)
                {
                    try
                    {
                        System.IO.File.Delete(secFile);
                    }
                    catch(Exception) { }
                    string time = secFile.Split('\\')[secFile.Split('\\').Length - 1].Split(' ')[0];
                    createSecondaryFileIfNotExists(time);
                }
                catch (IOException)
                {
                }
            }
            return sec;
        }

        /// <summary>
        /// load final files from xml
        /// </summary>
        /// <param name="finalFile"></param>
        /// <returns></returns>
        private static XElement loadFinalFile(string finalFile)
        {
            XElement final = null;
            while (true)
            {
                try
                {
                    final = XElement.Load(finalFile);
                    break;
                }
                catch (System.Xml.XmlException)
                {
                    try
                    {
                        System.IO.File.Delete(finalFile);
                    }
                    catch (Exception) { }
                    string time = finalFile.Split('\\')[finalFile.Split('\\').Length - 1].Split(' ')[0];
                    createSecondaryFileIfNotExists(finalFile);
                }
                catch (IOException)
                {
                }
            }
            return final;
        }

        /// <summary>
        /// load process statistics files from xml
        /// </summary>
        /// <param name="statFile"></param>
        /// <returns></returns>
        private static XElement loadProcessStatFile(string statFile)
        {
            XElement stat = null;
            while (true)
            {
                try
                {
                    stat = XElement.Load(statFile);
                    break;
                }
                catch (System.Xml.XmlException)
                {
                    try
                    {
                        System.IO.File.Delete(statFile);
                    }
                    catch (Exception) { }
                    string time = statFile.Split('\\')[statFile.Split('\\').Length - 1].Split(' ')[0];
                    createProcessStatsFileIfNotExists(statFile, time);
                }
                catch (IOException)
                {
                }
            }
            return stat;
        }

        /// <summary>
        /// finalize results after combining all data from temp and secondary xmls
        /// </summary>
        public void FinalizeResults()
        {
            XElement secElement = loadSecFile(this.SecondaryOutputFile);
            List<XElement> entries = (from entry in secElement.Element("entries").Elements("entry")
                                      select entry).ToList();
            XElement e = entries[entries.Count - 1];
            string finalFileName = createFinalFileIfNotExists((string)secElement.Attribute("date"));
            if ((string)secElement.Attribute("date") == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                lock (currentFinalDateFileAccess)
                {
                    finalizeResults(finalFileName, e);
                }
            }
            else
            {
                finalizeResults(finalFileName, e);
            }
        }

        /// <summary>
        /// same as previous method
        /// </summary>
        /// <param name="finalFileName"></param>
        /// <param name="e"></param>
        private void finalizeResults(string finalFileName, XElement e)
        {
            XElement finElement = loadFinalFile(finalFileName);
            List<XElement> entriesFinal = (from entry in finElement.Element("entries").Elements("entry")
                                           select entry).ToList();
            if (entriesFinal == null || entriesFinal.Count == 0)
            {
                addToFinalEntry(e, finElement.Element("entries"));
            }
            else
            {
                bool continuous = checkIfContinuousTime(entriesFinal[entriesFinal.Count - 1], e);
                if (continuous == false)
                {
                    addToFinalEntry(e, finElement.Element("entries"));
                }
                else
                {
                    bool mergeable = areMergeAble(e, entriesFinal[entriesFinal.Count - 1]);
                    if (mergeable)
                    {
                        changeFinalTime(entriesFinal[entriesFinal.Count - 1], e);
                        mergeElements(entriesFinal[entriesFinal.Count - 1], e);
                    }
                    else
                    {
                        addToFinalEntry(e, finElement.Element("entries"));
                    }
                }
            }
            saveXmlFile(finElement, finalFileName);
        }

        private void changeFinalTime(XElement xelement, XElement e)
        {
            string startTime = (string)xelement.Attribute("startTime");
            xelement.RemoveAttributes();
            xelement.Add(new XAttribute("startTime", startTime));
            xelement.Add(new XAttribute("endTime", (string)e.Attribute("endTime")));
        }

        /// <summary>
        /// merge elements of secondary file to get one combined time range in final file
        /// </summary>
        /// <param name="xelement"></param>
        /// <param name="e"></param>
        private void mergeElements(XElement xelement, XElement e)
        {
            List<XElement> processes = (from process in e.Elements("process")
                                        select process).ToList();
            foreach (XElement process in processes)
            {
                List<XElement> mainProcesses = (from mainprocess in xelement.Elements("process")
                                                select mainprocess).ToList();
                bool noProcessMatch = true;
                foreach (XElement mainProcess in mainProcesses)
                {
                    if ((string)mainProcess.Attribute("processName") == (string)process.Attribute("processName"))
                    {
                        string processName = (string)mainProcess.Attribute("processName");
                        string processPeriod = (string)mainProcess.Attribute("period");
                        TimeSpan period = TimeSpan.ParseExact((string)process.Attribute("period"), "g", culture);
                        TimeSpan mainPeriod = period.Add(TimeSpan.ParseExact(processPeriod, "g", culture));
                        mainProcess.RemoveAttributes();
                        mainProcess.Add(new XAttribute("processName", processName));
                        mainProcess.Add(new XAttribute("period", mainPeriod.ToString()));
                        noProcessMatch = false;
                        List<XElement> titles = (from title in process.Elements("record")
                                                 select title).ToList();
                        foreach (XElement title in titles)
                        {
                            List<XElement> mainTitles = (from mainTitle in mainProcess.Elements("record")
                                                         select mainTitle).ToList();
                            bool noTitleMatch = true;
                            foreach (XElement mainTitle in mainTitles)
                            {
                                if ((string)mainTitle.Attribute("title") == (string)title.Attribute("title"))
                                {
                                    noTitleMatch = false;
                                    TimeSpan titlePeriod = TimeSpan.ParseExact((string)title.Attribute("period"), "g", culture);
                                    TimeSpan mainTitlePeriod = titlePeriod.Add(TimeSpan.ParseExact((string)mainTitle.Attribute("period"), "g", culture));
                                    string titleName = (string)mainTitle.Attribute("title");
                                    mainTitle.RemoveAttributes();
                                    mainTitle.Add(new XAttribute("title", titleName));
                                    mainTitle.Add(new XAttribute("period", mainTitlePeriod.ToString()));
                                }
                            }
                            if (noTitleMatch)
                            {
                                XElement mainTitle = new XElement(title);
                                mainProcess.Add(mainTitle);
                            }
                        }
                        keepTwo(mainProcess);
                    }
                }
                if (noProcessMatch)
                {
                    string processName = (string)process.Attribute("processName");
                    XElement copyProcess = new XElement(process);
                    xelement.Add(copyProcess);
                }
            }
            keepTwo(xelement);
        }

        /// <summary>
        /// keep top two items in the given xelement accodring to period
        /// </summary>
        /// <param name="xelement"></param>
        private void keepTwo(XElement xelement)
        {
            XElement[] topTwo = getTopTwoTimes(xelement, "entry", false);
            if (topTwo[0] == null)
            {
                return;
            }
            else if (topTwo[1] == null)
            {
                xelement.RemoveNodes();
                xelement.Add(new XElement(topTwo[0]));
            }
            else
            {
                xelement.RemoveNodes();
                xelement.Add(topTwo[0]);
                xelement.Add(topTwo[1]);
            }
        }

        /// <summary>
        /// checks if 2 elements are mergable i.e. if they have common title/process etc or not
        /// </summary>
        /// <param name="e"></param>
        /// <param name="xelement"></param>
        /// <returns></returns>
        private bool areMergeAble(XElement e, XElement xelement)
        {
            List<XElement> processes = (from process in e.Elements("process")
                                        select process).ToList();
            foreach (XElement process in processes)
            {
                List<XElement> mainProcesses = (from mainprocess in xelement.Elements("process")
                                                select mainprocess).ToList();
                foreach (XElement mainProcess in mainProcesses)
                {
                    if ((string)mainProcess.Attribute("processName") == (string)process.Attribute("processName"))
                    {
                        List<XElement> titles = (from title in process.Elements("record")
                                                 select title).ToList();
                        foreach (XElement title in titles)
                        {
                            List<XElement> mainTitles = (from mainTitle in mainProcess.Elements("record")
                                                         select mainTitle).ToList();
                            foreach (XElement mainTitle in mainTitles)
                            {
                                if ((string)mainTitle.Attribute("title") == (string)title.Attribute("title"))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// checks if 2 elements are continuos or not
        /// </summary>
        /// <param name="xelement"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool checkIfContinuousTime(XElement xelement, XElement e)
        {
            if ((string)xelement.Attribute("endTime") == (string)e.Attribute("startTime"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// adds element to an entry
        /// </summary>
        /// <param name="e"></param>
        /// <param name="xelement"></param>
        private void addToFinalEntry(XElement e, XElement xelement)
        {
            XElement newAdd = new XElement(e);
            xelement.Add(newAdd);
        }

        /// <summary>
        /// creates final file if not exists
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static string createFinalFileIfNotExists(string date)
        {
            string fileName = FinalOutputDir + "\\" + date;
            createDirectoryIfNotExists(FinalOutputDir);
            bool isExists = System.IO.File.Exists(fileName);
            if (isExists)
            {
                return fileName;
            }
            XElement emptyfileElement = new XElement("log",
                new XAttribute("date",
                    date),
                new XElement("entries"));
            saveXmlFile(emptyfileElement, fileName);
            return fileName;
        }

        /// <summary>
        /// creates secondary fine if it does not exist
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static string createSecondaryFileIfNotExists(string date)
        {
            string fileName = SecondaryOutputDirectory + "\\" + date;
            createDirectoryIfNotExists(SecondaryOutputDirectory);
            bool isExists = System.IO.File.Exists(fileName);
            if (isExists)
            {
                return fileName;
            }
            XElement emptyfileElement = new XElement("quicklog",
                new XAttribute("date",
                    date),
                new XElement("entries"));
            saveXmlFile(emptyfileElement, fileName);
            return fileName;
        }

        /// <summary>
        /// constructs secondary element out of process and titles information
        /// </summary>
        /// <param name="xelement"></param>
        /// <param name="topProcesses"></param>
        /// <param name="topTitles"></param>
        /// <param name="secondTitles"></param>
        /// <returns></returns>
        private static XElement constructSecondaryElement(XElement xelement, XElement[] topProcesses, XElement[] topTitles, XElement[] secondTitles)
        {
            string startTime = (string)xelement.Attribute("time");
            string endTime = addHalfHour(startTime);
            string process1 = topProcesses[0] != null ? (string)topProcesses[0].Attribute("processName") : null;
            string period1 = topProcesses[0] != null ? (string)topProcesses[0].Attribute("period") : null;
            string process2 = topProcesses[1] != null ? (string)topProcesses[1].Attribute("processName") : null;
            string period2 = topProcesses[1] != null ? (string)topProcesses[1].Attribute("period") : null;
            XElement secElement = new XElement("entry",
                new XAttribute("startTime", startTime),
                new XAttribute("endTime", endTime));
            if (topProcesses[0] != null)
            {
                XElement topElement = makeProcessElement(process1, period1, topTitles);
                secElement.Add(topElement);
            }
            if (topProcesses[1] != null)
            {
                XElement secondElement = makeProcessElement(process2, period2, secondTitles);
                secElement.Add(secondElement);
            }
            return secElement;
        }

        /// <summary>
        /// makes process element out of process name and titles
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="period"></param>
        /// <param name="topTitles"></param>
        /// <returns></returns>
        private static XElement makeProcessElement(string processName, string period, XElement[] topTitles)
        {
            XElement secElement = new XElement("process",
                new XAttribute("processName", processName),
                new XAttribute("period", period));
            if (topTitles[0] != null)
            {
                secElement.Add(new XElement(topTitles[0]));
            }
            if (topTitles[1] != null)
            {
                secElement.Add(new XElement(topTitles[1]));
            }
            return secElement;
        }

        /// <summary>
        /// adds half hour to given time string
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        private static string addHalfHour(string startTime)
        {
            string[] times = startTime.Split(':');
            int hour = int.Parse(times[0]);
            if (times[1] == "00")
            {
                return times[0] + ":30";
            }
            else
            {
                if (hour + 1 < 10)
                {
                    return "0" + (int.Parse(times[0]) + 1).ToString() + ":00";
                }
                if (hour + 1 == 24)
                {
                    return "1.0:00";
                }
                return (int.Parse(times[0]) + 1).ToString() + ":00";
            }
        }

        /// <summary>
        /// gets the top two times in the element
        /// </summary>
        /// <param name="xelement"></param>
        /// <param name="entry"></param>
        /// <param name="changed"></param>
        /// <returns></returns>
        private XElement[] getTopTwoTimes(XElement xelement, string entry = "entry", bool changed = true)
        {
            List<XElement> processes = (from record in xelement.Elements()
                                        select record).ToList();
            XElement[] topProcesses = new XElement[2] { null, null };
            double[] topTimes = new double[2] { 0, 0 };
            foreach (XElement process in processes)
            {
                string period = (string)process.Attribute("period");
                string topPeriod = topProcesses[0] == null ? "00:00:00.00" : (string)topProcesses[0].Attribute("period");
                string secondPeriod = topProcesses[1] == null ? "00:00:00.00" : (string)topProcesses[1].Attribute("period");
                if (topProcesses[0] == null || (TimeSpan.ParseExact(period, "g", culture) >= TimeSpan.ParseExact(topPeriod, "g", culture)))
                {
                    topProcesses[1] = topProcesses[0];
                    topTimes[1] = topTimes[0];
                    topProcesses[0] = process;
                    topTimes[0] = TimeSpan.ParseExact(period, "g", culture).TotalSeconds;
                }
                else if (topProcesses[1] == null || (TimeSpan.ParseExact(period, "g", culture) >= TimeSpan.ParseExact(secondPeriod, "g", culture)))
                {
                    topProcesses[1] = process;
                    topTimes[1] = TimeSpan.ParseExact(period, "g", culture).TotalSeconds;
                }
            }
            List<XElement> backupTop = new List<XElement> { topProcesses[0], topProcesses[1] };
            if (changed == true && (topTimes[0] > rejectionRatioInv * topTimes[1] || topTimes[1] < minTimePerHalfHour))
            {
                topProcesses[1] = null;
            }
            if (topTimes[0] < minTimePerHalfHour)
            {
                topProcesses[0] = null;
            }
            if (entry == "record" && (topProcesses[0] == null))
            {
                string switchString = "Switched windows - ";
                if (backupTop[0] != null)
                {
                    switchString = switchString + "[" + (string)backupTop[0].Attribute("title");
                    if (backupTop[1] != null)
                    {
                        switchString = switchString + "; " + (string)backupTop[1].Attribute("title");
                    }
                    switchString += " etc.]";
                }
                topProcesses[0] = new XElement("record",
                    new XAttribute("title", switchString),
                    new XAttribute("period", (string)xelement.Attribute("period")));
            }
            return topProcesses;
        }

        /// <summary>
        /// checks if the temp file is processed or not
        /// </summary>
        /// <param name="tempFile"></param>
        /// <returns></returns>
        private bool isTempProcessed(string tempFile)
        {
            XElement xelement = loadTempFile(tempFile);
            string secFileName = createSecondaryFileIfNotExists((string)xelement.Attribute("date"));
            string time = (string)xelement.Attribute("time");
            XElement secElem = loadSecFile(secFileName);
            List<XElement> elems = (from entry in secElem.Element("entries").Elements("entry")
                                    where (string)entry.Attribute("startTime") == time
                                    select entry).ToList();
            if (elems.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// gets the tempdirectory name
        /// </summary>
        /// <returns></returns>
        private static string getTempDirectory()
        {
            string tempDir = GetUserApplicationDirectory() + "\\" + tempFolderName;
            createDirectoryIfNotExists(tempDir);
            return tempDir;
        }

        /// <summary>
        /// gets secondary directory name
        /// </summary>
        /// <returns></returns>
        private string getSecondaryOutputDirectory()
        {
            string secDir = GetUserApplicationDirectory() + "\\" + secondaryTempFolderName;
            createDirectoryIfNotExists(secDir);
            return secDir;
        }

        /// <summary>
        /// log the screen change for the sent process title and duration
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="processTitle"></param>
        /// <param name="duration"></param>
        public void SendScreenChange(string processName, string processTitle, TimeSpan duration)
        {
            this.TempFile = this.TempFile == null ? GetCurrentTempFileName() : this.TempFile;
            XElement xelement = null;
            xelement = loadTempFile(this.TempFile);
            Console.WriteLine(xelement.ToString() + "\n\n");
            List<XElement> processes = (from entry in xelement.Element("entries").Elements("entry")
                                        where (string)entry.Attribute("processName") == processName
                                        select entry).ToList();
            //Console.WriteLine(name.ToString() + "\n\n");
            if (processes.Count == 0)
            {
                XElement newProcess = new XElement("entry",
                    new XAttribute("processName", processName),
                    new XAttribute("period", duration.ToString()),
                    new XElement("record",
                        new XAttribute("title", processTitle),
                        new XAttribute("period", duration.ToString())));
                xelement.Element("entries").Add(newProcess);
            }
            else
            {
                XElement process = processes[0];
                string period = (string)process.Attribute("period");
                TimeSpan newSpan = duration.Add(TimeSpan.ParseExact(period, "g", culture));
                process.SetAttributeValue("period", newSpan.ToString());
                List<XElement> titles = (from record in process.Elements("record")
                                         where (string)record.Attribute("title") == processTitle
                                         select record).ToList();
                if (titles.Count == 0)
                {
                    XElement newTitle = new XElement("record",
                            new XAttribute("title", processTitle),
                            new XAttribute("period", duration.ToString()));
                    processes[0].Add(newTitle);
                }
                else
                {
                    XElement title = titles[0];
                    string titlePeriod = (string)title.Attribute("period");
                    TimeSpan newTitleSpan = duration.Add(TimeSpan.ParseExact(titlePeriod, "g", culture));
                    title.SetAttributeValue("period", newTitleSpan.ToString());
                }
            }
            if (processTitle == "")
            {
                return;
            }
            lock (currentProcessDateFileAccess)
            {
                addToProcessStatistics(processName, duration);
            }
            saveXmlFile(xelement, this.TempFile);
        }

        /// <summary>
        /// add process statics for the given process on date
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="duration"></param>
        /// <param name="date"></param>
        private static void addToProcessStatistics(string processName, TimeSpan duration, string date = null)
        {
            date = date == null ? DateTime.Now.ToString("yyyy-MM-dd") : date;
            string folder = GetUserApplicationDirectory() + "\\" + processStatsFolderName;
            string fileName = GetUserApplicationDirectory() + "\\" + processStatsFolderName + "\\" + date;
            createDirectoryIfNotExists(folder);
            createProcessStatsFileIfNotExists(fileName, DateTime.Now.ToString("yyyy-MM-dd"));
            XElement file = null;
            file = loadProcessStatFile(fileName);
            List<XElement> processes = (from record in file.Elements()
                                        where (string)record.Attribute("name") == processName
                                        select record).ToList();
            if (processes.Count == 0)
            {
                XElement process = new XElement("process",
                    new XAttribute("name", processName),
                    new XAttribute("period", duration.ToString()));
                file.Add(process);
                saveXmlFile(file, fileName);
                return;
            }
            string period = (string)processes[0].Attribute("period");
            TimeSpan newSpan = duration.Add(TimeSpan.ParseExact(period, "g", culture));
            XElement process2 = new XElement("process",
                    new XAttribute("name", processName),
                    new XAttribute("period", newSpan.ToString()));
            file.Add(process2);
            processes[0].Remove();
            saveXmlFile(file, fileName);
            return;
        }

        /// <summary>
        /// creates process statistics file if it does not exist
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static string createProcessStatsFileIfNotExists(string fileName, string dateTime)
        {
            bool isExists = System.IO.File.Exists(fileName);
            if (isExists)
            {
                return fileName;
            }
            XElement emptyfileElement = new XElement("processesStats",
                new XAttribute("date",
                    dateTime));
            saveXmlFile(emptyfileElement, fileName);
            return fileName;
        }

        /// <summary>
        /// get the current temporary file if it does not exist
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentTempFileName()
        {
            string applicationDirectory = GetUserApplicationDirectory();
            createDirectoryIfNotExists(applicationDirectory + "\\" + tempFolderName);
            DateTime now = DateTime.Now, newDateTime;
            newDateTime = now.Minute < 30 ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0) :
                            new DateTime(now.Year, now.Month, now.Day, now.Hour, 30, 0);
            string tempFileName = applicationDirectory + "\\" + tempFolderName + "\\" + newDateTime.ToString("yyyy-MM-dd HH_mm");
            createTempFileIfNotExists(tempFileName, newDateTime.ToString("yyyy-MM-dd"), newDateTime.ToString("hh:mm"));
            return tempFileName;
        }
        
        /// <summary>
        /// creates temporary file if it does not exist
        /// </summary>
        /// <param name="tempFileName"></param>
        /// <param name="date"></param>
        /// <param name="time"></param>
        private static void createTempFileIfNotExists(string tempFileName, string date, string time)
        {
            bool isExists = System.IO.File.Exists(tempFileName);
            if (isExists)
            {
                return;
            }
            XElement emptyfileElement = new XElement("catalog",
                new XAttribute("date",
                    date),
                new XAttribute("time",
                    time),
                new XElement("entries"));
            saveXmlFile(emptyfileElement, tempFileName);
        }

        /// <summary>
        /// creates a directory if it does not exist
        /// </summary>
        /// <param name="folder"></param>
        private static void createDirectoryIfNotExists(string folder)
        {
            bool isExists = System.IO.Directory.Exists(folder);

            if (!isExists)
                System.IO.Directory.CreateDirectory(folder);
        }

        /// <summary>
        /// gets the user's application directory
        /// </summary>
        /// <returns></returns>
        public static string GetUserApplicationDirectory()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            return key.GetValue("Path") as string;
        }

        /// <summary>
        /// returns a list of all activities(processes and titles) for a given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static List<DayFinalActivities.FinalActivities> getFinalActivities(DateTime date)
        {
            List<DayFinalActivities.FinalActivities> finalActivities = new List<DayFinalActivities.FinalActivities>();
            string fileName = GetUserApplicationDirectory() + "\\" + "Final\\" + date.ToString("yyyy-MM-dd");
            if (System.IO.File.Exists(fileName) == false)
            {
                return finalActivities;
            }
            XElement final = loadFinalFile(fileName);
            List<XElement> entries = (from entry in final.Element("entries").Elements()
                                      select entry).ToList();
            foreach (var entry in entries)
            {
                List<XElement> processes = (from process in entry.Elements()
                                            select process).ToList();
                if (processes.Count == 0)
                {
                    continue;
                }
                DayFinalActivities.FinalActivities newEntry = new DayFinalActivities.FinalActivities();
                newEntry.StartTime = TimeSpan.ParseExact((string)entry.Attribute("startTime"), "g", culture);
                if ((string)entry.Attribute("endTime") == "1.0:00")
                {
                    newEntry.EndTime = TimeSpan.Parse("1.0:00");
                }
                else
                {
                    newEntry.EndTime = TimeSpan.ParseExact((string)entry.Attribute("endTime"), "g", culture);
                }
                newEntry.Processes = new List<DayFinalActivities.FinalActivities.Process>();
                foreach (var process in processes)
                {
                    DayFinalActivities.FinalActivities.Process p = new DayFinalActivities.FinalActivities.Process();
                    p.Name = (string)process.Attribute("processName");
                    p.Titles = new List<DayFinalActivities.FinalActivities.Process.Title>();
                    TimeSpan duration = TimeSpan.ParseExact((string)process.Attribute("period"), "g", culture);
                    List<XElement> records = (from record in process.Elements()
                                              select record).ToList();
                    p.Duration = duration;
                    foreach (var record in records)
                    {
                        TimeSpan dur = TimeSpan.ParseExact((string)record.Attribute("period"), "g", culture);
                        p.Titles.Add(new DayFinalActivities.FinalActivities.Process.Title((string)record.Attribute("title"), dur));
                    }
                    newEntry.Processes.Add(p);
                }
                finalActivities.Add(newEntry);
            }
            return finalActivities;
        }

        /// <summary>
        /// deletes time entry for given date
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public static void DeleteTimeEntry(string date, string startTime, string endTime)
        {
            endTime = endTime == "00:00" ? "1.0:00" : endTime;
            XElement entry;
            if (date == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                lock (currentFinalDateFileAccess)
                {
                    entry = deleteTimeEntry(date, startTime, endTime);
                }
            }
            else
            {
                entry = deleteTimeEntry(date, startTime, endTime);
            }
            if (entry == null)
            {
                return;
            }
            List<XElement> processes = (from process in entry.Elements()
                                  select process).ToList();
            foreach (var process in processes)
            {
                deleteProcessStatEntry(date, process);
            }

        }

        /// <summary>
        /// deletes title entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="processName"></param>
        /// <param name="title"></param>
        public static void DeleteTitleEntry(string date, string startTime, string endTime, string processName, string title)
        {
            endTime = endTime == "00:00" ? "1.0:00" : endTime;
            XElement t;
            if (date == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                lock (currentFinalDateFileAccess)
                {
                    t = deleteTitleEntry(date, startTime, endTime, processName, title);
                }
            }
            else
            {
                t = deleteTitleEntry(date, startTime, endTime, processName, title);
            }
            if (t == null)
            {
                return;
            }
            deleteTitleStatEntry(date, t, processName);
        }

        /// <summary>
        /// deletes process entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="processName"></param>
        public static void DeleteProcessEntry(string date, string startTime, string endTime, string processName)
        {
            endTime = endTime == "00:00" ? "1.0:00" : endTime;
            XElement process;
            if (date == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                lock (currentFinalDateFileAccess)
                {
                    process = deleteProcessEntry(date, startTime, endTime, processName);
                }
            }
            else
            {
                process = deleteProcessEntry(date, startTime, endTime, processName);
            }
            if (process == null)
            {
                return;
            }
            deleteProcessStatEntry(date, process);
        }

        /// <summary>
        /// deletes title stats entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="title"></param>
        /// <param name="processName"></param>
        private static void deleteTitleStatEntry(string date, XElement title, string processName)
        {
            if (date == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                lock (currentProcessDateFileAccess)
                {
                    try
                    {
                        XElement file = loadProcessStatFile(GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                        deleteProcessStatEntry(file, title, processName);
                        saveXmlFile(file, GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                    }
                    catch (Exception) { }
                }
            }
            else
            {
                try
                {
                    XElement file = loadProcessStatFile(GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                    deleteProcessStatEntry(file, title, processName);
                    saveXmlFile(file, GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// deletes title entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="processName"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        private static XElement deleteTitleEntry(string date, string startTime, string endTime, string processName, string title)
        {
            deleteTempTitleEntry(date, startTime, endTime, processName, title);
            XElement file = loadFinalFile(GetUserApplicationDirectory() + "\\Final\\" + date);
            List<XElement> entries = (from entry in file.Element("entries").Elements()
                                      where (string)entry.Attribute("startTime") == startTime && (string)entry.Attribute("endTime") == endTime
                                      select entry).ToList();
            if (entries.Count == 0 || entries.Count > 1)
            {
                return null;
            }
            List<XElement> processes = (from process in entries[0].Elements()
                                        where (string)process.Attribute("processName") == processName
                                        select process).ToList();
            if (processes.Count == 0 || processes.Count > 1)
            {
                return null;
            }
            List<XElement> titles = null;
            if (title.Contains("Switched windows [") == false)
            {
                titles = (from t in processes[0].Elements()
                          where (string)t.Attribute("title") == title
                          select t).ToList();
            }
            else
            {
                DeleteProcessEntry(date, startTime, endTime, processName);
                return null;
            }
            if (titles.Count == 0 || titles.Count > 1)
            {
                return null;
            }
            List<XElement> allTitles = (from t in processes[0].Elements()
                                        select t).ToList();
            if (allTitles.Count == 1)
            {
                processes[0].Remove();
                saveXmlFile(file, GetUserApplicationDirectory() + "\\Final\\" + date);
                return titles[0];
            }
            else
            {
                string pName = (string)processes[0].Attribute("processName");
                string pperiod = (string)processes[0].Attribute("period");
                string tperiod = (string)titles[0].Attribute("period");
                TimeSpan totalPeriod =  TimeSpan.ParseExact(pperiod, "g", culture);
                TimeSpan titlePeriod = TimeSpan.ParseExact(tperiod, "g", culture);
                string newPeriod = (totalPeriod - titlePeriod).ToString();
                processes[0].RemoveAttributes();
                processes[0].Add(new XAttribute("processName", pName));
                processes[0].Add(new XAttribute("period", newPeriod));
                titles[0].Remove();
                saveXmlFile(file, GetUserApplicationDirectory() + "\\Final\\" + date);
                return titles[0];
            }
        }

        /// <summary>
        /// deletes process entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="processName"></param>
        /// <returns></returns>
        private static XElement deleteProcessEntry(string date, string startTime, string endTime, string processName)
        {
            deleteTempFileProcessEntry(date, startTime, endTime, processName);
            XElement file = loadFinalFile(GetUserApplicationDirectory() + "\\Final\\" + date);
            List<XElement> entries = (from entry in file.Element("entries").Elements()
                                      where (string)entry.Attribute("startTime") == startTime && (string)entry.Attribute("endTime") == endTime
                                      select entry).ToList();
            if (entries.Count == 0 || entries.Count > 1)
            {
                return null;
            }
            List<XElement> processes = (from process in entries[0].Elements()
                                        where (string)process.Attribute("processName") == processName
                                        select process).ToList();
            if (processes.Count == 0 || processes.Count > 1)
            {
                return null;
            }
            processes[0].Remove();
            saveXmlFile(file, GetUserApplicationDirectory() + "\\Final\\" + date);
            return processes[0];
        }

        /// <summary>
        /// deletes process stat entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="process"></param>
        private static void deleteProcessStatEntry(string date, XElement process)
        {
            if (date == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                lock (currentProcessDateFileAccess)
                {
                    try
                    {
                        XElement file = loadProcessStatFile(GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                        deleteProcessStatEntry(file, process, (string)process.Attribute("processName"));
                        saveXmlFile(file, GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                    }
                    catch (Exception) { }
                }
            }
            else
            {
                try
                {
                    XElement file = loadProcessStatFile(GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                    deleteProcessStatEntry(file, process, (string)process.Attribute("processName"));
                    saveXmlFile(file, GetUserApplicationDirectory() + "\\ProcessStats\\" + date);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// deletes processstat entry
        /// </summary>
        /// <param name="file"></param>
        /// <param name="mainItem"></param>
        /// <param name="processName"></param>
        private static void deleteProcessStatEntry(XElement file, XElement mainItem, string processName)
        {
            TimeSpan processPeriod = TimeSpan.ParseExact((string)mainItem.Attribute("period"), "g", culture);
            List<XElement> processStats = (from p in file.Elements()
                                          where (string)p.Attribute("name") == processName
                                          select p).ToList();
            if (processStats.Count == 0 || processStats.Count > 1)
            {
                return;
            }
            TimeSpan totalPeriod = TimeSpan.ParseExact((string)processStats[0].Attribute("period"), "g", culture);
            TimeSpan newPeriod = totalPeriod - processPeriod;
            processStats[0].Remove();
            file.Add(new XElement("process",
                new XAttribute("name", processName),
                new XAttribute("period", newPeriod.ToString())));
        }

        /// <summary>
        /// deletes time entry
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        private static XElement deleteTimeEntry(string date, string startTime, string endTime)
        {
            deleteTempFileEntries(date, startTime, endTime);
            XElement file = loadFinalFile(GetUserApplicationDirectory() + "\\Final\\" + date);
            List<XElement> entries = (from entry in file.Element("entries").Elements()
                                      where (string)entry.Attribute("startTime") == startTime && (string)entry.Attribute("endTime") == endTime
                                      select entry).ToList();
            if (entries.Count == 0 || entries.Count > 1)
            {
                return null;
            }
            entries[0].Remove();
            saveXmlFile(file, GetUserApplicationDirectory() + "\\Final\\" + date);
            return entries[0];
        }

        /// <summary>
        /// deletes temp files entries
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private static void deleteTempFileEntries(string date, string startTime, string endTime)
        {
            string dir = GetUserApplicationDirectory() + "\\Temp";
            string[] files = System.IO.Directory.GetFiles(dir);
            foreach (string f in files)
            {
                string fileName = f.Split('\\')[f.Split('\\').Length - 1];
                string d = fileName.Split(' ')[0];
                string s = fileName.Split(' ')[1].Replace("_", ":");
                if (lessThan(s, endTime) && (lessThan(startTime, s) || s == startTime))
                {
                    try
                    {
                        System.IO.File.Delete(f);
                        return;
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        ///  deletes process entries from temp files
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="processName"></param>
        private static void deleteTempFileProcessEntry(string date, string startTime, string endTime, string processName)
        {
            string dir = GetUserApplicationDirectory() + "\\Temp";
            string[] files = System.IO.Directory.GetFiles(dir);
            foreach (string f in files)
            {
                string fileName = f.Split('\\')[f.Split('\\').Length - 1];
                string d = fileName.Split(' ')[0];
                string s = fileName.Split(' ')[1].Replace("_", ":");
                if (lessThan(s, endTime) && (lessThan(startTime, s) || s == startTime))
                {
                    XElement file = loadTempFile(f);
                    List<XElement> l = (from p in file.Element("entries").Elements()
                                        where (string)p.Attribute("processName") == processName
                                        select p).ToList();
                    foreach (var p in l)
                    {
                        p.Remove();
                    }
                    saveXmlFile(file, f);
                }
            }
        }

        /// <summary>
        /// saves given xml element in the file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="f"></param>
        private static void saveXmlFile(XElement file, string f)
        {
            while (true)
            {
                try
                {
                    file.Save(f);
                    break;
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// delete title entry from temp files
        /// </summary>
        /// <param name="date"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="processName"></param>
        /// <param name="title"></param>
        private static void deleteTempTitleEntry(string date, string startTime, string endTime, string processName, string title)
        {
            string dir = GetUserApplicationDirectory() + "\\Temp";
            string[] files = System.IO.Directory.GetFiles(dir);
            foreach (string f in files)
            {
                string fileName = f.Split('\\')[f.Split('\\').Length - 1];
                string d = fileName.Split(' ')[0];
                string s = fileName.Split(' ')[1].Replace("_", ":");
                if (lessThan(s, endTime) && (lessThan(startTime, s) || s == startTime))
                {
                    XElement file = loadTempFile(f);
                    List<XElement> l = (from p in file.Element("entries").Elements()
                                        where (string)p.Attribute("processName") == processName
                                        select p).ToList();
                    foreach (var p in l)
                    {
                        List<XElement> lt = (from t in p.Elements()
                                             where (string)t.Attribute("title") == title
                                             select t).ToList();
                        string pName = (string)p.Attribute("processName");
                        string processPeriod = (string)p.Attribute("period");
                        foreach (var t in lt)
                        {
                            TimeSpan pp = TimeSpan.ParseExact(processPeriod, "g", culture);
                            TimeSpan tt = TimeSpan.ParseExact((string)t.Attribute("period"), "g", culture);
                            TimeSpan final = pp.Subtract(tt);
                            t.Remove();
                            p.RemoveAttributes();
                            p.Add(new XAttribute("processName", pName), new XAttribute("period", final.ToString()));
                        }
                    }
                    saveXmlFile(file, f);
                }
            }
        }

        /// <summary>
        /// checks if time given as s1 is less than s2
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        private static bool lessThan(string s1, string s2)
        {
            int hrs1 = Convert.ToInt32(s1.Split(':')[0]);
            int hrs2 = Convert.ToInt32(s2.Split(':')[0]);
            int min1 = Convert.ToInt32(s1.Split(':')[1]);
            int min2 = Convert.ToInt32(s2.Split(':')[1]);
            if (hrs1 < hrs2)
            {
                return true;
            }
            if (hrs1 > hrs2)
            {
                return false;
            }
            if (min1 < min2)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// creates configuration file if it does not exist
        /// </summary>
        public static void CreateConfigFileIfNotExists()
        {
            string cfile = GetUserApplicationDirectory() + "\\" + configFile;
            bool isExists = System.IO.File.Exists(cfile);
            if (isExists)
            {
                return;
            }
            XElement emptyfileElement = new XElement("config");
            saveXmlFile(emptyfileElement, cfile);
            return;
        }

        /// <summary>
        /// sets configuration entry
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="value"></param>
        public static void SetConfigEntry(string entry, string value)
        {
            string cfile = GetUserApplicationDirectory() + "\\" + configFile;
            CreateConfigFileIfNotExists();
            XElement main = XElement.Load(cfile);
            List<XElement> elems = (from e in main.Elements()
                                    where e.Name == entry
                                    select e).ToList();
            if (elems.Count == 0)
            {
                XElement e = new XElement(entry, new XAttribute("value", value));
                main.Add(e);
                saveXmlFile(main, cfile);
                return;
            }
            elems[0].RemoveAttributes();
            elems[0].Add(new XAttribute("value", value));
            saveXmlFile(main, cfile);
            return;
        }

        /// <summary>
        /// gets the configuration entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static string GetConfigEntry(string entry)
        {
            string cfile = GetUserApplicationDirectory() + "\\" + configFile;
            CreateConfigFileIfNotExists();
            XElement main = XElement.Load(cfile);
            List<XElement> elems = (from e in main.Elements()
                                    where e.Name == entry
                                    select e).ToList();
            if (elems.Count == 0)
            {
                return null;
            }

            return (string)elems[0].Attribute("value");
        }

        /// <summary>
        /// creates downloads file list
        /// </summary>
        public static void createDownloadsFileIfNotExists()
        {
            string dfile = GetUserApplicationDirectory() + "\\" + downloadsFile;
            bool isExists = System.IO.File.Exists(dfile);
            if (isExists)
            {
                return;
            }
            XElement emptyfileElement = new XElement("downloadsList");
            saveXmlFile(emptyfileElement, dfile);
            return;
        }

        /// <summary>
        /// sets download entry in downloads file
        /// </summary>
        /// <param name="entry"></param>
        public static void SetDownloadEntry(string entry)
        {
            string dfile = GetUserApplicationDirectory() + "\\" + downloadsFile;
            createDownloadsFileIfNotExists();
            XElement main = XElement.Load(dfile);
            XElement e = new XElement("downloaded", new XAttribute("file", entry));
            main.Add(e);
            saveXmlFile(main, dfile);
            return;
        }

        /// <summary>
        /// gets entry for a download entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool GetDownloadEntry(string entry)
        {
            string dfile = GetUserApplicationDirectory() + "\\" + downloadsFile;
            createDownloadsFileIfNotExists();
            XElement main = XElement.Load(dfile);
            List<XElement> elems = (from e in main.Elements()
                                    where (string)e.Attribute("file") == entry
                                    select e).ToList();
            if (elems.Count == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// checks if temp file exists or not
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static bool CheckIfTempExists(string title)
        {
            string tempDir = XmlDataLayer.GetUserApplicationDirectory()+"\\Temp";
            foreach (string file in System.IO.Directory.EnumerateFiles(tempDir))
            {
                string f = file.Split('\\')[file.Split('\\').Length-1];
                if (f == title)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// creates uploads file if it does not exist
        /// </summary>
        public static void createUploadsFileIfNotExists()
        {
            string ufile = GetUserApplicationDirectory() + "\\" + uploadsFile;
            bool isExists = System.IO.File.Exists(ufile);
            if (isExists)
            {
                return;
            }
            XElement emptyfileElement = new XElement("uploadsList");
            saveXmlFile(emptyfileElement, ufile);
            return;
        }

        /// <summary>
        /// sets uploads entry for a file
        /// </summary>
        /// <param name="entry"></param>
        public static void SetUploadEntry(string entry)
        {
            string ufile = GetUserApplicationDirectory() + "\\" + uploadsFile;
            createUploadsFileIfNotExists();
            XElement main = XElement.Load(ufile);
            XElement e = new XElement("uploaded", new XAttribute("file", entry));
            main.Add(e);
            saveXmlFile(main, ufile);
            return;
        }

        /// <summary>
        /// gets upload entry for a file
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool GetUploadEntry(string entry)
        {
            string ufile = GetUserApplicationDirectory() + "\\" + uploadsFile;
            createUploadsFileIfNotExists();
            XElement main = XElement.Load(ufile);
            List<XElement> elems = (from e in main.Elements()
                                    where (string)e.Attribute("file") == entry
                                    select e).ToList();
            if (elems.Count == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// gets the statistics for a day
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static List<DayProcessStats.FinalProcessStat> getDayProcessStats(DateTime date)
        {
            List<DayProcessStats.FinalProcessStat> finalProcessStatList = new List<DayProcessStats.FinalProcessStat>();

            string fileName = GetUserApplicationDirectory() + "\\" + "ProcessStats\\" + date.ToString("yyyy-MM-dd");
            if (System.IO.File.Exists(fileName) == false)
            {
                return finalProcessStatList;
            }

            XElement final = loadProcessStatFile(fileName);
            List<XElement> entries = (from entry in final.Elements()
                                      select entry).ToList();
            foreach (var entry in entries)
            {

                finalProcessStatList.Add(new DayProcessStats.FinalProcessStat((string)entry.Attribute("name"), TimeSpan.ParseExact((string)entry.Attribute("period"), "g", culture)));
            }
            return finalProcessStatList;
        }
    }
}
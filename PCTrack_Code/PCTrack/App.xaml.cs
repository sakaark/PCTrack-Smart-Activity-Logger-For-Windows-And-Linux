using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //private string createFolder(string path) 
        //{
        //    bool isExists = System.IO.Directory.Exists(path);
        //    if (!isExists)
        //    {
        //        Directory.CreateDirectory(path);
        //        return path;
        //    }
        //    return createFolder(path+"0");
        //}

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //RegistryKey key;
            //key = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("PCTrack", true);
            //string w = null;
            //try
            //{
            //    w = key.GetValue("Path") as string;
            //}
            //catch { }
            //if (w == null || w == "")
            //{
            //    var dialog = new System.Windows.Forms.FolderBrowserDialog();
            //    dialog.Description = "Choose folder where application data wil be saved.";
            //    string folderPath = "C:\\users\\" + Environment.UserName + "\\Documents\\PCTrack";
            //    dialog.SelectedPath = folderPath;
            //    dialog.RootFolder = Environment.SpecialFolder.MyDocuments;
            //    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            //    string selectedPath = dialog.SelectedPath;
            //    if (selectedPath != folderPath)
            //    {
            //        Directory.Delete(folderPath);
            //    }
            //    key.SetValue("Path", selectedPath);
            //}
        }
    }
}
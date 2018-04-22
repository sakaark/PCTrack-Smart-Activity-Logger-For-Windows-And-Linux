using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace wpf
{
    /// <summary>
    /// Interaction logic for Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        public Popup()
        {
            InitializeComponent();
        }

        private void sendResponse_Click_1(object sender, RoutedEventArgs e)
        {
            string code = verificationCode.Text;
            GoogleDriveSync.verificationString = verificationCode.Text;
            GoogleDriveSync.verificationEntered = true;
            this.Close();
        }
    }
}

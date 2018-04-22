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
    /// Interaction logic for Password.xaml
    /// </summary>
    public partial class Password : Window
    {
        public Password()
        {
            InitializeComponent();
        }

        private void savePassword_Click_1(object sender, RoutedEventArgs e)
        {
            string password = passwordBox.Password;
            int hash = password.GetHashCode();
            MainWindow.password = hash.ToString();
            this.Close();
        }
    }
}

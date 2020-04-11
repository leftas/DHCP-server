using DNS_Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DNS_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowModel model = new MainWindowModel();
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = this.model;
        }
        private Regex regex = new Regex("[0-9.]");
        private void IsIPAddress(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !this.regex.IsMatch(e.Text ?? "");

        }
    }
}

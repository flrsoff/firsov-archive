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

namespace Archiver
{
    /// <summary>
    /// Логика взаимодействия для WarningWindow.xaml
    /// </summary>
    public partial class WarningWindow : Window
    {
        
        public WarningWindow(string message)
        {
            InitializeComponent();
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            LocationChanged += WarningWindow_LocationChanged;
            TextBlock_message.Text = message;
        }
        private void WarningWindow_LocationChanged(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Left = Left + (Width - Application.Current.MainWindow.Width) / 2;
                Application.Current.MainWindow.Top = Top + (Height - Application.Current.MainWindow.Height) / 2;
            }
        }
        private void ClickButton_OK(object sender, RoutedEventArgs e)
        {
            Close(); 
        }
    }
}

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
    /// Логика взаимодействия для CreateFolder.xaml
    /// </summary>
    public partial class CreateFolder : Window
    {
        bool _isClickOk = false;
        public bool IsClickOK => _isClickOk;
        public CreateFolder()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            LocationChanged += PackingWindow_LocationChanged;
        }
        public void Get(out string name)
        {
            name = FolderName_TextBox.Text;
        }
        private void PackingWindow_LocationChanged(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Left = Left + (Width - Application.Current.MainWindow.Width) / 2;
                Application.Current.MainWindow.Top = Top + (Height - Application.Current.MainWindow.Height) / 2;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            _isClickOk = true;
            Close();
        }
    }
}

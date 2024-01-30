using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Archiver
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Uri iconUri = new Uri("pack://application:,,,/zip.png", UriKind.RelativeOrAbsolute);
            this.Resources.Add("ApplicationIcon", new BitmapImage(iconUri));

        }
    }
}

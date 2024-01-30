using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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


namespace Archiver
{
    /// <summary>
    /// Логика взаимодействия для UnpackingWindow.xaml
    /// </summary>
    public partial class UnpackingWindow : Window
    {
        List<TreeDirectory> Items;
        TreeDirectory Parent;
        bool _isClickOk = false;
        public bool IsClickOK => _isClickOk;

        public string SelectedPath { get
            {
                return SelectedDirectory_TextBox.Text;
            } }
        

        public UnpackingWindow(string currentPath)
        {
            InitializeComponent();
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            LocationChanged += UnpackingWindow_LocationChanged;
            
            
            SelectedDirectory_TextBox.Text = currentPath;
            LoadDirectoryTree("C:\\");
        }

        public UnpackingWindow(List<TreeDirectory> items, TreeDirectory parent) { 
        
            Items = items;
            Parent = parent;
        }
        public UnpackingWindow(TreeDirectory parent)
        {
            Parent = parent;
        }

        private void UnpackingWindow_LocationChanged(object sender, EventArgs e)
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
        
        private void Choose_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите директорию",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Папка",
                Filter = "Папки|*. ",
                ValidateNames = false
            };
            if (dialog.ShowDialog() == true)
            {
                SelectedDirectory_TextBox.Text = System.IO.Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void LoadDirectoryTree(string rootDirectory)
        {
            var rootItem = new TreeViewItem
            {
                Header = rootDirectory,
                Tag = rootDirectory
            };
            DirectoryTreeView.Items.Add(rootItem);

            LoadSubdirectories(rootItem);
        }

        private void LoadSubdirectories(TreeViewItem parentItem)
        {
            try
            {
                string[] subdirectories = Directory.GetDirectories(parentItem.Tag.ToString());

                foreach (string subdirectory in subdirectories)
                {
                    var subItem = new TreeViewItem
                    {
                        Header = Path.GetFileName(subdirectory),
                        Tag = subdirectory
                    };

                    subItem.Items.Add(null); // Placeholder, чтобы появился "+" для возможности раскрытия

                    subItem.Expanded += SubItem_Expanded;
                    parentItem.Items.Add(subItem);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Обработка случая отсутствия доступа к некоторым поддиректориям
            }
        }

        private void SubItem_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;

            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                LoadSubdirectories(item);
            }
        }

        private void DirectoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = (TreeViewItem)e.NewValue;
            if (selectedItem != null)
            {
                SelectedDirectory_TextBox.Text = selectedItem.Tag.ToString();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Diagnostics;
//using System.Windows.Shapes;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.RightsManagement;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Archiver
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private int _keySorting = 0x01; //0x_1(name); 0x_2(type); 0x_3(size) 0x0_(order); 0x1_(reverse)

        public ObservableCollection<FileSystemItem> FileList { get; set; }
        public DirectoryInfo CurrentDirectory;

        bool inArchive = false;
        TreeDirectory treeDirectory;
        public string DirectoryNameArchive = ""; //Name of the arhive in the directory
        NodeI1OM currentLayerTreeDirectory;
        public MainWindow()
        {
            InitializeComponent();


        
            CurrentDirectory = new DirectoryInfo("C:\\Users\\user\\Desktop\\Архиватор\\Отчет\\Тесты\\Рабочая область\\Архивация и разархивация\\");
            //CurrentDirectory = new DirectoryInfo("C:\\Users\\user\\Desktop\\Архиватор\\input-directory\\");

            FileList = new ObservableCollection<FileSystemItem>();
            fileSystemList.ItemsSource = FileList;
            
            LoadDirectory();

            

            //LoadImage("C:\\Users\\user\\Downloads\\folder.png");


        }

        private void _sort()
        {
            List<FileSystemItem> sortedList;
            switch (_keySorting)
            {
                case 0x01: sortedList = FileList.OrderBy          (p => p.Name).ToList(); break;
                case 0x02: sortedList = FileList.OrderBy          (p => p.Type).ToList(); break;
                case 0x03: sortedList = FileList.OrderBy          (p => p.Size).ToList(); break;
                case 0x11: sortedList = FileList.OrderByDescending(p => p.Name).ToList(); break;
                case 0x12: sortedList = FileList.OrderByDescending(p => p.Type).ToList(); break;
                case 0x13: sortedList = FileList.OrderByDescending(p => p.Size).ToList(); break;
                default: return;
            }

            bool flag = false;

            for (int i = 0; i < sortedList.Count; i++)
            {
                if (flag) FileList[i] = sortedList[i];
                else if (sortedList[i].Name == "..")
                {
                    FileList[0] = sortedList[i];
                    flag = true;
                }
                else FileList[i+1] = sortedList[i];
                
                
            }
        }
        
        private void LoadDirectory()
        {
            try
            {
                // Очищаем список файлов и директорий
                FileList.Clear();

                

                // Получаем все файлы и директории, включая скрытые
                if (inArchive)
                {
                    Debug.WriteLine($"InArchive({currentLayerTreeDirectory.Child.Count})");
                    FileList.Add(new FileSystemItem(true));
                    
                    foreach (var node in currentLayerTreeDirectory.Child)
                    {
                        FileList.Add(new FileSystemItem(node.Info));
                        Debug.WriteLine($"Add({node.Info.Name})");
                        
                    }
                }
                else
                {
                    // Используем DirectoryInfo для получения информации о директории
                    if (CurrentDirectory.Parent != null)
                        FileList.Add(new FileSystemItem (true));
                    foreach (FileSystemInfo fileInfo in CurrentDirectory.GetFileSystemInfos())
                    {
                        string size = "";
                        if (fileInfo is FileInfo file)
                        {
                            size = file.Length.ToString();
                        }
                        FileList.Add(new FileSystemItem
                            (
                            fileInfo.Name,
                            (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory ? "Directory" :
                                fileInfo.Name.EndsWith(".firsov-archive") ? "Archive .firsov-archive" : "File",
                            size
                            )
                        );
                    }

                }

                _sort();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directory: {ex.Message}");
            }
        }

       

        private void fileSystemList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while (dep != null && !(dep is GridViewColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is GridViewColumnHeader columnHeader)
            {
                string columnHeaderName = columnHeader.Column.Header.ToString();
                int newKeySorting;
                switch (columnHeaderName)
                {
                    case "Name": newKeySorting = 1; break;
                    case "Type": newKeySorting = 2; break;
                    case "Size": newKeySorting = 3; break;
                    default : return;
                }

                if (newKeySorting == (_keySorting & 0xf))
                {
                    _keySorting += 0x10; _keySorting %= 0x20;
                } else
                {
                    _keySorting = newKeySorting;
                }

                _sort();

            }


        }


        private void fileSystemList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {



            
            if (fileSystemList.SelectedItem is FileSystemItem selectedItem)//??
            {
                Debug.WriteLine(":file");
                if (inArchive)
                {
                    
                    if (selectedItem.Name == "..")
                    {
                        if (currentLayerTreeDirectory == treeDirectory.Root)
                        {
                            inArchive = false; 
                        }
                        else
                        {
                            currentLayerTreeDirectory = TreeDirectory.Up(currentLayerTreeDirectory);
                        }
                        LoadDirectory();
                    }
                    else if (selectedItem.Type == "Directory")
                    {
                        currentLayerTreeDirectory =
                                TreeDirectory.Down(currentLayerTreeDirectory, selectedItem.Name);
                        LoadDirectory();
                    }
                    else if (selectedItem.Type == "Archive .firsov-archive")
                    {

                    }
                }
                else
                {
                    if (selectedItem.Name == "..")
                    {
                        Debug.WriteLine("Parrent");
                        // Получаем родительскую директорию
                        DirectoryInfo parentDirectory = CurrentDirectory.Parent;

                        if (CurrentDirectory.Parent != null)
                        {

                            CurrentDirectory = CurrentDirectory.Parent;
                            LoadDirectory();

                        }
                    }
                    else if (selectedItem.Type == "Directory")
                    {

                        string currentDirectoryPath = CurrentDirectory.FullName;
                        if (currentDirectoryPath[currentDirectoryPath.Length - 1] != '\\')
                        {
                            currentDirectoryPath += '\\';
                        }
                        currentDirectoryPath += selectedItem.Name;

                        CurrentDirectory = new DirectoryInfo(currentDirectoryPath);
                        
                        LoadDirectory();


                    }
                    else if (selectedItem.Type == "Archive .firsov-archive")
                    { 
                        DirectoryNameArchive = CurrentDirectory.FullName + "\\" + selectedItem.Name;
                        using (FileStream fileStream = new FileStream(DirectoryNameArchive, FileMode.Open))
                        {
                            // Создаем объект BinaryReader для чтения бинарных данных
                            using (BinaryReader binaryReader = new BinaryReader(fileStream))
                            {

                                treeDirectory = new TreeDirectory(fileStream, binaryReader);
                                inArchive = true;
                                currentLayerTreeDirectory = treeDirectory.Root;
                                LoadDirectory();
                            }
                        }
                    }
                }

            }
            
        }
        
        private void fileSystemList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                fileSystemList_MouseDoubleClick(sender, null) ;
            }
        }

        private void OpenUnpackingWindow_Click(object sender, RoutedEventArgs e)
        {
            
            void callUnpacking(FileType     fileType,
                               Unpacker     unpacker,
                               FileStream   inputStream,
                               BinaryReader inputReader,
                               ulong        position,
                               ulong        nextPosition,
                               string       selectedPath,
                               ulong        unnecessaryPart)
            {
                string[] fullName; cName outputName = new cName();
                inputStream.Position = (long)position;
                outputName.bRead(inputStream, inputReader);

                if (outputName.PartNames[0].EndsWith(".firsov-archive"))
                {
                    outputName.LengthPartNames[0] -= 15;
                    outputName.PartNames[0] = outputName.PartNames[0].Substring(0, outputName.LengthPartNames[0]);
                }


                fullName = selectedPath.Split('\\').Concat(outputName.PartNames.Skip((int)unnecessaryPart).ToArray()).ToArray();

                ulong length = BinaryElement._readLength(inputStream, inputReader);
                if (fileType == FileType.File)
                {
                    unpacker.input.Create(length);
                    try
                    {
                        unpacker.CreateComponent(fullName);

                        unpacker.run();

                    }
                    finally
                    {
                        unpacker.RemoveComponent();
                    }
                } 
                else if (fileType == FileType.Directory)
                {
                    Directory.CreateDirectory(string.Join("\\", fullName));
                } else
                {
                        //надо учесть тот случай, когда архив не на первом уровне
                    using (FileStream outStream = new FileStream(string.Join("\\", fullName), FileMode.Create))
                    using(BinaryWriter writer = new BinaryWriter(outStream))
                    {
                        if (nextPosition == 0) while (inputStream.Position < inputStream.Length)
                            writer.Write((byte)inputStream.ReadByte());
                        
                        else while(inputStream.Position < (long)nextPosition)
                            writer.Write((byte)inputStream.ReadByte());
                        
                    }

                }
            }
            
            
            
            if (inArchive)
            {
                UnpackingWindow unpackingWindow = new UnpackingWindow(CurrentDirectory.FullName); ;
                unpackingWindow.ShowDialog();
                if (!unpackingWindow.IsClickOK) return;
                string selectedPath = unpackingWindow.SelectedPath;

                using (FileStream inputStream = new FileStream(DirectoryNameArchive, FileMode.Open))
                using (BinaryReader inputReader = new BinaryReader(inputStream))
                {
                    Unpacker unpacker = new Unpacker(treeDirectory.ArchiveRoot.dict, inputStream, inputReader);

                   

                    ulong lengthSelectedFiles = (ulong)fileSystemList.SelectedItems.Count;
                    ulong unnecessaryPart = 0;

                    for (NodeI1OM current = currentLayerTreeDirectory;
                    current.Parent != null; current = current.Parent) unnecessaryPart++;

                    if (lengthSelectedFiles != 1) unnecessaryPart--;

                    LinkedList<NodeI1OM> items = new LinkedList<NodeI1OM>();

                    //**
                    void getLeafs(NodeI1OM node)
                    {
                        if (node.LengthChildren == 0)
                        {
                            items.AddLast(node);
                        }
                        else foreach (var child in node.Child)
                        {
                            getLeafs(child);
                        }
                    }
                    //**

                    if (lengthSelectedFiles == 0)
                        items.AddLast(currentLayerTreeDirectory.Parent);
                    else foreach (var fileSystemListItem in fileSystemList.SelectedItems)
                        if(fileSystemListItem is FileSystemItem selectedItem)
                        {
                            string name = selectedItem.Name;
                            foreach(var item in currentLayerTreeDirectory.Child)
                            {
                                if (item.Name == selectedItem.Name)
                                {

                                    //**
                                    if(item.Info.Type != "Directory")
                                    {
                                        items.AddLast(item);
                                    } else
                                    {
                                        getLeafs(item);
                                    }
                                    //**

                                    items.AddLast(item); break;
                                   
                                }       
                                
                                    
                            }
                        }

                    items = new LinkedList<NodeI1OM>(items.OrderBy(p => p.Position));

                    for(var current =  items.First; current != null; current = current.Next)
                    //foreach (var item in items)
                    {
                        var item = current.Value;
                        callUnpacking(  item.Info.Type == "File" ? FileType.File : item.Info.Type == "Directory" ? FileType.Directory : FileType.Archive,
                                        unpacker,
                                        inputStream,
                                        inputReader,
                                        item.Position,
                                        current.Next != null ? current.Next.Value.Position : 0,
                                        selectedPath,
                                        unnecessaryPart);
                        if (item.Info.Type != "Directory")
                        {

                        }
                        else
                        {
                            LinkedList<NodeI1OM> leafs = new LinkedList<NodeI1OM>(); ;
                            //void getLeafs(ViewArchive.NodeI1OM node)
                            //{
                            //    if (node.LengthChildren == 0)
                            //    {
                            //        leafs.AddLast(node);
                            //    }
                            //    else foreach (var child in node.Child)
                            //        {
                            //            getLeafs(child);
                            //        }
                            //}
                            //getLeafs(item);
                            //foreach (var leaf in leafs)
                            //{

                            //    callUnpacking(leaf.Info.Type == "File" ? FileType.File : leaf.Info.Type == "Directory" ? FileType.Directory : FileType.Archive,
                            //                  unpacker,
                            //                  inputStream,
                            //                  inputReader,
                            //                  leaf.Position,
                            //                  current.Next != null ? current.Next.Value.Position : 0,
                            //                  selectedPath,
                            //                  unnecessaryPart);
                            //}

                        }
                        
                    }
                }
            }
            else if (fileSystemList.SelectedItem is FileSystemItem selectedItem &&
                selectedItem.Type == "Archive .firsov-archive"){
                UnpackingWindow unpackingWindow = new UnpackingWindow(CurrentDirectory.FullName);
                unpackingWindow.ShowDialog(); if (!unpackingWindow.IsClickOK) return;
                string selectedPath = unpackingWindow.SelectedPath;

                using (FileStream inputStream = new FileStream(CurrentDirectory.FullName+"\\"+selectedItem.Name, FileMode.Open))
                using (BinaryReader inputReader = new BinaryReader(inputStream))
                {
                    Archive archive = new Archive();
                    archive.Name.bRead(inputStream, inputReader);
                    archive.bRead(inputStream, inputReader);
                    Unpacker unpacker = new Unpacker(archive.dict, inputStream, inputReader);

                    ulong[] positions = new ulong[archive.Files.Length + archive.Archives.Length];
                    ulong index = 0;
                    foreach(var file in archive.Files) positions[index++] = file.Position;
                    foreach(var archvie in archive.Archives) positions[index++] = archvie.Position;
                    Array.Sort(positions);


                    ulong indexFile = 0;
                    ulong indexArchive = 0;
                    
                    

                    foreach (var file in archive.Files)
                    {
                        
                        callUnpacking(file.Name.PartNames[file.Name.LengthNames - 1].Contains('.') ? FileType.File  : FileType.Directory,
                                      unpacker,
                                      inputStream,
                                      inputReader,
                                      file.Position,
                                      0,//??
                                      selectedPath,
                                      0);     
                    }
                    foreach(var _archive in archive.Archives)//?? 
                    {
                        callUnpacking(FileType.Archive,
                                      unpacker,
                                      inputStream,
                                      inputReader,
                                      _archive.Position,
                                      0,//??
                                      selectedPath,
                                      0);
                    }
                }
            }
            else
            {
                WarningWindow warningWindow = new WarningWindow("Подходящие файлы не выбраны");
                warningWindow.ShowDialog();
            }
            LoadDirectory();
        }

        private void OpenPackingWindow_Click(object sender, RoutedEventArgs e)
        {
            if (fileSystemList.SelectedItems == null || fileSystemList.SelectedItems.Count == 0 || inArchive) {
                WarningWindow warningWindow = new WarningWindow("Подходящие файлы не выбраны");
                warningWindow.ShowDialog(); return;
            }

            //условие не в архиве и хотя бы 1 файл выбран
            string path, parent, name;
            path = parent = CurrentDirectory.FullName;
            name = fileSystemList.SelectedItems.Count != 1 ? CurrentDirectory.Name : ((FileSystemItem)fileSystemList.SelectedItem).Name;

            {
                PackingWindow packingWindow = new PackingWindow(path, name);
                packingWindow.ShowDialog(); if (!packingWindow.IsClickOK) return;
                packingWindow.Get(out path, out name);
            }

            

            Packer packer = new Packer(path, name, parent);
            foreach (var fileSystemListItem in fileSystemList.SelectedItems)
                if (fileSystemListItem is FileSystemItem selectedItem)
                {
                    packer.AddComponent(selectedItem.Name, selectedItem.Type == "Archive .firsov-archive" ? FileType.Archive : selectedItem.Type == "File" ? FileType.File : FileType.Directory);
                    
                }
            packer.Run();

            LoadDirectory();

        }

        private void OpenCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            string name_folder;
            {
                CreateFolder createFolder = new CreateFolder();
                createFolder.ShowDialog(); if (!createFolder.IsClickOK) return;
                createFolder.Get(out name_folder);
            }
            bool flagNeed = name_folder != "";
            if (inArchive)
            {
                WarningWindow warningWindow = new WarningWindow("Невозможно создать папку внутри архива");
                warningWindow.ShowDialog(); return;
            }
            if (name_folder == "")
            {
                WarningWindow warningWindow = new WarningWindow("Имя не может быть пустым");
                warningWindow.ShowDialog(); return;
            }
            foreach(var  item in FileList)
            {
                if(item.Name == name_folder)
                {
                    WarningWindow warningWindow = new WarningWindow("Элемент с таким именем уже существует");
                    warningWindow.ShowDialog(); return;
                }
            }
            string fullName = CurrentDirectory.FullName;
            if (!fullName.EndsWith("\\")) fullName += "\\";
            fullName += name_folder;
            Directory.CreateDirectory(fullName);
            LoadDirectory();
        }

        

    }

    

}


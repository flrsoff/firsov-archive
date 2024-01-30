using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media;

using System.Windows;
using System.Windows.Media.Effects;

namespace Archiver
{
    public abstract class BinaryElement
    {
        public abstract void bRead(FileStream fileStream, BinaryReader binaryReader);
        public static UInt64 _readLength(FileStream fileStream, BinaryReader binaryReader)
        {
            byte type = binaryReader.ReadByte();
            fileStream.Position--;
            switch (type & 0x3)
            {
                case 0x00: return (UInt64)(binaryReader.ReadByte() >> 2);
                case 0x01: return (UInt64)(binaryReader.ReadUInt16() >> 2);
                case 0x02: return (UInt64)(binaryReader.ReadUInt32() >> 2);
                case 0x03: return (UInt64)(binaryReader.ReadUInt64() >> 2);
                default: return 0;
            }

        }
        public static bool getBit(byte _byte, byte _index)
        {
            switch ((_index << 5) >> 5)
            {
                case 0: return ((_byte & 0x80) != 0);
                case 1: return ((_byte & 0x40) != 0);
                case 2: return ((_byte & 0x20) != 0);
                case 3: return ((_byte & 0x10) != 0);
                case 4: return ((_byte & 0x08) != 0);
                case 5: return ((_byte & 0x04) != 0);
                case 6: return ((_byte & 0x02) != 0);
                default: return ((_byte & 0x01) != 0);
            }
        }

        public static bool getBit(byte[] Data, ulong index)
        {
            return getBit(Data[index >> 3], (byte)(index & 0x8));
        }
    }

    public class cName : BinaryElement
    {
        public override void bRead(FileStream fileStream, BinaryReader binaryReader)
        {
            LengthNames = _readLength(fileStream, binaryReader);
            LengthPartNames = new byte[LengthNames];
            PartNames = new string[LengthNames];
            for (ulong i = 0; i < LengthNames; i++)
            {
                LengthPartNames[i] = binaryReader.ReadByte();
                for (ulong j = 0; j < LengthPartNames[i]; j++)
                {
                    try
                    {
                        PartNames[i] += binaryReader.ReadChar();
                    }
                    catch (Exception e)
                    {

                    }

                }
            }
        }
        
        public ulong LengthNames = 0;
        public byte[] LengthPartNames;
        public string[] PartNames;

        public string toString()
        {
            string result = LengthNames > 0 ? PartNames[0] : "";

            for (ulong i = 1; i < LengthNames; i++)
            {
                result += $"\\{PartNames[i]}";
            }
            return result;
        }
        public ulong Length()
        {
            ulong result = (ulong)(
                LengthNames < 64 ? sizeof(byte) :
                LengthNames < 16384 ? sizeof(ushort) :
                LengthNames < 1073741824 ? sizeof(uint) :
                                            sizeof(ulong));

            for (ulong i = 0; i < LengthNames; i++)
            {
                result += sizeof(byte) + (ulong)PartNames[i].Length;
            }
            return result;
        }
        

    }

    public class Code: BinaryElement
    {
        public byte Key;
        public byte LengthBits;
        public byte LengthBytes;
        public byte[] Value;

        public override void bRead(FileStream fileStream, BinaryReader binaryReader)
        {
            LengthBits = binaryReader.ReadByte();
            LengthBytes = (byte)((LengthBits & 111) == 0 ? LengthBits / 8 : LengthBits / 8 + 1);
            Value = new byte[LengthBytes];
            Key = binaryReader.ReadByte();
            for (byte i = 0; i < LengthBytes; i++)
            {
                Value[i] = binaryReader.ReadByte();
            }

        }
    }

    public class Dict : BinaryElement 
    {
        public byte Length;
        public Code[] Data;

        public override void bRead(FileStream fileStream, BinaryReader binaryReader)
        {
            Length = binaryReader.ReadByte();
            Data = new Code[Length];
            for (byte i = 0; i < Length; i++)
            {
                Data[i] = new Code();
                Data[i].bRead(fileStream, binaryReader);
            }
        }
    }

    public class Archive : BinaryElement
    {
        public cName Name;
        public ulong Position;
        public ulong LengthArchives;
        public ulong LengthFiles;
        public Dict dict;
        public Archive[] Archives;
        public File[] Files;
        
        public override void bRead(FileStream fileStream, BinaryReader binaryReader)
        {
            
            Position = (ulong)fileStream.Position - Name.Length();
            LengthArchives = _readLength(fileStream, binaryReader);
            LengthFiles = _readLength(fileStream, binaryReader);


            Archives = new Archive[LengthArchives];
            Files = new File[LengthFiles];


            dict.bRead(fileStream, binaryReader);

            for (ulong i = 0; i < LengthArchives; i++)
            {
                Archives[i] = new Archive();

                Archives[i].Name.bRead(fileStream, binaryReader);


                Archives[i].bRead(fileStream, binaryReader);
            }
            for (ulong i = 0; i < LengthFiles; i++)
            {
                Files[i] = new File();
                Files[i].Name.bRead(fileStream, binaryReader);


                Files[i].bRead(fileStream, binaryReader);


            }



        }
        public Archive()
        {
            Name = new cName();
            dict = new Dict();
        }
    }

    public class File : BinaryElement
    {
        public cName Name;
        public ulong Position;
        public ulong LengthCodeData;
        

        public override void bRead(FileStream fileStream, BinaryReader binaryReader)
        {
            Position = (ulong)fileStream.Position - Name.Length();
            LengthCodeData = _readLength(fileStream, binaryReader);
            fileStream.Position += (long)(LengthCodeData % 8 == 0 ? LengthCodeData / 8 : LengthCodeData / 8 + 1);

        }
        
        public File()
        {
            Name = new cName();
        }
    }

    public class FileItem 
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
    }

    public class FileSystemItem : FileItem
    {
        
        public ImageSource imageSource { get; set; }

        private void chooseImageSource()
        {

            switch (Type)
            {
                case "Directory":
                    imageSource = imageSource = (ImageSource)Application.Current.FindResource("Image_folder-16");
                    break;
                case "File":
                    imageSource = imageSource = (ImageSource)Application.Current.FindResource("Image_file-16");
                    break;
                case "Archive .firsov-archive":
                    imageSource = imageSource = (ImageSource)Application.Current.FindResource("Image_box-16");
                    break;

            }
        }

        public FileSystemItem(FileItem fileItem)
        {
            Name = fileItem.Name;
            Type = fileItem.Type;
            Size = fileItem.Size;
            chooseImageSource();
        }
        public FileSystemItem(string name, string type, string size)
        {
            Name = name;
            Type = type;
            Size = size;
            chooseImageSource();
        }
        public FileSystemItem(bool isUpload)
        {
            if (isUpload)
            {
                Name = "..";
                Type = "Directory";
                imageSource = imageSource = (ImageSource)Application.Current.FindResource("Image_upload-16");
            }
        }

    }

    public enum FileType
    {
        Archive = 0,
        File = 1,
        Directory = 2
    }

    public class ItemDict
    {
        public byte Byte;
        public byte Length;
        public byte[] Data;


        public ItemDict(byte Byte, byte Length, byte[] Data)
        {
            this.Byte = Byte;
            this.Length = Length;
            this.Data = new byte[Length / 8 + 1];
            for (int i = 0; i < Length / 8 + 1; i++)
                this.Data[i] = Data[i];

        }
    }

    public class PDict
    {

        public byte Length = 0;
        public List<ItemDict> Data;
        public PDict()
        {
            Data = new List<ItemDict>();
        }

        public void Add(byte Byte, byte Length, byte[] Data)
        {

            this.Data.Add(new ItemDict(Byte, Length, Data)); this.Length++;
        }

        public void Sort()
        {
            this.Data.Sort((p1, p2) => p1.Byte.CompareTo(p2.Byte));
        }

        public ItemDict Search(byte Byte)
        {
            foreach (var item in Data)
                if (item.Byte == Byte) return item;
            return null;
        }


    }

    public class LT
    {
        private NodeI1O3 _head = null;

        public void Add(byte value)
        {
            NodeI1O3 currentNode = _head;
            while (currentNode != null && currentNode.Byte != value) currentNode = currentNode.Next;

            if (currentNode == null)
            {
                NodeI1O3 newNode = new NodeI1O3(value);
                newNode.add();
                newNode.Next = _head;

                _head = newNode;
            }
            else
            {
                currentNode.add();
            }
        }
        private bool merge()
        {
            NodeI1O3 first = _head;
            NodeI1O3 second = _head.Next;
            if (second == null) return false;
            _head = second.Next;
            NodeI1O3 newNode = new NodeI1O3(first, second);

            NodeI1O3 currentNode = _head;


            if (currentNode != null)
            {
                if (newNode.Length > _head.Length)
                {
                    while (currentNode.Next != null)
                    {
                        if (currentNode.Next.Length < newNode.Length)
                        {
                            currentNode = currentNode.Next;
                        }
                        else
                        {
                            newNode.Next = currentNode.Next;
                            currentNode.Next = newNode;
                            return true;
                        }
                    }

                    currentNode.Next = newNode;
                    return true;

                }
                else
                {
                    newNode.Next = _head;
                    _head = newNode;
                    return true;
                }
            }
            else
            {
                _head = newNode;
            }
            return true;


        }


        public void Sort()
        {
            ushort size = 0;
            for (NodeI1O3 item = _head; item != null; item = item.Next) size++;

            NodeI1O3[] array = new NodeI1O3[size];
            ushort current_index = 0;
            for (NodeI1O3 item = _head; item != null; item = item.Next, current_index++)
                array[current_index] = item;
            Debug.WriteLine($"\t\tGenerate:Sorting:Length Array: {array.Length}");
            Array.Sort(array, (p1, p2) => p2.Length.CompareTo(p1.Length));
            Debug.WriteLine($"\t\tGenerate:Sorting:Length Array: {array.Length}");
            _head = null;




            for (current_index = 0; current_index < size; current_index++)
            {
                array[current_index].Next = _head;
                _head = array[current_index];
            }

        }
        public void listToTree()
        {
            Sort();
            if (_head == null)
            {
                _head = new NodeI1O3(null, null);
            }
            else
            {
                if (_head.Next == null) { _head = new NodeI1O3(_head, null); }
                else while (merge()) ;
            }




        }

        public PDict treeToDict()
        {
            PDict dict = new PDict();
            byte[] _bytes = new byte[256 / 8];


            void fill(NodeI1O3 node, string str, byte size)
            {

                if (node.FlagRoot == false)
                {
                    for (byte i = 0; i < 256 / 8; i++)
                    {
                        _bytes[i] = 0;
                    }
                    for (byte iByte = 0; iByte < size / 8; iByte++)
                    {

                        for (byte iBit = 0, bit = 128/*1000 0000*/; iBit < 8; iBit++, bit >>= 1)
                        {
                            if (str[iByte * 8 + iBit] == '1') _bytes[iByte] |= bit;
                        }
                    }

                    for (byte iBit = 0, bit = 128/*1000 0000*/; iBit < size % 8; iBit++, bit >>= 1)
                    {
                        if (str[size / 8 * 8 + iBit] == '1') _bytes[size / 8] |= bit;//??
                    }
                    dict.Add(node.Byte, size, _bytes);
                }

                if (node.Left != null) fill(node.Left, str + '0', (byte)(size + 1));
                if (node.Right != null) fill(node.Right, str + '1', (byte)(size + 1));
            }
            fill(_head, "", 0);
            return dict;
        }
    }



    internal class Packer
    {
        //NodeI1O3
        
        string pathTempFile = "C:\\Users\\user\\Desktop\\TEMP";



        

        FileStream _inputStream;
        BinaryReader _reader;
        void createComponent(FileStream inputStream)
        {
            _reader = new BinaryReader(inputStream);
        }

        void removeComponent()
        {
            _reader.Close();
        }

        void Read()
        {
            while (_inputStream.Position < _inputStream.Length)
                lt.Add(_reader.ReadByte());
            lt.Sort();

            lt.listToTree();


        }

        //поток вывода
        FileStream _outputStream;
        BinaryWriter _writer;
        private string _nameArchive;
        string _parent;

        LT lt;
        PDict dict;
        private LinkedList<string> _localNamesArchives;
        private LinkedList<string> _localNamesFiles;
        private LinkedList<string> _localNamesDirectories;
        private ulong _lengthArchives = 0;
        private ulong _lengthFiles = 0;
        private ulong _lengthDirectories = 0;

        public Packer(string Path, string Name, string Parent)
        {
            _nameArchive = Name + ".firsov-archive";
            string fullName = Path + "\\" + _nameArchive;
            lt = new LT();
            _parent = Parent + "\\";

            _outputStream = new FileStream(fullName, FileMode.OpenOrCreate);
            _writer = new BinaryWriter(_outputStream);

            _localNamesArchives = new LinkedList<string>();
            _localNamesFiles = new LinkedList<string>();
            _localNamesDirectories = new LinkedList<string>();

        }
        ~Packer()
        {
            _writer.Close();
            _outputStream.Close();//!!
        }



        private void _readComponent(string path)
        {
            using (_inputStream = new FileStream(path, FileMode.Open))
            using (_reader = new BinaryReader(_inputStream))
                while (_inputStream.Position < _inputStream.Length)
                    lt.Add(_reader.ReadByte());
        }

        private void _generateDict()
        {
            Debug.Write("\tGenerate:Sorting:");
            lt.Sort();
            Debug.WriteLine("ok"); Debug.Write("\tGenerate:To Tree:");
            lt.listToTree();
            Debug.WriteLine("ok"); Debug.Write("\tGenerate:To PDict:");
            dict = lt.treeToDict();
            Debug.WriteLine("ok"); Debug.Write("\tGenerate:Sorting:");
            dict.Sort();
            Debug.WriteLine("ok");
        }


        

        public void AddComponent(string localName, FileType type)
        {
            if (type == FileType.Directory)
            {
                string[] files = Directory.GetFiles(_parent + localName);
                string[] directories = Directory.GetDirectories(_parent + localName);

                for (int i = 0; i < files.Length; i++)
                {
                    AddComponent(files[i].Substring(_parent.Length), files[i].EndsWith(".firsov-archive") ? FileType.Archive : FileType.File);
                }
                if (directories.Length == 0 && files.Length == 0)
                {
                    _localNamesDirectories.AddLast(localName);
                    _lengthDirectories++;
                }
                else for (int i = 0; i < directories.Length; i++)
                    {
                        AddComponent(directories[i].Substring(_parent.Length), FileType.Directory);
                    }


            }
            else if (type == FileType.Archive)
            {
                _localNamesArchives.AddLast(localName);
                _lengthArchives++;
            }
            else
            {
                _localNamesFiles.AddLast(localName);
                _lengthFiles++;
            }
        }

        public void Run()
        {
            //-component-//
            byte _currentByte;
            //byte _byte;
            byte _lengthBits;
            byte _delta;
            ulong _lengthFile;
            ItemDict itemDict;
            //<-
            Debug.WriteLine("Reading(start)");
            foreach (string path in _localNamesFiles) _readComponent(_parent + path);
            Debug.WriteLine("Reading(finish)");

            Debug.WriteLine("Generatin(start)");
            _generateDict();
            Debug.WriteLine("Generatin(finish)");

            Debug.WriteLine("Writing(start)");

            {
                //_writeLength(1, _writer);
                //_writer.Write('!');
                _writeName(_nameArchive, _writer);//_writer.Write((byte)_nameArchive.Length);


                _writeLength(_lengthArchives, _writer);
                _writeLength(_lengthFiles + _lengthDirectories, _writer);

                _writer.Write(dict.Length);
                foreach (var item in dict.Data)
                {
                    _writer.Write(item.Length);
                    _writer.Write(item.Byte);
                    foreach (byte value in item.Data)
                        _writer.Write(value);
                }

                foreach (string path in _localNamesArchives)
                {
                    using (_inputStream = new FileStream(_parent + path, FileMode.Open))
                    using (_reader = new BinaryReader(_inputStream))
                    {
                        //DataFiles._readLength(_inputStream, _reader);
                        cName name = new cName();
                        name.bRead(_inputStream, _reader);

                        _writeName(_nameArchive + "\\" + string.Join("\\", name.PartNames), _writer);
                        while (_inputStream.Position < _inputStream.Length)
                        {
                            _writer.Write((byte)_inputStream.ReadByte());
                        }
                    }
                }

                foreach (string path in _localNamesFiles)
                {
                    _writeName(_nameArchive + "\\" + path, _writer);
                    using (_inputStream = new FileStream(_parent + path, FileMode.Open))
                    using (_reader = new BinaryReader(_inputStream))
                    using (FileStream tempStream = new FileStream(pathTempFile, FileMode.Create))
                    using (BinaryWriter tempWriter = new BinaryWriter(tempStream))
                    {
                        _lengthBits = 0; _currentByte = 0; _lengthFile = 0;

                        while (_inputStream.Position < _inputStream.Length)
                        {

                            itemDict = dict.Search(_reader.ReadByte());
                            _delta = (byte)(8 - _lengthBits);
                            byte i = 0;
                            for (; i < itemDict.Length / 8; i++)
                            {
                                tempWriter.Write((byte)(_currentByte + (itemDict.Data[i] >> _lengthBits)));
                                _currentByte = (byte)(itemDict.Data[i] << _delta);
                            }
                            _lengthFile += (ulong)(i * 8);

                            if (itemDict.Length % 8 + _lengthBits >= 8)
                            {
                                tempWriter.Write((byte)(_currentByte + (itemDict.Data[i] >> _lengthBits)));
                                _currentByte = (byte)(itemDict.Data[i] << _delta);
                                _lengthBits = (byte)(itemDict.Length % 8 - _delta);
                                _lengthFile += 8;
                            }
                            else
                            {

                                _currentByte += (byte)(itemDict.Data[i] >> _lengthBits);
                                _lengthBits += (byte)(itemDict.Length % 8);
                            }




                        }
                        if (_lengthBits > 0)
                        {
                            tempWriter.Write(_currentByte);
                            _lengthFile += _lengthBits;
                        }
                    }
                    using (FileStream tempStream = new FileStream(pathTempFile, FileMode.Open))
                    using (BinaryReader tempReader = new BinaryReader(tempStream))
                    {
                        _writeLength(_lengthFile, _writer);
                        while (tempStream.Position < tempStream.Length)
                        {
                            _writer.Write(tempReader.ReadByte());
                        }
                    }

                }

                foreach (string path in _localNamesDirectories)
                {
                    _writeName(_nameArchive + "\\" + path, _writer);
                    _writeLength(0, _writer);
                }
            }
            Debug.WriteLine("Writing(finish)");
        }

        private void _writeLength(ulong length, BinaryWriter writer)
        {

            if (length < 64) writer.Write((byte)((length << 2) + 0x00));
            else if (length < 16384) writer.Write((UInt16)((length << 2) + 0x01));
            else if (length < 1073741824) writer.Write((UInt32)((length << 2) + 0x02));
            else writer.Write((UInt64)((length << 2) + 0x03));
        }

        private void _writeName(string name, BinaryWriter writer)
        {
            string[] partNames = name.Split('\\');
            _writeLength((ulong)partNames.Length, writer);
            foreach (string partName in partNames)
            {
                
                writer.Write(partName);
                Debug.WriteLine(partName);

            }
        }



    }
    
    internal class Unpacker
    {
        //NodeI1O2
        

        private NodeI1O2 _root = null;
        public Unpucker_InStream input;
        public Unpucker_OutStream output;


        
        public Unpacker(Dict dict, FileStream fileStream, BinaryReader binaryReader)
        {
            input = new Unpucker_InStream(fileStream, binaryReader);
            output = new Unpucker_OutStream();
            if (dict != null)
            {
                _root = new NodeI1O2(); NodeI1O2 currentNode = null;
                foreach (var item in dict.Data)
                {
                    currentNode = _root;
                    for (byte i = 0; i < item.LengthBits; i++)
                    {

                        if (!BinaryElement.getBit(item.Value[i / 8], i))
                        {
                            if (currentNode.left == null)
                                currentNode.left = new NodeI1O2(currentNode, true, 0);
                            currentNode = currentNode.left;

                        }
                        else
                        {
                            if (currentNode.right == null)
                                currentNode.right = new NodeI1O2(currentNode, false, 1);
                            currentNode = currentNode.right;
                        }
                    }
                    currentNode.Value = item.Key;

                }
            }
        }

        public void CreateComponent(string[] partsPath)
        {
            string path = "";
            foreach (var subPath in partsPath)
            {
                path += subPath + "\\";
            }
            path = path.Remove(path.Length - 1);

            Directory.CreateDirectory(Path.GetDirectoryName(path));


            output.Create(new BinaryWriter(System.IO.File.Open(path, FileMode.Create)));

        }
        public void RemoveComponent()
        {
            output._binaryWriter.Close();
        }

        public void run()
        {
            byte length; NodeI1O2 current_node = _root;
            while (!input.End())
            {
                length = input.ReadByte();
                for (byte i = 0; i < length; i++)
                {
                    current_node = current_node.SetValue(!BinaryElement.getBit(input.Byte, i));
                    if (current_node.IsLeaf())
                    {
                        output.WriteByte(current_node.Value);
                        current_node = _root;
                    }
                }
            }
        }
    }

    public class Unpucker_InStream
    {
        public byte _currentByte;//??
        FileStream _fileStream;
        BinaryReader _binaryReader;

        ulong length = 0; //в битах

        public Unpucker_InStream(FileStream fileStream, BinaryReader binaryReader)
        {
            _fileStream = fileStream; _binaryReader = binaryReader;
        }
        public void Create(ulong length)
        {
            this.length = length;
        }

        public bool End() { return length == 0; }
        //возвращает кол-во битов
        public byte ReadByte()
        {

            if (length == 0) return 0;
            else
            {
                byte result = (byte)(length > 8 ? 8 : length);

                length -= length > 8 ? 8 : length;
                _currentByte = _binaryReader.ReadByte();
                return result;
            }
        }
        public byte Byte { get { return _currentByte; } }
    }

    public class Unpucker_OutStream
    {
        //FileStream _fileStream;
        public BinaryWriter _binaryWriter;

        public void WriteByte(byte value)
        {
            _binaryWriter.Write(value);
        }
        public void Create(/*FileStream fileStream, */BinaryWriter binaryWriter)
        {
            //_fileStream = fileStream; 
            _binaryWriter = binaryWriter;
        }
        public Unpucker_OutStream() { }
    }

    public class TreeDirectory
    {
        //NodeI1OM
        

        public NodeI1OM Root;
        public TreeDirectory(string root)
        {
            Root = new NodeI1OM(root, null);
        }
        public NodeI1OM Add(string[] PartNames, ulong position)
        {


            NodeI1OM current = Root;
            foreach (string curName in PartNames)
            {
                //Console.WriteLine($"Part{curName} {(current == null ? "current = null": current.Name)}" );
                current = current.Add(curName);
            }
            current.Position = position;
            return current;
        }

        public Archive ArchiveRoot;
        public TreeDirectory(FileStream fileStream, BinaryReader binaryReader)
        {
            ArchiveRoot = new Archive();


            ArchiveRoot.Name.bRead(fileStream, binaryReader);

            Root = new NodeI1OM(ArchiveRoot.Name.toString(), null);
            ArchiveRoot.bRead(fileStream, binaryReader);




            void fillTree(Archive archive)
            {
                for (ulong i = 0; i < archive.LengthFiles; i++)
                {
                    Add(archive.Files[i].Name.PartNames, archive.Files[i].Position);
                }
                for (ulong i = 0; i < archive.LengthArchives; i++)
                {
                    fillTree(archive.Archives[i]);
                    Add(archive.Archives[i].Name.PartNames, archive.Archives[i].Position);
                }
            }

            fillTree(ArchiveRoot);
            Add(ArchiveRoot.Name.PartNames, ArchiveRoot.Position);

            if (Root != null) Root = Root.Child.First.Value;//??

        }

        static public NodeI1OM Up(NodeI1OM current)
        {
            return current.Parent;
        }

        static public NodeI1OM Down(NodeI1OM current, string nameChild)
        {
            return current.Search(nameChild);
        }

    }
    
    public class NodeI1OM 
    {
        public ulong Position;
        public string Name;
        public ulong LengthChildren;
        public LinkedList<NodeI1OM> Child;
        public FileItem Info;
        public NodeI1OM Parent;

        public NodeI1OM(string name, NodeI1OM parent)
        {
            Parent = parent;
            Name = name;
            LengthChildren = 0;
            Child = new LinkedList<NodeI1OM>();
            Info = new FileItem();

            Info.Name = name;
            Info.Type = !name.Contains(".") ? "Directory" : name.EndsWith(".firsov-archive") ? "Archive .firsov-archive" : "File";
            Info.Size = "";
        }
        
        public NodeI1OM Search(string searchName)
        {
            NodeI1OM result = null;
            foreach (NodeI1OM child in Child)
            {
                if (child.Name == searchName)
                {
                    result = child;
                    break;
                }
            }
            return result;
        }
        public NodeI1OM Add(string currentName)
        {
            NodeI1OM child = Search(currentName);
            if (child == null)
            {
                child = new NodeI1OM(currentName, this);

                LengthChildren++;
                Child.AddLast(child);
            }
            return child;


        }
    }

    public class NodeI1O3 
    {
        public NodeI1O3 Left = null;
        public NodeI1O3 Right = null;
        public NodeI1O3 Next = null;

        public bool FlagRoot = false;
        private ulong _length = 0;
        public ulong Length => _length;
        private byte _byte;
        public byte Byte => _byte;

        public NodeI1O3(byte _byte)
        {
            this._byte = _byte;
        }
        public NodeI1O3(NodeI1O3 first, NodeI1O3 second)
        {
            Left = first;
            Right = second;
            FlagRoot = true;
            _length = (first != null ? first._length : 0) + (second != null ? second._length : 0);
        }

        public void add()
        {
            _length++;
        }
    }


    public class NodeI1O2 
    {
        public byte Value;

        public NodeI1O2 left = null;
        public NodeI1O2 right = null;
        
        public NodeI1O2(NodeI1O2 parrent, bool isLeft, byte value)
        {
            Value = value;
            if (isLeft)
            {
                parrent.left = this;
            }
            else
            {
                parrent.right = this;
            }

        }
        public NodeI1O2()
        {

        }
        
        public bool IsLeaf()
        {
            return (left == null && right == null) ? true : false;
        }
        public byte GetValue()
        {
            return Value;
        }
        public NodeI1O2 SetValue(bool bit_0)
        {
            return bit_0 ? left : right;
        }
    }

}
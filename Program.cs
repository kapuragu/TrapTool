using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TrapTool
{
    class Program
    {
        private const string nameDictionaryFileName = "trap_name_dictionary.txt";
        private const string dataSetDictionaryFileName = "trap_dataset_dictionary.txt";
        private const string nameHashListFileName = "trap_name_hashes.txt";
        private const string dataSetHashListFileName = "trap_dataset_hashes.txt";
        private const string nameCustomDictionaryFileName = "trap_name_custom_dictionary.txt";
        private const string dataSetCustomDictionaryFileName = "trap_dataset_custom_dictionary.txt";
        public static List<uint> nameHashList = new List<uint>();
        public static List<uint> dataSetHashList = new List<uint>();
        static void Main(string[] args)
        {
            var hashManager = new HashManager();

            // Multi-Dictionary Reading!!
            List<string> nameDictionaryNames = new List<string>
            {
                nameDictionaryFileName,
                nameCustomDictionaryFileName,
            };
            List<string> dataSetDictionaryNames = new List<string>
            {
                dataSetDictionaryFileName,
                dataSetCustomDictionaryFileName,
            };

            List<string> nameDictionaries = new List<string>();
            List<string> dataSetDictionaries = new List<string>();

            foreach (var dictionaryPath in nameDictionaryNames)
                if (File.Exists(GetNearAppFilePath(dictionaryPath)))
                    nameDictionaries.Add(GetNearAppFilePath(dictionaryPath));

            foreach (var dictionaryPath in dataSetDictionaryNames)
                if (File.Exists(GetNearAppFilePath(dictionaryPath)))
                    dataSetDictionaries.Add(GetNearAppFilePath(dictionaryPath));

            hashManager.StrCode32LookupTable = MakeHashLookupTableFromFiles(nameDictionaries, FoxHash.Type.StrCode32);
            hashManager.PathCode32LookupTable = MakeHashLookupTableFromFiles(dataSetDictionaries, FoxHash.Type.PathCode32);

            List<string> dataSetCustomStringList = new List<string>();
            List<string> nameCustomStringList = new List<string>();

            var nameHashListFilePath = GetNearAppFilePath(nameHashListFileName);
            var dataSetHashListFilePath = GetNearAppFilePath(dataSetHashListFileName);

            if (File.Exists(dataSetHashListFilePath))
            {
                var readStringList = MakeListFromFile(dataSetHashListFilePath);
                foreach (string line in readStringList)
                    if (uint.TryParse(line, out uint hash))
                        if (!dataSetHashList.Contains(hash))
                            dataSetHashList.Add(hash);
            }
            if (File.Exists(nameHashListFilePath))
            {
                var readStringList = MakeListFromFile(nameHashListFilePath);
                foreach (string line in readStringList)
                    if (uint.TryParse(line, out uint hash))
                        if (!nameHashList.Contains(hash))
                            nameHashList.Add(hash);
            }

            dataSetHashList.Sort();
            nameHashList.Sort();

            foreach (var arg in args)
            {
                if (File.Exists(arg))
                {
                    var filePath = arg;
                    // Read input file
                    string fileExtension = Path.GetExtension(filePath);
                    if (fileExtension.Equals(".trap", StringComparison.OrdinalIgnoreCase))
                        ReadTrap(filePath, hashManager);
                    else if(fileExtension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                        WriteTrap(filePath, hashManager, dataSetCustomStringList, nameCustomStringList);
                    else
                        throw new IOException("Unrecognized input type.");
                }
            }

            dataSetHashList.Sort();
            nameHashList.Sort();

            List<string> dataSetHashStringList = new List<string>();
            foreach (uint hash in dataSetHashList)
                if (!dataSetHashStringList.Contains(hash.ToString()) && !string.IsNullOrEmpty(hash.ToString()))
                    dataSetHashStringList.Add(hash.ToString());

            List<string> nameHashStringList = new List<string>();
            foreach (uint hash in nameHashList)
                if (!nameHashStringList.Contains(hash.ToString()) && !string.IsNullOrEmpty(hash.ToString()))
                    nameHashStringList.Add(hash.ToString());

            WriteListToFile(dataSetHashListFilePath, dataSetHashStringList);
            WriteListToFile(nameHashListFilePath, nameHashStringList);

            WriteUserStringsToFile(GetNearAppFilePath(dataSetCustomDictionaryFileName), dataSetCustomStringList);
            WriteUserStringsToFile(GetNearAppFilePath(nameCustomDictionaryFileName), nameCustomStringList);

            //Console.Read(); //DEBUG Hold
        }
        public static string GetNearAppFilePath(string fileName)
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + fileName;
        }
        public static void ReadTrap(string trapPath, HashManager hashManager)
        {
            Console.WriteLine($"Unpacking {trapPath}...");
            TrapFile trap = ReadFromBinary(trapPath, hashManager);
            WriteToXml(trap, Path.GetFileNameWithoutExtension(trapPath) + ".trap.xml");
        }
        public static TrapFile ReadFromBinary(string path, HashManager hashManager)
        {
            TrapFile trap = new TrapFile();
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                trap.Read(reader, hashManager);
            }

            Tuple<uint,List<uint>> dumpedHashList = GetHashesFromTrap(trap);
            uint dataSetName = dumpedHashList.Item1;
            List<uint> nameHashList = dumpedHashList.Item2;
            dataSetHashList.Add(dataSetName);
            foreach (uint nameHash in nameHashList)
                Program.nameHashList.Add(nameHash);

            return trap;
        }
        private static Tuple<uint, List<uint>> GetHashesFromTrap(TrapFile trap)
        {
            uint dataSetStringList = trap.DataSet.HashValue;
            List<uint> nameStringList = new List<uint>();
            foreach (TrapEntry entry in trap.Entries)
                nameStringList.Add(entry.Name.HashValue);

            return Tuple.Create(dataSetStringList, nameStringList);
        }
        public static void CollectUserStrings(TrapFile trap, HashManager hashManager, List<string> DataSetNameStrings, List<string> UserNameStrings)
        {
            if (IsUserString(trap.DataSet.StringLiteral, DataSetNameStrings, hashManager.PathCode32LookupTable))
                DataSetNameStrings.Add(trap.DataSet.StringLiteral);

            foreach (var entry in trap.Entries) // Analyze hashes
            {
                if (IsUserString(entry.Name.StringLiteral, UserNameStrings, hashManager.StrCode32LookupTable))
                    UserNameStrings.Add(entry.Name.StringLiteral);
            }
        }
        public static bool IsUserString(string userString, List<string> list, Dictionary<uint, string> dictionaryTable)
        {
            if (!dictionaryTable.ContainsValue(userString) && !list.Contains(userString) && !string.IsNullOrEmpty(userString))
                return true;
            else
                return false;
        }
        public static void WriteToXml(TrapFile trap, string path)
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };
            using (var writer = XmlWriter.Create(path, xmlWriterSettings))
            {
                trap.WriteXml(writer);
            }
        }
        public static void WriteTrap(string trapPath, HashManager hashManager, List<string> dataSetCustomDictionary, List<string> nameCustomDictionary)
        {
            TrapFile trap = ReadFromXml(trapPath);
            CollectUserStrings(trap, hashManager, dataSetCustomDictionary, nameCustomDictionary);
            WriteToBinary(trap, Path.GetFileNameWithoutExtension(trapPath) + ".trap");
        }
        public static TrapFile ReadFromXml(string trapPath)
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };

            TrapFile trap = new TrapFile();
            using (var reader = XmlReader.Create(trapPath, xmlReaderSettings))
            {
                trap.ReadXml(reader);
            }
            return trap;
        }
        public static void WriteToBinary(TrapFile trap, string trapPath)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(trapPath, FileMode.Create)))
            {
                trap.Write(writer);
            }
        }
        private static List<string> MakeListFromFile(string path)
        {
            // Read file
            List<string> stringList = new List<string>();
            using (StreamReader file = new StreamReader(path))
            {
                // TODO multi-thread
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    stringList.Add(line);
                }
            }
            return stringList;
        }
        /// <summary>
        /// Opens a file containing one string per line, hashes each string, and adds each pair to a lookup table.
        /// </summary>
        /// 
        private static void WriteListToFile(string path, List<string> hashList)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                foreach (string line in hashList)
                {
                    file.WriteLine(line);
                }
            }
        }
        private static Dictionary<uint, string> MakeHashLookupTableFromFiles(List<string> paths, FoxHash.Type hashType)
        {
            ConcurrentDictionary<uint, string> table = new ConcurrentDictionary<uint, string>();

            // Read file
            List<string> stringLiterals = new List<string>();
            foreach (var dictionary in paths)
            {
                using (StreamReader file = new StreamReader(dictionary))
                {
                    // TODO multi-thread
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        stringLiterals.Add(line);
                    }
                }
            }

            // Hash entries
            Parallel.ForEach(stringLiterals, (string entry) =>
            {
                if (hashType == FoxHash.Type.StrCode32)
                {
                    uint hash = HashManager.StrCode32(entry);
                    table.TryAdd(hash, entry);
                }
                else
                {
                    uint hash = HashManager.PathCode32(entry);
                    table.TryAdd(hash, entry);
                }
            });

            return new Dictionary<uint, string>(table);
        }
        public static void WriteUserStringsToFile(string path, List<string> stringList)
        {
            stringList.Sort(); //Sort alphabetically for neatness
            foreach (var userString in stringList)
                using (StreamWriter file = new StreamWriter(path, append: true))
                    file.WriteLine(userString); //Write them into the user dictionary
        }
    }
}

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
        static void Main(string[] args)
        {
            var hashManager = new HashManager();

            var nameDictionaryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + nameDictionaryFileName;
            var dataSetDictionaryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + dataSetDictionaryFileName;

            // Read hash dictionaries
            if (File.Exists(nameDictionaryPath))
                hashManager.StrCode32LookupTable = MakeHashLookupTableFromFile(nameDictionaryPath, FoxHash.Type.StrCode32);
            if (File.Exists(dataSetDictionaryPath))
                hashManager.PathCode32LookupTable = MakeHashLookupTableFromFile(dataSetDictionaryPath, FoxHash.Type.PathCode32);

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
                        WriteTrap(filePath);
                    else
                        throw new IOException("Unrecognized input type.");
                }
            }
            Console.Read(); //DEBUG Hold onscreen
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
            return trap;
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
        public static void WriteTrap(string trapPath)
        {
            TrapFile trap = ReadFromXml(trapPath);
            WriteToBinary(trap, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(trapPath)) + ".trap");
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
        /// <summary>
        /// Opens a file containing one string per line, hashes each string, and adds each pair to a lookup table.
        /// </summary>
        private static Dictionary<uint, string> MakeHashLookupTableFromFile(string path, FoxHash.Type hashType)
        {
            ConcurrentDictionary<uint, string> table = new ConcurrentDictionary<uint, string>();

            // Read file
            List<string> stringLiterals = new List<string>();
            using (StreamReader file = new StreamReader(path))
            {
                // TODO multi-thread
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    stringLiterals.Add(line);
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

    }
}

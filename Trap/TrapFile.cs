using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace TrapTool
{
    public class TrapFile : IXmlSerializable
    {
        public FoxHash DataSet { get; set; }
        public List<TrapEntry> Entries = new List<TrapEntry>();
        /// <summary>
        /// Reads and populates data from a binary lba file.
        /// </summary>
        public void Read(BinaryReader reader, HashManager hashManager)
        {
            Console.WriteLine($"Reading...");
            uint mgc = reader.ReadUInt32(); 
            if (mgc != 201406020) 
            { 
                throw new FormatException($"magic isn't 201406020!!! Is {mgc}!!!"); 
            };
            int offset = reader.ReadInt32(); 
            if (offset != 32) 
            { 
                throw new FormatException($"offset isn't 32!!! Is {offset}!!!"); 
            };
            uint fileSize = reader.ReadUInt32(); 
            if (fileSize != reader.BaseStream.Length) 
            { 
                throw new FormatException($"fileSize isn't file length!!! Is {fileSize}!!!"); 
            };
            Console.WriteLine($"File size: {fileSize}");
            DataSet = new FoxHash(FoxHash.Type.PathCode32);
            DataSet.Read(reader, hashManager.PathCode32LookupTable, hashManager.OnHashIdentified);
            Console.WriteLine($"DataSet: {DataSet.HashValue}");

            reader.ReadZeroes(24);
            int unknown0 = reader.ReadInt32(); 
            if (unknown0 != 1) 
            { 
                throw new FormatException($"unknown0 isn't 1!!! Is {unknown0}!!!"); 
            };
            uint headerSize = reader.ReadUInt32(); 
            if (headerSize != 48) 
            { 
                throw new FormatException($"headerSize isn't 48!!! Is {headerSize}!!!"); 
            };

            uint extraFileSize = reader.ReadUInt32(); 
            if (extraFileSize != fileSize + 16) 
            { 
                throw new FormatException($"headerSize isn't {fileSize + 16}!!! Is {extraFileSize}!!!"); 
            };
            reader.ReadZeroes(12);

            reader.ReadZeroes(4);
            int entryCount = reader.ReadInt32();
            Console.WriteLine($"Entries#: {entryCount}");
            reader.ReadZeroes(8);

            reader.AlignStream(16);
            long startOfOffsets = reader.BaseStream.Position;
            for (int i = 0; i < entryCount; i++)
            {
                reader.BaseStream.Position = startOfOffsets + (4 * i);
                uint offsetToEntry = reader.ReadUInt32();
                reader.BaseStream.Position = offsetToEntry + headerSize + 32;
                TrapEntry entry = new TrapEntry();
                entry.Read(reader,hashManager);
                Entries.Add(entry);
            }
            reader.AlignStream(16);
        }
        /// <summary>
        /// Writes data to a binary lba file.
        /// </summary>
        public void Write(BinaryWriter writer)
        {
            writer.Write((uint)201406020);
            writer.Write((uint)32);
            long posToWrite_fileSize = writer.BaseStream.Position; writer.Write((uint)0);
            DataSet.Write(writer);

            writer.WriteZeroes(24);
            writer.Write((int)1);
            writer.Write((uint)48);

            long posToWrite_extraFileSize = writer.BaseStream.Position; writer.Write((uint)0);
            writer.WriteZeroes(12);

            writer.WriteZeroes(4);
            writer.Write((uint)Entries.Count);
            writer.WriteZeroes(8);

            long startOfOffsetsToEntry = writer.BaseStream.Position;
            long[] posToWrite_offsetToEntry = new long[Entries.Count];
            for (int i = 0; i < Entries.Count; i++)
            {
                posToWrite_offsetToEntry[i] = writer.BaseStream.Position;
                writer.WriteZeroes(4);
            }
            writer.AlignStream(16);

            Entries = Entries.OrderBy(entry=>entry.Name.HashValue).ToList();

            int j = 0;
            foreach (TrapEntry entry in Entries)
            {
                long startPos = writer.BaseStream.Position;
                writer.BaseStream.Position = posToWrite_offsetToEntry[j];
                writer.Write((uint)(startPos - startOfOffsetsToEntry));

                writer.BaseStream.Position = startPos;
                j++;

                entry.Write(writer);
            }

            long eof = writer.BaseStream.Position;
            writer.BaseStream.Position = posToWrite_fileSize;
            writer.Write((uint)eof);
            writer.BaseStream.Position = posToWrite_extraFileSize;
            writer.Write((uint)eof + 16);
            writer.BaseStream.Position = eof;
        }
        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            reader.Read();
            DataSet = new FoxHash(FoxHash.Type.PathCode32);
            DataSet.ReadXml(reader, "dataSet");

            reader.ReadStartElement("trap");
            while (2 > 1)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        TrapEntry entry = new TrapEntry();
                        entry.ReadXml(reader);
                        Entries.Add(entry);
                        reader.ReadEndElement();
                        continue;
                    case XmlNodeType.EndElement:
                        return;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("trap");
            DataSet.WriteXml(writer, "dataSet");
            Console.WriteLine($"Name: {DataSet.HashValue}");
            foreach (TrapEntry entry in Entries)
            {
                writer.WriteStartElement("entry");
                entry.WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndDocument();
        }
        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}

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
    public enum ShapeType : byte
    {
        Box = 19,
        Vector = 27
    }
    [Flags]public enum Tag : ulong
    {
	    Intrude = 0x1,
	    Tower = 0x2,
	    InRoom = 0x4,
	    FallDeath = 0x8,

	    NearCamera1 = 0x10,
	    NearCamera2 = 0x20,
	    NearCamera3 = 0x40,
	    NearCamera4 = 0x80,

	    x9978c8d36f7 = 0x100,
	    NoRainEffect = 0x200,
	    x60e79a58dcc3 = 0x400,
	    GimmickNoFulton = 0x800,

	    innerZone = 0x1000,
	    outerZone = 0x2000,
	    hotZone = 0x4000,
	    x439898dcbf83 = 0x8000,

	    xe780e431a068 = 0x10000,
	    x53827eed3fbc = 0x20000,
	    x7e1121c5cb93 = 0x40000,
	    xcadd57b76a83 = 0x80000,

	    xe689072c4df8 = 0x100000,
	    x6d14396ebbe5 = 0x200000,
	    xd1ee7dc34fff = 0x400000,
	    xb07e254afcae = 0x800000,

	    xd6ee65d20b7a = 0x10000000,
	    xf287ba9cb7e3 = 0x20000000,
	    NoFulton = 0x40000000,
	    x24330b0e33cb = 0xffffffff80000000,
    };
    public class TrapEntry : IXmlSerializable
    {
        public ShapeType Type { get; set; }
        public ulong Tags { get; set; }
        public FoxHash Name { get; set; }
        public List<ITrapShape> Shapes = new List<ITrapShape>();
        public void Read(BinaryReader reader, HashManager hashManager)
        {
            uint shapeDefBitfield = reader.ReadUInt32();
            Type = (ShapeType)(byte)(shapeDefBitfield & 0xFF);
            byte shapeDefUnknown0 = (byte)((byte)(shapeDefBitfield >> 8) & 0xFF); 
            if (shapeDefUnknown0 != 0) 
            { 
                throw new FormatException($"@{reader.BaseStream.Position} shapeDefUnknown0 is not 0!!! Is {shapeDefUnknown0}!!!"); 
            };
            byte shapeDefUnknown1 = (byte)((byte)(shapeDefBitfield >> 16) & 0xFF); 
            if (shapeDefUnknown1 != 128) 
            { 
                throw new FormatException($"@{reader.BaseStream.Position} shapeDefUnknown1 is not 0!!! Is {shapeDefUnknown1}!!!"); 
            };
            byte shapeCount = (byte)((byte)(shapeDefBitfield >> 24) & 0xFF);
            reader.ReadZeroes(12);
            Console.WriteLine($"@{reader.BaseStream.Position} Type: {Type}, shape#: {shapeCount}");

            Tags = reader.ReadUInt64(); //TODO pretty?
            Name = new FoxHash(FoxHash.Type.StrCode32);
            Name.Read(reader, hashManager.StrCode32LookupTable, hashManager.OnHashIdentified);
            reader.ReadZeroes(4);
            Console.WriteLine($"@{reader.BaseStream.Position} Tags: {Tags}, Name: {Name.HashValue}");

            for (int i = 0; i < shapeCount; i++)
            {
                switch (Type)
                {
                    case ShapeType.Box:
                        ITrapShape boxShape = new TrapShapeBox();
                        boxShape.Read(reader);
                        Shapes.Add(boxShape);
                        break;
                    case ShapeType.Vector:
                        ITrapShape vectorShape = new TrapShapeVector();
                        vectorShape.Read(reader);
                        Shapes.Add(vectorShape);
                        break;
                    default:
                        throw new NotImplementedException($"@{reader.BaseStream.Position} Unknown ShapeType!!!");
                }
            }
            if (Type==ShapeType.Box)
            {
                reader.ReadZeroes(16);
            }

        }
        public void Write(BinaryWriter writer)
        {
            uint shapeDefBitfield = 0;
            shapeDefBitfield += (byte)Type;
            shapeDefBitfield += 0 << 8;
            shapeDefBitfield += 128 << 16;
            shapeDefBitfield += (uint)((byte)Shapes.Count << 24);
            writer.Write(shapeDefBitfield);
            writer.WriteZeroes(12);

            writer.Write(Tags);
            Name.Write(writer);
            writer.WriteZeroes(4);

            foreach (ITrapShape shape in Shapes)
            {
                shape.Write(writer);
            }

            if (Type == ShapeType.Box)
                writer.WriteZeroes(16);
        }

        public void ReadXml(XmlReader reader)
        {
            Name = new FoxHash(FoxHash.Type.StrCode32);
            Name.ReadXml(reader, "name");

            /*
            Tags = ulong.Parse(reader["tags"]);
            */

            Type = (ShapeType)byte.Parse(reader["type"]);
            reader.ReadStartElement("entry");

            reader.ReadStartElement("tags");
            List<int> flagArray = new List<int>();
            var loop = true;
            while (loop)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var tag = int.Parse(reader["tagId"]);
                        reader.ReadStartElement("tag");
                        flagArray.Add(tag);
                        continue;
                    case XmlNodeType.EndElement:
                        loop = false;
                        break;
                }
            }
            Tags = 0;
            foreach (int bitIndex in flagArray)
            {
                Console.WriteLine($"Tags add index {bitIndex}");
                Console.WriteLine($"Tags pre: {Tags}");
                var bitFlag = (ulong)1 << bitIndex;
                Tags |= bitFlag;
                Console.WriteLine($"Tags post: {Tags}");
            }
            reader.ReadEndElement();

            while (2 > 1)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        ITrapShape newShape = CreateShape();
                        newShape.ReadXml(reader);
                        reader.ReadEndElement();
                        Shapes.Add(newShape);
                        continue;
                    case XmlNodeType.EndElement:
                        return;
                }
            }
        }
        ITrapShape CreateShape()
        {
            switch (Type)
            {
                case ShapeType.Box:
                    return new TrapShapeBox();
                case ShapeType.Vector:
                    return new TrapShapeVector();
                default:
                    throw new ArgumentOutOfRangeException();
            }
            throw new ArgumentOutOfRangeException();
        }

        public void WriteXml(XmlWriter writer)
        {
            Name.WriteXml(writer, "name");
            Console.WriteLine($"Name: {Name.HashValue}");
            writer.WriteAttributeString("type", ((byte)Type).ToString());
            Console.WriteLine($"Type: {Type}");

            /*
            writer.WriteAttributeString("tags", Tags.ToString());
            Console.WriteLine($"Tags: {Tags}");
            */
            List<int> flagArray = new List<int>();
            for (int bitIndex = 0; bitIndex < 64; bitIndex++)
                if ((Tags & ((ulong)1 << bitIndex)) != 0)
                    flagArray.Add(bitIndex);

            writer.WriteStartElement("tags");
            foreach (int tag in flagArray)
            {
                writer.WriteStartElement("tag");
                writer.WriteAttributeString("tagId", tag.ToString());
                writer.WriteEndElement();

            }
            writer.WriteEndElement();

            foreach (ITrapShape shape in Shapes)
            {
                writer.WriteStartElement("shape");
                shape.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}

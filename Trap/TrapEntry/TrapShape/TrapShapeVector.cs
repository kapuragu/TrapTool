using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace TrapTool
{
    public class TrapShapeVector : ITrapShape
    {
        public ShapeType Type => ShapeType.Vector;
        public float YMin { get; set; }
        public float YMax { get; set; }
        public List<Vector4> Points { get; set; }

        public void Read(BinaryReader reader)
        {
            YMin = reader.ReadSingle();
            YMax = reader.ReadSingle();
            uint pointsCount = reader.ReadUInt32();
            uint unknown0 = reader.ReadUInt32(); 
            if (unknown0 != 32) //32 - offset to shapes?
            { 
                throw new FormatException($"@{reader.BaseStream.Position} unknown0 isn't 32!!! Is {unknown0}"); 
            };
            Console.WriteLine($"@{reader.BaseStream.Position}: y={YMin}/{YMax}, #{pointsCount}, {unknown0} ");

            reader.ReadZeroes(16);

            Console.WriteLine($"@{reader.BaseStream.Position}: start read points.");
            Points = new List<Vector4>();
            for (int i = 0; i < pointsCount; i++)
            {
                Points.Add(new Vector4()
                {
                    X = reader.ReadSingle(),
                    Y = reader.ReadSingle(),
                    Z = reader.ReadSingle(),
                    W = reader.ReadSingle(),
                });
                Console.WriteLine($"@{reader.BaseStream.Position} POINT#{i} : X: {Points[i].X}, Y: {Points[i].Y }, Z: {Points[i].Z }");
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(YMin);
            writer.Write(YMax);
            writer.Write((uint)Points.Count);
            writer.Write((uint)32);

            writer.WriteZeroes(16);

            foreach(Vector4 point in Points)
                point.Write(writer);
        }

        public void ReadXml(XmlReader reader)
        {
            YMin = Extensions.ParseFloatRoundtrip(reader["yMinimum"]);
            YMax = Extensions.ParseFloatRoundtrip(reader["yMaximum"]);
            reader.ReadStartElement("shape");
            reader.ReadStartElement("points");
            Points = new List<Vector4>();
            while (2 > 1)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        Vector4 point = new Vector4();
                        point.ReadXml(reader);
                        reader.ReadStartElement("point");
                        Points.Add(point);
                        Console.WriteLine($"POINT: X: {point.X}, Y: {point.Y }, Z: {point.Z }");
                        continue;
                    case XmlNodeType.EndElement:
                        reader.ReadEndElement();
                        return;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("yMinimum", YMin.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("yMaximum", YMax.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine($"y={YMin}/{YMax}");
            writer.WriteStartElement("points");
            foreach (Vector4 point in Points)
            {
                writer.WriteStartElement("point");
                point.WriteXml(writer);
                Console.WriteLine($"X: {point.X}, Y: {point.Y }, Z: {point.Z }");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}

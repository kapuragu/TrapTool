using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace TrapTool
{
    public class TrapShapeBox : ITrapShape
    {
        public ShapeType Type => ShapeType.Box;
        public Vector4 Scale { get; set; }
        public Matrix3 Rotation { get; set; }
        public Vector4 Translation { get; set; }

        public void Read(BinaryReader reader)
        {
            Scale = new Vector4();
            Scale.Read(reader);

            reader.ReadZeroes(16);

            Scale.X *= 2; //Written in binary as the 0.5 of the actual box size. Shrug emoji
            Scale.Y *= 2;
            Scale.Z *= 2;
            Console.WriteLine($"@{reader.BaseStream.Position} SCALE: X: {Scale.X}, Y: {Scale.Y }, Z: {Scale.Z }");

            Rotation = new Matrix3();
            Rotation.Read(reader);
            Console.WriteLine($"@{reader.BaseStream.Position} ROT1: X: {Rotation.Vec1.X}, Y: {Rotation.Vec1.Y}, Z: {Rotation.Vec1.Z}, Z: {Rotation.Vec1.W}");
            Console.WriteLine($"@{reader.BaseStream.Position} ROT2: X: {Rotation.Vec2.X}, Y: {Rotation.Vec2.Y}, Z: {Rotation.Vec2.Z}, Z: {Rotation.Vec2.W}");
            Console.WriteLine($"@{reader.BaseStream.Position} ROT3: X: {Rotation.Vec3.X}, Y: {Rotation.Vec3.Y}, Z: {Rotation.Vec3.Z}, Z: {Rotation.Vec3.W}");

            Translation = new Vector4();
            Translation.Read(reader);
            Console.WriteLine($"@{reader.BaseStream.Position} POS: X: {Translation.X}, Y: {Translation.Y }, Z: {Translation.Z }");
        }

        public void Write(BinaryWriter writer)
        {
            Scale.X = (float)(Scale.X * 0.5);
            Scale.Y = (float)(Scale.Y * 0.5);
            Scale.Z = (float)(Scale.Z * 0.5);
            Scale.Write(writer);

            writer.WriteZeroes(16);

            Rotation.Write(writer);

            Translation.Write(writer);
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("shape");

            Scale = new Vector4();
            Scale.ReadXml(reader);
            reader.ReadStartElement("scale");

            Rotation = new Matrix3();
            Rotation.ReadXml(reader);

            Translation = new Vector4();
            Translation.ReadXml(reader);
            reader.ReadStartElement("translation");
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("scale");
            Scale.WriteXml(writer);
            writer.WriteEndElement();

            Rotation.WriteXml(writer);

            writer.WriteStartElement("translation");
            Translation.WriteXml(writer);
            writer.WriteEndElement();
        }
        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}

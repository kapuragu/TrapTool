using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace TrapTool
{
    public class Matrix3 : IXmlSerializable
    {
        public Vector4 Vec1 { get; set; }
        public Vector4 Vec2 { get; set; }
        public Vector4 Vec3 { get; set; }

        public virtual void Read(BinaryReader reader)
        {
            Vec1 = new Vector4();
            Vec1.Read(reader);
            Vec2 = new Vector4();
            Vec2.Read(reader);
            Vec3 = new Vector4();
            Vec3.Read(reader);
        }

        public virtual void Write(BinaryWriter writer)
        {
            Vec1.Write(writer);
            Vec2.Write(writer);
            Vec3.Write(writer);
        }

        public virtual void ReadXml(XmlReader reader)
        {
            Vec1 = new Vector4();
            Vec1.ReadXml(reader);
            reader.ReadStartElement("rotation1");
            Vec2 = new Vector4();
            Vec2.ReadXml(reader);
            reader.ReadStartElement("rotation2");
            Vec3 = new Vector4();
            Vec3.ReadXml(reader);
            reader.ReadStartElement("rotation3");
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("rotation1");
            Vec1.WriteXml(writer);
            writer.WriteEndElement();
            writer.WriteStartElement("rotation2");
            Vec2.WriteXml(writer);
            writer.WriteEndElement();
            writer.WriteStartElement("rotation3");
            Vec3.WriteXml(writer);
            writer.WriteEndElement();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}

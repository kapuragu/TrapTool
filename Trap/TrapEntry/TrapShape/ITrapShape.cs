using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TrapTool
{
    public interface ITrapShape : IXmlSerializable
    {
        ShapeType Type { get; }
        void Read(BinaryReader reader);
        void Write(BinaryWriter writer);
    }
}

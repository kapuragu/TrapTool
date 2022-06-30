using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace TrapTool
{
    public class FoxHash
    {
        public enum Type
        {
            StrCode32,
            PathCode32
        }

        public uint HashValue;
        public string StringLiteral = string.Empty;
        public bool IsStringKnown => !string.IsNullOrEmpty(this.StringLiteral);

        private readonly Type type;

        public FoxHash(Type type)
        {
            this.type = type;
        }

        public virtual void Read(BinaryReader reader, Dictionary<uint, string> hashLookupTable, HashIdentifiedDelegate hashIdentifiedCallback)
        {
            HashValue = reader.ReadUInt32();

            if (hashLookupTable.ContainsKey(HashValue))
            {
                StringLiteral = hashLookupTable[HashValue];
                hashIdentifiedCallback.Invoke(HashValue, StringLiteral);
            }
        }

        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(HashValue);
        }

        public void ReadXml(XmlReader reader, string label)
        {
            string value = reader[label];

            if (uint.TryParse(value, out uint maybeHash))
            {
                HashValue = maybeHash;
            }
            else
            {
                StringLiteral = value;

                if (this.type == Type.StrCode32)
                {
                    HashValue = HashManager.StrCode32(StringLiteral);
                }
                else
                {
                    HashValue = HashManager.PathCode32(StringLiteral);
                }
            }
        }

        public void WriteXml(XmlWriter writer, string label)
        {
            if (IsStringKnown)
            {
                writer.WriteAttributeString(label, StringLiteral);
            }
            else
            {
                writer.WriteAttributeString(label, HashValue.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}

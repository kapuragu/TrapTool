using System;
using System.Collections.Generic;

namespace TrapTool
{
    public delegate void HashIdentifiedDelegate(uint hashValue, string stringLiteral);

    public class HashManager
    {
        private const ulong MetaFlag = 0x4000000000000;

        public Dictionary<uint, string> StrCode32LookupTable = new Dictionary<uint, string>();
        public Dictionary<uint, string> PathCode32LookupTable = new Dictionary<uint, string>();
        public Dictionary<uint, string> UsedHashes = new Dictionary<uint, string>();

        public static uint StrCode32(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            const ulong seed0 = 0x9ae16a3b2f90404f;
            ulong seed1 = text.Length > 0 ? (uint)((text[0]) << 16) + (uint)text.Length : 0;
            return (uint)(CityHash.CityHash.CityHash64WithSeeds(text + "\0", seed0, seed1) & 0xFFFFFFFFFFFF);
        }

        private static ulong HashFileName(string text, bool removeExtension = true)
        {
            if (removeExtension)
            {
                int index = text.IndexOf('.');
                text = index == -1 ? text : text.Substring(0, index);
            }

            bool metaFlag = false;
            const string assetsConstant = "/Assets/";
            if (text.StartsWith(assetsConstant))
            {
                text = text.Substring(assetsConstant.Length);

                if (text.StartsWith("tpptest"))
                {
                    metaFlag = true;
                }
            }
            else
            {
                metaFlag = true;
            }

            text = text.TrimStart('/');

            const ulong seed0 = 0x9ae16a3b2f90404f;
            byte[] seed1Bytes = new byte[sizeof(ulong)];
            for (int i = text.Length - 1, j = 0; i >= 0 && j < sizeof(ulong); i--, j++)
            {
                seed1Bytes[j] = Convert.ToByte(text[i]);
            }

            ulong seed1 = BitConverter.ToUInt64(seed1Bytes, 0);
            ulong maskedHash = CityHash.CityHash.CityHash64WithSeeds(text, seed0, seed1) & 0x3FFFFFFFFFFFF;

            return metaFlag
                ? maskedHash | MetaFlag
                : maskedHash;
        }

        public static uint PathCode32(string path)
        {
            return (uint)(HashFileName(path));
        }

        /// <summary>
        /// Whenever a hash is identified, keep track of it so we can output a list of all matching hashes.
        /// </summary>
        /// <param name="hashValue">Hash value that was matched.</param>
        /// <param name="stringLiteral">String literal the hashValue matches.</param>
        public void OnHashIdentified(uint hashValue, string stringLiteral)
        {
            if (!UsedHashes.ContainsKey(hashValue))
            {
                UsedHashes.Add(hashValue, stringLiteral);
            }
        }
    }
}

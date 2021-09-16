using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ZEVLR_LIB.Common
{
    public static class XmlEx
    {
        public static T Deserialize<T>(string path)
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(File.OpenRead(path));
        }

        public static void Write<T>(T obj, string path) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringBuilder sb = new();
            using XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                CheckCharacters = false,
                Indent = true,
            });
            serializer.Serialize(writer, obj);
            File.WriteAllText(path, sb.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RogueEssence.Dev;
using RogueEssence.LevelGen;

namespace RogueEssence.Data
{
    [Serializable]
    public class SerializationContainer
    {
        public Version Version;
        public object Object;
    }

    public static class Serializer
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new SerializerContractResolver(),
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            //NOTE: What do these converters do?  Are they just for the top level class?  They weren't working for class members of type matching the converter...
            //Converters = new List<JsonConverter>() { new TestConverter() },
        };

        /// <summary>
        /// A value that is temporarily set when deserializing a data object, serving as a global old version for converters in UpgradeConverters.cs to recognize the version.
        /// A bit hacky, but is currently the only way for converters to recognize version.
        /// </summary>
        public static Version OldVersion;

        public static object Deserialize(Stream stream, Type type)
        {
            object obj;
            Version pastVersion = OldVersion;
            OldVersion = Versioning.GetVersion();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, -1, true))
            {
                obj = JsonConvert.DeserializeObject(reader.ReadToEnd(), type, Settings);
            }
            OldVersion = pastVersion;
            return obj;
        }

        public static void Serialize(Stream stream, object entry)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, -1, true))
            {
                string val = JsonConvert.SerializeObject(entry, Settings);
                writer.Write(val);
            }
        }

        public static Version GetVersion(string containerStr)
        {
            Version objVersion = new Version(0, 0);
            try
            {
                using (JsonTextReader textReader = new JsonTextReader(new StringReader(containerStr)))
                {
                    textReader.Read();
                    textReader.Read();
                    while (true)
                    {
                        if (textReader.TokenType == JsonToken.PropertyName && (string)textReader.Value == "Version")
                        {
                            textReader.Read();
                            objVersion = new Version((string)textReader.Value);
                            break;
                        }
                        else
                        {
                            textReader.Skip();
                            textReader.Read();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
            }
            return objVersion;
        }

        public static object DeserializeData(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, -1, true))
            {
                string containerStr = reader.ReadToEnd();
                //Temporarily set global old version for converters in UpgradeConverters.cs to recognize the version.
                Version pastVersion = OldVersion;
                OldVersion = GetVersion(containerStr);
                SerializationContainer container = (SerializationContainer)JsonConvert.DeserializeObject(containerStr, typeof(SerializationContainer), Settings);
                OldVersion = pastVersion;
                return container.Object;
            }
        }

        public static void SerializeData(Stream stream, object entry)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, -1, true))
            {
                SerializationContainer container = new SerializationContainer();
                container.Object = entry;
                container.Version = Versioning.GetVersion();
                string val = SerializeObjectInternal(container, Settings);
                writer.Write(val);
            }
        }

        private static string SerializeObjectInternal(object value, JsonSerializerSettings settings)
        {
            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
            StringBuilder sb = new StringBuilder(256);
            StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = jsonSerializer.Formatting;
                jsonWriter.Indentation = 0;
                jsonWriter.IndentChar = '\t';

                jsonSerializer.Serialize(jsonWriter, value, null);
            }

            return sw.ToString();
        }

        private class SerializerContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                FieldInfo[] fieldsLess = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                List<MemberInfo> fields = type.GetSerializableMembers();
                List<JsonProperty> props = fields.Select(f => CreateProperty(f, memberSerialization))
                    .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
                
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace MagicSave
{
    public static class SaveJson
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new UnityFieldsResolver(),
            SerializationBinder = new ShortTypeNameBinder(),
            TypeNameHandling = TypeNameHandling.Auto,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Formatting = Formatting.Indented,
        };

        public static string Serialize(object data) => JsonConvert.SerializeObject(data, Settings);
        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }

    internal class UnityFieldsResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var members = new List<MemberInfo>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (var t = objectType; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    if (f.IsNotSerialized || f.IsDefined(typeof(JsonIgnoreAttribute))) continue;
                    if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                    if (!f.IsPublic && !f.IsDefined(typeof(SerializeField))) continue;

                    members.Add(f);
                }
            }

            return members;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var p = base.CreateProperty(member, memberSerialization);
            p.Readable = true;
            p.Writable = true;
            return p;
        }
    }

    internal class ShortTypeNameBinder : ISerializationBinder
    {
        private readonly Dictionary<string, Type> _cache = new();

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            if (_cache.TryGetValue(typeName, out var cached)) return cached;

            foreach (var asm in UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies())
            {
                var t = asm.GetType(typeName);
                if (t == null) continue;

                _cache[typeName] = t;
                return t;
            }

            throw new JsonSerializationException($"[MagicSave] Unknown saved type '{typeName}'");
        }
    }
}
using ECommon.Serializing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECommon.JsonNet
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public JsonSerializerSettings Settings { get; private set; }

        public NewtonsoftJsonSerializer()
        {
            Settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new IsoDateTimeConverter() },//转换器
                ContractResolver = new CustomContractResolver(),
                //Json.NET在返回到参数化构造函数之前，将使用非公共的默认构造函数
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };
        }

        public object Deserialize(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type, Settings);
        }

        public T Deserialize<T>(string value) where T : class
        {
            return JsonConvert.DeserializeObject<T>(JObject.Parse(value).ToString(), Settings);
        }

        public string Serialize(object obj)
        {
            return obj == null ? null : JsonConvert.SerializeObject(obj, Settings);
        }

        class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProperty= base.CreateProperty(member, memberSerialization);
                if (jsonProperty.Writable) return jsonProperty;
                var property = member as PropertyInfo;
                if (property == null) return jsonProperty;
                var hasPrivateSetter = property.GetSetMethod(true) != null;
                jsonProperty.Writable = hasPrivateSetter;
                return jsonProperty;
            }
        }
    }
}

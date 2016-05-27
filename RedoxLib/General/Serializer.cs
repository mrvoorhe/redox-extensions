using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using RedoxLib.Objects;

namespace RedoxLib.General
{
    internal static class Serializer
    {
        internal static string Serialize(ISerializableData obj)
        {
            var json = new JavaScriptSerializer().Serialize(obj);
            return json;
        }

        internal static T Deserialize<T>(string data) where T : ISerializableData
        {
            var obj = new JavaScriptSerializer().Deserialize<T>(data);
            return obj;
        }
    }
}

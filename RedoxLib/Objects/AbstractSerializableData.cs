using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedoxLib.General;

namespace RedoxLib.Objects
{
    public abstract class AbstractSerializableData : ISerializableData
    {
        public override string ToString()
        {
            // Note by Mike (4/9/2015) : The JSON Serializer requires that ToString() be overriden to
            // serialize the object
            return Serializer.Serialize(this);
        }

        public string Serialize()
        {
            return this.ToString();
        }
    }
}

using System;
using System.Runtime.Serialization;
using System.Reflection;

namespace Dpu.Utility
{
	/// <summary>
	/// Utilities for more reasonable XML serialization
	/// </summary>
	public sealed class SerializationHelper
	{
        // FxCop
        private SerializationHelper() {}

        /// <summary>
        /// Write out all the serializable member variables from 
        /// "o" into "info", as defined by the IsSerializable() method.
        /// </summary>
        public static void Serialize(SerializationInfo info, object o)
        {
            foreach(FieldInfo fi in o.GetType().GetFields())
            {
                if (IsSerializable(fi)) 
                {
                    info.AddValue(fi.Name, fi.GetValue(o));
                }
            }
            foreach(PropertyInfo pi in o.GetType().GetProperties())
            {
                if (IsSerializable(pi))
                {
                    info.AddValue(pi.Name, pi.GetValue(o, null));
                }
            }
        }


        /// <summary>
        /// Read in all of the member variables from "info" into "o".
        /// If certain variables are not defined, the exceptions will
        /// be caught if "isStrict" is set to false.
        /// </summary>
        public static void Deserialize(SerializationInfo info, object o, bool isStrict)
        {
            foreach(FieldInfo fi in o.GetType().GetFields())
            {
                if (IsSerializable(fi)) 
                {
                    try 
                    {
                        fi.SetValue(o, info.GetValue(fi.Name, fi.FieldType));
                    }
                    catch(SerializationException ex)
                    {
                        if(!isStrict)
                        {
                            Console.WriteLine("Field not found: {0}", ex);
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }
            foreach(PropertyInfo pi in o.GetType().GetProperties())
            {
                if (IsSerializable(pi)) 
                {
                    try 
                    {
                        pi.SetValue(o, info.GetValue(pi.Name, pi.PropertyType), null);
                    }
                    catch(SerializationException ex)
                    {
                        if(!isStrict)
                        {
                            Console.WriteLine("Property not found: {0}", ex);
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Return whether the member should be serialized.  It should be
        /// public, not marked with NonSerializableAttribute, gettable, and
        /// settable.
        /// </summary>
        private static bool IsSerializable(MemberInfo info)
        {
            if(info is PropertyInfo)
            {
                PropertyInfo pi = (PropertyInfo)info;
                if(!pi.CanWrite || !pi.CanRead)
                {
                    return false;
                }
            }
            foreach(Attribute attr in info.GetCustomAttributes(true)) 
            {
                if (attr is NonSerializedAttribute)
                {
                    return true;
                }
            }
            return false;
        }
	}
}

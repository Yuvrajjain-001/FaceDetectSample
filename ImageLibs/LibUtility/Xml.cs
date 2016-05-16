using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;

namespace Dpu.Utility
{
    /// <summary>
    /// Utilities for XML
    /// </summary>
    public sealed class XmlUtil 
    {
        private XmlUtil() {}

        public static string NodeGetAttribute(XmlNode node, string attName)
        {
            XmlAttribute att = (XmlAttribute) node.Attributes.GetNamedItem(attName);
            if (att != null)
                return att.Value;
            else
                return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[Serializable]
public class SerializableList<TVal> : List<TVal>, IXmlSerializable, ISerializable
{
    #region Private Members

    private XmlSerializer valueSerializer;

    #endregion

    #region Private Properties

    protected XmlSerializer ValueSerializer
    {
        get
        {
            if (valueSerializer == null)
            {
                valueSerializer = new XmlSerializer(typeof (TVal));
            }
            return valueSerializer;
        }
    }

    #endregion

    #region Constants

    private const string ListNodeName = "List";
    private const string ItemNodeName = "Item";
    private const string ValueNodeName = "Value";

    #endregion

    #region Constructors

    public SerializableList()
    {
    }

    public SerializableList(IList<TVal> list)
        : base(list)
    {
    }

    public SerializableList(int capacity)
        : base(capacity)
    {
    }

    #endregion

    #region ISerializable Members

    protected SerializableList(SerializationInfo info, StreamingContext context)
    {
        var itemCount = info.GetInt32("ItemCount");
        for (var i = 0; i < itemCount; i++)
        {
            var val = (TVal) info.GetValue(string.Format("Item{0}", i), typeof (TVal));
            Add(val);
        }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("ItemCount", Count);
        var itemIdx = 0;
        foreach (var val in this)
        {
            info.AddValue(string.Format("Item{0}", itemIdx), val, typeof (TVal));
            itemIdx++;
        }
    }

    #endregion

    #region IXmlSerializable Members

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        //writer.WriteStartElement(ListNodeName);
        foreach (var val in this)
        {
            writer.WriteStartElement(ValueNodeName);
            ValueSerializer.Serialize(writer, val);
            writer.WriteEndElement();
        }
        //writer.WriteEndElement();
    }

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        // Move past container
        if (!reader.Read())
        {
            throw new XmlException("Error in Deserialization of List");
        }

        //This is to ensure bad illegal chars are ignored when received via WCF
        if (reader.Settings != null) reader.Settings.CheckCharacters = false;

        //reader.ReadStartElement(DictionaryNodeName);
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            reader.ReadStartElement(ValueNodeName);
            var value = (TVal) ValueSerializer.Deserialize(reader);
            reader.ReadEndElement();
            Add(value);
            reader.MoveToContent();
        }
        //reader.ReadEndElement();
        reader.ReadEndElement(); // Read End Element to close Read of containing node
    }

    XmlSchema IXmlSerializable.GetSchema()
    {
        return null;
    }

    #endregion
}
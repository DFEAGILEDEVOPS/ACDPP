using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

[Serializable]
public class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IXmlSerializable, ISerializable
{
    public void AddRange(SerializableDictionary<TKey, TVal> dictionary)
    {
        if (dictionary == null) return;
        foreach (var kvp in dictionary)
        {
            this[kvp.Key] = dictionary[kvp.Key];
        }
    }

    public string ToString(TKey key)
    {
        if (!ContainsKey(key)) return null;
        return this[key] as string;
    }

    public Guid ToGuid(TKey key)
    {
        if (!ContainsKey(key)) return Guid.Empty;
        object value = this[key];
        if (value is string) return Guid.Parse((string) value);
        return (Guid) value;
    }

    public byte ToByte(TKey key)
    {
        if (!ContainsKey(key)) return 0;
        var value = this[key] as string;
        if (value != null) return byte.Parse(value);
        return Convert.ToByte(this[key]);
    }

    public int ToInt(TKey key)
    {
        if (!ContainsKey(key)) return 0;
        var value = this[key] as string;
        if (value != null) return int.Parse(value);
        return Convert.ToInt32(this[key]);
    }

    public long ToLong(TKey key)
    {
        if (!ContainsKey(key)) return 0;
        var value = this[key] as string;
        if (value != null) return long.Parse(value);
        return Convert.ToInt64(this[key]);
    }

    public bool ToBoolean(TKey key)
    {
        if (!ContainsKey(key)) return false;
        var value = this[key] as string;
        if (value != null) return bool.Parse(value);
        return Convert.ToBoolean(this[key]);
    }

    #region Constants

    private const string DictionaryNodeName = "Dictionary";
    private const string ItemNodeName = "Item";
    private const string KeyNodeName = "Key";
    private const string ValueNodeName = "Value";

    #endregion

    #region Constructors

    public SerializableDictionary()
    {
    }

    public SerializableDictionary(IDictionary<TKey, TVal> dictionary)
        : base(dictionary)
    {
    }

    public SerializableDictionary(IEqualityComparer<TKey> comparer)
        : base(comparer)
    {
    }

    public SerializableDictionary(int capacity)
        : base(capacity)
    {
    }

    public SerializableDictionary(IDictionary<TKey, TVal> dictionary, IEqualityComparer<TKey> comparer)
        : base(dictionary, comparer)
    {
    }

    public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        : base(capacity, comparer)
    {
    }

    public new TVal this[TKey key]
    {
        get
        {
            if (ContainsKey(key)) return base[key];
            return default(TVal);
        }
        set
        {
            if (ContainsKey(key))
                base[key] = value;
            else
                Add(key, value);
        }
    }

    #endregion

    #region ISerializable Members

    protected SerializableDictionary(SerializationInfo info, StreamingContext context)
    {
        var itemCount = info.GetInt32("ItemCount");
        for (var i = 0; i < itemCount; i++)
        {
            var kvp = (KeyValuePair<TKey, TVal>) info.GetValue(string.Format("Item{0}", i), typeof (KeyValuePair<TKey, TVal>));
            Add(kvp.Key, kvp.Value);
        }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("ItemCount", Count);
        var itemIdx = 0;
        foreach (var kvp in this)
        {
            info.AddValue(string.Format("Item{0}", itemIdx), kvp, typeof (KeyValuePair<TKey, TVal>));
            itemIdx++;
        }
    }

    #endregion

    #region IXmlSerializable Members

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        //writer.WriteStartElement(DictionaryNodeName);
        foreach (var kvp in this)
        {
            writer.WriteStartElement(ItemNodeName);
            writer.WriteStartElement(KeyNodeName);
            KeySerializer.Serialize(writer, kvp.Key);
            writer.WriteEndElement();
            writer.WriteStartElement(ValueNodeName);
            ValueSerializer.Serialize(writer, kvp.Value);
            writer.WriteEndElement();
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
            throw new XmlException("Error in Deserialization of Dictionary");
        }

        //This is to ensure bad illegal chars are ignored when received via WCF
        if (reader.Settings != null) reader.Settings.CheckCharacters = false;

        //reader.ReadStartElement(DictionaryNodeName);
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            reader.ReadStartElement(ItemNodeName);
            reader.ReadStartElement(KeyNodeName);
            var key = (TKey) KeySerializer.Deserialize(reader);
            reader.ReadEndElement();
            reader.ReadStartElement(ValueNodeName);
            var value = (TVal) ValueSerializer.Deserialize(reader);
            reader.ReadEndElement();
            reader.ReadEndElement();
            Add(key, value);
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

    private XmlSerializer KeySerializer
    {
        get
        {
            if (keySerializer == null)
            {
                keySerializer = new XmlSerializer(typeof (TKey));
            }
            return keySerializer;
        }
    }

    #endregion

    #region Private Members

    private XmlSerializer keySerializer;
    private XmlSerializer valueSerializer;

    #endregion
}
#include <GoApi/Properties/Serializers/XmlSerializer.h>
#include <GoApi/Properties/Serializers/StringConvert.h>
#include <GoApi/Properties/Serializers/XmlAccessors.h>

namespace Go
{
namespace Properties
{

class XmlValueWriter : public IValueWriter
{
public:
    XmlValueWriter() :
        isWritingArray(false)
    {
    }

    ~XmlValueWriter()
    {
    }

    // Sets the XML item to write to
    void SetXmlItem(kXml xml, kXmlItem item)
    {
        this->xml = xml;
        this->item = item;
    }

    void WriteChar(k8s value) override
    {
        SetItemValue(value);
    }

    void WriteUChar(k8u value) override
    {
        SetItemValue(value);
    }

    void WriteShort(k16s value) override
    {
        SetItemValue(value);
    }

    void WriteUShort(k16u value) override
    {
        SetItemValue(value);
    }

    void WriteInt(k32s value) override
    {
        SetItemValue(value);
    }

    void WriteUInt(k32u value) override
    {
        SetItemValue(value);
    }

    void WriteLong(k64s value) override
    {
        SetItemValue(value);
    }

    void WriteULong(k64u value) override
    {
        SetItemValue(value);
    }

    void WriteFloat(k32f value) override
    {
        SetItemValue(value);
    }

    void WriteDouble(k64f value) override
    {
        SetItemValue(value);
    }

    void WriteBool(bool value) override
    {
        SetItemValue(value);
    }

    void WriteString(const kChar* value) override
    {
        SetItemValue<std::string>(value);
    }

    void WriteBinary(const ByteVector value) override
    {
        // Xml doesn't support binary. Throw exception.
        // In the future, may want to encode to Base64.
        GoThrowMsg(kERROR_UNIMPLEMENTED, "XML Serialization does not support binary.");
    }

    void BeginWriteArray() override
    {
        isWritingArray = true;
        isFirstArrayItem = true;
        csvStream.str("");
    }

    void EndWriteArray() override
    {
        GoTest(kXml_SetItemText(xml, item, csvStream.str().c_str()));
        isWritingArray = false;
    }

private:
    kXml xml;
    kXmlItem item;
    bool isWritingArray;
    bool isFirstArrayItem;

    std::stringstream csvStream;

    template <typename T>
    void OutputCsvItem(const T& value)
    {
        if (isFirstArrayItem)
        {
            isFirstArrayItem = false;
        }
        else
        {
            csvStream << ",";
        }

        csvStream << value;
    }

    template <typename T>
    void SetItemValue(const T& value)
    {
        if (isWritingArray)
        {
            OutputCsvItem(value);
        }
        else
        {
            SerializersInternal::XmlAccessor<T>::Set(xml, item, value);
        }
    }
};

class XmlValueReader : public IValueReader
{
public:
    XmlValueReader() :
        arrayState(ArrayState::NONE)
    {
        GoTest(kString_Construct(arrayBuffer.Ref(), kNULL, kAlloc_App()));
        GoTest(kString_Construct(arrayToken.Ref(), kNULL, kAlloc_App()));
    }

    ~XmlValueReader()
    {
    }

    // Sets the XML item to read from
    void SetXmlItem(kXml xml, kXmlItem item)
    {
        this->xml = xml;
        this->item = item;
    }

    k8s ReadChar() override
    {
        return ReadItemValue<k8s>();
    }

    k8u ReadUChar() override
    {
        return ReadItemValue<k8u>();
    }

    k16s ReadShort() override
    {
        return ReadItemValue<k16s>();
    }

    k16u ReadUShort() override
    {
        return ReadItemValue<k16u>();
    }

    k32s ReadInt() override
    {
        return ReadItemValue<k32s>();
    }

    k32u ReadUInt() override
    {
        return ReadItemValue<k32u>();
    }

    k64s ReadLong() override
    {
        return ReadItemValue<k64s>();
    }

    k64u ReadULong() override
    {
        return ReadItemValue<k64u>();
    }

    k32f ReadFloat() override
    {
        return ReadItemValue<k32f>();
    }

    k64f ReadDouble() override
    {
        return ReadItemValue<k64f>();
    }

    bool ReadBool() override
    {
        return ReadItemValue<bool>();
    }

    std::string ReadString() override
    {
        return ReadItemValue<std::string>();
    }

    ByteVector ReadBinary() override
    {
        // Xml doesn't support binary. Throw exception.
        GoThrowMsg(kERROR_UNIMPLEMENTED, "XML Deserialization does not support binary.");
    }

    void BeginReadArray() override
    {
        GoTest(kXml_ItemString(xml, item, arrayBuffer));
        arrayBufferPos = 0;
        arrayState = ArrayState::INIT;
    }

    bool NextArrayItem() override
    {
        if (arrayState == ArrayState::INIT || arrayState == ArrayState::READING)
        {
            if (arrayBufferPos < kString_Length(arrayBuffer))
            {
                const kChar* delimPtr = kStrFindFirst(kString_Chars(arrayBuffer) + arrayBufferPos, ",");
                kSize tokenEnd; // end of token, exclusive (i.e. immediately after last valid index)

                if (delimPtr)
                {
                    tokenEnd = delimPtr - kString_Chars(arrayBuffer);
                }
                else
                {
                    tokenEnd = kString_Length(arrayBuffer);
                }

                kSize tokenLen = tokenEnd - arrayBufferPos;

                if (tokenLen > 0)
                {
                    GoTest(kString_Clear(arrayToken));
                    GoTest(kString_AddSubstring(arrayToken, kString_Chars(arrayBuffer), arrayBufferPos, tokenLen));

                    arrayState = ArrayState::READING;
                    arrayBufferPos = tokenEnd + 1;

                    return true;
                }
            }
        }

        arrayState = ArrayState::NONE;
        return false;
    }

    void EndReadArray() override
    {
        arrayState = ArrayState::NONE;
    }

private:
    kXml xml;
    kXmlItem item;

    enum struct ArrayState
    {
        NONE,
        INIT,
        READING
    };

    ArrayState arrayState;
    Go::Object<kString> arrayBuffer;
    kSize arrayBufferPos;
    Go::Object<kString> arrayToken;

    template <typename T>
    T ReadItemValue()
    {
        T val;

        if (arrayState == ArrayState::NONE)
        {
            SerializersInternal::XmlAccessor<T>::Get(xml, item, val);
        }
        else
        {
            SerializersInternal::StringFormatter<T>::FromString(kString_Chars(arrayToken), val);
        }

        return val;
    }
};

class XmlSerializerImpl : public INodeVisitorConst
{
public:
    void Serialize(const Node& node, kXml xml, kXmlItem root)
    {
        this->xml = xml;
        stack.emplace(&node, root);

        while (!stack.empty())
        {
            const Node& curNode = *stack.top().node;
            curXmlItem = stack.top().xmlItem;
            stack.pop();

            curNode.Accept(*this);
        }
    }

    void Visit(const Structure& node) override
    {
        for (auto it = node.begin(); it != node.end(); it++)
        {
            auto& childName = it->first;
            auto& childNode = it->second;
            kXmlItem childItem;

            GoTest(kXml_AddItem(xml, curXmlItem, childName.c_str(), &childItem));
            stack.emplace(&childNode, childItem);
        }
    }

    void Visit(const Internal::ArrayBase& node) override
    {
        for (size_t i = 0; i < node.Count(); ++i)
        {
            kXmlItem childItem;

            GoTest(kXml_AddItem(xml, curXmlItem, "Item", &childItem));
            stack.emplace(node.At(i), childItem);
        }
    }

    void Visit(const Internal::ReferenceArray& node) override
    {
        for (size_t i = 0; i < node.Count(); ++i)
        {
            kXmlItem childItem;

            GoTest(kXml_AddItem(xml, curXmlItem, "Item", &childItem));
            stack.emplace(node.At(i), childItem);
        }
    }

    void Visit(const Internal::ValueBase& node) override
    {
        valueWriter.SetXmlItem(xml, curXmlItem);
        node.WriteValue(valueWriter);
    }

private:
    struct NodeRef
    {
        const Node* node;
        kXmlItem xmlItem;

        NodeRef(const Node* node, kXmlItem xmlItem) :
            node(node), xmlItem(xmlItem)
        {
        }
    };

    std::stack<NodeRef> stack;
    kXml xml;
    kXmlItem curXmlItem;
    XmlValueWriter valueWriter;
};

class XmlDeserializerImpl : public INodeVisitor
{
public:
    XmlDeserializerImpl()
    {
    }

    void Deserialize(Node& node, kXml xml, kXmlItem root)
    {
        this->xml = xml;
        stack.emplace(&node, root);

        while (!stack.empty())
        {
            Node& curNode = *stack.top().node;
            curXmlItem = stack.top().xmlItem;
            stack.pop();

            curNode.Accept(*this);
        }
    }

    void Visit(Structure& node) override
    {
        node.SetIsRead(curXmlItem != nullptr);

        for (auto it = node.begin(); it != node.end(); ++it)
        {
            auto& name = it->first;
            auto& node = it->second;

            if (node.WriteOnly())
            {
                continue;
            }

            kXmlItem childXmlItem = kNULL;

            if (curXmlItem)
            {
                childXmlItem = kXml_Child(xml, curXmlItem, name.c_str());
            }

            stack.emplace(&node, childXmlItem);
        }
    }

    void Visit(Internal::ArrayBase& node) override
    {
        node.SetIsRead(curXmlItem != nullptr);

        if (curXmlItem)
        {
            std::vector<kXmlItem> xmlItemBuffer;

            for (kXmlItem childXmlItem = kXml_FirstChild(xml, curXmlItem);
                childXmlItem != kNULL;
                childXmlItem = kXml_NextSibling(xml, childXmlItem))
            {
                xmlItemBuffer.push_back(childXmlItem);
            }

            node.Resize(xmlItemBuffer.size());

            for (size_t i = 0; i < node.Count(); ++i)
            {
                stack.emplace(node.At(i), xmlItemBuffer.at(i));
            }
        }
        else
        {
            for (size_t i = 0; i < node.Count(); ++i)
            {
                stack.emplace(node.At(i), (kXmlItem)kNULL);
            }
        }        
    }

    void Visit(Internal::ReferenceArray& node) override
    {
        // ReferenceArray is used only in schemas, which are never read.
    }

    void Visit(Internal::ValueBase& node) override
    {
        node.SetIsRead(curXmlItem != nullptr);

        if (curXmlItem)
        {
            valueReader.SetXmlItem(xml, curXmlItem);
            node.ReadValue(valueReader);
        }        
    }

private:
    struct NodeRef
    {
        Node* node;
        kXmlItem xmlItem;

        NodeRef(Node* node, kXmlItem xmlItem) :
            node(node), xmlItem(xmlItem)
        {
        }
    };

    std::stack<NodeRef> stack;
    kXml xml;
    kXmlItem curXmlItem;
    XmlValueReader valueReader;
};

void XmlSerializer::Serialize(const Node& node, kXml xml, kXmlItem root)
{
    XmlSerializerImpl serializer;
    serializer.Serialize(node, xml, root);
}

void XmlSerializer::Deserialize(Node& node, kXml xml, kXmlItem root)
{
    XmlDeserializerImpl deserializer;
    deserializer.Deserialize(node, xml, root);
}

void XmlSerializer::SerializeSchema(const Node& node, kXml xml, kXmlItem root)
{
    if (node.Schema())
    {
        return Serialize(*node.Schema(), xml, root);
    }
    else
    {
        GoTest(kXml_ClearItem(xml, root));
    }
}

}
};

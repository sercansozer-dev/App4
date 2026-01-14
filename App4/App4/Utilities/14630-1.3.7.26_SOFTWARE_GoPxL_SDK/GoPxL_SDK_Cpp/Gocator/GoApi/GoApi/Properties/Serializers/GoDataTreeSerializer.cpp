#include <GoApi/Properties/Serializers/GoDataTreeSerializer.h>

namespace Go
{
namespace Properties
{

class GoDataTreeValueWriter : public IValueWriter
{
public:
    GoDataTreeValueWriter() : tree(kNULL), isWritingArray(false)
    { }

    ~GoDataTreeValueWriter() = default;

    // Sets the GoDataTree to write to.
    void SetGoDataTree(GoDataTree* tree)
    {
        this->tree = tree;
    }

    void WriteChar(k8s value) override
    {
        if (value == k8S_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteUChar(k8u value) override
    {
        if (value == k8U_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteShort(k16s value) override
    {
        if (value == k16S_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteUShort(k16u value) override
    {
        if (value == k16U_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteInt(k32s value) override
    {
        if (value == k32S_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteUInt(k32u value) override
    {
        if (value == k32U_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteLong(k64s value) override
    {
        if (value == k64S_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteULong(k64u value) override
    {
        if (value == k64U_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteFloat(k32f value) override
    {
        if (value == k32F_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteDouble(k64f value) override
    {
        if (value == k64F_NULL)
        {
            SetPrimitiveValue(nullptr);
        }
        else
        {
            SetPrimitiveValue(value);
        }
    }

    void WriteBool(bool value) override
    {
        SetPrimitiveValue(value);
    }

    void WriteString(const kChar* str) override
    {
        if (isWritingArray)
        {
            tree->PushBack<std::string>(str);
        }
        else
        {
            tree->Set<std::string>(str);
        }
    }

    void WriteBinary(const ByteVector bin) override
    {
        if (isWritingArray)
        {
            tree->PushBack((kArray1)bin);
        }
        else
        {
            tree->SetBinary((kArray1)bin);
        }
    }

    void BeginWriteArray() override
    {
        tree->MarkAsArray();
        isWritingArray = true;
    }

    void EndWriteArray() override
    {
        isWritingArray = false;
    }

private:
    template <typename T>
    void SetPrimitiveValue(const T& value)
    {
        if (isWritingArray)
        {
            tree->PushBack(value);
        }
        else
        {
            tree->Set(value);
        }
    }

private:
    GoDataTree* tree;

    bool isWritingArray;
};

class GoDataTreeValueReader : public IValueReader
{
private:
    enum struct ArrayState
    {
        NONE,
        INIT,
        READING
    };

public:
    GoDataTreeValueReader() : tree(kNULL), arrayIndex(0), arrayState(ArrayState::NONE)
    { }

    ~GoDataTreeValueReader() = default;

    // Sets the GoDataTree to read from.
    void SetGoDataTree(GoDataTree* tree)
    {
        this->tree = tree;
    }

    k8s ReadChar() override
    {
        if (ValueIsNull())
        {
            return k8S_NULL;
        }

        return ReadValue<k8s>();
    }

    k8u ReadUChar() override
    {
        if (ValueIsNull())
        {
            return k8U_NULL;
        }

        if (ValueIsNegative<k8u>())
        {
            GoThrowMsg(kERROR_PARAMETER, "Expected unsigned integer but the value is negative.");
        }

        return ReadValue<k8u>();
    }

    k16s ReadShort() override
    {
        if (ValueIsNull())
        {
            return k16S_NULL;
        }

        return ReadValue<k16s>();
    }

    k16u ReadUShort() override
    {
        if (ValueIsNull())
        {
            return k16U_NULL;
        }

        if (ValueIsNegative<k16u>())
        {
            GoThrowMsg(kERROR_PARAMETER, "Expected unsigned integer but the value is negative.");
        }

        return ReadValue<k16u>();
    }

    k32s ReadInt() override
    {
        if (ValueIsNull())
        {
            return k32S_NULL;
        }

        return ReadValue<k32s>();
    }

    k32u ReadUInt() override
    {
        if (ValueIsNull())
        {
            return k32U_NULL;
        }

        if (ValueIsNegative<k32u>())
        {
            GoThrowMsg(kERROR_PARAMETER, "Expected unsigned integer but the value is negative.");
        }

        return ReadValue<k32u>();
    }

    k64s ReadLong() override
    {
        if (ValueIsNull())
        {
            return k64S_NULL;
        }

        return ReadValue<k64s>();
    }

    k64u ReadULong() override
    {
        if (ValueIsNull())
        {
            return k64U_NULL;
        }

        if (ValueIsNegative<k64u>())
        {
            GoThrowMsg(kERROR_PARAMETER, "Expected unsigned integer but the value is negative.");
        }

        return ReadValue<k64u>();
    }

    k32f ReadFloat() override
    {
        if (ValueIsNull())
        {
            return k32F_NULL;
        }

        return ReadValue<k32f>();
    }

    k64f ReadDouble() override
    {
        if (ValueIsNull())
        {
            return k64F_NULL;
        }

        return ReadValue<k64f>();
    }

    bool ReadBool() override
    {
        return ReadValue<bool>();
    }

    std::string ReadString() override
    {
        return ReadValue<std::string>();
    }

    ByteVector ReadBinary() override
    {
        ByteVector binary;

        if (arrayState == ArrayState::READING)
        {
            binary = (*tree)[arrayIndex].GetBinary();
        }
        else
        {
            binary = tree->GetBinary();
        }

        return binary;
    }

    void BeginReadArray() override
    {
        if (tree->IsArray())
        {
            arrayState = ArrayState::INIT;
        }
        else
        {
            arrayState = ArrayState::NONE;
        }
    }

    bool NextArrayItem() override
    {
        switch (arrayState)
        {
            case ArrayState::INIT:
            {
                arrayIndex = 0;

                break;
            }
            case ArrayState::READING:
            {
                arrayIndex++;

                break;
            }
            case ArrayState::NONE:
            {
                return false;
            }
        }

        if (arrayIndex < tree->Count())
        {
            arrayState = ArrayState::READING;
        }
        else
        {
            arrayState = ArrayState::NONE;
        }

        return arrayState == ArrayState::READING;
    }

    void EndReadArray() override
    {
        arrayState = ArrayState::NONE;
    }

private:
    template <typename T>
    T ReadValue()
    {
        if (arrayState == ArrayState::READING)
        {
            return (*tree)[arrayIndex].Get<T>();
        }
        else
        {
            return tree->Get<T>();
        }
    }

    bool ValueIsNull()
    {
        if (arrayState == ArrayState::READING)
        {
            return (*tree)[arrayIndex].IsNull();
        }
        else
        {
            return tree->IsNull();
        }
    }

    template <typename T>
    bool ValueIsNegative()
    {
        if (!std::is_arithmetic<T>())
        {
            // Non-numeric (int/float) types are never negative.
            return false;
        }

        // 0 is signed by default, which makes the json value signed too when compared.
        // This makes large unsigned json values negative, leading to false positives.
        // Casting 0 unsigned prevents this.
        if (arrayState == ArrayState::READING)
        {
            return (*tree)[arrayIndex].IsNegativeNumber();
        }
        else
        {
            return (*tree).IsNegativeNumber();
        }
    }

private:
    GoDataTree* tree;

    k32u arrayIndex;
    ArrayState arrayState;
};

class GoDataTreeSerializerImpl : public INodeVisitorConst
{
public:
    void Serialize(const Node& node, GoDataTree& output)
    {
        stack.emplace(&node, output);

        while (!stack.empty())
        {
            const Node& currentNode = *stack.top().node;

            currentTree.Reset(stack.top().tree);

            stack.pop();

            currentNode.Accept(*this);
        }
    }

    void Visit(const Structure& node) override
    {
        currentTree.MarkAsObject();

        for (auto it = node.begin(); it != node.end(); it++)
        {
            GoDataTree tree = currentTree[it->first];

            stack.emplace(&it->second, tree);
        }
    }

    void Visit(const Internal::ArrayBase& node) override
    {
        currentTree.Resize(node.Count());

        for (size_t i = 0; i < node.Count(); ++i)
        {
            GoDataTree tree = currentTree[(k32u)i];

            stack.emplace(node.At(i), tree);
        }
    }

    void Visit(const Internal::ReferenceArray& node) override
    {
        currentTree.Resize(node.Count());

        for (size_t i = 0; i < node.Count(); ++i)
        {            
            GoDataTree tree = currentTree[(k32u)i];

            stack.emplace(node.At(i), tree);
        }
    }

    void Visit(const Internal::ValueBase& node) override
    {
        valueWriter.SetGoDataTree(&currentTree);

        node.WriteValue(valueWriter);
    }

private:
    struct NodeRef
    {
        const Node* node;

        GoDataTree tree;

        NodeRef(const Node* node) : node(node)
        { }

        NodeRef(const Node* node, GoDataTree& tree) :
            node(node), tree(tree)
        { }
    };

private:
    std::stack<NodeRef> stack;

    GoDataTree currentTree;
    GoDataTreeValueWriter valueWriter;
};

class GoDataTreeDeserializerImpl : public INodeVisitor
{
public:
    void Deserialize(Node& node, const GoDataTree& tree)
    {
        stack.emplace(&node, (GoDataTree&)tree);

        while (!stack.empty())
        {
            Node& curNode = *stack.top().node;

            currentTree.Reset(stack.top().tree);
            isCurrentTreeValid = stack.top().isValidTree;

            stack.pop();

            curNode.Accept(*this);
        }
    }

    void Visit(Structure& node) override
    {
        node.SetIsRead(isCurrentTreeValid);

        for (auto it = node.begin(); it != node.end(); ++it)
        {
            auto& name = it->first;
            auto& node = it->second;

            if (node.WriteOnly())
            {
                continue;
            }

            if (isCurrentTreeValid)
            {
                GoApi::GoDataTreeIterator treeIt = currentTree.Find(name);

                if (treeIt)
                {
                    GoDataTree tree = *treeIt;

                    stack.emplace(&node, tree);
                    continue;
                }
            }

            stack.emplace(&node);
        }
    }

    void Visit(Internal::ArrayBase& node) override
    {
        node.SetIsRead(isCurrentTreeValid);
        
        if (isCurrentTreeValid)
        {
            GoThrowIf(!currentTree.IsArray(), kERROR_PARAMETER);

            node.Resize(currentTree.Count());

            for (size_t i = 0; i < node.Count(); ++i)
            {
                GoDataTree tree = currentTree.At((k32u)i);

                stack.emplace(node.At(i), tree);
            }
        }
        else
        {
            for (size_t i = 0; i < node.Count(); ++i)
            {
                stack.emplace(node.At(i));
            }
        }
    }

    void Visit(Internal::ReferenceArray& node) override
    {
        // ReferenceArray is used only in schemas, which are never read.
    }

    void Visit(Internal::ValueBase& node) override
    {
        node.SetIsRead(isCurrentTreeValid);

        if (isCurrentTreeValid)
        {
            valueReader.SetGoDataTree(&currentTree);

            node.ReadValue(valueReader);
        }
    }

private:
    struct NodeRef
    {
        Node* node;

        GoDataTree tree;
        bool isValidTree;

        NodeRef(Node* node) : node(node), tree(), isValidTree(false)
        { }

        NodeRef(Node* node, GoDataTree& tree) :
            node(node), tree(tree), isValidTree(true)
        { }
    };

private:
    std::stack<NodeRef> stack;

    GoDataTree currentTree;
    bool isCurrentTreeValid;

    GoDataTreeValueReader valueReader;
};

class GoDataTreeValidatorImpl : public INodeVisitorConst
{
public:
    /**
     * Traverses through the GoDataTree, checking validation criteria for each value.
     * Currently throws if a discrepancy is identified.
     * Future improvements include container validation support and a returned list of errors (as opposed to throwing at the first one).
     */
    void Validate(const Node& node, const GoDataTree& tree)
    {
        nodeStack.emplace(&node, (GoDataTree&)tree);

        while (!nodeStack.empty())
        {
            const Node& curNode = *nodeStack.top().node;

            currentTree.Reset(nodeStack.top().tree);
            isCurrentTreeValid = nodeStack.top().isValidTree;

            nodeStack.pop();

            curNode.Accept(*this);
        }
    }

    void Visit(const Structure& node) override
    {
        std::vector<std::string> allNames;

        for (auto it = node.begin(); it != node.end(); ++it)
        {
            auto& name = it->first;
            auto& node = it->second;

            allNames.push_back(name);

            if (node.WriteOnly())
            {
                continue;
            }

            if (isCurrentTreeValid)
            {
                GoApi::GoDataTreeIterator treeIt = currentTree.Find(name);

                if (treeIt)
                {
                    GoDataTree tree = *treeIt;

                    nodeStack.emplace(&node, tree);

                    continue;
                }
            }

            nodeStack.emplace(&node);
        }

        // TODO: add structure validation once implemented
        // useful args: allNames
        // node.CheckValidation(args...)
    }

    void Visit(const Internal::ArrayBase& node) override
    {
        if (isCurrentTreeValid)
        {
            GoThrowIf(!currentTree.IsArray(), kERROR_PARAMETER);

            size_t arrayCount = node.Count() > currentTree.Count() ? currentTree.Count() : node.Count();

            for (size_t i = 0; i < arrayCount; ++i)
            {
                GoDataTree tree = currentTree.At((k32u)i);

                nodeStack.emplace(node.At(i), tree);
            }
        }
        else
        {
            for (size_t i = 0; i < node.Count(); ++i)
            {
                nodeStack.emplace(node.At(i));
            }
        }

        // TODO: add array validation once implemented
        // useful args: currentTree->Count()
        // node.CheckValidation(args...)
    }

    void Visit(const Internal::ReferenceArray& node) override
    {
        // ReferenceArray is used only in schemas, which are never read.
    }

    void Visit(const Internal::ValueBase& node) override
    {
        if (isCurrentTreeValid)
        {
            valueReader.SetGoDataTree(&currentTree);

            node.ValidateValue(valueReader);
        }
    }

private:
    struct NodeRef
    {
        const Node* node;

        GoDataTree tree;
        bool isValidTree;

        NodeRef(const Node* node) : node(node), tree(), isValidTree(false)
        { }

        NodeRef(const Node* node, GoDataTree& tree) :
            node(node), tree(tree), isValidTree(true)
        { }
    };

private:
    std::stack<NodeRef> nodeStack;

    GoDataTree currentTree;
    bool isCurrentTreeValid;

    GoDataTreeValueReader valueReader;
};

void GoDataTreeSerializer::Serialize(const Node& node, GoDataTree tree)
{
    GoDataTreeSerializerImpl serializer;
    
    serializer.Serialize(node, tree);
}

void GoDataTreeSerializer::Deserialize(Node& node, const GoDataTree tree)
{
    GoDataTreeDeserializerImpl deserializer;
    
    deserializer.Deserialize(node, tree);
}

void GoDataTreeSerializer::ValidateDataTree(const Node& node, const GoDataTree tree)
{
    GoDataTreeValidatorImpl validator;
    
    validator.Validate(node, tree);
}

void GoDataTreeSerializer::SerializeSchema(const Node& node, GoDataTree tree)
{
    if (node.Schema())
    {
        Serialize(*node.Schema(), tree);
    }
}

ByteVector GoDataTreeSerializer::DecipherBinaryJson(const GoDataTree& binary)
{
    if (binary.IsBinary())
    {
        return binary.GetBinary();
    }
    else if (binary.IsObject())
    {
        if (binary["bytes"].IsBinary())
        {
            return binary["bytes"].GetBinary();
        }
        else if (binary["bytes"].IsArray())
        {
            GoDataTree bytes = binary["bytes"];

            Go::Object<kArray1> bin;
            GoTest(kArray1_Construct(bin.Ref(), kTypeOf(kByte), bytes.Count(), kAlloc_App()));

            for (k32u i = 0; i < (k32u)bytes.Count(); i++)
            {
                GoThrowMsgIf(!bytes[i].Is8u(), kERROR_PARAMETER, "Misformatted binary json object.");

                kArray1_SetAsT(bin, i, bytes[i].Get<k8u>(), kByte);
            }

            return bin;
        }
    }

    GoThrowMsg(kERROR_PARAMETER, "Misformatted binary json object.");
}

}} // namespaces
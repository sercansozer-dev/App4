#include <GoApi/Properties/Serializers/JsonSerializer.h>
#include <GoApi/Properties/Nodes/TypeInfo.h>

using nlohmann::json;

namespace Go
{
namespace Properties
{

static const kByte DECODING_MAP[] =
{
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 0
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 16
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 0, 0, 0, 63,               // offset 32
    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 0, 0, 0, 0, 0, 0,       // offset 48
    0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,            // offset 64
    15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 0, 0, 0, 0, 0,      // offset 80
    0, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,  // offset 96
    41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 0, 0, 0, 0, 0,      // offset 112
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 128
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 144
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 160
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 176
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 192
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 208
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 224
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                 // offset 240
};

class JsonValueWriter : public IValueWriter
{
public:
    JsonValueWriter() :
        isWritingArray(false)
    {
    }

    ~JsonValueWriter()
    {
    }

    // Sets the JSON value to write to
    void SetJsonValue(json::pointer jsonValue)
    {
        this->jsonValue = jsonValue;
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
            jsonValue->push_back(std::string(str));
        }
        else
        {
            *jsonValue = std::string(str);
        }
    }

    void WriteBinary(const ByteVector bin) override
    {
        auto binaryJson = json::binary(bin);

        if (isWritingArray)
        {
            jsonValue->push_back(binaryJson);
        }
        else
        {
            *jsonValue = binaryJson;
        }
    }

    void BeginWriteArray() override
    {
        *jsonValue = json::array();
        isWritingArray = true;
    }

    void EndWriteArray() override
    {
        isWritingArray = false;
    }

private:
    json::pointer jsonValue;
    bool isWritingArray;

    template <typename T>
    void SetPrimitiveValue(T value)
    {
        if (isWritingArray)
        {
            jsonValue->push_back(value);
        }
        else
        {
            *jsonValue = value;
        }
    }
};

class JsonValueReader : public IValueReader
{
public:
    /**
     * @param logging       Whether or not to output user logs when issues with reading occur, such as when a number is out of bounds.
     */
    JsonValueReader(bool logging = true) :
        arrayState(ArrayState::NONE),
        logging(logging)
    {
    }

    ~JsonValueReader()
    {
    }

    // Sets the JSON value to read from
    void SetJsonValue(json::const_pointer jsonValue)
    {
        this->jsonValue = jsonValue;
    }

    k8s ReadChar() override
    {
        if (ValueIsNull())
        {
            return k8S_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, (k64s)k8S_MIN))
        {
            // We clamp to k8S_MIN + 1 since k8S_MIN is the same as k8S_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for char. Clamping to %c."), jsonVal.get<k64s>(), k8S_MIN + 1);
            }
            return k8S_MIN + 1;
        }
        else if (AboveMaximum(jsonVal, (k64s)k8S_MAX))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for char. Clamping to %c."), jsonVal.get<k64u>(), k8S_MAX);
            }
            return k8S_MAX;
        }

        return ReadValue<k8s>();
    }

    k8u ReadUChar() override
    {
        if (ValueIsNull())
        {
            return k8U_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, (k64u)k8U_MIN))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for unsigned char. Clamping to %c."), jsonVal.get<k64s>(), k8U_MIN);
            }
            return k8U_MIN;
        }
        else if (AboveMaximum(jsonVal, (k64u)k8U_MAX))
        {
            // We clamp to k8U_MAX - 1 since k8U_MAX is the same as k8U_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for unsigned char. Clamping to %c."), jsonVal.get<k64u>(), k8U_MAX - 1);
            }
            return k8U_MAX - 1;
        }

        return ReadValue<k8u>();
    }

    k16s ReadShort() override
    {
        if (ValueIsNull())
        {
            return k16S_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, (k64s)k16S_MIN))
        {
            // We clamp to k16S_MIN + 1 since k16S_MIN is the same as k16S_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for short. Clamping to %hi."), jsonVal.get<k64s>(), k16S_MIN + 1);
            }
            return k16S_MIN + 1;
        }
        else if (AboveMaximum(jsonVal, (k64s)k16S_MAX))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for short. Clamping to %hi."), jsonVal.get<k64u>(), k16S_MAX);
            }
            return k16S_MAX;
        }

        return ReadValue<k16s>();
    }

    k16u ReadUShort() override
    {
        if (ValueIsNull())
        {
            return k16U_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, (k64u)k16U_MIN))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for unsigned short. Clamping to %hu."), jsonVal.get<k64s>(), k16U_MIN);
            }
            return k16U_MIN;
        }
        else if (AboveMaximum(jsonVal, (k64u)k16U_MAX))
        {
            // We clamp to k16U_MAX - 1 since k16U_MAX is the same as k16U_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for unsigned short. Clamping to %hu."), jsonVal.get<k64u>(), k16U_MAX - 1);
            }
            return k16U_MAX - 1;
        }

        return ReadValue<k16u>();
    }

    k32s ReadInt() override
    {
        if (ValueIsNull())
        {
            return k32S_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, (k64s)k32S_MIN))
        {
            // We clamp to k32S_MIN + 1 since k32S_MIN is the same as k32S_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for int. Clamping to %d."), jsonVal.get<k64s>(), k32S_MIN + 1);
            }
            return k32S_MIN + 1;
        }
        else if (AboveMaximum(jsonVal, (k64s)k32S_MAX))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for int. Clamping to %d."), jsonVal.get<k64u>(), k32S_MAX);
            }
            return k32S_MAX;
        }

        return ReadValue<k32s>();
    }

    k32u ReadUInt() override
    {
        if (ValueIsNull())
        {
            return k32U_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, (k64u)k32U_MIN))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for unsigned int. Clamping to %u."), jsonVal.get<k64s>(), k32U_MIN);
            }
            return k32U_MIN;
        }
        else if (AboveMaximum(jsonVal, (k64u)k32U_MAX))
        {
            // We clamp to k32U_MAX - 1 since k32U_MAX is the same as k32U_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for unsigned int. Clamping to %u."), jsonVal.get<k64u>(), k32U_MAX - 1);
            }
            return k32U_MAX - 1;
        }

        return ReadValue<k32u>();
    }

    k64s ReadLong() override
    {
        if (ValueIsNull())
        {
            return k64S_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, k64S_MIN))
        {
            // We clamp to k64S_MIN + 1 since k64S_MIN is the same as k64S_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for long. Clamping to %lld."), jsonVal.get<k64s>(), k64S_MIN + 1);
            }
            return k64S_MIN + 1;
        }
        else if (AboveMaximum(jsonVal, k64S_MAX))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for long. Clamping to %lld."), jsonVal.get<k64u>(), k64S_MAX);
            }
            return k64S_MAX;
        }

        return ReadValue<k64s>();
    }

    k64u ReadULong() override
    {
        if (ValueIsNull())
        {
            return k64U_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        // GOS-2371: If the value entered is above the max or below the min, it will get clamped to the nearest valid value.
        if (BelowMinimum(jsonVal, k64U_MIN))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %lld is below the minimum for unsigned long. Clamping to %llu."), jsonVal.get<k64s>(), k64U_MIN);
            }
            return k64U_MIN;
        }
        else if (AboveMaximum(jsonVal, k64U_MAX))
        {
            // We clamp to k64U_MAX - 1 since k64U_MAX is the same as k64U_NULL.
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %llu is above the maximum for unsigned long. Clamping to %llu."), jsonVal.get<k64u>(), k64U_MAX - 1);
            }
            return k64U_MAX - 1;
        }

        return ReadValue<k64u>();
    }

    k32f ReadFloat() override
    {
        if (ValueIsNull())
        {
            return k32F_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        if (BelowMinimum(jsonVal, k32F_NULL + k32F_MIN)) // k32F_NULL is the smallest positive value
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %f is below the minimum for float. Clamping to %f."), jsonVal.get<k32f>(), k32F_NULL + k32F_MIN + k32F_MIN);
            }
            return k32F_NULL + k32F_MIN + k32F_MIN;
        }
        if (AboveMaximum(jsonVal, k32F_MAX))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %f is above the maximum for float. Clamping to %f."), jsonVal.get<k32f>(), k32F_MAX);
            }
            return k32F_MAX;
        }

        return ReadValue<k32f>();
    }

    k64f ReadDouble() override
    {
        if (ValueIsNull())
        {
            return k64F_NULL;
        }

        auto& jsonVal = GetValueIfArrayed();

        if (BelowMinimum(jsonVal, k64F_NULL + k64F_MIN)) // k64F_NULL is the smallest positive value.
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %f is below the minimum for double. Clamping to %f."), jsonVal.get<k64f>(), k64F_NULL + k64F_MIN + k64F_MIN);
            }
            return k64F_NULL + k64F_MIN + k64F_MIN;
        }
        if (AboveMaximum(jsonVal, k64F_MAX))
        {
            if (logging)
            {
                GoLogUserWarn("sensor", bepgettext("sensor", "Value %f is above the maximum for double. Clamping to %f."), jsonVal.get<k64f>(), k64F_MAX);
            }
            return k64F_MAX;
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
        JsonSerializer serializer;
        if (arrayState == ArrayState::READING)
        {
            return serializer.DecipherBinaryJson((*jsonValue)[arrayIndex]);
        }
        else
        {
            return serializer.DecipherBinaryJson(*jsonValue);
        }
    }

    void BeginReadArray() override
    {
        if (jsonValue->is_array())
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
            arrayIndex = 0;
            break;
        case ArrayState::READING:
            arrayIndex++;
            break;
        case ArrayState::NONE:
            return false;
        }

        if (arrayIndex < jsonValue->size())
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
    json::const_pointer jsonValue;

    enum struct ArrayState
    {
        NONE,
        INIT,
        READING
    };

    ArrayState arrayState;
    size_t arrayIndex;
    
    // GOS-2371: Added some logs to JsonValueReader. Want to log when deserializing but not when validating, so
    // add logging flag to log only when the calling class desires.
    bool logging;

    // If reading an array, return the json at the current index. If not reading an array, return the current json.
    const json& GetValueIfArrayed()
    {
        return (arrayState == ArrayState::READING ? (*jsonValue)[arrayIndex] : (*jsonValue));
    }

    template <typename T>
    T ReadValue()
    {
        return GetValueIfArrayed().get<T>();
    }

    bool ValueIsNull()
    {
        return GetValueIfArrayed().is_null();
    }

    bool BelowMinimum(const json& jsonVal, k64s minimum)
    {
        // If the json value would overflow when cast to signed, it must be larger than the minimum.
        if (OverflowRisk(jsonVal))
        {
            return false;
        }

        return jsonVal < minimum;
    }

    bool BelowMinimum(const json& jsonVal, k64u minimum)
    {
        // If jsonVal is less than the smallest possible signed number, it must be smaller than the minimum.
        if (jsonVal < k64U_MIN)
        {
            return true;
        }

        // Must cast to k64u as minimum is otherwise cast to signed and could overflow if jsonVal is signed.
        // The cast won't underflow due to the above check against k64U_MIN.
        return jsonVal.get<k64u>() < minimum;
    }

    bool BelowMinimum(const json& jsonVal, k64f minimum)
    {
        // If the json value would overflow when cast to signed, it must be larger than the minimum.
        if (OverflowRisk(jsonVal))
        {
            return false;
        }

        return jsonVal < minimum;
    }

    bool AboveMaximum(const json& jsonVal, k64s maximum)
    {
        // If the json value would overflow when cast to signed, it must be larger than the maximum.
        if (OverflowRisk(jsonVal))
        {
            return true;
        }

        return jsonVal > maximum;
    }

    bool AboveMaximum(const json& jsonVal, k64u maximum)
    {
        // If jsonVal is less than the smallest possible signed number, it must be smaller than the maximum.
        if (jsonVal < k64U_MIN)
        {
            return false;
        }

        // Must cast to k64u as minimum is otherwise cast to signed and could overflow if jsonVal is signed.
        // The cast won't underflow due to the above check against k64U_MIN.
        return jsonVal.get<k64u>() > maximum;
    }

    bool AboveMaximum(const json& jsonVal, k64f maximum)
    {
        // If the json value would overflow when cast to signed, it must be larger than the maximum.
        if (OverflowRisk(jsonVal))
        {
            return true;
        }

        return jsonVal > maximum;
    }

    // GOS-2371: json doesn't like comparing signed and unsigned integers.
    // When comparing signed and unsigned, the unsigned is cast to signed before comparison.
    // This function returns true if the current json value would overflow if treated as signed.
    bool OverflowRisk(const json& jsonVal)
    {
        return ((jsonVal.type() == json::value_t::number_unsigned) && (jsonVal.get<k64u>() > k64S_MAX));
    }
};

class JsonSerializerImpl : public INodeVisitorConst
{
public:
    json Serialize(const Node& node)
    {
        json output;

        stack.emplace(&node, &output);

        while (!stack.empty())
        {
            const Node& curNode = *stack.top().node;
            curJson = stack.top().jsonPtr;
            stack.pop();

            curNode.Accept(*this);
        }

        return output;
    }

    void Visit(const Structure& node) override
    {
        *curJson = json::object();

        for (auto it = node.begin(); it != node.end(); it++)
        {
            auto& jsonItem = (*curJson)[it->first];
            stack.emplace(&it->second, &jsonItem);
        }
    }

    void Visit(const Internal::ArrayBase& node) override
    {
        *curJson = json(node.Count(), nullptr);

        for (size_t i = 0; i < node.Count(); ++i)
        {
            auto& jsonItem = (*curJson)[i];
            stack.emplace(node.At(i), &jsonItem);
        }
    }

    void Visit(const Internal::ReferenceArray& node) override
    {
        *curJson = json(node.Count(), nullptr);

        for (size_t i = 0; i < node.Count(); ++i)
        {
            auto& jsonItem = (*curJson)[i];
            stack.emplace(node.At(i), &jsonItem);
        }
    }

    void Visit(const Internal::ValueBase& node) override
    {
        valueWriter.SetJsonValue(curJson);
        node.WriteValue(valueWriter);
    }

private:
    struct NodeRef
    {
        const Node* node;
        json::pointer jsonPtr;

        NodeRef(const Node* node, json::pointer jsonPtr) :
            node(node), jsonPtr(jsonPtr)
        {
        }
    };

    std::stack<NodeRef> stack;
    json::pointer curJson;
    JsonValueWriter valueWriter;
};

class JsonDeserializerImpl : public INodeVisitor
{
public:
    void Deserialize(Node& node, const json& json)
    {
        stack.emplace(&node, &json);

        while (!stack.empty())
        {
            Node& curNode = *stack.top().node;
            curJson = stack.top().jsonPtr;
            stack.pop();

            curNode.Accept(*this);
        }
    }

    void Visit(Structure& node) override
    {
        node.SetIsRead(curJson != nullptr);

        for (auto it = node.begin(); it != node.end(); ++it)
        {
            auto& name = it->first;
            auto& node = it->second;

            if (node.WriteOnly())
            {
                continue;
            }

            json::const_pointer jsonItem = nullptr;

            if (curJson)
            {
                auto jsonIt = curJson->find(name);
                if (jsonIt != curJson->end())
                {
                    jsonItem = &jsonIt.value();
                }
            }

            stack.emplace(&node, jsonItem);
        }
    }

    void Visit(Internal::ArrayBase& node) override
    {
        node.SetIsRead(curJson != nullptr);

        if (curJson)
        {
            // Modified based on GOS-1337
            GoThrowIf(!curJson->is_array(), kERROR_PARAMETER);

            node.Resize(curJson->size());

            for (size_t i = 0; i < node.Count(); ++i)
            {
                stack.emplace(node.At(i), &curJson->at(i));
            }
        }
        else
        {
            for (size_t i = 0; i < node.Count(); ++i)
            {
                stack.emplace(node.At(i), nullptr);
            }
        }        
    }

    void Visit(Internal::ReferenceArray& node) override
    {
        // ReferenceArray is used only in schemas, which are never read.
    }

    void Visit(Internal::ValueBase& node) override
    {
        node.SetIsRead(curJson != nullptr);

        if (curJson)
        {
            valueReader.SetJsonValue(curJson);
            node.ReadValue(valueReader);
        }        
    }

private:
    struct NodeRef
    {
        Node* node;
        json::const_pointer jsonPtr;

        NodeRef(Node* node, json::const_pointer jsonPtr) :
            node(node), jsonPtr(jsonPtr)
        {
        }
    };

    std::stack<NodeRef> stack;
    json::const_pointer curJson;
    JsonValueReader valueReader;
};

/**
 * Similar to JsonDeserializerImpl, but instead of writing the json data to the node,
 * it checks that the json data satisfies the node's validation criteria.
 */
class JsonValidatorImpl : public INodeVisitorConst
{
public:
    JsonValidatorImpl() : 
        curJson(nullptr),
        valueReader(false) // Disable logging in the value reader to avoid duplicating logs that'll be seen during deserialization.
    {}

    /**
     * Traverses through the json, checking validation criteria for each value.
     * Currently throws if a discrepancy is identified.
     * Future improvements include container validation support and a returned list of errors (as opposed to throwing at the first one).
     */
    void Validate(const Node& node, const json& json)
    {
        stack.emplace(&node, &json);

        while (!stack.empty())
        {
            const Node& curNode = *stack.top().node;
            curJson = stack.top().jsonPtr;
            stack.pop();

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

            json::const_pointer jsonItem = nullptr;

            if (curJson)
            {
                auto jsonIt = curJson->find(name);
                if (jsonIt != curJson->end())
                {
                    jsonItem = &jsonIt.value();
                }
            }

            stack.emplace(&node, jsonItem);
        }

        // TODO: add structure validation once implemented
        // useful args: allNames
        // node.CheckValidation(args...)
    }

    void Visit(const Internal::ArrayBase& node) override
    {
        if (curJson)
        {
            // Modified based on GOS-1337
            GoThrowIf(!curJson->is_array(), kERROR_PARAMETER);

            // GOS-3739 - Depending if the json array has less or more entries
            // than the node, we need to index properly.
            // Only validate to the smallest size array to avoid out of bounds indexing.
            size_t arrayCount = node.Count() > curJson->size() ? curJson->size() : node.Count();
            for (size_t i = 0; i < arrayCount; ++i)
            {
                stack.emplace(node.At(i), &curJson->at(i));
            }
        }
        else
        {
            for (size_t i = 0; i < node.Count(); ++i)
            {
                stack.emplace(node.At(i), nullptr);
            }
        }

        // TODO: add array validation once implemented
        // useful args: curJson->size()
        // node.CheckValidation(args...)
    }

    void Visit(const Internal::ReferenceArray& node) override
    {
        // ReferenceArray is used only in schemas, which are never read.
    }

    void Visit(const Internal::ValueBase& node) override
    {
        if (curJson)
        {
            valueReader.SetJsonValue(curJson);
            node.ValidateValue(valueReader);
        }        
    }

private:
    struct NodeRef
    {
        const Node* node;
        json::const_pointer jsonPtr;

        NodeRef(const Node* node, json::const_pointer jsonPtr) :
            node(node), jsonPtr(jsonPtr)
        {
        }
    };

    std::stack<NodeRef> stack;
    json::const_pointer curJson;
    JsonValueReader valueReader;
};

json JsonSerializer::Serialize(const Node& node)
{
    JsonSerializerImpl serializer;

    return serializer.Serialize(node);
}

void JsonSerializer::Deserialize(Node& node, const json& json)
{
    JsonDeserializerImpl deserializer;

    deserializer.Deserialize(node, json);
}

void JsonSerializer::ValidateJson(const Node& node, const json& json)
{
    JsonValidatorImpl validator;

    validator.Validate(node, json);
}

json JsonSerializer::SerializeSchema(const Node& node)
{
    if (node.Schema())
    {
        return Serialize(*node.Schema());
    }

    return nullptr;
}

ByteVector JsonSerializer::DecipherBinaryJson(const json& binary)
{
    if (binary.is_binary())
    {
        // Received via msgpack. Parse binary content.
        json::binary_t bytes = binary.get<json::binary_t>();

        return ByteVector(bytes);
    }
    else if (binary.is_object() && (binary["bytes"].is_binary() || binary["bytes"].is_array()))
    {
        // Received as normal json. Parse bytes sub-element.
        return ByteVector(binary["bytes"].get<std::vector<kByte>>());
    }
    else if (binary.is_string())
    {
        Go::Object<kArrayList> decodedBin;
        GoThrowMsgIf(Base64Decode(binary.get<std::string>().c_str(), decodedBin.Ref(), kAlloc_App()) != kOK, kERROR_PARAMETER, "JSON parameter not formatted for binary data.");

        return ByteVector(decodedBin.Get());
    }

    GoThrowMsg(kERROR_PARAMETER, "JSON parameter not formatted for binary data.");
}

kStatus JsonSerializer::Base64Decode(const kChar* encoded, kArrayList* output, kAlloc alloc)
{
    k16u accumulator = 0;
    size_t bits = 0;
    kStatus exception;

    kTry
    {
        kTest(kArrayList_Construct(output, kTypeOf(kByte), 0, alloc));

        for (; *encoded != '\0'; encoded++)
        {
            // Padding does not form part of a valid sequence; safe to skip
            if (*encoded == '=')
            {
                break;
            }

            // Append new bits to right side (less significant) of existing bits
            k16u value = DECODING_MAP[(kSize)(*encoded)] & 0x3F;
            accumulator |= (value << 10) >> bits;
            bits += 6;

            while (bits >= 8)
            {
                kByte decodedByte = (kByte)((accumulator >> 8) & 0xFF);
                kTest(kArrayList_Add(*output, &decodedByte));
                accumulator <<= 8;
                bits -= 8;
            }
        }
    }
    kCatch(&exception)
    {
        kEndCatch(exception);
    }

    return kOK;
}

}

};

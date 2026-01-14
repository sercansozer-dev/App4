#include <GoApi/GoDataTree/GoDataTree.h>
#include <GoApi/GoDataTree/Serializer/GoJsonSerializer.h>
#include <GoApi/GoDataTree/Serializer/GoMsgPackSerializer.h>

#include <kApi/Data/kBox.h>

kBeginValueEx(GoApi, GoDataTreeArray)
    kAddFlags(GoDataTreeArray, kTYPE_FLAGS_VALUE)
kEndValueEx()

kBeginValueEx(GoApi, GoDataTreeObject)
    kAddFlags(GoDataTreeObject, kTYPE_FLAGS_VALUE)
kEndValueEx()

namespace GoApi
{

GoDataTree::GoDataTree(kAlloc allocator) :
    tree(kNULL),
    item(kNULL)
{
    GoTest(kDataTree_Construct(&tree, allocator));

    // kDataTree needs a root element. Create it and assign the "item".
    GoTest(kDataTree_Add(tree, kNULL, GO_DATA_TREE_NAME_EMPTY, &item));
}

GoDataTree::GoDataTree(const GoDataTree& dataTree) :
    tree(kNULL),
    item(kNULL)
{
    if (!kIsNull(dataTree.tree))
    {
        GoTest(kObject_Share(dataTree.tree));

        tree = dataTree.tree;
        item = dataTree.item;
    }
}

GoDataTree::~GoDataTree()
{
    try
    {
        if (!kIsNull(tree))
        {
            GoTest(kObject_Dispose(tree));
        }
    }
    catch (const std::exception& e)
    {
        GoLogException(e);
    }
}

GoDataTree GoDataTree::Clone()
{
    GoDataTree dataTree;

    // Clone the tree at the current item.
    GoTest(kObject_Clone(&dataTree.item, item, Allocator()));

    // Create a new kDataTree and point it to the root item.
    GoTest(kDataTree_Construct(&dataTree.tree, Allocator()));

    kObj(kDataTree, dataTree.tree);
    obj->root = dataTree.item;

    return dataTree;
}

GoDataTree GoDataTree::At(k32u index) const
{
    GoThrowIf(IsObject(), kERROR_PARAMETER);
    GoThrowIf(index >= kDataTree_ChildCount(tree, item), kERROR_PARAMETER);

    GoDataTree element = *this;

    element.item = kDataTree_ChildAt(tree, item, index);

    return element;
}

void GoDataTree::Resize(k64u size)
{
    // If item does not have value (is null), mark it as an array and proceed.
    if (IsNull())
    {
        // This will be called only once for item, if value is null.
        MarkAsArray();
    }

    // Only arrays can be resized. If item was null prior to calling this function, it will be marked as an array and this check will pass.
    GoThrowIf(!IsArray(), kERROR_STATE);

    if (size < Count())
    {
        for (kSize index = Count(); index >= Count() - size; index--)
        {
            Remove((k32u)index);
        }
    }
    else if (size > Count())
    {
        EnsureChildExists((k32u)size - 1);
    }
}

void GoDataTree::SetBinary(const std::vector<kByte>& bytes)
{
    kArray1 byteArray = kNULL;
    GoTest(kArray1_Construct(&byteArray, kTypeOf(kByte), bytes.size(), Allocator()));

    // TODO: Use kArray1_Attach instead of copying the data.
    memcpy(kArray1_Data(byteArray), bytes.data(), bytes.size());

    Set(byteArray);

    GoTest(kObject_Dispose(byteArray));
}

void GoDataTree::SetBinary(kArray1 bytes)
{
    GoThrowIf(!kType_Is(kTypeOf(kByte), kArray1_ItemType(bytes)), kERROR_PARAMETER);

    Set(bytes);
}

Go::Object<kArray1> GoDataTree::GetBinary() const
{
    kArray1 byteArray = Get<kArray1>();

    GoThrowIf(!kType_Is(kTypeOf(kByte), kArray1_ItemType(byteArray)), kERROR_PARAMETER);

    Go::Object<kArray1> binary;
    binary.Reset(byteArray, true);

    return binary;
}

void GoDataTree::Remove(const std::string& key)
{
    kDataTreeItem child = kDataTree_FirstChild(tree, item);
    kDataTreeItem sibling = child;

    while (!kIsNull(sibling))
    {
        if (kStrEquals(kDataTreeItem_Name(sibling), key.c_str()))
        {
            GoTest(kDataTree_Delete(tree, sibling));

            return;
        }

        sibling = kDataTree_NextSibling(tree, sibling);
    }

    GoThrow(kERROR_NOT_FOUND);
}

void GoDataTree::Remove(k32u index)
{
    kDataTreeItem childIt = kNULL;

    if ((childIt = kDataTree_ChildAt(tree, item, index)))
    {
        GoTest(kDataTree_Delete(tree, childIt));

        return;
    }

    GoThrow(kERROR_NOT_FOUND);
}

bool GoDataTree::Contains(const std::string& key) const
{
    kDataTreeItem child = kDataTree_FirstChild(tree, item);
    kDataTreeItem sibling = child;

    while (!kIsNull(sibling))
    {
        if (kStrEquals(kDataTreeItem_Name(sibling), key.c_str()))
        {
            return true;
        }

        sibling = kDataTree_NextSibling(tree, sibling);
    }

    return false;
}

void GoDataTree::Reset(const GoDataTree& other)
{
    if (!kIsNull(tree))
    {
        GoTest(kObject_Dispose(tree));
    }

    GoTest(kObject_Share(other.tree));

    tree = other.tree;
    item = other.item;
}

void GoDataTree::Clear()
{
    GoTest(kDataTree_Clear(tree));
    GoTest(kDataTree_Add(tree, kNULL, GO_DATA_TREE_NAME_EMPTY, &item));
}

kSize GoDataTree::Count() const
{
    return kDataTree_ChildCount(tree, item);
}

bool GoDataTree::IsEmpty() const
{
    return Count() == 0;
}

kType GoDataTree::Type() const
{
    return Type(item);
}

kType GoDataTree::ChildType(const std::string& key) const
{
    return Type(GetChildItem(key));
}

bool GoDataTree::IsRoot() const
{
    return kDataTree_Root(tree) == item;
}

bool GoDataTree::IsNull() const
{
    return kDataTree_ItemContent(tree, item) == nullptr;
}

bool GoDataTree::IsObject() const
{
    kObject value = kDataTree_ItemContent(tree, item);

    if (kIsNull(value) || !kType_Is(kTypeOf(kBox), kObject_Type(value)))
    {
        return false;
    }

    return kType_Is(kTypeOf(GoDataTreeObject), kBox_ItemType(value));
}

bool GoDataTree::IsArray() const
{
    kObject value = kDataTree_ItemContent(tree, item);

    if (kIsNull(value) || !kType_Is(kTypeOf(kBox), kObject_Type(value)))
    {
        return false;
    }

    return kType_Is(kTypeOf(GoDataTreeArray), kBox_ItemType(value));
}

bool GoDataTree::IsBinary() const
{
    if (kType_Is(Type(), kTypeOf(kArray1)))
    {
        return kType_Is(kTypeOf(kByte), kArray1_ItemType(Get<kArray1>()));
    }

    return false;
}

bool GoDataTree::IsNumber() const
{
    kType itemType = Type();

    return kType_Is(itemType, kTypeOf(k8u))  ||
           kType_Is(itemType, kTypeOf(k8s))  ||
           kType_Is(itemType, kTypeOf(k16u)) ||
           kType_Is(itemType, kTypeOf(k16s)) ||
           kType_Is(itemType, kTypeOf(k32u)) ||
           kType_Is(itemType, kTypeOf(k32s)) ||
           kType_Is(itemType, kTypeOf(k32f)) ||
           kType_Is(itemType, kTypeOf(k64u)) ||
           kType_Is(itemType, kTypeOf(k64s)) ||
           kType_Is(itemType, kTypeOf(k64f));
}

bool GoDataTree::IsNegativeNumber() const
{
    kType itemType = Type();

    if (kType_Is(itemType, kTypeOf(k8u)) || kType_Is(itemType, kTypeOf(k16u)) || kType_Is(itemType, kTypeOf(k32u)) || kType_Is(itemType, kTypeOf(k64u)))
    {
        return false;
    }

    if (kType_Is(itemType, kTypeOf(k8s)))
    {
        return Get8s() < (k8s)0;
    }

    if (kType_Is(itemType, kTypeOf(k16s)))
    {
        return Get16s() < (k16s)0;
    }

    if (kType_Is(itemType, kTypeOf(k32s)))
    {
        return Get32s() < (k32s)0;
    }

    if (kType_Is(itemType, kTypeOf(k32f)))
    {
        return Get32f() < (k32f)0;
    }

    if (kType_Is(itemType, kTypeOf(k64s)))
    {
        return Get64s() < (k64s)0;
    }

    if (kType_Is(itemType, kTypeOf(k64f)))
    {
        return Get64f() < (k64f)0;
    }

    return false;
}

bool GoDataTree::Is8u() const
{
    return kType_Is(Type(), kTypeOf(k8u));
}

bool GoDataTree::Is8s() const
{
    return kType_Is(Type(), kTypeOf(k8s));
}

bool GoDataTree::Is16u() const
{
    return kType_Is(Type(), kTypeOf(k16u));
}

bool GoDataTree::Is16s() const
{
    return kType_Is(Type(), kTypeOf(k16s));
}

bool GoDataTree::Is32u() const
{
    return kType_Is(Type(), kTypeOf(k32u));
}

bool GoDataTree::Is32s() const
{
    return kType_Is(Type(), kTypeOf(k32s));
}

bool GoDataTree::Is32f() const
{
    return kType_Is(Type(), kTypeOf(k32f));
}

bool GoDataTree::Is64u() const
{
    return kType_Is(Type(), kTypeOf(k64u));
}

bool GoDataTree::Is64s() const
{
    return kType_Is(Type(), kTypeOf(k64s));
}

bool GoDataTree::Is64f() const
{
    return kType_Is(Type(), kTypeOf(k64f));
}

bool GoDataTree::IsBoolean() const
{
    return kType_Is(Type(), kTypeOf(kBool));
}

bool GoDataTree::IsString() const
{
    return kType_Is(Type(), kTypeOf(kString));
}

const char* GoDataTree::Name() const
{
    return kDataTree_ItemName(tree, item);
}

GoDataTreeIterator GoDataTree::Iterator() const
{
    return GoDataTreeIterator(*this);
}

GoDataTreeIterator GoDataTree::Find(const std::string& key) const
{
    GoDataTree tree = *this;

    kDataTreeItem result = Find(key, item);

    tree.item = result;

    return GoDataTreeIterator(tree);
}

kAlloc GoDataTree::Allocator() const
{
    kObj(kDataTree, tree);

    return obj->base.alloc;
}

std::string GoDataTree::Dump(bool prettyPrint) const
{
    std::string treeString;

    GoDataTree::ToJson(*this, treeString, prettyPrint);

    return treeString;
}

GoDataTree GoDataTree::operator[](const std::string& key)
{
    GoThrowIf(IsArray(), kERROR_PARAMETER);

    // If item does not have a value (is null) or children (is empty) mark it as an object and proceed.
    if (IsNull() && IsEmpty())
    {
        MarkAsObject();
    }

    GoDataTree element = *this;
    element.item = kNULL;

    GoTest(kDataTree_EnsureExists(tree, item, key.c_str(), &element.item));

    // If path was used (/key1/key2/key3) as an argument, iterate through created items and mark all as objects.
    kDataTreeItem itemFromPath = kDataTree_Parent(tree, element.item);
    while (!kIsNull(itemFromPath) && itemFromPath != item)
    {
        MarkAsObject(itemFromPath);
        itemFromPath = kDataTree_Parent(tree, itemFromPath);
    }

    return element;
}

GoDataTree GoDataTree::operator[](const std::string& key) const
{
    GoThrowIf(IsArray(), kERROR_PARAMETER);

    GoDataTree element = *this;
    element.item = kNULL;

    if (kDataTreeItem_Child(item, key.c_str(), &element.item) != kOK)
    {
        GoThrow(kERROR_PARAMETER);
    }

    return element;
}

GoDataTree GoDataTree::operator[](k32u index)
{
    GoThrowIf(IsObject(), kERROR_PARAMETER);

    if (IsNull() && IsEmpty())
    {
        MarkAsArray();
    }

    GoDataTree element = *this;

    element.item = EnsureChildExists(index);

    return element;
}

GoDataTree GoDataTree::operator[](k32u index) const
{
    return At(index);
}

GoDataTree& GoDataTree::operator=(const GoDataTree& other)
{
    // Validate other data tree.
    GoThrowIf(kIsNull(other.tree) || kIsNull(other.item), kERROR_PARAMETER);

    // If item is a root element, just share other item and replace.
    if (IsRoot())
    {
        if (!kIsNull(tree))
        {
            // This item is a root element. For simplicity, dispose current tree and replace it with other.
            GoTest(kObject_Dispose(tree));
        }

        // Share other tree.
        GoTest(kObject_Share(other.tree));

        // Set current tree and item to shared other tree/item.
        tree = other.tree;
        item = other.item;

        return *this;
    }

    // Get current item name and parent.
    std::string name = Name();
    kDataTreeItem parent = kDataTree_Parent(tree, item);

    // Remove current item.
    GoTest(kDataTree_Delete(tree, item));

    // Clone other tree into child object. Use CloneEx instead of kObject_Clone to avoid copying values.
    kDataTreeItem child = kNULL;
    CloneEx(&child, other.item);

    // Override "other.item" name with "this.item" name.
    GoTest(kDataTreeItem_SetName(child, name.c_str()));

    // Insert cloned item into parent.
    GoTest(kDataTreeItem_Insert(parent, kNULL, child));
    item = child;

    return *this;
}

GoDataTree GoDataTree::Array()
{
    GoDataTree tree;

    tree.MarkAsArray();

    return tree;
}

GoDataTree GoDataTree::Object()
{
    GoDataTree tree;

    tree.MarkAsObject();

    return tree;
}

void GoDataTree::ToMsgPack(const GoDataTree& tree, GoMsgPackFormat& out)
{
    out.clear();

    GoMsgPackSerializer serializer;

    serializer.Serialize(tree, out);
}

void GoDataTree::ToMsgPack(const GoDataTree& tree, kObject out)
{
    GoMsgPackSerializer serializer;

    serializer.Serialize(tree, out);
}

kSize GoDataTree::CalculateMsgPackSize(const GoDataTree& tree)
{
    GoMsgPackSerializer serializer;

    return serializer.SerializedOutputSize(tree);
}

void GoDataTree::ToJson(const GoDataTree& tree, GoJsonFormat& out, bool prettyPrint)
{
    out.clear();

    GoJsonSerializer serializer(prettyPrint);

    serializer.Serialize(tree, out);
}

void GoDataTree::ToJson(const GoDataTree& tree, kObject out, bool prettyPrint)
{
    GoJsonSerializer serializer(prettyPrint);

    serializer.Serialize(tree, out);
}

kSize GoDataTree::CalculateJsonSize(const GoDataTree& tree, bool prettyPrint)
{
    GoJsonSerializer serializer(prettyPrint);

    return serializer.SerializedOutputSize(tree);
}

void GoDataTree::FromMsgPack(const GoMsgPackFormat& msgPack, GoDataTree& output)
{
    GoMsgPackSerializer serializer;

    serializer.Deserialize(msgPack, output);
}

void GoDataTree::FromMsgPack(kStream stream, kSize size, GoDataTree& output)
{
    GoMsgPackSerializer serializer;

    serializer.Deserialize(stream, size, output);
}

void GoDataTree::FromJson(const GoJsonFormat& json, GoDataTree& output)
{
    GoJsonSerializer serializer;

    serializer.Deserialize(json, output);
}

void GoDataTree::FromJson(const std::vector<kByte>& json, GoDataTree& output)
{
    GoJsonSerializer serializer;

    serializer.Deserialize(json, output);
}

void GoDataTree::FromJson(kStream stream, kSize size, GoDataTree& output)
{
    GoJsonSerializer serializer;

    serializer.Deserialize(stream, size, output);
}

kType GoDataTree::Type(kDataTreeItem item)  const
{
    if (kIsNull(item))
    {
        GoThrow(kERROR_NOT_FOUND);
    }

    kObject object = kDataTree_GetItemData(tree, item);

    if (kIsNull(object))
    {
        return kTypeOf(kDataTree);
    }

    if (kType_Is(kObject_Type(object), kTypeOf(kBox)))
    {
        return kBox_ItemType(object);
    }
    else
    {
        return kObject_Type(object);
    }
}

void GoDataTree::SetNull()
{
    kObject value = kDataTreeItem_GetData(item);

    GoTest(kDestroyRef(&value));
}

void GoDataTree::SetNull(const std::string& key)
{
    kObject value = kDataTreeItem_GetData(GetChildItem(key));

    GoTest(kDestroyRef(&value));

    MarkAsObject();
}

void GoDataTree::Set8u(const k8u& value)
{
    GoTest(kDataTreeItem_SetValue(item, kTypeOf(k8u), &value));
}

void GoDataTree::Set8u(const std::string& key, const k8u& value)
{
    GoTest(kDataTree_SetChildValue(tree, item, key.c_str(), kTypeOf(k8u), &value));

    MarkAsObject();
}

void GoDataTree::Set8s(const k8s& value)
{
    GoTest(kDataTreeItem_SetValue(item, kTypeOf(k8s), &value));
}

void GoDataTree::Set8s(const std::string& key, const k8s& value)
{
    GoTest(kDataTree_SetChildValue(tree, item, key.c_str(), kTypeOf(k8s), &value));

    MarkAsObject();
}

void GoDataTree::Set16u(const k16u& value)
{
    GoTest(kDataTreeItem_SetValue(item, kTypeOf(k16u), &value));
}

void GoDataTree::Set16u(const std::string& key, const k16u& value)
{
    GoTest(kDataTree_SetChildValue(tree, item, key.c_str(), kTypeOf(k16u), &value));

    MarkAsObject();
}

void GoDataTree::Set16s(const k16s& value)
{
    GoTest(kDataTreeItem_SetValue(item, kTypeOf(k16s), &value));
}

void GoDataTree::Set16s(const std::string& key, const k16s& value)
{
    GoTest(kDataTree_SetChildValue(tree, item, key.c_str(), kTypeOf(k16s), &value));

    MarkAsObject();
}

void GoDataTree::Set32u(const k32u& value)
{
    GoTest(kDataTree_SetItem32u(tree, item, value));
}

void GoDataTree::Set32u(const std::string& key, const k32u& value)
{
    GoTest(kDataTree_SetChild32u(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::Set32s(const k32s& value)
{
    GoTest(kDataTree_SetItem32s(tree, item, value));
}

void GoDataTree::Set32s(const std::string& key, const k32s& value)
{
    GoTest(kDataTree_SetChild32s(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::Set32f(const k32f& value)
{
    GoTest(kDataTree_SetItem32f(tree, item, value));
}

void GoDataTree::Set32f(const std::string& key, const k32f& value)
{
    GoTest(kDataTree_SetChild32f(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::Set64u(const k64u& value)
{
    GoTest(kDataTree_SetItem64u(tree, item, value));
}

void GoDataTree::Set64u(const std::string& key, const k64u& value)
{
    GoTest(kDataTree_SetChild64u(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::Set64s(const k64s& value)
{
    GoTest(kDataTree_SetItem64s(tree, item, value));
}

void GoDataTree::Set64s(const std::string& key, const k64s& value)
{
    GoTest(kDataTree_SetChild64s(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::Set64f(const k64f& value)
{
    GoTest(kDataTree_SetItem64f(tree, item, value));
}

void GoDataTree::Set64f(const std::string& key, const k64f& value)
{
    GoTest(kDataTree_SetChild64f(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::SetBoolean(const bool& value)
{
    GoTest(kDataTree_SetItemBool(tree, item, value));
}

void GoDataTree::SetBoolean(const std::string& key, const bool& value)
{
    GoTest(kDataTree_SetChildBool(tree, item, key.c_str(), value));

    MarkAsObject();
}

void GoDataTree::SetString(const std::string& value)
{
    GoTest(kDataTree_SetItemString(tree, item, value.c_str()));
}

void GoDataTree::SetString(const std::string& key, const std::string& value)
{
    GoTest(kDataTree_SetChildText(tree, item, key.c_str(), value.c_str()));

    MarkAsObject();
}

void GoDataTree::SetArray(const kArray1& value)
{
    GoThrowIf(!kType_Is(kObject_Type(value), kTypeOf(kArray1)), kERROR_PARAMETER);

    GoTest(kDataTreeItem_SetData(item, value, false));
    GoTest(kObject_Share(value));
}

void GoDataTree::SetArray(const std::string& key, const kArray1& value)
{
    GoThrowIf(!kType_Is(kObject_Type(value), kTypeOf(kArray1)), kERROR_PARAMETER);

    kDataTreeItem child;
    GoTest(kDataTree_Add(tree, item, key.c_str(), &child));
    GoTest(kDataTreeItem_SetData(child, value, false));
    GoTest(kObject_Share(value));

    MarkAsObject();
}

void GoDataTree::SetItem(const GoDataTree& value)
{
    // This would need to make a copy of kDataTreeItem object. Implement only if needed.
    GoThrow(kERROR_UNIMPLEMENTED);
}

void GoDataTree::SetChild(const std::string& key, const GoDataTree& value)
{
    // This would need to make a copy of kDataTreeItem object. Implement only if needed.
    GoThrow(kERROR_UNIMPLEMENTED);
}

k8u GoDataTree::Get8u() const
{
    k8u value;
    GoTest(kDataTreeItem_Value(item, kTypeOf(k8u), &value));

    return value;
}

k8u GoDataTree::Get8u(const std::string& key) const
{
    k8u value;
    GoTest(kDataTree_ChildValue(tree, item, key.c_str(), kTypeOf(k8u), &value));

    return value;
}

k8s GoDataTree::Get8s() const
{
    k8s value;
    GoTest(kDataTreeItem_Value(item, kTypeOf(k8s), &value));

    return value;
}

k8s GoDataTree::Get8s(const std::string& key) const
{
    k8s value;
    GoTest(kDataTree_ChildValue(tree, item, key.c_str(), kTypeOf(k8s), &value));

    return value;
}

k16u GoDataTree::Get16u() const
{
    k16u value;
    GoTest(kDataTreeItem_Value(item, kTypeOf(k16u), &value));

    return value;
}

k16u GoDataTree::Get16u(const std::string& key) const
{
    k16u value;
    GoTest(kDataTree_ChildValue(tree, item, key.c_str(), kTypeOf(k16u), &value));

    return value;
}

k16s GoDataTree::Get16s() const
{
    k16s value;
    GoTest(kDataTreeItem_Value(item, kTypeOf(k16s), &value));

    return value;
}

k16s GoDataTree::Get16s(const std::string& key) const
{
    k16s value;
    GoTest(kDataTree_ChildValue(tree, item, key.c_str(), kTypeOf(k16s), &value));

    return value;
}

k32u GoDataTree::Get32u() const
{
    k32u value;
    GoTest(kDataTree_Item32u(tree, item, &value));

    return value;
}

k32u GoDataTree::Get32u(const std::string& key) const
{
    k32u value;
    GoTest(kDataTree_Child32u(tree, item, key.c_str(), &value));

    return value;
}

k32s GoDataTree::Get32s() const
{
    k32s value;
    GoTest(kDataTree_Item32s(tree, item, &value));

    return value;
}

k32s GoDataTree::Get32s(const std::string& key) const
{
    k32s value;
    GoTest(kDataTree_Child32s(tree, item, key.c_str(), &value));

    return value;
}

k32f GoDataTree::Get32f() const
{
    k32f value;
    GoTest(kDataTree_Item32f(tree, item, &value));

    return value;
}

k32f GoDataTree::Get32f(const std::string& key) const
{
    k32f value;
    GoTest(kDataTree_Child32f(tree, item, key.c_str(), &value));

    return value;
}

k64u GoDataTree::Get64u() const
{
    k64u value;
    GoTest(kDataTree_Item64u(tree, item, &value));

    return value;
}

k64u GoDataTree::Get64u(const std::string& key) const
{
    k64u value;
    GoTest(kDataTree_Child64u(tree, item, key.c_str(), &value));

    return value;
}

k64s GoDataTree::Get64s() const
{
    k64s value;
    GoTest(kDataTree_Item64s(tree, item, &value));

    return value;
}

k64s GoDataTree::Get64s(const std::string& key) const
{
    k64s value;
    GoTest(kDataTree_Child64s(tree, item, key.c_str(), &value));

    return value;
}

k64f GoDataTree::Get64f() const
{
    k64f value;
    GoTest(kDataTree_Item64f(tree, item, &value));

    return value;
}

k64f GoDataTree::Get64f(const std::string& key) const
{
    k64f value;
    GoTest(kDataTree_Child64f(tree, item, key.c_str(), &value));

    return value;
}

bool GoDataTree::GetBoolean() const
{
    kBool value;
    GoTest(kDataTree_ItemBool(tree, item, &value));

    return (bool)value;
}

bool GoDataTree::GetBoolean(const std::string& key) const
{
    kBool value;
    GoTest(kDataTree_ChildBool(tree, item, key.c_str(), &value));

    return (bool)value;
}

std::string GoDataTree::GetString() const
{
    kObject object = kDataTree_ItemContent(tree, item);

    GoThrowIf(object == kNULL || !kType_Is(kObject_Type(object), kTypeOf(kString)), kERROR_PARAMETER);

    std::string value(kString_Length(object) + 1, '\0');

    GoTest(kDataTree_ItemText(tree, item, value.data(), (k32u)value.size()));

    if (value[value.size() - 1] == '\0')
    {
        value.pop_back();
    }

    return value;
}

std::string GoDataTree::GetString(const std::string& key) const
{
    kObject object = kDataTree_ItemContent(tree, GetChildItem(key));

    GoThrowIf(!kType_Is(kObject_Type(object), kTypeOf(kString)), kERROR_PARAMETER);

    std::string value(kString_Length(object) + 1, '\0');

    GoTest(kDataTree_ChildText(tree, item, key.c_str(), value.data(), (k32u)value.size()));

    if (value[value.size() - 1] == '\0')
    {
        value.pop_back();
    }

    return value;
}

kArray1 GoDataTree::GetArray() const
{
    kObject object = kDataTreeItem_GetData(item);

    GoThrowIf(!kType_Is(kObject_Type(object), kTypeOf(kArray1)), kERROR_PARAMETER);

    return object;
}

kArray1 GoDataTree::GetArray(const std::string& key) const
{
    kObject object = kDataTreeItem_GetData(GetChildItem(key));

    GoThrowIf(!kType_Is(kObject_Type(object), kTypeOf(kArray1)), kERROR_PARAMETER);

    return object;
}

GoDataTree GoDataTree::GetChild(const std::string& key) const
{
    GoDataTree child = *this;

    child.item = GetChildItem(key);

    return child;
}

void GoDataTree::MarkAsArray()
{
    GoDataTreeArray arrayValue;

    // Set item value to GoDataTreeArray. This indicates that current item is an array.
    // It's needed to distinct objects, arrays and nulls for serialization.
    GoTest(kDataTreeItem_SetValue(item, kTypeOf(GoDataTreeArray), &arrayValue));
}

void GoDataTree::MarkAsObject()
{
    MarkAsObject(item);
}

void GoDataTree::MarkAsObject(kDataTreeItem item)
{
    GoDataTreeObject objectValue;

    // Set item value to GoDataTreeObject. This indicates that current item is an object.
    // It's needed to distinct objects, arrays and nulls for serialization.
    GoTest(kDataTreeItem_SetValue(item, kTypeOf(GoDataTreeObject), &objectValue));
}

kObject GoDataTree::GetChildItem(const std::string& key) const
{
    kDataTreeItem child;

    GoTest(kDataTree_ChildItem(tree, item, key.c_str(), &child));

    return child;
}

kDataTreeItem GoDataTree::EnsureChildExists(k32u index) const
{
    k32u childCount = kDataTree_ChildCount(tree, item);

    if (childCount <= index)
    {
        for (k32u i = 0; i <= index - childCount; i++)
        {
            GoTest(kDataTree_Add(tree, item, GO_DATA_TREE_NAME_EMPTY, kNULL));
        }
    }

    return kDataTree_ChildAt(tree, item, index);
}

kDataTreeItem GoDataTree::Find(const std::string& key, kDataTreeItem item) const
{
    if (kDataTreeItem_Name(item) == key)
    {
        return item;
    }

    kDataTreeItem sibling = kDataTree_FirstChild(tree, item);

    while (sibling)
    {
        kDataTreeItem child = Find(key, sibling);

        if (child)
        {
            return child;
        }

        sibling = kDataTreeItem_NextSibling(sibling);
    }

    return kNULL;
}

void GoDataTree::CloneEx(kDataTreeItem* object, kDataTreeItem source)
{
    kAlloc alloc = Allocator();

    GoTest(kDataTreeItem_Construct(object, kDataTreeItem_Name(source), alloc));

    kObj(kDataTreeItem, *object);
    kObjN(kDataTreeItem, srcObj, source);

    kDataTreeItem childIt = srcObj->firstChild;
    kDataTreeItem childCopy = kNULL;

    try
    {
        if (!kIsNull(srcObj->value))
        {
            GoTest(kObject_Share(srcObj->value));
            obj->value = srcObj->value;
        }

        while (!kIsNull(childIt))
        {
            CloneEx(&childCopy, childIt);

            GoTest(kDataTreeItem_Insert(*object, kNULL, childCopy));
            childCopy = kNULL;

            childIt = kDataTreeItem_NextSibling(childIt);
        }
    }
    catch(const std::exception&)
    {
        kObject_Dispose(childCopy);

        kDisposeRef(object);

        throw;
    }
}

void* GoApi::GoDataTree::GetItemNameValuePair(const kChar** name, kType* type)
{
    kObj(kDataTreeItem, item);

    *name = kIsNull(obj->name.buffer) ? obj->name.text : obj->name.buffer;

    if (kIsNull(obj->value))
    {
        type = kNULL;

        return nullptr;
    }

    kType objectType = kObject_Type(obj->value);

    if (kType_Is(objectType, kTypeOf(kBox)))
    {
        *type = kBox_ItemType(obj->value);

        return kBox_Data(obj->value);
    }

    *type = objectType;

    return obj->value;
}

GoDataTreeIterator::GoDataTreeIterator(const GoDataTree& tree) : dataTree(tree)
{ }

GoDataTreeIterator GoDataTreeIterator::NextSibling()
{
    GoDataTreeIterator sibling = *this;

    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        sibling.dataTree.item = kDataTree_NextSibling(dataTree.tree, dataTree.item);
    }
    else
    {
        sibling.dataTree.item = kNULL;
    }

    return sibling;
}

GoDataTreeIterator GoDataTreeIterator::PreviousSibling()
{
    GoDataTreeIterator sibling = *this;

    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        sibling.dataTree.item = kDataTree_PreviousSibling(dataTree.tree, dataTree.item);
    }
    else
    {
        sibling.dataTree.item = kNULL;
    }

    return sibling;
}

bool GoDataTreeIterator::HasSibling()
{
    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        return !kIsNull(kDataTree_NextSibling(dataTree.tree, dataTree.item));
    }

    return false;
}

GoDataTreeIterator GoDataTreeIterator::FirstChild()
{
    GoDataTreeIterator child = *this;

    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        child.dataTree.item = kDataTree_FirstChild(dataTree.tree, dataTree.item);
    }
    else
    {
        child.dataTree.item = kNULL;
    }

    return child;
}

GoDataTreeIterator GoDataTreeIterator::LastChild()
{
    GoDataTreeIterator child = *this;

    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        child.dataTree.item = kDataTree_LastChild(dataTree.tree, dataTree.item);
    }
    else
    {
        child.dataTree.item = kNULL;
    }

    return *this;
}

bool GoDataTreeIterator::HasChild()
{
    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        return !kIsNull(kDataTree_FirstChild(dataTree.tree, dataTree.item));
    }

    return false;
}

GoDataTreeIterator GoDataTreeIterator::Parent()
{
    GoDataTreeIterator parent = *this;

    if (!kIsNull(dataTree.tree) && !kIsNull(dataTree.item))
    {
        parent.dataTree.item = kDataTree_Parent(dataTree.tree, dataTree.item);
    }
    else
    {
        parent.dataTree.item = kNULL;
    }

    return parent;
}

GoDataTree GoDataTreeIterator::operator*()
{
    return dataTree;
}

GoDataTreeIterator& GoDataTreeIterator::operator=(const GoDataTreeIterator& other)
{
    if (!kIsNull(dataTree.tree))
    {
        GoTest(kObject_Dispose(dataTree.tree));
        dataTree.tree = kNULL;
        dataTree.item = kNULL;
    }

    if (!kIsNull(other.dataTree.tree))
    {
        GoTest(kObject_Share(other.dataTree.tree));

        dataTree.tree = other.dataTree.tree;
        dataTree.item = other.dataTree.item;
    }

    return *this;
}

GoDataTreeIterator::operator bool() const
{
    return !kIsNull(dataTree.item);
}

}
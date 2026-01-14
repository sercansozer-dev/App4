/**
* @file     XmlAccessors.h
* @brief    Declares templated kXml accessors used for serialization.
*/

#ifndef GOAPI_XML_ACCESSORS_H
#define GOAPI_XML_ACCESSORS_H

#include <GoApi/Exception.h>
#include <GoApi/Object.h>
#include <kApi/Data/kXml.h>
#include <kApi/Data/kString.h>

namespace Go
{
namespace Properties
{
namespace SerializersInternal
{

// Default implementation
template <typename T, typename = void>
struct XmlAccessor
{
    static void Set(kXml xml, kXmlItem item, const T& value)
    {
        throw Go::Exception(kERROR_UNIMPLEMENTED);
    }

    static void Get(kXml xml, kXmlItem item, T& value)
    {
        throw Go::Exception(kERROR_UNIMPLEMENTED);
    }
};

#define DECLARE_XML_PRIMITIVE_ACCESSOR(TYPE)                                \
template <typename T>                                                       \
struct XmlAccessor<T, std::enable_if_t<std::is_same<T, k##TYPE>::value>>    \
{                                                                           \
    static void Set(kXml xml, kXmlItem item, const T& value)                \
    {                                                                       \
        GoTest(kXml_SetItem##TYPE(xml, item, value));                     \
    }                                                                       \
    static void Get(kXml xml, kXmlItem item, T& value)                      \
    {                                                                       \
        GoTest(kXml_Item##TYPE(xml, item, &value));                       \
    }                                                                       \
};

DECLARE_XML_PRIMITIVE_ACCESSOR(16s);
DECLARE_XML_PRIMITIVE_ACCESSOR(16u);
DECLARE_XML_PRIMITIVE_ACCESSOR(32s);
DECLARE_XML_PRIMITIVE_ACCESSOR(32u);
DECLARE_XML_PRIMITIVE_ACCESSOR(64s);
DECLARE_XML_PRIMITIVE_ACCESSOR(64u);
DECLARE_XML_PRIMITIVE_ACCESSOR(32f);
DECLARE_XML_PRIMITIVE_ACCESSOR(64f);


// kXml doesn't explicitly support 8-bit ints
template <typename T>
struct XmlAccessor<T, std::enable_if_t<std::is_same<T, k8s>::value>>
{
    static void Set(kXml xml, kXmlItem item, k8s value)
    {
        GoTest(kXml_SetItem16s(xml, item, static_cast<k16s>(value)));
    }

    static void Get(kXml xml, kXmlItem item, k8s& value)
    {
        k16s kval;

        GoTest(kXml_Item16s(xml, item, &kval));
        value = static_cast<k8s>(kval);
    }
};

template <typename T>
struct XmlAccessor<T, std::enable_if_t<std::is_same<T, k8u>::value>>
{
    static void Set(kXml xml, kXmlItem item, k8u value)
    {
        GoTest(kXml_SetItem16u(xml, item, static_cast<k16u>(value)));
    }

    static void Get(kXml xml, kXmlItem item, k8u& value)
    {
        k16u kval;

        GoTest(kXml_Item16u(xml, item, &kval));
        value = static_cast<k8u>(kval);
    }
};

template <typename T>
struct XmlAccessor<T, std::enable_if_t<std::is_same<T, bool>::value>>
{
    static void Set(kXml xml, kXmlItem item, bool value)
    {
        GoTest(kXml_SetItemBool(xml, item, value ? kTRUE : kFALSE));
    }

    static void Get(kXml xml, kXmlItem item, bool& value)
    {
        kBool kval;

        GoTest(kXml_ItemBool(xml, item, &kval));
        value = kval == kTRUE;
    }
};

template <typename T>
struct XmlAccessor<T, std::enable_if_t<std::is_same<T, std::string>::value>>
{
    static void Set(kXml xml, kXmlItem item, const std::string& value)
    {
        GoTest(kXml_SetItemText(xml, item, value.c_str()));
    }

    static void Get(kXml xml, kXmlItem item, std::string& value)
    {
        Go::Object<kString> kstr;

        GoTest(kString_Construct(kstr.Ref(), kNULL, kAlloc_App()));
        GoTest(kXml_ItemString(xml, item, kstr));
        value = kString_Chars(kstr);
    }
};

}}} //Namespaces

#endif

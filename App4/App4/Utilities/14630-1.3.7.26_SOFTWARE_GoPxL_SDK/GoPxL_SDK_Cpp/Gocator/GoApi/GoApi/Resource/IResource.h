/**@file    IResource.h
 * GoApi Resource Interface class
 */

#ifndef GOAPI_IRESOURCE_H
#define GOAPI_IRESOURCE_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Event.h>
#include <GoApi/Resource/EventType.h>
#include <GoApi/Resource/ReqRespInfo.h>
#include <GoApi/GoDataTree/GoDataTree.h>
#include <string>

#include <nlohmann/json.hpp>

using GoDataTree = GoApi::GoDataTree;

//! namespace used for all GoApi related classes.
namespace GoApi
{

using StreamId = k32u;

/*! @class IDocumentResource IResource.h
 *  @brief Document Resource Interface class containing virtual methods to retreive and update model objects
 *  @ingroup GoApi-Resources
 */
class GoApiClass IDocumentResource
{
public:
    /**
     * Retrieves the model object. This method should not be const because
     * it may update the model.
     *
     * @param reqInfo        Contains information about the request.
     * @param respInfo       Used by the implementation to describe the response.
     * @param reply          GoDataTree reply output.
     */
    virtual void Document(const RequestInfoEx& reqInfo, ResponseInfoEx& respInfo, GoDataTree& reply) = 0;

    /**
     * Call to update the underlying model with new data
     *
     * @param reqInfo        A RequestInfo struct.
     * @param data           A JSON object containing key value pairs of parameters to update.
     */
    virtual void UpdateDocument(const RequestInfo& reqInfo, const nlohmann::json& data) = 0;
};

/*! @class IStreamReceiver IResource.h
 *  @brief a Stream Receiver Interface class containing virtual methods to handle receiving streamed resources.
 *  @ingroup GoApi-Resources
 */
class GoApiClass IStreamReceiver
{
public:
    /**
     * Called by the streaming resource (other than scan output message) whenever new data is available.
     * The callback can originate from any thread
     *
     * @param data       The current chunk of data. The content depends on the use case.
     */
    virtual void OnData(const GoDataTree& data) = 0;

    /**
     * Called by the streaming resource whenever new data is available from scan output.
     * The callback can originate from any thread
     *
     * @param msgPackData       The current chunk of data. The message from scan output message.
     */
    virtual void OnData(std::shared_ptr<std::vector<kByte>> msgPackData) = 0;

    /**
     * Called when the stream completes and there is no more data to be sent.
     * Not all streams call this function.
     */
    virtual void OnComplete() = 0;

    /**
     * Called when a stream aborts for any reason
     *
     * @param status     The abort status.
     */
    virtual void OnAbort(kStatus status) = 0;

    /**
     * Called before a stream starts sending data,
     * Used to notify callers if data should be sent or not.
     *
     * @param isLastMsg  Boolean indicating if the data being streamed is the last one.
     * @param repeat     Boolean indicating if received data is a repeat or not.
     * @return           Boolean indicating if data should be sent.
     */
    virtual bool OnDataBegin(bool isLastMsg, bool repeat) = 0;

    /**
     * Called after a corresponding @ref OnDataBegin()
     * to signal the end of a data sending cycle.
     */
    virtual void OnDataEnd() = 0;
};

/*! @class IStreamableResource IResource.h
 *  @brief Streamable Resource class containing virtual methods to allow for writing and reading of streamable data.
 *  @ingroup GoApi-Resources
 */
class GoApiClass IStreamableResource
{
public:
    using StreamStartEvent = Go::Event<const RequestInfoEx&>;

    /**
     * Start streaming data into the receiver.
     * Returns the stream Id to be used to stop the stream.
     * All streams must be stopped to prevent leaking system resources.
     *
     * @param reqInfo    a RequestInfo object.
     * @param receiver   an IStreamReciver object in charge of receiving the streamed resource.
     * @return           A stream ID of the stream started.
     */
    virtual StreamId StartStream(const RequestInfoEx& reqInfo, IStreamReceiver& receiver) = 0;

    /**
     * Stops the stream
     *
     * @param id         The stream id of the stream to stop.
     */
    virtual void StopStream(StreamId id) = 0;

    /**
     * Stream start event that can be used to listen on when the stream has started.
     *
     * @return          A StreamStartEvent to listen on.
     */
    virtual StreamStartEvent* StartEvent() = 0;
};

/**
 * @struct CreateResult
 *
 * @brief Represents return information when a Create call is invoked.
 * @ingroup GoApi-Resource
 */
struct CreateResult
{
    std::string id;
    std::string path;
};

/*! @class IParentResource IResource.h
 *  @brief Parent Resource class that represents a resource that contains a collection of resources.
 *         Virtual methods include creation and deletion of child resources.
 *  @ingroup GoApi-Resources
 */
class GoApiClass IParentResource
{
public:
    /**
     * Creates a child resource using the given args and returns the results.
     *
     * @param reqInfo    A RequestInfo object.
     * @param params     A json object containing parameters needed to create the child resource.
     *                   params may be an empty json object if no parameters are needed.
     * @return           A CreateResult object.
     */
    virtual CreateResult Create(const RequestInfo& reqInfo, const nlohmann::json& params) = 0;

    /**
     * Deletes a child resource.
     *
     * @param reqInfo    A RequestInfo containing keys to the child resource.
     */
    virtual void Delete(const RequestInfoEx& reqInfo) = 0;
};

/*! @class ICallableResource IResource.h
 *  @brief A Callable Resource that contains an invoke method to call more specific commands on a resource.
 *  @ingroup GoApi-Resources
 */
class GoApiClass ICallableResource
{
public:
    /**
     * All Invoke methods must return a GoDataTree object with at least a k32s "status" field.
     *
     * @param reqInfo    A RequestInfo object.
     * @param args       A GoDataTree object containing arguments to be used.
     * @return           A GoDataTree object containing the response.
     */
    virtual GoDataTree Invoke(const RequestInfoEx& reqInfo, const GoDataTree& args) = 0;
};

/*! @class IResource IResource.h
 *  @brief A class containing virtual methods to return the corresponding resource type.
 *  @ingroup GoApi-Resources
 */
class GoApiClass IResource
{
public:
    //Resource, Event Type, and Path to notification.
    using ResourceNotificationEvent = Go::Event<IResource&, EventType, const std::string&>;
    virtual ResourceNotificationEvent* NotificationEvent() = 0;

    virtual IParentResource* ParentProvider() = 0;

    virtual IDocumentResource* DocumentProvider() = 0;

    virtual ICallableResource* CallableProvider() = 0;

    virtual IStreamableResource* StreamableProvider() = 0;
};

} // Namespace

#endif  // GOAPI_IRESOURCE_H

using System;
using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Utilities;
using System.Collections;
using System.Xml.Serialization;
using System.Reflection;

namespace System.IO.BACnet
{
    #region Enums
    public enum BACnetObjectSorting
    {
        ByIdentifier,
    }
    #endregion
    #region Types
    /// <summary>
    /// Represents an endpoint that provides specific services (e.g. <see cref="BACnetEndpoint.ReadObjectListAsync"/>).
    /// </summary>
    /// <remarks>
    /// All provided serices are basically wrappers of the low level requests (e.g. <see cref="BacnetClient.ReadPropertyRequestAsync"/>).
    /// </remarks>
    [DebuggerDisplay("{this.ToString(true)} {Client}")]
    public class BACnetEndpoint : IDisposable
    {
        #region Constants
        internal static readonly BacnetPropertyReference[] AllProperties = new BacnetPropertyReference[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, ASN1.BACNET_ARRAY_ALL) };
        #endregion


        #region Types
        private class RequestModeSwitch
        {
            public RequestModeSwitch(int maxRetries)
            {
                this.MaxRetries = maxRetries;
                this.Fails = 0;
            }


            #region Properties.Management
            public int MaxRetries { get; }
            public bool Enabled => (Fails < MaxRetries);
            #endregion
            #region Properties
            public int Fails { private set; get; }
            #endregion


            #region Management
            public void Update(bool succeeded)
            {
                if (succeeded)
                    this.Fails = 0;
                else
                    this.Fails++;
            }
            #endregion
        }
        #endregion


        public BACnetEndpoint(BacnetClient client, BacnetAddress address, uint deviceId = 0xFFFFFFFF)
        {
            this.Client = client;
            this.Address = address;
            this.DeviceId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId);
        }


        #region Properties.Management
        public BacnetClient Client { get; }

        public CancellationToken CommunicationToken => Client.CommunicationToken.Token;
        #endregion
        #region Properties.Services
        /// <summary>
        /// Indicates support of 'Read Property Multiple' requests.
        /// </summary>
        public bool SupportsRpm { private set; get; } = true;
        #endregion
        #region Properties
        public BacnetAddress Address { get; }
        public BacnetObjectId DeviceId { get; private set; }
        public uint InstanceId => DeviceId.Instance;
        #endregion


        public override bool Equals(object obj)
        {
            if (obj is BACnetDevice other)
            {
                if (!this.Client.Equals(other.Client)) return false;
                if (!this.Address.Equals(other.Address)) return false;
                if (!this.DeviceId.Equals(other.DeviceId)) return false;
                return true;
            }
            return false;
        }
        public override int GetHashCode() => (int)DeviceId.Instance + (Client.GetHashCode() * 20);
        public override string ToString() => Client.ToString();
        public string ToString(bool sourceOnly) => $"Device {DeviceId.Instance} - {Address.ToString(sourceOnly)}";


        #region Initialization
        public void Dispose()
        {
            Client.Dispose();
        }
        #endregion
        #region Management
        private static IReadOnlyDictionary<BacnetObjectTypes, BacnetPropertyIds[]> GetPropertyDescription()
        {
            if (objDescr == null)
            {
                var xmlSerializer = new XmlSerializer(typeof(List<BacnetObjectDescription>));
                var asm = Assembly.GetExecutingAssembly();
                var streams = new (StreamReader Reader, bool Required)[] {
                    // Embedded standard resource:
                    (new StreamReader(asm.GetManifestResourceStream("Yabe.ReadSinglePropDescrDefault.xml")), true),
                    // External file (optional):
                    (new StreamReader("ReadSinglePropDescr.xml"), false)
                };

                // Read XML description files:
                objDescr = streams
                    .Select(stream => GetPropertyDescription(stream.Reader, stream.Required))
                    .WhereNotNull()
                    .SelectMany(res => res)
                    .GroupBy(res => res.typeId)
                    .ToDictionary(
                        res => res.Key,
                        res => res
                            .SelectMany(x => x.propsId)
                            .Distinct()
                            .ToArray()
                    );
            }
            return (objDescr);
        }
        private static List<BacnetObjectDescription> GetPropertyDescription(StreamReader reader, bool required = false)
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(List<BacnetObjectDescription>));
                return ((List<BacnetObjectDescription>)xmlSerializer.Deserialize(reader));
            }
            catch (Exception)
            {
                if (required)
                    throw;
            }
            return (null);
        }
        private static IReadOnlyDictionary<BacnetObjectTypes, BacnetPropertyIds[]> objDescr = null;
        #endregion
        #region Services
        public void SynchronizeTime(DateTime dateTime, bool utc) => Client.SynchronizeTime(Address, dateTime, utc);
        /// <summary>
        /// Updates the internal device ID.
        /// </summary>
        /// <param name="forceUpdate">Forces the ID to be read if <c>true</c>. Otherwise the ID is read only once from the <see cref="BACnetEndpoint.Client">client</see>.</param>
        public async Task UpdateDeviceIdAsync(bool forceUpdate = false)
        {
            if ((forceUpdate) || (DeviceId.Instance == 0xFFFFFFFF))
            {
                var res = await Client.ReadPropertyRequestAsync(Address, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, ASN1.BACNET_MAX_INSTANCE), BacnetPropertyIds.PROP_OBJECT_IDENTIFIER).ConfigureAwait(false);
                DeviceId = res.Unwrap<BacnetObjectId>();
            }
            else
                ; // Skip.
        }
        public Task<bool> SubscribeCOVRequestAsync(BacnetObjectId objectId, uint subscribeId, bool cancel, bool issueConfirmedNotifications, uint lifetime) => Client.SubscribeCOVRequestAsync(Address, objectId, subscribeId, cancel, issueConfirmedNotifications, lifetime);
        /// <summary>
        /// Returns a read property as <see cref="BacnetPropertyValue"/>.
        /// </summary>
        public async Task<BacnetPropertyValue> ReadPropertyValueAsync(BacnetObjectId objectId, BacnetPropertyIds property, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            return (new BacnetPropertyValue()
            {
                property = new BacnetPropertyReference((uint)property, arrayIndex),
                value = await ReadPropertyAsync(objectId, property, arrayIndex).ConfigureAwait(false)
            });
        }
        /// <inheritdoc cref="ReadPropertyAsync(BacnetObjectId, BacnetPropertyIds, uint)"/>
        public async Task<T> ReadPropertyAsync<T>(BacnetObjectId objectId, BacnetPropertyIds property, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            var res = await ReadPropertyAsync(objectId, property, arrayIndex).ConfigureAwait(false);
            return (res.Unwrap<T>());
        }
        /// <summary>
        /// Reads a property value by use of the most efficient request(s).
        /// </summary>
        public async Task<IList<BacnetValue>> ReadPropertyAsync(BacnetObjectId objectId, BacnetPropertyIds property, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            try
            {
                // Try 1) Read per 'ReadProperty' request:
                try
                {
                    return (await Client.ReadPropertyRequestAsync(Address, objectId, property, default, arrayIndex).ConfigureAwait(false));
                }
                catch (Exception)
                {
                    // Unable to read per single request.
                    // > Turn over to split request.
                }

                // Try 2) Split request and read property one by one:
                if (property.IsListType())
                {
                    var res = await Client.ReadPropertyRequestAsync(Address, objectId, property, default, 0).ConfigureAwait(false);
                    var objCount = res.Unwrap<ulong>();

                    var result = new List<BacnetValue>();
                    for (ulong i = 1; i <= objCount; i++)
                    {
                        res = await Client.ReadPropertyRequestAsync(Address, objectId, property, default, (uint)i).ConfigureAwait(false);
                        result.Add(res.Single());
                    }
                    return (result);
                }
                else
                    // State failed request:
                    throw new BACnetCommunicationException($"Failed to read {property}!");
            }
            catch (TaskCanceledException)
            {
                Trace.WriteLine($"Canceled reading {property}!");
                return (null);
            }
        }
        /// <summary>
        /// Reads all of an objects properties by use of the most efficient request.
        /// </summary>
        public Task<IList<BacnetReadAccessResult>> ReadPropertiesAsync(BacnetObjectId objectId) => ReadPropertiesAsync(objectId, AllProperties);
        /// <summary>
        /// Reads several properties by use of the most efficient request.
        /// </summary>
        public Task<IList<BacnetReadAccessResult>> ReadPropertiesAsync(BacnetObjectId objectId, params BacnetPropertyIds[] properties) => ReadPropertiesAsync(objectId, properties
            .Select(prop => (BacnetPropertyReference)prop)
            .ToList());
        /// <inheritdoc cref="ReadPropertiesAsync(BacnetObjectId, BacnetPropertyIds[])"/>
        public async Task<IList<BacnetReadAccessResult>> ReadPropertiesAsync(BacnetObjectId objectId, IList<BacnetPropertyReference> properties)
        {
            try
            {
                // Try 1) Read per 'ReadPropertyMultiple' request:
                if (rpmMode.Enabled)
                {
                    try
                    {
                        return (await Client.ReadPropertyMultipleRequestAsync(Address, objectId, properties).ConfigureAwait(false));
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Unable to read per single request:
                        if (ex is TimeoutException)
                            ; // Suffer timeouts and keep current mode.
                        else
                            // Turn over to read properties one by one:
                            rpmMode.Update(false);

                        Trace.TraceWarning("Could not perform 'ReadPropertyMultiple'. Trying 'ReadProperty' instead...");
                    }
                }

                // Try 2) Read per properties one by one:
                if (object.ReferenceEquals(properties, AllProperties))
                {
                    // We don't want to spend too much time on non existing properties:
                    int _retries = Client.Retries;
                    Client.Retries = 1;

                    try
                    {
                        var values = new List<BacnetPropertyValue>();
                        var readPropList = new HashSet<BacnetPropertyIds>();

                        // The 'PROP_LIST' property was added as an addendum to 135-2010.
                        // Test to see if it is supported, otherwise fall back to the the predefined delault property list.
                        var res = await ReadPropertyAsync(objectId, BacnetPropertyIds.PROP_PROPERTY_LIST).ConfigureAwait(false);
                        var propListSupported = res.TryUnwrap<List<uint>>(out var propList);
                        if (propListSupported)
                            readPropList.AddRange(propList.ChangeType<uint, BacnetPropertyIds>());
                        else
                        {
                            // Get property description from internal or external XML file:
                            var propDesc = GetPropertyDescription();
                            if (propDesc.TryGetValue(objectId.type, out var propIds))
                                readPropList.AddRange(propIds);
                        }

                        // Three mandatory common properties to all objects (PROP_OBJECT_IDENTIFIER, PROP_OBJECT_TYPE, PROP_OBJECT_NAME).
                        // Add known values:
                        values.Add(new BacnetPropertyValue(BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, new BacnetValue(objectId)));
                        values.Add(new BacnetPropertyValue(BacnetPropertyIds.PROP_OBJECT_TYPE, new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)objectId.type)));
                        // Add unknown values:
                        readPropList.Add(BacnetPropertyIds.PROP_OBJECT_IDENTIFIER);

                        // Read property values:
                        foreach (var prop in readPropList)
                            values.Add(await ReadPropertyValueAsync(objectId, prop));

                        return (new BacnetReadAccessResult[] { new BacnetReadAccessResult(objectId, values) });
                    }
                    finally
                    {
                        Client.Retries = _retries;
                    }
                }
                else
                {
                    var requests = properties.Select(prop => (
                        Property: prop,
                        Task: this.ReadPropertyAsync(objectId, (BacnetPropertyIds)prop.propertyIdentifier, prop.propertyArrayIndex)
                    )).ToArray();
                    var results = await Task
                        .WhenAll(requests.Select(req => req.Task))
                        .ConfigureAwait(false);

                    Debug.Assert(requests.Length == results.Length);
                    var resValues = new List<BacnetPropertyValue>();
                    for (int i = 0; i < results.Length; i++)
                    {
                        resValues.Add(new BacnetPropertyValue()
                        {
                            property = requests[i].Property,
                            value = results[i],
                            priority = default // (BETA) ... TODO: Wich priority to chose here!?
                        });
                    }
                    return (new List<BacnetReadAccessResult>() {
                        new BacnetReadAccessResult(objectId, resValues)
                    });
                }
            }
            catch (TaskCanceledException)
            {
                Trace.WriteLine($"Canceled reading {properties.Count} properties!");
                return (null);
            }
        }
        public Task<bool> WritePropertyAsync(BacnetObjectId objectId, BacnetPropertyIds property, IEnumerable<BacnetValue> values) => Client.WritePropertyRequestAsync(Address, objectId, property, values);
        #endregion


        private RequestModeSwitch rpmMode = new RequestModeSwitch(1);
    }
    /// <summary>
    /// Represents a BACnet device wich provides additional management (e.g. <see cref="GetObjectListAsync"/>) on top of the <see cref="BACnetEndpoint"/> services.
    /// </summary>
    public class BACnetDevice : BACnetEndpoint
    {
        #region Types
        private sealed class ObjectFactory
        {
            public static BACnetObject Create(BACnetDevice device, BacnetObjectId objectId)
            {
                switch (objectId.Type)
                {
                    case BacnetObjectTypes.OBJECT_STRUCTURED_VIEW: return (new BACnetView(device, objectId));
                }
                return (new BACnetObject(device, objectId));
            }
        }
        #endregion


        public BACnetDevice(BacnetClient client, BacnetAddress address, uint deviceId = 0xFFFFFFFF) : base(client, address, deviceId)
        {
        }


        #region Properties.Services
        /// <summary>
        /// Time of last object list update.
        /// </summary>
        public DateTime ObjectListUpdated { private set; get; }
        private Dictionary<BacnetObjectTypes, BACnetObject[]> objectList = new Dictionary<BacnetObjectTypes, BACnetObject[]>();
        /// <summary>
        /// Time of last structured object list update.
        /// </summary>
        public DateTime StructuredObjectListUpdated { private set; get; }
        private List<BACnetObject> structuredObjectList = new List<BACnetObject>();
        #endregion
        #region Properties
        public BACnetObject this[BacnetObjectId id] => (objectList.TryGetObject(id, out var obj) ? obj : null);
        #endregion


        #region Services
        public async Task<BACnetObject> GetDeviceObjectAsync(bool forceUpdate = false)
        {
            if ((forceUpdate) || (this.objectList.Count == 0))
                await GetObjectListAsync().ConfigureAwait(false);

            if (!objectList.TryGetValue(BacnetObjectTypes.OBJECT_DEVICE, out var devices))
                return (null);
            else
                return (devices.SingleOrDefault());
        }
        public async Task<string> GetDeviceNameAsync(bool forceUpdate = false)
        {
            var devObj = await GetDeviceObjectAsync().ConfigureAwait(false);
            if (devObj is null)
                return (null);
            else
                return (await devObj.GetPropertyAsync<string>(BacnetPropertyIds.PROP_OBJECT_NAME, default, forceUpdate).ConfigureAwait(false));
        }
        /// <summary>
        /// Returns the list of containing objects.
        /// </summary>
        /// <param name="forceUpdate">Forces the list to be read if <c>true</c>. Otherwise the list is read only once from the <see cref="BACnetEndpoint.Client">client</see>.</param>
        public async Task<IReadOnlyDictionary<BacnetObjectTypes, BACnetObject[]>> GetObjectListAsync(bool forceUpdate = false)
        {
            List<BACnetObject> objList = null;
            if ((forceUpdate) || (this.objectList.Count == 0))
            {
                var objIds = await ReadPropertyAsync<List<BacnetObjectId>>(DeviceId, BacnetPropertyIds.PROP_OBJECT_LIST).ConfigureAwait(false);
                objList = objIds
                    .Select(id => ObjectFactory.Create(this, id))
                    .ToList();

                // Initialize objects:
                await Task.WhenAll(objList.Select(obj => obj.InitAsync())).ConfigureAwait(false);

                Debug.Assert(objList.Count > 0);
                ObjectListUpdated = DateTime.Now;
            }

            lock (this)
            {
                if (objList != null)
                {
                    this.objectList = objList
                        .GroupBy(obj => obj.ObjectId.Type)
                        .ToDictionary(grp => grp.Key, grp => grp.ToArray());
                    this.structuredObjectList.Clear();
                }
                return (this.objectList.ToDictionary(item => item.Key, item => item.Value));
            }
        }
        /// <summary>
        /// Returns the list of structured objects wich consist of <see cref="BACnetObject">child</see>/<see cref="BACnetView">parent</see> relations.
        /// </summary>
        /// <param name="forceUpdate">Forces the list to be read if <c>true</c>. Otherwise the list is read only once from the <see cref="BACnetEndpoint.Client">client</see>.</param>
        public async Task<IReadOnlyCollection<BACnetObject>> GetStructuredObjectListAsync(bool forceUpdate = false)
        {
            List<BACnetObject> structObjList = null;
            if ((forceUpdate) || (this.structuredObjectList.Count == 0))
            {
                // Update object list:
                var objList = await GetObjectListAsync(forceUpdate).ConfigureAwait(false);

                if (objList.ContainsKey(BacnetObjectTypes.OBJECT_STRUCTURED_VIEW))
                {
                    // Read subordinate lists of all view objects:
                    var viewList = objList[BacnetObjectTypes.OBJECT_STRUCTURED_VIEW]
                        .Cast<BACnetView>()
                        .ToArray();
                    var res = await Task.WhenAll(viewList.Select(viewObj => ReadPropertyAsync(viewObj.ObjectId, BacnetPropertyIds.PROP_SUBORDINATE_LIST))).ConfigureAwait(false);
                    var subordinateLists = res.Select(list => list.Unwrap<List<BacnetObjectId>>()).ToArray();

                    // Find may recursive subordinates:
                    var objIds = subordinateLists.SelectMany(list => list).ToArray();
                    var duplicates = objIds
                        .GroupBy(id => id)
                        .Where(grp => grp.Count() > 1)
                        .ToDictionary(
                            grp => grp.Key,
                            grp => grp.Count() - 1
                        );
                    Func<BacnetObjectId, BACnetObject> TryResolveSubordinate = (objId) =>
                    {
                        if (objList.TryGetObject(objId, out var obj))
                            return (obj);
                        else
                        {
                            Trace.TraceWarning($"Dropped bad subordinate {objId} ({this}) from structured object list!");
                            return (null);
                        }
                    };
                    Func<BacnetObjectId, bool> TryPopDuplicate = (objId) =>
                    {
                        if (duplicates.TryGetValue(objId, out var count))
                        {
                            if (count <= 1)
                                duplicates.Remove(objId);
                            else
                                duplicates[objId]--;
                            Trace.TraceWarning($"Dropped recursive subordinate {objId} ({this}) from structured object list!");
                            return (true);
                        }
                        else
                            return (false);
                    };

                    // Build structured object list from read subordinates:
                    Debug.Assert(viewList.Length == subordinateLists.Length);
                    for (int i = 0; i < viewList.Length; i++)
                    {
                        var subordinates = subordinateLists[i]
                            .Select(sub => TryResolveSubordinate(sub))      // Skip bad (phantom-) subordinates.
                            .Where(obj => (obj != null))
                            .Where(obj => !TryPopDuplicate(obj.ObjectId))   // Skip duplicates.
                            .ToArray();
                        viewList[i].SetChildren(subordinates);
                    }

                    // Create structured object list from root objects:
                    structObjList = objList.Values
                        .SelectMany(objects => objects)
                        .Where(obj => obj.IsRoot)
                        .Cast<BACnetObject>()
                        .ToList();

                    Debug.Assert(structObjList.Count > 0);
                }
                else
                    // No structured view objects found:
                    structObjList = new List<BACnetObject>();

                StructuredObjectListUpdated = DateTime.Now;
            }

            lock (this)
            {
                if (structObjList != null)
                    this.structuredObjectList = structObjList;
                return (this.structuredObjectList.ToArray());
            }
        }
        #endregion
    }
    /// <summary>
    /// Represents a BACnet object.
    /// </summary>
    public class BACnetObject
    {
        #region Constants
        /// <summary>
        /// Placeholder to imply <i>to use for all object types</i>.
        /// </summary>
        private const BacnetObjectTypes AnyType = (BacnetObjectTypes)0xFFFFFF;
        /// <summary>
        /// Common properties, separated per <see cref="BacnetObjectTypes"/>.
        /// </summary>
        internal static readonly IReadOnlyDictionary<BacnetObjectTypes, BacnetPropertyIds[]> CommonProperties = new Dictionary<BacnetObjectTypes, BacnetPropertyIds[]>()
        {
            { AnyType, new BacnetPropertyIds[] { BacnetPropertyIds.PROP_OBJECT_NAME } },

            { BacnetObjectTypes.OBJECT_GROUP, new BacnetPropertyIds[] { BacnetPropertyIds.PROP_LIST_OF_GROUP_MEMBERS } }
        };
        #endregion


        protected BACnetObject() { }
        internal BACnetObject(BACnetDevice device, BacnetObjectId objectId)
        {
            this.Device = device;
            this.ObjectId = objectId;
        }


        #region Properties.Management
        public bool IsRoot => (Parent == null);

        public BACnetView Parent { internal set; get; }
        #endregion
        #region Properties.Services
        /// <summary>
        /// Returns the the value of the specified <paramref name="property"/>.
        /// </summary>
        /// <remarks>
        /// Fails if <paramref name="property"/> does not exist!
        /// </remarks>
        public object this[BacnetPropertyIds property]
        {
            get { lock (this) { return (properties[property]); } }
        }
        private Dictionary<BacnetPropertyIds, object> properties = new Dictionary<BacnetPropertyIds, object>();
        #endregion
        #region Properties
        public BACnetDevice Device { get; }
        public BacnetObjectId ObjectId { get; }
        #endregion


        public override string ToString() => ObjectId.ToString();


        #region Initialization
        /// <summary>
        /// Initialize common properties.
        /// </summary>
        /// <returns></returns>
        internal Task InitAsync()
        {
            // Update common properties:
            CommonProperties.TryGetValue(AnyType, out var commonAllTypes);
            CommonProperties.TryGetValue(ObjectId.Type, out var commonThisType);

            return (UpdatePropertiesAsync(commonAllTypes
                .Union(commonThisType ?? new BacnetPropertyIds[0])
                .Select(id => new BacnetPropertyReference(id))
                .ToArray()));
        }
        #endregion
        #region Management
        public bool Contains(BacnetPropertyIds property)
        {
            lock (this)
            {
                return (properties.ContainsKey(property));
            }
        }
        #endregion


        #region Services
        /// <summary>
        /// Returns the values of all of this objects properties by use of the most efficient request.
        /// </summary>
        /// <param name="forceUpdate">Forces the property to be read if <c>true</c>. Otherwise the property is read only once from the <see cref="BACnetEndpoint.Client">client</see>.</param>
        public Task<IDictionary<BacnetPropertyIds, object>> GetPropertiesAsync(bool forceUpdate = false, params BacnetPropertyIds[] properties)
        {
            if (properties.IsEmpty())
                return (GetPropertiesAsync(BACnetEndpoint.AllProperties));
            else
                return (GetPropertiesAsync(properties
                    .Select(prop => (BacnetPropertyReference)prop)
                    .ToList()));
        }
        /// <inheritdoc cref="GetPropertiesAsync(bool, BacnetPropertyIds[])"/>
        public async Task<IDictionary<BacnetPropertyIds, object>> GetPropertiesAsync(IList<BacnetPropertyReference> properties, bool forceUpdate = false)
        {
            if ((forceUpdate) || (!this.properties.IsEmpty()))
                await UpdatePropertiesAsync(properties).ConfigureAwait(false);

            // (BETA) ...
            // Accessing (-> updating and returning) the property is not locked!
            // > Improve this to ensure a consistent value to be returned.

            lock (this)
            {
                if (object.ReferenceEquals(properties, BACnetEndpoint.AllProperties))
                    return (this.properties.ToDictionary(item => item.Key, item => item.Value));
                else
                    return (properties
                        .Select(item => (BacnetPropertyIds)item.propertyIdentifier)
                        .Where(id => this.properties.ContainsKey(id))
                        .ToDictionary(
                            id => id,
                            id => this.properties[id]
                        ));
            }
        }
        /// <summary>
        /// Returns the value of the specified <paramref name="property"/> by use of the most efficient request.
        /// </summary>
        /// <param name="forceUpdate">Forces the property to be read if <c>true</c>. Otherwise the property is read only once from the <see cref="BACnetEndpoint.Client">client</see>.</param>
        public async Task<T> GetPropertyAsync<T>(BacnetPropertyIds property, uint arrayIndex = ASN1.BACNET_ARRAY_ALL, bool forceUpdate = false) => (T)await GetPropertyAsync(property, arrayIndex, forceUpdate).ConfigureAwait(false);
        /// <inheritdoc cref="GetPropertyAsync{T}(BacnetPropertyIds, uint, bool)"/>
        public async Task<object> GetPropertyAsync(BacnetPropertyIds property, uint arrayIndex = ASN1.BACNET_ARRAY_ALL, bool forceUpdate = false)
        {
            if ((forceUpdate) || (!this.properties.ContainsKey(property)))
                await UpdatePropertyAsync(property, arrayIndex).ConfigureAwait(false);

            // (BETA) ...
            // Accessing (-> updating and returning) the property is not locked!
            // > Improve this to ensure a consistent value to be returned.

            lock (this)
            {
                return (properties[property]);
            }
        }
        /// <summary>
        /// Updates the values of all or specified <paramref name="properties"/> by use of the most efficient request.
        /// </summary>
        public Task UpdatePropertiesAsync(params BacnetPropertyIds[] properties)
        {
            if (properties.IsEmpty())
                return (UpdatePropertiesAsync(BACnetEndpoint.AllProperties));
            else
                return (UpdatePropertiesAsync(properties
                    .Select(prop => (BacnetPropertyReference)prop)
                    .ToList()));
        }
        /// <inheritdoc cref="UpdatePropertiesAsync(BacnetPropertyIds[])"/>
        public async Task UpdatePropertiesAsync(IList<BacnetPropertyReference> properties)
        {
            if ((properties.Count > 1) || (object.ReferenceEquals(properties, BACnetEndpoint.AllProperties)))
            {
                // Read all properties:
                var res = await Device.ReadPropertiesAsync(ObjectId, properties).ConfigureAwait(false);
                var propRes = res.Single();
                lock (this)
                {
                    foreach (var propVal in propRes.values.OfSucceeded())
                    {
                        var propId = (BacnetPropertyIds)propVal.property.propertyIdentifier;
                        this.properties.AddOrUpdate(propId, propVal.value.Unwrap());
                    }
                }
            }
            else if (properties.Count == 1)
            {
                var propId = (BacnetPropertyIds)properties.First().propertyIdentifier;
                await UpdatePropertyAsync(propId).ConfigureAwait(false);
            }
            else
                ;
        }
        /// <summary>
        /// Reads the values of the specified <paramref name="property"/>.
        /// </summary>
        public async Task UpdatePropertyAsync(BacnetPropertyIds property, uint arrayIndex = ASN1.BACNET_ARRAY_ALL)
        {
            var res = await Device.ReadPropertyAsync(ObjectId, property, arrayIndex).ConfigureAwait(false);
            lock (this)
            {
                this.properties.AddOrUpdate(property, res.Unwrap());
            }
        }
        #endregion
    }
    /// <summary>
    /// Represents a BACnet structured view object.
    /// </summary>
    public class BACnetView : BACnetObject, IEnumerable<BACnetObject>
    {
        protected BACnetView() { }
        internal BACnetView(BACnetDevice device, BacnetObjectId objectId) : base(device, objectId)
        {
        }


        #region Properties
        public BACnetObject[] Children
        {
            get { lock (this) { return (children.ToArray()); } }
        }
        private List<BACnetObject> children = new List<BACnetObject>();
        #endregion


        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public IEnumerator<BACnetObject> GetEnumerator() => children.GetEnumerator();


        #region Management
        internal void SetChildren(IEnumerable<BACnetObject> subordinates)
        {
            // Clear old subordinates:
            children.ForEach(obj => obj.Parent = null);
            children.Clear();

            // Associate new subordinates:
            foreach (BACnetObject obj in subordinates)
            {
                Debug.Assert((obj.Device == this.Device), "Found subordinate of different device!");

                obj.Parent = this;
                children.Add(obj);
            }
        }
        #endregion
    }
    #endregion


    public static partial class Extensions
    {
        #region Debug
        /// <summary>
        /// Writes <paramref name="source"/> to log for debugging purposes.
        /// </summary>
        /// <param name="recursive">Dumps specified objects as well as their containing child objects as tree if <c>true</c>, otherwise the specified objects are dumped as a flat list.</param>
        [Conditional("DEBUG")]
        public static void Dump(this IEnumerable<BACnetObject> source, bool recursive = true)
        {
            int count = 0;
            foreach (var obj in source)
                Dump(obj, ref count, recursive);
        }
        private static void Dump(this BACnetObject source, ref int index, bool recursive = true, int level = 0)
        {
            var indent = "";
            if (level > 0)
                indent = string.Concat(Enumerable.Repeat(' ', (level * 2)));
            
            Debug.WriteLine($"{++index,3}. {indent}{source}");
            if ((recursive) && (source is BACnetView view))
            {
                level++;
                foreach (var obj in view)
                    Dump(obj, ref index, true, level);
            }
        }
        #endregion
    }
}

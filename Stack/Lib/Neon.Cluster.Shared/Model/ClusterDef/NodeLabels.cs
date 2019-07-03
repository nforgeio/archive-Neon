//-----------------------------------------------------------------------------
// FILE:	    NodeLabels.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Stack.Common;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes the standard NeonCluster and custom labels to be assigned to 
    /// a Docker node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Labels are name/value properties that can be assigned to the Docker daemon
    /// managing each host node.  These labels can be used by Swarm as container
    /// scheduling criteria.
    /// </para>
    /// <para>
    /// By convention, label names should use a reverse domain name form using a
    /// DNS domain you control.  For example, NeonCluster related labels are prefixed
    /// with <b>"io.neoncluster."</b>.  You should follow this convention for any
    /// custom labels you define.
    /// </para>
    /// <note>
    /// Docker reserves the use of labels without dots for itself.
    /// </note>
    /// <para>
    /// Label names must begin and end with a letter or digit and may include
    /// letters, digits, dashes and dots within.  Dots or dashes must not appear
    /// consecutively.
    /// </para>
    /// <note>
    /// Whitespace is not allowed in label values.  This was a bit of a surprise
    /// since Docker supports double quoting, but there it is.
    /// </note>
    /// <para>
    /// This class exposes several built-in NeonCluster properties.  You can use
    /// the <see cref="Custom"/> dictionary to add your own labels.
    /// </para>
    /// </remarks>
    public class NodeLabels
    {
        private NodeDefinition parentNode;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        public NodeLabels(NodeDefinition parentNode)
        {
            Covenant.Requires<ArgumentNullException>(parentNode != null);

            this.parentNode = parentNode;
        }

        //---------------------------------------------------------------------
        // Define global NeonCluster labels.

        /// <summary>
        /// Reserved label name that identifies the datacenter.
        /// </summary>
        public const string LabelDatacenter = ClusterDefinition.ReservedLabelPrefix + ".datacenter";

        /// <summary>
        /// Reserved label name that describes the environment.
        /// </summary>
        public const string LabelEnvironment = ClusterDefinition.ReservedLabelPrefix + ".environment";

        //---------------------------------------------------------------------
        // Define the node storage related labels.

        /// <summary>
        /// Reserved label name for <see cref="StorageCapacity"/>.
        /// </summary>
        public const string LabelStorageCapacity = ClusterDefinition.ReservedLabelPrefix + ".storage.capacity";

        /// <summary>
        /// Reserved label name for <see cref="StorageLocal"/>.
        /// </summary>
        public const string LabelStorageLocal = ClusterDefinition.ReservedLabelPrefix + ".storage.local";

        /// <summary>
        /// Reserved label name for <see cref="StorageSSD"/>.
        /// </summary>
        public const string LabelStorageSSD = ClusterDefinition.ReservedLabelPrefix + ".storage.ssd";

        /// <summary>
        /// Reserved label name for <see cref="StorageRedundant"/>.
        /// </summary>
        public const string LabelStorageRedundant = ClusterDefinition.ReservedLabelPrefix + ".storage.redundant";

        /// <summary>
        /// Reserved label name for <see cref="StorageEphemeral"/>.
        /// </summary>
        public const string LabelStorageEphemeral = ClusterDefinition.ReservedLabelPrefix + ".storage.ephemral";

        /// <summary>
        /// <b>io.neoncluster.storage.capacity</b> [<c>int</c>]: Specifies the node storage capacity
        /// in megabytes.  This defaults to <b>zero</b>.
        /// </summary>
        [JsonProperty(PropertyName = "storage_capacity", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(0)]
        public int StorageCapacity { get; set; } = 0;

        /// <summary>
        /// <b>io.neoncluster.storage.local</b> [<c>bool</c>]: Specifies whether the node storage is hosted
        /// on the node itself or is mounted as a remote file system or block device.  This defaults
        /// to <c>true</c>.
        /// </summary>
        [JsonProperty(PropertyName = "storage_local", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(true)]
        public bool StorageLocal { get; set; } = true;

        /// <summary>
        /// <b>io.neoncluster.storage.ssd</b> [<c>bool</c>]: Indicates that the storage is backed
        /// by SSDs as opposed to rotating hard drive.  This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "storage_ssd", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(false)]
        public bool StorageSSD { get; set; } = false;

        /// <summary>
        /// <b>io.neoncluster.storage.redundant</b> [<c>bool</c>]: Indicates that the storage is redundant.  This
        /// may be implemented locally using RAID1+ or remotely using network or cloud-based file systems.
        /// This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "storage_redundant", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(false)]
        public bool StorageRedundant { get; set; } = false;

        /// <summary>
        /// <b>io.neoncluster.storage.redundant</b> [<c>bool</c>]: Indicates that the storage is ephemeral.
        /// All data will be lost when the host is restarted.  This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "storage_ephemeral", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(false)]
        public bool StorageEphemeral { get; set; } = false;

        //---------------------------------------------------------------------
        // Define host compute related labels.

        /// <summary>
        /// Reserved label name for <see cref="ComputeCores"/>.
        /// </summary>
        public const string LabelComputeCores = ClusterDefinition.ReservedLabelPrefix + ".compute.cores";

        /// <summary>
        /// Reserved label name for <see cref="ComputeArchitecture"/>.
        /// </summary>
        public const string LabelComputeArchitecture = ClusterDefinition.ReservedLabelPrefix + ".compute.architecture";

        /// <summary>
        /// Reserved label name for <see cref="ComputeRAM"/>.
        /// </summary>
        public const string LabelComputeRAM = ClusterDefinition.ReservedLabelPrefix + ".compute.ram";

        /// <summary>
        /// <b>io.neoncluster.compute.cores</b> [<c>int</c>]: Specifies the number of CPU cores.
        /// This defaults to <b>zero</b>.
        /// </summary>
        [JsonProperty(PropertyName = "compute_cores", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(0)]
        public int ComputeCores { get; set; } = 0;

        /// <summary>
        /// <b>io.neoncluster.compute.architecture</b> [<c>int</c>]: Specifies the CPU architecture.
        /// This defaults to <see cref="CpuArchitecture.x64"/>.
        /// </summary>
        [JsonProperty(PropertyName = "compute_architecture", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(CpuArchitecture.x64)]
        public CpuArchitecture ComputeArchitecture { get; set; } = CpuArchitecture.x64;

        /// <summary>
        /// <b>io.neoncluster.compute.ram</b> [<c>int</c>]: Specifies the the available RAM in
        /// megabytes.  This defaults to <b>zero</b>.
        /// </summary>
        [JsonProperty(PropertyName = "compute_ram", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(0)]
        public int ComputeRAM { get; set; } = 0;

        //---------------------------------------------------------------------
        // Define physical host labels.

        private string physicalFaultDomain = string.Empty;

        /// <summary>
        /// Reserved label name for <see cref="LabelPhysicalPower"/>.
        /// </summary>
        public const string LabelPhysicalLocation = ClusterDefinition.ReservedLabelPrefix + ".physical.location";

        /// <summary>
        /// Reserved label name for <see cref="LabelPhysicalModel"/>.
        /// </summary>
        public const string LabelPhysicalModel = ClusterDefinition.ReservedLabelPrefix + ".physical.machine";

        /// <summary>
        /// Reserved label name for <see cref="PhysicalFaultDomain"/>.
        /// </summary>
        public const string LabelPhysicalFaultDomain = ClusterDefinition.ReservedLabelPrefix + ".physical.faultdomain";

        /// <summary>
        /// Reserved label name for <see cref="LabelPhysicalPower"/>.
        /// </summary>
        public const string LabelPhysicalPower = ClusterDefinition.ReservedLabelPrefix + ".physical.power";

        /// <summary>
        /// <b>io.neoncluster.physical.location</b> [<c>string</c>]: A free format string describing the
        /// physical location of the server.  This defaults to the 
        /// <b>empty string</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You should use a consistent convention to describe a physical machine location.
        /// Here are some examples:
        /// </para>
        /// <list type="bullet">
        /// <item><i>rack-slot</i></item>
        /// <item><i>rack-number</i>/<i>rack-slot</i></item>
        /// <item><i>row</i>/<i>rack-number</i>/<i>rack-slot</i></item>
        /// <item><i>floor</i>/<i>row</i>/<i>rack-number</i>/<i>rack-slot</i></item>
        /// <item><i>building</i>/<i>floor</i>/<i>row</i>/<i>rack-number</i>/<i>rack-slot</i></item>
        /// </list>
        /// </remarks>
        [JsonProperty(PropertyName = "physical_location", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string PhysicalLocation { get; set; } = string.Empty;

        /// <summary>
        /// <b>io.neoncluster.physical.model</b> [<c>string</c>]: A free format string describing the
        /// physical server computer model (e.g. <b>Dell-PowerEdge-R220</b>).  This defaults to the <b>empty string</b>.
        /// </summary>
        [JsonProperty(PropertyName = "physical_machine", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string PhysicalMachine { get; set; } = string.Empty;

        /// <summary>
        /// <b>io.neoncluster.physical.faultdomain</b> [<c>string</c>]: A free format string 
        /// grouping the host by the possibility of underlying hardware or software failures.
        /// This defaults to the <b>empty string</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The idea here is to identify broad possible failure scenarios and to assign hosts
        /// to fault domains in such a way that a failure for one domain will be unlikely
        /// to impact the hosts in another.  These groupings can be used to spread application
        /// containers across available fault domains such that an application has a reasonable 
        /// potential to continue operating in the face of hardware or network failures.
        /// </para>
        /// <para>
        /// Fault domains will be mapped to your specific hardware and networking architecture.
        /// Here are some example scenarios:
        /// </para>
        /// <list type="table">
        /// <item>
        ///     <term><b>VMs on one machine:</b></term>
        ///     <description>
        ///     <para>
        ///     This will be a common setup for development and test where every host
        ///     node is simply a virtual machine running locally.  In this case, the
        ///     fault domain could be set to the virtual machine name such that
        ///     failures can be tested by simply stopping a VM.
        ///     </para>
        ///     <note>
        ///     If no fault domain is specified for a node, then the fault domain
        ///     will default to the node name.
        ///     </note>
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Single Rack:</b></term>
        ///     <description>
        ///     For a cluster deployed to a single rack with a shared network connection,
        ///     the fault domain will typically be the physical machine such that the 
        ///     loss of a machine can be tolerated.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Multiple Racks:</b></term>
        ///     <description>
        ///     For clusters deployed to multiple racks, each with their own network
        ///     connection, the fault domain will typically be set at the rack
        ///     level, such that the loss of a rack or its network connectivity can
        ///     be tolerated.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term><b>Advanced:</b></term>
        ///     <description>
        ///     <para>
        ///     More advanced scenarios are possible.  For example, a datacenter may
        ///     have multiple pods, floors, or buildings that each have redundant 
        ///     infrastructure such as power and networking.  You could set the fault
        ///     domain at the pod or floor level.
        ///     </para>
        ///     <para>
        ///     For clusters that span physical datacenters, you could potentially map
        ///     each datacenter to an fault domain.
        ///     </para>
        ///     </description>
        /// </item>
        /// </list>
        /// </remarks>
        [JsonProperty(PropertyName = "physical_fault_domain", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string PhysicalFaultDomain
        {
            get { return string.IsNullOrWhiteSpace(physicalFaultDomain) ? parentNode.Name : physicalFaultDomain; }
            set { physicalFaultDomain = value; }
        }

        /// <summary>
        /// <b>io.neoncluster.physical.power</b> [<c>string</c>]: Describes host the physical power
        /// to the server may be controlled.  This defaults to the <b>empty string</b>.
        /// </summary>
        /// <remarks>
        /// <note>
        /// The format for this property is not currently defined.
        /// </note>
        /// <para>
        /// This field includes the information required to remotely control the power to
        /// the physical host machine via a Power Distribution Unit (PDU).
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "physical_power", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue("")]
        public string PhysicalPower { get; set; } = string.Empty;

        // $todo(jeff.lill): Define the format of this string for APC PDUs.

        //---------------------------------------------------------------------
        // Build-in cluster logging related labels.

        /// <summary>
        /// Reserved label name for <see cref="LogEsData"/>.
        /// </summary>
        public const string LabelLogEsData = ClusterDefinition.ReservedLabelPrefix + ".log.esdata";

        /// <summary>
        /// <b>io.neoncluster.log.esdata</b> [<c>bool</c>]: Indicates that the node should host an
        /// Elasticsearch node to be used to store cluster logging data. This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "log_es_data", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Include)]
        [DefaultValue(false)]
        public bool LogEsData { get; set; } = false;

        //---------------------------------------------------------------------
        // Implementation

        /// <summary>
        /// Enumerates the NeonCluster standard Docker labels and values.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Standard
        {
            get
            {
                // $note(jeff.lill): This code will need to be updated whenever new 
                //              standard labels are added.

                var list = new List<KeyValuePair<string, object>>(20);

                list.Add(new KeyValuePair<string, object>(LabelStorageCapacity,     StorageCapacity));
                list.Add(new KeyValuePair<string, object>(LabelStorageLocal,        StorageLocal));
                list.Add(new KeyValuePair<string, object>(LabelStorageSSD,          StorageSSD));
                list.Add(new KeyValuePair<string, object>(LabelStorageRedundant,    StorageRedundant));
                list.Add(new KeyValuePair<string, object>(LabelStorageEphemeral,    StorageEphemeral));
                list.Add(new KeyValuePair<string, object>(LabelComputeCores,        ComputeCores));
                list.Add(new KeyValuePair<string, object>(LabelComputeArchitecture, ComputeArchitecture));
                list.Add(new KeyValuePair<string, object>(LabelComputeRAM,          ComputeRAM));
                list.Add(new KeyValuePair<string, object>(LabelPhysicalLocation,    PhysicalLocation));
                list.Add(new KeyValuePair<string, object>(LabelPhysicalModel,       PhysicalMachine));
                list.Add(new KeyValuePair<string, object>(LabelPhysicalFaultDomain, PhysicalFaultDomain));
                list.Add(new KeyValuePair<string, object>(LabelPhysicalPower,       PhysicalPower));
                list.Add(new KeyValuePair<string, object>(LabelLogEsData,           LogEsData));

                return list;
            }
        }

        /// <summary>
        /// Custom node labels.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this property to define custom host node labels.
        /// </para>
        /// <note>
        /// The <b>io.neoncluster</b> label prefix is reserved.
        /// </note>
        /// <note>
        /// Labels names will be converted to lowercase when the Docker daemon is started
        /// on the host node.
        /// </note>
        /// </remarks>
        [JsonProperty(PropertyName = "custom")]
        public Dictionary<string, string> Custom { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Returns a clone of the current instance.
        /// </summary>
        /// <param name="parentNode">The cloned parent node.</param>
        /// <returns>The clone.</returns>
        public NodeLabels Clone(NodeDefinition parentNode)
        {
            Covenant.Requires<ArgumentNullException>(parentNode != null);

            var clone = new NodeLabels(parentNode);

            this.CopyTo(clone);

            return clone;
        }

        /// <summary>
        /// Performs a deep copy of the current cluster node to another instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        internal void CopyTo(NodeLabels target)
        {
            Covenant.Requires<ArgumentNullException>(target != null);

            target.StorageCapacity          = this.StorageCapacity;
            target.StorageLocal             = this.StorageLocal;
            target.StorageSSD               = this.StorageSSD;
            target.StorageRedundant         = this.StorageRedundant;
            target.StorageEphemeral         = this.StorageEphemeral;

            target.ComputeCores             = this.ComputeCores;
            target.ComputeArchitecture      = this.ComputeArchitecture;
            target.ComputeRAM               = this.ComputeRAM;

            target.PhysicalLocation         = this.PhysicalLocation;
            target.PhysicalMachine          = this.PhysicalMachine;
            target.PhysicalFaultDomain      = this.PhysicalFaultDomain;
            target.PhysicalPower            = this.PhysicalPower;

            target.LogEsData                = this.LogEsData;

            foreach (var item in this.Custom)
            {
                target.Custom.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Validates the node definition.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ArgumentException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            // Verify that custom node label names satisfy the 
            // following criteria:
            // 
            //      1. Be at least one character long.
            //      2. Start and end with an alpha numeric character.
            //      3. Include only alpha numeric characters, dashes,
            //         underscores or dots.
            //      4. Does not have consecutive dots or dashes.

            foreach (var item in Custom)
            {
                if (item.Key.Length == 0)
                {
                    throw new ClusterDefinitionException($"Custom node label for value [{item.Value}] has no label name.");
                }
                else if (item.Key.Contains(".."))
                {
                    throw new ClusterDefinitionException($"Custom node name [{item.Key}] has consecutive dots.");
                }
                else if (item.Key.Contains("--"))
                {
                    throw new ClusterDefinitionException($"Custom node name [{item.Key}] has consecutive dashes.");
                }
                else if (!char.IsLetterOrDigit(item.Key.First()))
                {
                    throw new ClusterDefinitionException($"Custom node name [{item.Key}] does not begin with a letter or digit.");
                }
                else if (!char.IsLetterOrDigit(item.Key.Last()))
                {
                    throw new ClusterDefinitionException($"Custom node name [{item.Key}] does not begin with a letter or digit.");
                }

                foreach (var ch in item.Key)
                {
                    if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_')
                    {
                        continue;
                    }

                    throw new ClusterDefinitionException($"Custom node name [{item.Key}] has an illegal character.  Only letters, digits, dash and dots are allowed.");
                }

                foreach (var ch in item.Value)
                {
                    if (char.IsWhiteSpace(ch))
                    {
                        throw new ClusterDefinitionException($"Whitespace in the value of [{item.Key}={item.Value}] is not allowed.");
                    }
                }
            }
        }
    }
}

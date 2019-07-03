//-----------------------------------------------------------------------------
// FILE:	    DotnetOptions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
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
    /// Describes the .NET Core options for NeonCluster hosts.
    /// </summary>
    public class DotnetOptions
    {
        private const bool      defaultEnabled = false;
        private const string    defaultVersion = "dotnet-dev-1.0.0-preview2-003131";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DotnetOptions()
        {
        }

        /// <summary>
        /// Indicates whether .NET Core is to be deployed to cluster hosts.
        /// This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "enabled", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultEnabled)]
        public bool Enabled { get; set; } = defaultEnabled;

        /// <summary>
        /// The version of .NET Core to be installed.  This defaults to a reasonable
        /// recent version.
        /// </summary>
        [JsonProperty(PropertyName = "version", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultVersion)]
        public string Version { get; set; } = defaultVersion;

        /// <summary>
        /// Validates the options definition and also ensures that all <c>null</c> properties are
        /// initialized to their default values.
        /// </summary>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <exception cref="ClusterDefinitionException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(ClusterDefinition clusterDefinition)
        {
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);

            if (string.IsNullOrWhiteSpace(Version))
            {
                throw new ClusterDefinitionException($"Invalid version [{nameof(Version)}={Version}].");
            }
        }

        /// <summary>
        /// Returns a deep clone of the current instance.
        /// </summary>
        /// <returns>The clone.</returns>
        public DotnetOptions Clone()
        {
            return new DotnetOptions()
            {
                Version = this.Version,
                Enabled = this.Enabled,
            };
        }
    }
}

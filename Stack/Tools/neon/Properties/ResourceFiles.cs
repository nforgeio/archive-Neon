//-----------------------------------------------------------------------------
// FILE:	    ResourceFiles.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Cluster;

namespace NeonCluster
{
    /// <summary>
    /// Provides a nice abstraction over the files and folders from the
    /// <b>Linux</b> folder that are embedded into the application.
    /// </summary>
    /// <remarks>
    /// This class must be manually modified to remain in sync with changes 
    /// to the <b>Linux</b> source folder.
    /// </remarks>
    public static class ResourceFiles
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Simulates a file.
        /// </summary>
        public class File
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">The local file name.</param>
            /// <param name="contents">The file contents.</param>
            /// <param name="hasVariables">
            /// Indicates whether the file references variables from a <see cref="ClusterDefinition"/>
            /// that need to be expanded.
            /// </param>
            public File(string name, byte[] contents, bool hasVariables = false)
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
                Covenant.Requires<ArgumentNullException>(contents != null);

                this.Name         = name;
                this.Contents     = contents;
                this.HasVariables = hasVariables;
            }

            /// <summary>
            /// Returns the local file name.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Returns the file contents.
            /// </summary>
            public byte[] Contents { get; private set; }

            /// <summary>
            /// Creates a stream over the file contents.
            /// </summary>
            /// <returns>The new <see cref="Stream"/>.</returns>
            public Stream ToStream()
            {
                return new MemoryStream(Contents);
            }

            /// <summary>
            /// Indicates whether the file references variables from a <see cref="ClusterDefinition"/>
            /// that need to be expanded.
            /// </summary>
            public bool HasVariables { get; private set; }
        }

        /// <summary>
        /// Simulates a file folder.
        /// </summary>
        public class Folder
        {
            private Dictionary<string, File> files;
            private Dictionary<string, Folder> folders;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">The folder name.</param>
            /// <param name="files">Optional files to be added.</param>
            /// <param name="folders">Optional folders to be added.</param>
            public Folder(string name, IEnumerable<File> files = null, IEnumerable<Folder> folders = null)
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));

                this.Name    = name;
                this.files   = new Dictionary<string, File>();
                this.folders = new Dictionary<string, Folder>();

                if (files != null)
                {
                    foreach (var file in files)
                    {
                        this.files.Add(file.Name, file);
                    }
                }

                if (folders != null)
                {
                    foreach (var folder in folders)
                    {
                        this.folders.Add(folder.Name, folder);
                    }
                }
            }

            /// <summary>
            /// Returns the folder name.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Adds a file to the folder.
            /// </summary>
            /// <param name="name">The local file name.</param>
            /// <param name="file">The file.</param>
            public void AddFile(string name, File file)
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
                Covenant.Requires<ArgumentNullException>(file != null);

                files.Add(name, file);
            }

            /// <summary>
            /// Enumerates the files in the folder.
            /// </summary>
            /// <returns>An <see cref="IEnumerable{File}"/>.</returns>
            public IEnumerable<File> Files()
            {
                return files.Values;
            }

            /// <summary>
            /// Enumerates the sub folders.
            /// </summary>
            /// <returns>An <see cref="IEnumerable{Folder}"/>.</returns>
            public IEnumerable<Folder> Folders()
            {
                return folders.Values;
            }

            /// <summary>
            /// Returns the local file with the specified name.
            /// </summary>
            /// <param name="name">The local file name.</param>
            /// <returns>The <see cref="File"/>.</returns>
            /// <exception cref="FileNotFoundException">Thrown if the file is not present.</exception>
            public File GetFile(string name)
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));

                File file;

                if (files.TryGetValue(name, out file))
                {
                    return file;
                }

                throw new FileNotFoundException($"File [{name}] is not present.", name);
            }

            /// <summary>
            /// Returns the local folder with the specified name.
            /// </summary>
            /// <param name="name">The local folder name.</param>
            /// <returns>The <see cref="File"/>.</returns>
            /// <exception cref="FileNotFoundException">Thrown if the folder is not present.</exception>
            public Folder GetFolder(string name)
            {
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));

                Folder folder;

                if (folders.TryGetValue(name, out folder))
                {
                    return folder;
                }

                throw new FileNotFoundException($"Folder [{name}] is not present.", name);
            }
        }

        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Returns the Linux root folder.
        /// </summary>
        public static Folder Linux { get; private set; }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ResourceFiles()
        {
            Linux = new Folder("Linux",
                folders: new List<Folder>()
                {
                    new Folder("Ubuntu-16.04",
                        folders: new List<Folder>()
                        {
                            new Folder("conf",
                                files: new List<File>()
                                {
                                    new File("cluster.conf.sh", Properties.U1604.cluster_conf, hasVariables: true),
                                }),
                            new Folder("setup",
                                files: new List<File>()
                                {
                                    new File("setup-apt-proxy.sh", Properties.U1604.setup_apt_proxy, hasVariables: true),
                                    new File("setup-apt-ready.sh", Properties.U1604.setup_apt_ready, hasVariables: true),
                                    new File("setup-consul-proxy.sh", Properties.U1604.setup_consul_proxy, hasVariables: true),
                                    new File("setup-consul-server.sh", Properties.U1604.setup_consul_server, hasVariables: true),
                                    new File("setup-docker.sh", Properties.U1604.setup_docker, hasVariables: true),
                                    new File("setup-dotnet.sh", Properties.U1604.setup_dotnet, hasVariables: true),
                                    new File("setup-environment.sh", Properties.U1604.setup_environment, hasVariables: true),
                                    new File("setup-exists.sh", Properties.U1604.setup_exists, hasVariables: true),
                                    new File("setup-node.sh", Properties.U1604.setup_node, hasVariables: true),
                                    new File("setup-ntp.sh", Properties.U1604.setup_ntp, hasVariables: true),
                                    new File("setup-prep-node.sh", Properties.U1604.setup_prep_node, hasVariables: true),
                                    new File("setup-ssd.sh", Properties.U1604.setup_ssd, hasVariables: true),
                                    new File("setup-utility.sh", Properties.U1604.setup_utility, hasVariables: true),
                                    new File("setup-vault-server.sh", Properties.U1604.setup_vault_server, hasVariables: true),
                                    new File("setup-vault-client.sh", Properties.U1604.setup_vault_client, hasVariables: true)
                                }),
                            new Folder("tools",
                                files: new List<File>()
                                {
                                    new File("docker-volume-create.sh", Properties.U1604.docker_volume_create, hasVariables: true),
                                    new File("docker-volume-exists.sh", Properties.U1604.docker_volume_exists, hasVariables: true),
                                    new File("docker-volume-rm.sh", Properties.U1604.docker_volume_rm, hasVariables: true)
                                })
                        })
                });
        }
    }
}

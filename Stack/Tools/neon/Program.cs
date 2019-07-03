//-----------------------------------------------------------------------------
// FILE:	    Program.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Stack;
using Neon.Stack.Common;
using Neon.Stack.Diagnostics;

namespace NeonCluster
{
    /// <summary>
    /// This tool is used to configure the nodes of a Neon Docker Swarm cluster.
    /// See <b>~/Stack/Dock/Ubuntu-16.04 Cluster Deploy.docx</b> for more information.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            string usage = $@"
Neon Cluster Configuration Tool: neon [v{Build.Version}]
{Build.Copyright}

USAGE:

    neon [OPTIONS] COMMAND [ARG...]

COMMAND SUMMARY:

    neon help           COMMAND

    neon add            CLUSTER-ADMIN
    neon cert           CMD...
    neon consul         [--neon-node=NAME] ARGS
    neon create         key
    neon create         password
    neon docker         [--neon-node=NAME] ARGS
    neon download        SOURCE TARGET [NODE]
    neon example
    neon exec           [--neon-node=NAME] BASH-CMD
    neon get            [VALUE-EXPR]
    neon health
    neon key            CLUSTER
    neon list
    neon login          [CLUSTER]
    neon logout
    neon prepare        [CLUSTER-DEF]
    neon prepare        SERVER1 [SERVER2...]
    neon proxy          CMD...
    neon reboot         NODE...
    neon remove         CLUSTER
    neon scp            [NODE]
    neon setup          [CLUSTER-DEF]
    neon ssh            [NODE]
    neon validate       CLUSTER-DEF
    neon update-tools
    neon update-tools   SERVER1 [SERVER2... ]
    neon upload         SOURCE TARGET [NODE...]
    neon validate       [CLUSTER-DEF]
    neon vault          [--neon-node=NAME] ARGS

ARGUMENTS:

    ARGS                - Command pass-thru arguments.

    CLUSTER             - Names the cluster to be selected for subsequent
                          operations.

    CLUSTER-ADMIN       - Path to a cluster admin information file including
                          the cluster definition and admin credentials.

    CLUSTER-DEF         - Path to a cluster definition file.  This is
                          optional for some commands when logged in.

    CMD...              - Subcommand and arguments.

    BASH-CMD            - Bash command.

    NODE                - Identifies a cluster node by name.

    VALUE-EXPR          - A cluster value expression.  See the command for
                          more details.

    SERVER1...          - IP addresses or FQDNs of target servers

    SOURCE              - Path to a source file.

    TARGET              - Path to a destination file.

OPTIONS:

    --help                              - Display help
    -u=USER, --user=USER                - Server admin user name
    -p=PASSWORD, --password=PASSWORD    - Server admin password
    --os=ubuntu=16.04                   - Target host OS (default)
    --log=LOG-FOLDER                    - Optional log folder path
    -q, --quiet                         - Disables logging and console progress
    -m=COUNT, --max-parallel=COUNT      - Maximum number of nodes to be 
                                          configured in parallel [default=1]
    -w=SECONDS, --wait=SECONDS          - Seconds to delay for cluster
                                          stablization.  Defaults to 60.
";
            // Disable any log4net logging that might be performed by library classes.

            LogManager.LogLevel = LogLevel.None;

            // Configure the encrypted user-specific application data folder and initialize
            // the subfolders.

            ClusterRootFolder    = NeonClusterHelper.GetClusterRootFolder();
            ClusterSecretsFolder = NeonClusterHelper.GetClusterSecretsFolder();
            CurrentClusterPath   = Path.Combine(ClusterSecretsFolder, ".current");
            ClusterTempFolder    = Path.Combine(ClusterRootFolder, "temp");

            Directory.CreateDirectory(ClusterSecretsFolder);
            Directory.CreateDirectory(ClusterTempFolder);

            // It looks like .NET 4.5 defaults to just SSL 3.0.  This prevents us from connecting
            // to more advanced services (like HashiCorp Vault) that disable older insecure encryption
            // protocols.  We're going to enable all known protocols here.

#if !NETCORE
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif

            // Process the command line.

            try
            {
                ICommand command;

                CommandLine = new CommandLine(args);

                CommandLine.DefineOption("-u", "--user");
                CommandLine.DefineOption("-p", "--password");
                CommandLine.DefineOption("-os").Default = "ubuntu=14.04";
                CommandLine.DefineOption("-q", "--quiet");
                CommandLine.DefineOption("-m", "--max-parallel").Default = "1";
                CommandLine.DefineOption("-w", "--wait").Default = "60";

                var validOptions = new HashSet<string>();

                validOptions.Add("-u");
                validOptions.Add("--user");
                validOptions.Add("-p");
                validOptions.Add("--password");
                validOptions.Add("--os");
                validOptions.Add("--log");
                validOptions.Add("-q");
                validOptions.Add("--quiet");
                validOptions.Add("-m");
                validOptions.Add("--max-parallel");
                validOptions.Add("-w");
                validOptions.Add("--wait");

                if (CommandLine.Arguments.Length == 0)
                {
                    Console.WriteLine(usage);
                    Program.Exit(0);
                }

                var commands = new List<ICommand>()
                {
                    new AddCommand(),
                    new CertCommand(),
                    new ConsulCommand(),
                    new CreateKeyCommand(),
                    new CreatePasswordCommand(),
                    new DockerCommand(),
                    new DownloadCommand(),
                    new ExecCommand(),
                    new ExampleCommand(),
                    new GetCommand(),
                    new HealthCommand(),
                    new ListCommand(),
                    new LoginCommand(),
                    new LogoutCommand(),
                    new PrepareCommand(),
                    new ProxyCommand(),
                    new RebootCommand(),
                    new RemoveCommand(),
                    new ScpCommand(),
                    new SetupCommand(),
                    new SshCommand(),
                    new UpdateToolsCommand(),
                    new UploadCommand(),
                    new ValidateCommand(),
                    new VaultCommand()
                };

                if (CommandLine.Arguments[0] == "help")
                {
                    if (CommandLine.Arguments.Length == 1)
                    {
                        Console.WriteLine(usage);
                        Program.Exit(0);
                    }

                    CommandLine = CommandLine.Shift(1);

                    command = GetCommand(CommandLine, commands);

                    if (command == null)
                    {
                        Console.Error.WriteLine($"*** ERROR: Unknown command: {CommandLine.Arguments[0]}");
                        Console.Error.WriteLine(usage);
                        Program.Exit(1);
                    }

                    command.Help();
                    Program.Exit(0);
                }

                // Locate and run the command.

                command = GetCommand(CommandLine, commands);

                if (command == null)
                {
                    Console.Error.WriteLine($"*** ERROR: Unknown command: {CommandLine.Arguments[0]}");
                    Program.Exit(1);
                }

                if (!command.IsPassThru)
                {
                    Console.WriteLine();

                    // Process the standard command line options.

                    var os = CommandLine.GetOption("--os", "ubuntu-16.04").ToLowerInvariant();

                    switch (os)
                    {
                        // Choose reasonable operating system specific defaults here.

                        case "ubuntu-16.04":

                            OSProperties = new DockerOSProperties()
                            {
                                TargetOS      = TargetOS.Ubuntu_16_04,
                                StorageDriver = DockerStorageDrivers.Overlay2
                            };
                            break;

                        default:

                            Console.Error.WriteLine($"*** ERROR: [--os={os}] is not a supported target operating system.");
                            Program.Exit(1);
                            break;
                    }

                    // Load the user name and password from the command line options, if present.

                    UserName = CommandLine.GetOption("--user");
                    Password = CommandLine.GetOption("--password");

                    // Handle the other options.

                    LogPath = CommandLine.GetOption("--log");
                    Quiet   = CommandLine.GetFlag("--quiet");

                    if (LogPath == string.Empty)
                    {
                        LogPath = Path.Combine(".", "neon.log");
                    }

                    if (Quiet)
                    {
                        LogPath = null;
                    }
                    else if (LogPath != null)
                    {
                        LogPath = Path.GetFullPath(LogPath);

                        Directory.CreateDirectory(LogPath);
                    }

                    var maxParallelOption = CommandLine.GetOption("--max-parallel");
                    int maxParallel;

                    if (!int.TryParse(maxParallelOption, out maxParallel) || maxParallel < 1)
                    {
                        Console.Error.WriteLine($"*** ERROR: [--max-parallel={maxParallelOption}] option is not valid.");
                        Program.Exit(1);
                    }

                    Program.MaxParallel = maxParallel;

                    var     waitSecondsOption = CommandLine.GetOption("--wait");
                    double  waitSeconds;

                    if (!double.TryParse(waitSecondsOption, out waitSeconds) || waitSeconds < 0)
                    {
                        Console.Error.WriteLine($"*** ERROR: [--wait={waitSecondsOption}] option is not valid.");
                        Program.Exit(1);
                    }

                    Program.WaitSeconds = waitSeconds;

                    if (CommandLine.Arguments.Length == 0)
                    {
                        Console.Error.Write("*** ERROR: The [command] argument is required.");
                        Console.Error.WriteLine(string.Empty);
                        Console.Error.WriteLine(usage);
                        Program.Exit(1);
                    }

                    // Make sure there are no unexpected command line options.

                    if (!command.IsPassThru)
                    {
                        validOptions.Add("--help");
                    }

                    foreach (var optionName in command.ExtendedOptions)
                    {
                        validOptions.Add(optionName);
                    }

                    foreach (var option in CommandLine.Options)
                    {
                        if (!validOptions.Contains(option.Key))
                        {
                            var commandWords = string.Empty;

                            foreach (var word in command.Words)
                            {
                                if (commandWords.Length > 0)
                                {
                                    commandWords += " ";
                                }

                                commandWords += word;
                            }

                            Console.WriteLine($"*** ERROR: Command [{commandWords}] does not support [{option.Key}].");
                            Program.Exit(1);
                        }
                    }
                }

                // Load the current cluster if there is one.

                if (File.Exists(CurrentClusterPath))
                {
                    var clusterName        = File.ReadAllText(CurrentClusterPath).Trim();
                    var clusterSecretsPath = GetClusterSecretsPath(clusterName);

                    if (File.Exists(clusterSecretsPath))
                    {
                        ClusterSecrets = NeonClusterHelper.LoadClusterSecrets(clusterName);
                    }
                    else
                    {
                        // The referenced cluster file doesn't exist so quietly remove the ".current" file.

                        File.Delete(CurrentClusterPath);
                    }
                }

                // Run the command.

                if (command.NeedsSshCredentials)
                {
                    if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
                    {
                        Console.WriteLine();
                        Console.WriteLine("    Enter cluster SSH credentials:");
                        Console.WriteLine("    ------------------------------");
                    }

                    while (string.IsNullOrWhiteSpace(UserName))
                    {
                        Console.Write("    username: ");
                        UserName = Console.ReadLine();
                    }

                    while (string.IsNullOrEmpty(Password))
                    {
                        Console.Write("    password: ");

                        Password = NeonHelper.ReadConsolePassword();
                    }
                }

                if (command.IsPassThru)
                {
                    // Disable standard logging.

                    Quiet = true;

                    // We don't shift the command line for pass-thru commands 
                    // because we don't want to change the order of any options.

                    command.Run(CommandLine);
                }
                else
                {
                    command.Run(CommandLine.Shift(command.Words.Length));
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"*** ERROR: {NeonHelper.ExceptionError(e)}");
                Console.Error.WriteLine(string.Empty);
                Program.Exit(1);
            }

            Program.Exit(0);
        }

        /// <summary>
        /// Message written then a user is not logged into a cluster.
        /// </summary>
        public const string MustLoginMessage = "*** ERROR: You must first log into a cluster.";

        /// <summary>
        /// Path to the WinSCP program executable.
        /// </summary>
        public static readonly string WinScpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"WinSCP\WinSCP.exe");

        /// <summary>
        /// Path to the PuTTY program executable.
        /// </summary>
        public static readonly string PuttyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"PuTTY\putty.exe");

        /// <summary>
        /// Attempts to match the command line to the <see cref="ICommand"/> to be used
        /// to implement the command.
        /// </summary>
        /// <param name="commandLine">The command line.</param>
        /// <param name="commands">The commands.</param>
        /// <returns>The command instance or <c>null</c>.</returns>
        private static ICommand GetCommand(CommandLine commandLine, List<ICommand> commands)
        {
            // Sort the commands in decending order by number of words in the
            // command (we want to match the longest sequence).

            foreach (var command in commands.OrderByDescending(c => c.Words.Length))
            {
                if (command.Words.Length > commandLine.Arguments.Length)
                {
                    // Not enough arguments to match the command.

                    continue;
                }

                var matches = true;

                for (int i = 0; i < command.Words.Length; i++)
                {
                    if (!string.Equals(command.Words[i], commandLine.Arguments[i], StringComparison.OrdinalIgnoreCase))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return command;
                }
            }

            // No match.

            return null;
        }

        /// <summary>
        /// Exits the program returning the specified process exit code.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        public static void Exit(int exitCode)
        {
            if (NeonClusterHelper.IsConnected)
            {
                NeonClusterHelper.DisconnectCluster();
            }

            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Returns the <see cref="CommandLine"/>.
        /// </summary>
        public static CommandLine CommandLine { get; private set; }

        /// <summary>
        /// Returns the command line as a string with sensitive information like a password
        /// obscured.  This is suitable for using as a <see cref="SetupController"/>'s
        /// operation summary.
        /// </summary>
        public static string SafeCommandLine
        {
            get
            {
                // Obscure the [-p=xxxx] and [--password=xxxx] options.

                var sb = new StringBuilder();

                foreach (var item in CommandLine.Items)
                {
                    if (item.StartsWith("-p="))
                    {
                        sb.AppendWithSeparator("-p=[...]");
                    }
                    else if (item.StartsWith("--password="))
                    {
                        sb.AppendWithSeparator("--password=[...]");
                    }
                    else
                    {
                        sb.AppendWithSeparator(item);
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the node operating system specific information.
        /// </summary>
        public static DockerOSProperties OSProperties { get; private set; }

        /// <summary>
        /// Returns the folder where <b>neon.exe</b> persists local state.  This
        /// folder and all subfolders are encrypted whwn supported by the current
        /// operating system.
        /// </summary>
        public static string ClusterRootFolder { get; private set; }

        /// <summary>
        /// Returns the folder where <b>neon.exe</b> persists sensitive cluster secrets.
        /// </summary>
        public static string ClusterSecretsFolder { get; private set; }

        /// <summary>
        /// Returns the path to the file where the name of the current cluster is saved.
        /// </summary>
        public static string CurrentClusterPath { get; private set; }

        /// <summary>
        /// Returns the path to the (hopefully) encrypted temporary folder.
        /// </summary>
        public static string ClusterTempFolder { get; private set; }

        /// <summary>
        /// Returns the path to the credentials for the named cluster.
        /// </summary>
        /// <param name="clusterName">The cluster name.</param>
        /// <returns>The path to the cluster's credentials file.</returns>
        public static string GetClusterSecretsPath(string clusterName)
        {
            return Path.Combine(ClusterSecretsFolder, $"{clusterName}.json");
        }

        /// <summary>
        /// Uses <see cref="NeonClusterHelper.ConnectCluster(DebugSecrets, string)"/> to 
        /// establish an emulated connection to the logged-in cluster.
        /// </summary>
        public static void ConnectCluster()
        {
            if (Program.ClusterSecrets == null)
            {
                Console.Error.WriteLine(Program.MustLoginMessage);
                Program.Exit(1);
            }

            NeonClusterHelper.ConnectCluster(clusterName: Program.ClusterSecrets.Name);
        }

        /// <summary>
        /// Returns the cluster's SSH user name.
        /// </summary>
        public static string UserName { get; private set; }

        /// <summary>
        /// Returns the cluster's SSH user password.
        /// </summary>
        public static string Password { get; private set; }

        /// <summary>
        /// Returns the credentials information for the currently logged in cluster or <c>null</c>.
        /// </summary>
        public static ClusterSecrets ClusterSecrets { get; private set; }

        /// <summary>
        /// Returns the log folder path or <c>null</c> for quiet mode.
        /// </summary>
        public static string LogPath { get; private set; }

        /// <summary>
        /// The maximum number of nodes to be configured in parallel.
        /// </summary>
        public static int MaxParallel { get; set; }

        /// <summary>
        /// The seconds to wait for cluster stablization.
        /// </summary>
        public static double WaitSeconds { get; set; }

        /// <summary>
        /// Indicates whether node logging and console output is to be suppressed.
        /// </summary>
        public static bool Quiet { get; private set; }

        /// <summary>
        /// Creates a <see cref="NodeProxy{TMetadata}"/> for the specified host and server name,
        /// configuring logging and the credentials as specified by the global command
        /// line options.
        /// </summary>
        /// <param name="host">The host IP address or FQDN.</param>
        /// <param name="name">The optional host name (defaults to <paramref name="host"/>).</param>
        /// <typeparam name="TMetadata">Defines the metadata type the command wishes to associate with the sewrver.</typeparam>
        /// <returns>The <see cref="NodeProxy{TMetadata}"/>.</returns>
        public static NodeProxy<TMetadata> CreateNodeProxy<TMetadata>(string host, string name = null)
            where TMetadata : class
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(host));

            name = name ?? host;

            var logWriter = (TextWriter)null;

            if (LogPath != null)
            {
                logWriter = new StreamWriter(Path.Combine(LogPath, name + ".log"));
            }

            SshCredentials sshCredentials;

            if (!string.IsNullOrEmpty(Program.UserName) && !string.IsNullOrEmpty(Program.Password))
            {
                sshCredentials = SshCredentials.FromUserPassword(Program.UserName, Program.Password);
            }
            else if (Program.ClusterSecrets != null)
            {
                sshCredentials = Program.ClusterSecrets.GetSshCredentials();
            }
            else
            {
                Console.WriteLine("*** ERROR: Expected some node credentials.");
                Program.Exit(1);

                return null;
            }

            var proxy = new NodeProxy<TMetadata>(name, host, sshCredentials, logWriter);

            proxy.RemotePath += $":{NodeHostFolders.Setup}";
            proxy.RemotePath += $":{NodeHostFolders.Tools}";

            return proxy;
        }

        /// <summary>
        /// Returns the folder holding the Linux resource files for the target operating system.
        /// </summary>
        public static ResourceFiles.Folder LinuxFolder
        {
            get
            {
                switch (Program.OSProperties.TargetOS)
                {
                    case TargetOS.Ubuntu_16_04:

                        return ResourceFiles.Linux.GetFolder("Ubuntu-16.04");

                    default:

                        throw new NotImplementedException($"Unexpected [{Program.OSProperties.TargetOS}] target operating system.");
                }
            }
        }

        /// <summary>
        /// Identifies the service manager present on the target Linux distribution.
        /// </summary>
        public static ServiceManager ServiceManager
        {
            get
            {
                switch (Program.OSProperties.TargetOS)
                {
                    case TargetOS.Ubuntu_16_04:

                        return ServiceManager.Systemd;

                    default:

                        throw new NotImplementedException($"Unexpected [{Program.OSProperties.TargetOS}] target operating system.");
                }
            }
        }

        /// <summary>
        /// Returns the file path to the currently running executable.
        /// </summary>
        public static string PathToExecutable
        {
            get
            {
                // $hack(jeff.lill):
                //
                // This is a bit of hack.  There are two cases:
                //
                //      1. The tool is being run in the debugger, in which case
                //         we're not running the standalone executable that has
                //         all of the referenced assemblies merged in.  We can
                //         tell this if the file name begins with an underscore (_).
                //     
                //         In this case, we're going to reference the fully merged
                //         executable built in the post-build script and written
                //         to %NR_BUILD%\bin.
                //
                //      2. The tool is running from the fully merged executable.
                //         This will happen when running on the command line or
                //         on a Linux box.

                var path = Process.GetCurrentProcess().MainModule.FileName;

                if (Path.GetFileName(path).StartsWith("_"))
                {
                    path = Path.Combine(BuildEnvironment.BuildArtifactPath, "neon.exe");
                }

                return path;
            }
        }
    }
}

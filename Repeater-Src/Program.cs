/*
* C.Winters / US / Arizona / Thinkpad T15g 
*
* Purpose:
* Repeater is a tool that uses SSH comms to run tasks or automation on virtual or static machine nodes. 
* Repeater can be configured for individual or global credentials to log into a system. The configuration 
* is in JSON format and a dry-run test can be used to test out output.
* 
* My current usage is - I have two machines. Instead of remote desktop, I can call Repeater to display
* current information on the systems, send an event log to the server reporting I am no longer using it, 
* and shuts them both down after writing the event log. There are many other uses. 
*
* I realise Ansible is a product out there you can use. But, when I started writing this, I was aware, 
* but never used it. So, I wrote my own. I may revamp this to follow the "playbook" technique, 
* because the Global commands file isn't the best solution as "playbooks" are. 
* 
* This project is currently under development. See "APP_VERSION" string in this file for release version.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.winters.config;
using com.winters.sshconnhlp;
using com.winters.commandutils;
using Renci.SshNet;
using Renci.SshNet.Common;
using com.winters.securityhelper;
using Repeater;

namespace com.repeater.program
{
    class RepeaterPrg
    {

        public static string configFile = ".\\config.json";
        public static readonly string APP_VERSION = "alpha-1.2.6";

        //Set up ssh, scp, and ftp clients
        public static SSHConn sshconn = new SSHConn();

        // ------------ json stuff class
        #region JSON Config - USED
        // -------------------------------
        public static Configs oConfigs = new Configs();
        //-------------------------------
        #endregion JSON Config - USED


        enum OPERATIONS
        {
            Upload = 1010, 
            Download, 
            Reboot, 
            SysInfo, 
            linuxcommandfile, 
            macoscommandfile, 
            wincommandfile 
        };

        static void Main(string[] args)
        {

            System.Console.WriteLine("Repeater Version {0} - by C. Winters", APP_VERSION);

            //var taskKeys = new System.Threading.Tasks.Task(ReadKeys);
            //taskKeys.Start();
            //var tasks = new[] { taskKeys };
            //System.Threading.Tasks.Task.WaitAll(tasks);


            #region Parameters
            //Command line arguments. Supports example params like /param {choice}, -param {choice}, and --param {choice}
            string[] sPrgArgs = Environment.GetCommandLineArgs();

            // Dictionary to store parsed arguments
            Dictionary<string, string> dictArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int iIndex = 1; iIndex < sPrgArgs.Length; iIndex++)
            {
                string currentArg = sPrgArgs[iIndex];

                // Handle switches and parameters like -cmd, --cmd
                if (currentArg.StartsWith("/") || currentArg.StartsWith("-"))
                {
                    string key = currentArg.TrimStart('/', '-', '-');

                    // Check if the next argument is a value or another switch
                    string value = (iIndex + 1 < sPrgArgs.Length && !sPrgArgs[iIndex + 1].StartsWith("/") && !sPrgArgs[iIndex + 1].StartsWith("-"))
                                   ? sPrgArgs[++iIndex]
                                   : string.Empty;

                    if (!dictArgs.ContainsKey(key))
                        dictArgs.Add(key, value);
                }
                else if (currentArg.Contains("=")) // Handle sub-command arguments like "password=ThisIsMyPassword"
                {
                    string[] keyValue = currentArg.Split('=');

                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        if (!dictArgs.ContainsKey(key))
                            dictArgs.Add(key, value);
                    }
                }
                else // Handle cases where parameter value follows the key directly (e.g., password ThisIsMyPassword)
                {
                    string key = currentArg.Trim();

                    // Ensure we have a value after the key
                    if (iIndex + 1 < sPrgArgs.Length && !sPrgArgs[iIndex + 1].StartsWith("/") && !sPrgArgs[iIndex + 1].StartsWith("-"))
                    {
                        string value = sPrgArgs[++iIndex].Trim();
                        dictArgs.Add(key, value);
                    }
                }
            }

            //Override configuration file switch
            // /config "C:\somefile.ini" 
            if (dictArgs.ContainsKey("config"))
                //Override the config file
                configFile = dictArgs["config"];


            if (dictArgs.ContainsKey("version"))
            {
                System.Console.WriteLine("Repeater Version {0} - by C. Winters", APP_VERSION);
                Environment.Exit(0);
            }

            //configme switch
            //If you don't have a config file and want to start your own... 
            // example-config.json is created in same directory with a few samples.
            if (dictArgs.ContainsKey("configme"))
            {
                
                string GetcurPathExe = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string ConfigPath = GetcurPathExe + "\\example-config.json";

                if (GetcurPathExe.Contains("/"))
                {
                    //We're on unix type system, modify path
                    ConfigPath = "/" + ConfigPath.Replace("\\", "/");
                }

                oConfigs.ConfigFile = ConfigPath;

                oConfigs.oConfigOptions.AppConfig = new AppConfig();


                oConfigs.oConfigOptions.AppConfig.DryRun = "true";
                oConfigs.oConfigOptions.AppConfig.DefaultPort = "20";
                oConfigs.oConfigOptions.Servers = new List<Server>
                {
                    new Server()
                    {
                        Name = "Server2",

                        ID = "ServerID2",
                        IPAddress = "192.168.1.2",
                        Cmds = new List<string>
                        {
                            "@sysinfo",
                            "help"
                        },
                        Delay ="100",
                        Reboot = "false"
                    },
                    new Server()
                    {
                        Name = "Server3",
                        ID = "ServerID3",
                        IPAddress = "192.168.1.3",
                        Cmds = new List<string>
                        {
                            "@sysinfo",
                            "help"
                        },
                        Delay ="100",
                        Reboot = "false"
                    }
                };

                //Since we've built up our configuration _manually_, we m,ust set the config loaded flag to save.
                oConfigs.oConfigOptions.ConfigLoaded = true;    
                oConfigs.SaveConfig();
                System.Console.WriteLine("Example config saved: " + oConfigs.ConfigFile );
                Environment.Exit(0);
            }

            //nolog switch
            //If you want to disable the log file immediately.
            if (dictArgs.ContainsKey("nolog"))
            {
            }


            //Display config
            if (dictArgs.ContainsKey("showconfig"))
            {
                oConfigs.ConfigFile = configFile;
                oConfigs.ReadConfigJSON();
                oConfigs.DisplayConfig();
                return;
            }

            //Change the default user:password
            if (dictArgs.ContainsKey("defaultuserpassword"))
            {
                oConfigs.ConfigFile = configFile;
                oConfigs.ReadConfigJSON();

                string sDefaultUserPassword = string.Empty;

                if (dictArgs.TryGetValue("defaultuserpassword", out sDefaultUserPassword))
                {
                    oConfigs.oConfigOptions.AppConfig.DefaultUserPassword = sDefaultUserPassword;
                    oConfigs.SaveConfig();
                }
                return;
            }


            //cmd args: -cmd run server {ServerID}
            // Example: -cmd run server Lev001
            if (dictArgs.ContainsKey("cmd") && dictArgs.ContainsValue("run"))
            {
                try
                {
                    oConfigs.ConfigFile = configFile;
                    if (oConfigs.ReadConfigJSON() != 0)
                    {
                        System.Console.WriteLine("Invalid config file. Check for duplicate server IDs if the config file exist.");
                    }

                    string sDefaultUser = string.Empty;

                    string serverID = dictArgs.ContainsKey("server") ? dictArgs["server"].Trim() : string.Empty;
                    
                    if (string.IsNullOrWhiteSpace(serverID))
                    {
                        Console.WriteLine("Server ID is missing or invalid.");
                        return;
                    }

                    StartSvrWorkByID(serverID);

                }
                catch(Exception ex)
                {
                    System.Console.WriteLine("Command line parameters invalid.") ;
                }
                return;

            }


            //cmd args: -cmd attach server {ServerID}
            // Example: -cmd attach server Lev001
            if (dictArgs.ContainsKey("cmd") && dictArgs.ContainsValue("attach"))
            {
                try
                {
                    oConfigs.ConfigFile = configFile;
                    if (oConfigs.ReadConfigJSON() != 0)
                    {
                        System.Console.WriteLine("Well shit. Config is bad. Check for duplicate server IDs if the config file exist.");
                    }

                    string sDefaultUser = string.Empty;

                    string serverID = dictArgs["server"].Trim();

                    //StartSvrWorkByID(serverID);
                    AttachSvrByID(serverID);

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Command line parameters invalid.");
                }
                return;

            }



            //Override log file usage switch
            // /log c:\files_processed.log
            // If you want to use your own log file, use this switch.
            if (dictArgs.ContainsKey("log"))
            {
                //Override the log file
                //                STRING_LOGGING_FILE = dictArgs["log"];

            }






            //if (dictArgs.TryGetValue("cmd", out var command) && command.Equals("encrypt", StringComparison.OrdinalIgnoreCase))
            //{
            //    Console.WriteLine("Encrypt command detected!");

            //    if (dictArgs.TryGetValue("password", out var password))
            //    {
            //        Console.WriteLine($"Password: {password}");
            //    }

            //    if (dictArgs.TryGetValue("key", out var key))
            //    {
            //        Console.WriteLine($"Key: {key}");
            //    }
            //}








            //encrypt a password to use as update credentials
            // Two examples to use:
            // -cmd encrypt password=MyPassword key=123456789ABC
            // -cmd encrypt password ThisIsMyPassword Key 123456789AB
            
            //if ( dictArgs.ContainsKey("cmd") && 
            //     dictArgs.ContainsValue("encrypt") 
            //   )
            if (dictArgs.TryGetValue("cmd", out var command) && command.Equals("encrypt", StringComparison.OrdinalIgnoreCase))
            {
                
                if (! (dictArgs.TryGetValue("password", out string stringToEncrypt)) ||
                      !(dictArgs.TryGetValue("key", out string stringEncryptionKey))
                   )
                {
                    System.Console.WriteLine(StringTable.errInvalidCmdEncrypt);
                    return;
                }

                if ( string.IsNullOrEmpty(stringToEncrypt) ||
                     string.IsNullOrEmpty(stringEncryptionKey) 
                    )
                {
                    System.Console.WriteLine(StringTable.setupEncrypt);
                    stringToEncrypt = System.Console.ReadLine();

                    stringEncryptionKey = System.Console.ReadLine();
                    System.Console.WriteLine(StringTable.setupEnterEncryptionKey);
                }


                //Set up encryption key (password) and secure key (salt)
                com.winters.securityhelper.SecurityHelper.EncryptionKey = stringEncryptionKey;
                //com.winters.securityhelper.SecurityHelper.Salt = stringSalt;
                
                //Randomizer. 
                //SecurityHelper.EncryptionKey = SecurityHelper.GenerateRandomString(32);
                SecurityHelper.Salt = SecurityHelper.ConvertByteArrayToString(SecurityHelper.GenerateRandomBytes(32));

                //Initialize security validation
                if (com.winters.securityhelper.SecurityHelper.Init())
                {

                    //Encapsulation encrypt and decrypt
                    string encryptedText = com.winters.securityhelper.SecurityHelper.Encrypt(stringToEncrypt);
                    string decryptedText = com.winters.securityhelper.SecurityHelper.Decrypt(encryptedText);

                    string uncombinedencryptionkey = SecurityHelper.GetEncryptionKey(SecurityHelper.GetCombinedKeySalt());
                    string decryptByCombinedKeySalt = SecurityHelper.DecryptByCombinedKeySalt(encryptedText, SecurityHelper.GetCombinedKeySalt());

                    Console.WriteLine();
                    Console.WriteLine("Store these value soemwhere safe!");
                    Console.WriteLine();
                    Console.WriteLine("Use ONLY the SECURE KEY to decrypt password (or any encrypted text)");
                    Console.WriteLine($"Password: {decryptedText}");
                    Console.WriteLine($"Secret Key: {SecurityHelper.EncryptionKey}");
                    Console.WriteLine($"Secure Key: {SecurityHelper.GetCombinedKeySalt()}");
                    Console.WriteLine();

                } //End of SecurityHelper.Init()
                else
                {
                    Console.WriteLine("Invalid salt! No null or whitespaces, invalid chars, or not multiples of 4 in length.");
                }


                return;
            }
            


            //... More configs here.... 

            #endregion Parameters


            if ( (dictArgs.ContainsKey("help")) || (dictArgs.ContainsKey("?")) )
            {
                System.Console.WriteLine("\nOptions:");
                System.Console.WriteLine("config [full path to config file] - load a config from a location");
                System.Console.WriteLine("showconfig  -show configuration loaded from config file");
                System.Console.WriteLine("defaultuser {username}  -sets the default username");
                System.Console.WriteLine("defaultpassword {password}   -sets the default password");
                System.Console.WriteLine("defaultuserpassword {username:password}  - sets the global default user and password");
                System.Console.WriteLine("encrypt -u [user] -p [password]  - encrypts user password to store in config file. *not implemented*");
                System.Console.WriteLine("cred -serverid [serverid] [credentials]  - pass in the encrypted credentials rather than from config file. *not implemented*");

                System.Console.WriteLine("cmd [verb] [noun] [option]");
                System.Console.WriteLine("Verbs: run");
                System.Console.WriteLine("Noun: server");
                System.Console.WriteLine("\nExample: Run a specific server, by ID");
                System.Console.WriteLine("-cmd run server {serverID}");
                return;
            }


            if (configFile.Length > 4)
                oConfigs.ConfigFile = configFile;

            if (oConfigs.ReadConfigJSON()!=0)
            {
                System.Console.WriteLine("Config is bad. Check for duplicate server IDs if the config file exist.");
            }

            StartSvrWorkByID();
        }


        private static void ReadKeys()
        {
            // Put this in MAIN code. You'll have a loop, but oh well.
            //Console.WriteLine("Press ESC to stop");
            //do
            //{
            //    while (!Console.KeyAvailable)
            //    {
            //        // Do something
            //    }
            //} while (Console.ReadKey(true).Key != ConsoleKey.Escape);


            ConsoleKeyInfo key = new ConsoleKeyInfo();

            while (!Console.KeyAvailable && key.Key != ConsoleKey.Escape)
            {

                key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        Console.WriteLine("UpArrow was pressed");
                        break;
                    case ConsoleKey.DownArrow:
                        Console.WriteLine("DownArrow was pressed");
                        break;

                    case ConsoleKey.RightArrow:
                        Console.WriteLine("RightArrow was pressed");
                        break;

                    case ConsoleKey.LeftArrow:
                        Console.WriteLine("LeftArrow was pressed");
                        break;

                    case ConsoleKey.Escape:
                        break;

                    //default:
                    //    if (Console.)
                    //    {
                    //        Console.WriteLine(key.KeyChar);
                    //    }
                    //    break;
                }
            }
        }


        /// <summary>
        /// Runs a command  -non async 
        /// NON-PROD: prototype function!
        /// </summary>
        /// <param name="sshClient"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        //private static String ExecuteCommand(SshClient sshClient, String command)
        //{
        //    if (!sshClient.IsConnected)
        //        sshClient.Connect();

        //    return sshClient.CreateCommand(command).Execute();
        //}


        /// <summary>
        /// Connect to a Windows, Linux, Mac server or node supporting SSH.
        /// </summary>
        /// <param name="sHost">IP of the machine to connect to</param>
        /// <param name="iPort">default port</param>
        /// <param name="sUser">user to log into from the user ledger</param>
        /// <param name="sPasswd">password credetnial of the user</param>
        /// <returns></returns>
        public static bool ConnectToServer(string sHost, int iPort, string sUser, string sPasswd)
        {
            //TODO: If connection refused, we shouldn't exit, but log and move on.
            if (sshconn.SShClient(sHost, iPort, sUser, sPasswd) == 1)
                //Environment.Exit(-1);
                return (false);

            sshconn.SCPClient();
            sshconn.FTPClient();
            return (true);
        }


        /// <summary>
        /// Scans commands for custom keywords and returns the operation of the found keyword
        /// </summary>
        /// <param name="sCmd">string of command passed as string</param>
        /// <returns></returns>
        private static int ProcessOperations (string sCmd)
        {
            //Return values of 1010 and on...
            if (sCmd.ToLower().StartsWith("@download"))
                return ((int)OPERATIONS.Download ); 

            if (sCmd.ToLower().StartsWith("@upload"))
                return (((int)OPERATIONS.Upload));

            if (sCmd.ToLower().Contains("@reboot"))
                return (((int)OPERATIONS.Reboot ));

            if (sCmd.ToLower().StartsWith("@sysinfo"))
                return (((int)OPERATIONS.SysInfo));

            if (sCmd.ToLower().StartsWith("@macoscommandfile"))
                return (((int)OPERATIONS.macoscommandfile));

            if (sCmd.ToLower().StartsWith("@linuxcommandfile"))
                return (((int)OPERATIONS.linuxcommandfile));

            if ( (sCmd.ToLower().StartsWith("@wincommandfile")) || (sCmd.ToLower().StartsWith("@windowscommandfile")) )
                return (((int)OPERATIONS.wincommandfile));

            return (-1);
        }
        
        
        /// <summary>
        /// Process custom/internal commands 
        /// </summary>
        /// <param name="sCmd">string of typical OS command similar to dir c:\test or ls -lisa</param>
        private static int ProcessCommands(string sCmd, out string sCmdFinal)
        {

            if (string.IsNullOrEmpty(sCmd))
            {
                sCmdFinal = null;
                return -1;
            }

            int operations = 0;
            operations = ProcessOperations(sCmd);


             sCmdFinal = string.Empty;
            string[] stringSeparators = new string[] { 
                                                        "@download ".ToLower(), 
                                                        "@upload ".ToLower(),
                                                        "@linuxcommandfile ".ToLower(),
                                                        "@macoscommandfile ".ToLower(),
                                                        "@wincommandfile ".ToLower(),
                                                        "@windowscommandfile ".ToLower()
                                                     };

            sCmd = sCmd.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).ElementAt(0);
            
            sCmdFinal = sCmd;
            return (operations);
        }

        private static string[] ExtractUserPassword(string UsrColonPasswd)
        {
            string[]? tmp;

            //tmp = UsrColonPasswd.Split(":", StringSplitOptions.RemoveEmptyEntries).ElementAt(1);
            tmp = UsrColonPasswd.Split(":", StringSplitOptions.RemoveEmptyEntries);
            return (tmp);

        }

        //This function will enable user to attach to a server and run commands until user disconnects.
        // The shell works, but we need to re-do PrcessCommands. This has all been tested but commands need to detect OS and all that
        // 1-7-2024 cw
        public static void AttachSvrByID(string serverID = "@all")
        {
            bool bComplete = false;

            if (string.IsNullOrEmpty(serverID))
            {
                System.Console.WriteLine("No servers to process.");
                return;
            }

            if (oConfigs.oConfigOptions.AppConfig == null)
            {
                System.Console.WriteLine("Application configuration is not right. Check AppConfig section. Run with --createconfig to start a new template.");
                return;
            }

            string[] UserCredential = null;
            bool bDryRun = false;

            if ((oConfigs.oConfigOptions.AppConfig.DryRun == "false") || (oConfigs.oConfigOptions.AppConfig.DryRun == ""))
                bDryRun = false;
            else
                bDryRun = true;

            //Load up the commands file into memory, if any.
            CmdUtils.listGlobalLinuxCommands = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
            CmdUtils.listGlobalWinCommands = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);

            //Set up ssh, scp, and ftp clients
            SSHConn sshconn = new SSHConn();


            //oConfigs
            System.Console.WriteLine("----------------------Repeater Version {0}---------------------", APP_VERSION);
            if (oConfigs.oConfigOptions.ConfigLoaded)
                System.Console.WriteLine("Config file : " + oConfigs.ConfigFile);
            else
            {
                System.Console.WriteLine("Config loaded : false");
                Environment.Exit(-1);
            }

            //if (bDryRun)
            //    System.Console.WriteLine("Dry Run : true");
            //else
            //    System.Console.WriteLine("Dry Run : false");

            //System.Console.WriteLine("GlobalLinuxCommandFile: {0}", oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
            //System.Console.WriteLine("GlobalWinCommandFile  : {0}", oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);

            //System.Console.WriteLine("Processing {0} servers...\n", oConfigs.oConfigOptions.Servers.Count);

            //If we used --cmd attache server [serverid], let's look it up and use that
            foreach (Server ServerItem in oConfigs.oConfigOptions.Servers)
            {
                if ((ServerItem.ID == serverID) || (serverID == "@all"))
                {

                    if ((ServerItem.Delay == null) || (ServerItem.Delay == null))
                        ServerItem.Delay = "0";

                    int doUseUserPasswordDefault = doesUseDefaultUserPassword(ServerItem.ID);
                    bool bUseDefaultUserPassword = false;


                    // doUseUserPasswordDefault
                    //0 - If they are both empty or null then use default
                    //1 - If either password or user contain anything, then return error reporting user or password is null
                    //-1 -If both contain anything, then skip
                    switch (doUseUserPasswordDefault)
                    {
                        case 0:
                            //System.Console.WriteLine("If user/password are both empty then use default user password\n");
                            bUseDefaultUserPassword = true;
                            UserCredential = ExtractUserPassword(oConfigs.oConfigOptions.AppConfig.DefaultUserPassword);
                            break;
                        case 1:
                            //System.Console.WriteLine("Both user/password are configured, don't use default\n");
                            bUseDefaultUserPassword = false;
                            break;
                        case -1:
                            //System.Console.WriteLine("If either contain anything and the other doesn't, then return error reporting user or password is null\n");
                            bUseDefaultUserPassword = true;
                            break;

                        default:
                            bUseDefaultUserPassword = true;
                            break;

                    }

                    //if ((!string.IsNullOrEmpty(ServerItem.NoRepeat)) && (ServerItem.NoRepeat.ToLower() == "true"))
                    //{
                    //    System.Console.WriteLine("\n****************************************");
                    //    System.Console.WriteLine("Skipping server {0} [{1}] due to NO REPEAT.", ServerItem.Name, ServerItem.ID);
                    //    System.Console.WriteLine("****************************************");
                    //    continue;
                    //}

                    System.Console.WriteLine("\n----------------------------------------------------------");
                    System.Console.WriteLine("Server ID  : {0}", ServerItem.ID);
                    System.Console.WriteLine("Server Name: {0}", ServerItem.Name);
                    System.Console.WriteLine("Server IP  : {0}", ServerItem.IPAddress);
                    if (ServerItem.Port != null)
                        System.Console.WriteLine("Server Port: {0}", ServerItem.Port);
                    else
                    {
                        if (oConfigs.oConfigOptions.AppConfig.DefaultPort != null)
                        {
                            ServerItem.Port = oConfigs.oConfigOptions.AppConfig.DefaultPort;
                            System.Console.WriteLine("Server Port: {0} *default", oConfigs.oConfigOptions.AppConfig.DefaultPort);
                        }
                        else
                        {
                            System.Console.WriteLine("*** COMMS ERROR! **** You need to set the server's POrt configuration or define DefaultPort under the AppConfig section.");
                            return;
                        }
                    }


                    if ((ServerItem.User == null) && (ServerItem.Password == null))
                    {
                        System.Console.WriteLine("User       : {0} *default", UserCredential[0]);
                        System.Console.WriteLine("Password   : {0} *default", UserCredential[1]);

                    }
                    else
                    {
                        System.Console.WriteLine("User       : {0}", ServerItem.User);
                        System.Console.WriteLine("Password   : {0}", ServerItem.Password);
                    }
                    //UserCredential

                    System.Console.WriteLine("Command delay: {0} seconds", ServerItem.Delay);
                    System.Console.WriteLine("Task repeat   : {0}", ServerItem.NoRepeat);
                    System.Console.WriteLine("Reboot after  : {0}", ServerItem.Reboot);
                    System.Console.WriteLine("Frequency     : {0}\n", ServerItem.Frequency);


                    System.Console.WriteLine("Attempting to connect to server: {0} [{1}]\n", ServerItem.Name, ServerItem.ID);

                    if (!bDryRun)
                    {
                        //Use global default user:password credential
                        if (bUseDefaultUserPassword)
                        {

                            if (!ConnectToServer(ServerItem.IPAddress, Int32.Parse(ServerItem.Port), UserCredential[0], UserCredential[1]))
                            {
                                System.Console.WriteLine("ERROR! Cannot connect to this server!\n");
                                continue;
                            }
                            else
                                System.Console.WriteLine("Connecting to {0}:{1} as {2} using default user and password", sshconn.GetHost(), sshconn.GetPort(), sshconn.GetUser());

                        }

                        else
                        {
                            //Use server user/password specifics
                            if (!ConnectToServer(ServerItem.IPAddress, Int32.Parse(ServerItem.Port), ServerItem.User, ServerItem.Password))
                            {
                                System.Console.WriteLine("ERROR! Cannot connect to this server!\n");
                                continue;
                            }
                            else
                                System.Console.WriteLine("Connecting to {0}:{1} as {2} using specific user and password.", sshconn.GetHost(), sshconn.GetPort(), sshconn.GetUser());
                        }


                    }


                    //BELOW doa  infinite while loop and process commds. Exit will get you out!
                    // break the functionality below getting out of loop foreach (Server ServerItem in oConfigs.oConfigOptions.Servers) since we've found our server.
                    //When user types "exit" then exit gracefully.



                    //////////////////////////////////
                    /////////////////////////////////
                    //////////////////////////////////
                    /////////////////////////////////

                    //////////////////////////////////
                    /////////////////////////////////
                    //////////////////////////////////
                    /////////////////////////////////

                    //This works, needs improvemtn and somehow able to inject custom commands.
                    //quitting for now. Getting late. 1-5-24 cw
                    //ShellStream shellStreamSSH = sshconn.CreateShell2();

                    SshClient cursshconn = sshconn.GetConn;
                    string CmdItemCmd = string.Empty;
                    string cmdResult = string.Empty;

                    ShellStream shellStreamSSH = cursshconn.CreateShellStream("vt-100", 80, 60, 800, 600, 65536);

                    //This works
                    //ShellStream shellStreamSSH = sshconn.CreateShell2();

                    StreamWriter writer = new StreamWriter(shellStreamSSH);

                    // Read from SSH. Subscribe event to write out the data.
                    shellStreamSSH.DataReceived += (object sender, ShellDataEventArgs e) =>
                    {
                        System.Console.Write(Encoding.UTF8.GetString(e.Data));
                        //Emulation. For the oldies, raise up the sleep to make the system appear like a 56K modem!
                        System.Threading.Thread.Sleep(50);
                    };

                    //do
                    //    {
                    //        CmdItemCmd = Console.ReadLine() ?? string.Empty;
                    //        //shellStreamSSH.Write(CmdItemCmd + "\n");

                    //        writer.AutoFlush = true;
                    //        writer.Write(CmdItemCmd  + "\n" );

                    //    } while (!CmdItemCmd.Equals("exit", StringComparison.OrdinalIgnoreCase));



                    //Is linux, windows or macos?
                    //if (sshconn.isWindows())
                    //{
                    //    ;
                    //}

                    //if (sshconn.isLinux())
                    //{
                    //    ;
                    //}

                    //if (sshconn.isMacOS())
                    //{
                    //    ;
                    //}





                    string CmdItem = string.Empty;
                    do
                    {
                        CmdItem = Console.ReadLine() ?? string.Empty;
                        //shellStreamSSH.Write(CmdItem + "\n");

                        writer.AutoFlush = true;
                        writer.Write(CmdItem + "\n");


                        ////in our commands, we must have come across empty line.
                        if (string.IsNullOrEmpty(CmdItem))
                            continue;

                        //if (!CmdItem.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        //{
                        //    //Prepare to exit after closing comms

                        //}

                        string sOut;
                        int rVal;

                        rVal = ProcessCommands(CmdItem, out sOut);

                        switch (rVal)
                        {
                            //case -1: //in our commands, we must have come across empty line.
                            //    System.Console.WriteLine("    *** NULL LINE ***");
                            //    break;

                            case (int)OPERATIONS.Download: //download a file
                                string[] sRemoteLocalFile = sOut.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                if (sRemoteLocalFile.Length > 2)
                                {
                                    System.Console.WriteLine("ERROR! Download config not right. Use @download [remote file] [local file]");
                                    break;
                                }

                                System.Console.WriteLine("\tDownloading: {0} {1}", sRemoteLocalFile[0], sRemoteLocalFile[1]);

                                if (!bDryRun)
                                {
                                    sshconn.Download(sRemoteLocalFile[0], sRemoteLocalFile[1]);
                                }
                                break;

                            case (int)OPERATIONS.Upload: //upload a file
                                string[] sLocalRemoteFile = sOut.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                if (sLocalRemoteFile.Length > 2)
                                {
                                    System.Console.WriteLine("ERROR! Upload config not right. Use @upload [local file] [remote file]");
                                    break;
                                }


                                System.Console.WriteLine("\tUploading: {0} {1}", sshconn.FilePathSepClean(sLocalRemoteFile[0]), sshconn.FilePathSepClean(sLocalRemoteFile[1]));

                                if (!bDryRun)
                                {
                                    try
                                    {
                                        sshconn.Upload(sLocalRemoteFile[0], sLocalRemoteFile[1]);
                                    }
                                    catch (Exception ex)
                                    {

                                    }

                                }

                                break;
                            case (int)OPERATIONS.Reboot: //reboot
                                List<string> cmdListReboot = new List<string>();
                                System.Console.WriteLine("    *** REBOOTING ***");

                                cmdListReboot = CmdUtils.Reboot();
                                if (cmdListReboot.Count > 0)
                                {
                                    foreach (string cmd in cmdListReboot)
                                    {
                                        if (!bDryRun)
                                        {
                                            Dictionary<int, string> retvals = new Dictionary<int, string>(sshconn.ExecuteCommandEx(cmd));
                                            string Result = retvals.First().Value;
                                            System.Console.WriteLine("\tCMD (internal) {0}{1}", cmd, Result);
                                        }
                                        else
                                        {
                                            System.Console.WriteLine("\tCMD (internal) {0}", cmd);
                                        }
                                    }
                                }
                                break;

                            case (int)OPERATIONS.SysInfo: //sysinfo
                                List<string> cmdList = new List<string>();
                                System.Console.WriteLine("    *** SYSINFO ***");
                                cmdList = CmdUtils.SystemInfo();

                                if (cmdList.Count > 0)
                                {
                                    foreach (string cmd in cmdList)
                                    {
                                        if (!bDryRun)
                                        {
                                            Dictionary<int, string> retvals = new Dictionary<int, string>(sshconn.ExecuteCommandEx(cmd));
                                            string Result = retvals.First().Value;
                                            System.Console.WriteLine("\tCMD (internal) {0}{1}", cmd, Result);
                                        }
                                        else
                                        {
                                            System.Console.WriteLine("\tCMD (internal) {0}", cmd);
                                        }
                                    }
                                }


                                break;
                        }

                        //Custom commands huh? then continue on...
                        if (rVal >= 1010)
                            continue;
                        else
                        {
                            System.Console.WriteLine("\tCMD -> {0}", CmdItem);
                            //This needs to be put into encapsulation. sshconn exposed.
                            if (!bDryRun)
                            {
                                //var sRetVal = sshconn.ExecuteCmd(CmdItem);
                                //Disabled above command because we're already running commands via CreateShell

                                //Dictionary<int, string> retvals = new Dictionary<int, string>(sshconn.ExecuteCommandEx(CmdItem));
                                //int ExitStatus = retvals.First().Key;
                                //string Result = retvals.First().Value;
                                //System.Console.WriteLine($"        Return Code: {ExitStatus} Result:\n{Result}");
                                //System.Console.WriteLine($"        Result:\n{sRetVal}");

                            }
                        }



                        //Delay 
                        if ((ServerItem.Delay != string.Empty) || (ServerItem.Delay != null))
                        {
                            if (ServerItem.Delay != "0")
                            {
                                System.Console.WriteLine("  Delaying in seconds: {0}", ServerItem.Delay);
                                System.Threading.Thread.Sleep(Int32.Parse(ServerItem.Delay) * 1000);
                            }
                        }



                    } while (!CmdItem.Equals("exit", StringComparison.OrdinalIgnoreCase));


                    //if (ServerItem.Reboot != null)
                    //{
                    //    if (ServerItem.Reboot.ToLower() == "true")
                    //        System.Console.WriteLine("Rebooting Server ID  : {0} [{1}]\n", ServerItem.ID, ServerItem.Name);
                    //}


                    
                    shellStreamSSH.Close();


                } //End if server id check

            } //End of while not bComplete loop...


            if (sshconn.IsAllConnected)
                sshconn.Disconnect();

        }


        //COMPLETE
        //This function is the main work horse to process through servers and enable automation from the config files.
        public static void StartSvrWorkByID(string serverID="@all")
        {

            //First thing, first: are therer any servers to process?
            if (serverID == "")
            {
                System.Console.WriteLine("No servers to process.");
                return;
            }

            if (oConfigs.oConfigOptions.AppConfig==null)
            {
                System.Console.WriteLine("Application configuration is not right. Check AppConfig section. Run with --createconfig to start a new template.");
                return;
            }

            string[] UserCredential = null;
            bool bDryRun = false;

            if ( (oConfigs.oConfigOptions.AppConfig.DryRun == "false") || (oConfigs.oConfigOptions.AppConfig.DryRun == "") )
                bDryRun = false; 
            else
                bDryRun = true;

            //Load up the commands file into memory, if any.
            CmdUtils.listGlobalLinuxCommands = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
            CmdUtils.listGlobalWinCommands = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);

            //Set up ssh, scp, and ftp clients
            SSHConn sshconn = new SSHConn();


            //oConfigs
            System.Console.WriteLine("----------------------Repeater Version {0}---------------------", APP_VERSION);
            if (oConfigs.oConfigOptions.ConfigLoaded)
                System.Console.WriteLine("Config file : " + oConfigs.ConfigFile);
            else
            {
                System.Console.WriteLine("Config loaded : false");
                Environment.Exit(-1);
            }

            if (bDryRun)
                System.Console.WriteLine("Dry Run : true");
            else
                System.Console.WriteLine("Dry Run : false");

            System.Console.WriteLine("GlobalLinuxCommandFile: {0}", oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
            System.Console.WriteLine("GlobalWinCommandFile  : {0}", oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);

            System.Console.WriteLine("Processing {0} servers...\n", oConfigs.oConfigOptions.Servers.Count);

            foreach (Server ServerItem in oConfigs.oConfigOptions.Servers)
            {
                if ( (ServerItem.ID == serverID) || (serverID == "@all") )
                {

                    if ((ServerItem.Delay == null) || (ServerItem.Delay == null))
                        ServerItem.Delay = "0";

                    int doUseUserPasswordDefault = doesUseDefaultUserPassword(ServerItem.ID);
                    bool bUseDefaultUserPassword  =false;


                    //if (!bDryRun)
                    //{
                    //    //Connect!
                    //    //TODO: If connection refused, we shouldn't exit, but log and move on.
                    //    //TODO look into why this is here when later ConnectToServer() is used.... SEE BELOW                        
                    //    if (sshconn.SShClient(ServerItem.IPAddress, Int32.Parse(ServerItem.Port), ServerItem.User, ServerItem.Password) == 1)
                    //        Environment.Exit(-1);

                    //    sshconn.SCPClient();
                    //    sshconn.FTPClient();
                    //}


                    // doUseUserPasswordDefault
                    //0 - If they are both empty or null then use default
                    //1 - If either password or user contain anything, then return error reporting user or password is null
                    //-1 -If both contain anything, then skip
                    switch (doUseUserPasswordDefault)
                    {
                        case 0:
                            //System.Console.WriteLine("If user/password are both empty then use default user password\n");
                            bUseDefaultUserPassword = true;
                            UserCredential = ExtractUserPassword(oConfigs.oConfigOptions.AppConfig.DefaultUserPassword);
                            break;
                        case 1:
                            //System.Console.WriteLine("Both user/password are configured, don't use default\n");
                            bUseDefaultUserPassword = false;
                            break;
                        case -1:
                            //System.Console.WriteLine("If either contain anything and the other doesn't, then return error reporting user or password is null\n");
                            bUseDefaultUserPassword = true;
                            break;

                        default:
                            bUseDefaultUserPassword = true;
                            break;

                    }

                    if ( (!string.IsNullOrEmpty(ServerItem.NoRepeat)) && (ServerItem.NoRepeat.ToLower() == "true") )
                    {
                        System.Console.WriteLine("\n****************************************");
                        System.Console.WriteLine("Skipping server {0} [{1}] due to NO REPEAT.", ServerItem.Name, ServerItem.ID);
                        System.Console.WriteLine("****************************************");
                        continue;
                    }

                    System.Console.WriteLine("\n----------------------------------------------------------");
                    System.Console.WriteLine("Server ID  : {0}", ServerItem.ID);
                    System.Console.WriteLine("Server Name: {0}", ServerItem.Name);
                    System.Console.WriteLine("Server IP  : {0}", ServerItem.IPAddress);
                    if (ServerItem.Port!=null)
                        System.Console.WriteLine("Server Port: {0}", ServerItem.Port);
                    else
                    {
                        if (oConfigs.oConfigOptions.AppConfig.DefaultPort!=null)
                        {
                            ServerItem.Port = oConfigs.oConfigOptions.AppConfig.DefaultPort;
                            System.Console.WriteLine("Server Port: {0} *default", oConfigs.oConfigOptions.AppConfig.DefaultPort);
                        }
                        else
                        {
                            System.Console.WriteLine("*** COMMS ERROR! **** You need to set the server's POrt configuration or define DefaultPort under the AppConfig section.");
                            return;
                        }
                    }


                    if ( (ServerItem.User ==null) && (ServerItem.Password==null) )
                    {
                        System.Console.WriteLine("User       : {0} *default", UserCredential[0]);
                        System.Console.WriteLine("Password   : {0} *default", UserCredential[1]);

                    }
                    else
                    {
                        System.Console.WriteLine("User       : {0}", ServerItem.User);
                        System.Console.WriteLine("Password   : {0}", ServerItem.Password);
                    }
                    //UserCredential

                    System.Console.WriteLine("Command delay: {0} seconds", ServerItem.Delay);
                    System.Console.WriteLine("Task repeat   : {0}", ServerItem.NoRepeat);
                    System.Console.WriteLine("Reboot after  : {0}", ServerItem.Reboot);
                    System.Console.WriteLine("Frequency     : {0}\n", ServerItem.Frequency);


                    System.Console.WriteLine("Attempting to connect to server: {0} [{1}]\n", ServerItem.Name, ServerItem.ID);

                    if (!bDryRun)
                    {
                        //Use global default user:password credential
                        if (bUseDefaultUserPassword)
                        {

                            if ( ! ConnectToServer(ServerItem.IPAddress, Int32.Parse(ServerItem.Port), UserCredential[0], UserCredential[1]) )
                            {
                                System.Console.WriteLine("ERROR! Cannot connect to this server!\n");
                                continue;
                            }
                            else
                                System.Console.WriteLine("Connecting to {0}:{1} as {2} using default user and password", sshconn.GetHost(), sshconn.GetPort(), sshconn.GetUser());

                        }
                            
                        else
                        {
                            //Use server user/password specifics
                            if (!ConnectToServer(ServerItem.IPAddress, Int32.Parse(ServerItem.Port), ServerItem.User, ServerItem.Password) )
                            {
                                System.Console.WriteLine("ERROR! Cannot connect to this server!\n");
                                continue;
                            }
                            else
                                System.Console.WriteLine("Connecting to {0}:{1} as {2} using specific user and password.", sshconn.GetHost(), sshconn.GetPort(), sshconn.GetUser());
                        }


                    }


                    //Are we using a global command list file?
                    // Here. On the current server, is global command file is used, then wipe out commands (if any) and replace with ones in:
                    //CmdUtils.listGlobalLinuxCommands = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
                    //CmdUtils.listGlobalWinCommands = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);
                    //That being said, we can remove the variable CmdUtils.listGlobalLinuxCommands and CmdUtils.listGlobalWinCommands
                    //But... how do we detect OS? ok ... working on this. We need to detect OS because we need to know WHICH OS commands file to use.
                    // See isWindows() in ssh-connhelp.cs
                    
                    //If commands are NULL, then we must check the global file
                    //TODO we can override the OS detection, but we need to figure out where the OS belongs to.
                    if (ServerItem.Cmds==null)
                    {
                        //Commands are null or 0. So, this will default to global commands. 

                        //Is linux, windows or macos?
                        if (sshconn.isWindows())
                        {
                            //Are there any global commands to process? If not, then just continue on..
                            //if windows ssh conn client is detected, check global commands. Are they valid?
                            ServerItem.Cmds = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);
                        }

                        if (sshconn.isLinux())
                        {
                            //Are there any global commands to process? If not, then just continue on..
                            //if windows ssh conn client is detected, check global commands. Are they valid?
                            ServerItem.Cmds = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile );
                        }

                        if (sshconn.isMacOS())
                        {
                            //Are there any global commands to process? If not, then just continue on..
                            //if windows ssh conn client is detected, check global commands. Are they valid?
                            ServerItem.Cmds = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
                        }


                        if (bDryRun)
                        {
                            //Well, we have to load up all the command lines in both windows/linux/mac because we don't know the OS.
                            if (oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile !=null)
                                ServerItem.Cmds = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile);

                            //ServerItem.Cmds = CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile);
                            //List<string> tmpCmds = new List<string>(CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalWinCommandFile));
                            //List<string> tmpCmds2 = new List<string>(CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile));
                            //                            ServerItem.Cmds.Add("--WINDOWS COMMANDS---");
                            //ServerItem.Cmds.AddRange(tmpCmds);
                            //                            ServerItem.Cmds.Add("--LINUX/MAC COMMANDS---");
                            //ServerItem.Cmds.AddRange(tmpCmds2);

                            List<string> tmpCmds2 = new List<string>();

                            if (oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile != null)
                            {
                                tmpCmds2.AddRange(CmdUtils.LoadGlobalCommandFile(oConfigs.oConfigOptions.AppConfig.GlobalLinuxCommandFile));
                            }
                        }

                        if (ServerItem.Cmds == null)
                        {
                            //Nope. Commands are not valid.
                            System.Console.WriteLine("No commands to process on this server. What a wasted connection! ^_^ ");
                            continue;
                        }


                    }

                    System.Console.WriteLine("  Processing {0} commands...\n", ServerItem.Cmds.Count);


                    //Process comnmands and delays
                    foreach (string CmdItem in ServerItem.Cmds)
                    {
                        //System.Console.WriteLine("  CMD: {0}", CmdItem);

                        ////in our commands, we must have come across empty line.
                        if (string.IsNullOrEmpty(CmdItem))
                            continue;

                        string sOut;
                        int rVal;

                        rVal = ProcessCommands(CmdItem, out sOut);

                        switch (rVal)
                        {
                            //case -1: //in our commands, we must have come across empty line.
                            //    System.Console.WriteLine("    *** NULL LINE ***");
                            //    break;

                            case (int)OPERATIONS.Download: //download a file
                                string[] sRemoteLocalFile = sOut.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                if (sRemoteLocalFile.Length > 2)
                                {
                                    System.Console.WriteLine("ERROR! Download config not right. Use @download [remote file] [local file]");
                                    break;
                                }

                                System.Console.WriteLine("\tDownloading: {0} {1}", sRemoteLocalFile[0], sRemoteLocalFile[1]);

                                if (!bDryRun)
                                {
                                    sshconn.Download(sRemoteLocalFile[0], sRemoteLocalFile[1]);
                                }
                                break;

                            case (int)OPERATIONS.Upload: //upload a file
                                string[] sLocalRemoteFile = sOut.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                if (sLocalRemoteFile.Length > 2)
                                {
                                    System.Console.WriteLine("ERROR! Upload config not right. Use @upload [local file] [remote file]");
                                    break;
                                }
                                

                                System.Console.WriteLine("\tUploading: {0} {1}", sshconn.FilePathSepClean(sLocalRemoteFile[0]), sshconn.FilePathSepClean(sLocalRemoteFile[1]));

                                if (!bDryRun)
                                {
                                    try
                                    {
                                        sshconn.Upload(sLocalRemoteFile[0], sLocalRemoteFile[1]);
                                    }
                                    catch (Exception ex)
                                    {
                                        
                                    }

                                }

                                break;
                            case (int)OPERATIONS.Reboot: //reboot
                                List<string> cmdListReboot = new List<string>();
                                System.Console.WriteLine("    *** REBOOTING ***");

                                cmdListReboot = CmdUtils.Reboot();
                                if (cmdListReboot.Count > 0)
                                {
                                    foreach (string cmd in cmdListReboot)
                                    {
                                        if (!bDryRun)
                                        {
                                            Dictionary<int, string> retvals = new Dictionary<int, string>(sshconn.ExecuteCommandEx(cmd));
                                            string Result = retvals.First().Value;
                                            System.Console.WriteLine("\tCMD (internal) {0}{1}", cmd, Result);
                                        }
                                        else
                                        {
                                            System.Console.WriteLine("\tCMD (internal) {0}", cmd);
                                        }
                                    }
                                }
                                break;

                            case (int)OPERATIONS.SysInfo: //sysinfo
                                List<string> cmdList = new List<string>(); 
                                System.Console.WriteLine("    *** SYSINFO ***");
                                cmdList = CmdUtils.SystemInfo();

                                if (cmdList.Count > 0)
                                {
                                    foreach(string cmd in cmdList)
                                    {
                                        if (!bDryRun)
                                        {
                                            Dictionary<int, string> retvals = new Dictionary<int, string>(sshconn.ExecuteCommandEx(cmd));
                                            string Result = retvals.First().Value;
                                            System.Console.WriteLine("\tCMD (internal) {0}{1}", cmd, Result);
                                        }
                                        else
                                        {
                                            System.Console.WriteLine("\tCMD (internal) {0}", cmd);
                                        }
                                    }
                                }


                                break;
                        }

                        //Custom commands huh? then continue on...
                        if (rVal >= 1010)
                            continue;
                        else
                        {
                            System.Console.WriteLine("\tCMD -> {0}", CmdItem);
                            //This needs to be put into encapsulation. sshconn exposed.
                            if (!bDryRun)
                            {
                                Dictionary<int, string> retvals = new Dictionary<int, string>(sshconn.ExecuteCommandEx(CmdItem));
                                int ExitStatus = retvals.First().Key;
                                string Result = retvals.First().Value;
                                System.Console.WriteLine($"        Return Code: {ExitStatus} Result:\n{Result}");

                            }
                        }



                        //Delay 
                        if ((ServerItem.Delay != string.Empty) || (ServerItem.Delay != null))
                        {
                            if (ServerItem.Delay != "0")
                            {
                                System.Console.WriteLine("  Delaying in seconds: {0}", ServerItem.Delay);
                                System.Threading.Thread.Sleep(Int32.Parse(ServerItem.Delay) * 1000);
                            }
                        }
                    }

                    if (ServerItem.Reboot!=null)
                    {
                        if (ServerItem.Reboot.ToLower() == "true")
                            System.Console.WriteLine("Rebooting Server ID  : {0} [{1}]\n", ServerItem.ID, ServerItem.Name);
                    }


                } //End if server id check


                //System.Console.WriteLine("----------------------------------------------------------");
            } //End of server loop

            if (sshconn.IsAllConnected)
                sshconn.Disconnect();   

        }        //End of StartSvrWorkByID()


        /// <summary>
        /// Determine if the default user and password will be used instead of user/password in server config
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns>0 - If user/password are both empty then use default
        /// 1 -  If both user/password are configured, don't use default
        /// -1  - If either contain anything and the other doesn't, then return error reporting user or password is null
        /// </returns>
        public static int doesUseDefaultUserPassword(string serverID)
        {
            foreach (Server ServerItem in oConfigs.oConfigOptions.Servers)
            {
                //Did we find server of interset?
                if (ServerItem.ID == serverID)
                {
                    //scan user and password. 
                    //0 - If they are both empty or null then use default
                    //1 - If both user/password are configured, don't use default
                    //-1 -If either contain anything and the other doesn't, then return error reporting user or password is null
                    //
                    if ( (ServerItem.User ==null) && (ServerItem.Password == null) )
                        //0
                        return (0);

                    if ((ServerItem.User == string.Empty) && (ServerItem.Password == string.Empty))
                        //0
                        return (0);

                    if ((ServerItem.User != string.Empty) || (ServerItem.Password != string.Empty))
                        //1
                        return (1);

                    if ((ServerItem.User == null) || (ServerItem.Password == null))
                        //-1
                        return (-1);
                }
            }
            return (-1);
        }

    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace com.winters.config
{
    //-------------------------------

    /// <summary>
    /// DefaultUser - Username used globally. *REMOVE*
    /// DefaultPassword - Password for user used globally. *REMOVE*
    /// DefaultUserPassword - Combination of username:password, used globally. 
    /// DefaultPOrt - default port to use in SSH connections.
    /// DryRun - Run without connection but show what connections would look like.
    /// </summary>
    public class AppConfig
    {
        public string DefaultUser { get; set; }
        public string DefaultPassword { get; set; }
        public string DefaultUserPassword { get; set; }

        public string DefaultPort { get; set; }

        public string DryRun { get; set; }
        
        public string GlobalLinuxCommandFile { get; set; }  
        public string GlobalWinCommandFile { get; set; }
    }

    /// <summary>
    /// Name - The name of the server
    /// ID - give an ID to the server. If no ID is passed, one will be created. (TODO)
    /// IPAddress - IP Address of server to comm with
    /// User - If global user is not used, then use this user to login
    /// Password - If global password is not userd, then use password for login
    /// Cmds - a list of command to run on the server in a session.
    /// Delay - amount of delay between commands, in seconds.
    /// NoRepeat - Do not repeat these commands again. Remove this configuration after its run.
    /// Reboot - reboot after session ends.
    /// Frequency - run a session by minute, hourly, weekly, or monthly. If application has exited, then the time will reflect current day as a reset counter.
    /// </summary>
    public class Server
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string IPAddress { get; set; }
        public string Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public List<string> Cmds { get; set; }
        public string Delay { get; set; }
        public string NoRepeat { get; set; }
        public string Reboot { get; set; }
        public string Frequency { get; set; }
    }

    /*
        RootConfig is the root of the configuration tree.
        It contains the application and server configuration.
        See example configuration below to follow rules of the RootConfig
        class.
        When adding more members, add them to the class and reference them in
        the source code (for example DisplayConfig())

    Config format:

            {
              "AppConfig": {
                "DefaultUserPassword": "SureBot:ThePassw0rd",
                "DryRun": "true"
              },
              "Servers": [
                {
                  "Name": "Server001",
                  "ID": "001",
                  "IPAddress": "10.23.2.204",
                  "Port": 22,
                  "Cmds": [
                    "@sysinfo"
                  ],
                  "NoRepeat": "false",
                  "Reboot": "false",
                  "Frequency": "Monthly"
                },
                {
                  "Name": "HomesoftServ",
                  "ID": "Lev001",
                  "IPAddress": "test.com",
                  "Port": 22,
                  "User": "user@user.com",
                  "Password": "aNewPassWordT3st",
                  "Cmds": [
                    "dir",
                    "@reboot",
                    "dir /w /p"
                  ],
                  "Delay": "1",
                  "NoRepeat": "false",
                  "Reboot": "false"
                }

              ]
            }
     */

    public class RootConfig
    {
        public AppConfig AppConfig { get; set; }
        public List<Server> Servers { get; set; }

        //Detects if config is loaded. 
        public bool ConfigLoaded = false;
        public string configFile = string.Empty;
    }

    public class Configs
    {
        // -------------------------------
        public RootConfig oConfigOptions = new RootConfig();

        //TODO - if this is cross-platform build, we need to adjust directory paths marks.
        public Configs(string configFile = ".\\config.json")
        {
            if (configFile.Length == 0)
                configFile = ".\\config.json";

            ConfigFile = configFile;
        }

        /// <summary>
        /// Sets the config file to use
        /// </summary>
        public string ConfigFile
        {
            get => oConfigOptions.configFile;
            set { oConfigOptions.configFile = value; }
        }


        /// <summary>
        /// Load in a JSON configuration file
        /// </summary>
        /// <param name="filepath">Path to the configuration file</param>
        //public void ReadConfigJSON(string filepath)
        public int ReadConfigJSON()
        {
            //This really helped to cut time: https://json2csharp.com/

            if (!System.IO.File.Exists(ConfigFile))
            {
                Console.WriteLine("Configuration file " + ConfigFile + " does not exist.");
                return (-1);
            }

            //The config file gets "reset" because the object is returned. 
            string tmp = ConfigFile;

            try
            {
                oConfigOptions = JsonConvert.DeserializeObject<RootConfig>(File.ReadAllText(ConfigFile));

                if (oConfigOptions.Servers.Count >= 1)
                    oConfigOptions.ConfigLoaded = true;
                else
                    oConfigOptions.ConfigLoaded = false;

                //Create hash set to make servers unique
                HashSet<string> hsUniqueServerID = new HashSet<string>();
                foreach (Server ServerItem in oConfigOptions.Servers)
                {
                    //Find duplicate server ID. If we found one then return false
                    //after unloading config file.
                    if (!hsUniqueServerID.Add(ServerItem.ID))
                    {
                        oConfigOptions.ConfigLoaded = false;
                        //oConfigOptions = null;
                        return (1);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                Console.WriteLine("Error! Invalid character or formatting in config path {0}, line {1}", ex.Path, ex.LineNumber-1);
            }


            ConfigFile = tmp;
            return (0);
        } //End of ReadConfigJSON()


        /// <summary>
        /// Saves configuration in memory
        /// </summary>
        public void SaveConfig()
        {
            if (oConfigOptions.ConfigLoaded)
                File.WriteAllText(oConfigOptions.configFile, JsonConvert.SerializeObject(oConfigOptions, Formatting.Indented));
            else
                System.Console.WriteLine("Sorry, configuration is not loaded. Cannot save configuration at this time.");
        }


        /// <summary>
        /// Display the configuration currently loaded in memory, if loaded.
        /// </summary>
        public void DisplayConfig()
        {
            System.Console.WriteLine("----------------------Repeater Config---------------------");
            System.Console.WriteLine("[[App Config]]");
            if (oConfigOptions.ConfigLoaded)
            {
                System.Console.WriteLine("Config file           : " + ConfigFile);
                System.Console.WriteLine("Config loaded         : true");
            }
            else
            {
                System.Console.WriteLine("Config loaded    : false");
                return;
            }

            System.Console.WriteLine("DefaultUserPasswd     : {0}", HideSecretDisplay(oConfigOptions.AppConfig.DefaultUserPassword));
            System.Console.WriteLine("Default Port          : {0}", oConfigOptions.AppConfig.DefaultPort );
            System.Console.WriteLine("DryRun                : {0}", oConfigOptions.AppConfig.DryRun);

            System.Console.WriteLine("GlobalLinuxCommandFile: {0}", oConfigOptions.AppConfig.GlobalLinuxCommandFile);
            System.Console.WriteLine("GlobalWinCommandFile  : {0}", oConfigOptions.AppConfig.GlobalWinCommandFile);

            System.Console.WriteLine("Server items          : {0}\n", oConfigOptions.Servers.Count);

            System.Console.WriteLine("[[Server Config]]");
            foreach (Server ServerItem in oConfigOptions.Servers)
            {
                System.Console.WriteLine("-----------------------------------------------------------");
                System.Console.WriteLine("Server ID  : {0}", ServerItem.ID);
                System.Console.WriteLine("Server Name: {0}", ServerItem.Name);
                System.Console.WriteLine("Server IP  : {0}", ServerItem.IPAddress);
                System.Console.WriteLine("Server Port: {0}", ServerItem.Port);
                System.Console.WriteLine("User       : {0}", ServerItem.User);
                System.Console.WriteLine("Password   : {0}", ServerItem.Password);

                System.Console.WriteLine("Commands:");

                if (ServerItem.Cmds != null)
                {
                    foreach (string CmdItem in ServerItem.Cmds)
                    {
                        System.Console.WriteLine("  CMD: {0}", CmdItem);
                    }
                }
                else
                    System.Console.WriteLine("\t*** Using global command files ****");

                System.Console.WriteLine("Command delay: {0} seconds", ServerItem.Delay);
                System.Console.WriteLine("Task repeat   : {0}", ServerItem.NoRepeat);
                System.Console.WriteLine("Reboot after  : {0}", ServerItem.Reboot);
                System.Console.WriteLine("Frequency     : {0}\n", ServerItem.Frequency);
            }
            System.Console.WriteLine("----------------------------------------------------------");

        }

        /// <summary>
        /// Hides the secret at display. All characters, but last four, are shown.
        /// </summary>
        /// <param name="secret">string to the secret used</param>
        /// <returns>reformatted where all but last four characters are displayed.</returns>
        /// 
        // This function was designed to hide secrets during display. Our secrets are in the format of:
        // "username:secret"
        // If user uses the ':' as a part of password, then we decide the _last_ colon is the effective
        // separator of the secret.
        //
        // Test Scenarios:
        //
        // Single colon:
        // Input: "ThisIsASecret:ThisIsMyPassword"
        // Output: "******ecret:********word"
        //
        // Multiple colons:
        // Input: "This:Is:A:Secret:Password"
        // Output: "**************ret:*******word"
        //
        // No colon:
        // Input: "JustAPlainPassword"
        // Output: "***********word"
        //
        // Short secret:
        // Input: "abc"
        // Output: "***"

        public string HideSecretDisplay(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return null;

            //Find the last colon in the string, if any
            int lastColonIndex = secret.LastIndexOf(':');

            if (lastColonIndex != -1)
            {
                // Split the string into the part before and after the last colon
                string beforeColon = secret.Substring(0, lastColonIndex);
                string afterColon = secret.Substring(lastColonIndex + 1);

                // Mask each part using inline private MaskString()
                string maskedBeforeColon = MaskString(beforeColon, 4);
                string maskedAfterColon = MaskString(afterColon, 4);

                //REmix and return
                return (maskedBeforeColon + ":" + maskedAfterColon);
            }

            // No ':' found? Mask the whole thing, return
            return (MaskString(secret, 4));
        }

        /// <summary>
        /// Inline masking function to mask sensitive strings at customized visible chars
        /// </summary>
        /// <param name="input">string input to mask</param>
        /// <param name="visibleChars">How many visible characters you want at end of string</param>
        /// <returns></returns>
        private string MaskString(string input, int visibleChars)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length <= visibleChars)
                return new string('*', input.Length);

            int hiddenChars = input.Length - visibleChars;
            return new string('*', hiddenChars) + input.Substring(hiddenChars);
        }


    }
}

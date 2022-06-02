using System.Collections.Generic;

namespace com.winters.commandutils
{
    class CmdUtils
    {
        public static bool isWin() => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        public static bool isMacOS() => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
        public static bool isLinux() => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

        public static List<string> listGlobalLinuxCommands;
        public static List<string> listGlobalWinCommands;


        //Get basic system information
        public static List<string> SystemInfo()
        {
            List<string> cmdList = new List<string>();
            
            if (isWin())
            {
                cmdList.Add("systeminfo > C:\\Users\\Public\\Downloads\\systeminfo.log");
                cmdList.Add("type C:\\Users\\Public\\Downloads\\systeminfo.log");
                return (cmdList);
            }

            if ((isLinux()) || (isMacOS()))
            {
                cmdList.Add("uname -a");
                cmdList.Add("df -h");
                return (cmdList);
            }

            //Cannot detect OS means list will be empty.
            return (cmdList);
        }

        /// <summary>
        /// Reboot system
        /// </summary>
        /// <returns></returns>
        public static List<string> Reboot()
        {
            List<string> cmdList = new List<string>();

            if (isWin())
            {
                cmdList.Add("shutdown /r /t 0 /d p:0:0");
                return (cmdList);
            }

            if ( (isLinux()) || (isMacOS()) )
            {
                cmdList.Add("sudo shutdown /r now");
                return (cmdList);
            }
            return (cmdList);
        }

        /// <summary>
        /// Get Diskspace from all drives on system
        /// </summary>
        /// <returns></returns>
        public static List<string> DriveInfo()
        {
            List<string> cmdList = new List<string>();

            if (isWin())
            {
                cmdList.Add("wmic logicaldisk  where drivetype=3 get DeviceID,FreeSpace,Size");
                return (cmdList);
            }

            if ((isLinux()) || (isMacOS()))
            {
                cmdList.Add("df -h");
                return (cmdList);
            }

            //Cannot detect OS means list will be empty.
            return (cmdList);
        }

        /// <summary>
        /// Loads a global command file. Commands are line by line.
        /// Commands are typically internal commands of this application or shell/BATch files.
        /// </summary>
        /// <param name="sFile">Full path to commands file</param>
        /// <returns></returns>
        public static List<string> LoadGlobalCommandFile(string sFile)
        {
            List<string> listRetVal = new List<string>();  

            if (System.IO.File.Exists(sFile))
            {
                //Added IEnumerable in case we need to detect something in the line
                //see detecting cls/clearscreen.
                IEnumerable<string> pData =  System.IO.File.ReadLines(sFile);
                foreach (string line in pData)
                {
                    if (line.Contains("cls") & line.Contains("clear"))
                    {
                        System.Console.WriteLine("Clear screen detected: {0}", line);
                        listRetVal.Add(line);   
                    }
                    else
                    {
                        listRetVal.Add(line);
                    }
                    
                }
                return (listRetVal);                
            }
            return (null);  
        }


    }
}


// wmic fsdir where name = 'c:\mnt' get Archive, CreationDate, LastModified, Readable, Writeable, System, Hidden, Status
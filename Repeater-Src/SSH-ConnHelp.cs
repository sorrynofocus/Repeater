using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet;
using System.Threading.Tasks;

//TODO: break up the scp, ftp, and ssh clients.

// uncomment and hover mouse over version to get c# lang ver.
//#error version
// or you could use csc -langversion:?  at command developer prompt.
//
// Needed to upgrade this from .net framework 4.3 C# 7.3 to .net 5.0 (c# 9.0)
// Install NET Portability Analyzer by Choosing Extension and select visual studio marketplace. In search bar, type NET Portability Analyzer. Download. REstarst VS 2019.
//open developer command prompt.
// dotnet tool install -g upgrade-assistant
// cd C:\Users\cwrun\source\repos\Repeater-Console
// run upgrade-assistant upgrade C:\Users\cwrun\source\repos\Repeater-Console\Repeater-Console.sln
//
// https://dotnet.microsoft.com/platform/upgrade-assistant
// https://github.com/dotnet/upgrade-assistant
// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version


namespace com.winters.sshconnhlp
{
    class SSHConn
    {

        //Overall SSH/SCP connection setup
        private static ConnectionInfo ConnNfo = null;

        //SSH Client connection
        private static SshClient sshClient = null;

        //Connection for up/download
        private static ScpClient scpClient = null;

        //Run SSH command -async
        private static SshCommand sshComd = null;

        //FTP client if we need to upload files
        SftpClient ftpClient = null;

        //Buffer size for ftp upload/download client (4096 kB)
        private uint ftpBufferSize = (4 * 1024);

        private string ExceptionMessage = string.Empty;
        private int ExceptionErrorCode = 0;


        public int SShClient(string sHost, Int32 iPort, string sUser, string sPasswd)
        {
            //ConnectionInfo ConnNfo = new ConnectionInfo(sHost, iPort, sUser, new AuthenticationMethod[]
            ConnNfo = new ConnectionInfo(sHost, iPort, sUser, new AuthenticationMethod[]
                     {
                        // Pasword based Authentication
                        new PasswordAuthenticationMethod(sUser,sPasswd)/*,

                        // Key Based Authentication (using keys in OpenSSH Format)
                        new PrivateKeyAuthenticationMethod("username",new PrivateKeyFile[]{
                            new PrivateKeyFile(@"..\openssh.key","passphrase")
                        }),*/
                     }
              );

            ConnNfo.Encoding = Encoding.UTF8;

            sshClient = new SshClient(ConnNfo);

            try
            {
                sshClient.Connect();
            }
            //catch (Renci.SshNet.Common.SshAuthenticationException ex)
             catch (Exception ex) 
            {

                //  System.Net.Sockets.SocketException: 'A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond'
                // ErrorCode (int) 10060 Message (string) socketErrorCode (int) 10060

                System.Console.WriteLine (HandleException(ex));

                //if (ex.Message.Contains("Permission denied (password)"))
                //{

                //    System.Console.WriteLine("Permission denied. Check your password in configuration. (for server ID)");
                //    //Environment.Exit(-255);
                //    //TODO Get HResult codes 
                //    return (ex.HResult);
                //}
                return (1);
            }

            return (0);
        }

        public string GetHost() => sshClient.ConnectionInfo.Host.Trim();
        public string GetPort() => sshClient.ConnectionInfo.Port.ToString();
        public string GetClientVersion() => sshClient.ConnectionInfo.ClientVersion.ToString();

        public string GetUser() => sshClient.ConnectionInfo.Username.Trim();    


        public string HandleException(Exception prgException)
        {

            //  System.Net.Sockets.SocketException: 'A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond'
            // ErrorCode (int) 10060 Message (string) socketErrorCode (int) 10060

            //if (prgException is System.Net.Sockets.SocketException || prgException is System.Net.Sockets.SocketException)
            //if (prgException is System.Net.Sockets.SocketException )
            //{
            //    return;
            //}

            switch (prgException)
            {
                case Renci.SshNet.Common.SshAuthenticationException:

                    if (prgException.Message.Contains("Permission denied (password)"))
                    {
                        //System.Console.WriteLine("Permission denied. Check your password in configuration. (for server ID)");
                        //Environment.Exit(-255);
                        //TODO Get HResult codes 
                        ExceptionMessage = "Permission denied. Check your password in configuration. (for server ID)";
                        return (ExceptionMessage);
                    }
                    break;

                case System.Net.Sockets.SocketException:

                    if (prgException.Message.Contains("failed to respond"))
                    {
                        //System.Console.WriteLine("Check your server config. Cannot connect. No network? Or, it refuses to connect.");
                        //Environment.Exit(-255);
                        //TODO Get HResult codes 
                        ExceptionMessage = "*** COMMS ERROR **** Check your server config. Connection refused to connect.";
                        return (ExceptionMessage);
                    }
                    break;

                case Renci.SshNet.Common.ScpException:

                    if (prgException.Message.Contains("No such file or directory"))
                    {
                        ExceptionMessage = "*** FILE ERROR **** File/Directory not found OR a reference to a mapped resource not recognised if it is mapped under another workspace.";
                        return (ExceptionMessage);
                    }
                    break;


                case System.NullReferenceException:

                    if (prgException.Message.Contains("Object reference"))
                    {

                        System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(prgException, true);
                        System.Diagnostics.StackFrame[] frames = st.GetFrames();
                        StringBuilder traceString = new StringBuilder();

                        foreach (var frame in frames)
                        {
                            if (frame.GetFileLineNumber() < 1)
                                continue;

                            traceString.Append("File: " + frame.GetFileName());
                            traceString.Append(", Method:" + frame.GetMethod().Name);
                            traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
                            traceString.Append("  -->  ");
                        }

                        return (traceString.ToString());
                    }
                    break;

                default:  break;
                    
            }

            return (string.Empty);
        }

        public bool SCPClient()
        {
            scpClient = new ScpClient(ConnNfo);
            scpClient.Connect();

            return (scpClient.IsConnected);
        }

        /// <summary>
        /// Open an FTP client to perform some funcitons on remote.
        /// </summary>
        /// <returns></returns>
        public bool FTPClient()
        {
            ftpClient = new SftpClient(ConnNfo);
            ftpClient.Connect();

            if (ftpClient.IsConnected)
                return (true);
            else
                return (false);
        }

        /// <summary>
        /// Gets or sets bufer size of ftp client download/uploads.
        /// </summary>
        public uint FtpClientBufferSize 
        {
            get => ftpBufferSize;
            set { ftpBufferSize = value; }
        }

        /// <summary>
        /// Change directory on remote (ftp) use nix like dirs. For example: ./Downloads will go to Downloads directory if under c:\user\username
        /// </summary>
        /// <param name="remoteDir"></param>
        public void ChangeDir(string remoteDir)
        {
            
            //TODO change all \ to /
            //handle exceptions
            ftpClient.ChangeDirectory(remoteDir);
        }

        /// <summary>
        ///  Gets remote working directory.
        /// </summary>
        public string Workdir
        {
            get => ftpClient.WorkingDirectory;
        }


        /// <summary>
        /// Uploads a local file manually to remote (ftp) without automation such as changing dir, setting buffer size, etc.
        /// </summary>
        /// <param name="filestream">file stream of a local file to upload</param>
        /// <param name="destRemoteFile">full path to the remote file location on remote</param>
        /// 
        /// To facilitate a manual upload with customised actions between uploads, follow this code:
        /// using (FileStream filestream = new FileStream(@"C:\\mnt\\windows\\systeminfo-remote.log", FileMode.Open))
        /// {
        ///    sshconn.FtpClientBufferSize = 4096;
        ///    //Change dir, format the directory for OS unix or windows.
        ///    //windows prefers /c/test/dir1 on unix and c:\test/dir1 on windows but you have to know what system you're on.
        ///    sshconn.ChangeDir(sshconn.FormatRemoteOSPath("C:\\MNT\\windows\\temp", sshconn.Workdir) );
        ///    //NOT WORKING.
        ///    sshconn.FtpUpload2(filestream, "C:\\MNT\\windows\\temp\\system-info-uploadtolocal.log");
        /// }
        /// 
        public void FtpUploadFile(System.IO.FileStream filestream, string destRemoteFile)
        {
            ftpClient.UploadFile(filestream, System.IO.Path.GetFileName(destRemoteFile));
        }

        /// <summary>
        /// Uploads a local file automatically to remote (ftp) 
        /// </summary>
        /// <param name="sourceLocalFile">full path to the file location on local system to upload</param>
        /// <param name="destRemoteFile">full path to the remote file location on remote</param>
        public void FtpUploadFile(string sourceLocalFile, string destRemoteFile)
        {
            //Set a general buffersize
            if (FtpClientBufferSize == 0)
                FtpClientBufferSize = 4096;

            //Get the current directory. Once we switch to it, we upload file. Then, we switch back to previous dir.
            string curDir = Workdir;

            //Get the path to the file we are passing in. We'll need to switch to the dir first, then upload to remote.
            string tempSwitchTodir = System.IO.Path.GetDirectoryName(destRemoteFile);

            using (System.IO.FileStream filestream = new System.IO.FileStream(sourceLocalFile, System.IO.FileMode.Open))
            {
                //Passing in workdir determines what os we are on, giving a sample of the file being uploaded.
                //windows prefers /c/test/dir1 on unix and c:\test/dir1 on windows but you have to know what system you're on.
                //FormatRemoteOSPath() takes care of this for us.
                ChangeDir(FormatRemoteOSPath(tempSwitchTodir, Workdir) );
                ftpClient.UploadFile(filestream, System.IO.Path.GetFileName(destRemoteFile));
            }

            //change back to the original directory.
            ChangeDir(curDir);
        }

        /// <summary>
        /// Format path in Unix or Windows format, depending on what system we're talking to
        /// </summary>
        /// <param name="path"></param>
        /// <param name="samplePath"></param>
        public string FormatRemoteOSPath(string actorPath, string remoatePathSample)
        {
            if (remoatePathSample.Contains("/"))
            {
                //We're on unix type system, modify path
                actorPath = "/" + actorPath.Replace("\\", "/");
                return (actorPath);
            }
            else
                //we're attached to a windows system, no need to alter path
                return (actorPath);
        }

        /// <summary>
        /// Disconnect from all services and release resources.
        /// </summary>
        public void Disconnect()
        {
            if (IsSSHConnected)
            {
                if (sshClient != null)
                {
                    sshClient.Disconnect();
                    sshClient.Dispose();
                }
            }

            if (IsSCPConnected)
            {
                if (scpClient != null)
                {
                    scpClient.Disconnect();
                    scpClient.Dispose();
                }
            }

            if (ftpClient != null)
            {
                if (IsFTPConnected)
                {
                    ftpClient.Disconnect();
                    ftpClient.Dispose();
                }
            }

        }

        public bool IsSSHConnected { get => sshClient.IsConnected; }
        public bool IsSCPConnected { get => scpClient.IsConnected; }

        public bool IsFTPConnected { get => ftpClient == null ? false : ftpClient.IsConnected; }


        public bool IsAllConnected
        {  
            get
            {
                if ((sshClient==null) && (scpClient == null))
                    return (false);

                if ((!sshClient.IsConnected) && (!scpClient.IsConnected))
                    return (false);
                else
                    return(true);
            }
        }

        /// <summary>
        /// Takes a windows path and converts it to linux type. Used for upload/download funcs for example.
        /// </summary>
        /// <param name="Src">source string of a filename</param>
        /// <returns>modified string with linux path types</returns>
        public string FilePathSepClean(string Src)
        {
            StringBuilder tmpSB = new StringBuilder(Src);
            tmpSB.Replace("\\\\", "/");
            tmpSB.Replace("\\", "/");

            return (tmpSB.ToString());
        }

        /// <summary>
        /// Runs a command  -non async 
        /// NON-PROD: prototype function!
        /// </summary>
        /// <param name="sshClient"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static String ExecuteCommand(String command)
        {
            if (sshClient == null) return (null);

            if (!sshClient.IsConnected)
                sshClient.Connect();

            return sshClient.CreateCommand(command).Execute();
        }

        public Dictionary<int, string> ExecuteCommandEx(String command)
        {
            if (sshClient == null) return (null);

            //TODO find a better way to return this. Maybe dict isn't the best.
            Dictionary<int, string> Results = new Dictionary<int, string>();
            sshComd = sshClient.RunCommand(command);
            Results.Add(sshComd.ExitStatus, sshComd.Result);

            return (Results);
        }

        /// <summary>
        /// Downloads a file from a remote system to the local system.
        /// </summary>
        /// <param name="sourceRemoteFile"></param>
        /// <param name="destinLocalFile"></param>
        public void Download (string sourceRemoteFile, string destinLocalFile)
        {
            //TODO, rollcreate directories
            scpClient.Download(sourceRemoteFile, new System.IO.FileInfo(destinLocalFile));
        }

        public void Upload (string destinLocalFile, string sourceRemoteFile)
        {
            destinLocalFile = FilePathSepClean(destinLocalFile);
            sourceRemoteFile = FilePathSepClean(sourceRemoteFile);

            System.IO.FileInfo pFile = new System.IO.FileInfo(destinLocalFile);
            scpClient.Upload(pFile, sourceRemoteFile);
            System.Console.WriteLine("\tUpload complete: {0} {1}", sourceRemoteFile, destinLocalFile);

        }

        //Detecting windows will be a bit tricky since SSH doesn't have protocol to do so.
        // So, 

        public static int DetectOS(int Override=4000) 
        {
            //Git maybe installed, or some other tool that has unix style commands, so uname -a will return something like this:
            // MSYS_NT-10.0-22000 Precelsior 3.1.7-340.x86_64 2020-10-23 13:08 UTC x86_64 Msys
            // pulling "MSYS_NT" would give us a hint that this could be windows MSYS = minimal system and NT would be windows NT. 10.0-22000 is the build number.
            //If windows returns 'uname' is not recognized as an internal or external command, operable program or batch file. then this means git is not installed and uname is not found anywhere and should return as windows system.
            // the command "ver" will always be on the system returning "Microsoft Windows [Version 10.0.{build number
            string sUnameResults = ExecuteCommand("uname -a");
            string sWinVerResults = ExecuteCommand("ver");

            //Return values:
            // -1     - could not find os
            // winnt  - 1000
            // mac os - 2000
            // linux  - 3000
            // Dry run or override  - pass in the return value as param

            if (sUnameResults != null)
            {
                System.Console.WriteLine("DetectOS() - sUnameResults - {0}  ", sUnameResults);

                if (sUnameResults.StartsWith("Darwin", true, null))
                    return (2000);

                if (sUnameResults.StartsWith("Linux", true, null))
                    return (3000);

                //What if we have linux tools like git on the system?
                if (sUnameResults.StartsWith("MSYS_NT", true, null))
                {
                    if (sUnameResults.Contains("10.0"))
                        return (1000);
                }

                //What if we have linux tools like git on the system?
                if (sUnameResults.StartsWith("CYGWIN", true, null))
                {
                    if (sUnameResults.Contains("10.0"))
                        return (1000);
                }


                if (sUnameResults.Contains("is not recognized"))
                    return (1000);
            }

            if (sWinVerResults != null)
            {

               // System.Console.WriteLine("DetectOS() - sWinVerResults -{0}-  ", sWinVerResults);

                //if (sWinVerResults.StartsWith("Microsoft Windows", true, null))
                if (sWinVerResults.Contains("Microsoft Windows"))
                    return (1000);

                if (sWinVerResults.Contains("command not found"))
                    return (3000);
            }

            switch(Override)
            {
                case 1000: return (1000);
                case 2000: return (2000);
                case 3000: return (3000);
            }

            // Uname for Linux starts off with "Linux" and elsewhere down the string.
            // Uname for Mac OS starts off with "Darwin" and elsewhere down the string.
            //System.Console.WriteLine("DetectOS() - returning -1 ");
            return (-1);
        }


        public  bool isWindows()
        {
            if ( DetectOS() == 1000)
                return true;
            else
                return false;   
        }

        public  bool isLinux()
        {
            if (DetectOS() == 3000)
                return true;
            else
                return false;
        }

        public  bool isMacOS()
        {
            if (DetectOS() == 2000)
                return true;
            else
                return false;
        }


        //
    }
}

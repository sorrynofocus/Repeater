This is a MARKDOWN file.

July 21 2021

Most of the work has been configuration and processing configuration. As of this date, conmfiguration is parsing correct. The final config format will 
be changed as it is not blessed right now. Also, the code in MAIN is bunched up to work, and that will be broken up and separated later. Right now, 
this is in alpha mode, passing the proof of concept. The idea was to SSH into machines and do commands. Since then, custom commands have been added 
such as rebooting a system, etc.



* Tested the parameter _--config <config file>_ This is also evident in running the application. A display will show the location of the config used.
* Currently working on _ -cmd run server <server ID>_
* Added connection code and tested home local server. Ran a few commands. Success! However, if you run a UI program such as _notepad_ then the process
would not end. This is expected. Will have to add code that if an application lasts more than a minute, then it should be considered _released_ 
This will enable other commands to process. Commented out comms code for now.

3/27/2022
-Bump!-
Began  working on project again.Going over code and notes to catchup
- Adding display config with parameter "showconfig"
- Adding defaultuser:password and will remove default user and default password. Using "username:password" is more secure. Encryption will be the next 
phase. 

3/28/2022
 - Small console text changes for server work output
 - added small key capture to later install key captures such as ESCAPE to abort.
 - Implemented defaultuser:password in ConnectServer() call
 - Adding "DryRun" [true|false] to run without connection but show what connections would look like.
 - Adding " CMD -> Sys:" debug to begin processing commands to be sent to system (custom command, not app specific like @reboot)

 3/31/2022
 - cleaning up, adding additional DryRun areas.
 - removing old files
 - Tested on linux and windows with simple commands. They tested. However, program crashed in beginning and had to fill out user paswords for each server. 
 Discovered server is being connected to without username/password and doesn't utilise the default username and password. ConnectToServer() later on in 
 code is used and a connector has been placed to connect without the information. Either this can be removed or if it _is_ used, then the handling of 
 default credentials have to be used. It's 12:30am, and a busy week. 

04/03/2022
- Fixed double connection causing null connection and forced to use user/password in config for each server.
- Added internal command systeminfo for windows. Added new class CmdUtils() for utility internal commands. SystemInfo() tested for windows looks good. 
we could switch this to wmic, however. Linux and Mac has not been added yet.

04/04/2022
- Added reboot and other commands, for linux, windows, and Mac.
- Got upload/download working for windows. Using Upload() and Download() in class SSHConn, these use the scpClient instead of FTP.The configuration ended
up looking like this:
        "@download c:\\users\\chris_winters\\Downloads\\MicrosoftEdgeSetup.exe  c:\\mnt\\MicrosoftEdgeSetup.exe",
        "@upload C:\\Users\\cwrun\\GetWindowsProductKey.bat c:\\opt_cm/GetWindowsProductKey.bat"
        Notice the @download line has double "\\" path marks. We need to make this more user friendly and only use single path marks.
        Note the @upload. This also includes the double paths marks _BUT_ c:\\opt_cm/GetWindowsProductKey.bat has a unix style forward path "/". ALL 
        paths would need to be replaced with forward paths on the REMOTE filename. Roll creation of directories ARE NOT SUPPORTED yet.
        
        FAILED:
        c:\\opt_cm\GetWindowsProductKey.bat
        /c/opt_cm/GetWindowsProductKey.bat

        PASSED:
        c:\\opt_cm/GetWindowsProductKey.bat
        c:/opt_cm/GetWindowsProductKey.bat

- Added macoscommandfile, linuxcommandfile, and wincommandfile (or windowscommandfile) to process comamnds on global scale rather than redundant commands for 
each server. If all the servers do the same, the global files can be used to process on them.
- Added some logic to the global lines. Need to "detect" Os so we can figure out which command file to use.
- Added display new config for global file
- Added LoadGlobalCommandFile() in commandutils.cs to load command file.
- Added a few error checks for nullable variables.

04/05/2022
- Handled exceptions for bad configuration, such as illegal character
TODO: pass in --createconfig to create template

04/25/2022
Tested on MAc, Windows, and linux machine.
FAilure trying to copy from a mapped drive to a local. We forget that SSH does not have access to drives because we are logged into another workstation. 
So, we get
Unhandled exception. Renci.SshNet.Common.ScpException: scp: Z:/test/sdksetup.exe: No such file or directory
   at Renci.SshNet.ScpClient.ReadString(Stream stream)
   at Renci.SshNet.ScpClient.InternalDownload(IChannelSession channel, Stream input, FileSystemInfo fileSystemInfo)
   at Renci.SshNet.ScpClient.Download(String filename, FileInfo fileInfo)
   at TestSSH.TestSSH.StartSvrWorkByID(String serverID)
   at TestSSH.TestSSH.Main(String[] args)

On Mac we get this error trying to upload a file to MAc. However, we relay on checking if sshconn.ftpclient is connected, which we do not use. Error:


        Uploading: C:\temp\TestSSH-Tool\doc-upload-to-linux-machines-demo.txt /tmp/doc-upload-to-linux-machines-demo.txt
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
   at com.winters.sshconnhlp.SSHConn.get_IsFTPConnected()
   at com.winters.sshconnhlp.SSHConn.Disconnect()
   at TestSSH.TestSSH.StartSvrWorkByID(String serverID)
   at TestSSH.TestSSH.Main(String[] args)

Other than that, commands used worked well such as common shells commands and wmic commands used in test config. Ideal to stay ina  clean environment and 
remember that mapped drives, aliasas, cannot be vieweed in a clean workstation.


If we need to get parameters of called function we can use this reflection:

```
            System.Diagnostics.StackTrace callStack = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame frame = null;
            System.Reflection.MethodBase calledMethod = null;
            System.Reflection.ParameterInfo[] passedParams = null;
            for (int x = 0; x < callStack.FrameCount; x++)
            {
                frame = callStack.GetFrame(x);
                calledMethod = frame.GetMethod();
                passedParams = calledMethod.GetParameters();
                foreach (System.Reflection.ParameterInfo param in passedParams)
                    System.Console.WriteLine(param.ToString());
            }
```
or a nicer function:
https://stackoverflow.com/questions/4272579/how-to-print-full-stack-trace-in-exception
```
public string GetAllFootprints(Exception x)
{
        var st = new StackTrace(x, true);
        var frames = st.GetFrames();
        var traceString = new StringBuilder();

        foreach (var frame in frames)
        {
            if (frame.GetFileLineNumber() < 1)
                continue;

            traceString.Append("File: " + frame.GetFileName());
            traceString.Append(", Method:" + frame.GetMethod().Name);
            traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
            traceString.Append("  -->  ");
        }

        return traceString.ToString();
}
```

04/25/2022
- Fixed bugs such as:
  - if adding extra blank newlines in widnows/linux command file list files, an error occured where index of commands could not process.
  - null objects
  - fixing detection of windows os correctly.
  - fixing upload but TODO is to change ALL \ to / if windows.
  - IsAllConnected() 
  Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
   at com.winters.sshconnhlp.SSHConn.get_IsAllConnected() in C:\Users\cwrun\source\repos\TestSSH-CSharp-9.0\TestSSH\ssh-connhelp.cs:line 355
   at TestSSH.TestSSH.StartSvrWorkByID(String serverID) in C:\Users\cwrun\source\repos\TestSSH-CSharp-9.0\TestSSH\Program.cs:line 811
   at TestSSH.TestSSH.Main(String[] args) in C:\Users\cwrun\source\repos\TestSSH-CSharp-9.0\TestSSH\Program.cs:line 256

- Adding defaultport and functionality to detect which to switch to. 
- changed windows command line for system reboot. Cannot use APIs because the application affects the current system , not the remote.
- Since there's no way to determine the OS during a dry run when loading up commands, all commands are displayed on each server.
- Upload() in sshconhelp.cs replace all \ with /. This has to be done like this because:
Doesn't work in commands file:
@upload C:\temp\TestSSH-Tool\getproductkey.ps1 c:\temp\getproductkey.ps1
Does work in commands file:
@upload C:/temp/TestSSH-Tool/getproductkey.ps1 c:/temp/getproductkey.ps1

1-8-2024
It's been a long time!
What's new?
 - New function AttachSvrByID() where you can attach to a server in the config via basic SSH. You can list directories, examine files, run 
   basic Os commands. Editing files is a bit of a challenge, but this is not as important as just getting basic ssh working. This took some 
   time to get working. The function si not finished as it needs commands revamping. And...
 - I've discovered why sometimes the isWindows, isLinux, or isMac() are not working. This is because we're detecting OS by interoperable 
   means. The correct functions that work as a remote detection are the functions in sshconn-help.cs-- see DetectOS(). We may need to move
   these commands over into the CommandUtils.cs and rename the ones that use interoperable 
 - Added new command line arg: -cmd attach server {ServerID} this is the activate the ssh shell as described above. Currently, you can use 
   server ID to connect to. For credentials, you can use the ones from config. Using a server IP alone is not supported. It's best to add 
   it to the config.
 TODOs:
 - encrypt/decrypt password
 - Add SSH key authentication
 - AttachSvrByID() -finish it.
 - Clean up code
 - Some command line args functionality are missing. Example: "--configme"
 - Go over older bugs.

10-8-2024
It's been a long time -again!
- Fixed a bug where if dry run and loading global run files (linux/windows) will produce unhandeld exception.
   Unhandled exception. System.ArgumentNullException: Value cannot be null. (Parameter 'collection')
   at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection)
   at com.repeater.program.RepeaterPrg.StartSvrWorkByID(String serverID)
   at com.repeater.program.RepeaterPrg.Main(String[] args)
- Changing a few strings displaying status
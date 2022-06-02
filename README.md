# Repeater
A cross-platform SSH tool to automate tasks on various VMs or machines. Repeater is written in C# built on .NET Core.

Repeater is a tool that uses SSH comms to run tasks or automation on virtual machines or static machine nodes. Repeater can be configured for individual or global credentials to log into a system. The configuration is in JSON format and a dry-run test can be used to test out output.

# Current development
Repeater works, but is in current development. Code will change, files will change, and possibly some features will be removed or improved. However, I hope the tool is useful. Releases will be in the _release_ section (soon to come). You can build this utility with build files. 

**Requirements** 
This tool was written in _Visual Studio 2022 .NET Core 5.0_ minimum. Instructions on setting up .NET Core will come later for both Windows and Linux.

Repeater uses _NewtonSoft.JSON_ and _SSH.Net_ packages. The solution build will _restore_ the packages if they are not installed. 

build.cmd - build file to build Repeater on _Windows_. The build will produce a Linux, Mac, and Windows single-file distribution for easy install.

build.sh - build file to build Repeater on _Linux_. The build will produce a Linux, Mac, and Windows single-file distribution for easy install.

**NOTES**
Look under the source directory for the _Notes_ directory. There will be a dev-notes.md file to read to show development progression.

For configuration examples, look under the _Notes/Config_ directory. There's also global files to check out if you need automation to run on many machines with same tasks.







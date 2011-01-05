=======================================================================
Welcome to:
 * Apache.NMS.EMS : Apache NMS for Tibco EMS Client Library
=======================================================================

For more information see http://activemq.apache.org/nms

=======================================================================
Building With NAnt 0.86 see http://nant.sourceforge.net/
=======================================================================

NAnt version 0.86 or newer is required to build Apache.NMS.EMS.  Version 0.90
or newer is highly recommended.
To build the code using NAnt, run:

  nant

To generate the documentation, run:

  nant doc

=======================================================================
Building With Visual Studio 2008
=======================================================================

First build the project with nant, this will download and install 
all the 3rd party dependencies for you.

Open the solution File.  Build using "Build"->"Build Solution" 
menu option.

The resulting DLLs will be in build\${framework}\debug or the 
build\${framework}\release directories depending on your settings 
under "Build"->"Configuration Manager"


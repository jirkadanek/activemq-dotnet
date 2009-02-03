=======================================================================
Welcome to:
 * Apache.NMS.EMS : Apache NMS for Tibco EMS Class Library
=======================================================================

For more information see http://activemq.apache.org/nms

=======================================================================
Building With NAnt 0.86-Beta2 http://nant.sourceforge.net/
=======================================================================

A recent nightly build of the NAnt 0.86 beta 2 is required to build Apache.NMS.
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


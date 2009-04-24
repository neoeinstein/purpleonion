PurpleOnion
===========
*The Purple Onion Router*

An implementation of the [Tor Onion Router] [tor] on Mono/.NET.

License
-------
PurpleOnion binaries and sources are licensed under a BSD-style license.
For more information, see the `LICENSE` file.

POSIX
-----
Support for receiving certain terminating signals and exiting cleanly is
automatically enabled in long-running applications on POSIX machines where
Mono.Posix is available. These applications should still behave and JIT
cleanly even when run on non-POSIX hosts, though applications will not be able
to receive signals.

When POSIX signal handling is enabled, background threads will be allowed 
to complete their queued up work before terminating. Currently, SIGHUP,
SIGTERM, and SIGINT are processed as requests to exit. Since all background
threads are allowed to exit cleanly, a high numbers of workers may allow the
program to continue running for a few seconds while the remaining worker
threads finish processing.

Receiving a SIGKILL signal will immediately cause execution to cease;
this signal cannot be caught or handled.

Applications
------------
Applications can be run using the Mono framework by calling the executable
prefixed with `mono` as in:

	mono Por.Application.exe [arguments]

When running on a Windows machine using the .NET framework, the executable can
be called directly without prefixing the runtime:

	Por.Application.exe [arguments]

### Por.OnionGenerator
Currently the only fully implemented module in the PurpleOnion package, the
OnionGenerator is a program that facilitates the brute-force generation of
vanity hidden service, or .onion, addresses.

Development
-----------
### Compilation
PurpleOnion requires at least Mono 2.0 to compile and run. The
compiled binary will run on the .NET 2.0 framework with the appropriate
libraries available (including `Mono.Security`).

The main development takes place in [MonoDevelop][] with `PurpleOnion.sln` as
the root solution. All projects can be compiled within MonoDevelop, or by
running `mdtool`:

	mdtool build PurpleOnion.sln

Makefiles are also generally kept up to date as well. In order to compile from
the make files, use the standard:

	./configure && make

### Standards
Unless otherwise noted, PurpleOnion chooses to follow the design guidelines set
forth in [_Framework Design Guidelines, Second Edition_] [fdg2e] by Cwalina and
Abrams.

#### Namespaces
PurpleOnion consumes the `Por` namespace which is an acronym for "Purple Onion
Router". Only the initial letter is capitalized in keeping with
initialism-namespace standards.

Attribution
-----------
Contributors to this project are self-identified by their commit history
and by self-attribution in the AUTHORS file.

### Mono.Options
This project uses transcluded code from the Mono.Options project, as part
of the Mono project and in accordance with the [Guidelines for Application
Deployment] [mono-gad] to permit simple access for reuse, as licensed under
the MIT/X11 license:

	Copyright (C) 2008 Novell (http://www.novell.com)
	
	Permission is hereby granted, free of charge, to any person obtaining
	a copy of this software and associated documentation files (the
	"Software"), to deal in the Software without restriction, including
	without limitation the rights to use, copy, modify, merge, publish,
	distribute, sublicense, and/or sell copies of the Software, and to
	permit persons to whom the Software is furnished to do so, subject to
	the following conditions:
	
	The above copyright notice and this permission notice shall be
	included in all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
	MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
	NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
	LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
	OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
	WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

[tor]: http://www.torproject.org/
[fdg2e]: http://www.pearsonhighered.com/educator/academic/product/0,3110,0321545613,00.html
[mono-gad]: http://mono-project.com/Guidelines:Application_Deployment
[monodevelop]: http://www.monodevelop.org/

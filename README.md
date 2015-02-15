# Telnet in the browser using SignalR #

**Towser** is an application to allow access to telnet services from the browser. An telnet client runs on an IIS web server, and a terminal emulator runs in the browser. These two components communicate using SignalR.

### Set up ###

Configure the telnet client by editing web.config:

* `server`: Telnet server
* `port`: Telnet port, usually 23
* `encoding`: Character encoding used by the telnet server - a list of encoding names is available at http://msdn.microsoft.com/en-us/library/system.text.encoding(v=vs.110).aspx
* `altencoding`: Alternate character encoding for use between ASCII ShiftOut (0x0E) and ShiftIn (0x0F) control codes
* `termtype`: Terminal emulation type (always VT100)

### Terminal emulator ###

The terminal emulation is term.js by Christopher Jeffrey (MIT License) https://github.com/chjj/term.js
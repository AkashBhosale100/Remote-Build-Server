:compile.bat

csc /t:library interfaces.cs
csc /t:library /r:interfaces.dll testedlib.cs
csc /t:library  /r:interfaces.dll /r:testedlib.dll testlib.cs
csc /r:interfaces.dll dllloader.cs
:
: Note: DllLoader is bound only to the interfaces.dll.
:       It is not bound to any application specific code.  
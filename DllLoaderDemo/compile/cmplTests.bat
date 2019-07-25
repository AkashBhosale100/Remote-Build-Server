:cmplTests.bat

csc /t:library  /r:interfaces.dll /r:testedlib.dll testlib.cs
copy testlib.dll testers
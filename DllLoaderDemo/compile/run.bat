:run.bat

md testers
copy *.dll testers
dllloader
dllloader >run.dat
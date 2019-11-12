rem only register the plugin when the Release configuration is set
rem this allows for using a separate version for testing while
rem the original one remains registered.

rem set the variables so we can find regasm (VS must be installed)
call "%VS90COMNTOOLS%vsvars32.bat" 

if /i "%1" == "Arc9" regasm /u %2
if /i "%1" == "Arc9" regasm /tlb /codebase %2

set _cf=%CommonProgramFiles(x86)%
if /i %PROCESSOR_ARCHITECTURE% == "x86" set _cf=%CommonProgramFiles%

if /i "%1" == "Arc10" regasm /tlb /codebase %2
if /i "%1" == "Arc10" "%_cf%\ArcGIS\bin\ESRIRegAsm.exe" /u /p:desktop /s %2
if /i "%1" == "Arc10" "%_cf%\ArcGIS\bin\ESRIRegAsm.exe" /p:desktop /s %2
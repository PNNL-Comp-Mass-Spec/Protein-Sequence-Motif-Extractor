@echo Off

rem This file automates the process of updating the zipped source code and zipped installer for distribution
rem Steps:
rem 1. Make sure the distribution folder exists
rem 2. Call Update_Supporting_Source_Code.bat
rem 3. Update the zipped source code file
rem 4. Zip together the zipped source code file and the DLL source code files
rem 5. Copy this file to the final folder
rem 6. Create the zipped program file
rem 7. Copy the zipped program file to the final folder

Set ProgramName=ProteinSequenceMotifExtractor
Set DistributionFolderBase=F:\Public\Software

Set SourceCodeFolder=%ProgramName%_SourceCode
Set SourceCodeFile=%ProgramName%_Source_v*.zip
Set SourceAndSupportingDLLsFile=%ProgramName%_SourceAndSupportingDLLs.zip

Set ZippedProgramFile=%ProgramName%_Program.zip

Set DistributionFolderFinal=%DistributionFolderBase%\%ProgramName%

echo.
echo 1) Making sure the distribution folder exists
If Not Exist %DistributionFolderBase% (MkDir %DistributionFolderBase%)
If Not Exist %DistributionFolderFinal% (MkDir %DistributionFolderFinal%)

cd SourceAndSupportingDLLs

echo.
echo 2) Updating Supporting DLL Zip Files
Call Update_Supporting_Source_Code.bat

echo.
echo 3) Updating Source Code file for %ProgramName%
Move %SourceCodeFile% ..\..\
CD ..\..
for %%i in (%SourceCodeFile%) do "c:\program files\winrar\winRar.exe" f %%i
Move %SourceCodeFile% %SourceCodeFolder%\SourceAndSupportingDLLs\

cd %SourceCodeFolder%

echo.
echo 4) Creating %SourceAndSupportingDLLsFile%
If Exist %SourceAndSupportingDLLsFile% (Del %SourceAndSupportingDLLsFile%)
"c:\program files\winrar\winRar.exe" a -ep %SourceAndSupportingDLLsFile% SourceAndSupportingDLLs\*.zip
"c:\program files\winrar\winRar.exe" a -ep %SourceAndSupportingDLLsFile% ..\bin\ReadMe.txt
"c:\program files\winrar\winRar.exe" a -ep %SourceAndSupportingDLLsFile% ..\bin\RevisionHistory.txt

echo.
echo 5) Copying %SourceAndSupportingDLLsFile% to %DistributionFolderFinal%
Copy %SourceAndSupportingDLLsFile% %DistributionFolderFinal%

echo.
echo 6) Update the Zipped Program file
Move %ZippedProgramFile% ..\bin\
CD ..\bin
for %%i in (%ZippedProgramFile%) do "c:\program files\winrar\winRar.exe" f %%i
Move %ZippedProgramFile% ..\%SourceCodeFolder%\
cd ..\%SourceCodeFolder%

echo.
echo 7) Copying %ZippedProgramFile% to %DistributionFolderFinal%
rem Copy %ZippedProgramFile% %DistributionFolderFinal%


:Done
Pause
@echo off
echo Find K# in proteins

@echo on
ProteinSequenceMotifExtractor.exe Example_Proteins.txt /M:K# /N:30 /K /T

@echo off
echo Use a RegEx to match DNA bases

@echo on
ProteinSequenceMotifExtractor.exe DNA.txt /M:"GTA.{8,10}TAC" /N:30 /K /T /X

@echo off
echo.
pause
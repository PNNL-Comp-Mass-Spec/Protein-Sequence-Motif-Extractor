echo Find K# in proteins
ProteinSequenceMotifExtractor.exe Example_Proteins.txt /M:K# /N:30 /K /T

echo Use a RegEx to match DNA bases
ProteinSequenceMotifExtractor.exe DNA.txt /M:"GTA.{8,10}TAC" /N:30 /K /T /X

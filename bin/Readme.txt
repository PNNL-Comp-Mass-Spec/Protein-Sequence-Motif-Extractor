The Protein Sequence Motif Extractor reads a fasta file or tab delimited file
containing protein sequences, then looks for the specified motif in each 
protein sequence.  Results are stored in a new file containing the regions 
of the protein that contain the specified motif.  The default output format
is a new fasta file named _Motifs.fasta, but you can use /T to specify
that a tab-delimited text file be created.

Use the /N switch to specify the number of residues to include before and after
the matching motif.  If the motif is close to the beginning or end of a protein
then dashes will be used to assure that there are still the desired number
of characters before and after the motif (this is a common requirement
for alignment software).

By default, any modification symbols (or lower case letters, or numbers, etc.)
are removed when writing out the motif and the flanking residues.  To keep
the modification symbols, use switch /K


Example command line to look for motif K# in file Proteins.fasta and 
includes 30 residues before and after the matching motif:

   ProteinSequenceMotifExtractor.exe /I:Proteins.fasta /M:K# /N:30

You can optionally use a regular expression (RegEx) for matching the motif.  
Use the /X switch to enable this mode.

-------------------------------------------------------------------------------
Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
Copyright 2010, Battelle Memorial Institute.  All Rights Reserved.

E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
Website: http://ncrr.pnl.gov/ or http://omics.pnl.gov
-------------------------------------------------------------------------------

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0

All publications that result from the use of this software should include 
the following acknowledgment statement:
 Portions of this research were supported by the W.R. Wiley Environmental 
 Molecular Science Laboratory, a national scientific user facility sponsored 
 by the U.S. Department of Energy's Office of Biological and Environmental 
 Research and located at PNNL.  PNNL is operated by Battelle Memorial Institute 
 for the U.S. Department of Energy under contract DE-AC05-76RL0 1830.

Notice: This computer software was prepared by Battelle Memorial Institute, 
hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
Department of Energy (DOE).  All rights in the computer software are reserved 
by DOE on behalf of the United States Government and the Contractor as 
provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
SOFTWARE.  This notice including this sentence must appear on any copies of 
this computer software.

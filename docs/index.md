# __<span style="color:#D57500">Protein Sequence Motif Extractor</span>__
The Protein Sequence Motif Extractor reads a fasta file or tab delimited file containing protein sequences, then looks for the specified motif in each protein sequence.

### Description
Results are stored in a new file containing the regions of the protein that contain the specified motif. The default output format is a new fasta file named _Motifs.fasta, but you can alternatively use /T to specify that a tab-delimited text file be created.

Use the /N switch to specify the number of residues to include before and after the matching motif. If the motif is close to the beginning or end of a protein then dashes will be used to assure that there are still the desired number of characters before and after the motif (this is a common requirement for alignment software).

By default, any modification symbols (or lower case letters, or numbers, etc.) are removed when writing out the motif and the flanking residues. To keep the modification symbols, use switch /K

Example command line to look for motif K# in file Proteins.fasta and includes 30 residues before and after the matching motif:

`ProteinSequenceMotifExtractor.exe Proteins.fasta /M:K# /N:30`

You can optionally use a regular expression (RegEx) for matching the motif. Use the /X switch to enable this mode. Example command line:

`ProteinSequenceMotifExtractor.exe DNA.txt /M:"GTA.{8,10}TAC" /N:30 /K /T /X`

### Downloads
* [Latest version](https://github.com/PNNL-Comp-Mass-Spec/Protein-Sequence-Motif-Extractor/releases/latest)
* [Source code on GitHub](https://github.com/PNNL-Comp-Mass-Spec/Protein-Sequence-Motif-Extractor)

### Acknowledgment

All publications that utilize this software should provide appropriate acknowledgement to PNNL and the Protein-Sequence-Motif-Extractor GitHub repository. However, if the software is extended or modified, then any subsequent publications should include a more extensive statement, as shown in the Readme file for the given application or on the website that more fully describes the application.

### Disclaimer

These programs are primarily designed to run on Windows machines. Please use them at your own risk. This material was prepared as an account of work sponsored by an agency of the United States Government. Neither the United States Government nor the United States Department of Energy, nor Battelle, nor any of their employees, makes any warranty, express or implied, or assumes any legal liability or responsibility for the accuracy, completeness, or usefulness or any information, apparatus, product, or process disclosed, or represents that its use would not infringe privately owned rights.

Portions of this research were supported by the NIH National Center for Research Resources (Grant RR018522), the W.R. Wiley Environmental Molecular Science Laboratory (a national scientific user facility sponsored by the U.S. Department of Energy's Office of Biological and Environmental Research and located at PNNL), and the National Institute of Allergy and Infectious Diseases (NIH/DHHS through interagency agreement Y1-AI-4894-01). PNNL is operated by Battelle Memorial Institute for the U.S. Department of Energy under contract DE-AC05-76RL0 1830.

We would like your feedback about the usefulness of the tools and information provided by the Resource. Your suggestions on how to increase their value to you will be appreciated. Please e-mail any comments to proteomics@pnl.gov

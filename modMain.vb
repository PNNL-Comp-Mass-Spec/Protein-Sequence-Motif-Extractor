Option Strict On

' This program can be used to read a fasta file or tab delimited file
' containing protein sequences. It then looks for the specified motif in each protein sequence
' and creates a new file containing the regions of the protein that contain the specified motif
'
' Example command line to look for motif K# in file Proteins.fasta and includes 30 residues 
'  to the left and the right of the matching motif:
'  ProteinSequenceMotifExtractor.exe /I:Proteins.fasta /M:K# /N:30
'
' By default, any mod symbols will be removed; to keep them, use /K

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started March 31, 2010

' E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
' Website: http://ncrr.pnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute, 
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
' Department of Energy (DOE).  All rights in the computer software are reserved 
' by DOE on behalf of the United States Government and the Contractor as 
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
' SOFTWARE.  This notice including this sentence must appear on any copies of 
' this computer software.

Module modMain

    Public Const PROGRAM_DATE As String = "September 18, 2010"

    Private mInputFilePath As String
    Private mAssumeFastaFile As Boolean

    Private mMotif As String
    Private mRegexMotif As Boolean

    Private mPrefixResidueCount As Integer
    Private mSuffixResidueCount As Integer
    Private mKeepModSymbols As Boolean

    Private mOutputMotifsAsDelimitedTextFile As Boolean

    Private mInputFileDelimiter As Char

    Private mOutputFolderName As String             ' Optional
    Private mParameterFilePath As String            ' Optional

    Private mOutputFolderAlternatePath As String                ' Optional
    Private mRecreateFolderHierarchyInAlternatePath As Boolean  ' Optional

    Private mRecurseFolders As Boolean
    Private mRecurseFoldersMaxLevels As Integer

    Private mLogMessagesToFile As Boolean
    Private mQuietMode As Boolean

    Private WithEvents mMotifExtractor As clsProteinSequenceMotifExtractor
    Private mLastProgressReportTime As System.DateTime
    Private mLastProgressReportValue As Integer

    Private Sub DisplayProgressPercent(ByVal intPercentComplete As Integer, ByVal blnAddCarriageReturn As Boolean)
        If blnAddCarriageReturn Then
            Console.WriteLine()
        End If
        If intPercentComplete > 100 Then intPercentComplete = 100
        Console.Write("Processing: " & intPercentComplete.ToString & "% ")
        If blnAddCarriageReturn Then
            Console.WriteLine()
        End If
    End Sub

    Public Function Main() As Integer
        ' Returns 0 if no error, error code if an error

        Dim intReturnCode As Integer
        Dim objParseCommandLine As New clsParseCommandLine
        Dim blnProceed As Boolean

        ' Initialize the options
        intReturnCode = 0
        mInputFilePath = String.Empty
        mAssumeFastaFile = False

        mMotif = "K#"
        mPrefixResidueCount = clsProteinSequenceMotifExtractor.DEFAULT_PREFIX_RESIDUE_COUNT
        mSuffixResidueCount = clsProteinSequenceMotifExtractor.DEFAULT_SUFFIX_RESIDUE_COUNT
        mKeepModSymbols = False

        mOutputMotifsAsDelimitedTextFile = False

        mInputFileDelimiter = ControlChars.Tab

        mOutputFolderName = String.Empty
        mParameterFilePath = String.Empty

        mRecurseFolders = False
        mRecurseFoldersMaxLevels = 0

        mQuietMode = False
        mLogMessagesToFile = False

        Try
            blnProceed = False
            If objParseCommandLine.ParseCommandLine Then
                If SetOptionsUsingCommandLineParameters(objParseCommandLine) Then blnProceed = True
            End If

            If Not blnProceed OrElse _
               objParseCommandLine.NeedToShowHelp OrElse _
               objParseCommandLine.ParameterCount + objParseCommandLine.NonSwitchParameterCount = 0 OrElse _
               mInputFilePath.Length = 0 Then
                ShowProgramHelp()
                intReturnCode = -1
            Else

                mMotifExtractor = New clsProteinSequenceMotifExtractor
                mMotifExtractor.ShowMessages = Not mQuietMode

                ' Note: the following settings will be overridden if mParameterFilePath points to a valid parameter file that has these settings defined
                With mMotifExtractor
                    .ShowMessages = Not mQuietMode
                    .LogMessagesToFile = mLogMessagesToFile

                    .Motif = mMotif
                    .RegexMotif = mRegexMotif

                    .PrefixResidueCount = mPrefixResidueCount
                    .SuffixResidueCount = mSuffixResidueCount
                    .KeepModSymbols = mKeepModSymbols

                    .OutputMotifsAsDelimitedTextFile = mOutputMotifsAsDelimitedTextFile

                    .AssumeFastaFile = mAssumeFastaFile
                    .InputFileDelimiter = mInputFileDelimiter
                    .DelimitedFileFormatCode = ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence
                End With

                If mRecurseFolders Then
                    If mMotifExtractor.ProcessFilesAndRecurseFolders(mInputFilePath, mOutputFolderName, mOutputFolderAlternatePath, mRecreateFolderHierarchyInAlternatePath, mParameterFilePath, mRecurseFoldersMaxLevels) Then
                        intReturnCode = 0
                    Else
                        intReturnCode = mMotifExtractor.ErrorCode
                    End If
                Else
                    If mMotifExtractor.ProcessFilesWildcard(mInputFilePath, mOutputFolderName, mParameterFilePath) Then
                        intReturnCode = 0
                    Else
                        intReturnCode = mMotifExtractor.ErrorCode
                        If intReturnCode <> 0 AndAlso Not mQuietMode Then
                            Console.WriteLine("Error while processing: " & mMotifExtractor.GetErrorMessage())
                        End If
                    End If
                End If

                DisplayProgressPercent(mLastProgressReportValue, True)
            End If

        Catch ex As Exception
            If mQuietMode Then
                Throw ex
            Else
                Console.WriteLine("Error occurred in modMain->Main: " & ControlChars.NewLine & ex.Message)
            End If
            intReturnCode = -1
        End Try

        Return intReturnCode

    End Function

    Private Function GetAppVersion() As String
        'Return System.Windows.Forms.Application.ProductVersion & " (" & PROGRAM_DATE & ")"

        Return System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString & " (" & PROGRAM_DATE & ")"
    End Function

    Private Function SetOptionsUsingCommandLineParameters(ByVal objParseCommandLine As clsParseCommandLine) As Boolean
        ' Returns True if no problems; otherwise, returns false

        Dim strValue As String = String.Empty
        Dim strValidParameters() As String = New String() {"I", "F", "M", "X", "N", "K", "AD", "O", "T", "P", "S", "A", "R", "L", "Q"}

        Try
            ' Make sure no invalid parameters are present
            If objParseCommandLine.InvalidParametersPresent(strValidParameters) Then
                Return False
            Else
                With objParseCommandLine
                    ' Query objParseCommandLine to see if various parameters are present
                    If .RetrieveValueForParameter("I", strValue) Then
                        mInputFilePath = strValue
                    ElseIf .NonSwitchParameterCount > 0 Then
                        mInputFilePath = .RetrieveNonSwitchParameter(0)
                    End If

                    If .RetrieveValueForParameter("F", strValue) Then mAssumeFastaFile = True

                    If .RetrieveValueForParameter("M", strValue) Then mMotif = strValue
                    If .RetrieveValueForParameter("X", strValue) Then mRegexMotif = True

                    If .RetrieveValueForParameter("N", strValue) Then
                        If Not Integer.TryParse(strValue, mPrefixResidueCount) Then
                            ShowErrorMessage("Error parsing number from the /N parameter; use /N:30 to specify " & _
                                             clsProteinSequenceMotifExtractor.DEFAULT_PREFIX_RESIDUE_COUNT & " residues be included before and after the matching motif")

                            mPrefixResidueCount = clsProteinSequenceMotifExtractor.DEFAULT_PREFIX_RESIDUE_COUNT
                        End If
                        mSuffixResidueCount = mPrefixResidueCount
                    End If

                    If .RetrieveValueForParameter("K", strValue) Then mKeepModSymbols = True

                    If .RetrieveValueForParameter("AD", strValue) Then mInputFileDelimiter = strValue.Chars(0)

                    If .RetrieveValueForParameter("O", strValue) Then mOutputFolderName = strValue
                    If .RetrieveValueForParameter("T", strValue) Then mOutputMotifsAsDelimitedTextFile = True

                    If .RetrieveValueForParameter("P", strValue) Then mParameterFilePath = strValue

                    If .RetrieveValueForParameter("S", strValue) Then
                        mRecurseFolders = True
                        If Not Integer.TryParse(strValue, mRecurseFoldersMaxLevels) Then
                            mRecurseFoldersMaxLevels = 0
                        End If
                    End If
                    If .RetrieveValueForParameter("A", strValue) Then mOutputFolderAlternatePath = strValue
                    If .RetrieveValueForParameter("R", strValue) Then mRecreateFolderHierarchyInAlternatePath = True

                    If .RetrieveValueForParameter("L", strValue) Then mLogMessagesToFile = True
                    If .RetrieveValueForParameter("Q", strValue) Then mQuietMode = True

                End With

                Return True
            End If

        Catch ex As Exception
            If mQuietMode Then
                Throw New System.Exception("Error parsing the command line parameters", ex)
            Else
                Console.WriteLine("Error parsing the command line parameters: " & ControlChars.NewLine & ex.Message)
            End If
        End Try

    End Function

    Private Sub ShowErrorMessage(ByVal strMessage As String)
        Dim strSeparator As String = "------------------------------------------------------------------------------"

        Console.WriteLine()
        Console.WriteLine(strSeparator)
        Console.WriteLine(strMessage)
        Console.WriteLine(strSeparator)
        Console.WriteLine()

    End Sub

    Private Sub ShowProgramHelp()

        Try

            Console.WriteLine("This program reads a fasta file or tab delimited file containing protein sequences. " & _
                              "It then looks for the specified motif in each protein sequence and creates a new file " & _
                              "containing the regions of the protein that contain the specified motif.")
            Console.WriteLine()

            Console.WriteLine("Program syntax:")
            Console.WriteLine(System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) & _
                              " /I:SourceFastaOrTextFile [/O:OutputFolderPath] [/T]")
            Console.WriteLine(" [/F] [/M:Motif] [/X] [/N:NumberOfFlankingResidues] [/K]")
            Console.WriteLine(" [/AD:AlternateDelimeter] [/P:ParameterFilePath] ")
            Console.WriteLine(" [/S:[MaxLevel]] [/A:AlternateOutputFolderPath] [/R] [/L] [/Q]")
            Console.WriteLine()
            Console.WriteLine("The input file path can contain the wildcard character * and should point to a fasta file or tab-delimited text file (with columns Protein Name, Description, and Sequence). " & _
                              "The output folder switch is optional.  If omitted, the output file will be created in the same folder as the input file. " & _
                              "By default, the output file will be a .Fasta file; use /T to instead create a tab-delimited text file. " & _
                              "Use /F to indicate that the input file is a fasta file.  If /F is not used, then the format will be assumed to be fasta only if the file contains .fasta in the name.")
            Console.WriteLine()

            Console.WriteLine("Use /M to define the motif to look for (examples are /M:K# to find 'K#' or /M:KCLK to find 'KCLK').  Use /X to specify that the Motif is a Regular Expression (RegEx) to match, for example:")
            Console.WriteLine("/M:N[^P][ST][^P] /X to find N, then any residue except P, then S or T, then any residue except P.")
            Console.WriteLine()

            Console.WriteLine("/N specifies the number of residues to include before or after the matching motif (default " & clsProteinSequenceMotifExtractor.DEFAULT_PREFIX_RESIDUE_COUNT & ").  /K specifies to not remove (thus Keep) any modification symbols when creating the output file.")
            Console.WriteLine()
            Console.WriteLine("Use /AD to specify a delimiter other than the Tab character (not applicable for fasta files). The parameter file path is optional.  If included, it should point to a valid XML parameter file.")
            Console.WriteLine()

            Console.WriteLine("Use /S to process all valid files in the input folder and subfolders. Include a number after /S (like /S:2) to limit the level of subfolders to examine. " & _
                              "When using /S, you can redirect the output of the results using /A. " & _
                              "When using /S, you can use /R to re-create the input folder hierarchy in the alternate output folder (if defined).")

            Console.WriteLine("Use /L to log messages to a file.  If /Q is used, then no messages will be displayed at the console.")
            Console.WriteLine()

            Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2010")
            Console.WriteLine("Version: " & GetAppVersion())
            Console.WriteLine()

            Console.WriteLine("E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com")
            Console.WriteLine("Website: http://ncrr.pnl.gov/ or http://omics.pnl.gov")
            Console.WriteLine()

            ' Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
            System.Threading.Thread.Sleep(750)

        Catch ex As Exception
            Console.WriteLine("Error displaying the program syntax: " & ex.Message)
        End Try

    End Sub

    Private Sub mMotifExtractor_ProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single) Handles mMotifExtractor.ProgressChanged
        Const PERCENT_REPORT_INTERVAL As Integer = 25
        Const PROGRESS_DOT_INTERVAL_MSEC As Integer = 250

        If percentComplete >= mLastProgressReportValue Then
            If mLastProgressReportValue > 0 Then
                Console.WriteLine()
            End If
            DisplayProgressPercent(mLastProgressReportValue, False)
            mLastProgressReportValue += PERCENT_REPORT_INTERVAL
            mLastProgressReportTime = DateTime.Now
        Else
            If DateTime.Now.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC Then
                mLastProgressReportTime = DateTime.Now
                Console.Write(".")
            End If
        End If
    End Sub

    Private Sub mMotifExtractor_ProgressReset() Handles mMotifExtractor.ProgressReset
        mLastProgressReportTime = DateTime.Now
        mLastProgressReportValue = 0
    End Sub
End Module

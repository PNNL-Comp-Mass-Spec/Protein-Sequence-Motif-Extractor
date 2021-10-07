Option Strict On

' This class will read a protein fasta file or tab delimited file
' containing protein sequences. It then looks for the specified motif in each protein sequence
' and creates a new file containing the regions of the protein that contain the specified motif
'
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
'
' Started March 31, 2010

Public Class clsProteinSequenceMotifExtractor
    Inherits clsProcessFilesBaseClass

    Public Sub New()
        MyBase.mFileDate = "November 1, 2013"
        InitializeLocalVariables()
    End Sub

#Region "Constants and Enums"

    Public Const XML_SECTION_OPTIONS As String = "ProteinSequenceMotifExtractorOptions"

    Public Const DEFAULT_PREFIX_RESIDUE_COUNT As Integer = 30
    Public Const DEFAULT_SUFFIX_RESIDUE_COUNT As Integer = 30

    ' Error codes specialized for this class
    Public Enum eMotifExtractorErrorCodes
        NoError = 0
        ErrorReadingInputFile = 1
        ErrorCreatingMotifsOutputFile = 2
        ErrorWritingOutputFile = 4
        InvalidMotif = 8
        UnspecifiedError = -1
    End Enum

    Public Enum DelimiterCharConstants
        Space = 0
        Tab = 1
        Comma = 2
    End Enum

#End Region

#Region "Structures"

    Public Structure udtProteinInfoType
        Public Name As String
        Public Description As String
        Public Sequence As String
        Public UniqueSequenceID As Integer              ' Only applies if reading a delimited text file containing peptide sequences and UniqueSequenceID values
    End Structure
#End Region

#Region "Classwide Variables"
    Private mAssumeDelimitedFile As Boolean
    Private mAssumeFastaFile As Boolean
    Private mInputFileDelimiter As Char                             ' Only used for delimited protein input files, not for fasta files
    Private mDelimitedInputFileFormatCode As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode

    Private mMotif As String
    Private mRegexMotif As Boolean

    Private mPrefixResidueCount As Integer
    Private mSuffixResidueCount As Integer
    Private mKeepModSymbols As Boolean

    Private mOutputMotifsAsDelimitedTextFile As Boolean                         ' If False, then the output file will be a .Fasta file; if True, then a tab-delimited file

    Private mInputFileProteinsProcessed As Integer
    Private mInputFileLinesRead As Integer
    Private mInputFileLineSkipCount As Integer

    Private mParsedFileIsFastaFile As Boolean

    Public FastaFileOptions As FastaFileOptionsClass

    Private mLocalErrorCode As eMotifExtractorErrorCodes
#End Region

#Region "Processing Options Interface Functions"
    Public Property AssumeDelimitedFile() As Boolean
        Get
            Return mAssumeDelimitedFile
        End Get
        Set(ByVal Value As Boolean)
            mAssumeDelimitedFile = Value
        End Set
    End Property

    Public Property AssumeFastaFile() As Boolean
        Get
            Return mAssumeFastaFile
        End Get
        Set(ByVal Value As Boolean)
            mAssumeFastaFile = Value
        End Set
    End Property

    Public Property DelimitedFileFormatCode() As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode
        Get
            Return mDelimitedInputFileFormatCode
        End Get
        Set(ByVal Value As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode)
            mDelimitedInputFileFormatCode = Value
        End Set
    End Property

    Public Property InputFileDelimiter() As Char
        Get
            Return mInputFileDelimiter
        End Get
        Set(ByVal Value As Char)
            If Not Value = Nothing Then
                mInputFileDelimiter = Value
            End If
        End Set
    End Property

    Public ReadOnly Property InputFileProteinsProcessed() As Integer
        Get
            Return mInputFileProteinsProcessed
        End Get
    End Property

    Public ReadOnly Property InputFileLinesRead() As Integer
        Get
            Return mInputFileLinesRead
        End Get
    End Property

    Public ReadOnly Property InputFileLineSkipCount() As Integer
        Get
            Return mInputFileLineSkipCount
        End Get
    End Property

    Public Property KeepModSymbols() As Boolean
        Get
            Return mKeepModSymbols
        End Get
        Set(ByVal value As Boolean)
            mKeepModSymbols = value
        End Set
    End Property

    Public ReadOnly Property LocalErrorCode() As eMotifExtractorErrorCodes
        Get
            Return mLocalErrorCode
        End Get
    End Property

    Public Property Motif() As String
        Get
            Return mMotif
        End Get
        Set(ByVal value As String)
            If Not value Is Nothing AndAlso value.Length > 0 Then
                mMotif = value
            End If
        End Set
    End Property

    Public Property OutputMotifsAsDelimitedTextFile() As Boolean
        Get
            Return mOutputMotifsAsDelimitedTextFile
        End Get
        Set(ByVal value As Boolean)
            mOutputMotifsAsDelimitedTextFile = value
        End Set
    End Property

    Public ReadOnly Property ParsedFileIsFastaFile() As Boolean
        Get
            Return mParsedFileIsFastaFile
        End Get
    End Property

    Public Property PrefixResidueCount() As Integer
        Get
            Return mPrefixResidueCount
        End Get
        Set(ByVal value As Integer)
            If value < 0 Then value = 0
            mPrefixResidueCount = value
        End Set
    End Property

    Public Property RegexMotif() As Boolean
        Get
            Return mRegexMotif
        End Get
        Set(ByVal value As Boolean)
            mRegexMotif = value
        End Set
    End Property

    Public Property SuffixResidueCount() As Integer
        Get
            Return mSuffixResidueCount
        End Get
        Set(ByVal value As Integer)
            If value < 0 Then value = 0
            mSuffixResidueCount = value
        End Set
    End Property

#End Region

    ''' <summary>
    ''' Search the Sequence in udtProtein for each motif in strMotif or reMotif
    ''' </summary>
    ''' <param name="swMotifsOutputFile">File to write out the results</param>
    ''' <param name="blnOutputAsDelimitedText">If True, then writing to a delimited text file; otherwise, writing to a .Fasta file</param>
    ''' <param name="udtProtein">Protein info</param>
    ''' <param name="strMotif">Text to find; if blank, then will use reMotif</param>
    ''' <param name="reMotif">Regex to use (only used if strMotif is blank)</param>
    ''' <remarks></remarks>
    Protected Sub FindMatchingMotifs(ByRef swMotifsOutputFile As IO.StreamWriter,
                                     ByVal blnOutputAsDelimitedText As Boolean,
                                     ByRef udtProtein As udtProteinInfoType,
                                     ByVal strMotif As String,
                                     ByRef reMotif As Text.RegularExpressions.Regex)

        Static sbResidues As New Text.StringBuilder

        Dim blnUseRegex As Boolean
        Dim strProteinSequence As String
        Dim intProteinSequenceLength As Integer

        Dim reMatch As Text.RegularExpressions.Match

        Dim intIndex As Integer
        Dim intProteinSeqIndex As Integer
        Dim intStartIndex As Integer

        Dim intMatchIndex As Integer
        Dim intMatchIndexPrevious As Integer
        Dim strMatchResidues As String

        Dim intPrefixCount As Integer
        Dim intSuffixCount As Integer

        Dim intResidueLoc As Integer

        Dim blnMatchFound As Boolean

        Try
            strProteinSequence = udtProtein.Sequence
            intProteinSequenceLength = strProteinSequence.Length
            intResidueLoc = 0
            strMatchResidues = String.Empty

            If strMotif Is Nothing OrElse strMotif.Length = 0 Then
                blnUseRegex = True
            Else
                blnUseRegex = False
            End If

            intStartIndex = 0
            intMatchIndexPrevious = -1

            Do
                If blnUseRegex Then
                    ' Using reMotif to look for matches
                    reMatch = reMotif.Match(strProteinSequence, intStartIndex)

                    If reMatch.Success Then
                        intMatchIndex = reMatch.Index
                        strMatchResidues = reMatch.Value
                    Else
                        intMatchIndex = -1
                    End If
                Else
                    ' Using strMotif to look for matches
                    intMatchIndex = strProteinSequence.IndexOf(strMotif, intStartIndex)
                    If intMatchIndex >= 0 Then
                        strMatchResidues = strProteinSequence.Substring(intMatchIndex, strMotif.Length)
                    End If
                End If

                If intMatchIndex >= 0 Then
                    blnMatchFound = True

                    ' Determine the residue number that the match occurs at
                    ' Count the number of upper case letters up to this point
                    ' We can start counting from index intStartIndexPrevious
                    For intIndex = intMatchIndexPrevious + 1 To intMatchIndex
                        If Char.IsUpper(strProteinSequence.Chars(intIndex)) Then
                            intResidueLoc += 1
                        End If
                    Next

                    ' Clear the stringbuilder object
                    sbResidues.Length = 0

                    If mKeepModSymbols Then
                        sbResidues.Append(strMatchResidues)
                    Else
                        For intIndex = 0 To strMatchResidues.Length - 1
                            If Char.IsUpper(strMatchResidues.Chars(intIndex)) Then
                                sbResidues.Append(strMatchResidues.Chars(intIndex))
                            End If
                        Next
                    End If


                    ' Step backward through strProteinSequence to find the previous mPrefixResidueCount residues (not counting mod symbols)
                    intPrefixCount = 0
                    intProteinSeqIndex = intMatchIndex - 1

                    Do While intPrefixCount < mPrefixResidueCount
                        If intProteinSeqIndex < 0 Then
                            ' We have moved before the first residue of the protein
                            ' Insert a dash to make sure we have at least 30 characters
                            sbResidues.Insert(0, "-", 1)
                            intPrefixCount += 1
                        Else

                            If Char.IsUpper(strProteinSequence.Chars(intProteinSeqIndex)) Then
                                ' Uppercase letter; assume it is an amino acid
                                sbResidues.Insert(0, strProteinSequence.Chars(intProteinSeqIndex), 1)
                                intPrefixCount += 1
                            Else
                                ' This is not an upper case letter (could be a symbol, lowercase, a number, whitespace, a dash, etc.)
                                ' Only keep it if mKeepModSymbols is True
                                If mKeepModSymbols Then
                                    sbResidues.Insert(0, strProteinSequence.Chars(intProteinSeqIndex), 1)
                                End If
                            End If
                            intProteinSeqIndex -= 1
                        End If
                    Loop


                    ' Step forward through strProteinSequence to find the next mSuffixResidueCount residues (not counting mod symbols)
                    intSuffixCount = 0
                    intProteinSeqIndex = intMatchIndex + strMatchResidues.Length

                    Do While intSuffixCount < mSuffixResidueCount
                        If intProteinSeqIndex >= intProteinSequenceLength Then
                            ' We have after the last residue of the protein
                            ' Append a dash to make sure we have at least 30 characters
                            sbResidues.Append("-")
                            intSuffixCount += 1
                        Else

                            If Char.IsUpper(strProteinSequence.Chars(intProteinSeqIndex)) Then
                                ' Uppercase letter; assume it is an amino acid
                                sbResidues.Append(strProteinSequence.Chars(intProteinSeqIndex))
                                intSuffixCount += 1
                            Else
                                ' This is not an upper case letter (could be a symbol, lowercase, a number, whitespace, a dash, etc.)
                                ' Only keep it if mKeepModSymbols is True
                                If mKeepModSymbols Then
                                    sbResidues.Append(strProteinSequence.Chars(intProteinSeqIndex))
                                End If
                            End If
                            intProteinSeqIndex += 1
                        End If
                    Loop
                    intStartIndex = intMatchIndex + 1

                    ' Write out the motif along with the prefix and suffix residues
                    If blnOutputAsDelimitedText Then
                        ' Tab-delimited text
                        swMotifsOutputFile.WriteLine(udtProtein.Name & ControlChars.Tab &
                                                     strMatchResidues & ControlChars.Tab &
                                                     intResidueLoc & ControlChars.Tab &
                                                     sbResidues.ToString)
                    Else
                        ' Fasta format
                        swMotifsOutputFile.WriteLine(FastaFileOptions.ProteinLineStartChar & udtProtein.Name &
                                                     FastaFileOptions.ProteinLineAccessionEndChar & udtProtein.Description)
                        swMotifsOutputFile.WriteLine(sbResidues.ToString)
                    End If

                    intMatchIndexPrevious = intMatchIndex
                Else
                    blnMatchFound = False
                End If

            Loop While blnMatchFound

        Catch ex As Exception
            HandleException("Error in FindMatchingMotifs", ex)
        End Try

    End Sub

    Public Overrides Function GetDefaultExtensionsToParse() As String()
        Dim strExtensionsToParse(1) As String

        strExtensionsToParse(0) = ".fasta"
        strExtensionsToParse(1) = ".txt"

        Return strExtensionsToParse

    End Function

    Public Overrides Function GetErrorMessage() As String
        ' Returns "" if no error

        Dim strErrorMessage As String

        If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError Or
           MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError Then
            Select Case mLocalErrorCode
                Case eMotifExtractorErrorCodes.NoError
                    strErrorMessage = ""

                Case eMotifExtractorErrorCodes.ErrorReadingInputFile
                    strErrorMessage = "Error reading input file"

                Case eMotifExtractorErrorCodes.ErrorCreatingMotifsOutputFile
                    strErrorMessage = "Error creating the output file"

                Case eMotifExtractorErrorCodes.ErrorWritingOutputFile
                    strErrorMessage = "Error writing to the output file"

                Case eMotifExtractorErrorCodes.InvalidMotif
                    strErrorMessage = "Invalid Motif"
                    If Not mMotif Is Nothing Then
                        If mMotif.Length = 0 Then
                            strErrorMessage &= ": Motif to match is not defined"
                        Else
                            If mRegexMotif Then
                                strErrorMessage &= ": RegEx '" & mMotif & "'"
                            Else
                                strErrorMessage &= ": " & mMotif
                            End If
                        End If
                    End If

                Case eMotifExtractorErrorCodes.UnspecifiedError
                    strErrorMessage = "Unspecified localized error"
                Case Else
                    ' This shouldn't happen
                    strErrorMessage = "Unknown error state"
            End Select
        Else
            strErrorMessage = MyBase.GetBaseClassErrorMessage()
        End If

        Return strErrorMessage

    End Function

    Private Sub InitializeLocalVariables()
        mLocalErrorCode = eMotifExtractorErrorCodes.NoError

        mAssumeDelimitedFile = False
        mAssumeFastaFile = False
        mInputFileDelimiter = ControlChars.Tab
        mDelimitedInputFileFormatCode = ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence

        mMotif = "K#"
        mRegexMotif = False

        mPrefixResidueCount = DEFAULT_PREFIX_RESIDUE_COUNT
        mSuffixResidueCount = DEFAULT_PREFIX_RESIDUE_COUNT
        mKeepModSymbols = False

        mOutputMotifsAsDelimitedTextFile = False

        mInputFileProteinsProcessed = 0
        mInputFileLinesRead = 0
        mInputFileLineSkipCount = 0

        FastaFileOptions = New FastaFileOptionsClass

    End Sub

    Public Shared Function IsFastaFile(ByVal strFilePath As String) As Boolean
        ' Examines the file's extension and true if it ends in .fasta

        If IO.Path.GetExtension(strFilePath).ToLower = ".fasta" Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function LoadParameterFileSettings(ByVal strParameterFilePath As String) As Boolean

        Dim objSettingsFile As New XmlSettingsFileAccessor

        Dim intDelimeterIndex As Integer

        Try

            If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
                ' No parameter file specified; nothing to load
                Return True
            End If

            If Not IO.File.Exists(strParameterFilePath) Then
                ' See if strParameterFilePath points to a file in the same directory as the application
                strParameterFilePath = IO.Path.Combine(IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location), IO.Path.GetFileName(strParameterFilePath))
                If Not IO.File.Exists(strParameterFilePath) Then
                    MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.ParameterFileNotFound)
                    Return False
                End If
            End If

            If objSettingsFile.LoadSettings(strParameterFilePath) Then
                If Not objSettingsFile.SectionPresent(XML_SECTION_OPTIONS) Then
                    ShowErrorMessage("The node '<section name=""" & XML_SECTION_OPTIONS & """> was not found in the parameter file: " & strParameterFilePath)
                    MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
                    Return False
                Else

                    intDelimeterIndex = DelimiterCharConstants.Tab
                    intDelimeterIndex = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "InputFileColumnDelimiterIndex", intDelimeterIndex)

                    Me.InputFileDelimiter = LookupColumnDelimiterChar(intDelimeterIndex, ControlChars.Tab, Me.InputFileDelimiter)

                    Me.DelimitedFileFormatCode = CType(objSettingsFile.GetParam(XML_SECTION_OPTIONS, "InputFileColumnOrdering", CInt(Me.DelimitedFileFormatCode)), ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode)


                    Me.Motif = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "Motif", Me.Motif)
                    Me.RegexMotif = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "RegexMotif", Me.RegexMotif)

                    Me.PrefixResidueCount = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "PrefixResidueCount", Me.PrefixResidueCount)
                    Me.SuffixResidueCount = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "SuffixResidueCount", Me.SuffixResidueCount)

                    Me.KeepModSymbols = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "KeepModSymbols", Me.KeepModSymbols)

                    Me.OutputMotifsAsDelimitedTextFile = objSettingsFile.GetParam(XML_SECTION_OPTIONS, "OutputMotifsAsDelimitedTextFile", Me.OutputMotifsAsDelimitedTextFile)
                End If
            End If

        Catch ex As Exception
            HandleException("Error in LoadParameterFileSettings", ex)
            Return False
        End Try

        Return True

    End Function

    Public Shared Function LookupColumnDelimiterChar(ByVal intDelimiterIndex As Integer, ByVal strCustomDelimiter As String, ByVal strDefaultDelimiter As Char) As Char

        Dim strDelimiter As String

        Select Case intDelimiterIndex
            Case DelimiterCharConstants.Space
                strDelimiter = " "
            Case DelimiterCharConstants.Tab
                strDelimiter = ControlChars.Tab
            Case DelimiterCharConstants.Comma
                strDelimiter = ","
            Case Else
                ' Includes DelimiterCharConstants.Other
                strDelimiter = String.Copy(strCustomDelimiter)
        End Select

        If strDelimiter Is Nothing OrElse strDelimiter.Length = 0 Then
            strDelimiter = String.Copy(strDefaultDelimiter)
        End If

        Try
            Return strDelimiter.Chars(0)
        Catch ex As Exception
            Return ControlChars.Tab
        End Try

    End Function

    Protected Function OpenInputFile(ByVal strInputFilePath As String,
                                     ByVal strOutputFolderPath As String,
                                     ByVal strOutputFileNameBaseOverride As String,
                                     ByRef objProteinFileReader As ProteinFileReader.ProteinFileReaderBaseClass,
                                     ByRef blnUseUniqueIDValuesFromInputFile As Boolean,
                                     ByRef strOutputMotifsFilePath As String) As Boolean

        Dim blnSuccess As Boolean
        Dim strOutputFileName As String

        Dim objFastaFileReader As ProteinFileReader.FastaFileReader
        Dim objDelimitedFileReader As ProteinFileReader.DelimitedFileReader

        Try

            If strInputFilePath Is Nothing OrElse strInputFilePath.Length = 0 Then
                SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
            Else

                ' Determine whether the input file is a .Fasta file or a tab delimited text ifle
                If mAssumeFastaFile OrElse IsFastaFile(strInputFilePath) Then
                    If mAssumeDelimitedFile Then
                        mParsedFileIsFastaFile = False
                    Else
                        mParsedFileIsFastaFile = True
                    End If
                Else
                    mParsedFileIsFastaFile = False
                End If

                ' Verify that the input file exists
                If Not IO.File.Exists(strInputFilePath) Then
                    MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
                    blnSuccess = False
                    Exit Try
                End If

                ' Instantiate the protein file reader object
                If mParsedFileIsFastaFile Then
                    objFastaFileReader = New ProteinFileReader.FastaFileReader
                    With objFastaFileReader
                        .ProteinLineStartChar = FastaFileOptions.ProteinLineStartChar
                        .ProteinLineAccessionEndChar = FastaFileOptions.ProteinLineAccessionEndChar
                    End With
                    objProteinFileReader = objFastaFileReader

                    blnUseUniqueIDValuesFromInputFile = False
                Else
                    objDelimitedFileReader = New ProteinFileReader.DelimitedFileReader
                    With objDelimitedFileReader
                        .Delimiter = mInputFileDelimiter
                        .DelimitedFileFormatCode = mDelimitedInputFileFormatCode
                    End With
                    objProteinFileReader = objDelimitedFileReader

                    If mDelimitedInputFileFormatCode = ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID Or
                       mDelimitedInputFileFormatCode = ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode.UniqueID_Sequence Then
                        blnUseUniqueIDValuesFromInputFile = True
                    Else
                        blnUseUniqueIDValuesFromInputFile = False
                    End If
                End If

                ' Define the output file name
                strOutputFileName = String.Empty
                If Not strOutputFileNameBaseOverride Is Nothing AndAlso strOutputFileNameBaseOverride.Length > 0 Then
                    If IO.Path.HasExtension(strOutputFileNameBaseOverride) Then
                        strOutputFileName = String.Copy(strOutputFileNameBaseOverride)

                        If mOutputMotifsAsDelimitedTextFile Then
                            If IO.Path.GetExtension(strOutputFileName).Length > 4 Then
                                strOutputFileName &= ".txt"
                            End If
                        Else
                            If IO.Path.GetExtension(strOutputFileName).ToLower <> ".fasta" Then
                                strOutputFileName &= ".fasta"
                            End If
                        End If
                    Else
                        If mOutputMotifsAsDelimitedTextFile Then
                            strOutputFileName = strOutputFileNameBaseOverride & "_Motifs.txt"
                        Else
                            strOutputFileName = strOutputFileNameBaseOverride & "_Motifs.fasta"
                        End If
                    End If
                End If

                If strOutputFileName.Length = 0 Then
                    ' Output file name is not defined; auto-define it
                    If mOutputMotifsAsDelimitedTextFile Then
                        strOutputFileName = IO.Path.GetFileNameWithoutExtension(strInputFilePath) & "_Motifs.txt"
                    Else
                        strOutputFileName = IO.Path.GetFileNameWithoutExtension(strInputFilePath) & "_Motifs.fasta"
                    End If
                End If

                ' Make sure the output file isn't the same as the input file
                If IO.Path.GetFileName(strInputFilePath).ToLower = IO.Path.GetFileName(strOutputFileName).ToLower Then
                    strOutputFileName = IO.Path.GetFileNameWithoutExtension(strOutputFileName) & "_new" & IO.Path.GetExtension(strOutputFileName)
                End If

                If strOutputFolderPath Is Nothing OrElse strOutputFolderPath.Length = 0 Then
                    ' This code likely won't be reached since CleanupFilePaths() should have already initialized strOutputFolderPath
                    Dim fiInputFile As IO.FileInfo
                    fiInputFile = New IO.FileInfo(strInputFilePath)

                    strOutputFolderPath = fiInputFile.Directory.FullName
                End If

                ' Define the full path to output file
                strOutputMotifsFilePath = IO.Path.Combine(strOutputFolderPath, strOutputFileName)

                blnSuccess = True
            End If

        Catch ex As Exception
            HandleException("OpenInputFile", ex)
            SetLocalErrorCode(eMotifExtractorErrorCodes.ErrorReadingInputFile)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Public Function ParseProteinFile(ByVal strProteinInputFilePath As String, ByVal strOutputFolderPath As String) As Boolean
        Return ParseProteinFile(strProteinInputFilePath, strOutputFolderPath, String.Empty)
    End Function

    ''' <summary>
    ''' Search for mMotif in file strProteinInputFilePath
    ''' The output file will be created in strOutputFolderPath (or the same folder as strProteinInputFilePath if strOutputFolderPath is empty)
    ''' </summary>
    ''' <param name="strProteinInputFilePath"></param>
    ''' <param name="strOutputFolderPath"></param>
    ''' <param name="strOutputFileNameBaseOverride"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ParseProteinFile(ByVal strProteinInputFilePath As String,
                                     ByVal strOutputFolderPath As String,
                                     ByVal strOutputFileNameBaseOverride As String) As Boolean
        ' If strOutputFileNameBaseOverride is defined, then uses that name for the protein output filename rather than auto-defining the name

        Dim objProteinFileReader As ProteinFileReader.ProteinFileReaderBaseClass = Nothing

        Dim swMotifsOutputFile As IO.StreamWriter = Nothing

        Dim strLineOut As String = String.Empty

        Dim strOutputMotifsFilePath As String = String.Empty

        Dim blnSuccess As Boolean = False
        Dim blnInputProteinFound As Boolean
        Dim blnUseUniqueIDValuesFromInputFile As Boolean

        Dim udtProtein As udtProteinInfoType
        Dim strMotif As String
        Dim reMotif As Text.RegularExpressions.Regex = Nothing

        Dim strMessage As String

        Try
            ' Open the input file and define the output file path
            blnSuccess = OpenInputFile(strProteinInputFilePath,
                            strOutputFolderPath,
                            strOutputFileNameBaseOverride,
                            objProteinFileReader,
                            blnUseUniqueIDValuesFromInputFile,
                            strOutputMotifsFilePath)

            ' Abort processing if we couldn't successfully open the input file
            If Not blnSuccess Then Return False


            ' Create the output file
            Try
                ' This will cause an error if the input file is the same as the output file
                swMotifsOutputFile = New IO.StreamWriter(New IO.FileStream(strOutputMotifsFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))
                blnSuccess = True
            Catch ex As Exception
                SetLocalErrorCode(eMotifExtractorErrorCodes.ErrorCreatingMotifsOutputFile)
                blnSuccess = False
            End Try
            If Not blnSuccess Then Return False


            ' Attempt to open the input file
            If Not objProteinFileReader.OpenFile(strProteinInputFilePath) Then
                SetLocalErrorCode(eMotifExtractorErrorCodes.ErrorReadingInputFile)
                Return False
            End If

            If mMotif Is Nothing Then mMotif = String.Empty
            If mMotif.Length = 0 Then
                SetLocalErrorCode(eMotifExtractorErrorCodes.InvalidMotif)
                Return False
            End If

            If mRegexMotif Then
                ' Initialize the Motif Regex
                ' Note that we are creating a case-sensitive RegEx
                Try
                    reMotif = New Text.RegularExpressions.Regex(mMotif, Text.RegularExpressions.RegexOptions.Compiled Or Text.RegularExpressions.RegexOptions.Singleline)
                Catch ex As Exception
                    HandleException("Error initializing the RegEx-based Motif using " & mMotif, ex)
                    SetLocalErrorCode(eMotifExtractorErrorCodes.InvalidMotif)
                End Try

                ' Set strMotif to an empty string so that FindMatchingMotifs() knows to use reMotif
                strMotif = String.Empty
            Else
                strMotif = String.Copy(mMotif)
            End If

            UpdateProgress("Parsing protein input file", 0)

            If mOutputMotifsAsDelimitedTextFile Then
                ' Write the header line to the output file
                strLineOut = "Protein" & ControlChars.Tab & "Motif" & ControlChars.Tab & "ResidueStartLoc" & ControlChars.Tab & "Residues"
                swMotifsOutputFile.WriteLine(strLineOut)
            End If

            ' Read each protein in the input file and process appropriately
            mInputFileProteinsProcessed = 0
            mInputFileLineSkipCount = 0
            mInputFileLinesRead = 0
            Do
                blnInputProteinFound = objProteinFileReader.ReadNextProteinEntry()
                mInputFileLineSkipCount += objProteinFileReader.LineSkipCount

                If blnInputProteinFound Then
                    mInputFileProteinsProcessed += 1
                    mInputFileLinesRead = objProteinFileReader.LinesRead

                    With udtProtein
                        .Name = objProteinFileReader.ProteinName
                        .Description = objProteinFileReader.ProteinDescription

                        .Sequence = objProteinFileReader.ProteinSequence

                        If blnUseUniqueIDValuesFromInputFile Then
                            .UniqueSequenceID = objProteinFileReader.EntryUniqueID
                        Else
                            .UniqueSequenceID = 0
                        End If

                    End With

                    FindMatchingMotifs(swMotifsOutputFile, mOutputMotifsAsDelimitedTextFile, udtProtein, strMotif, reMotif)

                    UpdateProgress(objProteinFileReader.PercentFileProcessed())

                End If
            Loop While blnInputProteinFound

            ' Close the input file
            objProteinFileReader.CloseFile()

            ' Close the output file
            If Not swMotifsOutputFile Is Nothing Then
                swMotifsOutputFile.Close()
            End If


            UpdateProgress("Done: Processed " & mInputFileProteinsProcessed.ToString("###,##0") & " proteins (" & mInputFileLinesRead.ToString("###,###,##0") & " lines)", 100)

            If mInputFileLineSkipCount > 0 Then
                strMessage = "Note that " & mInputFileLineSkipCount.ToString("###,##0") & " lines were skipped in the input file due to having an unexpected format. "
                If mParsedFileIsFastaFile Then
                    strMessage &= "This is an unexpected error for fasta files."
                Else
                    strMessage &= "Make sure that " & mDelimitedInputFileFormatCode.ToString & " is the appropriate format for this file (see the File Format Options tab)."
                End If

                LogMessage(strMessage)
            End If

            blnSuccess = True

        Catch ex As Exception
            HandleException("Error in ParseProteinFile", ex)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    ' Main processing function -- Calls ParseProteinFile
    Public Overloads Overrides Function ProcessFile(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String, ByVal blnResetErrorCode As Boolean) As Boolean
        ' Returns True if success, False if failure

        Dim ioFile As IO.FileInfo
        Dim strInputFilePathFull As String

        Dim blnSuccess As Boolean

        If blnResetErrorCode Then
            SetLocalErrorCode(eMotifExtractorErrorCodes.NoError)
        End If

        If Not LoadParameterFileSettings(strParameterFilePath) Then
            ShowErrorMessage("Parameter file load error: " & strParameterFilePath)

            If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError Then
                MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
            End If
            Return False
        End If

        Try
            If strInputFilePath Is Nothing OrElse strInputFilePath.Length = 0 Then
                ShowMessage("Input file name is empty")
                MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
            Else

                Console.WriteLine()
                Console.WriteLine("Parsing " & IO.Path.GetFileName(strInputFilePath))

                ' Note that CleanupFilePaths() will update mOutputFolderPath, which is used by LogMessage()
                If Not CleanupFilePaths(strInputFilePath, strOutputFolderPath) Then
                    MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.FilePathError)
                Else

                    MyBase.ResetProgress()

                    Try
                        ' Obtain the full path to the input file
                        ioFile = New IO.FileInfo(strInputFilePath)
                        strInputFilePathFull = ioFile.FullName

                        blnSuccess = ParseProteinFile(strInputFilePathFull, strOutputFolderPath)


                        If blnSuccess Then
                            ShowMessage(String.Empty, False)
                        Else
                            SetLocalErrorCode(eMotifExtractorErrorCodes.UnspecifiedError)
                            ShowErrorMessage("Error")
                        End If

                    Catch ex As Exception
                        HandleException("Error calling ParseProteinFile", ex)
                    End Try
                End If
            End If
        Catch ex As Exception
            HandleException("Error in ProcessFile", ex)
        End Try

        Return blnSuccess

    End Function

    Private Sub SetLocalErrorCode(ByVal eNewErrorCode As eMotifExtractorErrorCodes)
        SetLocalErrorCode(eNewErrorCode, False)
    End Sub

    Private Sub SetLocalErrorCode(ByVal eNewErrorCode As eMotifExtractorErrorCodes, ByVal blnLeaveExistingErrorCodeUnchanged As Boolean)

        If blnLeaveExistingErrorCodeUnchanged AndAlso mLocalErrorCode <> eMotifExtractorErrorCodes.NoError Then
            ' An error code is already defined; do not change it
        Else
            mLocalErrorCode = eNewErrorCode

            If eNewErrorCode = eMotifExtractorErrorCodes.NoError Then
                If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError Then
                    MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError)
                End If
            Else
                MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError)
            End If
        End If

    End Sub

    ' Options class
    Public Class FastaFileOptionsClass

        Public Sub New()
            mProteinLineStartChar = ">"c
            mProteinLineAccessionEndChar = " "c

            mLookForAddnlRefInDescription = False
            mAddnlRefSepChar = "|"c
            mAddnlRefAccessionSepChar = ":"c
        End Sub

#Region "Classwide Variables"
        Private mReadonlyClass As Boolean

        Private mProteinLineStartChar As Char
        Private mProteinLineAccessionEndChar As Char

        Private mLookForAddnlRefInDescription As Boolean

        Private mAddnlRefSepChar As Char
        Private mAddnlRefAccessionSepChar As Char

#End Region

#Region "Processing Options Interface Functions"
        Public Property ReadonlyClass() As Boolean
            Get
                Return mReadonlyClass
            End Get
            Set(ByVal Value As Boolean)
                If Not mReadonlyClass Then
                    mReadonlyClass = Value
                End If
            End Set
        End Property

        Public Property ProteinLineStartChar() As Char
            Get
                Return mProteinLineStartChar
            End Get
            Set(ByVal Value As Char)
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mProteinLineStartChar = Value
                End If
            End Set
        End Property

        Public Property ProteinLineAccessionEndChar() As Char
            Get
                Return mProteinLineAccessionEndChar
            End Get
            Set(ByVal Value As Char)
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mProteinLineAccessionEndChar = Value
                End If
            End Set
        End Property

        Public Property LookForAddnlRefInDescription() As Boolean
            Get
                Return mLookForAddnlRefInDescription
            End Get
            Set(ByVal Value As Boolean)
                If Not mReadonlyClass Then
                    mLookForAddnlRefInDescription = Value
                End If
            End Set
        End Property

        Public Property AddnlRefSepChar() As Char
            Get
                Return mAddnlRefSepChar
            End Get
            Set(ByVal Value As Char)
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mAddnlRefSepChar = Value
                End If
            End Set
        End Property

        Public Property AddnlRefAccessionSepChar() As Char
            Get
                Return mAddnlRefAccessionSepChar
            End Get
            Set(ByVal Value As Char)
                If Not Value = Nothing AndAlso Not mReadonlyClass Then
                    mAddnlRefAccessionSepChar = Value
                End If
            End Set
        End Property
#End Region

    End Class

End Class

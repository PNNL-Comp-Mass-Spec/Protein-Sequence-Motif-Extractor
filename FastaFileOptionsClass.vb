Public Class FastaFileOptionsClass

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        LookForAddnlRefInDescription = False
        AddnlRefSepChar = "|"c
        AddnlRefAccessionSepChar = ":"c
    End Sub

    ''' <summary>
    ''' When true, look for additional references in the protein description
    ''' </summary>
    ''' <returns></returns>
    Public Property LookForAddnlRefInDescription As Boolean

    ''' <summary>
    ''' Additional reference separation character
    ''' </summary>
    ''' <returns></returns>
    Public Property AddnlRefSepChar As Char

    ''' <summary>
    ''' Additional reference accession separation character
    ''' </summary>
    ''' <returns></returns>
    Public Property AddnlRefAccessionSepChar As Char

End Class

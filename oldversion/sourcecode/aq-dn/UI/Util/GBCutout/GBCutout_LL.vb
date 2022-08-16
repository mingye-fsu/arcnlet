Imports System.Windows.Forms
Imports System.Drawing
''' <summary>
''' Cutout for the groupbox on the transport module tab
''' </summary>
''' <remarks></remarks>
Public Class GBCutout_LL
    Private Structure DLLVERSIONINFO

        Public cbSize As Integer

        Public dwMajorVersion As Integer

        Public dwMinorVersion As Integer

        Public dwBuildNumber As Integer

        Public dwPlatformID As Integer

    End Structure

    ''' <summary>
    ''' Import from comctl32.dll
    ''' </summary>
    ''' <param name="version"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Declare Function DllGetVersion Lib "comctl32.dll" (ByRef version As DLLVERSIONINFO) As Integer
    ''' <summary>
    ''' Import from uxtheme.dll
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Declare Function IsThemeActive Lib "uxtheme.dll" () As Boolean
    ''' <summary>
    ''' Import from uxtheme.dll
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Declare Function IsAppThemed Lib "uxtheme.dll" () As Boolean

    ''' <summary>
    ''' Determine whether visual styles are enabled
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function IsVisualStylesEnabledRevisited() As Boolean

        Dim os As OperatingSystem = System.Environment.OSVersion

        If os.Platform = PlatformID.Win32NT AndAlso (((os.Version.Major = 5) And (os.Version.Minor >= 1)) Or (os.Version.Major > 5)) Then

            Dim version As New DLLVERSIONINFO

            version.cbSize = Len(version)

            If DllGetVersion(version) = 0 Then

                Return (version.dwMajorVersion > 5) AndAlso IsThemeActive() AndAlso IsAppThemed()

            End If

        End If

        Return False

    End Function

    ''' <summary>
    ''' Control load event
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub UserControl2_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsVisualStylesEnabledRevisited() Then
            'this is necessary to get rid of the extra pixel wide
            'white border when visual styles are disabled

            Dim p1 As New Panel With {.Width = 1, .height = Me.Height, _
                                     .Location = New Point(Me.Width - 1, -5), _
                                     .BackColor = Panel1.BackColor, _
                                     .BorderStyle = Windows.Forms.BorderStyle.None, _
                                     .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right}

            Dim p2 As New Panel With {.Width = Me.Width, .height = 1, _
                                     .Location = New Point(0, Me.Height - 1), _
                                     .BackColor = Panel1.BackColor, _
                                     .BorderStyle = Windows.Forms.BorderStyle.None, _
                                     .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}

            Dim g As GroupBox = New GroupBox With {.Location = New Point(0, -5), _
                                                .Width = GroupBox3.Width, _
                                                .Height = GroupBox3.Height, _
                                                .BackColor = GroupBox3.BackColor, _
                                                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top}
            Me.Controls.Add(p1)
            Me.Controls.Add(p2)
            Me.Controls.Add(g)
        Else
            GroupBox3.Visible = True
            'Me.Width = Me.Width - 1
            'Me.Height = Me.Height - 1
        End If
    End Sub
End Class

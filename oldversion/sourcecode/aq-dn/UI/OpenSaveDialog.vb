Imports ESRI.ArcGIS.CatalogUI
Imports ESRI.ArcGIS.Catalog

''' <summary>
''' Represents an ArcGIS open/save dialog
''' </summary>
''' <remarks>There is a bug in the GxDialog where the file icons won't appear if XP visual styles are enabled
''' There is no solution (as of ArcGIS 9.3) other than to disable visual styles.
''' </remarks>
Public Class OpenSaveDialog

    Private m_ft As IGxObjectFilter
    Private m_initialName As String

    ''' <summary>
    ''' Constructor.
    ''' </summary>
    ''' <param name="ft">Shows only files of type defined by ft. Used for both the open and save dialog</param>
    ''' <remarks>can use the FilterTypes class to get a new filter to pass to this constructor.</remarks>
    Public Sub New(ByVal ft As IGxObjectFilter)
        m_ft = ft
        m_initialName = ""
    End Sub

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="ft">Shows only files of type defined by ft. Used for both the open and save dialog</param>
    ''' <param name="fn">The initial file name</param>
    ''' <remarks>can use the FilterTypes class to get a new filter to pass to this constructor.</remarks>
    Public Sub New(ByVal ft As IGxObjectFilter, ByVal fn As String)
        m_ft = ft
        m_initialName = fn
    End Sub


    ''' <summary>
    ''' Shows an ARrcGIS modal save dialog.
    ''' </summary>
    ''' <param name="owner">The owner form</param>
    ''' <param name="forceExtension">A string specifying the file extension to force.
    ''' If an empty string is passed in, no extension will be forced.</param>
    ''' <returns>A string representing the typed file name.  An empty string if the user clicked cancel.</returns>
    ''' <remarks>The file format will be set to the IMAGINE image format</remarks>
    Public Function showSave(Optional ByVal owner As Windows.Forms.Form = Nothing, Optional ByVal forceExtension As String = "") As String
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Dim dlg As New GxDialog()
        Dim dlgflt As IGxObjectFilterCollection = CType(dlg, IGxObjectFilterCollection)
        COM.ManageLifetime(dlg)
        COM.ManageLifetime(dlgflt)

        dlgflt.AddFilter(m_ft, True)
        dlg.Title = "Save As..."
        dlg.AllowMultiSelect = False
        dlg.RememberLocation = True
        dlg.StartingLocation = Main.ActiveDocPath
        dlg.Name = m_initialName

        Dim handle As Integer = 0
        If Not owner Is Nothing Then
            handle = owner.Handle
        End If

        Dim file As String = ""
        Dim validOrCancel As Boolean = False
        Do
            If (dlg.DoModalSave(handle)) Then
                If forceExtension <> "" Then
                    If IO.Path.GetExtension(dlg.Name) <> forceExtension Then
                        file = IO.Path.Combine(dlg.FinalLocation.FullName, IO.Path.GetFileNameWithoutExtension(dlg.Name) & "." & forceExtension)
                    Else
                        file = IO.Path.Combine(dlg.FinalLocation.FullName, dlg.Name)
                    End If
                Else
                    file = IO.Path.Combine(dlg.FinalLocation.FullName, dlg.Name)
                End If


                If Not Utilities.checkExist(file) Then
                    validOrCancel = True
                Else
                    MsgBox("The target file '" & IO.Path.GetFileName(file) & "' exists. Please choose a different file name")
                    file = ""
                End If
            Else
                validOrCancel = True
                file = ""
            End If
        Loop Until validOrCancel

        Return file
    End Function

    ''' <summary>
    ''' Shows an ArcGIS modal open dialog
    ''' </summary>
    ''' <param name="owner">The owner form</param>
    ''' <returns>A string representing the selected file.  An empty string is returned if the user clicked cancel.</returns>
    ''' <remarks>
    ''' Although the underlying ArcGIS library supports multiple selection, this function does not.
    ''' This was done for simplicity.
    ''' </remarks>
    Public Function showOpen(Optional ByVal owner As Windows.Forms.Form = Nothing) As String
        Dim dlg As New GxDialog()
        Dim dlgflt As IGxObjectFilterCollection = CType(dlg, IGxObjectFilterCollection)
        Dim selectedFiles As IEnumGxObject = Nothing

        dlgflt.AddFilter(m_ft, True)
        dlg.Title = "Open File..."
        dlg.AllowMultiSelect = False
        dlg.RememberLocation = True
        dlg.StartingLocation = Main.ActiveDocPath


        Dim handle As Integer = 0
        If Not owner Is Nothing Then
            handle = owner.Handle
        End If
        If (dlg.DoModalOpen(handle, selectedFiles)) Then
            Marshal.FinalReleaseComObject(dlg)
            Marshal.FinalReleaseComObject(dlgflt)
            GC.Collect()
            GC.WaitForPendingFinalizers()
            Return IO.Path.Combine(dlg.FinalLocation.FullName, selectedFiles.Next.FullName)
        Else
            Marshal.FinalReleaseComObject(dlg)
            Marshal.FinalReleaseComObject(dlgflt)
            GC.Collect()
            GC.WaitForPendingFinalizers()
            Return ""
        End If
    End Function


End Class

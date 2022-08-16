Imports System.Windows.Forms
Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.DataSourcesRaster
Imports System.Reflection
Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Channels
Imports System.Runtime.Remoting.Channels.Ipc
Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry

Partial Public Class MainForm


    Private m_denitrification As Denitrification
    Private m_denitrification_NH4 As Denitrification

    Private m_plumesfile_lastrun As String
    Private m_plumesfile_lastrun_NH4 As String
    Private m_riskfac_lastrun As Single

    ''' <summary>
    ''' used to start this module's calculations. 
    ''' </summary>
    ''' <param name="AddOutputToActiveMap">
    ''' If true, adds the final output to the layers list of the active map.
    ''' </param>
    ''' <returns>If there are any errors in the form inputs or errors in calculation, returns false. 
    ''' Else, returns true
    ''' </returns>
    ''' <remarks>
    ''' This function validates all the form inputs and returns false if the validation fails. After 
    ''' validation, the validated parameters are passes to the computation module.  If there are errors
    ''' returned from the computation module, this function returns false.
    ''' </remarks>
    Public Function runDn(Optional ByVal AddOutputToActiveMap As Boolean = True) As Boolean
        GC.Collect()
        GC.WaitForPendingFinalizers()

        Trace.WriteLine("Nitrate Load Estimation: START")

        Dim errOccurred As Boolean = False
        Dim ChkDnSimulation_NH4 As Boolean = chkDnUseNH4.Checked
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Try

            'validate the inputs
            Dim err As String = ""
            For Each v As validator In DNValidators
                err = v()
                If err <> "" AndAlso err <> "-1" Then
                    Throw New Exception(err)
                ElseIf err = "-1" Then
                    Trace.WriteLine("validation cancelled")
                    Return True
                End If
            Next

            'if were here, the form validated successfully
            Trace.WriteLine("Nitrate Load Estimation: Form inputs validated")

            'gather the inputs
            Dim sel_plumesinfo As FeatureLayer = cmbDNPlumesInfo.SelectedItem.baselayer
            Dim sel_riskfac As Single = CType(txtDNRiskFac.Value, Single)


            'ADDING THE NH4 LOADING CALCULATION
            If ChkDnSimulation_NH4 Then

                Dim sel_plumesinfo_NH4 As FeatureLayer = cmbDNPlumesInfo_NH4.SelectedItem.baselayer
                Try
                    'echo inputs
                    Trace.WriteLine("Plumes Info = " & IO.Path.Combine(CType(sel_plumesinfo_NH4, IDataset).Workspace.PathName, sel_plumesinfo_NH4.Name))
                    Trace.WriteLine("Risk Factor = " & sel_riskfac)
                Catch ex As Exception
                    Trace.WriteLine(ex.ToString)
                End Try
                If sel_plumesinfo_NH4.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("The input NH4 feature layer is empty")
                Dim layers_NH4 As New List(Of ILayer)
                layers_NH4.Add(sel_plumesinfo_NH4)
                If Not Utilities.checkLayerSpatialReferences(layers_NH4, Main.ActiveMap) Then
                    Throw New Exception("NH4 Input data must have the same spatial references")
                End If
                Trace.WriteLine("NH4 Input spatial referecenes OK")
                Trace.WriteLine("Running module Ammonium Load Estimation...")
                m_denitrification_NH4 = New Denitrification(PlumesInfo:=sel_plumesinfo_NH4.FeatureClass, RiskFactor:=sel_riskfac, _
                                                      OutputIntermediateCalcs:=mnuOutputIntermediateToolStripMenuItem.Checked)
                If Not m_denitrification_NH4.CalculateLoad() Then Throw New Exception("Coulnd't calculate NH4 load")
                'get the results
                txtDnOut_NH4.Clear()
                txtDnOut_NH4.Rtf = m_denitrification_NH4.OutParams("rtf")
                Try
                    Dim ext As String = ""
                    If CType(sel_plumesinfo_NH4, IDataset).Workspace.Type = esriWorkspaceType.esriFileSystemWorkspace Then
                        ext = ".shp"
                    End If
                    m_plumesfile_lastrun_NH4 = IO.Path.Combine(CType(sel_plumesinfo_NH4, IDataset).Workspace.PathName, CType(sel_plumesinfo_NH4, IDataset).Name & ext)
                Catch ex As Exception
                End Try
                m_riskfac_lastrun = txtDNRiskFac.Value
                Trace.WriteLine("Running module NH4 Load Estimation...Done")
                m_denitrification_NH4 = Nothing 'necessary so that dropdowns refresh when they're supposed to


                'Start to calculate NO3 Loading.
                'Updating the plume_info. The two fields are needed to be updated. "massDNRate" and "MsInRrNmr".

                Try
                    'echo inputs
                    Trace.WriteLine("Plumes Info = " & IO.Path.Combine(CType(sel_plumesinfo, IDataset).Workspace.PathName, sel_plumesinfo.Name))
                    Trace.WriteLine("Risk Factor = " & sel_riskfac)
                Catch ex As Exception
                    Trace.WriteLine(ex.ToString)
                End Try

                'check whether the input is empty
                If sel_plumesinfo.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("The input feature layer is empty")

                'check layer spatial references.  Sometimes get unexplainable errors
                'and/or results when the spatial references are different.
                Dim layers As New List(Of ILayer)
                layers.Add(sel_plumesinfo)
                If Not Utilities.checkLayerSpatialReferences(layers, Main.ActiveMap) Then
                    Throw New Exception("Input data must have the same spatial references")
                End If
                Trace.WriteLine("Input spatial referecenes OK")
                Trace.WriteLine("Running module Nitrate Load Estimation...")
                m_denitrification = New Denitrification(PlumesInfo:=sel_plumesinfo.FeatureClass, PlumesInfo_NH4:=sel_plumesinfo_NH4.FeatureClass, RiskFactor:=sel_riskfac, _
                                                      OutputIntermediateCalcs:=mnuOutputIntermediateToolStripMenuItem.Checked)
                If Not m_denitrification.CalculateLoad_NO3_1() Then Throw New Exception("Coulnd't calculate load")

                'get the results
                txtDnOut_NO3.Clear()
                txtDnOut_NO3.Rtf = m_denitrification.OutParams("rtf")
                Try
                    Dim ext As String = ""
                    If CType(sel_plumesinfo, IDataset).Workspace.Type = esriWorkspaceType.esriFileSystemWorkspace Then
                        ext = ".shp"
                    End If
                    m_plumesfile_lastrun = IO.Path.Combine(CType(sel_plumesinfo, IDataset).Workspace.PathName, CType(sel_plumesinfo, IDataset).Name & ext)
                Catch ex As Exception
                End Try
                m_riskfac_lastrun = txtDNRiskFac.Value

                Trace.WriteLine("Running module Nitrate Load Estimation...Done")

                m_denitrification = Nothing 'necessary so that dropdowns refresh when they're supposed to

                'ENDING NH4 and NO3 LOADING CALCULATION
            Else
                'FOR CALCULATING NO3 ONLY.
                Try
                    'echo inputs
                    Trace.WriteLine("Plumes Info = " & IO.Path.Combine(CType(sel_plumesinfo, IDataset).Workspace.PathName, sel_plumesinfo.Name))
                    Trace.WriteLine("Risk Factor = " & sel_riskfac)
                Catch ex As Exception
                    Trace.WriteLine(ex.ToString)
                End Try

                'check whether the input is empty
                If sel_plumesinfo.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("The input feature layer is empty")

                'check layer spatial references.  Sometimes get unexplainable errors
                'and/or results when the spatial references are different.
                Dim layers As New List(Of ILayer)
                layers.Add(sel_plumesinfo)
                If Not Utilities.checkLayerSpatialReferences(layers, Main.ActiveMap) Then
                    Throw New Exception("Input data must have the same spatial references")
                End If
                Trace.WriteLine("Input spatial referecenes OK")


                Trace.WriteLine("Running module Nitrate Load Estimation...")

                m_denitrification = New Denitrification(PlumesInfo:=sel_plumesinfo.FeatureClass, RiskFactor:=sel_riskfac, _
                                                      OutputIntermediateCalcs:=mnuOutputIntermediateToolStripMenuItem.Checked)
                If Not m_denitrification.CalculateLoad() Then Throw New Exception("Coulnd't calculate load")

                'get the results
                txtDnOut_NO3.Clear()
                txtDnOut_NO3.Rtf = m_denitrification.OutParams("rtf")
                Try
                    Dim ext As String = ""
                    If CType(sel_plumesinfo, IDataset).Workspace.Type = esriWorkspaceType.esriFileSystemWorkspace Then
                        ext = ".shp"
                    End If
                    m_plumesfile_lastrun = IO.Path.Combine(CType(sel_plumesinfo, IDataset).Workspace.PathName, CType(sel_plumesinfo, IDataset).Name & ext)
                Catch ex As Exception
                End Try
                m_riskfac_lastrun = txtDNRiskFac.Value

                Trace.WriteLine("Running module Nitrate Load Estimation...Done")

                m_denitrification = Nothing 'necessary so that dropdowns refresh when they're supposed to
            End If

        Catch ex As Exception
            errOccurred = True
            Trace.WriteLine("[Error] Nitrate Load Estimation (" & Reflection.MethodInfo.GetCurrentMethod.Name & "): " & ex.Message)
        End Try

        btnAbort.Enabled = False
        Trace.WriteLine("Denitrification: FINISHED")

        If errOccurred Then
            Return False
        Else
            Return True
        End If
    End Function

    ''' <summary>
    ''' Cancels the currently running plume calculation operation (if any).
    ''' </summary>
    ''' <remarks>Called by the abort button and the form close event</remarks>
    Friend Sub cancelDenitrification()
        If m_denitrification Is Nothing Then
            'do nothing
        Else
            m_denitrification.cancelDenitrification()
        End If
    End Sub

    ''' <summary>
    ''' initializes the components on this tab
    ''' </summary>
    ''' <remarks>Should be called from the forms load event</remarks>
    Private Sub DNInit()
        'populate the dropdowns with the map's layers.
        DNPopulateDropdowns()

        'register the form validators
        DNValidators.Clear()
        DNValidators.Add(New validator(AddressOf validate_cmbDNPlumesInfo))
        DNValidators.Add(New validator(AddressOf validate_txtDNRiskFac))
        DNValidators.Add(New validator(AddressOf validate_cmbDNPlumesInfo_NH4))
    End Sub

#Region "UI event handlers"

    Private Sub btnDNPlumesInfoInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnDNPlumesInfoInfo.LinkClicked
        If Not cmbDNPlumesInfo.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbDNPlumesInfo.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnDNExport_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDNExport.Click
        Dim theFile As String = ""
        Try
            Dim savedlg As New Windows.Forms.SaveFileDialog

            savedlg.CheckPathExists = True
            savedlg.DefaultExt = "csv"
            savedlg.Filter = "*.csv|*.csv"
            savedlg.FileName = "Load" & Now.ToString("yyyyMMdd.HHmm") & "." & savedlg.DefaultExt
            savedlg.OverwritePrompt = True
            savedlg.SupportMultiDottedExtensions = True
            savedlg.ValidateNames = True
            savedlg.Title = "Save loads"

            If savedlg.ShowDialog = Windows.Forms.DialogResult.OK Then
                Dim f As New IO.StreamWriter(savedlg.OpenFile)
                theFile = IO.Path.Combine(savedlg.InitialDirectory, savedlg.FileName)
                Dim csv As String = "Plumes input file: """ & m_plumesfile_lastrun & """" & vbCrLf & _
                                    "Risk Factor: " & m_riskfac_lastrun & vbCrLf & _
                                    "FID,M_out,M_out*RF,M_dn,M_in" & vbCrLf
                Dim rows As String() = txtDnOut_NO3.Text.Trim.Split(New Char() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                Dim tokens As String()
                For Each row As String In rows
                    tokens = row.Trim.Split(New Char() {vbTab}, StringSplitOptions.RemoveEmptyEntries)
                    For i As Short = 0 To tokens.Length - 1
                        If Not i = tokens.Length - 1 Then
                            csv = csv & """" & tokens(i).Trim & ""","
                        Else
                            csv = csv & """" & tokens(i).Trim & """" & vbCrLf
                        End If
                    Next
                Next
                'ADDING NH4 SIMULATION
                If chkDnUseNH4.Checked Then
                    Dim csv_NH4 As String = "Plumes input file: """ & m_plumesfile_lastrun_NH4 & """" & vbCrLf & _
                    "Risk Factor: " & m_riskfac_lastrun & vbCrLf & _
                    "FID,M_out,M_out*RF,M_dn,M_in" & vbCrLf
                    Dim rows_NH4 As String() = txtDnOut_NH4.Text.Trim.Split(New Char() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    Dim tokens_NH4 As String()
                    For Each row As String In rows_NH4
                        tokens_NH4 = row.Trim.Split(New Char() {vbTab}, StringSplitOptions.RemoveEmptyEntries)
                        For i As Short = 0 To tokens_NH4.Length - 1
                            If Not i = tokens_NH4.Length - 1 Then
                                csv_NH4 = csv_NH4 & """" & tokens_NH4(i).Trim & ""","
                            Else
                                csv_NH4 = csv_NH4 & """" & tokens_NH4(i).Trim & """" & vbCrLf
                            End If
                        Next
                    Next
                    f.Write(csv_NH4)
                End If
                'END ADDING.
                f.Write(csv)
                f.Close()
            End If
        Catch ex As Exception
            Dim msg As String
            msg = "There was an error saving the file '" & theFile & "': " & ex.ToString
            Trace.WriteLine(msg)
            MsgBox(msg, MsgBoxStyle.Critical)
        End Try

    End Sub
    'Adding the NH4 calculation.
    Private Sub chkDnUseNH4_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkDnUseNH4.CheckedChanged
        If chkDnUseNH4.Checked Then
            cmbDNPlumesInfo_NH4.Enabled = True
            txtDnOut_NH4.Enabled = True
            Label56.Enabled = True

        Else
            cmbDNPlumesInfo_NH4.Enabled = False
            cmbDNPlumesInfo_NH4.Text = ""
            txtDnOut_NH4.Enabled = False
            'txtDnOut_NH4.Visible = False
            Label56.Enabled = False
            'Label56.Visible = False

        End If
    End Sub
    Private Sub btnDNPlumesNH4InfoInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnDNPlumesNH4InfoInfo.LinkClicked
        If Not cmbDNPlumesInfo_NH4.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbDNPlumesInfo_NH4.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub



#End Region


#Region "Helpers"
    ''' <summary>
    ''' Populates the drop down boxes with the appropriate layers
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub DNPopulateDropdowns()
        'only show point feature layers that have a name that ends in "_info"
        cmbDNPlumesInfo.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, esriGeometryType.esriGeometryPoint, New System.Text.RegularExpressions.Regex("_info$"))
        cmbDNPlumesInfo_NH4.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, esriGeometryType.esriGeometryPoint, New System.Text.RegularExpressions.Regex("_info$"))
    End Sub

#End Region


#Region "validators"
    Private DNValidators As New List(Of [Delegate])

    Private Function validate_cmbDNPlumesInfo() As String
        Dim errstr As String = ""
        If cmbDNPlumesInfo.SelectedItem Is Nothing Then
            errstr = "Please select the appropriate point layer corresponding to the selected NO3 plumes layer"
        End If
        ErrorProvider1.SetError(cmbDNPlumesInfo, errstr)
        Return errstr
    End Function

    Private Function validate_txtDNRiskFac() As String
        Dim errstr As String = ""
        'invalid inputs are forbidden by the numeric control's properties
        Return ""
    End Function
    Private Function validate_cmbDNPlumesInfo_NH4() As String
        Dim errstr As String = ""
        If cmbDNPlumesInfo_NH4.Enabled Then
            If cmbDNPlumesInfo_NH4.SelectedItem Is Nothing Then
                errstr = "Please select the appropriate point layer corresponding to the selected NH4 plumes layer"
            End If
            ErrorProvider1.SetError(cmbDNPlumesInfo_NH4, errstr)
        End If
        Return errstr
    End Function



#End Region


End Class

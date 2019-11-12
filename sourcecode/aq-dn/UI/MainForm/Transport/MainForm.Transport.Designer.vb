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


'This class implements the functionality contained in the Particle Tracking tab on the main form
Partial Public Class MainForm
    'the reference to the more options panel
    Private WithEvents m_cmbMoreopt_dombdy As ComboBox

    'comm channel between this module and the wrapper
    Private m_chanwrapper As IpcServerChannel

    Private m_TransportBootstrapper As IAqDnApplication.IWrapperBootstrapper

    Private m_transportRunner As ModuleRunRemote

    Private m_finished As Boolean
    Private m_finished_ok As Boolean
    'Declare variable
    Private strFileName_VZMOD As String

#Region "UI Event Handlers"
    Private Sub btnTROutputPlumes_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTROutputPlumes.Click
        Dim dlg As New OpenSaveDialog(FilterTypes.Raster, IO.Path.GetFileNameWithoutExtension(txtTROutputPlumes.Text))
        Dim r As String = dlg.showSave(Me, "img")
        If r <> "" Then
            txtTROutputPlumes.Text = r
        End If
    End Sub

    Private Sub btnVZMODLocation_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnVZMODLocation.Click
        'Set the Open dialog properties
        With OpenFileDialog1
            .Filter = "VZMOD set file types (*.pyw)|*.pyw|(*.pyc)|*.pyc|All Files (*.*)|*.*"
            .FilterIndex = 1
            .Title = "Open VZMOD Model"
        End With
        'Show the Open dialog and if the user clicks the Open button,
        'load the file
        If OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            Try
                'Save the file path and name
                strFileName_VZMOD = OpenFileDialog1.FileName
                txtVZMODLocation.Text = strFileName_VZMOD
            Catch ex As Exception
                MessageBox.Show(ex.Message, My.Application.Info.Title, _
                MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub




    Private Sub btnTRPointsLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnTRPointsLayerInfo.LinkClicked
        If Not cmbTRPointLayer.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbTRPointLayer.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnTRWaterbodiesLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnTRWaterbodiesLayerInfo.LinkClicked
        If Not cmbTRWaterbodies.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbTRWaterbodies.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnTRPathsLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnTRPathsLayerInfo.LinkClicked
        If Not cmbTRPaths.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbTRPaths.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub m_cmbMoreopt_dombdy_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_cmbMoreopt_dombdy.SelectedIndexChanged
        Dim dombdy As DomenicoSourceBoundaries.DomenicoSourceBoundary
        If Not m_cmbMoreopt_dombdy.SelectedItem Is Nothing Then
            dombdy = [Enum].Parse(GetType(DomenicoSourceBoundaries.DomenicoSourceBoundary), m_cmbMoreopt_dombdy.SelectedItem)

            If dombdy = DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate Then
                txtTRM_in.Enabled = True
                txtTRSourceZ.Enabled = False
                lblTRSourceZ.Tag = txtTRSourceZ.Text
                txtTRSourceZ.Text = "[Auto]"
                chk_Specify_Zmax.Enabled = True
                chk_Specify_Zmax.Checked = False
                txtTRSourceZ_max.Enabled = False
                'add the NH4 calculation paramter.
                'If chkTRUseNH4.Checked Then
                'txtTRM_in_NH4.Enabled = True
                'txtTRSourceZ_NH4.Enabled = False
                'txtTRSourceZ_NH4.Text = "[Auto]"
                'End If
                'End adding the NH4 Calculation parameter.
                If lblTRM_in.Tag <> "" Then txtTRM_in.Text = lblTRM_in.Tag
            End If

            If dombdy = DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Z Then
                txtTRM_in.Enabled = False
                txtTRSourceZ.Enabled = True
                lblTRM_in.Tag = txtTRM_in.Text
                txtTRM_in.Text = "[Auto]"
                chk_Specify_Zmax.Enabled = False
                chk_Specify_Zmax.Checked = False
                txtTRSourceZ_max.Enabled = False
                'txtTRSourceZ_max.Text = "Auto"
                'add the NH4 calculation paramter.
                'If chkTRUseNH4.Checked Then
                'txtTRM_in_NH4.Enabled = False
                'txtTRSourceZ_NH4.Enabled = True
                'txtTRM_in_NH4.Text = "[Auto]"
                'End If
                'End adding the NH4 Calculation parameter.
                If lblTRSourceZ.Tag <> "" Then txtTRSourceZ.Text = lblTRSourceZ.Tag
            End If
        End If
    End Sub

    Private Sub cmbTRSolType_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmbTRSolType.SelectedIndexChanged
        Dim stype As SolutionTypes.SolutionType
        If Not cmbTRSolType.SelectedItem Is Nothing Then
            stype = CType(cmbTRSolType.SelectedItem.value, SolutionTypes.SolutionType)

            If SolutionTypes.is2D(stype) Then
                txtTRMeshZ.Enabled = False
                txtTRDispTV.Enabled = False
                'don't disable source Z since that is what we will use to set meshZ                
            Else
                txtTRMeshZ.Enabled = True
                txtTRDispTV.Enabled = True
                txtTRSourceZ.Enabled = True
            End If

            If SolutionTypes.isSteadyState(stype) Then
                txtTRSolTime.Enabled = False
            Else
                txtTRSolTime.Enabled = True
            End If

            If SolutionTypes.hasDecay(stype) Then
                txtTRDecay.Enabled = True
                'add the NH4 calculation paramter
                If chkTRUseNH4.Checked Then txtTRDecay_NH4.Enabled = True
                'End adding the NH4 Calculation parameter.
            Else
                txtTRDecay.Enabled = False
            End If
        End If
    End Sub


    Private Sub txtTRMeshX_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtTRMeshX.TextChanged
        'make sure that the y mesh size is always the same as x
        txtTRMeshY.Text = txtTRMeshX.Text
    End Sub

    Private Sub txtTRSourceY_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtTRSourceY.TextChanged
        'set the cell size to a reasonable value. reasonable values are (for plumes that aren't too short)
        'around 5 to 30x smaller than the input source width
        Try
            Dim val As Single = txtTRSourceY.Text / 15
            If val <= 10 Then
                txtTRMeshX.Text = Math.Round(val, 1)
            Else
                txtTRMeshX.Text = Math.Round(val, 0)
            End If

        Catch ex As Exception

        End Try
    End Sub

    Private Sub cmbTRPointLayer_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmbTRPointLayer.SelectedIndexChanged
        Try
            Dim selectedLayerFC As IFeatureClass

            'open the feature class and check to see which fields are available
            If Not cmbTRPointLayer.SelectedItem Is Nothing Then
                selectedLayerFC = CType(CType(cmbTRPointLayer.SelectedItem, MyLayer2).BaseLayer, FeatureLayer).FeatureClass

                'if the feature class contains a known field, indicate to the user
                'by coloring the appropriate text box. Set the value < 0 to use the field
                'value from the attributes table.  Set the text box to any other number to
                'use that number.
                If selectedLayerFC.FindField("N0_conc") >= 0 Then
                    If txtTRC0.Text <> "-1" Then txtTRC0.Tag = txtTRC0.Text
                    txtTRC0.Text = -1
                    txtTRC0.BackColor = Drawing.Color.PaleGoldenrod
                Else
                    txtTRC0.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window)
                    If txtTRC0.Tag <> "" Then txtTRC0.Text = txtTRC0.Tag
                End If

                'Adding the NH4 Concentration
                If chkTRUseNH4.Checked Then
                    If selectedLayerFC.FindField("NH4_conc") >= 0 Then
                        If txtTRC0_NH4.Text <> "-1" Then txtTRC0_NH4.Tag = txtTRC0_NH4.Text
                        txtTRC0_NH4.Text = -1
                        txtTRC0_NH4.BackColor = Drawing.Color.PaleGoldenrod
                    Else
                        txtTRC0_NH4.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window)
                        If txtTRC0_NH4.Tag <> "" Then txtTRC0_NH4.Text = txtTRC0_NH4.Tag
                    End If
                End If
                'End adding.


                If selectedLayerFC.FindField("dispL") >= 0 Then
                    If txtTRDispL.Text <> "-1" Then txtTRDispL.Tag = txtTRDispL.Text
                    txtTRDispL.Text = -1
                    txtTRDispL.BackColor = Drawing.Color.PaleGoldenrod
                Else
                    txtTRDispL.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window)
                    If txtTRDispL.Tag <> "" Then txtTRDispL.Text = txtTRDispL.Tag
                End If

                If selectedLayerFC.FindField("dispTH") >= 0 Then
                    If txtTRDispTH.Text <> "-1" Then txtTRDispTH.Tag = txtTRDispTH.Text
                    txtTRDispTH.Text = -1
                    txtTRDispTH.BackColor = Drawing.Color.PaleGoldenrod
                Else
                    txtTRDispTH.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window)
                    If txtTRDispTH.Tag <> "" Then txtTRDispTH.Text = txtTRDispTH.Tag
                End If

                If selectedLayerFC.FindField("decayCoeff") >= 0 Then
                    If txtTRDecay.Text <> "-1" Then txtTRDecay.Tag = txtTRDecay.Text
                    txtTRDecay.Text = -1
                    txtTRDecay.BackColor = Drawing.Color.PaleGoldenrod
                Else
                    txtTRDecay.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window)
                    If txtTRDecay.Tag <> "" Then txtTRDecay.Text = txtTRDecay.Tag
                End If

                If selectedLayerFC.FindField("Min") >= 0 Then
                    If txtTRM_in.Text <> "-1" Then txtTRM_in.Tag = txtTRM_in.Text
                    txtTRM_in.Text = -1
                    txtTRM_in.BackColor = Drawing.Color.PaleGoldenrod
                Else
                    txtTRM_in.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window)
                    If txtTRM_in.Tag <> "" Then txtTRM_in.Text = txtTRM_in.Tag
                End If
            End If


        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical)
        End Try
    End Sub
    'Add the events about the NH4 calculation.
    Private Sub btnTROutputPlumes_NH4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTROutputPlumes_NH4.Click
        Dim dlg As New OpenSaveDialog(FilterTypes.Raster, IO.Path.GetFileNameWithoutExtension(txtTROutputPlumes_NH4.Text))
        Dim r As String = dlg.showSave(Me, "img")
        If r <> "" Then
            txtTROutputPlumes_NH4.Text = r
        End If
    End Sub
    Private Sub chkTRUseNH4_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkTRUseNH4.CheckedChanged
        If chkTRUseNH4.Checked Then
            txtTRC0_NH4.Enabled = True
            'txtTRM_in_NH4.Enabled = True
            'txtTRSourceZ_NH4.Enabled = True
            txtTRDispL_NH4.Enabled = True
            txtTRDispTH_NH4.Enabled = True
            txtTRDecay_NH4.Enabled = True
            txtTRBulkdensity_NH4.Enabled = True
            txtTRAdsorp_NH4.Enabled = True
            txtTRAverageTheta_NH4.Enabled = True
            btnTROutputPlumes_NH4.Enabled = True
            txtTROutputPlumes_NH4.Enabled = True
        Else
            txtTRC0_NH4.Enabled = False
            'txtTRM_in_NH4.Enabled = False
            'txtTRSourceZ_NH4.Enabled = False
            txtTRDispL_NH4.Enabled = False
            txtTRDispTH_NH4.Enabled = False
            txtTRDecay_NH4.Enabled = False
            txtTRBulkdensity_NH4.Enabled = False
            txtTRAdsorp_NH4.Enabled = False
            txtTRAverageTheta_NH4.Enabled = False
            btnTROutputPlumes_NH4.Enabled = False
            txtTROutputPlumes_NH4.Enabled = False
            txtTROutputPlumes_NH4.Text = ""
        End If
    End Sub

    Private Sub chk_Specify_Zmax_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chk_Specify_Zmax.CheckedChanged
        If chk_Specify_Zmax.Enabled Then
            If chk_Specify_Zmax.Checked Then
                txtTRSourceZ_max.Enabled = True
                txtTRSourceZ_max.Text = "3.0"
            Else
                txtTRSourceZ_max.Enabled = False
                txtTRSourceZ_max.Text = "3.0"
            End If
        End If
    End Sub





#End Region

#Region "Validators"
    Private TRValidators As New List(Of [Delegate])

    Private Function validate_cmbTRPointLayer() As String
        Dim errstr As String = ""
        If cmbTRPointLayer.SelectedItem Is Nothing Then
            errstr = "Please select a source locations layer"
        End If
        ErrorProvider1.SetError(cmbTRPointLayer, errstr)
        Return errstr
    End Function

    Private Function validate_cmbTRWaterbodies() As String
        Dim errstr As String = ""
        If chkTRUseWaterbodies.Checked Then
            If cmbTRWaterbodies.SelectedItem Is Nothing Then
                errstr = "Please select a water bodies layer"
            End If
        End If
        ErrorProvider1.SetError(cmbTRWaterbodies, errstr)
        Return errstr
    End Function

    Private Function validate_cmbTRPaths() As String
        Dim errstr As String = ""
        If cmbTRPaths.SelectedItem Is Nothing Then
            errstr = "Please select a particle paths layer"
        End If
        ErrorProvider1.SetError(cmbTRPaths, errstr)
        Return errstr
    End Function

    Private Function validate_cmbTRSolType() As String
        Dim errstr As String = ""
        If cmbTRSolType.SelectedItem Is Nothing Then
            errstr = "Please select a solution type"
        End If
        ErrorProvider1.SetError(cmbTRSolType, errstr)
        Return errstr
    End Function
    'Note by Yan 20141029:This is for spatial distribution of Min.
    'Note by Yan 20141029: The original function is for fixed Min.
    'Private Function validate_txtTRM_in() As String
    'Dim errstr As String = ""
    'Dim d As Double
    'If txtTRM_in.Enabled Then
    'Try
    'If Not Double.TryParse(txtTRM_in.Text, d) Then Throw New Exception("You must specify a valid number for the Mass input rate")
    'If d <= 0 Then Throw New Exception("The mass input rate must be greater than zero.")
    'Catch ex As Exception
    'errstr = ex.Message
    'End Try
    'End If
    'ErrorProvider1.SetError(txtTRM_in, errstr)
    'Return errstr
    'End Function

    Private Function validate_txtTRM_in() As String
        Dim d As Double
        Dim errstr As String = ""

        If txtTRM_in.Enabled Then
            Try
                If Not Double.TryParse(txtTRM_in.Text, d) Then Throw New Exception("You must specify a valid number for Min")

                'only perform this check if the feature class attribute table doesn't contain this field
                'the less than zero condition will be checked by the Transport class
                If txtTRM_in.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d < 0 Then Throw New Exception("Min must be greater than zero.")
                End If
                If d = 0 Then Throw New Exception("Min must be greater than zero.")
            Catch ex As Exception
                errstr = ex.Message
            End Try
        End If

        ErrorProvider1.SetError(txtTRM_in, errstr)
        Return errstr
    End Function




    Private Function validate_txtTRSourceY() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        Try
            If Not Double.TryParse(txtTRSourceY.Text, d) Then Throw New Exception("You must specify a valid number for the Y source dimension")
            If d <= 0 Then Throw New Exception("The Y source dimension must be greater than zero.")
        Catch ex As Exception
            errstr = ex.Message
        End Try
        ErrorProvider1.SetError(txtTRSourceY, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRSourceZ() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim d As Double
        Dim errstr As String = ""
        If txtTRSourceZ.Enabled Then
            Try
                If Not Double.TryParse(txtTRSourceZ.Text, d) Then Throw New Exception("You must specify a valid number for the Z source dimension")
                If d <= 0 Then Throw New Exception("The Z source dimension must be greater than zero.")

                'set the dz value to this
                txtTRMeshZ.Text = d

            Catch ex As Exception
                errstr = ex.Message
            End Try
        End If
        ErrorProvider1.SetError(txtTRSourceZ, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRMeshX(Optional ByVal b As Boolean = False) As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        Try
            If Not Double.TryParse(txtTRMeshX.Text, d) Then Throw New Exception("You must specify a valid number for the x mesh size")
            If d <= 0 Then Throw New Exception("The x mesh size must be greater than zero.")
            If validate_txtTRSourceY() = "" Then
                If d > txtTRSourceY.Text / 5 Then
                    Dim msg As String = "The cell size " & d & " may be too large for the specified source dimensions " & txtTRSourceY.Text & ". Accuracy of concentration profiles may be too low.  Suggested values for the cell size are between 5 and 30 times smaller than the source width."
                    Trace.WriteLine(msg)
                    If Not b AndAlso MsgBox(msg & " Continue anyways?", MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo, "Warning") = MsgBoxResult.No Then
                        Throw New Exception("The cell size may be too large")
                    End If
                End If
                If Utilities.DivRem(txtTRSourceY.Text, d) <> 0 Then
                    Throw New Exception("The Y source dimension must be an integer multiple of the cell size!")
                End If
            End If
        Catch ex As Exception
            errstr = ex.Message
        End Try
        ErrorProvider1.SetError(txtTRMeshX, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRMeshY() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        Try
            If Not Double.TryParse(txtTRMeshY.Text, d) Then Throw New Exception("You must specify a valid number for the y mesh size")
            If d <= 0 Then Throw New Exception("The y mesh size must be greater than zero.")
            If txtTRMeshY.Text <> txtTRMeshX.Text Then Throw New Exception("Different mesh sizes for x and y are not supported")
        Catch ex As Exception
            errstr = ex.Message
        End Try
        ErrorProvider1.SetError(txtTRMeshY, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRMeshZ() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        If txtTRMeshZ.Enabled Then
            Try
                If Not Double.TryParse(txtTRMeshZ.Text, d) Then Throw New Exception("You must specify a valid number for the z mesh size")
                If d <= 0 Then Throw New Exception("The z mesh size must be greater than zero.")
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            If Not Double.TryParse(txtTRMeshZ.Text, d) Then
                txtTRMeshZ.Text = 0
            End If
        End If
        ErrorProvider1.SetError(txtTRMeshZ, errstr)
        Return errstr
    End Function


    Private Function validate_txtTRDispL() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        Try
            If Not Double.TryParse(txtTRDispL.Text, d) Then Throw New Exception("You must specify a valid number for the longitudinal dispersivity")

            'only perform this check if the feature class attribute table doesn't contain this field
            If txtTRDispL.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                If d < 0 Then Throw New Exception("The longitudinal dispersivity must be greater than or equal to zero.")
            End If
        Catch ex As Exception
            errstr = ex.Message
        End Try
        ErrorProvider1.SetError(txtTRDispL, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRDispTH() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        Try
            If Not Double.TryParse(txtTRDispTH.Text, d) Then Throw New Exception("You must specify a valid number for the transverse horizontal dispersivity")

            'only perform this check if the feature class attribute table doesn't contain this field
            If txtTRDispTH.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                If d < 0 Then Throw New Exception("The transverse horizontal dispersivity must be greater than or equal to zero.")
            End If
        Catch ex As Exception
            errstr = ex.Message
        End Try
        ErrorProvider1.SetError(txtTRDispTH, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRDispTV() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        If txtTRDispTV.Enabled Then
            Try
                If Not Double.TryParse(txtTRDispTV.Text, d) Then Throw New Exception("You must specify a valid number for the transverse vertical dispersivity")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRDispTV.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d < 0 Then Throw New Exception("The transverse vertical dispersivity must be greater than or equal to zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            If Not Double.TryParse(txtTRDispTV.Text, d) Then
                txtTRDispTV.Text = 0
            End If
        End If
        ErrorProvider1.SetError(txtTRDispTV, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRThreshConc() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        Dim f As AdditionalOptionsAndParamsTR = CType(tsTRMoreParams.Items.Item(0), Windows.Forms.ToolStripControlHost).Control
        Try
            If Not Double.TryParse(f.txtTRThreshConc.Text, d) Then Throw New Exception("You must specify a valid number for the threshold concentration")
            If d < 0 Then Throw New Exception("The threshold concentration must be greater than or equal to zero.")
            If d = 0 Then
                If MsgBox("Setting the threshold to zero may cause excessive memory consumption and increased running time.  Continue anyways?", MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo) = MsgBoxResult.No Then
                    Throw New Exception("Enter a value greater than zero")
                End If
            End If
            If d > 0.001 Then
                Dim msg As String = "Setting the threshold (" & d & ") too high may cause excessive mass balance errors."
                If MsgBox(msg & " Continue anyways?", MsgBoxStyle.YesNo Or MsgBoxStyle.Exclamation) = MsgBoxResult.No Then
                    Throw New Exception("Threshold too high")
                End If
            End If
        Catch ex As Exception
            errstr = ex.Message
        End Try
        ErrorProvider1.SetError(f.txtTRThreshConc, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRSolTime() As String
        Dim errstr As String = ""
        Dim d As Double
        If txtTRSolTime.Enabled Then
            Try
                If Not Double.TryParse(txtTRSolTime.Text, d) Then Throw New Exception("You must specify a valid number for the solution time")
                If d <= 0 Then Throw New Exception("The solution time must be greater than zero.")
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            'this will happen when the selected solution type is steady state
            '(this is the only available option currently)
            If Not Double.TryParse(txtTRSolTime.Text, d) Then
                txtTRSolTime.Text = -1
            End If
        End If
        ErrorProvider1.SetError(txtTRSolTime, errstr)
        Return errstr
    End Function

    Private Function validate_txtTROutputPlumes() As String
        Dim errstr As String = ""
        txtTROutputPlumes.Text = txtTROutputPlumes.Text.Trim
        If txtTROutputPlumes.Text = "" Then
            errstr = "Please select an output raster for the plumes"
        ElseIf Utilities.checkExist(txtTROutputPlumes.Text) Then
            errstr = "The output raster '" & txtTROutputPlumes.Text & "' already exists! Please choose a different name"
        End If
        ErrorProvider1.SetError(txtTROutputPlumes, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRDecay() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim d As Double
        Dim errstr As String = ""
        If txtTRDecay.Enabled Then
            Try
                If Not Double.TryParse(txtTRDecay.Text, d) Then Throw New Exception("You must specify a valid number for the Decay")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRDecay.BackColor = System.Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d <= 0 Then Throw New Exception("The decay coefficient must be greater than zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            If Not Double.TryParse(txtTRDecay.Text, d) Then
                txtTRDecay.Text = 0
            End If
        End If
        ErrorProvider1.SetError(txtTRDecay, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRC0() As String
        Dim d As Double
        Dim errstr As String = ""

        Try
            If Not Double.TryParse(txtTRC0.Text, d) Then Throw New Exception("You must specify a valid number for C0")

            'only perform this check if the feature class attribute table doesn't contain this field
            'the less than zero condition will be checked by the Transport class
            If txtTRC0.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                If d < 0 Then Throw New Exception("C0 must be greater than zero.")
            End If
            If d = 0 Then Throw New Exception("C0 must be greater than zero.")
        Catch ex As Exception
            errstr = ex.Message
        End Try

        ErrorProvider1.SetError(txtTRC0, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRWarpingCtrlPtSpacing() As String
        Dim f As AdditionalOptionsAndParamsTR = CType(tsTRMoreParams.Items.Item(0), Windows.Forms.ToolStripControlHost).Control
        Dim tso As ToolStripOverflow = f.Parent
        Dim errstr As String = ""
        Dim d As Double
        Dim thresh As Integer

        If validate_txtTRMeshX(True) = "" Then
            d = txtTRMeshX.Text

            'set some reasonable looking values for the control point spacing            
            Select Case Math.Truncate(d / 6)
                Case Is <= 0
                    'dx between 0 and 5
                    thresh = 48
                Case Is <= 1
                    'between 6 and 11
                    thresh = 36
                Case Is <= 2
                    'between 12 and 17
                    thresh = 24
                Case Is <= 3
                    'between 18 and 23
                    thresh = 12
                Case Is <= 4
                    'between 24 and 29
                    thresh = 6
                Case Else
                    '30 and above
                    thresh = 3
            End Select

            If f.txtTRWarpingCtrlPtSpacing.Value > thresh Then
                Trace.WriteLine("The control point spacing (" & f.txtTRWarpingCtrlPtSpacing.Value & ") may be too large for the specified grid size (dx= " & d & ").  Suggested value= " & thresh)
                If MsgBox("The control point spacing may be too large for the specified grid size. Change now?.", MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo, "Warning") = MsgBoxResult.Yes Then
                    'the user has decided to change the value. need to cancel the validation
                    'without throwing an error.  Throwing an error causes the error msgbox to
                    'pop up, causing the extended options window to close                    
                    tso.Show()
                    ErrorProvider1.SetError(f.txtTRWarpingCtrlPtSpacing, "Consider lowering this value to " & thresh)
                    Return "-1"
                End If
            End If
        End If

        ErrorProvider1.SetError(f.txtTRWarpingCtrlPtSpacing, errstr)
        Return errstr
    End Function
    ' add the NH4 calculation paramter validator functions.
    Private Function validate_txtTRC0_NH4() As String
        Dim d As Double
        Dim errstr As String = ""
        If txtTRC0_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRC0_NH4.Text, d) Then Throw New Exception("You must specify a valid number for NH4_C0")

                'only perform this check if the feature class attribute table doesn't contain this field
                'the less than zero condition will be checked by the Transport class
                If txtTRC0_NH4.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d < 0 Then Throw New Exception("NH4_C0 must be greater than zero.")
                End If
                If d = 0 Then Throw New Exception("NH4_C0 must be greater than zero.")
            Catch ex As Exception
                errstr = ex.Message
            End Try
        End If

        ErrorProvider1.SetError(txtTRC0_NH4, errstr)
        Return errstr
    End Function
    'Private Function validate_txtTRM_in_NH4() As String
    'Dim errstr As String = ""
    'Dim d As Double
    'If txtTRM_in_NH4.Enabled Then
    'Try
    'If Not Double.TryParse(txtTRM_in_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the NH4 Mass input rate")
    'If d <= 0 Then Throw New Exception("The NH4 mass input rate must be greater than zero.")
    'Catch ex As Exception
    'errstr = ex.Message
    'End Try
    'End If
    'ErrorProvider1.SetError(txtTRM_in_NH4, errstr)
    'Return errstr
    'End Function

    'Private Function validate_txtTRSourceZ_NH4() As String
    'could use a numeric up/down but it takes up too much space on the form
    'Dim d As Double
    'Dim errstr As String = ""
    'If chkTRUseNH4.Checked AndAlso txtTRSourceZ.Enabled Then
    'Try
    'If Not Double.TryParse(txtTRSourceZ_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the Z source dimension")
    'If d <= 0 Then Throw New Exception("The Z source dimension must be greater than zero.")
    ' Catch ex As Exception
    'errstr = ex.Message
    'End Try
    'End If
    'ErrorProvider1.SetError(txtTRSourceZ_NH4, errstr)
    'Return errstr
    'End Function
    Private Function validate_txtTRDispL_NH4() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        If txtTRDispL_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRDispL_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the longitudinal dispersivity of NH4")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRDispL_NH4.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d < 0 Then Throw New Exception("The longitudinal dispersivity of NH4 must be greater than or equal to zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        End If
        ErrorProvider1.SetError(txtTRDispL_NH4, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRDispTH_NH4() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        If txtTRDispTH_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRDispTH_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the transverse horizontal dispersivity of NH4")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRDispTH_NH4.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d < 0 Then Throw New Exception("The transverse horizontal dispersivity of NH4 must be greater than or equal to zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        End If
        ErrorProvider1.SetError(txtTRDispTH_NH4, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRDecay_NH4() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim d As Double
        Dim errstr As String = ""
        If txtTRDecay_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRDecay_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the nitrification")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRDecay_NH4.BackColor = System.Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d <= 0 Then Throw New Exception("The nitrification coefficient must be greater than zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            If Not Double.TryParse(txtTRDecay_NH4.Text, d) Then
                txtTRDecay_NH4.Text = 0
            End If
        End If
        ErrorProvider1.SetError(txtTRDecay_NH4, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRAdsorp_NH4() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim d As Double
        Dim errstr As String = ""
        If txtTRAdsorp_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRAdsorp_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the adsorption")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRAdsorp_NH4.BackColor = System.Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d <= 0 Then Throw New Exception("The adsorption coefficient must be greater than zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            If Not Double.TryParse(txtTRAdsorp_NH4.Text, d) Then
                txtTRAdsorp_NH4.Text = 0
            End If
        End If
        ErrorProvider1.SetError(txtTRAdsorp_NH4, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRBulkdensity_NH4() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim d As Double
        Dim errstr As String = ""
        If txtTRBulkdensity_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRBulkdensity_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the bulk density")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRBulkdensity_NH4.BackColor = System.Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d <= 0 Then Throw New Exception("The bulk density must be greater than zero.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        Else
            If Not Double.TryParse(txtTRBulkdensity_NH4.Text, d) Then
                txtTRBulkdensity_NH4.Text = 0
            End If
        End If
        ErrorProvider1.SetError(txtTRBulkdensity_NH4, errstr)
        Return errstr
    End Function
    Private Function validate_txtTRAverageTheta_NH4() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim d As Double
        Dim errstr As String = ""
        If txtTRAverageTheta_NH4.Enabled Then
            Try
                If Not Double.TryParse(txtTRAverageTheta_NH4.Text, d) Then Throw New Exception("You must specify a valid number for the average theta")

                'only perform this check if the feature class attribute table doesn't contain this field
                If txtTRBulkdensity_NH4.BackColor = System.Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                    If d <= 0 Then Throw New Exception("The average theta must be greater than zero.")
                    If d >= 1.0 Then Throw New Exception("The average theta must be less than 1.0.")
                End If
            Catch ex As Exception
                errstr = ex.Message
            End Try
        End If
        ErrorProvider1.SetError(txtTRAverageTheta_NH4, errstr)
        Return errstr
    End Function

    Private Function validate_txtTROutputPlumes_NH4() As String
        Dim errstr As String = ""
        If txtTROutputPlumes_NH4.Enabled Then
            txtTROutputPlumes_NH4.Text = txtTROutputPlumes_NH4.Text.Trim
            If txtTROutputPlumes_NH4.Text = "" Then
                errstr = "Please select an output raster for the plumes"
            ElseIf Utilities.checkExist(txtTROutputPlumes_NH4.Text) Then
                errstr = "The output raster '" & txtTROutputPlumes_NH4.Text & "' already exists! Please choose a different name"
            End If
        End If
        ErrorProvider1.SetError(txtTROutputPlumes_NH4, errstr)
        Return errstr
    End Function

    Private Function validate_txtTRSourceZ_max() As String
        'could use a numeric up/down but it takes up too much space on the form
        Dim errstr As String = ""
        Dim d As Double
        If chk_Specify_Zmax.Checked Then
            If txtTRSourceZ_max.Enabled Then
                Try
                    If Not Double.TryParse(txtTRSourceZ_max.Text, d) Then Throw New Exception("You must specify a valid number for the maximum specified plume thickness Z value")

                    'only perform this check if the feature class attribute table doesn't contain this field
                    If txtTRSourceZ_max.BackColor = Drawing.Color.FromKnownColor(Drawing.KnownColor.Window) Then
                        If d < 0 Then Throw New Exception("The maximum specified plume thickness Z must be greater than or equal to zero.")
                    End If
                Catch ex As Exception
                    errstr = ex.Message
                End Try
            End If
        End If
        ErrorProvider1.SetError(txtTRSourceZ_max, errstr)
        Return errstr
    End Function
    'End adding the validators for NH4 calculation.
#End Region


#Region "Helpers"
    ''' <summary>
    ''' initializes the components on this tab
    ''' </summary>
    ''' <remarks>Should be called from the forms load event</remarks>
    Private Sub TRInit()

        'populate the dropdowns with the map's layers.
        TRPopulateDropdowns()

        'register the form validators
        TRValidators.Clear()
        TRValidators.Add(New validator(AddressOf validate_cmbTRPaths))
        TRValidators.Add(New validator(AddressOf validate_cmbTRPointLayer))
        TRValidators.Add(New validator(AddressOf validate_cmbTRSolType))
        TRValidators.Add(New validator(AddressOf validate_cmbTRWaterbodies))
        TRValidators.Add(New validator(AddressOf validate_txtTRDispL))
        TRValidators.Add(New validator(AddressOf validate_txtTRDispTH))
        TRValidators.Add(New validator(AddressOf validate_txtTRDispTV))
        TRValidators.Add(New validator(AddressOf validate_txtTRMeshX))
        TRValidators.Add(New validator(AddressOf validate_txtTRMeshY))
        TRValidators.Add(New validator(AddressOf validate_txtTRMeshZ))
        TRValidators.Add(New validator(AddressOf validate_txtTRSolTime))
        TRValidators.Add(New validator(AddressOf validate_txtTRSourceY))
        TRValidators.Add(New validator(AddressOf validate_txtTRSourceZ))
        TRValidators.Add(New validator(AddressOf validate_txtTRThreshConc))
        TRValidators.Add(New validator(AddressOf validate_txtTRWarpingCtrlPtSpacing))
        TRValidators.Add(New validator(AddressOf validate_txtTROutputPlumes))
        TRValidators.Add(New validator(AddressOf validate_txtTRC0))
        TRValidators.Add(New validator(AddressOf validate_txtTRDecay))
        TRValidators.Add(New validator(AddressOf validate_txtTRM_in))
        'Add the validators for NH4 paramter.
        TRValidators.Add(New validator(AddressOf validate_txtTRC0_NH4))
        'TRValidators.Add(New validator(AddressOf validate_txtTRM_in_NH4))
        'TRValidators.Add(New validator(AddressOf validate_txtTRSourceZ_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRDispL_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRDispTH_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRDecay_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRAdsorp_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRBulkdensity_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRAverageTheta_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTROutputPlumes_NH4))
        TRValidators.Add(New validator(AddressOf validate_txtTRSourceZ_max))

    End Sub
    ''' <summary>
    ''' Populates the drop down boxes with the appropriate layers
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub TRPopulateDropdowns()
        cmbTRPointLayer.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
        cmbTRWaterbodies.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
        cmbTRPaths.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline)

        'add the control to the toolstrip. note the control must have a minimum size
        'set because sometimes it will collapse to 0 making it invisible.
        'only do this routine the first time it is called to avoid refreshing
        'the dropdowns on ArcGIS layer change events.
        Dim th As Windows.Forms.ToolStripControlHost
        Dim moreopt As AdditionalOptionsAndParamsTR
        If tsTRMoreParams.Items.Count = 0 Then
            moreopt = New AdditionalOptionsAndParamsTR
            th = New Windows.Forms.ToolStripControlHost(moreopt)
            th.Overflow = ToolStripItemOverflow.Always
            tsTRMoreParams.Items.Add(th)

            m_cmbMoreopt_dombdy = moreopt.cmbTRDomenicoBdy

            'the members of DictionaryEntry
            cmbTRSolType.DisplayMember = "Key"
            cmbTRSolType.ValueMember = "Value"

            'use SS 2D domenico
            cmbTRSolType.Items.Add(New DictionaryEntry(SolutionTypes.SolutionType.DomenicoRobbinsSS2D.ToString, SolutionTypes.SolutionType.DomenicoRobbinsSS2D))
            'use SS 2D domenico with decay
            cmbTRSolType.Items.Add(New DictionaryEntry(SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D.ToString, SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D))
            cmbTRSolType.SelectedIndex = 1

            Dim f As AdditionalOptionsAndParamsTR = CType(tsTRMoreParams.Items.Item(0), ToolStripControlHost).Control
            f.cmbTRWarpingMethod.Items.AddRange([Enum].GetNames(GetType(WarpingMethods.WarpingMethod)))
            f.cmbTRWarpingMethod.SelectedItem = [Enum].GetName(GetType(WarpingMethods.WarpingMethod), WarpingMethods.WarpingMethod.Polynomial2)
            f.cmbTRPostProcessing.Items.AddRange([Enum].GetNames(GetType(PostProcessing.PostProcessingAmount)))
            f.cmbTRPostProcessing.SelectedItem = [Enum].GetName(GetType(PostProcessing.PostProcessingAmount), PostProcessing.PostProcessingAmount.Medium)
            f.cmbTRDomenicoBdy.Items.AddRange([Enum].GetNames(GetType(DomenicoSourceBoundaries.DomenicoSourceBoundary)))
            f.cmbTRDomenicoBdy.SelectedItem = [Enum].GetName(GetType(DomenicoSourceBoundaries.DomenicoSourceBoundary), DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate)
        End If

    End Sub
#End Region

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
    Public Function runTransp(Optional ByVal AddOutputToActiveMap As Boolean = True) As Boolean
        GC.Collect()
        GC.WaitForPendingFinalizers()

        Trace.WriteLine("Transport: START")

        Dim errOccurred As Boolean = False
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser

        Try
            'validate the textbox inputs
            Dim err As String = ""
            For Each v As validator In TRValidators
                err = v()
                If err <> "" AndAlso err <> "-1" Then
                    Throw New Exception(err)
                ElseIf err = "-1" Then
                    Trace.WriteLine("validation cancelled")
                    Return True
                End If
            Next

            'if were here, the form validated successfully
            Trace.WriteLine("Transport: Form inputs validated")

            'gather the inputs
            Dim sel_waterbodies As FeatureLayer = Nothing
            Dim sel_sources As FeatureLayer = Nothing
            Dim sel_particlepaths As FeatureLayer = Nothing
            Dim sel_solutiontype As SolutionTypes.SolutionType = cmbTRSolType.SelectedItem.value
            Dim sel_sourceY As Single = txtTRSourceY.Text
            Dim sel_sourceZ As Single 'see below for sourceZ
            Dim sel_meshX As Single = txtTRMeshX.Text
            Dim sel_meshy As Single = txtTRMeshY.Text
            Dim sel_meshz As Single = txtTRMeshZ.Text
            Dim sel_dispL As Single = txtTRDispL.Text
            Dim sel_dispTH As Single = txtTRDispTH.Text
            Dim sel_dispTV As Single = txtTRDispTV.Text
            Dim sel_soltime As Integer = txtTRSolTime.Text  'since the current solution types are all steady state, this will be -1
            Dim f As AdditionalOptionsAndParamsTR = CType(tsTRMoreParams.Items.Item(0), ToolStripControlHost).Control
            Dim sel_ctrlptspac As Integer = f.txtTRWarpingCtrlPtSpacing.Value
            Dim sel_decay As Single = txtTRDecay.Text
            Dim sel_concinit As Single = txtTRC0.Text
            Dim sel_warpmethod As WarpingMethods.WarpingMethod = [Enum].Parse(GetType(WarpingMethods.WarpingMethod), f.cmbTRWarpingMethod.SelectedItem)
            Dim sel_useapproxwarp As Boolean = f.chkTRWarpingUseApprox.Checked
            Dim sel_postprocamt As PostProcessing.PostProcessingAmount = [Enum].Parse(GetType(PostProcessing.PostProcessingAmount), f.cmbTRPostProcessing.SelectedItem)
            Dim sel_domenicoBdy As DomenicoSourceBoundaries.DomenicoSourceBoundary = [Enum].Parse(GetType(DomenicoSourceBoundaries.DomenicoSourceBoundary), f.cmbTRDomenicoBdy.SelectedItem)
            Dim sel_concthresh As Single = f.txtTRThreshConc.Text
            Dim sel_r_outputpath As String = txtTROutputPlumes.Text
            Dim sel_volConversionFac As Single = Decimal.ToInt32(txtTRVolConversionFac.Value)
            Dim sel_outputintermediateplumes As Boolean
            'Add the inputs for NH4 calculation.
            Dim sel_concinit_NH4 As Single = txtTRC0_NH4.Text
            Dim sel_sourceZ_NH4 As Single = 0.0
            Dim sel_dispL_NH4 As Single = txtTRDispL_NH4.Text
            Dim sel_dispTH_NH4 As Single = txtTRDispTH_NH4.Text
            Dim sel_decay_NH4 As Single = txtTRDecay_NH4.Text
            Dim sel_adsorp_NH4 As Single = txtTRAdsorp_NH4.Text
            Dim sel_r_outputpath_NH4 As String = txtTROutputPlumes_NH4.Text
            Dim sel_Bulkdensity_NH4 As String = txtTRBulkdensity_NH4.Text
            Dim sel_AverageTheta_NH4 As String = txtTRAverageTheta_NH4.Text
            Dim sel_decay_NH4_calculation As Single = 0.0
            Dim sel_concinit_calculation As Single = 0.0
            Dim sel_concinit_calculation_value As Single = 0.0
            Dim sel_calculation_NH4 As Boolean = chkTRUseNH4.Checked
            Dim sel_concinit_NO3 As Single
            Dim sel_chk_Specify_Zmax As Boolean = chk_Specify_Zmax.Checked
            Dim sel_sourceZ_max As Single = txtTRSourceZ_max.Text
            'End adding the inputs for NH4 calculation


            If My.Settings.OuputIntermediateCalcs Then
                If MsgBox("Output of intermediate calculations is enabled for the Transport module. Output all intermediate results?" & vbCrLf & vbCrLf & _
                          "To output all calculations, including the temp. calculations for each individual plume, click YES" & vbCrLf & _
                          "To exclude individual plumes (faster) and just output other intermediate results, click NO", MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2, "ArcNLET") = MsgBoxResult.Yes Then
                    sel_outputintermediateplumes = True
                Else
                    sel_outputintermediateplumes = False
                End If
            End If

            'occurs when the use waterbodies checkbox is unchecked
            'otherwise, will get a null reference exception when we try to access baselayer
            If chkTRUseWaterbodies.Checked AndAlso Not cmbTRWaterbodies.SelectedItem Is Nothing Then
                sel_waterbodies = cmbTRWaterbodies.SelectedItem.baselayer
                If sel_waterbodies.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("Water bodies input contains no features")
            End If
            sel_sources = cmbTRPointLayer.SelectedItem.baselayer
            sel_particlepaths = cmbTRPaths.SelectedItem.baselayer
            If sel_sources.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("Sources input contains no features")
            If sel_particlepaths.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("Particle Paths input contains no features")


            'use sourceZ to pass in the mass input load
            If sel_domenicoBdy = DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate Then
                sel_sourceZ = txtTRM_in.Text
            Else
                sel_sourceZ = txtTRSourceZ.Text
            End If

            'Add the NH4 calculation.
            If chkTRUseNH4.Checked Then
                If sel_domenicoBdy = DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate Then
                    sel_sourceZ_NH4 = txtTRM_in.Text
                Else
                    sel_sourceZ_NH4 = txtTRSourceZ.Text
                End If
            End If
            If chkTRUseNH4.Checked Then
                sel_decay_NH4_calculation = (1.0 + sel_Bulkdensity_NH4 * sel_adsorp_NH4 / sel_AverageTheta_NH4) * sel_decay_NH4
                'Trace.WriteLine(sel_decay_NH4_calculation)
                If sel_concinit <> -1 Then
                    If sel_concinit_NH4 <> -1 Then
                        sel_concinit_NH4 = txtTRC0_NH4.Text
                        'Trace.WriteLine(txtTRC0_NH4.Text)
                        sel_concinit_calculation_value = sel_decay_NH4_calculation * sel_concinit_NH4 / (sel_decay_NH4_calculation - sel_decay)
                        sel_concinit_calculation = txtTRC0.Text + sel_concinit_calculation_value
                        sel_concinit_NO3 = txtTRC0.Text
                    Else
                        sel_concinit_calculation = -1
                        sel_concinit_NO3 = sel_concinit
                    End If
                Else
                    sel_concinit_calculation = sel_concinit
                    sel_concinit_NO3 = sel_concinit
                End If
            Else
                sel_concinit_calculation = sel_concinit
            End If
            'End adding.


            'check layer spatial references.  Sometimes get unexplainable errors
            'and/or results when the spatial references are different.
            Dim layers As New List(Of ILayer)
            layers.Add(sel_waterbodies)
            layers.Add(sel_sources)
            layers.Add(sel_particlepaths)
            If Not Utilities.checkLayerSpatialReferences(layers, Main.ActiveMap) Then
                Throw New Exception("Input data must have the same spatial references")
            End If
            Trace.WriteLine("Input spatial referecenes OK")

            '***********************************
            'server channel
            'used by the client to get the parameters and report the result
            '************************************
            Try
                'Create and register an IPC channel
                'use a unique name so that we can have multiple instances without error
                'also relax permissions to avoid security exceptions
                Dim provider As New System.Runtime.Remoting.Channels.BinaryServerFormatterSinkProvider
                provider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full
                m_chanwrapper = New IpcServerChannel(portName:="aq-dn-main" & Now.Ticks, sinkProvider:=provider, Name:="aq-dn-main" & Now.Ticks)
                ChannelServices.RegisterChannel(m_chanwrapper, False)

                'Expose an object
                RemotingConfiguration.RegisterWellKnownServiceType(GetType(ModuleRunRemote), "srv", WellKnownObjectMode.Singleton)

                'Wait for calls
                Trace.WriteLine(String.Format(Assembly.GetExecutingAssembly.GetName.Name & " Listening on {0}", m_chanwrapper.GetChannelUri()))
            Catch ex As Exception
                Throw New Exception("Could not create wrapper-client server channel" & vbCrLf & ex.ToString)
            End Try

            '***************************
            'setup the object to be remoted
            '****************************
            Trace.WriteLine("Start seting up the object to be remoted")
            m_transportRunner = New ModuleRunRemote(Me)
            Dim h As New Hashtable
            h.Add("particletracks_path", CType(sel_particlepaths.FeatureClass, IDataset).Workspace.PathName)
            h.Add("particletracks_shpname", CType(sel_particlepaths.FeatureClass, IDataset).BrowseName)          'no shp extension is added
            h.Add("sources_path", CType(sel_sources, IDataset).Workspace.PathName)
            h.Add("sources_shpname", CType(sel_sources, IDataset).BrowseName)                                   'no shp extension is added
            If chkTRUseWaterbodies.Checked AndAlso Not sel_waterbodies Is Nothing Then
                h.Add("waterbodies_path", CType(sel_waterbodies, IDataset).Workspace.PathName)
                h.Add("waterbodies_shpname", CType(sel_waterbodies, IDataset).BrowseName)                       'no shp extension is added
            Else
                h.Add("waterbodies_path", Nothing)
                h.Add("waterbodies_shpname", Nothing)
            End If
            h.Add("Y", sel_sourceY)
            h.Add("Z", sel_sourceZ)
            h.Add("ax", sel_dispL)
            h.Add("ay", sel_dispTH)
            h.Add("az", sel_dispTV)
            h.Add("mesh_x", sel_meshX)
            h.Add("mesh_y", sel_meshy)
            h.Add("mesh_z", sel_meshz)
            h.Add("conc_init", sel_concinit_calculation)
            h.Add("conc_thresh", sel_concthresh)
            h.Add("solution_time", sel_soltime)
            h.Add("solution_type", sel_solutiontype)
            h.Add("decay_coeff", sel_decay)
            h.Add("warp_ctrlptspac", sel_ctrlptspac)
            h.Add("warp_method", sel_warpmethod)
            h.Add("warp_useapprox", sel_useapproxwarp)
            h.Add("postprocessing", sel_postprocamt)
            h.Add("output_intermediate", mnuOutputIntermediateToolStripMenuItem.Checked)
            h.Add("output_intermediate_plumes", sel_outputintermediateplumes)
            h.Add("raster_output_path", sel_r_outputpath)
            h.Add("vol_conversion_fac", sel_volConversionFac)
            h.Add("domenico_bdy", sel_domenicoBdy)
            h.Add("max_mem", My.Settings.MaxMemory)
            'Add parameters for NH4 calculation.
            h.Add("Z_NH4", sel_sourceZ_NH4)
            h.Add("ax_NH4", sel_dispL_NH4)
            h.Add("ay_NH4", sel_dispTH_NH4)
            h.Add("conc_init_NH4", sel_concinit_NH4)
            h.Add("decay_coeff_NH4", sel_decay_NH4)
            h.Add("Adsorp_Coeff_NH4", sel_adsorp_NH4)
            h.Add("raster_output_path_NH4", sel_r_outputpath_NH4)
            h.Add("decay_coeff_NH4_calculation", sel_decay_NH4_calculation)
            h.Add("NH4_calculation_checked", sel_calculation_NH4)
            h.Add("conc_init_NO3", sel_concinit_NO3)
            h.Add("z_max", sel_sourceZ_max)
            h.Add("z_max_checked", sel_chk_Specify_Zmax)
            'End adding.
            m_transportRunner.InParams = h

            '***************************************
            'start the client process
            'the client is assumed to be in the same directory as this assembly.
            'this will be true if the post build event in AqDnWrapper sucessfully copies AqDnwrapper
            'to the output folder
            '**************************************
            Trace.WriteLine("Running module Transport...")
            btnAbort.Enabled = True
            Trace.Flush()
            Windows.Forms.Application.DoEvents()
            startTRWrapper()

            'wait for the calculation to finish
            'not the most elegant solution but it is simple
            m_finished = False
            While Not m_finished
                'sleep for a bit so as not to take up 100% cpu
                'sleeping this thread also causes the wrapper to block
                'because of the trace calls. Therefore don't set this value
                'high or the calculation will slow down considerably
                Threading.Thread.Sleep(2)
                'imporant so that form events (such as trace outputs or mouse clicks)
                'are registered
                Windows.Forms.Application.DoEvents()
            End While
            If Not m_finished_ok Then Throw New Exception("There was an error running the module or the run was aborted")

            Trace.WriteLine("Running module Transport...done")
            Dim results As Hashtable = m_transportRunner.OutParams
            m_transportRunner = Nothing 'necessary so that dropdowns refresh when they're supposed to

            If AddOutputToActiveMap Then
                'retrieve the output parameters
                Utilities.AddOutParamDatasetsToActiveMap(results)
            End If

        Catch ex As Exception
            Trace.WriteLine("[Error] Transport (" & Reflection.MethodInfo.GetCurrentMethod.Name & "): " & ex.Message)
            errOccurred = True
        End Try

        btnAbort.Enabled = False
        Trace.WriteLine("Transport: FINISHED")

        m_TransportBootstrapper = Nothing
        m_transportRunner = Nothing

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
    Friend Sub cancelTransport()
        If m_TransportBootstrapper Is Nothing Then
            Return
        End If

        'just kill the process...
        m_TransportBootstrapper.kill()
        Return

    End Sub

    ''' <summary>
    ''' Receives the notification from ModuleRunRemote that the transport process has terminated
    ''' </summary>
    ''' <param name="msg">system error code. 0 means no error</param>
    ''' <remarks>This is meant to notify of process related errors (ie. the process was killed or any serious error)
    ''' When the wrapper executable terminates normally, the error code is 0 regardless of the
    ''' actual calculation result</remarks>
    Public Sub ProcessTerminated(ByVal msg As String)
        If msg = "0" Then
            m_finished_ok = True
        Else
            Trace.WriteLine("Transport process exited with errors. Code: " & msg)
            m_finished_ok = False
        End If
        m_finished = True
    End Sub

    ''' <summary>
    ''' Receives the notification from TransportRunRemote that the calculation is complete
    ''' </summary>
    ''' <param name="msg">The message. If it is the empty string "", it means there are no
    ''' error messages and the calculation completed sucessfully</param>
    ''' <remarks>See <see cref="ProcessTerminated"/></remarks>
    ''' 
    Public Sub CalculationCompleted(ByVal msg As String)
        If msg = "" Then
            m_finished_ok = True
        Else
            Trace.WriteLine(msg)
            m_finished_ok = False
        End If
        m_finished = True
    End Sub

#Region "Create New AppDomain"
    Private Sub startTRWrapper()

        'Create a new application domain
        'This is critical so that we can run the wrapper in it's own memory space.  
        'Without creating a new app domain, the assembly binder won't be
        'able to find our assemblies when it needs to do type checking (see ApplicationBase
        'below)
        Try

            ' Create application domain setup information.                
            'set the base path for the new domain. this is critical so that the assemblies can be
            'found when the .Net remoting assembly binder needs to bind the assemblies to check types.  
            'Without this, you will get FileNotFoundExceptions when trying to use remoting
            'since the base path will be set by default to the ArcMap.exe folder.  It is set like that by
            'default since this program is loaded by ArcMap.
            '
            '
            'It is assumed that the wrapper and bootstrapper are in the same directory as this assembly
            'this can be ensured by post build events (when working inside of Visual studio) or dependencies
            'when making deployment projects
            Dim domaininfo As New AppDomainSetup()
            domaininfo.ApplicationBase = IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly.Location)

            ' Create the application domain.
            Dim domain As AppDomain = AppDomain.CreateDomain("AqDn", Nothing, domaininfo)

            'The assemblyresolve event is called when the binder can't find an assembly
            'The only reason this is here is due to a bug in the .NET framework
            'According to the MSDN, adding handlers to this event could result in a security exception
            'if there are insufficient permissions. It seems to work under a restricted account too (guest)
            'so I'm not sure what security restrictions they're talking about.
            'Note that the handler must be added to the CurrentDomain, not the newly created domain
            'If we add it to the newly created domain, it will never be called since the binder
            'is in the CurrentDomain, not the new one.
            AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf CurrentDomain_AssemblyResolve

            'load the wrapper into a new domain
            'The wrapper will take care of loading our main executable into it's own process
            m_TransportBootstrapper = domain.CreateInstanceAndUnwrap("AqDnWrapperBootstrapper", "AqDnWrapperBootstrapper.RemotingBootstrapper")
            m_TransportBootstrapper.run(m_chanwrapper.GetChannelUri, "transport")
        Catch ex As Exception
            Trace.WriteLine(ex)
            Throw New Exception("Error creating new domain at " & IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly.Location) & vbCrLf & ex.ToString)
        End Try

    End Sub
    ''' <summary>
    ''' Event for the AppDomain AssemblyResolve event.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="args"></param>
    ''' <returns></returns>
    ''' <remarks>This event works around a .NET bug where the assembly isn't found when
    ''' casting the remoted obeject to its type. The exception thrown when the bug occurs is "Unable to
    ''' cast transparent proxy to type ‘Namespace.Type’"</remarks>
    Friend Function CurrentDomain_AssemblyResolve(ByVal sender As Object, ByVal args As ResolveEventArgs) As Assembly
        'http://www.west-wind.com/WebLog/posts/601200.aspx

        'try to load it normally first
        Try

            Dim assembly As Assembly = assembly.Load(args.Name)
            If Not assembly Is Nothing Then
                Return assembly
            End If
        Catch ex As Exception
            'ignore load error
        End Try

        ' *** Try to load by filename - split out the filename of the full assembly name
        ' *** and append the base path of the original assembly (ie. look in the same dir)
        ' *** NOTE: this doesn't account for special search paths but then that never
        '         worked before either.
        Dim ass As Assembly = Nothing
        Try
            Dim parts() As String = args.Name.Split(",")
            Dim file As String = IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + parts(0).Trim() + ".dll"
            ass = System.Reflection.Assembly.LoadFrom(file)
        Catch ex As Exception
            Trace.WriteLine("Could not resolve " & args.Name)
            MsgBox("could not resolve " & args.Name & vbCrLf & ex.ToString)
        End Try
        Return ass
    End Function
#End Region

End Class

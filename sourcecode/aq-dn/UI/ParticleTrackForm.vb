Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.GeoAnalyst 'feature to raster
Imports ESRI.ArcGIS.DataSourcesGDB

'UPDATE 21-jul-10
'i've disabled the particle tracking functionality where you specify a layer containing points
'as the particle sources.  This functionality is now in the main form.  The functionality that
'lets you particle track by clicking a point on the map remains.

''' <summary>
''' The particle track tool window.  
''' </summary>
''' <remarks>This is the window that is shown when the Particle Track toolbar button is clicked.</remarks>
Public Class ParticleTrackForm

    ''' <summary>
    ''' the trace listener object.
    ''' </summary>
    ''' <remarks>
    ''' Should be set either in the class constructor or by the instantiator of this class.  This is used
    ''' to be able to properly unload the trace listener when this form closes (instead of waiting for ArcGIS
    ''' to unload the object).  This avoids problems with multiple instances being run, e.g. closing the app
    ''' and running it again.
    ''' </remarks>
    Public myTraceListener As TraceOutput

    ''' <summary>
    ''' A generic delegate function used for form validation
    ''' </summary>
    ''' <remarks>
    ''' When it comes time to validate the form, we can use a loop to loop through all defined
    ''' validators.  Each control will have an associated validation function which is added to this
    ''' list on form load.  The validation function is separate from the validation event handler
    '''  so we can use it from a Control's
    ''' validated event (for the ErrorProvider) or called from a function that validates the whole form.
    ''' </remarks>
    Private Delegate Function validator() As String

    'the reference to the particle tracker
    Private ptrack As ParticleTracker

    ''' <summary>
    ''' Sets the value of the x-coord textbox
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>Called by the onclick event raised the class ParticleTrack</remarks>
    Public Property XCoord() As Double
        Get
            XCoord = txtXCoord.Text
        End Get
        Set(ByVal value As Double)
            txtXCoord.Text = value
        End Set
    End Property
    ''' <summary>
    ''' Sets the value of the y-coord textbox
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>Called by the onclick event raised the class ParticleTrack</remarks>
    Public Property YCoord() As Double
        Get
            YCoord = txtYCoord.Text
        End Get
        Set(ByVal value As Double)
            txtYCoord.Text = value
        End Set
    End Property

    Public ReadOnly Property MessageLog() As Windows.Forms.TextBox
        Get
            Return Me.txtMessageLog
        End Get
    End Property

#Region "UI event handlers"
    Private Sub ParticleTrackForm_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        init()
    End Sub

    Private Sub ParticleTrackForm_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        If Not myTraceListener Is Nothing Then myTraceListener.close()
    End Sub

    Private Sub btnLayerInfoMag_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnLayerInfoMag.LinkClicked
        If Not cmbRasterMag.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbRasterMag.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnLayerPorosityInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnLayerPorosityInfo.LinkClicked
        If Not cmbPorosity.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbPorosity.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub
    Private Sub btnLayerDirInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnLayerDirInfo.LinkClicked
        If Not cmbRasterDir.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbRasterDir.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub
    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        If btnGo.Text = "Go" Then
            If formValidate() Then
                btnGo.Text = "Cancel"
                track()
                btnGo.Text = "Go"
            End If
        Else
            cancelTrack()
            btnGo.Text = "Go"
        End If
    End Sub
    Private Sub radUseManual_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radUseManual.CheckedChanged
        lblDesc.Text = "Click the map to select the starting" & vbCrLf & _
                        "point.  The particle will be tracked starting" & vbCrLf & _
                        "from that point, using the selected magnitude" & vbCrLf & _
                        "and direction layers.  The files will be saved" & vbCrLf & _
                        "with the specified name"
        txtXCoord.Visible = radUseManual.Checked
        txtYCoord.Visible = radUseManual.Checked
        lblX.Visible = radUseManual.Checked
        lblY.Visible = radUseManual.Checked
    End Sub

    Private Sub radUseLayer_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles radUseLayer.CheckedChanged

        cmbPointLayer.Visible = radUseLayer.Checked
        lblDesc.Text = "Select the feature layer to use." & vbCrLf & _
                       "the point features will be used as the" & vbCrLf & _
                       "starting points"
    End Sub
    Private Sub cmbRasterDir_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmbRasterDir.SelectedIndexChanged
        Try
            Dim rp_dir As IRasterProps = CType(CType(CType(cmbRasterDir.SelectedItem, MyLayer2).BaseLayer, RasterLayer).Raster, IRasterProps)
            Dim cellsz As Single = Math.Max(rp_dir.MeanCellSize.X, rp_dir.MeanCellSize.Y) / 2
            txtPTWBRasterCellSz.Text = cellsz
            txtPTStepSize.Text = cellsz
        Catch ex As Exception

        End Try

    End Sub
#End Region

#Region "validators"
    Private validators As New List(Of [Delegate])

    Private Function validate_txtXCoord() As String
        Dim s As Single
        Dim errstr As String = ""
        If radUseManual.Checked Then
            If Not Single.TryParse(txtXCoord.Text, s) Then
                errstr = "The X-Coordinate must be a number"
            End If
        End If
        ErrorProvider1.SetError(txtXCoord, errstr)
        Return errstr
    End Function

    Private Function validate_txtPTWBRasterCellSz() As String
        Dim s As Single
        Dim errstr As String = ""

        If Not Single.TryParse(txtPTWBRasterCellSz.Text, s) Then
            errstr = "The WB Cell size must be a number"
        Else
            If s <= 0 Then
                errstr = "The WB cell size must be greater than zero"
            End If
        End If

        ErrorProvider1.SetError(txtPTWBRasterCellSz, errstr)
        Return errstr
    End Function

    Private Function validate_txtyCoord() As String
        Dim s As Single
        Dim errstr As String = ""
        If radUseManual.Checked Then
            If Not Single.TryParse(txtYCoord.Text, s) Then
                errstr = "the y-coordinate must be a number"
            End If
        End If
        ErrorProvider1.SetError(txtYCoord, errstr)
        Return errstr
    End Function

    Private Function validate_txtName() As String
        txtName.Text = txtName.Text.Trim
        Dim errstr As String = ""
        If txtName.Text = "" Then
            errstr = "Please enter a name"
        ElseIf txtName.Text.Contains(".") Then
            errstr = "Please remove the file extension in the file name."
        ElseIf Utilities.checkExist(IO.Path.Combine(Main.ActiveDocPath, txtName.Text)) Then
            errstr = "'" & IO.Path.Combine(Main.ActiveDocPath, txtName.Text) & "' already exists. Please choose a new name"
        End If
        ErrorProvider1.SetError(txtName, errstr)
        Return errstr
    End Function

    Private Function validate_cmbRasterMag() As String
        Dim errstr As String = ""
        If cmbRasterMag.SelectedItem Is Nothing Then
            errstr = "Please select a raster layer for the magnitude"
        End If
        ErrorProvider1.SetError(cmbRasterMag, errstr)
        Return errstr
    End Function
    Private Function validate_cmbRasterDir() As String
        Dim errstr As String = ""
        If cmbRasterDir.SelectedItem Is Nothing Then
            errstr = "Please select a raster layer for the direction"
        End If
        ErrorProvider1.SetError(cmbRasterDir, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPorosity() As String
        Dim errstr As String = ""
        If cmbPorosity.SelectedItem Is Nothing Then
            errstr = "Please select a raster layer for the porosity"
        End If
        ErrorProvider1.SetError(cmbPorosity, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPointLayer() As String
        Dim errstr As String = ""
        If radUseLayer.Checked Then
            If cmbPointLayer.SelectedItem Is Nothing Then
                errstr = "Please select a feature class layer containing points."
            End If
        End If
        ErrorProvider1.SetError(cmbPointLayer, errstr)
        Return errstr
    End Function
    Private Function validate_cmbWB() As String
        Dim errstr As String = ""

        If cmbWB.SelectedItem Is Nothing Then
            errstr = "Please select a polygon layer containing water bodies."
        End If
        ErrorProvider1.SetError(cmbWB, errstr)
        Return errstr
    End Function
    Private Function validate_txtPTStepSize() As String
        Dim s As Single
        Dim errstr As String = ""

        If Not Single.TryParse(txtPTStepSize.Text, s) Then errstr = "the step size must be a number"

        If errstr = "" Then
            If s <= 0 Then errstr = "the step size must be greater than zero."
        End If
        ErrorProvider1.SetError(txtPTStepSize, errstr)
        Return errstr
    End Function

#End Region

#Region "helpers"
    Private Sub init()
        'populate the dropdowns
        cmbRasterDir.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbRasterMag.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbPointLayer.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, esriGeometryType.esriGeometryPoint)
        cmbPorosity.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbWB.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, esriGeometryType.esriGeometryPolygon)

        'register the form validators
        validators.Add(New validator(AddressOf validate_txtName))
        validators.Add(New validator(AddressOf validate_txtXCoord))
        validators.Add(New validator(AddressOf validate_txtyCoord))
        validators.Add(New validator(AddressOf validate_cmbRasterDir))
        validators.Add(New validator(AddressOf validate_cmbRasterMag))
        validators.Add(New validator(AddressOf validate_cmbPointLayer))
        validators.Add(New validator(AddressOf validate_cmbWB))
        validators.Add(New validator(AddressOf validate_txtPTStepSize))
        validators.Add(New validator(AddressOf validate_txtPTWBRasterCellSz))
        validators.Add(New validator(AddressOf validate_cmbPorosity))

    End Sub


    ''' <summary>
    ''' Returns true if all the form controls validated successfully
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function formValidate() As Boolean
        Dim errorOccurred As Boolean = False
        Dim errstr As String = ""

        'validate the textbox inputs
        For Each v As validator In validators
            errstr = v()
            If errstr <> "" Then
                Trace.WriteLine("ERROR: " & errstr)
                errorOccurred = True
            End If
        Next

        Return Not errorOccurred
    End Function

    ''' <summary>
    ''' Tracks particles.  A single or multiple particles are tracked depending on the form options
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub track()
        ProgressBar1.Show()
        Trace.Indent()
        Try
            Dim waterbodies_layer As FeatureLayer
            Dim rasterDir_layer As RasterLayer
            Dim rasterMag_layer As RasterLayer
            Dim rasterPorosity_layer As RasterLayer
            Dim sources_layer As FeatureLayer
            'Dim rasterDir As String
            'Dim rasterMag As String


            rasterDir_layer = CType(cmbRasterDir.SelectedItem, MyLayer2).BaseLayer
            rasterMag_layer = CType(cmbRasterMag.SelectedItem, MyLayer2).BaseLayer
            waterbodies_layer = CType(cmbWB.SelectedItem, MyLayer2).BaseLayer
            rasterPorosity_layer = CType(cmbPorosity.SelectedItem, MyLayer2).BaseLayer
            If radUseLayer.Checked Then
                sources_layer = CType(cmbPointLayer.SelectedItem, MyLayer2).BaseLayer
            Else
                sources_layer = Nothing
            End If

            'get the resolution. by default, the particle track function uses half of the raster resolution
            'i found that by doing this, sometimes the particle will get stuck even when there is no sink.
            'setting the step size to that of the raster resolution seems to fix it.
            Dim rp_dir As IRasterProps = rasterDir_layer.Raster
            Dim rp_mag As IRasterProps = rasterMag_layer.Raster


            'check spatial references
            'all input data MUST be in the same projection as each other AND as the data frame
            'If one data set has a different projection than the rest, can get the error
            '"Unable to set analysis window" when running the darcy() function.
            'if the data is different than the projection of the data frame, FlowDirection
            'won't work properly, you will get lines across the resulting raster.
            Dim layers As New List(Of ILayer)
            layers.Add(waterbodies_layer)
            layers.Add(rasterDir_layer)
            layers.Add(rasterMag_layer)
            layers.Add(sources_layer)
            layers.Add(rasterPorosity_layer)
            If Not Utilities.checkLayerSpatialReferences(layers, Main.ActiveMap) Then
                Throw New Exception("Input data must have the same spatial references")
            End If

            'convert the waterbodies to raster
            Trace.WriteLine("Converting water bodies to raster")
            Dim pConversionOp As IConversionOp = New RasterConversionOp

            Dim pFC01 As IFeatureClass = waterbodies_layer.FeatureClass



            Dim pRasWSF As IScratchWorkspaceFactory2 = New ScratchWorkspaceFactory
            Dim pRasWS As RasterWorkspace = pRasWSF.CreateNewScratchWorkspace

            Dim pRasout As RasterDataset
            Dim pFDesc As IFeatureClassDescriptor = New FeatureClassDescriptor
            pFDesc.Create(pFC01, Nothing, "FID")
            CType(pConversionOp, IRasterAnalysisEnvironment).SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, Single.Parse(txtPTWBRasterCellSz.Text))
            pRasout = pConversionOp.ToRasterDataset(pFDesc, "IMAGINE Image", pRasWS, "output.img")
            Dim wb_raster As IRaster2 = CType(pRasout, IRasterDataset2).CreateFullRaster            
            Trace.WriteLine("Converting water bodies to raster...done")


            Dim trackfile_path As String

            'if the user wants to manually specify a single starting point, use the first option.
            'else use the second option.
            If radUseManual.Checked Then
                trackfile_path = IO.Path.Combine(Main.ActiveDocPath, txtName.Text)
                Dim p As New ESRI.ArcGIS.Geometry.Point
                p.X = txtXCoord.Text
                p.Y = txtYCoord.Text

                ptrack = New ParticleTracker(CType(CType(cmbRasterMag.SelectedItem.BaseLayer, RasterLayer).Raster, IRaster2), _
                                             CType(CType(cmbRasterDir.SelectedItem.BaseLayer, RasterLayer).Raster, IRaster2), _
                                             wb_raster, _
                                             CType(CType(cmbPorosity.SelectedItem.BaseLayer, RasterLayer).Raster, IRaster2), _
                                             txtName.Text, _
                                             Main.ActiveDocPath, _
                                             Main.ActiveMap.SpatialReference, _
                                             p, _
                                             Single.Parse(txtPTStepSize.Text), _
                                             Decimal.ToInt32(txtPTMaxSteps.Value))
                If ptrack.track Then
                    Utilities.createFeatureLayerFromFeatureClass(ptrack.ParticleTracks)
                Else
                    Throw New Exception("Could not track the poimt")
                End If

            Else
                '********************
                'My particle tracker
                '********************
                ptrack = New ParticleTracker(CType(CType(cmbRasterMag.SelectedItem.BaseLayer, RasterLayer).Raster, IRaster2), _
                                             CType(CType(cmbRasterDir.SelectedItem.BaseLayer, RasterLayer).Raster, IRaster2), _
                                             wb_raster, _
                                             CType(CType(cmbPorosity.SelectedItem.BaseLayer, RasterLayer).Raster, IRaster2), _
                                             txtName.Text, _
                                             Main.ActiveDocPath, _
                                             Main.ActiveMap.SpatialReference, _
                                             sources_layer.FeatureClass, _
                                             Single.Parse(txtPTStepSize.Text), _
                                             Decimal.ToInt32(txtPTMaxSteps.Value))
                If ptrack.track Then
                    Utilities.createFeatureLayerFromFeatureClass(ptrack.ParticleTracks)
                Else
                    Throw New Exception("Could not track the points")
                End If
            End If

        Catch ex As Exception
            Trace.WriteLine(ex.Message)
            MsgBox("An error occured. Check the message log for details", MsgBoxStyle.Critical)
        Finally
            ProgressBar1.Hide()
            Trace.Unindent()
        End Try
    End Sub

    Private Sub cancelTrack()
        ptrack.cancelTrack()
    End Sub

    ''' <summary>
    ''' Tracks a particle from the given point to the nearest sink using the Darcy particle track tool
    ''' </summary>
    ''' <param name="rasterDir_path">The fully qualified path to the input direction raster</param>
    ''' <param name="rasterMag_path">The fully qualified path to the input magnitude raster</param>
    ''' <param name="trackfile_path ">The full path and name of the output tracking file and particle path output layer. </param>
    ''' <param name="step_size">The step size to use in the integration</param>
    ''' <param name="x">The starting x coordinate</param>
    ''' <param name="y">The starting y coordinate</param>
    ''' <remarks>Returns false if there was an error. True otherwise</remarks>
    Private Function tracker(ByVal rasterDir_path As String, ByVal rasterMag_path As String, ByVal trackfile_path As String, ByVal step_size As Double, ByVal x As Double, ByVal y As Double) As Boolean
        Dim track As New ESRI.ArcGIS.SpatialAnalystTools.ParticleTrack()

        track.in_direction_raster = rasterDir_path
        track.in_magnitude_raster = rasterMag_path
        track.out_track_file = trackfile_path & ".txt"
        track.out_track_polyline_features = trackfile_path


        track.source_point = x & " " & y
        track.step_length = Math.Ceiling(step_size)

        Try
            If Not Utilities.RunTool(track, Nothing) Then
                Return False
            End If
        Catch ex As Exception
            Trace.WriteLine(ex.Message)
            Return False
        End Try
        Return True
    End Function
#End Region





End Class
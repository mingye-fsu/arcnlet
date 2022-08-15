Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.DataSourcesRaster

'This class implements the functionality contained in the Particle Tracking tab on the main form
Partial Public Class MainForm

    'the reference to the particle tracker
    Private ptrack As ParticleTracker



#Region "UI Event Handlers"
    Private Sub btnPTMagLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnPTMagLayerInfo.LinkClicked
        If Not cmbPTRasterMag.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbPTRasterMag.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnPTDirLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnPTDirLayerInfo.LinkClicked
        If Not cmbPTRasterDir.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbPTRasterDir.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnPTWaterBodiesLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnPTWaterBodiesLayerInfo.LinkClicked
        If Not cmbPTWaterBodies.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbPTWaterBodies.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnPTSourcesLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnPTSourcesLayerInfo.LinkClicked
        If Not cmbPTPointLayer.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbPTPointLayer.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub


    Private Sub btnPTPorosityLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnPTPorosityLayerInfo.LinkClicked
        If Not cmbPTPorosityRaster.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbPTPorosityRaster.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnPTTrackOutput_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPTTrackOutput.Click
        Dim dlg As New OpenSaveDialog(FilterTypes.Feature, IO.Path.GetFileNameWithoutExtension(txtPTName.Text))
        Dim r As String = dlg.showSave(Me, "shp")
        If r <> "" Then
            txtPTName.Text = r
        End If
    End Sub

    Private Sub cmbPTRasterDir_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmbPTRasterDir.SelectedIndexChanged
        Try
            Dim rp_dir As IRasterProps = CType(CType(CType(cmbPTRasterDir.SelectedItem, MyLayer2).BaseLayer, RasterLayer).Raster, IRasterProps)
            Dim maxcellsize As Single = Math.Max(rp_dir.MeanCellSize.X, rp_dir.MeanCellSize.Y) / 2

            txtPTWBRasterCellSz.Text = maxcellsize
            txtPTStepSize.Text = maxcellsize * 2
        Catch ex As Exception
        End Try
    End Sub

    Private Sub cmbPTRasterMag_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmbPTRasterMag.SelectedIndexChanged
        Try
            Dim rp_dir As IRasterProps = CType(CType(CType(cmbPTRasterMag.SelectedItem, MyLayer2).BaseLayer, RasterLayer).Raster, IRasterProps)
            Dim maxcellsize As Single = Math.Max(rp_dir.MeanCellSize.X, rp_dir.MeanCellSize.Y) / 2

            txtPTWBRasterCellSz.Text = maxcellsize
            txtPTStepSize.Text = maxcellsize * 2
        Catch ex As Exception
        End Try
    End Sub
#End Region

#Region "validators"

    Private PTValidators As New List(Of [Delegate])

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

    Private Function validate_txtPTName() As String
        txtPTName.Text = txtPTName.Text.Trim
        Dim errstr As String = ""
        If txtPTName.Text = "" Then
            errstr = "Please select an output raster for the plumes"
        ElseIf Utilities.checkExist(txtPTName.Text) Then
            errstr = "The output raster '" & txtPTName.Text & "' already exists! Please choose a different name"
        End If
        ErrorProvider1.SetError(txtPTName, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPTRasterMag() As String
        Dim errstr As String = ""
        If cmbPTRasterMag.SelectedItem Is Nothing Then
            errstr = "Please select a raster layer for the magnitude"
        End If
        ErrorProvider1.SetError(cmbPTRasterMag, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPTRasterDir() As String
        Dim errstr As String = ""
        If cmbPTRasterDir.SelectedItem Is Nothing Then
            errstr = "Please select a raster layer for the direction"
        End If
        ErrorProvider1.SetError(cmbPTRasterDir, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPTPorosity() As String
        Dim errstr As String = ""
        If cmbPTPorosityRaster.SelectedItem Is Nothing Then
            errstr = "Please select a raster layer for the porosity"
        End If
        ErrorProvider1.SetError(cmbPTPorosityRaster, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPTPointLayer() As String
        Dim errstr As String = ""

        If cmbPTPointLayer.SelectedItem Is Nothing Then
            errstr = "Please select a feature class layer containing points."
        End If

        ErrorProvider1.SetError(cmbPTPointLayer, errstr)
        Return errstr
    End Function

    Private Function validate_cmbPTWaterBodies() As String
        Dim errstr As String = ""

        If chkPTUseWaterBodies.Checked Then
            If cmbPTWaterBodies.SelectedItem Is Nothing Then
                errstr = "Please select a polygon layer containing water bodies."
            End If
        End If
        ErrorProvider1.SetError(cmbPTWaterBodies, errstr)
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

#Region "Helpers"
    ''' <summary>
    ''' initializes the controls and validators in this tab
    ''' </summary>
    ''' <remarks>
    ''' this function should be called from the form's Load event
    ''' </remarks>
    Private Sub PTinit()
        'populate the dropdowns with the map's layers.
        PTPopulateDropdowns()

        'register the form validators
        PTValidators.Clear()
        PTValidators.Add(New validator(AddressOf validate_txtPTWBRasterCellSz))
        PTValidators.Add(New validator(AddressOf validate_txtPTName))
        PTValidators.Add(New validator(AddressOf validate_cmbPTRasterMag))
        PTValidators.Add(New validator(AddressOf validate_cmbPTRasterDir))
        PTValidators.Add(New validator(AddressOf validate_cmbPTPointLayer))
        PTValidators.Add(New validator(AddressOf validate_cmbPTWaterBodies))
        PTValidators.Add(New validator(AddressOf validate_txtPTStepSize))
        PTValidators.Add(New validator(AddressOf validate_cmbPTPorosity))

    End Sub

    ''' <summary>
    ''' Populates the drop down boxes with the appropriate layers
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub PTPopulateDropdowns()
        cmbPTPointLayer.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, ESRI.ArcGIS.Geometry.esriShapeType.esriShapePoint)
        cmbPTRasterDir.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbPTRasterMag.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbPTWaterBodies.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
        cmbPTPorosityRaster.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
    End Sub

    ''' <summary>
    ''' Cancels the currently running particle tracking operation (if any)
    ''' </summary>
    ''' <remarks></remarks>
    Friend Sub cancelParticleTracking()
        If Not ptrack Is Nothing Then
            ptrack.cancelTrack()
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
    Public Function runPT(ByVal AddOutputToActiveMap As Boolean) As Boolean
        GC.Collect()
        GC.WaitForPendingFinalizers()

        Trace.WriteLine("Particle Tracking: START")

        Dim errOccurred As Boolean = False
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser

        Try
            Dim err As String = ""

            'validate the textbox inputs
            For Each v As validator In PTValidators
                err = v()
                If err <> "" Then
                    Throw New Exception(err)
                End If
            Next

            'if were here, the form validated successfully
            Trace.WriteLine("Form inputs validated")

            'gather the inputs   
            Dim sel_waterbodies As FeatureLayer = Nothing
            Dim sel_velmag As RasterLayer = cmbPTRasterMag.SelectedItem.baselayer
            Dim sel_veldir As RasterLayer = cmbPTRasterDir.SelectedItem.baselayer
            Dim sel_sources As FeatureLayer = cmbPTPointLayer.SelectedItem.baselayer
            Dim sel_cellsz As Single = txtPTWBRasterCellSz.Text
            Dim sel_stepsz As Single = txtPTStepSize.Text
            Dim sel_maxsteps As Integer = Decimal.ToInt32(txtPTMaxSteps.Value)
            Dim sel_shpname As String = IO.Path.GetFileNameWithoutExtension(txtPTName.Text)
            Dim sel_shppath As String = IO.Path.GetDirectoryName(txtPTName.Text)
            Dim sel_porosity As RasterLayer = cmbPTPorosityRaster.SelectedItem.baselayer

            If sel_shppath Is Nothing Then
                sel_shppath = IO.Path.GetPathRoot(txtPTName.Text)
            End If

            Try
                'echo the inputs
                Trace.WriteLine("Magnitude = " & sel_velmag.FilePath)
                Trace.WriteLine("Direction = " & sel_veldir.FilePath)
                Trace.WriteLine("Porosity = " & sel_porosity.FilePath)
                Trace.WriteLine("Sources = " & IO.Path.Combine(CType(sel_sources, IDataset).Workspace.PathName, sel_sources.Name))

                'occurs when the use waterbodies checkbox is unchecked
                'otherwise, will get a null reference exception when we try to access baselayer
                If Not cmbPTWaterBodies.SelectedItem Is Nothing Then
                    sel_waterbodies = cmbPTWaterBodies.SelectedItem.baselayer
                    Trace.WriteLine("Water bodies = " & IO.Path.Combine(CType(sel_waterbodies, IDataset).Workspace.PathName, sel_waterbodies.Name))
                    If sel_waterbodies.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("Water bodies input contains no features")
                End If

                Trace.WriteLine("WB Cell Sz = " & sel_cellsz)
                Trace.WriteLine("Step size = " & sel_stepsz)
                Trace.WriteLine("Max steps = " & sel_maxsteps)
                Trace.WriteLine("Output = " & IO.Path.Combine(sel_shppath, sel_shpname))
            Catch ex As Exception
                Trace.WriteLine(ex.ToString)
            End Try

            If sel_sources.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("Sources input contains no features")

            'check layer spatial references.  Sometimes get unexplainable errors
            'and/or results when the spatial references are different.
            Dim layers As New List(Of ILayer)
            layers.Add(sel_waterbodies)
            layers.Add(sel_veldir)
            layers.Add(sel_velmag)
            layers.Add(sel_sources)
            layers.Add(sel_porosity)
            If Not Utilities.checkLayerSpatialReferences(layers, Main.ActiveMap) Then
                Throw New Exception("Input data must have the same spatial references")
            End If
            Trace.WriteLine("Input spatial referecenes OK")

            'convert the water bodies to raster.
            Dim r_wb As IRaster2 = Nothing
            If chkPTUseWaterBodies.Checked Then
                Trace.WriteLine("Converting '" & sel_waterbodies.FeatureClass.AliasName & "' to raster")
                r_wb = Utilities.FeatureclassToRaster(sel_waterbodies.FeatureClass, sel_cellsz, sel_velmag)

                If mnuOutputIntermediateToolStripMenuItem.Checked Then
                    Utilities.createRasterLayerFromRaster(r_wb)
                End If
            End If

            Trace.WriteLine("Running module ParticleTrack")
            btnAbort.Enabled = True
            Trace.Flush()
            Windows.Forms.Application.DoEvents()

            ptrack = New ParticleTracker(CType(sel_velmag.Raster, IRaster2), _
                                         CType(sel_veldir.Raster, IRaster2), _
                                         CType(r_wb, IRaster2), _
                                         CType(sel_porosity.Raster, IRaster2), _
                                         sel_shpname, _
                                         sel_shppath, _
                                         Main.ActiveMap.SpatialReference, _
                                         sel_sources.FeatureClass, _
                                         sel_stepsz, _
                                         sel_maxsteps)
            Dim result As Boolean = ptrack.track
            Dim resultFC As IFeatureClass = ptrack.ParticleTracks
            ptrack = Nothing 'necessary so that dropdowns refresh when they're supposed to

            If result Then
                If AddOutputToActiveMap Then
                    Utilities.createFeatureLayerFromFeatureClass(resultFC)
                End If
            Else
                Throw New Exception("Could not track the points")
            End If
            btnAbort.Enabled = False
        Catch ex As Exception
            Trace.WriteLine("[Error] Particle Tracking (" & Reflection.MethodInfo.GetCurrentMethod.Name & "): " & ex.Message)
            errOccurred = True
        End Try

            Trace.WriteLine("Particle Tracking: FINISHED")
            If errOccurred Then Return False Else Return True
    End Function


End Class

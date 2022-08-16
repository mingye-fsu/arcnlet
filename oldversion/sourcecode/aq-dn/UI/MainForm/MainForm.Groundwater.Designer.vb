Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.DataSourcesRaster

'this class implements the functionality in the groundwater tab.
'The filename is the way it is so that double cliking it in the solution explorer
'doesn't bring up an empty form while double clicking the orignal form does.
Partial Public Class MainForm

    'the darcy flow module
    Private flow As DarcyFlow

#Region "UI event handlers"
    Private Sub btnGWDEMLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnGWDEMLayerInfo.LinkClicked
        If Not cmbGWDEMLayers.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbGWDEMLayers.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub
    Private Sub btnWTWaterBodiesLayerInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnGWWaterBodiesLayerInfo.LinkClicked
        If Not cmbGWWaterBodiesLayers.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbGWWaterBodiesLayers.SelectedItem.baselayer, FeatureLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub
    Private Sub btnGWDarcyPorosityInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnGWDarcyPorosityInfo.LinkClicked
        If Not cmbGWDarcyPorosity.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbGWDarcyPorosity.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub
    Private Sub btnGWDarcyKInfo_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles btnGWDarcyKInfo.LinkClicked
        If Not cmbGWDarcyK.SelectedItem Is Nothing Then
            Dim f As New PopupInfo(CType(cmbGWDarcyK.SelectedItem.baselayer, RasterLayer), "Layer Info")
            f.Show(Me)
        Else
            MsgBox("Please select a layer", MsgBoxStyle.Exclamation)
        End If
    End Sub

    Private Sub btnGWDarcyOutputRasterMag_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGWDarcyOutputRasterMag.Click
        Dim dlg As New OpenSaveDialog(FilterTypes.Raster, IO.Path.GetFileNameWithoutExtension(txtGWDarcyOutputRasterMag.Text))
        Dim r As String = dlg.showSave(Me, "img")
        If r <> "" Then
            txtGWDarcyOutputRasterMag.Text = r
        End If
    End Sub
    Private Sub btnGWDarcyOutputRasterDir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGWDarcyOutputRasterDir.Click
        Dim dlg As New OpenSaveDialog(FilterTypes.Raster, IO.Path.GetFileNameWithoutExtension(txtGWDarcyOutputRasterDir.Text))
        Dim r As String = dlg.showSave(Me, "img")
        If r <> "" Then
            txtGWDarcyOutputRasterDir.Text = r
        End If
    End Sub
    Private Sub btnGWDarcyOutputRasterHydrGr_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGWDarcyOutputRasterHydrGr.Click
        Dim dlg As New OpenSaveDialog(FilterTypes.Raster, IO.Path.GetFileNameWithoutExtension(txtGWDarcyOutputRasterHydrGr.Text))
        Dim r As String = dlg.showSave(Me, "img")
        If r <> "" Then
            txtGWDarcyOutputRasterHydrGr.Text = r
        End If
    End Sub

    Private Sub chkGWOutputHydrGr_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkGWOutputHydrGr.CheckedChanged
        If chkGWOutputHydrGr.Checked Then
            btnGWDarcyOutputRasterHydrGr.Enabled = True
        Else
            btnGWDarcyOutputRasterHydrGr.Enabled = False
            txtGWDarcyOutputRasterHydrGr.Text = ""
        End If
    End Sub
    Private Sub txtGWDarcyOutputRasterHydrGr_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtGWDarcyOutputRasterHydrGr.Validated
        ErrorProvider1.SetError(sender, validate_txtGWDarcyOutputRasterHydrGr)
    End Sub
    Private Sub txtGWDarcyOutputRasterMag_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtGWDarcyOutputRasterMag.Validated
        ErrorProvider1.SetError(sender, validate_txtGWDarcyOutputRasterMag)
    End Sub

    Private Sub txtGWDarcyOutputRasterDir_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtGWDarcyOutputRasterDir.Validated
        ErrorProvider1.SetError(sender, validate_txtGWDarcyOutputRasterDir)
    End Sub

    Private Sub txtGWDarcyZFactor_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtGWZFactor.Validated
        ErrorProvider1.SetError(sender, validate_txtGWZFactor)
    End Sub

    Private Sub chkWTUseWaterBodies_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkGWUseWaterBodies.CheckedChanged
        Me.cmbGWWaterBodiesLayers.Enabled = Me.chkGWUseWaterBodies.Checked
    End Sub

    ''' Add output smoothed DEM option
    ''' Hongzhuan lei, 04/26/2016.
    Private Sub txtSmthCell_Validated(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtSmthCell.Validated
        ErrorProvider1.SetError(sender, validate_txtSmthCell)
    End Sub

    Private Sub chkGWOutputSmDEM_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkGWOutputSmDEM.CheckedChanged
        If chkGWOutputSmDEM.Checked Then
            btnGWOutputSmDEM.Enabled = True
        Else
            btnGWOutputSmDEM.Enabled = False
            txtGWOutputSmDEM.Text = ""
        End If
    End Sub

    Private Sub btnGWOutputSmDEM_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGWOutputSmDEM.Click
        'Dim dialog As New System.Windows.Forms.FolderBrowserDialog()
        'dialog.RootFolder = Environment.SpecialFolder.Desktop
        'dialog.SelectedPath = "C:\"
        'dialog.Description = "Select Output Smoothed DEM Files Path"
        'If dialog.ShowDialog() = Windows.Forms.DialogResult.OK Then
        '    txtGWOutputSmDEM.Text = dialog.SelectedPath
        'End If
        Dim dlg As New OpenSaveDialog(FilterTypes.Raster, IO.Path.GetFileNameWithoutExtension(txtGWOutputSmDEM.Text))
        Dim r As String = dlg.showSave(Me, "img")
        If r <> "" Then
            txtGWOutputSmDEM.Text = r
        End If
    End Sub

    Private Sub txtGWOutputSmDEM_Validated(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtGWOutputSmDEM.Validated
        ErrorProvider1.SetError(sender, validate_txtGWDarcyOutputSmDEM)
    End Sub

#End Region

#Region "validators"

    Private GWValidators As New List(Of [Delegate])

    Private Function validate_txtGWZFactor() As String
        Dim d As Double
        If Not Double.TryParse(txtGWZFactor.Text, d) Then Return "Z-Factor: you must specify a valid number for the Z-Factor"
        If d <= 0 Then Return "Z-Factor The Z-Factor must be greater than zero."
        Return ""
    End Function

    Private Function validate_txtGWDarcyOutputRasterMag() As String
        If txtGWDarcyOutputRasterMag.Text = "" Then
            Return "Please select an output velocity magnitude raster"
        End If
        If Utilities.checkExist(txtGWDarcyOutputRasterMag.Text) Then
            Return "The output raster '" & txtGWDarcyOutputRasterMag.Text & "' already exists! Please choose a different name"
        End If
        Return ""
    End Function
    Private Function validate_txtGWDarcyOutputRasterDir() As String
        If txtGWDarcyOutputRasterDir.Text = "" Then
            Return "Please select an output velocity direction raster"
        End If
        If Utilities.checkExist(txtGWDarcyOutputRasterDir.Text) Then
            Return "The output raster '" & txtGWDarcyOutputRasterDir.Text & "' already exists! Please choose a different name"
        End If
        If txtGWDarcyOutputRasterDir.Text = txtGWDarcyOutputRasterMag.Text Then
            Return "The output rasters must have a different file name!"
        End If
        Return ""
    End Function

    Private Function validate_txtGWDarcyOutputRasterHydrGr() As String
        If chkGWOutputHydrGr.Checked Then
            If txtGWDarcyOutputRasterHydrGr.Text = "" Then
                Return "Please select an output hydraulic gradient raster"
            End If
            If Utilities.checkExist(txtGWDarcyOutputRasterHydrGr.Text) Then
                Return "The output raster '" & txtGWDarcyOutputRasterHydrGr.Text & "' already exists! Please choose a different name"
            End If
        End If
        Return ""
    End Function
    ' hongzhuan Lei, 04262016
    Private Function validate_txtSmthCell() As String
        Dim iNum As Integer
        If Not Integer.TryParse(txtSmthCell.Text, iNum) Then Return "Smoothing Cell: you must specify a valid number for the Smoothing Cell"
        If iNum <= 0 Then Return "Smoothing Cell: The number must be greater than zero."
        Return ""
    End Function

    Private Function validate_txtGWDarcyOutputSmDEM() As String
        If chkGWOutputSmDEM.Checked Then
            'If txtGWOutputSmDEM.Text = "" Then
            '    Return "Please select an output folder for smoothed DEM"
            'End If
            If txtGWOutputSmDEM.Text = "" Then
                Return "Please select an output smoothed DEM raster"
            End If
            If Utilities.checkExist(txtGWOutputSmDEM.Text) Then
                Return "The output raster '" & txtGWOutputSmDEM.Text & "' already exists! Please choose a different name"
            End If
        End If
        Return ""
    End Function
#End Region

#Region "Helpers"
    ''' <summary>
    ''' initializes the controls and validators in this tab
    ''' </summary>
    ''' <remarks>
    ''' this function should be called from the form''s Load event
    ''' </remarks>
    Private Sub GWinit()
        'populate the dropdowns with the map's layers.
        GWPopulateDropdowns()

        'register the form validators
        GWValidators.Clear()
        GWValidators.Add(New validator(AddressOf validate_txtGWDarcyOutputRasterMag))
        GWValidators.Add(New validator(AddressOf validate_txtGWDarcyOutputRasterDir))
        GWValidators.Add(New validator(AddressOf validate_txtGWDarcyOutputRasterHydrGr))
        GWValidators.Add(New validator(AddressOf validate_txtGWDarcyOutputSmDEM))
        GWValidators.Add(New validator(AddressOf validate_txtGWZFactor))

    End Sub

    ''' <summary>
    ''' Populates the drop down boxes with the appropriate layers
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub GWPopulateDropdowns()
        cmbGWDEMLayers.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbGWWaterBodiesLayers.Populate(Main.ActiveMap, LayerTypes.LayerType.FeatureLayer, ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon)
        cmbGWDarcyK.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
        cmbGWDarcyPorosity.Populate(Main.ActiveMap, LayerTypes.LayerType.RasterLayer)
    End Sub

    ''' <summary>
    '''  Cancels the currently running darcy flow operation (if any)
    ''' </summary>
    ''' <remarks></remarks>
    Friend Sub cancelDarcyFlow()
        If Not flow Is Nothing Then
            'TODO
        End If
    End Sub
#End Region

    ''' <summary>
    ''' used to start this module's calculations. 
    ''' </summary>
    ''' <param name="addtolayers">
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
    Public Function runGW(Optional ByVal addtolayers As Boolean = True) As Boolean
        GC.Collect()
        GC.WaitForPendingFinalizers()

        Trace.WriteLine("Compute Darcy Flow: START")

        Dim errOccurred As Boolean = False
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser

        Try
            Dim err As String = ""

            'validate the textbox inputs
            For Each v As validator In GWValidators
                err = v()
                If err <> "" Then
                    Throw New Exception(err)
                End If
            Next

            'if were here, the form validated successfully
            Trace.WriteLine("Compute Darcy Flow: Form inputs validated")

            'gather the inputs
            Dim smoothing As Integer = Me.txtGWSmoothingAmt.Value               'the number of smoothing iterations
            Dim zFactor As Single = txtGWZFactor.Text                           'the Z Factor            
            Dim outputRasterMagPath As String = txtGWDarcyOutputRasterMag.Text   'the path where the magnitude raster will be saved
            Dim outputRasterDirPath As String = txtGWDarcyOutputRasterDir.Text   'the path where the direction raster will be saved
            Dim outputRasterHydrGrPath As String = txtGWDarcyOutputRasterHydrGr.Text   'the path where the hydraulic gradient raster will be saved
            Dim outputSmDEMPath As String = txtGWOutputSmDEM.Text   'the path where the smoothed DEM will be saved
            Dim smthCellNum As Integer = txtSmthCell.Text               'the number of smoothing cell, default 7x7.
            '***********************************************************************************************************
            'do some type checking of the selected layers, since the combo boxes can contain objects of different types
            'dont do this with validators since the combo boxes can contain objects of different types which may
            'or may not be convertible to a layer (i.e. it might be possible to have a path to a file which can 
            'then be converted to a valid raster later)
            '
            'UPDATE 21-jul10
            'Originally I had planned to allow the input of file paths in the combo boxes in addition to 
            'layers from the active map.  I decided against this for simplicity.  Since the combo boxes already
            'filter out the proper layer types.  Most (not all) of the checks below are redundant. I'm
            'leaving them in though since they're already there and it doesn't hurt to have them.
            '***********************************************************************************************************
            Dim dem As IRaster2 = Nothing       'the DEM elevation raster
            Dim wb As IFeatureClass = Nothing   'the water bodies feature class
            Dim k As IRaster2 = Nothing         'hydraulic conductivity
            Dim porosity As IRaster2 = Nothing  'porosity           

            'check the raster elevation layer
            Dim sel_raster_dem As RasterLayer = Nothing
            If TypeOf cmbGWDEMLayers.SelectedItem Is MyLayer2 Then
                If TypeOf CType(cmbGWDEMLayers.SelectedItem, MyLayer2).BaseLayer Is RasterLayer Then
                    sel_raster_dem = CType(cmbGWDEMLayers.SelectedItem.BaseLayer, RasterLayer)

                    'get the raster object of the layer. 
                    dem = sel_raster_dem.Raster

                    If dem.RasterDataset.Format <> "GRID" Then
                        'disable this. didn't come across this problem again
                        'If MsgBox("Input DEM should be GRID format to avoid problems with sink fill. Continue anyways?", MsgBoxStyle.YesNo Or MsgBoxStyle.Exclamation) = MsgBoxResult.No Then
                        ' Throw New Exception("Input DEM should be grid format")
                        'End If
                    End If
                Else
                    Throw New Exception("Selected raster is not a RasterLayer type")
                End If
            Else
                'this case is an error for now
                Throw New Exception("Selected raster is not a layer")
            End If

            'check the water bodies layer
            Dim sel_wb As FeatureLayer = Nothing
            If chkGWUseWaterBodies.Checked Then
                If TypeOf cmbGWWaterBodiesLayers.SelectedItem Is MyLayer2 Then
                    If TypeOf CType(cmbGWWaterBodiesLayers.SelectedItem, MyLayer2).BaseLayer Is FeatureLayer Then
                        sel_wb = CType(cmbGWWaterBodiesLayers.SelectedItem.BaseLayer, FeatureLayer)

                        'get the featureclass object of the layer. 
                        wb = sel_wb.FeatureClass
                    Else
                        Throw New Exception("Selected water bodies layer is not a FeatureLayer type")
                    End If
                Else
                    'this case is an error for now
                    Throw New Exception("Selected water bodies layer is not a layer")
                End If
                If sel_wb.FeatureClass.FeatureCount(Nothing) <= 0 Then Throw New Exception("Water bodies input contains no features")
            End If

            'check hydraulic conductivity
            Dim sel_raster_k As RasterLayer = Nothing
            If TypeOf cmbGWDarcyK.SelectedItem Is MyLayer2 Then
                If TypeOf CType(cmbGWDarcyK.SelectedItem, MyLayer2).BaseLayer Is RasterLayer Then
                    sel_raster_k = CType(cmbGWDarcyK.SelectedItem.BaseLayer, RasterLayer)

                    'get the raster object of the layer. 
                    k = sel_raster_k.Raster
                Else
                    Throw New Exception("Selected raster is not a RasterLayer type")
                End If
            Else
                'this case is an error for now
                Throw New Exception("Selected raster is not a layer")
            End If

            'check porosity
            Dim sel_raster_n As RasterLayer = Nothing
            If TypeOf cmbGWDarcyPorosity.SelectedItem Is MyLayer2 Then
                If TypeOf CType(cmbGWDarcyPorosity.SelectedItem, MyLayer2).BaseLayer Is RasterLayer Then
                    sel_raster_n = CType(cmbGWDarcyPorosity.SelectedItem.BaseLayer, RasterLayer)

                    'get the raster object of the layer. 
                    porosity = sel_raster_n.Raster
                Else
                    Throw New Exception("Selected raster is not a RasterLayer type")
                End If
            Else
                'this case is an error for now
                Throw New Exception("Selected raster is not a layer")
            End If

            'if we're here, type checking was successful
            Trace.WriteLine("Compute Darcy Flow: Selected layers are of the correct type")

            'echo inputs
            Try
                Trace.WriteLine("DEM = " & sel_raster_dem.FilePath)
                Trace.WriteLine("Hydr. Cond. = " & sel_raster_k.FilePath)
                Trace.WriteLine("Porosity = " & sel_raster_n.FilePath)
                If Not sel_wb Is Nothing Then
                    Trace.WriteLine("Water bodies = " & IO.Path.Combine(CType(sel_wb, IDataset).Workspace.PathName, sel_wb.Name))
                End If
                Trace.WriteLine("Smth. Fac. = " & smoothing)
                Trace.WriteLine("Smth. Cell = " & smthCellNum)
                Trace.WriteLine("Fill Sinks = " & chkGWFillSinks.Checked)
                Trace.WriteLine("Z-Factor = " & zFactor)
                Trace.WriteLine("Out Mag. = " & outputRasterMagPath)
                Trace.WriteLine("Out Dir. = " & outputRasterDirPath)
                If outputRasterHydrGrPath <> "" Then
                    Trace.WriteLine("Out Grad. = " & outputRasterHydrGrPath)
                End If
                If outputSmDEMPath <> "" Then
                    Trace.WriteLine("Out Smth. DEM = " & outputSmDEMPath)
                End If

            Catch ex As Exception
                Trace.WriteLine(ex.ToString)
            End Try

            'check spatial references
            'all input data MUST be in the same projection as each other AND as the data frame
            'If one data set has a different projection than the rest, can get the error
            '"Unable to set analysis window" when running the darcy() function.
            'if the data is different than the projection of the data frame, FlowDirection
            'won't work properly, you will get lines across the resulting raster.
            Dim layers As New List(Of ILayer)
            layers.Add(sel_raster_dem)
            layers.Add(sel_raster_k)
            layers.Add(sel_raster_n)
            layers.Add(sel_wb)
            If Not Utilities.checkLayerSpatialReferences(layers, Main.ActiveMap) Then
                Throw New Exception("Input data must have the same spatial references")
            End If
            Trace.WriteLine("Compute Darcy Flow: Input spatial referecenes OK")
            '*********************************************************************************************

            'now, run the computation
            Trace.WriteLine("Running module DarcyFlow")
            Trace.Flush()
            ' Hongzhuan Lei, add output smth dem and set smth cell num. 04/26/2016.
            flow = New DarcyFlow(dem:=dem, wb:=wb, k:=k, porosity:=porosity, slope_zfactor:=zFactor, smoothing:=smoothing, _
                                 p_mag:=outputRasterMagPath, p_dir:=outputRasterDirPath, _
                                 p_hydrgr:=outputRasterHydrGrPath, _
                                 fillsinks:=chkGWFillSinks.Checked, _
                                 p_smthDEM:=outputSmDEMPath, _
                                 outputIntermediateRasters:=mnuOutputIntermediateToolStripMenuItem.Checked, _
                                 smthCellNum:=smthCellNum)
            Dim output() As RasterDataset = flow.calculateDarcyFlow()
            If output Is Nothing Then
                Throw New Exception("There was an error running the water table routine. Check the log for errors")
            End If
            flow = Nothing

            'create a new layer and add it to the active map
            If addtolayers Then
                ' Hongzhuan Lei, add smoothed DEM as layer, 04/27/2016.
                'Utilities.createRasterLayerFromDataset(output(0))
                'Utilities.createRasterLayerFromDataset(output(1))
                For ix As Integer = 0 To output.Length - 1
                    If Not output(ix) Is Nothing Then
                        Utilities.createRasterLayerFromDataset(output(ix))
                    End If
                Next
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] Compute Darcy Flow (" & Reflection.MethodInfo.GetCurrentMethod.Name & "): " & ex.Message)
            errOccurred = True
        End Try

        Trace.WriteLine("Compute Darcy Flow: FINISHED")
        If errOccurred Then Return False Else Return True
    End Function


End Class

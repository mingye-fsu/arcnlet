Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.DataSourcesGDB
Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.Geoprocessor
Imports ESRI.ArcGIS.esriSystem              'itrackcancel
Imports System.Runtime.InteropServices
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.GeoAnalyst              'feature to raster
Imports ESRI.ArcGIS.SpatialAnalyst

''' <summary>
''' Misc. utility functions
''' </summary>
''' <remarks></remarks>
Public Class Utilities

    ''' <summary>
    ''' Calculates the remainder of two doubles a/b and returns the quotient in an output parameter.    
    ''' </summary>
    ''' <param name="a">String representation of the numerator</param>
    ''' <param name="b">String representation of the denominator</param>
    ''' <param name="quotient">Ouput quotient</param>
    ''' <returns>Remainder of a/b. NaN on error</returns>
    ''' <remarks>Similar to the Math.DivRem function (with parameter and ouptut reversed)
    ''' except it supports floating point numbers as well.  The input parameters are strings instead
    ''' of doubles because if the inputs are given as Single, there will be an implicit conversion to a double, the number may change a small amount
    ''' because of the conversion and as a result, the wrong answer will be returned.  By enconding the numbers as strings,
    ''' the actual values are preserved. Note: an implicit conversion from number to string for the input parameters is OK (i.e.
    ''' passing numbers instead of strings for a and b)</remarks>
    Public Shared Function DivRem(ByVal a As String, ByVal b As String, Optional ByRef quotient As Double = Double.NaN) As Double
        Dim r As Double = Double.NaN
        Dim q As Double = Double.NaN
        Try
            Dim ad As Double = Double.Parse(a)
            Dim bd As Double = Double.Parse(b)

            'using the built in mod function doesn't seem to return the right result sometimes
            'e.g. 4.5 mod 0.3 should return 0 but it doesn't. Therefore the need for this workaround

            q = ad / bd
            r = ad - bd * Math.Truncate(q)

        Catch ex As Exception
            Trace.WriteLine("Couldn't calc. quotient: " & ex.Message)
            q = Double.NaN
            r = Double.NaN
        End Try
        If Not Double.IsNaN(quotient) Then quotient = q
        Return r
    End Function


    ''' <summary>
    ''' Checks to see whether the given output file exists
    ''' </summary>
    ''' <param name="path">The name of the file to check</param>
    ''' <returns>
    ''' True if it exists. False otherwise
    ''' </returns>
    ''' <remarks>
    ''' This function should be run before any processing is done, so that the user can correct
    ''' any errors before the run begins.  For example if running a calculation as part
    ''' of a batch, the batch function should call this method and see if any outputs exist.
    ''' This is necessary because many ArcGIS functions give an error if the output exists.
    ''' </remarks>
    Public Shared Function checkExist(ByVal path As String) As Boolean
        'was using the geoprocessor's exists function but it was too slow
        'use the built in exists function instead.
        Return System.IO.File.Exists(path.Replace("""", ""))
    End Function

    ''' <summary>
    ''' Runs a tool from the ArcGIS toolbox
    ''' </summary>
    ''' <param name="process">The tool object</param>
    ''' <param name="TC">Process cancellation. can be Nothing</param>
    ''' <remarks>
    ''' Example: Run the fill tool from spatial analyst
    ''' <code>
    ''' Imports ESRI.ArcGIS.SpatialAnalystTools
    ''' 
    ''' 'set up the tool parameters
    ''' Dim filler As New Fill(m_dem.RasterDataset, "C:/GIS_tests/aq-dn-test/t")
    ''' Utilities.RunTool(filler, Nothing)
    ''' </code>
    ''' </remarks>
    Public Shared Function RunTool(ByVal process As IGPProcess, ByVal TC As ITrackCancel) As Boolean
        Dim geoprocessor As New Geoprocessor
        ' Set the overwrite output option to true
        geoprocessor.OverwriteOutput = True
        Dim msg As String = ""
        Try
            geoprocessor.Execute(process, Nothing)
            msg = ReturnMessages(geoprocessor)
            If msg.Contains("ERROR") Then
                Throw New Exception(msg)
            Else
                Trace.WriteLine(msg)
            End If
        Catch ex As Exception
            Trace.WriteLine(msg)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    '''  Function for returning the tool messages.
    ''' </summary>
    ''' <param name="gp"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function ReturnMessages(ByVal gp As Geoprocessor) As String
        ' Print out the messages from tool executions
        Dim ret As String = ""
        Dim Count As Integer
        If gp.MessageCount > 0 Then
            For Count = 0 To gp.MessageCount - 1
                ret = ret & vbTab & gp.GetMessage(Count) & vbCrLf
            Next
        End If
        Return ret
    End Function

    ''' <summary>
    ''' creates a feature layer from a given shapefile
    ''' </summary>
    ''' <param name="fullpath">The full path to the shapefile</param>
    ''' <remarks></remarks>
    Friend Shared Sub createFeatureLayerFromShapeFile(ByVal fullpath As String)
        Try
            Dim fc As IFeatureClass = createFeatureClassFromShapeFile(fullpath)

            createFeatureLayerFromFeatureClass(fc)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & "couldn't load " & fullpath & ": " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Create a featureclass from a shapefile. Returns Nothing on error
    ''' </summary>
    ''' <param name="fullpath">path and file name with extension</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function createFeatureClassFromShapeFile(ByVal fullpath As String) As IFeatureClass
        Dim COM As New ComReleaser
        Dim ret As IFeatureClass = Nothing
        Dim fname As String = IO.Path.GetFileNameWithoutExtension(fullpath.Replace("""", ""))
        Dim path As String = IO.Path.GetDirectoryName(fullpath.Replace("""", ""))
        If fullpath = "" Then
            path = IO.Path.GetPathRoot(fullpath)
        End If
        Try
            'create the workspace which we will use to open the file
            Dim shpWPF As IWorkspaceFactory2 = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory"))
            Dim featWS As IFeatureWorkspace
            If Not Main.App Is Nothing Then
                featWS = shpWPF.OpenFromFile(path, Main.App.hWnd)
            Else
                featWS = shpWPF.OpenFromFile(path, Nothing)
            End If
            COM.ManageLifetime(featWS)

            'open the featureclass
            ret = featWS.OpenFeatureClass(fname)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & "couldn't load " & fullpath & ": " & ex.Message)
            ret = Nothing
        End Try
        Return ret
    End Function

    ''' <summary>
    ''' Creates a raster layer from a file
    ''' </summary>
    ''' <param name="fullpath">The full path (incl. file name and extension) of the raster to load</param>
    ''' <param name="layername">The name of the layer to add the raster into</param>
    ''' <remarks></remarks>
    Friend Shared Sub createRasterLayerFromFile(ByVal fullpath As String, ByVal layername As String)
        Dim newlayer As RasterLayer = Nothing
        Dim COM As New ComReleaser
        Dim fname As String = IO.Path.GetFileName(fullpath)         'get filename with extension
        Dim path As String = IO.Path.GetDirectoryName(fullpath)     'get path (with root)
        If fullpath = "" Then
            path = IO.Path.GetPathRoot(fullpath)
        End If

        Try
            'open the workspace
            'see buildnotes.txt
            Dim wf As IWorkspaceFactory = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory"))
            Dim ws As IRasterWorkspace = wf.OpenFromFile(path, Nothing)
            COM.ManageLifetime(wf)
            COM.ManageLifetime(ws)

            'load up the raster
            Dim rds As IRasterDataset = ws.OpenRasterDataset(fname)
#If CONFIG = "Arc10" Or CONFIG = "mydebug-Arc10" Or CONFIG = "Release" Or CONFIG = "Arc10.2" Then
            rds.PrecalculateStats(0)
#End If
            'note by Yan: update to ArcGIS10.1
#If CONFIG = "Arc10.1" Then
            rds.PrecalculateStats(0)
#End If
            createRasterLayerFromDataset(rds)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & "couldn't load " & fullpath & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Loads a raster from the given file name (full path and name with extension).
    ''' Returns Nothing on error
    ''' </summary>
    ''' <param name="fullpath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function createRasterFromFile(ByVal fullpath As String) As IRaster2
        Dim rds As IRasterDataset2
        Dim r As IRaster2
        rds = createRasterDatasetFromFile(fullpath)
        If rds Is Nothing Then
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & "couldn't create raster from " & fullpath)
            r = Nothing
        Else
            r = rds.CreateFullRaster
        End If
        ComReleaser.ReleaseCOMObject(rds)
        Return r
    End Function

    Public Shared Function createRasterDatasetFromFile(ByVal fullpath As String) As IRasterDataset2
        Dim r As IRasterDataset2 = Nothing
        Try
            Dim fname As String = IO.Path.GetFileName(fullpath.Replace("""", ""))         'get filename with extension, strip quotes
            Dim path As String = IO.Path.GetDirectoryName(fullpath.Replace("""", ""))     'get path (with root)
            If fullpath = "" Then
                path = IO.Path.GetPathRoot(fullpath)
            End If

            'open the workspace
            'see BuildNotes.txt
            Dim wf As IWorkspaceFactory = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory"))
            Dim ws As IRasterWorkspace
            If Not Main.App Is Nothing Then
                ws = wf.OpenFromFile(path, Main.App.hWnd)
            Else
                ws = wf.OpenFromFile(path, Nothing)
            End If

            'load up the rasterdataset
            r = ws.OpenRasterDataset(fname)

            ComReleaser.ReleaseCOMObject(ws)
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & "couldn't load " & fullpath & ": " & ex.Message)
            r = Nothing
        End Try
        Return r
    End Function


    ''' <summary>
    ''' Creates a layer from an a feature class reference
    ''' </summary>
    ''' <param name="f">The FeatureClass object implementing IFeatureClass</param>
    ''' <remarks></remarks>
    Friend Shared Sub createFeatureLayerFromFeatureClass(ByVal f As IFeatureClass)
        Dim newlayer As IFeatureLayer = New FeatureLayer()
        newlayer.FeatureClass = f
        newlayer.Name = IO.Path.GetFileNameWithoutExtension(f.AliasName)
        Main.ActiveMap.AddLayer(newlayer)

        Trace.WriteLine("Added feature '" & newlayer.Name & "' as new layer in " & Main.ActiveMap.Name)
    End Sub

    ''' <summary>
    ''' Adds the specified RasterDataset as a new layer into the active map
    ''' </summary>
    ''' <param name="r">The raster dataset to add</param>
    ''' <remarks>
    ''' If there is an error, an exception will be thrown.  If the datum and coordinate system
    ''' of the raster don't match the destination, show a warning (to do)
    ''' </remarks>
    Friend Shared Sub createRasterLayerFromDataset(ByVal r As RasterDataset)
        Dim newlayer As RasterLayer = New RasterLayer()
        If Not Main.App Is Nothing Then
            Try
                newlayer.CreateFromDataset(r)
                newlayer.Name = IO.Path.GetFileNameWithoutExtension(r.CompleteName)
                Main.ActiveMap.AddLayer(newlayer)
            Catch ex As Exception
                Trace.WriteLine("couldn't add layer to active map")
                Return
            End Try
            Trace.WriteLine("Added raster '" & newlayer.Name & "' as new layer in " & Main.ActiveMap.Name)
        End If
    End Sub

    ''' <summary>
    ''' Creates a new raster data set and adds it to the active map
    ''' </summary>
    ''' <param name="r">The raster</param>
    ''' <param name="path">The path (including the file name) where the dataset will be saved
    ''' If not specified, the file will be named with a numerical file name appended to the dataset name.
    ''' and will be saved in the same folder as the active document.</param>
    ''' <remarks></remarks>
    Friend Shared Sub createRasterLayerFromRaster(ByVal r As IRaster2, Optional ByVal path As String = "")

        Try
            If path = "" Then
                path = IO.Path.Combine(Main.ActiveDocPath, IO.Path.GetFileNameWithoutExtension(r.RasterDataset.CompleteName) & Now.Ticks & ".img")
            End If
        Catch ex As Exception
        End Try

        Trace.WriteLine("saving raster " & path)
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Dim rd As RasterDataset = CType(CType(r, ISaveAs2).SaveAs(path, Nothing, "IMAGINE Image"), IRasterDataset2)
        COM.ManageLifetime(rd)
        If Not Main.App Is Nothing Then
            createRasterLayerFromDataset(rd)
        End If
    End Sub

    ''' <summary>
    ''' Checks the list of given layers (feature or raster) to see if they have the same
    ''' spatial reference compared to each other and compared to the data frame
    ''' </summary>
    ''' <param name="layers">a List of ILayer. The layers to compare</param>
    ''' <param name="map">If supplied, represents the map that the layers to be checked belong to.
    ''' All spatial references will also be checked with the spatial referene of the active data frame</param>
    ''' <returns>True if the layers have the same spatial reference compared to each other and 
    ''' to the data frame. false otherwise</returns>
    ''' <remarks>This is necessary because ArcGIS has some bugs when running commands when
    ''' the spatial references are different.  For example, when calculating the groundwater flow
    ''' if the porosity raster has a different projection than all the other datasets, there
    ''' will be an error when you try to run map calculator in the darcy() function "Unable to set
    ''' Analysis window".  Another example is with some projections (e.g. FDEP Albers) if the data
    ''' is different than the coordinate system of the data frame, you will get strange lines across
    ''' the FlowDirection raster.</remarks>
    Public Shared Function checkLayerSpatialReferences(ByVal layers As List(Of ILayer), _
                                                       ByVal map As IMap) As Boolean
        Try
            'get rid of layers that are nothing.
            Dim layersnonull As New List(Of ILayer)
            For Each layer As ILayer In layers
                If Not layer Is Nothing Then
                    layersnonull.Add(layer)
                End If
            Next

            'get the first layer
            Dim firstlayer As ILayer = Nothing
            Dim item1 As IClone = Nothing
            If layersnonull.Count > 0 Then
                firstlayer = layersnonull.Item(0)
                item1 = CType(firstlayer, IGeoDataset).SpatialReference
            Else
                Return True
            End If

            'compare the first layer with all the rest. if at least one is different 
            'then return false
            If layersnonull.Count > 1 Then
                Dim item2 As IClone
                For Each layer As ILayer In layersnonull
                    item2 = CType(layer, IGeoDataset).SpatialReference
                    If Not item1.IsEqual(item2) Then
                        Throw New Exception("Different spatial references detected - Layer '" & firstlayer.Name & "' and Layer '" & layer.Name & "'")
                    End If
                Next
            End If

            'if we're here, all of the layers have the same spatial reference and 
            'its equal to the one from the first layer.

            'check to see if its the same as the data frame
            Dim item3 As IClone = map.SpatialReference
            If layersnonull.Count > 0 Then
                If Not item1.IsEqual(item3) Then
                    Throw New Exception("The spatial reference of the input data is different than the data frame (" & Main.ActiveMap.SpatialReference.Name & ")")
                End If
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' returns the NoData value of the given raster
    ''' </summary>
    ''' <param name="band">The band in the raster to get the value from</param>
    ''' <returns>The NoData Value</returns>
    ''' <remarks></remarks>
    Public Shared Function getRasterNoDataValue(ByVal r As IRaster2, Optional ByVal band As Integer = 0) As Object
        Dim rProps As IRasterProps = CType(r, IRasterProps)
        Return rProps.NoDataValue(band)
    End Function

    ''' <summary>
    ''' Resets the NoData value of the m_dem raster to the given value
    ''' </summary>
    ''' <param name="newNoData">the new nodata value. should be a large negative number</param>
    ''' <remarks></remarks>
    Public Shared Sub fix_nodata(ByVal newNoData As Single, ByVal r As IRaster2)
        'replace the old NoData values with the new NoData values
        Dim pxCursor As IRasterCursor = r.CreateCursorEx(Nothing)   'for scanning the raster
        Dim pxEdit As IRasterEdit = CType(r, IRasterEdit)           'for editing pixel blocks
        Dim rProps As IRasterProps = CType(r, IRasterProps)         '
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        Dim nodata As Single = getRasterNoDataValue(r)
        pxCursor.Reset()
        Do
            pxBlock = pxCursor.PixelBlock

            'get the pixel array of the first raster band
            pixels = CType(pxBlock.PixelData(0), System.Array)
            For i As Integer = 0 To pxBlock.Width - 1
                For j As Integer = 0 To pxBlock.Height - 1
                    If pixels.GetValue(i, j) = nodata Then
                        pixels.SetValue(newNoData, i, j)
                    End If
                Next
            Next
            pxBlock.PixelData(0) = pixels
            pxEdit.Write(pxCursor.TopLeft, pxBlock)
        Loop While pxCursor.Next
        rProps.NoDataValue = New Single() {newNoData}

        Marshal.FinalReleaseComObject(pxCursor)
    End Sub

    ''' <summary>
    ''' Replaces values less than -1000 with the raster's NoData value
    ''' </summary>
    ''' <param name="r">The input Raster</param>
    ''' <remarks>
    ''' This is necessary because some ArcGIS functions (e.g. convolution) don't ignore
    ''' NoData values when they should, and end up including them in the calculation.
    ''' -1000 was chosen because we are assuming that since all these calculations deal with elevations
    ''' ,which should never be large negative numbers or slopes (which should be positive, in the downslope
    ''' direction), or physical properties like porosity and hydraulic conductivity which can never be negative,
    ''' this assumption is safe.
    ''' </remarks>
    Public Shared Sub replace_DEM_nodata(ByVal r As IRaster2)
        Dim pxCursor As IRasterCursor = r.CreateCursorEx(Nothing)   'for scanning the raster
        Dim pxEdit As IRasterEdit = CType(r, IRasterEdit)           'for editing pixel blocks
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        Dim nodata As Single = getRasterNoDataValue(r)
        pxCursor.Reset()
        Do
            pxBlock = pxCursor.PixelBlock

            'get the pixel array of the first raster band
            pixels = CType(pxBlock.PixelData(0), System.Array)
            For i As Integer = 0 To pxBlock.Width - 1
                For j As Integer = 0 To pxBlock.Height - 1
                    If pixels.GetValue(i, j) < -1000 Then
                        pixels.SetValue(nodata, i, j)
                    End If
                Next
            Next
            pxBlock.PixelData(0) = pixels
            pxEdit.Write(pxCursor.TopLeft, pxBlock)
        Loop While pxCursor.Next
    End Sub


    ''' <summary>
    ''' Replaces all values contained in <paramref name="valsToReplace "/> with the value of <paramref name="replacewith"/>
    ''' </summary>
    ''' <param name="r"></param>
    ''' <param name="valsToReplace"></param>
    ''' <param name="replacewith"></param>
    ''' <remarks></remarks>
    Public Shared Sub replaceRasterVals(ByVal r As IRaster2, ByVal valsToReplace() As Object, ByVal replacewith As Object)
        Dim pxCursor As IRasterCursor = r.CreateCursorEx(New Pnt With {.X = 512, .Y = 512})   'for scanning the raster        
        Dim pxEdit As IRasterEdit = CType(r, IRasterEdit)           'for editing pixel blocks
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        Dim pixval As Object
        Dim replace As Boolean
        Dim n_cells As ULong = CType(r, IRasterProps).Width * CType(r, IRasterProps).Height
        Dim n_blockcells As ULong = 0
        Dim str As String
        Dim nodata As Object = CType(r, IRasterProps).NoDataValue(0)

        pxCursor.Reset()
        Do
            pxBlock = pxCursor.PixelBlock

            str = "Processing pixels " & n_blockcells & " - "
            n_blockcells = n_blockcells + pxBlock.Width * pxBlock.Height
            str = str & n_blockcells & " of  " & n_cells
            Trace.WriteLine(str)

            'get the pixel array of the first raster band
            pixels = CType(pxBlock.PixelData(0), System.Array)
            For i As Integer = 0 To pxBlock.Width - 1
                For j As Integer = 0 To pxBlock.Height - 1
                    pixval = pixels.GetValue(i, j)
                    If pixval <> nodata Then
                        replace = False
                        For k As Integer = 0 To valsToReplace.Count - 1
                            If pixval = valsToReplace(k) Then
                                replace = True
                                Exit For
                            Else
                                replace = False
                            End If
                        Next
                        If replace Then
                            pixels.SetValue(replacewith, i, j)
                        End If
                    End If
                Next
            Next
            pxBlock.PixelData(0) = pixels
            pxEdit.Write(pxCursor.TopLeft, pxBlock)
        Loop While pxCursor.Next

    End Sub

    ''' <summary>
    ''' Opens a temporary workspace
    ''' </summary>
    ''' <returns>A temporary workspace, usually in the user's %temp% folder</returns>
    ''' <remarks>The first time this function is called, a new workspace is created.
    ''' subsequent calls return the same workspace.</remarks>
    Public Shared Function OpenScratchWorkspace() As IWorkspace
        Dim wf As IWorkspaceFactory = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory"))
        Dim ws As IRasterWorkspace

        Dim newname As String = "ArcNLET" & Now.Ticks

        Return wf.OpenFromFile(IO.Path.GetTempPath, Nothing)
        'a gdb workspace seems to work better than an access workspace since the workspace
        'is properly deleted at the end (mostly)
        'Dim scratchWorkspaceFactory As IScratchWorkspaceFactory = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBScratchWorkspaceFactory"))

        'create a new scratch workspace. for some reason, using the default workspace
        'caused errors in ArcNLET_UA in the DarcyFlow module when trying to run SaveAs on a 
        'raster using scratchWorkspaceFactory.DefaultScratchWorkspace
        'Return scratchWorkspaceFactory.CreateNewScratchWorkspace
    End Function

    ''' <summary>
    ''' Converts a feature class to a raster
    ''' </summary>
    ''' <param name="shp">The feature class to convert</param>
    ''' <param name="cellsz">The output cell size</param>
    ''' <param name="extent">An object supporting the IEnvelope interface (e.g. a Raster object).  
    ''' If speficied, the output will be clipped to this extent.  This increases performance</param>
    ''' <param name="spatialRefernce">The spatial reference that the output will use.  If not speficied
    ''' the spatial refernce of the input is used</param>
    ''' <param name="valueField">The name of the field to use for conversion. Default is the FID field</param>
    ''' <param name="fmt">The format. Default is IMAGINE Image. Refer to the ArcObjects documentation for 
    ''' additional formats. Note format MEM is not supported</param>
    ''' <param name="outputToFile">If true, the raster will be output to file with the same file name as     
    ''' the input feature class with the suffix _r.  The output folder will be the folder of the active
    ''' ArcMap document.
    ''' </param>
    ''' <returns>The converted raster file.  Nothing on error</returns>
    ''' <remarks>The values of the raster cells will correspond to the FID (OID) field of the input</remarks>
    Public Shared Function FeatureclassToRaster(ByVal shp As IFeatureClass, ByVal cellsz As Single, _
                                                Optional ByVal extent As IGeoDataset = Nothing, _
                                                Optional ByVal spatialRefernce As ISpatialReference = Nothing, _
                                                Optional ByVal valueField As String = Nothing, _
                                                Optional ByVal fmt As String = "IMAGINE Image", _
                                                Optional ByVal outputToFile As Boolean = False) As IRaster2
        Dim ret As IRaster2 = Nothing
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser

        Trace.Indent()

        Try

            Dim pRasout As RasterDataset
            Dim pConversionOp As IConversionOp
            Dim pFDesc As IFeatureClassDescriptor
            Dim pRasWS As IWorkspace

            pConversionOp = New RasterConversionOp
            COM.ManageLifetime(pConversionOp)

            ' Create RasterDescriptor
            pFDesc = New FeatureClassDescriptor
            If valueField Is Nothing Then
                pFDesc.Create(shp, Nothing, shp.OIDFieldName)
            Else
                pFDesc.Create(shp, Nothing, valueField)
            End If
            COM.ManageLifetime(pFDesc)

            pRasWS = OpenScratchWorkspace()
            If pRasWS Is Nothing Then Throw New Exception("Couldn't create workspace")
            COM.ManageLifetime(pRasWS)

            'set analysis props
            CType(pConversionOp, IRasterAnalysisEnvironment).SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, cellsz)
            CType(pConversionOp, IRasterAnalysisEnvironment).OutWorkspace = pRasWS
            If Not extent Is Nothing Then
                CType(pConversionOp, IRasterAnalysisEnvironment).SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, extent)
            End If
            If Not spatialRefernce Is Nothing Then
                CType(pConversionOp, IRasterAnalysisEnvironment).OutSpatialReference = spatialRefernce
            End If

            pRasout = pConversionOp.ToRasterDataset(pFDesc, fmt, pRasWS, "r" & Now.Ticks & ".img")
            ret = CType(pRasout, IRasterDataset2).CreateFullRaster

            If outputToFile Then
                Utilities.createRasterLayerFromRaster(ret, IO.Path.Combine(Main.ActiveDocPath, shp.AliasName & "_r.img"))
            End If

        Catch ex As Exception
            ret = Nothing
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": could not convert '" & shp.AliasName & "' to raster: " & ex.Message)
        End Try

        Trace.Unindent()

        Return ret
    End Function

    ''' <summary>
    ''' Win32 API call SetWindowLong
    ''' </summary>
    ''' <param name="hWnd"></param>
    ''' <param name="nIndex"></param>
    ''' <param name="dwNewLong"></param>
    ''' <returns></returns>
    ''' <remarks>Used to set the owner of this program's main form to the ArcGIS window</remarks>
    <DllImport("user32.dll")> _
  Public Shared Function SetWindowLong( _
       ByVal hWnd As IntPtr, _
       ByVal nIndex As Integer, _
       ByVal dwNewLong As IntPtr) As Integer
    End Function

    ''' <summary>
    ''' Attempts to delete an IMG raster and associated dataset from disk and memory.
    ''' </summary>
    ''' <param name="raster"></param>
    ''' <remarks>Only IMAGINE Image rasters are supported at this time.<para></para><para>
    ''' The input raster or any of its sub-objects must not have any references other
    ''' than the ones pointed to by the <paramref name="raster"/> input variable.  If there are any
    ''' other references, deletion of the dataset from disk will fail because a file handle will still
    ''' be open.  If an error occurs, a message is printed to Trace.
    ''' </para><para>This function should
    ''' be called as few times as possible due to the garbage collection step</para>
    ''' </remarks>
    Public Shared Sub DeleteRaster(ByVal raster As IRaster2)
        Dim ds As IRasterDataset
        Dim r_name As String = ""
        Dim r_nameNoExt As String = ""
        Try
            If Not raster Is Nothing Then
                GC.Collect()
                GC.WaitForPendingFinalizers()

                ds = raster.RasterDataset               'get the raster dataset
                If ds.Format = "MEM" Then
                    ComReleaser.ReleaseCOMObject(ds)
                    ComReleaser.ReleaseCOMObject(raster)
                    ds = Nothing
                    raster = Nothing
                    GC.Collect()
                    GC.WaitForPendingFinalizers()
                Else
                    r_name = String.Copy(ds.CompleteName)   'get the filename. use string copy so the reference to the ds object is deleted

                    r_nameNoExt = IO.Path.Combine(IO.Path.GetDirectoryName(r_name), IO.Path.GetFileNameWithoutExtension(r_name))

                    ComReleaser.ReleaseCOMObject(ds)         'release the com resources of the dataset. necessary so the file handle gets closed later
                    ComReleaser.ReleaseCOMObject(raster)
                    ds = Nothing                             'let the garbage collector delete references to this object that we touched.
                    raster = Nothing

                    'garbage collect to release references.  This may slow down the code if it is called repeatedly
                    GC.Collect()
                    GC.WaitForPendingFinalizers()

                    If Not r_name Is Nothing Then
                        IO.File.Delete(r_name)
                        If IO.File.Exists(r_nameNoExt & ".rrd") Then IO.File.Delete(r_nameNoExt & ".rrd")
                        If IO.File.Exists(r_nameNoExt & ".img.vat.dbf") Then IO.File.Delete(r_nameNoExt & ".img.vat.dbf")
                        If IO.File.Exists(r_nameNoExt & ".img.aux.xml") Then IO.File.Delete(r_nameNoExt & ".img.aux.xml")
                    End If
                End If

            End If
        Catch ex As Exception
            Trace.WriteLine("Couldn't delete raster " & r_name & ": " & ex.ToString)
        End Try
    End Sub

    Public Shared Sub DeleteRasterByName(ByVal fullpath As String)
        Try
            GC.Collect()
            GC.WaitForPendingFinalizers()
            If IO.File.Exists(fullpath) Or IO.Directory.Exists(fullpath) Then
                If IO.Path.HasExtension(fullpath) Then
                    If IO.Path.GetExtension(fullpath) = ".img" Then
                        Dim r_nameNoExt As String = IO.Path.Combine(IO.Path.GetDirectoryName(fullpath), IO.Path.GetFileNameWithoutExtension(fullpath))
                        IO.File.Delete(fullpath)
                        If IO.File.Exists(r_nameNoExt & ".rrd") Then IO.File.Delete(r_nameNoExt & ".rrd")
                        If IO.File.Exists(r_nameNoExt & ".img.vat.dbf") Then IO.File.Delete(r_nameNoExt & ".img.vat.dbf")
                        If IO.File.Exists(r_nameNoExt & ".img.aux.xml") Then IO.File.Delete(r_nameNoExt & ".img.aux.xml")
                    Else
                        Trace.WriteLine("unsuported raster")
                    End If
                Else
                    Utilities.DeleteFilesAndFoldersQuick(fullpath)
                    If IO.File.Exists(fullpath & ".aux") Then IO.File.Delete(fullpath & ".aux")
                End If
            End If
        Catch ex As Exception
            Trace.WriteLine("Couldn't delete raster " & fullpath & ": " & ex.ToString)
        End Try
    End Sub

    ''' <summary>
    ''' Deletes the specified shapefile
    ''' </summary>
    ''' <param name="fullpath">Full path to the shapefile (with extension)</param>
    ''' <remarks></remarks>
    Public Shared Sub DeleteShapefileByName(ByVal fullpath As String)
        GC.Collect()
        GC.WaitForPendingFinalizers()
        Try
            Dim nameNoext As String = IO.Path.GetFileNameWithoutExtension(fullpath)
            Dim path As String = IO.Path.GetDirectoryName(fullpath)

            For Each myFile As String In IO.Directory.GetFiles(path, nameNoext & "*")
                IO.File.Delete(myFile)
            Next
        Catch ex As Exception
            Trace.WriteLine("Couldn't delete " & fullpath & ": " & ex.ToString)
        End Try
    End Sub

    ''' <summary>
    ''' Recursively deletes files and folders in the given folder.
    ''' </summary>
    ''' <param name="sPath">The folder in which all files will be deleted. This folder will not be deleted.
    ''' only files and folders contained within</param>
    ''' <param name="dp">Do not use.</param>
    ''' <param name="fd">Do not use.</param>
    ''' <param name="fnd">Do not use.</param>
    ''' <param name="sz">Do not use.</param>
    ''' <param name="sznd">Do not use.</param>
    ''' <remarks></remarks>
    Public Shared Sub DeleteFilesAndFolders(ByVal sPath As String, Optional ByVal dp As Integer = 0, Optional ByRef fd As Integer = 0, Optional ByRef fnd As Integer = 0, Optional ByRef sz As Single = 0, Optional ByRef sznd As Single = 0)
        Dim f() As String
        Dim d() As String
        Dim fi As IO.FileInfo
        Dim s As Integer
        Try
            f = IO.Directory.GetFiles(sPath)
        Catch ex As Exception
            Exit Sub
        End Try

        If f.GetLength(0) > 0 Then
            For i As Integer = 0 To f.GetUpperBound(0)
                fi = New IO.FileInfo(f(i))
                Try
                    System.IO.File.SetAttributes(f(i), IO.FileAttributes.Normal)
                    s = fi.Length
                    IO.File.Delete(f(i))
                    sz = sz + s
                    fd = fd + 1
                Catch ex As Exception
                    fnd = fnd + 1
                    sznd = sznd + s
                End Try
            Next
        End If

        d = IO.Directory.GetDirectories(sPath)
        For i As Integer = 0 To d.GetUpperBound(0)
            DeleteFilesAndFolders(d(i), dp + 1, fd, fnd, sz, sznd)
        Next

        If dp <> 0 Then
            Try
                IO.Directory.Delete(sPath)
            Catch ex As Exception
            End Try
        Else
            MsgBox(fd & " files deleted successfully (" & (sz / 1024 / 1024).ToString("0.###") & " MB)" & vbCrLf & _
                   fnd & " Files not deleted since access was denied or they were in use (" & (sznd / 1024 / 1024).ToString("0.###") & " MB)", _
                   MsgBoxStyle.Information, "Report")
        End If

    End Sub


    ''' <summary>
    ''' Delete files and folders recursively. Don't read file size to increase speed.
    ''' If a file or folder can't be deleted, it is ignored.
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub DeleteFilesAndFoldersQuick(ByVal sPath As String, Optional ByVal dp As Integer = 0, Optional ByRef fd As Integer = 0, Optional ByRef fnd As Integer = 0)
        Dim f() As String
        Dim d() As String
        Try
            f = IO.Directory.GetFiles(sPath)
        Catch ex As Exception
            Exit Sub
        End Try

        If f.GetLength(0) > 0 Then
            For i As Integer = 0 To f.GetUpperBound(0)
                Try
                    IO.File.Delete(f(i))
                    fd = fd + 1
                Catch ex As Exception
                    fnd = fnd + 1
                End Try
            Next
        End If

        d = IO.Directory.GetDirectories(sPath)
        For i As Integer = 0 To d.GetUpperBound(0)
            DeleteFilesAndFolders(d(i), dp + 1, fd, fnd)
        Next

        If dp <> 0 Then
            Try
                IO.Directory.Delete(sPath)
            Catch ex As Exception
            End Try
        End If

    End Sub

    Private Shared pr As IRasterGeometryProc = New RasterGeometryProc
    ''' <summary>
    ''' Saves the provided raster object to a file
    ''' </summary>
    ''' <param name="r">The raster to save</param>
    ''' <param name="fullpath">The full path (including name and extesion) to save the raster to</param>
    ''' <param name="l">An optional hashtable to which the fullpath will be added if the operation succeeds</param>
    ''' <param name="fmt">The format. Default is IMAGINE Image. Refer to the ArcObjects documentation for 
    ''' additional formats</param>
    ''' <remarks>If saving fails, a message will be output to Trace and there will be no exceptions thrown</remarks>    
    Public Shared Function saveRasterToFile(ByVal r As IRaster2, ByVal fullpath As String, Optional ByVal l As Hashtable = Nothing, Optional ByVal fmt As String = "IMAGINE Image", Optional ByVal outputTrace As Boolean = True, Optional ByVal rectify As Boolean = False) As IRaster2
        Trace.Indent()
        If outputTrace Then Trace.WriteLine("Creating '" & fullpath & "'...")
        Dim ret As IRaster2
        Dim r_save As IRaster2
        Try
            If IO.File.Exists(fullpath) Then
                Throw New Exception("'" & fullpath & "' exists. please select a different name")
            End If

            If rectify Then
                pr.Rectify(fullpath, fmt, r)
                ret = createRasterFromFile(fullpath)
            Else
                r_save = CType(r, IClone).Clone
                ret = CType(CType(r_save, ISaveAs2).SaveAs(fullpath, Nothing, fmt), IRasterDataset2).CreateFullRaster
                ComReleaser.ReleaseCOMObject(r_save)
            End If

#If CONFIG = "Arc10" Or CONFIG = "mydebug-Arc10" Or CONFIG = "Release" Or CONFIG = "Arc10.2" Then
            'arc10 doesn't calulate stats. without stats, loading a raster gives a gray raster until you change
            'the stretch type. This doesn't work when full post processing is selected for some reason.
            ret.RasterDataset.PrecalculateStats(0)
#End If
            'note by Yan: update to ArcGIS 10.1
#If CONFIG = "Arc10.1" Then
            ret.RasterDataset.PrecalculateStats(0)
#End If
            If Not l Is Nothing Then
                l.Add("RasterDataset" & Now.Ticks, fullpath)
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": Couldn't save " & fullpath & ": " & ex.Message)
            ret = r
        End Try
        Trace.Unindent()
        Return ret
    End Function

    ''' <summary>
    ''' Creates a new shapefile
    ''' </summary>
    ''' <param name="name">Name of the file (no extension)</param>
    ''' <param name="path">Path to save it to</param>
    ''' <param name="fields">An array of fields</param>
    ''' <param name="l">An optional hashtable where the path to the newly created file will be added to</param>
    ''' <returns>The shapefile. Nothing on error</returns>
    ''' <remarks></remarks>
    Public Shared Function createShapefile(ByVal name As String, ByVal path As String, ByVal fields As IFields2, Optional ByVal l As Hashtable = Nothing) As IFeatureClass
        Dim ret As IFeatureClass = Nothing
        Dim COM As New ComReleaser
        Dim fname As String = IO.Path.Combine(path, name & ".shp")

        Trace.Indent()
        Trace.WriteLine("Creating '" & fname & "'...")
        Try
            If IO.File.Exists(fname) Then
                Throw New Exception("file exists. please select a different name")
            End If

            ' Create the shapefile
            ''see BuildNotes.txt
            ' (some parameters apply to geodatabase options and can be defaulted as Nothing)
            Dim wf As IWorkspaceFactory2 = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory"))
            Dim fw As IFeatureWorkspace = wf.OpenFromFile(path, Nothing)
            COM.ManageLifetime(fw)

            ret = fw.CreateFeatureClass(name, fields, Nothing, Nothing, esriFeatureType.esriFTSimple, "Shape", "")

            If Not l Is Nothing Then
                l.Add("FeatureDataset" & Now.Ticks, fname)
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": Couldn't save " & fname & ": " & ex.Message)
            ret = Nothing
        End Try

        Trace.Unindent()
        Return ret
    End Function

    ''' <summary>
    ''' Adds any raster datasets or feature datasets contained in the table to the active map
    ''' </summary>
    ''' <param name="params">The hashtable containing paramters names and values.</param>
    ''' <remarks>Only parameters with key=RasterDataset or FeatureDataset will be added</remarks>
    Public Shared Sub AddOutParamDatasetsToActiveMap(ByVal params As Hashtable)
        Try
            Dim ext As String
            For Each param As DictionaryEntry In params
                If param.Key.ToString.Contains("RasterDataset") Or param.Key.ToString.Contains("FeatureDataset") Then
                    ext = IO.Path.GetExtension(param.Value)
                    If ext = ".img" Then
                        Utilities.createRasterLayerFromFile(param.Value, IO.Path.GetFileNameWithoutExtension(param.Value))
                    ElseIf ext = ".shp" Then
                        'parse out the file name and path
                        Utilities.createFeatureLayerFromShapeFile(param.Value)
                    Else
                        Trace.WriteLine("unknown file, skipping: " & param.Value)
                    End If
                End If
            Next
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
        End Try
    End Sub

    ''' <summary>
    ''' Gets a FeatureCursor given a feature class and a query
    ''' </summary>
    ''' <param name="table">The feature class to get the cursor to</param>
    ''' <param name="query">The where clause</param>
    ''' <returns>A feature cursor. Nothing or an exception on error</returns>
    ''' <remarks>If there is an error reading the file, or the database cursor is locked
    ''' an exception is thrown.  For any other errors, Nothing is returned</remarks>
    Public Shared Function getCursor(ByVal table As IFeatureClass, ByVal query As String, Optional ByVal recycling As Boolean = True) As IFeatureCursor
        Dim COM As New ComReleaser

        Dim fcur As IFeatureCursor = Nothing             'cursor for reading the segments
        Dim q As IQueryFilter2 = New QueryFilter        'query for selecting the segments

        Try
            'don't manage fcur since thats what we'll be returning
            COM.ManageLifetime(q)

            q.WhereClause = query
            fcur = table.Search(q, recycling)
            If fcur Is Nothing Then Throw New Exception("Feature cursor is nothing")
        Catch ex As Exception
            If ex.Message.Contains("0x80041054") Then
                'FDO_E_CURSOR_LOCKED 	-2147217324 	The cursor cannot aquire a lock against the data.
                'only solution seems to close and re-open arggis
                Throw New Exception("Could not read the file '" & table.AliasName & "'.  The database cursor is locked.")
            Else
                Throw New Exception("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": Couldn't read the file '" & table.AliasName & "': " & ex.Message)
            End If
        End Try

        Return fcur
    End Function

    ''' <summary>
    ''' Calculates the total value of the sum of all the non-zero and non-NoData
    ''' raster cells
    ''' </summary>
    ''' <param name="r">The raster where each cell's value will be summed to the total</param>
    ''' <returns>-1 on error</returns>
    ''' <remarks>Assumes all cell values are positive</remarks>
    Public Shared Function getRasterSum(ByVal r As IRaster2) As Double
        Dim ret As Double = -1

        Try

            Dim props As IRasterProps                                   'get the properties of the input plumes raster

            '**********************************************************************

            'get the properties of the input plumes raster
            props = CType(r, IRasterProps)

            '*******************************************
            'sum up all the cells
            '*******************************************

            Trace.WriteLine("Summing raster values")
            Dim pxCursor As IRasterCursor = r.CreateCursorEx(New Pnt With {.X = props.Width, .Y = 128})   'for scanning the raster          
            Dim pxBlock As IPixelBlock3
            Dim pixels As System.Array
            Dim nodata As Object = CType(r, IRasterProps).NoDataValue(0)
            Dim val As Object

            ret = 0
            pxCursor.Reset()
            Do
                pxBlock = pxCursor.PixelBlock

                'get the pixel array of the first raster band
                pixels = CType(pxBlock.PixelData(0), System.Array)
                For i As Integer = 0 To pxBlock.Height - 1
                    For j As Integer = 0 To pxBlock.Width - 1
                        val = pixels.GetValue(j, i)

                        'check for nothing as well since sometimes it returns nothing when there is no data
                        If Not val Is Nothing AndAlso val <> nodata Then
                            ret = ret + val
                        End If
                    Next
                Next
            Loop While pxCursor.Next
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = -1
        End Try

        Return ret
    End Function

    ''' <summary>
    ''' gets a list of the unique values in the raster (excluding nodata) and returns
    ''' the results as a hashtable
    ''' </summary>
    ''' <param name="r"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function getRasterUniqueVals(ByVal r As IRaster2) As Hashtable
        Trace.Indent()

        Dim uniqueVals As New Hashtable
        Dim pxCursor As IRasterCursor
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        Dim nodata As Object
        Dim val As Object

        Try

            Trace.WriteLine("Getting raster values")
            pxCursor = r.CreateCursorEx(New Pnt With {.X = 512, .Y = 512})
            nodata = CType(r, IRasterProps).NoDataValue(0)


            pxCursor.Reset()
            Do
                pxBlock = pxCursor.PixelBlock

                'get the pixel array of the first raster band
                pixels = CType(pxBlock.PixelData(0), System.Array)
                For i As Integer = 0 To pxBlock.Height - 1
                    For j As Integer = 0 To pxBlock.Width - 1
                        val = pixels.GetValue(j, i)

                        'check for nothing as well since sometimes it returns nothing when there is no data
                        If Not val Is Nothing AndAlso val <> nodata Then
                            If Not uniqueVals.ContainsKey(val) Then
                                uniqueVals.Add(val, Nothing)
                            End If
                        End If
                    Next
                Next
            Loop While pxCursor.Next

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            uniqueVals = Nothing
        End Try

        Trace.Unindent()

        Return uniqueVals
    End Function

    ''' <summary>
    ''' Checks if a raster consists of NoData. Should not be used on large rasters
    ''' </summary>
    ''' <param name="r"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function isRasterEmpty(ByVal r As IRaster2) As Boolean

        Dim pxCursor As IRasterCursor
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        Dim rprops As IRasterProps
        Dim nodata As Object
        Dim val As Object

        Dim ret As Boolean = True

        Try
            rprops = r
            pxCursor = r.CreateCursorEx(New Pnt With {.X = rprops.Width, .Y = rprops.Height})
            nodata = rprops.NoDataValue(0)

            pxCursor.Reset()
            pxBlock = pxCursor.PixelBlock

            Do
                pxBlock = pxCursor.PixelBlock

                'get the pixel array of the first raster band
                pixels = CType(pxBlock.PixelData(0), System.Array)
                For i As Integer = 0 To pxBlock.Height - 1
                    For j As Integer = 0 To pxBlock.Width - 1
                        val = pixels.GetValue(j, i)

                        'check for nothing as well since sometimes it returns nothing when there is no data
                        If Not val Is Nothing AndAlso val <> nodata Then
                            ret = False
                            Exit For
                        End If

                    Next
                Next
            Loop While pxCursor.Next

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = True
        End Try

        Return ret
    End Function


    Public Shared Sub outputSystemInfo()
        Try
            Trace.WriteLine("Free space on " & IO.Path.GetPathRoot(IO.Path.GetTempPath) & " = " & My.Computer.FileSystem.GetDriveInfo(IO.Path.GetPathRoot(IO.Path.GetTempPath)).AvailableFreeSpace / 1048576L & " MB")
            Trace.WriteLine("Total mem    p = " & My.Computer.Info.TotalPhysicalMemory / 1048576L & " MB  v = " & My.Computer.Info.TotalVirtualMemory / 1048576L & " MB")
            Trace.WriteLine("Avail mem    p = " & My.Computer.Info.AvailablePhysicalMemory / 1048576L & " MB  v = " & My.Computer.Info.AvailableVirtualMemory / 1048576L & " MB")
            Trace.WriteLine("System    OS = " & My.Computer.Info.OSFullName & "  v = " & My.Computer.Info.OSVersion & "  platform = " & My.Computer.Info.OSPlatform)
        Catch ex As Exception
            Trace.WriteLine("couldn't get system info")
        End Try
    End Sub

    Public Shared Function getAqDnVersion() As String
        'http://www.codeproject.com/KB/vb/aboutbox.aspx
        Dim assemblyVersion As Version = System.Reflection.Assembly.GetExecutingAssembly.GetName.Version
        Dim dt As DateTime = CType("01/01/2000", DateTime). _
                            AddDays(assemblyVersion.Build). _
                            AddSeconds(assemblyVersion.Revision * 2)
        If TimeZone.IsDaylightSavingTime(dt, TimeZone.CurrentTimeZone.GetDaylightChanges(dt.Year)) Then
            dt = dt.AddHours(1)
        End If

        Dim v As String = "v" & assemblyVersion.Major & "." & assemblyVersion.Minor & "." & assemblyVersion.Build & "." & assemblyVersion.Revision
        If assemblyVersion.Build < 730 Or assemblyVersion.Revision = 0 Then
            'do nothing
        Else
            v = v & " (" & dt.ToString("yyyy-MM-dd HH:mm:ss") & ")"
        End If

        Return v
    End Function
    Public Shared Function converttoNO3(ByVal plumes_r As IRaster2, ByVal plumes_r_NH4 As IRaster2, ByVal ConverFactortoNO3 As Single) As IRaster2
        Dim COM As New ComReleaser
        Trace.WriteLine("Start converting to NO3...")
        Dim op As IMapAlgebraOp = New RasterMapAlgebraOp
        Dim r_out As IRaster2

        'Dim lOp As ILogicalOp = New RasterMathOps



        Dim rAEnv As IRasterAnalysisEnvironment
        Dim wsf_out As IWorkspaceFactory2 = New RasterWorkspaceFactory
        Dim ws_out_namePath As String = IO.Path.GetTempPath
        Dim ws_out_nameName As String = Now.Ticks
        Dim ws_out_nameStr As String = IO.Path.Combine(ws_out_namePath, ws_out_nameName)
        Dim ws_out_name As ESRI.ArcGIS.esriSystem.IName = wsf_out.Create(ws_out_namePath, ws_out_nameName, Nothing, 0)
        Dim ws_out As IRasterWorkspace = ws_out_name.Open
        COM.ManageLifetime(wsf_out)
        COM.ManageLifetime(ws_out_name)
        COM.ManageLifetime(ws_out)


        Dim mOp As IRasterMakerOp = New RasterMakerOp

        rAEnv = mOp
        Dim r_plumes As IRasterProps
        r_plumes = CType(plumes_r, IRasterProps)
        Dim r_plumes_NH4_r As IRasterProps
        Dim r_out_r As IRasterProps
        'r_out_r = CType(r_out, IRasterProps)
        r_plumes_NH4_r = CType(plumes_r_NH4, IRasterProps)
        Dim r_plumes_NH4 As IRaster2 = plumes_r_NH4


        rAEnv.OutSpatialReference = r_plumes.SpatialReference
        rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, r_plumes)
        rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, r_plumes)
        rAEnv.OutWorkspace = ws_out

        If r_plumes_NH4_r.NoDataValue(0) < -30000000000.0 Then
            Utilities.fix_nodata(0.0, r_plumes_NH4)
        End If

        Dim consttoNO3 As IRaster2
        consttoNO3 = mOp.MakeConstant(ConverFactortoNO3, False)
        COM.ManageLifetime(mOp)
        op.BindRaster(plumes_r, "a1")
        op.BindRaster(r_plumes_NH4, "c1")
        op.BindRaster(consttoNO3, "b")

        r_out = op.Execute("( [a1] - [b] * [c1] ) ")
        'Trace.WriteLine("End transforming.")
        'clear out garbage data
        Utilities.replace_negetivedata1(0.000001, r_out)

        'r_out_r = CType(r_out, IRasterProps)
        'Dim rasterProps As IRasterProps = CType(r_out, IRasterProps)
        'If rasterProps.NoDataValue(0) > -3.0E+38 Then
        'Utilities.fix_nodata(-3.40282347E+38, r_out)
        'End If
        'If r_out_r.NoDataValue(0) < -30000000000.0 Then
        'Utilities.fix_nodata(0.000001, r_out)
        'End If

        ComReleaser.ReleaseCOMObject(op)
        Trace.WriteLine("Converting to NO3 finished")
        Return r_out
    End Function


    Public Shared Sub replace_negetivedata1(ByVal newData As Single, ByVal r As IRaster2)
        Dim pxCursor As IRasterCursor = r.CreateCursorEx(Nothing)   'for scanning the raster
        Dim pxEdit As IRasterEdit = CType(r, IRasterEdit)           'for editing pixel blocks
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        'Dim nodata As Single = getRasterNoDataValue(r)
        'Dim nodata As Single = 0
        Dim rProps As IRasterProps = CType(r, IRasterProps)
        pxCursor.Reset()
        Do
            pxBlock = pxCursor.PixelBlock
            'get the pixel array of the first raster band
            pixels = CType(pxBlock.PixelData(0), System.Array)
            For i As Integer = 0 To pxBlock.Width - 1
                For j As Integer = 0 To pxBlock.Height - 1
                    If pixels.GetValue(i, j) < 0.0 And pixels.GetValue(i, j) > -30000 Then
                        'Trace.WriteLine("start processing nodata")
                        pixels.SetValue(newData, i, j)
                    End If
                Next
            Next
            pxBlock.PixelData(0) = pixels
            pxEdit.Write(pxCursor.TopLeft, pxBlock)
        Loop While pxCursor.Next
        Marshal.FinalReleaseComObject(pxCursor)
    End Sub
End Class

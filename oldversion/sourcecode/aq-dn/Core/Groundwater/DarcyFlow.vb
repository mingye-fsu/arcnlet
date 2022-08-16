Imports ESRI.ArcGIS.Carto               'IRasterLayer
Imports ESRI.ArcGIS.Geodatabase         'IRaster
Imports ESRI.ArcGIS.DataSourcesRaster   'Raster
Imports ESRI.ArcGIS.GeoAnalyst          'slope
Imports ESRI.ArcGIS.SpatialAnalyst      'hydrology ops
Imports ESRI.ArcGIS.esriSystem          'iclobne
Imports ESRI.ArcGIS.ADF                 'comreleaser
Imports ESRI.ArcGIS.ADF.Connection.Local


''' <summary>
''' Calculates the seepage velocity
''' </summary>
''' <remarks>
''' This class takes as input 3 rasters: the water table elevation raster, and two rasters
''' representing the hydraulic conductivity K and the Porosity n.  The output is two rasters
''' representing the magnitude and direction of flow for each input cell.  The direction is 
''' in degrees with 0 degrees corresponding to North.
''' </remarks>
Public Class DarcyFlow



    Private m_Raster_in_dem As IRaster2 = Nothing       'the input DEM raster
    Private m_in_wb As IFeatureClass = Nothing             'the input waterbodies layer. nothing=don't use waterbodies
    Private m_Raster_in_k As IRaster2 = Nothing         'the hydraulic conductivity raster
    Private m_Raster_in_porosity As IRaster2 = Nothing  'the porosity

    Private m_Raster_tmp As IRaster2 = Nothing      'a working raster. this will be used by all the functions

    Private m_savepath_mag As String = Nothing      'the output raster path for the seepage vel magnitude
    Private m_savepath_dir As String = Nothing      'the output raster path for the seepage vel direction
    Private m_savepath As String = Nothing            'the path for saving intermediate output rasters
    Private m_savepath_hydrgr As String = Nothing      'the output raster path for the hydraulic gradient raster
    Private m_savepath_smthDEM As String = Nothing
    Private m_slope_zfactor As Single               'the z factor for the slope function
    Private m_smthCellNum As Single                 'the number of smoothing cell
    Private m_smoothing As Single                   'the number of smoothing iterations    
    Private m_slope_horizontalDemRes As Single      'horizontal dem resolution. can be obtained automatically
    Private m_fillsinks As Boolean                  'enables or disables sink filling

    'our output rasters
    Private m_Raster_out_dir As IRaster2 = Nothing
    Private m_Raster_out_mag As IRaster2 = Nothing

    Dim m_outputIntermediateRasters As Boolean

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="dem">The raster representing the terrain elevation</param>
    ''' <param name="wb">The polygon layer representing the locations of water bodies. If this is Nothing, then the water bodies processing will be disabled.</param>
    ''' <param name="k">The hydraulic conductivity raster</param>
    ''' <param name="porosity">The porosity raster</param>
    ''' <param name="slope_zfactor">
    ''' The Z factor for the slope calculation. If the horizontal units are the same as the vertical
    ''' units of measurement, the Z factor should be 1.  If they are different, they should be changed
    ''' to the appropritate value.  For example, if the horizontal units are meters and the vertical units
    ''' are feet, the Z factor would be 0.3048 (1 foot = 0.3048 meters)
    ''' </param>
    ''' <param name="smoothing">The amount of smoothing to apply to the DEM</param>
    ''' <param name="p_mag ">The path where the magnitude raster will be saved. Includes path, filename and extension (.img)</param>
    ''' <param name="p_dir">The path where the direction raster will be saved.  Includes path, filename and extension (.img)</param>
    ''' <param name="p_hydrgr">The path where the hydraulic gradient raster will be saved.  Includes path, filename and extension (.img)
    ''' Pass in an empty string to disable output of the hydraulic gradient</param>
    ''' <param name="outputIntermediateRasters">If true, saves output of intermediate calculations in the same directory as the output magnitude raster</param>
    ''' <param name="fillsinks">if true, enables sink filling</param>
    ''' <param name="p_smthDEM">If specified, outputs the smoothed DEM to the specified file (path and extension must be specified)</param>
    ''' <param name="smthCellNum">The amount of cell to apply to smoothing window </param>
    ''' <remarks>
    ''' </remarks>
    Public Sub New(ByVal dem As IRaster2, ByVal wb As IFeatureClass, ByVal k As IRaster2, ByVal porosity As IRaster2, ByVal slope_zfactor As Single, _
                   ByVal smoothing As Single, ByVal p_mag As String, ByVal p_dir As String, ByVal p_hydrgr As String, ByVal fillsinks As Boolean, _
                   Optional ByVal p_smthDEM As String = "", Optional ByVal outputIntermediateRasters As Boolean = False, _
                   Optional ByVal smthCellNum As Single = 7)
        m_Raster_in_dem = dem
        m_in_wb = wb
        m_Raster_in_k = k
        m_Raster_in_porosity = porosity
        m_savepath_mag = p_mag.Replace("""", "")
        m_savepath_dir = p_dir.Replace("""", "")
        m_savepath_hydrgr = p_hydrgr.Replace("""", "")
        m_savepath_smthDEM = p_smthDEM.Replace("""", "")
        m_slope_zfactor = slope_zfactor
        m_smoothing = smoothing
        m_outputIntermediateRasters = outputIntermediateRasters
        m_savepath = IO.Path.GetDirectoryName(m_savepath_mag)
        m_fillsinks = fillsinks
        m_smthCellNum = smthCellNum
    End Sub

    ''' <summary>
    ''' Calculates the groundwater flow field with darcy's law.
    ''' </summary>
    ''' <returns>An array of raster datasets representing the final calculation. Nothing on error</returns>
    ''' <remarks>
    ''' The raster dataset will be save to the location specified in the constructor.  The return array
    ''' consists of the magnitude velocity raster, direction velocity raster, and the smoothed DEM (if appliable),
    ''' in that order.
    ''' </remarks>
    Public Function calculateDarcyFlow() As RasterDataset()
        Utilities.outputSystemInfo()
        Trace.Indent()

        Dim ret As RasterDataset() = Nothing
        Dim COM As New ComReleaser
        Try
            If (CType(m_Raster_in_dem, IRasterBandCollection).Count <> 1) Then
                Throw New Exception("The input dem has more than 1 band")
            End If
            If (CType(m_Raster_in_k, IRasterBandCollection).Count <> 1) Then
                Throw New Exception("The input hydraulic conductivity has more than 1 band")
            End If
            If (CType(m_Raster_in_porosity, IRasterBandCollection).Count <> 1) Then
                Throw New Exception("The input porosity has more than 1 band")
            End If

            'get the horizontal resolution. use the max of the vert and horiz. res.
            Dim rp As IRasterProps = CType(m_Raster_in_dem, IRasterProps)
            m_slope_horizontalDemRes = Math.Max(CType(rp.MeanCellSize.X, Single), CType(rp.MeanCellSize.Y, Single))

            '0.convert the input to float and replace the nodata value with a known nodata value
            Dim rasterProps As IRasterProps = CType(m_Raster_in_dem, IRasterProps)
            If rasterProps.PixelType <> rstPixelType.PT_FLOAT Then
                rasterProps.PixelType = rstPixelType.PT_FLOAT
                m_Raster_in_dem = CType(CType(m_Raster_in_dem, ISaveAs2).SaveAs("dem", Nothing, "MEM"), IRasterDataset2).CreateFullRaster
                COM.ManageLifetime(m_Raster_in_dem)
            End If
            'reset the nodata value to a known one.  If the input raster was converted from
            'integer, not doing this will give an output raster that is a bit inconsistent
            'with the input data. e.g. there will be some elevations in the output that are significantly
            'lower than the input elevations.
            If rasterProps.NoDataValue(0) > -3.0E+38 Then
                Utilities.fix_nodata(-3.40282347E+38, m_Raster_in_dem)
            End If
            rasterProps = CType(m_Raster_in_k, IRasterProps)
            If rasterProps.PixelType <> rstPixelType.PT_FLOAT Then
                rasterProps.PixelType = rstPixelType.PT_FLOAT
                m_Raster_in_k = CType(CType(m_Raster_in_k, ISaveAs2).SaveAs("dem", Nothing, "MEM"), IRasterDataset2).CreateFullRaster
                COM.ManageLifetime(m_Raster_in_k)
            End If
            If rasterProps.NoDataValue(0) > -3.0E+38 Then
                Utilities.fix_nodata(-3.40282347E+38, m_Raster_in_k)
            End If
            rasterProps = CType(m_Raster_in_porosity, IRasterProps)
            If rasterProps.PixelType <> rstPixelType.PT_FLOAT Then
                rasterProps.PixelType = rstPixelType.PT_FLOAT
                m_Raster_in_porosity = CType(CType(m_Raster_in_porosity, ISaveAs2).SaveAs("dem", Nothing, "MEM"), IRasterDataset2).CreateFullRaster
                COM.ManageLifetime(m_Raster_in_porosity)
            End If
            If rasterProps.NoDataValue(0) > -3.0E+38 Then
                Utilities.fix_nodata(-3.40282347E+38, m_Raster_in_porosity)
            End If

            Dim smoothed_dem As IRaster2            'contains the smoothed raster
            Dim smoothed_filled_dem As IRaster2
            Dim GxGy As IRaster2()                  'i=0 partial x, i=1 partial y, i=2 locations of flat areas
            Dim FDDR As IRaster2()                  'i=0 flow directions from SA, i=1 drop raster from SA

            '''Hongzhuan Lei, comment this section since it will cause the result different with the original
            '''begin, date:10/13/2015
            '  Dim rAEnv As IRasterAnalysisEnvironment = New RasterAnalysis
            '  Dim ws As IWorkspace = Utilities.OpenScratchWorkspace
            '  rAEnv.OutSpatialReference = CType(m_Raster_in_dem.RasterDataset, IGeoDataset).SpatialReference
            '  rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, m_Raster_in_dem)
            '  rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, m_Raster_in_dem)
            '  rAEnv.OutWorkspace = ws
            '  rAEnv.SetAsNewDefaultEnvironment()
            '''end, date:10/13/2015

            Trace.WriteLine("Smoothing DEM")
            smoothed_dem = smooth(m_Raster_in_dem)
            COM.ManageLifetime(smoothed_dem)
            Windows.Forms.Application.DoEvents()
            If m_fillsinks Then
                Trace.WriteLine("Filling Sinks")
                'smoothed_filled_dem = fillsinks(smoothed_dem, m_Raster_in_dem, m_in_wb)    'superimpose original DEM
                'smoothed_filled_dem = fillsinks(smoothed_dem, m_Raster_in_dem)             'alternate way where water bodies are not super imposed
                smoothed_filled_dem = fillsinks(r:=smoothed_dem, waterbodies:=m_in_wb)      'superimpose smoothed dem
                COM.ManageLifetime(smoothed_filled_dem)
            Else
                smoothed_filled_dem = smoothed_dem
            End If

            Windows.Forms.Application.DoEvents()
            Trace.WriteLine("Calculating Slope")
            GxGy = slope2(smoothed_filled_dem)
            COM.ManageLifetime(GxGy(0))
            COM.ManageLifetime(GxGy(1))
            COM.ManageLifetime(GxGy(2))
            Windows.Forms.Application.DoEvents()

            Trace.WriteLine("Calculating flow directions")
            FDDR = flowdir(smoothed_filled_dem)
            COM.ManageLifetime(FDDR(0))
            COM.ManageLifetime(FDDR(1))
            Windows.Forms.Application.DoEvents()

            'from the flowdir tool, only take the direction in flat areas
            'this will then be combined with Gx and Gy to get the flow
            'in non flat areas as well.
            Trace.WriteLine("Processing flow directions")
            m_Raster_out_dir = convertFD(FDDR(0))
            ComReleaser.ReleaseCOMObject(FDDR(0))
            FDDR(0) = Nothing
            COM.ManageLifetime(m_Raster_out_dir)
            Windows.Forms.Application.DoEvents()
            m_Raster_out_dir = flowdir2(GxGy, m_Raster_out_dir)
            Windows.Forms.Application.DoEvents()

            Trace.WriteLine("Calculating gradient magnitude")
            m_Raster_out_mag = getHydrGradMag(GxGy, FDDR(1))
            ComReleaser.ReleaseCOMObject(FDDR(1))
            FDDR(1) = Nothing
            COM.ManageLifetime(m_Raster_out_mag)
            Windows.Forms.Application.DoEvents()
            Trace.WriteLine("Calculating seepage velocity magnitude.")
            Dim hydrGr_r As IRaster2 = m_Raster_out_mag
            m_Raster_out_mag = darcy(m_Raster_out_mag, m_Raster_in_k, m_Raster_in_porosity)

            'save the rasters
            ReDim ret(3)

            Trace.WriteLine("Saving raster...")
            Dim raster_save As ISaveAs2 = CType(m_Raster_out_mag, ISaveAs2)
            Dim newraster_dataset As IRasterDataset
            Try
                newraster_dataset = raster_save.SaveAs(m_savepath_mag, Nothing, "IMAGINE Image")
            Catch ex As Exception
                Throw New Exception("Couldn't save the raster to the location '" & m_savepath_mag & "'. Maybe the file exists already?")
            End Try
            ret(0) = CType(newraster_dataset, RasterDataset)
            Trace.WriteLine("Saved raster to " & m_savepath_mag)

            Trace.WriteLine("Saving raster...")
            raster_save = CType(m_Raster_out_dir, ISaveAs2)
            Try
                newraster_dataset = CType(raster_save.SaveAs(m_savepath_dir, Nothing, "IMAGINE Image"), IRasterDataset)
            Catch ex As Exception
                Throw New Exception("Couldn't save the raster to the location '" & m_savepath_dir & "'. Maybe the file exists already?")
            End Try
            ret(1) = CType(newraster_dataset, RasterDataset)
            Trace.WriteLine("Saved raster to " & m_savepath_dir)

            'save the hydraulic gradient raster, if the user has selected to do so
            'note this will be the same raster output by the slope function if the user also selected
            'to output intermediate rasters.
            If m_savepath_hydrgr <> "" Then
                Trace.WriteLine("Saving raster...")
                raster_save = CType(hydrGr_r, ISaveAs2)
                Try
                    newraster_dataset = CType(raster_save.SaveAs(m_savepath_hydrgr, Nothing, "IMAGINE Image"), IRasterDataset)
                Catch ex As Exception
                    Throw New Exception("Couldn't save the raster to the location '" & m_savepath_hydrgr & "'. Maybe the file exists already?")
                End Try
                ret(2) = CType(newraster_dataset, RasterDataset)
                Utilities.createRasterLayerFromDataset(CType(newraster_dataset, RasterDataset))
                Trace.WriteLine("Saved raster to " & m_savepath_hydrgr)
            End If

            If m_savepath_smthDEM <> "" Then
                Trace.WriteLine("Saving raster...")
                raster_save = CType(smoothed_dem, ISaveAs2)
                Try
                    newraster_dataset = CType(raster_save.SaveAs(m_savepath_smthDEM, Nothing, "IMAGINE Image"), IRasterDataset)
                Catch ex As Exception
                    Throw New Exception("Couldn't save the raster to the location '" & m_savepath_smthDEM & "'. Maybe the file exists already?")
                End Try
                ret(3) = CType(newraster_dataset, RasterDataset)
                Trace.WriteLine("Saved raster to " & m_savepath_smthDEM)
            End If

            ComReleaser.ReleaseCOMObject(m_Raster_out_dir)
            ComReleaser.ReleaseCOMObject(m_Raster_out_mag)
            ComReleaser.ReleaseCOMObject(hydrGr_r)
            ComReleaser.ReleaseCOMObject(smoothed_dem)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = Nothing
        End Try

        Trace.Unindent()

        Return ret
    End Function



    ''' <summary>
    ''' Fills in sinks and then superimposes the elevations that are overlain by the water bodies layer
    ''' </summary>
    ''' <param name="r">the smoothed input raster</param>
    ''' <param name="r_orig">The unsmoothed version of the input raster. Only used when the waterbodies
    ''' parameter is specified</param>
    ''' <param name="waterbodies">the water bodes layer. if nothing, this parameter will be ignored</param>
    ''' <remarks>
    ''' After this function completes, the output raster will have the NoData value set to 
    ''' the default NoData value, regardless of what value was set before (due to the mask and fill functions).  
    ''' For float rasters this value is (approximately) -3.40282347E+38.
    ''' http://www.nacs.uci.edu/rcs/gis/recipes/RASTER_NOTES.txt
    ''' </remarks>
    Private Function fillsinks(ByVal r As IRaster2, Optional ByVal r_orig As IRaster2 = Nothing, Optional ByVal waterbodies As IFeatureClass = Nothing) As IRaster2
        Trace.Indent()
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        Trace.WriteLine("Filling sinks...")

        If r_orig Is Nothing Then
            r_orig = r
        End If


        'fill sinks
        'sometimes this will fail with ERROR 010067: Error in executing grid expression. 
        'don't know what causes this, but re-importing the .dem file using DEM to raster fixed it.
        Dim filler As IHydrologyOp2 = New RasterHydrologyOp()
        Dim filledRaster As Raster

        Dim errRetry As Boolean = True
RetryPoint_1:
        Try
            filledRaster = filler.Fill(r)
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_1
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        If Not waterbodies Is Nothing Then
            'extract the areas of the raster that overlap the waterbodies
            Dim mask As IExtractionOp2 = New RasterExtractionOp
            Dim maskedRaster As IRaster2

            errRetry = True
RetryPoint_2:
            Try
                maskedRaster = mask.Raster(r_orig, waterbodies)
            Catch ex As Exception
                'MsgBox(ex.ToString)
                If errRetry Then
                    handleRasterOpError010240(ex.ToString)
                    errRetry = False
                    GoTo RetryPoint_2
                Else
                    Throw New Exception(ex.ToString)
                End If
            End Try

            'combine the two rasters via mosaic
            Dim mosaicer As New MosaicRaster()
            Dim mosaicer_rc As IRasterCollection = CType(mosaicer, IRasterCollection)
            Dim mosaicer_save As ISaveAs2 = CType(mosaicer, ISaveAs2)

            mosaicer.MosaicOperatorType = rstMosaicOperatorType.MT_LAST
            mosaicer_rc.Append(filledRaster)
            mosaicer_rc.Append(CType(maskedRaster, IRaster))

            'clear out garbage data
            Utilities.replace_DEM_nodata(CType(filledRaster, IRaster2))
            filledRaster = CType(mosaicer_save.SaveAs("tmp_filledDEM", ws, "MEM"), IRasterDataset3).CreateFullRaster
        Else
            'clear out garbage data
            Utilities.replace_DEM_nodata(CType(filledRaster, IRaster2))
            filledRaster = CType((CType(filledRaster, ISaveAs)).SaveAs("tmp_filledDEM", ws, "MEM"), IRasterDataset3).CreateFullRaster
        End If

        If m_outputIntermediateRasters Then Utilities.createRasterLayerFromRaster(CType(filledRaster, IRaster2), IO.Path.Combine(m_savepath, "tmp_filledDEM" & Now.Ticks & ".img"))

        Trace.WriteLine("Filling sinks finished")
        Trace.Unindent()

        Return CType(filledRaster, IRaster2)
    End Function

    ''' <summary>
    ''' smooths the given raster the number of times specified by m_smoothing
    ''' </summary>
    ''' <param name="r">The raster to smooth</param>
    ''' <remarks>
    ''' Smooths with a 7x7 averaging filter. This function throws exceptions which must be caught by the caller.
    ''' </remarks>
    Private Function smooth(ByRef r As IRaster2) As IRaster2
        Trace.Indent()
        Trace.WriteLine("Smoothing...")

        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Dim op As INeighborhoodOp = New RasterNeighborhoodOp
        Dim op_neighb As New RasterNeighborhood
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace
        'Dim rAEnv As IRasterAnalysisEnvironment        
        Dim r2 As IRaster2 = r
        Dim tmp As IRaster2 = r2

        'rAEnv = op
        'rAEnv.OutSpatialReference = CType(r.RasterDataset, IGeoDataset).SpatialReference
        'rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, r)
        'rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, r)
        'rAEnv.OutWorkspace = ws

        'manage com objects
        COM.ManageLifetime(op)
        COM.ManageLifetime(op_neighb)
        ' Hongzhuan Lei, set cell number for smoothing, 04/27/2016.
        'op_neighb.SetRectangle(7, 7, esriGeoAnalysisUnitsEnum.esriUnitsCells)
        op_neighb.SetRectangle(m_smthCellNum, m_smthCellNum, esriGeoAnalysisUnitsEnum.esriUnitsCells)

        Dim errRetry As Boolean = True
RetryPoint:
        Try
            For i As Integer = 1 To Convert.ToInt32(m_smoothing)
                errRetry = True
                r2 = CType(op.FocalStatistics(tmp, esriGeoAnalysisStatisticsEnum.esriGeoAnalysisStatsMean, op_neighb, True), IRaster2)
                'release each generated com object when we no longer need it
                'dont release on the first iteration since we don't want to release the input raster
                If i > 1 Then Marshal.FinalReleaseComObject(tmp)
                tmp = r2
            Next
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        Utilities.replace_DEM_nodata(r2)
        If m_outputIntermediateRasters Then Utilities.createRasterLayerFromRaster(r2, IO.Path.Combine(m_savepath, "tmp_smoothedDEM" & Now.Ticks & ".img"))

        Trace.WriteLine("Smoothing finished")
        Trace.Unindent()

        Return r2
    End Function

    ''' <summary>
    ''' computes sqrt(x^2 + y^2) for the two input rasters
    ''' </summary>
    ''' <param name="r">array of rasters. r(0) is the x component of the slope, r(1) is the y component</param>
    ''' <param name="dr">the drop raster as calculated by SA FlowDirections.</param>
    ''' <returns>The hydraulic gradient magnitude raster</returns>
    ''' <remarks>In flat areas, the value of <paramref name="dr"></paramref> will be used instead of the
    ''' calculated one.</remarks>
    Private Function getHydrGradMag(ByVal r As IRaster2(), ByVal dr As IRaster2) As IRaster2
        Trace.Indent()
        Trace.WriteLine("Computing magnitude...")

        '****************************************************************************
        'combine the results
        'for some reason, this calculation didnt work with the raster calculator
        'so I had to do it manually
        '****************************************************************************
        Dim result As IRaster2
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser        
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        'create a new raster for writing
        result = CType(CType(r(0), ISaveAs).SaveAs("tmp_gradMag", ws, "MEM"), IRasterDataset2).CreateFullRaster

        Dim p As New Pnt
        COM.ManageLifetime(p)
        p.SetCoords(CType(r(0), IRasterProps).Width, 256)

        Dim pxCursor_Gx As IRasterCursor = r(0).CreateCursorEx(p)   'for scanning the raster
        Dim pxCursor_Gy As IRasterCursor = r(1).CreateCursorEx(p)   'for scanning the raster            
        Dim pxCursor_result As IRasterCursor = result.CreateCursorEx(p)   'for scanning the raster
        Dim pxCursor_flats As IRasterCursor = r(2).CreateCursorEx(p)   'for scanning the raster. use the second index since we want the unxepanded flat areas here 
        COM.ManageLifetime(pxCursor_Gx)
        COM.ManageLifetime(pxCursor_Gy)
        COM.ManageLifetime(pxCursor_result)
        COM.ManageLifetime(pxCursor_flats)

        Dim pxBlock_Gx As IPixelBlock3
        Dim pxBlock_Gy As IPixelBlock3
        Dim pxBlock_result As IPixelBlock3
        Dim pxBlock_flats As IPixelBlock3
        Dim pixels_Gx As System.Array
        Dim pixels_Gy As System.Array
        Dim pixels_result As System.Array
        Dim pixels_flats As System.Array
        Dim pixval_Gx As Single
        Dim pixval_Gy As Single
        Dim pixval_flats As Single
        Dim nodata As Single = Convert.ToSingle(Utilities.getRasterNoDataValue(r(0)))
        Dim pxEdit_result As IRasterEdit = CType(result, IRasterEdit)           'for editing pixel blocks
        Dim x, y As Double          'for maptopixel and pixeltomap operations
        Dim row, col As Integer     'for maptopixel and pixeltomap operations

        'the Gx and Gy rasters should be the same size (if this is not the case then this wont work)
        pxCursor_Gx.Reset()
        pxCursor_Gy.Reset()
        pxCursor_result.Reset()
        pxCursor_flats.Reset()
        Do
            pxBlock_Gx = pxCursor_Gx.PixelBlock
            pxBlock_Gy = pxCursor_Gy.PixelBlock
            pxBlock_result = pxCursor_result.PixelBlock
            pxBlock_flats = pxCursor_flats.PixelBlock

            'get the pixel array of the first raster band
            pixels_Gx = CType(pxBlock_Gx.PixelData(0), System.Array)
            pixels_Gy = CType(pxBlock_Gy.PixelData(0), System.Array)
            pixels_result = CType(pxBlock_result.PixelData(0), System.Array)
            pixels_flats = CType(pxBlock_flats.PixelData(0), System.Array)
            If pxBlock_Gx.Width <> pxBlock_Gy.Width Or pxBlock_Gx.Height <> pxBlock_Gy.Height Then
                Throw New Exception("Pixel blocks are of different size!!")
            End If
            For i As Integer = 0 To pxBlock_Gx.Width - 1
                For j As Integer = 0 To pxBlock_Gx.Height - 1
                    'get the pixel values
                    pixval_Gx = Convert.ToSingle(pixels_Gx.GetValue(i, j))
                    pixval_Gy = Convert.ToSingle(pixels_Gy.GetValue(i, j))
                    pixval_flats = Convert.ToSingle(pixels_flats.GetValue(i, j))

                    If pixval_flats = 1 Then
                        'there is a flat area
                        r(2).PixelToMap(Convert.ToInt32(pxCursor_Gx.TopLeft.X + i), Convert.ToInt32(pxCursor_Gx.TopLeft.Y + j), x, y)
                        dr.MapToPixel(x, y, col, row)
                        'in the flat area, assign the slope calculated by flowdir
                        pixels_result.SetValue(CType(Convert.ToSingle(dr.GetPixelValue(1, col, row)) / 100, Single), i, j)
                    Else
                        If pixval_Gx = 0 And pixval_Gy = 0 Then
                            'we have a flat area
                            'Trace.WriteLine("flat area where there should be none")
                        Else
                            If pixval_Gx <> nodata And pixval_Gy <> nodata Then
                                pixels_result.SetValue(CType(Math.Sqrt(pixval_Gx * pixval_Gx + pixval_Gy * pixval_Gy), Single), i, j)
                            Else
                                pixels_result.SetValue(nodata, i, j)
                            End If
                        End If
                    End If
                Next
            Next
            pxBlock_result.PixelData(0) = pixels_result
            pxEdit_result.Write(pxCursor_result.TopLeft, pxBlock_result)
        Loop While pxCursor_Gx.Next And pxCursor_Gy.Next And pxCursor_result.Next And pxCursor_flats.Next

        If m_outputIntermediateRasters Then Utilities.createRasterLayerFromRaster(result, IO.Path.Combine(m_savepath, "tmp_gradMag" & Now.Ticks & ".img"))

        Trace.WriteLine("Computing magnitude done")
        Trace.Unindent()

        Return result
    End Function

    ''' <summary>
    ''' Calculates the flow directions using the SA tool FlowDirection
    ''' </summary>
    ''' <param name="r">the raster to calculate the directions of</param>
    ''' <returns>An array of raster represeting the flow in one of 8 directions (first index), and 
    ''' the percent drop raster (second index)</returns>
    ''' <remarks></remarks>
    Private Function flowdir(ByRef r As IRaster2) As IRaster2()
        Trace.Indent()
        Trace.WriteLine("Calculating flow directions...")

        Dim f(1) As IRaster2
        Dim dir, slp As IRaster2
        Dim op As IHydrologyOp2 = New RasterHydrologyOp
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace
        'Dim raEnv As IRasterAnalysisEnvironment

        'raEnv = op
        'raEnv.OutWorkspace = ws

        'flow directions
        Dim errRetry As Boolean = True
RetryPoint_1:
        Try
            dir = op.FlowDirection(r, False, True)
            f(0) = CType(CType(dir, ISaveAs2).SaveAs("tmp_flowdir", ws, "MEM"), IRasterDataset2).CreateFullRaster
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_1
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try
        

        'drop raster
        errRetry = True
RetryPoint_2:
        Try
            slp = op.FlowDirection(r, True, True)
            f(1) = CType(CType(slp, ISaveAs2).SaveAs("tmp_flowdir_slope", ws, "MEM"), IRasterDataset2).CreateFullRaster
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_2
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        If m_outputIntermediateRasters Then
            Utilities.createRasterLayerFromRaster(f(0), IO.Path.Combine(m_savepath, "tmp_flowdir" & Now.Ticks & ".img"))
            Utilities.createRasterLayerFromRaster(f(1), IO.Path.Combine(m_savepath, "tmp_flowdir_slope" & Now.Ticks & ".img"))
        End If

        ComReleaser.ReleaseCOMObject(op)
        ComReleaser.ReleaseCOMObject(dir)
        ComReleaser.ReleaseCOMObject(slp)

        Trace.WriteLine("Calculating flow directions finished.")
        Trace.Unindent()

        Return f
    End Function

    ''' <summary>
    ''' Calculates flow direction based on partial x and partial y. 
    ''' </summary>
    ''' <param name="r">array containing partial x in the frist index and partial y in the second, locations 
    ''' of flat areas in the third.
    ''' The input rasters should have the same cell size, extent, projection etc.</param>
    ''' <param name="rf">The raster used for flat areas.  When there is a flat area, the value of
    ''' partial x and partial y will be replaced by the value of rf.  The values in rf should be in degrees clockwise from
    ''' north</param>
    ''' <returns>a raster representing the angle clockwise from north</returns>
    ''' <remarks></remarks>
    Private Function flowdir2(ByRef r As IRaster2(), ByVal rf As IRaster2) As IRaster2
        Trace.Indent()
        Trace.WriteLine("Calculating flow direction...")

        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        'create a new raster for writing
        Dim result As IRaster2 = CType(CType(r(0), ISaveAs).SaveAs("tmp_flowdirtwo", ws, "MEM"), IRasterDataset2).CreateFullRaster
        Dim tmp As IRaster2

        Dim p As New Pnt
        p.SetCoords(CType(r(0), IRasterProps).Width, 256)
        Dim pxCursor_Gx As IRasterCursor = r(0).CreateCursorEx(p)   'for scanning the raster
        Dim pxCursor_Gy As IRasterCursor = r(1).CreateCursorEx(p)   'for scanning the raster            
        Dim pxCursor_flats As IRasterCursor = r(3).CreateCursorEx(p)   'for scanning the raster. use r(3) since we want the expanded flat areas            
        Dim pxCursor_result As IRasterCursor = result.CreateCursorEx(p)   'for scanning the raster
        COM.ManageLifetime(pxCursor_Gx)
        COM.ManageLifetime(pxCursor_Gy)
        COM.ManageLifetime(pxCursor_flats)
        COM.ManageLifetime(pxCursor_result)

        Dim pxBlock_Gx As IPixelBlock3
        Dim pxBlock_Gy As IPixelBlock3
        Dim pxBlock_flats As IPixelBlock3
        Dim pxBlock_result As IPixelBlock3
        Dim pixels_Gx As System.Array
        Dim pixels_Gy As System.Array
        Dim pixels_flats As System.Array
        Dim pixels_result As System.Array
        Dim pixval_Gx As Single
        Dim pixval_Gy As Single
        Dim pixval_flats As Single
        Dim pxEdit_result As IRasterEdit = CType(result, IRasterEdit)           'for editing pixel blocks

        Dim nodata As Single = Convert.ToSingle(Utilities.getRasterNoDataValue(r(0)))

        'the angle assigned to any given cell
        Dim ang As Single
        Dim x, y As Double
        Dim row, col As Integer

        'the Gx and Gy rasters should be the same size (if this is not the case then this wont work)
        pxCursor_Gx.Reset()
        pxCursor_Gy.Reset()
        pxCursor_flats.Reset()
        pxCursor_result.Reset()
        Do
            pxBlock_Gx = pxCursor_Gx.PixelBlock
            pxBlock_Gy = pxCursor_Gy.PixelBlock
            pxBlock_flats = pxCursor_flats.PixelBlock
            pxBlock_result = pxCursor_result.PixelBlock

            'get the pixel array of the first raster band
            pixels_Gx = CType(pxBlock_Gx.PixelData(0), System.Array)
            pixels_Gy = CType(pxBlock_Gy.PixelData(0), System.Array)
            pixels_flats = CType(pxBlock_flats.PixelData(0), System.Array)
            pixels_result = CType(pxBlock_result.PixelData(0), System.Array)
            If pxBlock_Gx.Width <> pxBlock_Gy.Width Or pxBlock_Gx.Height <> pxBlock_Gy.Height Then
                Throw New Exception("Pixel blocks are of different size!!")
            End If
            For i As Integer = 0 To pxBlock_Gx.Width - 1
                For j As Integer = 0 To pxBlock_Gx.Height - 1
                    pixval_Gx = Convert.ToSingle(pixels_Gx.GetValue(i, j))
                    pixval_Gy = Convert.ToSingle(pixels_Gy.GetValue(i, j))
                    pixval_flats = Convert.ToSingle(pixels_flats.GetValue(i, j))

                    If pixval_flats = 1 Then
                        r(3).PixelToMap(Convert.ToInt32(pxCursor_flats.TopLeft.X + i), Convert.ToInt32(pxCursor_flats.TopLeft.Y + j), x, y)
                        rf.MapToPixel(x, y, col, row)
                        ang = Convert.ToSingle(rf.GetPixelValue(0, col, row))
                    Else
                        'angles returned by atan are relative to east, increasing counter clockwise
                        'the code below corrects for it so that it is an angle in degrees from North.
                        'Could also use atan2 but would need extra conversion
                        'later anyways
                        Select Case pixval_Gx
                            Case Is = nodata
                                ang = nodata
                                Exit Select
                            Case Else
                                If pixval_Gx <> 0 Then
                                    ang = Math.Atan(pixval_Gy / pixval_Gx) * 57.2957795
                                Else
                                    'flow is either north or south (don't know yet)
                                    ang = 90
                                End If

                                'determine flow direction angle clockwise from north
                                Select Case pixval_Gy
                                    Case Is = nodata
                                        ang = nodata
                                        Exit Select
                                    Case Is >= 0
                                        If pixval_Gx >= 0 Then
                                            If pixval_Gx = 0 And pixval_Gy = 0 Then
                                                'flat area    
                                                'Throw New Exception("Flat area found where there should be none...")
                                            Else                                                
                                                ang = 270 - ang     'third quadrant
                                            End If
                                        Else                                            
                                            ang = 90 - ang      'fourth quadrant (angle is negative)
                                        End If
                                        Exit Select
                                    Case Is < 0
                                        If pixval_Gx <= 0 Then                                            
                                            ang = 90 - ang      'first quadrant
                                        Else                                            
                                            ang = 270 - ang     'second quadrant (angle is negative)
                                        End If
                                        Exit Select
                                End Select
                        End Select
                    End If
                    pixels_result.SetValue(ang, i, j)
                Next
            Next
            pxBlock_result.PixelData(0) = pixels_result
            pxEdit_result.Write(pxCursor_result.TopLeft, pxBlock_result)
        Loop While pxCursor_Gx.Next And pxCursor_Gy.Next And pxCursor_flats.Next And pxCursor_result.Next


        tmp = CType(CType(result, ISaveAs2).SaveAs("tmp_flowdirtwo", ws, "MEM"), IRasterDataset2).CreateFullRaster
        ComReleaser.ReleaseCOMObject(result)
        result = tmp

        Trace.WriteLine("Calculating flow direction finished")
        Trace.Unindent()

        Return result
    End Function


    ''' <summary>
    ''' Calculates the slope with a filter mask
    ''' </summary>
    ''' <param name="r">The input raster</param>
    ''' <returns>Returns 4 rasters, partial x (Gx) partial y (Gy) and a raster representing the locations 
    ''' of flat areas (unexpanded and expanded). index 0=Gx, 1=Gy, 2=unexpanded flat areas, 3=expanded 
    ''' flat areas.
    ''' </returns>
    ''' <remarks>Many different filters can be used.  When used with the Sobel filter, this function
    ''' returns Gx and Gx that when properly combined, give the same result as Slope and Aspect functions of spatial analyst</remarks>
    Private Function slope2(ByRef r As IRaster2) As IRaster2()
        Trace.Indent()
        Trace.WriteLine("Calculating slope...")

        'Create array to specify neighborhood and wieghts       
        'sobel
        Dim lwidth_x As Integer = 3
        Dim lwidth_y As Integer = 3
        Dim lheight_x As Integer = 3
        Dim lheight_y As Integer = 3
        Dim normalization As Integer = 8
        Dim Gx(2, 2) As Integer
        Dim Gy(2, 2) As Integer

        Gx(0, 0) = -1
        Gx(0, 1) = 0
        Gx(0, 2) = 1
        Gx(1, 0) = -2
        Gx(1, 1) = 0
        Gx(1, 2) = 2
        Gx(2, 0) = -1
        Gx(2, 1) = 0
        Gx(2, 2) = 1

        Gy(0, 0) = 1
        Gy(0, 1) = 2
        Gy(0, 2) = 1
        Gy(1, 0) = 0
        Gy(1, 1) = 0
        Gy(1, 2) = 0
        Gy(2, 0) = -1
        Gy(2, 1) = -2
        Gy(2, 2) = -1

        'central difference
        'Gx(0, 0) = 0
        'Gx(0, 1) = 0
        'Gx(0, 2) = 0
        'Gx(1, 0) = -1
        'Gx(1, 1) = 0
        'Gx(1, 2) = 1
        'Gx(2, 0) = 0
        'Gx(2, 1) = 0
        'Gx(2, 2) = 0

        'Gy(0, 0) = 0
        'Gy(0, 1) = 1
        'Gy(0, 2) = 0
        'Gy(1, 0) = 0
        'Gy(1, 1) = 0
        'Gy(1, 2) = 0
        'Gy(2, 0) = 0
        'Gy(2, 1) = -1
        'Gy(2, 2) = 0

        'Dim lwidth_x As Integer = 7
        'Dim lwidth_y As Integer = 5
        'Dim lheight_x As Integer = 5
        'Dim lheight_y As Integer = 7
        'Dim Gx(4, 6) As Integer
        'Dim Gy(6, 4) As Integer

        'Gx(0, 0) = 1
        'Gx(0, 1) = 4
        'Gx(0, 2) = 5
        'Gx(0, 3) = 0
        'Gx(0, 4) = -5
        'Gx(0, 5) = -4
        'Gx(0, 6) = -1
        'Gx(1, 0) = 4
        'Gx(1, 1) = 16
        'Gx(1, 2) = 20
        'Gx(1, 3) = 0
        'Gx(1, 4) = -20
        'Gx(1, 5) = -16
        'Gx(1, 6) = -4
        'Gx(2, 0) = 6
        'Gx(2, 1) = 24
        'Gx(2, 2) = 30
        'Gx(2, 3) = 0
        'Gx(2, 4) = -30
        'Gx(2, 5) = -24
        'Gx(2, 6) = -6
        'Gx(3, 0) = 4
        'Gx(3, 1) = 16
        'Gx(3, 2) = 20
        'Gx(3, 3) = 0
        'Gx(3, 4) = -20
        'Gx(3, 5) = -16
        'Gx(3, 6) = -4
        'Gx(4, 0) = 1
        'Gx(4, 1) = 4
        'Gx(4, 2) = 5
        'Gx(4, 3) = 0
        'Gx(4, 4) = -5
        'Gx(4, 5) = -4
        'Gx(4, 6) = -1

        'Gy(0, 0) = -1
        'Gy(0, 1) = -4
        'Gy(0, 2) = -6
        'Gy(0, 3) = -4
        'Gy(0, 4) = -1
        'Gy(1, 0) = -4
        'Gy(1, 1) = -16
        'Gy(1, 2) = -24
        'Gy(1, 3) = -16
        'Gy(1, 4) = -4
        'Gy(2, 0) = -5
        'Gy(2, 1) = -20
        'Gy(2, 2) = -30
        'Gy(2, 3) = -20
        'Gy(2, 4) = -5
        'Gy(3, 0) = 0
        'Gy(3, 1) = 0
        'Gy(3, 2) = 0
        'Gy(3, 3) = 0
        'Gy(3, 4) = 0
        'Gy(4, 0) = 5
        'Gy(4, 1) = 20
        'Gy(4, 2) = 30
        'Gy(4, 3) = 20
        'Gy(4, 4) = 5
        'Gy(5, 0) = 4
        'Gy(5, 1) = 16
        'Gy(5, 2) = 24
        'Gy(5, 3) = 16
        'Gy(5, 4) = 4
        'Gy(6, 0) = 1
        'Gy(6, 1) = 4
        'Gy(6, 2) = 6
        'Gy(6, 3) = 4
        'Gy(6, 4) = 1

        'Dim lwidth_x As Integer = 5
        'Dim lwidth_y As Integer = 3
        'Dim lheight_x As Integer = 3
        'Dim lheight_y As Integer = 5
        'Dim Gx(2, 4) As Integer
        'Dim Gy(4, 2) As Integer

        'Gx(0, 0) = 1
        'Gx(0, 1) = -8
        'Gx(0, 2) = 0
        'Gx(0, 3) = 8
        'Gx(0, 4) = -1
        'Gx(1, 0) = 2
        'Gx(1, 1) = -16
        'Gx(1, 2) = 0
        'Gx(1, 3) = 16
        'Gx(1, 4) = -2
        'Gx(2, 0) = 1
        'Gx(2, 1) = -8
        'Gx(2, 2) = 0
        'Gx(2, 3) = 8
        'Gx(2, 4) = -1

        'Gy(0, 0) = -1
        'Gy(0, 1) = -2
        'Gy(0, 2) = -1
        'Gy(1, 0) = 8
        'Gy(1, 1) = 16
        'Gy(1, 2) = 8
        'Gy(2, 0) = 0
        'Gy(2, 1) = 0
        'Gy(2, 2) = 0
        'Gy(3, 0) = -8
        'Gy(3, 1) = -16
        'Gy(3, 2) = -8
        'Gy(4, 0) = 1
        'Gy(4, 1) = 2
        'Gy(4, 2) = 1

        'Dim lwidth As Integer = 5
        'Dim lheight As Integer = 5
        'Dim Gx(4, 4) As Integer
        'Dim Gy(4, 4) As Integer

        'Gx(0, 0) = 1
        'Gx(0, 1) = -8
        'Gx(0, 2) = 0
        'Gx(0, 3) = 8
        'Gx(0, 4) = -1
        'Gx(1, 0) = 2
        'Gx(1, 1) = -16
        'Gx(1, 2) = 0
        'Gx(1, 3) = 16
        'Gx(1, 4) = -2
        'Gx(2, 0) = 3
        'Gx(2, 1) = -24
        'Gx(2, 2) = 0
        'Gx(2, 3) = 24
        'Gx(2, 4) = -3
        'Gx(3, 0) = 2
        'Gx(3, 1) = -16
        'Gx(3, 2) = 0
        'Gx(3, 3) = 16
        'Gx(3, 4) = -2
        'Gx(4, 0) = 1
        'Gx(4, 1) = -8
        'Gx(4, 2) = 0
        'Gx(4, 3) = 8
        'Gx(4, 4) = -1

        'Gy(0, 0) = -1
        'Gy(0, 1) = -2
        'Gy(0, 2) = -3
        'Gy(0, 3) = -2
        'Gy(0, 4) = -1
        'Gy(1, 0) = 8
        'Gy(1, 1) = 16
        'Gy(1, 2) = 24
        'Gy(1, 3) = 16
        'Gy(1, 4) = 8
        'Gy(2, 0) = 0
        'Gy(2, 1) = 0
        'Gy(2, 2) = 0
        'Gy(2, 3) = 0
        'Gy(2, 4) = 0
        'Gy(3, 0) = -8
        'Gy(3, 1) = -16
        'Gy(3, 2) = -24
        'Gy(3, 3) = -16
        'Gy(3, 4) = -8
        'Gy(4, 0) = 1
        'Gy(4, 1) = 2
        'Gy(4, 2) = 3
        'Gy(4, 3) = 2
        'Gy(4, 4) = 1

        'Gx(0, 0) = 0
        'Gx(0, 1) = 0
        'Gx(0, 2) = 0
        'Gx(0, 3) = 0
        'Gx(0, 4) = 0
        'Gx(1, 0) = 0
        'Gx(1, 1) = 0
        'Gx(1, 2) = 0
        'Gx(1, 3) = 0
        'Gx(1, 4) = 0
        'Gx(2, 0) = 1
        'Gx(2, 1) = -8
        'Gx(2, 2) = 0
        'Gx(2, 3) = 8
        'Gx(2, 4) = -1
        'Gx(3, 0) = 0
        'Gx(3, 1) = 0
        'Gx(3, 2) = 0
        'Gx(3, 3) = 0
        'Gx(3, 4) = 0
        'Gx(4, 0) = 0
        'Gx(4, 1) = 0
        'Gx(4, 2) = 0
        'Gx(4, 3) = 0
        'Gx(4, 4) = 0

        'Gy(0, 0) = 0
        'Gy(0, 1) = 0
        'Gy(0, 2) = -1
        'Gy(0, 3) = 0
        'Gy(0, 4) = 0
        'Gy(1, 0) = 0
        'Gy(1, 1) = 0
        'Gy(1, 2) = 8
        'Gy(1, 3) = 0
        'Gy(1, 4) = 0
        'Gy(2, 0) = 0
        'Gy(2, 1) = 0
        'Gy(2, 2) = 0
        'Gy(2, 3) = 0
        'Gy(2, 4) = 0
        'Gy(3, 0) = 0
        'Gy(3, 1) = 0
        'Gy(3, 2) = -8
        'Gy(3, 3) = 0
        'Gy(3, 4) = 0
        'Gy(4, 0) = 0
        'Gy(4, 1) = 0
        'Gy(4, 2) = 1
        'Gy(4, 3) = 0
        'Gy(4, 4) = 0

        'dont set the normalization factor here. ArcGIS doesnt seem to play nice with
        'non integer weights. instead, apply normalization  below

        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Dim nop As INeighborhoodOp = New RasterNeighborhoodOp
        Dim op_neighb_Gx As New RasterNeighborhood
        Dim op_neighb_Gy As New RasterNeighborhood
        Dim ras_Gx, ras_Gy, tmp As IRaster2
        'Dim rAEnv As IRasterAnalysisEnvironment
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        'rAEnv = nop
        'rAEnv.OutWorkspace = ws

        'manage com objects
        COM.ManageLifetime(nop)
        COM.ManageLifetime(op_neighb_Gx)
        COM.ManageLifetime(op_neighb_Gy)

        'Set the neghborhoodp
        op_neighb_Gx.SetWeight(lheight_x, lwidth_x, CType(Gx, Object))
        op_neighb_Gy.SetWeight(lheight_y, lwidth_y, CType(Gy, Object))

        'calculate Gx and Gy
        Dim errRetry As Boolean = True
RetryPoint_1:
        Try
            ras_Gx = nop.FocalStatistics(r, esriGeoAnalysisStatisticsEnum.esriGeoAnalysisStatsSum, op_neighb_Gx, True)            
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_1
            Else
                Throw New Exception(ex.ToString)
            End If            
        End Try

        errRetry = True

RetryPoint_2:
        Try            
            ras_Gy = nop.FocalStatistics(r, esriGeoAnalysisStatisticsEnum.esriGeoAnalysisStatsSum, op_neighb_Gy, True)
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_2
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try
        '***********************************************************************************
        'normalize the filter results and detect flat areas
        '***********************************************************************************
        'get the nodata value
        Dim rProps As IRasterProps = CType(r, IRasterProps)
        Dim nodata As Single = rProps.NoDataValue(0)

        Dim p As New Pnt
        p.SetCoords(rProps.Width, 256)
        Dim pxCursor_Gx As IRasterCursor = ras_Gx.CreateCursorEx(p)   'for scanning the raster
        Dim pxCursor_Gy As IRasterCursor = ras_Gy.CreateCursorEx(p)   'for scanning the raster                     
        Dim pxBlock_Gx As IPixelBlock3
        Dim pxBlock_Gy As IPixelBlock3
        Dim pixels_Gx As System.Array
        Dim pixels_Gy As System.Array
        Dim pxEdit_Gx As IRasterEdit = CType(ras_Gx, IRasterEdit)           'for editing pixel blocks  
        Dim pxEdit_Gy As IRasterEdit = CType(ras_Gy, IRasterEdit)           'for editing pixel blocks  
        Dim pixval_Gx As Single
        Dim pixval_Gy As Single


        'the threshold at which the slope will be considered zero.
        Dim zeroThresh As Single = 0

        'the Gx and Gy rasters should be the same size (if this is not the case then this wont work)
        pxCursor_Gx.Reset()
        pxCursor_Gy.Reset()
        Do
            pxBlock_Gx = pxCursor_Gx.PixelBlock
            pxBlock_Gy = pxCursor_Gy.PixelBlock

            'get the pixel array of the first raster band
            pixels_Gx = CType(pxBlock_Gx.PixelData(0), System.Array)
            pixels_Gy = CType(pxBlock_Gy.PixelData(0), System.Array)
            If pxBlock_Gx.Width <> pxBlock_Gy.Width Or pxBlock_Gx.Height <> pxBlock_Gy.Height Then
                Throw New Exception("Pixel blocks are of different size!!")
            End If
            For i As Integer = 0 To pxBlock_Gx.Width - 1
                For j As Integer = 0 To pxBlock_Gx.Height - 1
                    pixval_Gx = pixels_Gx.GetValue(i, j)
                    pixval_Gy = pixels_Gy.GetValue(i, j)

                    If pixval_Gx <> nodata And pixval_Gy <> nodata Then
                        'apply the normalization to the filter results
                        pixels_Gx.SetValue(1 / (normalization * (m_slope_horizontalDemRes / m_slope_zfactor)) * pixval_Gx, i, j)
                        pixels_Gy.SetValue(1 / (normalization * (m_slope_horizontalDemRes / m_slope_zfactor)) * pixval_Gy, i, j)
                    Else
                        pixels_Gx.SetValue(nodata, i, j)
                        pixels_Gy.SetValue(nodata, i, j)
                    End If
                Next
            Next
            pxBlock_Gx.PixelData(0) = pixels_Gx
            pxBlock_Gy.PixelData(0) = pixels_Gy
            pxEdit_Gx.Write(pxCursor_Gx.TopLeft, pxBlock_Gx)
            pxEdit_Gy.Write(pxCursor_Gy.TopLeft, pxBlock_Gy)
        Loop While pxCursor_Gx.Next And pxCursor_Gy.Next
        ComReleaser.ReleaseCOMObject(pxCursor_Gx)
        ComReleaser.ReleaseCOMObject(pxCursor_Gy)

        'expand the flat areas by half of the mask size. this ensures that
        'areas which will be marked as flat by the Flow DIrection tool
        'will remain flat when the flow direction and magnitude are caluclated by
        'flowdir2 and getHydrGradMag
        Dim filterdims(3) As Integer
        filterdims(0) = Math.Truncate((lwidth_x + 1) / 2)
        filterdims(1) = Math.Truncate((lheight_x + 1) / 2)
        filterdims(2) = Math.Truncate((lwidth_y + 1) / 2)
        filterdims(3) = Math.Truncate((lheight_y + 1) / 2)
        Dim ras_flats() As IRaster2 = findFlatAreas(r, filterdims.Max)

        'save the raster. withouth this get weird glitches in the output.        
        tmp = CType(CType(ras_Gx, ISaveAs2).SaveAs("tmp_slope_Gx", ws, "MEM"), IRasterDataset2).CreateFullRaster
        Marshal.FinalReleaseComObject(ras_Gx)
        ras_Gx = tmp
        tmp = CType(CType(ras_Gy, ISaveAs2).SaveAs("tmp_slope_Gy", ws, "MEM"), IRasterDataset2).CreateFullRaster
        Marshal.FinalReleaseComObject(ras_Gy)
        ras_Gy = tmp

        tmp = CType(CType(ras_flats(0), ISaveAs2).SaveAs("tmp_flatareas" & Now.Ticks, ws, "MEM"), IRasterDataset2).CreateFullRaster
        Marshal.FinalReleaseComObject(ras_flats(0))
        ras_flats(0) = tmp
        tmp = CType(CType(ras_flats(1), ISaveAs2).SaveAs("tmp_flatareas_expanded" & Now.Ticks, ws, "MEM"), IRasterDataset2).CreateFullRaster
        Marshal.FinalReleaseComObject(ras_flats(1))
        ras_flats(1) = tmp

        If m_outputIntermediateRasters Then
            Utilities.createRasterLayerFromRaster(ras_Gx, IO.Path.Combine(m_savepath, "tmp_slope_Gx" & Now.Ticks & ".img"))
            Utilities.createRasterLayerFromRaster(ras_Gy, IO.Path.Combine(m_savepath, "tmp_slope_Gy" & Now.Ticks & ".img"))
            Utilities.createRasterLayerFromRaster(ras_flats(0), IO.Path.Combine(m_savepath, "tmp_flatareas" & Now.Ticks & ".img"))
            Utilities.createRasterLayerFromRaster(ras_flats(1), IO.Path.Combine(m_savepath, "tmp_flatareas_expanded" & Now.Ticks & ".img"))
        End If

        Dim ret(3) As IRaster2
        ret(0) = ras_Gx
        ret(1) = ras_Gy
        ret(2) = ras_flats(0)
        ret(3) = ras_flats(1)

        Trace.WriteLine("Calculating slope finished.")
        Trace.Unindent()
        Return ret
    End Function

    ''' <summary>
    ''' Finds areas that will be considered flat by the SA FlowDirection algorithm.
    ''' THe flow direction algorithm considers a cell as flat when all of its eight
    ''' neigbors have a higher or the same elvation as the cell in question.
    ''' </summary>
    ''' <param name="r">The input raster</param>
    ''' <param name="bufferAround">The number of cells to expand the flat area by</param>
    ''' <returns>An array of raster. The first index represents the original flat area and the second
    ''' the expanded flat area</returns>
    ''' <remarks></remarks>
    Private Function findFlatAreas(ByVal r As IRaster2, Optional ByVal bufferAround As Integer = 0) As IRaster2()
        Trace.Indent()
        Trace.WriteLine("Finding flat areas")

        Dim r_inv As IRaster2       'the inverse of the input raster
        Dim r_ff As IRaster2        'the output of focal flow        
        Dim result(1) As IRaster2

        Dim zonelist(0) As Integer  'zones to expand

        Dim RAop As IMapAlgebraOp = New RasterMapAlgebraOp
        Dim RAop2 As IMapAlgebraOp = New RasterMapAlgebraOp
        Dim RNop As INeighborhoodOp = New RasterNeighborhoodOp
        Dim RGop As IGeneralizeOp = New RasterGeneralizeOp
        Dim rAEnv As IRasterAnalysisEnvironment
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        'rAEnv = RAop
        'rAEnv.OutSpatialReference = CType(m_Raster_in_dem.RasterDataset, IGeoDataset).SpatialReference
        'rAEnv.OutWorkspace = ws
        'rAEnv = RAop2
        'rAEnv.OutSpatialReference = CType(m_Raster_in_dem.RasterDataset, IGeoDataset).SpatialReference
        'rAEnv.OutWorkspace = ws
        'rAEnv = RNop
        'rAEnv.OutSpatialReference = CType(m_Raster_in_dem.RasterDataset, IGeoDataset).SpatialReference
        'rAEnv.OutWorkspace = ws
        'rAEnv = RGop
        'rAEnv.OutSpatialReference = CType(m_Raster_in_dem.RasterDataset, IGeoDataset).SpatialReference
        'rAEnv.OutWorkspace = ws

        'invert elevation
        Dim errRetry As Boolean = True
RetryPoint_1:
        Try
            RAop.BindRaster(r, "DEM")
            r_inv = RAop.Execute("[DEM] * -1")
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_1
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        errRetry = True

        'calculate focal flow
RetryPoint_2:
        Try
            r_ff = RNop.FocalFlow(r_inv)
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_2
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        errRetry = True

        'extract flat areas
RetryPoint_3:
        Try
            RAop2.BindRaster(r_ff, "FF")
            result(0) = RAop2.Execute("Con([FF] == 0,1,0)")
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_3
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        errRetry = True

        'expand the flat area by a specified amount
        'expand only zones having a value of 1
RetryPoint_4:
        Try
            zonelist(0) = 1
            result(1) = RGop.Expand(result(0), bufferAround, zonelist)
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint_4
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try

        'clean up
        Marshal.FinalReleaseComObject(r_inv)
        Marshal.FinalReleaseComObject(r_ff)
        Marshal.FinalReleaseComObject(RAop)
        Marshal.FinalReleaseComObject(RAop2)
        Marshal.FinalReleaseComObject(RNop)
        Marshal.FinalReleaseComObject(RGop)
        GC.Collect()
        GC.WaitForPendingFinalizers()

        Trace.WriteLine("Finding flat areas finished")
        Trace.Unindent()

        Return result
    End Function

    ''' <summary>
    ''' Calculates the magnitude of the Darcy flow
    ''' By Yan:  This is pore velocity, but not the Darcy velocity.
    ''' </summary>
    ''' <param name="r_k">the hydraulic conductivity raster</param>
    ''' <param name="r_n">the porosity raster</param>
    ''' <param name="r_slp">the raster representing the slope</param>
    ''' <remarks>
    ''' All the input rasters should have the same extent, same number of rows and columns and same cell size
    ''' </remarks>
    Private Function darcy(ByVal r_slp As IRaster2, ByVal r_k As IRaster2, ByVal r_n As IRaster2) As IRaster2
        Trace.WriteLine("Calculating Darcy flow...")

        'calculate the darcy flow using the input rasters with the MapAlgebra object.
        'using the map algebra object will save having to create lots of temporary rasters ourselves
        '(arcgis will handle this)
        Dim op As IMapAlgebraOp = New RasterMapAlgebraOp
        Dim r_out As IRaster2
        Dim rAEnv As IRasterAnalysisEnvironment
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        'rAEnv = op
        'rAEnv.OutSpatialReference = CType(m_Raster_in_dem.RasterDataset, IGeoDataset).SpatialReference
        'rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, m_Raster_in_dem)
        'rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, m_Raster_in_dem)
        'rAEnv.OutWorkspace = ws

        op.BindRaster(r_k, "K")
        op.BindRaster(r_slp, "dh")
        op.BindRaster(r_n, "n")

        'don't multiply darcy's law by -1 since the
        'calculated slope is positive in the downslope direction.
        Dim errRetry As Boolean = True
RetryPoint:
        Try
            r_out = op.Execute("( [K] * [dh] ) / [n]")
        Catch ex As Exception
            'MsgBox(ex.ToString)
            If errRetry Then
                handleRasterOpError010240(ex.ToString)
                errRetry = False
                GoTo RetryPoint
            Else
                Throw New Exception(ex.ToString)
            End If
        End Try



        'clear out garbage data
        Utilities.replace_DEM_nodata(r_out)

        ComReleaser.ReleaseCOMObject(op)

        Trace.WriteLine("Calculating Darcy flow finished")
        Return r_out

    End Function

    Private Function convertFD(ByRef r As IRaster2) As IRaster2
        Trace.Indent()
        Trace.WriteLine("Converting flow directions...")

        Dim fd As IRaster2
        Dim rProps As IRasterProps = CType(r, IRasterProps)
        Dim ws As IWorkspace = Utilities.OpenScratchWorkspace

        'convert the input to float
        rProps.PixelType = rstPixelType.PT_FLOAT
        fd = CType(CType(r, ISaveAs2).SaveAs("tmp_fd", ws, "MEM"), IRasterDataset2).CreateFullRaster

        'fix the slope, convert to "real" slope (i.e. divide the pct slope by 100) and assign a slope to flat areas
        'replace the old NoData values with the new NoData values
        Dim COM As New ESRI.ArcGIS.ADF.ComReleaser
        Dim pxCursor As IRasterCursor = fd.CreateCursorEx(Nothing)   'for scanning the raster
        Dim pxEdit As IRasterEdit = CType(fd, IRasterEdit)           'for editing pixel blocks        
        Dim pxBlock As IPixelBlock3
        Dim pixels As System.Array
        Dim pixval As Single

        COM.ManageLifetime(pxCursor)

        pxCursor.Reset()
        Do
            pxBlock = pxCursor.PixelBlock

            'get the pixel array of the first raster band
            pixels = CType(pxBlock.PixelData(0), System.Array)
            For i As Integer = 0 To pxBlock.Width - 1
                For j As Integer = 0 To pxBlock.Height - 1
                    pixval = pixels.GetValue(i, j)
                    Select Case pixval
                        Case 64
                            pixels.SetValue(359.999F, i, j)
                        Case 128
                            pixels.SetValue(45.0F, i, j)
                        Case 1
                            pixels.SetValue(90.0F, i, j)
                        Case 2
                            pixels.SetValue(135.0F, i, j)
                        Case 4
                            pixels.SetValue(180.0F, i, j)
                        Case 8
                            pixels.SetValue(225.0F, i, j)
                        Case 16
                            pixels.SetValue(270.0F, i, j)
                        Case 32
                            pixels.SetValue(315.0F, i, j)
                        Case Else
                            pixels.SetValue(0.0F, i, j)
                    End Select

                Next
            Next
            pxBlock.PixelData(0) = pixels
            pxEdit.Write(pxCursor.TopLeft, pxBlock)
        Loop While pxCursor.Next

        Trace.WriteLine("Converting flow directions finished.")
        Trace.Unindent()
        Return fd
    End Function

    'if the given error message string is not of error 10240, then 
    'an exception is thrown containing the original message.
    '
    'This solves a strange problem in my tests (Arc10 SP5) where
    'some raster operations with spatial analyst sometimes end up putting
    'files where its not supposed to, thereby ignoring the raster environment
    'settings specifying the output workspace. In these cases, the file name
    'that it uses may exist from a previous run therefore we must delete
    'the existing GRID rasters.  Luckily, the COM exception thrown
    'includes the name of the offending raster which we can then parse
    'out of the error message.
    Private Sub handleRasterOpError010240(ByVal errmsg As String)
        errmsg = errmsg.Replace(vbCrLf, " ")
        errmsg = errmsg.Replace(vbTab, " ")
        errmsg = errmsg.Replace(vbCr, " ")
        errmsg = errmsg.Replace(vbLf, " ")

        'the full error message of 010240 is
        'System.Runtime.InteropServices.COMException (0x80041098):
        '("esriDataSourcesRaster.GdalDriver") Raster dataset already exists
        'ERROR 010240: Could not save raster dataset to <dataset path> with
        'output format GRID
        If errmsg.Contains("ERROR 010240: Could not save raster dataset to") Then
            Try
                errmsg = errmsg.Split(New String() {"ERROR 010240: Could not save raster dataset to", "with output format GRID"}, StringSplitOptions.None)(1).Trim()
                Trace.WriteLine("Attempting to delete " & errmsg)
                Dim fname As String
                Dim path As String
                If IO.Directory.Exists(errmsg) Then
                    If Not errmsg.EndsWith("\") Then
                        path = errmsg & "\"
                    Else
                        path = errmsg
                    End If
                    IO.Directory.Delete(path, True)
                ElseIf IO.File.Exists(errmsg) Then
                    IO.File.Delete(errmsg)
                End If

                'get the parent folder of the file/folder specified in errmsg
                path = IO.Path.GetDirectoryName(errmsg)
                'get the file name
                fname = IO.Path.GetFileNameWithoutExtension(errmsg)

                'delete associated files
                For Each myFile As String In IO.Directory.GetFiles(path, fname & "*")
                    IO.File.Delete(myFile)
                Next
                Trace.WriteLine("Deleted successfully")
            Catch ex1 As Exception
                Trace.WriteLine("ERROR 010240 occurred but could not delete the exisiting file: " & ex1.ToString & vbCrLf & errmsg.ToString)
            End Try
        Else
            Throw New Exception(errmsg)
        End If
    End Sub

End Class

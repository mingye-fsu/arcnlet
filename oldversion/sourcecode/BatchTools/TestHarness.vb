Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.ADF.connection.local

''' <summary>
''' Used for batch testing / processing of non-ui related stuff (ie. the core modules).  
''' All inputs are hard coded. In order to use this test harness with 
''' different inputs, the code needs to be modified.
''' </summary>
''' <remarks>When using this test harness, it is advisable to use the MyDebug build config
''' instead of the regular Debug</remarks>
Module TestHarness

    Private tr As New TraceOutputConsole
    Private m_AOLicenseInitializer As LicenseInitializer
#If CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10" Or CONFIG = "Release" Or CONFIG = "Arc10.2" Then
    Sub Main()
#Else
    sub RunTestHarness()
#End If

        Try
#If CONFIG = "Arc10" Or CONFIG = "mydebug-Arc10" Or CONFIG = "Release" Or CONFIG = "Arc10.2" Then
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop)
#End If
            'note by Yan: update to Arc10.1
#If CONFIG = "Arc10.1" Then
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop)
#End If
            m_AOLicenseInitializer = New LicenseInitializer()

            'ESRI License Initializer generated code.
            If (Not m_AOLicenseInitializer.InitializeApplication(New esriLicenseProductCode() {esriLicenseProductCode.esriLicenseProductCodeBasic, esriLicenseProductCode.esriLicenseProductCodeStandard, esriLicenseProductCode.esriLicenseProductCodeAdvanced}, _
            New esriLicenseExtensionCode() {esriLicenseExtensionCode.esriLicenseExtensionCode3DAnalyst, esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst})) Then
                Dim msg As String = ""
                msg = msg & m_AOLicenseInitializer.LicenseMessage() & vbCrLf
                msg = msg & "This application could not initialize with the correct ArcGIS license and will shutdown."
                m_AOLicenseInitializer.ShutdownApplication()
                MsgBox(msg, MsgBoxStyle.Critical)
                System.Environment.ExitCode = 8
                Return
            End If
        Catch ex As Exception
            MsgBox("Error initializing license manager" & vbCrLf & ex.ToString, MsgBoxStyle.Critical)
            System.Environment.ExitCode = 8
            Return
        End Try

        'particletrack_tester()
        transport_tester()
        'denitrification_tester()

        Trace.WriteLine("")
        Trace.WriteLine("Done.")
        Console.ReadKey(True)

        'ESRI License Initializer generated code.
        'Do not make any call to ArcObjects after ShutDownApplication()
        m_AOLicenseInitializer.ShutdownApplication()
    End Sub



    Private Sub transport_tester()
        Dim COM As New ComReleaser
        'Dim fc_name As String = "tracks_50x_sub_area2_reduced"
        'Dim fc_name As String = "tracks_HALcbc_sub_area2_new"
        'Dim fc_septic_name As String = "pts_sub_area2"
        'Dim fc_wb_name As String = "waterbodies_hal"
        'Dim fc_name As String = "flowpaths_PotentialSepticTankLocations_clp_UTM"
        'Dim fc_septic_name As String = "PotentialSepticTankLocations_clp_UTM"
        'Dim fc_wb_name As String = "waterbodies"
        'Dim fc_name As String = "path"
        Dim fc_name As String = "paths"
        'Dim fc_name As String = "path_square"
        'Dim fc_name As String = "pathsreach2"
        'Dim fc_name As String = "paths_plumesizeanaly"
        'Dim fc_name As String = "tracks_50x_sub_area2"
        'Dim fc_name As String = "paths50_noditch_noponds_burn30_buffmore_zlmt_subset"
        'Dim fc_name As String = "paths50_noditch_noponds_burn30_buffmore_zlmt"        
        'Dim fc_name As String = "test_paths"
        'Dim fc_name As String = "path_sm60"
        'Dim fc_name As String = "path_subset"
        'Dim fc_name As String = "calbrtd_paths"

        'Dim fc_septic_name As String = "pts_mt3d_ConstConc_40"        
        'Dim fc_septic_name As String = "pts"
        'Dim fc_septic_name As String = "tankreach2"
        'Dim fc_septic_name As String = "sep_subset"
        'Dim fc_septic_name As String = "sep"
        Dim fc_septic_name As String = "PotentialSepticTankLocations"
        'Dim fc_septic_name As String = "septic_tanks"
        'Dim fc_septic_name As String = "septic_tanks_subset"

        Dim fc_wb_name As String = "waterbodies"
        'Dim fc_wb_name As String = "waterbodies_noditch_noponds_mod"        
        'Dim fc_wb_name As String = "waterbodies_noditch_noponds_buffmore_clip"
        'Dim fc_wb_name As String = "waterbodies_square"        

        'Dim fc_path As String = "C:\GIS_tests\naval_station_9_transtest\"
        'Dim fc_path As String = "C:\GIS_tests\naval_station_7\"
        'Dim fc_path As String = "C:\GIS_tests\mt3dms_simple_model\"
        'Dim fc_path As String = "C:\GIS_tests\mt3dms_simple_model2\"
        'Dim fc_path As String = "C:\GIS_tests\mt3dms_simple_model3\"
        'Dim fc_path As String = "C:\GIS_tests\mt3dms_simple_model4\"
        'Dim fc_path As String = "C:\GIS_tests\mt3dms_simple_model\simple_model plume size analysis2\"
        'Dim fc_path As String = "C:\GIS_tests\naval_station_11_mt3dms_refinedgrid"
        'Dim fc_path As String = "C:\GIS_tests\naval_station_12_cutofftest\"
        'Dim fc_path As String = "C:\GIS_tests\lakeshore4_dn_test\"
        'Dim fc_path As String = "C:\GIS_tests\lakeshore_twotanks\"
        Dim fc_path As String = "e:\GIS_tests\lakeshore_example\"
        'Dim fc_path As String = "e:\liyingc\eggleston_heights\"
        'Dim fc_path As String = "e:\GIS\julington creek\"
        'Dim fc_path As String = "e:\GIS\eggleston heights\"

        Dim result As Boolean = True

        'open the tracks shapefile
        Dim wf As IWorkspaceFactory2 = New ShapefileWorkspaceFactory
        Dim fw As IFeatureWorkspace = wf.OpenFromFile(fc_path, Nothing)
        Dim fc As IFeatureClass = fw.OpenFeatureClass(fc_name)
        Trace.WriteLine("Opened feature class: " & fc_path & fc.AliasName)
        Dim fc_septic As IFeatureClass = fw.OpenFeatureClass(fc_septic_name)
        Trace.WriteLine("Opened feature class: " & fc_path & fc_septic.AliasName)
        Dim fc_wb As IFeatureClass = fw.OpenFeatureClass(fc_wb_name)
        Trace.WriteLine("Opened feature class: " & fc_path & fc_wb.AliasName)



        Dim files As New List(Of String)
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_r.img"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_r.rrd"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_r.img.vat.dbf"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_r.img.aux.xml"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_r_warped.img"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_r_warped.rrd"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_ctrlpts.shp"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_ctrlpts.dbf"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_ctrlpts.prj"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "*_ctrlpts.shx"))
        files.AddRange(IO.Directory.GetFiles(fc_path, "00Atest*"))

        For Each file As String In files
            Try
                IO.File.Delete(IO.Path.Combine(fc_path, file))
            Catch ex As Exception
                Debug.WriteLine("Cannot delete files." & ex.Message)
                'Return
            End Try
        Next


        '************
        'important
        If Not AqDn.MathSpecial.initErfcTable() Then Return
        '*************

        COM.ManageLifetime(wf)
        COM.ManageLifetime(fw)
        COM.ManageLifetime(fc)
        COM.ManageLifetime(fc_septic)
        COM.ManageLifetime(fc_wb)

        Dim plumes As AqDn.Transport
        Dim CalculateNO3 As Boolean = True
        Dim sw As New Stopwatch

        'plumes = New AqDn.Transport(ParticleTracks:=fc, Sources:=fc_septic, waterbodies:=fc_wb, _
        '                  ax:=10, ay:=1, az:=0.234, _
        '                  Y:=6, Z:=1.0, _
        '                  MeshCellSize_x:=0.4, MeshCellSize_y:=0.4, MeshCellSize_z:=1.5, _
        '                  InitialConcentration:=-1, _
        '                  ThresholdConcentration:=0.000001, _
        '                  SolutionTime:=-1, _
        '                  DecayRateConstant:=0.012, _
        '                  SolutionType:=AqDn.SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D, _
        '                  WarpMethod:=AqDn.WarpingMethods.WarpingMethod.Spline, _
        '                  WarpCtrlPtSpac:=48, _
        '                  WarpUseApprox:=True, _
        '                  PostProcessing:=AqDn.PostProcessing.PostProcessingAmount.Medium, _
        '                  OutputIntermediateCalcs:=False, _
        '                  OutputIntermediatePlumes:=False, _
        '                  OutputPlumesFile:=IO.Path.Combine(fc_path, "00Atest.img"), _
        '                  VolumeConversionFactor:=1000, _
        '                  DomenicoBoundary:=AqDn.DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Z, _
        '                  PathID:=-1)
        plumes = New AqDn.Transport(ParticleTracks:=fc, Sources:=fc_septic, waterbodies:=fc_wb, _
                  ax:=2.113, ay:=0.234, az:=0.234, _
                  Y:=6, Z:=20000, _
                  MeshCellSize_x:=0.4, MeshCellSize_y:=0.4, MeshCellSize_z:=1.5, _
                  InitialConcentration:=-1, _
                  InitialConcentrationNO3:=40, _
                  CalculatingNO3:=False, _
                  CalculatingNH4:=False, _
                  InitialConcentration_CNH4:=10, _
                  DecayRateConstant_NH4:=0.001, _
                  DecayRateConstant_NO3:=0.008, _
                  ThresholdConcentration:=0.000001, _
                   plume_z_max:=3.0, _
                   plume_z_max_checked:=False, _
                  SolutionTime:=-1, _
                  DecayRateConstant:=0.008, _
                  SolutionType:=AqDn.SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D, _
                  WarpMethod:=AqDn.WarpingMethods.WarpingMethod.Polynomial2, _
                  WarpCtrlPtSpac:=48, _
                  WarpUseApprox:=True, _
                  PostProcessing:=AqDn.PostProcessing.PostProcessingAmount.Medium, _
                  OutputIntermediateCalcs:=False, _
                  OutputIntermediatePlumes:=False, _
                  OutputPlumesFile:=IO.Path.Combine(fc_path, "00Atest.img"), _
                  VolumeConversionFactor:=1000, _
                  DomenicoBoundary:=AqDn.DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate, _
                  PathID:=-1)
        '3361
        '3372


        sw.Start()

        'get the plumes raster
        Dim plumes_r As IRaster2 = plumes.CalculatePlumes
        'Dim plumes_r As IRaster2 = plumes.snipTheTip(Nothing)
        If Not plumes_r Is Nothing Then
            AqDn.Utilities.saveRasterToFile(plumes_r, IO.Path.Combine(fc_path, "00Atest.img"))
            AqDn.Utilities.DeleteRaster(plumes_r)
        End If

        sw.Stop()
        If Not result Then
            Trace.WriteLine("Plume calculation failed.  " & sw.ElapsedMilliseconds / 1000 & " s")
        Else
            Trace.WriteLine("Plume calculation succeeded  " & sw.ElapsedMilliseconds / 1000 & " s")
        End If

    End Sub

    Private Sub denitrification_tester()
        Dim COM As New ComReleaser

        'Dim r_plumes_name As String = "plumes_01_PotentialSepticTankLocations_clp_UTM.img"
        'Dim fc_plumes_info_name As String = "plumes_01_PotentialSepticTankLocations_clp_UTM_info"

        'Dim r_plumes_name As String = "conc_vm_DispDR2_decay8E-3_200yr.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay8E-3_SS_rev97_info"
        'Dim r_plumes_name As String = "conc_vm_DispDR2_decay8E-3_200yr_hires.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay8E-3_SS_medres_info"
        'Dim r_plumes_name As String = "conc_vm_DispDR2_decay1E-5_200yr.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay1E-5_SS_r97_info"
        'Dim r_plumes_name As String = "conc_vm_DispDR_NoDecay_200yr.img"
        'Dim r_plumes_name As String = "conc_vm_DispDR2_decay25E-3_200yr.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay25E-3_SS_r97_info"
        'Dim r_plumes_name As String = "conc_vm_DispDR2_decay1E-5_200yr.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay1E-5_SS_r97_info"
        'Dim r_plumes_name As String = "conc_vm_DispDR2_nodecay_200yr.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_SS_r97_info"

        'Dim r_plumes_name As String = "conc_dr2D_SS_r97.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_SS_r97_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay0_5_SS.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay0_5_SS_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay1E-5_SS_r97.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay1E-5_SS_r97_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay8E-3_SS_r97.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay8E-3_SS_r97_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay8E-3_SS_lores_r97.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay8E-3_SS_lores_r97_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay8E-3_SS_medres.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay8E-3_SS_medres_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay25E-3_SS_r97.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay25E-3_SS_r97_info"
        'Dim r_plumes_name As String = "conc_dr2D_decay25E-3_SS.img"
        'Dim fc_plumes_info_name As String = "conc_dr2D_decay25E-3_SS_info"

        Dim r_plumes_name As String = "00ATest.img"
        Dim fc_plumes_info_name As String = "00Atest_info"
        'Dim r_plumes_name As String = "1test.img"
        'Dim fc_plumes_info_name As String = "1test_info"
        Dim fc_wb_name As String = "waterbodies"

        'Dim input_path As String = "C:\GIS_tests\lakeshore4_dn_test\"
        'Dim input_path As String = "C:\GIS_tests\mt3dms_simple_model3\"
        'Dim input_path As String = "C:\GIS_tests\mt3dms_simple_model4\"
        Dim input_path As String = "C:\GIS_tests\lakeshore_twotanks\"

        'open the files
        Dim wf As IWorkspaceFactory2 = New ShapefileWorkspaceFactory
        Dim fw As IFeatureWorkspace = wf.OpenFromFile(input_path, Nothing)

        Dim fc_plumes_info As IFeatureClass = fw.OpenFeatureClass(fc_plumes_info_name)
        Trace.WriteLine("Opened feature class: " & input_path & fc_plumes_info.AliasName)
        Dim fc_wb As IFeatureClass = fw.OpenFeatureClass(fc_wb_name)
        Trace.WriteLine("Opened feature class: " & input_path & fc_wb.AliasName)

        Dim rwf As IWorkspaceFactory2 = New RasterWorkspaceFactory
        Dim rw As IRasterWorkspace2 = rwf.OpenFromFile(input_path, Nothing)
        Dim r_plumes_ds As IRasterDataset2 = rw.OpenRasterDataset(r_plumes_name)
        Dim r_plumes As IRaster2 = r_plumes_ds.CreateFullRaster
        Trace.WriteLine("Opened " & r_plumes_ds.Format & " raster dataset " & r_plumes_ds.CompleteName)
        'Dim rwf As IWorkspaceFactory2 = New RasterWorkspaceFactory
        'Dim rw As IRasterWorkspace2 = rwf.OpenFromFile("C:\Documents and Settings\riosjfern\Local Settings\Temp\", Nothing)
        'Dim r_plumes_ds As IRasterDataset2 = rw.OpenRasterDataset("calc227")
        'Dim r_plumes As IRaster2 = r_plumes_ds.CreateFullRaster
        'Trace.WriteLine("Opened " & r_plumes_ds.Format & " raster dataset " & r_plumes_ds.CompleteName)


        COM.ManageLifetime(wf)
        COM.ManageLifetime(fw)
        COM.ManageLifetime(fc_plumes_info)
        COM.ManageLifetime(fc_wb)

        COM.ManageLifetime(rwf)
        COM.ManageLifetime(rw)
        COM.ManageLifetime(r_plumes_ds)
        COM.ManageLifetime(r_plumes)

        Dim dn As AqDn.Denitrification
        Dim sw As New Stopwatch

        dn = New AqDn.Denitrification(PlumesInfo:=fc_plumes_info, RiskFactor:=1, _
                                      OutputIntermediateCalcs:=False)

        sw.Start()


        Dim files As New List(Of String)
        files.AddRange(IO.Directory.GetFiles(input_path, "*_r.img"))
        files.AddRange(IO.Directory.GetFiles(input_path, "*_r.rrd"))
        files.AddRange(IO.Directory.GetFiles(input_path, "*_r.img.vat.dbf"))

        For Each file As String In files
            Try
                'IO.File.Delete(IO.Path.Combine(input_path, file))
            Catch ex As Exception
                Debug.WriteLine("Cannot delete files." & ex.Message)
            End Try
        Next


        'get the result
        Dim result As Boolean

        'Trace.WriteLine(dn.getRasterSum(r_plumes))
        'Return
        result = dn.CalculateLoad

        If Not result Then
            Trace.WriteLine("calculation failed")
        Else
            'get the results
            Dim N0, Ndn, Nload As Single
            Dim wbId As Integer
            N0 = dn.OutParams("N0")
            Ndn = dn.OutParams("Ndn")
            Nload = dn.OutParams("Nload")
            wbId = dn.OutParams("WbID")
            Dim s As New String(vbTab, Trace.IndentLevel)
            Trace.WriteLine(vbCrLf & s & _
                            "N0" & vbTab & "=" & N0 & vbCrLf & s & _
                            "Ndn" & vbTab & "=" & Ndn & vbCrLf & s & _
                            "Nload" & vbTab & "=" & Nload & vbCrLf & s & _
                            "WbID" & vbTab & "=" & wbId)

        End If

        sw.Stop()
        Trace.WriteLine("Plume calculation done  " & sw.ElapsedMilliseconds & "ms")

    End Sub



    Private Sub particletrack_tester()
        Dim COM As New ComReleaser

        'Dim path As String = "E:\liyingc\eggleston_heights"         'workspace paths
        'Dim path2 As String = "E:\liyingc\eggleston_heights"
        Dim path As String = "E:\GIS_tests\lakeshore_example"
        Dim path2 As String = "E:\GIS_tests\lakeshore_example"
        Dim output_path As String = path2
        Dim output_shp_name As String = "test_paths"
        'Dim r_mag_name As String = "a.img"
        'Dim r_dir_name As String = "b.img"
        Dim r_mag_name As String = "mag50.img"
        Dim r_dir_name As String = "dir50.img"
        Dim r_por_name As String = "porosity.img"
        'Dim r_wb_name As String = "waterbodies_noditch_noponds_buffmore_clip.img"
        Dim r_wb_name As String = "waterbodies.img"
        'Dim starting_pts_name As String = "sep_subset"
        Dim starting_pts_name As String = "PotentialSepticTankLocations"

        Dim r_mag As IRaster2                                       'the rasters
        Dim r_dir As IRaster2
        Dim r_wb As IRaster2
        Dim r_por As IRaster2

        Dim wf As RasterWorkspaceFactory                            'variables for opening the rasters
        Dim ws As IRasterWorkspace2
        Dim rds As IRasterDataset2
        Dim rbc As IRasterBandCollection
        Dim rpr As IRasterProps

        Dim swf As IWorkspaceFactory2                               'variables for opening the shapefiles
        Dim fw As IFeatureWorkspace

        Dim fc As IFeatureClass                                     'the shapefiles

        Dim output_spatialref As ISpatialReference2                 'output spatial reference

        Try
            'open the workspace
            wf = New RasterWorkspaceFactory
            ws = wf.OpenFromFile(path, Nothing)
            COM.ManageLifetime(wf)
            COM.ManageLifetime(ws)

            '************
            'load up the rasters
            '********************
            'open magnitude raster
            Trace.WriteLine("Opening magnitude raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path, r_mag_name))

            rds = ws.OpenRasterDataset(r_mag_name)
            rbc = CType(rds, IRasterBandCollection)
            r_mag = rds.CreateFullRaster
            rpr = CType(r_mag, IRasterProps)
            COM.ManageLifetime(rds)
            COM.ManageLifetime(r_mag)

            Debug.WriteLine("Spatial Ref: " & rpr.SpatialReference.Name & vbTab & "NoData: " & rpr.NoDataValue(0))
            Debug.WriteLine("Mean Cell Size X: " & rpr.MeanCellSize.X & vbTab & " Mean Cell Size Y: " & rpr.MeanCellSize.Y)
            Debug.WriteLine("Num Rows: " & rpr.Width & vbTab & vbTab & "Num Columns: " & rpr.Height)
            Debug.WriteLine("Max: " & rbc.Item(0).Statistics.Maximum & vbTab & " Min: " & rbc.Item(0).Statistics.Minimum)
            Debug.WriteLine("Avg: " & rbc.Item(0).Statistics.Mean & vbTab & " Std: " & rbc.Item(0).Statistics.StandardDeviation)

            Trace.Unindent()

            'open direction raster
            Trace.WriteLine("Opening direction raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path, r_dir_name))

            rds = ws.OpenRasterDataset(r_dir_name)
            rbc = CType(rds, IRasterBandCollection)
            r_dir = rds.CreateFullRaster
            rpr = CType(r_dir, IRasterProps)
            COM.ManageLifetime(rds)
            COM.ManageLifetime(r_dir)

            Debug.WriteLine("Spatial Ref: " & rpr.SpatialReference.Name & vbTab & "NoData: " & rpr.NoDataValue(0))
            Debug.WriteLine("Mean Cell Size X: " & rpr.MeanCellSize.X & vbTab & " Mean Cell Size Y: " & rpr.MeanCellSize.Y)
            Debug.WriteLine("Num Rows: " & rpr.Width & vbTab & vbTab & "Num Columns: " & rpr.Height)
            Debug.WriteLine("Max: " & rbc.Item(0).Statistics.Maximum & vbTab & " Min: " & rbc.Item(0).Statistics.Minimum)
            Debug.WriteLine("Avg: " & rbc.Item(0).Statistics.Mean & vbTab & " Std: " & rbc.Item(0).Statistics.StandardDeviation)

            Trace.Unindent()

            'open water bodies raster
            Trace.WriteLine("Opening water bodies raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path, r_wb_name))

            rds = ws.OpenRasterDataset(r_wb_name)
            rbc = CType(rds, IRasterBandCollection)
            r_wb = rds.CreateFullRaster
            rpr = CType(r_wb, IRasterProps)
            COM.ManageLifetime(rds)
            COM.ManageLifetime(r_wb)

            Debug.WriteLine("Spatial Ref: " & rpr.SpatialReference.Name & vbTab & "NoData: " & rpr.NoDataValue(0))
            Debug.WriteLine("Mean Cell Size X: " & rpr.MeanCellSize.X & vbTab & " Mean Cell Size Y: " & rpr.MeanCellSize.Y)
            Debug.WriteLine("Num Rows: " & rpr.Width & vbTab & vbTab & "Num Columns: " & rpr.Height)
            Debug.WriteLine("Max: " & rbc.Item(0).Statistics.Maximum & vbTab & " Min: " & rbc.Item(0).Statistics.Minimum)
            Debug.WriteLine("Avg: " & rbc.Item(0).Statistics.Mean & vbTab & " Std: " & rbc.Item(0).Statistics.StandardDeviation)

            Trace.Unindent()

            'open porosity raster
            Trace.WriteLine("Opening porosity raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path, r_por_name))

            rds = ws.OpenRasterDataset(r_por_name)
            rbc = CType(rds, IRasterBandCollection)
            r_por = rds.CreateFullRaster
            rpr = CType(r_por, IRasterProps)
            COM.ManageLifetime(rds)
            COM.ManageLifetime(r_por)

            Debug.WriteLine("Spatial Ref: " & rpr.SpatialReference.Name & vbTab & "NoData: " & rpr.NoDataValue(0))
            Debug.WriteLine("Mean Cell Size X: " & rpr.MeanCellSize.X & vbTab & " Mean Cell Size Y: " & rpr.MeanCellSize.Y)
            Debug.WriteLine("Num Rows: " & rpr.Width & vbTab & vbTab & "Num Columns: " & rpr.Height)
            Debug.WriteLine("Max: " & rbc.Item(0).Statistics.Maximum & vbTab & " Min: " & rbc.Item(0).Statistics.Minimum)
            Debug.WriteLine("Avg: " & rbc.Item(0).Statistics.Mean & vbTab & " Std: " & rbc.Item(0).Statistics.StandardDeviation)

            Trace.Unindent()

            '*******************
            'load up the starting locations file
            '*****************************
            Trace.WriteLine("Opening starting points feature class")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path, starting_pts_name))

            swf = New ShapefileWorkspaceFactory
            fw = swf.OpenFromFile(path2, Nothing)
            fc = fw.OpenFeatureClass(starting_pts_name)
            COM.ManageLifetime(swf)
            COM.ManageLifetime(fw)
            COM.ManageLifetime(fc)

            output_spatialref = CType(fc, IGeoDataset).SpatialReference

            Debug.WriteLine("Spatial reference: " & vbTab & output_spatialref.Name)
            Debug.WriteLine("Feature type: " & vbTab & [Enum].GetName(GetType(esriFeatureType), fc.FeatureType))
            Debug.WriteLine("Shape type: " & vbTab & [Enum].GetName(GetType(esriGeometryType), fc.ShapeType))
            Debug.WriteLine("Feature Count: " & vbTab & fc.FeatureCount(Nothing))


            Trace.Unindent()

            Dim files As New List(Of String)
            files.AddRange(IO.Directory.GetFiles(output_path, "00Atest*"))

            For Each file As String In files
                Try
                    IO.File.Delete(IO.Path.Combine(output_path, file))
                Catch ex As Exception
                    Debug.WriteLine("Cannot delete files." & ex.Message)
                    'Return
                End Try
            Next

            '****************************
            'particle track
            '****************************
            Trace.WriteLine("Particle tracking...")
            Trace.Indent()
            Dim ptrack As New AqDn.ParticleTracker(r_mag, r_dir, r_wb, r_por, _
                                                   output_shp_name, output_path, _
                                                   output_spatialref, _
                                                   fc, _
                                                   5, 800)
            If ptrack.track Then
                Trace.WriteLine("Particle tracking done")
            Else
                Trace.WriteLine("There was an error tracking the particles")
            End If

            Trace.Unindent()
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Uncertainty Analysis Project
    ''' generate a few flow fields using a different smoothing parameter for each one    
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub UA_project_generateFlowFields()
        Dim COM As New ComReleaser

        'workspace paths
        Dim path_hydr_por As String = "C:\GIS_tests\naval_station4_davisdata"
        Dim path_dem As String = "C:\GIS_tests\naval_station1\NEDnaval_air_station"
        Dim path_wb As String = "C:\GIS_tests\naval_station_7"
        Dim output_path As String = "C:\GIS_tests\naval_station_8_UA_finalproj"

        'input file names
        Dim r_hydcond_name As String = "hydr.img"
        Dim r_porosity_name As String = "porosity.img"
        Dim r_dem_name As String = "navalbase_utm.img"
        Dim fc_wb_name As String = "waterbodies_hal"

        'output file names. note the direction and magnitude raster names are just prefixes
        'since this function generates multiple direction and magnitude rasters.
        Dim output_r_dir_name_prefix As String = "dir"
        Dim output_r_mag_name_prefix As String = "mag"

        'the rasters and feature classees
        Dim r_hydr As IRaster2
        Dim r_poro As IRaster2
        Dim r_dem As IRaster2
        Dim fc_wb As IFeatureClass

        'variables for opening the rasters
        Dim wf As RasterWorkspaceFactory
        Dim ws As IRasterWorkspace2
        Dim rds As IRasterDataset2


        'variables for opening the shapefiles
        Dim swf As IWorkspaceFactory2
        Dim fw As IFeatureWorkspace

        Dim sw As New Stopwatch

        Try
            'open the workspace
            wf = New RasterWorkspaceFactory
            COM.ManageLifetime(wf)

            '************
            'load up the rasters
            '********************
            'open dem
            Trace.WriteLine("Opening magnitude raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path_dem, r_dem_name))
            ws = wf.OpenFromFile(path_dem, Nothing)
            COM.ManageLifetime(ws)

            rds = ws.OpenRasterDataset(r_dem_name)
            r_dem = rds.CreateFullRaster
            COM.ManageLifetime(r_dem)
            COM.ManageLifetime(rds)

            Utilities.debug_OutputRasterProps(r_dem)

            Trace.Unindent()

            'open hydraulic conductivity raster
            Trace.WriteLine("Opening hydraulic conductivity raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path_hydr_por, r_hydcond_name))
            ws = wf.OpenFromFile(path_hydr_por, Nothing)
            COM.ManageLifetime(ws)

            rds = ws.OpenRasterDataset(r_hydcond_name)
            r_hydr = rds.CreateFullRaster
            COM.ManageLifetime(r_hydr)
            COM.ManageLifetime(rds)

            Utilities.debug_OutputRasterProps(r_hydr)

            Trace.Unindent()

            'open porosity conductivity raster
            Trace.WriteLine("Opening porosity conductivity raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path_hydr_por, r_porosity_name))
            ws = wf.OpenFromFile(path_hydr_por, Nothing)
            COM.ManageLifetime(ws)

            rds = ws.OpenRasterDataset(r_porosity_name)
            r_poro = rds.CreateFullRaster
            COM.ManageLifetime(r_poro)

            Utilities.debug_OutputRasterProps(r_poro)

            Trace.Unindent()

            'open water bodies shapefile
            Trace.WriteLine("Opening water bodies feature class")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path_wb, fc_wb_name))
            swf = New ShapefileWorkspaceFactory
            fw = swf.OpenFromFile(path_wb, Nothing)
            fc_wb = fw.OpenFeatureClass(fc_wb_name)
            COM.ManageLifetime(swf)
            COM.ManageLifetime(fw)
            COM.ManageLifetime(fc_wb)

            Utilities.debug_OutputFeatureClassProps(fc_wb)

            Trace.Unindent()

            Trace.WriteLine("Press Any Key to Continue...")
            Console.ReadKey(True)

            ''****************************
            ''generate seepage velocity maps
            ''****************************
            Trace.WriteLine("Generating flow maps...")
            Trace.Indent()

            Dim flowcalc As AqDn.DarcyFlow
            Dim results(1) As IRasterDataset

            Dim smoothing_factors() As Integer = {2, 5, 10, 20, 40, 50, 60, 80}

            Dim totaltime As Long = 0

            For i As Integer = 4 To smoothing_factors.Length - 1
                sw.Start()
                Trace.WriteLine("Processing index " & i & "(smoothing factor " & smoothing_factors(i) & ")")
                flowcalc = New AqDn.DarcyFlow(r_dem, _
                                            fc_wb, _
                                            r_hydr, _
                                            r_poro, _
                                            1, _
                                            smoothing_factors(i), _
                                            IO.Path.Combine(output_path, "mag_" & smoothing_factors(i).ToString("00") & "x.img"), _
                                            IO.Path.Combine(output_path, "dir_" & smoothing_factors(i).ToString("00") & "x.img"), _
                                            "", _
                                            True)
                results = flowcalc.calculateDarcyFlow
                sw.Stop()
                totaltime = totaltime + sw.ElapsedMilliseconds / 1000
                Debug.WriteLine("Elapsed: " & (sw.ElapsedMilliseconds / 1000).ToString("#.0") & " s")

                If results Is Nothing Then
                    Throw New Exception("Darcy flow failed. " & i.ToString("00"))
                End If
            Next
            Debug.WriteLine("Total time: " & totaltime.ToString("#.0") & " s")

            Trace.Unindent()
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' particle track using the flow fields generated by UA_project_generateFlowFields
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub UA_project_particleTrack()
        Dim COM As New ComReleaser

        Dim path_dir_mag As String = "C:\GIS_tests\naval_station_8_UA_finalproj"         'workspace paths
        Dim path_pts As String = "C:\GIS_tests\naval_station5_dinf"
        Dim path_wb As String = "C:\GIS_tests\naval_station_7\Hal_darcyflow"
        Dim output_path As String = path_dir_mag
        Dim output_shp_name_prefix As String = "tracks_all_"
        Dim r_dir_name_prefix As String = "dir_"
        Dim r_mag_name_prefix As String = "mag_"
        Dim starting_pts_name As String = "pts"
        Dim waterbodies_name As String = "waterbodies_hal.img"

        Dim r_mag As IRaster2                                       'the rasters
        Dim r_dir As IRaster2
        Dim r_wb As IRaster2

        Dim wf As RasterWorkspaceFactory                            'variables for opening the rasters
        Dim ws As IRasterWorkspace2
        Dim rds As IRasterDataset2
        Dim rbc As IRasterBandCollection
        Dim rpr As IRasterProps

        Dim swf As IWorkspaceFactory2                               'variables for opening the shapefiles
        Dim fw As IFeatureWorkspace

        Dim fc As IFeatureClass                                     'the shapefiles

        Try
            'open the workspace
            wf = New RasterWorkspaceFactory
            ws = wf.OpenFromFile(path_wb, Nothing)
            COM.ManageLifetime(wf)
            COM.ManageLifetime(ws)


            'open water bodies raster
            Trace.WriteLine("Opening water bodies raster")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path_wb, waterbodies_name))

            rds = ws.OpenRasterDataset(waterbodies_name)
            rbc = CType(rds, IRasterBandCollection)
            r_wb = rds.CreateFullRaster
            rpr = CType(r_wb, IRasterProps)
            COM.ManageLifetime(rds)
            COM.ManageLifetime(r_wb)
            Utilities.debug_OutputRasterProps(r_wb)
            Trace.Unindent()

            'load up the starting locations file
            Trace.WriteLine("Opening starting points feature class")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(path_pts, starting_pts_name))

            swf = New ShapefileWorkspaceFactory
            fw = swf.OpenFromFile(path_pts, Nothing)
            fc = fw.OpenFeatureClass(starting_pts_name)
            COM.ManageLifetime(swf)
            COM.ManageLifetime(fw)
            COM.ManageLifetime(fc)
            Utilities.debug_OutputFeatureClassProps(fc)
            Trace.Unindent()

            'open the workspace from which the flow mag and dir will be loaded
            wf = New RasterWorkspaceFactory
            ws = wf.OpenFromFile(path_dir_mag, Nothing)
            COM.ManageLifetime(wf)
            COM.ManageLifetime(ws)



            Dim smoothing_factors() As Integer = {2, 5, 10, 20, 40, 50, 60, 80}
            Trace.WriteLine("Processing...")
            Trace.Indent()
            For Each fac As Integer In smoothing_factors
                Trace.WriteLine("Factor: " & fac)

                '************
                'load up the raster
                '********************
                'open magnitude raster
                Trace.WriteLine("Opening magnitude raster")
                Trace.Indent()
                Trace.WriteLine(IO.Path.Combine(path_dir_mag, r_mag_name_prefix & fac.ToString("00") & "x.img"))

                rds = ws.OpenRasterDataset(r_mag_name_prefix & fac.ToString("00") & "x.img")
                rbc = CType(rds, IRasterBandCollection)
                r_mag = rds.CreateFullRaster
                rpr = CType(r_mag, IRasterProps)
                COM.ManageLifetime(rds)
                COM.ManageLifetime(r_mag)
                Utilities.debug_OutputRasterProps(r_mag)
                Trace.Unindent()

                'open direction raster
                Trace.WriteLine("Opening direction raster")
                Trace.Indent()
                Trace.WriteLine(IO.Path.Combine(path_dir_mag, r_dir_name_prefix & fac.ToString("00") & "x.img"))

                rds = ws.OpenRasterDataset(r_dir_name_prefix & fac.ToString("00") & "x.img")
                rbc = CType(rds, IRasterBandCollection)
                r_dir = rds.CreateFullRaster
                rpr = CType(r_mag, IRasterProps)
                COM.ManageLifetime(rds)
                COM.ManageLifetime(r_mag)
                Utilities.debug_OutputRasterProps(r_dir)
                Trace.Unindent()

                '****************************
                'particle track
                '****************************
                Trace.WriteLine("Particle tracking...")
                Trace.Indent()
                'Dim ptrack As New AqDn.ParticleTracker(r_mag, r_dir, r_wb, _
                '                                       output_shp_name_prefix & fac.ToString("00") & "x", output_path, _
                '                                       rpr.SpatialReference, _
                '                                       fc, _
                '                                       5, 800)
                'If ptrack.track Then
                '    Trace.WriteLine("Particle tracking done")
                'Else
                '    Trace.WriteLine("There was an error tracking the particles")
                'End If

                Trace.Unindent()
            Next
            Trace.Unindent()
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message)
            Trace.Unindent()
        End Try

    End Sub

    Private Sub UA_finalproj_TravelTimeDifference()
        Dim COM As New ComReleaser

        Dim input_tracks_hal_path As String = "C:\GIS_tests\naval_station_7"
        'Dim input_tracks_hal_shp_name As String = "tracks_HAL_sub_area2"
        Dim input_tracks_hal_shp_name As String = "tracks_HAL_cbc"

        Dim input_tracks_path As String = "C:\GIS_tests\naval_station_8_UA_finalproj"
        Dim input_tracks_prefix As String = "tracks_all_"

        Dim output_filename_prefix As String = "traveltime_difference_all_"
        Dim output_path As String = "C:\GIS_tests\naval_station_8_UA_finalproj"

        Dim swf As IWorkspaceFactory2                               'variables for opening the shapefiles
        Dim fw As IFeatureWorkspace
        Dim fw_hal As IFeatureWorkspace

        Dim fc_hal As IFeatureClass                                     'the shapefiles
        Dim fc As IFeatureClass

        Dim smoothing_factors() As Integer = {2, 5, 10, 20, 40, 50, 60, 80}

        'Try

        'load up the feature class to compare to            
        Trace.WriteLine("Opening feature class...")
        Trace.Indent()
        Trace.WriteLine(IO.Path.Combine(input_tracks_hal_path, input_tracks_hal_shp_name))

        swf = New ShapefileWorkspaceFactory
        fw_hal = swf.OpenFromFile(input_tracks_hal_path, Nothing)
        fc_hal = fw_hal.OpenFeatureClass(input_tracks_hal_shp_name)
        COM.ManageLifetime(swf)
        COM.ManageLifetime(fw_hal)
        COM.ManageLifetime(fc_hal)
        Utilities.debug_OutputFeatureClassProps(fc_hal)
        Trace.Unindent()

        'open up a feature cursor to select all records
        Dim fcur As IFeatureCursor = fc_hal.Search(Nothing, False)

        'get unique pathid values
        Dim stats As IDataStatistics = New DataStatistics
        Dim unique_PathIDs As IEnumerator
        stats.Field = "PathID"
        stats.Cursor = fcur
        unique_PathIDs = stats.UniqueValues

        Dim u As New Collection
        While unique_PathIDs.MoveNext
            u.Add(unique_PathIDs.Current)
        End While
        unique_PathIDs = u.GetEnumerator

        Dim pathID As Integer

        Dim q As IQueryFilter2 = New QueryFilter

        Dim next_feature As IFeature
        Dim fid As Integer
        Dim totDist As Double
        Dim totTime As Double
        Dim wbID As String = ""
        Dim fid_hal As Integer
        Dim totDist_hal As Double
        Dim totTime_hal As Double
        Dim wbID_hal As String = ""

        Dim ignored_noEndingWbID As Integer = 0
        Dim ignored_noMatchingWbID As Integer = 0

        For Each fac As Integer In smoothing_factors
            'open up the feature class to test
            Trace.WriteLine("Fac: & " & fac)
            Trace.Indent()

            Trace.WriteLine("Opening feature class...")
            Trace.Indent()
            Trace.WriteLine(IO.Path.Combine(input_tracks_path, input_tracks_prefix & fac.ToString("00") & "x"))
            fw = swf.OpenFromFile(input_tracks_path, Nothing)
            fc = fw.OpenFeatureClass(input_tracks_prefix & fac.ToString("00") & "x")
            Utilities.debug_OutputFeatureClassProps(fc)
            Trace.Unindent()

            unique_PathIDs.Reset()

            Dim fout As New IO.StreamWriter(IO.Path.Combine(output_path, output_filename_prefix & fac.ToString("00") & "x.csv"))
            Dim fout_ignored As New IO.StreamWriter(IO.Path.Combine(output_path, output_filename_prefix & fac.ToString("00") & "x_ignored.csv"))
            fout.WriteLine("PathID,WBId_hal,WBId,FID_hal,FID,TotDist_hal,TotDist,TotTime_hal,TotTime")
            fout_ignored.WriteLine("PathID,WBId_hal,WBId,FID_hal,FID,TotDist_hal,TotDist,TotTime_hal,TotTime")

            'for each pathID, search for that patID in both tracks files.  also further limit the
            'result to only the final segment of each path (th
            While unique_PathIDs.MoveNext
                pathID = unique_PathIDs.Current
                Trace.WriteLine("PathID" & pathID)

                q.WhereClause = """PathID""=" & pathID

                'find the final segment in the first feature class
                fcur = fc_hal.Search(q, False)
                next_feature = fcur.NextFeature
                While Not next_feature Is Nothing
                    fid_hal = next_feature.OID
                    totDist_hal = Convert.ToDouble(next_feature.Value(fcur.FindField("TotDist")))
                    totTime_hal = Convert.ToDouble(next_feature.Value(fcur.FindField("TotTime")))
                    wbID_hal = Convert.ToString(next_feature.Value(fcur.FindField("WBId")))
                    next_feature = fcur.NextFeature
                End While


                'find the final segment in the second fc
                fcur = fc.Search(q, False)
                next_feature = fcur.NextFeature
                While Not next_feature Is Nothing
                    fid = next_feature.OID
                    totDist = next_feature.Value(fcur.FindField("TotDist"))
                    totTime = next_feature.Value(fcur.FindField("TotTime"))
                    wbID = next_feature.Value(fcur.FindField("WBId"))
                    next_feature = fcur.NextFeature
                End While

                If wbID <> wbID_hal Then
                    ignored_noMatchingWbID = ignored_noMatchingWbID + 1
                    Trace.WriteLine("ignored pathID: " & pathID & " Wbid: " & wbID & " WBId_hal: " & wbID_hal)
                    fout_ignored.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", pathID, wbID_hal, wbID, fid_hal, fid, totDist_hal, totDist, totTime_hal, totTime))
                Else
                    If wbID <> "" And wbID_hal <> "" Then
                        fout.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", pathID, wbID_hal, wbID, fid_hal, fid, totDist_hal, totDist, totTime_hal, totTime))
                    Else
                        fout_ignored.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", pathID, wbID_hal, wbID, fid_hal, fid, totDist_hal, totDist, totTime_hal, totTime))
                        Trace.WriteLine("ignored pathID: " & pathID & " Wbid: " & wbID & " WBId_hal: " & wbID_hal)
                    End If

                End If



            End While
            ComReleaser.ReleaseCOMObject(fw)
            ComReleaser.ReleaseCOMObject(fc)

            fout.Close()
            fout_ignored.Close()
            Trace.Unindent()
        Next
        'Catch ex As Exception
        '    Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message)
        '    Trace.Unindent()
        'End Try
    End Sub

End Module

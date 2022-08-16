Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry
Imports AqDn
Imports ESRI.ArcGIS.version


''' <summary>
''' Enables command line usage of ArcNLET
''' </summary>
''' <remarks></remarks>
Module ArcNLETc

    Private tr As New TraceOutputConsole
    Private m_AOLicenseInitializer As LicenseInitializer

    Private testing As Boolean
    'note by Yan: Update to ArcGIS10.1
#If CONFIG = "Arc9" Or CONFIG = "Arc10" Or CONFIG = "Arc10.1" Or CONFIG = "Arc10.2" Then
    Sub Main()
#Else
    Sub ArcNLETc()
#End If


#If CONFIG = "mydebugC-Arc9" Or CONFIG = "mydebugC-Arc10" Then
        testing=True
#Else
        testing = False
#End If

        tr = New TraceOutputConsole

        Try
#If CONFIG = "Arc10" Or CONFIG = "mydebugC-Arc10" Or CONFIG = "Arc10.2" Then
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop)
#End If
            'note by Yan: update to Arc10.1
#If CONFIG = "Arc10.1" Then
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop)
#End If
            m_AOLicenseInitializer = New LicenseInitializer()

            'ESRI License Initializer generated code.
            If (Not m_AOLicenseInitializer.InitializeApplication(New esriLicenseProductCode() {esriLicenseProductCode.esriLicenseProductCodeBasic, esriLicenseProductCode.esriLicenseProductCodeStandard, esriLicenseProductCode.esriLicenseProductCodeAdvanced}, _
            New esriLicenseExtensionCode() {esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst})) Then
                Dim msg As String = ""
                msg = msg & m_AOLicenseInitializer.LicenseMessage() & vbCrLf
                msg = msg & "This application could not initialize with the correct ArcGIS license and will shutdown."
                m_AOLicenseInitializer.ShutdownApplication()
                Trace.WriteLine(msg)
                System.Environment.ExitCode = 8
                Return
            End If
        Catch ex As Exception
            Trace.WriteLine("Error initializing license manager" & vbCrLf & ex.ToString)
            System.Environment.ExitCode = 8
            Return
        End Try

        If testing Then AqDn.Utilities.DeleteFilesAndFoldersQuick("c:\temp")

        Dim args() As String = System.Environment.GetCommandLineArgs

        If testing Then
            args = getArgs("flow")
            'args = getArgs("particleTrack")
            'args = getArgs("transport")
            'args = getArgs("loadEstimation")
        End If

        If args.Length < 2 Then
            System.Environment.ExitCode = 10
            Trace.WriteLine("No command specified")
            Return
        End If

        Dim operation As String = args(1)
        Dim sw As New Stopwatch

        sw.Start()
        Trace.WriteLine("Start")
        Select Case operation
            Case "flow"
                runFlow(args)
            Case "particleTrack"
                runParticleTracking(args)
            Case "transport"
                runTransport(args)
            Case "loadEstimation"
                runLoadEstimation(args)
            Case Else
                Console.WriteLine("No module selected")
        End Select

        m_AOLicenseInitializer.ShutdownApplication()
        sw.Stop()
        Trace.WriteLine("done. Elapsed time: " & sw.ElapsedMilliseconds / 1000 & " sec")
        If testing Then Console.ReadKey(True)
    End Sub

    Sub runFlow(ByVal args() As String)
        Dim flow As DarcyFlow

        Dim in_fullpath_dem As String = ""
        Dim in_fullpath_wb As String = ""
        Dim in_fullpath_porosity As String = ""
        Dim in_fullpath_hydrcond As String = ""
        Dim in_zFactor As String = ""
        Dim in_smoothing As String = ""
        Dim in_fillSinks As String = ""
        Dim in_outputIntermediate As String = ""
        Dim out_fullpath_mag As String = ""
        Dim out_fullpath_dir As String = ""
        Dim out_fullpath_hydrGrad As String = ""
        Dim out_fullpath_smthDEM As String = ""
        Dim out_fullpath_XYSmthDEM As String = ""
        Dim out_XY As String = ""

        Dim argName As String = ""
        Dim argVal As String = ""
        Dim argIdxDelim As Integer = 0

        Try

            'get parameter values
            Dim msgParam As String = ""
            For Each arg As String In args
                argIdxDelim = arg.IndexOf("=")
                If argIdxDelim >= 0 Then
                    argName = arg.Substring(0, argIdxDelim)
                    argVal = arg.Substring(argIdxDelim + 1, arg.Length - argIdxDelim - 1)
                    Select Case argName
                        Case "inDEM"
                            msgParam = msgParam & "inDem=" & argVal & vbCrLf
                            in_fullpath_dem = argVal
                        Case "inWB"
                            msgParam = msgParam & "inWB=" & argVal & vbCrLf
                            in_fullpath_wb = argVal
                        Case "inPoro"
                            msgParam = msgParam & "inPoro=" & argVal & vbCrLf
                            in_fullpath_porosity = argVal
                        Case "inHCond"
                            msgParam = msgParam & "inHCond=" & argVal & vbCrLf
                            in_fullpath_hydrcond = argVal
                        Case "inZFac"
                            msgParam = msgParam & "inZFac=" & argVal & vbCrLf
                            in_zFactor = argVal
                        Case "inSmth"
                            msgParam = msgParam & "inSmth=" & argVal & vbCrLf
                            in_smoothing = argVal
                        Case "inFillSnk"
                            msgParam = msgParam & "inFillSnk=" & argVal & vbCrLf
                            in_fillSinks = argVal
                        Case "inSaveTMP"
                            msgParam = msgParam & "inSaveTMP=" & argVal & vbCrLf
                            in_outputIntermediate = argVal
                        Case "outMag"
                            msgParam = msgParam & "outMag=" & argVal & vbCrLf
                            out_fullpath_mag = argVal
                        Case "outDir"
                            msgParam = msgParam & "outDir=" & argVal & vbCrLf
                            out_fullpath_dir = argVal
                        Case "outHydrGr"
                            msgParam = msgParam & "outHydrGr=" & argVal & vbCrLf
                            If argVal = "" Then argVal = "."
                            out_fullpath_hydrGrad = argVal
                        Case "outSmthDEM"
                            msgParam = msgParam & "outSmthDEM=" & argVal & vbCrLf
                            If argVal = "" Then argVal = "."
                            out_fullpath_smthDEM = argVal
                        Case "outXYSmthDEM"
                            msgParam = msgParam & "outXYSmthDEM=" & argVal & vbCrLf
                            out_fullpath_XYSmthDEM = argVal
                        Case "outXY"
                            msgParam = msgParam & "outXY=" & argVal & vbCrLf
                            out_XY = argVal
                        Case Else
                            Trace.WriteLine("Unknown parameter: " & argName)
                            System.Environment.ExitCode = 9
                            Return
                    End Select
                End If
            Next
            Trace.WriteLine(msgParam)

            'find missing parameters
            Dim msgParamErr As String = ""
            If in_fullpath_dem = "" Then msgParamErr = msgParamErr & " inDEM "
            If in_fullpath_wb = "" Then msgParamErr = msgParamErr & " inWB "
            If in_fullpath_porosity = "" Then msgParamErr = msgParamErr & " inPoro "
            If in_fullpath_hydrcond = "" Then msgParamErr = msgParamErr & " inHCond "
            If in_zFactor = "" Then msgParamErr = msgParamErr & " inZFac "
            If in_smoothing = "" Then msgParamErr = msgParamErr & " inSmth "
            If in_fillSinks = "" Then msgParamErr = msgParamErr & " inFillSnk "
            If in_outputIntermediate = "" Then msgParamErr = msgParamErr & " inSaveTMP "
            If out_fullpath_mag = "" Then msgParamErr = msgParamErr & " outMag "
            If out_fullpath_dir = "" Then msgParamErr = msgParamErr & " outDir "
            If out_fullpath_hydrGrad = "" Then msgParamErr = msgParamErr & " outHydrGr "
            If out_fullpath_hydrGrad = "." Then out_fullpath_hydrGrad = ""
            If out_fullpath_smthDEM = "." Then out_fullpath_smthDEM = ""
            If out_XY <> "" And out_fullpath_XYSmthDEM = "" Then msgParamErr = msgParamErr & " outXYSmthDEM "
            If out_XY = "" And out_fullpath_XYSmthDEM <> "" Then msgParamErr = msgParamErr & " outXY "
            If msgParamErr <> "" Then
                Trace.WriteLine("The following parameters are missing!  " & msgParamErr)
                System.Environment.ExitCode = 15
                Return
            End If


            'open the data sets
            Dim dem_r As IRaster2
            Dim wb_fc As IFeatureClass
            Dim cond_r As IRaster2
            Dim poro_r As IRaster2

            Trace.WriteLine("opening inDEM: " & in_fullpath_dem)
            If Not AqDn.Utilities.checkExist(in_fullpath_dem) Then
                Trace.WriteLine(in_fullpath_dem & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            dem_r = AqDn.Utilities.createRasterFromFile(in_fullpath_dem)
            Trace.WriteLine("opening inWB: " & in_fullpath_wb)
            If Not AqDn.Utilities.checkExist(in_fullpath_wb) Then
                Trace.WriteLine(in_fullpath_wb & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            wb_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_wb)
            Trace.WriteLine("opening inPoro: " & in_fullpath_porosity)
            If Not AqDn.Utilities.checkExist(in_fullpath_porosity) Then
                Trace.WriteLine(in_fullpath_porosity & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            poro_r = AqDn.Utilities.createRasterFromFile(in_fullpath_porosity)
            Trace.WriteLine("opening inHCond: " & in_fullpath_hydrcond)
            If Not AqDn.Utilities.checkExist(in_fullpath_hydrcond) Then
                Trace.WriteLine(in_fullpath_hydrcond & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            cond_r = AqDn.Utilities.createRasterFromFile(in_fullpath_hydrcond)

            Trace.WriteLine("parsing remaining parameters")
            Dim zfac As Single
            Dim smth As Integer
            Dim fillsink As Boolean
            Dim outintermediate As Boolean
            Dim pointsXY As List(Of Point)
            

            zfac = CType(in_zFactor, Single)
            smth = CType(in_smoothing, Integer)
            fillsink = CType(in_fillSinks, Boolean)
            outintermediate = CType(in_outputIntermediate, Boolean)
            pointsXY = parseXY(out_XY)
            If pointsXY Is Nothing Then Throw New Exception("Error parsing xy points")

            If AqDn.Utilities.checkExist(out_fullpath_dir) Then
                Trace.WriteLine(out_fullpath_dir & " already exists!")
                System.Environment.ExitCode = 5
                Return
            End If
            If AqDn.Utilities.checkExist(out_fullpath_mag) Then
                Trace.WriteLine(out_fullpath_mag & " already exists!")
                System.Environment.ExitCode = 5
                Return
            End If

            'perform the flow module calculation
            Trace.WriteLine("Running flow module...")
            flow = New DarcyFlow(dem:=dem_r, wb:=wb_fc, k:=cond_r, porosity:=poro_r, slope_zfactor:=zfac, smoothing:=smth, _
                                     p_mag:=out_fullpath_mag, p_dir:=out_fullpath_dir, _
                                     p_hydrgr:=out_fullpath_hydrGrad, _
                                     fillsinks:=fillsink, _
                                     p_smthDEM:=out_fullpath_smthDEM, _
                                     outputIntermediateRasters:=outintermediate)
            Dim output() As RasterDataset
            output = flow.calculateDarcyFlow()
            If output Is Nothing Then
                Throw New Exception("There was an error running the water table routine. Check the log for errors")
            End If
            Trace.WriteLine("Running flow module...Done")
            flow = Nothing

            'read the smoothed dem at the specified xy locations
            If pointsXY.Count > 0 Then
                Trace.WriteLine("Getting values of the smoothed DEM at the specified locations")
                Dim smthdem_r As IRaster2 = CType(output(3), IRasterDataset2).CreateFullRaster
                readRasterXY(smthdem_r, pointsXY, out_fullpath_XYSmthDEM)
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            System.Environment.ExitCode = 20
        End Try
    End Sub

    Sub runParticleTracking(ByVal args() As String)
        Dim track As ParticleTracker

        Dim in_fullpath_wb As String = ""
        Dim in_fullpath_mag As String = ""
        Dim in_fullpath_dir As String = ""
        Dim in_fullpath_por As String = ""
        Dim in_fullpath_src As String = ""
        Dim in_WBCellSz As String = ""
        Dim in_stepSz As String = ""
        Dim in_maxSteps As String = ""
        Dim out_fullpath_paths As String = ""

        Dim argName As String = ""
        Dim argVal As String = ""
        Dim argIdxDelim As Integer = 0

        Try

            'get parameter values
            Dim msgParam As String = ""
            For Each arg As String In args
                argIdxDelim = arg.IndexOf("=")
                If argIdxDelim >= 0 Then
                    argName = arg.Substring(0, argIdxDelim)
                    argVal = arg.Substring(argIdxDelim + 1, arg.Length - argIdxDelim - 1)
                    Select Case argName
                        Case "inWB"
                            msgParam = msgParam & "inWB=" & argVal & vbCrLf
                            in_fullpath_wb = argVal
                        Case "inMag"
                            msgParam = msgParam & "inMag=" & argVal & vbCrLf
                            in_fullpath_mag = argVal
                        Case "inDir"
                            msgParam = msgParam & "inDir=" & argVal & vbCrLf
                            in_fullpath_dir = argVal
                        Case "inPoro"
                            msgParam = msgParam & "inPoro=" & argVal & vbCrLf
                            in_fullpath_por = argVal
                        Case "inSources"
                            msgParam = msgParam & "inSources=" & argVal & vbCrLf
                            in_fullpath_src = argVal
                        Case "inWBCellSz"
                            msgParam = msgParam & "inWBCellSz=" & argVal & vbCrLf
                            in_WBCellSz = argVal
                        Case "inStepSz"
                            msgParam = msgParam & "inStepSz=" & argVal & vbCrLf
                            in_stepSz = argVal
                        Case "inMaxSteps"
                            msgParam = msgParam & "inMaxSteps=" & argVal & vbCrLf
                            in_maxSteps = argVal
                        Case "outPaths"
                            msgParam = msgParam & "outPaths=" & argVal & vbCrLf
                            out_fullpath_paths = argVal
                        Case Else
                            Trace.WriteLine("Unknown parameter: " & argName)
                            System.Environment.ExitCode = 9
                            Return
                    End Select
                End If
            Next
            Trace.WriteLine(msgParam)

            'find missing parameters
            Dim msgParamErr As String = ""
            If in_fullpath_wb = "" Then msgParamErr = msgParamErr & " inWB "
            If in_fullpath_mag = "" Then msgParamErr = msgParamErr & " inMag "
            If in_fullpath_dir = "" Then msgParamErr = msgParamErr & " inDir "
            If in_fullpath_por = "" Then msgParamErr = msgParamErr & " inPoro "
            If in_fullpath_src = "" Then msgParamErr = msgParamErr & " inSources "
            If in_WBCellSz = "" Then msgParamErr = msgParamErr & " inWBCellSz "
            If in_stepSz = "" Then msgParamErr = msgParamErr & " inStepSz "
            If in_maxSteps = "" Then msgParamErr = msgParamErr & " inMaxSteps "
            If out_fullpath_paths = "" Then msgParamErr = msgParamErr & " outPaths "
            If msgParamErr <> "" Then
                Trace.WriteLine("The following parameters are missing!  " & msgParamErr)
                System.Environment.ExitCode = 15
                Return
            End If

            'open the data sets
            Dim mag_r, dir_r, wb_r As IRaster2
            Dim sources_fc, wb_fc As IFeatureClass
            Dim poro_r As IRaster2

            Trace.WriteLine("opening inMag: " & in_fullpath_mag)
            If Not AqDn.Utilities.checkExist(in_fullpath_mag) Then
                Trace.WriteLine(in_fullpath_mag & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            mag_r = AqDn.Utilities.createRasterFromFile(in_fullpath_mag)
            Trace.WriteLine("opening inDir: " & in_fullpath_dir)
            If Not AqDn.Utilities.checkExist(in_fullpath_dir) Then
                Trace.WriteLine(in_fullpath_dir & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            dir_r = AqDn.Utilities.createRasterFromFile(in_fullpath_dir)
            Trace.WriteLine("opening inWB: " & in_fullpath_wb)
            If Not AqDn.Utilities.checkExist(in_fullpath_wb) Then
                Trace.WriteLine(in_fullpath_wb & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            wb_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_wb)
            Trace.WriteLine("opening inPoro: " & in_fullpath_por)
            If Not AqDn.Utilities.checkExist(in_fullpath_por) Then
                Trace.WriteLine(in_fullpath_por & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            poro_r = AqDn.Utilities.createRasterFromFile(in_fullpath_por)
            Trace.WriteLine("opening inSources " & in_fullpath_src)
            If Not AqDn.Utilities.checkExist(in_fullpath_src) Then
                Trace.WriteLine(in_fullpath_src & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            sources_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_src)

            Trace.WriteLine("parsing remaining parameters")
            Dim WBCellSz As Single
            Dim stepSz As Single
            Dim maxsteps As Integer
            Dim shpName, shpPath As String

            WBCellSz = CType(in_WBCellSz, Single)
            stepSz = CType(in_WBCellSz, Single)
            maxsteps = CType(in_maxSteps, Integer)
            shpName = IO.Path.GetFileNameWithoutExtension(out_fullpath_paths.Replace("""", ""))
            shpPath = IO.Path.GetDirectoryName(out_fullpath_paths.Replace("""", ""))

            If AqDn.Utilities.checkExist(out_fullpath_paths) Then
                Trace.WriteLine(out_fullpath_paths & " already exists!")
                System.Environment.ExitCode = 5
                Return
            End If

            Trace.WriteLine("Converting '" & wb_fc.AliasName & "' to raster")
            wb_r = AqDn.Utilities.FeatureclassToRaster(wb_fc, WBCellSz, mag_r, CType(mag_r, IRasterProps).SpatialReference)
            
            Trace.WriteLine("Running particle tracking...")
            track = New ParticleTracker(mag_r, dir_r, wb_r, poro_r, _
                                      shpName, shpPath, _
                                      CType(mag_r, IRasterProps).SpatialReference, sources_fc, _
                                      stepSz, maxsteps)
            Dim result As Boolean = track.track
            If Not result Then Throw New Exception("Particle tracking failed")

            Trace.WriteLine("Running particle tracking...Done")

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            System.Environment.ExitCode = 20
        End Try
    End Sub

    Sub runTransport(ByVal args() As String)
        Dim trans As Transport

        Dim in_fullpath_src As String = ""
        Dim in_fullpath_ptracks As String = ""
        Dim in_fullpath_wb As String = ""    
        Dim in_ax As String = ""
        Dim in_ay As String = ""
        Dim in_Y As String = ""
        Dim in_Z As String = ""
        Dim in_cellSz As String = ""
        Dim in_C0 As String = ""
        Dim in_CThresh As String = ""
        Dim in_decay As String = ""
        Dim in_warpMethod As String = ""
        Dim in_warpCtrlPtSpacing As String = ""
        Dim in_warpUseApprox As String = ""
        Dim in_postProc As String = ""
        Dim in_volFac As String = ""
        Dim in_domBdy As String = ""
        Dim in_outputIntermediate As String = ""
        Dim in_maxMem As String = ""
        Dim out_fullpath_plumes As String = ""
        Dim out_fullpath_XYConc As String = ""
        Dim out_XY As String = ""


        Dim argName As String = ""
        Dim argVal As String = ""
        Dim argIdxDelim As Integer = 0
        Try

            'get parameter values
            Dim msgParam As String = ""
            For Each arg As String In args
                argIdxDelim = arg.IndexOf("=")
                If argIdxDelim >= 0 Then
                    argName = arg.Substring(0, argIdxDelim)
                    argVal = arg.Substring(argIdxDelim + 1, arg.Length - argIdxDelim - 1)
                    Select Case argName
                        Case "inSources"
                            msgParam = msgParam & "inSources=" & argVal & vbCrLf
                            in_fullpath_src = argVal
                        Case "inPTracks"
                            msgParam = msgParam & "inPTracks=" & argVal & vbCrLf
                            in_fullpath_ptracks = argVal
                        Case "inWB"
                            msgParam = msgParam & "inWB=" & argVal & vbCrLf
                            in_fullpath_wb = argVal
                        Case "inAx"
                            msgParam = msgParam & "inAx=" & argVal & vbCrLf
                            in_ax = argVal
                        Case "inAy"
                            msgParam = msgParam & "inAy=" & argVal & vbCrLf
                            in_ay = argVal
                        Case "inY"
                            msgParam = msgParam & "inY=" & argVal & vbCrLf
                            in_Y = argVal
                        Case "inZ"
                            msgParam = msgParam & "inZ=" & argVal & vbCrLf
                            in_Z = argVal
                        Case "inCellSz"
                            msgParam = msgParam & "inCellSz=" & argVal & vbCrLf
                            in_cellSz = argVal
                        Case "inC0"
                            msgParam = msgParam & "inC0=" & argVal & vbCrLf
                            in_C0 = argVal
                        Case "inCThresh"
                            msgParam = msgParam & "inCThresh=" & argVal & vbCrLf
                            in_Cthresh = argVal
                        Case "inDecay"
                            msgParam = msgParam & "inDecay=" & argVal & vbCrLf
                            in_decay = argVal
                        Case "inWarpMthd"
                            msgParam = msgParam & "inWarpMthd=" & argVal & vbCrLf
                            in_warpMethod = argVal
                        Case "inWarpCtrlPtSpc"
                            msgParam = msgParam & "inWarpCtrlPtSpc=" & argVal & vbCrLf
                            in_warpCtrlPtSpacing = argVal
                        Case "inWarpUseApprox"
                            msgParam = msgParam & "inWarpUseApprox=" & argVal & vbCrLf
                            in_warpUseApprox = argVal
                        Case "inPostProc"
                            msgParam = msgParam & "inPostProc=" & argVal & vbCrLf
                            in_postProc = argVal
                        Case "inVolFac"
                            msgParam = msgParam & "inVolFac=" & argVal & vbCrLf
                            in_volFac = argVal
                        Case "inDomenicoBdy"
                            msgParam = msgParam & "inDomenicoBdy=" & argVal & vbCrLf
                            in_domBdy = argVal
                        Case "inSaveTMP"
                            msgParam = msgParam & "inSaveTMP=" & argVal & vbCrLf
                            in_outputIntermediate = argVal
                        Case "inMaxMem"
                            msgParam = msgParam & "inMaxMem=" & argVal & vbCrLf
                            in_maxMem = argVal
                        Case "outPlumes"
                            msgParam = msgParam & "outPlumes=" & argVal & vbCrLf
                            out_fullpath_plumes = argVal
                        Case "outXYConc"
                            msgParam = msgParam & "outXYConc=" & argVal & vbCrLf
                            out_fullpath_XYConc = argVal
                        Case "outXY"
                            msgParam = msgParam & "outXY=" & argVal & vbCrLf
                            out_XY = argVal
                        Case Else
                            Trace.WriteLine("Unknown parameter: " & argName)
                            System.Environment.ExitCode = 9
                            Return
                    End Select
                End If
            Next
            Trace.WriteLine(msgParam)

            'find missing parameters
            Dim msgParamErr As String = ""
            If in_fullpath_wb = "" Then msgParamErr = msgParamErr & " inWB "
            If in_fullpath_ptracks = "" Then msgParamErr = msgParamErr & " inPTracks "
            If in_fullpath_src = "" Then msgParamErr = msgParamErr & " inSources "
            If in_ax = "" Then msgParamErr = msgParamErr & " inAx "
            If in_ay = "" Then msgParamErr = msgParamErr & " inAy "
            If in_Y = "" Then msgParamErr = msgParamErr & " inY "
            If in_Z = "" Then msgParamErr = msgParamErr & " inZ "
            If in_cellSz = "" Then msgParamErr = msgParamErr & " inCellSz "
            If in_CThresh = "" Then msgParamErr = msgParamErr & " inCThresh "
            If in_C0 = "" Then msgParamErr = msgParamErr & " inC0 "
            If in_decay = "" Then msgParamErr = msgParamErr & " inDecay "
            If in_warpMethod = "" Then msgParamErr = msgParamErr & " inWarpMthd "
            If in_warpCtrlPtSpacing = "" Then msgParamErr = msgParamErr & " inWarpCtrlPtSpc "
            If in_warpUseApprox = "" Then msgParamErr = msgParamErr & " inWarpUseApprox "
            If in_postProc = "" Then msgParamErr = msgParamErr & " inPostProc "
            If in_volFac = "" Then msgParamErr = msgParamErr & " inVolFac "
            If in_domBdy = "" Then msgParamErr = msgParamErr & " inDomenicoBdy "
            If in_outputIntermediate = "" Then msgParamErr = msgParamErr & " inSaveTMP "
            If in_maxMem = "" Then msgParamErr = msgParamErr & " inMaxMem "
            If out_fullpath_plumes = "" Then msgParamErr = msgParamErr & " outPlumes "
            If out_XY <> "" And out_fullpath_XYConc = "" Then msgParamErr = msgParamErr & " outXYConc "
            If out_XY = "" And out_fullpath_XYConc <> "" Then msgParamErr = msgParamErr & " outXY "

            If msgParamErr <> "" Then
                Trace.WriteLine("The following parameters are missing!  " & msgParamErr)
                System.Environment.ExitCode = 15
                Return
            End If

            'open the data sets.
            Dim wb_fc As IFeatureClass
            Dim src_fc As IFeatureClass
            Dim paths_fc As IFeatureClass

            Trace.WriteLine("opening inSources " & in_fullpath_src)
            If Not AqDn.Utilities.checkExist(in_fullpath_src) Then
                Trace.WriteLine(in_fullpath_src & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            src_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_src)
            Trace.WriteLine("opening inWB " & in_fullpath_wb)
            If Not AqDn.Utilities.checkExist(in_fullpath_wb) Then
                Trace.WriteLine(in_fullpath_wb & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            wb_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_wb)
            Trace.WriteLine("opening inPTracks " & in_fullpath_ptracks)
            If Not AqDn.Utilities.checkExist(in_fullpath_ptracks) Then
                Trace.WriteLine(in_fullpath_ptracks & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            paths_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_ptracks)

            Trace.WriteLine("parsing remaining parameters")
            Dim pointsXY As List(Of Point)
            Dim ax, ay, Y, Z, cellSz, c0, cthresh, decay, volfac As Single
            Dim warpmethod, warpctrlpts, postproc, dombdy, maxmem As Integer
            Dim warpuseapprox, outputintermediate As Boolean

            ax = CType(in_ax, Single)
            ay = CType(in_ay, Single)
            Y = CType(in_Y, Single)
            Z = CType(in_Z, Single)
            cellSz = CType(in_cellSz, Single)
            c0 = CType(in_C0, Single)
            cthresh = CType(in_CThresh, Single)
            decay = CType(in_decay, Single)
            warpmethod = CType(in_warpMethod, Integer)
            warpctrlpts = CType(in_warpCtrlPtSpacing, Integer)
            warpmethod = CType(in_warpMethod, Integer)
            warpuseapprox = CType(in_warpUseApprox, Boolean)
            postproc = CType(in_postProc, Integer)
            volfac = CType(in_volFac, Single)
            dombdy = CType(in_domBdy, Integer)
            outputintermediate = CType(in_outputIntermediate, Boolean)
            maxmem = CType(in_maxMem, Integer)
            out_fullpath_plumes = out_fullpath_plumes.Replace("""", "")
            pointsXY = parseXY(out_XY)
            If pointsXY Is Nothing Then Throw New Exception("Error parsing xy points")

            If AqDn.Utilities.checkExist(out_fullpath_plumes) Then
                Trace.WriteLine(in_fullpath_ptracks & " already exists!")
                System.Environment.ExitCode = 5
                Return
            End If

            Trace.WriteLine("Running Transport module...")
            MathSpecial.initErfcTable()
            trans = New Transport(ParticleTracks:=paths_fc, Sources:=src_fc, waterbodies:=wb_fc, _
                                  ax:=ax, ay:=ay, az:=0, _
                                  Y:=Y, Z:=Z, _
                                  MeshCellSize_x:=cellSz, MeshCellSize_y:=cellSz, MeshCellSize_z:=Z, _
                                  InitialConcentration:=c0, _
                                  InitialConcentrationNO3:=40, _
                                  CalculatingNO3:=True, _
                                  CalculatingNH4:=True, _
                                  InitialConcentration_CNH4:=10, _
                                  DecayRateConstant_NH4:=0.001, _
                                  DecayRateConstant_NO3:=0.008, _
                                  ThresholdConcentration:=cthresh, _
                                  plume_z_max:=3.0, _
                                  plume_z_max_checked:=False, _
                                  SolutionTime:=-1, _
                                  SolutionType:=SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D, _
                                  DecayRateConstant:=decay, _
                                  warpmethod:=CType(warpmethod, WarpingMethods.WarpingMethod), _
                                  WarpCtrlPtSpac:=warpctrlpts, _
                                  warpuseapprox:=warpuseapprox, _
                                  PostProcessing:=CType(postproc, PostProcessing.PostProcessingAmount), _
                                  OutputIntermediateCalcs:=outputintermediate, _
                                  OutputPlumesFile:=out_fullpath_plumes, _
                                  VolumeConversionFactor:=volfac, _
                                  DomenicoBoundary:=CType(dombdy, DomenicoSourceBoundaries.DomenicoSourceBoundary), _
                                  MaxMemory:=maxmem, _
                                  OutputIntermediatePlumes:=False)
            Dim plumes_r As IRaster2 = trans.CalculatePlumes

            If plumes_r Is Nothing Then
                Trace.WriteLine("Ouptut raster is nothing!")
            Else
                Try
                    Trace.WriteLine("Saving...")

                    'the path should include the file extension already
                    Dim old_r = plumes_r
                    plumes_r = AqDn.Utilities.saveRasterToFile(plumes_r, out_fullpath_plumes, rectify:=True)
                    AqDn.Utilities.DeleteRaster(old_r)
                Catch ex As Exception
                    Trace.WriteLine("couldn't save the output raster to " & out_fullpath_plumes)
                End Try
            End If
            Trace.WriteLine("Running Transport module...Done")

            'read the hydraulic gradient at the specified xy locations
            If pointsXY.Count > 0 Then
                Trace.WriteLine("Getting values of hydraulic gradient at the specified locations")
                readRasterXY(plumes_r, pointsXY, out_fullpath_XYConc)
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            System.Environment.ExitCode = 20
        End Try
    End Sub

    Sub runLoadEstimation(ByVal args() As String)
        Dim nle As Denitrification

        Dim in_fullpath_info As String = ""
        Dim in_riskFactor As String = ""
        Dim out_fullpath_nle As String = ""

        Dim argName As String = ""
        Dim argVal As String = ""
        Dim argIdxDelim As Integer = 0

        Try

            'get parameter values
            Dim msgParam As String = ""
            For Each arg As String In args
                argIdxDelim = arg.IndexOf("=")
                If argIdxDelim >= 0 Then
                    argName = arg.Substring(0, argIdxDelim)
                    argVal = arg.Substring(argIdxDelim + 1, arg.Length - argIdxDelim - 1)
                    Select Case argName
                        Case "inInfoFile"
                            msgParam = msgParam & "inInfoFile=" & argVal & vbCrLf
                            in_fullpath_info = argVal
                        Case "inRiskFac"
                            msgParam = msgParam & "inRiskFac=" & argVal & vbCrLf
                            in_riskFactor = argVal
                        Case "outLoads"
                            msgParam = msgParam & "outLoads=" & argVal & vbCrLf
                            out_fullpath_nle = argVal
                        Case Else
                            Trace.WriteLine("Unknown parameter: " & argName)
                            System.Environment.ExitCode = 9
                            Return
                    End Select
                End If
            Next
            Trace.WriteLine(msgParam)

            'find missing parameters
            Dim msgParamErr As String = ""
            If in_fullpath_info = "" Then msgParamErr = msgParamErr & " inInfoFile "
            If in_riskFactor = "" Then msgParamErr = msgParamErr & " inRiskFac "
            If out_fullpath_nle = "" Then msgParamErr = msgParamErr & " outLoads "
            If msgParamErr <> "" Then
                Trace.WriteLine("The following parameters are missing!  " & msgParamErr)
                System.Environment.ExitCode = 15
                Return
            End If

            'open dataset
            Dim info_fc As IFeatureClass

            Trace.WriteLine("opening inInfoFile: " & in_fullpath_info)
            If Not AqDn.Utilities.checkExist(in_fullpath_info) Then
                Trace.WriteLine(in_fullpath_info & " does not exist!")
                System.Environment.ExitCode = 5
                Return
            End If
            info_fc = AqDn.Utilities.createFeatureClassFromShapeFile(in_fullpath_info)

            Trace.WriteLine("Parsing remaining parameters")
            Dim riskfac As Single

            riskfac = CType(in_riskFactor, Single)
            out_fullpath_nle = out_fullpath_nle.Replace("""", "")

            Trace.WriteLine("Running Nitrate Load Estimation module...")
            nle = New Denitrification(PlumesInfo:=info_fc, RiskFactor:=riskfac)
            If Not nle.CalculateLoad() Then Throw New Exception("Coulnd't calculate load")

            'get the results
            Dim resulttxt As String = nle.OutParams("txt")
            Trace.WriteLine("Running Nitrate Load Estimation module...Done")

            Trace.WriteLine("Saving load estimates to " & out_fullpath_nle)
            Dim f As New IO.StreamWriter(out_fullpath_nle, False)
            Dim csv As String = "Plumes input file: """ & in_fullpath_info.Replace("""", "") & """" & vbCrLf & _
                                "Risk Factor: " & riskfac & vbCrLf & _
                                "FID,M_out,M_out*RF,M_dn,M_in" & vbCrLf
            Dim rows As String() = resulttxt.Trim.Split(New Char() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim tokens As String()
            For Each row As String In rows
                tokens = row.Trim.Split(New Char() {vbTab, " "}, StringSplitOptions.RemoveEmptyEntries)
                For i As Short = 0 To tokens.Length - 1
                    If Not i = tokens.Length - 1 Then
                        csv = csv & """" & tokens(i).Trim & ""","
                    Else
                        csv = csv & """" & tokens(i).Trim & """" & vbCrLf
                    End If
                Next
            Next
            f.Write(csv)
            f.Close()
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            System.Environment.ExitCode = 20
        End Try
    End Sub

#Region "helper functions"
    ''' <summary>
    ''' Generate commandline arguments for testing purposes
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function getArgs(ByVal theModule As String) As String()
        Dim args() As String
        Select Case theModule
            Case "flow"
                args = New String() {"BatchTools.exe", _
                                    "flow", _
                                    "inDEM=""C:\GIS\lakeshore_example\lakeshore.img""", _
                                    "inWB=""C:\GIS\lakeshore_example\waterbodies.shp""", _
                                    "inHCond=""C:\GIS\lakeshore_example\hydr_cond.img""", _
                                    "inPoro=""C:\GIS\lakeshore_example\porosity.img""", _
                                    "inZFac=1", _
                                    "inSmth=20", _
                                    "inFillSnk=false", _
                                    "inSaveTMP=false", _
                                    "outMag=""C:\temp\mag.img""", _
                                    "outDir=""c:\temp\dir.img""", _
                                    "outSmthDEM=""c:\temp\smthDEM.img""", _
                                    "outHydrGr=", _
                                    "outXYSmthDEM=""c:\temp\XYSmthDEM.txt""", _
                                    "outXY=430296.585,3348829.014|429923.522,3349231.182|0,0"}
            Case "particleTrack"
                args = New String() {"BatchTools.exe", _
                                     "particleTrack", _
                                     "inWB=""C:\GIS\lakeshore_example\waterbodies.shp""", _
                                     "inMag=""C:\GIS\lakeshore_example\mag50.img""", _
                                     "inDir=""C:\GIS\lakeshore_example\dir50.img""", _
                                     "inPoro=""C:\GIS\lakeshore_example\porosity.img""", _
                                     "inSources=""c:\GIS\lakeshore_example\PotentialSepticTankLocations.shp""", _
                                     "inWBCellSz=5", _
                                     "inStepSz=10", _
                                     "inMaxSteps=1000", _
                                     "outPaths=""c:\temp\paths.shp"""}
            Case "transport"
                args = New String() {"Batchtools.exe", _
                                     "transport", _
                                     "inSources=""c:\GIS\lakeshore_example\PotentialSepticTankLocations.shp""", _
                                     "inPTracks=""c:\GIS\lakeshore_example\paths.shp""", _
                                     "inWB=""C:\GIS\lakeshore_example\waterbodies.shp""", _
                                     "inAx=2.113", _
                                     "inAy=0.234", _
                                     "inY=6", _
                                     "inZ=1", _
                                     "inCellSz=0.4", _
                                     "inC0=40", _
                                     "inCThresh=0.000001", _
                                     "inDecay=0.008", _
                                     "inWarpMthd=1", _
                                     "inWarpCtrlPtSpc=48", _
                                     "inWarpUseApprox=true", _
                                     "inPostProc=1", _
                                     "inVolFac=1000", _
                                     "inDomenicoBdy=2", _
                                     "inSaveTMP=false", _
                                     "inMaxMem=1100", _
                                     "outPlumes=""c:\temp\plumes.img""", _
                                     "outXYConc=""c:\temp\XYConc.txt""", _
                                     "outXY=430296.585,3348829.014|429923.522,3349231.182|0,0"}
            Case "loadEstimation"
                args = New String() {"batchtools.exe", _
                                     "loadEstimation", _
                                     "inInfoFile=""c:\GIS\lakeshore_example\plumes_info.shp""", _
                                     "inRiskFac=1", _
                                     "outLoads=""c:\temp\load.csv"""}


            Case Else
                args = New String() {}
        End Select
        Return args
    End Function

    ''' <summary> 
    ''' Returns a list of points given the input string.  The input string must specify
    ''' the points in UTM Zone 17N.
    ''' </summary>
    ''' <param name="out_XY"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function parseXY(ByVal out_XY As String) As List(Of Point)
        Dim pointsXY As New List(Of Point)
        Dim pt As Point
        Dim strPoints() As String = Nothing
        Dim strPoint() As String = Nothing
        Dim UTM_Z17N As IProjectedCoordinateSystem
        Dim spatRefFac As ISpatialReferenceFactory
        Trace.Indent()
        Try
            spatRefFac = New SpatialReferenceEnvironment
            UTM_Z17N = spatRefFac.CreateProjectedCoordinateSystem(esriSRProjCSType.esriSRProjCS_NAD1983UTM_17N)
            If out_XY <> "" Then
                Trace.WriteLine("parsing xy as UTM 17N NAD1983")
                strPoints = out_XY.Split("|")
                For Each point As String In strPoints
                    strPoint = point.Split(",")
                    If strPoint.Length <> 2 Then
                        Throw New Exception("The outXY parameter is specified incorrectly. Correct format is x1,y2|x2,y2|xn,yn")
                    Else
                        pt = New Point With {.X = CType(strPoint(0), Single), .Y = CType(strPoint(1), Single)}
                        pt.SpatialReference = UTM_Z17N
                        pointsXY.Add(pt)
                    End If
                Next
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
        End Try
        Trace.Unindent()
        Return pointsXY
    End Function

    ''' <summary>
    ''' Reads the values of the given raster at the specified points an outputs the results to the
    ''' specified file
    ''' </summary>
    ''' <param name="r"></param>
    ''' <param name="ptsXY"></param>
    ''' <param name="outfile"></param>
    ''' <remarks></remarks>
    Private Sub readRasterXY(ByVal r As IRaster2, ByVal ptsXY As List(Of Point), ByVal outfile As String)
        Trace.Indent()
        Try
            outfile = outfile.Replace("""", "")
            Trace.WriteLine("Opening output file " & outfile)
            Dim f As New IO.StreamWriter(outfile, False)

            Trace.WriteLine("Reading values from " & r.RasterDataset.CompleteName & " ...")

            Dim row, col As Integer
            Dim val As Object
            Dim str As String
            Dim nodata As Object = AqDn.Utilities.getRasterNoDataValue(r)
            For Each pt As Point In ptsXY
                pt.Project(CType(r, IRasterProps).SpatialReference)
                r.MapToPixel(pt.X, pt.Y, col, row)
                val = r.GetPixelValue(0, col, row)
                If val Is Nothing Then val = CType(0, Single)
                If val = nodata Then val = 0
                str = pt.X & vbTab & pt.Y & vbTab & val
                f.WriteLine(str)
                Trace.WriteLine(str)
                f.Flush()
            Next
            f.Close()

            Trace.WriteLine("Reading values...Done")
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            System.Environment.ExitCode = 20
        End Try
        Trace.Unindent()
    End Sub
#End Region

End Module

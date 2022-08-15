Imports AqDn
Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Channels
Imports System.Runtime.Remoting.Channels.Ipc
Imports System.Reflection
Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.DataSourcesraster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports IAqDnApplication
Imports ESRI.ArcGIS.ADF.Connection.Local



Public Class TransportWrapper
    Inherits MarshalByRefObject
    Implements IAqDnApplication.IModuleWrapper

    Private Shared m_chanToMain As IpcClientChannel
    'Private Shared m_chanFromMain As IpcServerChannel
    Private Shared m_trackingcancelled As Boolean

    'parameters that will be read by the main application when the transport calc completts
    'successfully.
    Private Shared m_outparams As New Hashtable

    'parameters that will be read by the main application when the transport calc completts
    'successfully.
    Public ReadOnly Property OutputParams() As Hashtable Implements IModuleWrapper.OutputParams
        Get
            Return m_outparams
        End Get
    End Property

    ''' <summary>
    ''' this will be set by the main program
    ''' when set, cancellation will be attempted.
    ''' NOT CURRENTLY IMPLEMENTED
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Property TrackingCancelled() As Boolean Implements IAqDnApplication.IModuleWrapper.TransportCancelled
        Get
            Return m_trackingcancelled
        End Get
        Set(ByVal value As Boolean)
            m_trackingcancelled = value
        End Set
    End Property

    Public Shared Sub run(ByVal serverURI As String)
        Try

            'the trace listener
            Dim tl As TraceOutputForward

            'the reference to the mainUI
            Dim aqdnMain As IModuleRunRemote

            '***********************************
            'client channel
            'used to fetch stuff from the main UI
            '************************************
            Try
                'initialize remote object
                aqdnMain = Activator.GetObject(GetType(ModuleRunRemote), _
                                               serverURI & "/srv")
                'intialize logging forwarding
                tl = New TraceOutputForward(aqdnMain)

            Catch ex As Exception
                Throw New Exception(Assembly.GetExecutingAssembly.GetName.Name & " Error initializing TransportWrapper-Main client channel" & vbCrLf & ex.ToString)
            End Try

            '***********************************
            'server channel
            'used by the main UI to notify us of cancellation and to read output paths
            '   * not currently implemented. mainUI just kills the process for now*
            '************************************
            'Try
            '    'Create and register an IPC channel
            '    'use a unique name so that we can have multiple instances without error
            '    'also relax permissions to avoid security exceptions
            '    Dim provider As New System.Runtime.Remoting.Channels.BinaryServerFormatterSinkProvider
            '    provider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full
            '    m_chanFromMain = New IpcServerChannel(portName:="aq-dn-wrapper" & Now.Ticks, sinkProvider:=provider, Name:="")
            '    ChannelServices.RegisterChannel(m_chanFromMain, False)

            '    'Expose an object
            '    RemotingConfiguration.RegisterWellKnownServiceType(GetType(TransportWrapper), "srv", WellKnownObjectMode.Singleton)

            '    'use the client channel to set the address of this channel in the main program
            '    aqdnMain.CancelChannelURI = m_chanFromMain.GetChannelUri

            '    'Wait for calls
            '    Trace.WriteLine(String.Format(Assembly.GetExecutingAssembly.GetName.Name & " Listening on {0}", m_chanFromMain.GetChannelUri()))
            'Catch ex As Exception
            '    Throw New Exception("Could not create TransportWrapper-Main server channel" & vbCrLf & ex.ToString)
            'End Try            

            '********************************
            'communications has been initialized
            'now proceed with running the calculation
            '********************************

            'get parameters from the mainUI
            Dim h As Hashtable = aqdnMain.InParams

            'open the necessary files
            Dim wf As IWorkspaceFactory2
            Dim fw As IFeatureWorkspace
            Dim fc_paths, fc_septic, fc_wb As IFeatureClass
            Dim r_wb As IRaster2
            Dim sourcesN0FldName As String
            Dim sourcesN0FldName_NH4 As String

            'NOTE BY Yan 20141029 adding the spatial Min
            Dim SourcesMin As String

            'sort the values in the hashtable by key and output
            For Each item As DictionaryEntry In CType(h, IEnumerable).Cast(Of DictionaryEntry).OrderBy(Function(DictEntry) DictEntry.Key)
                Trace.WriteLine(item.Key & " = " & item.Value)
            Next

            Try
                wf = New ShapefileWorkspaceFactory

                'open the particle paths
                fw = wf.OpenFromFile(h("particletracks_path"), Nothing)
                fc_paths = fw.OpenFeatureClass(h("particletracks_shpname"))
                Trace.WriteLine("Opened feature class: " & h("particletracks_path") & "\" & fc_paths.AliasName)
                ComReleaser.ReleaseCOMObject(fw)

                'open the septic tanks locations
                fw = wf.OpenFromFile(h("sources_path"), Nothing)
                fc_septic = fw.OpenFeatureClass(h("sources_shpname"))
                Trace.WriteLine("Opened feature class: " & h("sources_path") & "\" & fc_septic.AliasName)
                ComReleaser.ReleaseCOMObject(fw)

                'open the waterbodies feature class or raster depending on the type
                'Modify for NH4 calculation. the following one can get no warning.
                fc_wb = Nothing
                'End modifying.
                If h.ContainsKey("waterbodies_israster") AndAlso h("waterbodies_israster") = "True" Then
                    If Not h("waterbodies_path") Is Nothing Then
                        r_wb = Utilities.createRasterFromFile(IO.Path.Combine(h("waterbodies_path"), h("waterbodies_shpname") & ".img"))
                        Trace.WriteLine("Opened raster: " & IO.Path.Combine(h("waterbodies_path"), h("waterbodies_shpname") & ".img"))
                    End If
                Else
                    If Not h("waterbodies_path") Is Nothing Then
                        fw = wf.OpenFromFile(h("waterbodies_path"), Nothing)
                        fc_wb = fw.OpenFeatureClass(h("waterbodies_shpname"))
                        Trace.WriteLine("Opened feature class: " & h("waterbodies_path") & "\" & fc_wb.AliasName)
                        ComReleaser.ReleaseCOMObject(fw)
                    End If                    
                End If


                If Not h.ContainsKey("sourcesN0FldName") Then
                    sourcesN0FldName = "N0_conc"
                Else
                    sourcesN0FldName = h("sourcesN0FldName")
                End If

                'adding the NH4 calculation.
                If Not h.ContainsKey("sourcesN0FldName_NH4") Then
                    sourcesN0FldName_NH4 = "NH4_conc"
                Else
                    sourcesN0FldName_NH4 = h("sourcesN0FldName_NH4")
                End If
                'End adding.

                'NOte by Yan: adding the spatial Min
                If Not h.ContainsKey("SourcesMin") Then
                    SourcesMin = "Min"
                Else
                    SourcesMin = h("SourcesMin")
                End If
                'end adding


                '************
                'important
                Trace.WriteLine("Initializing ERFC() lookup table")
                If Not MathSpecial.initErfcTable() Then Throw New Exception("Could not initialize erfc lookup table")
                '*************

                'setup the transport object
                Dim plumes As Transport
                Dim plumes_NH4 As AqDn.Transport
                Dim CalculateNO3 As Boolean
                Dim NH4Calculationchecked As Boolean = CType(h("NH4_calculation_checked"), Boolean)





                'Add the NH4 calculation.
                If NH4Calculationchecked Then
                    Trace.WriteLine("Start NH4 calculation.")
                    CalculateNO3 = False
                    If Not fc_wb Is Nothing Then
                        plumes_NH4 = New Transport(ParticleTracks:=fc_paths, Sources:=fc_septic, waterbodies:=fc_wb, _
                                              ax:=h("ax_NH4"), ay:=h("ay_NH4"), az:=h("az"), _
                                              Y:=h("Y"), Z:=h("Z_NH4"), _
                                              MeshCellSize_x:=h("mesh_x"), MeshCellSize_y:=h("mesh_y"), MeshCellSize_z:=h("mesh_z"), _
                                              InitialConcentration:=h("conc_init_NH4"), _
                                              InitialConcentrationNO3:=h("conc_init_NO3"), _
                                              CalculatingNO3:=CalculateNO3, _
                                              CalculatingNH4:=NH4Calculationchecked, _
                                              InitialConcentration_CNH4:=h("conc_init_NH4"), _
                                              DecayRateConstant_NH4:=h("decay_coeff_NH4_calculation"), _
                                              DecayRateConstant_NO3:=h("decay_coeff"), _
                                              ThresholdConcentration:=h("conc_thresh"), _
                                              plume_z_max:=h("z_max"), _
                                              plume_z_max_checked:=h("z_max_checked"), _
                                              SolutionTime:=h("solution_time"), _
                                              SolutionType:=h("solution_type"), _
                                              DecayRateConstant:=h("decay_coeff_NH4_calculation"), _
                                              WarpMethod:=h("warp_method"), _
                                              WarpCtrlPtSpac:=h("warp_ctrlptspac"), _
                                              WarpUseApprox:=h("warp_useapprox"), _
                                              PostProcessing:=h("postprocessing"), _
                                              OutputIntermediateCalcs:=h("output_intermediate"), _
                                              OutputPlumesFile:=h("raster_output_path_NH4"), _
                                              VolumeConversionFactor:=h("vol_conversion_fac"), _
                                              DomenicoBoundary:=h("domenico_bdy"), _
                                              MaxMemory:=h("max_mem"), _
                                              OutputIntermediatePlumes:=h("output_intermediate_plumes"), _
                                              sourcesN0FldName:=sourcesN0FldName, _
                                              sourcesN0FldName_NH4:=sourcesN0FldName_NH4, _
                                              SourcesMin:=SourcesMin)
                    Else
                        plumes_NH4 = New Transport(ParticleTracks:=fc_paths, Sources:=fc_septic, waterbodies:=r_wb, _
                          ax:=h("ax_NH4"), ay:=h("ay_NH4"), az:=h("az"), _
                          Y:=h("Y"), Z:=h("Z_NH4"), _
                          MeshCellSize_x:=h("mesh_x"), MeshCellSize_y:=h("mesh_y"), MeshCellSize_z:=h("mesh_z"), _
                          InitialConcentration:=h("conc_init_NH4"), _
                          InitialConcentrationNO3:=h("conc_init_NO3"), _
                          CalculatingNO3:=CalculateNO3, _
                          CalculatingNH4:=NH4Calculationchecked, _
                          InitialConcentration_CNH4:=h("conc_init_NH4"), _
                          DecayRateConstant_NH4:=h("decay_coeff_NH4_calculation"), _
                          DecayRateConstant_NO3:=h("decay_coeff"), _
                          ThresholdConcentration:=h("conc_thresh"), _
                          plume_z_max:=h("z_max"), _
                          plume_z_max_checked:=h("z_max_checked"), _
                          SolutionTime:=h("solution_time"), _
                          SolutionType:=h("solution_type"), _
                          DecayRateConstant:=h("decay_coeff_NH4_calculation"), _
                          WarpMethod:=h("warp_method"), _
                          WarpCtrlPtSpac:=h("warp_ctrlptspac"), _
                          WarpUseApprox:=h("warp_useapprox"), _
                          PostProcessing:=h("postprocessing"), _
                          OutputIntermediateCalcs:=h("output_intermediate"), _
                          OutputPlumesFile:=h("raster_output_path_NH4"), _
                          VolumeConversionFactor:=h("vol_conversion_fac"), _
                          DomenicoBoundary:=h("domenico_bdy"), _
                          MaxMemory:=h("max_mem"), _
                          OutputIntermediatePlumes:=h("output_intermediate_plumes"), _
                          sourcesN0FldName:=sourcesN0FldName, _
                          sourcesN0FldName_NH4:=sourcesN0FldName_NH4, _
                                              SourcesMin:=SourcesMin)
                    End If
                    Dim plumes_r_NH4 As IRaster2 = plumes_NH4.CalculatePlumes
                    If plumes_NH4 Is Nothing Then Throw New Exception("There was an error running the Transport module")

                    Trace.WriteLine("Start A2 calculation.")
                    CalculateNO3 = True
                    If Not fc_wb Is Nothing Then
                        plumes = New Transport(ParticleTracks:=fc_paths, Sources:=fc_septic, waterbodies:=fc_wb, _
                                          ax:=h("ax"), ay:=h("ay"), az:=h("az"), _
                                          Y:=h("Y"), Z:=h("Z"), _
                                          MeshCellSize_x:=h("mesh_x"), MeshCellSize_y:=h("mesh_y"), MeshCellSize_z:=h("mesh_z"), _
                                          InitialConcentration:=h("conc_init"), _
                                          InitialConcentrationNO3:=h("conc_init_NO3"), _
                                          CalculatingNO3:=CalculateNO3, _
                                          CalculatingNH4:=NH4Calculationchecked, _
                                          InitialConcentration_CNH4:=h("conc_init_NH4"), _
                                          DecayRateConstant_NH4:=h("decay_coeff_NH4_calculation"), _
                                          DecayRateConstant_NO3:=h("decay_coeff"), _
                                          ThresholdConcentration:=h("conc_thresh"), _
                                              plume_z_max:=h("z_max"), _
                                              plume_z_max_checked:=h("z_max_checked"), _
                                          SolutionTime:=h("solution_time"), _
                                          SolutionType:=h("solution_type"), _
                                          DecayRateConstant:=h("decay_coeff"), _
                                          WarpMethod:=h("warp_method"), _
                                          WarpCtrlPtSpac:=h("warp_ctrlptspac"), _
                                          WarpUseApprox:=h("warp_useapprox"), _
                                          PostProcessing:=h("postprocessing"), _
                                          OutputIntermediateCalcs:=h("output_intermediate"), _
                                          OutputPlumesFile:=h("raster_output_path"), _
                                          VolumeConversionFactor:=h("vol_conversion_fac"), _
                                          DomenicoBoundary:=h("domenico_bdy"), _
                                          MaxMemory:=h("max_mem"), _
                                          OutputIntermediatePlumes:=h("output_intermediate_plumes"), _
                                          sourcesN0FldName:=sourcesN0FldName, _
                          sourcesN0FldName_NH4:=sourcesN0FldName_NH4, _
                                              SourcesMin:=SourcesMin)
                    Else
                        plumes = New Transport(ParticleTracks:=fc_paths, Sources:=fc_septic, waterbodies:=r_wb, _
                                          ax:=h("ax"), ay:=h("ay"), az:=h("az"), _
                                          Y:=h("Y"), Z:=h("Z"), _
                                          MeshCellSize_x:=h("mesh_x"), MeshCellSize_y:=h("mesh_y"), MeshCellSize_z:=h("mesh_z"), _
                                          InitialConcentration:=h("conc_init"), _
                                          InitialConcentrationNO3:=h("conc_init_NO3"), _
                                          CalculatingNO3:=CalculateNO3, _
                                          CalculatingNH4:=NH4Calculationchecked, _
                                          InitialConcentration_CNH4:=h("conc_init_NH4"), _
                                          DecayRateConstant_NH4:=h("decay_coeff_NH4_calculation"), _
                                          DecayRateConstant_NO3:=h("decay_coeff"), _
                                          ThresholdConcentration:=h("conc_thresh"), _
                                              plume_z_max:=h("z_max"), _
                                              plume_z_max_checked:=h("z_max_checked"), _
                                          SolutionTime:=h("solution_time"), _
                                          SolutionType:=h("solution_type"), _
                                          DecayRateConstant:=h("decay_coeff"), _
                                          WarpMethod:=h("warp_method"), _
                                          WarpCtrlPtSpac:=h("warp_ctrlptspac"), _
                                          WarpUseApprox:=h("warp_useapprox"), _
                                          PostProcessing:=h("postprocessing"), _
                                          OutputIntermediateCalcs:=h("output_intermediate"), _
                                          OutputPlumesFile:=h("raster_output_path"), _
                                          VolumeConversionFactor:=h("vol_conversion_fac"), _
                                          DomenicoBoundary:=h("domenico_bdy"), _
                                          MaxMemory:=h("max_mem"), _
                                          OutputIntermediatePlumes:=h("output_intermediate_plumes"), _
                                          sourcesN0FldName:=sourcesN0FldName, _
                                 sourcesN0FldName_NH4:=sourcesN0FldName_NH4, _
                                              SourcesMin:=SourcesMin)
                    End If
                    'get the plumes raster
                    Dim plumes_r As IRaster2 = plumes.CalculatePlumes
                    If plumes Is Nothing Then Throw New Exception("There was an error running the Transport module")
                    'Add the NH4 calculation-Converting the plumes_r to NO3 concentration.
                    Dim plumes_NO3 As IRaster2
                    Dim ConverFactortoNO3 As Single = 0.0
                    Dim DecayCoffNH4Calculation As Single = CType(h("decay_coeff_NH4_calculation"), Single)
                    Dim DecayCoffNO3 As Single = CType(h("decay_coeff"), Single)
                    Dim differ_decaycoff_NH4_NO3 As Single = 0.0
                    differ_decaycoff_NH4_NO3 = DecayCoffNH4Calculation - DecayCoffNO3
                    Try
                        If differ_decaycoff_NH4_NO3 <> 0 Then
                            ConverFactortoNO3 = DecayCoffNH4Calculation / differ_decaycoff_NH4_NO3
                        Else
                            Throw New Exception("The differ_decaycoff_NH4_NO3 value must not be equal to zero.")
                        End If
                    Catch ex As Exception
                    End Try
                    Trace.WriteLine("The (k1/k1-k2) equal to  " & ConverFactortoNO3)
                    plumes_NO3 = Utilities.converttoNO3(plumes_r, plumes_r_NH4, ConverFactortoNO3)

                    'save the results
                    m_outparams.Clear()
                    If Not plumes_r_NH4 Is Nothing Then
                        Try
                            Trace.WriteLine("Saving...")

                            'the path should include the file extension already
                            Utilities.saveRasterToFile(plumes_r_NH4, h("raster_output_path_NH4"), m_outparams, rectify:=True)
                            Utilities.DeleteRaster(plumes_r_NH4)
                            'retreive any intermediate calculations. If there are none,
                            'OutParams will at least contain the path of the _info file.
                            'add output NH4_info.shp file, lhz, 05/19/2016
                            For Each param As DictionaryEntry In plumes_NH4.OutParams
                                m_outparams.Add(param.Key, param.Value)
                            Next
                        Catch ex As Exception
                            Trace.WriteLine("couldn't save the output raster to " & h("raster_output_path_NH4"))
                        End Try
                    End If

                    'save the results
                    'm_outparams.Clear() 'output NH4 as layer,lhz,05/19/2016.
                    If Not plumes_NO3 Is Nothing Then
                        Try
                            Trace.WriteLine("Saving...")
                            'the path should include the file extension already
                            Utilities.saveRasterToFile(plumes_NO3, h("raster_output_path"), m_outparams, rectify:=True)
                            Utilities.DeleteRaster(plumes_NO3)
                        Catch ex As Exception
                            Trace.WriteLine("couldn't save the output raster to " & h("raster_output_path"))
                        End Try
                    End If
                Else
                    'End NH4 calculation.
                    Trace.WriteLine("Start NO3 calculation without NH4 calculation.")
                    CalculateNO3 = True
                    If Not fc_wb Is Nothing Then
                        plumes = New Transport(ParticleTracks:=fc_paths, Sources:=fc_septic, waterbodies:=fc_wb, _
                                          ax:=h("ax"), ay:=h("ay"), az:=h("az"), _
                                          Y:=h("Y"), Z:=h("Z"), _
                                          MeshCellSize_x:=h("mesh_x"), MeshCellSize_y:=h("mesh_y"), MeshCellSize_z:=h("mesh_z"), _
                                          InitialConcentration:=h("conc_init"), _
                                          InitialConcentrationNO3:=h("conc_init_NO3"), _
                                          CalculatingNO3:=CalculateNO3, _
                                          CalculatingNH4:=NH4Calculationchecked, _
                                          InitialConcentration_CNH4:=h("conc_init_NH4"), _
                                          DecayRateConstant_NH4:=h("decay_coeff_NH4_calculation"), _
                                          DecayRateConstant_NO3:=h("decay_coeff"), _
                                          ThresholdConcentration:=h("conc_thresh"), _
                                              plume_z_max:=h("z_max"), _
                                              plume_z_max_checked:=h("z_max_checked"), _
                                          SolutionTime:=h("solution_time"), _
                                          SolutionType:=h("solution_type"), _
                                          DecayRateConstant:=h("decay_coeff"), _
                                          WarpMethod:=h("warp_method"), _
                                          WarpCtrlPtSpac:=h("warp_ctrlptspac"), _
                                          WarpUseApprox:=h("warp_useapprox"), _
                                          PostProcessing:=h("postprocessing"), _
                                          OutputIntermediateCalcs:=h("output_intermediate"), _
                                          OutputPlumesFile:=h("raster_output_path"), _
                                          VolumeConversionFactor:=h("vol_conversion_fac"), _
                                          DomenicoBoundary:=h("domenico_bdy"), _
                                          MaxMemory:=h("max_mem"), _
                                          OutputIntermediatePlumes:=h("output_intermediate_plumes"), _
                                          sourcesN0FldName:=sourcesN0FldName, _
                                 sourcesN0FldName_NH4:=sourcesN0FldName_NH4, _
                                              SourcesMin:=SourcesMin)
                    Else
                        plumes = New Transport(ParticleTracks:=fc_paths, Sources:=fc_septic, waterbodies:=r_wb, _
                                          ax:=h("ax"), ay:=h("ay"), az:=h("az"), _
                                          Y:=h("Y"), Z:=h("Z"), _
                                          MeshCellSize_x:=h("mesh_x"), MeshCellSize_y:=h("mesh_y"), MeshCellSize_z:=h("mesh_z"), _
                                          InitialConcentration:=h("conc_init"), _
                                          InitialConcentrationNO3:=h("conc_init_NO3"), _
                                          CalculatingNO3:=CalculateNO3, _
                                          CalculatingNH4:=NH4Calculationchecked, _
                                          InitialConcentration_CNH4:=h("conc_init_NH4"), _
                                          DecayRateConstant_NH4:=h("decay_coeff_NH4_calculation"), _
                                          DecayRateConstant_NO3:=h("decay_coeff"), _
                                          ThresholdConcentration:=h("conc_thresh"), _
                                          plume_z_max:=h("z_max"), _
                                          plume_z_max_checked:=h("z_max_checked"), _
                                          SolutionTime:=h("solution_time"), _
                                          SolutionType:=h("solution_type"), _
                                          DecayRateConstant:=h("decay_coeff"), _
                                          WarpMethod:=h("warp_method"), _
                                          WarpCtrlPtSpac:=h("warp_ctrlptspac"), _
                                          WarpUseApprox:=h("warp_useapprox"), _
                                          PostProcessing:=h("postprocessing"), _
                                          OutputIntermediateCalcs:=h("output_intermediate"), _
                                          OutputPlumesFile:=h("raster_output_path"), _
                                          VolumeConversionFactor:=h("vol_conversion_fac"), _
                                          DomenicoBoundary:=h("domenico_bdy"), _
                                          MaxMemory:=h("max_mem"), _
                                          OutputIntermediatePlumes:=h("output_intermediate_plumes"), _
                                          sourcesN0FldName:=sourcesN0FldName, _
                                         sourcesN0FldName_NH4:=sourcesN0FldName_NH4, _
                                              SourcesMin:=SourcesMin)
                    End If
                    'get the plumes raster
                    Dim plumes_r As IRaster2 = plumes.CalculatePlumes
                    If plumes Is Nothing Then Throw New Exception("There was an error running the Transport module")
                    m_outparams.Clear()
                    If Not plumes_r Is Nothing Then
                        Try
                            Trace.WriteLine("Saving...")
                            'the path should include the file extension already
                            Utilities.saveRasterToFile(plumes_r, h("raster_output_path"), m_outparams, rectify:=True)
                            Utilities.DeleteRaster(plumes_r)
                        Catch ex As Exception
                            Trace.WriteLine("couldn't save the output raster to " & h("raster_output_path"))
                        End Try
                    End If
                End If
                'retreive any intermediate calculations. If there are none,
                'OutParams will at least contain the path of the _info file.
                For Each param As DictionaryEntry In plumes.OutParams
                    m_outparams.Add(param.Key, param.Value)
                Next

                'send the results. will at least contain the path of the plumes raster and
                'associated _info file
                aqdnMain.OutParams = m_outparams

            Catch ex As Exception
                'don't throw another exception here since this is not a communications exception
                aqdnMain.CalculationComplete(ex.Message)
                Return
            End Try

        Catch ex As Exception
            'a serious initialization error occured
            Trace.WriteLine(ex)
            MsgBox(ex.ToString)

            'set the error code
            System.Environment.ExitCode = 9
        End Try
    End Sub

    Public Overrides Function InitializeLifetimeService() As Object
        'prevent this remoted object from being destroyed after a period of inactivity
        'see http://blogs.microsoft.co.il/blogs/sasha/archive/2008/07/19/appdomains-and-remoting-life-time-service.aspx
        Return Nothing
    End Function
 

End Class

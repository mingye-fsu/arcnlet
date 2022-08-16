Imports System.Runtime.Remoting.Channels.Ipc
Imports System.Runtime.Remoting.Channels
Imports System.Reflection
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports AqDn
'Imports IAqDnApplication

Public Class FlowWrapper
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
    Public ReadOnly Property OutputParams() As System.Collections.Hashtable Implements IAqDnApplication.IModuleWrapper.OutputParams
        Get
            Return m_outparams
        End Get
    End Property

    ''' <summary>
    ''' this will be set by the main program
    ''' when set, cancellation will be attempted.
    ''' NOT CURRENTLY IMPLEMENTED.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property TransportCancelled() As Boolean Implements IAqDnApplication.IModuleWrapper.TransportCancelled
        Get
            Return m_trackingcancelled
        End Get
        Set(ByVal value As Boolean)
            m_trackingcancelled = True
        End Set
    End Property

    Public Shared Sub run(ByVal serverURI As String)
        Try
            'the trace listener
            Dim tl As TraceOutputForward

            'the reference to the server
            Dim aqdnMain As IAqDnApplication.IModuleRunRemote

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

            '********************************
            'communications has been initialized
            'now proceed with running the calculation
            '********************************

            Dim r_dem, r_hyCon, r_poro, r_wb As IRaster2
            Dim fc_wb As IFeatureClass
            Dim zFactor As Single
            Dim smthFac As Integer
            Dim fillsinks As Boolean
            Dim outmag_fname, outdir_fname As String

            Dim flow As DarcyFlow
            Dim outputs(1) As IRasterDataset

            Try
                'get parameters from the mainUI
                Dim h As Hashtable = aqdnMain.InParams

                'sort the values in the hashtable by key and output
                For Each item As DictionaryEntry In CType(h, IEnumerable).Cast(Of DictionaryEntry).OrderBy(Function(DictEntry) DictEntry.Key)
                    Trace.WriteLine(item.Key & " = " & item.Value)
                Next

                'open get the required inputs


                'open the rasters. The parameter should contain the full path of the raster
                r_dem = Utilities.createRasterFromFile(h("dem"))
                If r_dem Is Nothing Then Throw New Exception("Couldn't open " & h("dem"))
                r_hyCon = Utilities.createRasterFromFile(h("k"))
                If r_hyCon Is Nothing Then Throw New Exception("Couldn't open " & h("k"))
                r_poro = Utilities.createRasterFromFile(h("porosity"))
                If r_poro Is Nothing Then Throw New Exception("Couldn't open " & h("porosity"))

                'open the waterbodies feature class or raster depending on the type
                'Parameter wb should contain the full name and path of the raster or shapefile
                If h.ContainsKey("waterbodies_israster") AndAlso h("waterbodies_israster") = "True" Then
                    If Not h("waterbodies") Is Nothing Then
                        r_wb = Utilities.createRasterFromFile(h("waterbodies"))
                        If r_wb Is Nothing Then Throw New Exception("Couldn't open " & h("waterbodies"))
                        Trace.WriteLine("Opened raster: " & h("waterbodies"))
                    End If
                Else
                    If Not h("waterbodies") Is Nothing Then
                        fc_wb = Utilities.createFeatureClassFromShapeFile(h("waterbodies"))
                        If fc_wb Is Nothing Then Throw New Exception("Couldn't open " & h("waterbodies"))
                        Trace.WriteLine("Opened shapefile class: " & h("waterbodies"))
                    End If
                End If

                zFactor = CType(h("slope_zfactor"), Single)
                smthFac = CType(h("smoothing"), Integer)
                fillsinks = CType(h("fillsinks"), Boolean)
                outmag_fname = h("p_mag")
                outdir_fname = h("p_dir")

                '********************************

                'note, DarcyFlow does not fully support running in this wrapper: outputting intermediate
                'calculations won't work
                flow = New AqDn.DarcyFlow(r_dem, fc_wb, r_hyCon, r_poro, zFactor, smthFac, outmag_fname, outdir_fname, "", fillsinks, "", False)
                outputs = flow.calculateDarcyFlow()
                If outputs Is Nothing Then Throw New Exception("There was an error running the flow module")

                'add the output to the results
                'the key indicates the type of dataset and the name of the dataset.
                'This info is used to retreive the data
                m_outparams.Clear()
                m_outparams.Add("RasterDataset" & "velMag" & Now.Ticks, outputs(0).CompleteName)
                m_outparams.Add("RasterDataset" & "velDir" & Now.Ticks, outputs(1).CompleteName)

                aqdnMain.OutParams = m_outparams

            Catch ex As Exception
                'don't throw another exception here since this is not a communications exception
                aqdnMain.CalculationComplete(ex.Message)
            Finally
                'release resources
                If Not outputs Is Nothing Then
                    ComReleaser.ReleaseCOMObject(outputs(0))
                    ComReleaser.ReleaseCOMObject(outputs(1))
                    outputs = Nothing
                End If
                ComReleaser.ReleaseCOMObject(r_dem)
                ComReleaser.ReleaseCOMObject(r_hyCon)
                ComReleaser.ReleaseCOMObject(r_poro)
                ComReleaser.ReleaseCOMObject(r_wb)
                ComReleaser.ReleaseCOMObject(fc_wb)
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

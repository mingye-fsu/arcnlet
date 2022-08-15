Imports ESRI.ArcGIS.ADF                     'com management
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile         'for opening shapefiles
Imports ESRI.ArcGIS.Geodatabase             'feature classes and cursors
Imports ESRI.ArcGIS.Geometry                'points polylines etc.
Imports ESRI.ArcGIS.GeoAnalyst              'requires spatial analyst
Imports ESRI.ArcGIS.SpatialAnalyst          'hydrology ops
Imports ESRI.ArcGIS.ADF.Connection.Local

''' <summary>
''' Calculates the contaminant plume given a set of particle paths 
''' (generated from the particle tracking module)
''' </summary>
''' <remarks>
''' First, a new instance of this class is created, initializing all the required parameters.  Then,
''' CalculatePlumes is called which returns a Raster representing the concentrations of all the plumes in 
''' the domain
''' </remarks>
Public Class Transport

    Private m_COM As New ComReleaser
    Private m_cancelled As Boolean                  'a flag used to indicate if the user cancelled the operation

    Private m_tracks As IFeatureClass               'particle paths
    Private m_sources As IFeatureClass              'contaminant sources
    Private m_sourcesN0FldName As String            'name of the field in m_sources in which N0 is to be found
    Private m_sourcesN0FldName_NH4 As String
    Private m_SourcesMin As String
    Private m_waterbodies As IFeatureClass          'target water bodies
    Private m_waterbodies_r As IRaster2             'target water bodies raster
    Private m_Dx, m_Dy, m_Dz As Single              'dispersivities
    Private m_k As Single                           '1st order decay coefficient
    Private m_Y, m_Z As Single                      'dimensions of plane for domenico robbins
    Private m_Min As Single                         'the mass input load of the domenico plane (if speficied)
    Private m_meshsize_x As Single                  'cell size of evaluation mesh
    Private m_meshsize_y As Single
    Private m_meshsize_z As Single
    Private m_concInit As Single                    'the initial concentration
    Private m_concInit_NO3 As Single
    Private m_cNO3 As Single
    Private m_calculatingNO3 As Boolean
    Private m_calculatingNH4 As Boolean
    Private m_concInit_NH4 As Single
    Private m_CNH4 As Single
    Private m_KNH4 As Single
    Private m_KNO3 As Single
    Private m_z_max As Single
    Private m_z_max_checked As Boolean

    Private m_concThresh As Single                  'threshold concentration (in units of the inital conc.)
    Private m_time As Single                        'the time to calculate the solution for. If -1, will use the flow path travel time.
    Private m_pathid As Integer                     'the desired path id to track. this will be -1 when tracking all the paths
    Private m_warpnumctrlpts As Integer             'the number of warping control points to use. default 48
    Private m_warpmethod As WarpingMethods.WarpingMethod    'the warping method. default spline
    Private m_warpuseapprox As Boolean              'whether to use an approximate warp (faster). default true
    Private m_postprocamt As PostProcessing.PostProcessingAmount
    Private m_plumesFilename As String              'the file name of the output plumes raster
    Private m_plumesPath As String                  'the directory of the output plumes raster.
    Private m_outputintermediateplumes As Boolean
    Private m_outputintermediate As Boolean
    Private m_outputintermediate_path As String
    Private m_volConversionFac As Single            'cell volume to concnetration volume factor
    Private m_outputintermediate_outputs As Hashtable 'the list of paths to intermediate outputs
    Private m_maxmemory As Integer

    Private m_flowpaths As Hashtable                'holds all the particle paths
    Private m_flowpaths_seg0 As Hashtable

    'array of calculated plumes. orignally, plumes were calculated and placed into this array
    'now, they are calculated and output to raster in one step therefore this array is cleared
    'at each iteration and ever only holds one element at a time. used for its original purpose
    'in calculatePlumes_old
    Private m_plumes As List(Of ContaminantPlume)

    Private m_soltype As SolutionTypes.SolutionType 'which analytical solution to use  
    Private m_domenicoBdy As DomenicoSourceBoundaries.DomenicoSourceBoundary

    Private m_plumesProps As IFeatureClass          'shapefile for output of plume properties

    Private m_idx_pathID As Integer = -1            'keep track of field indeces for writing the info file
    Private m_idx_is2D As Integer = -1
    Private m_idx_decayCoeff As Integer = -1
    Private m_idx_avgVel As Integer = -1
    Private m_idx_ax As Integer = -1
    Private m_idx_ay As Integer = -1
    Private m_idx_az As Integer = -1
    Private m_idx_plumelength As Integer = -1
    Private m_idx_pathlength As Integer = -1
    Private m_idx_plumetime As Integer = -1
    Private m_idx_pathtime As Integer = -1
    Private m_idx_volume As Integer = -1
    Private m_idx_srcAngle As Integer = -1
    Private m_idx_srcConc As Integer = -1
    Private m_idx_threshConc As Integer = -1
    Private m_idx_wbid_plume As Integer = -1
    Private m_idx_wbid_path As Integer = -1
    Private m_idx_sourceY As Integer = -1
    Private m_idx_sourceZ As Integer = -1
    Private m_idx_MeshDx As Integer = -1
    Private m_idx_MeshDy As Integer = -1
    Private m_idx_MeshDz As Integer = -1
    Private m_idx_avgporosity As Integer = -1
    Private m_idx_nextConc As Integer = -1
    Private m_idx_massInRateMT3D As Integer = -1
    Private m_idx_massInRate As Integer = -1
    Private m_idx_massDNRate As Integer = -1
    Private m_idx_warp As Integer = -1
    Private m_idx_PP As Integer = -1
    Private m_idx_volFac As Integer = -1
    Private m_idx_domBdy As Integer = -1    


    ''' <summary>
    ''' after calculation is complete, holds the paths to any intermediate outputs
    ''' If there were no outputs or the user selected not to output intermediate
    ''' calculations, this list will be empty except for the full path of the _info file
    ''' which is always present
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property OutParams() As Hashtable
        Get
            Return m_outputintermediate_outputs
        End Get
    End Property

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="ParticleTracks">The polyline class containing particle paths</param>
    ''' <param name="Sources">A set of points containing the locations of the nitrate sources.
    ''' Each point should also contain a field named N0_Conc indicating the nitrate concentration
    ''' contributed by that source</param>
    ''' <param name="Waterbodies">The target water bodies. If Nothing, then it will be assumed that
    ''' no water bodies are present</param>
    ''' <param name="ax">Dispersivity in the longitudinal direction.  If this parameter is less than zero,
    '''  the value speficied for each source in the <paramref name="Sources"/> attribute table will be used. </param>
    ''' <param name="ay">Dispersivity in the transverse horizontal direction.  If this parameter is less than zero,
    '''  the value speficied for each source in the <paramref name="Sources"/> attribute table will be used. </param>
    ''' <param name="az">Dispersivity in the transverse vertical direction.  If this parameter is less than zero,
    '''  the value speficied for each source in the <paramref name="Sources"/> attribute table will be used. </param>
    ''' <param name="Y">The width of the source plane</param>
    ''' <param name="Z">The depth of the source plane</param>
    ''' <param name="MeshCellSize_x">Mesh size in x</param>
    ''' <param name="MeshCellSize_y">Mesh size in y</param>
    ''' <param name="MeshCellSize_z">Mesh size in z</param>
    ''' <param name="InitialConcentration">The initial concentration to use.  If this parameter is less than zero,
    '''  the value speficied for each source in the <paramref name="Sources"/> attribute table will be used. </param>
    ''' <param name="ThresholdConcentration">The plume cutoff concentration.</param>
    ''' <param name="VolumeConversionFactor">For convenience. Since often the concentration of nitrate in the plumes
    ''' is given with units of volume in Liters, this is the conversion factor from the units of cell volume
    ''' to units of concentration volume.
    ''' E.g. if the linear units of the Plumes raster are in meters , the
    ''' resulting volume units would be m^3.  This conversion factor would then be used to specify the conversion from m^3
    ''' to L, VolumeConversionFactor=1000 since 1 m^3 = 1000L.</param>
    ''' <param name="SolutionTime">Time that will be solved for in the analytical solution. If not specified it means use the path travel time</param>
    ''' <param name="DecayRateConstant">The rate coefficient to use (if using a solution that requires one).  If this parameter is less than zero,
    '''  the value speficied for each source in the <paramref name="Sources"/> attribute table will be used. </param>
    ''' <param name="PathID">The path ID to calculate. If not specified, all paths will be calculated</param>     
    ''' <param name="SolutionType">The analytical solution to use.  Depending on the solution chosen, some 
    ''' parameters will be ignored.  E.g. if a 2D solution is chosen, Z and Dz will not be used. The default is the 2D Modified Domenico solution </param>
    ''' <param name="WarpMethod">The warping method to use to map the plume to the path. Default is a spline warp</param>
    ''' <param name="WarpCtrlPtSpac">The control point spacing to use for warping. A lower number will result
    ''' in more control points, giving a better result at a cost of a slower computation.  Setting this number too high
    ''' or too low may cause undesirable results.  The default is recommended unless problems are observed</param>
    ''' <param name="WarpUseApprox">Use an approximate transform in order to increase computation speed.  The 
    ''' difference in the result between the approximate and exact transform are minimal if the plume
    ''' is large enough however the computation for the approximate transform is significantly faster.</param>
    ''' <param name="PostProcessing">The level of post-processing</param>
    ''' <param name="OutputIntermediateCalcs">Whether to output intermediate calculation rasters 
    ''' (for debugging purposes.) The rasters will be output to the same folder as the input ParticleTracks file</param>
    ''' <param name="OutputPlumesFile">The path (incl. file name and extension) of the location to save the output plumes raster. The info file
    ''' will also be saved to the same location except with an _info suffix</param>
    ''' <param name="DomenicoBoundary">Selects whether to interpret the <paramref name=" Z"/> parameter as either
    ''' the Z dimension of the Domenico plane or as the Mass Input Load of the source plane.</param>
    ''' <remarks>Depending of the selected solution type, parameters that are irrelevant will be ignored</remarks>
    Public Sub New(ByVal ParticleTracks As IFeatureClass, ByVal Sources As IFeatureClass, ByVal Waterbodies As IFeatureClass, _
                   ByVal ax As Single, ByVal ay As Single, _
                   ByVal az As Single, ByVal Y As Single, ByVal Z As Single, _
                   ByVal MeshCellSize_x As Single, ByVal MeshCellSize_y As Single, ByVal MeshCellSize_z As Single, _
                   ByVal InitialConcentration As Single, _
                   ByVal InitialConcentrationNO3 As Single, _
                   ByVal CalculatingNO3 As Boolean, _
                   ByVal CalculatingNH4 As Boolean, _
                   ByVal InitialConcentration_CNH4 As Single, _
                   ByVal DecayRateConstant_NH4 As Single, _
                   ByVal DecayRateConstant_NO3 As Single, _
                   ByVal ThresholdConcentration As Single, _
                   ByVal plume_z_max As Single, _
                   ByVal plume_z_max_checked As Boolean, _
                   ByVal VolumeConversionFactor As Single, _
                   Optional ByVal SolutionTime As Single = -1, _
                   Optional ByVal DecayRateConstant As Single = 0, _
                   Optional ByVal PathID As Integer = -1, _
                   Optional ByVal SolutionType As SolutionTypes.SolutionType = SolutionTypes.SolutionType.ModifiedDomenico2D, _
                   Optional ByVal WarpCtrlPtSpac As Integer = 48, _
                   Optional ByVal WarpMethod As WarpingMethods.WarpingMethod = WarpingMethods.WarpingMethod.Polynomial2, _
                   Optional ByVal WarpUseApprox As Boolean = True, _
                   Optional ByVal PostProcessing As PostProcessing.PostProcessingAmount = PostProcessing.PostProcessingAmount.Medium, _
                   Optional ByVal OutputIntermediateCalcs As Boolean = False, _
                   Optional ByVal OutputPlumesFile As String = "", _
                   Optional ByVal DomenicoBoundary As DomenicoSourceBoundaries.DomenicoSourceBoundary = DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate, _
                   Optional ByVal MaxMemory As Integer = 4400, _
                   Optional ByVal OutputIntermediatePlumes As Boolean = False, _
                   Optional ByVal sourcesN0FldName As String = "N0_conc", _
                   Optional ByVal sourcesN0FldName_NH4 As String = "NH4_conc", _
                   Optional ByVal SourcesMin As String = "Min")
        m_Dy = ay
        m_Dx = ax
        m_Dz = az
        m_Y = Y
        m_Z = Z                     'these values may change later depending on the 
        m_Min = Z                   'DomenicoBoundary parameter value
        m_tracks = ParticleTracks
        m_sources = Sources
        m_sourcesN0FldName = sourcesN0FldName
        m_sourcesN0FldName_NH4 = sourcesN0FldName_NH4
        m_SourcesMin = SourcesMin
        m_waterbodies = Waterbodies
        m_meshsize_x = MeshCellSize_x
        m_meshsize_y = MeshCellSize_y
        m_meshsize_z = MeshCellSize_z
        m_concInit = InitialConcentration
        m_concInit_NO3 = InitialConcentrationNO3
        m_cNO3 = InitialConcentrationNO3
        m_calculatingNO3 = CalculatingNO3
        m_calculatingNH4 = CalculatingNH4
        m_concInit_NH4 = InitialConcentration_CNH4
        m_CNH4 = InitialConcentration_CNH4
        m_KNH4 = DecayRateConstant_NH4
        m_KNO3 = DecayRateConstant_NO3
        m_concThresh = ThresholdConcentration
        m_z_max = plume_z_max
        m_z_max_checked = plume_z_max_checked

        m_plumes = New List(Of ContaminantPlume)
        m_soltype = SolutionType
        m_time = SolutionTime   'will be -1 since the available solution types are all steady state
        m_cancelled = False
        m_pathid = PathID
        m_warpmethod = WarpMethod
        m_warpnumctrlpts = WarpCtrlPtSpac
        m_warpuseapprox = WarpUseApprox
        m_postprocamt = PostProcessing
        m_outputintermediate = OutputIntermediateCalcs
        m_outputintermediateplumes = OutputIntermediatePlumes
        m_outputintermediate_outputs = New Hashtable
        If m_outputintermediate_path = "" Then
            m_outputintermediate_path = CType(m_tracks, IDataset).Workspace.PathName
        End If
        m_plumesFilename = IO.Path.GetFileNameWithoutExtension(OutputPlumesFile.Replace("""", ""))
        m_plumesPath = IO.Path.GetDirectoryName(OutputPlumesFile.Replace("""", ""))
        If m_plumesPath Is Nothing Then Throw New Exception("Please save the file '" & OutputPlumesFile & "' in a subdirectory of the root folder")
        m_k = DecayRateConstant
        m_volConversionFac = VolumeConversionFactor
        m_domenicoBdy = DomenicoBoundary
        m_maxmemory = MaxMemory

        If m_concInit = 0 Then Throw New Exception("Initial concentration must be non-zero")

        'so that the input mass rate calculation gives a value as close as possible to the real value
        'the following should be avoided.        
        If Utilities.DivRem(Y, MeshCellSize_y) <> 0 Then
            Trace.WriteLine("Warning: The source dimension Y=" & Y & " should be an integer multiple of the y-cell size " & MeshCellSize_y)
        End If

        'even if the user has selected a 2D solution, a value for the z mesh size
        'is still required so that the solver functions
        If m_meshsize_z <= 0 Then m_meshsize_z = MeshCellSize_x

        'warn if an unsupported model was selected
        If m_soltype <> SolutionTypes.SolutionType.DomenicoRobbinsSS2D And m_soltype <> SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D Then
            Trace.WriteLine("The selected model '" & m_soltype.ToString & "' is not supported by the conceptual model! Use at your own risk.")
        End If
    End Sub

    ''' <summary>
    ''' This alternative constructor takes in an exisiting water bodies raster. This avoids having to convert
    ''' a water bodies polygon feature class to raster if a raster already exists
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New(ByVal ParticleTracks As IFeatureClass, ByVal Sources As IFeatureClass, ByVal Waterbodies As IRaster2, _
                 ByVal ax As Single, ByVal ay As Single, _
                 ByVal az As Single, ByVal Y As Single, ByVal Z As Single, _
                 ByVal MeshCellSize_x As Single, ByVal MeshCellSize_y As Single, ByVal MeshCellSize_z As Single, _
                 ByVal InitialConcentration As Single, _
                 ByVal InitialConcentrationNO3 As Single, _
                 ByVal CalculatingNO3 As Boolean, _
                 ByVal CalculatingNH4 As Boolean, _
                 ByVal InitialConcentration_CNH4 As Single, _
                 ByVal DecayRateConstant_NH4 As Single, _
                 ByVal DecayRateConstant_NO3 As Single, _
                 ByVal ThresholdConcentration As Single, _
                   ByVal plume_z_max As Single, _
                   ByVal plume_z_max_checked As Boolean, _
                 ByVal VolumeConversionFactor As Single, _
                 Optional ByVal SolutionTime As Single = -1, _
                 Optional ByVal DecayRateConstant As Single = 0, _
                 Optional ByVal PathID As Integer = -1, _
                 Optional ByVal SolutionType As SolutionTypes.SolutionType = SolutionTypes.SolutionType.ModifiedDomenico2D, _
                 Optional ByVal WarpCtrlPtSpac As Integer = 48, _
                 Optional ByVal WarpMethod As WarpingMethods.WarpingMethod = WarpingMethods.WarpingMethod.Polynomial2, _
                 Optional ByVal WarpUseApprox As Boolean = True, _
                 Optional ByVal PostProcessing As PostProcessing.PostProcessingAmount = PostProcessing.PostProcessingAmount.Medium, _
                 Optional ByVal OutputIntermediateCalcs As Boolean = False, _
                 Optional ByVal OutputPlumesFile As String = "", _
                 Optional ByVal DomenicoBoundary As DomenicoSourceBoundaries.DomenicoSourceBoundary = DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate, _
                 Optional ByVal MaxMemory As Integer = 4400, _
                 Optional ByVal OutputIntermediatePlumes As Boolean = False, _
                 Optional ByVal sourcesN0FldName As String = "N0_conc", _
                 Optional ByVal sourcesN0FldName_NH4 As String = "NH4_conc", _
                 Optional ByVal SourcesMin As String = "Min")

        Me.New(ParticleTracks, Sources, CType(Nothing, IFeatureClass), ax, ay, az, Y, Z, MeshCellSize_x, MeshCellSize_y, MeshCellSize_z, _
               InitialConcentration, InitialConcentrationNO3, CalculatingNO3, CalculatingNH4, InitialConcentration_CNH4, DecayRateConstant_NH4, DecayRateConstant_NO3, ThresholdConcentration, plume_z_max, plume_z_max_checked, VolumeConversionFactor, SolutionTime, DecayRateConstant, _
               PathID, SolutionType, WarpCtrlPtSpac, WarpMethod, WarpUseApprox, PostProcessing, OutputIntermediateCalcs, _
               OutputPlumesFile, DomenicoBoundary, MaxMemory, OutputIntermediatePlumes, sourcesN0FldName, sourcesN0FldName_NH4, SourcesMin)

        If Waterbodies Is Nothing Then Throw New Exception("Water bodies raster cannot be nothing")
        m_waterbodies_r = Waterbodies
    End Sub

    ''' <summary>
    ''' Attempts to cancel the entire transport calculation operation
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub cancelTransport()
        Trace.WriteLine("Cancelling transport...")
        m_cancelled = True
    End Sub

    ''' <summary>
    ''' Calculates a plume for each individual source and combines them all into one raster.
    ''' </summary>
    ''' <returns>A raster containing a (planar) 2d cross section of all plumes. Nothing on error </returns>
    ''' <remarks></remarks>
    Public Function CalculatePlumes() As IRaster2
        Utilities.outputSystemInfo()
        Trace.Indent()
        Trace.WriteLine("Calculating plumes...")

        Dim COM As New ComReleaser                          'memory manager
        Dim r As IRaster2 = Nothing                         'output raster
        Try

            'open the workspace for plume to raster
            Dim wf As New RasterWorkspaceFactory
            Dim ws As IRasterWorkspace2
            Dim ws_name As ESRI.ArcGIS.esriSystem.IName = wf.Create(IO.Path.GetTempPath, Now.Ticks & "plumestmp", Nothing, 0)
            ws = ws_name.Open
            'wf = New RasterWorkspaceFactory
            'ws = wf.OpenFromFile(IO.Path.GetTempPath, Nothing)
            COM.ManageLifetime(wf)
            COM.ManageLifetime(ws)
            COM.ManageLifetime(ws_name)

            '**************************************
            'vars for reading the polyline particle paths
            '**************************************
            Dim tseg As IFeature                        'segment
            Dim seg As FlowSegment                      'my segment            
            Dim idx_tracks_totdist As Integer = -1      'indeces into the fields list of the tracks table
            Dim idx_tracks_tottime As Integer = -1      'set by CalculatePlumeSingle
            Dim idx_tracks_segvel As Integer = -1
            Dim idx_tracks_segpor As Integer = -1
            Dim idx_tracks_wbID As Integer = -1
            Dim idx_tracks_pathWbID As Integer = -1
            Dim idx_tracks_pathID As Integer = -1
            Dim idx_tracks_segID As Integer = -1

            '**************************************
            'plume to raster conversion variables
            '**************************************
            Dim tmp_rd As IRasterDataset2
            Dim tmp_r As IRaster2
            Dim old_r As IRaster2
            Dim tmp_rbc As IRasterBandCollection
            Dim tmp_rb As IRasterBand
            Dim tmp_rb_rp As IRawPixels
            Dim tmp_pxEdit As IRasterEdit
            Dim tmp_pxBlock As IPixelBlock3
            Dim tmp_pixels(,) As Single
            Dim xsection(,) As Single                       'plume cross section
            Dim ctrlpts_bdy As List(Of Pnt)                 'plume boudnary control points, row column
            Dim ctrlpts_bdy_frac As List(Of Single)         'plume boudnary control points distance along plume (ratio)
            Dim ctrlpts_bdy_map() As Point                  'plume centerline control points map coordinates            
            Dim ctrlpts_center As List(Of Pnt)              'center line control points
            Dim ctrlpts_center_frac As List(Of Single)
            Dim ctrlpts_center_map() As Point

            Dim featuregeodataset As IGeoDataset = CType(m_tracks, IGeoDataset)
            Dim numcols As Integer
            Dim numrows As Integer
            Dim p As IPnt = New Pnt
            Dim e As New Envelope

            Dim controlpts As IPointCollection4             'the combined control points (center and boundary) 
            Dim targetpts As Polyline              'the combined target points


            Dim r_props As IRasterProps                     'stores the raster properties prior to warping
            Dim r_extent As IEnvelope
            Dim r_xCell As Single
            Dim r_yCell As Single

            Dim mosaicer As MosaicRaster                    'combines the plumes into a single raster
            Dim mosaicer_rc As IRasterCollection
            Dim mosaicer_save As ISaveAs2
            Dim mosaicoperator As SumMosaicOperator

            Dim resampler As IRasterGeometryProc

            Dim iter As Integer = 0
            Dim errorOccurred As Integer = 0                'keeps track of whether an error occurred
            Dim initialMem As Long = 0                      'initial memory usage after loading flowpaths, but before starting plume calc.
            Dim maxmemDelta As Long = 0                     'max amout of memory usage before automatic mosaic+save
            '***********************************************************************

            Trace.Indent()

            '***********************************************************************
            'create the shapefile that will hold the plume info
            '***********************************************************************
            Dim fc_plumesInfo As IFeatureClass = Nothing
            Trace.WriteLine("Creating the info file...")
            Try
                If fc_plumesInfo Is Nothing Then
                    'attempt to create an empty particle track feature class
                    fc_plumesInfo = createNewPlumeDataShapefile(m_plumesPath)
                    If fc_plumesInfo Is Nothing Then
                        Throw New Exception("Could not create the blank shapefile ")
                    End If
                Else
                    Throw New Exception("plume calculation has already been run for this instance.")
                End If
            Catch ex As Exception
                Throw New Exception("error creating info file for plumes.")
            End Try

            Trace.WriteLine("Clearing temp folder " & IO.Path.GetTempPath)
            Utilities.DeleteFilesAndFoldersQuick(IO.Path.GetTempPath)

            'create insert buffer
            Dim fc As IFeatureCursor
            Dim fb As IFeatureBuffer
            fc = fc_plumesInfo.Insert(True)
            fb = fc_plumesInfo.CreateFeatureBuffer
            COM.ManageLifetime(fc)
            COM.ManageLifetime(fb)

            '***********************************************************************
            'initialize a bunch of stuff
            '***********************************************************************         

            'initialize the control point arrays
            ctrlpts_bdy = New List(Of Pnt)
            ctrlpts_center = New List(Of Pnt)
            ctrlpts_bdy_frac = New List(Of Single)
            ctrlpts_center_frac = New List(Of Single)

            'initialize the mosaicker
            'combine the plumes by summing the concentrations
            mosaicer = New MosaicRaster
            mosaicoperator = New SumMosaicOperator
            mosaicer_rc = CType(mosaicer, IRasterCollection)
            mosaicer_save = CType(mosaicer, ISaveAs2)
            CType(mosaicer, IRasterProps).SpatialReference = featuregeodataset.SpatialReference
            CType(mosaicer, IRaster).ResampleMethod = rstResamplingTypes.RSP_NearestNeighbor
            mosaicer.MosaicOperatorType = rstMosaicOperatorType.MT_CUSTOM
            mosaicer.MosaicOperator = mosaicoperator


            'initialize resampler       
            resampler = New RasterGeometryProc
            COM.ManageLifetime(resampler)

            'initializes the flowpaths table
            m_flowpaths = New Hashtable
            m_flowpaths_seg0 = New Hashtable

            '***********************************************************************
            'read the segments feature class (flowpaths)
            '***********************************************************************
            Trace.WriteLine("Reading flow paths...")

            Dim fcur As IFeatureCursor
            Dim query As String
            Dim segments As New List(Of FlowSegment)

            'open up a feature cursor to select pathId's >= 0
            'if no path is specified (-1) then select all paths
            If m_pathid = -1 Then
                query = """PathID"" >= 0"
            Else
                query = """PathID"" = " & m_pathid
            End If
            fcur = Utilities.getCursor(m_tracks, query)
            If fcur Is Nothing Then Throw New Exception("Error reading the flow paths")
            COM.ManageLifetime(fcur)

            'find indeces            
            tseg = fcur.NextFeature
            If tseg Is Nothing Then Throw New Exception("No flow paths found")
            idx_tracks_pathID = tseg.Fields.FindField("PathID")
            If idx_tracks_pathID < 0 Then Throw New Exception("Field PathID not found in " & m_tracks.AliasName)
            idx_tracks_segID = tseg.Fields.FindField("SegID")
            If idx_tracks_segID < 0 Then Throw New Exception("Field SegID not found in " & m_tracks.AliasName)
            idx_tracks_totdist = tseg.Fields.FindField("TotDist")
            If idx_tracks_totdist < 0 Then Throw New Exception("Field TotDist not found in " & m_tracks.AliasName)
            idx_tracks_tottime = tseg.Fields.FindField("TotTime")
            If idx_tracks_tottime < 0 Then Throw New Exception("Field TotTime not found in " & m_tracks.AliasName)
            idx_tracks_segvel = tseg.Fields.FindField("SegVel")
            If idx_tracks_segvel < 0 Then Throw New Exception("Field SegVel not found in " & m_tracks.AliasName)
            idx_tracks_wbID = tseg.Fields.FindField("WBId")
            If idx_tracks_wbID < 0 Then Throw New Exception("Field WBId not found in " & m_tracks.AliasName)
            idx_tracks_segpor = tseg.Fields.FindField("SegPrsity")
            If idx_tracks_segpor < 0 Then Throw New Exception("Field SegPrsity not found in " & m_tracks.AliasName)
            idx_tracks_pathWbID = tseg.Fields.FindField("PathWBId")
            If idx_tracks_pathWbID < 0 Then Throw New Exception("Field PathWBId not found in " & m_tracks.AliasName)
            While Not tseg Is Nothing
                seg = New FlowSegment
                seg.shape = CType(tseg.Shape, ESRI.ArcGIS.esriSystem.IClone).Clone
                seg.totDist = tseg.Value(idx_tracks_totdist)
                seg.totTime = tseg.Value(idx_tracks_tottime)
                seg.segVel = tseg.Value(idx_tracks_segvel)
                seg.segPor = tseg.Value(idx_tracks_segpor)
                seg.pathID = tseg.Value(idx_tracks_pathID)
                seg.segID = tseg.Value(idx_tracks_segID)
                seg.wbID = tseg.Value(idx_tracks_wbID)
                seg.PathWbID = tseg.Value(idx_tracks_pathWbID)
                segments.Add(seg)
                If seg.segID = 0 Then m_flowpaths_seg0.Add(seg.pathID, CType(seg.shape, ESRI.ArcGIS.esriSystem.IClone).Clone)
                tseg = fcur.NextFeature
            End While

            Trace.WriteLine("Building flowpaths...")

            'sort the segments by pathid and segid (should already be sorted but do this just in case)
            Dim query2 = From a_segment In segments _
                         Order By a_segment.pathID Ascending, a_segment.segID Ascending

            Dim sortedsegments As List(Of FlowSegment) = query2.ToList()
            Dim path As FlowPath = Nothing
            Dim prevPathID As Integer = -99
            For i As Integer = 0 To sortedsegments.Count - 1
                If prevPathID <> sortedsegments(i).pathID Then
                    'save path
                    If Not path Is Nothing Then
                        m_flowpaths.Add(path.PathID, path)
                    End If

                    'create a new flow path
                    path = New FlowPath(sortedsegments(i).pathID, sortedsegments(i).PathWbID)
                    prevPathID = path.PathID
                End If

                'add the ith segment to the current flowpath
                path.AddSegment(sortedsegments(i))

                If i = sortedsegments.Count - 1 Then
                    'if we're on the last segment of the last path, the loop won't iterate again
                    'therefore must save the flow path here
                    m_flowpaths.Add(path.PathID, path)
                End If
            Next
            segments.Clear()
            sortedsegments.Clear()
            segments = Nothing
            sortedsegments = Nothing

            '***********************************************************************

            'initialize wb raster
            'read the water bodies and convert to raster.
            'set the cell size to the minium size of the input.
            If Not m_waterbodies Is Nothing Then
                Trace.WriteLine("Converting '" & m_waterbodies.AliasName & "' to raster...")
                m_waterbodies_r = Utilities.FeatureclassToRaster(shp:=m_waterbodies, cellsz:=Math.Max(Math.Min(m_meshsize_x, m_meshsize_y), 1))
                If m_waterbodies_r Is Nothing Then Throw New Exception("Couldn't convert the water bodies to raster")
                If m_outputintermediate Then Utilities.saveRasterToFile(m_waterbodies_r, IO.Path.Combine(m_outputintermediate_path, Now.Ticks & "wb_r.img"), m_outputintermediate_outputs)
            Else
                Trace.Write("Using water bodies raster " & m_waterbodies_r.RasterDataset.CompleteName)
            End If


            '***********************************************************************
            'begin calculation for each path id
            '***********************************************************************

            'get source fid's. these will be matched to flow path PathId's
            If m_pathid = -1 Then
                query = """FID"" >= 0"
            Else
                query = """FID"" = " & m_pathid
            End If
            Dim q As IQueryFilter = New QueryFilter With {.WhereClause = query}
            fcur = Utilities.getCursor(m_sources, query)
            If fcur Is Nothing Then Throw New Exception("Sources feature cursor is nothing")
            COM.ManageLifetime(fcur)
            Dim numUniqueVals As Integer = 0
            Dim uniqueValueCount As Integer = 0
            Dim sortedkeys(m_sources.FeatureCount(q) - 1) As Integer
            Dim point As IFeature = fcur.NextFeature
            If point Is Nothing Then Throw New Exception("No sources found!")
            Try
                While Not point Is Nothing
                    sortedkeys(uniqueValueCount) = point.OID
                    uniqueValueCount = uniqueValueCount + 1
                    point = fcur.NextFeature
                End While
            Catch ex As Exception
                Trace.WriteLine("Error reading sources file!")
            End Try
            If uniqueValueCount <> m_sources.FeatureCount(q) Then Throw New Exception("Sources calculated feature count is different than reported feature count! File may be corrupt.")
            Array.Sort(sortedkeys)
            ComReleaser.ReleaseCOMObject(q)

            initialMem = Process.GetCurrentProcess.PrivateMemorySize64

            Dim mm As Integer
            If initialMem / 1048576L <= 300 Then
                'reserve a minimum amount of memory for in-memory plumes when the 
                'initial memory usage is <=300 mb
                mm = 600
            Else
                'for higher initialMem, reserve a maximum of 333mb for in-memory plumes
                'memory usage is capped at 800mb for in memory plumes before auto plume save+mosaic
                mm = m_maxmemory
            End If
            maxmemDelta = (mm - (initialMem / 1048576L)) / 1.1
            Trace.WriteLine("Init. Mem:  " & initialMem / 1048576L & " MB    Max mem: " & mm & "    Max. Delta: " & maxmemDelta & " MB")

            For Each pathID As Integer In sortedkeys
                If m_cancelled Then Exit For
                numUniqueVals = numUniqueVals + 1
                Trace.WriteLine("Processing path " & numUniqueVals & " of " & uniqueValueCount)
                If Not CalculatePlumeSingle(pathID) Then
                    'if an error occurred skip this plume
                    errorOccurred = errorOccurred + 1
                ElseIf Not m_cancelled Then
                    '*********************************************************************************
                    ' convert the plume to raster
                    '*********************************************************************************

                    'get the plume
                    Dim plume As ContaminantPlume = m_plumes(0)

                    Try

                        Trace.WriteLine("Computing X-section...")
                        'get a plume cross secton                                                
                        xsection = plume.getPlumeSectXY0(ctrlpts_bdy, ctrlpts_center, ctrlpts_bdy_frac, ctrlpts_center_frac, m_warpnumctrlpts)

                        If (ctrlpts_bdy.Count + ctrlpts_center.Count < 10) Or xsection Is Nothing Then
                            Trace.WriteLine("Skipped plume with PathID " & plume.PathID & " because there are not enough control points")
                            errorOccurred = errorOccurred + 1
                        Else

                            '********************************************************
                            'create a dataset that will hold the cross section
                            'each cell in the dataset corresponds to a cell of the plume
                            '*********************************************************
                            'create a temporary raster and write the plume to it
                            'the coordinate system for the pixel blcok starts at the top left corner
                            'with +x to the right and +y down
                            numcols = plume.PlumeWidthCells
                            numrows = plume.PlumeLengthCells
                            tmp_rd = ws.CreateRasterDataset("", "MEM", _
                                                   plume.SourceLocation, _
                                                   numrows, _
                                                   numcols, _
                                                   m_meshsize_x, m_meshsize_y, 1, rstPixelType.PT_FLOAT, _
                                                   featuregeodataset.SpatialReference, _
                                                   False)
                            tmp_r = tmp_rd.CreateFullRaster
                            tmp_rbc = CType(tmp_r, IRasterBandCollection)
                            tmp_rb = tmp_rbc.Item(0)
                            tmp_rb_rp = CType(tmp_rb, IRawPixels)
                            tmp_pxEdit = CType(tmp_r, IRasterEdit)
                            CType(tmp_r, IRasterProps).NoDataValue = 0
                            p.SetCoords(numrows, numcols)
                            tmp_pxBlock = tmp_rb_rp.CreatePixelBlock(p)
                            tmp_pixels = tmp_pxBlock.PixelData(0)

                            'copy the plume cross section to the raster
                            tmp_pxBlock.PixelData(0) = xsection
                            p.SetCoords(0, 0)
                            tmp_pxEdit.Write(p, tmp_pxBlock)

                            'end cross section
                            '***********************************************

                            '***********************************************
                            'add the plume info
                            '***********************************************
                            Trace.WriteLine("Saving info...")
                            addPlumeData(fb, plume, xsection)
                            fc.InsertFeature(fb)

                            'write every 20 iterations
                            If numUniqueVals Mod 20 = 0 Then
                                fc.Flush()
                            End If
                            '***********************************************

                            'convert the control points from grid to map coords
                            ctrlpts_bdy_map = plumeCoordsToMapCoords(ctrlpts_bdy, tmp_r)
                            ctrlpts_center_map = plumeCoordsToMapCoords(ctrlpts_center, tmp_r)

                            'map the plume points to the path points
                            'the function will convert the list of control pts into a form we can use for warping
                            controlpts = New Polyline
                            COM.ManageLifetime(controlpts)
                            targetpts = getTargetPointsLocationsFromPath(plume.PathID, _
                                                                         ctrlpts_bdy_map, _
                                                                         ctrlpts_bdy_frac.ToArray, _
                                                                         ctrlpts_center_map, _
                                                                         ctrlpts_center_frac.ToArray, _
                                                                         plume.PlumeLength, _
                                                                         controlpts)

                            If m_outputintermediateplumes Then
                                'if there was an error getting the target points, just output the source points
                                If Not targetpts Is Nothing Then
                                    outputWarpPointsToShapefile(controlpts, targetpts, plume.PathID, m_outputintermediate_path)
                                Else
                                    outputWarpPointsToShapefile2(ctrlpts_bdy_map, ctrlpts_center_map, plume.PathID, m_outputintermediate_path)
                                End If

                                Dim fname As String = IO.Path.Combine(m_outputintermediate_path, plume.PathID & "_" & Now.Ticks & "_r.img")
                                Utilities.saveRasterToFile(tmp_r, fname, m_outputintermediate_outputs)
                            End If

                            Trace.WriteLine("Warping...")
                            '********************************************************
                            'warp               
                            '*********************************************************
                            Dim xform As IGeodataXform = Nothing
                            'if the selected transform fails, try the 2nd order and 1st order
                            'polly transforms. TODO: Clean up this code to avoid reduncancy
                            Dim old_r2 As IRaster2 = Nothing
                            Try
                                old_r2 = CType(tmp_r, ESRI.ArcGIS.esriSystem.IClone).Clone

                                'get the original raster properties
                                r_props = tmp_r
                                r_extent = r_props.Extent
                                r_xCell = r_props.MeanCellSize.X
                                r_yCell = r_props.MeanCellSize.Y

                                'define a transformation.
                                xform = setupPlumeTransform(controlpts, targetpts, m_warpmethod, r_xCell, r_yCell)
                                tmp_r.GeodataXform = xform

                                'transform cell size first, then extent. The sequence matters
                                '*Disable transforming the cell size since it seems to give a bit better results*
                                'xform.TransformCellsize(esriTransformDirection.esriTransformForward, r_xCell, r_yCell, r_extent)
                                xform.TransformExtent(esriTransformDirection.esriTransformForward, r_extent)

                                'Put the transformed extent and cell size on the raster and saveas
                                r_props.Extent = r_extent
                                r_props.Width = Math.Round(r_extent.Width / r_xCell)
                                r_props.Height = Math.Round(r_extent.Height / r_yCell)
                                r_props.SpatialReference = featuregeodataset.SpatialReference

                                'important: save before adding to the output raster so the warp is applied.                            
                                old_r = tmp_r
                                tmp_r = CType(CType(tmp_r, ISaveAs2).SaveAs("", Nothing, "MEM"), IRasterDataset2).CreateFullRaster
                                ComReleaser.ReleaseCOMObject(old_r)

                                '**********
                                'a couple of very important checks to make sure the warp worked
                                'these are important for spline warp b/c spline is really flakey with short plumes
                                CType(targetpts, IGeometry).QueryEnvelope(e)
                                If Not CType(e, IRelationalOperator).Within(r_extent) Then
                                    Trace.WriteLine("Warped raster does not contain all target points")
                                    Throw New Exception
                                End If

                                'if the plume is empty, there was an error warping
                                If Utilities.isRasterEmpty(tmp_r) Then
                                    Trace.WriteLine("Warped raster was empty.")
                                    Throw New Exception
                                End If

                                '***********
                            Catch ex As Exception
                                Try
                                    If Not m_warpmethod = WarpingMethods.WarpingMethod.Polynomial2 Then
                                        Trace.WriteLine("Warp '" & [Enum].GetName(GetType(WarpingMethods.WarpingMethod), m_warpmethod) & "' failed.  Reverting to '" & [Enum].GetName(GetType(WarpingMethods.WarpingMethod), WarpingMethods.WarpingMethod.Polynomial2) & "'")

                                        'get the original raster properties
                                        r_props = old_r2
                                        r_extent = r_props.Extent
                                        r_xCell = r_props.MeanCellSize.X
                                        r_yCell = r_props.MeanCellSize.Y

                                        xform = setupPlumeTransform(controlpts, targetpts, WarpingMethods.WarpingMethod.Polynomial2, r_xCell, r_yCell)
                                        old_r2.GeodataXform = xform
                                        'xform.TransformCellsize(esriTransformDirection.esriTransformForward, r_xCell, r_yCell, r_extent)
                                        xform.TransformExtent(esriTransformDirection.esriTransformForward, r_extent)
                                        r_props.Extent = r_extent
                                        r_props.Width = Math.Round(r_extent.Width / r_xCell)
                                        r_props.Height = Math.Round(r_extent.Height / r_yCell)
                                        r_props.SpatialReference = featuregeodataset.SpatialReference

                                        ComReleaser.ReleaseCOMObject(tmp_r)
                                        tmp_r = CType(CType(old_r2, ISaveAs2).SaveAs("", Nothing, "MEM"), IRasterDataset2).CreateFullRaster
                                        ComReleaser.ReleaseCOMObject(old_r2)
                                    End If
                                Catch ex1 As Exception
                                    Try
                                        If Not m_warpmethod = WarpingMethods.WarpingMethod.Polynomial1 Then
                                            Trace.WriteLine("Warp '" & [Enum].GetName(GetType(WarpingMethods.WarpingMethod), WarpingMethods.WarpingMethod.Polynomial2) & "' failed.  Reverting to '" & [Enum].GetName(GetType(WarpingMethods.WarpingMethod), WarpingMethods.WarpingMethod.Polynomial1) & "'")

                                            'get the original raster properties
                                            r_props = old_r2
                                            r_extent = r_props.Extent
                                            r_xCell = r_props.MeanCellSize.X
                                            r_yCell = r_props.MeanCellSize.Y

                                            xform = setupPlumeTransform(controlpts, targetpts, WarpingMethods.WarpingMethod.Polynomial1, r_xCell, r_yCell)
                                            old_r2.GeodataXform = xform
                                            'xform.TransformCellsize(esriTransformDirection.esriTransformForward, r_xCell, r_yCell, r_extent)
                                            xform.TransformExtent(esriTransformDirection.esriTransformForward, r_extent)
                                            r_props.Extent = r_extent
                                            r_props.Width = Math.Round(r_extent.Width / r_xCell)
                                            r_props.Height = Math.Round(r_extent.Height / r_yCell)
                                            r_props.SpatialReference = featuregeodataset.SpatialReference

                                            ComReleaser.ReleaseCOMObject(tmp_r)
                                            tmp_r = CType(CType(old_r2, ISaveAs2).SaveAs("", Nothing, "MEM"), IRasterDataset2).CreateFullRaster
                                            ComReleaser.ReleaseCOMObject(old_r2)
                                        End If
                                    Catch ex2 As Exception
                                        Throw New Exception("Warp failed: " & ex2.Message)
                                    End Try
                                End Try
                            End Try
                            'end warp
                            '**************************************************************

                            If m_outputintermediateplumes Then
                                Dim fname As String = IO.Path.Combine(m_outputintermediate_path, plume.PathID & "_" & Now.Ticks & "_r_warped.img")
                                Utilities.saveRasterToFile(tmp_r, fname, m_outputintermediate_outputs)
                            End If

                            'the full post processing consists of cutting each plume individually
                            'this takes care of some special cases
                            If m_postprocamt = PostProcessing.PostProcessingAmount.Full Then
                                If Not m_waterbodies_r Is Nothing Then

                                    Trace.WriteLine("Post processing...")
                                    old_r = tmp_r
                                    tmp_r = snipTheTip(tmp_r, plume.PathID)
                                    ComReleaser.ReleaseCOMObject(old_r)
                                    If tmp_r Is Nothing Then
                                        Throw New Exception("Error during plume raster post-processing")
                                    End If
                                End If
                            End If


                            '**************************************************************
                            'add plume to output raster
                            '*************************************************************

                            'add the calculated plume to the raster of calculated plumes.
                            'dont release the raster since the mosaicer still needs it
                            mosaicer_rc.Append(tmp_r)
                            If (Process.GetCurrentProcess.PrivateMemorySize64 - initialMem) / 1048576L >= maxmemDelta Then
                                'If True Then
                                Trace.WriteLine("Saving...")

                                'save                                
                                Dim tmp_ds As IRasterDataset2 = mosaicer_save.SaveAs(IO.Path.Combine(IO.Path.GetTempPath, Now.Ticks & ".img"), Nothing, "IMAGINE Image")
                                old_r = r
                                r = tmp_ds.CreateFullRaster

                                Dim r_names As New List(Of String)
                                Dim r_name As String
                                Dim r_rc As IRaster2
                                Dim r_ds As IRasterDataset
                                For i As Integer = 0 To mosaicer_rc.RasterCount - 1
                                    r_rc = mosaicer_rc.Get(i)
                                    r_ds = r_rc.RasterDataset
                                    r_name = String.Copy(r_ds.CompleteName)
                                    r_names.Add(r_name)
                                    ComReleaser.ReleaseCOMObject(r_ds)
                                    ComReleaser.ReleaseCOMObject(r_rc)
                                    r_rc = Nothing
                                    r_ds = Nothing
                                    r_name = Nothing
                                Next

                                'clean up to save memory. Emptying the mosaic raster collection
                                'gets rid of all the temp. rasters. huge memory savings
                                mosaicer_rc.Empty()

                                'delete the temp raster from the previous save. 
                                Utilities.DeleteRaster(old_r)

                                'release the memory of other temp objects
                                ComReleaser.ReleaseCOMObject(tmp_ds)
                                ComReleaser.ReleaseCOMObject(tmp_r)
                                ComReleaser.ReleaseCOMObject(mosaicer)
                                tmp_ds = Nothing
                                mosaicer = Nothing
                                mosaicer_rc = Nothing
                                mosaicer_save = Nothing

                                For Each name As String In r_names
                                    Utilities.DeleteRasterByName(name)
                                    Try
                                        Utilities.DeleteFilesAndFoldersQuick(IO.Path.GetDirectoryName(name))
                                    Catch ex As Exception
                                    End Try
                                Next

                                're-initialize the mosaicker to work around an ArcGIS bug where
                                'the extent of the mosaicked rasters is set only to the extent of the
                                'first added raster.  Note this doesn't happen when only a single mosaic
                                'operation is carried out (ie. only a single SaveAs call) on the same mosaicker                            
                                mosaicer = New MosaicRaster
                                mosaicoperator = New SumMosaicOperator
                                mosaicer_rc = CType(mosaicer, IRasterCollection)
                                mosaicer_save = CType(mosaicer, ISaveAs2)
                                CType(mosaicer, IRasterProps).SpatialReference = featuregeodataset.SpatialReference
                                CType(mosaicer, IRaster).ResampleMethod = rstResamplingTypes.RSP_NearestNeighbor
                                mosaicer.MosaicOperatorType = rstMosaicOperatorType.MT_CUSTOM
                                mosaicer.MosaicOperator = mosaicoperator

                                'append our newly created intermediate raster.
                                mosaicer_rc.Append(r)
                            End If


                            '**************************************************************
                            'clean up
                            '**************************************************************
                            ComReleaser.ReleaseCOMObject(controlpts)
                            ComReleaser.ReleaseCOMObject(targetpts)
                            ComReleaser.ReleaseCOMObject(tmp_rd)
                            ComReleaser.ReleaseCOMObject(tmp_pxBlock)
                            ComReleaser.ReleaseCOMObject(tmp_pxEdit)
                            ComReleaser.ReleaseCOMObject(tmp_rb)
                            ComReleaser.ReleaseCOMObject(tmp_rb_rp)
                            ComReleaser.ReleaseCOMObject(tmp_rbc)
                            ComReleaser.ReleaseCOMObject(xform)
                            tmp_pixels = Nothing
                            ctrlpts_bdy.ForEach(AddressOf ComReleaser.ReleaseCOMObject)
                            Array.ForEach(ctrlpts_bdy_map, AddressOf ComReleaser.ReleaseCOMObject)
                            ctrlpts_bdy_map = Nothing
                            ctrlpts_center.ForEach(AddressOf ComReleaser.ReleaseCOMObject)
                            Array.ForEach(ctrlpts_center_map, AddressOf ComReleaser.ReleaseCOMObject)

                            'save some memory. plume is no longer reusable
                            plume.deleteData()
                            '**************************************************************

                        End If 'end ctrlpts<10

                    Catch ex As Exception
                        errorOccurred = errorOccurred + 1
                        Trace.WriteLine("Error processing plume with PathID " & plume.PathID & ": " & ex.ToString)
                    End Try

                    m_plumes.Clear()
                    GC.Collect()
                    GC.WaitForPendingFinalizers()

                    'end plume to raster
                    '*******************************************************************************
                End If 'end if not m_cancelled

                Windows.Forms.Application.DoEvents()
            Next

            Trace.Unindent()

            fc.Flush()

            m_flowpaths.Clear()
            m_flowpaths = Nothing

            If numUniqueVals > 0 And Not m_cancelled Then

                If errorOccurred > 0 Then
                    Trace.WriteLine(errorOccurred & " out of " & uniqueValueCount & " plumes were not calculated. Check the log for details")
                End If

                If mosaicer_rc.RasterCount > 0 Then
                    Trace.WriteLine("Saving...")
                    'save                    
                    Dim tmp_ds As IRasterDataset2 = mosaicer_save.SaveAs(IO.Path.Combine(IO.Path.GetTempPath, Now.Ticks & ".img"), Nothing, "IMAGINE Image")
                    old_r = r
                    r = tmp_ds.CreateFullRaster
                    ComReleaser.ReleaseCOMObject(tmp_ds)
                    mosaicer_rc.Empty()
                    Utilities.DeleteRaster(old_r)


                    If m_outputintermediate Then
                        Utilities.saveRasterToFile(r, IO.Path.Combine(m_outputintermediate_path, Now.Ticks & "_unproc_plumes_r.img"), m_outputintermediate_outputs)
                    End If

                    'for the medium post processing, only perform the cut once all the plumes
                    'are calculated.  This will fail in certain cases (e.g. sources on opposite
                    'sides of the river.) in which case Full post processing must be used
                    If m_postprocamt = PostProcessing.PostProcessingAmount.Medium Then
                        If Not m_waterbodies_r Is Nothing Then
                            Trace.WriteLine("Post processing...")
                            tmp_r = snipTheTip(r, m_pathid)
                            Utilities.DeleteRaster(r)
                            If tmp_r Is Nothing Then
                                Utilities.DeleteRaster(r)
                                Throw New Exception("Error during plume raster post-processing")
                            End If
                            r = tmp_r
                            Trace.WriteLine("Post processing...Done")
                        End If
                    End If

                    If m_outputintermediate Then
                        Utilities.saveRasterToFile(r, IO.Path.Combine(m_outputintermediate_path, Now.Ticks & "_postproc_plumes_r.img"), m_outputintermediate_outputs)
                    End If

                    'resample to original cell size
                    resampler.Resample(rstResamplingTypes.RSP_NearestNeighbor, Math.Min(m_meshsize_x, m_meshsize_y), r)

                    old_r = r
                    r = Utilities.saveRasterToFile(r, IO.Path.Combine(IO.Path.GetTempPath, Now.Ticks & ".img"), outputTrace:=False, rectify:=True)
                    Utilities.DeleteRaster(old_r)
                    old_r = Nothing

                    If m_outputintermediate Then
                        Utilities.saveRasterToFile(r, IO.Path.Combine(m_outputintermediate_path, Now.Ticks & "_postprocres_plumes_r.img"), m_outputintermediate_outputs)
                    End If
                End If
            Else
                Trace.WriteLine("No plumes detected or plume calculation was cancelled")
                ComReleaser.ReleaseCOMObject(r)
                r = Nothing
            End If

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            r = Nothing
        End Try

        GC.Collect()
        GC.WaitForPendingFinalizers()
        Trace.WriteLine("Calculating plumes... Done")
        Trace.Unindent()
        Return r
    End Function


    ''' <summary>
    ''' Calculates a plume for a single particle path
    ''' </summary>
    ''' <param name="PathID">The ID of the path to calculate the plume for. Obtained from the particle tracks file</param>    
    ''' <returns>true on ok, false on error</returns>
    ''' <remarks></remarks>
    Private Function CalculatePlumeSingle(ByVal PathID As Integer) As Boolean
        Trace.Indent()
        Trace.WriteLine("Calculating plume with PathID: " & PathID)

        Dim ret As Boolean = True
        Dim COM As New ComReleaser
        Dim sw As New Stopwatch

        'used to distiguish exceptions that are not severe in the grand scheme of things
        Dim exceptionIsWarning As Boolean = False

        sw.Start()

        Try

            Dim PlumeFunc As IAnalyticalFunction4D          'the function to use for solving

            'these will be obtained from the shapefile or specified by passing in a value>=0 in the constructor
            Dim concInit As Single                          'THe initial concentration correpsponding to the source that corresponds to this PathID
            Dim k, dispcons, dispcons1, m_a2 As Single                                 'decay coefficient
            Dim dispL, dispTH As Single                     'dispersivities
            Dim srcLocation As IPoint                       'point shape

            Dim path As FlowPath


            srcLocation = Nothing
            Try
                If Not getInputParams1(pathID:=PathID, InitialConc:=concInit, DecayCoeff:=k, DispL:=dispL, dispTH:=dispTH, srcpoint:=srcLocation) Then
                    Throw New Exception("Couldn't get the source parameters")
                End If
                If concInit = 0 Then Throw New Exception("Initial concentration for source with ID: " & PathID & " is zero. Ignoring this source")
                If k < 0 Then Throw New Exception("Decay constant must be >= 0. Ignoring this souce")
                If dispL < 0 Then Throw New Exception("Longitudinal dispersivity must be >=0. Ignoring this source")
                If dispTH < 0 Then Throw New Exception("Transverse horizontal dispersivity must be >=0. Ignoring this source")
                If srcLocation Is Nothing Then Throw New Exception("Source location must be a point shape. Ignoring this source")
                If m_Min < 0 Then Throw New Exception("The input load Min must be >=0. Ignoring this source")

                'make sure that the starting point is not inside a water body.
                'if it is, this will cause problems during post processing
                Dim row, col As Integer
                Dim val As Object
                srcLocation.Project(CType(m_waterbodies_r, IRasterProps).SpatialReference)
                m_waterbodies_r.MapToPixel(srcLocation.X, srcLocation.Y, col, row)
                val = m_waterbodies_r.GetPixelValue(0, col, row)
                'if the pixel value of the water bodies is not NoData or nothing, we are in a water body
                '(same method as the particle tracker)
                If val <> Utilities.getRasterNoDataValue(m_waterbodies_r) And Not val Is Nothing Then
                    Throw New Exception("The source location is inside of a water body")
                End If

                'get the flow path with id PathID
                If Not m_flowpaths.ContainsKey(PathID) Then Throw New Exception("PathID " & PathID & " not found")
                path = m_flowpaths(PathID)
            Catch ex As Exception
                exceptionIsWarning = True
                Throw New Exception(ex.Message)
            End Try

            path.calculatePath(m_time)

#If CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10" Then
            'Debug.Indent()
            'Debug.WriteLine("Start Line pathID: " & path.PathID & vbTab & "Step size: " & path.StepSize & vbTab & "Start angle: " & path.StartAngle)

            'Debug.WriteLine("Path: " & vbTab & "Dist=" & path.PathDist & vbTab & "Time=" & path.PathTime)
            'Debug.WriteLine("Plume: " & vbTab & "Dist=" & path.PlumeDist & vbTab & "Time=" & path.PlumeTime & vbTab & "Avgvel: " & path.AvgVelocity)
            'Debug.Unindent()
#End If
            '*******************************************************
            'calculate the plume
            '*******************************************************

            'initialize the ananlytical function to evaluate
            'use the flow path time if no time was specified
            'get the initial concentration            

            'if Z has not been specified, calculate it now
            'Yan Zhu: when calculating the reactive nitrogen including the ammonia and nitrate, conInit refers to the a1 or a2. 
            '         a1 means the real ammonium concentration but a2 is not the real nitrate concentration. 
            '         the real nitrate ConInit should be used to calculate the m_Z, that is the reason changing the codes herein.


            'By Yan 1027 before modifying.
            'If m_calculatingNH4 Then
            'If m_domenicoBdy <> DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Z Then
            'If Not m_calculatingNO3 Then
            'm_Z = m_Min / (m_volConversionFac * concInit * m_Y * path.AvgPorosity * path.AvgVelocity * (0.5 + 0.5 * Math.Sqrt(1 + (4 * k * dispL) / path.AvgVelocity)))

            'Else
            'Trace.WriteLine(m_concInit_NO3)
            'Trace.WriteLine(m_Min)
            'dispcons = 0.5 * m_CNH4 * (m_KNH4 / (m_KNH4 - k)) * (Math.Sqrt(1 + (4 * k * dispL) / path.AvgVelocity) - Math.Sqrt(1 + (4 * m_KNH4 * dispL) / path.AvgVelocity))
            'dispcons1 = m_concInit_NO3 * (0.5 + 0.5 * Math.Sqrt(1 + (4 * k * dispL) / path.AvgVelocity))
            'Trace.WriteLine(dispcons)
            'm_Z = m_Min / (m_volConversionFac * m_Y * path.AvgPorosity * path.AvgVelocity * Math.Max(dispcons1, (dispcons1 + dispcons)))
            'End If
            'End If
            'Else
            'If m_domenicoBdy <> DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Z Then
            'm_Z = m_Min / (m_volConversionFac * concInit * m_Y * path.AvgPorosity * path.AvgVelocity * (0.5 + 0.5 * Math.Sqrt(1 + (4 * k * dispL) / path.AvgVelocity)))
            'End If
            'End If
            'By Yan above are before modifying.


            'Yan:1027 Using the same z.
            If m_calculatingNH4 Then
                If m_domenicoBdy <> DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Z Then
                    'If Not m_calculatingNO3 Then
                    m_a2 = m_cNO3 + m_KNH4 / (m_KNH4 - m_KNO3) * m_CNH4
                    dispcons = -(m_KNO3 / (m_KNO3 - m_KNH4)) * m_CNH4 * (0.5 + 0.5 * Math.Sqrt(1 + (4 * m_KNH4 * dispL) / path.AvgVelocity))
                    dispcons1 = m_a2 * (0.5 + 0.5 * Math.Sqrt(1 + (4 * m_KNO3 * dispL) / path.AvgVelocity))
                    'Else
                    Trace.WriteLine("Start to calculate m_z")
                    m_Z = m_Min / (m_volConversionFac * m_Y * path.AvgPorosity * path.AvgVelocity * Math.Max(dispcons1, (dispcons1 + dispcons)))
                    'End If
                    If m_Z < 0 Then m_Z = Math.Max(m_Z, 0.0001)
                End If
            Else
                If m_domenicoBdy <> DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Z Then
                    m_Z = m_Min / (m_volConversionFac * concInit * m_Y * path.AvgPorosity * path.AvgVelocity * (0.5 + 0.5 * Math.Sqrt(1 + (4 * k * dispL) / path.AvgVelocity)))
                    If m_Z < 0 Then m_Z = Math.Max(m_Z, 0.0001)
                End If
            End If






            'ensure the mesh sizse is the same as Z (because model is 2D)
            m_meshsize_z = m_Z

            If m_time = -1 Then
                PlumeFunc = setupAnalyticalSoln(path.PathTime, path.AvgVelocity, concInit, k, dispL, dispTH)
            Else
                PlumeFunc = setupAnalyticalSoln(m_time, path.AvgVelocity, concInit, k, dispL, dispTH)
            End If

            'use lists instead of dynamic arrays for cleaner code.  lists automatically resize
            '(something i'd have to do anyways) and the performance hit of using lists vs
            'arrays is small.  Just don't iterate over the list with a foreach since its slower
            'http://benjaminnitschke.com/Blog/post/2009/04/22/For-vs-Foreach-Performance.aspx

            'the concentration at x,y,z where x,y,z correspond to the coordinates of the center of
            'the cubic cell of size m_meshsize.  The coordinate system is centered at the center of the
            'plume source plane of the Domenico Robbins solution.  I.e. (0,0,0) corresponds to the center
            'of the cell of dimensions m_meshsize, centered at the origin.
            'only the positive y and -z axes will be evaluated since the plume is symmetrical
            'the other values can be mirrored.
            Dim c As New List(Of List(Of List(Of Single)))
            c.Capacity = 16384

            'evaluate the solution in increments of m_meshsize in each dimension
            'iterate z in the inner loop, then y, then x. 
            'stop iterating in a given dimension when the concentration drops below
            'the threshold
            Dim x As Single = 0
            Dim y As Single = 0
            Dim z As Single = 0

            'track the number of cells within the threshold
            Dim n_cells As Long = 0
            Dim x_count As Long = 0
            Dim y_count As Long = 0
            Dim z_count As Long = 0

            'temporary variables
            Dim z_list As List(Of Single)
            Dim y_list As List(Of List(Of Single))
            Dim f As Single

            'set to the width of the plume when the plume becomes longer 
            'than the flow path. used when truncating the plume at the water body.
            'so that we have some room to cut
            Dim widthAtPathDist As Single = -1

            'set to the length of the plume when the plume reaches the water body
            'want to save this value because, depending on the postprocessing method
            'the user selects, the plume will continue to be evaluated past the length
            'of the river for visualization.  Need to keep the real length though
            Dim lengthAtPathDist As Single = -1
            Dim n_cellsAtPathDist As Integer = -1

            'this solver assumes that iso-surfaces of the function are symmetrical at least
            'across the y and z axes, that the surface is convex (no holes or dimples on the surface)
            'and that the function is non-zero only in the positive x side of the cartesian space

            'small speed improvement: save the previous iteration capacities
            'to prevent excessive array resizing
            Dim prev_cap_y As Integer = 1024
            Dim prev_cap_z As Integer = 1024

            Do
                y_list = New List(Of List(Of Single))
                y_list.Capacity = prev_cap_y

                'reset the index and coordinate value
                y = 0

                Do
                    z_list = New List(Of Single)
                    z_list.Capacity = prev_cap_z

                    'reset the index and coordinate value
                    z = 0

                    Do
                        f = PlumeFunc.eval(x, y, z)
                        If Single.IsNaN(f) OrElse Single.IsInfinity(f) Then
                            'under normal circumstances, NaN will occur only when x=0 and y or z is exactly Y/2 or Z/2 respectively
                            Exit Do
                        End If



                        If m_calculatingNH4 Then
                            'NH4 calculation
                            If Not m_calculatingNO3 Then
                                If f < m_concThresh Then
                                    Exit Do 'exit if we've passed the threshold
                                End If
                            Else
                                'NO3 calculation.
                                If concInit > 0 Then
                                    If f < m_concThresh Then
                                        Exit Do 'exit if we've passed the threshold
                                    End If
                                Else
                                    If f > -m_concThresh Then 'By Yan: If a2 is negative, then a would be a negative value.
                                        Exit Do
                                    End If
                                End If
                            End If

                        Else
                            'note only calculating NO3.
                            If f < m_concThresh Then
                                Exit Do 'exit if we've passed the threshold
                            End If
                        End If



                        z_list.Add(f)
                        n_cells = n_cells + 1
                        z = z - m_meshsize_z
                    Loop Until False

                    If z_list.Count < 1 Then
                        Exit Do
                    End If

                    z_list.TrimExcess()
                    prev_cap_z = z_list.Count
                    z_count = z_count + z_list.Count

                    y_list.Add(z_list)

                    y = y + m_meshsize_y
                Loop Until False

                If y_list.Count < 1 Then
                    Exit Do
                End If

                'this the other stopping condition (the first stopping condition is if we've
                'dropped below the threshold concentration)
                If x >= path.PathDist Then
                    'if we're inside this IF, that means the plume reached the end of the path
                    'without dropping below the initial concetration.  we must now make a choice
                    'on how to proceed

                    'save the length
                    If lengthAtPathDist = -1 Then lengthAtPathDist = x
                    'save the number of cells
                    If n_cellsAtPathDist = -1 Then n_cellsAtPathDist = n_cells


                    'The first option is if the user selected to do no post processing.
                    'If so, don't bother with any fancy stuff, just cut the plume here
                    If m_postprocamt = PostProcessing.PostProcessingAmount.None Then
                        Exit Do
                    End If

                    'the second option is if the user selected to do some kind of post
                    'processing to make the plume boundary at the water body a bit nicer.
                    'note the condition below only applies if the path ends at a water body. If it doesn't
                    'end at a water body, then just cut the plume here like above
                    If path.PathWBID <> -1 Then
                        'the first time we reach this if, it means we are exactly at the end of the path
                        'Record the plume width at this time.
                        If widthAtPathDist = -1 Then
                            widthAtPathDist = y_list.Count * 2 * m_meshsize_y
                        End If
                        'keep evaluating the solution until we reach a distance of 
                        '1.25 times the width of the plume. Then stop.
                        'This extra evaluation gives a buffer which we can then cut away
                        'pieces from.
                        If x > path.PathDist + widthAtPathDist * 1.25 Then
                            Exit Do
                        End If
                    Else
                        Exit Do
                    End If
                End If

                y_list.TrimExcess()
                prev_cap_y = y_list.Count
                y_count = y_count + y_list.Count

                c.Add(y_list)

                x = x + m_meshsize_x
            Loop Until m_cancelled

            c.TrimExcess()
            x_count = c.Count

            'if we didn't reach the end of the path, set these values to the max values
            If n_cellsAtPathDist = -1 Then n_cellsAtPathDist = n_cells
            If lengthAtPathDist = -1 Then lengthAtPathDist = c.Count * m_meshsize_x

            'take advantage of the assumed symmetry and of the function to get the volume
            'for the 2 positive-x, negative-z octants (we only calculated the first octant)
            Dim plume_vol As Single
            n_cellsAtPathDist = n_cellsAtPathDist * 2
            plume_vol = m_meshsize_x * m_meshsize_y * m_meshsize_z * n_cellsAtPathDist

            'if the plume is 2d report the area instead
            If SolutionTypes.is2D(m_soltype) Then
                plume_vol = m_meshsize_x * m_meshsize_y * n_cellsAtPathDist
            End If

            Dim nextConc As Single = 0
            If c.Count > 1 Then
                If c(1).Count > 0 Then
                    If c(1)(0).Count > 0 Then
                        nextConc = c(1)(0)(0)
                    End If
                End If
            End If

            Dim p As New ContaminantPlume(PathID, path.PathDist, path.PathTime,
                                          m_Y,
                                          m_Z,
                                          path.StartAngle,
                                          c,
                                          PlumeFunc.t,
                                          plume_vol,
                                          path.AvgPorosity,
                                          m_meshsize_x,
                                          m_meshsize_y,
                                          m_meshsize_z,
                                          nextConc,
                                          concInit,
                                          m_concThresh,
                                          k,
                                          dispL, dispTH, m_Dz,
                                          path.AvgVelocity,
                                          path.PathWBID,
                                          lengthAtPathDist,
                                          n_cellsAtPathDist,
                                          m_soltype,
                                          srcLocation,
                                          m_z_max,
                                          m_z_max_checked)
            m_plumes.Add(p)

            sw.Stop()


#If CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10" Then
            'these statements (including all calculations in the arguments) 
            'will be deleted by the compiler when the program is compiled under any other config.

            'each element in x and y contains an integer pointer (in 32bit systems this is 4 bytes)
            'each element in z contains a 32 single precision fp #
            'Dim plume_mem As Long = (x_count + y_count + z_count) * 4   'bytes


            'Debug.Indent()
            'Debug.WriteLine("# Cells: " & n_cells & "  # computed cells: " & n_cells / 4 & "  mesh size: " & m_meshsize_x & ", " & m_meshsize_y & ", " & m_meshsize_z)
            'Debug.WriteLine("Estimated plume memory (sp): " & plume_mem / 1024 & " KB")
            'Debug.WriteLine("Approx volume: " & plume_vol)
            'If TypeOf PlumeFunc Is Hemisphere Then
            '    Debug.WriteLine("True volume: " & CType(PlumeFunc, Hemisphere).Volume)
            '    Debug.WriteLine("% cell volume to total volume: " & (m_meshsize_x * m_meshsize_y * m_meshsize_z) / CType(PlumeFunc, Hemisphere).Volume * 100)
            '    Debug.WriteLine("Difference: " & plume_vol - CType(PlumeFunc, Hemisphere).Volume)
            '    Debug.WriteLine("Difference relative to true vol (%): " & 100 * (plume_vol - CType(PlumeFunc, Hemisphere).Volume) / CType(PlumeFunc, Hemisphere).Volume)
            'End If
            'Debug.WriteLine("plume bounding box  L: " & p.PlumeLength & "   width: " & p.PlumeWidth & "   height: " & p.PlumeHeight)
            'Debug.Unindent()
#End If

        Catch ex As Exception
            If exceptionIsWarning Then
                Trace.WriteLine("[Warning] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            Else
                Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            End If

            ret = False
        End Try

        Trace.WriteLine("Calculation complete (" & sw.ElapsedMilliseconds & " ms)")
        Trace.Unindent()
        Return ret
    End Function


#Region "Helper functions"
    '********************************************************************************************************
    '   Helper functions
    ''********************************************************************************************************
    'lhz, 02/25/2019
    ''' <summary>
    ''' self-defined conditional operators for 'Con'
    ''' </summary>
    Public Function cond_ops_con(ByVal con_r As IRaster2, ByVal true_r As IRaster2) As IRaster2
        Dim rprops As IRasterProps
        Dim pxCursor1, pxCursor2 As IRasterCursor
        Dim result_r As IRaster2 = true_r
        Dim pxEdit As IRasterEdit = CType(result_r, IRasterEdit)           'for editing pixel blocks

        Dim pxBlock1, pxBlock2 As IPixelBlock3
        Dim pixels1, pixels2 As System.Array
        Dim nodata As Single
        rprops = result_r
        nodata = rprops.NoDataValue(0)
        pxCursor1 = con_r.CreateCursorEx(New Pnt With {.X = rprops.Width, .Y = rprops.Height})
        pxCursor2 = result_r.CreateCursorEx(New Pnt With {.X = rprops.Width, .Y = rprops.Height})
        pxCursor1.Reset()
        pxCursor2.Reset()
        Do
            pxBlock1 = pxCursor1.PixelBlock
            pxBlock2 = pxCursor2.PixelBlock
            'get the pixel array of the first raster band
            pixels1 = CType(pxBlock1.PixelData(0), System.Array)
            pixels2 = CType(pxBlock2.PixelData(0), System.Array)
            For i As Integer = 0 To pxBlock1.Width - 1
                For j As Integer = 0 To pxBlock1.Height - 1
                    If pixels1.GetValue(i, j) = 1 Then
                        pixels2.SetValue(nodata, i, j)
                    End If
                Next
            Next
            pxBlock2.PixelData(0) = pixels2
            pxEdit.Write(pxCursor2.TopLeft, pxBlock2)
        Loop While pxCursor1.Next And pxCursor2.Next
        Return result_r
    End Function

    ''' <summary>
    ''' Used in calculatePlumes to truncate the plumes at the target water bodies.
    ''' </summary>
    ''' <param name="plumes"></param>
    ''' <returns>The truncated raster on success. Nothing on error</returns>
    ''' <remarks></remarks>
    Public Function snipTheTip(ByVal plumes As IRaster2, Optional ByVal pathid As Integer = -1) As IRaster2
        Dim ret As IRaster2 = Nothing
        Dim COM As New ComReleaser

        Trace.Indent()

        Try

            Dim mincellsize As Single           'the minimum cell size of the input plume            
            Dim rprops_plumes, rprops_grp, rprops_wb As IRasterProps
            Dim r_tmp1, old_r As IRaster2        'temp rasters
            'Dim r_wb_clip_ds, old_ds As IRasterDataset2
            Dim r_wb_clip As IRaster2

            Dim seg As Polyline                 'the current line segment
            Dim pt1, pt2 As Point               'the start and end of the current segment
            Dim row, col As Integer             'the row and column of the regions raster. used for mapToPixel
            Dim val_grp, val_plume, val_wb As Object     'the value of the pixel at the given row and column            
            Dim isinwb, isinplume As Boolean               'whether the current source is in a waterbody or not
            Dim srcRegions As Hashtable         'list of regions containing a source

            Dim fname As String

            '**************************************************
            'this code for testing purposes only
            'Dim wf As RasterWorkspaceFactory                            'variables for opening the rasters
            'Dim ws As IRasterWorkspace2
            'Dim rds As IRasterDataset2
            'wf = New RasterWorkspaceFactory
            'ws = wf.OpenFromFile("E:\GIS\julington creek", Nothing)
            'rds = ws.OpenRasterDataset("-1_634477448338362824_groups_r.img")
            'r_tmp1 = rds.CreateFullRaster
            'rds = ws.OpenRasterDataset("-1_634477447458050324_wb_clip_r.img")
            'r_wb_clip = rds.CreateFullRaster
            'rds = ws.OpenRasterDataset("-1_634477447588675324_unproc_plumes.img")
            'plumes = rds.CreateFullRaster
            'rprops_wb = CType(r_wb_clip, IRasterProps)
            '**************************************************

            rprops_plumes = CType(plumes, IRasterProps)
            'get the minimum cell size from the input raster
            mincellsize = Math.Min(rprops_plumes.MeanCellSize.X, rprops_plumes.MeanCellSize.Y)

            '#If False Then
            If m_waterbodies_r Is Nothing Then Throw New Exception("Water bodies raster is nothing")

            Trace.WriteLine("Aligning rasters...")

            'clip and align the waterbodies to the subregion of the current plume
            'also resample the plumes to the user's specified plume size so that the
            'rasters align perfectly.
            'Create a new workspace each time. This is for full post processing since
            'there is a limit on how many rasters you can have.  Hopefully, this will avoid
            'the Directory Full (INFDEF) error with large data sets
            '
            'note: the RasterGeometryProc resampler seems to have a memory leak, even though it runs faster
            'than IGeneralizeOp.  Use IRasterGeometryProc since it seems more reliable under Arc10
            'In Arc10 using IGeneralizeOp sometimes causes the raster to not save properly.  

            'Dim rop As ITransformationOp = New RasterTransformationOp
            'Dim rAEnv As IRasterAnalysisEnvironment = rop
            'Dim wsf_out As IWorkspaceFactory2 = New RasterWorkspaceFactory
            'Dim ws_out_name As ESRI.ArcGIS.esriSystem.IName = wsf_out.Create(IO.Path.GetTempPath, Now.Ticks, Nothing, 0)
            'Dim ws_out As IRasterWorkspace = ws_out_name.Open
            'COM.ManageLifetime(rop)
            'COM.ManageLifetime(wsf_out)
            'COM.ManageLifetime(ws_out_name)
            'COM.ManageLifetime(ws_out)

            ''don't set the cell size here. Clip will give an error for some reason.
            'rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            'rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, rprops_plumes.Extent, plumes.RasterDataset)
            'rAEnv.OutWorkspace = ws_out

            'r_wb_clip_ds = rop.Clip(m_waterbodies_r, rprops_plumes.Extent)
            'old_ds = r_wb_clip_ds
            'r_wb_clip_ds = rop.Resample(r_wb_clip_ds, mincellsize, esriGeoAnalysisResampleEnum.esriGeoAnalysisResampleNearest)
            'ComReleaser.ReleaseCOMObject(old_ds)
            'r_wb_clip = r_wb_clip_ds.CreateFullRaster
            'ComReleaser.ReleaseCOMObject(r_wb_clip_ds)
            'rprops_wb = CType(r_wb_clip, IRasterProps)
            'If m_outputintermediate And pathid = -1 Then
            '    fname = IO.Path.Combine(m_outputintermediate_path, pathid & "_" & Now.Ticks & "_wb_clip_r.img")
            '    Utilities.saveRasterToFile(r_wb_clip, fname, m_outputintermediate_outputs)
            'End If

            Dim rOp As IRasterGeometryProc2 = New RasterGeometryProc
            Dim rAEnv As IRasterAnalysisEnvironment
            Dim wsf_out As IWorkspaceFactory2 = New RasterWorkspaceFactory
            Dim ws_out_namePath As String = IO.Path.GetTempPath
            Dim ws_out_nameName As String = Now.Ticks
            Dim ws_out_nameStr As String = IO.Path.Combine(ws_out_namePath, ws_out_nameName)
            Dim ws_out_name As ESRI.ArcGIS.esriSystem.IName = wsf_out.Create(ws_out_namePath, ws_out_nameName, Nothing, 0)
            Dim ws_out As IRasterWorkspace = ws_out_name.Open
            COM.ManageLifetime(rOp)
            COM.ManageLifetime(wsf_out)
            COM.ManageLifetime(ws_out_name)
            COM.ManageLifetime(ws_out)

            rOp.Clip(rprops_plumes.Extent, m_waterbodies_r)
            rOp.Resample(rstResamplingTypes.RSP_NearestNeighbor, mincellsize, m_waterbodies_r)
            r_wb_clip = m_waterbodies_r
            rprops_wb = CType(r_wb_clip, IRasterProps)
            If m_outputintermediate And pathid = -1 Then
                fname = IO.Path.Combine(m_outputintermediate_path, pathid & "_" & Now.Ticks & "_wb_clip_r.img")
                Utilities.saveRasterToFile(r_wb_clip, fname, m_outputintermediate_outputs, rectify:=True)
            End If


            Trace.WriteLine("Grouping regions...")

            Dim cOp As IConditionalOp = New RasterConditionalOp
            Dim mOp As IRasterMakerOp = New RasterMakerOp
            Dim lOp As ILogicalOp = New RasterMathOps
            Dim gOp As IGeneralizeOp = New RasterGeneralizeOp
            Dim const_r0, const_r1 As IRaster2
            Dim tf_r, setnull_wboverlap As IRaster2
            Dim null_r, Notnull_r, null_setnull_wboverlap As IRaster2
            Dim null_plume_r, tmp_plume As IRaster2 'lhz, 02/25/2019
            Dim pre_rg As IRaster2
            COM.ManageLifetime(cOp)
            COM.ManageLifetime(mOp)
            COM.ManageLifetime(lOp)
            rAEnv = cOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out
            rAEnv = mOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out
            rAEnv = lOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out
            rAEnv = gOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out

            'perform the map algebra operation "Regiongroup(   Con(IsNull( SetNull(^ IsNull([wb]), [plumes]  ) ) == 0,1)   ) "
            'In Arc9, the map algebra expressions worked.  In Arc10, they weren't so reliable.
            'hence the switch to the arcobjects version of the above expression.

            null_r = lOp.IsNull(r_wb_clip)
            Notnull_r = lOp.BooleanNot(null_r)
            'setnull_wboverlap = cOp.SetNull(Notnull_r, plumes) 'lhz, 02/25/2019
            'null_setnull_wboverlap = lOp.IsNull(setnull_wboverlap) 'lhz, 02/25/2019
            null_plume_r = lOp.IsNull(plumes) 'lhz, 02/25/2019
            null_setnull_wboverlap = lOp.BooleanOr(Notnull_r, null_plume_r) 'lhz, 02/25/2019

            const_r0 = mOp.MakeConstant(0, True)
            const_r1 = mOp.MakeConstant(1, True)
            tf_r = lOp.EqualTo(null_setnull_wboverlap, const_r0)

            'pre_rg = cOp.Con(tf_r, const_r1) 'lhz, 02/25/2019
            pre_rg = tf_r 'lhz, 02/25/2019

            r_tmp1 = gOp.RegionGroup(pre_rg, False, True, False)

            If r_tmp1 Is Nothing Then Throw New Exception("Error running map algebra expression for region groups")
            ComReleaser.ReleaseCOMObject(null_r)
            ComReleaser.ReleaseCOMObject(Notnull_r)
            'ComReleaser.ReleaseCOMObject(setnull_wboverlap) 'lhz, 02/25/2019
            ComReleaser.ReleaseCOMObject(null_plume_r) 'lhz, 02/25/2019
            ComReleaser.ReleaseCOMObject(null_setnull_wboverlap)
            ComReleaser.ReleaseCOMObject(const_r0)
            ComReleaser.ReleaseCOMObject(const_r1)
            ComReleaser.ReleaseCOMObject(tf_r)
            ComReleaser.ReleaseCOMObject(pre_rg)
            old_r = Nothing
            null_r = Nothing
            Notnull_r = Nothing
            setnull_wboverlap = Nothing
            null_setnull_wboverlap = Nothing
            const_r0 = Nothing
            const_r1 = Nothing
            tf_r = Nothing
            pre_rg = Nothing


            If m_outputintermediate And pathid = -1 Then
                fname = IO.Path.Combine(m_outputintermediate_path, pathid & "_" & Now.Ticks & "_groups_r.img")
                Utilities.saveRasterToFile(r_tmp1, fname, m_outputintermediate_outputs)
            End If
            '#End If


            '******************************************************************
            'find the regions that contain a starting segment
            'use starting segments instead of source locations in case
            'there are more source locations than paths (i.e. when using the 
            'option to calculate only a specified path, given a single pathID)
            '******************************************************************  
            Trace.WriteLine("Finding source regions...")

            Dim grpUniqueVals As Hashtable = Utilities.getRasterUniqueVals(r_tmp1)

            rprops_grp = CType(r_tmp1, IRasterProps)

            Dim pathids() As Integer
            If pathid = -1 Then
                ReDim pathids(m_flowpaths_seg0.Keys.Count - 1)
                m_flowpaths_seg0.Keys.CopyTo(pathids, 0)
            Else
                ReDim pathids(0)
                pathids(0) = pathid
            End If

            srcRegions = New Hashtable
            For Each id As Integer In pathids
                seg = m_flowpaths_seg0(id)
                isinwb = False
                isinplume = True

                pt1 = seg.Point(0)
                pt2 = seg.Point(1)

                'get the value of the region that contains the ending point of the 
                'first segment of the flow Path. Use only the ending point (not starting
                'and ending) because sometimes, the starting point will include a region that is
                'not desired.  The side effect is that in other cases, a small sliver of the
                'beginning of the plume may be removed.
                pt2.Project(rprops_grp.SpatialReference)
                r_tmp1.MapToPixel(pt2.X, pt2.Y, col, row)
                val_grp = r_tmp1.GetPixelValue(0, col, row)
                pt2.Project(rprops_plumes.SpatialReference)
                plumes.MapToPixel(pt2.X, pt2.Y, col, row)
                val_plume = plumes.GetPixelValue(0, col, row)
                pt2.Project(rprops_wb.SpatialReference)
                r_wb_clip.MapToPixel(pt2.X, pt2.Y, col, row)
                val_wb = r_wb_clip.GetPixelValue(0, col, row)

                'check to see if the ending point of the first segment contains a plume
                'the value of the plumes raster canot be less than or equal to zero.  If it is
                'consider it as if there is no plume at that location
                If val_plume Is Nothing _
                       OrElse val_plume = rprops_plumes.NoDataValue(0) _
                       OrElse val_plume <= 0 Then
                    isinplume = False
                End If

                'if the pixel value of the water bodies is not NoData and not nothing and >=0, we are inside a water body
                'Since we used the FID to convert the water bodies, the the value of the raster in the water body should always
                'be >=0. This extra check for >=0 is needed b/c the clipped wb raster can have a different NoData value
                'than the original and ArcGis seems to return weird values if the raster is empty
                If Not val_wb Is Nothing _
                    AndAlso val_wb <> Utilities.getRasterNoDataValue(r_wb_clip) _
                    AndAlso val_wb >= 0 Then
                    isinwb = True
                End If

                'if there is no plume detected, it could be that the flow path consists of a single segmen
                'and the ending point of the segment is inside the water body.  A plume could still be
                'present in this case, therefore now use the starting point to check
                If Not isinplume And isinwb Then
                    isinplume = True

                    pt1.Project(rprops_grp.SpatialReference)
                    r_tmp1.MapToPixel(pt1.X, pt1.Y, col, row)
                    val_grp = r_tmp1.GetPixelValue(0, col, row)
                    pt1.Project(rprops_plumes.SpatialReference)
                    plumes.MapToPixel(pt1.X, pt1.Y, col, row)
                    val_plume = plumes.GetPixelValue(0, col, row)
                    pt1.Project(rprops_wb.SpatialReference)
                    r_wb_clip.MapToPixel(pt1.X, pt1.Y, col, row)
                    val_wb = r_wb_clip.GetPixelValue(0, col, row)


                    If val_plume Is Nothing _
                       OrElse val_plume = rprops_plumes.NoDataValue(0) _
                       OrElse val_plume <= 0 Then
                        isinplume = False
                    End If

                    If Not val_wb Is Nothing _
                    AndAlso val_wb <> Utilities.getRasterNoDataValue(r_wb_clip) _
                    AndAlso val_wb >= 0 Then
                        isinwb = True
                        'Dim idx As Integer = feat.Fields.FindField("PathID")
                        'Trace.WriteLine("Source with FID: " & feat.Value(idx) & " is in the water body")
                        Trace.WriteLine("Source with FID: " & id & " is in the water body")
                    End If
                End If

                If isinplume And Not isinwb And Not val_grp Is Nothing Then
                    'flag this zone so it is not deleted
                    If grpUniqueVals(val_grp) Is Nothing Then
                        grpUniqueVals(val_grp) = CType(1, Byte)
                        If m_outputintermediate Then Trace.WriteLine("src reg: " & val_grp)
                    End If
                End If

                'feat = fcur.NextFeature
                'End While
            Next

            '******************************************************************  
            'Delete regions that don't contain a source from the untruncated
            'raster.
            '******************************************************************  
            Dim numRegDel As Integer
            Dim i As Integer = 0

            'get the number of non-flagged regions
            For Each v In grpUniqueVals.Values
                If v Is Nothing Then numRegDel = numRegDel + 1
            Next

            Dim reg(numRegDel - 1) As Object
            For Each item As DictionaryEntry In grpUniqueVals
                If item.Value Is Nothing Then
                    reg(i) = item.Key
                    i = i + 1
                End If
            Next

            Trace.WriteLine("Deleting non-source regions (#=" & numRegDel & ")")

            'can use a value of 0 for replacewith bc Regiongroup always returns non-floating point values >0
            Dim replacewith As Object = CType(r_tmp1, IRasterProps).NoDataValue(0)
            If TypeOf replacewith Is Byte Then
                replacewith = CType(0, Byte)
            ElseIf TypeOf replacewith Is Integer Then
                replacewith = CType(0, Integer)
            ElseIf TypeOf replacewith Is Short Then
                replacewith = CType(0, Short)
            End If

            If numRegDel > 0 Then
                Utilities.replaceRasterVals(r_tmp1, reg, replacewith)
                'need to save here, else changes from above operation won't be applied
                old_r = r_tmp1
                r_tmp1 = Utilities.saveRasterToFile(r_tmp1, IO.Path.Combine(IO.Path.GetTempPath, Now.Ticks & ".img"), outputTrace:=False)
                Utilities.DeleteRaster(old_r)
                old_r = Nothing
            Else
                Trace.WriteLine("No regions to delete.")
            End If


            Trace.WriteLine("Generating output raster...")
            rAEnv = cOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out
            rAEnv = mOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out
            rAEnv = lOp
            rAEnv.OutSpatialReference = rprops_plumes.SpatialReference
            rAEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, plumes)
            rAEnv.OutWorkspace = ws_out

            const_r0 = mOp.MakeConstant(replacewith, True)

            tf_r = lOp.NotEqual(r_tmp1, const_r0)

            old_r = r_tmp1
            'r_tmp1 = cOp.Con(tf_r, plumes)  'lhz, 02/25/2019
            null_plume_r = lOp.BooleanOr(lOp.IsNull(plumes), lOp.EqualTo(r_tmp1, const_r0)) 'lhz, 02/25/2019
            r_tmp1 = cond_ops_con(null_plume_r, plumes)  'lhz, 02/25/2019

            Utilities.DeleteRaster(old_r)
            ComReleaser.ReleaseCOMObject(const_r0)
            ComReleaser.ReleaseCOMObject(tf_r)
            const_r0 = Nothing
            tf_r = Nothing
            old_r = Nothing

            reg = Nothing
            grpUniqueVals.Clear()
            grpUniqueVals = Nothing

            'use this instead of saveRasterToFile due to a bug in Arc10 where save will enter into an infinite loop for some plumes
            'ret = Utilities.saveRasterToFile(r_tmp1, IO.Path.Combine(IO.Path.GetTempPath, Now.Ticks & ".img"), outputTrace:=False, rectify:=True)
            ret = Utilities.saveRasterToFile(r_tmp1, IO.Path.Combine(ws_out_nameStr, ws_out_nameName & "_plume.img"), outputTrace:=False, rectify:=True)
            old_r = Nothing

            'clean up the wb raster. don't delete r_tmp1 since it is the return value
            'ComReleaser.ReleaseCOMObject(r_wb_clip)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": couldn't post process plume raster: " & ex.ToString)
            ret = Nothing
        End Try

        Trace.Unindent()
        Return ret
    End Function

    ''' <summary>
    ''' used in CalculatePlumeSingle to get the analytical solution parameters for the current contaminiant source
    ''' This allows the user to have potentially different parameters for each source.  If the value is not present
    ''' in the table, the value passed in the constructor is returned.
    ''' </summary>
    ''' <param name="pathID">The pathID that will be used to match against the FID
    ''' in the m_sources point class</param>
    ''' <param name="InitialConc">Output parameter representing the initial concentration</param>    
    ''' <param name="DecayCoeff">Output parameter representing the decay coefficient</param>
    ''' <param name="DispL">Ouput parameter representing the longitudinal dispersivity</param>
    ''' <param name="dispTH">Output parameter representing the transverse horizontal dispersivity.</param>
    ''' <param name="srcpoint" >Outputs a copy of the source location</param>
    ''' <returns>True on OK, false on error</returns>
    ''' <remarks></remarks>
    Private Function getInputParams(ByVal pathID As Integer, ByRef InitialConc As Single, _
                                     ByRef DecayCoeff As Single, ByRef DispL As Single, ByRef dispTH As Single, _
                                     ByRef srcpoint As IGeometry) As Boolean
        getInputParams = True

        Try
            Dim COM As New ComReleaser

            Dim fcur As IFeatureCursor                      'cursor for reading the points
            Dim q As String                                 'query for selecting the points            

            q = """" & m_sources.OIDFieldName & """ = " & pathID
            fcur = Utilities.getCursor(m_sources, q)
            If fcur Is Nothing Then Throw New Exception("Feature cursor is nothing")
            COM.ManageLifetime(fcur)

            'load the first query result. (there should only be 1 since FIDs are unique)
            Dim point As IFeature = fcur.NextFeature
            If point Is Nothing Then Throw New Exception("Source with ID " & pathID & " not found")

            'get the field indeces
            Dim idx_initconc As SByte
            Dim idx_k As SByte
            Dim idx_dispL As SByte
            Dim idx_dispTH As SByte

            'read the values
            idx_initconc = fcur.FindField(m_sourcesN0FldName)
            If idx_initconc < 0 AndAlso m_concInit < 0 Then
                'value was not specified in attributes table or manually
                Throw New Exception("Field " & m_sourcesN0FldName & " not found in " & m_sources.AliasName)
            ElseIf m_concInit > 0 Then
                'value was specified manually. this takes precedence over values in the table
                InitialConc = m_concInit
            Else
                'value was specified in the table
                InitialConc = point.Value(idx_initconc)
            End If

            idx_k = fcur.FindField("decayCoeff")
            If idx_k < 0 AndAlso m_k < 0 Then
                Throw New Exception("Field decayCoeff not found in " & m_sources.AliasName)
            ElseIf m_k >= 0 Then
                DecayCoeff = m_k
            Else
                DecayCoeff = point.Value(idx_k)
            End If

            idx_dispL = fcur.FindField("dispL")
            If idx_dispL < 0 AndAlso m_Dx < 0 Then
                Throw New Exception("Field dispL not found in " & m_sources.AliasName)
            ElseIf m_Dx >= 0 Then
                DispL = m_Dx
            Else
                DispL = point.Value(idx_dispL)
            End If

            idx_dispTH = fcur.FindField("dispTH")
            If idx_dispTH < 0 AndAlso m_Dy < 0 Then
                Throw New Exception("Field dispTH not found in " & m_sources.AliasName)
            ElseIf m_Dy >= 0 Then
                dispTH = m_Dy
            Else
                dispTH = point.Value(idx_dispTH)
            End If

            srcpoint = point.ShapeCopy

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": Couldn't process file '" & m_sources.AliasName & "': " & ex.ToString)
            getInputParams = False
        End Try


        Return getInputParams
    End Function
    ''' <summary>
    ''' used in CalculatePlumeSingle to get the analytical solution parameters for the current contaminiant source
    ''' This allows the user to have potentially different parameters for each source.  If the value is not present
    ''' in the table, the value passed in the constructor is returned.
    ''' </summary>
    ''' <param name="pathID">The pathID that will be used to match against the FID
    ''' in the m_sources point class</param>
    ''' <param name="InitialConc">Output parameter representing the initial concentration</param>    
    ''' <param name="DecayCoeff">Output parameter representing the decay coefficient</param>
    ''' <param name="DispL">Ouput parameter representing the longitudinal dispersivity</param>
    ''' <param name="dispTH">Output parameter representing the transverse horizontal dispersivity.</param>
    ''' <param name="srcpoint" >Outputs a copy of the source location</param>
    ''' <returns>True on OK, false on error</returns>
    ''' <remarks></remarks>
    Private Function getInputParams1(ByVal pathID As Integer, ByRef InitialConc As Single, _
                                     ByRef DecayCoeff As Single, ByRef DispL As Single, ByRef dispTH As Single, _
                                     ByRef srcpoint As IGeometry) As Boolean
        getInputParams1 = True

        Try
            Dim COM As New ComReleaser

            Dim fcur As IFeatureCursor                      'cursor for reading the points
            Dim q As String                                 'query for selecting the points            

            q = """" & m_sources.OIDFieldName & """ = " & pathID
            fcur = Utilities.getCursor(m_sources, q)
            If fcur Is Nothing Then Throw New Exception("Feature cursor is nothing")
            COM.ManageLifetime(fcur)

            'load the first query result. (there should only be 1 since FIDs are unique)
            Dim point As IFeature = fcur.NextFeature
            If point Is Nothing Then Throw New Exception("Source with ID " & pathID & " not found")

            'get the field indeces
            Dim idx_initconc As SByte
            Dim idx_initconc_NH4 As SByte
            Dim idx_min As SByte
            Dim idx_k As SByte
            Dim idx_dispL As SByte
            Dim idx_dispTH As SByte
            Dim m_coeffi As Single
            Dim m_conc_NH4 As Single

            'read the values
            idx_initconc = fcur.FindField(m_sourcesN0FldName)
            idx_initconc_NH4 = fcur.FindField(m_sourcesN0FldName_NH4)
            idx_min = fcur.FindField(m_SourcesMin)

            'add NH4 calculation.
            If m_calculatingNH4 Then
                'NH4 calculation
                If Not m_calculatingNO3 Then
                    If idx_initconc_NH4 < 0 AndAlso m_concInit < 0 Then
                        'value was not specified in attributes table or manually
                        Throw New Exception("Field " & m_sourcesN0FldName_NH4 & " not found in " & m_sources.AliasName)
                    ElseIf m_concInit > 0 Then
                        'value was specified manually. this takes precedence over values in the table
                        InitialConc = m_concInit
                    Else
                        'value was specified in the table
                        InitialConc = point.Value(idx_initconc_NH4)
                        m_CNH4 = point.Value(idx_initconc_NH4)
                    End If

                    'NOTE by Yan 10272014: This is for the calculation of Z.
                    If idx_initconc < 0 AndAlso m_concInit_NO3 < 0 Then
                        Throw New Exception("Field " & m_sourcesN0FldName & " not found in " & m_sources.AliasName)
                    ElseIf m_concInit_NO3 > 0 Then
                        m_cNO3 = m_concInit_NO3
                    Else
                        m_cNO3 = point.Value(idx_initconc)
                    End If

                Else
                    'NO3 calculation
                    If idx_initconc < 0 AndAlso m_concInit < 0 Then
                        'value was not specified in attributes table or manually
                        If m_concInit_NO3 < 0 Then
                            Throw New Exception("Field " & m_sourcesN0FldName & " not found in " & m_sources.AliasName)
                        ElseIf m_concInit_NO3 > 0 Then
                            'note By Yan 2014 1028: handling with mNO3 is homogeous, mNH4 is heteogeous.
                            m_cNO3 = m_concInit_NO3
                            If idx_initconc_NH4 < 0 AndAlso m_concInit_NH4 < 0 Then
                                Throw New Exception("Field " & m_sourcesN0FldName_NH4 & " not found in " & m_sources.AliasName)
                            ElseIf m_concInit_NH4 > 0 Then
                                m_CNH4 = m_concInit_NH4
                                m_cNO3 = m_concInit_NO3
                                m_coeffi = CType(m_KNH4 / (m_KNH4 - m_k), Single)
                                InitialConc = m_cNO3 + m_coeffi * CType(m_CNH4, Single)
                            Else
                                m_CNH4 = point.Value(idx_initconc_NH4)
                                m_coeffi = CType(m_KNH4 / (m_KNH4 - m_k), Single)
                                InitialConc = m_cNO3 + m_coeffi * CType(m_CNH4, Single)
                            End If
                        End If
                    ElseIf m_concInit > 0 Then
                        'value was specified manually. this takes precedence over values in the table
                        'InitialConc = m_concInit
                        'm_cNO3 = m_concInit_NO3
                        If idx_initconc_NH4 < 0 AndAlso m_concInit_NH4 < 0 Then
                            Throw New Exception("Field " & m_sourcesN0FldName_NH4 & " not found in " & m_sources.AliasName)
                        ElseIf m_concInit_NH4 > 0 Then
                            m_CNH4 = m_concInit_NH4
                            m_cNO3 = m_concInit_NO3
                            InitialConc = m_concInit
                        Else
                            'Note By Yan: this condition is not going to happen.
                            m_CNH4 = point.Value(idx_initconc_NH4)
                            m_coeffi = CType(m_KNH4 / (m_KNH4 - m_k), Single)
                            InitialConc = m_cNO3 + m_coeffi * CType(m_CNH4, Single)
                        End If
                    Else
                        'value was specified in the table
                        m_coeffi = CType(m_KNH4 / (m_KNH4 - m_k), Single)
                        If idx_initconc_NH4 < 0 AndAlso m_concInit_NH4 < 0 Then
                            Throw New Exception("Field " & m_sourcesN0FldName_NH4 & " not found in " & m_sources.AliasName)
                        ElseIf m_concInit_NH4 > 0 Then
                            m_conc_NH4 = m_concInit_NH4
                            m_CNH4 = m_concInit_NH4
                        Else
                            m_conc_NH4 = point.Value(idx_initconc_NH4)
                            m_CNH4 = point.Value(idx_initconc_NH4)
                        End If
                        If m_concInit_NO3 < 0 Then
                            m_cNO3 = point.Value(idx_initconc)
                        Else
                            m_cNO3 = m_concInit_NO3
                        End If
                        InitialConc = CType(m_cNO3, Single) + m_coeffi * CType(m_conc_NH4, Single)
                    End If
                    'Trace.WriteLine(m_coeffi)
                End If
                'End NH4 calcultion.
            Else
                'if there is no NH4 calculation. ONLY NO3 calculation
                If idx_initconc < 0 AndAlso m_concInit < 0 Then
                    'value was not specified in attributes table or manually
                    Throw New Exception("Field " & m_sourcesN0FldName & " not found in " & m_sources.AliasName)
                ElseIf m_concInit > 0 Then
                    'value was specified manually. this takes precedence over values in the table
                    InitialConc = m_concInit
                Else
                    'value was specified in the table
                    InitialConc = point.Value(idx_initconc)
                End If
            End If



            'Note by Yan 20141029: adding the spatial Min
            idx_min = fcur.FindField("Min")
            If idx_min < 0 AndAlso m_Min < 0 Then
                Throw New Exception("The input load not found in " & m_sources.AliasName)
            ElseIf m_Min >= 0 Then
                m_Min = m_Min
            Else
                m_Min = point.Value(idx_min)
            End If
            'End adding.



            idx_k = fcur.FindField("decayCoeff")
            If idx_k < 0 AndAlso m_k < 0 Then
                Throw New Exception("Field decayCoeff not found in " & m_sources.AliasName)
            ElseIf m_k >= 0 Then
                DecayCoeff = m_k
            Else
                DecayCoeff = point.Value(idx_k)
            End If

            idx_dispL = fcur.FindField("dispL")
            If idx_dispL < 0 AndAlso m_Dx < 0 Then
                Throw New Exception("Field dispL not found in " & m_sources.AliasName)
            ElseIf m_Dx >= 0 Then
                DispL = m_Dx
            Else
                DispL = point.Value(idx_dispL)
            End If

            idx_dispTH = fcur.FindField("dispTH")
            If idx_dispTH < 0 AndAlso m_Dy < 0 Then
                Throw New Exception("Field dispTH not found in " & m_sources.AliasName)
            ElseIf m_Dy >= 0 Then
                dispTH = m_Dy
            Else
                dispTH = point.Value(idx_dispTH)
            End If

            srcpoint = point.ShapeCopy

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": Couldn't process file '" & m_sources.AliasName & "': " & ex.ToString)
            getInputParams1 = False
        End Try


        Return getInputParams1
    End Function
    ''' <summary>
    ''' used in CalcualtePlumeSingle
    ''' </summary>
    ''' <param name="t_time"></param>
    ''' <param name="avg_vel"></param>
    ''' <param name="concInit"></param>
    ''' <returns>Throws an exception if an invalid type has been specified</returns>
    ''' <remarks></remarks>
    Private Function setupAnalyticalSoln(ByVal t_time As Single, ByVal avg_vel As Single, _
                                         ByVal concInit As Single, ByVal k As Single, ByVal dispL As Single, ByVal dispTH As Single) As IAnalyticalFunction4D

        'plume function
        Dim PlumeFunc As IAnalyticalFunction4D
        Select Case m_soltype
            Case SolutionTypes.SolutionType.DomenicoRobbins
                PlumeFunc = New DomenicoRobbins(concInit:=concInit, _
                                                 Dx:=dispL, Dy:=dispTH, Dz:=m_Dz, _
                                                 y:=m_Y, z:=m_Z, _
                                                 v:=avg_vel, t:=t_time)

            Case SolutionTypes.SolutionType.DomenicoRobbins2D
                PlumeFunc = New DomenicoRobbins2D(concInit:=concInit, _
                                         Dx:=dispL, Dy:=dispTH, _
                                         y:=m_Y, _
                                         v:=avg_vel, t:=t_time)

            Case SolutionTypes.SolutionType.DomenicoRobbinsSS
                PlumeFunc = New DomenicoRobbinsSS(concInit:=concInit, _
                                                 Dx:=dispL, Dy:=dispTH, Dz:=m_Dz, _
                                                 y:=m_Y, z:=m_Z, _
                                                 v:=avg_vel)
            Case SolutionTypes.SolutionType.DomenicoRobbinsSS2D
                PlumeFunc = New DomenicoRobbinsSS2D(concInit:=concInit, _
                                                 Dx:=dispL, Dy:=dispTH, _
                                                 y:=m_Y, _
                                                 v:=avg_vel)
            Case SolutionTypes.SolutionType.ModifiedDomenico2D
                PlumeFunc = New DomenicoRobbinsModified2D(concInit:=concInit, _
                                         Dx:=dispL, Dy:=dispTH, _
                                         y:=m_Y, _
                                         v:=avg_vel, t:=t_time)

            Case SolutionTypes.SolutionType.VD_ModifiedDomenico2D_PG
                PlumeFunc = New DomenicoRobbinsModified2D_VD(concInit:=concInit, _
                                                 w:=1, _
                                                 Dy_Dx:=0.1, _
                                                 y:=m_Y, _
                                                 v:=avg_vel, t:=t_time, _
                                                 method:=DomenicoRobbinsModified2D_VD.DispersivityMethod.PickensGrisak)

            Case SolutionTypes.SolutionType.VD_ModifiedDomenico2D_XE
                PlumeFunc = New DomenicoRobbinsModified2D_VD(concInit:=concInit, _
                                                 w:=1, _
                                                 Dy_Dx:=0.1, _
                                                 y:=m_Y, _
                                                 v:=avg_vel, t:=t_time, _
                                                 method:=DomenicoRobbinsModified2D_VD.DispersivityMethod.XuEckstein)
            Case SolutionTypes.SolutionType.DomenicoRobbinsSSDecay2D
                PlumeFunc = New DomenicoRobbinsSSDecay2D(concInit:=concInit, _
                                                 Dx:=dispL, _
                                                  Dy:=dispTH, _
                                                 y:=m_Y, _
                                                 v:=avg_vel, _
                                                 k:=k)
            Case Else
                Throw New Exception("Solution type must be one of the types in Transport.SolutionType")
        End Select
        Return PlumeFunc
    End Function

    Private Function setupPlumeTransform(ByVal controlpts As IPointCollection4, _
                                         ByVal targetpts As IPointCollection4, _
                                         ByVal warpmethod As WarpingMethods.WarpingMethod, _
                                         ByVal xcellsz As Single, ByVal ycellsz As Single) As IGeodataXform

        Dim xform As IGeodataXform
        Dim approxXform As IApproximationXform
        Dim gx As IGeodataXformApproximation

        Select Case warpmethod
            Case WarpingMethods.WarpingMethod.Spline
                xform = New SplineXform

                'you have to explicitly cast to SplineXform, else get a null pointer exception. Lame.
                CType(xform, SplineXform).DefineFromControlPoints(controlpts, targetpts)    'IMPORTANT: there must be at least 10 control points for the spline transform
            Case WarpingMethods.WarpingMethod.Polynomial2
                xform = New PolynomialXform

                'cast...
                CType(xform, PolynomialXform).DefineFromControlPoints(controlpts, targetpts, 2)
            Case WarpingMethods.WarpingMethod.Polynomial1
                xform = New PolynomialXform

                'cast...
                CType(xform, PolynomialXform).DefineFromControlPoints(controlpts, targetpts, 1)
            Case Else
                Throw New Exception("Unsupported transform type")
        End Select
        xform.SpatialReference = CType(m_tracks, IGeoDataset).SpatialReference

        'define the transform approxmation
        If m_warpuseapprox Then
            approxXform = New ApproximationXform
            gx = approxXform
            approxXform.GeodataXform = xform
            gx.GridSize = ((xcellsz + ycellsz) / 2) * 20        'in map units
            gx.Tolerance = ((xcellsz + ycellsz) / 2) * 2        'in map units
            gx.Approximation = True                             'set this to false to disable approximation
            xform = approxXform
        End If
        Return xform
    End Function


    ''' <summary>
    ''' given a path id and a list of plume control points (in map coordinates), this function generates target points
    ''' along the specified path with ID=pathID.  The target points can then be used to warp the
    ''' plume to the path.  The maxdist parameter is important. It determines the location of the end of the
    ''' plume.  All points in the control points array will be mapped relative to the starting point and
    ''' the point along the flow path located a distance of maxdist away from the start of the path.  For example,
    ''' if the plume has length 10 units and the mid point of the plume is located 5 units from the start
    ''' of the plume, if maxdist is set to 10, the end of the plume will be located 10 units along the
    ''' curved flow path and the the midpoint of the plume 5 units.  If maxdist is set to a different value
    ''' other than the plume length, it will effectively change the length of the mapped plume.  E.g. if 
    ''' in the previous example, maxdist was set to 5, then the end point of the plume would be 5 units from
    ''' the start of the flow path and the midpoint 2.5 units.  This effetively shrunk the plume from its 
    ''' original length of 10 units to 5 units.
    ''' </summary>
    ''' <param name="pathid"></param>
    ''' <param name="controlpts_bdy_frac">The control points on the boundary of the plume</param>
    ''' <param name="controlpts_center_frac">The control points on the plume center line</param>
    ''' <param name="maxdist">The maximum distance along the path. should be the plume length</param>
    ''' <param name="controlpts">An output array containing the input control points as a single
    ''' IPointCollection.  This array is used to mapp to the Target points</param>
    ''' <returns>The target points to map to. nothing on error</returns>
    ''' <remarks>the control points arrays must be sorted ascending</remarks>
    Private Function getTargetPointsLocationsFromPath(ByVal pathid As Integer, _
                                                      ByVal controlpts_bdy() As Point, _
                                                      ByVal controlpts_bdy_frac() As Single, _
                                                      ByVal controlpts_center() As Point, _
                                                      ByVal controlpts_center_frac() As Single, _
                                                      ByVal maxdist As Single, _
                                                      ByRef controlpts As IPointCollection4) As Polyline

        Dim COM As New ComReleaser

        Dim targetPts As New Polyline
        Dim fp As FlowPath
        Dim t_dist As Single = 0            'total distance so far
        Dim seg_dist As Single = 0          'total distance along the segment     
        Dim poly As Polyline = Nothing

        Dim i As Integer

        Try

            '****************************
            'First, get the single polyline version of the flow path.
            'this allows us to use some built in functions to calculate
            'distances along the path
            '****************************

            If Not m_flowpaths.ContainsKey(pathid) Then Throw New Exception("Flow path with pathID " & pathid & " not found")
            fp = CType(m_flowpaths(pathid), FlowPath)
            poly = fp.getFlowPath

            '***********************************
            'now, calculate the positions of the target points
            'from the corresponding fractional distances along the plume
            '**************************************

            Dim curve As ICurve3 = CType(poly, ICurve3)
            Dim outpt As New Point                          'for using the built in functions
            Dim outpt2 As New Point                         'for using the built in functions

            'first do the points on the center line.
            'at the same time, convert the input control points array into an
            'IPointCollection. That way, the indeces in the output targetpts array
            'will correspond to the indeces in the output controlpts array
            For i = 0 To controlpts_center_frac.Length - 1
                'add the control point were working on to the output array
                controlpts.AddPoint(controlpts_center(i))

                'convert each ratio to a distance along the curve, with distance maxdist
                t_dist = maxdist * controlpts_center_frac(i)

                'get the point on the curve that is the specified distance from the start of the curve
                'subtract half the x mesh size so that the source points fall on the center of the cell
                'instead of on the edge
                curve.QueryPoint(esriSegmentExtension.esriExtendAtTo, t_dist - (m_meshsize_x / 2), False, outpt)

                'add the point to the output geometry                
                targetPts.AddPoint(outpt)
            Next

            '*************************************************************
            'now do the points on the plume boundary. 
            '**************************************************************

            'for the boundary points, the frac array will have exactly half as many points
            'as the actual points array.  this is because each pair of indeces in the points
            'array corresponds to the pair that defines the line perpendicular to the flow line 
            'at the fractional distance along the flow path indicated by the _frac array

            'Under normal circumstances (i.e. for most plumes) the length of the controlpts_center array
            'will be equal to the length of the controlpts_bdy_frac array.  This is what we need since
            'the loop below uses the controlpts_center array to determine the distance of each boundary
            'point to the center line.  controlpts_center is used so as to take into account the fact than
            'the boundary points could be at different distances from the center.  In some cases though,
            'The controlpts_center will be shorter by 1 element than the controlpts_bdy_frac array.  This
            'occurs when the two boundary points happen to fall at the exact tip of the plume. In this case,
            'we assume that the center line falls at the mid point of the two points.
            '
            'If the controlpts_center array has 2 or less elements than controlpts_bdy_frac then we have a problem
            If controlpts_center.Length <= controlpts_bdy_frac.Length - 2 Then Throw New Exception("There are not enough center line control points to map the plume!")


            'create a new polyline
            Dim l As New Line
            Dim dist_left, dist_right As Double
            Dim pt_left, pt_right, pt_center As Point
            COM.ManageLifetime(l)

            For i = 0 To controlpts_bdy_frac.Length - 2
                'add the control point were working on to the output array
                pt_left = controlpts_bdy(2 * i)
                pt_right = controlpts_bdy(2 * i + 1)
                controlpts.AddPoint(pt_left)
                controlpts.AddPoint(pt_right)

                'convert each ratio to a distance along the curve, with distance maxdist
                t_dist = maxdist * controlpts_bdy_frac(i)

                'get the point on the curve that is the specified distance from the start of the curve
                curve.QueryPoint(esriSegmentExtension.esriExtendAtTo, t_dist, False, outpt)

                'find the distance between each boundary point (on either side of the center line)
                'to the center line control point. This takes into account that the boundary points
                'could be at different distances away from the center line (happens when the width of
                'the plume is even)
                pt_center = controlpts_center(i)
                dist_left = Math.Sqrt(Math.Pow(pt_left.X - pt_center.X, 2) + Math.Pow(pt_left.Y - pt_center.Y, 2))
                dist_right = Math.Sqrt(Math.Pow(pt_right.X - pt_center.X, 2) + Math.Pow(pt_right.Y - pt_center.Y, 2))

                'get the perpendicular line 
                curve.QueryNormal(esriSegmentExtension.esriExtendAtTo, t_dist - (m_meshsize_x / 2), False, 1, l)

                'set the plume width
                l.QueryPoint(esriSegmentExtension.esriExtendTangents, dist_left, False, outpt)
                l.QueryPoint(esriSegmentExtension.esriExtendTangents, -dist_right, False, outpt2)

                targetPts.AddPoint(outpt)
                targetPts.AddPoint(outpt2)
            Next

            '********************
            'handle the special case described above
            '*********************

            i = controlpts_bdy_frac.Length - 1

            'add the control point were working on to the output array
            pt_left = controlpts_bdy(2 * i)
            pt_right = controlpts_bdy(2 * i + 1)
            controlpts.AddPoint(pt_left)
            controlpts.AddPoint(pt_right)

            'convert each ratio to a distance along the curve, with distance maxdist
            t_dist = maxdist * controlpts_bdy_frac(i)

            'get the point on the curve that is the specified distance from the start of the curve
            curve.QueryPoint(esriSegmentExtension.esriExtendAtTo, t_dist, False, outpt)

            If controlpts_center.Length = i Then
                'special case

                'the distance to the center plume is the total distance divided by 2
                Dim dist As Single
                dist = Math.Sqrt(Math.Pow(pt_left.X - pt_right.X, 2) + Math.Pow(pt_left.Y - pt_right.Y, 2))
                dist_left = dist / 2
                dist_right = dist / 2
            Else
                'normal case

                'find the distance between each boundary point (on either side of the center line)
                'to the center line control point. This takes into account that the boundary points
                'could be at different distances away from the center line (happens when the width of
                'the plume is even)
                pt_center = controlpts_center(i)
                dist_left = Math.Sqrt(Math.Pow(pt_left.X - pt_center.X, 2) + Math.Pow(pt_left.Y - pt_center.Y, 2))
                dist_right = Math.Sqrt(Math.Pow(pt_right.X - pt_center.X, 2) + Math.Pow(pt_right.Y - pt_center.Y, 2))

            End If

            'get the perpendicular line 
            curve.QueryNormal(esriSegmentExtension.esriExtendAtTo, t_dist - (m_meshsize_x / 2), False, 1, l)

            'set the plume width
            l.QueryPoint(esriSegmentExtension.esriExtendTangents, dist_left, False, outpt)
            l.QueryPoint(esriSegmentExtension.esriExtendTangents, -dist_right, False, outpt2)

            targetPts.AddPoint(outpt)
            targetPts.AddPoint(outpt2)

            ComReleaser.ReleaseCOMObject(poly)
            poly = Nothing
            fp.clearData()
            fp = Nothing
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            Return Nothing
        End Try

        Return targetPts

    End Function

    ''' <summary>
    ''' Converts the specified points which are row and column indeces into the raster plumeraster to 
    ''' map coordinates.
    ''' </summary>
    ''' <param name="ctrlpts"></param>
    ''' <param name="plumeraster"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function plumeCoordsToMapCoords(ByVal ctrlpts As List(Of Pnt), ByVal plumeraster As IRaster2) As Point()
        Dim ret(ctrlpts.Count - 1) As Point
        Dim pt As Point
        Dim ptX, ptY As Double
        Dim sr As ISpatialReference = CType(plumeraster, IRasterProps).SpatialReference

        Dim i As Integer = 0

        For Each pnt As Pnt In ctrlpts
            pt = New Point
            pt.SpatialReference = sr
            plumeraster.PixelToMap(pnt.X, pnt.Y, ptX, ptY)
            pt.PutCoords(ptX, ptY)
            ret(i) = pt
            i = i + 1
        Next

        Return ret
    End Function



    Private Sub outputWarpPointsToShapefile(ByVal controlpts As IPointCollection4, ByVal targetpts As IPointCollection4, ByVal pathid As Integer, ByVal savepath As String)
        Dim insertfeaturecursor As IFeatureCursor
        Dim insertfeaturebuffer As IFeatureBuffer
        Dim ptsfeatureclass As IFeatureClass
        Dim COM As New ComReleaser

        Try
            '********************************************
            'create the shapefile
            '********************************************
            Dim fields As IFields2 = New Fields
            Dim fieldsEditor As IFieldsEdit = CType(fields, IFieldsEdit)
            COM.ManageLifetime(fields)
            COM.ManageLifetime(fieldsEditor)

            ' Make the shape field        
            Dim field As IField2 = New Field
            Dim fieldEditor As IFieldEdit2 = CType(field, IFieldEdit2)
            COM.ManageLifetime(field)
            COM.ManageLifetime(fieldEditor)

            fieldEditor.Name_2 = "Shape"
            fieldEditor.Type_2 = esriFieldType.esriFieldTypeGeometry

            'define the geometry
            Dim geomDef As IGeometryDef = New GeometryDef
            COM.ManageLifetime(geomDef)

            Dim geomDefEdit As IGeometryDefEdit = CType(geomDef, IGeometryDefEdit)
            geomDefEdit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint
            geomDefEdit.SpatialReference_2 = CType(m_tracks, IGeoDataset).SpatialReference

            fieldEditor.GeometryDef_2 = geomDef
            fieldsEditor.AddField(field)

            ' Add more fields
            field = New Field
            COM.ManageLifetime(field)
            fieldEditor = field
            With fieldEditor
                .Name_2 = "Ctrl0Targ1"
                .Type_2 = esriFieldType.esriFieldTypeInteger
            End With
            fieldsEditor.AddField(field)

            ptsfeatureclass = Utilities.createShapefile(pathid & "_" & Now.Ticks & "_ctrlpts", savepath, fields, m_outputintermediate_outputs)
            COM.ManageLifetime(ptsfeatureclass)

            '*****************************************
            'output the data
            '********************************************
            'create insert buffer
            insertfeaturecursor = ptsfeatureclass.Insert(True)
            insertfeaturebuffer = ptsfeatureclass.CreateFeatureBuffer
            COM.ManageLifetime(insertfeaturecursor)
            COM.ManageLifetime(insertfeaturebuffer)
            If Not controlpts Is Nothing Then
                For i As Integer = 0 To controlpts.PointCount - 1
                    insertfeaturebuffer.Shape = controlpts.Point(i)
                    insertfeaturebuffer.Value(2) = 0
                    insertfeaturecursor.InsertFeature(insertfeaturebuffer)
                Next
            End If
            If Not targetpts Is Nothing Then
                For i As Integer = 0 To targetpts.PointCount - 1
                    insertfeaturebuffer.Shape = targetpts.Point(i)
                    insertfeaturebuffer.Value(2) = 1
                    insertfeaturecursor.InsertFeature(insertfeaturebuffer)
                Next
            End If
            insertfeaturecursor.Flush()

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
        End Try
    End Sub
    Private Sub outputWarpPointsToShapefile2(ByVal controlpts1() As Point, ByVal controlpts2() As Point, ByVal pathid As Integer, ByVal savepath As String)
        Dim insertfeaturecursor As IFeatureCursor
        Dim insertfeaturebuffer As IFeatureBuffer
        Dim ptsfeatureclass As IFeatureClass
        Dim COM As New ComReleaser

        Try
            '********************************************
            'create the shapefile
            '********************************************
            Dim fields As IFields2 = New Fields
            Dim fieldsEditor As IFieldsEdit = CType(fields, IFieldsEdit)
            COM.ManageLifetime(fields)
            COM.ManageLifetime(fieldsEditor)

            ' Make the shape field        
            Dim field As IField2 = New Field
            Dim fieldEditor As IFieldEdit2 = CType(field, IFieldEdit2)
            COM.ManageLifetime(field)
            COM.ManageLifetime(fieldEditor)

            fieldEditor.Name_2 = "Shape"
            fieldEditor.Type_2 = esriFieldType.esriFieldTypeGeometry

            'define the geometry
            Dim geomDef As IGeometryDef = New GeometryDef
            COM.ManageLifetime(geomDef)

            Dim geomDefEdit As IGeometryDefEdit = CType(geomDef, IGeometryDefEdit)
            geomDefEdit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint
            geomDefEdit.SpatialReference_2 = CType(m_tracks, IGeoDataset).SpatialReference

            fieldEditor.GeometryDef_2 = geomDef
            fieldsEditor.AddField(field)

            ' Add more fields
            field = New Field
            COM.ManageLifetime(field)
            fieldEditor = field
            With fieldEditor
                .Name_2 = "Ctrl0Targ1"
                .Type_2 = esriFieldType.esriFieldTypeInteger
            End With
            fieldsEditor.AddField(field)

            ptsfeatureclass = Utilities.createShapefile(pathid & "_" & Now.Ticks & "_ctrlpts", savepath, fields, m_outputintermediate_outputs)
            COM.ManageLifetime(ptsfeatureclass)

            '*****************************************
            'output the data
            '********************************************
            'create insert buffer
            insertfeaturecursor = ptsfeatureclass.Insert(True)
            insertfeaturebuffer = ptsfeatureclass.CreateFeatureBuffer
            COM.ManageLifetime(insertfeaturecursor)
            COM.ManageLifetime(insertfeaturebuffer)
            If Not controlpts1 Is Nothing Then
                For i As Integer = 0 To controlpts1.Length - 1
                    insertfeaturebuffer.Shape = controlpts1(i)
                    insertfeaturebuffer.Value(2) = 0
                    insertfeaturecursor.InsertFeature(insertfeaturebuffer)
                Next
            End If
            If Not controlpts2 Is Nothing Then
                For i As Integer = 0 To controlpts2.Length - 1
                    insertfeaturebuffer.Shape = controlpts2(i)
                    insertfeaturebuffer.Value(2) = 1
                    insertfeaturecursor.InsertFeature(insertfeaturebuffer)
                Next
            End If
            insertfeaturecursor.Flush()

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
        End Try
    End Sub


    Private Sub addPlumeData(ByRef fb As IFeatureBuffer, ByVal plume As ContaminantPlume, ByVal xsection(,) As Single)
        fb.Shape = plume.SourceLocation
        fb.Value(m_idx_pathID) = plume.PathID
        If SolutionTypes.is2D(m_soltype) Then
            fb.Value(m_idx_is2D) = 1
        Else
            fb.Value(m_idx_is2D) = 0
        End If
        fb.Value(m_idx_decayCoeff) = plume.DecayCoeff
        fb.Value(m_idx_plumetime) = plume.PlumeTime
        fb.Value(m_idx_avgVel) = plume.Velocity
        fb.Value(m_idx_ax) = plume.DispL
        fb.Value(m_idx_ay) = plume.DispTH
        fb.Value(m_idx_az) = plume.DispTV
        fb.Value(m_idx_plumelength) = plume.PlumeTruncatedLength
        fb.Value(m_idx_pathlength) = plume.PathLength
        fb.Value(m_idx_pathtime) = plume.PathTime
        fb.Value(m_idx_volume) = plume.PlumeVolume
        fb.Value(m_idx_srcAngle) = plume.DirectionAngle
        fb.Value(m_idx_srcConc) = plume.ConcInit
        fb.Value(m_idx_threshConc) = plume.ConcThresh
        fb.Value(m_idx_wbid_plume) = plume.DestinationWaterbodyID_Plume
        fb.Value(m_idx_wbid_path) = plume.DestinationWaterbodyID_Path
        fb.Value(m_idx_sourceY) = plume.source_Y
        fb.Value(m_idx_sourceZ) = plume.source_Z
        fb.Value(m_idx_MeshDx) = plume.MeshSzDX
        fb.Value(m_idx_MeshDy) = plume.MeshSzDY
        fb.Value(m_idx_MeshDz) = plume.MeshSzDZ
        fb.Value(m_idx_nextConc) = plume.ConcNext
        fb.Value(m_idx_avgporosity) = plume.Porosity
        fb.Value(m_idx_warp) = m_warpmethod
        fb.Value(m_idx_volFac) = m_volConversionFac
        fb.Value(m_idx_PP) = m_postprocamt
        fb.Value(m_idx_domBdy) = m_domenicoBdy

        '***************************************************
        'calculate input load using methods similar to MT3D
        '***************************************************
        Dim source_cells_nextrow As New List(Of Single)
        Dim source_cells_width As New List(Of Single)
        For j = 0 To xsection.GetUpperBound(1)
            If xsection(0, j) > 0 Then
                source_cells_nextrow.Add(xsection(1, j))
                source_cells_width.Add(plume.MeshSzDY)
            End If
        Next
        Dim required_source_cells As Integer = plume.source_Y / plume.MeshSzDY
        If required_source_cells Mod 2 = 0 Then
            'even
            If source_cells_nextrow.Count - required_source_cells = 1 Then
                'correct for the half-cells at the ends
                'remember that the original function was evaluated block-centered
                'so if the number of required blocks is even, need to only consider 
                'half of the cells at the ends of the source plane
                source_cells_width(0) = source_cells_width(0) / 2
                source_cells_width(source_cells_width.Count - 1) = source_cells_width(source_cells_width.Count - 1) / 2
            Else
                Trace.WriteLine("Source input dimensions may have a small error")
            End If
        Else
            'odd number of source cells, everything is ok. do nothing
        End If
        'double check
        If source_cells_width.Sum <> plume.source_Y Then
            Trace.WriteLine("Source input dimensions may have a small error: Calculated: " & source_cells_width.Sum & " Required: " & plume.source_Y)
        End If

        'calcuate input load
        Dim M0 As Single = 0
        Dim dispcons, dispcons1 As Single
        For j As Integer = 0 To source_cells_nextrow.Count - 1
            M0 = M0 + plume.Porosity * source_cells_width(j) * plume.source_Z * plume.Velocity * m_volConversionFac * _
                          (plume.ConcInit - plume.DispL * (source_cells_nextrow(j) - plume.ConcInit) / plume.MeshSzDX)
        Next
        fb.Value(m_idx_massInRateMT3D) = M0
        '******************************************

        'calculate input load using analytical solution
        'Yan Zhu: when calculating the reactive nitrogen including the ammonia and nitrate, conInit refers to the a1 or a2. 
        '         a1 means the real ammonium concentration but a2 is not the real nitrate concentration. 
        '         the real nitrate ConInit should be used, that is the reason changing the codes herein.
        If m_domenicoBdy <> DomenicoSourceBoundaries.DomenicoSourceBoundary.Specified_Input_Mass_Rate Then
            If m_calculatingNH4 Then
                If Not m_calculatingNO3 Then
                    M0 = plume.Velocity * plume.Porosity * plume.source_Y * plume.source_Z * plume.ConcInit * m_volConversionFac * _
                                (0.5 + 0.5 * Math.Sqrt(1 + (4 * plume.DecayCoeff * plume.DispL) / plume.Velocity))
                Else
                    dispcons = 0.5 * m_CNH4 * (m_KNH4 / (m_KNH4 - plume.DecayCoeff)) * (Math.Sqrt(1 + (4 * plume.DecayCoeff * plume.DispL) / plume.Velocity) - Math.Sqrt(1 + (4 * m_KNH4 * plume.DispL) / plume.Velocity))
                    dispcons1 = m_cNO3 * (0.5 + 0.5 * Math.Sqrt(1 + (4 * plume.DecayCoeff * plume.DispL) / plume.Velocity))
                    'Trace.WriteLine(dispcons)
                    'Trace.WriteLine(dispcons1)
                    M0 = plume.Velocity * plume.Porosity * plume.source_Y * plume.source_Z * m_volConversionFac * _
                                            Math.Max(dispcons1, dispcons1 + dispcons)
                End If
                'Trace.WriteLine(m_CNH4)
                'Trace.WriteLine(m_cNO3)
            Else
                M0 = plume.Velocity * plume.Porosity * plume.source_Y * plume.source_Z * plume.ConcInit * m_volConversionFac * _
                                (0.5 + 0.5 * Math.Sqrt(1 + (4 * plume.DecayCoeff * plume.DispL) / plume.Velocity))
            End If
        Else
            'note by Yan 10/27/2014: if specified z and calculate NH4 and NO3, Then re-distribute the Min according to the percent of concentration.
            'Trace.WriteLine("Start to re-assign M_min")
            'Trace.WriteLine("m_Min" & m_Min)
            'Trace.WriteLine("m_CNH4" & m_CNH4)
            'Trace.WriteLine("m_cNO3" & m_cNO3)
            If m_calculatingNH4 Then
                If Not m_calculatingNO3 Then
                    M0 = m_Min * CType(m_CNH4 / (m_CNH4 + m_cNO3), Single)
                    'Trace.WriteLine("m_Min_CNH4" & M0)
                Else
                    M0 = m_Min * CType(m_cNO3 / (m_CNH4 + m_cNO3), Single)
                    'Trace.WriteLine("m_Min_CNO3" & M0)
                End If
            Else
                M0 = m_Min
            End If
            'Trace.WriteLine(M0)
        End If

        fb.Value(m_idx_massInRate) = M0

        'calculate the amount of denitrification per single plume. 
        Dim Mdn As Single = 0
        For i As Integer = 1 To Math.Min(xsection.GetUpperBound(0), plume.PlumeTruncatedLengthCells - 1)
            For j As Integer = 0 To xsection.GetUpperBound(1)
                Mdn = Mdn + plume.DecayCoeff * xsection(i, j) _
                          * plume.Porosity _
                          * plume.MeshSzDX * plume.MeshSzDY * plume.MeshSzDZ * m_volConversionFac

            Next
        Next
        fb.Value(m_idx_massDNRate) = Mdn
        'Trace.WriteLine("m_removal NH4_NITRIFICATION, NO3_DENITRIFICATION" & Mdn)

    End Sub


    ''' <summary>
    ''' creates a new shapefile in the same folder as the active map.  the spatial reference is
    ''' set to be the same as the active map.
    ''' </summary>
    ''' <returns>True on success false on error</returns>
    ''' <remarks></remarks>
    Private Function createNewPlumeDataShapefile(ByVal savepath As String) As IFeatureClass
        Dim COM As New ComReleaser
        Dim ret As IFeatureClass
        Try
            Dim fields As IFields2 = New Fields
            Dim fieldsEditor As IFieldsEdit = CType(fields, IFieldsEdit)
            COM.ManageLifetime(fieldsEditor)
            COM.ManageLifetime(fields)

            ' Make the shape field        
            Dim field As IField2 = New Field
            Dim fieldEditor As IFieldEdit2 = CType(field, IFieldEdit2)
            fieldEditor.Name_2 = "Shape"
            fieldEditor.Type_2 = esriFieldType.esriFieldTypeGeometry
            'define the geometry
            Dim geomDef As IGeometryDef = New GeometryDef
            Dim geomDefEdit As IGeometryDefEdit = CType(geomDef, IGeometryDefEdit)
            geomDefEdit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint
            geomDefEdit.SpatialReference_2 = CType(m_sources, IGeoDataset).SpatialReference

            fieldEditor.GeometryDef_2 = geomDef
            fieldsEditor.AddField(field)

            ' Add more fields
            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "PathID" 'The PathID
                .Type_2 = esriFieldType.esriFieldTypeInteger
            End With
            fieldsEditor.AddField(field)


            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "is2D"
                .Type_2 = esriFieldType.esriFieldTypeSmallInteger
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "domBdy"
                .Type_2 = esriFieldType.esriFieldTypeSmallInteger
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "decayCoeff"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "avgVel"
                .Type_2 = esriFieldType.esriFieldTypeDouble
                .Precision_2 = 31
                .Scale_2 = 8
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "avgPrsity"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "dispL"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "dispTH"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "dispTV"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "SourceY"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "SourceZ"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "MeshDX"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "MeshDY"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "MeshDZ"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "plumeTime"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "pathTime"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "plumeLen"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "pathLen"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "plumeVol"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "msInRtNmr"
                .Type_2 = esriFieldType.esriFieldTypeDouble
                .Precision_2 = 31
                .Scale_2 = 8
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "massInRate"
                .Type_2 = esriFieldType.esriFieldTypeDouble
                .Precision_2 = 31
                .Scale_2 = 8
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "massDNRate"
                .Type_2 = esriFieldType.esriFieldTypeDouble
                .Precision_2 = 31
                .Scale_2 = 8
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "srcAngle"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "warp"
                .Type_2 = esriFieldType.esriFieldTypeSmallInteger
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "PostP"
                .Type_2 = esriFieldType.esriFieldTypeSmallInteger
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "N0_Conc"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "volFac"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "nextConc"
                .Type_2 = esriFieldType.esriFieldTypeDouble
                .Precision_2 = 31
                .Scale_2 = 8
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "threshConc"
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "WBId_plume" 'the id of the water body where the plume terminates (-1 if it does not terminate at a water body)                
                .Type_2 = esriFieldType.esriFieldTypeSmallInteger
            End With
            fieldsEditor.AddField(field)

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "WBId_path" 'the id of the water body where the path terminates (-1 if it does not terminate at a water body)                
                .Type_2 = esriFieldType.esriFieldTypeSmallInteger
            End With
            fieldsEditor.AddField(field)

            m_idx_pathID = fieldsEditor.FindField("PathID") + 1
            m_idx_is2D = fieldsEditor.FindField("is2D") + 1
            m_idx_domBdy = fieldsEditor.FindField("domBdy") + 1
            m_idx_decayCoeff = fieldsEditor.FindField("decayCoeff") + 1
            m_idx_avgVel = fieldsEditor.FindField("avgVel") + 1
            m_idx_ax = fieldsEditor.FindField("dispL") + 1
            m_idx_ay = fieldsEditor.FindField("dispTH") + 1
            m_idx_az = fieldsEditor.FindField("dispTV") + 1
            m_idx_plumelength = fieldsEditor.FindField("plumeLen") + 1
            m_idx_pathlength = fieldsEditor.FindField("pathLen") + 1
            m_idx_plumetime = fieldsEditor.FindField("plumeTime") + 1
            m_idx_pathtime = fieldsEditor.FindField("pathTime") + 1
            m_idx_volume = fieldsEditor.FindField("plumeVol") + 1
            m_idx_srcAngle = fieldsEditor.FindField("srcAngle") + 1
            m_idx_srcConc = fieldsEditor.FindField("N0_Conc") + 1
            m_idx_threshConc = fieldsEditor.FindField("threshConc") + 1
            m_idx_wbid_plume = fieldsEditor.FindField("wbID_plume") + 1
            m_idx_wbid_path = fieldsEditor.FindField("wbID_path") + 1
            m_idx_sourceY = fieldsEditor.FindField("SourceY") + 1
            m_idx_sourceZ = fieldsEditor.FindField("SourceZ") + 1
            m_idx_MeshDx = fieldsEditor.FindField("MeshDX") + 1
            m_idx_MeshDy = fieldsEditor.FindField("MeshDY") + 1
            m_idx_MeshDz = fieldsEditor.FindField("MeshDZ") + 1
            m_idx_avgporosity = fieldsEditor.FindField("avgPrsity") + 1
            m_idx_nextConc = fieldsEditor.FindField("nextConc") + 1
            m_idx_massInRateMT3D = fieldsEditor.FindField("msInRtNmr") + 1
            m_idx_massInRate = fieldsEditor.FindField("massInRate") + 1
            m_idx_massDNRate = fieldsEditor.FindField("massDNRate") + 1
            m_idx_warp = fieldsEditor.FindField("warp") + 1
            m_idx_PP = fieldsEditor.FindField("PostP") + 1
            m_idx_volFac = fieldsEditor.FindField("volFac") + 1

            ret = Utilities.createShapefile(m_plumesFilename & "_info", savepath, fields, m_outputintermediate_outputs)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = Nothing
        End Try

        Return ret
    End Function
#End Region


End Class

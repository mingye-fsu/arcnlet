'Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.ADF


''' <summary>
''' Used by the particle track functionality of the program
''' </summary>
''' <remarks>This class implements the particle tracking functionality.
''' After initializing the particle tracker via the constructor, tracking is initiated by calling
''' the track() function. Tracking can be cancelled by the cancelTrack() function.  After tracking
''' is finished, the feature class containing the paths is available via the ParticleTracks property.
''' Note that this class can only be used to track the point(s) given in the constructor. To track a different
''' point(s) a new instance of the class must be constructed.
''' </remarks>
Public Class ParticleTracker

    Private m_mag As IRaster2
    Private m_dir As IRaster2
    Private m_wb As IRaster2
    Private m_porosity As IRaster2
    Private m_shp_fname As String           'name and path to save the shapefile in
    Private m_shp_path As String
    Private m_ptracksfc As IFeatureClass    'the reference to the feature class containing the final particle paths
    Private m_trackpt As Point              'the point to track
    Private m_trackpts As IFeatureClass     'the feature class containing the points to track
    Private m_stepsize As Single            'the size of the steps to take
    Private m_maxsteps As Long               'the maximum number of steps to run the tracking

    Private m_spatialref As ISpatialReference   'spatial reference to use when saving the shapefile

    Private m_fc As IFeatureCursor
    Private m_fb As IFeatureBuffer

    Private m_idx_pathID As Integer
    Private m_idx_segID As Integer
    Private m_idx_totTime As Integer
    Private m_idx_segVel As Integer
    Private m_idx_segVelTxt As Integer
    Private m_idx_totDist As Integer
    Private m_idx_wbID As Integer
    Private m_idx_pathWbID As Integer
    Private m_idx_segPrsity As Integer

    Private m_trackingcancelled As Boolean  'a flag used to indicate if the user cancelled the tracking

    ''' <summary>
    ''' Returns the feature class containing the particle track(s)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ParticleTracks() As IFeatureClass
        Get
            ParticleTracks = m_ptracksfc
        End Get        
    End Property

    ''' <summary>
    ''' Constructs a single point tracker
    ''' </summary>
    ''' <param name="r_mag">Input magnitude raster</param>
    ''' <param name="r_dir">Input direction raster</param>
    ''' <param name="r_wb">THe input raster representing the locations of the water boides. can be Nothing</param>
    ''' <param name="out_track_shp">The name of the shapefile that will hold the particle tracks. Don't include the path</param>
    ''' <param name="pt">The point to track</param>
    ''' <param name="maxsteps">The maximum number of steps to take before stopping</param>
    ''' <param name="out_track_path">Output folder for the shape file</param>
    ''' <param name="spatialref">The spatial reference of the output shapefile.  Should be the same
    ''' as the map frame spatial reference but can be different if desired</param>
    ''' <param name="steplen">The size of the step (in map units) to take</param>
    ''' <remarks>
    ''' All data sets should have the same spatial reference as the active map.  The water bodies raster should have a value only 
    ''' where water bodies are located. all other cells should have NoData
    ''' </remarks>
    Public Sub New(ByVal r_mag As IRaster2, _
                    ByVal r_dir As IRaster2, _
                    ByVal r_wb As IRaster2, _
                    ByVal r_porosity As IRaster2, _
                    ByVal out_track_shp As String, _
                    ByVal out_track_path As String, _
                    ByVal spatialref As ISpatialReference, _
                    ByVal pt As Point, _
                    ByVal steplen As Single, _
                    ByVal maxsteps As Integer)
        If out_track_shp = "" Then Throw New Exception("A shapefile file name must be specified")
        If r_dir Is Nothing Then Throw New Exception("direction raster cannot be Nothing")
        If r_mag Is Nothing Then Throw New Exception("magnitude raster cannot be Nothing")
        If r_porosity Is Nothing Then Throw New Exception("porosity raster cannot be Nothing")
        If pt Is Nothing Then Throw New Exception("Point cannot be Nothing")
        init(r_mag, r_dir, r_wb, r_porosity, out_track_path, out_track_shp, spatialref, steplen, maxsteps, pt, Nothing)
    End Sub

    ''' <summary>
    ''' Constructs a tracker that tracks multiple points
    ''' </summary>
    ''' <param name="r_mag">Input magnitude raster</param>
    ''' <param name="r_dir">Input direction raster</param>
    ''' <param name="r_wb">Input water bodies raster. can be nothing</param>
    ''' <param name="out_track_shp">The name of the shapefile that will hold the particle tracks. Don't include the path</param>
    ''' <param name="points" >A point feature class containing the starting points of the particle track.</param>
    ''' <param name="maxsteps">The maximum number of steps to take before stopping</param>
    ''' <param name="out_track_path">Output folder for the shape file</param>
    ''' <param name="spatialref">The spatial reference of the output shapefile.  Should be the same
    ''' as the map frame spatial reference but can be different if desired</param>
    ''' <param name="steplen">The size of the step (in map units) to take</param>
    ''' <remarks>
    ''' All data sets should have the same spatial reference as the active map.  The water bodies raster should have a value only 
    ''' where water bodies are located. all other cells should have NoData
    ''' </remarks>
    Public Sub New(ByVal r_mag As IRaster2, _
                   ByVal r_dir As IRaster2, _
                   ByVal r_wb As IRaster2, _
                   ByVal r_porosity As IRaster2, _
                   ByVal out_track_shp As String, _
                   ByVal out_track_path As String, _
                   ByVal spatialref As ISpatialReference, _
                   ByVal points As IFeatureClass, _
                   ByVal steplen As Single, _
                   ByVal maxsteps As Integer)
        If out_track_shp = "" Then Throw New Exception("A shapefile file name must be specified")
        If r_dir Is Nothing Then Throw New Exception("direction raster cannot be Nothing")
        If r_mag Is Nothing Then Throw New Exception("magnitude raster cannot be Nothing")
        If points Is Nothing Then Throw New Exception("Points feature class cannot be Nothing")
        If r_porosity Is Nothing Then Throw New Exception("Porosity class cannot be Nothing")
        init(r_mag, r_dir, r_wb, r_porosity, out_track_path, out_track_shp, spatialref, steplen, maxsteps, Nothing, points)
    End Sub

    ''' <summary>
    ''' initializes a new particle tracker
    ''' </summary>
    Private Sub init(ByVal r_mag As IRaster2, ByVal r_dir As IRaster2, ByVal r_wb As IRaster2, ByVal r_porosity As IRaster2, _
                     ByVal out_track_path As String, ByVal out_track_shp As String, _
                     ByVal sr As ISpatialReference, _
                     ByVal steplen As Single, ByVal maxsteps As Integer, _
                     ByVal pt As Point, ByVal fc As IFeatureClass)
        m_trackingcancelled = False

        m_mag = r_mag
        m_dir = r_dir
        m_wb = r_wb
        m_porosity = r_porosity
        m_shp_fname = out_track_shp
        m_trackpt = pt
        m_trackpts = fc
        m_maxsteps = maxsteps
        m_stepsize = steplen
        m_shp_path = out_track_path
        m_spatialref = sr

        'make sure the shapefile doesnt exist. else arcgis will crash when we try to create it.
        Dim pathtocheck As String = IO.Path.Combine(m_shp_path, m_shp_fname & ".shp")
        If Utilities.checkExist(pathtocheck) Then
            Throw New Exception("[Error] The shapefile " & pathtocheck & " exists. Please delete it or select a new file name")
        End If
    End Sub


    ''' <summary>
    ''' Creates the shapefile that will store the particle paths
    ''' </summary>
    ''' <returns>True on success false on error</returns>
    ''' <remarks></remarks>
    Private Function createNewShapefile() As Boolean
        Try
            Dim fields As IFields2 = New Fields
            Dim fieldsEditor As IFieldsEdit = CType(fields, IFieldsEdit)

            ' Make the shape field        
            Dim field As IField2 = New Field
            Dim fieldEditor As IFieldEdit2 = CType(field, IFieldEdit2)
            fieldEditor.Name_2 = "Shape"
            fieldEditor.Type_2 = esriFieldType.esriFieldTypeGeometry
            'define the geometry
            Dim geomDef As IGeometryDef = New GeometryDef
            Dim geomDefEdit As IGeometryDefEdit = CType(geomDef, IGeometryDefEdit)
            geomDefEdit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline
            geomDefEdit.SpatialReference_2 = m_spatialref

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
            m_idx_pathID = fieldsEditor.FindField("PathID") + 1


            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "SegID" 'The segmentID
                .Type_2 = esriFieldType.esriFieldTypeInteger
            End With
            fieldsEditor.AddField(field)
            m_idx_segID = fieldsEditor.FindField("SegID") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                '.Length_2 = 30
                .Name_2 = "TotDist" 'total cumulative travel distance
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)
            m_idx_totDist = fieldsEditor.FindField("TotDist") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                '.Length_2 = 30
                .Name_2 = "TotTime" 'total cumulative travel time
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)
            m_idx_totTime = fieldsEditor.FindField("TotTime") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "SegPrsity" 'segment porosity
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)
            m_idx_segPrsity = fieldsEditor.FindField("SegPrsity") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                '.Length_2 = 30
                .Name_2 = "SegVel" 'segment velocity
                .Type_2 = esriFieldType.esriFieldTypeSingle
            End With
            fieldsEditor.AddField(field)
            m_idx_segVel = fieldsEditor.FindField("SegVel") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Length_2 = 30
                .Name_2 = "SegVelTxt" 'segment velocity
                .Type_2 = esriFieldType.esriFieldTypeString
            End With
            fieldsEditor.AddField(field)
            m_idx_segVelTxt = fieldsEditor.FindField("SegVelTxt") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "WBId" 'the id of the water body where the path terminates
                'this will be -1 for all segments in a path except the last one
                .Type_2 = esriFieldType.esriFieldTypeInteger
            End With
            fieldsEditor.AddField(field)
            m_idx_wbID = fieldsEditor.FindField("WBId") + 1

            field = New Field
            fieldEditor = field
            With fieldEditor
                .Name_2 = "PathWBId" 'the id of the water body where the path terminates
                'this will be equal to the WBId of the last segment in the sequence
                .Type_2 = esriFieldType.esriFieldTypeInteger
            End With
            fieldsEditor.AddField(field)
            m_idx_pathWbID = fieldsEditor.FindField("PathWBId") + 1

            ' Create the shapefile
            ' (some parameters apply to geodatabase options and can be defaulted as Nothing)
            Trace.WriteLine("Creating " & IO.Path.Combine(m_shp_path, m_shp_fname & ".shp"))
            Dim wf As IWorkspaceFactory2 = Activator.CreateInstance(Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory"))
            Dim fw As IFeatureWorkspace = wf.OpenFromFile(m_shp_path, Nothing)
            m_ptracksfc = fw.CreateFeatureClass(m_shp_fname, fields, Nothing, Nothing, esriFeatureType.esriFTSimple, "Shape", "")
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' tracks a path given a point.
    ''' </summary>
    ''' <param name="pt">The point where the path will begin</param>
    ''' <param name="id">The ID that will be assigned to each segment in the path</param>
    ''' <returns>True on OK. False on error</returns>
    ''' <remarks></remarks>
    Private Function tracksingle(ByVal pt As Point, ByVal id As Integer) As Boolean
        Trace.Indent()

        'used to distiguish exceptions that are not severe in the grand scheme of things
        Dim exeptionIsWarning As Boolean = False

        Try
            If m_ptracksfc Is Nothing Then Throw New Exception("target feature class is Nothing")

            'inputs
            Dim currow_dir, currcol_dir As Integer      'direction
            Dim currow_mag, currcol_mag As Integer      'magnitude
            Dim currow_wb, currcol_wb As Integer        'water bodies
            Dim currow_porosity, currcol_porosity As Integer
            Dim nextcol_dir, nextrow_dir As Integer
            Dim nextcol_mag, nextrow_mag As Integer
            Dim nextcol_porosity, nextrow_porosity As Integer
            Dim nextcol_wb, nextrow_wb As Integer

            'define polyline vars and attributes
            Dim f_polyline As Polyline
            Dim currangle As Single, currveloc As Single, currporosity As Single, currwbid As Object            
            Dim cur_x = pt.X, cur_y = pt.Y, next_x, next_y As Double

            'accumulators
            Dim traveltime As Double = 0    'the total cumulative traveltime
            Dim totaldist As Single = 0     'the total cumulative distance
            Dim steps As Integer = 0        'the number of steps taken so far

            'define the start and end points for each line segment
            Dim pt1 As New Point With {.SpatialReference = m_spatialref}
            Dim pt2 As New Point With {.SpatialReference = m_spatialref}

            'save the path segments into a list, which will then be added to the shapefile
            Dim path As New List(Of PathSegment)
            Dim segment As PathSegment

            'get the nodata value for the water bodies raster
            Dim wb_nodata As Single
            If Not m_wb Is Nothing Then
                wb_nodata = Utilities.getRasterNoDataValue(m_wb)
            End If

            'this flag is used to stop the iterations if we reach a water body
            Dim done As Boolean = False

            '*******************************************************************

            'Note: MapToPixel WILL ALWAYS RETURN A ROW/COLUMN even if its outside the raster
            'when you try to get the value in this non existant cell, it will return 0

            'check to make sure we have a valid starting point
            m_mag.MapToPixel(pt.X, pt.Y, currcol_mag, currow_mag)
            m_dir.MapToPixel(pt.X, pt.Y, currcol_dir, currow_dir)
            m_porosity.MapToPixel(pt.X, pt.Y, currcol_porosity, currow_porosity)

            currveloc = m_mag.GetPixelValue(0, currcol_mag, currow_mag)
            If m_mag.GetPixelValue(0, currcol_mag, currow_mag) = 0 Then
                exeptionIsWarning = True
                Throw New Exception("Magnitude is zero. It is possible that the point (" & pt.X & "," & pt.Y & ") is outside of the raster extent or it is a flat area.")
            End If
            currporosity = m_porosity.GetPixelValue(0, currcol_porosity, currow_porosity)
            If m_porosity.GetPixelValue(0, currcol_porosity, currow_porosity) = 0 Then
                exeptionIsWarning = True
                Throw New Exception("Porosity is zero. It is possible that the point (" & pt.X & "," & pt.Y & ") is outside of the raster extent or it is a flat area.")
            End If
            If Not m_wb Is Nothing Then
                m_wb.MapToPixel(pt.X, pt.Y, currcol_wb, currow_wb)
                'if the pixel value of the water bodies is not NoData or nothing, we are in a water body
                currwbid = m_wb.GetPixelValue(0, currcol_wb, currow_wb)
                If currwbid <> wb_nodata And Not currwbid Is Nothing Then
                    exeptionIsWarning = True
                    Throw New Exception("Starting point (" & pt.X & "," & pt.Y & ") is in a water body!")
                End If
            Else
                currwbid = Nothing
            End If
            'dont check direction since it could actually be zero but initialize it anyways
            currangle = m_dir.GetPixelValue(0, currcol_dir, currow_dir)


            While steps < m_maxsteps And Not m_trackingcancelled And Not done

                currangle = m_dir.GetPixelValue(0, currcol_dir, currow_dir)
                currveloc = m_mag.GetPixelValue(0, currcol_mag, currow_mag)
                currporosity = m_porosity.GetPixelValue(0, currcol_porosity, currow_porosity)

                'get the next x and y
                next_x = cur_x + m_stepsize * Math.Sin(currangle * 0.0174533)
                next_y = cur_y + m_stepsize * Math.Cos(currangle * 0.0174533)
                m_dir.MapToPixel(next_x, next_y, nextcol_dir, nextrow_dir)
                m_mag.MapToPixel(next_x, next_y, nextcol_mag, nextrow_mag)
                m_porosity.MapToPixel(next_x, next_y, nextcol_porosity, nextrow_porosity)
                If Not m_wb Is Nothing Then
                    m_wb.MapToPixel(next_x, next_y, nextcol_wb, nextrow_wb)
                    'for the water bodies, check the segment end point rather than the start
                    'else will have an extra segment at the end.
                    currwbid = m_wb.GetPixelValue(0, nextcol_wb, nextrow_wb)
                End If

                'cumulative distance travelled
                totaldist = totaldist + m_stepsize
                traveltime = traveltime + m_stepsize / currveloc

                'check to make sure we haven't reached a flat area (there shouldnt be any
                'if the input magnitude was calculated with this program). a value of 0 will also
                'be returned if the cell coordinates are outside the raster, therefore check for 0
                If currveloc = 0 Then
                    done = True
                End If
                If currporosity = 0 Then
                    done = True
                End If

                'create the line segment
                segment = New PathSegment
                pt1.PutCoords(cur_x, cur_y)
                pt2.PutCoords(next_x, next_y)
                f_polyline = New Polyline
                f_polyline.AddPoint(pt1)
                f_polyline.AddPoint(pt2)
                segment.shape = f_polyline
                segment.id = id
                segment.steps = steps
                segment.segVel = currveloc
                segment.segPoro = currporosity
                segment.totDist = totaldist
                segment.totTime = traveltime
                If currwbid <> wb_nodata AndAlso Not currwbid Is Nothing Then
                    segment.segWbID = currwbid
                    path.Add(segment)
                    done = True
                Else
                    segment.segWbID = -1
                End If

                'add the feature to the buffer if not already added
                If Not done Then
                    path.Add(segment)
                End If

                'set the next row and column
                currcol_dir = nextcol_dir
                currow_dir = nextrow_dir
                currcol_mag = nextcol_mag
                currow_mag = nextrow_mag
                currow_porosity = nextrow_porosity
                currcol_porosity = nextcol_porosity
                cur_x = next_x
                cur_y = next_y

                steps = steps + 1
            End While

            If m_trackingcancelled Then
                Trace.WriteLine("Tracking was cancelled. Path is incomplete")
            Else
                'save the feature to the buffer
                Dim destinationWbID As Integer = path.Last().segWbID
                For Each seg As PathSegment In path
                    m_fb.Shape = seg.shape
                    m_fb.Value(m_idx_pathID) = seg.id
                    m_fb.Value(m_idx_segID) = seg.steps
                    m_fb.Value(m_idx_segVelTxt) = seg.segVel.ToString("E")
                    m_fb.Value(m_idx_segVel) = seg.segVel
                    m_fb.Value(m_idx_totDist) = seg.totDist
                    m_fb.Value(m_idx_totTime) = seg.totTime
                    m_fb.Value(m_idx_wbID) = seg.segWbID
                    m_fb.Value(m_idx_pathWbID) = destinationWbID
                    m_fb.Value(m_idx_segPrsity) = seg.segPoro
                    m_fc.InsertFeature(m_fb)
                Next
                path.Clear()
                path = Nothing
            End If
        Catch ex As Exception
            If exeptionIsWarning Then
                Trace.WriteLine("[Warning] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            Else
                Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            End If

            tracksingle = False
        Finally
            Trace.Unindent()
        End Try

        tracksingle = True
    End Function

    ''' <summary>
    ''' Begins the tracking.  After it is complete, the feature class will be available from
    ''' the ParticleTracks property
    ''' </summary>
    ''' <returns>True on success, false on error</returns>
    ''' <remarks>
    ''' If the class was constructed with the constructor that takes a single point, then just that point
    ''' will be tracked.  
    ''' If the class was constructed with the constructor that takes a feature class of points, then each
    ''' point will be tracked.
    ''' </remarks>
    Public Function track() As Boolean

        Utilities.outputSystemInfo()
        Trace.Indent()

        'return true unless an error occurs
        track = True

        Try
            If m_ptracksfc Is Nothing Then
                'attempt to create an empty particle track feature class
                If Not createNewShapefile() Then
                    Throw New Exception("Could not create the blank shapefile " & m_shp_fname)
                End If
            Else
                Throw New Exception("Tracking has already been run for this instance.")
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message)
            Return False
        End Try

        Try
            'create insert buffer
            m_fc = m_ptracksfc.Insert(True)
            m_fb = m_ptracksfc.CreateFeatureBuffer
            If Not m_trackpt Is Nothing Then
                'track a single point
                track = tracksingle(m_trackpt, 0)
            ElseIf Not m_trackpts Is Nothing Then
                'track multiple points


                Dim fcur As IFeatureCursor = Utilities.getCursor(m_trackpts, Nothing, False)
                If fcur Is Nothing Then Throw New Exception("Feature cursor is nothing")

                Dim p As ESRI.ArcGIS.Geometry.IPoint

                'get the first feature
                Dim feature As IFeature = fcur.NextFeature
                Dim count As Integer = m_trackpts.FeatureCount(Nothing)
                Dim i As Integer = 1
                Dim erroccured As Boolean = False

                If feature Is Nothing Then Throw New Exception("No points to track in the given feature class")

                'loop through all the points and generate a track for each.
                While Not feature Is Nothing And Not m_trackingcancelled

                    p = CType(feature.Shape, ESRI.ArcGIS.Geometry.IPoint)

                    'important: need to project the point into the map's spatial reference
                    'so that the points will be in the right spot.
                    p.Project(m_spatialref)

                    Trace.WriteLine(i & " of " & count & vbTab & m_trackpts.OIDFieldName & ": " & feature.OID.ToString("0000") & " - " & " x: " & p.X & vbTab & " y: " & p.Y)
                    If Not tracksingle(p, feature.OID) Then
                        'if there is an error tracking a point, dont abort, continue with the rest.
                        erroccured = True
                    End If

                    feature = fcur.NextFeature
                    i += 1

                    If i Mod 100 = 0 Then
                        m_fc.Flush()
                    End If

                    Windows.Forms.Application.DoEvents()
                End While
                If erroccured Then
                    Trace.WriteLine("There was an error processing some points. Check the log for details")
                End If

                ComReleaser.ReleaseCOMObject(fcur)
            Else
                Throw New Exception("Tracking error: no points to track")
            End If
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            track = False
        Finally
            If Not m_fc Is Nothing Then
                'write the buffer to disk                
                m_fc.Flush()
            End If
        End Try

        ComReleaser.ReleaseCOMObject(m_fc)
        ComReleaser.ReleaseCOMObject(m_fb)

        Trace.Unindent()
        Return track
    End Function

    Public Sub cancelTrack()
        Trace.WriteLine("Cancelling particle track...")
        m_trackingcancelled = True
    End Sub

    Private Class PathSegment
        Public shape As Polyline
        Public id As Integer
        Public steps As Integer
        Public segVel As Single
        Public totDist As Single
        Public totTime As Single
        Public segPoro As Single
        Public segWbID As Integer        
    End Class
End Class

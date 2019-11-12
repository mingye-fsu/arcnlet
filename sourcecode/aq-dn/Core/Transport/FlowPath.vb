Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.ADF

Public Class FlowPath
    'given members
    Private m_segments As List(Of FlowSegment)  'segments are assumed sorted by segid
    Private m_pathID As Integer = -99
    Private m_pathWBID As Integer = -99

    'calculated members
    Private m_flowpath As Polyline
    Private m_avgpor As Single = -99
    Private m_avgvel As Single = -99
    Private m_startangle As Single = Single.NaN
    Private m_plumedist As Single = -99
    Private m_plumetime As Single = -99

    Private m_startingpoint As Point
    Private m_stepsize As Single = -99
    Private m_pathdist As Single = -99
    Private m_pathtime As Single = -99

#Region "Properties"
    '*******************************************************************************************
    'properties
    '*******************************************************************************************
    Public ReadOnly Property PathID() As Integer
        Get
            Return m_pathID
        End Get
    End Property
    Public ReadOnly Property PathWBID() As Integer
        Get
            Return m_pathWBID
        End Get
    End Property    
    Public ReadOnly Property StartAngle() As Single
        Get
            If m_startangle < 0 Then Throw New Exception("Can't get the start angle for PathID=" & PathID & ". Path not initialized!")
            Return m_startangle
        End Get
    End Property
    Public ReadOnly Property StepSize() As Single
        Get            
            'assumes a constant stepsize           
            If m_stepsize <= 0 Then Throw New Exception("Can't get the step size for PathID=" & PathID & ". Path not initialized!")
        End Get
    End Property

    Public ReadOnly Property AvgPorosity() As Single
        Get
            If m_avgpor < 0 Then Throw New Exception("Can't get the average porosity for PathID=" & PathID & ". Path not initialized!")
            Return m_avgpor
        End Get
    End Property
    Public ReadOnly Property AvgVelocity() As Single
        Get
            If m_avgvel < 0 Then Throw New Exception("Can't get the average velocity for PathID=" & PathID & ". Path not initialized!")
            Return m_avgvel
        End Get
    End Property

    ''' <summary>
    ''' The distance to the plume advection front, x=vt.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>For steady state problems, this will be equal to PathDist</remarks>
    Public ReadOnly Property PlumeDist() As Single
        Get
            If m_plumedist < 0 Then Throw New Exception("Can't get the plume distance for PathID=" & PathID & ". Path not initialized!")
            Return m_plumedist
        End Get
    End Property

    ''' <summary>
    ''' The time to the plume advection front.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>For steady state problems this is equal to PathTime</remarks>
    Public ReadOnly Property PlumeTime() As Single
        Get
            If m_plumedist < 0 Then Throw New Exception("Can't get the plume time for PathID=" & PathID & ". Path not initialized!")
            Return m_plumetime
        End Get
    End Property

    ''' <summary>
    ''' The total length of the path.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PathDist() As Single
        Get            
            If m_pathdist <= 0 Then Throw New Exception("Can't get the path distance for PathID=" & PathID & ". Path not initialized!")
            Return m_pathdist
        End Get
    End Property
    ''' <summary>
    ''' The total time taken to travel the path
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PathTime() As Single
        Get            
            If m_pathtime <= 0 Then Throw New Exception("Can't get the path time for PathID=" & PathID & ". Path not initialized!")
            Return m_pathtime
        End Get
    End Property

    '*******************************************************************************************
    'end properties
    '*******************************************************************************************
#End Region


    Public Sub New(ByVal PathID As Integer, ByVal PathWBID As Integer)
        m_pathID = PathID
        m_pathWBID = PathWBID
        m_segments = New List(Of FlowSegment)
        m_segments.Capacity = 1000
    End Sub
    Protected Overrides Sub Finalize()
        If Not m_segments Is Nothing Then
            For Each segment As FlowSegment In m_segments
                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(segment.shape)
            Next
            m_segments.Clear()
            m_segments = Nothing
        End If
        ComReleaser.ReleaseCOMObject(m_flowpath)
        m_flowpath = Nothing
        MyBase.Finalize()
    End Sub
    Public Sub clearData()
        Me.Finalize()
    End Sub

    ''' <summary>
    ''' used to build the list of flow segments used by calculatePath.
    ''' </summary>
    ''' <param name="seg">A flow segment of the flow path, Subsequent calls must correspond to
    ''' a segment of the same flowpath, in order starting from the path origin</param>
    ''' <remarks></remarks>
    Public Sub AddSegment(ByVal seg As FlowSegment)
        m_segments.Add(seg)
    End Sub

    ''' <summary>
    ''' Returns the generated single polyline version of the flow path
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function getFlowPath() As Polyline
        If m_flowpath Is Nothing Then Throw New Exception("Path for PathID=" & PathID & " not yet initialized. Can't get flow path!")
        Return m_flowpath
    End Function

    ''' <summary>
    ''' Calculates path parameters Avgerage Porosity, Average Velocity, Initial Flow Direction Angle
    ''' given a plume length and time.
    ''' </summary>
    ''' <param name="solutionTime">The time of the solution. For steady state solutions, this is -1</param>
    ''' <remarks></remarks>
    Public Sub calculatePath(ByVal solutionTime As Single)

        Dim tpoly As Polyline                           'associated segment line
        Dim pt1, pt2 As Point                           'end points of a given line segment
        Dim path_dist As Single                         'the total path distance
        Dim path_time As Single                         'the total path time
        Dim plume_dist As Single                        'the distance to the advection front 
        Dim plume_time As Single                        'the time to the advection front in the absence of decay.
        Dim t_time As Single                            'the cumulative travel time
        Dim t_dist As Single                            'the cumulative travel distance
        Dim avg_vel As Single                           'average path velocity
        Dim seg_vel As Single                           'velocity of the current segment
        Dim avg_por As Single                           'average path porosity
        Dim seg_por As Single                           'segment porosity
        Dim seg_count As Integer                        'count of the # of segments in this path
        Dim prev_dist As Single                         'The cumulative distance of the previous segment. make sure the segments are in order
        Dim prev_time As Single                         'the cumulative time of the previous segment
        Dim weight As Single
        Dim avg_vel_calc_done As Boolean                'ends the average velocity calculation            
        Dim wbid As Integer                         'the ending water body id of the current path


        If m_segments.Count <= 0 Then Throw New Exception("Path for PathID=" & PathID & " not yet initialized!")

        'get the first segment
        Dim seg As FlowSegment = m_segments(0)

        m_stepsize = seg.totDist
        m_pathdist = m_segments(m_segments.Count - 1).totDist
        m_pathtime = m_segments(m_segments.Count - 1).totTime

        'keep track of the distance. make sure the next distance value is greater than the
        'current one. this ensures that the segments are in order
        'also note that the value of the first segment is the step size (constant step size
        'assumed)
        prev_dist = seg.totDist
        prev_time = seg.totTime

        'using the first poly line, find the start and direction (from north) of the plume
        tpoly = seg.shape
        pt1 = tpoly.Point(0)
        pt2 = tpoly.Point(1)
        m_startangle = angleFromPoints(pt1, pt2)

        'add the first two points to the single polyline flowpath
        m_flowpath = New Polyline
        m_flowpath.AddPoint(pt1)
        m_flowpath.AddPoint(pt2)

        ComReleaser.ReleaseCOMObject(pt1)
        ComReleaser.ReleaseCOMObject(pt2)
        tpoly = Nothing
        pt1 = Nothing
        pt2 = Nothing

        '**********************************************
        'calculate the average velocity and porosity
        '**********************************************
        t_dist = prev_dist
        t_time = prev_time
        path_dist = t_dist
        path_time = t_time
        plume_dist = t_dist
        plume_time = t_time
        avg_vel = 1 / seg.segVel      'harmonic  mean
        avg_por = seg.segPor          'arithmetic mean
        seg_count = 1
        avg_vel_calc_done = False
        For Each sgmnt As FlowSegment In m_segments
            If Not sgmnt.segID = 0 Then     'ignore the first segment since we already included it above

                'keep adding the ending point of each segment to the polyline
                m_flowpath.AddPoint(sgmnt.shape.Point(1))

                t_dist = sgmnt.totDist      'cumulative travel distance
                t_time = sgmnt.totTime      'cumulative travel time
                seg_vel = sgmnt.segVel      'segment velocity
                seg_por = sgmnt.segPor      'segment porosituy

                'check to make sure the segments are in order
                If (t_dist <= prev_dist) Then Throw New Exception("Path segment is corruputed or has zero length")

                'save the latest water body id. used later to determine if the path ends at a water body or not
                wbid = sgmnt.wbID

                'check to make sure we stop at the location along the path corresponding to m_time
                'note that when we reach the desired segment, we dont want to count any
                'subsequent segments in the average velocity calculation but we still want to traverse
                'the rest of the segments to get the ending time as well as to check for integrity
                'if m_time=-1 (i.e. use path travel time as plume time, currently the only option), 
                'avg_vel_calc_done will never be set therefore the calculation includes the entire 
                'path in the avg vel calculation
                weight = 1
                If Not avg_vel_calc_done Then
                    'set the plume distance and time to the current distance and time.
                    plume_dist = t_dist
                    plume_time = t_time

                    'check to see if we've reached the required location along the path,
                    'corresponding to m_time
                    If solutionTime > 0 Then
                        If t_time = solutionTime Then
                            'the ending time of the current segment happens to correspond to the
                            'plume time. Exiting the loop now will cause
                            'the correct average velocity to be calculated with no further calculations required
                            plume_time = t_time
                            plume_dist = t_dist
                            avg_vel_calc_done = True
                        ElseIf t_time > solutionTime Then
                            'this means we've gone past the location corresponding to the plume time
                            'The correct travel distance should be in between the end of the last
                            'segment and the end of the current segment.

                            'find out how far along is the plume time from the start of the interval
                            '(number between 0 and 1)
                            Dim fracdist As Single
                            fracdist = (solutionTime - prev_time) / (t_time - prev_time)

                            'get the true advection front distance and time
                            plume_time = prev_time + fracdist * (t_time - prev_time)
                            plume_dist = prev_dist + fracdist * (t_dist - prev_dist)

                            weight = (fracdist * (t_dist - prev_dist)) / (t_dist - prev_dist)

                            avg_vel_calc_done = True
                        End If
                    End If

                    'update the average with the possibly new weight
                    seg_count = seg_count + weight
                    avg_vel = avg_vel + weight / seg_vel
                    avg_por = avg_por + seg_por
                End If

                prev_dist = t_dist
                prev_time = t_time
                path_dist = t_dist
                path_time = t_time
            End If
        Next

        If t_dist <> PathDist Then Throw New Exception("Path with PathID=" & PathID & "is corrupted. t_dist(" & t_dist & ")<>path_dist(" & PathDist & ")")
        If t_time <> PathTime Then Throw New Exception("Path with PathID=" & PathID & "is corrupted. t_time(" & t_dist & ")<>path_time(" & PathTime & ")")

        m_plumedist = plume_dist
        m_plumetime = plume_time

        'find the HARMONIC MEAN velocity
        m_avgvel = seg_count / avg_vel

        'find the arthmethic mean porosity
        m_avgpor = avg_por / seg_count

        For Each segment As FlowSegment In m_segments
            ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(segment.shape)
            segment.shape = Nothing
        Next
        m_segments.Clear()
        m_segments = Nothing
    End Sub


    ''' <summary>
    ''' Calculates the angle clockwise from north made by the line defined by the points pt1 and pt2
    ''' </summary>
    ''' <param name="pt1">The starting point of the line</param>
    ''' <param name="pt2">The ending point of the line</param>
    ''' <returns>The angle in degrees</returns>
    ''' <remarks></remarks>
    Private Function angleFromPoints(ByVal pt1 As Point, ByVal pt2 As Point) As Single
        Dim ang As Single

        If pt2.X >= pt1.X And pt2.Y > pt1.Y Then
            'first quadrant (includes 0, excludes 90)
            ang = Math.Atan((pt2.X - pt1.X) / (pt2.Y - pt1.Y)) * 57.2957795
        ElseIf pt2.X > pt1.X And pt2.Y <= pt1.Y Then
            'second quadrant (includes 90, excludes 180)
            ang = 90 + Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X)) * 57.2957795
        ElseIf pt2.X <= pt1.X And pt2.Y < pt1.Y Then
            'third quadrant (includes 180, excludes 270)
            ang = 180 + Math.Atan((pt2.X - pt1.X) / (pt2.Y - pt1.Y)) * 57.2957795
        ElseIf pt2.X < pt1.X And pt2.Y >= pt1.Y Then
            'fourth quadrant (includes 270, excludes 360)
            ang = 270 + Math.Atan((pt2.Y - pt1.Y) / (pt2.X = pt1.Y)) * 57.2957795
        Else
            Throw New Exception("Error converting angle")
        End If

        Return ang
    End Function
End Class

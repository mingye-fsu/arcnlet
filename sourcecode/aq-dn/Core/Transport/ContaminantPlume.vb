Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.Geodatabase
''' <summary>
''' Represents a contaminant plume
''' </summary>
''' <remarks>This class represents the solution of the analytical equation
''' assuming the path is a straight line (no bends/segments)</remarks>
Friend Class ContaminantPlume

    Private m_pathID As Integer                     'associated pathID
    Private m_pathLength As Single                  'lenght of associated path (not the plume length)
    Private m_pathTime As Single                    'total path time (not the plume time)    
    Private m_origin_direction As Single            'starting direction of the plume (wrt to north)
    Private m_plumetime As Single
    Private m_plumevolume As Single                 '
    Private m_plumeSoilAvgPorosity As Single        'The average porosity that the centerline of the plume traverses
    Private m_concThresh As Single
    Private m_concInit As Single
    Private m_concNext As Single                    'the concentration at cell C(x=deltaX,0,0)
    Private m_meshsize_x As Single
    Private m_meshsize_y As Single
    Private m_meshsize_z As Single
    Private m_plumecells As List(Of List(Of List(Of Single)))
    Private m_Y As Single                           'Y - dimensions of source plane
    Private m_Z As Single                           'Z - dimensions of source plane, no meaning if 2D
    Private m_ax, m_ay, m_az As Single              'dispersivities
    Private m_k As Single                           'decay coefficient
    Private m_vel As Single                         'the velocity used
    Private m_wbid As Integer                       'the FID of the water body that the PATH ends up in
    Private m_plumeLength As Single                 'the actual plume length (not counting the extra part that was evaluated for visualization)
    Private m_plumeTotalCells As Single             'the actual number of cells (not counting the extra part for viz)
    Private m_solType As SolutionTypes.SolutionType
    Private m_srcLocation As Point                  'the location of the plume origin
    Private m_z_max As Single
    Private m_z_max_checked As Boolean

    ''' <summary>
    ''' The source point feature corresponding to the plume origin (0,0,0)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property SourceLocation() As Point
        Get
            Return m_srcLocation
        End Get
    End Property

    ''' <summary>
    ''' The analytical solution used to calculate this plume
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property SolutionType() As SolutionTypes.SolutionType
        Get
            Return m_solType
        End Get
    End Property

    ''' <summary>
    ''' The concentration at cell deltaX, ie. C(x=deltaX,y=0,z=0)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>Used by the denitrificaiton module</remarks>
    Public ReadOnly Property ConcNext() As Single
        Get
            Return m_concNext
        End Get
    End Property

    ''' <summary>
    ''' The average porosity along the plume centerline
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property Porosity() As Single
        Get
            Return m_plumeSoilAvgPorosity
        End Get
    End Property

    ''' <summary>
    ''' The mesh size in the x direction
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property MeshSzDX() As Single
        Get
            Return m_meshsize_x
        End Get
    End Property
    ''' <summary>
    ''' The mesh size in the y direction
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property MeshSzDY() As Single
        Get
            Return m_meshsize_y
        End Get
    End Property
    ''' <summary>
    ''' The mesh size in the z direction
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property MeshSzDZ() As Single
        Get
            If m_z_max_checked Then
                If m_meshsize_z <= m_z_max Then
                    Return m_meshsize_z
                Else
                    Return m_z_max
                End If
            Else
                Return m_meshsize_z
            End If
        End Get
    End Property

    ''' <summary>
    ''' The FID of the water body that the flow path corresponding to this plume terminates at
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DestinationWaterbodyID_Path() As Integer
        Get
            Return m_wbid
        End Get
    End Property
    ''' <summary>
    ''' The FID of the water body that the plume terminates at.  If the plume length is >= Path length
    ''' then this will return the same number as <see cref="DestinationWaterbodyID_Path"/>. Else
    ''' -1 is returned indicating this plume does not reach the water body
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DestinationWaterbodyID_Plume() As Integer
        Get
            If PlumeLength >= PathLength Then
                Return DestinationWaterbodyID_Path
            Else
                Return -1
            End If
        End Get
    End Property


    ''' <summary>
    ''' The initial concentration
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ConcInit() As Single
        Get
            Return m_concInit
        End Get
    End Property

    ''' <summary>
    ''' The threshold concentration
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ConcThresh() As Single
        Get
            Return m_concThresh
        End Get
    End Property

    ''' <summary>
    ''' Decay coefficient
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DecayCoeff() As Single
        Get
            If SolutionTypes.hasDecay(m_solType) Then
                DecayCoeff = m_k
            Else
                DecayCoeff = 0
            End If
        End Get
    End Property

    ''' <summary>
    ''' Longitudinal dispersivity
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DispL() As Single
        Get
            Return m_ax
        End Get
    End Property

    ''' <summary>
    ''' Tranverse horizontal dispersivity
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DispTH() As Single
        Get
            Return m_ay
        End Get
    End Property

    ''' <summary>
    ''' Transverse Vertical dispersivity
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DispTV() As Single
        Get
            Return m_az
        End Get
    End Property

    Public ReadOnly Property Velocity() As Single
        Get
            Return m_vel
        End Get
    End Property

    ''' <summary>
    ''' Plume volume
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeVolume() As Single
        Get
            Return m_plumevolume
        End Get
    End Property


    ''' <summary>
    ''' The angle wrt to grid north that the plume will initially take
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property DirectionAngle() As Single
        Get
            Return m_origin_direction
        End Get
    End Property

    ''' <summary>
    ''' The associated pathID (septic tank id) to which the plume corresponds
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PathID() As Integer
        Get
            Return m_pathID
        End Get
    End Property

    ''' <summary>
    ''' The length of the path given by PathID
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PathLength() As Single
        Get
            Return m_pathLength
        End Get
    End Property

    ''' <summary>
    ''' The travel time of the path given by PathID
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PathTime() As Single
        Get
            Return m_pathTime
        End Get
    End Property

    ''' <summary>
    ''' The time for which the plume was evaluated (if applicable)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeTime() As Single
        Get
            Return m_plumetime
        End Get
    End Property

    ''' <summary>
    ''' The width of the plume
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeWidth() As Single
        Get
            Return Me.PlumeWidthCells * m_meshsize_y
        End Get
    End Property

    Public ReadOnly Property source_Y() As Single
        Get
            Return m_Y
        End Get
    End Property
    Public ReadOnly Property source_Z() As Single
        Get
            Return m_Z
        End Get
    End Property

    ''' <summary>
    ''' The width of the plume given as the number of cells that span the plume width
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeWidthCells() As Integer
        Get
            'assumes plume is widest along the center line
            Dim maxwidth As Integer = 0
            For Each y As List(Of List(Of Single)) In m_plumecells
                If y.Count > maxwidth Then
                    maxwidth = y.Count
                End If
            Next

            'if the dimensions of the source are even, the plume raster width
            'should have an even number of cells. if its odd it should have
            'an odd number less than the even amount.
            'The reason for this is so the plume is mirrored correctly when
            'there are an even or odd number of cells.
            If Utilities.DivRem(Math.Round(m_Y / m_meshsize_y), 2) = 0 Then
                Return maxwidth * 2
            Else
                Return maxwidth * 2 - 1
            End If
        End Get
    End Property

    ''' <summary>
    ''' Plume height
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeHeight() As Single
        Get
            Return Me.PlumeHeightCells * m_meshsize_z
        End Get
    End Property

    ''' <summary>
    ''' Plume height given as the number of cells that span the plume height
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeHeightCells() As Integer
        Get
            Dim maxheight As Integer = 0
            For Each y As List(Of List(Of Single)) In m_plumecells
                For Each z As List(Of Single) In y
                    If z.Count > maxheight Then
                        maxheight = z.Count
                    End If
                Next
            Next

            Return maxheight
        End Get
    End Property

    ''' <summary>
    ''' Plume length.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>This is the same as PlumeLengthCells * MeshSize</remarks>
    Public ReadOnly Property PlumeLength() As Single
        Get
            Return m_plumecells.Count * m_meshsize_x
        End Get
    End Property

    ''' <summary>
    ''' Plume length given as the number of cells that span the length of the plume.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeLengthCells() As Integer
        Get
            Return m_plumecells.Count
        End Get
    End Property
    ''' <summary>
    ''' The length of the plume up to the tip.  If the plume was truncated, at the water body,
    '''  this length is the length up to the water body    
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property PlumeTruncatedLength() As Single
        Get
            Return m_plumeLength
        End Get
    End Property

    Public ReadOnly Property PlumeTruncatedLengthCells() As Integer
        Get
            Return PlumeTruncatedLength / MeshSzDX
        End Get
    End Property

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="pathid">The PathID of particle path that this plume is associated with</param>
    ''' <param name="pathlength">The length of the path given by PathID</param>
    ''' <param name="sourceLocation">A point specifying the location of this plume's source</param>
    ''' <param name="pathtime">Time total travel time of PathID</param>
    ''' <param name="Y">The Y dimension of the source plane</param>
    ''' <param name="Z">The Z dimension of the source plane.  This has no meaning if the plume is 2D</param>
    ''' <param name="angle">The initial angle the plume takes clockwise wrt to north</param>
    ''' <param name="plumecellarray">The plume.</param>
    ''' <param name="plume_time">The time at which the plume was evaluated</param>
    ''' <param name="plume_volume">The plume volume.  If the plume is 2D, this is the plume area</param>
    ''' <param name="mesh_cell_size_x">The x mesh size the plume was evaluated on</param>
    ''' <param name="mesh_cell_size_y">The y mesh size the plume was evaluated on</param>
    ''' <param name="mesh_cell_size_z">The z mesh size the plume was evaluated on.  This has no meaning for a 2D plume</param>
    ''' <param name="initialConcentration">The initial concentration of the source plane</param>
    ''' <param name="concentrationThreshold">The cutoff concentration of the plume</param>
    ''' <param name="avgVel">The seepage velocity of this plume</param>
    ''' <param name="decay">The decay consntant of this plume</param>
    ''' <param name="dispL">The longitudinal dispersivity of this plume</param>
    ''' <param name="dispTH">The transverse horizontal dispersivity of this plume</param>
    ''' <param name="dispTV">The transverse vertical dispersivity of this plume</param>
    ''' <param name="EndingWaterBodyID">The FID of the water body that this plume terminates at. -1 if this plume doesn't terminate at a water body</param>
    ''' <param name="nextConcentration">The concentration of the cell @x=<paramref name="mesh_cell_size_x"/> and y=0</param>
    ''' <param name="plume_avg_porosity">The porosity used for this plume (for calculating denitrification)</param>
    ''' <param name="plumeLength">The length of the plume (in map  units)</param>
    ''' <param name="plumeTotalCells">The total number of cells in the plume</param>
    ''' <param name="solutionType">The analytical solution used to calcuate the plume.</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal pathid As Integer, _
                   ByVal pathlength As Single, _
                   ByVal pathtime As Single, _
                   ByVal Y As Single, _
                   ByVal Z As Single, _
                   ByVal angle As Single, _
                   ByVal plumecellarray As List(Of List(Of List(Of Single))), _
                   ByVal plume_time As Single, _
                   ByVal plume_volume As Single, _
                   ByVal plume_avg_porosity As Single, _
                   ByVal mesh_cell_size_x As Single, _
                   ByVal mesh_cell_size_y As Single, _
                   ByVal mesh_cell_size_z As Single, _
                   ByVal nextConcentration As Single, _
                   ByVal initialConcentration As Single, _
                   ByVal concentrationThreshold As Single, _
                   ByVal decay As Single, _
                   ByVal dispL As Single, ByVal dispTH As Single, ByVal dispTV As Single, _
                   ByVal avgVel As Single, _
                   ByVal EndingWaterBodyID As Integer, _
                   ByVal plumeLength As Single, _
                   ByVal plumeTotalCells As Single, _
                   ByVal solutionType As SolutionTypes.SolutionType, _
                   ByVal sourceLocation As Point, _
                   ByVal plume_z_max As Single, _
                   ByVal plume_z_max_checked As Boolean)
        m_pathID = pathid
        m_pathLength = pathlength
        m_pathTime = pathtime
        m_concInit = initialConcentration
        m_concThresh = concentrationThreshold
        m_meshsize_x = mesh_cell_size_x
        m_meshsize_y = mesh_cell_size_y
        m_meshsize_z = mesh_cell_size_z
        m_plumetime = plume_time
        m_plumevolume = plume_volume
        m_plumecells = plumecellarray
        m_origin_direction = angle
        m_Y = Y
        m_Z = Z
        m_k = decay
        m_ax = dispL
        m_ay = dispTH
        m_az = dispTV
        m_vel = avgVel
        m_wbid = EndingWaterBodyID
        m_plumeLength = plumeLength
        m_plumeTotalCells = plumeTotalCells
        m_plumeSoilAvgPorosity = plume_avg_porosity
        m_concNext = nextConcentration
        m_solType = solutionType
        m_srcLocation = sourceLocation
        m_z_max = plume_z_max
        m_z_max_checked = plume_z_max_checked
    End Sub

    ''' <summary>
    ''' Returns an matrix containg a cross sectional slice of the plume on the (x,y,0) plane)
    ''' </summary>
    ''' <param name="controlpts_bdy">The control points defining the edge of the plume</param>
    ''' <param name="controlpts_center">The control points defining the center of the plume</param>
    ''' <param name="controlpts_bdy_frac">The fractional distance along the plume corresponding to each opposing 
    ''' pair of boundary control points</param>
    ''' <param name="controlpts_center_frac">The fractional distance along the plume corresponding
    ''' to each control point along the plume center line</param>
    ''' <param name="ControlPtSpacing">Adds a control point every ControlPtDensity number of cells.
    ''' For example, the default value of 2 adds a control point every 2 cells along the plume.
    ''' Note if you make the points too dense, the calculation will take longer.  The choice of this parameter
    ''' depends on the choice of the plume mesh size, and the length of the plume i.e. if the mesh size is too coarse, you should select
    ''' a smaller value for this parameter since there might not be enough cells in the plume for larger
    ''' point densities.  In principle, larger values (i.e. points farther apart) will give faster calculation
    ''' at the expense of less accuracy. </param>
    ''' <returns>A 2D array containing the plume cross section.  On error, Nothing is returned and the 
    ''' control point arrays will be empty </returns>
    ''' <remarks>This function simultaneously calculates the control points and target points for 
    ''' warping the plume</remarks>
    Public Function getPlumeSectXY0(ByRef controlpts_bdy As List(Of Pnt), ByRef controlpts_center As List(Of Pnt), _
                                    ByRef controlpts_bdy_frac As List(Of Single), ByVal controlpts_center_frac As List(Of Single), _
                                    Optional ByVal ControlPtSpacing As Integer = 48) As Single(,)

        'for short plumes, try to find the largest spacing that still gives more than 10 points
        'along the centerline. For plumes where its not possible to find this spacing,
        'the spacing will be set to every cell.
        Dim npts As Integer = Math.Floor(Me.PlumeLengthCells / ControlPtSpacing)
        If Me.PlumeLengthCells < 3 Then Throw New Exception("Plume is too short to warp. Skipping")
        While npts < 10
            ControlPtSpacing = ControlPtSpacing / 2
            npts = Math.Floor(Me.PlumeLengthCells / ControlPtSpacing)
            If ControlPtSpacing = 1 Then
                Exit While
            End If
        End While

        Dim ret(,) As Single = Nothing
        Try
            Dim nrows As Integer = Me.PlumeLengthCells
            Dim ncols As Integer = Me.PlumeWidthCells

            ReDim ret(nrows - 1, ncols - 1)
            Dim i As Integer
            Dim j As Integer


            Dim p As Pnt                                    'multi-use point variable

            'used to make sure there are no duplicate control points
            Dim ctrlpts_bdy_hash As New Hashtable
            Dim ctrlpts_center_hash As New Hashtable
            Dim key As String

            Dim slice As Integer = 0

            'clear the input arrays
            controlpts_bdy.Clear()
            controlpts_center.Clear()
            controlpts_bdy_frac.Clear()
            controlpts_center_frac.Clear()

            i = 0
            For Each y As List(Of List(Of Single)) In m_plumecells
                'add the cells on the z=0 plane to the right (positive) half of the array
                j = Math.Floor(ncols / 2)
                For Each z As List(Of Single) In y
                    If z.Count >= slice + 1 Then
                        ret(i, j) = z(slice)
                        j = j + 1
                    End If
                Next

                'add control points along the edge of the plume with a given spacing
                If i Mod ControlPtSpacing = 0 Then
                    'ret(i, j - 1) = 1
                    p = New Pnt
                    p.SetCoords(i, j - 1)
                    controlpts_bdy.Add(p)
                    key = p.X & "," & p.Y
                    ctrlpts_bdy_hash.Add(key, Nothing)
                    p = New Pnt
                    p.SetCoords(i, ncols - (j - 1) - 1)
                    key = p.X & "," & p.Y
                    controlpts_bdy.Add(p)
                    If Not ctrlpts_bdy_hash.ContainsKey(key) Then
                        ctrlpts_bdy_hash.Add(key, Nothing)
                    End If


                    'add the location of the control points on the boundaries
                    'as a fraction of the total plume length      
                    'correct for the fact that we want to measure from the cell center
                    'not the cell edge by adding half of the mesh size to the distance
                    controlpts_bdy_frac.Add(((i * m_meshsize_x) + (m_meshsize_x / 2)) / Me.PlumeLength)
                End If

                i = i + 1
            Next

            'add the center control points        
            For i = 0 To nrows - 1 Step ControlPtSpacing
                p = New Pnt
                p.SetCoords(i, Math.Floor(ncols / 2))
                key = p.X & "," & p.Y
                If Not ctrlpts_bdy_hash.ContainsKey(key) AndAlso Not ctrlpts_center_hash.Contains(key) Then
                    controlpts_center.Add(p)
                    controlpts_center_frac.Add(((i * m_meshsize_x) + (m_meshsize_x / 2)) / Me.PlumeLength)
                    ctrlpts_center_hash.Add(key, Nothing)
                End If
            Next
            'make sure there is always a control point at the tip of the plume
            p = New Pnt
            p.SetCoords(nrows - 1, ncols / 2)
            key = p.X & "," & p.Y
            If Not ctrlpts_bdy_hash.ContainsKey(key) AndAlso Not ctrlpts_center_hash.Contains(key) Then
                controlpts_center.Add(p)
                controlpts_center_frac.Add(nrows * m_meshsize_x / Me.PlumeLength)
            End If


            'mirror the half plume to generate the whole thing
            'note if the number of cells which make up the source is even, 
            'then this loop will generate an extra column of pixels at the source
            'this is ok for now since we can correct it later if we have to.
            'Note: numsourcecells is an ugly hack since for some reason, the wrong
            'value was passed to the DivRem function for certain combinations of
            'source_Y and MeshSzDY, even though the right values were shown in the debugger.
            'e.g. source_Y=12 and MeshSzDY=0.6: 12/0.6=20. Floor(20) should be 20
            'however 19 was being passed to the function. probably due to a floating point issue.
            Dim lb As Integer
            Dim numsourcecells As String = Me.source_Y / Me.MeshSzDY
            numsourcecells = Math.Floor(Single.Parse(numsourcecells))
            If Utilities.DivRem(numsourcecells, 2) = 0 Then
                lb = 1
            Else
                lb = 0
            End If
            For i = 0 To nrows - 1
                For j = Math.Floor(ncols / 2) - 1 To lb Step -1
                    ret(i, j) = ret(i, Math.Floor(ncols / 2) + (Math.Floor(ncols / 2) - j))
                Next
            Next
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.Message & ex.StackTrace)
        End Try
        Return ret
    End Function

    ''' <summary>
    ''' Deletes the plume to free memory
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub deleteData()
        m_plumecells.Clear()
        m_plumecells = Nothing
    End Sub
End Class

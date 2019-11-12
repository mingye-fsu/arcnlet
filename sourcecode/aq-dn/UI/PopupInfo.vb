Imports ESRI.ArcGIS.DataSourcesRaster   'RasterProps
Imports ESRI.ArcGIS.Carto               'RasterLayer, FeatureLayer
Imports ESRI.ArcGIS.Geodatabase         'IgeoDataset, IDataset
Imports ESRI.ArcGIS.Geometry

''' <summary>
''' A popup window that can be used to display any text. 
''' </summary>
''' <remarks> To use it, create a new instance
''' then when you are ready to show it, call the Show() method, optionally specifying the owning form.
''' the window will then appear wherever the mouse was located when the class was initially constructed.
''' This class is meant to be constructed within the click events of form controls.
''' </remarks>
Public Class PopupInfo

    Private m_text As String = ""
    Private m_title As String = ""

    ''' <summary>
    ''' Constructor.
    ''' </summary>
    ''' <param name="text">The text that will appear in the popup window</param>
    ''' <param name="title">The title of the window</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal text As String, ByVal title As String)

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        m_text = text
        m_title = title
        showform()
    End Sub

    ''' <summary>
    ''' Overloaded Constructor
    ''' </summary>
    ''' <param name="l">A raster layer to show the info of</param>
    ''' <param name="title"></param>
    ''' <remarks></remarks>
    Public Sub New(ByVal l As RasterLayer, ByVal title As String)
        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        m_title = title

        Dim r As IRaster2 = l.Raster
        Dim rp As IRasterProps = CType(r, IRasterProps)
        Dim projCoordSyst As IProjectedCoordinateSystem = CType(rp.SpatialReference, IProjectedCoordinateSystem)
        Dim geogCoordSyst As IGeographicCoordinateSystem = projCoordSyst.GeographicCoordinateSystem


        m_text = "Name:" & vbTab & vbTab & l.Name & vbCrLf
        m_text = m_text & "BrowseName:" & vbTab & CType(r.RasterDataset, IDataset).BrowseName & vbCrLf
        m_text = m_text & "Path:" & vbTab & vbTab & l.FilePath & vbCrLf
        m_text = m_text & "Rows:" & vbTab & vbTab & l.RowCount & vbCrLf
        m_text = m_text & "Columns:" & vbTab & vbTab & l.ColumnCount & vbCrLf
        m_text = m_text & "Visible:" & vbTab & vbTab & l.Visible & vbCrLf
        m_text = m_text & "Spatial Reference:" & vbTab & rp.SpatialReference.Name & vbCrLf
        m_text = m_text & "Projection:" & vbTab & projCoordSyst.Projection.Name & vbCrLf
        m_text = m_text & "Geog. Coord. Sys.:" & vbTab & geogCoordSyst.Name & vbCrLf
        m_text = m_text & "Datum:" & vbTab & vbTab & geogCoordSyst.Datum.Name & vbCrLf
        m_text = m_text & "Coordinate Unit:" & vbTab & projCoordSyst.CoordinateUnit.Name & vbCrLf
        m_text = m_text & "Meters Per Unit:" & vbTab & projCoordSyst.CoordinateUnit.MetersPerUnit & vbCrLf
        m_text = m_text & "Pixel Type:" & vbTab & RasterPixelTypes.GetPixelType(rp.PixelType) & vbCrLf
        m_text = m_text & "Cell Size:" & vbTab & vbTab & rp.MeanCellSize.X & ", " & rp.MeanCellSize.Y & vbCrLf

        Me.Height = 250

        showform()
    End Sub


    Public Sub New(ByVal l As FeatureLayer, ByVal title As String)
        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        m_title = title

        Dim l1 As IFeatureLayer = CType(l, IFeatureLayer)
        Dim l2 As IFeatureLayer2 = CType(l, IFeatureLayer2)
        Dim g As IGeoDataset = CType(l, IGeoDataset)
        Dim d As IDataset = CType(l, IDataset)
        m_text = "Name:" & vbTab & vbTab & l1.Name & vbCrLf
        m_text = m_text & "BrowseName:" & vbTab & d.BrowseName & vbCrLf
        m_text = m_text & "Path:" & vbTab & vbTab & CType(l.FeatureClass, IDataset).Workspace.PathName & vbCrLf
        m_text = m_text & "Data Source Type:" & vbTab & l1.DataSourceType & vbCrLf
        m_text = m_text & "Visible:" & vbTab & vbTab & l1.Visible & vbCrLf
        m_text = m_text & "Shape Type:" & vbTab & System.Enum.GetName(GetType(ESRI.ArcGIS.Geometry.esriGeometryType), l2.FeatureClass.ShapeType) & vbCrLf
        m_text = m_text & "Shape Field Name:" & vbTab & l2.FeatureClass.ShapeFieldName & vbCrLf
        m_text = m_text & "Spatial Reference:" & vbTab & g.SpatialReference.Name

        showform()
    End Sub

    Private Sub showform()
        Me.Text = m_title
        Me.TextBox1.AppendText(m_text)
        Me.Location = Windows.Forms.Cursor.Position + New System.Drawing.Point(5, 5)
    End Sub

    Private Sub PopupInfo_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Windows.Forms.Keys.Escape Then
            Me.Close()
        End If
    End Sub
End Class
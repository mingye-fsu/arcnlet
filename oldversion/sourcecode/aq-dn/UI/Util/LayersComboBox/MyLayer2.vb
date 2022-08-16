Imports ESRI.ArcGIS.Framework
Imports ESRI.ArcGIS.ArcMapUI
Imports ESRI.ArcGIS.Carto
''' <summary>
''' Implements ILayer2. this class overrides the tostring function so that we can
''' add layer objects directly into a combobox
''' </summary>
''' <remarks>This class stores an object of type ILayer2. An object of this class
''' can then be stored directly in a combobox control and have the name of the layer 
''' shown as the text</remarks>
Public Class MyLayer2
    Implements ILayer2

    Private m_lyr As ILayer2

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="lr">A layer object that implements the ILayer2 interface</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal lr As ILayer2)
        m_lyr = lr
    End Sub

    Public Property AreaOfInterest() As ESRI.ArcGIS.Geometry.IEnvelope Implements ESRI.ArcGIS.Carto.ILayer2.AreaOfInterest
        Get
            AreaOfInterest = m_lyr.AreaOfInterest
        End Get
        Set(ByVal value As ESRI.ArcGIS.Geometry.IEnvelope)
            m_lyr = value
        End Set
    End Property

    Public Property Cached() As Boolean Implements ESRI.ArcGIS.Carto.ILayer2.Cached
        Get
            Cached = m_lyr.Cached
        End Get
        Set(ByVal value As Boolean)
            m_lyr.Cached = value
        End Set
    End Property

    Public Sub Draw(ByVal DrawPhase As ESRI.ArcGIS.esriSystem.esriDrawPhase, ByVal Display As ESRI.ArcGIS.Display.IDisplay, ByVal TrackCancel As ESRI.ArcGIS.esriSystem.ITrackCancel) Implements ESRI.ArcGIS.Carto.ILayer2.Draw
        m_lyr.Draw(DrawPhase, Display, TrackCancel)
    End Sub

    Public Property MaximumScale() As Double Implements ESRI.ArcGIS.Carto.ILayer2.MaximumScale
        Get
            MaximumScale = m_lyr.MaximumScale
        End Get
        Set(ByVal value As Double)
            m_lyr.MaximumScale = value
        End Set
    End Property

    Public Property MinimumScale() As Double Implements ESRI.ArcGIS.Carto.ILayer2.MinimumScale
        Get
            MinimumScale = m_lyr.MinimumScale
        End Get
        Set(ByVal value As Double)
            m_lyr.MinimumScale = value
        End Set
    End Property

    Public Property Name() As String Implements ESRI.ArcGIS.Carto.ILayer2.Name
        Get
            Name = m_lyr.Name
        End Get
        Set(ByVal value As String)
            m_lyr.Name = value
        End Set
    End Property

    Public ReadOnly Property ScaleRangeReadOnly() As Boolean Implements ESRI.ArcGIS.Carto.ILayer2.ScaleRangeReadOnly
        Get
            ScaleRangeReadOnly = m_lyr.ScaleRangeReadOnly
        End Get
    End Property

    Public Property ShowTips() As Boolean Implements ESRI.ArcGIS.Carto.ILayer2.ShowTips
        Get
            ShowTips = m_lyr.ShowTips
        End Get
        Set(ByVal value As Boolean)
            m_lyr.ShowTips = value
        End Set
    End Property

    Public WriteOnly Property SpatialReference() As ESRI.ArcGIS.Geometry.ISpatialReference Implements ESRI.ArcGIS.Carto.ILayer2.SpatialReference
        Set(ByVal value As ESRI.ArcGIS.Geometry.ISpatialReference)
            m_lyr.SpatialReference = value
        End Set
    End Property

    Public ReadOnly Property SupportedDrawPhases() As Integer Implements ESRI.ArcGIS.Carto.ILayer2.SupportedDrawPhases
        Get
            SupportedDrawPhases = m_lyr.SupportedDrawPhases
        End Get
    End Property

    Public ReadOnly Property TipText(ByVal x As Double, ByVal y As Double, ByVal Tolerance As Double) As String Implements ESRI.ArcGIS.Carto.ILayer2.TipText
        Get
            TipText = m_lyr.TipText(x, y, Tolerance)
        End Get
    End Property

    Public ReadOnly Property Valid() As Boolean Implements ESRI.ArcGIS.Carto.ILayer2.Valid
        Get
            Valid = m_lyr.Valid
        End Get
    End Property

    Public Property Visible() As Boolean Implements ESRI.ArcGIS.Carto.ILayer2.Visible
        Get
            Visible = m_lyr.Visible
        End Get
        Set(ByVal value As Boolean)
            m_lyr.Visible = value
        End Set
    End Property

    ''' <summary>
    ''' The overriden ToString function that allows us to show the layer name in the combobox
    ''' </summary>
    ''' <returns>The name of the layer</returns>
    ''' <remarks></remarks>
    Public Overrides Function ToString() As String
        Return Me.Name
    End Function

    ''' <summary>
    ''' Gets the actual layer object so we can work with it directly if needed
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property BaseLayer()
        Get
            BaseLayer = m_lyr
        End Get
    End Property

End Class

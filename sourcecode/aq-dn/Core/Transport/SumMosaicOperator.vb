Imports ESRI.ArcGIS.DataSourcesRaster

''' <summary>
''' A custom mosaic operator to combine the plume rasters into a single raster.  Used in Transport.CreatePlumeRaster
''' </summary>
''' <remarks></remarks>
Friend Class SumMosaicOperator
    Implements IMosaicOperator
    Implements IMosaicOperator2



    Public WriteOnly Property CurrentBand() As Integer Implements ESRI.ArcGIS.DataSourcesRaster.IMosaicOperator.CurrentBand
        Set(ByVal value As Integer)
            'empty
        End Set
    End Property

    Public Sub Init(ByVal nBands As Integer, ByVal nBlockCols As Integer, ByVal nBlockRows As Integer) Implements ESRI.ArcGIS.DataSourcesRaster.IMosaicOperator.Init
        'empty
    End Sub

    Public Sub Operate(ByVal x As Integer, ByVal y As Integer, ByVal v1 As Integer, ByRef v2 As Integer) Implements ESRI.ArcGIS.DataSourcesRaster.IMosaicOperator.Operate
        v2 = v1 + v2
    End Sub

    Public Sub Operate(ByVal x As Integer, ByVal y As Integer, ByVal v1 As Double, ByRef v2 As Double) Implements IMosaicOperator2.Operate
        v2 = v1 + v2
    End Sub

    Public Property Properties() As ESRI.ArcGIS.esriSystem.IPropertySet Implements ESRI.ArcGIS.DataSourcesRaster.IMosaicOperator.Properties
        Get
            Return Nothing
        End Get
        Set(ByVal value As ESRI.ArcGIS.esriSystem.IPropertySet)
            'empty
        End Set
    End Property


End Class

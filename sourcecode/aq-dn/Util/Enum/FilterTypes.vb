Imports ESRI.ArcGIS.Catalog

''' <summary>
''' Used for the GxDialog open/save dialog. This class can be used to return a given type of filter
''' that can be used for the open/save dialog.  This is just a wrapper to make the filter names easier
''' to remember.
''' </summary>
''' <remarks></remarks>
Public Class FilterTypes
    Public Shared ReadOnly Property Raster() As IGxObjectFilter
        Get
            Raster = New GxFilterRasterDatasets()
        End Get
    End Property
    Public Shared ReadOnly Property Feature() As IGxObjectFilter
        Get
            Feature = New GxFilterFeatureDatasetsAndFeatureClasses()
        End Get
    End Property

    ''' <summary>
    ''' checks whether the argument is a raster type filter
    ''' </summary>
    ''' <param name="filter"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function IsRaster(ByVal filter As IGxObjectFilter) As Boolean
        If TypeOf (filter) Is ESRI.ArcGIS.Catalog.GxFilterRasterDatasetsClass Then
            Return True
        Else
            Return False
        End If
    End Function
End Class

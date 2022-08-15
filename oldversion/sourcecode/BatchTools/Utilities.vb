Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry

Public Class Utilities

    Public Shared Sub debug_OutputRasterProps(ByVal r As IRaster2)
        Dim rbc As IRasterBandCollection
        Dim rpr As IRasterProps

        rbc = CType(r.RasterDataset, IRasterBandCollection)
        rpr = CType(r, IRasterProps)

        Debug.WriteLine("Spatial Ref: " & rpr.SpatialReference.Name & vbTab & "NoData: " & rpr.NoDataValue(0))
        Debug.WriteLine("Mean Cell Size X: " & rpr.MeanCellSize.X & vbTab & " Mean Cell Size Y: " & rpr.MeanCellSize.Y)
        Debug.WriteLine("Num Rows: " & rpr.Width & vbTab & vbTab & "Num Columns: " & rpr.Height)
        Debug.WriteLine("Max: " & rbc.Item(0).Statistics.Maximum & vbTab & " Min: " & rbc.Item(0).Statistics.Minimum)
        Debug.WriteLine("Avg: " & rbc.Item(0).Statistics.Mean & vbTab & " Std: " & rbc.Item(0).Statistics.StandardDeviation)
    End Sub

    Public Shared Sub debug_OutputFeatureClassProps(ByVal fc As IFeatureClass)
        Debug.WriteLine("Spatial reference: " & vbTab & CType(fc, IGeoDataset).SpatialReference.Name)
        Debug.WriteLine("Feature type: " & vbTab & [Enum].GetName(GetType(esriFeatureType), fc.FeatureType))
        Debug.WriteLine("Shape type: " & vbTab & [Enum].GetName(GetType(esriGeometryType), fc.ShapeType))
        Debug.WriteLine("Feature Count: " & vbTab & fc.FeatureCount(Nothing))
    End Sub


End Class

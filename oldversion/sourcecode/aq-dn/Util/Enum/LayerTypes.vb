﻿''' <summary>
''' Used for the LayersComboBox right now, but can be used anytime the UID of a layer type is needed
''' </summary>
''' <remarks>
''' The different layer GUID's and Interface's are:
''' "{AD88322D-533D-4E36-A5C9-1B109AF7A346}" = IACFeatureLayer
''' "{74E45211-DFE6-11D3-9FF7-00C04F6BC6A5}" = IACLayer
''' "{495C0E2C-D51D-4ED4-9FC1-FA04AB93568D}" = IACImageLayer
''' "{65BD02AC-1CAD-462A-A524-3F17E9D85432}" = IACAcetateLayer
''' "{4AEDC069-B599-424B-A374-49602ABAD308}" = IAnnotationLayer
''' "{DBCA59AC-6771-4408-8F48-C7D53389440C}" = IAnnotationSublayer
''' "{E299ADBC-A5C3-11D2-9B10-00C04FA33299}" = ICadLayer
''' "{7F1AB670-5CA9-44D1-B42D-12AA868FC757}" = ICadastralFabricLayer
''' "{BA119BC4-939A-11D2-A2F4-080009B6F22B}" = ICompositeLayer
''' "{9646BB82-9512-11D2-A2F6-080009B6F22B}" = ICompositeGraphicsLayer
''' "{0C22A4C7-DAFD-11D2-9F46-00C04F6BC78E}" = ICoverageAnnotationLayer
''' "{6CA416B1-E160-11D2-9F4E-00C04F6BC78E}" = IDataLayer
''' "{0737082E-958E-11D4-80ED-00C04F601565}" = IDimensionLayer
''' "{48E56B3F-EC3A-11D2-9F5C-00C04F6BC6A5}" = IFDOGraphicsLayer
''' "{40A9E885-5533-11D0-98BE-00805F7CED21}" = IFeatureLayer
''' "{605BC37A-15E9-40A0-90FB-DE4CC376838C}" = IGdbRasterCatalogLayer
''' "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}" = IGeoFeatureLayer
''' "{34B2EF81-F4AC-11D1-A245-080009B6F22B}" = IGraphicsLayer
''' "{EDAD6644-1810-11D1-86AE-0000F8751720}" = IGroupLayer
''' "{D090AA89-C2F1-11D3-9FEF-00C04F6BC6A5}" = IIMSSubLayer
''' "{DC8505FF-D521-11D3-9FF4-00C04F6BC6A5}" = IIMAMapLayer
''' "{34C20002-4D3C-11D0-92D8-00805F7C28B0}" = ILayer
''' "{E9B56157-7EB7-4DB3-9958-AFBF3B5E1470}" = IMapServerLayer
''' "{B059B902-5C7A-4287-982E-EF0BC77C6AAB}" = IMapServerSublayer
''' "{82870538-E09E-42C0-9228-CBCB244B91BA}" = INetworkLayer
''' "{D02371C7-35F7-11D2-B1F2-00C04F8EDEFF}" = IRasterLayer
''' "{AF9930F0-F61E-11D3-8D6C-00C04F5B87B2}" = IRasterCatalogLayer
''' "{FCEFF094-8E6A-4972-9BB4-429C71B07289}" = ITemporaryLayer
''' "{5A0F220D-614F-4C72-AFF2-7EA0BE2C8513}" = ITerrainLayer
''' "{FE308F36-BDCA-11D1-A523-0000F8774F0F}" = ITinLayer
''' "{FB6337E3-610A-4BC2-9142-760D954C22EB}" = ITopologyLayer
''' "{005F592A-327B-44A4-AEEB-409D2F866F47}" = IWMSLayer
''' "{D43D9A73-FF6C-4A19-B36A-D7ECBE61962A}" = IWMSGroupLayer
''' "{8C19B114-1168-41A3-9E14-FC30CA5A4E9D}" = IWMSMapLayer
''' </remarks>
Public Class LayerTypes

    Public Enum LayerType
        AllTypes = 0
        RasterLayer = 1
        FeatureLayer = 2
    End Enum

    ''' <summary>
    ''' Use this to get the UID of the given layer type
    ''' </summary>
    ''' <param name="LayerType"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetUIDFromEnum(ByVal LayerType As LayerType) As String
        Dim ret As String = ""
        Select Case LayerType
            Case LayerType.AllTypes
                ret = "{0}"
            Case LayerType.RasterLayer
                ret = "{D02371C7-35F7-11D2-B1F2-00C04F8EDEFF}"
            Case LayerType.FeatureLayer
                ret = "{40A9E885-5533-11D0-98BE-00805F7CED21}"
            Case Else
                Throw New Exception("Invalid layer type " & LayerType.ToString)
        End Select
        Return ret
    End Function


End Class

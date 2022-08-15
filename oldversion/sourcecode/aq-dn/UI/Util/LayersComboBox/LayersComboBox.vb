Imports ESRI.ArcGIS.Carto

''' <summary>
''' This class extends the standard combo box to automatically populate itself with
''' a list of layers
''' </summary>
''' <remarks>
''' This control is placed on a form like a regular combobox control.  It behaves like a standard
''' combobox control unless the populate method is called.  The populate method fills the combobox
''' with a list of layers of the specified type from the given map.  Addionally, the dropdown style
''' is fixed to a DropDownList style (the textbox can't be edited).   The comobobox can also contain
''' other objects (e.g. strings) in conjuction with layer objects.
''' </remarks> 
''' 



Public Class LayersComboBox
    Inherits Windows.Forms.ComboBox

    Private COM As New ESRI.ArcGIS.ADF.ComReleaser

    Public Sub New()
        MyBase.DropDownStyle = Windows.Forms.ComboBoxStyle.DropDownList
        MyBase.MaxDropDownItems = 15
        MyBase.DropDownWidth = 300
    End Sub

    ''' <summary>
    ''' Populate the dropdown list with MyLayer2 ojbects
    ''' </summary>
    ''' <param name="map">The map to get the layers from. Obtained from an MxDocument.Maps object</param>
    ''' <param name="layerFilter">The type of layer to filter by. If no filter is specified, all layers 
    ''' will be added.  The filter can be obtained from the LayerTypes class.
    ''' </param>
    ''' <param name="ShapeType">For shape files only (ignored for all others): selects the shape type, e.g. points only</param>
    ''' <param name="nameMatch">Shows only the layers that have a name matching the specified regular expression</param>
    ''' <remarks></remarks>
    Public Sub Populate(ByVal map As IMap, Optional ByVal layerFilter As LayerTypes.LayerType = LayerTypes.LayerType.AllTypes, Optional ByVal ShapeType As ESRI.ArcGIS.Geometry.esriGeometryType = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryAny, Optional ByVal nameMatch As System.Text.RegularExpressions.Regex = Nothing)
        Dim uid As ESRI.ArcGIS.esriSystem.IUID = New ESRI.ArcGIS.esriSystem.UIDClass

        If layerFilter <> LayerTypes.LayerType.AllTypes Then uid.Value = LayerTypes.GetUIDFromEnum(layerFilter)

        'Every map document contains at least one Map object but a map document can contain any number of Map objects. 
        'The Maps object contains a collection of all the maps of the document
        Try
            Dim layer As MyLayer2 = Nothing
            Dim layerObj As ILayer2 = Nothing
            Dim layerEnum As IEnumLayer = map.Layers((CType(uid, ESRI.ArcGIS.esriSystem.UID)), True)

            'save the selected item, if there was one
            Dim selItem As String = Nothing
            Dim theIdx = -1, newIdx As Integer = -1
            If Not MyBase.SelectedItem Is Nothing Then
                selItem = MyBase.SelectedItem.ToString
            End If

            MyBase.Items.Clear()

            layerEnum.Reset()
            layerObj = layerEnum.Next

            'dont manage layerobjects since for some reason, closing the program
            'and reopening it will cause exceptions
            'COM.ManageLifetime(layerObj)

            layer = New MyLayer2(layerObj)
            While Not layer.BaseLayer Is Nothing
                If layerFilter = LayerTypes.LayerType.FeatureLayer And Not ShapeType = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryAny Then
                    If CType(layerObj, IGeoFeatureLayer).Valid AndAlso Not CType(layerObj, FeatureLayer).FeatureClass Is Nothing Then
                        If CType(layerObj, FeatureLayer).FeatureClass.ShapeType = ShapeType Then
                            If nameMatch Is Nothing OrElse nameMatch.IsMatch(layer.ToString) Then
                                theIdx = MyBase.Items.Add(layer)
                                If Not selItem Is Nothing AndAlso layer.ToString = selItem And newIdx = -1 Then
                                    newIdx = theIdx
                                End If
                            End If
                        End If
                    Else
                        Trace.WriteLine("Layer '" & layerObj.Name & "' is no longer valid")
                    End If
                Else
                    If layerFilter = LayerTypes.LayerType.RasterLayer Then
                        If CType(layerObj, IRasterLayer).Valid Then
                            If nameMatch Is Nothing OrElse nameMatch.IsMatch(layer.ToString) Then
                                theIdx = MyBase.Items.Add(layer)
                                If Not selItem Is Nothing AndAlso layer.ToString = selItem And newIdx = -1 Then
                                    newIdx = theIdx
                                End If
                            End If
                        Else
                            Trace.WriteLine("Layer '" & layerObj.Name & "' is no longer valid")
                        End If
                    End If
                End If

                layerObj = layerEnum.Next
                layer = New MyLayer2(layerObj)
            End While
            If newIdx <> -1 Then MyBase.SelectedIndex = newIdx
        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
        End Try

    End Sub
End Class

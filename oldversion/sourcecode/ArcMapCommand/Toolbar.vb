Imports ESRI.ArcGIS.ADF.CATIDs
Imports ESRI.ArcGIS.ADF.BaseClasses
Imports System.Runtime.InteropServices

''' <summary>
''' An instance of an ArcGIS toolbar to which the buttons of this app. will be added to.
''' </summary>
''' <remarks></remarks>
<ComClass(Toolbar.ClassId, Toolbar.InterfaceId, Toolbar.EventsId), _
 ProgId("AqDn.Toolbar")> _
Public NotInheritable Class Toolbar
    Inherits BaseToolbar

#Region "ArcGIS Component Category Registrar generated code"
    ''' <summary>
    ''' Required method for ArcGIS Component Category registration -
    ''' Do not modify the contents of this method with the code editor.
    ''' </summary>
    Private Shared Sub ArcGISCategoryRegistration(ByVal registerType As Type)
        Dim regKey As String = String.Format("HKEY_CLASSES_ROOT\CLSID\{{{0}}}", registerType.GUID)
        MxCommandBars.Register(regKey)

    End Sub
    ''' <summary>
    ''' Required method for ArcGIS Component Category unregistration -
    ''' Do not modify the contents of this method with the code editor.
    ''' </summary>
    Private Shared Sub ArcGISCategoryUnregistration(ByVal registerType As Type)
        Dim regKey As String = String.Format("HKEY_CLASSES_ROOT\CLSID\{{{0}}}", registerType.GUID)
        MxCommandBars.Unregister(regKey)
    End Sub

#End Region

#Region "COM Registration Function(s)"
    <ComRegisterFunction(), ComVisibleAttribute(False)> _
    Public Shared Sub RegisterFunction(ByVal registerType As Type)
#If Not (CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10") Then
        ' Required for ArcGIS Component Category Registrar support
        ' Required for ArcGIS Component Category Registrar support
        ArcGISCategoryRegistration(registerType)

        'MsgBox("Registering " & System.Reflection.Assembly.GetExecutingAssembly.CodeBase & " " & Reflection.MethodInfo.GetCurrentMethod.DeclaringType.Name)
#End If


    End Sub

    <ComUnregisterFunction(), ComVisibleAttribute(False)> _
    Public Shared Sub UnregisterFunction(ByVal registerType As Type)
#If Not (CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10") Then
        ' Required for ArcGIS Component Category Registrar support
        ArcGISCategoryUnregistration(registerType)

        'MsgBox("UnRegistering " & System.Reflection.Assembly.GetExecutingAssembly.CodeBase & " " & Reflection.MethodInfo.GetCurrentMethod.DeclaringType.Name)
#End If

    End Sub


#End Region

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "8c5b4244-50c5-400a-880c-466df40ed871"
    Public Const InterfaceId As String = "75db3e6d-5de1-4b08-8fdd-2c309c27e433"
    Public Const EventsId As String = "ff433b76-a6c5-4fb4-8a7d-dab86e639d08"
#End Region

    ' A creatable COM class must have a Public Sub New() 
    ' with no parameters, otherwise, the class will not be 
    ' registered in the COM registry and cannot be created 
    ' via CreateObject.
    Public Sub New()
        'add these guid's to the toolbar
        AddItem("{8f815091-ed4a-4e1d-855d-6603a1c99658}")
        AddItem("{926b78c5-66c4-49a3-983a-33f9001b02d1}")
        AddItem("{bd017fbe-e4c8-47a4-9168-3f7ab02f84cc}")
    End Sub

    Public Overrides ReadOnly Property Caption() As String
        Get
            Return "ArcNLET"
        End Get
    End Property

    Public Overrides ReadOnly Property Name() As String
        Get
            Return "ArcNLET Toolbar"
        End Get
    End Property
End Class

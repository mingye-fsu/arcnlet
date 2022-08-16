Imports System.Runtime.InteropServices
Imports ESRI.ArcGIS.ADF.BaseClasses
Imports ESRI.ArcGIS.ADF.CATIDs
Imports ESRI.ArcGIS.Framework
Imports ESRI.ArcGIS.ArcMapUI
'Imports ESRI.ArcGIS.ADF.local
Imports ArcNLET_UA

<ComClass(ArcNLET_UA.ClassId, ArcNLET_UA.InterfaceId, ArcNLET_UA.EventsId), ProgId("AqDn.ParticleTrack")> _
Public NotInheritable Class ArcNLET_UA
    Inherits BaseCommand

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "bd017fbe-e4c8-47a4-9168-3f7ab02f84cc"
    Public Const InterfaceId As String = "aea41c31-74ab-45ae-982a-05c9ae7dcf44"
    Public Const EventsId As String = "2bb31f3e-03ae-474a-b92f-d1b52ca457ce"
#End Region
#Region "COM registration notification"
    <System.Runtime.InteropServices.ComRegisterFunction()> Private Shared Sub COMRegister(ByVal t As Type)
        'only register with arcgis if I'm not using the MyDebug build
#If Not (CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10") Then
        ' Required for ArcGIS Component Category Registrar support
        ArcGISCategoryRegistration(t)

        'MsgBox("Registering " & System.Reflection.Assembly.GetExecutingAssembly.CodeBase & " " & Reflection.MethodInfo.GetCurrentMethod.DeclaringType.Name)
#End If
    End Sub
    <System.Runtime.InteropServices.ComUnregisterFunction()> Private Shared Sub COMUnRegister(ByVal t As Type)
#If Not (CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10") Then
        ' Required for ArcGIS Component Category Registrar support
        ArcGISCategoryUnregistration(t)

        'MsgBox("UnRegistering " & System.Reflection.Assembly.GetExecutingAssembly.CodeBase & " " & Reflection.MethodInfo.GetCurrentMethod.DeclaringType.Name)
#End If
    End Sub
#End Region
#Region "ArcGIS Component Category Registrar generated code"
    ''' <summary>
    ''' Required method for ArcGIS Component Category registration -
    ''' Do not modify the contents of this method with the code editor.
    ''' </summary>
    Private Shared Sub ArcGISCategoryRegistration(ByVal registerType As Type)
        Dim regKey As String = String.Format("HKEY_CLASSES_ROOT\CLSID\{{{0}}}", registerType.GUID)
        MxCommands.Register(regKey)

    End Sub
    ''' <summary>
    ''' Required method for ArcGIS Component Category unregistration -
    ''' Do not modify the contents of this method with the code editor.
    ''' </summary>
    Private Shared Sub ArcGISCategoryUnregistration(ByVal registerType As Type)
        Dim regKey As String = String.Format("HKEY_CLASSES_ROOT\CLSID\{{{0}}}", registerType.GUID)
        MxCommands.Unregister(regKey)

    End Sub

#End Region

    Private m_application As IApplication

    Private Shared f As mainform
    Private tr As TraceOutput

    ''' <summary>
    ''' Initializes a new instance of ArcNLET_UA
    ''' </summary>
    ''' <remarks>
    '''  A creatable COM class must have a Public Sub New() 
    ''' with no parameters, otherwise, the class will not be 
    ''' registered in the COM registry and cannot be created 
    ''' via CreateObject.
    ''' </remarks>
    Public Sub New()
        MyBase.New()

        MyBase.m_category = "ArcNLET"  'localizable text 
        MyBase.m_caption = "ArcNLET Uncertainty Quantification"   'localizable text 
        MyBase.m_message = "ArcNLET Uncertainty Quantification"   'localizable text 
        MyBase.m_toolTip = "ArcNLET Uncertainty Quantification" 'localizable text 
        MyBase.m_name = "ArcNLET Uncertainty Quantification"  'unique id, non-localizable (e.g. "MyCategory_ArcMapTool")

        Try
            MyBase.m_bitmap = My.Resources.icon3_16
        Catch ex As Exception
            System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap")
        End Try
    End Sub


    Public Overrides Sub OnCreate(ByVal hook As Object)
        If Not hook Is Nothing Then
            m_application = CType(hook, IApplication)

            'Disable if it is not ArcMap
            If TypeOf hook Is IMxApplication Then
                MyBase.m_enabled = True

                'important!!!
                Main.init(hook)
            Else
                MyBase.m_enabled = False
            End If
        End If


    End Sub

    Public Overrides Sub OnClick()
        MyBase.OnClick()

        Dim newformneeded As Boolean = False

        'only create a new form if the current one has been disposed
        'or if one hadn't been created yet
        If Not f Is Nothing Then
            If f.IsDisposed Then newformneeded = True
        Else
            newformneeded = True
        End If

        If newformneeded Then
            Try
                f = New MainForm
                f.TopLevel = True
                f.Icon = Drawing.Icon.FromHandle(My.Resources.icon3_16.GetHicon)
                AqDn.Utilities.SetWindowLong(f.Handle, -8, m_application.hWnd)
                tr = New TraceOutput(f.LogTextbox)
                f.myTraceListener = tr
                f.CreateControl()
                f.Show()
                Trace.WriteLine("ArcNLET UA load")
            Catch ex As Exception
                f = Nothing
            End Try
        End If
    End Sub

End Class



Imports System.Runtime.InteropServices
Imports System.Drawing
Imports ESRI.ArcGIS.ADF.BaseClasses
Imports ESRI.ArcGIS.ADF.CATIDs
Imports ESRI.ArcGIS.Framework
Imports ESRI.ArcGIS.ArcMapUI
Imports ESRI.ArcGIS.Geometry
Imports System.Windows.Forms
Imports AqDn

''' <summary>
''' COM accessible class that inherits the BaseTool class.  This is the class that is called by ArcMap when
''' the button is clicked for the particle track tool in ArcMap.
''' </summary>
''' <remarks>
''' This class creates the form that is shown when the toolbar button is clicked.  Note the form
''' is loaded in process (no remoting is performed)
''' </remarks>
<ComClass(ParticleTrackTool.ClassId, ParticleTrackTool.InterfaceId, ParticleTrackTool.EventsId), _
 ProgId("AqDn.ParticleTrack")> _
Public NotInheritable Class ParticleTrackTool
    Inherits BaseTool

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    ''' <summary>
    ''' COM Identity
    ''' </summary>
    ''' <remarks></remarks>
    Public Const ClassId As String = "926b78c5-66c4-49a3-983a-33f9001b02d1"
    ''' <summary>
    ''' COM Identity
    ''' </summary>
    ''' <remarks></remarks>
    Public Const InterfaceId As String = "a0fa30db-96f7-4056-81e4-9d9c1c029e10"
    ''' <summary>
    ''' COM Identity
    ''' </summary>
    ''' <remarks></remarks>
    Public Const EventsId As String = "bf665972-0271-44c0-bf1d-d4a5a62dc2ce"
#End Region

#Region "COM Registration Function(s)"
    ''' <summary>
    ''' Called when this class is registered for COM
    ''' </summary>
    ''' <param name="registerType">Type for ArcGIS Component Category registration</param>
    ''' <remarks></remarks>
    <ComRegisterFunction(), ComVisibleAttribute(False)> _
    Public Shared Sub RegisterFunction(ByVal registerType As Type)
#If Not (CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10") Then
        ' Required for ArcGIS Component Category Registrar support
        ' Required for ArcGIS Component Category Registrar support
        ArcGISCategoryRegistration(registerType)

        'MsgBox("Registering " & System.Reflection.Assembly.GetExecutingAssembly.CodeBase & " " & Reflection.MethodInfo.GetCurrentMethod.DeclaringType.Name)
#End If

    End Sub

    ''' <summary>
    ''' Called when this class is unregistered for COM
    ''' </summary>
    ''' <param name="registerType">Type for ArcGIS Component Category registration</param>
    ''' <remarks></remarks>
    <ComUnregisterFunction(), ComVisibleAttribute(False)> _
    Public Shared Sub UnregisterFunction(ByVal registerType As Type)
#If Not (CONFIG = "mydebug-Arc9" Or CONFIG = "mydebug-Arc10") Then
        ' Required for ArcGIS Component Category Registrar support
        ArcGISCategoryUnregistration(registerType)

        'MsgBox("UnRegistering " & System.Reflection.Assembly.GetExecutingAssembly.CodeBase & " " & Reflection.MethodInfo.GetCurrentMethod.DeclaringType.Name)
#End If
    End Sub

#Region "ArcGIS Component Category Registrar generated code"
    ''' <summary>
    ''' Registers this class in the appropriate component category
    ''' </summary>
    ''' <param name="registerType">Type to register</param>
    ''' <remarks></remarks>
    Private Shared Sub ArcGISCategoryRegistration(ByVal registerType As Type)
        Dim regKey As String = String.Format("HKEY_CLASSES_ROOT\CLSID\{{{0}}}", registerType.GUID)
        MxCommands.Register(regKey)

    End Sub
    ''' <summary>
    ''' Unregisters this class from the appropriate component category
    ''' </summary>
    ''' <param name="registerType">Type to unregister</param>
    ''' <remarks></remarks>
    Private Shared Sub ArcGISCategoryUnregistration(ByVal registerType As Type)
        Dim regKey As String = String.Format("HKEY_CLASSES_ROOT\CLSID\{{{0}}}", registerType.GUID)
        MxCommands.Unregister(regKey)

    End Sub

#End Region
#End Region


    Private m_application As IApplication

    Private Shared f As ParticleTrackForm
    Private tr As TraceOutput

    Private m_mousepos As IPoint = New ESRI.ArcGIS.Geometry.Point With {.X = 0, .Y = 0}

    ''' <summary>
    ''' Initializes a new instance of ParticleTrackTool
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
        MyBase.m_caption = "ArcNLET Particle Track"   'localizable text 
        MyBase.m_message = "ArcNLET Particle Track"   'localizable text 
        MyBase.m_toolTip = "ArcNLET interactive particle track tool" 'localizable text 
        MyBase.m_name = "ArcNLET Particle Track"  'unique id, non-localizable (e.g. "MyCategory_ArcMapTool")

        Try
            MyBase.m_bitmap = My.Resources.icon2_16            
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
                f = New ParticleTrackForm
                f.TopLevel = True
                f.Icon = Drawing.Icon.FromHandle(My.Resources.icon2_16.GetHicon)
                Utilities.SetWindowLong(f.Handle, -8, m_application.hWnd)
                tr = New TraceOutput(f.MessageLog)
                f.myTraceListener = tr
                f.CreateControl()
                f.Show()
                Trace.WriteLine("Particle track load")
            Catch ex As Exception
                f = Nothing
            End Try
        End If
    End Sub

    Public Overrides Function Deactivate() As Boolean
        Return MyBase.m_deactivate
    End Function

    Public Overrides Sub OnMouseDown(ByVal Button As Integer, ByVal Shift As Integer, ByVal X As Integer, ByVal Y As Integer)
        Try
            Dim pt As New ESRI.ArcGIS.Geometry.Point()
            pt.X = Windows.Forms.Cursor.Position.X
            pt.Y = Windows.Forms.Cursor.Position.Y

            f.XCoord = m_mousepos.X
            f.YCoord = m_mousepos.Y
        Catch ex As Exception
            Trace.WriteLine("ERROR: " & ex.Message)
        End Try
    End Sub

    Public Overrides Sub OnMouseMove(ByVal Button As Integer, ByVal Shift As Integer, ByVal X As Integer, ByVal Y As Integer)
        m_mousepos = CType(CType(CType(Main.App, ESRI.ArcGIS.ArcMapUI.IMxApplication).Display, ESRI.ArcGIS.Display.IAppDisplay).DisplayTransformation, ESRI.ArcGIS.Display.IDisplayTransformation).ToMapPoint(X, Y)
    End Sub


End Class


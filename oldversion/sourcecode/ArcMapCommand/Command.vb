Imports System.Runtime.InteropServices
Imports System.Drawing
Imports ESRI.ArcGIS.ADF.BaseClasses
Imports ESRI.ArcGIS.ADF.CATIDs
Imports ESRI.ArcGIS.Framework
Imports ESRI.ArcGIS.ArcMapUI
Imports AqDn
'Imports ersi.arcgis.adf.local

''' <summary>
''' This is the main COM visible class that gets instantiated when the main program button in the toolbar in ArcGIS
''' </summary>
''' <remarks>
''' <code>
''' Creating a COM class that is callable by ArcGIS involves the following
''' 1. create a COM callable project. Use visual studio's built in template for this.  The template will 
''' automatically set the Make Assembly COM visible option in the application properties and it will generate
''' a GUID
''' 2. Add a new COM callable class. Use the VS template for this.  The template will automatically generate
''' code to register the class to COM including some GUIDs. It will also set the COM Class anc COM Visible properties
''' of the class to true.
''' 3. Upon class registration, need to set the reg entries that tell ArcGIS that this tool exists.  There are
''' some functions that can be implemented that are called when the function is registered by regasm (or the VS IDE)
''' Can implement these functions manually by specifying the System.Runtime.InteropServices.ComRegisterFunction() attribute
''' on the desrired sub and then calling Register on the desried category.  The Add Component Category tool of the ArcGIS
''' .Net SDK VS Integration kit (accesible via the Project menu in VS) will do this for you.
''' 4. The class must implement the ICommand interface if its a command, or the ITool interface if its a tool (i.e. interacts with maps)
''' You can do this manually or with by extending the new (for ArcGIS 9.3) ADF classes BaseCommand or BaseTool.
''' IMPORTANT: some properties of ITool or ICommand must be set (e.g category,name) or else it won't show up in ArcGIS
''' 
''' In order to be able to debug the application, the debug application must be set to ArcMap.exe
''' </code>
''' </remarks>
''' 
''' 
<ComClass(Command.ClassId, Command.InterfaceId, Command.EventsId), _
 ProgId("ArcMapCommand.Command1")> _
Public NotInheritable Class Command
    Inherits BaseCommand

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "8f815091-ed4a-4e1d-855d-6603a1c99658"
    Public Const InterfaceId As String = "a6d11e4e-4984-4328-bcea-fd9fd8168baf"
    Public Const EventsId As String = "a908c60a-7cf8-473c-b635-bee4470c104c"
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


    Private Shared f As MainForm
    Private Shared tr As TraceOutput

    ''' <summary>
    ''' Mandatory parameterless constructor for COM
    ''' A creatable COM class must have a Public Sub New() 
    ''' with no parameters, otherwise, the class will not be 
    ''' registered in the COM registry and cannot be created 
    ''' via CreateObject.
    ''' </summary>
    ''' <remarks>
    ''' Initializes some member variables of BaseTool.
    ''' </remarks>
    Public Sub New()
        MyBase.New()

        MyBase.m_caption = "ArcNLET"
        MyBase.m_name = "ArcNLET"
        MyBase.m_category = "ArcNLET"
        MyBase.m_message = "ArcNLET"
        MyBase.m_toolTip = "Main ArcNLET Program"
        MyBase.m_bitmap = My.Resources.icon_16


    End Sub

    ''' <summary>
    ''' Called when the COM component is created.
    ''' </summary>
    ''' <param name="hook">The reference to the IApplication. In this case the reference should be an ArcMap IMxApplication2 reference</param>
    ''' <remarks>
    ''' <para>
    ''' From the ArcObjects documentation:
    ''' "When you implement ICommand to create a custom command, you will find that your 
    ''' class constructor and destructor are called more than once per session.  
    ''' Commands are constructed once initially to get information about them, like the name, bitmap, 
    ''' etc and then they are destroyed.  When the final, complete construction takes place, the 
    ''' OnCreate method gets called.  OnCreate gets called only once, so you can rely on it to 
    ''' perform initialization of member variables.  You can check for initialized member variables 
    ''' in the class destructor to find out if OnCreate has been called previously."
    ''' </para>
    ''' </remarks>
    Public Overrides Sub OnCreate(ByVal hook As Object)
        If Not hook Is Nothing Then
            Dim m_application As IApplication = CType(hook, IApplication)

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

    ''' <summary>
    ''' Called whenever the command is clicked.
    ''' </summary>
    ''' <remarks></remarks>
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
                Utilities.SetWindowLong(f.Handle, -8, Main.App.hWnd)
                tr = New TraceOutput(f.LogTextbox)
                f.myTraceListener = tr
                f.CreateControl()
                Trace.WriteLine("Application load")
                f.Show()
            Catch ex As Exception
                f = Nothing
                MsgBox(ex.ToString, MsgBoxStyle.Critical)
            End Try
        Else
            Try                
                f.WindowState = Windows.Forms.FormWindowState.Normal
                f.BringToFront()
            Catch ex As Exception

            End Try
        End If


    End Sub

End Class




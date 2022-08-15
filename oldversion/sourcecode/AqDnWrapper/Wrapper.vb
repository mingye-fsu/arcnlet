Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.version

Public Class Wrapper

    Private Shared m_AOLicenseInitializer As LicenseInitializer
    <STAThread()> Public Shared Sub main()
        Try
#If CONFIG = "Arc10" Or CONFIG = "mydebug-Arc10" Or CONFIG = "Release" Or CONFIG = "Arc10.2" Then
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop)
#End If
            'note by Yan: update to arc10.1
#If CONFIG = "Arc10.1" Then
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop)
#End If
            m_AOLicenseInitializer = New LicenseInitializer()

            'ESRI License Initializer generated code.
            If (Not m_AOLicenseInitializer.InitializeApplication(New esriLicenseProductCode() {esriLicenseProductCode.esriLicenseProductCodeBasic, esriLicenseProductCode.esriLicenseProductCodeStandard, esriLicenseProductCode.esriLicenseProductCodeAdvanced}, _
            New esriLicenseExtensionCode() {esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst})) Then
                Dim msg As String = ""
                msg = msg & m_AOLicenseInitializer.LicenseMessage() & vbCrLf
                msg = msg & "This application could not initialize with the correct ArcGIS license and will shutdown."
                m_AOLicenseInitializer.ShutdownApplication()
                MsgBox(msg, MsgBoxStyle.Critical)
                System.Environment.ExitCode = 8
                Return
            End If
        Catch ex As Exception
            MsgBox("Error initializing license manager" & vbCrLf & ex.ToString, MsgBoxStyle.Critical)
            System.Environment.ExitCode = 8
            Return
        End Try



        Dim operation As String = ""
        Dim serverURI As String = ""
        If System.Environment.GetCommandLineArgs.Length = 3 Then
            operation = System.Environment.GetCommandLineArgs(1)
            serverURI = System.Environment.GetCommandLineArgs(2)
        Else
            MsgBox("This executable cannot be run stand alone")
            m_AOLicenseInitializer.ShutdownApplication()
            Return
        End If


        'read the command line args to see what kind of calculation
        'were supposed to do
        Try
            Select Case operation
                Case "transport"
                    TransportWrapper.run(serverURI)
                Case "flow"
                    FlowWrapper.run(serverURI)
                Case Else
                    MsgBox("bad command")
            End Select
        Catch ex As Exception
            'catches any type load, or assembly load errors
            'MsgBox(ex.ToString, MsgBoxStyle.Critical)
            Trace.WriteLine(ex.ToString)
        End Try

        m_AOLicenseInitializer.ShutdownApplication()
    End Sub
End Class

Imports System.ComponentModel
Imports System.Configuration.Install
Imports System.Runtime.InteropServices

''' <summary>
''' Called by the installer MSI to register the DLLs with ArcGIS and COM
''' </summary>
''' <remarks>
''' This application should always be compiled as 32bit. 
''' then the installation folder should be the Program Files\Common Files or
''' Program Files (x86)\Common Files folder.
''' </remarks>
Public Class ToolbarInstaller

    Public Sub New()
        MyBase.New()

        'This call is required by the Component Designer.
        InitializeComponent()

        'Add initialization code after the call to InitializeComponent

    End Sub


#If CONFIG = "Arc9" Or CONFIG = "mydebug-Arc9" Then
    Public Overrides Sub Install(ByVal stateSaver As System.Collections.IDictionary)
        MyBase.Install(stateSaver)
        Dim regsrv As New RegistrationServices
        regsrv.RegisterAssembly(MyBase.GetType().Assembly, AssemblyRegistrationFlags.SetCodeBase)
    End Sub

    Public Overrides Sub Uninstall(ByVal savedState As System.Collections.IDictionary)
        MyBase.Uninstall(savedState)
        Dim regsrv As New RegistrationServices
        regsrv.UnregisterAssembly(MyBase.GetType().Assembly)
    End Sub
#End If

#If CONFIG = "Arc10" Or CONFIG = "mydebug-Arc10" Or CONFIG = "Release" Or CONFIG = "Arc10.2" Then
    Public Overrides Sub Install(ByVal stateSaver As System.Collections.IDictionary)
        MyBase.Install(stateSaver)

        'Register the custom component.
        '-----------------------------

        'The default location of the ESRIRegAsm utility.
        'Note how the whole string is embedded in quotes because of the spaces in the path.
        Dim cmd1 As String = """" + Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + "\ArcGIS\bin\ESRIRegAsm.exe" + """"

        Dim part1 As String = Reflection.Assembly.GetExecutingAssembly.Location

        'Add the appropriate command line switches when invoking the ESRIRegAsm utility.
        'In this case: /p:Desktop = means the ArcGIS Desktop product, /s = means a silent install.
        Dim part2 As String = " /p:Desktop /s"

        'It is important to embed the part1 in quotes in case there are any spaces in the path.
        Dim cmd2 As String = """" + part1 + """" + part2

        'Call the routing that will execute the ESRIRegAsm utility.
        Dim exitCode As Integer = ExecuteCommand(cmd1, cmd2, 10000)
        If exitCode <> 0 Then MsgBox("There was an error registering the assembly " & part1, MsgBoxStyle.Critical)
    End Sub

    Public Overrides Sub Uninstall(ByVal savedState As System.Collections.IDictionary)
        MyBase.Uninstall(savedState)

        'Unregister the custom component.
        '---------------------------------

        'The default location of the ESRIRegAsm utility.
        'Note how the whole string is embedded in quotes because of the spaces in the path.
        Dim cmd1 As String = """" + Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + "\ArcGIS\bin\ESRIRegAsm.exe" + """"

        Dim part1 As String = Reflection.Assembly.GetExecutingAssembly.Location

        'Add the appropriate command line switches when invoking the ESRIRegAsm utility.
        'In this case: /p:Desktop = means the ArcGIS Desktop product, /u = means unregister the Custom Component, /s = means a silent install.
        Dim part2 As String = " /p:Desktop /u /s"

        'It is important to embed the part1 in quotes in case there are any spaces in the path.
        Dim cmd2 As String = """" + part1 + """" + part2

        'Call the routing that will execute the ESRIRegAsm utility.
        Dim exitCode As Integer = ExecuteCommand(cmd1, cmd2, 10000)
        If exitCode <> 0 Then MsgBox("There was an error registering the assembly " & part1, MsgBoxStyle.Critical)
    End Sub

    Public Shared Function ExecuteCommand(ByVal Command1 As String, ByVal Command2 As String, ByVal Timeout As Integer) As Integer

        'Set up a ProcessStartInfo using your path to the executable (Command1) and the command line arguments (Command2).
        Dim ProcessInfo As ProcessStartInfo = New ProcessStartInfo(Command1, Command2)
        ProcessInfo.CreateNoWindow = True
        ProcessInfo.UseShellExecute = False

        'Invoke the process.
        Dim Process As Process
        Try
            Process = Process.Start(ProcessInfo)
            Process.WaitForExit(Timeout)
        Catch ex As Exception
            MsgBox("Error calling ESRIRegAsm" & ex.ToString)
            Return -1
        End Try

        'Finish.
        Dim ExitCode As Integer = Process.ExitCode
        Process.Close()
        Return ExitCode
    End Function
#End If
    'note by yan: update to Arc10.1
#If CONFIG = "Arc10.1" Then
    Public Overrides Sub Install(ByVal stateSaver As System.Collections.IDictionary)
        MyBase.Install(stateSaver)

        'Register the custom component.
        '-----------------------------

        'The default location of the ESRIRegAsm utility.
        'Note how the whole string is embedded in quotes because of the spaces in the path.
        Dim cmd1 As String = """" + Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + "\ArcGIS\bin\ESRIRegAsm.exe" + """"

        Dim part1 As String = Reflection.Assembly.GetExecutingAssembly.Location

        'Add the appropriate command line switches when invoking the ESRIRegAsm utility.
        'In this case: /p:Desktop = means the ArcGIS Desktop product, /s = means a silent install.
        Dim part2 As String = " /p:Desktop /s"

        'It is important to embed the part1 in quotes in case there are any spaces in the path.
        Dim cmd2 As String = """" + part1 + """" + part2

        'Call the routing that will execute the ESRIRegAsm utility.
        Dim exitCode As Integer = ExecuteCommand(cmd1, cmd2, 10000)
        If exitCode <> 0 Then MsgBox("There was an error registering the assembly " & part1, MsgBoxStyle.Critical)
    End Sub

    Public Overrides Sub Uninstall(ByVal savedState As System.Collections.IDictionary)
        MyBase.Uninstall(savedState)

        'Unregister the custom component.
        '---------------------------------

        'The default location of the ESRIRegAsm utility.
        'Note how the whole string is embedded in quotes because of the spaces in the path.
        Dim cmd1 As String = """" + Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + "\ArcGIS\bin\ESRIRegAsm.exe" + """"

        Dim part1 As String = Reflection.Assembly.GetExecutingAssembly.Location

        'Add the appropriate command line switches when invoking the ESRIRegAsm utility.
        'In this case: /p:Desktop = means the ArcGIS Desktop product, /u = means unregister the Custom Component, /s = means a silent install.
        Dim part2 As String = " /p:Desktop /u /s"

        'It is important to embed the part1 in quotes in case there are any spaces in the path.
        Dim cmd2 As String = """" + part1 + """" + part2

        'Call the routing that will execute the ESRIRegAsm utility.
        Dim exitCode As Integer = ExecuteCommand(cmd1, cmd2, 10000)
        If exitCode <> 0 Then MsgBox("There was an error registering the assembly " & part1, MsgBoxStyle.Critical)
    End Sub

    Public Shared Function ExecuteCommand(ByVal Command1 As String, ByVal Command2 As String, ByVal Timeout As Integer) As Integer

        'Set up a ProcessStartInfo using your path to the executable (Command1) and the command line arguments (Command2).
        Dim ProcessInfo As ProcessStartInfo = New ProcessStartInfo(Command1, Command2)
        ProcessInfo.CreateNoWindow = True
        ProcessInfo.UseShellExecute = False

        'Invoke the process.
        Dim Process As Process
        Try
            Process = Process.Start(ProcessInfo)
            Process.WaitForExit(Timeout)
        Catch ex As Exception
            MsgBox("Error calling ESRIRegAsm" & ex.ToString)
            Return -1
        End Try

        'Finish.
        Dim ExitCode As Integer = Process.ExitCode
        Process.Close()
        Return ExitCode
    End Function
#End If


End Class

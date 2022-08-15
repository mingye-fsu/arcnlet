Imports System.Reflection
Imports AqDnWrapper
Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Channels
Imports System.Runtime.Remoting.Channels.Ipc

''' <summary>
''' This class is loaded by the Remoting server into a separate AppDomain.  Its function is only to execute the
''' wrapper AqDnWrapper and notify the server if the process has exited
''' </summary>
''' <remarks></remarks>
Public Class RemotingBootstrapper
    Inherits MarshalByRefObject
    Implements IAqDnApplication.IWrapperBootstrapper

    Private WithEvents m_childproc As Process
    Private m_childproc_id As Integer
    Private m_childproc_name As String

    Private m_channelURI As String

    ''' <summary>
    ''' Kills the child process
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub kill() Implements IAqDnApplication.IWrapperBootstrapper.kill
        Try
            If m_childproc Is Nothing Then
                Trace.WriteLine("cannot kill the process because it is not initialized")
            Else
                If Not m_childproc.HasExited Then m_childproc.Kill()
            End If
        Catch ex As Exception
            Trace.WriteLine(ex)
            MsgBox(ex.ToString)
        End Try
    End Sub

    ''' <summary>
    ''' Starts execution of the wrapper
    ''' </summary>
    ''' <param name="channelURI">The URI of the remoting communications channel that the wrapper
    ''' will use to communicate with the mainUI</param>
    ''' <remarks></remarks>
    Public Sub run(ByVal channelURI As String, ByVal moduleName As String) Implements IAqDnApplication.IWrapperBootstrapper.run
        Try
            Dim si As New ProcessStartInfo
            Try
                m_childproc_id = -1
                m_childproc_name = "AqDnWrapper.exe"

                'got some weird errors in ArcNLET UA when starting the transport module for
                'the second iteration if UseShellExecute was set to false. Also, teh <STAThread()> property on Wrapper main() must be set
                m_channelURI = channelURI
                si.FileName = IO.Path.Combine(IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly.Location), m_childproc_name)
                si.Arguments = moduleName & " " & channelURI
                si.UseShellExecute = True
                m_childproc = New Process
                m_childproc.StartInfo = si
                m_childproc.EnableRaisingEvents = True
                si.CreateNoWindow = True
                m_childproc.Start()
                If Not m_childproc.HasExited Then
                    m_childproc_id = m_childproc.Id
                End If
            Catch ex As Exception
                Throw New Exception("Could not start client process '" & si.FileName & "'" & vbCrLf & ex.ToString)
            End Try
        Catch ex As Exception
            Trace.WriteLine(ex)
            MsgBox(ex.ToString)
        End Try
    End Sub

    ''' <summary>
    ''' Kill the child process if this form exits
    ''' </summary>
    ''' <remarks></remarks>
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        Trace.WriteLine("bootstrapper finalizing")
        Try
            If Not m_childproc Is Nothing Then
                m_childproc.Kill()
                m_childproc = Nothing
            End If
        Catch ex As Exception
            'ignore any errors in attempting to close
            'since an exception will be thrown if the process has already been closed
        End Try
    End Sub


    ''' <summary>
    ''' Used to unregister the wrapper-bootstrapper channel when the client exits
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub m_childproc_Exited(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_childproc.Exited
        Try
            Trace.WriteLine("Child process ended")
            'MsgBox("child process ended")

            Dim xc As Integer = m_childproc.ExitCode
            m_childproc.Close()
            m_childproc = Nothing

            'make sure the process has really exited. I sometimes came across
            'cases where this event was raised and the process had not yet terminated
            '(e.g. when the server created a new AppDomain with an instance of this class
            'without unloading the old one.)
            If m_childproc_id > 0 Then
                Dim p As Process = Nothing
                Dim counter As Integer = 0
                Do
                    Try
                        p = Process.GetProcessById(m_childproc_id)

                        'if we're here, no exception was thrown which means there
                        'exists a process with this id. Now chekc to see if the
                        'name matches

                        If p.MainModule.ModuleName = m_childproc_name Then
                            p = Nothing
                        End If

                        Threading.Thread.Sleep(50)
                    Catch ex As Exception
                        p = Nothing
                    End Try
                    counter = counter + 1
                Loop Until p Is Nothing Or counter > 100
            End If

            'notify the mainUI that the transport process has terminated
            '************************************
            Dim aqdnMain As IAqDnApplication.IModuleRunRemote
            Try
                'get a referece to the server's instance of ModuleRunRemote
                aqdnMain = Activator.GetObject(GetType(IAqDnApplication.IModuleRunRemote), _
                                               m_channelURI & "/srv")

            Catch ex As Exception
                Throw New Exception("Error initializing Bootstrapper-Main client channel" & vbCrLf & ex.ToString)
            End Try

            'notify            
            Try
                aqdnMain.WrapperProcessTerminated(xc)
            Catch ex1 As Threading.ThreadAbortException
                'do nothing.
                'if the AppDomain is unloaded in WrapperProcessTerminated, a ThreadAbortException will
                'be raised in this process. I ignore this exception
                'http://stackoverflow.com/questions/6451705/how-dos-appdomain-unload-abort-the-threads
            End Try

        Catch ex As Threading.ThreadAbortException
            'do nothing.
            'if the AppDomain is unloaded in WrapperProcessTerminated, a ThreadAbortException will
            'be raised in this process. I ignore this exception
            'http://stackoverflow.com/questions/6451705/how-dos-appdomain-unload-abort-the-threads
            'The exception must be caught here again for some reason
        Catch ex As Exception
            Trace.WriteLine(ex)
            MsgBox(ex.ToString)
        End Try
    End Sub

    Public Overrides Function InitializeLifetimeService() As Object
        'prevent this remoted object from being destroyed after a period of inactivity
        'see http://blogs.microsoft.co.il/blogs/sasha/archive/2008/07/19/appdomains-and-remoting-life-time-service.aspx
        Return Nothing
    End Function
End Class

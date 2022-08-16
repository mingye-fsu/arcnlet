''' <summary>
''' Adds color to console output from trace and debug.  Trace writes are shown in the normal colour while
''' debug writes are shown in green.  All that is required is to create an instance of this class.
''' </summary>
''' <remarks>
''' Creating an instance of this class will remove the default console trace listener and add a new
''' trace listener to Trace (and by extension Debug since Trace and Debug share the same listeners (and indentaion))
''' Calls to Trace.writeline will appear normal while calls to Debug.writeline will appear green.
''' <para>When the code is compiled for release, all debug statements are deleted by the compiler.
''' Even code contained within Debug.WriteLine (e.g. Debug.Writeline(Math.Sqrt(5)) will not execute</para>
''' </remarks>

Public Class TraceOutputConsole
    ''' <summary>
    ''' Outputs Trace and Debug statements to console except Debug statements
    ''' are coloured green
    ''' </summary>
    ''' <remarks></remarks>
    Private Class MyTextWriterTraceListener
        Inherits TextWriterTraceListener

        Dim s As StackTrace
        Dim oldindent As Integer

        Public Overrides Sub WriteLine(ByVal message As String)
            s = New StackTrace
            For Each frame As StackFrame In s.GetFrames
                If frame.GetMethod.DeclaringType.Name = "Debug" Then
                    System.Console.ForegroundColor = ConsoleColor.Green
                    Exit For
                End If
            Next

            oldindent = MyBase.IndentLevel
            MyBase.IndentLevel = 0
            MyBase.Write(Now.ToString("HH:mm:ss") & New String("  ", MyBase.IndentSize * oldindent + 1))
            MyBase.WriteLine(message)
            MyBase.IndentLevel = oldindent
            System.Console.ResetColor()
        End Sub

        Public Sub New(ByVal t As IO.TextWriter)
            MyBase.New(t)
            Try
                System.Console.BufferWidth = 130
                System.Console.BufferHeight = 5000
                System.Console.WindowWidth = 130
                System.Console.WindowHeight = 56
            Catch ex As Exception
            End Try
        End Sub
    End Class

    Private listener As MyTextWriterTraceListener

    Public Sub New()
        'debug and trace share the same listeners and indent
        listener = New MyTextWriterTraceListener(Console.Out)
        Trace.Listeners.RemoveAt(0)
        Trace.Listeners.Add(listener)
    End Sub

    Protected Overrides Sub Finalize()
        Trace.Listeners.Remove(listener)
        MyBase.Finalize()
    End Sub
End Class


Imports AqDn
''' <summary>
''' this class forwards trace messages to the mainUI
''' </summary>
''' <remarks></remarks>
Public Class TraceOutputForward

    Private myTraceListener As TheTraceListener


    ''' <summary>
    ''' Constructor.
    ''' </summary>
    ''' <param name="main"> The TransportRunRemote class to output to </param>      
    ''' <remarks>Delegates are used for thread safety according to Microsoft's documentation.</remarks>
    Public Sub New(ByVal main As ModuleRunRemote)
        Trace.UseGlobalLock = False
        Trace.AutoFlush = True

        'use a unique name for the trace listener. avoids problems
        'with multiple intances of the app, e.g. one instance removing the 
        'trace listener of another.
        Dim trname As String = "AqDnWrapperTraceListener" & Now.Ticks

        Me.myTraceListener = Trace.Listeners(trname)
        If Me.myTraceListener Is Nothing Then
            Me.myTraceListener = New TheTraceListener()
            Me.myTraceListener.setTextBox(main)
            Me.myTraceListener.Name = trname
            Trace.Listeners.Add(Me.myTraceListener)
        Else
            Me.myTraceListener.setTextBox(main)
        End If
    End Sub


    Protected Overrides Sub Finalize()
        Trace.Listeners.Remove(Me.myTraceListener)
        Me.myTraceListener.Dispose()
        MyBase.Finalize()
    End Sub

#Region "TheTraceListener"

    Private Class TheTraceListener
        Inherits System.Diagnostics.TraceListener

        Private m_mainUI As ModuleRunRemote

        Public Overrides ReadOnly Property IsThreadSafe() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overloads Overrides Sub Write(ByVal message As String)
            m_mainUI.TraceOutput(message, Trace.IndentLevel)
        End Sub

        Public Overloads Overrides Sub WriteLine(ByVal message As String)
            m_mainUI.TraceOutput(message, Trace.IndentLevel)
        End Sub

        Public Sub setTextBox(ByVal f As ModuleRunRemote)
            Me.m_mainUI = f
        End Sub

    End Class
#End Region

End Class


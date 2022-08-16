
'
'
'oct 25/2007 v1.0
'jul 2009 v1.1 - now takes as an argument a target textbox
Imports System.Windows.Forms

''' <summary>
''' this class displays calls to trace.write and trace.writeline in a given textbox
''' this class is thread-safe.
''' </summary>
''' <remarks></remarks>
Public Class TraceOutput

    Private myTraceListener As TheTraceListener

    ''' <summary>
    ''' Constructor.
    ''' </summary>
    ''' <param name="destTxtBox">The texbox to which the output will be written.
    ''' Delegates are used for thread safety according to Microsoft's documentation.
    ''' </param>
    ''' <remarks></remarks>
    Public Sub New(ByVal destTxtBox As TextBox)
        Trace.UseGlobalLock = False
        Trace.AutoFlush = True

        'use a unique name for the trace listener. avoids problems
        'with multiple intances of the app, e.g. one instance removing the 
        'trace listener of another.
        Dim trname As String = "AqDnTraceListener" & Now.Ticks

        Me.myTraceListener = Trace.Listeners(trname)
        If Me.myTraceListener Is Nothing Then
            Me.myTraceListener = New TheTraceListener()
            Me.myTraceListener.setTextBox(destTxtBox)
            Me.myTraceListener.Name = trname
            Trace.Listeners.Add(Me.myTraceListener)
        Else
            Me.myTraceListener.setTextBox(destTxtBox)
        End If
    End Sub

    ''' <summary>
    ''' Used to properly unload the custom trace listener
    ''' </summary>
    ''' <remarks>This function should be called when the program exits.  It is necessary because
    ''' arcgis won't immediately dispose of the application object, thereby leaving the trace listener
    ''' active.  When a new call to trace.write is made, the old trace listener will try to write to
    ''' a textbox that has been diposed already.  By unloading the trace listener when the main form closes,
    ''' we will avoid this problem.
    ''' </remarks>
    Public Sub close()
        Me.Finalize()
    End Sub

    Protected Overrides Sub Finalize()
        'MsgBox("Remove " & Me.myTraceListener.Name)
        Trace.Listeners.Remove(Me.myTraceListener)
        Me.myTraceListener.Dispose()
        MyBase.Finalize()
    End Sub

#Region "TheTraceListener"

    Private Class TheTraceListener
        Inherits System.Diagnostics.TraceListener

        Private theTextBox As TextBox

        Public Overrides ReadOnly Property IsThreadSafe() As Boolean
            Get
                Return True
            End Get
        End Property

        'keep track of the number of lines
        Private numLines As Integer

        'keep track of the indent level
        'Private Shadows indentLevel As Integer

        'this is to enable thread safe accesses output text box.  This is necessary
        'because the trace can be called from other threads.
        Delegate Sub SetTextCallback(ByVal text As String)

        Public Overloads Overrides Sub Write(ByVal message As String)
            SetText(Now.ToString("HH:mm:ss") & New String("  ", MyBase.IndentSize * MyBase.IndentLevel + 1) & message)
        End Sub

        Public Overloads Overrides Sub WriteLine(ByVal message As String)
            SetText(vbCrLf & Now.ToString("HH:mm:ss") & New String("  ", MyBase.IndentSize * MyBase.IndentLevel + 1) & message)
        End Sub

        Public Sub setTextBox(ByVal f As TextBox)
            Me.theTextBox = f
        End Sub

        'enables thread safe accesses of the text box
        Private Sub SetText(ByVal text As String)
            'InvokeRequired required compares the thread ID of the
            'calling thread to the thread ID of the creating thread.
            'If these threads are different, it returns true. and invokes the 
            'delegate
            'If Not theTextBox.FindForm.Visible Then Exit Sub
            If Me.theTextBox.InvokeRequired Then
                Dim d As New SetTextCallback(AddressOf SetText)
                Try
                    theTextBox.Invoke(d, New Object() {text})
                Catch e As Exception
                End Try
            Else
                If Me.theTextBox.Text = "" Then
                    text = text.Replace(vbCrLf, "")
                End If
                Me.theTextBox.AppendText(text)

                numLines = numLines + 1

            End If
            Application.DoEvents()
        End Sub

    End Class
#End Region

End Class


''' <summary>
''' The main program form
''' </summary>
''' <remarks>
''' This class is split up into several partial classes to modularize the code functionality.  For example, 
''' all the functionality in each tab is separated into a separate class. To the compiler, these partial classes
''' are treated as a single class.
''' </remarks>
Public Class MainForm

    ''' <summary>
    ''' A generic delegate function used for form validation
    ''' </summary>
    ''' <remarks>
    ''' When it comes time to validate the form, we can use a loop to loop through all defined
    ''' validators.  Each control will have an associated validation function which is added to this
    ''' list on form load.  The validation function is separate from the validation event handler
    '''  so we can use it from a Control's
    ''' validated event (for the ErrorProvider) or called from a function that validates the whole form.
    ''' </remarks>
    Private Delegate Function validator() As String

    Private WithEvents mapEvents As ESRI.ArcGIS.Carto.Map
    Private WithEvents proc As New Process

    ''' <summary>
    ''' the trace listener object.
    ''' </summary>
    ''' <remarks>
    ''' Should be set either in the class constructor or by the instantiator of this class.  This is used
    ''' to be able to properly unload the trace listener when this form closes (instead of waiting for ArcGIS
    ''' to unload the object).  This avoids problems with multiple instances being run, e.g. closing the app
    ''' and running it again.
    ''' </remarks>
    Public myTraceListener As TraceOutput

    Public ReadOnly Property LogTextbox() As Windows.Forms.TextBox
        Get
            Return Me.txtLog
        End Get
    End Property

    Private Sub MainForm_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        'unload the trace listener.
        If Not myTraceListener Is Nothing Then myTraceListener.close()

        'close any processes
        Me.cancelTransport()
        Me.cancelDarcyFlow()
        Me.cancelParticleTracking()
        Me.cancelDenitrification()
    End Sub

    Private Sub MainForm_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        My.Settings.Save()
    End Sub

    Private Sub MainForm_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        TabControl1.TabPages.RemoveByKey(tabImportantJunk.Name)
        refreshTabs()        
    End Sub

    Private Sub refreshTabs()
        'refresh all the tabs only when a run is not currently in progress
        If flow Is Nothing And ptrack Is Nothing And m_transportRunner Is Nothing And m_denitrification Is Nothing Then
            Trace.WriteLine("TOC Changed. Refreshing...")
            GWinit()
            PTinit()
            TRInit()
            DNInit()
        End If
    End Sub

    Private Sub AboutToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AboutToolStripMenuItem.Click
        
        Dim v As String = Utilities.getAqDnVersion()
        Dim msg As String = ""
#If CONFIG = "Arc9" Or CONFIG = "mydebug-Arc9" Then
        msg = "ArcNLET for ArcGIS 9.x " & v
#ElseIf CONFIG = "Arc10" Or CONFIG = "mydebug-Arc10" Then
        msg = "ArcNLET for ArcGIS 10 " & v
#ElseIf CONFIG = "Arc10.1" Then
        msg = "ArcNLET for ArcGIS v " & v
#ElseIf CONFIG = "Release" Or CONFIG = "Arc10.2" Then
        'msg = "ArcNLET for ArcGIS 10.2 " & v
        'msg = "ArcNLET for ArcGIS 10.3.1 " & v
        'msg = "ArcNLET for ArcGIS 10.4.1 " & v 'Update version info., H.L., 11/07/2017.
        msg = "ArcNLET for ArcGIS 10.6.1 " & v 'Update version info., H.L., 02/24/2019.
#End If
        Trace.WriteLine(msg)
        MsgBox(msg, MsgBoxStyle.Information, "ArcNLET")
    End Sub

    Private Sub btnSaveLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSaveLog.Click
        Dim savedlg As New Windows.Forms.SaveFileDialog

        savedlg.CheckPathExists = True
        savedlg.DefaultExt = "log"
        savedlg.Filter = "*.log|*.log"
        savedlg.FileName = Now.ToString("yyyyMMdd.HHmm") & "." & savedlg.DefaultExt
        savedlg.OverwritePrompt = True
        savedlg.SupportMultiDottedExtensions = True
        savedlg.ValidateNames = True
        savedlg.Title = "Save log"
        Try
            If savedlg.ShowDialog = Windows.Forms.DialogResult.OK Then
                Dim f As New IO.StreamWriter(savedlg.OpenFile)
                f.Write(Me.txtLog.Text)
                f.Close()
            End If
        Catch ex As Exception
            MsgBox("Error saving the log: " & ex.Message)
        End Try
    End Sub

    Private Sub mnuOutputIntermediateToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles mnuOutputIntermediateToolStripMenuItem.Click
        My.Settings.OuputIntermediateCalcs = mnuOutputIntermediateToolStripMenuItem.Checked
    End Sub


    Private Sub btnAbort_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAbort.Click
        cancelDarcyFlow()
        cancelParticleTracking()
        cancelTransport()
        cancelDenitrification()
    End Sub

    Private Sub ParticleTrackingToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ParticleTrackingToolStripMenuItem.Click
        If Not runPT(True) Then
            MsgBox("There were errors running the Particle Tracking module.  Check the message log for details", MsgBoxStyle.Critical)
        End If
    End Sub

    Private Sub GroundwaterToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroundwaterToolStripMenuItem.Click
        If Not runGW(True) Then
            MsgBox("There were errors running the Groundwater module.  Check the message log for details", MsgBoxStyle.Critical)
        End If
    End Sub

    Private Sub TransportToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TransportToolStripMenuItem.Click
        If Not runTransp(True) Then
            MsgBox("There were errors running the Transport module.  Check the message log for details", MsgBoxStyle.Critical)
        End If
    End Sub

    Private Sub DenitrificationToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DenitrificationToolStripMenuItem.Click
        If Not runDn(True) Then
            MsgBox("There were errors running the Nitrate Load Estimation module.  Check the message log for details", MsgBoxStyle.Critical)
        End If
    End Sub

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
        Windows.Forms.Application.Exit()
    End Sub

    Private Sub ExecuteAllModulesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub ClearTempFolderToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ClearTempFolderToolStripMenuItem.Click
        If MsgBox("This will clear your Temp folder (" & IO.Path.GetTempPath & ")" & vbCrLf & "Do not clear it if there is an operation in progress. Continue?", MsgBoxStyle.YesNo Or MsgBoxStyle.Exclamation) = MsgBoxResult.Yes Then
            Utilities.DeleteFilesAndFolders(IO.Path.GetTempPath)
        End If
    End Sub


    Private Sub ShowHelpFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ShowHelpFileToolStripMenuItem.Click
        Dim helpfilename As String = "users_manual.pdf"
        Dim path As String = IO.Path.Combine(IO.Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly.Location), helpfilename)
        Try
            Process.Start(path)
        Catch ex As Exception
            MsgBox("Couldn't open the help file  '" & helpfilename & "'" & vbCrLf & ex.Message, MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub mapEvents_ItemAdded(ByVal Item As Object) Handles mapEvents.ItemAdded
        refreshTabs()
    End Sub

    Private Sub mapEvents_ItemDeleted(ByVal Item As Object) Handles mapEvents.ItemDeleted
        refreshTabs()
    End Sub

    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        mapEvents = Main.ActiveMap
    End Sub

    Private Sub btnMemAdjust_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnMemAdjust.MouseDoubleClick
        If My.Computer.Keyboard.CtrlKeyDown And My.Computer.Keyboard.ShiftKeyDown Then
            Dim str As String = InputBox("Enter MaxMemory (in MB): ", "ArcNLET", My.Settings.MaxMemory)
            If Not str = "" Then
                My.Settings.MaxMemory = str
                My.Settings.Save()
            End If            
        End If
    End Sub


    Private Sub Label56_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label56.Click

    End Sub

    Private Sub Label58_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        'proc.StartInfo.FileName = "python"
        Dim VZMODLOCATIONFOLD As String = txtVZMODLocation.Text
        'If VZMODLOCATIONFOLD = "" Then Throw New Exception("Please select the VZMOD model to calculate the vadose zone transport!")
        If VZMODLOCATIONFOLD <> "" Then
            proc.StartInfo.FileName = VZMODLOCATIONFOLD
            'proc.StartInfo.FileName = "D:\hsun4\YanZhu-file\Couple VZMOD ArcNLET\VZMOD\VZMOD\VZMOD.pyw"
            'proc.StartInfo.FileName = "c:\python27\VZMOD\VZMOD.pyw"
            'proc.StartInfo.Arguments = "D:\hsun4\YanZhu-file\Couple VZMOD ArcNLET\VZMOD\VZMOD\VZMOD.py"
            'proc.StartInfo.CreateNoWindow = True
            'proc.StartInfo.UseShellExecute = False
            'proc.EnableRaisingEvents = True 'Use this if you want to receive the ProcessExited event
            'proc.StartInfo.RedirectStandardOutput = True
            'proc.Start()
            'proc.BeginOutputReadLine()
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Maximized
            Dim myProcess As Process = Process.Start(proc.StartInfo)
        ElseIf VZMODLOCATIONFOLD = "" Then
            Trace.WriteLine("Using the default VZMOD file 'C:\Program Files (x86)\ArcNLET\VZMOD\VZMOD.pyw'")
            Trace.WriteLine("Please make sure the VZMOD files locate in the fold 'C:\Program Files (x86)\ArcNLET\VZMOD'")
            proc.StartInfo.FileName = "C:\Program Files (x86)\ArcNLET\VZMOD\VZMOD.pyw"
            'proc.StartInfo.FileName = "D:\hsun4\YanZhu-file\Couple VZMOD ArcNLET\VZMOD\VZMOD\VZMOD.pyw"
            'proc.StartInfo.FileName = "c:\python27\VZMOD\VZMOD.pyw"
            'proc.StartInfo.Arguments = "D:\hsun4\YanZhu-file\Couple VZMOD ArcNLET\VZMOD\VZMOD\VZMOD.py"
            'proc.StartInfo.CreateNoWindow = True
            'proc.StartInfo.UseShellExecute = False
            'proc.EnableRaisingEvents = True 'Use this if you want to receive the ProcessExited event
            'proc.StartInfo.RedirectStandardOutput = True
            'proc.Start()
            'proc.BeginOutputReadLine()
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Maximized
            Dim myProcess As Process = Process.Start(proc.StartInfo)
        End If
    End Sub



End Class
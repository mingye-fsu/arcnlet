<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class AdditionalOptionsAndParamsTR
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.chkTRWarpingUseApprox = New System.Windows.Forms.CheckBox
        Me.Label2 = New System.Windows.Forms.Label
        Me.cmbTRWarpingMethod = New System.Windows.Forms.ComboBox
        Me.txtTRWarpingCtrlPtSpacing = New System.Windows.Forms.NumericUpDown
        Me.Label1 = New System.Windows.Forms.Label
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.cmbTRPostProcessing = New System.Windows.Forms.ComboBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.txtTRThreshConc = New System.Windows.Forms.TextBox
        Me.Label26 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.cmbTRDomenicoBdy = New System.Windows.Forms.ComboBox
        Me.GroupBox1.SuspendLayout()
        CType(Me.txtTRWarpingCtrlPtSpacing, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.chkTRWarpingUseApprox)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.cmbTRWarpingMethod)
        Me.GroupBox1.Controls.Add(Me.txtTRWarpingCtrlPtSpacing)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Dock = System.Windows.Forms.DockStyle.Top
        Me.GroupBox1.Location = New System.Drawing.Point(0, 0)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(228, 108)
        Me.GroupBox1.TabIndex = 4
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Plume Warping"
        '
        'chkTRWarpingUseApprox
        '
        Me.chkTRWarpingUseApprox.AutoSize = True
        Me.chkTRWarpingUseApprox.Checked = True
        Me.chkTRWarpingUseApprox.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkTRWarpingUseApprox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.chkTRWarpingUseApprox.Location = New System.Drawing.Point(6, 76)
        Me.chkTRWarpingUseApprox.Name = "chkTRWarpingUseApprox"
        Me.chkTRWarpingUseApprox.RightToLeft = System.Windows.Forms.RightToLeft.Yes
        Me.chkTRWarpingUseApprox.Size = New System.Drawing.Size(137, 18)
        Me.chkTRWarpingUseApprox.TabIndex = 4
        Me.chkTRWarpingUseApprox.Text = "Use approximate warp"
        Me.chkTRWarpingUseApprox.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.chkTRWarpingUseApprox.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(3, 16)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(75, 26)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "control point" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "spacing [Cells]"
        '
        'cmbTRWarpingMethod
        '
        Me.cmbTRWarpingMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbTRWarpingMethod.FormattingEnabled = True
        Me.cmbTRWarpingMethod.Location = New System.Drawing.Point(92, 48)
        Me.cmbTRWarpingMethod.Name = "cmbTRWarpingMethod"
        Me.cmbTRWarpingMethod.Size = New System.Drawing.Size(121, 21)
        Me.cmbTRWarpingMethod.TabIndex = 2
        '
        'txtTRWarpingCtrlPtSpacing
        '
        Me.txtTRWarpingCtrlPtSpacing.Location = New System.Drawing.Point(92, 22)
        Me.txtTRWarpingCtrlPtSpacing.Maximum = New Decimal(New Integer() {1000, 0, 0, 0})
        Me.txtTRWarpingCtrlPtSpacing.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.txtTRWarpingCtrlPtSpacing.Name = "txtTRWarpingCtrlPtSpacing"
        Me.txtTRWarpingCtrlPtSpacing.Size = New System.Drawing.Size(63, 20)
        Me.txtTRWarpingCtrlPtSpacing.TabIndex = 3
        Me.txtTRWarpingCtrlPtSpacing.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.txtTRWarpingCtrlPtSpacing.Value = New Decimal(New Integer() {48, 0, 0, 0})
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 51)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(43, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Method"
        '
        'cmbTRPostProcessing
        '
        Me.cmbTRPostProcessing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbTRPostProcessing.FormattingEnabled = True
        Me.cmbTRPostProcessing.Location = New System.Drawing.Point(92, 141)
        Me.cmbTRPostProcessing.Name = "cmbTRPostProcessing"
        Me.cmbTRPostProcessing.Size = New System.Drawing.Size(121, 21)
        Me.cmbTRPostProcessing.TabIndex = 5
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(3, 144)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(83, 13)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "Post Processing"
        '
        'txtTRThreshConc
        '
        Me.txtTRThreshConc.Location = New System.Drawing.Point(92, 111)
        Me.txtTRThreshConc.Name = "txtTRThreshConc"
        Me.txtTRThreshConc.Size = New System.Drawing.Size(82, 20)
        Me.txtTRThreshConc.TabIndex = 39
        Me.txtTRThreshConc.Text = "0.000001"
        Me.txtTRThreshConc.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label26
        '
        Me.Label26.AutoSize = True
        Me.Label26.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label26.Location = New System.Drawing.Point(6, 111)
        Me.Label26.Margin = New System.Windows.Forms.Padding(0)
        Me.Label26.Name = "Label26"
        Me.Label26.Size = New System.Drawing.Size(72, 26)
        Me.Label26.TabIndex = 40
        Me.Label26.Text = "Threshold" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Conc. [M/L^3]"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(3, 171)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(79, 13)
        Me.Label4.TabIndex = 42
        Me.Label4.Text = "Domenico Bdy."
        '
        'cmbTRDomenicoBdy
        '
        Me.cmbTRDomenicoBdy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbTRDomenicoBdy.DropDownWidth = 220
        Me.cmbTRDomenicoBdy.FormattingEnabled = True
        Me.cmbTRDomenicoBdy.Location = New System.Drawing.Point(92, 168)
        Me.cmbTRDomenicoBdy.Name = "cmbTRDomenicoBdy"
        Me.cmbTRDomenicoBdy.Size = New System.Drawing.Size(121, 21)
        Me.cmbTRDomenicoBdy.TabIndex = 41
        '
        'AdditionalOptionsAndParamsTR
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.cmbTRDomenicoBdy)
        Me.Controls.Add(Me.txtTRThreshConc)
        Me.Controls.Add(Me.Label26)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.cmbTRPostProcessing)
        Me.Controls.Add(Me.GroupBox1)
        Me.MinimumSize = New System.Drawing.Size(228, 117)
        Me.Name = "AdditionalOptionsAndParamsTR"
        Me.Size = New System.Drawing.Size(228, 204)
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        CType(Me.txtTRWarpingCtrlPtSpacing, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents chkTRWarpingUseApprox As System.Windows.Forms.CheckBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cmbTRWarpingMethod As System.Windows.Forms.ComboBox
    Friend WithEvents txtTRWarpingCtrlPtSpacing As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
    Friend WithEvents cmbTRPostProcessing As System.Windows.Forms.ComboBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents txtTRThreshConc As System.Windows.Forms.TextBox
    Friend WithEvents Label26 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents cmbTRDomenicoBdy As System.Windows.Forms.ComboBox

End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ParticleTrackForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ParticleTrackForm))
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.Label7 = New System.Windows.Forms.Label
        Me.txtPTWBRasterCellSz = New System.Windows.Forms.TextBox
        Me.txtPTMaxSteps = New System.Windows.Forms.NumericUpDown
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.txtPTStepSize = New System.Windows.Forms.TextBox
        Me.btnLayerWBInfo = New System.Windows.Forms.LinkLabel
        Me.Label1 = New System.Windows.Forms.Label
        Me.cmbWB = New AqDn.LayersComboBox
        Me.cmbPointLayer = New AqDn.LayersComboBox
        Me.radUseLayer = New System.Windows.Forms.RadioButton
        Me.radUseManual = New System.Windows.Forms.RadioButton
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar
        Me.btnGo = New System.Windows.Forms.Button
        Me.txtYCoord = New System.Windows.Forms.TextBox
        Me.txtXCoord = New System.Windows.Forms.TextBox
        Me.btnLayerDirInfo = New System.Windows.Forms.LinkLabel
        Me.btnLayerInfoMag = New System.Windows.Forms.LinkLabel
        Me.lblY = New System.Windows.Forms.Label
        Me.lblX = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.txtName = New System.Windows.Forms.TextBox
        Me.lblDesc = New System.Windows.Forms.Label
        Me.cmbRasterDir = New AqDn.LayersComboBox
        Me.cmbRasterMag = New AqDn.LayersComboBox
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.txtMessageLog = New System.Windows.Forms.TextBox
        Me.ErrorProvider1 = New System.Windows.Forms.ErrorProvider(Me.components)
        Me.btnLayerPorosityInfo = New System.Windows.Forms.LinkLabel
        Me.Label8 = New System.Windows.Forms.Label
        Me.cmbPorosity = New AqDn.LayersComboBox
        Me.GroupBox1.SuspendLayout()
        CType(Me.txtPTMaxSteps, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ErrorProvider1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox1.Controls.Add(Me.btnLayerPorosityInfo)
        Me.GroupBox1.Controls.Add(Me.Label8)
        Me.GroupBox1.Controls.Add(Me.cmbPorosity)
        Me.GroupBox1.Controls.Add(Me.Label7)
        Me.GroupBox1.Controls.Add(Me.txtPTWBRasterCellSz)
        Me.GroupBox1.Controls.Add(Me.txtPTMaxSteps)
        Me.GroupBox1.Controls.Add(Me.Label6)
        Me.GroupBox1.Controls.Add(Me.Label5)
        Me.GroupBox1.Controls.Add(Me.txtPTStepSize)
        Me.GroupBox1.Controls.Add(Me.btnLayerWBInfo)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Controls.Add(Me.cmbWB)
        Me.GroupBox1.Controls.Add(Me.cmbPointLayer)
        Me.GroupBox1.Controls.Add(Me.radUseLayer)
        Me.GroupBox1.Controls.Add(Me.radUseManual)
        Me.GroupBox1.Controls.Add(Me.ProgressBar1)
        Me.GroupBox1.Controls.Add(Me.btnGo)
        Me.GroupBox1.Controls.Add(Me.txtYCoord)
        Me.GroupBox1.Controls.Add(Me.txtXCoord)
        Me.GroupBox1.Controls.Add(Me.btnLayerDirInfo)
        Me.GroupBox1.Controls.Add(Me.btnLayerInfoMag)
        Me.GroupBox1.Controls.Add(Me.lblY)
        Me.GroupBox1.Controls.Add(Me.lblX)
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.txtName)
        Me.GroupBox1.Controls.Add(Me.lblDesc)
        Me.GroupBox1.Controls.Add(Me.cmbRasterDir)
        Me.GroupBox1.Controls.Add(Me.cmbRasterMag)
        Me.GroupBox1.Location = New System.Drawing.Point(3, 4)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(400, 264)
        Me.GroupBox1.TabIndex = 5
        Me.GroupBox1.TabStop = False
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(9, 181)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(84, 13)
        Me.Label7.TabIndex = 31
        Me.Label7.Text = "WB Raster Res."
        '
        'txtPTWBRasterCellSz
        '
        Me.txtPTWBRasterCellSz.Location = New System.Drawing.Point(9, 199)
        Me.txtPTWBRasterCellSz.Name = "txtPTWBRasterCellSz"
        Me.txtPTWBRasterCellSz.Size = New System.Drawing.Size(67, 20)
        Me.txtPTWBRasterCellSz.TabIndex = 30
        Me.txtPTWBRasterCellSz.Text = "10"
        Me.ToolTip1.SetToolTip(Me.txtPTWBRasterCellSz, "The cell size for converting the water bodies to raster")
        '
        'txtPTMaxSteps
        '
        Me.txtPTMaxSteps.Location = New System.Drawing.Point(193, 199)
        Me.txtPTMaxSteps.Maximum = New Decimal(New Integer() {999999, 0, 0, 0})
        Me.txtPTMaxSteps.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.txtPTMaxSteps.Name = "txtPTMaxSteps"
        Me.txtPTMaxSteps.Size = New System.Drawing.Size(60, 20)
        Me.txtPTMaxSteps.TabIndex = 29
        Me.txtPTMaxSteps.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        Me.ToolTip1.SetToolTip(Me.txtPTMaxSteps, "The maximum number of steps to take")
        Me.txtPTMaxSteps.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(190, 181)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(60, 13)
        Me.Label6.TabIndex = 22
        Me.Label6.Text = "Max Steps:"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(102, 181)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(55, 13)
        Me.Label5.TabIndex = 20
        Me.Label5.Text = "Step Size:"
        '
        'txtPTStepSize
        '
        Me.txtPTStepSize.Location = New System.Drawing.Point(102, 199)
        Me.txtPTStepSize.Name = "txtPTStepSize"
        Me.txtPTStepSize.Size = New System.Drawing.Size(67, 20)
        Me.txtPTStepSize.TabIndex = 19
        Me.txtPTStepSize.Text = "10"
        Me.ToolTip1.SetToolTip(Me.txtPTStepSize, "The size (in map units) of each step")
        '
        'btnLayerWBInfo
        '
        Me.btnLayerWBInfo.AutoSize = True
        Me.btnLayerWBInfo.Location = New System.Drawing.Point(99, 136)
        Me.btnLayerWBInfo.Name = "btnLayerWBInfo"
        Me.btnLayerWBInfo.Size = New System.Drawing.Size(54, 13)
        Me.btnLayerWBInfo.TabIndex = 18
        Me.btnLayerWBInfo.TabStop = True
        Me.btnLayerWBInfo.Text = "Layer Info"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 136)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(74, 13)
        Me.Label1.TabIndex = 17
        Me.Label1.Text = "Water Bodies:"
        '
        'cmbWB
        '
        Me.cmbWB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbWB.FormattingEnabled = True
        Me.cmbWB.Location = New System.Drawing.Point(9, 153)
        Me.cmbWB.MaxDropDownItems = 15
        Me.cmbWB.Name = "cmbWB"
        Me.cmbWB.Size = New System.Drawing.Size(144, 21)
        Me.cmbWB.TabIndex = 16
        Me.ToolTip1.SetToolTip(Me.cmbWB, "The Darcy velocity direction raster")
        '
        'cmbPointLayer
        '
        Me.cmbPointLayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbPointLayer.FormattingEnabled = True
        Me.cmbPointLayer.Location = New System.Drawing.Point(174, 106)
        Me.cmbPointLayer.MaxDropDownItems = 15
        Me.cmbPointLayer.Name = "cmbPointLayer"
        Me.cmbPointLayer.Size = New System.Drawing.Size(201, 21)
        Me.cmbPointLayer.TabIndex = 15
        Me.cmbPointLayer.Visible = False
        '
        'radUseLayer
        '
        Me.radUseLayer.AutoSize = True
        Me.radUseLayer.Location = New System.Drawing.Point(285, 17)
        Me.radUseLayer.Name = "radUseLayer"
        Me.radUseLayer.Size = New System.Drawing.Size(100, 17)
        Me.radUseLayer.TabIndex = 14
        Me.radUseLayer.Text = "Use Point Layer"
        Me.radUseLayer.UseVisualStyleBackColor = True
        Me.radUseLayer.Visible = False
        '
        'radUseManual
        '
        Me.radUseManual.AutoSize = True
        Me.radUseManual.Checked = True
        Me.radUseManual.Location = New System.Drawing.Point(160, 17)
        Me.radUseManual.Name = "radUseManual"
        Me.radUseManual.Size = New System.Drawing.Size(125, 17)
        Me.radUseManual.TabIndex = 13
        Me.radUseManual.TabStop = True
        Me.radUseManual.Text = "Specify X,Y Manually"
        Me.radUseManual.UseVisualStyleBackColor = True
        Me.radUseManual.Visible = False
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Location = New System.Drawing.Point(89, 226)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(283, 18)
        Me.ProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee
        Me.ProgressBar1.TabIndex = 12
        Me.ProgressBar1.Visible = False
        '
        'btnGo
        '
        Me.btnGo.Font = New System.Drawing.Font("Microsoft Sans Serif", 7.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnGo.Location = New System.Drawing.Point(8, 225)
        Me.btnGo.Name = "btnGo"
        Me.btnGo.Size = New System.Drawing.Size(75, 20)
        Me.btnGo.TabIndex = 11
        Me.btnGo.Text = "Go"
        Me.btnGo.UseVisualStyleBackColor = True
        '
        'txtYCoord
        '
        Me.txtYCoord.Location = New System.Drawing.Point(282, 107)
        Me.txtYCoord.Name = "txtYCoord"
        Me.txtYCoord.Size = New System.Drawing.Size(100, 20)
        Me.txtYCoord.TabIndex = 10
        Me.ToolTip1.SetToolTip(Me.txtYCoord, "Starting Y coord. in map units")
        '
        'txtXCoord
        '
        Me.txtXCoord.Location = New System.Drawing.Point(168, 107)
        Me.txtXCoord.Name = "txtXCoord"
        Me.txtXCoord.Size = New System.Drawing.Size(100, 20)
        Me.txtXCoord.TabIndex = 10
        Me.ToolTip1.SetToolTip(Me.txtXCoord, "Starting X coord. in map units")
        '
        'btnLayerDirInfo
        '
        Me.btnLayerDirInfo.AutoSize = True
        Me.btnLayerDirInfo.Location = New System.Drawing.Point(99, 89)
        Me.btnLayerDirInfo.Name = "btnLayerDirInfo"
        Me.btnLayerDirInfo.Size = New System.Drawing.Size(54, 13)
        Me.btnLayerDirInfo.TabIndex = 9
        Me.btnLayerDirInfo.TabStop = True
        Me.btnLayerDirInfo.Text = "Layer Info"
        '
        'btnLayerInfoMag
        '
        Me.btnLayerInfoMag.AutoSize = True
        Me.btnLayerInfoMag.Location = New System.Drawing.Point(99, 49)
        Me.btnLayerInfoMag.Name = "btnLayerInfoMag"
        Me.btnLayerInfoMag.Size = New System.Drawing.Size(54, 13)
        Me.btnLayerInfoMag.TabIndex = 9
        Me.btnLayerInfoMag.TabStop = True
        Me.btnLayerInfoMag.Text = "Layer Info"
        '
        'lblY
        '
        Me.lblY.AutoSize = True
        Me.lblY.Location = New System.Drawing.Point(269, 109)
        Me.lblY.Name = "lblY"
        Me.lblY.Size = New System.Drawing.Size(14, 13)
        Me.lblY.TabIndex = 8
        Me.lblY.Text = "Y"
        '
        'lblX
        '
        Me.lblX.AutoSize = True
        Me.lblX.Location = New System.Drawing.Point(154, 109)
        Me.lblX.Name = "lblX"
        Me.lblX.Size = New System.Drawing.Size(14, 13)
        Me.lblX.TabIndex = 8
        Me.lblX.Text = "X"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(9, 89)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(52, 13)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "Direction:"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(9, 49)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(60, 13)
        Me.Label3.TabIndex = 8
        Me.Label3.Text = "Magnitude:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(9, 10)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(38, 13)
        Me.Label2.TabIndex = 8
        Me.Label2.Text = "Name:"
        '
        'txtName
        '
        Me.txtName.Location = New System.Drawing.Point(9, 28)
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(144, 20)
        Me.txtName.TabIndex = 7
        Me.ToolTip1.SetToolTip(Me.txtName, "File name for the tracking file. Will be saved in the same directory as the proje" & _
                "ct" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "with a .txt extension")
        '
        'lblDesc
        '
        Me.lblDesc.AutoSize = True
        Me.lblDesc.Location = New System.Drawing.Point(157, 37)
        Me.lblDesc.Name = "lblDesc"
        Me.lblDesc.Size = New System.Drawing.Size(218, 65)
        Me.lblDesc.TabIndex = 6
        Me.lblDesc.Text = resources.GetString("lblDesc.Text")
        '
        'cmbRasterDir
        '
        Me.cmbRasterDir.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbRasterDir.FormattingEnabled = True
        Me.cmbRasterDir.Location = New System.Drawing.Point(9, 106)
        Me.cmbRasterDir.MaxDropDownItems = 15
        Me.cmbRasterDir.Name = "cmbRasterDir"
        Me.cmbRasterDir.Size = New System.Drawing.Size(144, 21)
        Me.cmbRasterDir.TabIndex = 5
        Me.ToolTip1.SetToolTip(Me.cmbRasterDir, "The Darcy velocity direction raster")
        '
        'cmbRasterMag
        '
        Me.cmbRasterMag.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbRasterMag.FormattingEnabled = True
        Me.cmbRasterMag.Location = New System.Drawing.Point(9, 66)
        Me.cmbRasterMag.MaxDropDownItems = 15
        Me.cmbRasterMag.Name = "cmbRasterMag"
        Me.cmbRasterMag.Size = New System.Drawing.Size(144, 21)
        Me.cmbRasterMag.TabIndex = 5
        Me.ToolTip1.SetToolTip(Me.cmbRasterMag, "The Darcy velocity magnitude raster.")
        '
        'txtMessageLog
        '
        Me.txtMessageLog.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtMessageLog.Font = New System.Drawing.Font("Microsoft Sans Serif", 6.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMessageLog.Location = New System.Drawing.Point(-2, 274)
        Me.txtMessageLog.Multiline = True
        Me.txtMessageLog.Name = "txtMessageLog"
        Me.txtMessageLog.ReadOnly = True
        Me.txtMessageLog.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtMessageLog.Size = New System.Drawing.Size(410, 150)
        Me.txtMessageLog.TabIndex = 6
        Me.txtMessageLog.WordWrap = False
        '
        'ErrorProvider1
        '
        Me.ErrorProvider1.ContainerControl = Me
        '
        'btnLayerPorosityInfo
        '
        Me.btnLayerPorosityInfo.AutoSize = True
        Me.btnLayerPorosityInfo.Location = New System.Drawing.Point(258, 136)
        Me.btnLayerPorosityInfo.Name = "btnLayerPorosityInfo"
        Me.btnLayerPorosityInfo.Size = New System.Drawing.Size(54, 13)
        Me.btnLayerPorosityInfo.TabIndex = 34
        Me.btnLayerPorosityInfo.TabStop = True
        Me.btnLayerPorosityInfo.Text = "Layer Info"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(168, 136)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(47, 13)
        Me.Label8.TabIndex = 33
        Me.Label8.Text = "Porosity:"
        '
        'cmbPorosity
        '
        Me.cmbPorosity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbPorosity.FormattingEnabled = True
        Me.cmbPorosity.Location = New System.Drawing.Point(168, 153)
        Me.cmbPorosity.MaxDropDownItems = 15
        Me.cmbPorosity.Name = "cmbPorosity"
        Me.cmbPorosity.Size = New System.Drawing.Size(144, 21)
        Me.cmbPorosity.TabIndex = 32
        Me.ToolTip1.SetToolTip(Me.cmbPorosity, "The Darcy velocity direction raster")
        '
        'ParticleTrackForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(406, 423)
        Me.Controls.Add(Me.txtMessageLog)
        Me.Controls.Add(Me.GroupBox1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow
        Me.Name = "ParticleTrackForm"
        Me.Text = "ParticleTrack"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        CType(Me.txtPTMaxSteps, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ErrorProvider1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents lblDesc As System.Windows.Forms.Label
    Friend WithEvents cmbRasterMag As AqDn.LayersComboBox
    Friend WithEvents btnLayerInfoMag As System.Windows.Forms.LinkLabel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtName As System.Windows.Forms.TextBox
    Friend WithEvents txtYCoord As System.Windows.Forms.TextBox
    Friend WithEvents txtXCoord As System.Windows.Forms.TextBox
    Friend WithEvents lblY As System.Windows.Forms.Label
    Friend WithEvents lblX As System.Windows.Forms.Label
    Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
    Friend WithEvents btnGo As System.Windows.Forms.Button
    Friend WithEvents txtMessageLog As System.Windows.Forms.TextBox
    Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
    Friend WithEvents radUseLayer As System.Windows.Forms.RadioButton
    Friend WithEvents radUseManual As System.Windows.Forms.RadioButton
    Friend WithEvents cmbPointLayer As AqDn.LayersComboBox
    Friend WithEvents btnLayerWBInfo As System.Windows.Forms.LinkLabel
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents cmbWB As AqDn.LayersComboBox
    Friend WithEvents btnLayerDirInfo As System.Windows.Forms.LinkLabel
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents cmbRasterDir As AqDn.LayersComboBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents txtPTStepSize As System.Windows.Forms.TextBox
    Friend WithEvents txtPTMaxSteps As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents txtPTWBRasterCellSz As System.Windows.Forms.TextBox
    Private WithEvents ErrorProvider1 As System.Windows.Forms.ErrorProvider
    Friend WithEvents btnLayerPorosityInfo As System.Windows.Forms.LinkLabel
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents cmbPorosity As AqDn.LayersComboBox
End Class

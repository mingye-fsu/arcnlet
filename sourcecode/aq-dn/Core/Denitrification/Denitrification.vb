Imports ESRI.ArcGIS.ADF                     'com management
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile         'for opening shapefiles
Imports ESRI.ArcGIS.Geodatabase             'feature classes and cursors
Imports ESRI.ArcGIS.Geometry                'points polylines etc.
Imports ESRI.ArcGIS.GeoAnalyst              'requires spatial analyst
Imports ESRI.ArcGIS.SpatialAnalyst


''' <summary>
''' Calculates the (mass) load per unit time to the provided water bodies.
''' </summary>
''' <remarks>
''' The calculation is carried out given 
''' a map defining the location and shape of the combined contaminant plumes of all the
''' provided sources.  The location and shape of the plumes is used in conjunction with
''' a plume thickness obtained from the Z thickness of the source plane of the Domenico solution 
''' to estimate a volume in which denitrification occurs.  This module only supports 2D steady state
''' solutions of the Domenico model.
''' <para>
''' This module assumes all grounwater flow paths terminate at the same water body
''' </para>
''' </remarks>
Public Class Denitrification


    Private m_plumesInfo As IFeatureClass
    Private m_plumesInfo_NH4 As IFeatureClass
    Private m_sourceID As Integer
    Private m_volConversionFac As Single
    Private m_riskfac As Single
    Private m_SEL_DOMENICO_DN As DomenicoSourceBoundaries.DomenicoSourceBoundary

    Private m_plumesInfoTable As List(Of PlumeInfo)             'the associeated plume information
    Private m_plumesInfoTable_NH4 As List(Of PlumeInfo)

    Private m_outputintermediate As Boolean
    Private m_outputintermediate_path As String
    Private m_outputintermediate_outputs As Hashtable           'the list of paths to intermediate outputs

    Private m_cancelDenitrification As Boolean

    Private isMT3DDispDR2 As Boolean = False                    'only for comparing my mt3d model with the improved Domenico SOurce


    ''' <summary>
    ''' after calculation is complete, holds the file paths to any intermediate outputs
    ''' If there were no outputs or the user selected not to output intermediate
    ''' calculations, this list will be empty
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property OutParams() As Hashtable
        Get
            Return m_outputintermediate_outputs
        End Get
    End Property


    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="PlumesInfo">A point class containing the information for each individual plume (generated from
    ''' the transport module)
    ''' </param>
    ''' <param name="RiskFactor">A constant by which the resulting load will be multiplied</param>    
    ''' <param name="SourceID">If specified, only includes the source with the given PathID in the calculation</param>
    ''' <param name="OutputIntermediateCalcs">Outputs any intermediate calculations for troubleshooting purposes.</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal PlumesInfo As FeatureClass, _
                   ByVal RiskFactor As Single, _
                   Optional ByVal SourceID As Integer = -1, _
                   Optional ByVal OutputIntermediateCalcs As Boolean = False)

        m_plumesInfo = PlumesInfo
        m_outputintermediate = OutputIntermediateCalcs
        m_outputintermediate_path = CType(PlumesInfo, IDataset).Workspace.PathName
        m_sourceID = SourceID
        m_riskfac = RiskFactor



        m_outputintermediate_outputs = New Hashtable(10)


        m_cancelDenitrification = False

    End Sub
    Public Sub New(ByVal PlumesInfo As FeatureClass, _
                   ByVal PlumesInfo_NH4 As FeatureClass, _
                   ByVal RiskFactor As Single, _
                   Optional ByVal SourceID As Integer = -1, _
                   Optional ByVal OutputIntermediateCalcs As Boolean = False)

        m_plumesInfo = PlumesInfo
        m_plumesInfo_NH4 = PlumesInfo_NH4
        m_outputintermediate = OutputIntermediateCalcs
        m_outputintermediate_path = CType(PlumesInfo, IDataset).Workspace.PathName
        m_sourceID = SourceID
        m_riskfac = RiskFactor


        m_outputintermediate_outputs = New Hashtable(10)


        m_cancelDenitrification = False

    End Sub
    

    ''' <summary>
    ''' Calculates the load on the waterbody specified by the plumesInfo file
    ''' </summary>
    ''' <returns>Returns true on success, false on error</returns>
    ''' <remarks>The results will be available in the OutParams after this function returns
    ''' successfully</remarks>
    Public Function CalculateLoad(Optional ByVal echoToTrace As Boolean = False) As Boolean
        Utilities.outputSystemInfo()

        Trace.Indent()
        Trace.WriteLine("Calculating total load on waterbodies...")

        Dim ret As Boolean = True

        Try

            If Not init() Then Throw New Exception("Couldn't get initialize plumes info")

            If m_cancelDenitrification Then Throw New Exception("Operation cancelled.")

            'find the total amount of denitrification for each water body
            'all water bodies will be output. the input and output loads for plumes that 
            'don't reach the water body are not counted in this query
            Dim query = From allWbFID In _
                            ( _
                                From path In m_plumesInfoTable _
                                Select path.DestinationWaterbodyIDPath _
                                Distinct _
                            ) _
                        Group Join plume In m_plumesInfoTable _
                            On allWbFID Equals plume.DestinationWaterbodyIDPlume _
                            Into sumM0 = Sum(plume.MassInputRate), sumMdn = Sum(plume.MassDenitrificationOutRate_IsolatedPlume), sumMLoad = Sum(plume.MassInputRate - plume.MassDenitrificationOutRate_IsolatedPlume) _
                            Where allWbFID <> -1

            'get the input load and denitrification load from plumes that don't reach the water body
            Dim query2 = From plume In m_plumesInfoTable _
                       Where plume.DestinationWaterbodyIDPlume = -1 _
                       Group By plume.DestinationWaterbodyIDPath Into _
                        sumM0 = Sum(plume.MassInputRate), sumMdn = Sum(plume.MassDenitrificationOutRate_IsolatedPlume)

            'For each waterbody ID, the contributions to the value of input to groundwater from septic tanks (m0),
            'the denitrification mass rate (mdn) and the load to the water body (mload) are calculated.
            'For plumes that reach the water body, the summed contributions of those plumes to m0, mdn and mload
            'corresponding to that water body ID are contained in the "result" query.  The "result2" query
            'contains the contributions to m0 and mdn of plumes that don't reach specified water body.  These 
            'contributions are then added to the contributions from plumes that do reach the water body.
            'Note that for plumes that don't reach the water body the contribution to mdn is equal to m0 
            'because all of the plume is denitrified.
            Dim m0, mdn, mload As Single
            Dim i As Integer = 0
            Dim highlight As String
            Dim outstring As String = "{\rtf1\ansi\ansicpg1252\deff0\deflang1033\deflangfe1033{\fonttbl{\f0\fmodern\fprq1\fcharset0 Lucida Console;}}\viewkind4\uc1\pard\tx630\tx2340\tx4230\tx5940\f0\fs20 "
            Dim outstringtxt As String = ""
            Dim str As String
            For Each result In query
                m0 = result.sumM0
                mdn = result.sumMdn
                mload = result.sumMLoad * m_riskfac
                For Each result2 In query2
                    If result.allWbFID = result2.DestinationWaterbodyIDPath Then
                        m0 = m0 + result2.sumM0
                        mdn = mdn + result2.sumM0
                    End If
                Next
                If i Mod 2 = 0 Then
                    highlight = "\highlight0 "
                Else
                    highlight = "\highlight1 "
                End If
                i = i + 1
                outstring = outstring & highlight & result.allWbFID.ToString("0000") & "\tab " & _
                                                    result.sumMLoad.ToString("N") & "\tab " & _
                                            "\b " & mload.ToString("N") & "\b0\tab " & _
                                                    mdn.ToString("N") & "\tab " & _
                                                    m0.ToString("N") & "\par"
                str = result.allWbFID & "  " & result.sumMLoad & vbTab & mload & vbTab & mdn & vbTab & m0
                If outstringtxt = "" Then
                    outstringtxt = str
                Else
                    outstringtxt = outstringtxt & vbCrLf & str
                End If

                If echoToTrace Then Trace.WriteLine(str)
            Next
            outstring = outstring & "}"

            m_outputintermediate_outputs.Add("rtf", outstring)
            m_outputintermediate_outputs.Add("txt", outstringtxt)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = False
        End Try

        GC.Collect()
        GC.WaitForPendingFinalizers()


        Trace.WriteLine("Calculating total load on waterbodies...Done")
        Trace.Unindent()
        Return ret
    End Function

    Public Function CalculateLoad_NO3(Optional ByVal echoToTrace As Boolean = False) As Boolean
        Utilities.outputSystemInfo()

        Trace.Indent()
        Trace.WriteLine("Calculating total load on waterbodies...")

        Dim ret As Boolean = True

        Try

            If Not UpdateInfor_NO3() Then Throw New Exception("Couldn't get initialize plumes info")

            If m_cancelDenitrification Then Throw New Exception("Operation cancelled.")

            'find the total amount of denitrification for each water body
            'all water bodies will be output. the input and output loads for plumes that 
            'don't reach the water body are not counted in this query


            Dim query = From allWbFID In _
                            ( _
                                From path In m_plumesInfoTable _
                                Select path.DestinationWaterbodyIDPath _
                                Distinct _
                            ) _
                        Group Join plume In m_plumesInfoTable _
                            On allWbFID Equals plume.DestinationWaterbodyIDPlume _
                            Into sumM0 = Sum(plume.MassInputRate), sumMdn = Sum(plume.MassDenitrificationOutRate_IsolatedPlume), _
                            SumMni = Sum(plume.MeshDy), _
                           sumMLoad = Sum(plume.MassInputRate + plume.MeshDy - plume.MassDenitrificationOutRate_IsolatedPlume) _
                            Where allWbFID <> -1

            Dim query3 = From allWbFID_NH4 In _
                ( _
                    From path In m_plumesInfoTable _
                    Select path.DestinationWaterbodyIDPath _
                    Distinct _
                ) _
            Group Join plume In m_plumesInfoTable _
                On allWbFID_NH4 Equals plume.PathID _
                Into sumM0_NH4 = Sum(plume.MeshDy), _
                SumMni_NH4 = Sum(plume.MeshDx), _
               sumMLoad_NH4 = Sum(plume.MeshDy - plume.MeshDx) _
                Where allWbFID_NH4 <> -1



            'get the input load and denitrification load from plumes that don't reach the water body
            Dim query2 = From plume In m_plumesInfoTable _
                       Where plume.DestinationWaterbodyIDPlume = -1 _
                       Group By plume.DestinationWaterbodyIDPath Into _
                        sumM0 = Sum(plume.MassInputRate), sumMdn = Sum(plume.MassDenitrificationOutRate_IsolatedPlume), _
                        SumMni = Sum(plume.MeshDy)
            'For each waterbody ID, the contributions to the value of input to groundwater from septic tanks (m0),
            'the denitrification mass rate (mdn) and the load to the water body (mload) are calculated.
            'For plumes that reach the water body, the summed contributions of those plumes to m0, mdn and mload
            'corresponding to that water body ID are contained in the "result" query.  The "result2" query
            'contains the contributions to m0 and mdn of plumes that don't reach specified water body.  These 
            'contributions are then added to the contributions from plumes that do reach the water body.
            'Note that for plumes that don't reach the water body the contribution to mdn is equal to m0 
            'because all of the plume is denitrified.
            Dim m0, mdn, mload, mni, mload_nh4 As Single
            Dim i As Integer = 0
            Dim highlight As String
            Dim outstring As String = "{\rtf1\ansi\ansicpg1252\deff0\deflang1033\deflangfe1033{\fonttbl{\f0\fmodern\fprq1\fcharset0 Lucida Console;}}\viewkind4\uc1\pard\tx630\tx2340\tx4230\tx5940\f0\fs20 "
            Dim outstringtxt As String = ""
            Dim str As String
            For Each result In query
                m0 = result.sumM0
                mni = result.SumMni
                mdn = result.sumMdn
                mload = result.sumMLoad

                'For Each result3 In query3
                'mload_nh4 = result3.sumMLoad_NH4 * m_riskfac
                'Trace.WriteLine(mload_nh4)
                'Next

                'mload = result.sumMLoad * m_riskfac - mload_nh4
                'mdn = mdn - mload_nh4

                For Each result2 In query2
                    If result.allWbFID = result2.DestinationWaterbodyIDPath Then
                        m0 = m0 + result2.sumM0
                        mni = mni + result2.SumMni
                        mdn = mdn + result2.sumM0 + result2.SumMni
                    End If

                Next
                'mload = (m0 + mni - mdn) * m_riskfac
                If i Mod 2 = 0 Then
                    highlight = "\highlight0 "
                Else
                    highlight = "\highlight1 "
                End If
                i = i + 1
                outstring = outstring & highlight & result.allWbFID.ToString("0000") & "\tab " & _
                                                    mload.ToString("N") & "\tab " & _
                                            "\b " & mload.ToString("N") & "\b0\tab " & _
                                                    mdn.ToString("N") & "\tab " & _
                                                    m0.ToString("N") & "\tab " & _
                                                    mni.ToString("N") & "\par"
                str = result.allWbFID & "  " & mload & vbTab & mload & vbTab & mdn & vbTab & m0 & vbTab & mni
                If outstringtxt = "" Then
                    outstringtxt = str
                Else
                    outstringtxt = outstringtxt & vbCrLf & str
                End If

                If echoToTrace Then Trace.WriteLine(str)


            Next
            outstring = outstring & "}"

            m_outputintermediate_outputs.Add("rtf", outstring)
            m_outputintermediate_outputs.Add("txt", outstringtxt)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = False
        End Try

        GC.Collect()
        GC.WaitForPendingFinalizers()


        Trace.WriteLine("Calculating total load on waterbodies...Done")
        Trace.Unindent()
        Return ret
    End Function

    Public Function CalculateLoad_NO3_1(Optional ByVal echoToTrace As Boolean = False) As Boolean
        Utilities.outputSystemInfo()

        Trace.Indent()
        Trace.WriteLine("Calculating total load on waterbodies...")

        Dim ret As Boolean = True

        Try

            If Not UpdateInfor_NO3() Then Throw New Exception("Couldn't get initialize plumes info")

            If m_cancelDenitrification Then Throw New Exception("Operation cancelled.")

            'find the total amount of denitrification for each water body
            'all water bodies will be output. the input and output loads for plumes that 
            'don't reach the water body are not counted in this query


            Dim query = From allWbFID In _
                            ( _
                                From path In m_plumesInfoTable _
                                Select path.DestinationWaterbodyIDPath _
                                Distinct _
                            ) _
                        Group Join plume In m_plumesInfoTable _
                            On allWbFID Equals plume.DestinationWaterbodyIDPlume _
                            Into sumM0 = Sum(plume.MassInputRate), sumMdn = Sum(plume.MassDenitrificationOutRate_IsolatedPlume), _
                            sumM0_NH4 = Sum(plume.MassInputRate_NH4), _
                            SumMni = Sum(plume.Mass_Nitrification), _
                            sumMLoad = Sum(plume.MassInputRate + plume.Mass_Nitrification - plume.MassDenitrificationOutRate_IsolatedPlume) _
                            Where allWbFID <> -1




            Dim query2 = From plume In m_plumesInfoTable _
                       Where plume.DestinationWaterbodyIDPlume = -1 _
                       Group By plume.DestinationWaterbodyIDPath Into _
                        sumM0 = Sum(plume.MassInputRate), sumMdn = Sum(plume.MassDenitrificationOutRate_IsolatedPlume), _
                        SumMni = Sum(plume.Mass_Nitrification), sumM0_NH4 = Sum(plume.MassInputRate_NH4)
            'For each waterbody ID, the contributions to the value of input to groundwater from septic tanks (m0),
            'the denitrification mass rate (mdn) and the load to the water body (mload) are calculated.
            'For plumes that reach the water body, the summed contributions of those plumes to m0, mdn and mload
            'corresponding to that water body ID are contained in the "result" query.  The "result2" query
            'contains the contributions to m0 and mdn of plumes that don't reach specified water body.  These 
            'contributions are then added to the contributions from plumes that do reach the water body.
            'Note that for plumes that don't reach the water body the contribution to mdn is equal to m0 
            'because all of the plume is denitrified.
            Dim m0, mdn, mload, mni As Single
            Dim i As Integer = 0
            Dim ii As Integer = 0
            Dim highlight As String
            Dim outstring As String = "{\rtf1\ansi\ansicpg1252\deff0\deflang1033\deflangfe1033{\fonttbl{\f0\fmodern\fprq1\fcharset0 Lucida Console;}}\viewkind4\uc1\pard\tx630\tx2340\tx4230\tx5940\f0\fs20 "
            Dim outstringtxt As String = ""
            Dim str As String




            For Each result In query
                m0 = result.sumM0 + result.SumMni
                mdn = result.sumMdn
                mload = result.sumMLoad * m_riskfac
                mni = result.SumMni

                For Each result2 In query2
                    If result.allWbFID = result2.DestinationWaterbodyIDPath Then
                        'm0 = m0 + result2.sumM0 + result2.sumM0_NH4
                        'mdn = mdn + result2.sumM0 + result2.sumM0_NH4
                        'mni = mni + result2.sumM0_NH4
                        m0 = m0 + result2.sumM0 + result2.SumMni
                        mdn = mdn + result2.sumM0 + result2.SumMni
                        mni = mni + result2.SumMni
                    End If
                Next

                If i Mod 2 = 0 Then
                    highlight = "\highlight0 "
                Else
                    highlight = "\highlight1 "
                End If
                i = i + 1
                outstring = outstring & highlight & result.allWbFID.ToString("0000") & "\tab " & _
                                                    result.sumMLoad.ToString("N") & "\tab " & _
                                            "\b " & mload.ToString("N") & "\b0\tab " & _
                                                    mdn.ToString("N") & "\tab " & _
                                                    m0.ToString("N") & "\par"
                str = result.allWbFID & "  " & result.sumMLoad & vbTab & mload & vbTab & mdn & vbTab & m0
                If outstringtxt = "" Then
                    outstringtxt = str
                Else
                    outstringtxt = outstringtxt & vbCrLf & str
                End If

                If echoToTrace Then Trace.WriteLine(str)
            Next
            outstring = outstring & "}"

            m_outputintermediate_outputs.Add("rtf", outstring)
            m_outputintermediate_outputs.Add("txt", outstringtxt)

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            ret = False
        End Try

        GC.Collect()
        GC.WaitForPendingFinalizers()


        Trace.WriteLine("Calculating total load on waterbodies...Done")
        Trace.Unindent()
        Return ret
    End Function



#Region "Helpers"

    Public Sub cancelDenitrification()
        Trace.WriteLine("Cancelling denitrification calculation...")
        m_cancelDenitrification = True
    End Sub

    Private Function init() As Boolean
        'the first thing to do is initialize the plumes info table. saves from having
        'to read each time (i.e. setting up ArcObjects to read from the actual shape file
        Try
            Dim COM As New ComReleaser
            Dim q As String                                         'query to select the sources
            Dim fcur As IFeatureCursor                              'for iterating through the sources
            Dim source As IFeature

            '**********************************************************************


            'get the specified sources only
            q = ""
            If m_sourceID <> -1 Then
                q = """PathID"" = " & m_sourceID
            End If
            fcur = Utilities.getCursor(m_plumesInfo, q)
            COM.ManageLifetime(fcur)
            If fcur Is Nothing Then Throw New Exception("Plumes info feature cursor is nothing")

            'get the index of the fields
            Dim idx_pathID As Integer = fcur.Fields.FindField("PathID")
            If idx_pathID <= 0 Then Throw New Exception("Couldn't find field 'PathID'")
            Dim idx_is2D As Integer = fcur.Fields.FindField("is2D")
            If idx_is2D <= 0 Then Throw New Exception("Couldn't find field 'is2D'")
            Dim idx_decayCoeff As Integer = fcur.Fields.FindField("decayCoeff")
            If idx_decayCoeff <= 0 Then Throw New Exception("Couldn't find field 'decayCoeff'")
            Dim idx_avgVel As Integer = fcur.Fields.FindField("avgVel")
            If idx_avgVel <= 0 Then Throw New Exception("Couldn't find field 'idx_avgVel'")
            Dim idx_ax As Integer = fcur.Fields.FindField("dispL")
            If idx_ax <= 0 Then Throw New Exception("Couldn't find field 'dispL'")
            Dim idx_ay As Integer = fcur.Fields.FindField("dispTH")
            If idx_ay <= 0 Then Throw New Exception("Couldn't find field 'dispTH'")
            Dim idx_az As Integer = fcur.Fields.FindField("dispTV")
            If idx_az <= 0 Then Throw New Exception("Couldn't find field 'dispTV'")
            Dim idx_plumelength As Integer = fcur.Fields.FindField("plumeLen")
            If idx_plumelength <= 0 Then Throw New Exception("Couldn't find field 'plumeLen'")
            Dim idx_pathlength As Integer = fcur.Fields.FindField("pathLen")
            If idx_pathlength <= 0 Then Throw New Exception("Couldn't find field 'pathLen'")
            Dim idx_plumetime As Integer = fcur.Fields.FindField("plumeTime")
            If idx_plumetime <= 0 Then Throw New Exception("Couldn't find field 'plumeTime'")
            Dim idx_pathtime As Integer = fcur.Fields.FindField("pathTime")
            If idx_pathtime <= 0 Then Throw New Exception("Couldn't find field 'pathTime'")
            Dim idx_volume As Integer = fcur.Fields.FindField("plumeVol")
            If idx_volume <= 0 Then Throw New Exception("Couldn't find field 'plumeVol'")
            Dim idx_srcAngle As Integer = fcur.Fields.FindField("srcAngle")
            If idx_srcAngle <= 0 Then Throw New Exception("Couldn't find field 'srcAngle'")
            Dim idx_srcConc As Integer = fcur.Fields.FindField("N0_Conc")
            If idx_srcConc <= 0 Then Throw New Exception("Couldn't find field 'N0_Conc'")
            Dim idx_threshConc As Integer = fcur.Fields.FindField("threshConc")
            If idx_threshConc <= 0 Then Throw New Exception("Couldn't find field 'threshConc'")
            Dim idx_wbid_plume As Integer = fcur.Fields.FindField("wbID_plume")
            If idx_wbid_plume <= 0 Then Throw New Exception("Couldn't find field 'wbID_plume'")
            Dim idx_wbid_path As Integer = fcur.Fields.FindField("wbID_path")
            If idx_wbid_path <= 0 Then Throw New Exception("Couldn't find field 'wbID_path'")
            Dim idx_sourceY As Integer = fcur.Fields.FindField("SourceY")
            If idx_sourceY <= 0 Then Throw New Exception("Couldn't find field 'SourceY'")
            Dim idx_sourceZ As Integer = fcur.Fields.FindField("SourceZ")
            If idx_sourceZ <= 0 Then Throw New Exception("Couldn't find field 'SourceZ'")
            Dim idx_MeshDx As Integer = fcur.Fields.FindField("MeshDX")
            If idx_MeshDx <= 0 Then Throw New Exception("Couldn't find field 'MeshDX'")
            Dim idx_MeshDy As Integer = fcur.Fields.FindField("MeshDY")
            If idx_MeshDy <= 0 Then Throw New Exception("Couldn't find field 'MeshDY'")
            Dim idx_MeshDz As Integer = fcur.Fields.FindField("MeshDZ")
            If idx_MeshDz <= 0 Then Throw New Exception("Couldn't find field 'MeshDZ'")
            Dim idx_avgporosity As Integer = fcur.Fields.FindField("avgPrsity")
            If idx_avgporosity <= 0 Then Throw New Exception("Couldn't find field 'avgPrsity'")
            Dim idx_concNext As Integer = fcur.Fields.FindField("nextConc")
            If idx_concNext <= 0 Then Throw New Exception("Couldn't find field 'nextConc'")
            Dim idx_massInRate As Integer = fcur.Fields.FindField("massInRate")
            If idx_massInRate <= 0 Then Throw New Exception("Couldn't find field 'massInRate'")
            Dim idx_massDNRate As Integer = fcur.Fields.FindField("massDNRate")
            If idx_massDNRate <= 0 Then Throw New Exception("Couldn't find field 'massDNRate'")
            Dim idx_volFac As Integer = fcur.Fields.FindField("volFac")
            If idx_volFac <= 0 Then Throw New Exception("Couldn't find field 'volFac'")


            'get the first feature
            source = fcur.NextFeature

            If source Is Nothing Then Throw New Exception("No sources to calculate in the given feature class")

            m_plumesInfoTable = New List(Of PlumeInfo)

            'loop through all the features
            Dim info As PlumeInfo
            While Not source Is Nothing
                info = New PlumeInfo
                info.AvgVelocity = source.Value(idx_avgVel)
                info.ConcInit = source.Value(idx_srcConc)
                info.DecayCoeff = source.Value(idx_decayCoeff)
                info.DestinationWaterbodyIDPath = source.Value(idx_wbid_path)
                info.DestinationWaterbodyIDPlume = source.Value(idx_wbid_plume)
                info.DirectionAngle = source.Value(idx_srcAngle)
                info.DispersivityLongitudinal = source.Value(idx_ax)
                info.DispersivityTransverseHorizontal = source.Value(idx_ay)
                info.is2D = source.Value(idx_is2D)
                info.MeshDx = source.Value(idx_MeshDx)
                info.MeshDy = source.Value(idx_MeshDy)
                info.MeshDz = source.Value(idx_MeshDz)
                info.PathID = source.Value(idx_pathID)
                info.PathLength = source.Value(idx_pathlength)
                info.PathTime = source.Value(idx_pathtime)
                info.PlumeLength = source.Value(idx_plumelength)
                info.PlumeTime = source.Value(idx_plumetime)
                info.Porosity = source.Value(idx_avgporosity)
                info.Y = source.Value(idx_sourceY)
                info.Z = source.Value(idx_sourceZ)
                info.ConcNext = source.Value(idx_concNext)
                info.MassInputRate = source.Value(idx_massInRate)
                info.MassDenitrificationOutRate_IsolatedPlume = source.Value(idx_massDNRate)
                info.SourceLocation = CType(source.ShapeCopy, IPoint)
                info.VolumeConversionFactor = source.Value(idx_volFac)

                m_plumesInfoTable.Add(info)

                source = fcur.NextFeature
            End While

            'do some checking on the data
            If m_plumesInfoTable.Count = 0 Then Throw New Exception("No plumes found. Nothing to do")

            'check to make sure there are only 2d plumes
            Dim query1 = From plume In m_plumesInfoTable _
                         Where plume.is2D <> 1
            If query1.Count <> 0 Then Throw New Exception("All plumes must be 2D. This module does not support 3D plumes")

            'check to make sure there are only steady state plumes
            Dim query2 = From plume In m_plumesInfoTable _
                         Where plume.PlumeTime <> -1.0
            If query2.Count <> 0 Then Throw New Exception("All plumes must be steady state. ")

            'check to make sure all plumes have the same cell size
            Dim query3 = From plume In m_plumesInfoTable _
                         Select plume.MeshDx, plume.MeshDy _
                         Distinct
            If query3.Count <> 1 Then Throw New Exception("All plumes must have the same mesh size dimesions!")

            'check to make sure that the DeltaZ value is the same as the Z value.
            'This must be true for 2D solutions only (checked above)
            'Noted by Yan: Since we set a considered reasonable meshz as lower than 2.0 m in the model,
            'then any calculated meshDZ larger than 2.0 will be set as 2.0. So plume.meshDz is NOT ALWAYS
            'equal to plume.z. That is why we cancel the check of query4.
            Dim query4 = From plume In m_plumesInfoTable _
                         Where plume.MeshDz <> plume.Z
            'If query4.Count <> 0 Then Throw New Exception("All plumes must have MeshDZ=Z")

            Dim query8 = From plume In m_plumesInfoTable _
                                     Select plume.VolumeConversionFactor _
                                     Distinct
            If query8.Count <> 1 Then Throw New Exception("All plumes must have the same volume conversion factor.")

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            Return False
        End Try

        Return True
    End Function

    'Update the infor of NO3.
    Private Function UpdateInfor_NO3() As Boolean
        'the first thing to do is initialize the plumes info table. saves from having
        'to read each time (i.e. setting up ArcObjects to read from the actual shape file
        Try
            Dim COM As New ComReleaser
            Dim q As String                                         'query to select the sources
            Dim fcur, fcur_NH4 As IFeatureCursor                              'for iterating through the sources
            Dim source As IFeature
            Dim source_NH4 As IFeature
            Dim m_domBdy As Integer




            '**********************************************************************


            'get the specified sources only
            q = ""
            If m_sourceID <> -1 Then
                q = """PathID"" = " & m_sourceID
            End If
            fcur = Utilities.getCursor(m_plumesInfo, q)
            fcur_NH4 = Utilities.getCursor(m_plumesInfo_NH4, q)
            COM.ManageLifetime(fcur)
            COM.ManageLifetime(fcur_NH4)
            If fcur Is Nothing Then Throw New Exception("Plumes info feature cursor is nothing")
            If fcur_NH4 Is Nothing Then Throw New Exception("Plumes info_NH4 feature cursor is nothing")

            'get the index of the fields OF NO3

            Dim idx_pathID As Integer = fcur.Fields.FindField("PathID")
            If idx_pathID <= 0 Then Throw New Exception("Couldn't find field 'PathID'")
            Dim idx_is2D As Integer = fcur.Fields.FindField("is2D")
            If idx_is2D <= 0 Then Throw New Exception("Couldn't find field 'is2D'")
            Dim idx_decayCoeff As Integer = fcur.Fields.FindField("decayCoeff")
            If idx_decayCoeff <= 0 Then Throw New Exception("Couldn't find field 'decayCoeff'")
            Dim idx_avgVel As Integer = fcur.Fields.FindField("avgVel")
            If idx_avgVel <= 0 Then Throw New Exception("Couldn't find field 'idx_avgVel'")
            Dim idx_ax As Integer = fcur.Fields.FindField("dispL")
            If idx_ax <= 0 Then Throw New Exception("Couldn't find field 'dispL'")
            Dim idx_ay As Integer = fcur.Fields.FindField("dispTH")
            If idx_ay <= 0 Then Throw New Exception("Couldn't find field 'dispTH'")
            Dim idx_az As Integer = fcur.Fields.FindField("dispTV")
            If idx_az <= 0 Then Throw New Exception("Couldn't find field 'dispTV'")
            Dim idx_plumelength As Integer = fcur.Fields.FindField("plumeLen")
            If idx_plumelength <= 0 Then Throw New Exception("Couldn't find field 'plumeLen'")
            Dim idx_pathlength As Integer = fcur.Fields.FindField("pathLen")
            If idx_pathlength <= 0 Then Throw New Exception("Couldn't find field 'pathLen'")
            Dim idx_plumetime As Integer = fcur.Fields.FindField("plumeTime")
            If idx_plumetime <= 0 Then Throw New Exception("Couldn't find field 'plumeTime'")
            Dim idx_pathtime As Integer = fcur.Fields.FindField("pathTime")
            If idx_pathtime <= 0 Then Throw New Exception("Couldn't find field 'pathTime'")
            Dim idx_volume As Integer = fcur.Fields.FindField("plumeVol")
            If idx_volume <= 0 Then Throw New Exception("Couldn't find field 'plumeVol'")
            Dim idx_srcAngle As Integer = fcur.Fields.FindField("srcAngle")
            If idx_srcAngle <= 0 Then Throw New Exception("Couldn't find field 'srcAngle'")
            Dim idx_srcConc As Integer = fcur.Fields.FindField("N0_Conc")
            If idx_srcConc <= 0 Then Throw New Exception("Couldn't find field 'N0_Conc'")
            Dim idx_threshConc As Integer = fcur.Fields.FindField("threshConc")
            If idx_threshConc <= 0 Then Throw New Exception("Couldn't find field 'threshConc'")
            Dim idx_wbid_plume As Integer = fcur.Fields.FindField("wbID_plume")
            If idx_wbid_plume <= 0 Then Throw New Exception("Couldn't find field 'wbID_plume'")
            Dim idx_wbid_path As Integer = fcur.Fields.FindField("wbID_path")
            If idx_wbid_path <= 0 Then Throw New Exception("Couldn't find field 'wbID_path'")
            Dim idx_sourceY As Integer = fcur.Fields.FindField("SourceY")
            If idx_sourceY <= 0 Then Throw New Exception("Couldn't find field 'SourceY'")
            Dim idx_sourceZ As Integer = fcur.Fields.FindField("SourceZ")
            If idx_sourceZ <= 0 Then Throw New Exception("Couldn't find field 'SourceZ'")
            Dim idx_MeshDx As Integer = fcur.Fields.FindField("MeshDX")
            If idx_MeshDx <= 0 Then Throw New Exception("Couldn't find field 'MeshDX'")
            Dim idx_MeshDy As Integer = fcur.Fields.FindField("MeshDY")
            If idx_MeshDy <= 0 Then Throw New Exception("Couldn't find field 'MeshDY'")
            Dim idx_MeshDz As Integer = fcur.Fields.FindField("MeshDZ")
            If idx_MeshDz <= 0 Then Throw New Exception("Couldn't find field 'MeshDZ'")
            Dim idx_avgporosity As Integer = fcur.Fields.FindField("avgPrsity")
            If idx_avgporosity <= 0 Then Throw New Exception("Couldn't find field 'avgPrsity'")
            Dim idx_concNext As Integer = fcur.Fields.FindField("nextConc")
            If idx_concNext <= 0 Then Throw New Exception("Couldn't find field 'nextConc'")
            Dim idx_massInRate As Integer = fcur.Fields.FindField("massInRate")
            If idx_massInRate <= 0 Then Throw New Exception("Couldn't find field 'massInRate'")
            Dim idx_massDNRate As Integer = fcur.Fields.FindField("massDNRate")
            If idx_massDNRate <= 0 Then Throw New Exception("Couldn't find field 'massDNRate'")
            Dim idx_volFac As Integer = fcur.Fields.FindField("volFac")
            If idx_volFac <= 0 Then Throw New Exception("Couldn't find field 'volFac'")
            Dim idx_domBdy As Integer = fcur.Fields.FindField("domBdy")
            If idx_domBdy <= 0 Then Throw New Exception("Couldn't find field 'domBdy'")

            '  get the index of the fields OF NH4          
            Dim idx_pathID_NH4 As Integer = fcur_NH4.Fields.FindField("PathID")
            If idx_pathID_NH4 <= 0 Then Throw New Exception("Couldn't find field 'PathID'")
            Dim idx_is2D_NH4 As Integer = fcur_NH4.Fields.FindField("is2D")
            If idx_is2D_NH4 <= 0 Then Throw New Exception("Couldn't find field 'is2D'")
            Dim idx_decayCoeff_NH4 As Integer = fcur_NH4.Fields.FindField("decayCoeff")
            If idx_decayCoeff_NH4 <= 0 Then Throw New Exception("Couldn't find field 'decayCoeff'")
            Dim idx_avgVel_NH4 As Integer = fcur_NH4.Fields.FindField("avgVel")
            If idx_avgVel_NH4 <= 0 Then Throw New Exception("Couldn't find field 'idx_avgVel'")
            Dim idx_ax_NH4 As Integer = fcur_NH4.Fields.FindField("dispL")
            If idx_ax_NH4 <= 0 Then Throw New Exception("Couldn't find field 'dispL'")
            Dim idx_ay_NH4 As Integer = fcur_NH4.Fields.FindField("dispTH")
            If idx_ay_NH4 <= 0 Then Throw New Exception("Couldn't find field 'dispTH'")
            Dim idx_az_NH4 As Integer = fcur_NH4.Fields.FindField("dispTV")
            If idx_az_NH4 <= 0 Then Throw New Exception("Couldn't find field 'dispTV'")
            Dim idx_plumelength_NH4 As Integer = fcur_NH4.Fields.FindField("plumeLen")
            If idx_plumelength_NH4 <= 0 Then Throw New Exception("Couldn't find field 'plumeLen'")
            Dim idx_pathlength_NH4 As Integer = fcur_NH4.Fields.FindField("pathLen")
            If idx_pathlength_NH4 <= 0 Then Throw New Exception("Couldn't find field 'pathLen'")
            Dim idx_plumetime_NH4 As Integer = fcur_NH4.Fields.FindField("plumeTime")
            If idx_plumetime_NH4 <= 0 Then Throw New Exception("Couldn't find field 'plumeTime'")
            Dim idx_pathtime_NH4 As Integer = fcur_NH4.Fields.FindField("pathTime")
            If idx_pathtime_NH4 <= 0 Then Throw New Exception("Couldn't find field 'pathTime'")
            Dim idx_volume_NH4 As Integer = fcur_NH4.Fields.FindField("plumeVol")
            If idx_volume_NH4 <= 0 Then Throw New Exception("Couldn't find field 'plumeVol'")
            Dim idx_srcAngle_NH4 As Integer = fcur_NH4.Fields.FindField("srcAngle")
            If idx_srcAngle_NH4 <= 0 Then Throw New Exception("Couldn't find field 'srcAngle'")
            Dim idx_srcConc_NH4 As Integer = fcur_NH4.Fields.FindField("N0_Conc")
            If idx_srcConc_NH4 <= 0 Then Throw New Exception("Couldn't find field 'N0_Conc'")
            Dim idx_threshConc_NH4 As Integer = fcur_NH4.Fields.FindField("threshConc")
            If idx_threshConc_NH4 <= 0 Then Throw New Exception("Couldn't find field 'threshConc'")
            Dim idx_wbid_plume_NH4 As Integer = fcur_NH4.Fields.FindField("wbID_plume")
            If idx_wbid_plume_NH4 <= 0 Then Throw New Exception("Couldn't find field 'wbID_plume'")
            Dim idx_wbid_path_NH4 As Integer = fcur_NH4.Fields.FindField("wbID_path")
            If idx_wbid_path_NH4 <= 0 Then Throw New Exception("Couldn't find field 'wbID_path'")
            Dim idx_sourceY_NH4 As Integer = fcur_NH4.Fields.FindField("SourceY")
            If idx_sourceY_NH4 <= 0 Then Throw New Exception("Couldn't find field 'SourceY'")
            Dim idx_sourceZ_NH4 As Integer = fcur_NH4.Fields.FindField("SourceZ")
            If idx_sourceZ_NH4 <= 0 Then Throw New Exception("Couldn't find field 'SourceZ'")
            Dim idx_MeshDx_NH4 As Integer = fcur_NH4.Fields.FindField("MeshDX")
            If idx_MeshDx_NH4 <= 0 Then Throw New Exception("Couldn't find field 'MeshDX'")
            Dim idx_MeshDy_NH4 As Integer = fcur_NH4.Fields.FindField("MeshDY")
            If idx_MeshDy_NH4 <= 0 Then Throw New Exception("Couldn't find field 'MeshDY'")
            Dim idx_MeshDz_NH4 As Integer = fcur_NH4.Fields.FindField("MeshDZ")
            If idx_MeshDz_NH4 <= 0 Then Throw New Exception("Couldn't find field 'MeshDZ'")
            Dim idx_avgporosity_NH4 As Integer = fcur_NH4.Fields.FindField("avgPrsity")
            If idx_avgporosity_NH4 <= 0 Then Throw New Exception("Couldn't find field 'avgPrsity'")
            Dim idx_concNext_NH4 As Integer = fcur_NH4.Fields.FindField("nextConc")
            If idx_concNext_NH4 <= 0 Then Throw New Exception("Couldn't find field 'nextConc'")
            Dim idx_massInRate_NH4 As Integer = fcur_NH4.Fields.FindField("massInRate")
            If idx_massInRate_NH4 <= 0 Then Throw New Exception("Couldn't find field 'massInRate'")
            Dim idx_massDNRate_NH4 As Integer = fcur_NH4.Fields.FindField("massDNRate")
            If idx_massDNRate_NH4 <= 0 Then Throw New Exception("Couldn't find field 'massDNRate'")
            Dim idx_volFac_NH4 As Integer = fcur_NH4.Fields.FindField("volFac")
            If idx_volFac_NH4 <= 0 Then Throw New Exception("Couldn't find field 'volFac'")



            'get the first feature
            source = fcur.NextFeature
            source_NH4 = fcur_NH4.NextFeature

            If source Is Nothing Then Throw New Exception("No sources to calculate in the given feature class")
            If source_NH4 Is Nothing Then Throw New Exception("No sources to calculate in the given feature class")

            m_plumesInfoTable = New List(Of PlumeInfo)
            m_plumesInfoTable_NH4 = New List(Of PlumeInfo)

            'loop through all the features
            Dim info As PlumeInfo
            Dim K1, K2, M0_NH4, M0_A2, Mdeni_A2, Mnitri_NH4, M0_NO3, Mdenitr_NO3 As Single

            While Not source Is Nothing And Not source_NH4 Is Nothing
                info = New PlumeInfo
                info.AvgVelocity = source.Value(idx_avgVel)
                info.ConcInit = source.Value(idx_srcConc)
                info.DecayCoeff = source.Value(idx_decayCoeff)

                K1 = source_NH4.Value(idx_decayCoeff_NH4)
                K2 = source.Value(idx_decayCoeff)

                info.DestinationWaterbodyIDPath = source.Value(idx_wbid_path)
                info.DestinationWaterbodyIDPlume = source.Value(idx_wbid_plume)
                info.DirectionAngle = source.Value(idx_srcAngle)
                info.DispersivityLongitudinal = source.Value(idx_ax)
                info.DispersivityTransverseHorizontal = source.Value(idx_ay)
                info.is2D = source.Value(idx_is2D)
                info.MeshDx = source.Value(idx_MeshDx)
                info.MeshDy = source.Value(idx_MeshDy)
                info.MeshDz = source.Value(idx_MeshDz)
                info.PathID = source.Value(idx_pathID)


                info.PathLength = source.Value(idx_pathlength)
                info.PathTime = source.Value(idx_pathtime)
                info.PlumeLength = source.Value(idx_plumelength)
                info.PlumeTime = source.Value(idx_plumetime)
                info.Porosity = source.Value(idx_avgporosity)
                info.Y = source.Value(idx_sourceY)
                info.Z = source.Value(idx_sourceZ)

                'Yan: It is assumed that Z1=Z2.
                'Z1 = source_NH4.Value(idx_sourceZ_NH4)
                'Z2 = source.Value(idx_sourceZ)

                info.ConcNext = source.Value(idx_concNext)
                info.MassInputRate = source.Value(idx_massInRate)

                info.MassInputRate_NH4 = source_NH4.Value(idx_massInRate_NH4)
                M0_A2 = source.Value(idx_massInRate)
                info.MassDenitrificationOutRate_IsolatedPlume = source.Value(idx_massDNRate)
                Mdeni_A2 = source.Value(idx_massDNRate)



                If info.DestinationWaterbodyIDPlume_NH4 <> -1 Then
                    info.Mass_Nitrification = source_NH4.Value(idx_massDNRate_NH4)
                Else
                    info.Mass_Nitrification = source_NH4.Value(idx_massInRate_NH4)
                End If


                Mnitri_NH4 = source_NH4.Value(idx_massDNRate_NH4)
                M0_NH4 = source_NH4.Value(idx_massInRate_NH4)
                m_domBdy = source.Value(idx_domBdy)
                If m_domBdy = 1 Then
                    M0_NO3 = M0_A2
                End If
                If m_domBdy = 2 Then
                    M0_NO3 = M0_A2 - K1 * M0_NH4 / (K1 - K2)
                    If M0_NO3 < 0.0001 Then
                        M0_NO3 = M0_A2
                    End If
                End If

                Mdenitr_NO3 = Mdeni_A2 - K2 * Mnitri_NH4 / (K1 - K2)
                'note by yan: because of the uncontrollable of Z, there is chance for  Mdenitr_NO3 to be negative.
                If Mdenitr_NO3 < 0.0001 Or Mdenitr_NO3 > M0_NO3 Then
                    Mdenitr_NO3 = Math.Min(Mdeni_A2, M0_NO3)
                End If


                source.Value(idx_massDNRate) = Mdenitr_NO3
                source.Value(idx_massInRate) = M0_NO3
                info.MassDenitrificationOutRate_IsolatedPlume = source.Value(idx_massDNRate)
                info.PathID = source.Value(idx_pathID) 'whether the NH4 plume reach the water body or not!

                info.DestinationWaterbodyIDPlume_NH4 = source_NH4.Value(idx_wbid_plume_NH4)
                info.DestinationWaterbodyIDPath_NH4 = source_NH4.Value(idx_wbid_path_NH4)

                info.SourceLocation = CType(source.ShapeCopy, IPoint)
                info.VolumeConversionFactor = source.Value(idx_volFac)
                m_plumesInfoTable.Add(info)
                source = fcur.NextFeature
                source_NH4 = fcur_NH4.NextFeature
            End While




            'do some checking on the data
            If m_plumesInfoTable.Count = 0 Then Throw New Exception("No plumes found. Nothing to do")

            'check to make sure there are only 2d plumes
            Dim query1 = From plume In m_plumesInfoTable _
                         Where plume.is2D <> 1
            If query1.Count <> 0 Then Throw New Exception("All plumes must be 2D. This module does not support 3D plumes")

            'check to make sure there are only steady state plumes
            Dim query2 = From plume In m_plumesInfoTable _
                         Where plume.PlumeTime <> -1.0
            If query2.Count <> 0 Then Throw New Exception("All plumes must be steady state. ")

            'check to make sure all plumes have the same cell size
            Dim query3 = From plume In m_plumesInfoTable _
                         Select plume.MeshDx, plume.MeshDy _
                         Distinct
            If query3.Count <> 1 Then Throw New Exception("All plumes must have the same mesh size dimesions!")

            'check to make sure that the DeltaZ value is the same as the Z value.
            'This must be true for 2D solutions only (checked above)
            'Noted by Yan: Since we set a considered reasonable meshz as lower than 2.0 m in the model,
            'then any calculated meshDZ larger than 2.0 will be set as 2.0. So plume.meshDz is NOT ALWAYS
            'equal to plume.z. That is why we cancel the check of query4.
            Dim query4 = From plume In m_plumesInfoTable _
                         Where plume.MeshDz <> plume.Z
            'If query4.Count <> 0 Then Throw New Exception("All plumes must have MeshDZ=Z")

            Dim query8 = From plume In m_plumesInfoTable _
                                     Select plume.VolumeConversionFactor _
                                     Distinct
            If query8.Count <> 1 Then Throw New Exception("All plumes must have the same volume conversion factor.")

        Catch ex As Exception
            Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": " & ex.ToString)
            Return False
        End Try

        Return True
    End Function







#End Region

    ''' <summary>
    ''' Interal class that represents the associated plume info
    ''' </summary>
    ''' <remarks></remarks>
    Private Class PlumeInfo
        Public PathID As Integer
        Public is2D As SByte
        Public DecayCoeff As Single
        Public PlumeLength As Single
        Public PlumeTime As Single
        Public AvgVelocity As Single
        Public DispersivityLongitudinal As Single
        Public DispersivityTransverseHorizontal As Single
        Public PathLength As Single
        Public PathTime As Single
        Public DirectionAngle As Single
        Public ConcInit As Single
        Public ConcNext As Single
        Public DestinationWaterbodyIDPlume As Integer
        Public DestinationWaterbodyIDPath As Integer
        Public Y As Single        
        Public Z As Single
        Public MeshDz As Single
        Public MeshDy As Single
        Public MeshDx As Single
        Public Porosity As Single
        Public SourceLocation As Point
        Public MassInputRate As Single
        Public MassDenitrificationOutRate_IsolatedPlume As Single
        Public VolumeConversionFactor As Single
        Public Mass_Nitrification As Single
        Public MassInputRate_NH4 As Single
        Public DestinationWaterbodyIDPlume_NH4 As Integer
        Public DestinationWaterbodyIDPath_NH4 As Integer
    End Class
End Class

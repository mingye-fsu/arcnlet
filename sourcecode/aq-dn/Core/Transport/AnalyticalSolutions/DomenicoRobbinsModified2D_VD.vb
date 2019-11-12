''' <summary>
''' Implements a 2D version of the Modified Domenico Robbins solution in the form given by eq (13)
''' in Srinivasan et al. (2007) along with a variable longitudinal (and transverse) dispersivities
''' based on the distance to the reference point along the plume.  Two methods are implemented (set via the
''' constructor): The method of Pickens and Grisak (1981) and Xu and Eckstein (1995)
''' </summary>
''' <remarks>Dy is set to 0.1Dx</remarks>
Friend Class DomenicoRobbinsModified2D_VD
    Implements IAnalyticalFunction4D

    Public Enum DispersivityMethod As SByte
        PickensGrisak = 0
        XuEckstein = 1
    End Enum

    Protected m_S As Single
    Protected m_concInit As Single
    Protected m_Y_over_2 As Single
    Protected m_vt As Single

    'dispersivities (not dispersion coefficient)
    Protected m_w As Single
    Protected m_Dy_Dx As Single
    Protected m_dmethod As DispersivityMethod

    Protected m_t As Single

    Public ReadOnly Property t() As Single Implements IAnalyticalFunction4D.t
        Get
            Return m_t
        End Get
    End Property

    Public Overridable Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single Implements IAnalyticalFunction4D.eval
        Dim Dx As Double
        Dim Dy As Double
        Dim correction As Double            'the correction term for the DR solution
        Dim correction_exp As Double
        Dim correction_erfc As Double



        'effectively restricts the solution to the plane x=0. Eliminates uncessary function evaluations
        If z <> 0 Then Return 0

        'enforce the bdy condition. necessary here b/c of the way dispersivity is calculated
        If x <= 0 And y <= m_Y_over_2 Then
            Return m_concInit
        End If


        Dx = 0.1 * x
        If m_dmethod = DispersivityMethod.XuEckstein AndAlso x > 1 Then
            Dx = 0.83 * Math.Pow(Math.Log10(x), 2.414)
        End If


        Dy = m_Dy_Dx * Dx
        Dim den_x As Double = 2 * Math.Sqrt(Dx * m_vt)
        Dim den_y As Double = 2 * Math.Sqrt(Dy * x)

        Dim erfy_p1 As Double = (y + m_Y_over_2) / den_y
        Dim erfy_p2 As Double = (y - m_Y_over_2) / den_y

        'handle the case when we get an undefined number (can only happen with erfy_p2 because of the subtraction)        
        If Double.IsNaN(erfy_p2) Then
            erfy_p2 = Double.NegativeInfinity
        End If

        correction_exp = Math.Exp(x / Dx)
        correction_erfc = MathSpecial.erfc((x + m_vt) / den_x)
        If correction_erfc <= 0 Then
            correction = 0
        Else
            correction = correction_exp * correction_erfc
        End If

        Return m_S * _
                   (MathSpecial.erfc2((x - m_vt) / den_x) + correction) * _
                   (MathSpecial.erf2(erfy_p1) - MathSpecial.erf2(erfy_p2))
    End Function

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="concInit">Concentration at source</param>
    ''' <param name="w">Longitudinal Dispersivity scaling factor</param>
    ''' <param name="Dy_Dx">Ratio of tranverse horizontal dispersivity to longitudinal, Dy/Dx</param>
    ''' <param name="Y">The Y dimension of the source plane</param>
    ''' <param name="v">The value of advection velocity (seepage velocity)</param>
    ''' <param name="t">Time at which the plume is evaluated</param>
    ''' <param name="method">The method to use for dispersivity calculation. Use the DispersivityMethod enum to select</param>
    ''' <remarks>All units must be consistent.  I.e. if the velocity is given in m/day, t should also be 
    ''' in days and source dimensions in meters.</remarks>
    Public Sub New(ByVal concInit As Single, ByVal w As Single, ByVal Dy_Dx As Single, ByVal Y As Single, ByVal v As Single, ByVal t As Single, ByVal method As DispersivityMethod)
        m_w = w
        m_Dy_Dx = Dy_Dx
        m_dmethod = method
        m_t = t

        '***pre-compute terms in the DR solution***
        m_concInit = concInit
        m_S = concInit / 4

        m_vt = v * t
        m_Y_over_2 = Y / 2
    End Sub


End Class




''' <summary>
''' Implements the Domenico Robbins solution in the form given by eq (3) in Gutierrez-Neri et. al. (2009)
''' Spreading is only in the -'ve z direction as given by Fig 3a in Domenico and Robbins 1985
''' </summary>
''' <remarks></remarks>
Friend Class DomenicoRobbins
    Implements IAnalyticalFunction4D

    Protected m_S As Single
    Protected m_xden As Double
    Protected m_Y_over_2 As Single
    Protected m_Z_over_2 As Single
    Protected m_vt As Single

    'dispersivities (not dispersion coefficient)
    Protected m_Dx As Single
    Protected m_Dy As Single
    Protected m_Dz As Single


    Protected m_t As Single

    Public ReadOnly Property t() As Single Implements IAnalyticalFunction4D.t
        Get
            Return m_t
        End Get
    End Property

    Public Overridable Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single Implements IAnalyticalFunction4D.eval
        Dim den_y As Double = 2 * Math.Sqrt(m_Dy * x)
        Dim den_z As Double = 2 * Math.Sqrt(m_Dz * x)
        Dim erfy_p1 As Double = (y + m_Y_over_2) / den_y
        Dim erfy_p2 As Double = (y - m_Y_over_2) / den_y
        Dim erfz_p1 As Double = (z + m_Z_over_2) / den_z
        Dim erfz_p2 As Double = (z - m_Z_over_2) / den_z

        'handle the case when we get an undefined number (can only happen with erfy_p2 because of the subtraction)        
        'for erfz, could happen for either case, depending on whether we traverse the mesh in the +z or -z directin
        'assuming -z direction"
        If Double.IsNaN(erfy_p2) Then
            erfy_p2 = Double.NegativeInfinity
        End If
        If Double.IsNaN(erfz_p1) Then
            erfz_p1 = Double.PositiveInfinity
        End If
        If Double.IsNaN(erfz_p2) Then
            erfz_p2 = Double.NegativeInfinity
        End If

        Return m_S * MathSpecial.erfc((x - m_vt) / m_xden) _
                   * ( _
                        MathSpecial.erf(erfy_p1) - MathSpecial.erf(erfy_p2) _
                     ) _
                   * ( _
                        MathSpecial.erf(erfz_p1) - MathSpecial.erf(erfz_p2) _
                     )
    End Function

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="concInit">Concentration at source</param>
    ''' <param name="Dx">Dispersivity longitudinal</param>
    ''' <param name="Dy">Dispersivity transverse horizontal</param>
    ''' <param name="Dz">Dispersivity transverse vertical</param>
    ''' <param name="Y">The Y dimension of the source plane</param>
    ''' <param name="Z">The Z dimension of the source plane</param>
    ''' <param name="v">The value of advection velocity to use</param>
    ''' <param name="t">Solution will be given for the specified time</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal concInit As Single, ByVal Dx As Single, ByVal Dy As Single, ByVal Dz As Single, ByVal Y As Single, ByVal Z As Single, ByVal v As Single, ByVal t As Single)
        m_Dx = Dx
        m_Dy = Dy
        m_Dz = Dz
        m_t = t

        '***pre-compute terms in the DR solution***
        m_S = concInit / 8

        m_vt = v * t
        m_xden = 2 * Math.Sqrt(Dx * m_vt)
        m_Y_over_2 = Y / 2
        m_Z_over_2 = Z      'as in Fig 3a of domenico and robbins (1985)
    End Sub
End Class

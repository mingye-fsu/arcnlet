''' <summary>
''' Implements the steady state Domenico Robbins solution in the form given by eq (5)
''' in Gutierrez-Neri et. al. (2009)
''' </summary>
''' <remarks></remarks>
Friend Class DomenicoRobbinsSS
    Inherits DomenicoRobbins


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
    ''' <remarks></remarks>
    Public Sub New(ByVal concInit As Single, ByVal Dx As Single, ByVal Dy As Single, ByVal Dz As Single, ByVal Y As Single, ByVal Z As Single, ByVal v As Single)
        MyBase.New(concInit, Dx, Dy, Dz, Y, Z, v, -1)
    End Sub

    Public Overrides Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single
        Dim den_y As Single = 2 * Math.Sqrt(m_Dy * x)
        Dim den_z As Single = 2 * Math.Sqrt(m_Dz * x)
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

        Return (m_S * 2) * _
               (MathSpecial.erf(erfy_p1) - MathSpecial.erf(erfy_p2)) * _
               (MathSpecial.erf(erfz_p1) - MathSpecial.erf(erfz_p2))

    End Function

End Class

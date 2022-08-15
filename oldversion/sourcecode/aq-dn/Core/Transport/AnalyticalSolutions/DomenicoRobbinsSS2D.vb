''' <summary>
''' Implements the steady state Domenico Robbins solution in the form given by eq (6)
''' in Gutierrez-Neri et. al. (2009)
''' </summary>
''' <remarks></remarks>
Friend Class DomenicoRobbinsSS2D
    Inherits DomenicoRobbins


    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="concInit">Concentration at source</param>
    ''' <param name="Dx">Dispersivity longitudinal</param>
    ''' <param name="Dy">Dispersivity transverse horizontal</param>
    ''' <param name="Y">The Y dimension of the source plane</param>
    ''' <param name="v">The value of advection velocity to use</param>    
    ''' <remarks></remarks>
    Public Sub New(ByVal concInit As Single, ByVal Dx As Single, ByVal Dy As Single, ByVal Y As Single, ByVal v As Single)
        MyBase.New(concInit, Dx, Dy, 0, Y, 0, v, -1)
    End Sub

    Public Overrides Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single
        'effectively restricts the solution to the plane x=0. Eliminates uncessary function evaluations
        If z <> 0 Then Return 0

        Dim den_y As Single = 2 * Math.Sqrt(m_Dy * x)
        Dim erfy_p1 As Double = (y + m_Y_over_2) / den_y
        Dim erfy_p2 As Double = (y - m_Y_over_2) / den_y

        'handle the case when we get an undefined number (can only happen with erfy_p2 because of the subtraction)        
        'for erfz, could happen for either case, depending on whether we traverse the mesh in the +z or -z directin
        'assuming -z direction"
        If Double.IsNaN(erfy_p2) Then
            erfy_p2 = Double.NegativeInfinity
        End If

        Return (m_S * 4) * (MathSpecial.erf(erfy_p1) - MathSpecial.erf(erfy_p2))

    End Function

End Class


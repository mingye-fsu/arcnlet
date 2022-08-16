''' <summary>
''' Implements a 2D version of the Modified Domenico Robbins solution in the form given by eq (13)
''' in Srinivasan et al. (2007)
''' </summary>
''' <remarks></remarks>
Friend Class DomenicoRobbinsModified2D
    Inherits DomenicoRobbins


    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="concInit">Concentration at source</param>
    ''' <param name="Dx">Dispersivity longitudinal</param>
    ''' <param name="Dy">Dispersivity transverse horizontal</param>
    ''' <param name="Y">The Y dimension of the source plane</param>
    ''' <param name="v">The value of advection velocity to use</param>    
    ''' <param name="t">The solution time</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal concInit As Single, ByVal Dx As Single, ByVal Dy As Single, ByVal Y As Single, ByVal v As Single, ByVal t As Single)
        MyBase.New(concInit, Dx, Dy, 0, Y, 0, v, t)
    End Sub

    Public Overrides Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single
        'effectively restricts the solution to the plane x=0. Eliminates uncessary function evaluations
        If z <> 0 Then Return 0

        Dim den_y As Double = 2 * Math.Sqrt(m_Dy * x)
        Dim erfy_p1 As Double = (y + m_Y_over_2) / den_y
        Dim erfy_p2 As Double = (y - m_Y_over_2) / den_y


        Dim correction As Double            'the correction term for the DR solution
        Dim correction_exp As Double
        Dim correction_erfc As Double

        'when the exponetial is so big it doesn't fit in a double, it will be given
        '1.#INF which corresponds to infinity.  If the argumet to the erfc term is too small
        'the value of erfc will become small enough that it will be rounded to zero.  In this case
        'will get a NaN when the two are multiplied (0 x Inf = undefined).  In this case, assign the value zero
        'to the correction term since the exp really isn't mathematically infinity, just numerically
        'infinity.  Note that the value of erfc isnt mathematically zero either but we don't know
        'what it is.  This fix simply allows the calculation to take place
        '
        'Note 2: the above solution works when exp=Inf and erfc=0 occur at the same time. In the transition
        'region, erfc may become zero first, in which case there is no problem.  If exp becomes Inf first
        'there is a problem since the value of the concentration will become Inf.  This will cause the
        'evaluation loop to stop, thereby truncating the plume.  To handle this case, we can set 
        'the correction value to zero like above since in reality, exp is not infinity, just a large number
        'and erfc is a very small number and when they are multiplied, they will give a value close to zero.
        'Example: Using the data from simple_model plume size anlysis2 and the following settings
        '   Dx:=2.113, Dy:=0.234, Dz:=0.234, _
        '   Y:=12, Z:=20, _
        '   MeshCellSize_x:=4, MeshCellSize_y:=4, MeshCellSize_z:=4, _
        '   ThresholdConcentration:=0.005, _
        '   SolutionTime:=9125, _
        '   SolutionType:=AqDn.SolutionTypes.SolutionType.ModifiedDomenico2D
        'The condition will occur at x=1500

        correction_exp = Math.Exp(x / m_Dx)
        correction_erfc = MathSpecial.erfc((x + m_vt) / m_xden)
        If correction_erfc <= 0 OrElse Double.IsPositiveInfinity(correction_exp) Then
            correction = 0
        Else
            correction = correction_exp * correction_erfc
        End If

        'handle the case when we get an undefined number (can only happen with erfy_p2 because of the subtraction)        
        If Double.IsNaN(erfy_p2) Then
            erfy_p2 = Double.NegativeInfinity
        End If

        Return (m_S * 2) * _
                       (MathSpecial.erfc((x - m_vt) / m_xden) + correction) * _
                       (MathSpecial.erf(erfy_p1) - MathSpecial.erf(erfy_p2))
    End Function

End Class
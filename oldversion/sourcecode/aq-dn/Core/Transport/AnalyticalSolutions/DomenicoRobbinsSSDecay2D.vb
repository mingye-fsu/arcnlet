''' <summary>
''' Implements a steady state version of equation (12) in Srinivasan et al (2007)
''' </summary>
''' <remarks></remarks>

Friend Class DomenicoRobbinsSSDecay2D
    Inherits DomenicoRobbinsSS2D

    'saves us from calculating an expensive square root every iteration
    Private m_decay_sqrt As Double

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="concInit">Concentration at source</param>
    ''' <param name="Dx">Dispersivity longitudinal</param>
    ''' <param name="Dy">Dispersivity transverse horizontal</param>
    ''' <param name="k">The first order rate constant to use for nitrate decay</param>
    ''' <param name="Y">The Y dimension of the source plane</param>
    ''' <param name="v">The value of advection velocity to use</param>    
    ''' <remarks></remarks>
    Public Sub New(ByVal concInit As Single, ByVal Dx As Single, ByVal Dy As Single, ByVal Y As Single, ByVal k As Single, ByVal v As Single)        
        MyBase.New(concInit, Dx, Dy, Y, v)
        If k <= 0 Then Throw New Exception("Decay constant must be greater than zero")
        m_decay_sqrt = Math.Sqrt(1 + (4 * k * Dx / v))
    End Sub

    Public Overrides Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single
        Dim x_over_2_ax As Double

        x_over_2_ax = x / (2 * MyBase.m_Dx)

        Return MyBase.eval(x, y, z) * Math.Exp(x_over_2_ax - x_over_2_ax * m_decay_sqrt)
    End Function

End Class

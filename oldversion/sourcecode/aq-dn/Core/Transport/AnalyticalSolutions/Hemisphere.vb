''' <summary>
''' Test function for my function evaluator. Represents a solid hemisphere of a specified radius R
''' centered at (0,0,0).  Time is ignored.  The base of the hemisphere sits on the y-z plane
''' in the positive x direction only.
''' </summary>
''' <remarks>At every x,y,z this function evaluates to the specified value if the point is inside the sphere
''' or 0 if it is outside</remarks>
Friend Class Hemisphere
    Implements IAnalyticalFunction4D

    Private m_R As Single
    Private m_conc As Single

    Public ReadOnly Property t() As Single Implements IAnalyticalFunction4D.t
        Get
            'time has no meaning here
            Return -1
        End Get
    End Property

    Public Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single Implements IAnalyticalFunction4D.eval
        If x < 0 Then Return 0

        Dim r As Double = Math.Sqrt(x * x + y * y + z * z)

        If r <= m_R Then Return m_conc Else Return 0
    End Function

    Public ReadOnly Property Volume() As Double
        Get
            Return (2 * Math.PI * Math.Pow(m_R, 3)) / 3
        End Get
    End Property

    Public ReadOnly Property SurfaceArea() As Double
        Get
            Return 2 * Math.PI * Math.Pow(m_R, 2)
        End Get
    End Property

    Public ReadOnly Property Radius() As Single
        Get
            Return m_R
        End Get
    End Property

    Public Sub New(ByVal radius As Single, ByVal conc As Single)
        m_R = radius
        m_conc = conc
    End Sub
End Class

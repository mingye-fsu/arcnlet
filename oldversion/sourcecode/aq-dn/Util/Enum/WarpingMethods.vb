''' <summary>
''' Enum class for the different transform methods in the transport calculations
''' </summary>
''' <remarks></remarks>
Public Class WarpingMethods

    ''' <summary>
    ''' The different transform methods available for use in the transport module
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum WarpingMethod
        ''' <summary>
        ''' Spline transform
        ''' </summary>
        ''' <remarks></remarks>
        Spline = 1
        ''' <summary>
        ''' 2nd order polynomial transform
        ''' </summary>
        ''' <remarks>Use when the spline transform don't give the desired results (shifting) or is too slow</remarks>
        Polynomial2 = 2
        ''' <summary>
        ''' 1st order polynomial transform
        ''' </summary>
        ''' <remarks>Use when plumes need to be shifted only (not curved).</remarks>
        Polynomial1 = 3

    End Enum
End Class

''' <summary>
''' Enum class for the treatment of the Domenico source boundary
''' </summary>
''' <remarks></remarks>
Public Class DomenicoSourceBoundaries

    ''' <summary>
    ''' Available options for the Domenico source boundary
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum DomenicoSourceBoundary
        ''' <summary>
        ''' The second box from the left on the main transport module will be treated as a specified
        ''' input mass rate.  Z will be calculated automatically
        ''' </summary>
        ''' <remarks></remarks>
        Specified_Input_Mass_Rate = 1
        ''' <summary>
        ''' The second box from the left on the main transport module will be treated as the value of the Z
        ''' dimension of the Domenico solution.  The mass input rate will be calculated automatically
        ''' </summary>
        ''' <remarks></remarks>
        Specified_Z = 2
    End Enum

End Class

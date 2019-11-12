''' <summary>
''' Solution types for the transport solver.  Also includes methods to determine whether a given
''' solution type is 2D, 3D or steady state.
''' </summary>
''' <remarks></remarks>
Public Class SolutionTypes
    ''' <summary>
    ''' A list of available analytical solutions. The default solution is the ModifiedDomenico2D
    ''' </summary>
    ''' <remarks>All solution types with a negative value represent steady state solutions
    ''' All solution types that are even represent 3D solutions. Odd are 2D. All types with
    ''' |value|>=100 are solutions with decay</remarks>
    Public Enum SolutionType
        ''' <summary>
        ''' 3D Domenico solution in the form given by eq (3) in Gutierrez-Neri et. al. (2009)
        ''' Spreading is only in the -'ve z direction as given by Fig 3a in Domenico and Robbins 1985
        ''' </summary>
        ''' <remarks></remarks>
        DomenicoRobbins = 2
        ''' <summary>
        ''' 3D steady state Domenico Robbins solution in the form given by eq (5)
        ''' in Gutierrez-Neri et. al. (2009)
        ''' </summary>
        ''' <remarks>Travel time parameter will be ignored</remarks>
        DomenicoRobbinsSS = -2
        ''' <summary>
        ''' 2D Domenico solution in the form given by eq (4) in Gutierrez-Neri et. al. (2009)
        ''' </summary>
        ''' <remarks>Depth of source plane (Z) and vertical dispersivity (Dz) will be ignored</remarks>
        DomenicoRobbins2D = 3
        ''' <summary>
        ''' 2D version of the Modified Domenico Robbins solution in the form given by eq (13)
        ''' in Srinivasan et al. (2007)
        ''' </summary>
        ''' <remarks>Depth of source plane (Z) and vertical dispersivity (Dz) will be ignored</remarks>
        ModifiedDomenico2D = 5
        ''' <summary>
        ''' 2D Modified Domenico solution in the form given by eq (13)
        ''' in Srinivasan et al. (2007) along with a variable longitudinal (and transverse) dispersivities
        ''' based on the method of Pickens and Grisak (1981)
        ''' </summary>
        ''' <remarks>
        ''' Depth of source plane (Z), longitudinal (Dx), horizontal (Dy) and vertical dispersivity (Dz) will be ignored.
        ''' Dy=0.1*Dx.
        ''' The use of these variable dispersivity methods is questionable since they have not been well
        ''' justified in the literature
        ''' </remarks>
        VD_ModifiedDomenico2D_PG = 7
        ''' <summary>
        ''' 2D Modified Domenico solution in the form given by eq (13)
        ''' in Srinivasan et al. (2007) along with a variable longitudinal (and transverse) dispersivities
        ''' based on the method of Xu and Eckstein (1995)
        ''' </summary>
        ''' <remarks>
        ''' Depth of source plane (Z), longitudinal (Dx), horizontal (Dy) and vertical dispersivity (Dz) will be ignored.
        ''' Dy=0.1*Dx.
        ''' The use of these variable dispersivity methods is questionable since they have not been well
        ''' justified in the literature
        ''' </remarks>
        VD_ModifiedDomenico2D_XE = 9
        ''' <summary>
        ''' 2D version of the steady state Domenico solution as given by eq (6) in Gutierrez-Neri et al.
        ''' </summary>
        ''' <remarks></remarks>
        DomenicoRobbinsSS2D = -7

        ''' <summary>
        ''' 2D steady state solution of the domenico solution as given by eq (12) in Srinivasan et al.
        ''' </summary>
        ''' <remarks>Includes first order decay</remarks>
        DomenicoRobbinsSSDecay2D = -101
    End Enum

    ''' <summary>
    ''' Tests the given solution type to see if it is 3D
    ''' </summary>
    ''' <param name="type">The type to check</param>
    ''' <returns>True if 3D, false otherwise</returns>
    ''' <remarks></remarks>
    Public Shared Function is3D(ByVal type As SolutionType) As Boolean
        If type Mod 2 = 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Tests the given solution type to see if it is 2D
    ''' </summary>
    ''' <param name="type">The type to check</param>
    ''' <returns>True if 2D, false otherwise</returns>
    ''' <remarks></remarks>
    Public Shared Function is2D(ByVal type As SolutionType) As Boolean
        If type Mod 2 <> 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Tests the given solution type to see if it is steady state
    ''' </summary>
    ''' <param name="type">The type to check</param>
    ''' <returns>True if it is, false otherwise</returns>
    ''' <remarks></remarks>
    Public Shared Function isSteadyState(ByVal type As SolutionType) As Boolean
        If type < 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function hasDecay(ByVal type As SolutionType) As Boolean
        If Math.Abs(type) >= 100 Then
            Return True
        Else
            Return False
        End If
    End Function
End Class

''' <summary>
''' Defines an interface to evaluate functions of 4 variables: x, y, z and time
''' </summary>
''' <remarks>Used for the transport calculation.  All functions that use this interface 
''' must be symmetric at least in the y and z dimensions AND be convex!!!</remarks>
Friend Interface IAnalyticalFunction4D

    ''' <summary>
    ''' represents the fixed time at which the solution is evaluated
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ReadOnly Property t() As Single

    ''' <summary>
    ''' Evaluates the function.    
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="y"></param>
    ''' <param name="z"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function eval(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Single


End Interface

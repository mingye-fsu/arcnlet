''' <summary>
''' The different post processing methods
''' </summary>
''' <remarks></remarks>
Public Class PostProcessing

    ''' <summary>
    ''' The different levels of plume post processing
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum PostProcessingAmount
        ''' <summary>
        ''' No post processing is done to the plume. The plume will end at the water body with a
        ''' cut that is perpendicular to the flow line.  This is the fastest method
        ''' </summary>
        ''' <remarks></remarks>
        None = 0
        ''' <summary>
        ''' Some post processing will be done.  The shape of the plume at the water body will
        ''' follow the shape of the water body boundary.  If the water bodies are not too complicated
        ''' and all the sources are on one side of the water body, this method will produce adequate results
        ''' with not much reduction in processing speed
        ''' </summary>
        ''' <remarks></remarks>
        Medium = 1
        ''' <summary>
        ''' When the medium level of post processing gives incorrect results (e.g. plumes extend past the
        ''' target water body). Full processing is required.  This method is the slowest. It is much
        ''' slower than the other two methods.
        ''' </summary>
        ''' <remarks></remarks>
        Full = 2
    End Enum
End Class

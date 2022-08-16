''' <summary>
''' A List of raster pixel types. 
''' </summary>
''' <remarks>Used by LayerInfo currently.</remarks>
Public Class RasterPixelTypes
    ''' <summary>
    ''' RasterPixelTypes= rstPixelType Constants
    ''' </summary>
    ''' <remarks>
    '''PT_UNKNOWN 	-1 	Pixel values are unknown.
    '''PT_U1 	0 	Pixel values are 1 bit.
    '''PT_U2 	1 	Pixel values are 2 bits.
    '''PT_U4 	2 	Pixel values are 4 bits.
    '''PT_UCHAR 	3 	Pixel values are unsigned 8 bit integers.
    '''PT_CHAR 	4 	Pixel values are 8 bit integers.
    '''PT_USHORT 	5 	Pixel values are unsigned 16 bit integers.
    '''PT_SHORT 	6 	Pixel values are 16 bit integers.
    '''PT_ULONG 	7 	Pixel values are unsigned 32 bit integers.
    '''PT_LONG 	8 	Pixel values are 32 bit integers.
    '''PT_FLOAT 	9 	Pixel values are single precision floating point.
    '''PT_DOUBLE 	10 	Pixel values are double precision floating point.
    '''PT_COMPLEX 	11 	Pixel values are complex.
    '''PT_DCOMPLEX 	12 	Pixel values are double precision complex.
    ''' </remarks>
    Public Enum RasterPixelType
        PT_UNKNOWN = -1
        PT_U1 = 0
        PT_U2 = 1
        PT_U4 = 2
        PT_UCHAR = 3
        PT_CHAR = 4
        PT_USHORT = 5
        PT_SHORT = 6
        PT_ULONG = 7
        PT_LONG = 8
        PT_FLOAT = 9
        PT_DOUBLE = 10
        PT_COMPLEX = 11
        PT_DCOMPLEX = 12
    End Enum

    ''' <summary>
    ''' Returns the string representing the given pixel type
    ''' </summary>
    ''' <param name="t">A pixel type corresponding to one of the valid values</param>
    ''' <returns>The string representing the value of the given pixel type.</returns>
    ''' <remarks></remarks>
    Public Shared Function GetPixelType(ByVal t As Integer) As String
        Return System.Enum.GetName(GetType(RasterPixelType), t)
    End Function

End Class

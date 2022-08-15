
''' <summary>
''' Contains functions to calculate the values of erf and erfc among other things
''' </summary>
''' <remarks>This class can be accessed outside of the AqDn namespace</remarks>
Public Class MathSpecial

    'erfc values
    'first dimension are the x values, second dimension are the y=erfc(x) values
    'this variable is initialized in initErfcTable
    Private Shared erfc_values(,) As Double = Nothing
    Private Shared erfc_ub As Integer = -1
    Private Shared erfc_dx As Double = -1

    'for erfc2
    Private Shared a0 As Single = -1.26551223
    Private Shared a1 As Single = 1.00002368
    Private Shared a2 As Single = 0.37409196
    Private Shared a3 As Single = 0.09678418
    Private Shared a4 As Single = -0.18628806
    Private Shared a5 As Single = 0.27886807
    Private Shared a6 As Single = -1.13520398
    Private Shared a7 As Single = 1.48851587
    Private Shared a8 As Single = -0.82215223
    Private Shared a9 As Single = 0.17087277

    ''' <summary>
    ''' Calculate the complement of the error function erfc(x)=1-erf(x)
    ''' </summary>
    ''' <param name="x"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function erfc2(ByVal x As Double) As Double
        'Complement of the error function approximation using Chebyshev fitting

        '     erfc(z) = t * exp(-z*z) * exp( a0 + a1*t + a2*t^2 + a3*t^3 ...
        '     where(t = 1 / (1 + z / 2))

        'Error:  1.2e-7 maximal relative error for z >= 0
        'Source:  Numerical Recipies, Chapter 6, Special functions 

        Dim z As Double = Math.Abs(x)
        Dim t As Double = 1 / (1 + 0.5 * z)

        erfc2 = t * Math.Exp(-z * z + a0 + t * (a1 + t * (a2 + t * (a3 + t * (a4 + t * (a5 + t * (a6 + t * (a7 + t * (a8 + t * a9)))))))))

        If x < 0 Then
            erfc2 = 2.0R - erfc2
        End If
    End Function

    ''' <summary>
    ''' Calculate the error function erf(x)
    ''' </summary>
    ''' <param name="x"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function erf2(ByVal x As Double) As Double
        erf2 = 1.0R - erfc2(x)
    End Function

    ''' <summary>
    ''' Calculate erfc(x) via a lookup table.  The value returned corresponds to the closest
    ''' available value in the table. No interpolation is done to increase running speed.
    ''' Error will be introduced in the result, especially if the value of x is outside the 
    ''' range contained in the table
    ''' </summary>
    ''' <param name="x"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function erfc(ByVal x As Double) As Double
        Dim z As Double = Math.Abs(x)

        If z > erfc_values(erfc_ub, 0) Then
            erfc = 0
        Else
            Dim idx As Integer = Math.Round(z / erfc_dx)
            erfc = erfc_values(idx, 1)
        End If

        If x < 0 Then
            erfc = 2.0R - erfc
        End If

    End Function
    ''' <summary>
    ''' Calculate erf(x) via a lookup table.
    ''' </summary>
    ''' <param name="x"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function erf(ByVal x As Double) As Double
        erf = 1.0R - erfc(x)
    End Function

    ''' <summary>
    ''' Machine epsilon for single precision
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function macheps_sp()
        Dim macheps As Single = 1.0F

        Trace.WriteLine("Single precision machine epsilon")
        Trace.WriteLine("Eps" & vbTab & vbTab & vbTab & "1+Eps")
        While CType(1.0F + macheps, Single) <> 1.0F
            Trace.WriteLine(macheps.ToString("E9") & vbTab & CType(1.0F + macheps, Single).ToString("E9"))
            macheps = macheps / 2.0F
            'If next epsilon yields 1, then break, because current
            'epsilon is the machine epsilon.
        End While
        Trace.WriteLine(macheps.ToString("E9") & vbTab & CType(1.0F + macheps, Single).ToString("E9"))
        Trace.WriteLine("Machine epsilon: " & macheps.ToString("E9"))
        Return macheps
    End Function

    ''' <summary>
    '''  Machine epsilon for double precision
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function macheps_dp()
        Dim macheps As Double = 1.0R

        Trace.WriteLine("Double precision machine epsilon")
        Trace.WriteLine("Eps" & vbTab & vbTab & vbTab & "1+Eps")
        While CType(1.0R + macheps, Double) <> 1.0R
            Trace.WriteLine(macheps.ToString("E17") & vbTab & CType(1.0R + macheps, Double).ToString("E17"))
            macheps = macheps / 2.0R
            'If next epsilon yields 1, then break, because current
            'epsilon is the machine epsilon.
        End While
        Trace.WriteLine(macheps.ToString("E17") & vbTab & CType(1.0R + macheps, Double).ToString("E17"))
        Trace.WriteLine("Machine epsilon: " & macheps.ToString("E17"))
        Return macheps
    End Function

    ''' <summary>
    ''' Initializes the lookup table for the erf function. This function must be called before
    ''' any calls to the erf function are made.  This is not necessary if using erf2
    ''' </summary>
    ''' <remarks>Calling this function is REQUIRED prior to calling erf and erfc.</remarks>
    Public Shared Function initErfcTable() As Boolean
        If erfc_values Is Nothing Then
            Try
                Dim buffer As New IO.MemoryStream(My.Resources.erfc)

                Dim s As New Runtime.Serialization.Formatters.Binary.BinaryFormatter
                erfc_values = s.Deserialize(buffer)
                buffer.Close()
                erfc_ub = erfc_values.GetUpperBound(0)
                erfc_dx = erfc_values(1, 0) - erfc_values(0, 0)
            Catch ex As Exception
                Trace.WriteLine("[Error] " & Reflection.MethodInfo.GetCurrentMethod.Name & ": Couldn't initialize error fucntion lookup: " & ex.ToString)
                Return False
            End Try
        End If
        Return True
    End Function

    ''' <summary>
    ''' Generates a table of the complementary error function values and writes them to the binary file erf.bin
    ''' This table is used by the erf and erfc functions of this class.  The table only needs to be
    ''' generated once.  The erfc.bin file can then be placed as a resource
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub generateErfcTable()
        Dim dx As Single = 0.00005          'the grid resolution
        Dim min_x As Single = 0             'the minimum value of x (should not be changed from 0)
        Dim max_x As Single = 80            'the maximum value of x    
        Dim erfxy(Math.Round(max_x / dx, 0), 1) As Double
        Dim s As New Runtime.Serialization.Formatters.Binary.BinaryFormatter

        Dim f As New IO.FileStream(String.Format("erfc.bin", min_x, max_x, dx), IO.FileMode.Create)

        erfxy(0, 0) = min_x
        erfxy(0, 1) = erfc2(min_x)
        For i As Integer = 1 To erfxy.GetUpperBound(0)
            erfxy(i, 0) = erfxy(i - 1, 0) + dx
            erfxy(i, 1) = erfc2(erfxy(i, 0))
        Next
        s.Serialize(f, erfxy)
        f.Close()
    End Sub
    Public Shared Sub generateErfTableASCII()
        Dim f As New IO.StreamWriter("erf.txt")

        Dim dx As Single = 0.0001
        Dim min_x As Single = 0
        Dim max_x As Single = 6
        Dim x(Math.Round(max_x / dx, 0)) As Double

        f.WriteLine(String.Format("Private Shared erf_{0}to{1}(,) As Double = {{{{ _", min_x, max_x))

        x(0) = min_x
        For i As Integer = 0 To x.Length - 2
            x(i + 1) = x(i) + dx
            f.WriteLine(String.Format("{0:F20}, _", x(i)))
        Next
        f.WriteLine(String.Format("{0:F20} _", x(x.Length - 2) + dx))
        f.WriteLine("}, { _")
        For i As Integer = 0 To x.Length - 2
            f.WriteLine(String.Format("{0:F20}, _", MathSpecial.erf(x(i))))
        Next
        f.WriteLine(String.Format("{0:F20} _", MathSpecial.erf(x(x.Length - 1))))
        f.WriteLine("}}")

        f.Close()


    End Sub

End Class

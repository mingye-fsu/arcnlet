''' <summary>
''' Interface used by for remoting an ArcNLET module. All type references and 
''' object declarations should declare the interface instead of the remoted 
''' object to avoid loading the assembly of the remoted object into
''' the calling AppDomain
''' </summary>
''' <remarks></remarks>
Public Interface IModuleRunRemote
    ''' <summary>
    ''' Set by the wrapper to the channel that it is listening for cancelation requests
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Property CancelChannelURI() As String

    ''' <summary>
    ''' The parameters that the wrapper will retrieve
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Property InParams() As Hashtable

    ''' <summary>
    ''' The output parameters that the transport module provides. Set by the wrapper
    ''' when the transport calculation finishes.
    ''' </summary>
    ''' <value></value>
    ''' <returns>The output parameters. If there are none the hash table will be empty</returns>
    ''' <remarks></remarks>
    Property OutParams() As Hashtable

    ''' <summary>
    ''' Set by the calling AppDomain to the current indent level of Trace.IndentLevel,
    ''' so that Trace output of the remoted object is properly indented in the calling
    ''' AppDomain's TraceListener
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Property Indent() As Integer


    ''' <summary>
    ''' Called by the bootstrapper to notify the main ui that the wrapper process has terminated
    ''' </summary>
    ''' <param name="msg">message</param>
    ''' <remarks>This function is only used by the Bootstrapper, not the wrapper</remarks>
    Sub WrapperProcessTerminated(ByVal msg As String)

    ''' <summary>
    ''' Notify the main UI that the calculation is complete (either sucess or failure).
    ''' Called by the wrapper
    ''' </summary>
    ''' <param name="msg">The return message</param>
    ''' <remarks></remarks>
    Sub CalculationComplete(ByVal msg As String)
End Interface

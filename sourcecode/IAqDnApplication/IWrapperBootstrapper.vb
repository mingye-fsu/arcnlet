''' <summary>
''' Interface used by RemotingBootstrapper
''' </summary>
''' <remarks></remarks>
Public Interface IWrapperBootstrapper
    Sub run(ByVal channelURI As String, ByVal moduleName As String)
    Sub kill()
End Interface

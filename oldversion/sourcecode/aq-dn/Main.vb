Imports ESRI.ArcGIS.ArcMapUI                'IMxApplication2
Imports ESRI.ArcGIS.Framework               'IApplication
Imports ESRI.ArcGIS.Carto
Imports System.Runtime.Remoting
Imports System.Runtime.Remoting.Channels
Imports System.Runtime.Remoting.Channels.Ipc
Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.ADF
Imports ESRI.ArcGIS.DataSourcesRaster
Imports ESRI.ArcGIS.DataSourcesFile
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Geometry


''' <summary>
''' This class holds shared members that extract properties and values from the 
''' main ArcMap reference that is passed in via the init() sub
''' If running within ArcMap, init() must be called before anything else
''' </summary>
Public Class Main



    'the reference to the ArcMap application
    Private Shared m_App As IMxApplication2

    'the client-wrapper communication channel
    Private Shared m_chanwrapper As IpcClientChannel


    ''' <summary>
    ''' The currently running ArcMap application instance
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property App() As IApplication
        Get
            App = m_App
        End Get
    End Property
    ''' <summary>
    ''' The currently loaded document.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Friend Shared ReadOnly Property ActiveDoc() As IMxDocument
        Get
            If TypeOf App.Document Is IMxDocument Then
                Return App.Document
            Else
                Trace.WriteLine("Error: The active document is not an ArcMap document")
                MsgBox("Error: the active document is not an ArcMap document")
                Return Nothing
            End If
        End Get
    End Property
    ''' <summary>
    ''' The path and name of the currently loaded document.  If no document is loaded, N/A is returned.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Friend Shared ReadOnly Property ActiveDocFullName() As String
        Get
            'In ArcMap, index 0 is the Normal template. If a base template is loaded, 
            'it is index 1 and the document is index 2. If there is no base template, the document is index 1. 
            If Not ActiveDoc Is Nothing Then
                Select Case App.Templates.Count
                    Case 1
                        Return "N/A"
                    Case 2
                        Return App.Templates.Item(1)
                    Case 3
                        Return App.Templates.Item(2)
                End Select
            Else
                Return "N/A"
            End If
            Return ""
        End Get
    End Property

    ''' <summary>
    ''' The full path without filename of the current document
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Friend Shared ReadOnly Property ActiveDocPath()
        Get
            ActiveDocPath = IO.Path.GetDirectoryName(ActiveDocFullName)
        End Get
    End Property

    ''' <summary>
    ''' The first map in the maps collection of the active document.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Friend Shared ReadOnly Property ActiveMap() As Map
        Get
            Return ActiveDoc.FocusMap
        End Get
    End Property

    ''' <summary>
    ''' Initializes the ArcMap application reference
    ''' </summary>
    ''' <param name="ArcMapApplication">The reference to the application</param>
    ''' <remarks>This function MUST be called before most classes in this namespace are used since many
    ''' of those classes use this Main class to extract information from ArcMap. The only exceptions
    ''' are the Core classes since they have been abstracted so that they are callable as stand alone
    ''' libraries</remarks>
    Public Shared Sub init(ByVal ArcMapApplication As IMxApplication2)
        m_App = ArcMapApplication
    End Sub


End Class



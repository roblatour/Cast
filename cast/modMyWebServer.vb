Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Xml
Imports System.Windows.Forms

'ref http://www.santry.com/Blog/tabid/90/ID/1289/Writing-a-Simple-HTTP-Server-in-VBNet.aspx

Module modMyWebServer

    Friend gSynchroMilliSeconds As Integer = 3500

    Friend MyDictionaryOfExtentionsAndMimeTypes As New Dictionary(Of String, String)

    Friend Sub EstablishAcceptableFileTypes()

        'MyDictionaryOfExtentionsAndMimeTypes.Add("flac", "audio/flac")
        MyDictionaryOfExtentionsAndMimeTypes.Add("mp3", "audio/mpeg")
        MyDictionaryOfExtentionsAndMimeTypes.Add("mp4", "video/mp4")
        MyDictionaryOfExtentionsAndMimeTypes.Add("txt", "text/plain")
        MyDictionaryOfExtentionsAndMimeTypes.Add("wav", "audio/wav")

    End Sub

    Friend ws As EmbeddedWebServer = EmbeddedWebServer.getWebServer

    Friend Sub StartEmbeddedWebServer(ByVal RootPath As String, ByVal CoordinateTheStartOfStreaming As Boolean)

        ws.DebugIsOn = DebugIsOn
        ws.VirtualRoot = RootPath
        ws.DictionaryOfExtentionsAndMimeTypes = MyDictionaryOfExtentionsAndMimeTypes

        If CoordinateTheStartOfStreaming Then
            ws.DelayStreamingUntilThisTime = Now.AddMilliseconds(gSynchroMilliSeconds)   ' this hold off all streaming until all devices can be started at the same time
            If ws.DebugIsOn Then Console.WriteLine(vbCrLf & "Streaming will begin at " & ws.DelayStreamingUntilThisTime.ToString)
        Else
            ws.DelayStreamingUntilThisTime = Now.AddSeconds(-1)  ' this will allow streaming to start immediately
            If ws.DebugIsOn Then Console.WriteLine(vbCrLf & "Streaming will begin immediately")
        End If

        ws.StartWebServer()

    End Sub

    Friend Sub StopEmbeddedWebServer()

        ws.StopWebServer()

    End Sub

End Module

Public Class EmbeddedWebServer

#Region "Declarations"

    Private Shared singleWebserver As EmbeddedWebServer
    Private LocalTCPListener As TcpListener
    Private WebThread As Thread

#End Region

#Region "Properties"

    Friend Property DebugIsOn As Boolean = False

    Friend Property DelayStreamingUntilThisTime As Date = Now
    Friend Property ListenWebPort As Integer = 55123
    Friend Property VirtualRoot As String

    Friend Property DictionaryOfExtentionsAndMimeTypes As New Dictionary(Of String, String)

#End Region

#Region "Methods"

    Friend Function GetIPHostAddress() As IPAddress

        Return System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList.Last

    End Function

    Friend Shared Function getWebServer() As EmbeddedWebServer

        If singleWebserver Is Nothing Then
            singleWebserver = New EmbeddedWebServer
        End If
        Return singleWebserver

    End Function

    Friend Sub StartWebServer()

        If ws.DebugIsOn Then Console.WriteLine("StartWebServer " & Now.ToString)

        Try

            LocalTCPListener = New TcpListener(GetIPHostAddress, ListenWebPort)
            LocalTCPListener.Start()
            WebThread = New Thread(AddressOf StartListen)
            WebThread.Start()

        Catch ex As Exception
            If ws.DebugIsOn Then Console.WriteLine(ex.Message)
        End Try

    End Sub

    Public Function GetMimeType(ByVal sRequestFile As String) As String

        On Error Resume Next

        Dim ReturnValue As String = String.Empty

        Dim sFileExt As String = Path.GetExtension(sRequestFile).Replace(".", "").ToLower

        ReturnValue = DictionaryOfExtentionsAndMimeTypes.Item(sFileExt)

        Return ReturnValue

    End Function

    Friend Function GetTheDefaultFileName(ByVal sLocalDirectory As String) As String
        Return "index.html"
    End Function

    Friend Function GetLocalPath(ByVal sWebServerRoot As String, ByVal sDirName As String) As String

        Dim sVirtualDir As String = ""
        Dim sRealDir As String = ""
        Dim iStartPos As Integer = 0
        sDirName.Trim()
        sWebServerRoot = sWebServerRoot.ToLower
        sDirName = sDirName.ToLower
        Select Case sDirName
            Case "/"
                sRealDir = VirtualRoot
            Case Else
                If Mid$(sDirName, 1, 1) = "/" Then
                    sDirName = Mid$(sDirName, 2, Len(sDirName))
                End If
                sRealDir = VirtualRoot & sDirName.Replace("/", "\")
        End Select
        Return sRealDir

    End Function

    Friend Sub SendHeader(ByVal sHttpVersion As String, ByVal sMimeHeader As String, ByVal iTotalBytes As Integer, ByVal sStatusCode As String, ByRef thisSocket As Socket)

        If ws.DebugIsOn Then Console.WriteLine("SendHeader " & Now.ToString)

        Dim sBuffer As String = ""
        If Len(sMimeHeader) = 0 Then
            sMimeHeader = "text/html"
        End If

        sBuffer = sHttpVersion & sStatusCode & vbCrLf &
            "Server: CastServer" & vbCrLf &
            "Content-Type: " & sMimeHeader & vbCrLf &
            "Accept-Ranges: bytes" & vbCrLf &
            "Content-Length: " & iTotalBytes & vbCrLf & vbCrLf

        If ws.DebugIsOn Then Console.WriteLine(sBuffer.Trim)

        Dim bSendData As [Byte]() = Encoding.ASCII.GetBytes(sBuffer)

        SendToBrowser(bSendData, thisSocket)

    End Sub

    Friend Overloads Sub SendToBrowser(ByVal sData As String, ByRef thisSocket As Socket)
        SendToBrowser(Encoding.ASCII.GetBytes(sData), thisSocket)
    End Sub

    Friend Overloads Sub SendToBrowser(ByVal bSendData As [Byte](), ByRef thisSocket As Socket)

        If ws.DebugIsOn Then Console.WriteLine("Send to Browser " & Now.ToString)

        If thisSocket.Connected Then

            DelayUntilAllDevicesAreReadyToPlay()

            If thisSocket.Send(bSendData, bSendData.Length, 0) = -1 Then
                'socket error can't send packet
            Else
                'number of bytes sent.
            End If

        Else

            'connection dropped.

        End If

    End Sub

    Friend Sub DelayUntilAllDevicesAreReadyToPlay()

        If Now < ws.DelayStreamingUntilThisTime Then

            Dim tspan As TimeSpan = DelayStreamingUntilThisTime - Now
            Thread.Sleep(tspan.TotalMilliseconds)

        End If

        If ws.DebugIsOn Then Console.WriteLine("********* Go: {0}", Now.ToString("MM/dd/yyyy hh:mm:ss.ffff tt"))

    End Sub

    Private Sub StartListen()

        If ws.DebugIsOn Then Console.WriteLine("Start Listen " & Now.ToString)

        Dim iStartPos As Integer
        Dim sRequest As String
        Dim sDirName As String
        Dim sRequestedFile As String
        Dim sErrorMessage As String
        Dim sLocalDir As String
        Dim sWebserverRoot = VirtualRoot
        Dim sQueryString As String
        Dim sPhysicalFilePath As String = ""
        Dim sFormattedMessage As String = ""

        Do While True

            'accept new socket connection
            Dim mySocket As Socket = LocalTCPListener.AcceptSocket
            If mySocket.Connected Then
                Dim bReceive() As Byte = New [Byte](1024) {}
                Dim i As Integer = mySocket.Receive(bReceive, bReceive.Length, 0)
                Dim sBuffer As String = Encoding.ASCII.GetString(bReceive)
                'find the GET request.
                If (sBuffer.Substring(0, 3) <> "GET") Then
                    mySocket.Close()
                    Return
                End If
                iStartPos = sBuffer.IndexOf("HTTP", 1)
                Dim sHttpVersion = sBuffer.Substring(iStartPos, 8)
                sRequest = sBuffer.Substring(0, iStartPos - 1)
                sRequest.Replace("\\", "/")
                If (sRequest.IndexOf(".") < 1) And (Not (sRequest.EndsWith("/"))) Then
                    sRequest = sRequest & "/"
                End If
                'get the file name
                iStartPos = sRequest.LastIndexOf("/") + 1
                sRequestedFile = sRequest.Substring(iStartPos)
                If InStr(sRequest, "?") <> 0 Then
                    iStartPos = sRequest.IndexOf("?") + 1
                    sQueryString = sRequest.Substring(iStartPos)
                    sRequestedFile = Replace(sRequestedFile, "?" & sQueryString, "")
                End If
                'get the directory
                sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3)
                'identify the physical directory.
                If (sDirName = "/") Then
                    sLocalDir = sWebserverRoot
                Else
                    sLocalDir = GetLocalPath(sWebserverRoot, sDirName)
                End If
                'if the directory isn't there then display error.
                If sLocalDir.Length = 0 Then
                    sErrorMessage = "Error!! Requested Directory does not exists"
                    SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", mySocket)
                    SendToBrowser(sErrorMessage, mySocket)
                    mySocket.Close()
                End If

                If sRequestedFile.Length = 0 Then
                    sRequestedFile = GetTheDefaultFileName(sLocalDir)
                    If sRequestedFile = "" Then
                        sErrorMessage = "Error!! No Default File Name Specified"
                        SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", mySocket)
                        SendToBrowser(sErrorMessage, mySocket)
                        mySocket.Close()
                        Return
                    End If
                End If

                Dim sMimeType As String = GetMimeType(sRequestedFile)

                sPhysicalFilePath = sLocalDir & sRequestedFile
                If Not File.Exists(sPhysicalFilePath) Then
                    sErrorMessage = "404 Error! File Does Not Exists..."
                    SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", mySocket)
                    SendToBrowser(sErrorMessage, mySocket)
                Else

                    Try
                        Dim iTotBytes As Integer = 0
                        Dim sResponse As String = ""
                        Dim fs As New FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                        Dim reader As New BinaryReader(fs)
                        Dim bytes() As Byte = New Byte(fs.Length) {}

                        While reader.BaseStream.Position < reader.BaseStream.Length
                            reader.Read(bytes, 0, bytes.Length)
                            sResponse = sResponse & Encoding.ASCII.GetString(bytes, 0, reader.BaseStream.Length)
                            iTotBytes = reader.BaseStream.Length
                        End While
                        reader.Close()
                        fs.Close()

                        SendHeader(sHttpVersion, sMimeType, iTotBytes, " 200 OK", mySocket)

                        SendToBrowser(bytes, mySocket)

                    Catch ex As Exception
                        sErrorMessage = "404 Error! File Does Not Exists..."
                        SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", mySocket)
                        SendToBrowser(sErrorMessage, mySocket)
                    End Try

                End If

                mySocket.Close()

                If ws.DebugIsOn Then Console.WriteLine("Close socket " & Now.ToString)

            End If

        Loop

    End Sub

    Friend Sub StopWebServer()

        If ws.DebugIsOn Then Console.WriteLine("StopWebServer " & Now.ToString)

        Try
            LocalTCPListener.Stop()
            WebThread.Abort()

        Catch ex As Exception
            If ws.DebugIsOn Then Console.WriteLine(ex.Message)
        End Try

    End Sub

#End Region

End Class

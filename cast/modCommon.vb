Imports System.IO
Imports System.Net
Imports Microsoft.Win32
Imports NAudio.Wave

Module modCommon

    Friend gIPAddressOfClient As String = String.Empty

    Friend gHostComputerName As String = String.Empty
    Friend gHostComputerIPAddress As IPAddress
    Friend gPlayThroughPCSpeakers As Boolean = False
    Friend gNoDefaultSpeaker As Boolean = False 'v1.6

    Friend gWebSite As String = "https://www.rlatour.com/cast/index.html"

    Friend TempFileInUse As String = String.Empty

    Friend Function GenerateATempFileName(ByVal Ext As String) As String

        Return System.IO.Path.GetTempPath() & "~cast~" & Guid.NewGuid().ToString().Replace("-", "") & Ext

    End Function

    Friend Function ConvertToFixedStringLength(ByVal input As String, ByVal len As Integer) As String

        Dim ReturnValue = input

        ReturnValue = ReturnValue & StrDup(len + 1, " ")
        ReturnValue = ReturnValue.Remove(len)

        Return ReturnValue

    End Function

    Friend Function GetAbridgedCommandLine() As String

        Dim ReturnValue As String = String.Empty

        Try
            'removes the program name from the command line

            Dim Raw As String = Environment.CommandLine
            Dim Program As String = Environment.GetCommandLineArgs(0)
            ReturnValue = Raw.Remove(0, Raw.IndexOf(Program) + Program.Length).Trim.TrimStart("""").Trim

        Catch ex As Exception
        End Try

        Return ReturnValue.Trim

    End Function

    Friend Function GetIPHostAddressString() As String

        Return System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList.Last.ToString

    End Function

    Friend Function GetIPHostAddress() As IPAddress

        Return System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList.Last

    End Function

    Dim SyncLockObject As New Object

    <System.Diagnostics.DebuggerStepThrough()> Friend Sub Console_WriteLineInColour(ByVal Message As String, Optional ByVal Colour As ConsoleColor = ConsoleColor.Gray, Optional IncludeCarrageReturn As Boolean = True)

        SyncLock SyncLockObject

            Dim OriginalForeGroundColour As ConsoleColor = Console.ForegroundColor

            Console.ForegroundColor = Colour

            If IncludeCarrageReturn Then
                Console.WriteLine(Message)
            Else
                Console.Write(Message)
            End If

            Console.ForegroundColor = OriginalForeGroundColour

        End SyncLock

    End Sub

    Friend Function GetAllCastabalefiles(ByVal folder As String) As String()

        Try

            Dim WavArray As String() = Directory.GetFiles(folder, "*.wav", SearchOption.AllDirectories)
            Dim MP3Array As String() = Directory.GetFiles(folder, "*.mp3", SearchOption.AllDirectories)
            Dim MP4Array As String() = Directory.GetFiles(folder, "*.mp4", SearchOption.AllDirectories)

            Dim FinalArray As String() = WavArray.Concat(MP3Array).Concat(MP4Array).ToArray

            System.Array.Sort(FinalArray)

            If DebugIsOn Then
                For Each filename As String In FinalArray
                    Console.WriteLine(filename)
                Next
            End If

            Return FinalArray

        Catch ex As Exception

            Dim ReturnValue(0) As String
            ReturnValue(0) = "Warning: " & ex.Message.ToArray
            Return ReturnValue

        End Try

    End Function

    Friend Sub CleanUpTempFiles()

        Try

            Dim TempArray As String() = Directory.GetFiles(System.IO.Path.GetTempPath(), "~cast~*.*", SearchOption.TopDirectoryOnly)

            For Each filename In TempArray

                Try
                    File.Delete(filename)
                Catch ex As Exception
                End Try

            Next

        Catch ex As Exception
        End Try

    End Sub

    Friend Function GetDuration(ByVal Filename As String) As Integer

        Dim Duration As Integer = 0

        Try

            Dim reader As Object = Nothing

            Select Case Path.GetExtension(Filename).ToLower

                Case Is = ".wav"

                    Try
                        reader = New WaveFileReader(Filename)
                        Duration = Int(reader.TotalTime.TotalMilliseconds)
                    Catch ex1 As Exception
                        Try
                            reader = New Mp3FileReader(Filename)
                            Duration = Int(reader.TotalTime.TotalMilliseconds)
                        Catch ex2 As Exception
                            If DebugIsOn Then Console.WriteLine(ex2.ToString)
                        End Try
                    End Try


                Case Is = ".mp3"

                    Try
                        reader = New Mp3FileReader(Filename)
                        Duration = Int(reader.TotalTime.TotalMilliseconds)
                    Catch ex1 As Exception
                        Try
                            reader = New WaveFileReader(Filename)
                            Duration = Int(reader.TotalTime.TotalMilliseconds)
                        Catch ex2 As Exception
                            If DebugIsOn Then Console.WriteLine(ex2.ToString)
                        End Try
                    End Try


                Case Is = ".mp4", ".m3u8"

                    ' convert to wav file
                    Using video = New MediaFoundationReader(Filename)
                        Duration = Int(video.TotalTime.TotalMilliseconds)
                    End Using

            End Select

            reader = Nothing

        Catch ex As Exception

            If DebugIsOn Then Console.WriteLine(ex.ToString)

        End Try

        If DebugIsOn Then

            Dim ts As New TimeSpan(0, 0, 0, 0, Duration)
            Console.WriteLine("Duration: " & ts.ToString())
            ts = Nothing

        End If

        Return Duration

    End Function


#Region "Memory Management"

    <System.Diagnostics.DebuggerStepThrough()> Friend Class MemoryManagement ' changed from public to friend in v4.2.7

        Private Declare Function SetProcessWorkingSetSize Lib "kernel32.dll" (
          ByVal process As IntPtr,
          ByVal minimumWorkingSetSize As Integer,
          ByVal maximumWorkingSetSize As Integer) As Integer

        Friend Shared Sub FlushMemory()

            Try

                'Dim x As Int32 = Int(System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024)
                'If x < 25 Then Exit Try

                If DebugIsOn Then Console.WriteLine("Freeing memory")

                If (Environment.OSVersion.Platform = PlatformID.Win32NT) Then
                    Dim p As Process = Process.GetCurrentProcess
                    SetProcessWorkingSetSize(p.Handle, -1, -1)
                    p.Dispose()
                Else
                    GC.Collect()
                    GC.WaitForPendingFinalizers()
                End If

            Catch ex As Exception
            End Try

        End Sub

    End Class

#End Region

    'Friend Function ReplaceDiacritics(ByVal PathAndFilename As String) As String
    '    'for example change é to e
    '    Dim tempBytes As Byte() = System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(PathAndFilename)
    '    Return System.Text.Encoding.UTF8.GetString(tempBytes)
    'End Function

End Module

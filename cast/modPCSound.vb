Imports System.Threading
Imports NAudio.Wave
Imports NAudio.CoreAudioApi
Imports System.Windows.Forms
Imports System.IO

Module modPCSound

    Private enumer As MMDeviceEnumerator = New MMDeviceEnumerator()

    Friend Function GetCurrentPCVolume() As Integer

        Try
            Dim dev As MMDevice = enumer.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) 'v1.6
            Return Int(dev.AudioEndpointVolume.MasterVolumeLevelScalar * 100)
        Catch ex As Exception
            gNoDefaultSpeaker = True
            Return 0
        End Try


    End Function

    Friend Function GetCurrentPCMuteSetting() As Boolean

        Try
            Dim dev As MMDevice = enumer.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) 'v1.6
            Return dev.AudioEndpointVolume.Mute
        Catch ex As Exception
            gNoDefaultSpeaker = True
            Return False
        End Try

    End Function

    Friend Sub PlayAFileThruThePCSpeakers(ByVal filename As String)

        Try

            Dim PlayOnNewThread As New Thread(New ThreadStart(Function()

                                                                  Try

                                                                      Dim reader As Object = Nothing

                                                                      If DebugIsOn Then Console.WriteLine("Preparing to play on speakers " & filename)

                                                                      Dim Duration As Integer = GetDuration(filename)

                                                                      Select Case Path.GetExtension(filename).ToLower

                                                                          Case Is = ".wav"
                                                                              Try
                                                                                  reader = New WaveFileReader(filename)
                                                                              Catch ex1 As Exception
                                                                                  Try
                                                                                      reader = New Mp3FileReader(filename)
                                                                                  Catch ex2 As Exception
                                                                                  End Try
                                                                              End Try

                                                                          Case Is = ".mp3"

                                                                              Try
                                                                                  reader = New Mp3FileReader(filename)
                                                                              Catch ex As Exception
                                                                                  Try
                                                                                      reader = New WaveFileReader(filename)
                                                                                  Catch ex2 As Exception
                                                                                  End Try
                                                                              End Try

                                                                          Case Is = ".mp4", "m38u"

                                                                              ' convert to wav file

                                                                              Dim TempPathAndFileName = GenerateATempFileName(Path.GetExtension(filename))
                                                                              If File.Exists(TempPathAndFileName) Then File.Delete(TempPathAndFileName)

                                                                              Using video = New MediaFoundationReader(filename)
                                                                                  WaveFileWriter.CreateWaveFile(TempPathAndFileName, video)
                                                                              End Using
                                                                              reader = New WaveFileReader(TempPathAndFileName)

                                                                      End Select

                                                                      If reader Is Nothing Then
                                                                          Console_WriteLineInColour("Could not play " & filename & " to your PC speakers", ConsoleColor.Yellow)
                                                                          Exit Try
                                                                      End If

                                                                      Dim WaveOut = New WaveOutEvent
                                                                      WaveOut.Init(reader)
                                                                      WaveOut.Play()

                                                                      While (Now < MaxWaitTime)  ' wait until the end of playing the sound file
                                                                          Thread.Sleep(250)
                                                                          Application.DoEvents()
                                                                      End While

                                                                      WaveOut.Stop()

                                                                  Catch ex As Exception

                                                                      ' Console_WriteLineInColour(("Error playing " & filename & " to your PC speakers:" & vbCrLf & ex.Message.ToString).Replace(". ", "." & vbCrLf), ConsoleColor.Yellow)

                                                                      gNoDefaultSpeaker = True

                                                                  End Try

                                                                  Return 0

                                                              End Function))

            PlayOnNewThread.Start()

        Catch ex As Exception

        End Try

        Update_CastingFinishedOnThisManyDevices(False)

    End Sub

End Module

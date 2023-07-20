Imports System.IO
Imports System.Speech.AudioFormat
Imports System.Speech.Synthesis

Module modSpeech

    Friend ListOfInstalledVoices As New List(Of InstalledVoice)
    Friend DesiredInstalledVoice As InstalledVoice

    Friend Function InitializeVoices() As Boolean

        Dim ReturnValue As Boolean

        BuildListOfInstalledVoices()
        ReturnValue = SetDefaultVoice()

        Return ReturnValue

    End Function

    Friend Sub BuildListOfInstalledVoices()

        Try

            Dim sp As New SpeechSynthesizer

            For Each InstalledVoice As InstalledVoice In sp.GetInstalledVoices()
                ListOfInstalledVoices.Add(InstalledVoice)
            Next

            sp.Dispose()

        Catch ex As Exception
        End Try

    End Sub

    Friend Function SetDefaultVoice() As Boolean

        Dim ReturnValue As Boolean = False
        Try
            Dim sp As New SpeechSynthesizer
            DesiredInstalledVoice = sp.GetInstalledVoices.FirstOrDefault
            ReturnValue = True
        Catch ex As Exception
        End Try

        Return ReturnValue

    End Function

    Friend Function SetDesiredVoice(ByVal DesiredVoice As String) As Boolean

        Dim ReturnValue As Boolean = False

        Try

            For Each InstalledVoice As InstalledVoice In ListOfInstalledVoices

                Dim v As VoiceInfo = InstalledVoice.VoiceInfo

                If DesiredVoice.ToLower = v.Name.ToLower Then
                    DesiredInstalledVoice = InstalledVoice
                    ReturnValue = True
                    Exit For
                End If

            Next

        Catch ex As Exception
        End Try

        Return ReturnValue

    End Function

    Friend Function ReportInstalledVoices() As List(Of String)

        Dim ReturnValue As New List(Of String)

        Try

            For Each InstalledVoice As InstalledVoice In ListOfInstalledVoices

                Dim v As VoiceInfo = InstalledVoice.VoiceInfo

                Dim DetailLine As String = String.Empty
                DetailLine &= ConvertToFixedStringLength(v.Name, 30) & " "
                DetailLine &= ConvertToFixedStringLength(v.Culture.DisplayName, 25) & " "
                DetailLine &= ConvertToFixedStringLength(v.Gender.ToString, 20) & " "
                DetailLine &= ConvertToFixedStringLength(v.Age.ToString, 10)

                ReturnValue.Add(DetailLine)

            Next

            ReturnValue.Sort()

        Catch ex As Exception
            ReturnValue.Add(ex.Message)
        End Try

        Return ReturnValue

    End Function

    Friend Sub CreateASpeechFile(ByVal Text As String, ByVal Filename As String)

        Dim sp As New SpeechSynthesizer
        sp.SelectVoice(DesiredInstalledVoice.VoiceInfo.Name)
        sp.SetOutputToWaveFile(Filename, New SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Mono)) 
        sp.Speak(Text)
        sp.SetOutputToDefaultAudioDevice()

        sp.Dispose()

    End Sub

End Module

Imports System.Net
Imports System.IO
Imports SharpCast
Imports NAudio.Wave
Imports NAudio.CoreAudioApi

Module modCasting

    'ref https://github.com/jpepiot/SharpCast
    'ref https://developers.google.com/cast/docs/reference/chrome/chrome.cast.media.MusicTrackMediaMetadata

    Const DefaultMediaReceiver As String = "CC1AD845"

    Friend synclockobj As New Object
    Friend CastingFinishedOnThisManyDevices As Integer ' CastingFinishedOnThisManyDevices is used to determine when a url should stop playing; its based on all devices reporting that they are finished playing

    Friend MaxWaitTime As DateTime ' maxwait time is used to determine when a file should stop playing; it is based on the current time when the file starts playing + the duration of the file + 1/4 of a second for good measure

    Friend Async Function CastAFile(ByVal IndividualIPAddress As IPAddress, ByVal Filename As String) As Task

        Dim FileIsAVideo As Boolean = (Path.GetExtension(Filename) = ".mp4") OrElse (Path.GetExtension(Filename) = ".m3u8")

        Dim ThisDevice As Device = GetDevice(IndividualIPAddress)

        If ThisDevice Is Nothing Then Exit Function 'v1.6

        If DebugIsOn Then
            Console.WriteLine("Casting to " & ThisDevice.FriendlyName & " " & Filename)
        End If

        Try

            If ThisDevice.Muted Then

                If (ThisDevice.DeviceKind = Device.DeviceType.PC_Speaker) AndAlso (gNoDefaultSpeaker) Then
                Else
                    Console_WriteLineInColour(ThisDevice.FriendlyName & " is muted so nothting will be cast to it", ConsoleColor.Yellow)
                End If

                Update_CastingFinishedOnThisManyDevices(False)

                Exit Try

            End If



            If ThisDevice.DeviceKind = Device.DeviceType.PC_Speaker Then

                If gNoDefaultSpeaker Then 'v1.6

                    Exit Try

                Else

                    ws.DelayUntilAllDevicesAreReadyToPlay()

                    PlayAFileThruThePCSpeakers(Filename)

                    gPlayThroughPCSpeakers = False ' modMain will wait for this to be set to false before deleting the temp file

                    Exit Try

                End If

            End If

            If (ThisDevice.DeviceKind = Device.DeviceType.Google_Assistant) AndAlso FileIsAVideo Then

                'convert .mp4 file to wav file

                Dim NewFileName As String = GenerateATempFileName(".wav")

                Using video = New MediaFoundationReader(Filename)
                    WaveFileWriter.CreateWaveFile(NewFileName, video)
                End Using

                Filename = NewFileName

            End If

            Try

                ' AddHandler MyPlayer.MediaStatusChanged, AddressOf AvailabilityChangedCallBack

                If ThisDevice.Player.GetRunningApp() Is Nothing Then
                    ThisDevice.Player.LaunchApp(DefaultMediaReceiver)
                End If

                Dim HostIPAddress As String = GetIPHostAddressString.ToString

                Dim Title As String = Path.GetFileNameWithoutExtension(Filename)

                'use a temp copied file in place of the original file (to avoid issues with filename having spaces etc.)
                TempFileInUse = GenerateATempFileName(Path.GetExtension(Filename))
                File.Copy(Filename, TempFileInUse, True)
                Filename = TempFileInUse

                Dim uri As New Uri("http://" & HostIPAddress & ":" & ws.ListenWebPort & "/" & Path.GetFileName(Filename))

                If DebugIsOn Then Console.WriteLine("Preparing to cast " & Filename)

                If Path.GetExtension(Filename) = ".mp4" Then

                    Dim MyMovieMediaMetadata As MovieMediaMetadata = New MovieMediaMetadata
                    MyMovieMediaMetadata.Title = Path.GetFileName(Filename)
                    ThisDevice.Player.LoadVideo(uri, MyDictionaryOfExtentionsAndMimeTypes.Item("mp4"), MyMovieMediaMetadata)

                Else

                    Dim MyMusicTrackMediaMetadata As MusicTrackMediaMetadata = New MusicTrackMediaMetadata
                    MyMusicTrackMediaMetadata.Title = Title
                    ThisDevice.Player.LoadMusic(uri, MyDictionaryOfExtentionsAndMimeTypes.Item(Path.GetExtension(Filename).Replace(".", "")), MyMusicTrackMediaMetadata)

                End If

                If DebugIsOn Then Console.WriteLine("Running app " & ThisDevice.Player.GetRunningApp().ToString)

            Catch ex As Exception

                If DebugIsOn Then Console.WriteLine(ex.ToString)

            End Try

            ' RemoveHandler MyPlayer.MediaStatusChanged, AddressOf AvailabilityChangedCallBack

        Catch ex As Exception

            If DebugIsOn Then Console.WriteLine(ex.ToString)

        End Try

    End Function

    Friend Async Function CastAURI(ByVal IndividualIPAddress As IPAddress, ByVal iURI As Uri) As Task

        Dim FileIsAVideo As Boolean = iURI.ToString.EndsWith(".mp4")

        Dim ThisDevice As Device = GetDevice(IndividualIPAddress)

        Try

            If ThisDevice.DeviceKind = Device.DeviceType.PC_Speaker Then
                Console_WriteLineInColour("cast does not currently support streaming a url file to your PC speakers", ConsoleColor.Yellow)
                Update_CastingFinishedOnThisManyDevices(False)
                Exit Try
            End If

            If ThisDevice.DeviceKind = Device.DeviceType.Google_Assistant Then
                If FileIsAVideo Then
                    Console_WriteLineInColour("cast does not currently support streaming a video to a Google Assistant device", ConsoleColor.Yellow)
                    Update_CastingFinishedOnThisManyDevices(False)
                    Exit Try
                End If
            End If

            If ThisDevice.Muted Then
                Console_WriteLineInColour(ThisDevice.FriendlyName & " is muted so nothting will be cast to it", ConsoleColor.Yellow)
                Exit Try
            End If

            Try

                AddHandler ThisDevice.Player.MediaStatusChanged, AddressOf AvailabilityChangedCallBack

                If ThisDevice.Player.GetRunningApp() Is Nothing Then
                    ThisDevice.Player.LaunchApp(DefaultMediaReceiver)
                End If

                If iURI.ToString.ToLower.EndsWith(".mp4") OrElse iURI.ToString.ToLower.EndsWith(".m3u8") Then

                    Dim MyMovieMediaMetadata As MovieMediaMetadata = New MovieMediaMetadata
                    MyMovieMediaMetadata.Title = iURI.ToString
                    ThisDevice.Player.LoadVideo(iURI, MyDictionaryOfExtentionsAndMimeTypes.Item("mp4"), MyMovieMediaMetadata)

                ElseIf iURI.ToString.ToLower.EndsWith(".mp3") Then

                    Dim MyMusicTrackMediaMetadata As MusicTrackMediaMetadata = New MusicTrackMediaMetadata
                    MyMusicTrackMediaMetadata.Title = iURI.ToString
                    ThisDevice.Player.LoadMusic(iURI, MyDictionaryOfExtentionsAndMimeTypes.Item("mp3"), MyMusicTrackMediaMetadata)

                ElseIf iURI.ToString.ToLower.EndsWith(".wav") Then

                    Dim MyMusicTrackMediaMetadata As MusicTrackMediaMetadata = New MusicTrackMediaMetadata
                    MyMusicTrackMediaMetadata.Title = iURI.ToString
                    ThisDevice.Player.LoadMusic(iURI, MyDictionaryOfExtentionsAndMimeTypes.Item("wav"), MyMusicTrackMediaMetadata)

                End If

                If DebugIsOn Then Console.WriteLine("Running app " & ThisDevice.Player.GetRunningApp().ToString)

            Catch systimeout As SystemException

                If systimeout.Message = "Could not get response for the request LoadRequest" Then
                    Console_WriteLineInColour("Could not find or load specified url", ConsoleColor.Red)
                End If
                If DebugIsOn Then Console.WriteLine(systimeout.ToString)

            Catch ex As Exception

                If DebugIsOn Then Console.WriteLine(ex.ToString)

            End Try

        Catch ex As Exception

            If DebugIsOn Then Console.WriteLine(ex.ToString)

        End Try

    End Function

    Friend Function GetDevicesFriendlyName(ByVal iIPaddress As IPAddress) As String

        Dim ReturnValue As String = String.Empty

        For Each device In Devices
            If device.IPAddress.ToString = iIPaddress.ToString Then
                ReturnValue = device.FriendlyName
                Exit For
            End If
        Next

        Return ReturnValue

    End Function

    Friend Sub CastMuteChange(ByVal IndividualIPAddress As IPAddress, ByVal DesiredMuteSetting As Boolean)

        If DebugIsOn Then Console.WriteLine("Set mute for " & IndividualIPAddress.ToString & " to " & DesiredMuteSetting.ToString)

        If DesiredMuteSetting Then
            Console_WriteLineInColour("Muting " & GetDevicesFriendlyName(IndividualIPAddress), ConsoleColor.Green)
        Else
            Console_WriteLineInColour("Unmuting " & GetDevicesFriendlyName(IndividualIPAddress), ConsoleColor.Green)
        End If

        Dim ThisDevice As Device = GetDevice(IndividualIPAddress)

        If ThisDevice.DeviceKind = Device.DeviceType.PC_Speaker Then

            Try 'v1.6 handle no default should card

                Dim enumer As MMDeviceEnumerator = New MMDeviceEnumerator()
                Dim dev As MMDevice = enumer.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)

                If dev.AudioEndpointVolume.Mute = DesiredMuteSetting Then
                    'current mute setting already matches desired mute setting, so do nothing
                Else
                    dev.AudioEndpointVolume.Mute = DesiredMuteSetting
                End If

                dev.Dispose()
                enumer.Dispose()

            Catch ex As Exception

                gNoDefaultSpeaker = True

            End Try

        Else

            Dim CurrentMute As Boolean = ThisDevice.Player.GetStatus.Volume.Muted

            If CurrentMute = DesiredMuteSetting Then
                'current mute setting already matches desired mute setting, so do nothing
            Else
                ThisDevice.Player.SetMuted(DesiredMuteSetting)
            End If

        End If

    End Sub

    Friend Function CastVolumeChange(ByVal iIPAddress As IPAddress, ByVal iDesiredVolume As Integer, Optional ShowDisplay As Boolean = False) As Boolean

        Dim ReturnValue As Boolean

        Try

            Dim NewVolume As Double = iDesiredVolume / 100

            Dim ThisDevice As Device = GetDevice(iIPAddress)

            If DebugIsOn Or ShowDisplay Then

                Dim BaseMessage As String = "Setting volume on " & ThisDevice.FriendlyName & " to " & iDesiredVolume.ToString & "%"

                If ThisDevice.Muted Then
                    Console_WriteLineInColour(BaseMessage & " (device is muted)", ConsoleColor.Yellow)
                Else
                    Console_WriteLineInColour(BaseMessage, ConsoleColor.Green)
                End If

            End If


            If ThisDevice.DeviceKind = Device.DeviceType.PC_Speaker Then

                Try ' handle no default sound card

                    Dim enumer As MMDeviceEnumerator = New MMDeviceEnumerator()
                    Dim dev As MMDevice = enumer.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)

                    If dev.AudioEndpointVolume.MasterVolumeLevelScalar = CType(NewVolume, Single) Then
                        'current volume is already set to desired volume, so do nothing
                    Else
                        dev.AudioEndpointVolume.MasterVolumeLevelScalar = CType(NewVolume, Single)
                    End If

                    dev.Dispose()
                    enumer.Dispose()

                Catch ex As Exception

                    gNoDefaultSpeaker = True

                End Try

            Else

                'Dim CurrentVolume As Integer = GetDeviceTableCurrentVolume(iIPAddress)  
                Dim CurrentVolume As Integer = GetCurrentVolume(ThisDevice.IPAddress) ' call to get volume from physical device in case user changed in manually on google device
                If Int(NewVolume * 100) = (CurrentVolume * 100) Then
                    'current volume is already set to desired volume, so do nothing
                Else
                    ThisDevice.Player.SetVolume(NewVolume)
                End If

            End If

            ReturnValue = True

        Catch ex As Exception

            ReturnValue = False

        End Try

        Return ReturnValue

    End Function

    Friend Function GetDevice(ByVal iIPAddress As IPAddress) As Device

        Dim ReturnValue As Device = Nothing

        For Each Device In Devices

            If Device.IPAddress.ToString = iIPAddress.ToString Then
                ReturnValue = Device
                Exit For
            End If

        Next

        Return ReturnValue

    End Function

    Private Function WhatTypeOfDeviceIsThis(ByVal iIPAddress As IPAddress) As Device.DeviceType

        Dim ReturnValue As Device.DeviceType

        For Each Device In Devices

            If Device.IPAddress.ToString = iIPAddress.ToString Then
                ReturnValue = Device.DeviceKind
                Exit For
            End If

        Next

        Return ReturnValue

    End Function

    Friend Function GetCurrentVolume(ByVal IndividualIPAddress As IPAddress) As Integer

        Dim ReturnValue As Integer = -1

        Dim ReturnValueNeeded As Boolean = True

        While ReturnValueNeeded

            Try

                Dim ThisDevice As Device = GetDevice(IndividualIPAddress)

                If WhatTypeOfDeviceIsThis(IndividualIPAddress) = Device.DeviceType.Google_Assistant Then
                    ReturnValue = CInt(ThisDevice.Player.GetStatus.Volume.Level * 110)  ' Google Home devices have volume that are base 110
                Else
                    ReturnValue = CInt(ThisDevice.Player.GetStatus.Volume.Level * 100)
                End If

                ReturnValueNeeded = False

            Catch ex As Exception
                If DebugIsOn Then Console.WriteLine(ex.Message)
                Exit While
            End Try

        End While

        Return ReturnValue

    End Function

    Friend Function GetCurrentDeviceVolume(ByVal ThisDevice As Device) As Integer

        Return CInt(ThisDevice.Player.GetStatus.Volume.Level * 100)

    End Function

    Friend Function GetCurrentMuteSetting(ByVal IndividualIPAddress As IPAddress) As Boolean

        Dim ReturnValue As Boolean

        Dim ThisDevice As Device = GetDevice(IndividualIPAddress)

        ReturnValue = ThisDevice.Player.GetStatus.Volume.Muted

        Return ReturnValue

    End Function

    Friend Function GetCurrentDeviceMuteSetting(ByVal thisdevice As Device) As Boolean

        Dim ReturnValue As Boolean

        ReturnValue = ThisDevice.Player.GetStatus.Volume.Muted

        Return ReturnValue

    End Function

    Friend Sub CastStop(ByVal IndividualIPAddress As IPAddress)

        Dim ThisDevice As Device = GetDevice(IndividualIPAddress)

        Try

            If ThisDevice.Player IsNot Nothing Then
                ThisDevice.Player.StopApp()
            End If

        Catch ex As Exception
        End Try

    End Sub

    Private Sub AvailabilityChangedCallBack(ByVal sender As SharpCast.Player, ByVal Status As MediaStatus)

        If DebugIsOn Then

            If (Status.IdleReason > Nothing) Then
                Console.WriteLine(Status.MediaSessionId.ToString & " " & Status.PlayerState.ToString.ToLower & " " & Status.IdleReason)
            Else
                Console.WriteLine(Status.MediaSessionId.ToString & " " & Status.PlayerState.ToString.ToLower)
            End If

        End If

        If (Status.IdleReason > Nothing) AndAlso (Status.IdleReason = "FINISHED") Then

            Update_CastingFinishedOnThisManyDevices(False)

        End If

    End Sub

    ' #####.Connect() Connects To ChromeCast device. 
    ' #####.Load(Uri contentUri, String contentType, MediaMetadata metadata, bool autoPlay, StreamType streamType) Loads New content. 
    ' #####.LoadVideo(Uri contentUri, String contentType, MovieMediaMetadata metadata, bool autoPlay, StreamType streamType) Loads New video content. 
    ' #####.LoadPhoto(Uri contentUri, String contentType, PhotoMediaMetadata metadata, bool autoPlay, StreamType streamType) Loads New photo content. 
    ' #####.LoadMusic(Uri contentUri, String contentType, MusicTrackMediaMetadata metadata, bool autoPlay, StreamType streamType) Loads New music content. 
    ' #####.Play() Begins playback Of the content that was loaded With the load Call. 
    ' #####.Pause() Pauses playback Of the current content. 
    ' #####.Seek(Double position) Sets the current position In the stream. 
    ' #####.SetVolume(Double level) Sets the stream volume.
    ' #####.SetMuted(bool muted) Mutes/Unmutes the stream volume. 
    ' #####.StopApp() Stops playback Of the current content. 
    ' #####.LaunchApp(String applicationId) 
    ' #####.GetRunningApp() Gets running application.  
    ' #####.GetAppAvailability(String applicationId) Checks whether an application Is available.  
    ' #####.GetStatus() Retrieves the media status.

End Module

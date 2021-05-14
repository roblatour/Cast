Imports System.Net
Imports System.IO
Imports System.Threading
'
' ref: https://github.com/roblatour/cast
'
' ref: https://stackoverflow.com/questions/189549/embedding-dlls-in-a-compiled-executable
' Install-Package Costura.Fody which embeds the sharpcast.dll

' ref https://www.dotnetperls.com/async-vbnet
'
Module modMain

    Friend gCommandLine_About As Boolean = False

    Friend gCommandLine_Background As Boolean = False

    Friend gCommandLine_Cancel As Boolean = False


    Friend gCommandLine_Dir As Boolean = False
    Friend gCommandLine_Dir_Value As String = String.Empty
    Friend gCommandLine_Dir_AllFiles As String()

    Friend gCommandLine_File As Boolean = False
    Friend gCommandLine_File_Value As String = String.Empty

    Friend gCommandLine_Help As Boolean = False

    Friend gCommandLine_Inventory As Boolean = False

    Friend gCommandLine_IP As Boolean = False
    Friend gCommandLine_IP_Addresses As New List(Of IPAddress)

    Friend gCommandLine_Mute As Boolean = False

    Friend gCommandLine_Pause As Boolean = False

    Friend gCommandLine_Port As Boolean = False
    Friend gCommandLine_Port_Value As Integer = -1

    Friend gCommandLine_Random As Boolean = False

    Friend gCommandLine_Text As Boolean = False
    Friend gCommandLine_Text_Value As String = String.Empty

    Friend gCommandLine_Unmute As Boolean = False

    Friend gCommandLine_URL As Boolean = False
    Friend gCommandLine_URL_Value As Uri = Nothing

    Friend gCommandLine_Voice As Boolean = False
    Friend gCommandLine_Voice_Value As String = String.Empty

    Friend gCommandLine_Volume As Boolean = False
    Friend gCommandLine_Volume_Value As Integer

    Friend gCommandLine_Website As Boolean = False

    Friend DefaultVoiceSetOK As Boolean

    Friend StartingVolume As Double = -1

    Friend Devices As New List(Of Device)

    Friend StartingColour As ConsoleColor = Console.ForegroundColor

    Sub Main()

        Randomize()

        Dim ReturnCode As Integer = 0

        CleanUpTempFiles()

        Dim CommandLine As String = GetAbridgedCommandLine()

        If DebugIsOn Then
            CommandLine = CommandLineOverride()
        End If

        If ASimpleRequest(CommandLine) Then

            If gCommandLine_Help Then ShowHelp()
            If gCommandLine_About Then ShowAbout()
            If gCommandLine_Website Then OpenWebSite()

        Else

            AddHandler Console.CancelKeyPress, AddressOf HandleCtrl_C

            InventoryGoogleDevices()
            DefaultVoiceSetOK = InitializeVoices()
            EstablishAcceptableFileTypes()

            Dim ErrorInCommandLine As String = String.Empty
            Dim WarningInCommandLine As String = String.Empty

            ValidateCommandLine(CommandLine, WarningInCommandLine, ErrorInCommandLine)

            If WarningInCommandLine > String.Empty Then

                Dim Warnings() As String = WarningInCommandLine.Split(vbCrLf)
                For Each IndividualWarning In Warnings
                    If IndividualWarning.Trim > String.Empty Then Console_WriteLineInColour("Warning: " & IndividualWarning.Trim, ConsoleColor.Yellow)
                Next

                ReturnCode = 1

            End If

            If ErrorInCommandLine > String.Empty Then

                Dim Errors() As String = ErrorInCommandLine.Split(vbCrLf)
                For Each IndividualError In Errors
                    If IndividualError.Trim > String.Empty Then Console_WriteLineInColour("Error:   " & IndividualError.Trim, ConsoleColor.Red)
                Next

                ReturnCode = 2

            End If

            If gCommandLine_Help Then ShowHelp()
            If gCommandLine_About Then ShowAbout()

            If ReturnCode < 2 Then

                If gCommandLine_Website Then OpenWebSite()

                If gCommandLine_Port Then SetEmbeddedServersListenPort(gCommandLine_Port_Value)
                If gCommandLine_Cancel Then ProcessCastStop()
                If gCommandLine_Mute Then ProcessMuteChange(True)
                If gCommandLine_Unmute Then ProcessMuteChange(False)
                If gCommandLine_Volume Then ProcessVolumeChange(gCommandLine_Volume_Value, True)
                If gCommandLine_Inventory Then ProcessInventoryCommand()

                If gCommandLine_Background Then
                Else
                    If gCommandLine_File OrElse gCommandLine_Text OrElse gCommandLine_URL OrElse gCommandLine_Dir Then
                        Dim task As Task = New Task(AddressOf ActivateControlConsole)
                        task.Start()
                    End If
                End If

                If gCommandLine_File Then ProcessCastFile(gCommandLine_File_Value)
                If gCommandLine_Text Then ProcessCastText(gCommandLine_Text_Value)
                If gCommandLine_URL Then ProcessCastUrl(gCommandLine_URL_Value)
                If gCommandLine_Dir Then ProcessCastDirectory(gCommandLine_Dir_Value, gCommandLine_Dir_AllFiles)

                CleanUpTempFiles()

            End If

        End If

        If gCommandLine_Background Then
        Else
            ProcessCastStop(False)
        End If

        If gCommandLine_Pause Then
            ExitUnderWay = True
            Console_WriteLineInColour(" ", ConsoleColor.White)
            Console_WriteLineInColour("Press enter to continue", ConsoleColor.White)
            Console.ReadLine()
        End If

        Console.ForegroundColor = StartingColour

        Environment.Exit(ReturnCode)

    End Sub
    Private ExitUnderway As Boolean = False

    Private Sub ActivateControlConsole()

        'note to self: pause and resume are not (yet) supported because pausing the pc speaker was not what I wanted to work out at the time

        Const StandardOptions As String =
            "  0, 1, 2, ... 9   set volume to 0%, 10%, 20%, ... 90%" & vbCrLf &
            "  Up arrow         set volume up by 1" & vbCrLf &
            "  Down arrow       set volume down by 1" & vbCrLf &
            "  M                mute" & vbCrLf &
            "  U                unmute" & vbCrLf &
            "  X                cancel casting and exit" & vbCrLf

        Const DirOptions As String =
            "  S                skip the current file" & vbCrLf &
            StandardOptions

        If gCommandLine_Dir Then
            Console_WriteLineInColour(vbCrLf & "Control keys:" & vbCrLf & DirOptions, ConsoleColor.White)
        Else
            Console_WriteLineInColour(vbCrLf & "Control keys:" & vbCrLf & StandardOptions, ConsoleColor.White)
        End If

        Dim InvalidKey As Boolean = False
        Dim KeyPressed As String = String.Empty

        While True

            KeyPressed = Console.ReadKey(True).Key.ToString.ToUpper

            Select Case KeyPressed

                Case Is = "S"
                    If gCommandLine_Dir Then
                        Console_WriteLineInColour("Skipping current file", ConsoleColor.Green)
                        MaxWaitTime = Now.AddSeconds(-1)
                    Else
                        InvalidKey = True
                    End If

                Case Is = "UPARROW"
                    AdjustVolume(1)

                Case Is = "DOWNARROW"
                    AdjustVolume(-1)

                Case Is = "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "NUMPAD0", "NUMPAD1", "NUMPAD2", "NUMPAD3", "NUMPAD4", "NUMPAD5", "NUMPAD6", "NUMPAD7", "NUMPAD8", "NUMPAD9"
                    KeyPressed = Right(KeyPressed, 1)
                    SetVolume(CInt(KeyPressed) * 10)

                Case Is = "M"
                    ProcessMuteChange(True)

                Case Is = "U"
                    ProcessMuteChange(False)

                Case Is = "X"
                    Console_WriteLineInColour("Cancel casting and exit", ConsoleColor.Green)
                    ShutdownTheProgram()
                    Exit Sub

                Case Else

                    If ExitUnderway Then
                    Else
                        InvalidKey = True
                    End If

            End Select

            If InvalidKey Then
                InvalidKey = False
                Console_WriteLineInColour(vbCrLf & "Invalid key, valid keys are: " & vbCrLf, ConsoleColor.White, False)
                If gCommandLine_Dir Then
                    Console_WriteLineInColour(DirOptions, ConsoleColor.White)
                Else
                    Console_WriteLineInColour(StandardOptions, ConsoleColor.White)
                End If
            End If

        End While

    End Sub

    Private Sub HandleCtrl_C(ByVal sender As Object, ByVal args As ConsoleCancelEventArgs)

        ProcessCastStop()
        Console_WriteLineInColour(vbCrLf & "Casting cancelled" & vbCrLf, ConsoleColor.White)
        Console.ForegroundColor = StartingColour
        CastingFinishedOnThisManyDevices = gCommandLine_IP_Addresses.Count
        Environment.Exit(99)

    End Sub

    Private Sub AdjustVolume(ByVal adjustement As Integer)

        For Each DeviceIPAddress In gCommandLine_IP_Addresses

            For Each Device In Devices

                If Device.IPAddress.ToString = DeviceIPAddress.ToString Then

                    Dim CurrentVolume As Integer = Device.Volume

                    Dim NewVolume As Integer = CurrentVolume + adjustement

                    If NewVolume < 0 Then
                        NewVolume = 0
                    ElseIf NewVolume > 100 Then
                        NewVolume = 100
                    End If

                    If Device.Muted Then
                        ProcessMuteChange(False)
                    End If

                    If CurrentVolume = NewVolume Then
                        If DebugIsOn Then Console_WriteLineInColour("Volume already set to " & NewVolume.ToString & "% on " & Device.FriendlyName, ConsoleColor.Yellow)
                    Else
                        If CastVolumeChange(Device.IPAddress, NewVolume) Then
                            UpdatedDeviceTableToReflectNewVolume(Device.IPAddress, NewVolume)
                        End If

                    End If

                End If

            Next

        Next

    End Sub

    Private Sub SetVolume(ByVal SetVolume As Integer)

        For Each DeviceIPAddress In gCommandLine_IP_Addresses

            For Each Device In Devices

                If Device.IPAddress.ToString = DeviceIPAddress.ToString Then

                    Dim CurrentVolume As Integer = Device.Volume

                    Dim NewVolume As Integer = SetVolume

                    If NewVolume < 0 Then
                        NewVolume = 0
                    ElseIf NewVolume > 100 Then
                        NewVolume = 100
                    End If

                    Dim ConsoleColour As ConsoleColor = ConsoleColor.Green

                    If Device.Muted Then
                        ProcessMuteChange(False)
                    End If

                    If CurrentVolume = NewVolume Then
                        If DebugIsOn Then Console_WriteLineInColour("Volume already set to " & NewVolume.ToString & "% on " & Device.FriendlyName, ConsoleColor.Yellow)
                    Else
                        If CastVolumeChange(Device.IPAddress, NewVolume) Then
                            UpdatedDeviceTableToReflectNewVolume(Device.IPAddress, NewVolume)
                        End If
                    End If

                End If

            Next

        Next

    End Sub

    Private Sub ShutdownTheProgram()

        ProcessCastStop()

        Console.ForegroundColor = StartingColour

        Update_CastingFinishedOnThisManyDevices(True, gCommandLine_IP_Addresses.Count)

        CleanUpTempFiles()

        Environment.Exit(99)

    End Sub

    Private Sub SetEmbeddedServersListenPort(ByVal ListenPort As Integer)

        ws.ListenWebPort = ListenPort

    End Sub

    Private Function ASimpleRequest(ByVal CommandLine As String) As Boolean

        Dim ReturnValue As String = True

        Dim TweakedCommandLine As String = TweakCommandLineChangingSwitchesToCHR255(CommandLine)

        If TweakedCommandLine = String.Empty Then

            gCommandLine_Help = True
            ReturnValue = True

        Else

            Dim Commands() As String = TweakedCommandLine.Split(Chr(255))

            For Each Command As String In Commands

                Select Case Command.ToLower.Trim

                    Case Is = String.Empty

                    Case Is = "about"
                        gCommandLine_About = True

                    Case Is = "debug"
                        DebugIsOn = True
                        ReturnValue = False

                    Case Is = "help"
                        gCommandLine_Help = True

                    Case Is = "pause"
                        gCommandLine_Pause = True

                    Case Is = "random"
                        gCommandLine_Random = True

                    Case Is = "website"
                        gCommandLine_Website = True

                    Case Else
                        ReturnValue = False
                        Exit For

                End Select

            Next

        End If

        If ReturnValue Then

            'if only a pause was enter, treat it like a pause + help request
            If gCommandLine_Pause Then
                If gCommandLine_About OrElse gCommandLine_Website Then
                Else
                    gCommandLine_Help = True
                    ReturnValue = True
                End If
            End If

        Else

            'something other than a help, pause, about, or website request was entered so reset everything except debug

            gCommandLine_Help = False
            gCommandLine_About = False
            gCommandLine_Pause = False
            gCommandLine_Website = False

        End If

        Return ReturnValue

    End Function

    Private Sub ValidateCommandLine(ByVal CommandLine As String, ByRef WarningInCommandLine As String, ByRef ErrorInCommandLine As String)

        Try

            CommandLine = CommandLine.Trim

            If CommandLine = String.Empty Then
                gCommandLine_Help = True
                Exit Try
            End If

            Dim Commands() As String = TweakCommandLineChangingSwitchesToCHR255(CommandLine).Split(Chr(255))

            If Commands(0) > Nothing Then

                'command index 0 is the program name, it should not be followed by anything
                Dim UnwantedExtraData As String = Commands(0).Trim
                If UnwantedExtraData.Length > 0 Then
                    WarningInCommandLine &= "unexpected information (""" & UnwantedExtraData & """) found at the start of the command line; this information will be ignored" & vbCrLf
                End If

            End If

            For x = 1 To Commands.Count - 1

                Dim Command As String = Commands(x).Trim

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("about") Then

                    gCommandLine_About = True

                    If Command.Length > 6 Then
                        WarningInCommandLine &= "unexpected information found directly after the -about switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("background") Then

                    gCommandLine_Background = True

                    If Command.Length > 10 Then
                        WarningInCommandLine &= "unexpected information found directly after the -background switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("broadcast") Then

                    gCommandLine_IP = True

                    For Each Device In Devices
                        gCommandLine_IP_Addresses.Add(Device.IPAddress)
                    Next

                    If Command.Length > 9 Then
                        WarningInCommandLine &= "unexpected information found directly after the -broadcast switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("cancel") Then

                    gCommandLine_Cancel = True

                    If Command.Length > 6 Then
                        WarningInCommandLine &= "unexpected information found directly after the -cancel switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("debug") Then

                    DebugIsOn = True

                    If Command.Length > 5 Then
                        WarningInCommandLine &= "unexpected information found directly after the -debug switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************
                If Command.ToLower.StartsWith("device") Then

                    gCommandLine_IP = True ' Device names if correctly matched will be converted to IP Addresses and treated as if the IP address was entered on the command line

                    Dim TheRestOfTheDeviceArgument = Command.Remove(0, 6).Trim

                    If TheRestOfTheDeviceArgument = String.Empty Then

                        WarningInCommandLine &= "the device switch was set but no devices were provided; switch will be ignored" & vbCrLf
                        gCommandLine_IP = False

                    Else

                        Dim ListOfDevicesFromCommandLine As New List(Of String)

                        If TheRestOfTheDeviceArgument.Contains("""") Then

                            If TheRestOfTheDeviceArgument.StartsWith("""") AndAlso TheRestOfTheDeviceArgument.EndsWith("""") Then

                                Dim QuoteOn As Boolean = False

                                Dim CurrentEntry As String = String.Empty
                                For Each c As Char In TheRestOfTheDeviceArgument

                                    If c = """"c Then

                                        QuoteOn = Not QuoteOn

                                        If QuoteOn Then
                                        Else
                                            If CurrentEntry > String.Empty Then
                                                ListOfDevicesFromCommandLine.Add(CurrentEntry)
                                                CurrentEntry = String.Empty
                                            End If
                                        End If

                                    Else

                                        If QuoteOn Then
                                            CurrentEntry &= c

                                        Else

                                            If c = " "c Then
                                                ' ingnor spaces between starting and ending quotes
                                            Else
                                                ErrorInCommandLine &= "a character was found in the device name when a quote was expected" & vbCrLf
                                            End If

                                        End If

                                    End If

                                Next

                            Else

                                ErrorInCommandLine &= "quotes may only be used when they surround the device name" & vbCrLf

                            End If

                        Else

                            ListOfDevicesFromCommandLine.Add(TheRestOfTheDeviceArgument)

                        End If

                        Dim DeviceFound As Boolean
                        Dim AllDevicesHaveBeenFound As Boolean = True

                        If ErrorInCommandLine = String.Empty Then

                            For Each DeviceName As String In ListOfDevicesFromCommandLine

                                DeviceFound = False
                                For Each Device In Devices
                                    If DeviceName.ToLower = Device.FriendlyName.ToLower Then
                                        gCommandLine_IP_Addresses.Add(Device.IPAddress)
                                        DeviceFound = True
                                        Exit For
                                    End If
                                Next

                                If DeviceFound Then
                                Else
                                    AllDevicesHaveBeenFound = False
                                End If

                            Next

                            'If all devices have not been found re-inventory the google devices and try one last time 

                            If AllDevicesHaveBeenFound Then
                            Else

                                If DebugIsOn Then Console.WriteLine("************* repeated attempt at inventory ******************")

                                InventoryGoogleDevices() ' reinventory Google devices to double check that it is not there

                                For Each DeviceName As String In ListOfDevicesFromCommandLine

                                    DeviceFound = False
                                    For Each Device In Devices
                                        If DeviceName.ToLower = Device.FriendlyName.ToLower Then
                                            gCommandLine_IP_Addresses.Add(Device.IPAddress)
                                            DeviceFound = True
                                            Exit For
                                        End If
                                    Next

                                    If DeviceFound Then
                                    Else
                                        ErrorInCommandLine &= "device name """ & DeviceName & """ could not be found" & vbCrLf   'strike 2 your out
                                    End If

                                Next

                            End If

                        End If

                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("dir") Then

                    gCommandLine_Dir = True

                    Dim TheRestOfTheDirArgument = Command.Remove(0, 3).Trim

                    Dim DirectoryName As String = TheRestOfTheDirArgument.Replace("""", "").Trim

                    If DirectoryName = String.Empty Then
                        ErrorInCommandLine &= "-dir switch used but no directory name was provided" & vbCrLf
                        gCommandLine_Dir = False
                        GoTo NextArgument
                    End If

                    Dim AllFiles As String() = GetAllCastabalefiles(DirectoryName)

                    Try

                        If AllFiles.Count = 1 Then
                            If AllFiles(0).StartsWith("Warning: ") Then
                                WarningInCommandLine &= AllFiles(0) & vbCrLf
                                gCommandLine_Dir = False
                                Exit Try
                            End If
                        End If

                        gCommandLine_Dir_Value = DirectoryName
                        gCommandLine_Dir_AllFiles = AllFiles

                    Catch ex As Exception

                    End Try

                    GoTo NextArgument

                End If


                '*******************************************************************************************************

                If Command.ToLower.StartsWith("file") Then

                    gCommandLine_File = True

                    Dim TheRestOfTheFileArgument = Command.Remove(0, 4).Trim

                    Dim Filename As String = TheRestOfTheFileArgument.Replace("""", "").Trim

                    Try

                        If Filename = String.Empty Then

                            WarningInCommandLine &= "the file switch was set but no filename was provided; switch will be ignored" & vbCrLf
                            gCommandLine_File = False

                        Else

                            If Not File.Exists(Filename) Then

                                ErrorInCommandLine &= "file " & Filename & " not found" & vbCrLf

                            Else

                                Dim Extention As String = Path.GetExtension(Filename).ToLower.Replace(".", "")

                                Dim ValidExtentions(MyDictionaryOfExtentionsAndMimeTypes.Count - 1) As String

                                Dim i As Integer = 0
                                For Each entry In MyDictionaryOfExtentionsAndMimeTypes
                                    ValidExtentions(i) = entry.Key.ToString
                                    i += 1
                                Next

                                If Not ValidExtentions.Contains(Extention.ToLower) Then

                                    ErrorInCommandLine &= "files with an extension of ." & Extention.ToLower & " are not supported. Supported extensions are:"
                                    For Each Extention In ValidExtentions
                                        ErrorInCommandLine &= " ." & Extention
                                    Next
                                    ErrorInCommandLine &= vbCrLf

                                Else

                                    If Extention.ToLower = "txt" Then

                                        gCommandLine_File = False
                                        gCommandLine_Text = True
                                        gCommandLine_Text_Value = My.Computer.FileSystem.ReadAllText(Filename)

                                    Else

                                        gCommandLine_File_Value = Filename

                                    End If

                                End If

                            End If

                        End If

                    Catch ex As Exception

                    End Try

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("help") Then

                    gCommandLine_Help = True

                    If Command.Length > 4 Then
                        WarningInCommandLine &= "unexpected information found directly after the -help switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("inventory") Then

                    gCommandLine_Inventory = True

                    If Command.Length > 9 Then
                        WarningInCommandLine &= "unexpected information found directly after the -inventory switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************
                If Command.ToLower.StartsWith("ip") Then

                    gCommandLine_IP = True

                    Dim TheRestOfTheIPArgument = Command.Remove(0, 2).Trim

                    If TheRestOfTheIPArgument = String.Empty Then

                        WarningInCommandLine &= "the IP switch was set but no IP addresses were provided; switch will be ignored" & vbCrLf
                        gCommandLine_IP = False

                    Else

                        Dim IPAddressValues() As String = TheRestOfTheIPArgument.Split(" ")

                        For Each IPValue In IPAddressValues

                            IPValue = IPValue.Trim

                            If IPValue > String.Empty Then

                                If IPValue.Count(Function(c As Char) c = "."c) = 3 Then ' ensure IP address has 3 "."

                                    Dim ipObject As IPAddress = Nothing

                                    If IPAddress.TryParse(IPValue, ipObject) Then

                                        gCommandLine_IP_Addresses.Add(ipObject)

                                    Else

                                        ErrorInCommandLine &= "IP address " & IPValue & " is invalid" & vbCrLf

                                    End If

                                Else

                                    ErrorInCommandLine &= "IP address " & IPValue & " is invalid" & vbCrLf

                                End If

                            End If

                        Next

                        'check that the address is found

                        Dim IPAddressFound As Boolean
                        Dim AllAddressesHaveBeenFound As Boolean = True

                        For Each IPAddress In gCommandLine_IP_Addresses

                            IPAddressFound = False

                            For Each Device In Devices
                                If IPAddress.ToString = Device.IPAddress.ToString Then
                                    IPAddressFound = True
                                    Exit For
                                End If
                            Next

                            If IPAddressFound Then
                            Else
                                AllAddressesHaveBeenFound = False
                            End If

                        Next

                        'If all addresses have not been found re-inventory the google devices and try one last time 

                        If AllAddressesHaveBeenFound Then
                        Else

                            If DebugIsOn Then Console.WriteLine("repeated attempt at inventory")

                            InventoryGoogleDevices() ' reinventory Google devices to double check that it is not there

                            For Each IPAddress In gCommandLine_IP_Addresses

                                IPAddressFound = False

                                For Each Device In Devices
                                    If IPAddress.ToString = Device.IPAddress.ToString Then
                                        IPAddressFound = True
                                        Exit For
                                    End If
                                Next

                                If IPAddressFound Then
                                Else
                                    ErrorInCommandLine &= "cannot cast to IP address " & IPAddress.ToString & vbCrLf   'strike 2 your out
                                End If

                            Next

                        End If

                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("mute") Then

                    gCommandLine_Mute = True

                    If Command.Length > 4 Then
                        WarningInCommandLine &= "unexpected information found directly after the -mute switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("pause") Then

                    gCommandLine_Pause = True

                    If Command.Length > 5 Then
                        WarningInCommandLine &= "unexpected information found directly after the -pause switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("port") Then

                    gCommandLine_Port = True

                    Dim TheRestOfThePortArgument = Command.Remove(0, 4).Trim

                    If TheRestOfThePortArgument = String.Empty Then

                        WarningInCommandLine &= "the port switch was set but no port number was provided; switch will be ignored" & vbCrLf
                        gCommandLine_Port = False

                    Else

                        Try

                            gCommandLine_Port_Value = CType(TheRestOfThePortArgument, Integer)

                            If (gCommandLine_Port_Value <> TheRestOfThePortArgument) OrElse (gCommandLine_Port_Value < 1025) OrElse (gCommandLine_Port_Value > 65535) Then
                                ErrorInCommandLine &= "port value enterend is not a whole number between 1025 and 65535 (inclusive). "
                                ErrorInCommandLine &= "for more information, please see https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers" & vbCrLf
                            End If

                        Catch ex As Exception
                            ErrorInCommandLine &= "port value entered is not a number" & vbCrLf
                        End Try

                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("random") Then

                    gCommandLine_Random = True

                    If Command.Length > 6 Then
                        WarningInCommandLine &= "unexpected information found directly after the -random switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("text") Then

                    gCommandLine_Text = True
                    gCommandLine_Text_Value = Command.Remove(0, 4).Replace("""", "").Trim

                    If gCommandLine_Text_Value = String.Empty Then
                        WarningInCommandLine &= "the text switch was set but no text was provided; switch will be ignored" & vbCrLf
                        gCommandLine_Text = False
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("unmute") Then

                    gCommandLine_Unmute = True

                    If Command.Length > 6 Then
                        WarningInCommandLine &= "unexpected information found directly after the -unmute switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("url") Then

                    gCommandLine_URL = True
                    Dim URL_String As String = Command.Remove(0, 3).Replace("""", "").Trim

                    If URL_String = String.Empty Then

                        WarningInCommandLine &= "url value missing; switch will be ignored" & vbCrLf
                        gCommandLine_URL = False

                    Else

                        If URL_String.ToLower.EndsWith(".mp3") OrElse URL_String.ToLower.EndsWith(".mp4") OrElse URL_String.ToLower.EndsWith(".wav") Then ' OrElse URL_String.ToLower.EndsWith(".flac") Then

                            Dim ValidatedURI As Uri = Nothing
                            Dim GoodURL As Boolean = Uri.TryCreate(URL_String, UriKind.Absolute, ValidatedURI)

                            'add code to add http: and https:

                            If GoodURL Then

                                gCommandLine_URL_Value = ValidatedURI

                            Else

                                ErrorInCommandLine &= "url value entered is invalid" & vbCrLf
                                gCommandLine_URL = False

                            End If

                        Else

                            WarningInCommandLine &= "url value must end with: "".mp3"", "".mp4"", or "".wav""; switch will be ignored" & vbCrLf
                            gCommandLine_URL = False

                        End If

                        GoTo NextArgument

                    End If

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("voice") Then

                    If DefaultVoiceSetOK Then

                        gCommandLine_Voice = True
                        gCommandLine_Voice_Value = Command.Remove(0, 5).Replace("""", "").Trim

                        If gCommandLine_Voice_Value = String.Empty Then

                            WarningInCommandLine &= "voice value missing; default voice will be used as required" & vbCrLf

                        Else

                            If SetDesiredVoice(gCommandLine_Voice_Value) Then
                            Else
                                WarningInCommandLine &= "voice value entered does not match any available Windows voice values; will use default voice" & vbCrLf
                            End If

                        End If

                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("volume") Then

                    gCommandLine_Volume = True

                    Dim TheRestOfTheVolumeArgument = Command.Remove(0, 6).Trim

                    TheRestOfTheVolumeArgument = TheRestOfTheVolumeArgument.Replace("%", "").Trim
                    TheRestOfTheVolumeArgument = TheRestOfTheVolumeArgument.ToLower.Replace("percent", "").Trim

                    If TheRestOfTheVolumeArgument = String.Empty Then

                        WarningInCommandLine &= "volume value missing from command line, current volume setting(s) will be used" & vbCrLf
                        gCommandLine_Volume = False

                    Else

                        Try

                            gCommandLine_Volume_Value = CType(TheRestOfTheVolumeArgument, Integer)

                            If (gCommandLine_Volume_Value <> TheRestOfTheVolumeArgument) OrElse (gCommandLine_Volume_Value < 0) OrElse (gCommandLine_Volume_Value > 100) Then
                                ErrorInCommandLine &= "colume value enterend is not a whole number between 0 and 100 (inclusive)"
                            End If

                        Catch ex As Exception

                            ErrorInCommandLine &= "volume value entered is not a number" & vbCrLf

                        End Try

                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                If Command.ToLower.StartsWith("website") Then

                    gCommandLine_Website = True

                    If Command.Length > 7 Then
                        WarningInCommandLine &= "unexpected information found directly after the -website switch; this information will be ignored" & vbCrLf
                    End If

                    GoTo NextArgument

                End If

                '*******************************************************************************************************

                ErrorInCommandLine &= "invalid switch: -" & Command & vbCrLf

                '*******************************************************************************************************
NextArgument:

            Next

            If gCommandLine_IP Then
            Else
                'No device names or IP Addresses indetified, so message will be broadcast to all devices by default
                gCommandLine_IP = True
                For Each Device In Devices
                    gCommandLine_IP_Addresses.Add(Device.IPAddress)
                Next
            End If

            If gCommandLine_Cancel Then
                If gCommandLine_Text Then
                    ErrorInCommandLine &= "the -cancel and -text switches can not be used together" & vbCrLf
                End If
                If gCommandLine_File Then
                    ErrorInCommandLine &= "the -cancel and -file switches can not be used together" & vbCrLf
                End If
                If gCommandLine_URL Then
                    ErrorInCommandLine &= "the -cancel and -url switches can not be used together" & vbCrLf
                End If
                If gCommandLine_Dir Then
                    ErrorInCommandLine &= "the -cancel and -dir switches can not be used together" & vbCrLf
                End If
            End If

            If gCommandLine_Dir And gCommandLine_Background Then
                ErrorInCommandLine &= "the -dir and -background switches cannot be used together" & vbCrLf
            End If

            Dim CastingOptions As Integer = 0

            If gCommandLine_Text Then CastingOptions += 1
            If gCommandLine_File Then CastingOptions += 1
            If gCommandLine_URL Then CastingOptions += 1
            If gCommandLine_Dir Then CastingOptions += 1
            If CastingOptions > 1 Then
                ErrorInCommandLine &= "only one of the following switches can be used at the same time: -text, -file, -url, or -dir" & vbCrLf
            End If

            If gCommandLine_Mute AndAlso gCommandLine_Unmute Then
                WarningInCommandLine &= "the -mute and -unmute switches can not be used at the same time, -unmute will be ignored" & vbCrLf
                gCommandLine_Unmute = False
            End If

            Dim CountBeforeDuplicateCheck As Integer = gCommandLine_IP_Addresses.Count
            gCommandLine_IP_Addresses = gCommandLine_IP_Addresses.Distinct().ToList()

            If CountBeforeDuplicateCheck = gCommandLine_IP_Addresses.Count Then
            Else
                WarningInCommandLine &= "one or more Google devices were duplicated in the command line" & vbCrLf
            End If

            If gCommandLine_Text Then
                If DefaultVoiceSetOK Then
                Else
                    ErrorInCommandLine &= "Windows does not appear to have any installed voices as needed to cast a message based on text" & vbCrLf
                End If
            End If

            If gCommandLine_Voice Then
                If gCommandLine_Text Then
                Else
                    WarningInCommandLine &= "-voice switch was used without the -text switch, -voice switch will be ignored" & vbCrLf
                End If
            End If

            If gCommandLine_Port Then
                If gCommandLine_Text OrElse gCommandLine_File Then
                Else
                    WarningInCommandLine &= "-port switch was used without the -text or -file switch, -port switch will be ignored" & vbCrLf
                    gCommandLine_Port = False
                End If
            End If

            If gCommandLine_About OrElse gCommandLine_Help OrElse gCommandLine_Cancel OrElse gCommandLine_Inventory OrElse gCommandLine_Mute OrElse gCommandLine_Unmute OrElse gCommandLine_Volume Then
            Else
                If gCommandLine_Text OrElse gCommandLine_File OrElse gCommandLine_URL OrElse gCommandLine_Dir Then
                Else
                    If ErrorInCommandLine = String.Empty Then
                        WarningInCommandLine &= "there was no -text, -file, -url, or -dir switch, so there is nothing to cast" & vbCrLf
                    End If
                End If
            End If

            If gCommandLine_Pause AndAlso (Commands.Count = 2) Then
                gCommandLine_Help = True
            End If

            If gCommandLine_Random Then
                If gCommandLine_Dir Then
                Else
                    WarningInCommandLine &= "-random switch was used without the -dir switch, -random switch will be ignored" & vbCrLf
                    gCommandLine_Port = False
                End If
            End If

        Catch ex As Exception

            ErrorInCommandLine &= ex.ToString

        End Try

    End Sub

    Private Function TweakCommandLineChangingSwitchesToCHR255(ByVal CommandLine As String) As String

        Dim ReturnValue As String = String.Empty

        Const Quote As String = """"
        Const Hyphen As String = "-"

        Dim QuoteON As Boolean = False
        Dim SplitCharcter As String = Chr(255).ToString

        For Each Character As Char In CommandLine

            If Character = Quote Then
                QuoteON = (Not QuoteON)
            End If

            If QuoteON Then
                ReturnValue &= Character
            Else
                If Character = Hyphen Then
                    ReturnValue &= SplitCharcter
                Else
                    ReturnValue &= Character
                End If
            End If

        Next

        Return ReturnValue

    End Function

    Private Sub ProcessInventoryCommand()

        Dim CurrentWidth As Integer = Console.WindowWidth
        Dim CurrentHeight As Integer = Console.WindowHeight

        If CurrentWidth < 100 Then Console.SetWindowSize(100, CurrentHeight)

        'Header
        Console_WriteLineInColour(" ", ConsoleColor.Cyan)
        Console_WriteLineInColour("Inventory", ConsoleColor.Cyan)

        'Inventory Google devices

        Console_WriteLineInColour(" ", ConsoleColor.Cyan)

        Dim Message = ConvertToFixedStringLength("Device", 30) & " " &
                      ConvertToFixedStringLength("Type", 25) & " " &
                      ConvertToFixedStringLength("IP address", 20) & " " &
                      ConvertToFixedStringLength("Volume", 8) & " " &
                      "Mute"

        Console_WriteLineInColour(Message, ConsoleColor.Cyan)

        For Each Device In Devices

            Dim DeviceVolumeString As String = Device.Volume.ToString & "%"

            Message = ConvertToFixedStringLength(Device.FriendlyName, 30) & " " &
                      ConvertToFixedStringLength(Device.DeviceName, 25) & " " &
                      ConvertToFixedStringLength(Device.IPAddress.ToString, 20) & " " &
                      ConvertToFixedStringLength(DeviceVolumeString, 8) & " " &
                      IIf(Device.Muted, "Muted", "Unmuted")

            Console_WriteLineInColour(Message, ConsoleColor.White)
        Next

        'Inventory installed voices
        Console_WriteLineInColour(" ", ConsoleColor.Cyan)
        Dim DetailLine As String = String.Empty
        DetailLine &= ConvertToFixedStringLength("Windows voice", 30) & " "
        DetailLine &= ConvertToFixedStringLength("Culture", 25) & " "
        DetailLine &= ConvertToFixedStringLength("Gender", 20) & " "
        DetailLine &= ConvertToFixedStringLength("Age", 10)
        Console_WriteLineInColour(DetailLine, ConsoleColor.Cyan)

        For Each Voice As String In ReportInstalledVoices()
            Console_WriteLineInColour(Voice, ConsoleColor.White)
        Next

    End Sub

    Private Sub ProcessCastText(ByVal TextToCast As String)

        Console_WriteLineInColour("Casting """ & TextToCast & """", ConsoleColor.White)

        Try

            TempFileInUse = GenerateATempFileName(".wav")

            gPlayThroughPCSpeakers = gCommandLine_IP_Addresses.Contains(gHostComputerIPAddress)

            'create the file to be spoken in parallel and start the web server as required

            Parallel.Invoke(
               Sub()
                   CreateASpeechFile(TextToCast, TempFileInUse)
               End Sub,
               Sub()

                   'the following determines if the web sever needs to be started 
                   'and as well if a delay for a coordinate start of playing he message is also needed

                   If gPlayThroughPCSpeakers Then
                       If gCommandLine_IP_Addresses.Count = 1 Then
                           ' output is only to pc speakers so websever is not required
                       Else
                           StartEmbeddedWebServer(System.IO.Path.GetTempPath(), True)
                       End If
                   Else
                       If gCommandLine_IP_Addresses.Count = 1 Then
                           StartEmbeddedWebServer(System.IO.Path.GetTempPath(), False)
                       Else
                           StartEmbeddedWebServer(System.IO.Path.GetTempPath(), True)
                       End If

                   End If

               End Sub)

            'cast to all devices in parallel
            Dim CoordinateTheStartOfStreaming As Boolean = (gCommandLine_IP_Addresses.Count > 1)

            Parallel.ForEach(gCommandLine_IP_Addresses,
                Sub(IPAddress)
                    CastAFile(IPAddress, TempFileInUse)
                End Sub)

            'wait until the casting is complete on all devices 

            If gCommandLine_Background Then

                System.Windows.Forms.Application.DoEvents()
                Threading.Thread.Sleep(1000)
                System.Windows.Forms.Application.DoEvents()

            Else

                MaxWaitTime = Now.AddMilliseconds(GetDuration(TempFileInUse)).AddMilliseconds(250)
                If CoordinateTheStartOfStreaming Then MaxWaitTime.AddMilliseconds(gSynchroMilliSeconds)

                While Now < MaxWaitTime
                    Threading.Thread.Sleep(250)
                    System.Windows.Forms.Application.DoEvents()
                End While

            End If

        Catch ex As Exception

        End Try

        CastingComplete()

    End Sub

    Private Sub CastingComplete()

        Console_WriteLineInColour("Casting complete", ConsoleColor.White)

    End Sub

    Private Sub ProcessCastDirectory(ByVal Directory As String, ByVal AllPathAndFilenames As String())

        AllPathAndFilenames = CleanUpPlayList(AllPathAndFilenames)

        If AllPathAndFilenames IsNot Nothing Then

            If gCommandLine_Random Then

                Console_WriteLineInColour("Casting directory """ & Directory & """ in random order; directory contains " & AllPathAndFilenames.Count.ToString("N0") & " files which can be cast" & vbCrLf, ConsoleColor.White)

                Dim RandomNumber As Integer

                While AllPathAndFilenames IsNot Nothing

                    RandomNumber = CInt(Math.Ceiling(Rnd() * AllPathAndFilenames.Count())) - 1

                    ProcessCastFile(AllPathAndFilenames(RandomNumber))

                    AllPathAndFilenames(RandomNumber) = String.Empty
                    AllPathAndFilenames = CleanUpPlayList(AllPathAndFilenames)

                    MemoryManagement.FlushMemory()

                End While

            Else

                Console_WriteLineInColour("Casting directory """ & Directory & """ in alphabetical order; directory contains " & AllPathAndFilenames.Count.ToString("N0") & " files which can be cast" & vbCrLf, ConsoleColor.White)

                For Each filename In AllPathAndFilenames

                    If filename.StartsWith("~cast~") Then
                    Else
                        ProcessCastFile(filename)
                    End If

                    MemoryManagement.FlushMemory()

                Next

            End If

        End If

        CastingComplete()

    End Sub

    Function CleanUpPlayList(ByVal Source As String()) As String()

        Static Dim FirstPass As Boolean = True

        Dim ReturnValue(Source.Count) As String

        Dim Index As Integer = 0

        Try

            If FirstPass Then

                FirstPass = False

                'remove invalid entries

                For Each entry In Source

                    If (entry.Length = 0) OrElse entry.StartsWith("~cast~") Then
                    Else

                        Dim entrylowercase = entry.ToLower
                        If entrylowercase.EndsWith(".mp3") OrElse entrylowercase.EndsWith(".mp4") OrElse entrylowercase.EndsWith(".wav") Then
                            ReturnValue(Index) = entry
                            Index += 1

                        End If

                    End If

                Next

            Else

                'remove blank entries
                For Each entry In Source

                    If entry.Length = 0 Then
                    Else
                        ReturnValue(Index) = entry
                        Index += 1
                    End If

                Next

            End If

        Catch ex As Exception

            Index = 0

        End Try

        If Index = 0 Then
            ReturnValue = Nothing
        Else
            ReDim Preserve ReturnValue(Index - 1)
        End If

        Return ReturnValue

    End Function

    Private Sub ProcessCastFile(ByVal PathAndFilename As String)

        Static counter As Integer = 0
        counter += 1

        If PathAndFilename Is Nothing Then Exit Sub

        Console_WriteLineInColour("Casting - " & counter & " - """ & PathAndFilename & """", ConsoleColor.White)

        gPlayThroughPCSpeakers = gCommandLine_IP_Addresses.Contains(gHostComputerIPAddress)

        Try

            Dim PathName As String = Path.GetDirectoryName(PathAndFilename) & "\"
            Dim Filename As String = Path.GetFileName(PathAndFilename)

            'cast to all devices in parallel
            Dim CoordinateTheStartOfStreaming As Boolean = (gCommandLine_IP_Addresses.Count > 1)

            StartEmbeddedWebServer(System.IO.Path.GetTempPath(), CoordinateTheStartOfStreaming)

            Parallel.ForEach(gCommandLine_IP_Addresses,
                Sub(IPAddress)
                    CastAFile(IPAddress, PathAndFilename)
                End Sub)

            'If DebugIsOn Then MaxWaitTime = Now.AddSeconds(2)  ' use to make tesing quicker

            If gCommandLine_Background Then

                System.Windows.Forms.Application.DoEvents()
                Threading.Thread.Sleep(1000)
                System.Windows.Forms.Application.DoEvents()

            Else

                MaxWaitTime = Now.AddMilliseconds(GetDuration(PathAndFilename)).AddMilliseconds(250)
                If CoordinateTheStartOfStreaming Then MaxWaitTime.AddMilliseconds(gSynchroMilliSeconds)

                While Now < MaxWaitTime
                    Threading.Thread.Sleep(250)
                    System.Windows.Forms.Application.DoEvents()
                End While

            End If

            StopEmbeddedWebServer()

        Catch ex As Exception
            ex = ex
        End Try

        CleanUpTempFiles()

        If gCommandLine_Dir Then
        Else
            CastingComplete()
        End If

    End Sub

    Friend Sub Update_CastingFinishedOnThisManyDevices(ByVal Reset As Boolean, Optional ByVal SetValue As Integer = 1)

        SyncLock synclockobj

            If Reset Then
                CastingFinishedOnThisManyDevices = SetValue
            Else
                CastingFinishedOnThisManyDevices += SetValue
            End If

        End SyncLock

    End Sub

    Private Sub ProcessCastUrl(ByVal iURI As Uri)

        Console_WriteLineInColour("Casting " & iURI.ToString, ConsoleColor.White)

        gPlayThroughPCSpeakers = gCommandLine_IP_Addresses.Contains(gHostComputerIPAddress)

        Try

            Dim CoordinateTheStartOfStreaming As Boolean = (gCommandLine_IP_Addresses.Count > 1)

            'cast to all devices in parallel

            Parallel.ForEach(gCommandLine_IP_Addresses,
                Sub(IPAddress)
                    CastAURI(IPAddress, iURI)
                End Sub)

            If gCommandLine_Background Then
                System.Windows.Forms.Application.DoEvents()
                Threading.Thread.Sleep(1000)
                System.Windows.Forms.Application.DoEvents()
            Else
                While (CastingFinishedOnThisManyDevices < gCommandLine_IP_Addresses.Count)
                    Threading.Thread.Sleep(250)
                    System.Windows.Forms.Application.DoEvents()
                End While
            End If

        Catch ex As Exception

        End Try

        CastingComplete()

    End Sub

    Private Sub ProcessCastStop(Optional ByVal ShowDisplay As Boolean = True)

        If ShowDisplay Then Console_WriteLineInColour("Cancelling casting", ConsoleColor.White)

        Try

            Parallel.ForEach(gCommandLine_IP_Addresses,
                Sub(IPAddress)
                    CastStop(IPAddress)
                End Sub)

        Catch ex As Exception
        End Try

    End Sub

    Private Sub ProcessMuteChange(ByVal Mute As Boolean)
        Try

            Parallel.ForEach(gCommandLine_IP_Addresses,
                Sub(IPAddress)
                    CastMuteChange(IPAddress, Mute)
                    UpdatedDeviceTableToReflectNewMuteSetting(IPAddress, Mute)  'muted and volume are seperate and should be managed as such
                End Sub)

        Catch ex As Exception
        End Try

    End Sub

    Private Sub UpdatedDeviceTableToReflectNewMuteSetting(ByVal iIPAddress As IPAddress, ByVal iMute As Boolean)

        For x = 0 To Devices.Count - 1

            If Devices(x).IPAddress.ToString = iIPAddress.ToString Then
                Devices(x).Muted = iMute
                Exit For
            End If

        Next

    End Sub

    Private Sub ProcessVolumeChange(ByVal Volume As Integer, Optional ByVal ShowDisplay As Boolean = False)

        Try

            Parallel.ForEach(gCommandLine_IP_Addresses,
                Sub(IPAddress)

                    If CastVolumeChange(IPAddress, Volume, ShowDisplay) Then
                        UpdatedDeviceTableToReflectNewVolume(IPAddress, Volume)
                    End If

                End Sub)

        Catch ex As Exception
        End Try

    End Sub

    Private Sub UpdatedDeviceTableToReflectNewVolume(ByVal iIPAddress As IPAddress, ByVal iVolume As Integer)

        For x = 0 To Devices.Count - 1

            If Devices(x).IPAddress.ToString = iIPAddress.ToString Then
                Devices(x).Volume = iVolume
                Exit For
            End If

        Next

    End Sub

    Friend Function GetDeviceTableCurrentVolume(ByVal iIPAddress As IPAddress) As Integer

        Dim ReturnValue As Integer = -1

        For x = 0 To Devices.Count - 1

            If Devices(x).IPAddress.ToString = iIPAddress.ToString Then
                ReturnValue = Devices(x).Volume
                Exit For
            End If

        Next

        Return ReturnValue

    End Function


    Private Sub ShowAbout()

        Dim CurrentWidth As Integer = Console.WindowWidth
        Dim CurrentHeight As Integer = Console.WindowHeight

        If CurrentWidth < 100 Then Console.SetWindowSize(100, CurrentHeight)

        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast v1.7 - About", ConsoleColor.White)
        Console_WriteLineInColour("Copyright 2021, Rob Latour", ConsoleColor.White)
        Console_WriteLineInColour("Cast is licensed under the following license:", ConsoleColor.Gray)
        Console_WriteLineInColour("MIT)", ConsoleColor.Gray)
        Console_WriteLineInColour("https://opensource.org/licenses/MIT", ConsoleColor.Gray)
        Console_WriteLineInColour("Cast open source: https://github.com/roblatour/cast", ConsoleColor.Gray)
        Console_WriteLineInColour("Cast author web reference: https://www.rlatour.com/cast", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast makes use of SharpCast, Copyright © Jeremy Pepiot, 2014", ConsoleColor.Cyan)
        Console_WriteLineInColour("SharpCast web reference: https://github.com/jpepiot/SharpCast", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast makes use of Newtonsoft by James Newton - King", ConsoleColor.Cyan)
        Console_WriteLineInColour("Newtonsoft is licensed under the following license:", ConsoleColor.Gray)
        Console_WriteLineInColour("https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md", ConsoleColor.Gray)
        Console_WriteLineInColour("Newtonsoft web reference: https://www.newtonsoft.com/json", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast makes use of NAudio by Mark Heath", ConsoleColor.Cyan)
        Console_WriteLineInColour("NAudio is licensed under the following license:", ConsoleColor.Gray)
        Console_WriteLineInColour("https://msdn.microsoft.com/en-us/library/ff647676.aspx", ConsoleColor.Gray)
        Console_WriteLineInColour("NAudio web reference: https://github.com/naudio/NAudio", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast makes use of Zeroconf by Oren Novotny", ConsoleColor.Cyan)
        Console_WriteLineInColour("Zeroconf is licensed under the following license:", ConsoleColor.Gray)
        Console_WriteLineInColour("https://opensource.org/licenses/ms-pl", ConsoleColor.Gray)
        Console_WriteLineInColour("Zeroconf web reference: https://github.com/onovotny/Zeroconf", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast makes use of the following Reactive components:", ConsoleColor.Cyan)
        Console_WriteLineInColour(" System.Reactive", ConsoleColor.Gray)
        Console_WriteLineInColour(" System.Reactive.Core", ConsoleColor.Gray)
        Console_WriteLineInColour(" System.Reactive.Interfaces", ConsoleColor.Gray)
        Console_WriteLineInColour(" System.Reactive.Linq", ConsoleColor.Gray)
        Console_WriteLineInColour(" System.Reactive.PlatformServices", ConsoleColor.Gray)
        Console_WriteLineInColour(" System.Reactive.WindowsThreading", ConsoleColor.Gray)
        Console_WriteLineInColour("The above Reactive components are licensed under the following license:", ConsoleColor.Gray)
        Console_WriteLineInColour("http://go.microsoft.com/fwlink/?LinkID=261272", ConsoleColor.Gray)
        Console_WriteLineInColour("Reactive web reference: http://go.microsoft.com/fwlink/?LinkId=261273", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast makes use of Google Protocol Buffers", ConsoleColor.Cyan)
        Console_WriteLineInColour("Google Protocol Buffers web reference: https://developers.google.com/protocol-buffers/", ConsoleColor.Gray)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast is shareware.", ConsoleColor.White)
        Console_WriteLineInColour("A donation through https://www.rlatour.com/cast/donate will be truly appreciated.", ConsoleColor.White)

        Console.ForegroundColor = ConsoleColor.Gray

    End Sub


    Private Sub ShowHelp()

        Dim CurrentWidth As Integer = Console.WindowWidth
        Dim CurrentHeight As Integer = Console.WindowHeight

        If CurrentWidth < 100 Then Console.SetWindowSize(100, CurrentHeight)

        Dim StartingColour As ConsoleColor = Console.ForegroundColor

        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast v1.7 - Help", ConsoleColor.White)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Switches:", ConsoleColor.White)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -about       show more information about cast")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -background  exits cast once the text, file or url has been cast")
        Console_WriteLineInColour("              does not wait until playing finishes before exiting")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -broadcast   cast to all devices (default)")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -cancel      cancel casting")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -debug       include diagnostic detail in the console displays")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -dir         followed by the full directory name of a directory to cast")
        Console_WriteLineInColour("              will cast all .mp3 .mp4 and .wav files in the directory and its sub-directories")
        Console_WriteLineInColour("              files will be cast in alphabetical order within each directory")
        Console_WriteLineInColour("              unless the -random switch is also used")
        Console_WriteLineInColour("              directory name may be surronded by double quotes ("")")
        Console_WriteLineInColour("              (required if the filename contains one or more hyphens)")
        Console_WriteLineInColour("              the -dir and -background switches cannot be used at the same time")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -device      followed by the name of device(s) to cast to")
        Console_WriteLineInColour("              use the -inventory switch to determine available device names")
        Console_WriteLineInColour("              device names must be surrounded by double quotes ("") when there are")
        Console_WriteLineInColour("              multiple device names or if a device name contains one or more hyphens")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -file        followed by the full filename to cast")
        Console_WriteLineInColour("              filename may be surronded by double quotes ("")")
        Console_WriteLineInColour("              (required if the filename contains one or more hyphens)")
        Console_WriteLineInColour("              supported file types are: .txt .mp3 .mp4 .wav")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -help        shows this help")
        Console_WriteLineInColour("              help is also shown if not switches are used")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -inventory   show an inventory of devices and available voices")
        Console_WriteLineInColour("              not all voices on your system may be available")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -ip          followed by the internal Lan IP Address(es) to cast to")
        Console_WriteLineInColour("              use the -inventory switch to determine available IP Addresses")
        Console_WriteLineInColour("              IP Addresses must be seperated by spaces")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -mute        followed by the device(s) to mute")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -pause       prompt to 'Press enter to continue' when processing finishes")
        Console_WriteLineInColour("              if the -pause switch is used without any other switch then")
        Console_WriteLineInColour("              the help will also be displayed")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -port        followed by the port to cast through (default 55123)")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -random      may be used with the -dir switch to cast files in a random order")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -text        follewed by the text to cast (words to be spoken)")
        Console_WriteLineInColour("              text may be surronded by double quotes ("")")
        Console_WriteLineInColour("              (required if text contains one or more hyphens)")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -unmute      followed by the device(s) to unmute")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -url         followed by the internet address of the file to cast")
        Console_WriteLineInColour("              should start with either http:// or https://")
        Console_WriteLineInColour("              url may be surronded by double quotes ("")")
        Console_WriteLineInColour("              (required if the url value contains one or more hyphens)")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -voice       followed by the name of voice to be used to speak text")
        Console_WriteLineInColour("              use the -inventory switch to determine available voices")
        Console_WriteLineInColour("              voice name may be surrounded by double quotes ("")")
        Console_WriteLineInColour("              (required if voice name contains one or more hyphens)")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -volume      followed by the desired volume level")
        Console_WriteLineInColour("              sets the volume to the desired volume level")
        Console_WriteLineInColour("              valid values are any whole number between 0 and 100 inclusive")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" -website     visit the cast website")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("  Additional commands available when casting:")
        Console_WriteLineInColour("  0, 1, 2, ... 9   set volume to 0%, 10%, 20%, ... 90%")
        Console_WriteLineInColour("  Up arrow         set volume up by 1")
        Console_WriteLineInColour("  Down arrow       set volume down by 1")
        Console_WriteLineInColour("  M                mute")
        Console_WriteLineInColour("  U                unmute")
        Console_WriteLineInColour("  X                cancel casting and exit")
        Console_WriteLineInColour("  S                skip the current file (only with -dir switch)")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Examples:", ConsoleColor.White)
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour(" cast -inventory")
        Console_WriteLineInColour(" cast -inventory -pause")
        Console_WriteLineInColour(" cast -text This is a test")
        Console_WriteLineInColour(" cast -text ""This test is ready-to-go with hyphens""")
        Console_WriteLineInColour(" cast -ip 192.168.0.20 -text This is a test using only one ip address")
        Console_WriteLineInColour(" cast -ip 192.168.0.20 192.168.0.22 -text This is a test using multiple ip addresses")
        Console_WriteLineInColour(" cast -device ""Office home"" -text This is a test using one device name")
        Console_WriteLineInColour(" cast -device ""Office home"" ""Basement mini"" -text This is a test using multiple device names")
        Console_WriteLineInColour(" cast -text This test is a whisper -volume 5")
        Console_WriteLineInColour(" cast -text This test is a shout -volume 100")
        Console_WriteLineInColour(" cast -text This is a test using an alternative port -port 9696")
        Console_WriteLineInColour(" cast -text This is a test using a specific voice -voice Microsoft David Desktop")
        Console_WriteLineInColour(" cast -file C:\Users\Rob Latour\Music\Eagles\Hotel California.mp3")
        Console_WriteLineInColour(" cast -file C:\Users\Rob Latour\Music\Eagles\Hotel California.mp3 -background")
        Console_WriteLineInColour(" cast -dir C:\Users\Rob Latour\Music\Eagles -random")
        Console_WriteLineInColour(" cast -url ""https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3""")
        Console_WriteLineInColour(" cast -mute -device ""Office home""")
        Console_WriteLineInColour(" cast -volume 45")
        Console_WriteLineInColour(" cast -cancel")
        Console_WriteLineInColour(" cast -website")
        Console_WriteLineInColour(" ")
        Console_WriteLineInColour("Cast v1.7 Copyright © Rob Latour, 2019, License: MIT", ConsoleColor.White)
        Console_WriteLineInColour("          Open source: https://github.com/roblatour/cast", ConsoleColor.White)

        Console.ForegroundColor = StartingColour

    End Sub

    Private Sub OpenWebSite()

        Process.Start(gWebSite)

    End Sub

End Module

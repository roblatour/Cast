Imports System.Collections.Generic
Imports System.Linq
Imports System.Net
Imports System.Threading.Tasks
Imports Zeroconf
Imports SharpCast
Imports System.IO
Imports System.Text.RegularExpressions

Module ModMyLocator

    Friend Sub InventoryGoogleDevices()

        Static Dim FirstRequest As Boolean = True

        'Sometimes the inventory google devices does not return all devices
        'If this is suspected to be the case, this routine can be called a second time
        'and the list that was initially created will be augmented if possible

        Dim task As Task

        If FirstRequest Then
            FirstRequest = False
            task = New Task(AddressOf FindGoogleDevices_FirstAttempt_Async)
        Else
            task = New Task(AddressOf FindGoogleDevices_SecondAttempt_Async)
        End If

        task.Start()
        task.Wait()

    End Sub

    Friend Async Sub FindGoogleDevices_FirstAttempt_Async()

        Dim task As Task(Of List(Of Device)) = FindGoogleDevices_FirstAttempt()
        task.Wait()
        Devices = Await task

    End Sub

    Friend Async Sub FindGoogleDevices_SecondAttempt_Async()

        Dim task As Task(Of List(Of Device)) = FindGoogleDevices_SecondAttempt()
        task.Wait()
        Devices = Await task

    End Sub

    Friend Async Function FindGoogleDevices_FirstAttempt() As Task(Of List(Of Device))

        Dim LocatedDevices As New List(Of Device)

        Try

            Const Protocal As String = "_googlecast._tcp.local."

                Dim DeviceFriendlyName As String
            Dim DeviceType As String
            Dim DeviceIPAddress As IPAddress
            Dim Device As Device
            Dim PCVolume As Double
            Dim PCIsMuted As Boolean

            'add the PC Speakers to start

            DeviceFriendlyName = gHostComputerName

            DeviceIPAddress = gHostComputerIPAddress
            PCVolume = GetCurrentPCVolume()
            If gNoDefaultSpeaker Then
                DeviceType = "PC speakers (unavailable)"
                PCIsMuted = True
            Else
                DeviceType = "PC speakers"
                PCIsMuted = GetCurrentPCMuteSetting()
            End If

            Device = New Device(DeviceFriendlyName, DeviceType, DeviceIPAddress, PCVolume, PCIsMuted)
            LocatedDevices.Add(Device)

            Dim TimeSpan2Seconds As TimeSpan = New TimeSpan(0, 0, 2) ' changed from 3 to 2 in v1.9

            If DebugIsOn Then Console.WriteLine("Start inventory devices - first attempt")
            If DebugIsOn Then Console.WriteLine("Wait time = " & TimeSpan2Seconds.ToString)

            Dim GoogleDevices As IReadOnlyList(Of IZeroconfHost) = Await ZeroconfResolver.ResolveAsync(Protocal, TimeSpan2Seconds, 5, 1000)

            If DebugIsOn Then Console.WriteLine("Google device count = " & GoogleDevices.Count)

            For Each GoogleDevice As IZeroconfHost In GoogleDevices

                DeviceFriendlyName = GetFriendlyDeviceName(GoogleDevice.IPAddress.ToString)

                DeviceType = GoogleDevice.DisplayName.Replace("Google-", "")
                DeviceType = DeviceType.Remove(DeviceType.LastIndexOf("-"))
                DeviceType = DeviceType.Replace("-", " ")

                DeviceIPAddress = IPAddress.Parse(GoogleDevice.IPAddress)

                Device = New Device(DeviceFriendlyName, DeviceType, DeviceIPAddress, 0, False)
                Device.Player.Connect()
                Device.Volume = GetCurrentDeviceVolume(Device)
                Device.Muted = GetCurrentDeviceMuteSetting(Device)

                LocatedDevices.Add(Device)

                If DebugIsOn Then Console.WriteLine(vbTab & Device.FriendlyName)

            Next

            If DebugIsOn Then Console.WriteLine("End inventory devices - first attempt")

            LocatedDevices.Sort(Function(x, y) x.FriendlyName.CompareTo(y.FriendlyName))

        Catch ex As Exception

            If DebugIsOn Then Console.WriteLine(ex.ToString)

        End Try

        Return LocatedDevices

    End Function


    Friend Async Function FindGoogleDevices_SecondAttempt() As Task(Of List(Of Device))

        Dim LocatedDevices As New List(Of Device)

        LocatedDevices = Devices

        Try

            Const Protocal As String = "_googlecast._tcp.local."

            Dim DeviceFriendlyName As String
            Dim DeviceType As String
            Dim DeviceIPAddress As IPAddress
            Dim DeviceVolume As Double
            Dim DeviceIsMuted As Boolean
            Dim Device As Device

            Dim TimeSpan4Seconds As TimeSpan = New TimeSpan(0, 0, 4)

            If DebugIsOn Then Console.WriteLine("Start inventory devices - second attempt")
            If DebugIsOn Then Console.WriteLine("Wait time = " & TimeSpan4Seconds.ToString)

            Dim GoogleDevices As IReadOnlyList(Of IZeroconfHost) = Await ZeroconfResolver.ResolveAsync(Protocal, TimeSpan4Seconds, 5, 1000)

            If DebugIsOn Then Console.WriteLine("Google device count = " & GoogleDevices.Count)

            GoogleDevices = Await ZeroconfResolver.ResolveAsync(Protocal)
            For Each GoogleDevice As IZeroconfHost In GoogleDevices

                DeviceIPAddress = IPAddress.Parse(GoogleDevice.IPAddress)

                Dim MatchFound As Boolean = False
                For Each ExistingDevice In LocatedDevices
                    If DeviceIPAddress.ToString = ExistingDevice.IPAddress.ToString Then
                        MatchFound = True
                        Exit For
                    End If
                Next

                If MatchFound Then
                    ' entry already there, do not add again
                Else
                    ' new entry found, add it

                    DeviceFriendlyName = GetFriendlyDeviceName(GoogleDevice.IPAddress.ToString)

                    DeviceType = GoogleDevice.DisplayName.Replace("Google-", "")
                    DeviceType = DeviceType.Remove(DeviceType.LastIndexOf("-"))
                    DeviceType = DeviceType.Replace("-", " ")

                    DeviceVolume = GetCurrentVolume(DeviceIPAddress)
                    DeviceIsMuted = GetCurrentMuteSetting(DeviceIPAddress)
                    Device = New Device(DeviceFriendlyName, DeviceType, DeviceIPAddress, DeviceVolume, DeviceIsMuted)

                    LocatedDevices.Add(Device)

                    If DebugIsOn Then Console.WriteLine(vbTab & Device.FriendlyName)

                End If

            Next

            If DebugIsOn Then Console.WriteLine("End inventory devices - second attempt")

            LocatedDevices.Sort(Function(x, y) x.FriendlyName.CompareTo(y.FriendlyName))

        Catch ex As Exception

            If DebugIsOn Then Console.WriteLine(ex.ToString)

        End Try

        Return LocatedDevices

    End Function

    Private Const PORT As Integer = 8008

    Friend Function GetFriendlyDeviceName(ipAddress As String) As String
        Try
            ' Create HTTP request to the Chromecast's info endpoint
            Dim url As String = $"http://{ipAddress}:{PORT}/setup/eureka_info"
            Dim request As HttpWebRequest = DirectCast(WebRequest.Create(url), HttpWebRequest)
            request.Method = "GET"
            request.Timeout = 5000 ' 5 second timeout

            ' Get response
            Using response As HttpWebResponse = DirectCast(request.GetResponse(), HttpWebResponse)
                Using reader As New StreamReader(response.GetResponseStream())
                    Dim result As String = reader.ReadToEnd()

                    ' Extract name from JSON response
                    ' Note: Using simple regex for demo. In production, use proper JSON parser
                    Dim nameMatch As Match = Regex.Match(result, """name"":""([^""]+)""")
                    If nameMatch.Success Then
                        Return nameMatch.Groups(1).Value
                    End If
                End Using
            End Using

            Return String.Empty
        Catch ex As Exception
            ' Handle any network or other errors
            Console.WriteLine($"Error getting device name: {ex.Message}")
            Return String.Empty
        End Try
    End Function

End Module

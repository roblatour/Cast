Imports System.Collections.Generic
Imports System.Linq
Imports System.Net
Imports System.Threading.Tasks
Imports Zeroconf
Imports SharpCast

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

            Dim PropertySet As String
            Dim DeviceFriendlyName As String
            Dim DeviceName As String
            Dim DeviceIPAddress As IPAddress
            Dim Device As Device
            Dim PCVolume As Double
            Dim PCIsMuted As Boolean

            'add the PC Speakers to start

            DeviceFriendlyName = gHostComputerName

            DeviceIPAddress = gHostComputerIPAddress
            PCVolume = GetCurrentPCVolume()
            If gNoDefaultSpeaker Then
                DeviceName = "PC speakers (unavailable)"
                PCIsMuted = True
            Else
                DeviceName = "PC speakers"
                PCIsMuted = GetCurrentPCMuteSetting()
            End If

            Device = New Device(DeviceFriendlyName, DeviceName, DeviceIPAddress, PCVolume, PCIsMuted)
            LocatedDevices.Add(Device)

            Dim TimeSpan3Seconds As TimeSpan = New TimeSpan(0, 0, 3)

            If DebugIsOn Then Console.WriteLine("Start inventory devices - first attempt")
            If DebugIsOn Then Console.WriteLine("Wait time = " & TimeSpan3Seconds.ToString)

            Dim GoogleDevices As IReadOnlyList(Of IZeroconfHost) = Await ZeroconfResolver.ResolveAsync(Protocal, TimeSpan3Seconds, 5, 1000)

            If DebugIsOn Then Console.WriteLine("Google device count = " & GoogleDevices.Count)

            For Each GoogleDevice As IZeroconfHost In GoogleDevices

                PropertySet = GoogleDevice.Services.Item(Protocal).ToString
                DeviceFriendlyName = PropertySet.Remove(PropertySet.IndexOf(vbCrLf & "ca = ")).Remove(0, PropertySet.IndexOf("fn = ") + 5)
                DeviceName = PropertySet.Remove(PropertySet.IndexOf(vbCrLf & "ic = ")).Remove(0, PropertySet.IndexOf("md = ") + 5)
                DeviceIPAddress = IPAddress.Parse(GoogleDevice.IPAddress)
                Device = New Device(DeviceFriendlyName, DeviceName, DeviceIPAddress, 0, False)
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

            Dim PropertySet As String
            Dim DeviceFriendlyName As String
            Dim DeviceName As String
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
                    PropertySet = GoogleDevice.Services.Item(Protocal).ToString
                    DeviceFriendlyName = PropertySet.Remove(PropertySet.IndexOf(vbCrLf & "ca = ")).Remove(0, PropertySet.IndexOf("fn = ") + 5)
                    DeviceName = PropertySet.Remove(PropertySet.IndexOf(vbCrLf & "ic = ")).Remove(0, PropertySet.IndexOf("md = ") + 5)
                    DeviceVolume = GetCurrentVolume(DeviceIPAddress)
                    DeviceIsMuted = GetCurrentMuteSetting(DeviceIPAddress)
                    Device = New Device(DeviceFriendlyName, DeviceName, DeviceIPAddress, DeviceVolume, DeviceIsMuted)

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

End Module

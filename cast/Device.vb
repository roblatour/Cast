Imports System.Net
Imports SharpCast

Friend Class Device

    Friend Enum DeviceType
        PC_Speaker = 0
        Google_Assistant = 1
        Chromecast = 2
    End Enum

    'Primary properities
    Friend Property FriendlyName As String
    Friend Property DeviceName As String
    Friend Property IPAddress As IPAddress
    Friend Property Muted As Boolean
    Friend Property Volume As Integer
    Friend Property DeviceKind As DeviceType 'derived properity
    Friend Property Player As Player 'derived properity

    Friend Sub New(ByVal iFriendlyName As String, iDeviceName As String, iIPAddress As IPAddress, iVolume As Integer, iMuted As Boolean)

        FriendlyName = iFriendlyName
        DeviceName = iDeviceName
        IPAddress = iIPAddress
        Volume = iVolume
        Muted = iMuted

        If iIPAddress.ToString = gHostComputerIPAddress.ToString Then
            DeviceKind = DeviceType.PC_Speaker
            Player = Nothing
        Else
            If iDeviceName.ToLower.StartsWith("google home") Then
                DeviceKind = DeviceType.Google_Assistant
            Else
                DeviceKind = DeviceType.Chromecast
            End If
            Player = New Player(iIPAddress.ToString)
        End If

    End Sub

End Class

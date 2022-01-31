Module modCommandLineTestingStub

#If DEBUG Then
    Friend DebugIsOn As Boolean = True
#Else
    Friend DebugIsOn As Boolean = False
#End If

    Friend Function CommandLineOverride() As String

        'Return  String.Empty
        'Return " ""xxx"" booboo"
        'Return "-pause"

        'Return "-pause"
        'eturn "-cancel"
        'Return "-broadcast -cancel -pause"
        'Return "-ip n.n.n.27 -volume 30 -file ""c:\temp\grace test.mp3"" -pause"
        ' Return "-ip n.n.n.27 -volume 30 -file ""c:\temp\grace test.mp3"" -background"
        'Return "-ip n.n.n.27 -volume 10 -file ""c:\temp\grace.mp3"" -pause"

        'Return "-inventory -pause"

        'Return "cast robspc -inventory -pause"
        'Return "-device ""Rob's office speaker"""
        'Return "-device ""Rob's office speaker""  -text the ball is red -pause"
        'Return "-text the ball is blue -pause"
        'Return "-device ""Rob's office speaker"" -volume 20 -text the ball is red -pause"
        'Return "-device ""Rob's office speaker""  ""Basement Speaker"" -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/ring.wav -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Rob's office speaker"" ""Rob's bedroom speaker""   -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Rob's office speaker"" -volume 20 -file c:/temp/grace.mp3 -pause"
        'Return "-file c:/temp/grace.mp3 -pause"

        'Return "-device ""Rob's office speaker"" -volume 10 -pause"

        'Return "-broadcast -volume 20 -file c:/temp/grace.mp3 -pause"
        'Return "-ip n.n.n.a -file c:/temp/speachfile.mp3 -pause" '**************************************************** throws an error need to correct for it
        'Return "-ip n.n.n.a -file ""c:/temp/speachfile.wav"" -pause"
        'Return "-ip n.n.n.a -file c:/temp/grace.mp3 -pause"
        'Return "-ip n.n.n.a n.n.n.b -file c:/temp/grace.mp3 -pause"
        'Return "-ip n.n.n.a  n.n.n.b -text this is a test -pause"
        'Return "-device ""Rob's office speaker"" -text this is a test -pause"
        'Return "-device ""Rob's office speaker""  ""Basement speaker"" -volume 60 -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Rob's office speaker""  ""Basement speaker"" -volume 60 -file c:/temp/grace.mp3 -pause"
        'Return "-ip n.n.n.a -file c:/temp/BigBuckBunny.mp4 -pause"
        'Return "-ip n.n.n.a n.n.n.b -file ""c:/temp/BigBuckBunny.mp4"" -pause"

        'Return "-device ""Rob's office speaker"" ""Basement speaker"" -file c:\temp\grace.mp3 -pause -port 9696"
        'Return "-device ""Rob's office speaker"" ""Basement speaker"" -cancel -pause -port 9696"
        'Return "-text the ball is red -pause"
        'Return "-text the ball is red -volume 60 -pause"
        'Return "-device ""Rob's office speaker"" -voice ""Microsoft David Desktop"" -text this is a test -pause"
        'Return "-device ""Rob's office speaker"" -voice ""Microsoft Zxira Desktop"" -text this is a test -pause
        'Return "-broadcast -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/test.txt -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/ring.wav"
        'Return "-device ""Rob's office speaker"" -file c:/temp/BigBuckBunny.mp4 -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/index.html -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/test.json -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/test2.txt -pause"
        'Return "-device ""Rob's office speaker"" -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Basement TV"" -file c:/temp/grace.mp3 -pause"
        'Return "-device ""Basement TV"" -file c:/temp/BigBuckBunny.mp4 -pause"
        'Return "-device ""Basement TV"" -volume 50 -pause"
        'Return "-ip n.n.n.a -text this is a test"
        'Return "-device ""Rob's office speaker"" -volume 40 -inventory -pause"
        'Return "-ip n.n.n.a n.n.n.b -text this is a test -pause"
        'eturn "-flim flam -pause ddd"

        'Return "-device ""Rob's office speaker"" -url ""https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3"" -pause"
        'Return "-device ""Rob's office speaker"" ""Basement speaker"" -url ""https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3"" -pause"

        'Return "-device ""Rob's office display"" -url ""http://www.lindberg.no/hires/test/2L-125_stereo-352k-24b_04.m3u8"" -debug -pause"

        'Return "-device ""Rob's office display"" -url ""https://drlive01hls.akamaized.net/hls/live-ull/2014185/drlive01/master.m3u8"" -debug -pause"
        'Return "-device ""Rob's office display"" -url ""https://multiplatform-f.akamaihd.net/i/multi/april11/sintel/sintel-hd_,512x288_450_b,640x360_700_b,768x432_1000_b,1024x576_1400_m,.mp4.csmil/master.m3u8"" -debug -pause"

        'Return "-device ""Rob's office speaker"" -url ""https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3"" -debug -pause"
        'Return "-device ""rob's office speaker"" -file c:/temp/grace.mp3 -pause"

        'Return "-device ""rob's office speaker"" -file c:/temp/grace.mp3 -pause"

        'Return "-device ""rob's office speaker"" -file c:/temp/BigBuckBunny.mp4 -pause"
        'Return "-device ""rob's office speaker"" -file ""c:\temp\007 Soundtrack - Best of BondJames Bond - 01 - James Bond Theme Song.mp3"" -pause"
        'Return "-device ""rob's office speaker"" -file ""c:\temp\test.mp3"" -pause"
        'Return "-device ""rob's office speaker"" -file ""c:\temp\Grace.mp3"" -pause"

        'Return "-device ""Rob's office speaker"" -mute -inventory -pause -port 9696"
        'Return "-device ""rob's office speaker"" -dir ""C:\Users\Rob Latour\Music"""
        'Return "-device ""rob's office speaker"" -dir ""e:\Music"""

        'Return "-device ""rob's office speaker"" -dir ""e:\music"" -random -pause"
        'Return " -device ""rob's office speaker""  -dir ""e:\music"" -random -pause"
        'Return " -device ""rob's office speaker"" -dir ""e:\music"" -random -volume 10 -pause"

        'Return "-Text ""hello how are you"" -device ""rob's office speaker"" -background"

        'Return " -device ""rob's office speaker"" -dir -random -pause"

        'Return " -device ""rob's office speaker"" -dir ""e:\music"" -random -pause"

        'Return " -dir ""C:\Users\Rob Latour\Desktop\test"" -random -pause"

        'Return " -dir ""C:\Users\Rob Latour\Desktop\test"" -pause"

        'Return " -device ""Basement TV"" -dir ""C:\Users\Rob Latour\Desktop\test"" -pause"

        'Return "-debug -device ""Rob's office speaker"" -dir ""C:\Users\Rob Latour\Desktop\test"" -pause"

        'Return -debug -device "Rob's office speaker" -dir "C:\Users\Rob Latour\Desktop\test" -pause

        'Return "-device ""Rob's office speaker"" -dir ""C:\Users\Rob Latour\Desktop\test"" -background -pause"

        'Return "-device ""Rob's office speaker"" -url ""http://www.abc.net.au/res/streaming/audio/mp3/news_radio.wav"" -pause"

        'Return " -inventory -pause"
        'Return "-device ""Rob's office speaker"" -mute -inventory -pause"
        'Return "-unmute -inventory -pause"

        'Return "-about -pause"
        'Return "-url http://www.callclerk.com -pause"

        'Return "-pause"
        Return "-inventory -pause"
        'Return " -volume 25 % -device ""rob's office speaker"" -pause"

    End Function

End Module

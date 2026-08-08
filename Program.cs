using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NAudio.Wave;

IPAddress addr = IPAddress.Any;
int port = 5000;
TcpListener listener = new TcpListener(addr, port);

listener.Start();
Console.Title = "Project CloBeats Debug Console";
Console.WriteLine("Listening on port " + port + "...");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\nWaiting for Unity to connect...");
    Console.ResetColor();

    TcpClient client = await listener.AcceptTcpClientAsync();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Unity connected.");
    Console.ResetColor();

    try
    {
        using StreamReader reader = new(client.GetStream());

        while (true)
        {
            string? line = await reader.ReadLineAsync();

            // Client disconnected
            if (line == null)
                break;

            if (line.StartsWith("$beep"))
            {
                string[] beepSplits = line.Split(",");
                // NAudio is already cross-platform
                Thread playThread = new Thread(() => PlayToneNAudio(double.Parse(beepSplits[1]), int.Parse(beepSplits[2])));
                playThread.IsBackground = true;
                playThread.Start();
            }

            Console.WriteLine(line);
        }
    }
    catch (IOException)
    {
        // Connection lost unexpectedly.
    }
    catch (SocketException)
    {
        // Socket closed unexpectedly.
    }

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Unity disconnected.");
    Console.ResetColor();

    // Loop continues and waits for another connection.
}
static void PlayToneNAudio(double frequency, int durationMs)
{
    var signalGen = new NAudio.Wave.SampleProviders.SignalGenerator()
    {
        Gain = 0.2,
        Frequency = frequency,
        Type = NAudio.Wave.SampleProviders.SignalGeneratorType.Sin
    };

    var sampleProvider = signalGen.Take(TimeSpan.FromMilliseconds(durationMs));

    // Convert ISampleProvider to IWaveProvider for DirectSoundOut
    var waveProvider = sampleProvider.ToWaveProvider();

    var output = new NAudio.Wave.DirectSoundOut();
    output.Init(waveProvider);
    output.Play();

    // Wait for duration
    Thread.Sleep(durationMs);
}
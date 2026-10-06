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
Console.WriteLine("Type 'clear' to clear the console.");

_ = Task.Run(() =>
{
    while (true)
    {
        string? command = Console.ReadLine();

        if (command == null)
            return;

        if (command.Equals("clear", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("cls", StringComparison.OrdinalIgnoreCase))
        {
            Console.Clear();
        }
        else
        {
            Console.WriteLine("Unknown command. Available command: clear");
        }
    }
});

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
                Thread playThread = new Thread(() => PlayToneNAudio(double.Parse(beepSplits[1]), int.Parse(beepSplits[2]), double.Parse(beepSplits[3]), beepSplits[4]));
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
static void PlayToneNAudio(double frequency, int durationMs, double volume, string waveType = "sine")
{
    NAudio.Wave.SampleProviders.SignalGeneratorType signalGeneratorType = waveType switch
    {
        "sine" => NAudio.Wave.SampleProviders.SignalGeneratorType.Sin,
        "square" => NAudio.Wave.SampleProviders.SignalGeneratorType.Square,
        "triangle" => NAudio.Wave.SampleProviders.SignalGeneratorType.Triangle,
        "sawtooth" => NAudio.Wave.SampleProviders.SignalGeneratorType.SawTooth,
        "noise" => NAudio.Wave.SampleProviders.SignalGeneratorType.White,
        _ => NAudio.Wave.SampleProviders.SignalGeneratorType.Sin
    };
    var signalGen = new NAudio.Wave.SampleProviders.SignalGenerator()
    {
        Gain = volume,
        Frequency = frequency,
        Type = signalGeneratorType
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
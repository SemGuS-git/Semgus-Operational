using System.Diagnostics;
using System.Threading.Channels;

namespace Semgus.OrderSynthesis {
    internal enum StdStreamName {
        Stdout,
        Stderr,
    }

    internal static class Wsl {

        public static async Task Invoke(string cmd, IStdStreamReceiver? receiver = null) {
            Console.WriteLine($"invoke `wsl {cmd}`");

            receiver ??= ConsoleStdStreamReceiver.Instance;

            ProcessStartInfo start = new();
            start.FileName = "wsl";
            start.Arguments = cmd;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            using var process = Process.Start(start);

            using var stdout = process.StandardOutput;
            using var stderr = process.StandardError;

            var channel = Channel.CreateUnbounded<(StdStreamName, string)>();

            var t_out = Task.Run(() => ReadToEnd(stdout, StdStreamName.Stdout, channel));
            var t_err = Task.Run(() => ReadToEnd(stderr, StdStreamName.Stderr, channel));

            _ = Task.WhenAll(t_out, t_err).ContinueWith(_ => channel.Writer.Complete());

            await foreach ((var tag, var line) in channel.Reader.ReadAllAsync()) {
                receiver.Receive(tag, line);
            }
            receiver.Done();
        }

        static async Task ReadToEnd(StreamReader stream, StdStreamName tag, Channel<(StdStreamName, string)> lines) {
            while (await stream.ReadLineAsync() is string line) {
                lines.Writer.TryWrite((tag, line));
            }
        }

        public static async Task<bool> RunSketch(FlexPath file_sketch, FlexPath file_soln, FlexPath file_xml) {
            var receiver = new SketchStdStreamReceiver();
            await Invoke($"time sketch --fe-output-xml {file_xml.PathWsl} {file_sketch.PathWsl} > {file_soln.PathWsl}", receiver);
            return !receiver.Rejected;
        }

        public static Task RunPython(string py_cmd, params string[] args) => Invoke($"python3 {py_cmd} {string.Join(' ', args)}");
    }
}

using System.Diagnostics;
using System.Threading.Channels;

namespace Semgus.OrderSynthesis
{
    internal static class IpcUtil {

        public static async Task Invoke(ProcessStartInfo start, IStdStreamReceiver? receiver = null) {
            Console.WriteLine($"in `{start.WorkingDirectory}` invoke `{start.FileName} {start.Arguments}`");

            receiver ??= ConsoleStdStreamReceiver.Instance;

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
        }

        private static async Task ReadToEnd(StreamReader stream, StdStreamName tag, Channel<(StdStreamName, string)> lines) {
            while (await stream.ReadLineAsync() is string line) {
                lines.Writer.TryWrite((tag, line));
            }
        }

        public static async Task<(bool,string)> RunSketch(FlexPath wd, string file_sketch, string file_xml) { 
            var receiver = new SketchStdStreamReceiver();
            ProcessStartInfo start = new();
            start.WorkingDirectory = wd.Value;
            start.FileName = "time";
            start.Arguments = $"sketch --fe-output-xml {WrapInnerArgString(file_xml)} {WrapInnerArgString(file_sketch)}";
            //start.Arguments = $"-h";
            //start.Arguments = $"--fe-output-xml {file_xml} {file_sketch} > {file_soln}";
            //start.FileName = "time";
            //start.Arguments = $"sketch --fe-output-xml {file_xml} {file_sketch} > {file_soln}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            

            
            if(System.Environment.GetEnvironmentVariable("PATH") is string env_path) start.EnvironmentVariables["PATH"] = env_path;

            await Invoke(start, receiver);


            return (!receiver.Rejected, receiver.GetResult());
        }

        private static string WrapInnerArgString(string s) {
            return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }
        //public static Task RunPython(string py_cmd, params string[] args) => Invoke($"python3", $"{py_cmd} {string.Join(' ', args)}");
    }


}

#define INT_ATOM_FLAGS
#define INT_MONO_FLAGS

using System.Diagnostics;

namespace Semgus.CommandLineInterface {
    public class Program {
        static void Main(string[] args) {
            var file = args[0];

            if (!File.Exists(file)) throw new FileNotFoundException("Missing input file", file);

            var items = ParseUtil.TypicalItems.Acquire(file);

            var ex = new Extractor();
            ex.Extract(items.Grammar);

            var s = ex.PrintFile();

            System.Console.WriteLine(s);

            string file_sketch = $"c:/Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch2/ord-{Path.GetFileName(file)}.sk";
            System.Console.WriteLine($"--- writing to {file_sketch} ---");
            File.WriteAllText(file_sketch, s);

            string file_sketch_wsl = "/mnt/c" + file_sketch.Substring(2);
            string file_xml_wsl = file_sketch_wsl + ".out.holes.xml";
            string file_out_wsl = file_sketch_wsl + ".out.txt";


            System.Console.WriteLine($"--- invoking Sketch on {file_sketch_wsl} ---");

            wsl_run_sketch(file_sketch_wsl, file_out_wsl, file_xml_wsl);

            System.Console.WriteLine($"--- sketch finished ---");

            string file_monotonicities_wsl = file_sketch_wsl + ".out.mono.json";
            string file_comparisons_wsl = file_sketch_wsl + ".out.cmp.sk";

            wsl_invoke_python("--version");
            wsl_invoke_python("read-mono-from-xml.py", file_sketch_wsl, file_xml_wsl, file_monotonicities_wsl);
            wsl_invoke_python("parse-cmp.py", file_out_wsl, file_comparisons_wsl);

            System.Console.WriteLine($"--- extractors finished ---");

            Console.ReadKey();
        }
        
        static void wsl_invoke(string cmd) {
            Console.WriteLine($"invoke `wsl {cmd}`");

            ProcessStartInfo start = new();
            start.FileName = "wsl";
            start.Arguments = cmd;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using var process = Process.Start(start);
            using var reader = process.StandardOutput;
            while (!reader.EndOfStream) {
                Console.WriteLine($"  wsl :: " + reader.ReadLine());
            }
        }

        static void wsl_run_sketch(string file_sketch, string file_soln, string file_xml) => wsl_invoke($"time sketch --fe-output-xml {file_xml} {file_sketch} > {file_soln}");

        static void wsl_invoke_python(string script, params string[] args) {
            //File.WriteAllText("hello.py", "print('Hello from Python!')\nprint('This is a test.')");
            wsl_invoke($"python3 {script} {string.Join(' ',args)}");
        }
    }
}
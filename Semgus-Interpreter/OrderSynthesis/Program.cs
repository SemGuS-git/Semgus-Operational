
using Semgus.CommandLineInterface;
using Semgus.OrderSynthesis.Subproblems;

namespace Semgus.OrderSynthesis {
    public class Program {
        static async Task Main(string[] args) {
            var file = args[0];

            if (!File.Exists(file)) throw new FileNotFoundException("Missing input file", file);

            var items = ParseUtil.TypicalItems.Acquire(file);

            var first = FirstStep.Extract(items.Grammar);

            FlexPath file_sketch = new($"Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/ord-{Path.GetFileName(file)}.sk");

            System.Console.WriteLine($"--- writing to {file_sketch} ---");

            using (StreamWriter sw = new(file_sketch.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in first.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            var file_xml = file_sketch.Append(".out.holes.xml");
            var file_out = file_sketch.Append(".out.txt");


            System.Console.WriteLine($"--- invoking Sketch on {file_sketch} ---");

            var sketch_result = await Wsl.RunSketch(file_sketch, file_out, file_xml);

            if (sketch_result) {
                Console.WriteLine($"--- Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- Sketch rejected; halting ---");
                return;
            }

            var file_monotonicities = file_sketch.Append(".out.mono.json");
            var file_comparisons = file_sketch.Append(".out.cmp.sk");

            await Wsl.RunPython("--version");
            await Wsl.RunPython("read-mono-from-xml.py", file_sketch.PathWsl, file_xml.PathWsl, file_monotonicities.PathWsl);
            await Wsl.RunPython("parse-cmp.py", file_out.PathWsl, file_comparisons.PathWsl);

            System.Console.WriteLine($"--- extractors finished ---");

            Console.ReadKey();
        }


    }
}
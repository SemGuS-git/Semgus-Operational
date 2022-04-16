
using Semgus.CommandLineInterface;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.Subproblems;
using Sprache;

namespace Semgus.OrderSynthesis {
    public class Program {
        static async Task Main(string[] args) {
            var file = args[0];

            if (!File.Exists(file)) throw new FileNotFoundException("Missing input file", file);

            FlexPath file_sketch = new($"Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/ord-{Path.GetFileName(file)}.sk");
            var file_xml = file_sketch.Append(".out.holes.xml");
            var file_out = file_sketch.Append(".out.txt");




            if (false) {
                var items = ParseUtil.TypicalItems.Acquire(file);

                var first = FirstStep.Extract(items.Grammar);

                System.Console.WriteLine($"--- writing to {file_sketch} ---");

                using (StreamWriter sw = new(file_sketch.PathWin)) {
                    LineReceiver receiver = new(sw);
                    foreach (var a in first.GetFile()) {
                        a.WriteInto(receiver);
                    }
                }

                System.Console.WriteLine($"--- invoking Sketch on {file_sketch} ---");
                var sketch_result = await Wsl.RunSketch(file_sketch, file_out, file_xml);

                if (sketch_result) {
                    Console.WriteLine($"--- Sketch succeeded ---");
                } else {
                    Console.WriteLine($"--- Sketch rejected; halting ---");
                    return;
                }
            }
            System.Console.WriteLine($"--- starting parser ---");

            var out_content = await File.ReadAllTextAsync(file_out.PathWin);

            var out_sketchy = SketchParser.WholeFile.Parse(out_content);


            //var content = await File.ReadAllTextAsync(file_sketch.PathWin);

            //var sketchy = SketchParser.WholeFile.Parse(content);

            System.Console.WriteLine($"--- ending parser ---");

            return;

            System.Console.WriteLine($"--- starting fact extraction ---");


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
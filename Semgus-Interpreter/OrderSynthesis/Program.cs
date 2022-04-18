#define NOT_REUSE

using Semgus.CommandLineInterface;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation;
using Semgus.OrderSynthesis.Subproblems;
using Sprache;
using System.Diagnostics;
using System.Text.Json;

namespace Semgus.OrderSynthesis {

    public class Program {

        record FirstResults(IReadOnlyList<FunctionDefinition> Comparisons, IReadOnlyList<MonotoneLabeling> MonoFunctions);
        record RefinementResults(IReadOnlyList<FunctionDefinition> Comparisons, bool stopFlag);
        record ReductionResults(IReadOnlyList<FunctionDefinition> Comparisons);

        static async Task<Result<FirstResults>> RunFirst(FirstStep first, FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);
            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            var file_mono = dir.Append("result.mono.json");
            var file_cmp = dir.Append("result.comparisons.sk");

#if NOT_REUSE
            System.Console.WriteLine($"--- [Initial] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in first.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Initial] Invoking Sketch on {file_in} ---");
            var sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

            if (sketch_result) {
                Console.WriteLine($"--- [Initial] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Initial] Sketch rejected; halting ---");
                return Result.Err<FirstResults>("Sketch rejected");
            }
#endif
            //System.Console.WriteLine($"--- parsing output code ---");

            //var out_sketchy = SketchParser.WholeFile.Parse(await File.ReadAllTextAsync(file_out.PathWin));

            // if(!first.IsValidOutput(out_sketchy)) throw new Exception();

            Console.WriteLine($"--- [Initial] Extracting monotonicities ---");

            await Wsl.RunPython("read-mono-from-xml.py", file_in.PathWsl, file_holes.PathWsl, file_mono.PathWsl);
            var mono = await ExtractMonotonicitiesJson(first.MaybeMonotoneFunctions, file_mono.PathWin);
            await Wsl.RunPython("parse-cmp.py", file_out.PathWsl, file_cmp.PathWsl);

            Console.WriteLine($"--- [Initial] Reading compare functions ---");

            //var compare_functions =
            //    SketchParser.AnyFunctionIn(
            //        first.Structs.Select(s => s.CompareId)
            //    ).Many().Parse(
            //        await File.ReadAllTextAsync(file_out.PathWin)
            //    ).ToList();

            var compare_functions = ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), first.Structs.Select(s => s.CompareId));

            if (compare_functions.Count != first.Structs.Count) {
                Console.WriteLine($"--- Failed to extract all comparison functions; halting ---");
                return Result<FirstResults>.Err("Comparison extraction failed");
            }

            Console.WriteLine($"--- [Initial] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted;
            try {
                compacted = Compactify(compare_functions);
            } catch (Exception ex) {
                Console.WriteLine($"--- Failed to transform all comparison functions; halting ---");
                return Result<FirstResults>.Err("Comparison transform failed");
            }

            return Result.Ok(new FirstResults(compacted, mono));
        }

        static IReadOnlyList<FunctionDefinition> ReadSelectedFunctions(string text, IEnumerable<Identifier> targets) {
            var indexMap = targets.Select((t, i) => (t, i)).ToDictionary(u => u.t, u => u.i);
            var raw = SketchParser.WholeFile.Parse(text).Contents.Where(st => st is FunctionDefinition fd && targets.Contains(fd.Id)).Cast<FunctionDefinition>();

            var ordered = new FunctionDefinition[indexMap.Count];

            foreach(var fd in raw) {
                ordered[indexMap.Remove(fd.Id,out var idx) ? idx : throw new KeyNotFoundException()] = fd;
            }

            if(indexMap.Count != 0) {
                throw new KeyNotFoundException();
            }
            return ordered;
        }

        static FunctionDefinition Compactify(FunctionDefinition function, IReadOnlyDictionary<Identifier, FunctionDefinition> available) {
            var x = SymbolicInterpreter.Evaluate(function, available);

            var out_val = x.RefVariables[new("_out")];

            var norm_1 = BitTernaryFlattener.Normalize(out_val);

            return new FunctionDefinition(function.Signature.AsFunctional(), new ReturnStatement(norm_1));
            //var norm_2 = NegationNormalForm.Normalize(norm_1);
            //var norm_3 = DisjunctiveNormalForm.Normalize(norm_2);
        }

        static IReadOnlyList<FunctionDefinition> Compactify(IReadOnlyList<FunctionDefinition> input, params FunctionDefinition[] available) => Compactify(input, (IEnumerable<FunctionDefinition>)available);

        static IReadOnlyList<FunctionDefinition> Compactify(IReadOnlyList<FunctionDefinition> input, IEnumerable<FunctionDefinition> available) {
            var functionMap = available.ToDictionary(k => k.Id);
            return input.Select(fn => Compactify(fn, functionMap)).ToList();
        }

        static async Task<Result<RefinementResults>> RunRefinement(int iter, OrderRefinementStep data, FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            //var file_cmp = dir.Append("result.comparisons.sk");


            System.Console.WriteLine($"--- [Refinement {iter}] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in data.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Refinement {iter}] Invoking Sketch on {file_in} ---");

            var step2_sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

            if (step2_sketch_result) {
                Console.WriteLine($"--- [Refinement {iter}] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Refinement {iter}] Sketch rejected; done with refinement ---");
                return Result<RefinementResults>.Ok(new(null, true));
            }

            //System.Console.WriteLine($"--- 2 parsing output code ---");

            //var step2_out_code = SketchParser.WholeFile.Parse(await File.ReadAllTextAsync(file_out.PathWin));

            //System.Console.WriteLine($"--- 2 starting fact extraction ---");

            //await Wsl.RunPython("parse-cmp.py", file_out.PathWsl, file_cmp.PathWsl);

            Console.WriteLine($"--- [Refinement {iter}] Reading compare functions ---");

            var compare_ids = data.Structs.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(data.PrevComparisons.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), extraction_targets);
            } catch (Exception ex) {
                Console.WriteLine($"--- [Refinement {iter}] Failed to extract all comparison functions; halting ---");
                return Result<RefinementResults>.Err("Comparison extraction failed");
            }

            Console.WriteLine($"--- [Refinement {iter}] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted;
            try {
                compacted = Compactify(extracted_functions.Where(f=>compare_ids.Contains(f.Id)).ToList(), data.PrevComparisons);
            } catch (Exception ex) {
                Console.WriteLine($"--- Failed to transform all comparison functions; halting ---");
                return Result<RefinementResults>.Err("Comparison transform failed");
            }


            return Result<RefinementResults>.Ok(new(compacted,false));
            //var cmp_file_content = SketchParser.WholeFile.Parse(await File.ReadAllTextAsync(file_cmp.PathWin));

            //if (ExtractCompares(cmp_file_content, data.Structs).TryUnwrap(out var compare_bodies)) {
            //    return Result.Ok(compare_bodies!);
            //} else {
            //    Console.WriteLine($"--- Failed to extract all comparison functions; halting ---");
            //    return Result<IReadOnlyList<FunctionDefinition>>.Err("Comparison extraction failed");
            //}
        }

        static async Task<Result<ReductionResults>> RunReduction(ReductionStep data, FlexPath dir) {
            Directory.CreateDirectory(dir.PathWin);

            var file_in = dir.Append("input.sk");
            var file_out = dir.Append("result.sk");
            var file_holes = dir.Append("result.holes.xml");
            //var file_cmp = dir.Append("result.comparisons.sk");

            System.Console.WriteLine($"--- [Reduction] Writing input file at {file_in} ---");

            using (StreamWriter sw = new(file_in.PathWin)) {
                LineReceiver receiver = new(sw);
                foreach (var a in data.GetFile()) {
                    a.WriteInto(receiver);
                }
            }

            System.Console.WriteLine($"--- [Reduction] Invoking Sketch on {file_in} ---");

            var step2_sketch_result = await Wsl.RunSketch(file_in, file_out, file_holes);

            if (step2_sketch_result) {
                Console.WriteLine($"--- [Reduction] Sketch succeeded ---");
            } else {
                Console.WriteLine($"--- [Reduction] Sketch rejected; reduction failed ---");
                return Result<ReductionResults>.Err("Sketch rejected");
            }

            Console.WriteLine($"--- [Reduction] Reading compare functions ---");

            var compare_ids = data.Structs.Select(s => s.CompareId).ToList();

            var extraction_targets = compare_ids.Concat(data.PrevComparisons.Select(p => p.Id));
            IReadOnlyList<FunctionDefinition> extracted_functions = Array.Empty<FunctionDefinition>();
            try {
                extracted_functions = ReadSelectedFunctions(await File.ReadAllTextAsync(file_out.PathWin), extraction_targets);
            }
            catch (Exception ex) {
                Console.WriteLine($"--- [Reduction] Failed to extract all comparison functions ---");
                return Result<ReductionResults>.Err("Comparison extraction failed");
            }

            Console.WriteLine($"--- [Reduction] Transforming compare functions ---");

            IReadOnlyList<FunctionDefinition> compacted;
            try {
                compacted = Compactify(extracted_functions.Where(f => compare_ids.Contains(f.Id)).ToList(), data.PrevComparisons);
            } catch (Exception ex) {
                Console.WriteLine($"--- [Reduction] Failed to transform all comparison functions; halting ---");
                return Result<ReductionResults>.Err("Comparison transform failed");
            }


            return Result<ReductionResults>.Ok(new(compacted));
        }

        static async Task Main(string[] args) {
            var file = args[0];

            Debug.Assert(File.Exists(file), "Missing input file {0}", file);

            FlexPath dir = new($"Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/{Path.GetFileName(file)}/");
#if NOT_REUSE

            if (Directory.Exists(dir.PathWin)) {
                Directory.Delete(dir.PathWin, true);
            }
            Directory.CreateDirectory(dir.PathWin);
#endif

            FlexPath dir_step1 = dir.Append("first/");

            var items = ParseUtil.TypicalItems.Acquire(file);

            var first = FirstStep.Extract(items.Grammar);

            IReadOnlyList<FunctionDefinition> comparisons;
            IReadOnlyList<FunctionDefinition> prev_compare = Array.Empty<FunctionDefinition>();
            IReadOnlyList<MonotoneLabeling> mono;
            {
                if ((await RunFirst(first, dir_step1)).TryUnwrap(out var tu)) {
                    (comparisons, mono) = tu!;
                } else {
                    return;
                }
            }

            Dictionary<Identifier, IType> type_map = LibFunctions.PrimTypes.Concat(first.Structs).ToDictionary(s => s.Id);
            const int MAX_REFINEMENT_STEPS = 100;

            int i = 0;
            for ( ; i < MAX_REFINEMENT_STEPS; i++) {
                prev_compare = comparisons
                    .Select(c => c with { Signature = c.Signature.AsHydrated(type_map, new("prev_" + c.Id.Name)) })
                    .ToList();

                var refinement_step = new OrderRefinementStep(first.Structs, mono, prev_compare);

                var dir_refinement = dir.Append($"refine_{i}/");
                if ((await RunRefinement(i, refinement_step, dir_refinement)).TryUnwrap(out var result)) {
                    if(result.stopFlag) {
                        break;
                    } else {
                        comparisons = result.Comparisons;
                    }
                } else {
                    return;
                }
            }

            var last = new ReductionStep(first.Structs, prev_compare);


            {
                FlexPath dir_reduce = dir.Append("reduce/");
                if ((await RunReduction(last, dir_reduce)).TryUnwrap(out var result3)) {
                    comparisons = result3.Comparisons;
                } else {
                    // Don't treat this as a hard stop
                }
            }



            // TODO: load compares and monotonicities into abstract interpretation framework

            Console.ReadKey();
        }

        private static async Task<IReadOnlyList<MonotoneLabeling>> ExtractMonotonicitiesJson(IReadOnlyList<FunctionDefinition> maybeMonotoneFunctions, string fname) {
            using var fs = File.OpenRead(fname);

            var obj = await JsonSerializer.DeserializeAsync<IReadOnlyDictionary<string, IReadOnlyList<string>>>(fs);

            Debug.Assert(obj.Count == maybeMonotoneFunctions.Count);

            return maybeMonotoneFunctions.Select(fn => new MonotoneLabeling(fn, obj[fn.Id.Name].Select(s => Enum.Parse<Monotonicity>(s, true)).ToList())).ToList();
        }

    }
}
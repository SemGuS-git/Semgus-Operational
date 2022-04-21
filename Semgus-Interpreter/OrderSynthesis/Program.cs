#define NOT_REUSE

using Semgus.CommandLineInterface;
using Semgus.MiniParser;
using Semgus.Operational;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation;
using Semgus.OrderSynthesis.Subproblems;
using Sprache;
using System.Diagnostics;
using System.Text.Json;

namespace Semgus.OrderSynthesis {

    public class Program {

        static async Task Main(string[] args) {
            var file = args[0];

            Debug.Assert(File.Exists(file), "Missing input file {0}", file);

            FlexPath dir = new($"Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/{Path.GetFileName(file)}/");

            var result = await RunPipeline(dir, file);

        }

        static async Task<PipelineState> RunPipeline(FlexPath dir, string input_file) {
            if (Directory.Exists(dir.PathWin)) {
                Directory.Delete(dir.PathWin, true);
            }
            Directory.CreateDirectory(dir.PathWin);

            PipelineState? state = null;

            try {
                {
                    var items = ParseUtil.TypicalItems.Acquire(input_file); // May throw

                    var step = MonotonicityStep.FromSemgusGrammar(items.Grammar); // May throw

                    state = new(PipelineState.Step.Initial, LibFunctions.PrimTypes.Concat(step.Structs).ToDictionary(s => s.Id), step.Structs);

                    var result = await step.Execute(dir.Append("step_1_mono/")); // May throw

                    state = state with {
                        Reached = PipelineState.Step.Monotonicity,
                        Comparisons = result.Comparisons,
                        MonotoneFunctions = result.MonoFunctions,
                        AllMonotonicities = result.MonoFunctions.Concat(result.NonMonoFunctions.Select(MonotoneLabeling.None)).ToList()
                    };
                }

                {
                    var result = await OrderExpansionStep.ExecuteLoop(dir.Append("step_2_expand/"), state); // May throw
                    state = state with { Reached = PipelineState.Step.OrderExpansion, Comparisons = result.Comparisons };
                }

                try {

                    var step = new SimplificationStep(state.Structs, state.Comparisons);
                    var result = await step.Execute(dir.Append("step_3_simplify/"));
                    state = state with { Reached = PipelineState.Step.Simplification, Comparisons = result.Comparisons };

                } catch (Exception e) {
                    // Don't treat this as a hard stop
                    Console.Error.Write(e);
                    Console.Error.Write("Continuing");
                }

                {
                    var step = new LatticeStep(state.Structs.Zip(state.Comparisons!));
                    var result = await step.Execute(dir.Append("step_4_lattice/"));
                    state = state with { Reached = PipelineState.Step.Lattice, Lattices = result.Lattices };
                }


                // TODO: load compares and monotonicities into abstract interpretation framework
                await WriteState(dir.Append("result/"), state);

                Console.WriteLine("--- Pipeline finished ---");
                return state;
            } catch (Exception e) {
                Console.Error.Write(e);
                Console.Error.Write("Halting");
                if (state is not null) {
                    var stash = dir.Append("incomplete_result/");
                    Directory.CreateDirectory(stash.PathWin);
                    await WriteState(stash, state);
                }
                throw;
            }

        }

        record MonoOutputLine(string Alias, IReadOnlyList<Monotonicity> Labels);

        static async Task WriteState(FlexPath path, PipelineState state) {
            File.WriteAllText(path.Append("step_reached.txt").PathWin, state.Reached.ToString());

            if (state.Comparisons is not null) {
                PipelineUtil.WriteSketchFile(path.Append("comparisons.sk"), state.Comparisons);
            }
            if (state.AllMonotonicities is not null) {
                var obj = new SortedDictionary<string, MonoOutputLine>();
                foreach (var a in state.AllMonotonicities) {
                    obj.Add(a.Function.Id.ToString(), new(a.Function.Alias!, a.ArgMonotonicities));
                }
                using var fs = File.OpenRead(path.Append("monotonicities.json").PathWin);

                await JsonSerializer.SerializeAsync(fs, obj);
            }
            if (state.Lattices is not null) {
                PipelineUtil.WriteSketchFile(path.Append("lattices.sk"), state.Lattices.SelectMany(l => l.GetEach()));
            }
        }
    }

    internal static class PipelineUtil {
        public static void WriteSketchFile(FlexPath path, IEnumerable<IStatement> content) {
            using StreamWriter sw = new(path.PathWin);
            LineReceiver receiver = new(sw);

            foreach (var a in content) {
                a.WriteInto(receiver);
            }
        }

        public static IReadOnlyList<FunctionDefinition> ReadSelectedFunctions(string text, IEnumerable<Identifier> targets) {
            var indexMap = targets.Select((t, i) => (t, i)).ToDictionary(u => u.t, u => u.i);

            var (head, body, foot) = SketchSyntaxParser.StripHeaders(text).Unwrap();

            var parsed = SketchSyntaxParser.Instance.FileContent.Symbol.ParseString(body).Unwrap();

            var raw = parsed.Where(st => st is FunctionDefinition fd && targets.Contains(fd.Id)).Cast<FunctionDefinition>();

            var ordered = new FunctionDefinition[indexMap.Count];

            foreach (var fd in raw) {
                ordered[indexMap.Remove(fd.Id, out var idx) ? idx : throw new KeyNotFoundException()] = fd;
            }

            if (indexMap.Count != 0) {
                throw new KeyNotFoundException();
            }
            return ordered;
        }

        // note: this doesn't work for return statements in inner scopes.
        public static FunctionDefinition SloppyFunctionalize(FunctionDefinition function) {
            var sig_functional = function.Signature.AsFunctional(out var refVarId);

            if (refVarId is null) {
                return function;
            }

            // Declare the out variable at the start, and then insert it into all return statements

            var adjusted = function.Body.Select(stmt => stmt is ReturnStatement ? new ReturnStatement(new VariableRef(refVarId)) : stmt);

            adjusted = adjusted.Prepend(new WeakVariableDeclaration(sig_functional.ReturnTypeId, refVarId));


            return new FunctionDefinition(sig_functional, adjusted.ToList());
        }

        delegate IStatement productor(IExpression value);


        public static FunctionDefinition Compactify(FunctionDefinition function, IReadOnlyDictionary<Identifier, FunctionDefinition> available) {
            var (raw_output, pro) = CompactifyInner(function, available);

            var norm_1 = BitTernaryFlattener.Normalize(raw_output);

            return function with { Body = new[] { pro(norm_1) } };

            //return new FunctionDefinition(sig_functional, pro(norm_1))

            //var norm_2 = NegationNormalForm.Normalize(norm_1);
            //var norm_3 = DisjunctiveNormalForm.Normalize(norm_2);
        }

        private static (IExpression,productor) CompactifyInner(FunctionDefinition function, IReadOnlyDictionary<Identifier, FunctionDefinition> available) {
            function.Signature.AsFunctional(out var refVarId);  // todo strip this down
            var eval_result = SymbolicInterpreter.Evaluate(function, available);

            IExpression raw_output;

            if (refVarId is null) {
                raw_output = eval_result.ReturnValue;
                return (raw_output, e => new ReturnStatement(e));
            } else {
                raw_output = eval_result.RefVariables[refVarId];
                return (raw_output, e => new Assignment(new VariableRef(refVarId), e));
            }
        }

        public static IReadOnlyList<FunctionDefinition> Compactify(IReadOnlyList<FunctionDefinition> input, params FunctionDefinition[] available) => Compactify(input, (IEnumerable<FunctionDefinition>)available);

        public static IReadOnlyList<FunctionDefinition> Compactify(IReadOnlyList<FunctionDefinition> input, IEnumerable<FunctionDefinition> available) {
            var functionMap = available.ToDictionary(k => k.Id);
            return input.Select(fn => Compactify(fn, functionMap)).ToList();
        }
    }
}
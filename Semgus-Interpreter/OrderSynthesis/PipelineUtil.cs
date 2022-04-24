#define NOT_REUSE

using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation;
using Semgus.OrderSynthesis.Subproblems;
using System.Text.Json;

namespace Semgus.OrderSynthesis {
    internal static class PipelineUtil {
        public static void WriteSketchFile(FlexPath path, IEnumerable<IStatement> content) {
            using StreamWriter sw = new(path.PathWin);
            LineReceiver receiver = new(sw);

            foreach (var a in content) {
                a.WriteInto(receiver);
            }
        }
        record MonoOutputLine(string Alias, IReadOnlyList<Monotonicity> Labels);

        public static async Task WriteState(FlexPath path, PipelineState state) {
            Directory.CreateDirectory(path.PathWin);
            File.WriteAllText(path.Append("step_reached.txt").PathWin, state.Reached.ToString());

            if (state.Comparisons is not null) {
                PipelineUtil.WriteSketchFile(path.Append("comparisons.sk"), state.Comparisons);
            }
            if (state.AllMonotonicities is not null) {
                var obj = new SortedDictionary<string, MonoOutputLine>();
                foreach (var a in state.AllMonotonicities) {
                    obj.Add(a.Function.Id.ToString(), new(a.Function.Alias!, a.ArgMonotonicities));
                }

                using var fs = File.OpenWrite(path.Append("monotonicities.json").PathWin);
                await JsonSerializer.SerializeAsync(fs, obj);
            }
            if (state.Lattices is not null) {
                PipelineUtil.WriteSketchFile(path.Append("lattices.sk"), state.Lattices.SelectMany(l => l.GetEach()));
            }
        }

        public static IReadOnlyList<FunctionDefinition> ReadSelectedFunctions(string text, IEnumerable<Identifier> targets) {
            var indexMap = targets.Select((t, i) => (t, i)).ToDictionary(u => u.t, u => u.i);

            var (head, body, foot) = SketchSyntaxParser.StripHeaders(text).Unwrap();

            var parsed = SketchSyntaxParser.Instance.FileContent.Symbol.ParseString(body).Unwrap();

            var raw = parsed.Where(st => st is FunctionDefinition fd && targets.Contains(fd.Id)).Cast<FunctionDefinition>();

            var ordered = new FunctionDefinition[indexMap.Count];

            foreach (var fd in raw) {
                ordered[indexMap.Remove(fd.Id, out var idx) ? idx : throw new KeyNotFoundException($"{fd}")] = fd;
            }

            if (indexMap.Count != 0) {
                throw new KeyNotFoundException(
                    $"The expected function IDs [{string.Join(", ", indexMap.Keys)}] were not found in the Sketch output file.\n" +
                    $"  (only [{string.Join(", ", ordered.Where(o => o is not null).Select(o => o.Id))}] were found)\n" +
                    $"  (the file had [{string.Join(",", parsed.Where(n => n is FunctionDefinition).Cast<FunctionDefinition>().Select(fd => fd.Id))}])"
                );
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


        public static FunctionDefinition ReduceToSingleExpression(FunctionDefinition function, IReadOnlyDictionary<Identifier, FunctionDefinition>? available = null) {
            available ??= new Dictionary<Identifier, FunctionDefinition>();

            var sig_functional = function.Signature.AsFunctional(out var refVarId);
            var eval_result = SymbolicInterpreter.Evaluate(function, available);

            var raw_output = refVarId is null ? eval_result.ReturnValue : eval_result.RefVariables[refVarId];

            var norm_1 = BitTernaryFlattener.Normalize(raw_output);

            return function with { Signature = sig_functional, Body = new[] { new ReturnStatement(norm_1) } };
        }

        public static IReadOnlyList<FunctionDefinition> ReduceEachToSingleExpression(IReadOnlyList<FunctionDefinition> targets, params FunctionDefinition[] available) => ReduceEachToSingleExpression(targets, (IEnumerable<FunctionDefinition>)available);

        public static IReadOnlyList<FunctionDefinition> ReduceEachToSingleExpression(IReadOnlyList<FunctionDefinition> targets, IEnumerable<FunctionDefinition> available) {
            var functionMap = available.ToDictionary(k => k.Id);
            return targets.Select(fn => ReduceToSingleExpression(fn, functionMap)).ToList();
        }
    }
}
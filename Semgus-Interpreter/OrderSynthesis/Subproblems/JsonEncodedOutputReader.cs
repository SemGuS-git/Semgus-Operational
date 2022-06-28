//using Semgus.Operational;
//using Semgus.OrderSynthesis.SketchSyntax.Helpers;
//using System.Diagnostics;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//using Microsoft.Extensions.Logging;
//using Semgus.Constraints;
//using Semgus.Operational;


//namespace Semgus.OrderSynthesis.AI2 {
//    public class IntervalAbstractReduction : Solvers.Enumerative.IReduction {
//        public ILogger? Logger { get; set; }
//        IReadOnlyList<BehaviorExample> Examples { get; }
//        AbstractSem AbsSem { get; }

//        public IntervalAbstractReduction(IReadOnlyList<BehaviorExample> examples, AbstractSem absSem) {
//            Examples = examples;
//            AbsSem = absSem;
//        }


//        public bool CanPrune(IDSLSyntaxNode node) {
//            if (node.CanEvaluate) return false; // don't need to abstractly evaluate concrete terms
//            for (int i = 0; i < Examples.Count; i++) {
//                BehaviorExample? example = Examples[i];
//                var res = AbsSem.Interpret(node, input, example_idx);

//                if (AbsSem.Prune(node, example.Values)) return true;
//            }
//            return false;
//        }
//    }
//    public class JsonEncodedOutputReader {
//        public class BottledMain {
//            [JsonPropertyName("term_types")] public List<BottledTermType> TermTypes { get; set; }
//            [JsonPropertyName("structs")] public List<BottledStruct> Structs { get; set; }
//            [JsonPropertyName("productions")] public List<BottledProduction> Productions { get; set; }
//            [JsonPropertyName("nonterminals")] public List<BottledNonterminal> Nonterminals { get; set; }
//        }
//        public class BottledTermType {
//            [JsonPropertyName("name")] public string Name { get; set; }
//            [JsonPropertyName("struct_in")] public string InputStructName { get; set; }
//            [JsonPropertyName("struct_out")] public string OutputStructName { get; set; }

//        }
//        public class BottledStruct {
//            [JsonPropertyName("name")] public string Name { get; set; }
//            [JsonPropertyName("members")] public List<string> MemberTypes { get; set; }
//            [JsonPropertyName("cmp")] public string CompareFunctionSexpr { get; set; }
//            [JsonPropertyName("top")] public List<string> Top { get; set; }
//            [JsonPropertyName("bot")] public List<string> Bot { get; set; }
//            [JsonPropertyName("join_incomparable")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string> IncomparableJoinSexpr { get; set; }
//            [JsonPropertyName("meet_incomparable")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string> IncomparableMeetSexpr { get; set; }
//        }
//        public class BottledProduction {
//            [JsonPropertyName("debug")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ProductionDebugInfo Debug { get; set; }
//            [JsonPropertyName("semantics")] public List<BottledSemantics> Semantics { get; set; }
//        }
//        public class BottledSemantics {
//            [JsonPropertyName("mono")] public List<string> Monotonicities { get; set; }
//            [JsonPropertyName("steps")]public List<BottledStep> Steps { get; set; }
//        }
//        public class BottledStep {
//            [JsonPropertyName("type")]
//        }

//        public class BottledNonterminal {
//            [JsonPropertyName("name")] public string Name { get; set; }
//            [JsonPropertyName("term_type")] public string TermTypeName { get; set; }
//            [JsonPropertyName("bounds")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<BottledBounds> Bounds { get; set; }
//        }
//        public class BottledBounds {
//            [JsonPropertyName("type")] public string BoundsType { get; }
//            [JsonPropertyName("type")] public List<string> Low { get; }
//            [JsonPropertyName("high")] public List<string> High { get; }
//            [JsonPropertyName("input")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Input { get; }
//            [JsonPropertyName("input_range")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string InputRange { get; }

//        }
//        public static void ReadIt(string blob) {

//        }

//    }
//    public class AbstractSem {

//        public Interval Interpret(IDSLSyntaxNode node, Interval input, int example_idx) {

//        }

//        public class NonterminalAbstraction {
//            public Lettuce OutputLattice { get; set; }
//            public Interval GetHoleAbstraction(int example_idx) { }
//            public Interval GetHoleAbstraction() {
//                return OutputLattice.BotTop();
//            }
//        }

//        public class AbstractProcedure {
//            public interface IStep {

//            }
//            public class EvalStep {
//                public int term_idx { get; }
//                public int tuple_idx_in { get; }
//                public int tuple_idx_out { get; }
//            }
//            public class AssertStep {
//                public ISmtLibExpression expr { get; }
//            }
//            public class TransformStep {
//                public List<ISmtLibExpression> expr { get; }
//            }
//        }
//        public class AbstractProd {

//        }

//        public class Interval {
//            public Lettuce Lattice { get; }
//            public Toople Left { get; }
//            public Toople Right { get; }

//            public Interval(Lettuce lattice, Toople left, Toople right) {
//                Lattice = lattice;
//                Left = left;
//                Right = right;
//            }
//            public Interval(Lettuce lattice, Toople single) {
//                Lattice = lattice;
//                Left = single;
//                Right = single;
//            }

//            public bool TryGetSize(out int size) {
//                size = -1;
//                return false;
//            }

//            public bool TryGetEnumerable(out IEnumerable<Toople> enumerable) {
//                enumerable = default;
//                return false;
//            }

//            public bool Contains(Toople other) {
//                return Lattice.Leq(Left, other) && Lattice.Leq(other, Right);
//            }

//            public Interval IntervalJoin(Interval other) {
//                Debug.Assert(Lattice == other.Lattice);
//                return new Interval(Lattice, Lattice.Meet(Left, other.Left), Lattice.Join(Right, other.Right));
//            }
//        }

//        public class Lettuce {
//            public Toople Bot { get; set; }
//            public Toople Top { get; set; }

//            public ISmtLibExpression eval_cmp { get; set; }
//            public ISmtLibExpression join_incomparable { get; set; }

//            public ISmtLibExpression meet_incomparable { get; set; }

//            public bool OrderIsTotal { get; set; }

//            public bool Leq(Toople a, Toople b) {
//                return Evall(eval_cmp, a, b);
//            }

//            public Interval BotTop() => new Interval(this, Bot, Top);

//            public Toople Meet(Toople a, Toople b) {
//                if (Evall(eval_cmp, a, b)) return a;
//                if (OrderIsTotal || Evall(eval_cmp, b, a)) return b;
//                return Evall(meet_incomparable, a, b);
//            }

//            public Toople Join(Toople a, Toople b) {
//                if (Evall(eval_cmp, a, b)) return b;
//                if (OrderIsTotal || Evall(eval_cmp, b, a)) return a;
//                return Evall(join_incomparable, a, b);
//            }

//            static dynamic Evall(ISmtLibExpression expr, params Toople[] tup) {
//                throw new NotImplementedException();
//            }
//        }

//        public class Toople {
//            public dynamic[] Values { get; }
//        }


//        public List<Lettuce> Lattices { get; } = new();

//    }
//}

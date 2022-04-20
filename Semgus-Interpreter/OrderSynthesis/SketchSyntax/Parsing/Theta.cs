using Semgus.Util;
using System.Diagnostics;

namespace Semgus.OrderSynthesis.SketchSyntax.Parsing {
    namespace Theta {

        internal interface ISymbolFrame : IEnumerator<Symbol> {
            public void NotifySuccess(IEnumerable<INode> ok);
            public void NotifyFailure();
            bool IsSuccess { get; }
            int Cursor { get; set; }

            IEnumerable<INode> Bake();
        }

        interface ITerminal {
            bool Check(IToken token, out INode node);
        }


        internal class StackMachine {
            private readonly StreamAccumulator<IToken> tokens;
            int tokenCursor = 0;

            public StackMachine(IEnumerator<IToken> tokenStream) {
                this.tokens = new(tokenStream);
            }

            record Frame(Symbol symbol, int Cursor) { }

            public bool TryParse(Symbol root, out IEnumerable<INode> result) {

                var stack = new Stack<ISymbolFrame>();

                if(root is INonTerminal root_nt) {
                    stack.Push(root_nt.GetFrame());
                } else {
                    if (!tokens.Ok(1) && tokens.TryGet(0, out var root_tk) && root.CheckTerminal(root_tk, out var first)) {
                        result = new[] { first };
                        return true;
                    } else {
                        result = Enumerable.Empty<INode>();
                        return false;
                    }
                }

                while (stack.TryPeek(out var frame)) {
                    if (frame.MoveNext()) {
                        var symbol = frame.Current;

                        if (symbol is INonTerminal nt) {
                            var next_frame = nt.GetFrame();
                            next_frame.Cursor = frame.Cursor;
                            stack.Push(next_frame);
                            continue;
                        }

                        if (!tokens.TryGet(frame.Cursor, out var token)) break;

                        if (symbol.CheckTerminal(token, out var node)) {
                            frame.Cursor++;
                            frame.NotifySuccess(new[] { node });
                        } else {
                            frame.NotifyFailure();
                        }
                        continue;
                    }

                    if (frame.IsSuccess) {
                        var baked = frame.Bake();
                        {
                            if (stack.TryPeek(out var parent)) {
                                parent.Cursor = frame.Cursor;
                                parent.NotifySuccess(baked);
                                continue;
                            }
                            result = baked;
                            return true;
                        }
                    }
                    {
                        if (stack.TryPeek(out var parent)) {
                            parent.NotifyFailure();
                            continue;
                        }
                    }

                    break;
                }
                result = Enumerable.Empty<INode>();
                return false;

            }

            internal bool TryRead(string exact) {
                if (tokens.TryGet(tokenCursor, out var t) && t.Is(exact)) {
                    tokenCursor++;
                    return true;
                }
                return false;
            }

            internal bool TryRead<T>() where T : IToken {
                if (tokens.TryGet(tokenCursor, out var t) && t is T tt) {
                    tokenCursor++;
                    return true;
                }
                return false;
            }

            internal bool TryRead<T>(Func<T, bool> predicate) where T : IToken {
                if (tokens.TryGet(tokenCursor, out var t) && t is T tt && predicate(tt)) {
                    tokenCursor++;
                    return true;
                }
                return false;
            }

        }

        internal static class Extensions {
            internal static T Take<T>(this Queue<INode> q) {
                return (T)q.Dequeue();
            }

            internal static T TakeEnumKeyword<T>(this Queue<INode> q) where T : struct => Enum.Parse<T>(q.Take<Keyword>().Value);

            internal static Queue<INode> Skip<T>(this Queue<INode> q) {
                if (q.TryDequeue(out var a) && a is T) return q;
                throw new Exception();
            }

            internal static bool TrySkip<T>(this Queue<INode> q) {
                if (!q.TryPeek(out var a) || a is not T) return false;
                q.Dequeue();
                return true;
            }

            internal static bool TrySkipKeyword(this Queue<INode> q, string s) {
                if (!q.TryPeek(out var a) || a is not Keyword key || key.Value != s) return false;
                q.Dequeue();
                return true;
            }

            internal static bool TryTake<T>(this Queue<INode> q, out T value) {
                if (q.TryPeek(out var a) && a is T aa) {
                    value = aa;
                    q.Dequeue();
                    return true;
                }
                value = default;
                return false;
            }

            internal static IReadOnlyList<T> TakeStar<T>(this Queue<INode> q) => q.TakeAtLeast<T>(0);

            internal static IReadOnlyList<T> TakeAtLeast<T>(this Queue<INode> q, int n) {
                List<T> list = new List<T>();
                while (q.TryPeek(out var obj) && obj is T va) {
                    list.Add(va);
                    q.Dequeue();
                }
                Debug.Assert(list.Count >= n);
                return list;
            }
            internal static void AddRange<T>(this Queue<T> q, IEnumerable<T> values) {
                foreach (var value in values) q.Enqueue(value);
            }
        }
        static class Mu {
            static void Main() {


                LiteralSymbol literal = new();
                IdentifierSymbol identifier = new();

                var expression = new Placeholder();

                // todo dots

                var unit = literal | identifier | "(" + expression + ")";
                var call = (identifier + "(" + (expression + ("," + expression).Star()).Some() + ")");

                var assign = identifier + "=" + expression;
                var call_ctor = "new" + identifier + "(" + (assign + ("," + assign).Star()).Maybe() + ")";

                var unary = call_ctor | call | unit;
                unary |= unary.Prefix("!", "-");

                var e_mul = unary.Infix("*");
                var e_add = e_mul.Infix("+", "-");
                var e_cmp = e_add.Binary("<=", "<", ">=", ">");
                var e_eq = e_cmp.Binary("==", "!=");
                var e_and = e_eq.Infix("&&");
                var e_or = e_and.Infix("||");
                var e_tern = e_or;
                e_tern |= e_tern + "?" + e_tern + ":" + e_or;

                expression.Install(e_tern);

                var declare_var = identifier + identifier + ("=" + expression).Maybe();

                var def_struct = "struct" + identifier + "{" + (declare_var + ";").Some() + "}";

                var exclaim = (new Keyword("assert") | "minimize") + expression;
                var returns = "return" + expression.Maybe();

                var line = (declare_var | assign | call | returns + exclaim) + ";";



                Symbol statement = line;

                var ite = "if" + "(" + expression + ")" + (line | "{" + (statement).Star() + "}") + ("else" + statement).Maybe();
                var repeat = "repeat" + "(" + expression + ")" + (line | "{" + (statement).Star() + "}");

                statement |= ite | repeat;

                var arg = new Keyword("ref").Maybe() + identifier + identifier;

                var fn_args = "(" + (arg + ("," + arg).Star()).Maybe() + ")";

                var def_function = (new Keyword("harness") | "generator").Maybe() + identifier + identifier + fn_args + "{" + statement.Star() + "}";

                var file_content = (def_struct | def_function | declare_var).Star();


                call_ctor.SetTransformer(ctx => new StructNew(ctx.Skip<Keyword>().Take<Identifier>(), ctx.TakeStar<Assignment>()));

                def_struct.SetTransformer(ctx => new StructDefinition(ctx.Skip<Keyword>().Take<Identifier>(), ctx.TakeStar<WeakVariableDeclaration>()));

                declare_var.SetTransformer(ctx => new WeakVariableDeclaration(ctx.Take<Identifier>(), ctx.Take<Identifier>(), ctx.TryTake<IExpression>(out var expr) ? expr : Empty.Instance));

                assign.SetTransformer(ctx => new Assignment(new VariableRef(ctx.Take<Identifier>()), ctx.Take<IExpression>()));

                e_tern.SetTransformer(ctx => new Ternary(ctx.Take<IExpression>(), ctx.Take<IExpression>(), ctx.Take<IExpression>()));

                arg.SetTransformer(ctx => new FunctionArg(ctx.TrySkip<Keyword>(), ctx.Take<Identifier>(), ctx.Take<Identifier>()));

                returns.SetTransformer(ctx => ctx.Skip<Keyword>().TryTake<IExpression>(out var expr) ? new ReturnStatement(expr) : new ReturnStatement());

                exclaim.SetTransformer(ctx => ctx.Take<Keyword>().Value switch {
                    "assert" => new AssertStatement(ctx.Take<IExpression>()),
                    "minimize" => new MinimizeStatement(ctx.Take<IExpression>()),
                    _ => throw new Exception()
                });

                ite.SetTransformer(ctx => new IfStatement(
                    ctx.Skip<Keyword>().Take<IExpression>(),
                    ctx.TakeStar<IStatement>().ToList(),
                    (
                        ctx.TryTake<Keyword>(out _)
                        ? ctx.TakeStar<IStatement>()
                        : Array.Empty<IStatement>()).ToList()
                    )
                );

                repeat.SetTransformer(ctx => new RepeatStatement(ctx.Skip<Keyword>().Take<IExpression>(), ctx.TakeStar<IStatement>()));

                def_function.SetTransformer(ctx => new FunctionDefinition(
                        new WeakFunctionSignature(
                            Flag: ctx.TryTake<Keyword>(out var kw) ? Enum.Parse<FunctionModifier>(kw.Value) : FunctionModifier.None,
                            ReturnTypeId: ctx.Take<Identifier>(),
                            Id: ctx.Take<Identifier>(),
                            Args: ctx.TakeStar<FunctionArg>()
                        ),
                        Body: ctx.TakeStar<IStatement>()
                    ));

                call.SetTransformer(ctx => new FunctionEval(ctx.Take<Identifier>(), ctx.TakeStar<IExpression>().ToList()));


            }
        }

        internal class OnceOrMore : Symbol, INonTerminal {
            private class Frame : FrameBase {
                public override Symbol Current => source.symbol;

                private Queue<INode> items = new();


                private readonly OnceOrMore source;
                private bool done = false;

                public Frame(OnceOrMore source) {
                    this.source = source;
                }


                public override void NotifyFailure() => done = true;

                public override void NotifySuccess(IEnumerable<INode> ok) {
                    items.AddRange(ok);
                    IsSuccess = true;
                }

                public override IEnumerable<INode> Bake() => source.Transform is null ? items : new[] { source.Transform(items) };

                public override bool MoveNext() => !done;
            }

            private Symbol symbol;

            public OnceOrMore(Symbol symbol) {
                this.symbol = symbol;
            }

            public ISymbolFrame GetFrame() => new Frame(this);
        }


        internal class MaybeOnce : Symbol, INonTerminal {
            private class Frame : FrameBase {
                public override Symbol Current => source.symbol;

                private Queue<INode> items = new();


                private readonly MaybeOnce source;
                private bool done = false;

                public Frame(MaybeOnce source) {
                    this.source = source;
                    IsSuccess = true;
                }

                public override void NotifyFailure() { }

                public override void NotifySuccess(IEnumerable<INode> ok) {
                    items.AddRange(ok);
                }

                public override IEnumerable<INode> Bake() => source.Transform is null ? items :  new[] { source.Transform(items) };

                public override bool MoveNext() => !done && (done = true);
            }

            private Symbol symbol;

            public MaybeOnce(Symbol a) {
                this.symbol = a;
            }

            public ISymbolFrame GetFrame() => new Frame(this);
        }

        internal class Starred : Symbol, INonTerminal {
            private class Frame : FrameBase {
                public override Symbol Current => source.symbol;

                private Queue<INode> items = new();

                private readonly Starred source;
                private bool done = false;

                public Frame(Starred source) {
                    this.source = source;
                    IsSuccess = true;
                }

                public override void NotifyFailure() => done = true;

                public override void NotifySuccess(IEnumerable<INode> ok) => items.AddRange(ok);

                public override IEnumerable<INode> Bake() => source.Transform is null ? items : new[] { source.Transform(items) };

                public override bool MoveNext() => !done;
            }

            private Symbol symbol;

            public Starred(Symbol a) {
                this.symbol = a;
            }
            public ISymbolFrame GetFrame() => new Frame(this);
        }

        internal static class SymbolExtensions {

            public static Starred Star(this Symbol a) => new(a);
            public static OnceOrMore Some(this Symbol a) => new(a);
            public static MaybeOnce Maybe(this Symbol a) => new(a);


            public static Symbol Prefix(this Symbol term, params Symbol[] ops) => (Earliest.Of(ops) + term)
                .SetTransformer(ctx => new UnaryOperation(ctx.TakeEnumKeyword<SketchSyntax.UnaryOp>(), ctx.Take<IExpression>()));

            public static Symbol Binary(this Symbol term, params Symbol[] ops) => (term + Maybe(Earliest.Of(ops) + term))
                .SetTransformer((ctx) => new InfixOperation(ctx.TakeEnumKeyword<Op>(), ctx.Take<IExpression>(), ctx.Take<IExpression>()));

            public static Symbol Infix(this Symbol term, params Symbol[] ops) => (term + Star(Earliest.Of(ops) + term))
                .SetTransformer((ctx) => new InfixOperation(ctx.TakeEnumKeyword<Op>(), ctx.TakeAtLeast<IExpression>(2)));
        }


        internal abstract class OpBase : Symbol {
            public Symbol term;
            public readonly Symbol[] ops;

            public OpBase(Symbol term, params Symbol[] ops) {
                this.term = term;
                this.ops = ops;
            }

            protected string args_expression() => (ops.Length == 1 ? ops[0].ToString() : "(" + string.Join('|', ops.Select(o => o.ToString())) + ")")!;
        }


        internal class LiteralSymbol : Symbol {
            public override string ToString() => "INT";

            public override bool CheckTerminal(IToken token, out INode node) {
                if (token is Literal lit) {
                    node = lit;
                    return true;
                } else {
                    node = default;
                    return false;
                }
            }
        }
        internal class IdentifierSymbol : Symbol {
            public override string ToString() => "ID";

            public override bool CheckTerminal(IToken token, out INode node) {
                if(token is Identifier id) {
                    node = id;
                    return true;
                } else {
                    node = default;
                    return false;
                }
            }
        }

        internal class Placeholder : Symbol {
            private Symbol? _inner;

            public override Transformer? Transform => _inner.Transform;

            internal void Install(Symbol inner) => _inner = inner;

            public override bool CheckTerminal(IToken token, out INode node) => _inner.CheckTerminal(token, out node);
            public override Symbol SetTransformer(Transformer fn) => _inner.SetTransformer(fn);
        }




        internal class Earliest : Symbol, INonTerminal {

            private class Frame : FrameBase {
                private readonly Earliest source;
                private readonly IEnumerator<Symbol> enumerator;

                public override Symbol Current => enumerator.Current;

                private bool stop = false;

                private Queue<INode> results = new();

                public Frame(Earliest source) {
                    this.source = source;
                    this.enumerator = source.list.GetEnumerator();
                }

                public override IEnumerable<INode> Bake() => source.Transform is null ? results : new[] { source.Transform(results) };

                public override bool MoveNext() => !stop && enumerator.MoveNext();

                public override void NotifyFailure() { }

                public override void NotifySuccess(IEnumerable<INode> ok) {
                    IsSuccess = true;
                    results.AddRange(ok);
                    stop = true;
                }
            }

            public List<Symbol> list = new List<Symbol>();

            public Earliest(params Symbol[] a) {
                list.AddRange(a);
            }

            public Earliest(IEnumerable<Symbol> a) {
                list.AddRange(a);
            }

            public override string ToString() => $"( {string.Join(" | ", list)} )";

            public static Earliest operator |(Earliest a, Earliest b) => new(a.list.Concat(b.list));
            public static Earliest operator |(Earliest a, Symbol b) => new(a.list.Append(b));
            public static Earliest operator |(Symbol a, Earliest b) => new(b.list.Prepend(a));

            public static Symbol Of(params Symbol[] s) => s.Length == 1 ? s[0] : new Earliest(s);

            public ISymbolFrame GetFrame() => new Frame(this);
        }

        internal interface INonTerminal {
            ISymbolFrame GetFrame();
        }
        abstract class FrameBase : ISymbolFrame {
            public bool IsSuccess { get; protected set; } = false;
            public int Cursor { get; set; }

            public abstract Symbol Current { get; }

            object System.Collections.IEnumerator.Current => Current;


            public abstract IEnumerable<INode> Bake();
            public abstract void NotifyFailure();

            public abstract void NotifySuccess(IEnumerable<INode> ok);

            public abstract bool MoveNext();

            public virtual void Reset() => throw new NotSupportedException();

            public virtual void Dispose() { }
        }
        internal class All : Symbol, INonTerminal {

            private class Frame : FrameBase {
                private readonly All source;
                private readonly IEnumerator<Symbol> enumerator;

                public override Symbol Current => enumerator.Current;

                private bool stop = false;
                private bool anyFailed = false;

                private Queue<INode> results = new();

                public Frame(All source) {
                    this.source = source;
                    this.enumerator = source.list.GetEnumerator();
                }

                public override IEnumerable<INode> Bake() => source.Transform is null ? results : new[] { source.Transform(results) };

                public override bool MoveNext() {
                    if (stop) return false;
                    if (enumerator.MoveNext()) return true;
                    stop = true;
                    IsSuccess = true;
                    return false;
                }

                public override void NotifyFailure() {
                    stop = true;
                    IsSuccess = false;
                }

                public override void NotifySuccess(IEnumerable<INode> ok) {
                    results.AddRange(ok);
                }
            }

            public List<Symbol> list = new List<Symbol>();

            public All(params Symbol[] a) {
                list.AddRange(a);
            }

            public All(IEnumerable<Symbol> a) {
                list.AddRange(a);
            }


            public override string ToString() => $"( {string.Join(" ", list)} )";



            public static All operator +(All a, All b) => new(a.list.Concat(b.list));
            public static All operator +(All a, Symbol b) => new(a.list.Append(b));
            public static All operator +(Symbol a, All b) => new(b.list.Prepend(a));
            public static Symbol Of(params Symbol[] s) => s.Length == 1 ? s[0] : new All(s);

            public ISymbolFrame GetFrame() => new Frame(this);
        }


        internal class Delimiter : Symbol {
            private string str;

            public Delimiter(string str) {
                this.str = str;
            }

            public override bool CheckTerminal(IToken token, out INode node) {
                node = Empty.Instance;
                return token.Is(str);
            }

            public override string ToString() => $"\"{str}\"";
        }

        internal class Keyword : Symbol, INode {
            public string Value;

            public Keyword(string s) {
                this.Value = s;
            }

            public override bool CheckTerminal(IToken token, out INode node) {
                if (token.Is(Value)) {
                    node = this;
                    return true;
                } else {
                    node = Empty.Instance;
                    return false;
                }
            }

            public override string ToString() => $"\"{Value}\"";
        }

        internal abstract class Symbol {
            public virtual Transformer? Transform { get; private set; }
            public delegate INode Transformer(Queue<INode> context);

            public static All operator +(Symbol a, Symbol b) => (a is All aa ? aa : new All(a)) + b;


            public static Earliest operator |(Symbol a, Symbol b) => (a is Earliest aa ? aa : new Earliest(a)) | b;

            public static implicit operator Symbol(string s) => s switch {
                "(" or ")" or "{" or "}" or "?" or ":" or ";" or "," => new Delimiter(s),
                _ => new Keyword(s),
            };

            public virtual Symbol SetTransformer(Transformer fn) {
                Transform = fn;
                return this;
            }

            public virtual bool CheckTerminal(IToken token, out INode node) => throw new NotSupportedException();
        }

        public interface IToken {
            bool Is(string s);
        }

    }

}

using Sprache;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Semgus.SketchLang.Tests")]
namespace Semgus.OrderSynthesis.SketchSyntax.Parsing {

    internal static class DiyLexer {
        //class Bus {
        //    private readonly string _inner;

        //    private readonly int _length;
        //    private int _cursor;

        //    public Bus(string inner) {
        //        _inner = inner;
        //        _length = inner.Length;
        //        _cursor = -1;
        //    }

        //    public bool MoveNext() => ++_cursor < _length;

        //    public char Peek() => _inner[_cursor];
        //    public char Peek(int index) => _inner[_cursor + index];
        //}

        //struct LongerToken {
        //    public char[] value;
        //}

        //struct CharToken {
        //    public char value;
        //}

        //enum Token {
        //    Invalid,

        //    Comma,
        //    Semicolon,

        //    ParenOpen,
        //    ParenClose,
        //    BraceOpen,
        //    BraceClose,

        //    Bang,
        //    Minus,
        //    Plus,
        //    Times,
        //    Slash,

        //    Dot,

        //    Eq,
        //    Neq,

        //    Lt,
        //    Gt,
        //    Leq,
        //    Geq,


        //    SingleQuestion,
        //    Colon,

        //    DoubleQuestion,

        //    DoubleAnd,
        //    DoubleOr,
        //    DoubleEq,

        //    DoubleSlash,
        //    SlashStar,

        //    Keyword,
        //    Identifier,

        //    RawIdentifier,
        //    Number
        //}
        //static string Str(Token t) => t switch {
        //    Comma,
        //    Semicolon,

        //    ParenOpen,
        //    ParenClose,
        //    BraceOpen,
        //    BraceClose,

        //    Bang,
        //    Minus,
        //    Plus,
        //    Times,

        //    Dot,

        //    Eq,
        //    Neq,

        //    Lt,
        //    Gt,
        //    Leq,
        //    Geq,


        //    SingleQuestion,
        //    Colon,

        //    DoubleQuestion,

        //    DoubleAnd,
        //    DoubleOr,
        //    DoubleEq,

        //    DoubleSlash,
        //    SlashStar,
        //    StarSlash,

        //    RawIdentifier,
        //    Number
        //}


        class Trie {
            public Token value;
            public Dictionary<char, Trie> children;

            public Trie(Token value) {
                this.value = value;
            }

            void Put(string s, Token value) {
                Trie now = this;
                for (int i = 0; i < s.Length - 1; i++) {
                    if (now.children is null) {
                        now.children = new();
                        now = new(Token.Invalid);
                        now.children.Add(s[i], now);
                    } else if (children.TryGetValue(s[i], out var ch)) {
                        now = ch;
                    } else {
                        now = new(Token.Invalid);
                        now.children.Add(s[i], now);
                    }
                }
            }
        }



        IEnumerable<Token> Scan(string value) {
            var bus = new Bus(value);

            while (bus.MoveNext()) {

                char.MaxValue

            }

        }
    }
    internal static class Parser2 {
        enum Token {
        }

        static void A() {
            Parser<Token> Tokens = null;

            Tokens.
        }

        protected internal virtual Parser<string> RawIdentifier =>
            from identifier in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit.Or(Parse.Char('_')))
            where !ApexKeywords.ReservedWords.Contains(identifier)
            select identifier;

        protected internal virtual Parser<string> Identifier =>
            RawIdentifier.Token().Named("Identifier");
    }


    internal static class SketchParser {
        static class Keywords {
            public const string Struct = "struct";
            public const string New = "new";
            public const string Assert = "assert";
            public const string Harness = "harness";
            public const string Generator = "generator";
            public const string Return = "return";
            public const string Repeat = "repeat";
            public const string Minimize = "minimize";
            public const string If = "if";
            public const string Else = "else";
            public const string Ref = "ref";
            public const string Implements = "implements";

            internal static bool IsNotKeyword(string arg) => arg switch {
                Struct or New or Assert or Harness or Generator or Return or Repeat or Minimize or If or Else or Ref or Implements => false,
                _ => true,
            };
        }

        class SafetyWrapper<T> {
            private Parser<T>? plug = null;
            public IResult<T> Parse(IInput input) => plug!.Invoke(input);

            public void Install(Parser<T> actual) {
                plug = actual;
            }
        }

        static SketchParser() {
            var wrapExpr = new SafetyWrapper<IExpression>();
            var wrapStmt = new SafetyWrapper<IStatement>();

            Expression = wrapExpr.Parse;
            Statement = wrapStmt.Parse;

            MiscChar = Parse.Chars("_@$");
            IdentifierFirstChar = Parse.Letter.Or(MiscChar);
            IdentifierSuffixChar = Parse.LetterOrDigit.Or(MiscChar);

            IdentifierOrKeyword =
                from first in IdentifierFirstChar.Once()
                from rest in IdentifierSuffixChar.Many()
                select new string(first.Concat(rest).ToArray());
            Identifier = IdentifierOrKeyword.Isol().Where(Keywords.IsNotKeyword).Select(s => new Identifier(s));

            Zero = Parse.Char('0').Then(_ => Parse.LetterOrDigit.Not()).Return(0);

            NonZeroNumeral = Parse.Chars("123456789");
            Numeral = Parse.Chars("0123456789");
            var positiveNumber =
                from first in NonZeroNumeral.Once()
                from rest in Numeral.Many()
                select int.Parse(new string(first.Concat(rest).ToArray()));

            NonNegativeNumber = Zero.XOr(positiveNumber).Then(v => IdentifierFirstChar.Not().Return(v));

            Literal = NonNegativeNumber.Isol().Select(n => new Literal(n));

            Hole = Parse.String("??").Isol().Return(new Hole());

            VariableRef = Identifier.Select(id => new VariableRef(id));


            WrappedExpression =
                from _0 in Parse.Char('(').Isol()
                from expr in Expression
                from _1 in Parse.Char(')').Isol()
                select expr;


            PropertyAccess =
                from expr in WrappedExpression.Or(VariableRef) //Expression
                from chained_ids in (
                    Parse.Char('.').Isol().Then(_ => Identifier)
                ).AtLeastOnce()
                select (PropertyAccess)chained_ids.Aggregate(expr, (e, id) => new PropertyAccess(e, id));

            Settable = PropertyAccess.Or<ISettable>(VariableRef);



            WeakVariableDeclaration =
                from type in Identifier
                from v in (
                    from e in Expression
                    where e is VariableRef v
                    select (VariableRef)e
                )
                from expr in (
                    Parse.Char('=').Isol().Then(_ => Expression)
                ).Or(Parse.Return(Empty.Instance))
                select new WeakVariableDeclaration(type, v.TargetId, expr);


            FunctionArg =
                from maybe_out in Parse.String(Keywords.Ref).Isol().Optional()
                from decl in WeakVariableDeclaration
                where decl.Def is Empty
                select (IVariableInfo)(maybe_out.IsDefined ? new RefVariableDeclaration(decl) : decl);

            FunctionArgList =
                from _0 in Parse.Char('(').Isol()
                from args in FunctionArg.DelimitedBy(Parse.Char(',').Isol()).Optional().OrEmpty()
                from _1 in Parse.Char(')').Isol()
                select args;

            FunctionModifier =
                (
                    Parse.String(Keywords.Harness).Isol().Return(SketchSyntax.FunctionModifier.Harness)
                    .Or(Parse.String(Keywords.Generator).Isol().Return(SketchSyntax.FunctionModifier.Generator))
                )
                .Optional().Select(option => option.GetOrElse(SketchSyntax.FunctionModifier.None));

            WeakFunctionSignature =
                from modifier in FunctionModifier
                from type in Identifier
                from name in Identifier
                from args in FunctionArgList
                from maybe_impl in (
                    from _0 in Parse.String(Keywords.Implements).Token()
                    from id in Identifier
                    select id
                ).Optional()
                select new WeakFunctionSignature(modifier, type, name, args.ToList()) { ImplementsId = maybe_impl.GetOrDefault() };


            ProceduralBlock =
                from _2 in Parse.Char('{').Isol()
                from body in Statement.Many()
                from _3 in Parse.Char('}').Isol()
                select body;


            Assignment =
                from subject in Settable
                from _0 in Parse.Char('=').Isol()
                from rhs in Expression
                select new Assignment(subject, rhs);

            StructNew =
                from _0 in Parse.String(Keywords.New)
                from type in Identifier
                from _1 in Parse.Char('(').Isol()
                from assigns in Assignment.DelimitedBy(Parse.Char(',').Isol()).Optional().OrEmpty()
                from _2 in Parse.Char(')').Isol()
                select new StructNew(type, assigns.ToList());

            FunctionDefinition =
                from sig in WeakFunctionSignature
                from body in ProceduralBlock
                select new FunctionDefinition(sig, body.ToList());

            StructDefinition =
                from _0 in Parse.String(Keywords.Struct).Isol()
                from id in Identifier
                from _3 in Parse.Char('{').Isol()
                from props in WeakVariableDeclaration.WithSemicolon().Many()
                from _5 in Parse.Char('}').Isol()
                select new StructDefinition(id, props.ToList());

            IfStatement =
                from _0 in Parse.String(Keywords.If).Isol()
                from cond in WrappedExpression
                from body in ProceduralBlock.Or(Statement.Once())
                from opt_rhs in (
                    from _1 in Parse.String(Keywords.Else).Isol()
                    from body_rhs in ProceduralBlock.Or(Statement.Once())
                    select body_rhs
                ).Optional()
                select new IfStatement(cond, body.ToList(), opt_rhs.IsDefined ? opt_rhs.Get().ToList() : Array.Empty<IStatement>());

            RepeatStatement =
                from _0 in Parse.String(Keywords.Repeat).Isol()
                from cond in WrappedExpression
                from body in ProceduralBlock
                select new RepeatStatement(cond, body.ToList());

            AssertStatement =
                from _0 in Parse.String(Keywords.Assert).Isol()
                from pred in Expression
                from _1 in Parse.Char(';').Isol()
                select new AssertStatement(pred);

            ReturnStatement =
                from _0 in Parse.String(Keywords.Return).Isol()
                from expr in Expression.Or(Parse.Return(Empty.Instance))
                from _1 in Parse.Char(';').Isol()
                select new ReturnStatement(expr);

            MinimizeStatement =
                from _0 in Parse.String(Keywords.Minimize).Isol()
                from expr in Expression
                from _1 in Parse.Char(';').Isol()
                select new MinimizeStatement(expr);


            CallExpression =
                from id in Identifier
                from _0 in Parse.Char('(')//.Isol()
                from args in Expression.DelimitedBy(Parse.Char(',').Isol())
                    .Optional()
                    .Select<IOption<IEnumerable<IExpression>>, IEnumerable<IExpression>>(
                        v => v.IsDefined ? v.Get() : Array.Empty<IExpression>()
                    )
                from _1 in Parse.Char(')')//.Isol()
                select new FunctionEval(id, args.ToList());



            IEnumerable<T> SortToDisambiguate<T>(IEnumerable<T> values, Func<T, string> stringify) {
                var d = values.ToDictionary(stringify);
                var strs = d.Keys.ToList();
                strs.Sort((string a, string b) => a.StartsWith(b) ? -1 : b.StartsWith(a) ? 1 : 0);
                return strs.Select(s => d[s]);
            }

            static Parser<UnaryOp> MakeUnaryOpParser(UnaryOp op) => Parse.String(op.Str()).Return(op);

            var negate = MakeUnaryOpParser(UnaryOp.Minus).Isol();

            UnaryOperator =
                MakeUnaryOpParser(UnaryOp.Not)
                .Or(
                    MakeUnaryOpParser(UnaryOp.Minus).NotRepeated()
                ).Isol();



            UnaryOperand = Literal
                .Or<IExpression>(Hole)
                .Or(StructNew)
                .Or(CallExpression)
                .Or(PropertyAccess)
                .Or(VariableRef)
                .Or(WrappedExpression);

            UnaryOperation =
                from op in UnaryOperator
                from operand in UnaryOperand
                select new UnaryOperation(op, operand);




            static Parser<Op> MakeOpParser(Op op) => Parse.String(op.Str()).Return(op).NotRepeated(); // Disallow unexpected repeats, e.g. 1 -- 2

            var opParsers = SortToDisambiguate(Enum.GetValues<Op>(), OpExtensions.Str).Select(MakeOpParser).ToList();

            InfixOperator = opParsers.Skip(1).Aggregate(opParsers[0], (a, b) => a.Or(b)).Isol();

            InfixOperand = UnaryOperation.Or(UnaryOperand);

            InfixSequence =
                from head in InfixOperand
                from tail in (
                    from op in InfixOperator
                    from operand in InfixOperand
                    select (op, operand)
                ).AtLeastOnce()
                select InfixOperation.GroupOperators(head, tail);


            AnyInfixOperation = Parse.Char('~').Many().Then(_ => LogicalAnd.Or(LogicalOr).Or(Comparison).Or(ArithMul).Or(ArithBasic));



            var ternCond = InfixSequence.Or(InfixOperand);

            Ternary =
                from cond in ternCond
                from _0 in Parse.Char('?').Isol()
                from lhs in Expression
                from _1 in Parse.Char(':').Isol()
                from rhs in Expression
                select new Ternary(cond, lhs, rhs);


            wrapExpr.Install(Ternary.Or(InfixSequence).Or(InfixOperand));

            wrapStmt.Install(
                StructDefinition
                .Or<IStatement>(IfStatement)
                .Or(RepeatStatement)
                .Or(AssertStatement)
                .Or(ReturnStatement)
                .Or(MinimizeStatement)
                .Or(FunctionDefinition)
                .Or(WeakVariableDeclaration.WithSemicolon<IStatement>())
                .Or(Assignment.WithSemicolon<IStatement>())
                .Or(CallExpression.WithSemicolonOrWeirdThing<IStatement>())
                );

            var skVersion =
                from _0 in Parse.String("SKETCH version")
                from v in Parse.Digit.AtLeastOnce().DelimitedBy(Parse.Char('.')).Token()
                select new string(v.SelectMany(c => c).ToArray());

            var bench =
                from _0 in Parse.String("Benchmark = ")
                from t in Parse.AnyChar.Until(Parse.LineEnd).Text()
                select t;

            var skDone = Parse.String("[SKETCH] DONE").Token();

            var ttime =
                from _0 in Parse.String("Total time = ")
                from t in Parse.Number.Token()
                select int.Parse(t);

            WholeFile =
                from opt_ver in skVersion.Isol().Optional()
                from opt_file in bench.Isol().Optional()
                from contents in Statement.Many()
                from _0 in skDone.Isol().Optional()
                from t in ttime.Isol().Optional()
                select new SketchFileContent(contents, opt_ver.GetOrDefault(), opt_file.GetOrDefault(), t.GetOrDefault());


        }

        //public static Parser<FunctionDefinition> AnyFunctionIn(IEnumerable<Identifier> targets) {
        //    var lookup = targets.ToHashSet();
        //    return
        //        from sig in WeakFunctionSignature
        //        where lookup.Contains(sig.Id)
        //        from body in ProceduralBlock
        //        select new FunctionDefinition(sig, body.ToList());
        //}

        public static Parser<SketchFileContent> WholeFile { get; }

        static Parser<char> MiscChar { get; }
        static Parser<char> IdentifierFirstChar { get; }
        static Parser<char> IdentifierSuffixChar { get; }

        static Parser<string> IdentifierOrKeyword { get; }

        public static Parser<Identifier> Identifier { get; }

        static Parser<int> Zero { get; }
        static Parser<char> NonZeroNumeral { get; }
        static Parser<char> Numeral { get; }


        public static Parser<int> NonNegativeNumber { get; }

        public static Parser<Literal> Literal { get; }


        public static Parser<IExpression> Expression { get; }
        public static Parser<IStatement> Statement { get; }
        public static Parser<IVariableInfo> FunctionArg { get; }
        public static Parser<IEnumerable<IVariableInfo>> FunctionArgList { get; }
        public static Parser<FunctionModifier> FunctionModifier { get; }
        public static Parser<WeakFunctionSignature> WeakFunctionSignature { get; }
        public static Parser<FunctionDefinition> FunctionDefinition { get; }
        public static Parser<StructDefinition> StructDefinition { get; }
        public static Parser<IfStatement> IfStatement { get; }
        public static Parser<RepeatStatement> RepeatStatement { get; }
        public static Parser<AssertStatement> AssertStatement { get; }
        public static Parser<ReturnStatement> ReturnStatement { get; }
        public static Parser<MinimizeStatement> MinimizeStatement { get; }
        public static Parser<WeakVariableDeclaration> WeakVariableDeclaration { get; }
        public static Parser<IEnumerable<IStatement>> ProceduralBlock { get; }
        public static Parser<ISettable> Settable { get; }
        public static Parser<Assignment> Assignment { get; }
        public static Parser<StructNew> StructNew { get; }
        public static Parser<Ternary> Ternary { get; }
        public static Parser<PropertyAccess> PropertyAccess { get; }
        public static Parser<VariableRef> VariableRef { get; }
        public static Parser<Hole> Hole { get; }
        public static Parser<FunctionEval> CallExpression { get; }
        public static Parser<IExpression> InfixOperand { get; }
        public static Parser<UnaryOp> UnaryOperator { get; }
        public static Parser<IExpression> UnaryOperand { get; }
        public static Parser<Op> InfixOperator { get; }
        public static Parser<UnaryOperation> UnaryOperation { get; }
        public static Parser<IExpression> InfixSequence { get; }
        public static Parser<IExpression> WrappedExpression { get; }
        public static Parser<IExpression> AnyInfixOperation { get; }
        public static Parser<IExpression> Comparison { get; }
        public static Parser<IExpression> LogicalAnd { get; }
        public static Parser<IExpression> LogicalOr { get; }
        public static Parser<IExpression> ArithMul { get; }
        public static Parser<IExpression> ArithBasic { get; }
    }
}

using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.Util;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("Semgus.SketchLang.Tests")]
namespace Semgus.MiniParser {

    internal class SketchSyntaxParser {
        public static SketchSyntaxParser Instance { get; } = new();


        public Parser<Identifier> Identifier { get; }

        public Parser<Literal> Literal { get; }
        public Parser<IExpression> Expression { get; }
        public Parser<IStatement> Statement { get; }
        public Parser<FunctionArg> FunctionArg { get; }
        public Parser<FunctionArg> FunctionArgList { get; }
        public Parser<FunctionSignature> FunctionSignature { get; }
        public Parser<FunctionDefinition> FunctionDefinition { get; }
        public Parser<StructDefinition> StructDefinition { get; }
        public Parser<IfStatement> IfStatement { get; }
        public Parser<RepeatStatement> RepeatStatement { get; }
        public Parser<AssertStatement> AssertStatement { get; }
        public Parser<ReturnStatement> ReturnStatement { get; }
        public Parser<MinimizeStatement> MinimizeStatement { get; }
        public Parser<VariableDeclaration> WeakVariableDeclaration { get; }
        public Parser<ISettable> Settable { get; }
        public Parser<Assignment> Assignment { get; }
        public Parser<StructNew> StructNew { get; }
        public Parser<Ternary> Ternary { get; }
        public Parser<PropertyAccess> PropertyAccess { get; }
        public Parser<Hole> Hole { get; }
        public Parser<FunctionEval> CallExpression { get; }
        public Parser<UnaryOperation> UnaryOperation { get; }
        public Parser<InfixOperation> InfixOperation { get; }
        public Parser<IStatement> FileContent { get; }

        static KeywordSymbol Kw(string s) => new(s);
        private SketchSyntaxParser() {

            HoleSymbol hole = new();
            LiteralSymbol literal = new();
            IdentifierSymbol identifier = new();
            LiteralStringSymbol literalString = new();

            var expression = new Placeholder();

            var var_ref = identifier.Transform(ctx => new VariableRef(ctx.Take<Identifier>()));
            var_ref.Name = "var_ref";

            var assignable = ((var_ref | "(" + expression + ")") + ("." + identifier).Star())
                .Transform(ctx => {
                    var head = ctx.Take<IExpression>();
                    while (ctx.TrySkipKeyword(".")) {
                        var next = ctx.Take<Identifier>();
                        head = new PropertyAccess(head, next);
                    }
                    return head;
                });

            assignable.Name = "assignable";

            var unit = literal | assignable | hole; // literal strings currently not supported as exprs

            var call = (identifier + "(" + (expression + ("," + expression).Star()).Maybe() + ")")
                .Transform(ctx => new FunctionEval(ctx.Take<Identifier>(), ctx.TakeStar<IExpression>().ToList()));
            call.Name = "call";

            var assign = (assignable + "=" + expression)
                .Transform(ctx => new Assignment(ctx.Take<ISettable>(), ctx.SkipKeyword("=").Take<IExpression>()));
            assign.Name = "assign";


            var call_ctor = ("new" + identifier + "(" + (assign + ("," + assign).Star()).Maybe() + ")")
                .Transform(ctx => new StructNew(ctx.Skip<KeywordInstance>().Take<Identifier>(), ctx.TakeStar<Assignment>()));
            call_ctor.Name = "new_struct";

            var unary = (call_ctor | call | unit);
            unary.CanDissolve = false;
            unary.list.Add(unary.Prefix("!", "-"));
            unary.Name = "unary";

            var e_mul = unary.Infix("*");
            var e_add = e_mul.Infix("+", "-");
            var e_cmp = e_add.Infix("<=", "<", ">=", ">"); // semantically binary
            var e_eq = e_cmp.Infix("==", "!=");            // semantically binary
            var e_and = e_eq.Infix("&&");
            var e_or = e_and.Infix("||");
            e_or.Name = "infix_or";


            var e_tern = (e_or + ("?" + expression + ":" + e_or).Star())
                .Transform(ctx => {
                    var e1 = ctx.Take<IExpression>();
                    while (ctx.TrySkipKeyword("?")) {
                        var e2 = ctx.Take<IExpression>();
                        var e3 = ctx.SkipKeyword(":").Take<IExpression>();
                        e1 = new Ternary(e1, e2, e3);
                    }
                    return e1;
                });

            expression.Install(e_tern);
            expression.Name = "expression";

            var variable = (identifier + identifier)
                .Transform(ctx => {
                    var typeId = ctx.Take<Identifier>();
                    var id = ctx.Take<Identifier>();
                    return new Variable(id, typeId);
                }); 
            variable.Name = "variable";

            var declare_var = (variable + ("=" + expression).Maybe())
                .Transform(ctx => {
                    var a = ctx.Take<Variable>();
                    var b = ctx.TrySkipKeyword("=") ? ctx.Take<IExpression>() : Empty.Instance;
                    return new VariableDeclaration(a, b);
                });
            declare_var.Name = "declare_var";


            var def_struct = ("struct" + identifier + "{" + (variable + ";").Some() + "}")
                .Transform(ctx => {
                    var id = ctx.SkipKeyword("struct").Take<Identifier>();
                    var decs = ctx.TakeStar<Variable>();
                    return new StructDefinition(id, decs);
                });
            def_struct.Name = "def_struct";

            var exclaim = ((Kw("assert") | "minimize" | "assume") + expression + (":" + literalString).Maybe())
                .Transform(ctx => {
                    var kw = ctx.Take<KeywordInstance>().Value;
                    switch (kw) {
                        case "assert": return new AssertStatement(ctx.Take<IExpression>());
                        case "minimize": return new MinimizeStatement(ctx.Take<IExpression>());
                        case "assume":
                            var pred = ctx.Take<IExpression>();
                            if (ctx.TrySkipKeyword(":")) {
                                ctx.Skip<LiteralString>();
                            }
                            return new AssumeStatement(pred);
                        default: throw new Exception();
                    };
                });

            var returns = ("return" + expression.Maybe())
                .Transform(ctx => ctx.Skip<KeywordInstance>().TryTake<IExpression>(out var expr) ? new ReturnStatement(expr) : new ReturnStatement());

            var line = (declare_var | assign | call | returns | exclaim) + ";";
            line.Name = "line";

            var statement = new Earliest(line) { CanDissolve = false, Name = "statement" };

            var ite = (Kw("if") + "(" + expression + ")" + (statement | "{" + statement.Star() + "}") + ("else" + (statement | "{" + statement.Star() + "}")).Maybe())
                .Transform(ctx => new IfStatement(
                    ctx.SkipKeyword("if").Take<IExpression>(),
                    ctx.TakeStar<IStatement>().ToList(),
                    (
                        ctx.TryTake<KeywordInstance>(out _)
                        ? ctx.TakeStar<IStatement>()
                        : Array.Empty<IStatement>()).ToList()
                    )
                );

            statement.list.Add(ite);

            var repeat = (Kw("repeat") + "(" + expression + ")" + (statement | "{" + statement.Star() + "}"))
                .Transform(ctx => new RepeatStatement(ctx.Skip<KeywordInstance>().Take<IExpression>(), ctx.TakeStar<IStatement>()));

            statement.list.Add(repeat);


            var fn_arg = (Kw("ref").Maybe() + variable)
                .Transform(ctx => {
                    var is_ref = ctx.TrySkipKeyword("ref");
                    return new FunctionArg(ctx.Take<Variable>(), is_ref);
                });

            var fn_arg_list = "(" + (fn_arg + ("," + fn_arg).Star()).Maybe() + ")";

            var modmaps = new Dictionary<string, FunctionModifier>() {
                {"harness",FunctionModifier.Harness },
                {"generator",FunctionModifier.Generator }
            };

            var fn_sig = ((Kw("harness") | "generator").Maybe() + identifier + identifier + fn_arg_list + ("implements" + identifier).Maybe())
                .Transform(ctx =>
                    new FunctionSignature(
                        Flag: ctx.TryTakeMappedKeyword(modmaps, out var mod) ? mod : FunctionModifier.None,
                        ReturnTypeId: ctx.Take<Identifier>(),
                        Id: ctx.Take<Identifier>(),
                        Args: ctx.TakeStar<FunctionArg>(),
                        ctx.TrySkipKeyword("implements") ? ctx.Take<Identifier>() : null
                    )
                );

            var def_function = (fn_sig + "{" + statement.Star() + "}")
                .Transform(ctx => new FunctionDefinition(Signature: ctx.Take<FunctionSignature>(), Body: ctx.TakeStar<IStatement>()));
            def_function.Name = "def_function";

            var file_content = (def_struct | def_function | declare_var).Star();

            //def_struct.SetTransformer(ctx => new StructDefinition(ctx.Skip<KeywordSymbol>().Take<Identifier>(), ctx.TakeStar<WeakVariableDeclaration>()));


            //declare_var.SetTransformer(ctx => new WeakVariableDeclaration(ctx.Take<Identifier>(), ctx.Take<Identifier>(), ctx.TryTake<IExpression>(out var expr) ? expr : Empty.Instance));





            Identifier = new(identifier);
            Literal = new(literal);
            Expression = new(expression);
            Statement = new(statement);
            FunctionArg = new(fn_arg);
            FunctionArgList = new(fn_arg_list);
            FunctionSignature = new(fn_sig);
            FunctionDefinition = new(def_function);
            StructDefinition = new(def_struct);
            IfStatement = new(ite);
            RepeatStatement = new(repeat);
            AssertStatement = new(exclaim);
            MinimizeStatement = new(exclaim);
            ReturnStatement = new(returns);
            WeakVariableDeclaration = new(declare_var);
            Settable = new(assignable);
            Assignment = new(assign);
            StructNew = new(call_ctor);
            Ternary = new(e_tern);
            PropertyAccess = new(assignable);
            Hole = new(hole);
            CallExpression = new(call);
            InfixOperation = new(e_or);
            UnaryOperation = new(unary);
            FileContent = new(file_content);
        }

        public static Result<(string header, string body, string footer), Exception> StripHeaders(string raw) {
            Regex start_of_package = new(@"/\* BEGIN PACKAGE ([a-zA-Z_0-9]+)\s*\*(/)", RegexOptions.Compiled);

            var match = start_of_package.Match(raw);

            if (!match.Success) {
                return Result.Ok<(string header, string body, string footer), Exception>((string.Empty, raw, string.Empty));
            }

            var name = match.Groups[1].Value;
            var body_start = match.Groups[2].Index + 1;

            Regex end_of_package = new(@$"(/)\* END PACKAGE {name}\s*\*/", RegexOptions.Compiled);

            var end_match = end_of_package.Match(raw);

            if (!end_match.Success) {
                return Result.Err<(string header, string body, string footer), Exception>(new Exception("Found header but not footer"));
            }

            var body_end = end_match.Groups[1].Index;

            return Result.Ok<(string header, string body, string footer), Exception>((
                raw.Substring(0, body_start),
                raw.Substring(body_start, body_end - body_start),
                raw.Substring(body_end)
            ));
        }

    }
}

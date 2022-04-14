namespace Semgus.OrderSynthesis.SketchSyntax {
    internal class FunctionDefinition : IStatement {
        public FunctionId Id { get; }

        public string? Alias { get; set; } = null;

        public FunctionFlag Flag { get; }
        public IType ReturnType { get; }
        public IReadOnlyList<VarId> Args { get; }
        public IReadOnlyList<IStatement> Body { get; }

        public FunctionDefinition(FunctionId name, FunctionFlag kind, IType return_type, IReadOnlyList<VarId> args, IReadOnlyList<IStatement> body) {
            this.Id = name;
            this.Flag = kind;
            this.ReturnType = return_type;
            this.Args = args;
            this.Body = body;
        }

        public FunctionDefinition(FunctionId name, FunctionFlag kind, IType return_type, IReadOnlyList<VarId> args, params IStatement[] body) {
            this.Id = name;
            this.Flag = kind;
            this.ReturnType = return_type;
            this.Args = args;
            this.Body = body;
        }

        static string GetPrefix(FunctionFlag flag) => flag switch {
            FunctionFlag.None => "",
            FunctionFlag.Harness => "harness ",
            FunctionFlag.Generator => "generator ",
            _ => throw new ArgumentOutOfRangeException(),
        };

        public void WriteInto(ILineReceiver lineReceiver) {
            if (Alias is not null) lineReceiver.Add($"// {Alias}");
            lineReceiver.Add($"{GetPrefix(Flag)}{ReturnType} {Id} ({string.Join(", ", Args.Select(a => a.GetArgString()))}) {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
            lineReceiver.Add(""); // blank line
        }
    }
}

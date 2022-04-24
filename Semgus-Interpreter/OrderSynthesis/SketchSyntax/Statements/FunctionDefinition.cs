using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax {

    internal record FunctionDefinition (FunctionSignature Signature, IReadOnlyList<IStatement> Body)  : IStatement  {
        public string? Alias { get; set; } = null;
        public Identifier Id => Signature.Id;

        public FunctionDefinition(FunctionSignature signature, params IStatement[] body) : this(signature, body.ToList()) { }


        public void WriteInto(ILineReceiver lineReceiver) {
            if (Alias is not null) lineReceiver.Add($"// {Alias}");
            lineReceiver.Add($"{Signature} {{");
            lineReceiver.IndentIn();
            foreach (var stmt in Body) {
                stmt.WriteInto(lineReceiver);
            }
            lineReceiver.IndentOut();
            lineReceiver.Add("}");
            lineReceiver.Add(""); // blank line
        }

        public virtual bool Equals(FunctionDefinition? other) =>
            other is not null &&
            Signature.Equals(other.Signature) &&
            EqualityComparer<string>.Default.Equals(Alias,other.Alias) &&
            Body.SequenceEqual(other.Body);



        public override string ToString() => this.PrettyPrint(true);

    }
}

namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record VariableRef  (Identifier TargetId)  : IExpression, ISettable  {
        public override string ToString() => TargetId.ToString();
    }
}

﻿namespace Semgus.OrderSynthesis.SketchSyntax {
    internal record Assignment  (ISettable Subject, IExpression Value)  : IStatement, IEquatable<Assignment?>  {

        public void WriteInto(ILineReceiver lineReceiver) {
            lineReceiver.Add($"{Subject} = {Value};");
        }

        public override string ToString() => $"{Subject} = {Value}";
    }
}

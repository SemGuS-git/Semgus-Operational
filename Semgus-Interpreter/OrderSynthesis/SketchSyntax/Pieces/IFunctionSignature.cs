namespace Semgus.OrderSynthesis.SketchSyntax {
    internal interface IFunctionSignature {
        FunctionModifier Flag { get; }
        Identifier ReturnTypeId { get; }
        Identifier Id { get; }

        Identifier? ImplementsId { get; }

        IReadOnlyList<IVariableInfo> Args { get; }

        FunctionSignature AsHydrated(IReadOnlyDictionary<Identifier, IType> typeDict, Identifier? replacement_id = null);

        IFunctionSignature AsFunctional();
    }
}

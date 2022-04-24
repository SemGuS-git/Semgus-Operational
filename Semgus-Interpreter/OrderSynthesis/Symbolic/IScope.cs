using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {
    internal interface IScope {
        IEnumerator<IStatement> Enumerator { get; }
        IEnumerable<KeyValuePair<Identifier, IExpression>> GetSideEffectAssigns();


        Stack<ExpressionScope> PendingStack { get; }


        bool TryGetLocalValue(Identifier identifier, out IExpression expr);


        void OnPop(ScopeStack stack);

        void Declare(Identifier id, IExpression expression);
        void Assign(Identifier id, IExpression expression);
    }
}
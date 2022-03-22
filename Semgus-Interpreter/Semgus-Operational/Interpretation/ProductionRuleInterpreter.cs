using Semgus.Model;

namespace Semgus.Operational {
    public class ProductionRuleInterpreter {
        public SemgusTermType TermType { get; }
        public SemgusTermType.Constructor SyntaxConstructor { get; }

        public IReadOnlyList<VariableInfo> InputVariables { get; }
        public IReadOnlyList<VariableInfo> OutputVariables { get; }

        // Maximum number of auxiliary variables used in any of this production's semantic rules.
        public int ScratchSize { get; private set; } = 0;
        public int MemorySize => InputVariables.Count + OutputVariables.Count + ScratchSize;

        public IReadOnlyList<SemanticRuleInterpreter> Semantics => _semantics;
        private readonly List<SemanticRuleInterpreter> _semantics = new();

        public ProductionRuleInterpreter(SemgusTermType termType, SemgusTermType.Constructor syntaxConstructor, IReadOnlyList<VariableInfo> inputVariables, IReadOnlyList<VariableInfo> outputVariables) {
            TermType = termType;
            SyntaxConstructor = syntaxConstructor;
            InputVariables = inputVariables;
            OutputVariables = outputVariables;
        }

        public void AddSemanticRule(SemanticRuleInterpreter rule) {
            _semantics.Add(rule);
            var n_aux = rule.AuxiliaryVariables.Count;
            if (ScratchSize < n_aux) ScratchSize = n_aux; 
        }

        // Interpret a syntax node.
        // Results are stored by mutating the values of the argument variables.
        public void Interpret(EvaluationContext context, InterpreterState state) {

            for (int i = 0; i < _semantics.Count;i++) {
                if (_semantics[i].TryInterpret(context, state) || state.HasError) return;
            }

            var node = context.ThisTerm;
            throw new InterpreterLanguageException("DSL runtime error: no valid semantics",node,node.LabelInputs(context.Variables));
        }

        public override string ToString() => $"{SyntaxConstructor.Operator.Symbol}"; // TODO
    }
}
//namespace Semgus.Interpretation {
//    /// <summary>
//    /// Collects and processes information obtained during predicate analysis.
//    /// 
//    /// Note in particular the use of a variable dependency graph to determine the order
//    /// in which to execute assignment statements.
//    /// </summary>
//    public class OperationalAnalysisResult {
//        private readonly SyntaxConstraint _syntax;
//        public VariableTracker Variables { get; }
//        public StatementOrganizer Clauses { get; } = new();

//        public OperationalAnalysisResult(SyntaxConstraint syntax, VariableTracker variableTracker) {
//            this._syntax = syntax;
//            this.Variables = variableTracker;
//        }

//        public ProductionRuleInterpreter GetProductionIntepreter() => new(_syntax, Variables.Input, Variables.Output);

//        public SemanticRuleInterpreter AddSemanticInterpeterTo(ProductionRuleInterpreter prodInterpreter) {
//            var semInterpreter = new SemanticRuleInterpreter(prodInterpreter, Variables.Auxiliary, Clauses.Resolve(Variables));
//            prodInterpreter.AddSemanticRule(semInterpreter);
//            return semInterpreter;
//        }
//    }
//}
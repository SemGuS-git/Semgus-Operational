//using Semgus.Syntax;
//using System.Collections.Generic;
//using System.Linq;

//namespace Semgus.Interpretation {
//    public class OperationalSemanticsAnalyzer {
//        public Theory Theory { get; }
//        private readonly IReadOnlyDictionary<SemanticRelationDeclaration, SemanticRelationInstance> _relationInstanceMap;

//        public OperationalSemanticsAnalyzer(Theory theory, IEnumerable<SemanticRelationInstance> relationInstances) {
//            this.Theory = theory;
//            this._relationInstanceMap = relationInstances.ToDictionary(r => r.Relation, r => r);
//        }

//        public SemanticRelationInstance GetRelationInstance(SemanticRelationDeclaration relation) => _relationInstanceMap[relation];

//        /// <summary>
//        /// Attempts to convert <paramref name="predicate"/> into an evaluation semantics
//        /// (i.e., a sequence of interpretable statements that assign values to variables).
//        /// 
//        /// This makes strict assumptions about the shape of the predicate.
//        /// * At the top level, it is either an `and` of one or more `clause`s, or a single `clause`.
//        /// * Each `clause` is either a local Boolean formula or a semantic relation query.
//        /// * The clauses themselves must match other expectations described in the comments below.
//        /// 
//        /// If the predicate does not match the expected shape, an exception will be thrown.
//        /// </summary>
//        public OperationalAnalysisResult AnalyzePredicate(SemanticRelationInstance relationInstance, SyntaxConstraint syntax, IFormula predicate) {
//            var builder = new OperationalAnalysisInstance(this, syntax, relationInstance);
            
//            const string NAME_AND = "and";
//            if(predicate is LibraryFunctionCall call && call.LibraryFunction.Name == NAME_AND) {
//                // Treat top-level "and" as conjunction of clauses
//                foreach (var arg in call.Arguments) {
//                    builder.AnalyzeClause(arg);
//                }
//            } else {
//                builder.AnalyzeClause(predicate);
//            }

//            return builder.Result;
//        }
//    }
//}
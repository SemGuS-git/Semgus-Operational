//using Semgus.Syntax;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Semgus.Interpretation {
//    internal class OperationalAnalysisInstance {
//        private readonly OperationalSemanticsAnalyzer _top;
//        private readonly IReadOnlyDictionary<string, TermVariableInfo> _termMap;
//        public OperationalAnalysisResult Result { get; }

//        public OperationalAnalysisInstance(OperationalSemanticsAnalyzer top, SyntaxConstraint syntax, SemanticRelationInstance relationInstance) {
//            _top = top;
//            _termMap = syntax.ChildTerms.Prepend(syntax.TermVariable).ToDictionary(t => t.Name);

//            var tracker = VariableTracker.DeriveFrom(top.Theory, relationInstance);
//            Result = new OperationalAnalysisResult(syntax, tracker);
//        }

//        /// <summary>
//        /// Each clause must either be a library function call or an instance of a semantic relation.
//        /// </summary>
//        /// <param name="clause"></param>
//        /// <param name="results"></param>
//        public void AnalyzeClause(IFormula clause) {
//            switch (clause) {
//                case SemanticRelationQuery query: AnalyzeQuery(query); return;
//                case LibraryFunctionCall call: AnalyzeCallFormula(call); return;
//                // TODO: support boolean variables, literals, library constants
//                default: throw new ArgumentException($"Unsupported clause type {clause.GetType().Name}");
//            }
//        }

//        /// <summary>
//        /// If a clause is a formula whose root node is a library function call, attempt to categorize it in
//        /// one of two ways:
//        /// 1. As a statement assigning the result of some local formula to a non-input variable;
//        /// 2. As a precondition on zero or more input variables.
//        /// </summary>
//        /// <param name="call"></param>
//        /// <param name="results"></param>
//        private void AnalyzeCallFormula(LibraryFunctionCall call) {
//            // Check whether this is "shaped like" a variable assignment statement
//            if (IsShapedLikeAssignment(call, out var subject) && TryInterpretEqualityAsPossibleAssignment(call, subject, call.Arguments[1])) return;

//            // Otherwise, try to treat this as a condition
//            var dependencies = CollectVariableTerms(call, new HashSet<VariableInfo>());

//            var expr = ToInterpretableExpression(call);
//            if (expr.ResultType != typeof(bool)) throw new ArgumentException();

//            Result.Clauses.Add(new ConditionalAssertion(expr, dependencies));
//        }

//        // TODO make symmetric
//        private bool IsShapedLikeAssignment(LibraryFunctionCall call, out VariableInfo subject) {
//            // TODO make symmetric
//            if (call.LibraryFunction.Name == BasicLibrary.NAME_EQ && call.Arguments.Count == 2 && call.Arguments[0] is VariableEvaluation varEval) {
//                subject = Result.Variables.Map(varEval.Variable);
//                return true;
//            } else {
//                subject = default;
//                return false;
//            }
//        }

//        private bool TryInterpretEqualityAsPossibleAssignment(IFormula fullFormula, VariableInfo subject, IFormula rhsFormula) {
//            // Cannot assign to input variable
//            if (subject.Usage == VariableUsage.Input) return false;

//            var dependencies = CollectVariableTerms(rhsFormula, new HashSet<VariableInfo>());

//            // LHS variable not permitted in RHS expression
//            if (dependencies.Contains(subject)) return false;

//            Result.Clauses.Add(new AmbiguousVariableEquality(
//                ToInterpretableExpression(fullFormula),
//                ToInterpretableExpression(rhsFormula),
//                subject,
//                dependencies.ToList()
//            ));

//            return true;
//        }


//        /// <summary>
//        /// If a clause is a semantic relation instance, assume that it is setting some output variable(s)
//        /// equal to the result of evaluating the child term on the provided input formula(s).
//        /// </summary>
//        /// <param name="query"></param>
//        /// <param name="results"></param>
//        private void AnalyzeQuery(SemanticRelationQuery query) {
//            var n = query.Terms.Count;

//            // Assert query is (Term, ...)
//            if (!IsEvaluableTerm(query.Terms[0], out var term)) {
//                throw new ArgumentException($"Invalid first element in relational query {query.PrintFormula()}");
//            }

//            var inputVariables = new List<VariableInfo>();
//            var outputVariables = new List<VariableInfo>();

//            var relInstance = _top.GetRelationInstance(query.Relation);

//            // Break into input, output
//            for (int i = 1; i < n; i++) {
//                var arg = query.Terms[i];

//                // For now, require that each argument is a plain variable reference
//                if (arg is not VariableEvaluation varEval) throw new NotSupportedException($"Unsupported term {arg} in semantic relation query {query.PrintFormula()}: only variables are permitted");

//                var info = Result.Variables.Map(varEval.Variable);

//                (VariableUtil.IsFlaggedAsOutput(relInstance, i) ? outputVariables : inputVariables).Add(info);
//            }

//            Result.Clauses.Add(new TermEvaluation(term, inputVariables, outputVariables));
//        }

//        private bool IsEvaluableTerm(IFormula formula, out TermVariableInfo info) {
//            if(formula is VariableEvaluation ve && ve.Variable is NonterminalTermDeclaration v && (
//                v.DeclarationContext is VariableDeclaration.Context.PR_Subterm or VariableDeclaration.Context.NT_Term
//            )) {
//                if (!_termMap.TryGetValue(v.Name, out info)) throw new KeyNotFoundException($"Term {v.Name} is not in-scope");
//                if (v.Nonterminal != info.Nonterminal) throw new InvalidOperationException($"Nonterminal mismatch for variable {v.Name}: {v.Nonterminal} vs {info.Nonterminal}");
//                return true;
//            } else {
//                info = default;
//                return false;
//            }
//        }

//        /// <summary>
//        /// Collect information about all variables that are referenced in the given formula.
//        /// </summary>
//        /// <param name="formula"></param>
//        /// <param name="result">The collection into which to insert discovered variables</param>
//        /// <returns><paramref name="result"/></returns>
//        private HashSet<VariableInfo> CollectVariableTerms(IFormula formula, HashSet<VariableInfo> result) {
//            switch (formula) {
//                case LibraryFunctionCall call:
//                    foreach (var arg in call.Arguments) CollectVariableTerms(arg, result);
//                    return result;

//                case VariableEvaluation vareval:
//                    var vardec = vareval.Variable;
//                    result.Add(Result.Variables.Map(vardec));
//                    return result;

//                case LiteralBase:
//                case LibraryDefinedSymbol:
//                    return result;

//                default:
//                    throw new ArgumentException();
//            }
//        }

//        private ISmtLibExpression ToInterpretableExpression(IFormula formula) => formula switch {
//            LibraryFunctionCall call => 
//                new FunctionCallExpression(formula, _top.Theory.GetFunction(call.LibraryFunction), call.Arguments.Select(ToInterpretableExpression).ToList()),
//            VariableEvaluation varEval =>
//                new VariableEvalExpression(formula, Result.Variables.Map(varEval.Variable)),
//            LiteralBase lit =>
//                new LiteralExpression(formula, lit),
//            LibraryDefinedSymbol sym =>
//                new LiteralExpression(formula, _top.Theory.GetConstant(sym.Identifier)),
//            _ => throw new ArgumentException(),
//        };
//    }
//}
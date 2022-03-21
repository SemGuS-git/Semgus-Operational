//using Semgus.Syntax;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Semgus.Interpretation {
//    public class VariableTracker {
//        private readonly Theory _theory;
//        public IReadOnlyList<VariableInfo> Input { get; }
//        public IReadOnlyList<VariableInfo> Output { get; }
//        public IReadOnlyList<VariableInfo> Auxiliary => _auxiliary;

//        private readonly List<VariableInfo> _auxiliary = new();

//        private readonly Dictionary<string, VariableInfo> _nameMap;

//        public VariableTracker(Theory theory, IReadOnlyList<VariableDeclaration> inputVariables, IReadOnlyList<VariableDeclaration> outputVariables) {
//            _theory = theory;
//            Input = Convert(theory, inputVariables, 0, VariableUsage.Input);
//            Output = Convert(theory, outputVariables, Input.Count, VariableUsage.Output);
//            _nameMap = Input.Concat(Output).ToDictionary(info => info.Name);
//        }

//        private static IReadOnlyList<VariableInfo> Convert(Theory theory, IReadOnlyList<VariableDeclaration> variables, int start, VariableUsage usage) {
//            var array = new VariableInfo[variables.Count];

//            for (int i = 0; i < array.Length; i++) {
//                var v = variables[i];
//                array[i] = new VariableInfo(v.Name, start + i, theory.GetType(v.Type), usage);
//            }

//            return array;
//        }

//        public VariableInfo Map(VariableDeclaration v) {
//            var name = v.Name;
//            if (_nameMap.TryGetValue(name, out var info)) return info;

//            info = new VariableInfo(name, _nameMap.Count, _theory.GetType(v.Type), VariableUsage.Auxiliary);
//            _auxiliary.Add(info);
//            _nameMap.Add(name, info);
//            return info;
//        }

//        public static VariableTracker DeriveFrom(Theory theory, SemanticRelationInstance relationInstance) {
//            int n = relationInstance.Elements.Count;
//            if (n != relationInstance.ElementAnnotations.Count) throw new ArgumentException(); // sanity check

//            List<VariableDeclaration> inputs = new(), outputs = new();

//            for (int i = 1; i < n; i++) {
//                (VariableUtil.IsFlaggedAsOutput(relationInstance, i) ? outputs : inputs).Add(relationInstance.Elements[i]);
//            }

//            return new(theory, inputs, outputs);
//        }

//        public override string ToString() => $"in: [{GetNamesText(Input)}], out: [{GetNamesText(Output)}, aux: [{GetNamesText(Auxiliary)}]";
//        private static string GetNamesText(IReadOnlyList<VariableInfo> list) => string.Join(", ", list.Select(v => v.Name));
//    }
//}
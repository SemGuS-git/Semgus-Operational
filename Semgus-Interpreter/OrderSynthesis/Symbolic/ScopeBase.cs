using Semgus.MiniParser;

namespace Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation {

    internal abstract class ScopeBase : IScope {
        protected Dictionary<Identifier, IExpression> LocalAssigns { get; } = new();
        protected HashSet<Identifier> LocalDefines { get; } = new();
        public Stack<ExpressionScope> PendingStack { get; } = new();
        public IEnumerator<IStatement> Enumerator { get; }

        protected Dictionary<Identifier, StructValuePlaceholder> StructPlaceholders { get; } = new();

        public ScopeBase(IEnumerable<IStatement> statements) {
            Enumerator = statements.GetEnumerator();
        }

        public virtual bool TryGetLocalValue(Identifier identifier, out IExpression value) => LocalAssigns.TryGetValue(identifier, out value);

        public void Declare(Identifier id, IExpression expression) {
            switch (expression) {
                case StructNew struct_value:
                    expression = DeclarePlaceholderFromStruct(id, struct_value);
                    break;
                case StructValuePlaceholder placeholder:
                    expression = DeclareCloneOfPlaceholder(id, placeholder);
                    break;
            }

            if (!LocalDefines.Add(id)) throw new InvalidOperationException("Redefinition of variable in scope");
            if (!LocalAssigns.TryAdd(id, expression)) throw new InvalidOperationException("Variable was assigned before definition in scope");
        }

        private IExpression DeclarePlaceholderFromStruct(Identifier id, StructNew struct_value) {
            var flat_to_prop_map = DeclareStructProps(id, struct_value.Args, id, false);
            var placeholder = new StructValuePlaceholder(id, struct_value, flat_to_prop_map);
            StructPlaceholders.Add(id, placeholder);
            return placeholder;
        }

        private IExpression DeclareCloneOfPlaceholder(Identifier id, StructValuePlaceholder obj) {
            var flat_to_prop_map = DeclareStructProps(id, obj.Source.Args, obj.Id, true);
            var placeholder = new StructValuePlaceholder(id, obj.Source, flat_to_prop_map);
            StructPlaceholders.Add(id, placeholder);
            return placeholder;
        }

        private Dictionary<Identifier, Identifier> DeclareStructProps(Identifier dest_id, IEnumerable<Assignment> args, Identifier src_id, bool lookup) {
            var flat_to_prop_map = new Dictionary<Identifier, Identifier>();

            foreach (var (prop_id, prop_source_value) in args.Select(arg => (((VariableRef)arg.Subject).TargetId, arg.Value))) {
                var flat_id = new Identifier($"{dest_id}.{prop_id}");
                flat_to_prop_map.Add(flat_id, prop_id);

                if (lookup) {
                    var lookup_id = new Identifier($"{src_id}.{prop_id}");
                    if (TryGetLocalValue(lookup_id, out var overwrite)) {
                        Declare(flat_id, overwrite);
                        continue;
                    }
                }
                Declare(flat_id, prop_source_value);
            }

            return flat_to_prop_map;
        }

        public void Assign(Identifier id, IExpression expression) {
            switch (expression) {
                case StructNew struct_value:
                    expression = AssignPlaceholderFromStruct(id, struct_value);
                    break;
                case StructValuePlaceholder placeholder:
                    expression = AssignCloneOfPlaceholder(id, placeholder);
                    break;
            }
            LocalAssigns[id] = expression;
        }

        private IExpression AssignPlaceholderFromStruct(Identifier id, StructNew struct_value) {
            var flat_to_prop_map = new Dictionary<Identifier, Identifier>();

            foreach (var m in struct_value.Args) {
                var prop_id = ((VariableRef)m.Subject).TargetId;
                var flat_id = new Identifier($"{id}.{prop_id}");

                flat_to_prop_map.Add(flat_id, prop_id);
                Assign(flat_id, m.Value);
            }
            var placeholder = new StructValuePlaceholder(id, struct_value, flat_to_prop_map);
            StructPlaceholders[id] = placeholder;
            return placeholder;
        }


        private IExpression AssignCloneOfPlaceholder(Identifier id, StructValuePlaceholder obj) {
            var flat_to_prop_map = new Dictionary<Identifier, Identifier>();

            foreach (var m in obj.Source.Args) {
                var prop_id = ((VariableRef)m.Subject).TargetId;
                var flat_id = new Identifier($"{id}.{prop_id}");
                flat_to_prop_map.Add(flat_id, prop_id);

                var lookup_id = new Identifier($"{obj.Id}.{prop_id}");
                if (TryGetLocalValue(lookup_id, out var overwrite)) {
                    Assign(flat_id, overwrite);
                } else {
                    Assign(flat_id, m.Value);
                }
            }
            var placeholder = new StructValuePlaceholder(id, obj.Source, flat_to_prop_map);
            StructPlaceholders[id] = placeholder;
            return placeholder;
        }

        public IEnumerable<KeyValuePair<Identifier, IExpression>> GetSideEffectAssigns() {
            return LocalAssigns.Where(kvp => !LocalDefines.Contains(kvp.Key));
        }

        public void BakeAllStructVars() {
            foreach (var (id, placeholder) in StructPlaceholders) {
                // check against overwrite by something else (should not happen)
                if (!LocalAssigns.TryGetValue(id, out var current) || current is not StructValuePlaceholder) continue;

                var overwrites = new Dictionary<Identifier, IExpression>();

                foreach (var (flat_id, prop_id) in placeholder.FlatToPropMap) {
                    LocalDefines.Remove(flat_id); // this may not have been set if the struct was an input variable
                    if (LocalAssigns.Remove(flat_id, out var value)) overwrites.Add(prop_id, value);
                }

                LocalAssigns[id] = placeholder.Source.WithOverwrites(overwrites);
            }
            StructPlaceholders.Clear();
        }

        public abstract void OnPop(ScopeStack stack);
    }

}

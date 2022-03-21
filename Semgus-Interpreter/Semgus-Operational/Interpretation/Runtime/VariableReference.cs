namespace Semgus.Interpretation {
    /// <summary>
    /// Slot to receive the value assigned to a variable.
    /// May correspond to an input or output.
    /// </summary>
    public class VariableReference {
        public bool HasValue { get; private set; } = false;
        public object Value => HasValue ? _value : throw new InvalidOperationException("Attempting to access an undefined variable");
        private object _value;

        /// <summary>
        /// Set this variable equal to some value.
        /// This can only be done if the variable currently has no value.
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(object value) {
            if (HasValue) throw new InvalidOperationException("Attempting to redefine a variable");
            _value = value;
            HasValue = true;
        }

        public override string ToString() => HasValue ? _value.ToString() : "None";
    }
}
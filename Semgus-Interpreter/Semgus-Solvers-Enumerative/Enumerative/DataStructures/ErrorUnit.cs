using System;
using System.Collections.Generic;

namespace Semgus.Solvers.Enumerative {
    public class ErrorUnit : IEquatable<ErrorUnit> {
        public static ErrorUnit Instance { get; } = new ErrorUnit();
        public static ErrorUnit[] InArray { get; } = new[] { Instance }; // todo make ireadonlylist?

        private ErrorUnit() { }

        public override bool Equals(object obj) => obj is ErrorUnit;
        public bool Equals(ErrorUnit other) => other is not null;
        public override int GetHashCode() => 188212555;
        public static bool operator ==(ErrorUnit left, ErrorUnit right) => EqualityComparer<ErrorUnit>.Default.Equals(left, right);
        public static bool operator !=(ErrorUnit left, ErrorUnit right) => !(left == right);
    }
}

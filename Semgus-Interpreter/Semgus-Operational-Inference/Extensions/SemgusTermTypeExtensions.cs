using Semgus.Model;

namespace Semgus {
    internal static class SemgusTermTypeExtensions {
        public static string StringName(this SemgusTermType a) => a.Name.AsString();
    }
}

//using Semgus.Syntax;

//namespace Semgus.Interpretation {
//    public static class VariableUtil {
//        /// <summary>
//        /// Returns true if the <paramref name="i"/>th element of <paramref name="relationInstance"/> has an :out annotation.
//        /// </summary>
//        /// <param name="relationInstance"></param>
//        /// <param name="i"></param>
//        /// <returns></returns>
//        public static bool IsFlaggedAsOutput(SemanticRelationInstance relationInstance, int i) {
//            var anno = relationInstance.ElementAnnotations[i];
//            return anno is not null && anno.Annotations.ContainsKey("out");
//        }
//    }
//}
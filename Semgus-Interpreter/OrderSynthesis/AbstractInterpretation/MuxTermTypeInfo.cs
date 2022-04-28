using Semgus.Model;

namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class MuxTermTypeInfo {
        public SemgusTermType TermType { get; }

        public MuxTupleType Inputs { get; }
        public MuxTupleType Outputs { get; }
    }



    //internal class Agglom {
    //    SemgusTermType TermType { get; }
    //    SynthComparisonFunction CompareIn { get; }
    //    SynthComparisonFunction CompareOut { get; }
    //}

    //internal class Interval {

    //}
    //internal class SemRelArgTupleType {

    //}

    //internal class SynthComparisonFunction {
    //    public bool Leq() {

    //    }
    //}
}

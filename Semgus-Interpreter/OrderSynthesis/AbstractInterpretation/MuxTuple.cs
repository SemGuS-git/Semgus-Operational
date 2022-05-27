namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class MuxTuple {

        public MuxTupleType TupleType { get; }
        public dynamic[] Values { get; }

        public MuxTuple(MuxTupleType tupleType, dynamic[] result) {
            this.TupleType = tupleType;
            this.Values = result;
        }

        public dynamic this[int i] {
            get { return Values[i]; }
            set { Values[i] = value; }
        }

        public override string ToString() => $"({string.Join(", ", Values)})";
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

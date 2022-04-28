namespace Semgus.OrderSynthesis.AbstractInterpretation {
    internal class MuxTuple {

        public MuxTupleType TupleType { get; }
        public object[] Values { get; }

        public MuxTuple(MuxTupleType tupleType, object[] result) {
            this.TupleType = tupleType;
            this.Values = result;
        }

        public object this[int i] {
            get { return Values[i]; }
            set { Values[i] = value; }
        }
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

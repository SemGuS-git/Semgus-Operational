using Semgus.Model.Smt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.TheoryImplementation {

    public interface ISortHelper {
        public bool TryGetSort(SmtSortIdentifier id, out SmtSort sort);
    }
}

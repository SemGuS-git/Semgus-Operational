using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semgus.Operational {
    public class DemoBlock {
        public IDSLSyntaxNode Program { get; }
        public IReadOnlyList<object?[]> ArgLists { get; }

        public DemoBlock(IDSLSyntaxNode program, IReadOnlyList<object?[]> argLists) {
            Program = program;
            ArgLists = argLists;
        }
    }
}
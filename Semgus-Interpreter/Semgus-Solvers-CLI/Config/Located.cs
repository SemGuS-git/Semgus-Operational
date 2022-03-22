using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Semgus.CommandLineInterface {
    public record Located<T>(string WorkingDirectory, string Stem, T Value) {
        public string GetFilePath(string file) => Path.GetFullPath(Path.Combine(WorkingDirectory, file));
        public string GetIdentifier(string file) => Path.Combine(Path.GetRelativePath(Stem, WorkingDirectory), file);
    }

    public static class Located {
        public static IEnumerable<Located<T1>> Transfer<T0, T1>(IEnumerable<Located<T0>> enumerable, string stem, Func<T0, IEnumerable<T1>> selector) { 
        return enumerable.SelectMany(item =>
                selector(item.Value).Select(subItem =>
                    new Located<T1>(item.WorkingDirectory, stem, subItem)
                )
            );
        }
    }
}
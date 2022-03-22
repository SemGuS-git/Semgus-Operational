using System;
using System.Collections.Generic;
using System.Linq;
using Tomlyn.Model;

namespace Semgus.CommandLineInterface {
    public static class TomlTableExtensions {

        public static bool TryGetValue<T>(this TomlTable table, string key, Func<object, T> converter, out T value) {
            if (!table.TryGetValue(key, out var obj)) { value = default; return false; }
            value = converter(obj); // may throw exception
            return true;
        }

        public static bool TryGetValue<T>(this TomlTable table, string key, out T value) {
            if (!table.TryGetValue(key, out var obj)) { value = default; return false; }
            value = (T)obj; // may throw InvalidCastException
            return true;
        }

        public static T GetValueOrDefault<T>(this TomlTable table, string key, Func<object, T> converter, T defaultValue) {
            if (!table.TryGetValue(key, out var obj)) return defaultValue;
            return converter(obj); // may throw exception
        }

        public static T GetValueOrDefault<T>(this TomlTable table, string key, T defaultValue) {
            if (!table.TryGetValue(key, out var obj)) return defaultValue;
            return (T)obj; // may throw InvalidCastException
        }

        public static T GetValue<T>(this TomlTable table, string key, Func<object, T> converter, bool required) {
            if (!table.TryGetValue(key, out var obj)) {
                if (required) throw new KeyNotFoundException();
                else return default;
            }
            return converter(obj); // may throw exception
        }

        public static T GetValue<T>(this TomlTable table, string key, bool required) {
            if (!table.TryGetValue(key, out var obj)) {
                if (required) throw new KeyNotFoundException();
                else return default;
            }
            return (T)obj; // may throw InvalidCastException
        }



        public static IReadOnlyList<T> GetStructuredList<T>(this TomlTable table, string key, Func<TomlTable, T> converter, bool required) {
            if (!table.TryGetToml(key, out var obj)) {
                if (required) {
                    throw new KeyNotFoundException();
                } else {
                    return Array.Empty<T>();
                }
            }
            if (obj is not TomlTableArray tableArray) throw new ArgumentException();
            return tableArray.Select(converter).ToList();
        }

        public static IReadOnlyList<IReadOnlyDictionary<string, object>> GetListOfObjects(this TomlTable table, string key, bool required) {
            if (!table.TryGetToml(key, out var obj)) {
                if (required) {
                    throw new KeyNotFoundException();
                } else {
                    return Array.Empty<IReadOnlyDictionary<string, object>>();
                }
            }
            if (obj is not TomlArray array) throw new ArgumentException();
            return array.Select(k=>ToAtomDictionary((TomlTable)k)).ToList();
        }

        public static IReadOnlyList<T> GetAtomList<T>(this TomlTable table, string key, bool required) {
            if (!table.TryGetToml(key, out var obj)) {
                if (required) {
                    throw new KeyNotFoundException();
                } else {
                    return Array.Empty<T>();
                }
            }
            if (obj is not TomlArray array) throw new ArgumentException();

            // Have the array extract its own inner values (during enumeration), then cast each to the output type
            return array.Cast<T>().ToList();
        }

        public static IReadOnlyDictionary<string,object> GetDictionary(this TomlTable table, string key, bool required) {
            if (!table.TryGetToml(key, out var obj)) {
                if (required) {
                    throw new KeyNotFoundException();
                } else {
                    return new Dictionary<string, object>();
                }
            }
            if (obj is not TomlTable tableObj) throw new ArgumentException();
            return tableObj.ToAtomDictionary();
        }

        public static IReadOnlyDictionary<string, object> ToAtomDictionary(this TomlTable table) => table.GetTomlEnumerator().ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToAtom());
        
        // warning: this currently infers type from the toml string format, not from any contextual expectations of the type.
        // e.g., must have a decimal point to indicate that a number is a float rather than an int.
        public static object ToAtom(this TomlObject obj) => obj.Kind switch {
            ObjectKind.Table => ((TomlTable)obj).ToAtomDictionary(),
            ObjectKind.TableArray => ((TomlTableArray)obj).Select(ToAtomDictionary).ToList(),
            ObjectKind.Array => ((TomlArray)obj).GetTomlEnumerator().Select(ToAtom).ToList(),
            ObjectKind.Boolean => (bool)((TomlValue)obj).ValueAsObject,
            ObjectKind.String => (string)((TomlValue)obj).ValueAsObject,
            ObjectKind.Integer => Convert.ToInt32(((TomlValue)obj).ValueAsObject),
            ObjectKind.Float => Convert.ToDouble(((TomlValue)obj).ValueAsObject),
            _ => throw new NotSupportedException($"Toml object of kind {obj.Kind} cannot be used as a value"),
        };
    }
}
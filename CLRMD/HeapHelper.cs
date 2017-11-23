using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLRMD
{
    public static class HeapHelper
    {
        public class Object : Field
        {
            new public ulong Size { get; }

            public Segment Segment { get; internal set; }

            internal IList<ClrInstanceField> instanceFields;
            internal IList<ClrStaticField> staticFields;
            internal IList<ClrThreadStaticField> threadStaticField;
            internal IList<ClrMethod> methods;

            public List<Field> Fields { get; }

            public List<string> Methods { get; }

            public Object(ulong obj, ClrType type, Segment segment, string name = "") : base(obj, type, name)
            {
                Fields = new List<Field>();
                Methods = new List<string>();
                Size = type.GetSize(obj);

                instanceFields = Type.Fields;
                staticFields = Type.StaticFields;
                threadStaticField = Type.ThreadStaticFields;
                methods = Type.Methods;
                
                foreach (var field in instanceFields)
                    Fields.Add(new Field(obj, field));
                //foreach (var field in staticFields)
                //    Fields.Add(new Field(obj, field));
                //foreach (var field in threadStaticField)
                //    Fields.Add(new Field(obj, field));

                foreach (var method in methods)
                    Methods.Add(method.ToString());
                Console.Write("{0}",Name);
                Console.Write("{0}",Size);
                Console.Write("{0}",Address);
                Console.Write("{0}");
                Console.Write("{0}");
                Console.Write("{0}");
                /***************************************************************************/
                if (type.ToString().Contains("FloatAddition"))
                {
                    Console.WriteLine("Name\tSize\tAddress\tElement Type\tType\tValue");
                    foreach (var field in type.Fields)
                    {
                        uint count;
                        ulong size;
                        ObjSize(type.Heap, obj, out count, out size);
                        Console.WriteLine("{0}\t{1}\t{2:X}\t{3}\t{4}\t{5}\t{6}\t{7}", field.Name, field.Size, field.GetAddress(obj), field.ElementType, field.Type, GetValue(obj, field), size, count);
                    }
                }
                /***************************************************************************/

            }
        }


        public class Field
        {
            public string Name { get; }
            public int Size { get; }
            public ulong Address { get; }

            public string Value { get; }

            public ClrType Type { get; }

            public Field(ulong obj, ClrInstanceField field)
            {
                Name = field.Name;
                Size = field.Size;
                Address = field.GetAddress(obj);

                Type = field.Type;

                Value = GetValue(obj, field);
            }

            public Field(ulong obj, ClrStaticField field)
            {
                var appDomain = Type.Heap.Runtime.AppDomains[0];
                Name = field.Name;
                Size = field.Size;
                Address = field.GetAddress(appDomain);

                Type = field.Type;

                Value = GetValue(obj, field, appDomain);
            }

            //public Field(ulong obj, ClrThreadStaticField field)
            //{
            //    var appDomain = Type.Heap.Runtime.AppDomains[0];
            //    Name = field.Name;
            //    Size = field.Size;
            //    Address = field.GetAddress(appDomain);

            //    Type = field.Type;

            //    Value = GetValue(obj, field, appDomain);
            //}

            internal Field(ulong obj, ClrType type, string name = "")
            {
                Name = name;
                Type = type;

                Address = obj;
            }
        }





        public enum Segment
        {
            Ephemeral,  //Gen 0 + 1
            Large,      //Gen 2, size < 85,000 bytes
            Segment     //size > 85,000
        }

        static private void ObjSize(ClrHeap heap, ulong obj, out uint count, out ulong size)
        {
            // Evaluation stack
            Stack<ulong> eval = new Stack<ulong>();

            // To make sure we don't count the same object twice, we'll keep a set of all objects
            // we've seen before.  Note the ObjectSet here is basically just "HashSet<ulong>".
            // However, HashSet<ulong> is *extremely* memory inefficient.  So we use our own to
            // avoid OOMs.
            ObjectSet considered = new ObjectSet(heap);

            count = 0;
            size = 0;
            eval.Push(obj);

            while (eval.Count > 0)
            {
                // Pop an object, ignore it if we've seen it before.
                obj = eval.Pop();
                if (considered.Contains(obj))
                    continue;

                considered.Add(obj);

                // Grab the type. We will only get null here in the case of heap corruption.
                var type = heap.GetObjectType(obj);
                if (type == null)
                    continue;

                if (type.ToString().Contains("FloatAddition"))
                    Console.WriteLine();

                count++;
                size += type.GetSize(obj);

                // Now enumerate all objects that this object points to, add them to the
                // evaluation stack if we haven't seen them before.
                type.EnumerateRefsOfObject(obj, delegate (ulong child, int offset)
                {
                    if (child != 0 && !considered.Contains(child))
                        eval.Push(child);
                });
            }
        }

        static private string GetValue(ulong obj, ClrInstanceField field)
        {
            if (!field.HasSimpleValue)
                return field.GetAddress(obj).ToString();

            object value = field.GetValue(obj);
            if (value == null)
                return "{error}";

            switch (field.ElementType)
            {
                case ClrElementType.String:
                    return (string)value;

                case ClrElementType.Array:
                case ClrElementType.SZArray:
                case ClrElementType.Object:
                case ClrElementType.Class:
                case ClrElementType.FunctionPointer:
                case ClrElementType.NativeInt:
                case ClrElementType.NativeUInt:
                    return string.Format("{0:X}", value);

                default:
                    return value.ToString();
            }
        }
        static private string GetValue(ulong obj, ClrStaticField field, ClrAppDomain appDomain)
        {
            if (!field.HasSimpleValue)
                return field.GetAddress(appDomain).ToString();

            object value = field.GetValue(appDomain);
            if (value == null)
                return "{error}";

            switch (field.ElementType)
            {
                case ClrElementType.String:
                    return (string)value;

                case ClrElementType.Array:
                case ClrElementType.SZArray:
                case ClrElementType.Object:
                case ClrElementType.Class:
                case ClrElementType.FunctionPointer:
                case ClrElementType.NativeInt:
                case ClrElementType.NativeUInt:
                    return string.Format("{0:X}", value);

                default:
                    return value.ToString();
            }
        }
    }
}

using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CLRMD;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //RedirectOutput();

            //CLRStack stack = new CLRStack("FloatAddition");
            //stack.GetData(true);
            
            //Process process = Process.GetProcessesByName("FloatAddition")[0];
            //CLRMDTEST cLRMD = new CLRMDTEST(process.Id);
            //cLRMD.Dispose();
            CLRHeap heap = new CLRHeap("FloatAddition");
            var objects = heap.Objects;
        }

        private static void RedirectOutput()
        {
            FileStream filestream = new FileStream("Log.txt", FileMode.Truncate);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);
        }
    }
    class CLRMDTEST : IDisposable
    {
        DataTarget dataTarget;
        Dictionary<ClrType, List<HeapHelper.Object>> objects;

        public CLRMDTEST(int pid)
        {
            dataTarget = DataTarget.AttachToProcess(pid: pid, msecTimeout: 100, attachFlag: AttachFlag.Passive);
            objects = new Dictionary<ClrType, List<HeapHelper.Object>>();
            var v = GetData();
        }

        public Dictionary<ClrType, List<HeapHelper.Object>> GetData()
        {
            ClrRuntime runtime = dataTarget.ClrVersions[0].CreateRuntime();

            foreach (var segment in runtime.Heap.Segments)
            {
                Console.Write("\n\nSegment: {0}:\t", segment);
                if (segment.IsEphemeral)
                    Console.WriteLine("Ephemeral");
                else if (segment.IsLarge)
                    Console.WriteLine("Large");
                else
                    Console.WriteLine("Gen2");

                for (ulong obj = segment.FirstObject; obj != 0; obj = segment.NextObject(obj))
                {

                    var type = runtime.Heap.GetObjectType(obj);
                    if (type == null)
                        continue;
                    HeapHelper.Segment segmentType;
                    if (segment.IsEphemeral)
                        segmentType = HeapHelper.Segment.Ephemeral;
                    else if (segment.IsLarge)
                        segmentType = HeapHelper.Segment.Large;
                    else
                        segmentType = HeapHelper.Segment.Segment;
                    int i = segment.GetGeneration(obj);
                    if (i == 1)
                        Console.WriteLine();
                    else if (i == 2)
                        Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine(type + "\t" + obj);
                    if(type.ToString().Contains("FloatAddition"))
                        Console.WriteLine();
                    var v = new HeapHelper.Object(obj, type, segmentType);
                    Console.WriteLine("Name\tSize\tAddress\tElement Type\tType\tValue");
                    foreach (var field in type.Fields)
                    {
                        //if (objects.ContainsKey(type))
                        //    objects[type].Add(new HeapHelper.Object(obj, field, segment.GetGeneration(obj)));
                        //else
                        //    objects.Add(type, new List<HeapHelper.Object>());
                        uint count;
                        ulong size;
                        ObjSize(runtime.Heap, obj, out count, out size);
                        Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", field.Name, field.Size, field.GetAddress(obj), field.ElementType, field.Type, GetValue(obj, field), size, count);
                        //Console.WriteLine("Name:\t{0}",field.Name);
                        //Console.WriteLine("Size:\t{0}", field.Size);
                        //Console.WriteLine("Element Type:\t{0}", field.ElementType);
                        //Console.WriteLine("Type:\t{0}", field.Type);
                        //Console.WriteLine("Value:\t{0}",GetValue(obj,field));
                        //Console.WriteLine("Address:\t{0:X}\n",obj);
                    }
                }
            }
            return objects;
        }

        private bool Heap()
        {
            ClrRuntime runtime = dataTarget.ClrVersions[0].CreateRuntime();

            Console.WriteLine("\n\n\nRuntime Regions\n");
            foreach (var region in (from r in runtime.EnumerateMemoryRegions()
                                        //where r.Type != ClrMemoryRegionType.ReservedGCSegment
                                    group r by r.Type into g
                                    let total = g.Sum(p => (uint)p.Size)
                                    orderby total descending
                                    select new
                                    {
                                        TotalSize = total,
                                        Count = g.Count(),
                                        Type = g.Key
                                    }))
            {
                Console.WriteLine("{0,6:n0} {1,12:n0} {2}", region.Count, region.TotalSize, region.Type.ToString());
            }

            foreach (var regions in runtime.EnumerateMemoryRegions())
            {
            }

            foreach (ulong obj in runtime.EnumerateFinalizerQueueObjectAddresses())
            {
            }

            Console.WriteLine("\n\n\nRuntime Handles\n");
            foreach (var handle in runtime.EnumerateHandles())
            {
                string objectType = runtime.Heap.GetObjectType(handle.Object).Name;
                Console.WriteLine("{0,12:X} {1,12:X} {2,12} {3}", handle.Address, handle.Object, handle.Type.ToString(), objectType);
            }

            Console.WriteLine("\n\n\nRuntime Segments\n");
            Console.WriteLine("{0,12} {1,12} {2,12} {3,12} {4,4} {5}", "Start", "End", "Committed", "Reserved", "Heap", "Type");
            foreach (var segment in runtime.Heap.Segments)
            {
                string type;
                if (segment.IsEphemeral)
                    type = "Ephemeral";
                else if (segment.IsLarge)
                    type = "Large";
                else
                    type = "Gen2";

                Console.WriteLine("{0,12:X} {1,12:X} {2,12:X} {3,12:X} {4,4} {5}\n", segment.Start, segment.End, segment.CommittedEnd, segment.ReservedEnd, segment.ProcessorAffinity, type);
            }
            //HeapSize
            foreach (var item in (from seg in runtime.Heap.Segments
                                  group seg by seg.ProcessorAffinity into g
                                  orderby g.Key
                                  select new
                                  {
                                      Heap = g.Key,
                                      Size = g.Sum(p => (uint)p.Length)
                                  }))
            {
                Console.WriteLine("Heap {0,2}: {1:n0} bytes", item.Heap, item.Size);
            }

            foreach (var seg in runtime.Heap.Segments)
            {
                for (ulong obj = seg.FirstObject; obj != 0; obj = seg.NextObject(obj))
                {
                    var type = runtime.Heap.GetObjectType(obj);

                    // If heap corruption, continue past this object.
                    if (type == null)
                        continue;
                    uint count;
                    ulong size = type.GetSize(obj);
                    ulong size2;
                    ObjSize(runtime.Heap, obj, out count, out size2);
                    Console.WriteLine("{0,12:X} {1,8:n0} {4} {5} {2,1:n0} {3}", obj, size, seg.GetGeneration(obj), type.Name, size2, count);
                }
            }
            return true;
        }

        private void ObjSize(ClrHeap heap, ulong obj, out uint count, out ulong size)
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

        private string GetValue(ulong obj, ClrInstanceField field)
        {

            // If we don't have a simple value, return the address of the field in hex.
            if (!field.HasSimpleValue)
                return field.GetAddress(obj).ToString("X");

            object value = field.GetValue(obj);
            if (value == null)
                return "{error}";  // Memory corruption in the target process.

            // Decide how to format the string based on the underlying type of the field.
            switch (field.ElementType)
            {
                case ClrElementType.String:
                    // In this case, value is the actual string itself.
                    return (string)value;

                case ClrElementType.Array:
                case ClrElementType.SZArray:
                case ClrElementType.Object:
                case ClrElementType.Class:
                case ClrElementType.FunctionPointer:
                case ClrElementType.NativeInt:
                case ClrElementType.NativeUInt:
                    // These types are pointers.  Print as hex.
                    return string.Format("{0:X}", value);

                default:
                    // Everything else will look fine by simply calling ToString.
                    return value.ToString();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Console.WriteLine("Disposing...!");
                    dataTarget.Dispose();
                }
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CLRMD() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

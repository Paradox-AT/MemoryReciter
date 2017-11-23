using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CLRMD
{
    public class CLRHeap
    {
        private DataTarget dataTarget;
        public Dictionary<ClrType, List<HeapHelper.Object>> Objects { get; }

        public CLRHeap(string processName)
        {
            Process process = Process.GetProcessesByName(processName)[0];
            dataTarget = DataTarget.AttachToProcess(pid: process.Id, msecTimeout: 1000, attachFlag: AttachFlag.NonInvasive);
            Objects = new Dictionary<ClrType, List<HeapHelper.Object>>();

            Init();
        }
        private bool Init()
        {
            ClrRuntime runtime = dataTarget.ClrVersions[0].CreateRuntime();
            HeapHelper.Object heapObj;
            foreach (var segment in runtime.Heap.Segments)
            {
                HeapHelper.Segment segmentType;
                if (segment.IsEphemeral)
                    segmentType = HeapHelper.Segment.Ephemeral;
                else if (segment.IsLarge)
                    segmentType = HeapHelper.Segment.Large;
                else
                    segmentType = HeapHelper.Segment.Segment;

                Console.WriteLine("Segment:\t{0}",segmentType);
                Console.WriteLine("");
                for (ulong obj = segment.FirstObject; obj != 0; obj = segment.NextObject(obj))
                {
                    var type = runtime.Heap.GetObjectType(obj);
                    if (type == null )
                        continue;

                    heapObj = new HeapHelper.Object(obj, type, segmentType);

                    if (Objects.ContainsKey(type))
                        Objects[type].Add(heapObj);
                    else
                    {
                        Objects.Add(type, new List<HeapHelper.Object>());
                        Objects[type].Add(heapObj);
                    }
                }
            }
            return true;
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
    }
}

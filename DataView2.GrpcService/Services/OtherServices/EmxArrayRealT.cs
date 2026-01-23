using System;
using System;
using System.Runtime.InteropServices;

namespace DataView2.GrpcService.Services.OtherServices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct emxArray_real_T
    {
        public nint data;
        public nint size;
        public int allocatedSize;
        public int numDimensions;
        [MarshalAs(UnmanagedType.U1)]
        public bool canFreeData;
    }

    public class EmxArrayRealTWrapper : IDisposable
    {
        public emxArray_real_T Value;
        private GCHandle dataHandle;
        private GCHandle sizeHandle;

        public EmxArrayRealTWrapper(double[] data)
        {
            dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            sizeHandle = GCHandle.Alloc(new int[] { data.Length }, GCHandleType.Pinned);

            Value.data = dataHandle.AddrOfPinnedObject();
            Value.size = sizeHandle.AddrOfPinnedObject();
            Value.allocatedSize = data.Length;
            Value.numDimensions = 1;
            Value.canFreeData = false;
        }

        public void Dispose()
        {
            if (dataHandle.IsAllocated) dataHandle.Free();
            if (sizeHandle.IsAllocated) sizeHandle.Free();
        }
    }
}

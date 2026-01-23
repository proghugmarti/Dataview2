using System.Runtime.InteropServices;

namespace DataView2.Packages.Lcms;

public class ByteValuePtr : ValuePtr { public ByteValuePtr(byte[] byteArray) : base(byteArray) { } }
public class DoubleValuePtr : ValuePtr { public DoubleValuePtr(double value) : base(BitConverter.GetBytes(value)) { } }
public class FloatValuePtr : ValuePtr { public FloatValuePtr(float value) : base(BitConverter.GetBytes(value)) { } }
public class IntValuePtr : ValuePtr { public IntValuePtr(int value) : base(BitConverter.GetBytes(value)) { } }
public class LongValuePtr : ValuePtr { public LongValuePtr(long value) : base(BitConverter.GetBytes(value)) { } }
public class UIntValuePtr : ValuePtr { public UIntValuePtr(uint value) : base(BitConverter.GetBytes(value)) { } }
public class ULongValuePtr : ValuePtr { public ULongValuePtr(ulong value) : base(BitConverter.GetBytes(value)) { } }

public abstract class ValuePtr : IDisposable
{
    protected ValuePtr(byte[] arrBytes)
    {
        Length = arrBytes.Length;
        Ref = Marshal.AllocHGlobal(arrBytes.Length);

        Marshal.Copy(arrBytes, 0, Ref, arrBytes.Length);
    }

    public int Length { get; }
    public IntPtr Ref { get; }

    public void Dispose()
    {
        Marshal.FreeHGlobal(Ref);
        GC.SuppressFinalize(this);
    }
}
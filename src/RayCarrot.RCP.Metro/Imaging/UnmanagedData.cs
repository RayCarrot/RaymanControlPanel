using System.Runtime.InteropServices;

namespace RayCarrot.RCP.Metro.Imaging;

public class UnmanagedData<T> : IDisposable
{
    public UnmanagedData(byte[] rawData, Func<IntPtr, T> createResource)
    {
        Pointer = Marshal.AllocHGlobal(rawData.Length);

        try
        {
            Marshal.Copy(rawData, 0, Pointer, rawData.Length);
        }
        catch (Exception ex)
        {
            Marshal.FreeHGlobal(Pointer);
            throw;
        }

        Resource = createResource(Pointer);
    }

    public T Resource { get; }
    public IntPtr Pointer { get; }

    public void Dispose()
    {
        Marshal.FreeHGlobal(Pointer);
    }
}
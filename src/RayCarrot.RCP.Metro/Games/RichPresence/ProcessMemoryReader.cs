using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RayCarrot.RCP.Metro.Games.RichPresence;

// TODO: Should maybe use something like this for the runtime modifications. This class is much more optimized and has zero allocations.
public class ProcessMemoryReader : IDisposable
{
    #region Constructor

    public ProcessMemoryReader(Process process)
    {
        // Set properties
        _process = process;
        BaseAddress = process.MainModule?.BaseAddress.ToInt64() ??
                      throw new Exception("The process has no main module");
        Is64Bit = process.Is64Bit();

        // Open the process and get the handle
        _processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);

        if (_processHandle == IntPtr.Zero)
            throw new Win32Exception("Failed to open process");
    }

    #endregion

    #region Private Fields

    private readonly IntPtr _processHandle;
    private readonly Process _process;

    private IntPtr _bufferPtr;
    private int _bufferSize;

    #endregion

    #region Public Properties

    public long BaseAddress { get; }
    public bool Is64Bit { get; }

    #endregion

    #region P/Invoke

    private const int PROCESS_WM_READ = 0x0010;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, IntPtr lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    #endregion

    #region Private Methods

    private IntPtr GetBufferPtr(int requiredSize)
    {
        // Re-allocate if needed
        if (requiredSize > _bufferSize || _bufferSize == 0)
        {
            if (_bufferSize != 0)
                Marshal.FreeHGlobal(_bufferPtr);

            _bufferSize = Math.Max(4, requiredSize);
            _bufferPtr = Marshal.AllocHGlobal(_bufferSize);
        }

        return _bufferPtr;
    }

    #endregion

    #region Public Methods

    public IntPtr ReadToBuffer(long addr, int count)
    {
        IntPtr bufferPtr = GetBufferPtr(count);

        bool success = ReadProcessMemory(_processHandle, addr, bufferPtr, count, out int numBytesRead);

        if (!success)
            throw new Win32Exception();

        if (numBytesRead != count)
            throw new Exception("Unable to read data");

        return bufferPtr;
    }

    public unsafe T Read<T>(long addr)
        where T : unmanaged
    {
        IntPtr bufferPtr = ReadToBuffer(addr, sizeof(T));
        return *(T*)bufferPtr;
    }

    public T? ReadNullable<T>(long addr)
        where T : unmanaged
    {
        if (addr == 0)
            return null;

        return Read<T>(addr);
    }

    public unsafe string ReadString(long addr, Encoding encoding, int size)
    {
        IntPtr bufferPtr = ReadToBuffer(addr, size);
        return encoding.GetString((byte*)bufferPtr, size);
    }

    public long ReadPointer(long addr)
    {
        if (Is64Bit)
            return Read<long>(addr);
        else
            return Read<uint>(addr);
    }

    public void Dispose()
    {
        if (_bufferPtr != IntPtr.Zero)
            Marshal.FreeHGlobal(_bufferPtr);

        if (_processHandle != IntPtr.Zero)
            CloseHandle(_processHandle);

        _process.Dispose();
    }

    #endregion
}
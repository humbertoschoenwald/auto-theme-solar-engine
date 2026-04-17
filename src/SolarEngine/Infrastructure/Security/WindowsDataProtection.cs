using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SolarEngine.Infrastructure.Security;

internal static partial class WindowsDataProtection
{
    private const int CryptProtectUiForbidden = 0x1;

    public static byte[] Protect(byte[] plainBytes)
    {
        ArgumentNullException.ThrowIfNull(plainBytes);
        return Transform(plainBytes, protect: true);
    }

    public static byte[] Unprotect(byte[] protectedBytes)
    {
        ArgumentNullException.ThrowIfNull(protectedBytes);
        return Transform(protectedBytes, protect: false);
    }

    private static byte[] Transform(byte[] inputBytes, bool protect)
    {
        DataBlob input = default;
        DataBlob output = default;

        try
        {
            input = DataBlob.FromBytes(inputBytes);

            bool success = protect
                ? CryptProtectData(
                    ref input,
                    description: null,
                    optionalEntropy: nint.Zero,
                    reserved: nint.Zero,
                    promptStruct: nint.Zero,
                    CryptProtectUiForbidden,
                    out output)
                : CryptUnprotectData(
                    ref input,
                    description: nint.Zero,
                    optionalEntropy: nint.Zero,
                    reserved: nint.Zero,
                    promptStruct: nint.Zero,
                    CryptProtectUiForbidden,
                    out output);

            return !success ? throw new Win32Exception(Marshal.GetLastWin32Error()) : output.ToBytes();
        }
        finally
        {
            input.Free();
            output.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public nint pbData;

        public static DataBlob FromBytes(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return default;
            }

            nint buffer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, startIndex: 0, buffer, bytes.Length);

            return new DataBlob
            {
                cbData = bytes.Length,
                pbData = buffer
            };
        }

        public readonly byte[] ToBytes()
        {
            if (cbData <= 0 || pbData == nint.Zero)
            {
                return [];
            }

            byte[] bytes = new byte[cbData];
            Marshal.Copy(pbData, bytes, startIndex: 0, length: cbData);
            return bytes;
        }

        public void Free()
        {
            if (pbData != nint.Zero)
            {
                _ = LocalFree(pbData);
                pbData = nint.Zero;
                cbData = 0;
            }
        }
    }

    [LibraryImport("crypt32.dll", EntryPoint = "CryptProtectData", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptProtectData(
        ref DataBlob dataIn,
        string? description,
        nint optionalEntropy,
        nint reserved,
        nint promptStruct,
        int flags,
        out DataBlob dataOut);

    [LibraryImport("crypt32.dll", EntryPoint = "CryptUnprotectData", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptUnprotectData(
        ref DataBlob dataIn,
        nint description,
        nint optionalEntropy,
        nint reserved,
        nint promptStruct,
        int flags,
        out DataBlob dataOut);

    [LibraryImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
    private static partial nint LocalFree(nint handle);
}

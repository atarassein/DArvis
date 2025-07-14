using System;

using DArvis.Win32;

namespace DArvis.IO.Process
{
    [Flags]
    public enum ProcessAccess
    {
        Read = 0x1,
        Write = 0x2,
        ReadWrite = Read | Write
    }

    public static class ProcessAccessExtender
    {
        internal static ProcessAccessFlags ToWin32Flags(this ProcessAccess access)
        {
            var flags = ProcessAccessFlags.VmOperation | ProcessAccessFlags.QueryInformation;

            if (access.HasFlag(ProcessAccess.Read))
                flags |= ProcessAccessFlags.VmRead;

            if (access.HasFlag(ProcessAccess.Write))
                flags |= ProcessAccessFlags.VmWrite;

            return flags;
        }
    }
}

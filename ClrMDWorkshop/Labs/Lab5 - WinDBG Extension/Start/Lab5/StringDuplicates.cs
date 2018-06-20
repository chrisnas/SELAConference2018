using System;
using System.Collections.Generic;

namespace WindbgExtension
{
    public partial class DebuggerExtensions
    {
        // define the commands you want to export with the following signature and decorated by the DllExport attribute
        //[DllExport("<Command Name>")]
        //public static void CommandName(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        //{
        //    OnStringDuplicates(client, args);
        //}
    }
}

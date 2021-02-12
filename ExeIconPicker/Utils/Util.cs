using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ExeIconPicker.Utils
{
    public static class Util
    {
        public static void Log(params string[] args)
        {
#if DEBUG
            Debug.Print("[ExeIconPicker] " + string.Join("", args));
#endif
        }

        public static byte[] HashData(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}

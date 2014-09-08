using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;

namespace SysMana
{
    public enum VertAlign { Top, Center, Bottom };

    static class Misc
    {
        #region Get Files in Natural Order
        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }

        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                return SafeNativeMethods.StrCmpLogicalW(a, b);
            }
        }

        public static string[] GetFilesInNaturalOrder(string dir)
        {
            List<string> files = Directory.GetFiles(dir).ToList();
            files.Sort(new NaturalStringComparer());
            return files.ToArray();
        } 
        #endregion

        #region Play Sound
        [DllImport("WinMM.dll")]
        private static extern bool PlaySound(string fname, int Mod, int flag);

        private static int SND_ASYNC = 0x0001;

        public static void PlaySound(string file)
        {
            if (File.Exists(file))
                PlaySound(file, 0, SND_ASYNC);
        }
        #endregion

        public static FontStyle GenFontStyle(bool bold, bool italic, bool underline, bool strikeout)
        {
            return (bold ? FontStyle.Bold : FontStyle.Regular) | (italic ? FontStyle.Italic : FontStyle.Regular) | (underline ? FontStyle.Underline : FontStyle.Regular) | (strikeout ? FontStyle.Strikeout : FontStyle.Regular);
        }
    }
}

namespace Wallpaperr2
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    public class FileSystemInfoComparer : IEqualityComparer<FileSystemInfo>, IEqualityComparer
    {
        public static readonly FileSystemInfoComparer Default = new FileSystemInfoComparer();

        public bool Equals(FileSystemInfo x, FileSystemInfo y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x?.FullName, y?.FullName);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return this.Equals((FileSystemInfo)x, (FileSystemInfo)y);
        }

        public int GetHashCode(FileSystemInfo obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FullName);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return this.GetHashCode((FileSystemInfo)obj);
        }
    }
}

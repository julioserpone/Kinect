namespace Trumix.Library
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;

    public class Utilities
    {
        public static Uri LoadUriFromResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute);
        }

        public static Uri LoadUriImageUrl(string strBaseURL, string strDirectory, string strFile)
        {
            return new Uri(Path.Combine(strBaseURL, ((strDirectory != null) ? strDirectory + "\\": ""), strFile), UriKind.Absolute);
        }

    }

    public static class ForEachExtensions
    {
        public static void ForEachWithIndex<T>(this IEnumerable<T> enumerable, Action<T, int> handler)
        {
            int idx = 0;
            foreach (T item in enumerable)
                handler(item, idx++);
        }
    }
}
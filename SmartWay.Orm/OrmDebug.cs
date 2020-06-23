using System.Diagnostics;

namespace SmartWay.Orm
{
    public static class OrmDebug
    {
        public static void Trace(string text)
        {
            // Debug.WriteLine(text);
        }

        public static void Info(string text)
        {
#if DEBUG
            Debug.WriteLine(text);
#endif
        }
    }
}
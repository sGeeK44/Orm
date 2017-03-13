using System;
using System.Diagnostics;

namespace Orm.Core
{
    internal class Diagnostics
    {
        public static void Measure(string message, Action workToMeasure)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            workToMeasure();
#if DEBUG
            sw.Stop();
            Debug.WriteLine(string.Format("{0}. Elapsed time:{1}", message, sw.Elapsed));
#endif
        }
    }
}

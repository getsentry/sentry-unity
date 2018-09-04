using System;
using System.Collections.Generic;

namespace Sentry {
    [Serializable]
    public class Breadcrumb {
        public string timestamp;
        public string message;

        public Breadcrumb(string timestamp, string message)
        {
            this.timestamp = timestamp;
            this.message = message;
        }

        /* combine breadcrumbs from array[], start & count into List<Breadcrumb> */
        public static List<Breadcrumb> CombineBreadcrumbs(
            Breadcrumb[] breadcrumbs, int index, int number)
        {
            var res = new List<Breadcrumb>(number);
            var start = (index + SentrySdk.MAX_BREADCRUMBS - number) % SentrySdk.MAX_BREADCRUMBS;
            for (var i = 0; i < number; i++)
                res.Add(breadcrumbs[(i + start) % SentrySdk.MAX_BREADCRUMBS]);
            return res;
        }
    }
}

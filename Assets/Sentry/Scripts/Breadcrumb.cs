using System;
using System.Collections.Generic;

namespace Sentry
{
    [Serializable]
    public class Breadcrumb
    {
        public const int MaxBreadcrumbs = 128;

        private readonly string _timestamp;
        private readonly string _message;

        public Breadcrumb(string timestamp, string message)
        {
            _timestamp = timestamp;
            _message = message;
        }

        /* combine breadcrumbs from array[], start & count into List<Breadcrumb> */
        public static List<Breadcrumb> Combine(
            Breadcrumb[] breadcrumbs,
            int index,
            int number)
        {
            var res = new List<Breadcrumb>(number);
            var start = (index + MaxBreadcrumbs - number) % MaxBreadcrumbs;
            for (var i = 0; i < number; i++)
            {
                res.Add(breadcrumbs[(i + start) % MaxBreadcrumbs]);
            }

            return res;
        }

        // This eliminates C# warnings about the private fields never being referenced. 
        public override string ToString() => $"Breadcrumb [{_timestamp}]: {_message}";
    }
}

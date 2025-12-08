using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiSistemaAsistencia.Infrastructure.Helpers
{
    public static class TimeZoneHelper
    {
        private const int HoursOffset = -4;

        public static DateTime GetRDNow()
        {
            return DateTime.UtcNow.AddHours(HoursOffset);
        }

        public static DateTime ToRDTime(this DateTime utcDateTime)
        {
            return utcDateTime.AddHours(HoursOffset);
        }

        public static DateTime? ToRDTime(this DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue) return null;
            return utcDateTime.Value.AddHours(HoursOffset);
        }
    }
}

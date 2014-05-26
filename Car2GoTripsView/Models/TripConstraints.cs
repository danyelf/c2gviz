using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Car2GoTripsView.Models
{
    public class TripConstraints
    {
        public DateTime beginTime;
        public DateTime endTime;
        public int city;
        public float minLat, maxLat, minLon, maxLon;
    }

}
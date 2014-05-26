using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace Car2GoTripsView
{
    public class ColorBrewer
    {
        static ColorBrewer() {
            var blueString = new String[] { "#ffffd9", "#edf8b1", "#c7e9b4", "#7fcdbb", "#41b6c4", "#1d91c0", "#225ea8", "#253494", "#081d58" };
            var redString = new String[] { "#ffffcc", "#ffeda0", "#fed976", "#feb24c", "#fd8d3c", "#fc4e2a", "#e31a1c", "#bd0026", "#800026" };

            BLUES = blueString.Select(s => ColorTranslator.FromHtml(s)).ToArray();
            REDS = redString.Select(s => ColorTranslator.FromHtml(s)).ToArray();


        }

        public static readonly Color[] BLUES;
        public static readonly Color[] REDS;

    }
}
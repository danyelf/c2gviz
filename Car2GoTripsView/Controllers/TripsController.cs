using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace Car2GoTripsView.Controllers
{
	public class TripsController : Controller
	{
		Car2GoDataContext db = new Car2GoDataContext();

		//
		// GET: /Trips/

        // A few notes
        //
        // right now, as computed: 		MaxLat	-611297	decimal
		    // MaxLon	238699	decimal
		    // MinLat	-612078	decimal
		     // MinLon	237660	decimal
        // Note that these numbers are Lat/lon * 5000 
        // 
		// I want two more :
	   // 114,320 - 215,385 = ballard =>
        //  -122.3928 ... -122.3726 by 47.8038 ... 47.8168
	   // 439, 567 - 533,638


		public ActionResult Index()
		{
//            ViewBag.CityTable = db.CityLookups.ToDictionary( cl => cl.CityIndex.Value, cl => cl.City );
			ViewBag.CityTable = db.CityLookups.Where(c => c.City == "Seattle").ToDictionary(cl => cl.CityIndex.Value, cl => cl.City);
			return View();
		}

		public ActionResult StartTest(int city = 0, float minLat = -180, float maxLat = 180, float minLon = -180, float maxLon = 180, bool isBegin = false)
		{
			return TripEnds(new DateTime(2000, 12, 31), DateTime.Now, 
				city, minLat, maxLat, minLon, maxLon, 
				isBegin ? ColorBrewer.BLUES : ColorBrewer.REDS, 
				isBegin);
		}


		public string CityName( int city = 0)
		{
			return db.CityLookups.Where(s => s.CityIndex == city).First().City;
		}
		

		public ActionResult TripEnds(DateTime beginTime, DateTime endTime, int city, float minLat, float maxLat, float minLon, float maxLon, Color[] brewer, bool begin )
		{

            Debug.Assert(minLat < maxLat);
            Debug.Assert(minLon < maxLon);

			Car2GoTripsView.Models.TripConstraints tc = new Models.TripConstraints()
			{
				beginTime = beginTime,
				endTime = endTime,
				city = city,
				minLat = minLat,
				maxLat = maxLat,
				minLon = minLon,
				maxLon = maxLon
			};

			// we know that we have these, the 95%+5% range for each city 
			var LLRange = db.PercentileCoordDates.Where(s => s.cityIndex == city).Select(t => t).First();
			List<FindTripEndsResult> fter;

			var ValidAreaTester = InsideValidArea(LLRange, 5000); 

			if (begin)
			{
                var fter1 = db.FindTripStarts(beginTime, endTime, city, minLat, maxLat, minLon, maxLon).ToList();
					
                Debug.WriteLine("STARTS COUNT = " + fter1.Count() );

				fter = fter1.Select( s => ToEnd(s)).Where(e => ValidAreaTester(e))
					.ToList();
                Debug.WriteLine("SANITY FILTER COUNT = " + fter.Count());
            }
            else
			{
				var fter1 = db.FindTripEnds(beginTime, endTime, city, minLat, maxLat, minLon, maxLon).ToList();
                Debug.WriteLine("STARTS COUNT = " + fter1.Count());

                fter= fter1
					// should probably curry this
					.Where(e => ValidAreaTester(e))
					.ToList();
                Debug.WriteLine("SANITY FILTER COUNT = " + fter.Count());

			}

//            var range = db.CityRanges.Where(d => d.CityIndex == city).First();
			// our data runs across this range, give or take, and has mins and maxes
			LatLonToPixel lltp = new LatLonToPixel(fter);


			int maxValue = fter.Max(s => s.COUNT.Value);
						
			Bitmap bmp = lltp.MakeBitmap();
			Graphics g = Graphics.FromImage(bmp);
			int[,] colors = new int[bmp.Size.Width, bmp.Size.Height];
			for (int i = 0; i < colors.GetLength(0); i++)
				for (int j = 0; j < colors.GetLength(1); j++ )
					colors[i, j] = 0;

			foreach (var v in fter)
					{
						var res = lltp.Convert(v.Lat, v.Lon);
						if (res.HasValue)
						{
							colors[res.Value.X, res.Value.Y] = v.COUNT.Value;
						}
					}
            BitmapPainter.PaintPoints(bmp, colors, ColorFromValue(maxValue, brewer));
            BitmapPainter.PaintBounds(bmp, lltp.Convert(rescale(minLat), rescale(maxLon)), lltp.Convert(rescale(maxLat), rescale(minLon)));
			return new ImageResult { Image = bmp, ImageFormat = ImageFormat.Png }; ;
		}

        // this gets us back to the 'integer-typed' data, rounded to the nearest counted pixel
        private int rescale( double latlon )
        {
            return (int)(latlon * 5000);
        }

		private FindTripEndsResult ToEnd(FindTripStartsResult s)
		{
			return new FindTripEndsResult
			{
				AverageDuration = s.AverageDuration,
				COUNT = s.COUNT,
				Lat = s.Lat,
				Lon = s.Lon,
				MaxDuration = s.MaxDuration,
				MinDuration = s.MinDuration
			};
		}

		private Func<FindTripEndsResult, bool> InsideValidArea(PercentileCoordDate LLRange, int multiplier)
		{
			var FivePctLatLow = LLRange.P10_Lat - LLRange.P05_Lat;
			var FivePctLatHigh = LLRange.P95_Lat - LLRange.P90_Lat;

			var FivePctLonLow = LLRange.P10_Lon - LLRange.P05_Lon;
			var FivePctLonHigh = LLRange.P95_Lon - LLRange.P90_Lon;

            var k = 2;

            var minLat = (LLRange.P05_Lat - k * FivePctLatLow) * multiplier;
            var minLon = (LLRange.P05_Lon - k * FivePctLonLow) * multiplier;
            var maxLon = (LLRange.P95_Lon + k * FivePctLonHigh) * multiplier;
            var maxLat = (LLRange.P95_Lat + k * FivePctLatHigh) * multiplier;

			return (e =>
				(e.Lat >= minLat) && (e.Lat <= maxLat )
                && (e.Lon >= minLon) && (e.Lon <= maxLon)
			   );

		}

		
		// TODO: Let's take a look at the DISTRIBUTION OF VALUES here. I think we're getting mostly near-zeros, and that's not very interesting.
		Car2GoTripsView.Controllers.BitmapPainter.ColorFunc ColorFromValue( int max, Color[] brewer )
		{
			return (value =>
			{
				if (value == 0)
					return Color.Black;

				double frac = Math.Log( value + 1) / (Math.Log(max+2));

				var len = brewer.Length;
				int fracVal = (int)(frac * len);
				Color c = brewer[len - fracVal - 1]; // Color.FromArgb(0, fracVal, fracVal, fracVal);
				return c;
			});
		}

	}




   class LatLonToPixel
	{
		struct CR
		{
			internal decimal MinLat, MaxLat, MinLon, MaxLon;
		}

		int xSize, ySize;
		CR cr;

		public LatLonToPixel(List<FindTripEndsResult> ends)
		{
			this.cr = new CR()
			{
				MinLat = ends.Min(s => s.Lat.Value),
				MaxLat = ends.Max(s => s.Lat.Value),
				MinLon = ends.Min(s => s.Lon.Value),
				MaxLon = ends.Max(s => s.Lon.Value)
			};
		}

		public Bitmap MakeBitmap()
		{
			this.ySize = (int) Math.Round( (cr.MaxLon - cr.MinLon) ) ;
			this.xSize = (int) Math.Round((cr.MaxLat - cr.MinLat) ) ;

			return new Bitmap(xSize + 1 , ySize +1 );
		}

		public Point? Convert(decimal? lat_n, decimal? lon_n)
		{
			if (lat_n == null || lon_n == null) return null;

			decimal lat = lat_n.Value;
			decimal lon = lon_n.Value;

			if( lat >= cr.MinLat && lat <= cr.MaxLat &&
				lon >= cr.MinLon && lon <= cr.MaxLon )
			{
				var y = ySize - (int)((lon - cr.MinLon)); // / (cr.MaxLon - cr.MinLon ));
				var x =  (int)((lat - cr.MinLat)) ; // / (cr.MaxLat - cr.MinLat));
				return new Point( x, y );
			}
			return null;

		}

   }



	public static class BitmapPainter
	{
		public delegate Color ColorFunc(int val);

        public static void PaintBounds( Bitmap bmp, Point? topleft, Point? botright )
        {
            if (topleft == null || botright == null || !topleft.HasValue || !botright.HasValue)
                return;

            Graphics g = Graphics.FromImage(bmp);
            Pen p = new Pen( new SolidBrush( Color.White ));
            var h = botright.Value.Y - topleft.Value.Y;
            var w = botright.Value.X - topleft.Value.X;
            g.DrawRectangle(p,  topleft.Value.X, topleft.Value.Y, w, h);
        }

		public static void PaintPoints( Bitmap bmp, int[,] colors, ColorFunc colorFromValue )
		{
			BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			int stride = data.Stride;
			unsafe
			{
				byte* ptr = (byte*)data.Scan0;
				// Check this is not a null area
					// Go through the draw area and set the pixels as they should be
					for (int y = 0; y < colors.GetLength(1); y++)
					{
						for (int x = 0; x < colors.GetLength(0); x++)
						{
							var col = colorFromValue(colors[x, y]);
							ptr[(x * 3) + y * stride] = col.B;
							ptr[(x * 3) + y * stride + 1] = col.G;
							ptr[(x * 3) + y * stride + 2] = col.R;
						}
				}
			}
			bmp.UnlockBits(data);
		}
	}
}
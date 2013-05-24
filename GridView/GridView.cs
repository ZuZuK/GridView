using Android.Content;
using Android.Util;
using Android.Views;

namespace Touchin.Views.GridView
{
	public class GridView : View
	{
		public GridView(Context context) :
			this (context, null)
		{
		}

		public GridView(Context context, IAttributeSet attrs) :
			this (context, attrs, 0)
		{
		}

		public GridView(Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{

		}
	}
}


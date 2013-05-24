using System;
using Android.Util;

namespace Touchin.Views
{
	public struct GridLength
	{
		private double _value;

		public GridUnitType GridUnitType { get; private set; }

		public bool IsStar
		{ 
			get { return GridUnitType == GridUnitType.Star; }
		}

		public bool IsPixel
		{ 
			get { return GridUnitType == GridUnitType.Pixel; }
		}

		public bool IsAuto
		{ 
			get { return GridUnitType == GridUnitType.Auto; }
		}

		public double Value
		{
			get	{ return _value; }
			private set
			{
				if(value < 0)
				{
					throw new ArgumentOutOfRangeException("Value");
				}
				_value = value;
			}
		}

		public GridLength(double value)	: this(value, GridUnitType.Pixel)
		{
		}

		public GridLength(double value, GridUnitType type) : this()
		{
			GridUnitType = type;
			Value = value;
		}

		public static GridLength Parse(string source, DisplayMetrics metrics)
		{
			if(source.ToLower() == "auto")
			{
				return new GridLength(1, GridUnitType.Auto);
			}
			if(source == "*")
			{
				return new GridLength(1, GridUnitType.Star);
			}
			if(source.EndsWith("*"))
			{
				return new GridLength(double.Parse(source.Substring(0, source.Length-1)), GridUnitType.Star);
			}

			return new GridLength(DimensionConverter.StringToDimensionPixelSize(source, metrics));
		}

		public override string ToString()
		{
			switch(GridUnitType)
			{
				case GridUnitType.Auto:
					return "auto";
				case GridUnitType.Pixel:
					return Value.ToString();
				case GridUnitType.Star:
					return Value + "*";
				default:
					throw new Exception("Unknown GridUnitType");
			}
		}
	}
}


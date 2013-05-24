using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.Util;

namespace Touchin.Views
{
	public class DimensionConverter
	{
		private static readonly Dictionary<string, int> _dimensionConstantLookup = InitDimensionConstantLookup();
		private static readonly Regex _dimensionPattern = new Regex("^\\s*(\\d+(\\.\\d+)*)\\s*([a-zA-Z]+)\\s*$");

		private static Dictionary<string, int> InitDimensionConstantLookup()
		{
			Dictionary<string, int> m = new Dictionary<string, int>();

			m.Add("px", (int)ComplexUnitType.Px);
			m.Add("dip", (int)ComplexUnitType.Dip);
			m.Add("dp", (int)ComplexUnitType.Dip);
			m.Add("sp", (int)ComplexUnitType.Sp);
			m.Add("pt", (int)ComplexUnitType.Pt);
			m.Add("in", (int)ComplexUnitType.In);
			m.Add("mm", (int)ComplexUnitType.Mm);

			return m;
		}

		public static int StringToDimensionPixelSize(string dimension, DisplayMetrics metrics)
		{
			InternalDimension internalDimension = StringToInternalDimension(dimension);

			float value = internalDimension.Value;
			float f = TypedValue.ApplyDimension((ComplexUnitType)(int)internalDimension.Unit, value, metrics);
			int res = (int)(f + 0.5f);

			if(res != 0)
				return res;
			if(value == 0)
				return 0;
			if(value > 0)
				return 1;

			return -1;
		}

		public static float StringToDimension(System.String dimension, DisplayMetrics metrics)
		{
			InternalDimension internalDimension = StringToInternalDimension(dimension);
			return TypedValue.ApplyDimension((ComplexUnitType)(int)internalDimension.Unit, internalDimension.Value, metrics);
		}

		private static InternalDimension StringToInternalDimension(string dimension)
		{
			MatchCollection matches = _dimensionPattern.Matches(dimension);

			if(matches.Count > 0)
			{
				Match matcher = matches[0];
				float value = float.Parse(matcher.Groups[1].Value);
				string unit = matcher.Groups[3].ToString().ToLower();

				int dimensionUnit;

				if(!_dimensionConstantLookup.TryGetValue(unit, out dimensionUnit))
				{
					throw new FormatException();
				}
				return new InternalDimension(value, dimensionUnit);
			}        
			throw new FormatException();
		}

		private class InternalDimension
		{
			public float Value { get; private set; }

			public int Unit { get; private set; }

			public InternalDimension(float value, int unit)
			{
				Value = value;
				Unit = unit;
			}
		}
	}
}


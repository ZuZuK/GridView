namespace Touchin.Views
{
	public class GridDefinition
	{
		public GridLength Length { get; set; }

		public GridDefinition() : this(new GridLength(1, GridUnitType.Auto))
		{
		}

		public GridDefinition(GridLength length)
		{
			Length = length;
		}
	}
}


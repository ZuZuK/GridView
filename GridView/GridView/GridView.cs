using Android.Content;
using Android.Util;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Xml;
using Android.Content.Res;
using GridView;
using Android.Runtime;

namespace Touchin.Views
{
	public class GridView : ViewGroup
	{
		public class GridViewLayoutParams : LayoutParams
		{
			private int _row;
			private int _rowSpan;
			private int _column;
			private int _columnSpan;

			public int Row
			{
				get { return _row; }
				set
				{
					if(value < 0)
					{
						throw new ArgumentOutOfRangeException("row");
					}
					_row = value;
				}
			}

			public int RowSpan
			{
				get { return _rowSpan; }
				set
				{
					if(value < 1)
					{
						throw new ArgumentOutOfRangeException("rowSpan");
					}
					_rowSpan = value;
				}
			}

			public int Column
			{
				get { return _column; }
				set
				{
					if(value < 0)
					{
						throw new ArgumentOutOfRangeException("column");
					}
					_column = value;
				}
			}

			public int ColumnSpan
			{
				get { return _columnSpan; }
				set
				{
					if(value < 1)
					{
						throw new ArgumentOutOfRangeException("columnSpan");
					}
					_columnSpan = value;
				}
			}

			public GridViewLayoutParams(int width, int height, int row, int rowSpan, int column, int columnSpan)
				: base(width, height)
			{
				Row = row;
				RowSpan = rowSpan;
				Column = column;
				ColumnSpan = columnSpan;
			}
		}

		public class ViewMeasureInfo
		{
			public View View { get; private set; }

			public List<GridDefinition> AssociatedColumns { get; private set; }

			public List<GridDefinition> AssociatedRows { get; private set; }

			public bool IsMeasured { get; set; }

			public bool IsInStarColumn
			{
				get
				{
					foreach(var column in AssociatedColumns)
					{
						if(column.Length.IsStar)
						{
							return true;
						}
					}
					return false;
				}
			}

			public bool IsInStarRow
			{
				get
				{
					foreach(var row in AssociatedRows)
					{
						if(row.Length.IsStar)
						{
							return true;
						}
					}
					return false;
				}
			}

			public bool IsOnlyInPixelColumns
			{
				get
				{
					foreach(var column in AssociatedColumns)
					{
						if(!column.Length.IsPixel)
						{
							return false;
						}
					}
					return true;
				}
			}

			public bool IsOnlyInPixelRows
			{
				get
				{
					foreach(var row in AssociatedRows)
					{
						if(!row.Length.IsPixel)
						{
							return false;
						}
					}
					return true;
				}
			}

			public bool IsInAutoColumn
			{
				get
				{
					foreach(var column in AssociatedColumns)
					{
						if(column.Length.IsAuto)
						{
							return true;
						}
					}
					return false;
				}
			}

			public bool IsInAutoRow
			{
				get
				{
					foreach(var row in AssociatedRows)
					{
						if(row.Length.IsAuto)
						{
							return true;
						}
					}
					return false;
				}
			}

			public double PixelTypeColumnsWidth
			{
				get
				{
					double result = 0;
					foreach(var column in AssociatedColumns)
					{
						if(column.Length.IsPixel)
						{
							result += column.Length.Value;
						}
					}
					return result;
				}
			}

			public double PixelTypeRowsHeight
			{
				get
				{
					double result = 0;
					foreach(var row in AssociatedRows)
					{
						if(row.Length.IsPixel)
						{
							result += row.Length.Value;
						}
					}
					return result;
				}
			}

			public ViewMeasureInfo(View view)
			{
				View = view;
				AssociatedColumns = new List<GridDefinition>();
				AssociatedRows = new List<GridDefinition>();
			}
		}

		public class GridDefinitionMeasureInfo
		{
			public GridDefinition Definition { get; private set; }

			public List<ViewMeasureInfo> AttachedViews { get; private set; }

			public double Length { get; private set; }

			public bool IsMeasured { get; private set; }

			public GridDefinitionMeasureInfo(GridDefinition definition)
			{
				Definition = definition;
				AttachedViews = new List<ViewMeasureInfo>();
			}

			public void SetLength(double length)
			{
				IsMeasured = true;
				Length = length;
			}

			public void Unmeasure()
			{
				IsMeasured = false;
				Length = 0;
			}
		}

		private const int DefaultGridMarkupValue = -1;
		private double _heightStarSum = 0;
		private double _widthStarSum = 0;
		private List<GridDefinition> _rowDefinitions = new List<GridDefinition>();
		private List<GridDefinition> _columnDefinitions = new List<GridDefinition>();
		private Dictionary<View, ViewMeasureInfo> _viewsMeasureInfos = new Dictionary<View, ViewMeasureInfo>();
		private Dictionary<GridDefinition, GridDefinitionMeasureInfo> _rowsInfo = new Dictionary<GridDefinition, GridDefinitionMeasureInfo>();
		private Dictionary<GridDefinition, GridDefinitionMeasureInfo> _columnsInfo = new Dictionary<GridDefinition, GridDefinitionMeasureInfo>();

		public GridView(IntPtr a, JniHandleOwnership b) : base (a, b)
		{
		}

		public GridView(Context context, IAttributeSet attrs) :
			this (context, attrs, 0)
		{
		}

		public GridView(Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			var a = context.ObtainStyledAttributes(attrs, Resource.Styleable.GridView, defStyle, 0);

			int markupId = a.GetResourceId(Resource.Styleable.GridView_grid_markup, DefaultGridMarkupValue);
			if(markupId != DefaultGridMarkupValue)
			{
				ParseMarkupXml(markupId);
			}
			else
			{
				_rowDefinitions.Add(new GridDefinition());
				_columnDefinitions.Add(new GridDefinition());
			}
			foreach(var row in _rowDefinitions)
			{
				_rowsInfo.Add(row, new GridDefinitionMeasureInfo(row));
				if(row.Length.IsStar)
				{
					_heightStarSum += row.Length.Value;
				}
			}
			foreach(var column in _columnDefinitions)
			{
				_columnsInfo.Add(column, new GridDefinitionMeasureInfo(column));
				if(column.Length.IsStar)
				{
					_widthStarSum += column.Length.Value;
				}
			}
		}

		public override ViewGroup.LayoutParams GenerateLayoutParams(IAttributeSet attrs)
		{
			var a = Context.ObtainStyledAttributes(attrs, Resource.Styleable.GridView, 0, 0);

			var baseAttrs = base.GenerateLayoutParams(attrs);

			return new GridViewLayoutParams(baseAttrs.Width, baseAttrs.Height,
			                                a.GetInt(Resource.Styleable.GridView_row, 0),
			                                a.GetInt(Resource.Styleable.GridView_row_span, 1),
			                                a.GetInt(Resource.Styleable.GridView_column, 0),
			                                a.GetInt(Resource.Styleable.GridView_column_span, 1));
		}

		private void ReadMarkupGroup(XmlReader xrp, List<GridDefinition> definitionsList, string groupMemberName, string dimensionString)
		{
			xrp.Read();
			int i = 0;
			while(xrp.Name == groupMemberName)
			{
				var definition = new GridDefinition();
				if(xrp.MoveToAttribute(dimensionString))
				{
					definition.Length = GridLength.Parse(xrp.Value, Resources.DisplayMetrics);
				}
				definitionsList.Add(definition);
				xrp.Read();
				xrp.Read();
				i++;
			}
			if(i == 0)
			{
				throw new Exception("Empty markup definitions");
			}
			xrp.Read();
		}

		private void ParseMarkupXml(int resourceId)
		{
			var xrp = Resources.GetXml(resourceId);
			xrp.Read();
			if(xrp.EOF)
			{
				throw new Exception("Empty grid markup xml");
			}
			if(xrp.Name != "GridMarkup")
			{
				throw new Exception("Wrong name of grid markup root element");
			}
			xrp.Read();
			while(!xrp.EOF)
			{
				switch(xrp.Name)
				{
					case "ColumnDefinitions":
						ReadMarkupGroup(xrp, _columnDefinitions, "ColumnDefinition", "width");
						break;
					case "RowDefinitions":
						ReadMarkupGroup(xrp, _rowDefinitions, "RowDefinition", "height");
						break;
					case "GridMarkup":
						xrp.Read();
						break;
					default:
						throw new Exception(string.Format("Unknown grid markup group: {0}", xrp.Name));
				}
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			foreach(var viewInfo in _viewsMeasureInfos)
			{
				viewInfo.Value.IsMeasured = false;
			}

			var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
			var width = MeasureSpec.GetSize(widthMeasureSpec);
			var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
			var height = MeasureSpec.GetSize(heightMeasureSpec);

			// sizes without fixed columns
			int availableWidth = width;
			int availableHeight = height;

			foreach(var info in _columnsInfo)
			{
				info.Value.Unmeasure();
				if(widthMode != MeasureSpecMode.Unspecified && info.Value.Definition.Length.IsPixel)
				{
					availableWidth -= (int)info.Value.Definition.Length.Value;
				}
			}
			foreach(var info in _rowsInfo)
			{
				info.Value.Unmeasure();
				if(heightMode != MeasureSpecMode.Unspecified && info.Value.Definition.Length.IsPixel)
				{
					availableHeight -= (int)info.Value.Definition.Length.Value;
				}
			}

			// measure all views that we can measure at this moment (a-a and a-unspec)
			for(int i = 0; i < ChildCount; i++)
			{
				var child = GetChildAt(i);
				var lp = (GridViewLayoutParams)child.LayoutParameters;
				
				// TODO: do this while overriding adding
				if(!_viewsMeasureInfos.ContainsKey(child))
				{
					_viewsMeasureInfos.Add(child, new ViewMeasureInfo(child));
					for(int j = lp.Row; j < lp.Row + lp.RowSpan && j < _rowDefinitions.Count; j++)
					{
						_viewsMeasureInfos[child].AssociatedRows.Add(_rowDefinitions[j]);
						_rowsInfo[_rowDefinitions[j]].AttachedViews.Add(_viewsMeasureInfos[child]);
					}
					for(int j = lp.Column; j < lp.Column + lp.ColumnSpan && j < _columnDefinitions.Count; j++)
					{
						_viewsMeasureInfos[child].AssociatedColumns.Add(_columnDefinitions[j]);
						_columnsInfo[_columnDefinitions[j]].AttachedViews.Add(_viewsMeasureInfos[child]);
					}
				}

				bool canMeasureHeight = _viewsMeasureInfos[child].IsInAutoRow
					|| _viewsMeasureInfos[child].IsOnlyInPixelRows
					|| (_viewsMeasureInfos[child].IsInStarRow && heightMode == MeasureSpecMode.Unspecified);

				bool canMeasureWidth = _viewsMeasureInfos[child].IsInAutoColumn
					|| _viewsMeasureInfos[child].IsOnlyInPixelColumns
					|| (_viewsMeasureInfos[child].IsInStarColumn && widthMode == MeasureSpecMode.Unspecified);

				if(!canMeasureHeight || !canMeasureWidth)
				{
					continue;
				}

				var childWidthMeasureSpec = MeasureSpec.MakeMeasureSpec(width, 
				                                                        widthMode == MeasureSpecMode.Unspecified ? widthMode : MeasureSpecMode.AtMost);
				var childHeightMeasureSpec = MeasureSpec.MakeMeasureSpec(height, 
				                                                         heightMode == MeasureSpecMode.Unspecified ? heightMode : MeasureSpecMode.AtMost);

				child.Measure(childWidthMeasureSpec, childHeightMeasureSpec);
				_viewsMeasureInfos[child].IsMeasured = true;

				foreach(var column in _viewsMeasureInfos[child].AssociatedColumns)
				{
					var info = _columnsInfo[column];
					if(widthMode == MeasureSpecMode.Unspecified)
					{
						if(info.Definition.Length.IsStar
							&& !(info.IsMeasured && info.Length > child.MeasuredWidth))
						{
							info.SetLength(child.MeasuredWidth - _viewsMeasureInfos[child].PixelTypeColumnsWidth);
							break;
						}
					}
					else
					{
						if(info.Definition.Length.IsAuto
							&& !(info.IsMeasured && info.Length > child.MeasuredWidth))
						{
							info.SetLength(Math.Max(0, child.MeasuredWidth - _viewsMeasureInfos[child].PixelTypeColumnsWidth));
							break;
						}
					}
				}
				
				foreach(var row in _viewsMeasureInfos[child].AssociatedRows)
				{
					var info = _rowsInfo[row];
					if(heightMode == MeasureSpecMode.Unspecified)
					{
						if(info.Definition.Length.IsStar
							&& !(info.IsMeasured && info.Length > child.MeasuredWidth))
						{
							info.SetLength(Math.Max(0, child.MeasuredHeight - _viewsMeasureInfos[child].PixelTypeRowsHeight));
							break;
						}
					}
					else
					{
						if(info.Definition.Length.IsAuto
							&& !(info.IsMeasured && info.Length > child.MeasuredWidth))
						{
							info.SetLength(child.MeasuredHeight - _viewsMeasureInfos[child].PixelTypeRowsHeight);
							break;
						}
					}
				}
			}

			if(widthMode == MeasureSpecMode.Unspecified)
			{
				double maxLength = 0;
				double maxStarLength = 0;
				foreach(var column in _columnsInfo)
				{
					if(!column.Value.Definition.Length.IsStar || !column.Value.IsMeasured)
					{
						continue;
					}
					if(maxStarLength == 0 
						|| (_widthStarSum / column.Value.Definition.Length.Value) * column.Value.Length > (_widthStarSum / maxStarLength) * maxLength)
					{
						maxStarLength = column.Value.Definition.Length.Value;
						maxLength = column.Value.Length;
					}
				}
				double fullWidth = (_widthStarSum / maxStarLength) * maxLength;
				foreach(var column in _columnsInfo)
				{
					if(column.Value.Definition.Length.IsStar)
					{
						column.Value.SetLength(fullWidth*(column.Value.Definition.Length.Value/_widthStarSum));
					}
				}
			}
			else
			{
				foreach(var column in _columnsInfo)
				{
					if(column.Value.IsMeasured)
					{
						availableWidth -= (int)column.Value.Length;
					}
				}
				if(availableWidth > 0)
				{
					foreach(var column in _columnsInfo)
					{
						if(column.Value.Definition.Length.IsStar)
						{
							column.Value.SetLength((column.Value.Definition.Length.Value/_widthStarSum)*availableWidth);
						}
					}
				}
			}

			if(heightMode == MeasureSpecMode.Unspecified)
			{
				double maxLength = 0;
				double maxStarLength = 0;
				foreach(var row in _rowsInfo)
				{
					if(!row.Value.Definition.Length.IsStar || !row.Value.IsMeasured)
					{
						continue;
					}
					if(maxStarLength == 0 
						|| (_widthStarSum / row.Value.Definition.Length.Value) * row.Value.Length > (_widthStarSum / maxStarLength) * maxLength)
					{
						maxStarLength = row.Value.Definition.Length.Value;
						maxLength = row.Value.Length;
					}
				}
				double fullHeight = (_heightStarSum / maxStarLength) * maxLength;
				foreach(var row in _rowsInfo)
				{
					if(row.Value.Definition.Length.IsStar)
					{
						row.Value.SetLength(fullHeight*(row.Value.Definition.Length.Value/_heightStarSum));
					}
				}
			}
			else
			{
				foreach(var row in _rowsInfo)
				{
					if(row.Value.IsMeasured)
					{
						availableHeight -= (int)row.Value.Length;
					}
				}
				if(availableHeight > 0)
				{
					foreach(var row in _rowsInfo)
					{
						if(row.Value.Definition.Length.IsStar)
						{
							row.Value.SetLength((row.Value.Definition.Length.Value/_heightStarSum)*availableHeight);
						}
					}
				}
			}

			foreach(var columnInfo in _columnsInfo)
			{
				if(columnInfo.Value.Definition.Length.IsPixel)
				{
					columnInfo.Value.SetLength(columnInfo.Value.Definition.Length.Value);
				}
			}

			foreach(var rowInfo in _rowsInfo)
			{
				if(rowInfo.Value.Definition.Length.IsPixel)
				{
					rowInfo.Value.SetLength(rowInfo.Value.Definition.Length.Value);
				}
			}

			bool isAvailableSizeChanged = false;
			foreach(var columnInfo in _columnsInfo)
			{
				if(columnInfo.Value.Definition.Length.IsAuto)
				{
					int maxWidth = 0;
					foreach(var viewInfo in columnInfo.Value.AttachedViews)
					{
						if(!viewInfo.IsMeasured)
						{
							int measureHeight = 0;

							foreach(var row in viewInfo.AssociatedRows)
							{
								if(_rowsInfo[row].IsMeasured)
								{
									measureHeight += (int)_rowsInfo[row].Length;
								}
							}
							viewInfo.View.Measure(MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.AtMost),
							                      MeasureSpec.MakeMeasureSpec(measureHeight, MeasureSpecMode.AtMost));
						}
						maxWidth = Math.Max(maxWidth, viewInfo.View.MeasuredWidth);
					}
					columnInfo.Value.SetLength(maxWidth);
					availableWidth -= maxWidth;
					isAvailableSizeChanged = true;
				}
			}

			if(isAvailableSizeChanged && heightMode != MeasureSpecMode.Unspecified)
			{
				foreach(var column in _columnsInfo)
				{
					if(column.Value.Definition.Length.IsStar)
					{
						if(availableWidth > 0)
						{
							column.Value.SetLength((column.Value.Definition.Length.Value/_widthStarSum)*availableWidth);
						}
						else
						{
							column.Value.SetLength(0);
						}
					}
				}
			}
			
			isAvailableSizeChanged = false;
			foreach(var rowInfo in _rowsInfo)
			{
				if(rowInfo.Value.Definition.Length.IsAuto)
				{
					int maxHeight = 0;
					foreach(var viewInfo in rowInfo.Value.AttachedViews)
					{
						if(!viewInfo.IsMeasured)
						{
							int measureWidth = 0;

							foreach(var column in viewInfo.AssociatedColumns)
							{
								if(_columnsInfo[column].IsMeasured)
								{
									measureWidth += (int)_columnsInfo[column].Length;
								}
							}
							viewInfo.View.Measure(MeasureSpec.MakeMeasureSpec(measureWidth, MeasureSpecMode.AtMost),
							                      MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.AtMost));
						}
						maxHeight = Math.Max(maxHeight, viewInfo.View.MeasuredHeight);
					}
					rowInfo.Value.SetLength(maxHeight);
					availableWidth -= maxHeight;
					isAvailableSizeChanged = true;
				}
			}

			if(isAvailableSizeChanged && heightMode != MeasureSpecMode.Unspecified)
			{
				if(availableHeight > 0)
				{
					foreach(var row in _rowsInfo)
					{
						if(row.Value.Definition.Length.IsStar)
						{
							if(availableHeight > 0)
							{
								row.Value.SetLength((row.Value.Definition.Length.Value/_heightStarSum)*availableHeight);
							}
							else
							{
								row.Value.SetLength(0);
							}
						}
					}
				}
			}

			// TODO - shift in *apa* scheme
			/*if(widthMode != MeasureSpecMode.Unspecified)
			{
				foreach(var columnInfo in _columnsInfo)
				{
					if(!columnInfo.Value.Definition.Length.IsAuto)
					{
						continue;
					}
					double maxDiffer = 0;
					foreach(var viewInfo in columnInfo.Value.AttachedViews)
					{
						if(!viewInfo.IsMeasured)
						{
							continue;
						}

						double sum = 0;
						double starSum = 0;
						foreach(var column in viewInfo.AssociatedColumns)
						{
							if(column.Length.IsStar)
							{
								starSum += column.Length.Value;
							}
							else if(column.Length.IsPixel)
							{
								sum += column.Length.Value;
							}
						}
					}
				}
			}*/

			// remove all views that removed before onMeasure
			// TODO: do this while overriding removing
			if(_viewsMeasureInfos.Count > ChildCount)
			{
				var list = new List<View>(ChildCount);
				for(int i = 0; i < ChildCount; i++)
				{
					list.Add(GetChildAt(i));
				}
				foreach(var info in _viewsMeasureInfos)
				{
					if(!list.Contains(info.Value.View))
					{
						RemoveViewFromCache(info.Value.View);
					}
				}
			}

			double mWidth = 0;
			double mHeight = 0;
			foreach(var columnInfo in _columnsInfo)
			{
				mWidth += columnInfo.Value.Length;
			}
			foreach(var rowInfo in _rowsInfo)
			{
				mHeight += rowInfo.Value.Length;
			}
			SetMeasuredDimension((int)mWidth, (int)mHeight);
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			foreach(var viewInfo in _viewsMeasureInfos)
			{
				double shiftTemp = 0;
				int left = 0;
				int right = 0;
				int top = 0;
				int bottom = 0;
				var lp = (GridViewLayoutParams)viewInfo.Value.View.LayoutParameters;
				for(int i = 0; i < lp.Column+lp.ColumnSpan; i++)
				{
					if(i == lp.Column)
					{
						left = (int)shiftTemp;
					}
					shiftTemp += _columnsInfo[_columnDefinitions[i]].Length;
				}
				right = (int)shiftTemp;
				shiftTemp = 0;
				for(int i = 0; i < lp.Row+lp.RowSpan; i++)
				{
					if(i == lp.Row)
					{
						top = (int)shiftTemp;
					}
					shiftTemp += _rowsInfo[_rowDefinitions[i]].Length;
				}
				bottom = (int)shiftTemp;
				if(!viewInfo.Value.IsMeasured)
				{
					viewInfo.Value.View.Measure(MeasureSpec.MakeMeasureSpec(right - left,MeasureSpecMode.Exactly),
					                            MeasureSpec.MakeMeasureSpec(bottom - top, MeasureSpecMode.Exactly));
				}
				viewInfo.Value.View.Layout(left, top, right, bottom);
			}
		}

		private void RemoveViewFromCache(View view)
		{
			_viewsMeasureInfos.Remove(view);
			_viewsMeasureInfos.Remove(view);
			foreach(var column in _columnsInfo)
			{
				if(column.Value.AttachedViews.Contains(_viewsMeasureInfos[view]))
				{
					column.Value.AttachedViews.Remove(_viewsMeasureInfos[view]);
				}
			}
			foreach(var row in _rowsInfo)
			{
				if(row.Value.AttachedViews.Contains(_viewsMeasureInfos[view]))
				{
					row.Value.AttachedViews.Remove(_viewsMeasureInfos[view]);
				}
			}
		}
	}
}


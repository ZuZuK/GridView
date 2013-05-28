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
			private double _staticWidth = 0;
			private double _staticHeight = 0;
			private double _widthStarSum = 0;
			private double _heightStarSum = 0;
			private GridDefinitionMeasureInfo _firstAutoRow = null;
			private GridDefinitionMeasureInfo _firstAutoColumn = null;

			public View View { get; private set; }

			public List<GridDefinitionMeasureInfo> AssociatedColumns { get; private set; }

			public List<GridDefinitionMeasureInfo> AssociatedRows { get; private set; }

			public bool IsMeasured { get; set; }

			public GridDefinitionMeasureInfo FirstAutoRow
			{
				get { return _firstAutoRow; }
			}

			public GridDefinitionMeasureInfo FirstAutoColumn
			{
				get { return _firstAutoColumn; }
			}

			public double WidthStarSum
			{
				get { return _widthStarSum; }
			}

			public double HeightStarSum
			{
				get { return _heightStarSum; }
			}

			public bool IsInStarColumn
			{
				get { return _widthStarSum > 0; }
			}

			public bool IsInStarRow
			{
				get { return _heightStarSum > 0; }
			}

			public bool IsOnlyInPixelColumns
			{
				get { return !IsInStarColumn && !IsInAutoColumn; }
			}

			public bool IsOnlyInPixelRows
			{
				get { return !IsInStarRow && !IsInAutoRow; }
			}

			public bool IsInAutoColumn
			{
				get { return _firstAutoColumn != null; }
			}

			public bool IsInAutoRow
			{
				get { return _firstAutoRow != null; }
			}

			public double StaticWidth
			{
				get { return _staticWidth; }
			}

			public double StaticHeight
			{
				get { return _staticHeight; }
			}

			public ViewMeasureInfo(View view, GridViewLayoutParams lp,
			                       List<GridDefinition> rows, List<GridDefinition> columns,
			                       Dictionary<GridDefinition, GridDefinitionMeasureInfo> rowsInfo, Dictionary<GridDefinition, GridDefinitionMeasureInfo> columnsInfo)
			{
				View = view;
				AssociatedColumns = new List<GridDefinitionMeasureInfo>();
				AssociatedRows = new List<GridDefinitionMeasureInfo>();
				for(int j = lp.Row; j < lp.Row + lp.RowSpan && j < rows.Count; j++)
				{
					AssociatedRows.Add(rowsInfo[rows[j]]);
					rowsInfo[rows[j]].AttachedViews.Add(this);
				}
				for(int j = lp.Column; j < lp.Column + lp.ColumnSpan && j < columns.Count; j++)
				{
					AssociatedColumns.Add(columnsInfo[columns[j]]);
					columnsInfo[columns[j]].AttachedViews.Add(this);
				}
				
				foreach(var row in AssociatedRows)
				{
					if(row.Definition.Length.IsStar)
					{
						_heightStarSum += row.Definition.Length.Value;
					}
					else if(row.Definition.Length.IsAuto)
					{
						if(_firstAutoRow == null)
						{
							_firstAutoRow = row;
						}
					}
					else
					{
						_staticHeight += row.Definition.Length.Value;
					}
				}
				
				foreach(var column in AssociatedColumns)
				{
					if(column.Definition.Length.IsStar)
					{
						_widthStarSum += column.Definition.Length.Value;
					}
					else if(column.Definition.Length.IsAuto)
					{
						if(_firstAutoColumn == null)
						{
							_firstAutoColumn = column;
						}
					}
					else
					{
						_staticWidth += column.Definition.Length.Value;
					}
				}
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
				if(Definition.Length.IsPixel)
				{
					SetLength(Definition.Length.Value);
				}
			}

			public void SetLength(double length)
			{
				IsMeasured = true;
				Length = length;
			}

			public void Unmeasure()
			{
				if(Definition.Length.IsPixel)
				{
					return;
				}
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
				var rowInfo = new GridDefinitionMeasureInfo(row);
				_rowsInfo.Add(row, rowInfo);
				if(row.Length.IsStar)
				{
					_heightStarSum += row.Length.Value;
				}
			}
			foreach(var column in _columnDefinitions)
			{
				var columnInfo = new GridDefinitionMeasureInfo(column);
				_columnsInfo.Add(column, columnInfo);
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
		// check all views and definitions
		private void PremeasureActions()
		{
			var children = new List<View>(ChildCount);
			for(int i = 0; i < ChildCount; i++)
			{
				children.Add(GetChildAt(i));
			}
			foreach(var child in children)
			{
				if(!_viewsMeasureInfos.ContainsKey(child))
				{
					var lp = (GridViewLayoutParams)child.LayoutParameters;
					_viewsMeasureInfos.Add(child, 
					                       new ViewMeasureInfo(child, lp, _rowDefinitions, _columnDefinitions, _rowsInfo, _columnsInfo));
				}
			}

			if(_viewsMeasureInfos.Count > ChildCount)
			{
				foreach(var info in _viewsMeasureInfos.Values)
				{
					if(!children.Contains(info.View))
					{
						RemoveViewFromCache(info.View);
					}
				}
			}

			foreach(var viewInfo in _viewsMeasureInfos.Values)
			{
				viewInfo.IsMeasured = false;
			}
		}

		private void RemoveViewFromCache(View view)
		{
			_viewsMeasureInfos.Remove(view);
			foreach(var columnInfo in _columnsInfo.Values)
			{
				if(columnInfo.AttachedViews.Contains(_viewsMeasureInfos[view]))
				{
					columnInfo.AttachedViews.Remove(_viewsMeasureInfos[view]);
				}
			}
			foreach(var rowInfo in _rowsInfo.Values)
			{
				if(rowInfo.AttachedViews.Contains(_viewsMeasureInfos[view]))
				{
					rowInfo.AttachedViews.Remove(_viewsMeasureInfos[view]);
				}
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			PremeasureActions();

			var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
			var width = MeasureSpec.GetSize(widthMeasureSpec);
			var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
			var height = MeasureSpec.GetSize(heightMeasureSpec);

			// sizes without fixed columns
			double availableWidth = width;
			double availableHeight = height;

			// compute size for auto and star definitions
			foreach(var columnInfo in _columnsInfo.Values)
			{
				columnInfo.Unmeasure();
			}
			foreach(var rowInfo in _rowsInfo.Values)
			{
				rowInfo.Unmeasure();
			}
			// measure all views that we can measure at start
			MeasureFirstStageViews(width, widthMode, height, heightMode);

			if(widthMode == MeasureSpecMode.Unspecified)
			{
				double maxLength = 0;
				double maxStarLength = 0;
				foreach(var columnInfo in _columnsInfo.Values)
				{
					if(!columnInfo.Definition.Length.IsStar || !columnInfo.IsMeasured)
					{
						continue;
					}
					if(maxStarLength == 0 
						|| (_widthStarSum / columnInfo.Definition.Length.Value) * columnInfo.Length > (_widthStarSum / maxStarLength) * maxLength)
					{
						maxStarLength = columnInfo.Definition.Length.Value;
						maxLength = columnInfo.Length;
					}
				}
				double fullWidth = (_widthStarSum / maxStarLength) * maxLength;
				foreach(var columnInfo in _columnsInfo.Values)
				{
					if(columnInfo.Definition.Length.IsStar)
					{
						columnInfo.SetLength(fullWidth*(columnInfo.Definition.Length.Value/_widthStarSum));
					}
				}
			}
			else
			{
				foreach(var columnInfo in _columnsInfo.Values)
				{
					if(columnInfo.IsMeasured)
					{
						availableWidth -= columnInfo.Length;
					}
				}
				if(availableWidth > 0)
				{
					foreach(var columnInfo in _columnsInfo.Values)
					{
						if(columnInfo.Definition.Length.IsStar)
						{
							columnInfo.SetLength((columnInfo.Definition.Length.Value/_widthStarSum)*availableWidth);
						}
					}
				}
			}

			if(heightMode == MeasureSpecMode.Unspecified)
			{
				double maxLength = 0;
				double maxStarLength = 0;
				foreach(var rowInfo in _rowsInfo.Values)
				{
					if(!rowInfo.Definition.Length.IsStar || !rowInfo.IsMeasured)
					{
						continue;
					}
					if(maxStarLength == 0 
						|| (_widthStarSum / rowInfo.Definition.Length.Value) * rowInfo.Length > (_widthStarSum / maxStarLength) * maxLength)
					{
						maxStarLength = rowInfo.Definition.Length.Value;
						maxLength = rowInfo.Length;
					}
				}
				double fullHeight = (_heightStarSum / maxStarLength) * maxLength;
				foreach(var rowInfo in _rowsInfo.Values)
				{
					if(rowInfo.Definition.Length.IsStar)
					{
						rowInfo.SetLength(fullHeight*(rowInfo.Definition.Length.Value/_heightStarSum));
					}
				}
			}
			else
			{
				foreach(var rowInfo in _rowsInfo.Values)
				{
					if(rowInfo.IsMeasured)
					{
						availableHeight -= rowInfo.Length;
					}
				}
				if(availableHeight > 0)
				{
					foreach(var rowInfo in _rowsInfo.Values)
					{
						if(rowInfo.Definition.Length.IsStar)
						{
							rowInfo.SetLength((rowInfo.Definition.Length.Value/_heightStarSum)*availableHeight);
						}
					}
				}
			}

			bool isAvailableSizeChanged = false;
			foreach(var columnInfo in _columnsInfo.Values)
			{
				if(columnInfo.Definition.Length.IsAuto)
				{
					int maxWidth = 0;
					foreach(var viewInfo in columnInfo.AttachedViews)
					{
						if(!viewInfo.IsMeasured)
						{
							double viewMeasureHeight = 0;

							foreach(var row in viewInfo.AssociatedRows)
							{
								if(row.IsMeasured)
								{
									viewMeasureHeight += row.Length;
								}
							}
							viewInfo.View.Measure(MeasureSpec.MakeMeasureSpec((int)width, MeasureSpecMode.AtMost),
							                      MeasureSpec.MakeMeasureSpec((int)viewMeasureHeight, MeasureSpecMode.AtMost));
						}
						maxWidth = Math.Max(maxWidth, viewInfo.View.MeasuredWidth);
					}
					columnInfo.SetLength(maxWidth);
					availableWidth -= maxWidth;
					isAvailableSizeChanged = true;
				}
			}

			if(isAvailableSizeChanged && heightMode != MeasureSpecMode.Unspecified)
			{
				foreach(var columnInfo in _columnsInfo.Values)
				{
					if(columnInfo.Definition.Length.IsStar)
					{
						if(availableWidth > 0)
						{
							columnInfo.SetLength((columnInfo.Definition.Length.Value / _widthStarSum) * availableWidth);
						}
						else
						{
							columnInfo.SetLength(0);
						}
					}
				}
			}
			
			isAvailableSizeChanged = false;
			foreach(var rowInfo in _rowsInfo.Values)
			{
				if(rowInfo.Definition.Length.IsAuto)
				{
					int maxHeight = 0;
					foreach(var viewInfo in rowInfo.AttachedViews)
					{
						if(!viewInfo.IsMeasured)
						{
							double measureWidth = 0;

							foreach(var column in viewInfo.AssociatedColumns)
							{
								if(column.IsMeasured)
								{
									measureWidth += column.Length;
								}
							}
							viewInfo.View.Measure(MeasureSpec.MakeMeasureSpec((int)measureWidth, MeasureSpecMode.AtMost),
							                      MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.AtMost));
						}
						maxHeight = Math.Max(maxHeight, viewInfo.View.MeasuredHeight);
					}
					rowInfo.SetLength(maxHeight);
					availableHeight -= maxHeight;
					isAvailableSizeChanged = true;
				}
			}

			if(isAvailableSizeChanged && heightMode != MeasureSpecMode.Unspecified)
			{
				if(availableHeight > 0)
				{
					foreach(var rowInfo in _rowsInfo.Values)
					{
						if(rowInfo.Definition.Length.IsStar)
						{
							if(availableHeight > 0)
							{
								rowInfo.SetLength((rowInfo.Definition.Length.Value / _heightStarSum) * availableHeight);
							}
							else
							{
								rowInfo.SetLength(0);
							}
						}
					}
				}
			}

			double changedSize;
			do
			{
				changedSize = 0;
				foreach(var columnInfo in _columnsInfo.Values)
				{
					if(!columnInfo.Definition.Length.IsAuto)
					{
						continue;
					}
					double columnShrinkedSize = 0;
					foreach(var viewInfo in columnInfo.AttachedViews)
					{
						double staticSizeForColumn = Math.Max(0, viewInfo.View.MeasuredWidth - viewInfo.StaticWidth);
						foreach(var viewColumnInfo in viewInfo.AssociatedColumns)
						{
							if(staticSizeForColumn == 0)
							{
								break;
							}
							if(viewColumnInfo.Definition.Length.IsPixel || viewColumnInfo == columnInfo)
							{
								continue;
							}
							staticSizeForColumn = Math.Max(0, staticSizeForColumn - viewColumnInfo.Length);
						}
						columnShrinkedSize = Math.Max(columnShrinkedSize, staticSizeForColumn);
					}
					if(columnShrinkedSize < columnInfo.Length)
					{
						columnInfo.SetLength(columnShrinkedSize);
						changedSize += columnInfo.Length - columnShrinkedSize;
						foreach(var starColumnInfo in _columnsInfo.Values)
						{
							if(starColumnInfo.Definition.Length.IsStar)
							{
								starColumnInfo.SetLength(starColumnInfo.Length + changedSize * (starColumnInfo.Definition.Length.Value / _widthStarSum));
							}
						}
					}
				}
			} while(changedSize!=0);
			
			do
			{
				changedSize = 0;
				foreach(var rowInfo in _rowsInfo.Values)
				{
					if(!rowInfo.Definition.Length.IsAuto)
					{
						continue;
					}
					double rowShrinkedSize = 0;
					foreach(var viewInfo in rowInfo.AttachedViews)
					{
						double staticSizeForRow = Math.Max(0, viewInfo.View.MeasuredWidth - viewInfo.StaticHeight);
						foreach(var viewRowInfo in viewInfo.AssociatedRows)
						{
							if(staticSizeForRow == 0)
							{
								break;
							}
							if(viewRowInfo.Definition.Length.IsPixel || viewRowInfo == rowInfo)
							{
								continue;
							}
							staticSizeForRow = Math.Max(0, staticSizeForRow - viewRowInfo.Length);
						}
						rowShrinkedSize = Math.Max(rowShrinkedSize, staticSizeForRow);
					}
					if(rowShrinkedSize < rowInfo.Length)
					{
						rowInfo.SetLength(rowShrinkedSize);
						changedSize += rowInfo.Length - rowShrinkedSize;
						foreach(var starRowInfo in _rowsInfo.Values)
						{
							if(starRowInfo.Definition.Length.IsStar)
							{
								starRowInfo.SetLength(starRowInfo.Length + changedSize * (starRowInfo.Definition.Length.Value / _heightStarSum));
							}
						}
					}
				}
			} while(changedSize!=0);

			double mWidth = 0;
			double mHeight = 0;
			foreach(var columnInfo in _columnsInfo.Values)
			{
				mWidth += columnInfo.Length;
			}
			foreach(var rowInfo in _rowsInfo.Values)
			{
				mHeight += rowInfo.Length;
			}
			SetMeasuredDimension((int)mWidth, (int)mHeight);
		}

		private void MeasureFirstStageViews(int width, MeasureSpecMode widthMode, int height, MeasureSpecMode heightMode)
		{			
			// measure all children that we can measure at this moment (p-a, p-p, a-a, p-unspec, a-unspec)
			foreach(var viewInfo in _viewsMeasureInfos.Values)
			{
				var child = viewInfo.View;

				bool canMeasureHeight = heightMode == MeasureSpecMode.Unspecified
					|| viewInfo.IsInAutoRow
					|| viewInfo.IsOnlyInPixelRows;

				bool canMeasureWidth = widthMode == MeasureSpecMode.Unspecified
					|| viewInfo.IsInAutoColumn
					|| viewInfo.IsOnlyInPixelColumns;

				if(!canMeasureHeight || !canMeasureWidth)
				{
					continue;
				}

				var childWidthMeasureSpec = MeasureSpec.MakeMeasureSpec(width,
				                                                        widthMode == MeasureSpecMode.Unspecified ? widthMode : MeasureSpecMode.AtMost);
				var childHeightMeasureSpec = MeasureSpec.MakeMeasureSpec(height,
				                                                         heightMode == MeasureSpecMode.Unspecified ? heightMode : MeasureSpecMode.AtMost);

				child.Measure(childWidthMeasureSpec, childHeightMeasureSpec);
				viewInfo.IsMeasured = true;

				var dynamicWidth = Math.Max(0, child.MeasuredWidth - viewInfo.StaticWidth);
				var dynamicHeight = Math.Max(0, child.MeasuredHeight - viewInfo.StaticHeight);
				if(viewInfo.IsInAutoRow)
				{
					if(!(viewInfo.FirstAutoRow.IsMeasured && viewInfo.FirstAutoRow.Length > dynamicHeight))
					{
						viewInfo.FirstAutoRow.SetLength(dynamicHeight);
					}
				}
				else if(viewInfo.IsInStarRow && heightMode == MeasureSpecMode.Unspecified)
				{
					foreach(var rowInfo in viewInfo.AssociatedRows)
					{
						if(rowInfo.Definition.Length.IsStar)
						{
							var dynamicStarHeight = dynamicHeight * (rowInfo.Definition.Length.Value / viewInfo.HeightStarSum);
							if(!(rowInfo.IsMeasured && rowInfo.Length > dynamicStarHeight))
							{
								rowInfo.SetLength(dynamicStarHeight);
							}
						}
					}
				}
				if(viewInfo.IsInAutoColumn)
				{
					if(!(viewInfo.FirstAutoColumn.IsMeasured && viewInfo.FirstAutoColumn.Length > dynamicWidth))
					{
						viewInfo.FirstAutoColumn.SetLength(dynamicWidth);
					}
				}
				else if(viewInfo.IsInStarColumn && heightMode == MeasureSpecMode.Unspecified)
				{
					foreach(var columnInfo in viewInfo.AssociatedColumns)
					{
						if(columnInfo.Definition.Length.IsStar)
						{
							var dynamicStarWidth = dynamicWidth * (columnInfo.Definition.Length.Value / viewInfo.WidthStarSum);
							if(!(columnInfo.IsMeasured && columnInfo.Length > dynamicStarWidth))
							{
								columnInfo.SetLength(dynamicStarWidth);
							}
						}
					}
				}
			}
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
	}
}


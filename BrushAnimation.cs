using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ProjectSpotlight
{
	public class BrushAnimation : AnimationTimeline
	{
		VisualBrush WorkingBrush;


		#region Properties
		public override Type TargetPropertyType
		{
			get
			{
				return typeof(Brush);
			}
		}

		//we must define From and To, AnimationTimeline does not have this properties
		public Brush From
		{
			get { return (Brush)GetValue(FromProperty); }
			set { SetValue(FromProperty, value); }
		}

		public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
				nameof(From),
				typeof(Brush),
				typeof(BrushAnimation));

		public Brush To
		{
			get { return (Brush)GetValue(ToProperty); }
			set { SetValue(ToProperty, value); }
		}

		public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
				nameof(To),
				typeof(Brush),
				typeof(BrushAnimation));
		#endregion Properties


		public object GetCurrentValue(Brush defaultOriginValue, Brush defaultDestinationValue, AnimationClock animationClock)
		{
			if (!animationClock.CurrentProgress.HasValue)
				return Brushes.Transparent;

			//use the standard values if From and To are not set 
			//(it is the value of the given property)
			defaultOriginValue = From ?? defaultOriginValue;
			defaultDestinationValue = To ?? defaultDestinationValue;

			if (animationClock.CurrentProgress.Value == 0)
				return defaultOriginValue;

			if (animationClock.CurrentProgress.Value == 1)
				return defaultDestinationValue;

			if (WorkingBrush == null)
				WorkingBrush = new VisualBrush(new Border()
				{
					Width = 1,
					Height = 1,
					Background = defaultOriginValue,
					Child = new Border()
					{
						Background = defaultDestinationValue,
						//Opacity = animationClock.CurrentProgress.Value,
					}
				});

			((WorkingBrush.Visual as Border).Child as Border).Opacity = animationClock.CurrentProgress.Value;

			return WorkingBrush;
		}

		public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
		{
			return GetCurrentValue(defaultOriginValue as Brush, defaultDestinationValue as Brush, animationClock);
		}

		protected override Freezable CreateInstanceCore()
		{
			return new BrushAnimation();
		}
	}
}

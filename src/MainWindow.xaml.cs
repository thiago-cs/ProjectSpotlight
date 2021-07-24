#if DEBUG
#define SIMULATION_MODE
#endif

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;

namespace ProjectSpotlight
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		#region Fields
		private bool forcedClose = false;
		private FrameworkElement currentContainer = null;

		// Animations for BigPicture control.
		private readonly Storyboard carouselStoryboard;
		private readonly ColorAnimation backgroundBrushAnimation;
		private readonly DoubleAnimation scaleXAnimation;
		private readonly DoubleAnimation scaleYAnimation;
		private readonly DoubleAnimation translateXAnimation;
		private readonly DoubleAnimation translateYAnimation;
		#endregion Fields


		#region Constructor and MainWindow events
		public MainWindow()
		{
			// 1. 
			InitializeComponent();

			if (SmallPicturesContainer.ItemsSource == null)
			{
				CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(App.Logic.NewItems);
				view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Model.ImageOrientation)));

				//SmallPicturesContainer.ItemsSource = App.Logic.NewItems;
				SmallPicturesContainer.ItemsSource = view;
			}

			if (Carousel.ItemsSource == null)
				Carousel.ItemsSource = SmallPicturesContainer.ItemsSource;

#if !DEBUG
			TestsButton.Visibility = Visibility.Collapsed;

#endif


			// 2. Animations
			Duration duration = new Duration(TimeSpan.FromMilliseconds(350));

			// 2.1. Fade in/out animations
			backgroundBrushAnimation = new ColorAnimation(Colors.Transparent, duration, FillBehavior.HoldEnd);
			Storyboard.SetTarget(backgroundBrushAnimation, CarouselContainer);
			Storyboard.SetTargetProperty(backgroundBrushAnimation, new PropertyPath("Background.Color"));

			// 2.2. Scale animations.
			scaleXAnimation = new DoubleAnimation(0, duration, FillBehavior.HoldEnd);
			//Storyboard.SetTarget(scaleXAnimation, TheScaleTransform);
			//Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
			Storyboard.SetTarget(scaleXAnimation, Carousel);
			Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.Children[0].ScaleX"));

			scaleYAnimation = new DoubleAnimation(0, duration, FillBehavior.HoldEnd);
			Storyboard.SetTarget(scaleYAnimation, Carousel);
			Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.Children[0].ScaleY"));

			// 2.3. Translation animation.
			translateXAnimation = new DoubleAnimation(0, duration, FillBehavior.HoldEnd);
			Storyboard.SetTarget(translateXAnimation, Carousel);
			Storyboard.SetTargetProperty(translateXAnimation, new PropertyPath("RenderTransform.Children[1].X"));

			translateYAnimation = new DoubleAnimation(0, duration, FillBehavior.HoldEnd);
			Storyboard.SetTarget(translateYAnimation, Carousel);
			Storyboard.SetTargetProperty(translateYAnimation, new PropertyPath("RenderTransform.Children[1].Y"));

			carouselStoryboard = new Storyboard();
			carouselStoryboard.Children.Add(backgroundBrushAnimation);
			carouselStoryboard.Children.Add(scaleXAnimation);
			carouselStoryboard.Children.Add(scaleYAnimation);
			carouselStoryboard.Children.Add(translateXAnimation);
			carouselStoryboard.Children.Add(translateYAnimation);
			carouselStoryboard.Completed += CarouselStoryboard_Completed;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// 1. 
#if SIMULATION_MODE
			App.Logic.SimulateScavenge();
#else
			App.Logic.Scavenge();
#endif

			// 2. ...
			if (App.Logic.NewItems.Count == 0)
			{
				Text1.Visibility = Visibility.Visible;
				Text1.Foreground = Brushes.Black;
			}
		}

		private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (forcedClose)
			{
				return;
			}

			// Unless some logic below oposes, it is assumed that the window can be closed normally.
			bool canClose = true;

			// Is there any new images?
			if (App.Logic.NewItems.Count != 0)
			{
				e.Cancel = true;

				string message = App.Logic.NewItems.Count == 1 ?
					"Do you want to keep the new image found?" :
					"Do you want to keep the new images found?";

				var dialogSettings = new MetroDialogSettings
				{
					AffirmativeButtonText = "Yes",
					NegativeButtonText = "No",
					DefaultButtonFocus = MessageDialogResult.Affirmative,
				};

				var result = await this.ShowMessageAsync(Title, message, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);

				if (result == MessageDialogResult.Affirmative)
				{
					await SaveRemainingItems();

					if (App.Logic.NewItems.Count != 0)
						canClose = false;
				}
				else
					DiscardRemainingItems();

				// If the "close window" process has been canceled for any reason
				// but everyone agrees that the window can be closed now, then...
				if (e.Cancel && canClose)
				{
					forcedClose = true;
					Close();
				}
			}
		}
		#endregion Constructor and MainWindow events


		#region Methods
		
		// UI Methods

		private void ShowCarousel()
		{
			// 1. Sets the image to be displayed.
			//Carousel.SelectedIndex = -1;
			//Carousel.SelectedIndex = SmallPicturesContainer.SelectedIndex;
			CarouselContainer.Visibility = Visibility.Visible;
			HideCarouselControls();

			// 3. Animations for a smooth transition
			// 3.1. Background color
			backgroundBrushAnimation.From = Colors.Transparent;
			backgroundBrushAnimation.To = Colors.Black;

			// 3.2. Positions and sizes the BigPicture control to coincide with the image inside the corresponding listviewitem.
			SetupRenderTransformAnimation(true);

			// 3.3. Starts the animation.
			carouselStoryboard.Begin();
		}

		private void HideCarousel()
		{
			// 1. Animations for a smooth transition
			// 1.1. Background color
			backgroundBrushAnimation.From = Colors.Black;
			backgroundBrushAnimation.To = Colors.Transparent;

			// 1.2. Positions and sizes the Carousel control to coincide with the image inside the corresponding ListViewItem.
			SetupRenderTransformAnimation(false);

			// 1.3. Starts the animation.
			carouselStoryboard.Begin();
			HideCarouselControls();
		}

		private void ShowCarouselControls()
		{
			DeleteButton.Visibility = Visibility.Visible;
			BackButton.Visibility = Visibility.Visible;
			Carousel.ShowControlButtons();
		}

		private void HideCarouselControls()
		{
			Carousel.HideControlButtons();
			DeleteButton.Visibility = Visibility.Collapsed;
			BackButton.Visibility = Visibility.Collapsed;
		}

		private void SetupRenderTransformAnimation(bool entrance)
		{
			// 0. 
			if (Carousel.SelectedIndex == -1)
				return;


			// 1. Calculations.
			// 1.1. 
			currentContainer = GetSelectedItemContainer();
			FrameworkElement image = GetImageControl(currentContainer);
			BitmapImage bitmap = (SmallPicturesContainer.SelectedItem as Model).Image;

			// 1.2. 
			double ratioBitmapToBigImage = Math.Min(LayoutRoot.ActualHeight / bitmap.PixelHeight, LayoutRoot.ActualWidth / bitmap.PixelWidth);
			double ratioBitmapToSmallImage = image.ActualHeight / bitmap.PixelHeight;        // or	image.ActualWidth / bitmap.PixelWidth;

			double widthGap = (LayoutRoot.ActualWidth - bitmap.PixelWidth * ratioBitmapToBigImage) / 2;
			double heightGap = (LayoutRoot.ActualHeight - bitmap.PixelHeight * ratioBitmapToBigImage) / 2;

			double smallWidthGap = widthGap / ratioBitmapToBigImage * ratioBitmapToSmallImage;
			double smallHeightGap = heightGap / ratioBitmapToBigImage * ratioBitmapToSmallImage;


			// 2. Results.
			// 2.1. Translate
			double scale = ratioBitmapToSmallImage / ratioBitmapToBigImage;

			// 2.2. Scale
			Point offset = GetElementOffset(SmallPicturesContainer, image);
			double x = offset.X - smallWidthGap;
			double y = offset.Y - smallHeightGap;


			// 3. Applies results.
			currentContainer.Visibility = Visibility.Hidden;

			if (entrance)
			{
				scaleXAnimation.From = scaleYAnimation.From = scale;
				scaleXAnimation.To = scaleYAnimation.To = 1;

				translateXAnimation.From = x;
				translateYAnimation.From = y;
				translateXAnimation.To = translateYAnimation.To = 0;
			}
			else
			{
				scaleXAnimation.From = scaleYAnimation.From = 1;
				scaleXAnimation.To = scaleYAnimation.To = scale;

				translateXAnimation.From = translateYAnimation.From = 0;
				translateXAnimation.To = x;
				translateYAnimation.To = y;
			}

			//
			FrameworkElement GetImageControl(DependencyObject element)
			{
				element = VisualTreeHelper.GetChild(element, 0);
				element = VisualTreeHelper.GetChild(element, 0);
				element = VisualTreeHelper.GetChild(element, 1);

				return element as FrameworkElement;
			}
		}

		private FrameworkElement GetSelectedItemContainer()
		{
			if (SmallPicturesContainer.ItemContainerGenerator.ContainerFromItem(SmallPicturesContainer.SelectedItem) is FrameworkElement element)
				return element;

			return null;
		}

		private static Point GetElementOffset(Visual ancestor, FrameworkElement element)
		{
			return element.TransformToAncestor(ancestor).Transform(new Point(0, 0));
		}

		// Non-UI Methods

		private static void RemoveItem(Model item)
		{
#if SIMULATION_MODE
			App.Logic.RemoveItem(item, false);
#else
			try
			{
				App.Logic.RemoveItem(item, true);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
#endif
		}

		private async Task SaveRemainingItems()
		{
			var dialogSettings = new MetroDialogSettings
			{
				AffirmativeButtonText = "Try again",
				NegativeButtonText = "Cancel",
				DefaultButtonFocus = MessageDialogResult.Affirmative,
			};

			bool keepTrying;
			do try
				{
					App.Logic.GroupImagesByAspectRation();
					keepTrying = false;
				}
				catch (Exception ex)
				{
					var list = ex.Data["fails"] as System.Collections.Generic.List<Tuple<Model, Exception>>;
					var message = $"Could not move {list.Count} file(s):";

					foreach (var tuple in list)
					{
						var item = tuple.Item1;
						var exception = tuple.Item2;
						message += "\n" + item.FileName + "\t" + exception.Message;
					}

					var result = await this.ShowMessageAsync(Title, message, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);

					keepTrying = result == MessageDialogResult.Affirmative;
				}
			while (keepTrying);
		}

		private void DiscardRemainingItems()
		{
			for (int i = App.Logic.NewItems.Count - 1; i >= 0; i--)
				RemoveItem(App.Logic.NewItems[i]);
		}
		#endregion Methods


		#region Events
		private void ItemContainer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (SmallPicturesContainer.SelectedIndex == -1)
				return;

			ShowCarousel();
		}

		private void WindowBackButton_Click(object sender, RoutedEventArgs e)
		{
			HideCarousel();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			//
			RemoveItem(Carousel.SelectedItem as Model);

			//
			if (App.Logic.NewItems.Count == 0)
			{
				HideCarouselControls();
				Text1.Visibility = Visibility.Visible;
				Text1.Foreground = Brushes.White;
			}
		}

		private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
		{
			//
			if (!(sender is Button button && button.Tag is ImageOrientation orientation))
				return;

			//
			for (int i = App.Logic.NewItems.Count - 1; 0 <= i; i--)
			{
				Model item = App.Logic.NewItems[i];
				if (item.ImageOrientation == orientation)
					RemoveItem(item);
			}

			//
			if (App.Logic.NewItems.Count == 0)
				Text1.Visibility = Visibility.Visible;
		}

		private void CarouselStoryboard_Completed(object sender, EventArgs e)
		{
			currentContainer.Visibility = Visibility.Visible;


			if (backgroundBrushAnimation.From != Colors.Black)
			{
				// entrance
				//CarouselContainer.Visibility = Visibility.Visible;
				ShowCarouselControls();
				Carousel.Focus();
			}
			else
			{
				// exit
				CarouselContainer.Visibility = Visibility.Hidden;
				SmallPicturesContainer.Focus();
			}
		}

		private void Carousel_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			switch (e.Key)
			{
				case System.Windows.Input.Key.Left:
				case System.Windows.Input.Key.Right:
					e.Handled = true;
					break;

				case System.Windows.Input.Key.PageUp:
					if (0 < Carousel.SelectedIndex)
						Carousel.SelectedIndex--;
					e.Handled = true;
					break;

				case System.Windows.Input.Key.PageDown:
					if (Carousel.SelectedIndex < App.Logic.NewItems.Count - 1)
						Carousel.SelectedIndex++;
					e.Handled = true;
					break;

				case System.Windows.Input.Key.Delete:
					RemoveItem(Carousel.SelectedItem as Model);
					e.Handled = true;
					break;

				case System.Windows.Input.Key.Escape:
					HideCarousel();
					e.Handled = true;
					break;
			}
		}

		private void TestsButton_Click(object sender, RoutedEventArgs e)
		{
		}
		#endregion Events


		/*T GetVisualParent<T>(DependencyObject child) where T : DependencyObject
		{
			//get parent item
			DependencyObject parentObject = child;

			//we've reached the end of the tree
			while (parentObject != null)
			{
				//check if the parent matches the type we're looking for
				if (parentObject is T)
					return parentObject as T;

				//use recursion to proceed with next level
				//parentObject = GetParentObject(parentObject);
				parentObject = VisualTreeHelper.GetParent(parentObject);
			}

			return null;
		}
		*/
	}
}
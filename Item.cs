using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ProjectSpotlight
{
	public class Item
	{
		#region Properties
		public string FilePath { get; }
		public BitmapImage Image { get; }
		public ImageOrientation ImageOrientation { get; }
		public string FileName => Path.GetFileNameWithoutExtension(FilePath);
		#endregion Properties


		public Item(string path)
		{
			// 
			FilePath = path;

			// 
			Image = new BitmapImage();
			Image.BeginInit();
			Image.CacheOption = BitmapCacheOption.OnLoad;
			Image.UriSource = new Uri(path);
			Image.EndInit();

			// 
			ImageOrientation = Image.PixelWidth < Image.PixelHeight ? ImageOrientation.Portrait : ImageOrientation.Landscape;
		}
	}


	public enum ImageOrientation
	{
		Landscape,
		Portrait,
	}
}
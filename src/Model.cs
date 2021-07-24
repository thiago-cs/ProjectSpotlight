using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ProjectSpotlight
{
	public class Model
	{
		#region Properties
		public string FilePath { get; }
		public BitmapImage Image { get; }
		public ImageOrientation ImageOrientation { get; }
		public string FileName => Path.GetFileNameWithoutExtension(FilePath);
		#endregion Properties


		public Model(string path)
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
}
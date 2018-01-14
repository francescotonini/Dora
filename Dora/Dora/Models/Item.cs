using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Dora.Models
{
    public class Item
    {
        public string Name { get; set; }
        public IEnumerable<Item> SubData { get; set; }
        public BitmapSource Icon { get; set; }
        public string FullPath { get; set; }
    }
}

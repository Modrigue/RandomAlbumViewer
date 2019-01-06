using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlbumViewer
{
    public static class Tools
    {

        // randomize list order
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rnd = new Random();

            for (var i = 0; i < list.Count; i++)
                list.Swap(i, rnd.Next(i, list.Count));
        }

        // swaps elements <i> and <j> in list
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}

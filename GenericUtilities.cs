using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DailyLoot
{
    public static class GenericUtilities
    {
        public static string RGBToHex(this Color color) => string.Format("{0:X2}{1:X2}{2:X2}", (int)color.R, (int)color.G, (int)color.B);

        /// <summary>
        /// Utilizes [c/hexcode:words] to color the input from some string.
        /// </summary>
        /// <param name="output">The text that should be colored.</param>
        /// <param name="col">The color you wish the text to be colored with.</param>
        /// <returns><paramref name="output"/> with a text color of <paramref name="col"/></returns>
        public static string ColorString(this string output, Color col) => $"[c/{col.RGBToHex()}:{output}]";

        /// <summary>
        /// Lazy shorthand to quickly lerp two colors.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="target"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Color LerpTo(this Color c, Color target, float t) => Color.Lerp(c, target, t);
    }
}

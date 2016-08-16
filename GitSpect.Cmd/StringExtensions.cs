using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public static class StringExtensions
    {
        /// <summary>
        /// Assumes a string is separated by spaces. This method will capitilize each word.
        /// </summary>
        /// <param name="me">The string to capitilize</param>
        /// <returns>The String To Capitilize</returns>
        public static string ToTitleCase(this string me)
        {
            string titled = me;

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            titled = textInfo.ToTitleCase(titled);

            return titled;
        }
    }
}

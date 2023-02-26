// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System.Text.RegularExpressions;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Helper class for providing common cross-platform string manipulations.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal static class StringExtensions
    {
        private const string HtmlLineBreakRegex = @"<br ?/?>";
        private const string HtmlStripperRegex = @"<(.|\n)*?>";

        /// <summary>
        /// Strips-out html code from text.
        /// </summary>
        /// <param name="htmlText">Text that may contain html code.</param>
        /// <returns>Plain text after html code is removed.</returns>
        internal static string ToPlainText(this string htmlText)
        {
            // Replace HTML line break tags.
            htmlText = Regex.Replace(htmlText, HtmlLineBreakRegex, System.Environment.NewLine);
            htmlText = htmlText.Replace("</p>", System.Environment.NewLine + System.Environment.NewLine);
            htmlText = htmlText.Replace("&nbsp;", " ");
            // Remove the rest of HTML tags.
            htmlText = Regex.Replace(htmlText, HtmlStripperRegex, string.Empty);

            return htmlText.Trim();
        }
    }
}
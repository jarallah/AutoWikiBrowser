/*

(C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;

namespace WikiFunctions
{
    /// <summary>
    /// Provides various tools as static methods, such as getting the html of a page
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Calculates the namespace index.
        /// </summary>
        public static int CalculateNS(string ArticleTitle)
        {
            if (!ArticleTitle.Contains(":"))
                return 0;

            foreach (KeyValuePair<int, string> k in Variables.Namespaces)
            {
                if (ArticleTitle.StartsWith(k.Value))
                    return k.Key;
            }

            foreach (KeyValuePair<int, string> k in Variables.enLangNamespaces)
            {
                if (ArticleTitle.StartsWith(k.Value))
                    return k.Key;
            }

            return 0;
        }

        /// <summary>
        /// Returns the version of WikiFunctions
        /// </summary>
        public static System.Version Version
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; } }

        /// <summary>
        /// Returns a String including the version of WikiFunctions
        /// </summary>
        public static string VersionString
        { get { return Version.ToString(); } }

        /// <summary>
        /// Displays the WikiFunctions About box
        /// </summary>
        public static void About()
        {
            try { new Controls.AboutBox().Show(); }
            catch { }
        }

        /// <summary>
        /// Tests title to make sure it is main space
        /// </summary>
        /// <param name="ArticleTitle">The title.</param>
        public static bool IsMainSpace(string ArticleTitle)
        {
            return (CalculateNS(ArticleTitle) == 0);
        }

        /// <summary>
        /// Tests title to make sure it is an editable namespace
        /// </summary>
        /// <param name="ArticleTitle">The title.</param>
        public static bool IsEditableSpace(string ArticleTitle)
        {
            if (ArticleTitle.StartsWith("Commons:"))
                return false;

            int i = CalculateNS(ArticleTitle);
            if (i < 0 || i > 99 || i == 7 || i == 8)
                return false;

            return true;
        }
        
        /// <summary>
        /// Tests article to see if it is a redirect
        /// </summary>
        /// <param name="Text">The title.</param>
        public static bool IsRedirect(string Text)
        {
            return WikiRegexes.RedirectRegex.IsMatch(Text);
        }

        /// <summary>
        /// Returns true if given project belongs to Wikimedia
        /// </summary>
        public static bool IsWikimediaProject(ProjectEnum p)
        {
            if (p == ProjectEnum.commons || p == ProjectEnum.meta || p == ProjectEnum.species
                || p == ProjectEnum.wikibooks || p == ProjectEnum.wikinews || p == ProjectEnum.wikipedia
                || p == ProjectEnum.wikiquote || p == ProjectEnum.wikisource || p == ProjectEnum.wikiversity
                || p == ProjectEnum.wiktionary)
                return true;
            else return false;
        }

        /// <summary>
        /// Gets the target of the redirect
        /// </summary>
        /// <param name="Text">The title.</param>
        public static string RedirectTarget(string Text)
        {
            Match m = WikiRegexes.RedirectRegex.Match(Text);
            return m.Groups[1].Value;
        }

        static string[] InvalidChars = new string[] { "[", "]", "{", "}", "|", "<",">", "#" };

        /// <summary>
        /// Tests article title to see if it is valid
        /// </summary>
        /// <param name="Text">The title.</param>
        public static bool IsValidTitle(string ArticleTitle)
        {
            foreach(string s in InvalidChars)
                if(ArticleTitle.Contains(s))
                    return false;

            return true;
        }

        /// <summary>
        /// Removes Invalid Characters from an Article Title
        /// </summary>
        /// <param name="Title">Article Title</param>
        /// <returns>Article Title with no invalid characters</returns>
        public static string RemoveInvalidChars(string Title)
        {
            foreach (string character in InvalidChars)
            {
                if (Regex.Match(Title, Regex.Escape(character)).Success)
                    Title = Title.Replace(character, "");
            }
            return Title;
        }

        /// <summary>
        /// Tests title to make sure it is either main, image, category or template namespace.
        /// </summary>
        /// <param name="ArticleTitle">The title.</param>
        public static bool IsImportantNamespace(string ArticleTitle)
        {
            int i = CalculateNS(ArticleTitle);
            return (i == 0 || i == 6 || i == 10 || i == 14);
        }

        /// <summary>
        /// Tests title to make sure it is a talk page.
        /// </summary>
        /// <param name="ArticleTitle">The title.</param>
        public static bool IsTalkPage(string ArticleTitle)
        {
            return (CalculateNS(ArticleTitle) % 2 == 1);
        }

        /// <summary>
        /// Tests title to make sure it is a talk page.
        /// </summary>
        /// <param name="Key">The namespace key</param>
        public static bool IsTalkPage(int Key)
        {
            return (Key % 2 == 1);
        }

        /// <summary>
        /// Returns Category key from article name e.g. "David Smith" returns "Smith, David".
        /// special case: "John Doe, Jr." turns into "Doe, Jonn Jr."
        /// </summary>
        public static string MakeHumanCatKey(string Name)
        {
            Name = RemoveNamespaceString(Regex.Replace(Name, @"\(.*?\)$", "").Trim()).Trim();
            string OrigName = Name;
            if (!Name.Contains(" ") || Variables.LangCode == LangCodeEnum.uk) return OrigName;
            // ukwiki uses "Lastname Firstname Patronymic" convention, nothing more is needed

            string Suffix = "";
            int pos = Name.IndexOf(',');

            // ruwiki has "Lastname, Firstname Patronymic" convention
            if(pos >= 0 && Variables.LangCode != LangCodeEnum.ru)
            {
                Suffix = Name.Substring(pos + 1).Trim();
                Name = Name.Substring(0, pos);
            }

            int intLast = Name.LastIndexOf(" ") + 1;
            string LastName = Name.Substring(intLast);
            Name = Name.Remove(intLast).Trim();
            if (IsRomanNumber(LastName))
            {
                if (Name.Contains(" "))
                {
                    Suffix += " " + LastName;
                    intLast = Name.LastIndexOf(" ") + 1;
                    LastName = Name.Substring(intLast);
                    Name = Name.Remove(intLast).Trim();
                }
                else { //if (Suffix != "") {
                    // We have something like "Peter" "II" "King of Spain" (first/last/suffix), so return what we started with
                    // OR We have "Fred" "II", we don't want to return "II, Fred" so we must return "Fred II"
                    return OrigName;
                }
            }

            Name = (LastName + ", " + Name + ", " + Suffix).Trim(" ,".ToCharArray());

            return RemoveDiacritics(Name);
        }

        /// <summary>
        /// Returns a string with the namespace removed
        /// </summary>
        /// <returns></returns>
        public static string RemoveNamespaceString(string Name)
        {
            foreach (KeyValuePair<int, string> Namespace in Variables.Namespaces)
            {
                if (Name.Contains(Namespace.Value))
                    Name = Name.Replace(Namespace.Value, "");
            }
            return Name;
        }

        /// <summary>
        /// Returns a string just including the namespace of the article
        /// </summary>
        /// <returns></returns>
        public static string GetNamespaceString(string Name)
        {
            string ret = "";
            foreach (KeyValuePair<int, string> Namespace in Variables.Namespaces)
            {
                if (Name.Contains(Namespace.Value))
                {
                    ret = Namespace.Value;
                    break;
                }
            }
            return ret.Replace(":", "");
        }

        /// <summary>
        /// checks if given string represents a small Roman number
        /// </summary>
        public static bool IsRomanNumber(string s)
        {
            if (s.Length > 5) return false;
            foreach (char c in s)
            {
                if (c != 'I' && c != 'V' && c != 'X') return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the HTML from the given web address.
        /// </summary>
        /// <param name="URL">The URL of the webpage.</param>
        /// <returns>The HTML.</returns>
        public static string GetHTML(string URL)
        {
            return GetHTML(URL, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the HTML from the given web address.
        /// </summary>
        /// <param name="URL">The URL of the webpage.</param>
        /// <param name="Enc">The encoding to use.</param>
        /// <returns>The HTML.</returns>
        public static string GetHTML(string URL, Encoding Enc)
        {
            string text = "";
            try
            {
                HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(URL);

                rq.Proxy.Credentials = CredentialCache.DefaultCredentials;
                rq.UserAgent = "WikiFunctions " + Tools.VersionString;

                HttpWebResponse response = (HttpWebResponse)rq.GetResponse();

                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream, Enc);

                text = sr.ReadToEnd();

                sr.Close();
                stream.Close();
                response.Close();

                return text;
            }
            catch { throw; }
        }

        /// <summary>
        /// Gets the wiki text of the given article.
        /// </summary>
        /// <param name="ArticleTitle">The name of the article.</param>
        /// <returns>The wiki text of the article.</returns>
        public static string GetArticleText(string ArticleTitle)
        {
            if (!IsValidTitle(ArticleTitle))
                return "";

            string text = "";
            try
            {
                text = GetHTML(Variables.GetPlainTextURL(ArticleTitle), Encoding.UTF8);
            }
            catch
            {
                throw new Exception("There was a problem loading " + Variables.URLLong + "index.php?title=" + ArticleTitle + ", please make sure the page exists");
            }

            return text;
        }

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        /// <summary>
        /// Flashes the given form in the taskbar
        /// </summary>
        public static void FlashWindow(System.Windows.Forms.Form window)
        { FlashWindow(window.Handle, true); }

        /// <summary>
        /// Returns a regex case insensitive version of a string for the first letter only e.g. "Category" returns "[Cc]ategory"
        /// </summary>
        public static string CaseInsensitive(string input)
        {
            if (input != "" && char.IsLetter(input[0]))
            {
                input = input.Trim();
                return "[" + char.ToUpper(input[0]) + char.ToLower(input[0]) + "]" + input.Remove(0, 1);
            }
            else
                return input;
        }

        /// <summary>
        /// Returns a regex case insensitive version of an entire string e.g. "Category" returns "[Cc][Aa][Tt][Ee][Gg][Oo][Rr][Yy]"
        /// </summary>
        public static string AllCaseInsensitive(string input)
        {
            if (input != "")
            {
                input = input.Trim();
                string result = "";
                for (int i = 0; i <= input.Length - 1; i++)
                {
                    if (char.IsLetter(input[i]))
                        result = result + "[" + char.ToUpper(input[i]) + char.ToLower(input[i]) + "]";
                    else result = result + input[i];
                }
                return result;
            }
            else
                return input;
        }

        /// <summary>
        /// Applies the key words "%%title%%" etc.
        /// </summary>
        public static string ApplyKeyWords(string Title, string Text)
        {
            Text = Text.Replace("%%title%%", Title);
            Text = Text.Replace("%%key%%", Tools.MakeHumanCatKey(Title));

            Text = Text.Replace("%%titlename%%", Tools.RemoveNamespaceString(Title));
            Text = Text.Replace("%%namespace%%", Tools.GetNamespaceString(Title));

            return Text;
        }

        /// <summary>
        /// Returns uppercase version of the string
        /// </summary>
        public static string TurnFirstToUpper(string input)
        {   //other projects don't always start with capital
            if (Variables.Project == ProjectEnum.wiktionary)
                return input;

            if (input.Length == 0)
                return "";

            input = char.ToUpper(input[0]) + input.Remove(0, 1);

            return input;
        }        

        /// <summary>
        /// Returns lowercase version of the string
        /// </summary>
        public static string TurnFirstToLower(string input)
        {
            //turns first character to lowercase
            if (input.Length == 0)
                return "";

            input = char.ToLower(input[0]) + input.Remove(0, 1);

            return input;
        }        

        static readonly Regex RegexWordCountTable = new Regex("\\{\\|.*?\\|\\}", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Returns word count of the string
        /// </summary>
        public static int WordCount(string Text)
        {
            Text = RegexWordCountTable.Replace(Text, "");
            Text = WikiRegexes.TemplateMultiLine.Replace(Text, "");

            return WikiRegexes.RegexWordCount.Matches(Text).Count;
        }

        /// <summary>
        /// Returns the number of [[links]] in the string
        /// </summary>
        public static int LinkCount(string Text)
        { return WikiRegexes.WikiLinksOnly.Matches(Text).Count; }

        /// <summary>
        /// Removes underscores and wiki syntax from links
        /// </summary>
        public static string RemoveSyntax(string Text)
        {
            Text = Text.Replace("_", " ").Trim();
            Text = Text.Trim('[', ']');

            return Text;
        }

        #region boring chars
        static readonly KeyValuePair<string, string>[] Diacritics = new KeyValuePair<string, string>[]
            {
                //Latin
                new KeyValuePair<string, string>("Á", "A"),
                new KeyValuePair<string, string>("á", "a"),
                new KeyValuePair<string, string>("Ć", "C"),
                new KeyValuePair<string, string>("ć", "c"),
                new KeyValuePair<string, string>("É", "E"),
                new KeyValuePair<string, string>("é", "e"),
                new KeyValuePair<string, string>("Í", "I"),
                new KeyValuePair<string, string>("í", "i"),
                new KeyValuePair<string, string>("Ĺ", "L"),
                new KeyValuePair<string, string>("ĺ", "l"),
                new KeyValuePair<string, string>("Ń", "N"),
                new KeyValuePair<string, string>("ń", "n"),
                new KeyValuePair<string, string>("Ó", "O"),
                new KeyValuePair<string, string>("ó", "o"),
                new KeyValuePair<string, string>("Ŕ", "R"),
                new KeyValuePair<string, string>("ŕ", "r"),
                new KeyValuePair<string, string>("Ś", "S"),
                new KeyValuePair<string, string>("ś", "s"),
                new KeyValuePair<string, string>("Ú", "U"),
                new KeyValuePair<string, string>("ú", "u"),
                new KeyValuePair<string, string>("Ý", "Y"),
                new KeyValuePair<string, string>("ý", "y"),
                new KeyValuePair<string, string>("Ź", "Z"),
                new KeyValuePair<string, string>("ź", "z"),
                new KeyValuePair<string, string>("À", "A"),
                new KeyValuePair<string, string>("à", "a"),
                new KeyValuePair<string, string>("È", "E"),
                new KeyValuePair<string, string>("è", "e"),
                new KeyValuePair<string, string>("Ì", "I"),
                new KeyValuePair<string, string>("ì", "i"),
                new KeyValuePair<string, string>("Ò", "O"),
                new KeyValuePair<string, string>("ò", "o"),
                new KeyValuePair<string, string>("Ù", "U"),
                new KeyValuePair<string, string>("ù", "u"),
                new KeyValuePair<string, string>("Â", "A"),
                new KeyValuePair<string, string>("â", "a"),
                new KeyValuePair<string, string>("Ĉ", "C"),
                new KeyValuePair<string, string>("ĉ", "c"),
                new KeyValuePair<string, string>("Ê", "E"),
                new KeyValuePair<string, string>("ê", "e"),
                new KeyValuePair<string, string>("Ĝ", "G"),
                new KeyValuePair<string, string>("ĝ", "g"),
                new KeyValuePair<string, string>("Ĥ", "H"),
                new KeyValuePair<string, string>("ĥ", "h"),
                new KeyValuePair<string, string>("Î", "I"),
                new KeyValuePair<string, string>("î", "i"),
                new KeyValuePair<string, string>("Ĵ", "J"),
                new KeyValuePair<string, string>("ĵ", "j"),
                new KeyValuePair<string, string>("Ô", "O"),
                new KeyValuePair<string, string>("ô", "o"),
                new KeyValuePair<string, string>("Ŝ", "S"),
                new KeyValuePair<string, string>("ŝ", "s"),
                new KeyValuePair<string, string>("Û", "U"),
                new KeyValuePair<string, string>("û", "u"),
                new KeyValuePair<string, string>("Ŵ", "W"),
                new KeyValuePair<string, string>("ŵ", "w"),
                new KeyValuePair<string, string>("Ŷ", "Y"),
                new KeyValuePair<string, string>("ŷ", "y"),
                new KeyValuePair<string, string>("Ä", "A"),
                new KeyValuePair<string, string>("ä", "a"),
                new KeyValuePair<string, string>("Ë", "E"),
                new KeyValuePair<string, string>("ë", "e"),
                new KeyValuePair<string, string>("Ï", "I"),
                new KeyValuePair<string, string>("ï", "i"),
                new KeyValuePair<string, string>("Ö", "O"),
                new KeyValuePair<string, string>("ö", "o"),
                new KeyValuePair<string, string>("Ü", "U"),
                new KeyValuePair<string, string>("ü", "u"),
                new KeyValuePair<string, string>("Ÿ", "Y"),
                new KeyValuePair<string, string>("ÿ", "y"),
                new KeyValuePair<string, string>("ß", "ss"),
                new KeyValuePair<string, string>("Ã", "A"),
                new KeyValuePair<string, string>("ã", "a"),
                new KeyValuePair<string, string>("Ẽ", "E"),
                new KeyValuePair<string, string>("ẽ", "e"),
                new KeyValuePair<string, string>("Ĩ", "I"),
                new KeyValuePair<string, string>("ĩ", "i"),
                new KeyValuePair<string, string>("Ñ", "N"),
                new KeyValuePair<string, string>("ñ", "n"),
                new KeyValuePair<string, string>("Õ", "O"),
                new KeyValuePair<string, string>("õ", "o"),
                new KeyValuePair<string, string>("Ũ", "U"),
                new KeyValuePair<string, string>("ũ", "u"),
                new KeyValuePair<string, string>("Ỹ", "Y"),
                new KeyValuePair<string, string>("ỹ", "y"),
                new KeyValuePair<string, string>("Ç", "C"),
                new KeyValuePair<string, string>("ç", "c"),
                new KeyValuePair<string, string>("Ģ", "G"),
                new KeyValuePair<string, string>("ģ", "g"),
                new KeyValuePair<string, string>("Ķ", "K"),
                new KeyValuePair<string, string>("ķ", "k"),
                new KeyValuePair<string, string>("Ļ", "L"),
                new KeyValuePair<string, string>("ļ", "l"),
                new KeyValuePair<string, string>("Ņ", "N"),
                new KeyValuePair<string, string>("ņ", "n"),
                new KeyValuePair<string, string>("Ŗ", "R"),
                new KeyValuePair<string, string>("ŗ", "r"),
                new KeyValuePair<string, string>("Ş", "S"),
                new KeyValuePair<string, string>("ş", "s"),
                new KeyValuePair<string, string>("Ţ", "T"),
                new KeyValuePair<string, string>("ţ", "t"),
                new KeyValuePair<string, string>("Đ", "D"),
                new KeyValuePair<string, string>("đ", "d"),
                new KeyValuePair<string, string>("Ů", "U"),
                new KeyValuePair<string, string>("ů", "u"),
                new KeyValuePair<string, string>("Ǎ", "A"),
                new KeyValuePair<string, string>("ǎ", "a"),
                new KeyValuePair<string, string>("Č", "C"),
                new KeyValuePair<string, string>("č", "c"),
                new KeyValuePair<string, string>("Ď", "D"),
                new KeyValuePair<string, string>("ď", "d"),
                new KeyValuePair<string, string>("Ě", "E"),
                new KeyValuePair<string, string>("ě", "e"),
                new KeyValuePair<string, string>("Ǐ", "I"),
                new KeyValuePair<string, string>("ǐ", "I"),
                new KeyValuePair<string, string>("Ľ", "L"),
                new KeyValuePair<string, string>("ľ", "l"),
                new KeyValuePair<string, string>("Ň", "N"),
                new KeyValuePair<string, string>("ň", "N"),
                new KeyValuePair<string, string>("Ǒ", "O"),
                new KeyValuePair<string, string>("ǒ", "o"),
                new KeyValuePair<string, string>("Ř", "R"),
                new KeyValuePair<string, string>("ř", "r"),
                new KeyValuePair<string, string>("Š", "S"),
                new KeyValuePair<string, string>("š", "s"),
                new KeyValuePair<string, string>("Ť", "T"),
                new KeyValuePair<string, string>("ť", "t"),
                new KeyValuePair<string, string>("Ǔ", "U"),
                new KeyValuePair<string, string>("ǔ", "u"),
                new KeyValuePair<string, string>("Ž", "Z"),
                new KeyValuePair<string, string>("ž", "z"),
                new KeyValuePair<string, string>("Ā", "A"),
                new KeyValuePair<string, string>("ā", "a"),
                new KeyValuePair<string, string>("Ē", "E"),
                new KeyValuePair<string, string>("ē", "e"),
                new KeyValuePair<string, string>("Ī", "I"),
                new KeyValuePair<string, string>("ī", "i"),
                new KeyValuePair<string, string>("Ō", "O"),
                new KeyValuePair<string, string>("ō", "o"),
                new KeyValuePair<string, string>("Ū", "U"),
                new KeyValuePair<string, string>("ū", "u"),
                new KeyValuePair<string, string>("Ȳ", "O"),
                new KeyValuePair<string, string>("ȳ", "o"),
                new KeyValuePair<string, string>("Ǣ", "E"),
                new KeyValuePair<string, string>("ǣ", "e"),
                new KeyValuePair<string, string>("ǖ", "u"),
                new KeyValuePair<string, string>("ǘ", "u"),
                new KeyValuePair<string, string>("ǚ", "u"),
                new KeyValuePair<string, string>("ǜ", "u"),
                new KeyValuePair<string, string>("Ă", "A"),
                new KeyValuePair<string, string>("ă", "a"),
                new KeyValuePair<string, string>("Ĕ", "E"),
                new KeyValuePair<string, string>("ĕ", "e"),
                new KeyValuePair<string, string>("Ğ", "G"),
                new KeyValuePair<string, string>("ğ", "g"),
                new KeyValuePair<string, string>("Ĭ", "I"),
                new KeyValuePair<string, string>("ĭ", "i"),
                new KeyValuePair<string, string>("Ŏ", "O"),
                new KeyValuePair<string, string>("ŏ", "o"),
                new KeyValuePair<string, string>("Ŭ", "U"),
                new KeyValuePair<string, string>("ŭ", "u"),
                new KeyValuePair<string, string>("Ċ", "C"),
                new KeyValuePair<string, string>("ċ", "c"),
                new KeyValuePair<string, string>("Ė", "E"),
                new KeyValuePair<string, string>("ė", "e"),
                new KeyValuePair<string, string>("Ġ", "G"),
                new KeyValuePair<string, string>("ġ", "g"),
                new KeyValuePair<string, string>("İ", "I"),
                new KeyValuePair<string, string>("ı", "i"),
                new KeyValuePair<string, string>("Ż", "Z"),
                new KeyValuePair<string, string>("ż", "z"),
                new KeyValuePair<string, string>("Ą", "A"),
                new KeyValuePair<string, string>("ą", "a"),
                new KeyValuePair<string, string>("Ę", "E"),
                new KeyValuePair<string, string>("ę", "e"),
                new KeyValuePair<string, string>("Į", "I"),
                new KeyValuePair<string, string>("į", "i"),
                new KeyValuePair<string, string>("Ǫ", "O"),
                new KeyValuePair<string, string>("ǫ", "o"),
                new KeyValuePair<string, string>("Ų", "U"),
                new KeyValuePair<string, string>("ų", "u"),
                new KeyValuePair<string, string>("Ḍ", "D"),
                new KeyValuePair<string, string>("ḍ", "d"),
                new KeyValuePair<string, string>("Ḥ", "H"),
                new KeyValuePair<string, string>("ḥ", "h"),
                new KeyValuePair<string, string>("Ḷ", "L"),
                new KeyValuePair<string, string>("ḷ", "l"),
                new KeyValuePair<string, string>("Ḹ", "L"),
                new KeyValuePair<string, string>("ḹ", "l"),
                new KeyValuePair<string, string>("Ṃ", "M"),
                new KeyValuePair<string, string>("ṃ", "m"),
                new KeyValuePair<string, string>("Ṇ", "N"),
                new KeyValuePair<string, string>("ṇ", "n"),
                new KeyValuePair<string, string>("Ṛ", "R"),
                new KeyValuePair<string, string>("ṛ", "r"),
                new KeyValuePair<string, string>("Ṝ", "R"),
                new KeyValuePair<string, string>("ṝ", "r"),
                new KeyValuePair<string, string>("Ṣ", "S"),
                new KeyValuePair<string, string>("ṣ", "s"),
                new KeyValuePair<string, string>("Ṭ", "T"),
                new KeyValuePair<string, string>("ṭ", "t"),
                new KeyValuePair<string, string>("Ł", "L"),
                new KeyValuePair<string, string>("ł", "l"),
                new KeyValuePair<string, string>("Ő", "O"),
                new KeyValuePair<string, string>("ő", "o"),
                new KeyValuePair<string, string>("Ű", "U"),
                new KeyValuePair<string, string>("ű", "u"),
                new KeyValuePair<string, string>("Ŀ", "L"),
                new KeyValuePair<string, string>("ŀ", "l"),
                new KeyValuePair<string, string>("Ħ", "H"),
                new KeyValuePair<string, string>("ħ", "h"),
                new KeyValuePair<string, string>("Ð", "D"),
                new KeyValuePair<string, string>("ð", "d"),
                new KeyValuePair<string, string>("Þ", "TH"),
                new KeyValuePair<string, string>("þ", "th"),
                new KeyValuePair<string, string>("Œ", "O"),
                new KeyValuePair<string, string>("œ", "o"),
                new KeyValuePair<string, string>("Æ", "E"),
                new KeyValuePair<string, string>("æ", "e"),
                new KeyValuePair<string, string>("Ø", "O"),
                new KeyValuePair<string, string>("ø", "o"),
                new KeyValuePair<string, string>("Å", "A"),
                new KeyValuePair<string, string>("å", "a"),
                new KeyValuePair<string, string>("Ə", "E"),
                new KeyValuePair<string, string>("ə", "e"),

                //Russian
                new KeyValuePair<string, string>("Ё", "Е"),
                new KeyValuePair<string, string>("ё", "е"),

            };
        #endregion

        /// <summary>
        /// substitutes characters with diacritics with their simple equivalents
        /// </summary>
        public static string RemoveDiacritics(string s)
        {
            foreach (KeyValuePair<string, string> p in Diacritics)
            {
                s = s.Replace(p.Key, p.Value);
            }
            return s;
        }

        /// <summary>
        /// Writes a message to the given file in the directory of the application.
        /// </summary>
        /// <param name="Message">The message to write.</param>
        /// <param name="File">The name of the file, e.g. "Log.txt".</param>
        public static void WriteTextFile(string Message, string File, bool append)
        {
            try
            {
                StreamWriter writer = new StreamWriter(Application.StartupPath + "\\" + File, append, Encoding.UTF8);
                writer.Write(Message);
                writer.Close();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Turns an HTML list into a wiki style list
        /// </summary>
        public static string HTMLListToWiki(string text, string bullet)
        {//converts wiki/html/plain text list to wiki formatted list, bullet should be * or #
            text = text.Replace("\r\n\r\n", "\r\n");
            text = Regex.Replace(text, "<br ?/?>", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "</?(ol|ul|li)>", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "^</?(ol|ul|li)>\r\n", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "^(<li>|\\:|\\*|#|\\(? ?[0-9]{1,3} ?\\)|[0-9]{1,3}\\.?)", "", RegexOptions.Multiline);
            text = Regex.Replace(text, "^", bullet, RegexOptions.Multiline);

            return text;
        }

        /// <summary>
        /// Beeps
        /// </summary>
        public static void Beep1()
        {//public domain sounds from http://www.partnersinrhyme.com/soundfx/PDsoundfx/beep.shtml
            System.Media.SoundPlayer sound = new System.Media.SoundPlayer();
            sound.Stream = Properties.Resources.beep1;
            sound.Play();
        }

        /// <summary>
        /// Beeps
        /// </summary>
        public static void Beep2()
        {
            System.Media.SoundPlayer sound = new System.Media.SoundPlayer();
            sound.Stream = Properties.Resources.beep2;
            sound.Play();
        }

        static bool bWriteDebug = false;
        /// <summary>
        /// Gets or sets value whether debug is enabled
        /// </summary>
        public static bool WriteDebugEnabled
        {
            get { return bWriteDebug; }
            set { bWriteDebug = value; }
        }

        /// <summary>
        /// Writes debug log message
        /// </summary>
        public static void WriteDebug(string Object, string Text)
        {
            if (!bWriteDebug)
                return;

            try
            {
                WriteTextFile(string.Format(
@"Object: {0}
Time: {1}
Message: {2}

", Object, DateTime.Now.ToLongTimeString(), Text), "Log.txt", true);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        /// <summary>
        /// Checks whether given string is a valid IP address
        /// </summary>
        public static bool IsIP(string s)
        {
            IPAddress dummy;
            return IPAddress.TryParse(s, out dummy);
        }

        /// <summary>
        /// returns content of a given string that lies between two other strings
        /// </summary>
        public static string StringBetween(string source, string start, string end)
        {
            try
            {
                return source.Substring(source.IndexOf(start), source.IndexOf(end) - source.IndexOf(start));
            }
            catch
            { return ""; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void OpenArticleInBrowser(string title)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.NonPrettifiedURL(title));
            }
            catch { }
        }

        /// <summary>
        /// Forces the loading of a page in En.Wiki
        /// Used for 'static' links to the english wikipedia
        /// </summary>
        public static void OpenENArticleInBrowser(string title, bool userspace)
        {
            try
            {
                if (userspace)
                    System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/User:" + WikiEncode(title));
                else
                    System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/" + WikiEncode(title));
            }
            catch
            { }
        }

        public static string GetENLinkWithSimpleSkinAndLocalLanguage(string Article)
        { return "http://en.wikipedia.org/w/index.php?title=" + Article + "&useskin=simple&uselang=" +
            WikiFunctions.Variables.LangCode.ToString(); }

        /// <summary>
        /// Opens the specified articles history in the browser
        /// </summary>
        public static void OpenArticleHistoryInBrowser(string title)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.GetArticleHistoryURL(title));
            }
            catch { }
        }

        /// <summary>
        /// Opens the specified users talk page in the browser
        /// </summary>
        public static void OpenUserTalkInBrowser(string username)
        {
            try
            {
                System.Diagnostics.Process.Start(username);
            }
            catch { }
        }

        /// <summary>
        /// Opens the current users talk page in the browser
        /// </summary>
        public static void OpenUserTalkInBrowser()
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.GetUserTalkURL());
            }
            catch { }
        }

        /// <summary>
        /// Opens the specified articles edit page
        /// </summary>
        public static void EditArticleInBrowser(string title)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.GetEditURL(title));
            }
            catch { }
        }

        /// <summary>
        /// Add the specified article to the current users watchlist
        /// </summary>
        public static void WatchArticleInBrowser(string title)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.GetAddToWatchlistURL(title));
            }
            catch { }
        }

        /// <summary>
        /// Removes the specified article from the current users watchlist
        /// </summary>
        public static void UnwatchArticleInBrowser(string title)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.GetRemoveFromWatchlistURL(title));
            }
            catch { }
        }

        /// <summary>
        /// Replaces spaces with underscores for article title names
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string WikiEncode(string title)
        {
            return HttpUtility.UrlEncode(title.Replace(' ', '_'));
        }

        static readonly Regex ExpandTemplatesRegex = new Regex("<textarea id=\"output\".*>([^<]*)</\\s*textarea>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ArticleText">The text of the article</param>
        /// <param name="ArticleTitle">The title of the artlce</param>
        /// <param name="Regexes"></param>
        /// <param name="includeComment"></param>
        /// <returns></returns>
        public static string ExpandTemplate(string ArticleText, string ArticleTitle, Dictionary<Regex, string> Regexes, bool includeComment)
        {
            WikiFunctions.Parse.Parsers parsers = new WikiFunctions.Parse.Parsers();

            foreach (KeyValuePair<Regex, string> p in Regexes)
            {
                MatchCollection uses = p.Key.Matches(ArticleText);
                foreach (Match m in uses)
                {
                    string Call = m.Value;

                    string expandUri = Variables.URLLong + "index.php?title=Special:ExpandTemplates&contexttitle=" + Tools.WikiEncode(ArticleTitle) + "&input=" + HttpUtility.UrlEncode(Call) + "&removecomments=1";
                    string result;

                    try
                    {
                        string respStr = Tools.GetHTML(expandUri);
                        Match m1 = ExpandTemplatesRegex.Match(respStr);
                        if (!m.Success) continue;
                        result = HttpUtility.HtmlDecode(m1.Groups[1].Value);
                    }
                    catch
                    {
                        continue;
                    }

                    bool SkipArticle;
                    result = parsers.Unicodify(result, out SkipArticle);
                    if (!includeComment)
                        ArticleText = ArticleText.Replace(Call, result);
                    else
                        ArticleText = ArticleText.Replace(Call, result + "<!-- " + Call + " -->");
                }
            }

            return ArticleText;
        }

        #region Copy
        /// <summary>
        /// Copy selected items from a listbox
        /// </summary>
        /// <param name="box">The list box to copy from</param>
        public static void Copy(ListBox box)
        {
            try
            {
                string ClipboardData = "";
                for (int i = 0; i < box.SelectedItems.Count; i++)
                {
                    ClipboardData += "\r\n" + box.SelectedItems[i];
                }
                ClipboardData = ClipboardData.Substring(2);
                Clipboard.SetDataObject(ClipboardData, true);
            }
            catch { }
        }

        /// <summary>
        /// Copy selected items from a listview
        /// </summary>
        /// <param name="view">The list view to copy from</param>
        public static void Copy(ListView view)
        {
            try
            {
                string ClipboardData = "";
                foreach (ListViewItem a in view.SelectedItems)
                {
                    string text = a.Text;
                    if (ClipboardData != "") ClipboardData += "\r\n";
                    ClipboardData += text;
                }
                Clipboard.SetDataObject(ClipboardData, true);
            }
            catch { }
        }
        #endregion
    }    
}

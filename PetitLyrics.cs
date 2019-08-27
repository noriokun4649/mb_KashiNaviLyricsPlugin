using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MusicBeePlugin
{
    public class PetitLyrics
    {
        private static readonly Encoding Encoding = Encoding.GetEncoding("shift_jis");

        public static string FetchLyrics(string trackTitle, string artist, string album)
        {
            var client = new WebClientEx() { Encoding = Encoding };

            // 検索結果ページ
            string lyricId;
            lyricId = GetId(client, HttpUtility.UrlEncode(trackTitle, Encoding), HttpUtility.UrlEncode(artist, Encoding) ?? "","");
            string id = lyricId;
            if (id == "")
            {
                return null;
            }
            string lyricPage = client.DownloadString($"https://kashinavi.com/song_view.html?{id}");
            lyricPage = lyricPage.Replace("<br>", "\n");
            int lyric_startIndex = lyricPage.IndexOf("align=left class=\"noprint\"");
            int lyric_length = lyricPage.Substring(lyric_startIndex).IndexOf("下記タグを貼り付けてもリンクできます。");
            var lyric_ms = Regex.Match(lyricPage.Substring(lyric_startIndex, lyric_length), @"unselectable=""on;""\>(?<lyric>.*)\</div\>\n</div\>", RegexOptions.Singleline);
            string lines = lyric_ms.Groups["lyric"].Value.Trim();
            return lines;
        }


        private static string GetId(WebClientEx client, string titele, string artist, string album)
        {
            string mode = album != "" ? "info" : "kyoku";
            string search = album != "" ? album : titele;
            string lyricId = "";
            string searchPage = client.DownloadString($"https://kashinavi.com/search.php?r={mode}&search={search}&m=bubun&start=1");
            if (!searchPage.Contains("該当データがありませんでした。"))
            {
                int startIndex = searchPage.IndexOf("<td bgcolor=\"#EE9900\" style=\"width:60px\"></td>");
                int length = searchPage.Substring(startIndex).IndexOf("<tr><td colspan=5 bgcolor=\"#FFDD55\" align=center><font color=\"#aa6600\">");

                var ms = Regex.Matches(searchPage.Substring(startIndex, length), @"href=""song_view.html\?(?<lyricId>\d+)""\>(?<title>\w+).+kashu=.+&start=1""\>(?<artist>\w+)");
                foreach (Match m in ms)
                {
                    string art = m.Groups["artist"].Value.Trim();
                    string title = m.Groups["title"].Value.Trim();

                    if ((HttpUtility.UrlDecode(artist, Encoding) == art) && (HttpUtility.UrlDecode(titele, Encoding) == title))
                    {
                        lyricId = m.Groups["lyricId"].Value.Trim();
                    }
                    else if (HttpUtility.UrlDecode(titele, Encoding) == title)
                    {
                        lyricId = m.Groups["lyricId"].Value.Trim();

                    }
                }
            }
            return lyricId;
        }

    }
}
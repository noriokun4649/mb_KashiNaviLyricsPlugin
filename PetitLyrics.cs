﻿using System;
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
            string lyricId;
            lyricId = GetId(client, HttpUtility.UrlEncode(trackTitle, Encoding), HttpUtility.UrlEncode(artist, Encoding) ?? "", "");
            //歌詞ナビではinfoにいろいろな情報が入る。アルバム情報やアニメ主題歌、ドラマ主題歌などの情報等
            //歌詞ナビ側のアルバム情報がまちまちなのでアルバムでの検索は""にして無効化中。設定から切り替え出来るように検討ちゅ　↑
            var id = lyricId;
            if (id == "")//IDが余白の場合は歌詞が見つからない状態
            {
                return null;//MusicBeeのプラグイン仕様通りにnullを返す。
            }
            var lyricPage = client.DownloadString($"https://kashinavi.com/song_view.html?{id}");
            lyricPage = lyricPage.Replace("<br>", "\n").Replace("<p>", "\n").Replace("</p>", "\n");//HTMLの改行コードをC#の改行コードに置き換え。
            var lyric_ms = Regex.Match(lyricPage, @"\<div class=""kashi"" oncopy=""return false;"" unselectable=""on;""\>(?<lyric>.*)\</div\>\n\</div\>", RegexOptions.Singleline);//指定範囲から歌詞を摘出
            var lines = lyric_ms.Groups["lyric"].Value.Trim();//歌詞をトリム。
            return lines;//歌詞を返す。
        }

        private static string GetId(WebClientEx client, string titele, string artist, string album)
        {
            var mode = album != "" ? "info" : "kyoku";//アルバム情報があれば優先的にアルバム情報での検索にする。
            var search = album != "" ? album : titele;//アルバム情報での検索時に検索ワードをMusicBee側のアルバム情報にする。
            var lyricId = "";
            var requestUrl = $"https://kashinavi.com/search.php?r={mode}&search={search}&m=bubun&start=1";
            var searchPage = client.DownloadString(requestUrl);
            //曲名から歌詞ナビにある情報をすべて取得。　m=bubunで部分一致になる。
            if (!searchPage.Contains("該当データがありませんでした。"))
            {
                var startTex = "<td bgcolor=\"#EE9900\"><font color=\"#ffffff\">- - - ◆　ミニ情報</font></td>";
                var startIndex = searchPage.IndexOf(startTex) + startTex.Length + 1;//正規表現ですべてから探すのはアレなので範囲指定
                var length = searchPage.Substring(startIndex).IndexOf("<tr><td colspan=5 bgcolor=\"#FFDD55\" align=center>") - 2;//同上

                var trimtext = searchPage.Substring(startIndex, length);
                var pattern = @"<a href=""/song_view\.html\?(?<lyricId>\d+)"">(?<Song>[^<]+)</a><\/td><td style=\""width:200px\""><a href=""/artist\.html\?.+"">(?<Artist>[^<]+)</a>";

                //正規表現　歌詞ページのIDと曲名とアーティストの情報を正規表現から取得。
                var ms = Regex.Matches(trimtext, pattern);//指定範囲から検索結果を摘出

                foreach (Match m in ms)//すべての検索結果
                {
                    var art = m.Groups["Artist"].Value.Trim();
                    var title = m.Groups["Song"].Value.Trim();

                    if ((art == HttpUtility.UrlDecode(artist, Encoding)) && (title == HttpUtility.UrlDecode(titele, Encoding)))//すべての検索結果からアーティストと曲名両方が一致したIDを入れる
                    {
                        lyricId = m.Groups["lyricId"].Value.Trim();
                        break;//曲名とアーティストが両方一致したら終了
                    }
                    else if (title == (HttpUtility.UrlDecode(titele, Encoding)))//一致したのがなければ曲名に一致した最後のIDを入れる。
                    {
                        lyricId = m.Groups["lyricId"].Value.Trim();

                    }
                }
            }
            return lyricId;
        }
    }
}
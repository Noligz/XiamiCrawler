using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HtmlAgilityPack;
using System.Net;
using System.Threading;

namespace XiamiCrawler
{
    class Crawler
    {
        HtmlDocument GetHtmlDoc(string url)
        {
            string docStr = GetWebPage(url);
            //if (docStr == null || docStr=="")
            //    return null;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(docStr);
            return doc;
        }

        string GetWebPage_Core(string url)
        {
            HttpWebRequest request;
            HttpWebResponse response;
            string s;
            TextReader tr;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                response = (HttpWebResponse)request.GetResponse();

                tr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"));//正确解释汉字
                s = tr.ReadToEnd();//保存在字符串中，也可以保存在文件中
                response.Close();
                return s;
            }
            catch (Exception e)
            {
                //出错处理
                return null;
            }
        }

        string GetWebPage(string url)
        {
            if (url.IndexOf("http") < 0)
                return null;
            string ret = null;
            while (ret == null)
            {
                ret = GetWebPage_Core(url);
                if (ret != null)
                    return ret;
                int sleepTime = 5000;
                Console.WriteLine(string.Format("Retry GetWebPage in {0} seconds", (float)sleepTime / 1000));
                Thread.Sleep(sleepTime);
            }
            return null;
        }


        public void SearchArtist()
        {
            string ifArtistList = "ArtistList.txt";
            if (!File.Exists(ifArtistList))
            {
                Console.WriteLine(ifArtistList + "not exists!");
                return;
            }

            StreamReader sr = new StreamReader(ifArtistList, Encoding.Default);
            List<string> artistList = new List<string>();
            while (!sr.EndOfStream)
                artistList.Add(sr.ReadLine());
            sr.Close();
            int fileLineCount = artistList.Count;

            string ofAtistLink = "ArtistLink.csv";
            StreamWriter sw = new StreamWriter(ofAtistLink, false, Encoding.Default);
            sw.AutoFlush = true;
            sw.WriteLine("Link,OriginArtist,FoundArtist,Count");

            bool secondSearch = false;
            for (int i = 0; i < fileLineCount; i++)
            {
                Console.Title = string.Format("{0:P1}, {1}/{2}", (float)i / fileLineCount, i, fileLineCount);
                string artistName = artistList[i];
                Console.WriteLine("Searching " + artistName);
                string url;
                if (secondSearch)
                    url = string.Format("http://www.xiami.com/search/artist?&key={0}", Utility.TrimKeyword(artistName));
                else
                    url = string.Format("http://www.xiami.com/search/artist?&key={0}&category=6&is_pub=", Utility.TrimKeyword(artistName));
                HtmlDocument doc = GetHtmlDoc(url);

                string foundArtist = "";
                string artistLink = "";
                int foundCount = 0;
                var countNodes = doc.DocumentNode.SelectNodes("//div[@class='artist_item100_block']");
                if (countNodes != null && countNodes.Count > 0)
                {
                    secondSearch = false;
                    foundCount = countNodes.Count;
                    var linkNode = countNodes.First().SelectSingleNode("//a[@class='title']");
                    artistLink = linkNode.GetAttributeValue("href", "href");

                    var artistNode = countNodes.First().SelectSingleNode("//strong");
                    foundArtist = artistNode.InnerText;
                    if (Utility.CompareName(foundArtist, artistName))
                        foundArtist = "";
                }
                else if (!secondSearch)
                {
                    secondSearch = true;
                    i--;
                    continue;
                }

                sw.WriteLine(Utility.CsvEncode(artistLink, artistName, foundArtist, foundCount));
            }

            sw.Close();
        }

        public void GetAlbumFromArtist()
        {
            string ifArtistLink = "ArtistLink.csv";
            if (!File.Exists(ifArtistLink))
            {
                Console.WriteLine(ifArtistLink + "not exists!");
                return;
            }

            StreamReader sr = new StreamReader(ifArtistLink, Encoding.Default);
            List<string> artistList = new List<string>();
            List<string> urlList = new List<string>();
            sr.ReadLine();
            while (!sr.EndOfStream)
            {
                string[] line = Utility.CsvDecode(sr.ReadLine());
                if (line[0] != null && line[0] != "")
                {
                    urlList.Add(line[0]);
                    artistList.Add(line[1]);
                }
            }
            sr.Close();
            int urlCount = urlList.Count;

            string ofAlbumLink = "AlbumLink.csv";
            StreamWriter sw = new StreamWriter(ofAlbumLink, false, Encoding.Default);
            sw.AutoFlush = true;
            sw.WriteLine("Link,OriginArtist,Album,Date");

            for (int i = 0; i < urlCount; i++)
            {
                Console.Title = string.Format("{0:P1}, {1}/{2}", (float)i / urlCount, i, urlCount);
                string artistName = artistList[i];
                Console.WriteLine("Getting Album From: " + artistName);

                int pageCount = 1;
                string artistID = urlList[i].Replace("http://www.xiami.com/artist/", "");
                for (int pageID = 1; pageID <= pageCount; pageID++)
                {
                    string url = string.Format("http://www.xiami.com/artist/album/id/{0}/d//p/pub/page/{1}", artistID, pageID);
                    HtmlDocument doc = GetHtmlDoc(url);

                    if (pageID == 1)
                    {
                        var countNode = doc.DocumentNode.SelectSingleNode("//p[@class='counts']");
                        string countStr = countNode.InnerText.Replace("共", "").Replace("张专辑", "");
                        try
                        {
                            int albumCount = int.Parse(countStr);
                            if (albumCount == 0)
                                continue;
                            pageCount = albumCount / 9 + 1;
                        }
                        catch (Exception) { }
                    }

                    foreach (var albumNode in doc.DocumentNode.SelectNodes("//div[@class='album_item100_block']"))
                    {
                        var nameNode = albumNode.SelectSingleNode(".//p[@class='name']").FirstChild;
                        string albumName = nameNode.InnerText;
                        string albumLink = "http://www.xiami.com" + nameNode.GetAttributeValue("href", "href");
                        var yearNode = albumNode.SelectSingleNode(".//p[@class='year']");
                        string albumYear = yearNode.InnerText;
                        sw.WriteLine(Utility.CsvEncode(albumLink, artistName, albumName, albumYear));
                    }
                }
            }
            sw.Close();
        }

        public void GetSongFromAlbum()
        {
            string ifAlbumLink = "AlbumLink.csv";
            if (!File.Exists(ifAlbumLink))
            {
                Console.WriteLine(ifAlbumLink + "not exists!");
                return;
            }

            StreamReader sr = new StreamReader(ifAlbumLink, Encoding.Default);
            List<string> albumList = new List<string>();
            List<string> artistList = new List<string>();
            List<string> urlList = new List<string>();
            List<string> dateList = new List<string>();
            sr.ReadLine();
            while (!sr.EndOfStream)
            {
                string[] line = Utility.CsvDecode(sr.ReadLine());
                string url = line[0];
                if (url != null && url != "")
                {
                    urlList.Add(url);
                    albumList.Add(line[2]);
                    artistList.Add(line[1]);
                    dateList.Add(line[3]);
                }
            }
            sr.Close();
            int urlCount = urlList.Count;

            string ofSongLink = "SongLink.csv";
            StreamWriter sw = new StreamWriter(ofSongLink, false, Encoding.Default);
            sw.AutoFlush = true;
            sw.WriteLine("Link,OriginArtist,Album,Title,Date,Hot,Hotness,AlbumRate");

            for (int i = 0; i < urlCount; i++)
            {
                Console.Title = string.Format("{0:P1}, {1}/{2}", (float)i / urlCount, i, urlCount);
                string albumName = albumList[i];
                Console.WriteLine("Getting Song From: " + albumName);

                string url = urlList[i];
                HtmlDocument doc = GetHtmlDoc(url);

                var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@property='v:itemreviewed']");
                string title = titleNode.InnerText;

                var rateNode = doc.DocumentNode.SelectSingleNode("//em[@property='v:value']");
                string albumRate = rateNode.InnerText;

                float hotSum = 0;
                List<string> songTitleList = new List<string>();
                List<string> songLinkList = new List<string>();
                List<int> songHotList = new List<int>();

                foreach (var songNameNode in doc.DocumentNode.SelectNodes("//td[@class='song_name']"))
                {
                    string songTitle = songNameNode.InnerText;
                    songTitleList.Add(songTitle);
                    var songLinkNode = songNameNode.FirstChild;//.SelectSingleNode("//a");
                    string songLink = "http://www.xiami.com" + songLinkNode.GetAttributeValue("href", "href");
                    songLinkList.Add(songLink);
                    var songHotNode = songNameNode.SelectSingleNode("..//td[@class='song_hot']");
                    int songHot = 0;
                    string songHotStr = songHotNode.InnerText;
                    if (songHotStr != null && songHotStr != "")
                        songHot = int.Parse(songHotNode.InnerText);
                    songHotList.Add(songHot);
                    hotSum += songHot;
                }

                for (int j = 0; j < songTitleList.Count; j++)
                {
                    float hotness = -0.01f;
                    if (hotSum > 0)
                        hotness = songHotList[j] / hotSum;
                    sw.WriteLine(Utility.CsvEncode(songLinkList[j], artistList[i], albumName, songTitleList[j], dateList[i], songHotList[j], hotness, albumRate));
                }
            }
            sw.Close();
        }

        //string iAlbumListFileName = "AlbumData.txt";
        //string oAlbumLinkFileName = "AlbumLink.csv";
        //public void GetAlbumLink()
        //{
        //    if (!File.Exists(iAlbumListFileName))
        //    {
        //        Console.WriteLine(iAlbumListFileName + "not exists!");
        //        return;
        //    }

        //    StreamReader sr = new StreamReader(iAlbumListFileName, Encoding.Default);
        //    List<string> albumList = new List<string>();
        //    List<string> artistList = new List<string>();
        //    while (!sr.EndOfStream)
        //    {
        //        string[] line = sr.ReadLine().Split('\t');
        //        albumList.Add(line[0]);
        //        artistList.Add(line[1]);
        //    }
        //    sr.Close();
        //    int fileLineCount = albumList.Count;

        //    StreamWriter sw = new StreamWriter(oAlbumLinkFileName, false, Encoding.Default);
        //    sw.AutoFlush = true;
        //    sw.WriteLine("Link,AlbumName,FoundAlbumName,Count,Artist,FoundArtist");

        //    for (int i = 0; i < fileLineCount; i++)
        //    {
        //        Console.Title = string.Format("{0:P1}, {1}/{2}", (float)i / fileLineCount, i, fileLineCount);
        //        string albumName = albumList[i];
        //        string artistName = artistList[i];
        //        Console.WriteLine("Getting " + albumName);
        //        string url = "http://www.xiami.com/search/album?key=" + TrimKeyword(albumName);
        //        HtmlDocument doc = GetHtmlDoc(url);

        //        string foundAlbumName = "";
        //        string foundArtist = "";
        //        string albumLink = "";
        //        int foundCount = 0;
        //        var countNodes = doc.DocumentNode.SelectNodes("//div[@class='album_item100_block']");
        //        if (countNodes != null && countNodes.Count > 0)
        //        {
        //            foundCount = countNodes.Count;
        //            var nameNode = countNodes.First().SelectSingleNode("//p[@class='name']");
        //            var albumNode = nameNode.FirstChild;
        //            albumLink = albumNode.GetAttributeValue("href", "href");

        //            foundAlbumName = albumNode.GetAttributeValue("title", "title");
        //            if (CompareName(foundAlbumName,albumName))
        //                foundAlbumName = "";

        //            var artistNode = countNodes.First().SelectSingleNode("//a[@class='singer']");
        //            foundArtist = artistNode.GetAttributeValue("title", "title");
        //            if (CompareName(foundArtist,artistName))
        //                foundArtist = "";
        //        }

        //        sw.WriteLine(String.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\""
        //            , albumLink, albumName, foundAlbumName, foundCount, artistName, foundArtist));
        //    }

        //    sw.Close();
        //}


        //public async Task<HtmlDocument> RetrieveHtmlDoc(string url)
        //{
        //    var client = new HttpClient();
        //    var responseMessage = await client.GetAsync(url);

        //    string result = await responseMessage.Content.ReadAsStringAsync();
        //    if (!responseMessage.IsSuccessStatusCode)
        //        throw new FileNotFoundException("Unable to retrieve document");
        //    var document = new HtmlAgilityPack.HtmlDocument();
        //    document.LoadHtml(result);
        //    return document;
        //}

        //private static string DecodeFromUtf8(string str)
        //{
        //    byte[] bytes = Encoding.Default.GetBytes(str);
        //    return Encoding.UTF8.GetString(bytes);
        //}
    }
}

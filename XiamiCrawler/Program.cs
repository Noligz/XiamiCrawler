using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XiamiCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Crawler crawler = new Crawler();
            //crawler.SearchArtist();
            //crawler.GetAlbumFromArtist();
            crawler.GetSongFromAlbum();

            //crawler.GetAlbumLink();
            //crawler.GetSongInfo();

            //string a = "あくえり２～ AQUAELIE fanDISC vol.2";
            //string b = "あくえり2 ～ AQUAELIE fanDISC vol.2";
            //bool bb = crawler.CompareName(a, b);
            ////a = crawler.TrimKeyword(a);

            //StreamReader sr = new StreamReader("symbol.txt", Encoding.Default);
            //StreamWriter sw = new StreamWriter("st.txt", false, Encoding.Default);
            //while (!sr.EndOfStream)
            //{
            //    string line = sr.ReadLine();
            //    //Console.WriteLine(line);
            //    sw.WriteLine(crawler.ReplaceSymbol(line));
            //    sw.Flush();
            //}
        }

    }
}

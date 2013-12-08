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
            crawler.SearchArtist();
            crawler.GetAlbumFromArtist();
            crawler.GetSongFromAlbum();
        }

    }
}

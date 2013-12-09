using System;

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
        }

    }
}

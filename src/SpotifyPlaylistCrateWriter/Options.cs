using CommandLine;

namespace SpotifyPlaylistCrateWriter
{
    public class Options
    {
        [Option('n', "name", HelpText = "Top to bottom list of the crate name at least one entry is required.", Min = 1, Required = true, Separator = ';')]
        public IEnumerable<string> CrateName { get; set; }
        [Option('t', "target", HelpText = "The Target Path of the Crate with out the crate name.", Required = true)]
        public string TargetPath { get; set; }
        [Option('m', "music", HelpText = "The Folder where to find you're music relative from the serato folder without drive names eg. --music \"Music\"", Required = false, Default = "Music")]
        public string MusicPath { get; set; } = "Music";
        [Option('a', "absolut-music",HelpText = "The absolut path of the music folder so the application can search there for the music from the spotify playlist", Required = true)]
        public string MusicFolderAbsolutePath { get; set; }
        [Option('p', "playlist", HelpText = "Urls to a spotify playlists, album, artist or track", Required = true, Min = 1, Separator = ';')]
        public IEnumerable<Uri> PlaylistUrls { get; set; }
        [Option("appId", HelpText = "Spotify App Id to authenticate with at spotify api", Required = false, Default = "")]
        public string SpotifyAppId { get; set; } = "";
        [Option("appSecret", HelpText = "Spotify App Secret to authenticate with at spotify api", Required = false, Default = "")]
        public string SpotifyAppSecret { get; set; } = "";
        [Option("fileType", HelpText = "The File Type of the audio files in the Music folder", Required = false, Default = "mp3")]
        public string AudioFileType { get; set; } = "mp3";

        public string GetCrateName()
        {

            string name = CrateName.First();
            if(CrateName.Count() > 1) 
            {
                var crateName = CrateName.ToArray();
                using(StringWriter sw = new StringWriter())
                {
                    for(int i = 0; i < crateName.Length; i++)
                    {
                        sw.Write(crateName[i]);
                        if(i < crateName.Length - 1)
                        {
                            sw.Write("%%");
                        }
                    }
                    name = sw.ToString();
                }
            }
            name += ".crate";
            return name;
        }
    }
}

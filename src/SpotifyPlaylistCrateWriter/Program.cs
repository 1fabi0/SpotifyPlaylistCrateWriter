using CommandLine;
using CrateWriter;
using SpotifyAPI.Web;

namespace SpotifyPlaylistCrateWriter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (!args.Any())
            {
                args = ["--help"];
            }
            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync((Func<Options, Task>)(async o =>
            {
                var crate = File.Create(Path.Combine(o.TargetPath, o.GetCrateName()));
                var crateWriter = CrateStreamWriter.Create(crate);
                try
                {
                    var spotifyConfig = SpotifyClientConfig.CreateDefault();
                    spotifyConfig = spotifyConfig.WithAuthenticator(new ClientCredentialsAuthenticator(o.SpotifyAppId, o.SpotifyAppSecret));
                    var spotifyClient = new SpotifyClient(spotifyConfig);
                    foreach (var playlistUrl in o.PlaylistUrls)
                    {
                        Console.WriteLine("Processing:" + playlistUrl);
                        switch (playlistUrl.AbsolutePath.Split('/')[1].ToLower())
                        {
                            case "playlist":
                                var playlist = await spotifyClient.Playlists.Get(playlistUrl.AbsolutePath.Split("/")[2]);

                                if (playlist.Tracks == null && o.PlaylistUrls.Count() == 1)
                                    throw new Exception("Tracks in the playlist are empty");
                                else if (playlist.Tracks == null)
                                    break;

                                await foreach (var item in spotifyClient.Paginate<PlaylistTrack<IPlayableItem>>(playlist.Tracks))
                                {
                                    if (item.Track is SimpleTrack track1)
                                    {
                                        WriteToCrate(crateWriter, track1.Name, track1.Artists, o);
                                    }
                                    else if (item.Track is FullTrack track2)
                                    {
                                        WriteToCrate(crateWriter, track2.Name, track2.Artists, o);
                                    }
                                }
                                break;
                            case "artist":
                                var artistAlbum = await spotifyClient.Artists.GetAlbums(playlistUrl.AbsolutePath.Split("/")[2]);
                                await foreach (var fullAlbum in spotifyClient.Paginate<SimpleAlbum>(artistAlbum))
                                {
                                    var album = await spotifyClient.Albums.Get(fullAlbum.Id);

                                    await foreach (var item in spotifyClient.Paginate<SimpleTrack>(album.Tracks))
                                    {
                                        WriteToCrate(crateWriter, item.Name, item.Artists, o);
                                    }
                                }
                                break;
                            case "track":
                                var track = await spotifyClient.Tracks.Get(playlistUrl.AbsolutePath.Split("/")[2]);
                                if (track == null && o.PlaylistUrls.Count() == 1)
                                {
                                    throw new Exception("Track not found");
                                }
                                else if (track == null)
                                {
                                    break;
                                }
                                WriteToCrate(crateWriter, track.Name, track.Artists, o);
                                break;
                            case "album":
                                var getAlbum = await spotifyClient.Albums.Get(playlistUrl.AbsolutePath.Split("/")[2]);
                                await foreach (var item in spotifyClient.Paginate<SimpleTrack>(getAlbum.Tracks))
                                {
                                    WriteToCrate(crateWriter, item.Name, item.Artists, o);
                                }
                                break;
                            default:
                                throw new Exception("Illegal spotify url, application needs to know if its ");
                        }
                    }
                }
                catch
                {
                    crate.Dispose();
                    File.Delete(crate.Name);
                    throw;
                }
                crate.Close();
                crate.Dispose();
            }));
        }

        private static void WriteToCrate(CrateStreamWriter crateWriter, string trackName, List<SimpleArtist> artists, Options options)
        {
            var trackArtistAndName = string.Empty;
            using (StringWriter sw = new StringWriter())
            {
                for (var i = 0; i < artists.Count; i++)
                {
                    sw.Write(artists[i].Name);
                    if (i < artists.Count - 1)
                    {
                        sw.Write(", ");
                    }
                }
                sw.Write(" - ");
                sw.Write(trackName);

                sw.Write(".");
                sw.Write(options.AudioFileType);
                trackArtistAndName = sw.ToString();
            }
            if (File.Exists(Path.Combine(options.MusicFolderAbsolutePath, trackArtistAndName)))
            {
                Console.WriteLine("Writing " + trackArtistAndName + " to crate.");
                crateWriter.WriteTrack(options.MusicPath + "/" + trackArtistAndName);
            }
            else
            {
                Console.WriteLine("Skipping " + trackArtistAndName);
            }
        }

    }
}

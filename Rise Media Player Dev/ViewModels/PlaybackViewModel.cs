﻿using Rise.App.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace Rise.App.ViewModels
{
    public class PlaybackViewModel : BaseViewModel, ICancellableTask
    {
        /// <summary>
        /// Creates a new <see cref="PlaybackViewModel"/>.
        /// </summary>
        public PlaybackViewModel()
        {
            Player.Source = PlaybackList;
            PlaybackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;
        }

        #region Variables
        /// <summary>
        /// Gets or sets the list of songs that are currently queued.
        /// </summary>
        public ObservableCollection<SongViewModel> PlayingSongs { get; set; } =
            new ObservableCollection<SongViewModel>();

        private SongViewModel _currentSong;

        /// <summary>
        /// Gets the song that's currently playing.
        /// </summary>
        public SongViewModel CurrentSong
        {
            get => _currentSong;
            set => Set(ref _currentSong, value);
        }

        public MediaPlayer Player { get; }
            = new MediaPlayer();

        public MediaPlaybackList PlaybackList { get; set; }
            = new MediaPlaybackList();

        public CancellationTokenSource CTS { get; set; }
            = new CancellationTokenSource();

        public CancellationToken Token => CTS.Token;

        public bool CanContinue { get; set; }
            = true;
        #endregion

        public async Task StartShuffle(IEnumerator<object> songs, int count)
        {
            List<SongViewModel> list = new List<SongViewModel>();

            while (songs.MoveNext())
            {
                list.Add(songs.Current as SongViewModel);
            }

            Random rng = new Random();
            list = list.OrderBy(s => rng.Next()).ToList();

            CancelTask();
            await CreatePlaybackList(0, count,
                list.AsEnumerable().GetEnumerator(), Token);
        }

        public async Task StartPlayback(IEnumerator<object> songs, int startIndex, int count)
        {
            CancelTask();
            await CreatePlaybackList(startIndex, count, songs, Token);
        }

        public async Task StartPlayback(IEnumerator<IStorageItem> songs, int startIndex, int count)
        {
            CancelTask();
            List<SongViewModel> list = new List<SongViewModel>();
            while (songs.MoveNext())
            {
                list.Add(new SongViewModel
                    (await (songs.Current as StorageFile).AsSongModelAsync()));
            }

            await CreatePlaybackList(startIndex, count, list.GetEnumerator(), Token);
            songs.Dispose();
        }

        public async Task CreatePlaybackList(int index, int count, IEnumerator<object> songs, CancellationToken token)
        {
            while (!CanContinue)
            {
                // Not so efficient, but it's legitimately the only thing I could
                // think of to prevent the tasks from overlapping
                await Task.Delay(30);
            }

            PlayingSongs.Clear();
            PlaybackList.Items.Clear();
            CanContinue = false;
            songs.MoveNext();

            int pos = 0;
            int addedSongs = 1;
            while (pos != index)
            {
                pos++;
                songs.MoveNext();
            }

            // Add initial item to avoid delays when starting playback
            MediaPlaybackItem item =
                await CreateMusicItem(songs.Current as SongViewModel);

            PlaybackList.Items.Add(item);
            PlayingSongs.Add(songs.Current as SongViewModel);

            // Not disposing the media player here is intentional, it gets
            // marshalled from a different thread when setting the media players
            // and running on the UI thread here isn't desirable.
            Player.Source = PlaybackList;
            Player.Play();

            SetCurrentSong(item);
            songs.MoveNext();

            while (addedSongs < count)
            {
                if (token.IsCancellationRequested)
                {
                    Debug.WriteLine("Stop!");
                    songs.Dispose();
                    CanContinue = true;
                    return;
                }

                if (!songs.MoveNext())
                {
                    songs.Reset();
                    songs.MoveNext();
                }

                item = await CreateMusicItem(songs.Current as SongViewModel);
                PlaybackList.Items.Add(item);
                PlayingSongs.Add(songs.Current as SongViewModel);

                addedSongs++;
            }

            songs.Dispose();
            CanContinue = true;
            return;
        }

        /// <summary>
        /// Creates a <see cref="MediaPlaybackItem"/> from a <see cref="SongViewModel"/>.
        /// </summary>
        /// <param name="model">Song to convert.</param>
        /// <returns>A <see cref="MediaPlaybackItem"/> based on the song.</returns>
        private async Task<MediaPlaybackItem> CreateMusicItem(SongViewModel model)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(model.Location);

            MediaSource source = MediaSource.CreateFromStorageFile(file);
            MediaPlaybackItem media = new MediaPlaybackItem(source);

            MediaItemDisplayProperties props = media.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;

            props.MusicProperties.Title = model.Title;
            props.MusicProperties.Artist = model.Artist;
            props.MusicProperties.AlbumTitle = model.Album;
            props.MusicProperties.AlbumArtist = model.AlbumArtist;
            props.MusicProperties.TrackNumber = model.Track;

            if (model.Thumbnail != null)
            {
                props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(model.Thumbnail));
            }

            media.ApplyDisplayProperties(props);
            return media;
        }

        public void SetCurrentSong(MediaPlaybackItem item)
        {
            if (item == null)
            {
                return;
            }

            MusicDisplayProperties props = item.GetDisplayProperties().MusicProperties;
            SongViewModel song = new SongViewModel
            {
                Title = props.Title,
                Artist = props.Artist,
                Album = props.AlbumTitle,
                AlbumArtist = props.AlbumArtist,
                Track = props.TrackNumber
            };

            CurrentSong = song;
        }

        public void CancelTask()
        {
            CTS.Cancel();
            CTS = new CancellationTokenSource();
        }

        private async void PlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            Debug.WriteLine("Item changed!");
            if (args.Reason == MediaPlaybackItemChangedReason.Error)
            {
                Debug.WriteLine("Can't do much with this really.");
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SetCurrentSong(sender.CurrentItem);
            });
        }
    }
}

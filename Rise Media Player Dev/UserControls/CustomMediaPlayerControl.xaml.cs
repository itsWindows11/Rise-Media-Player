using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Composition;
using System.Numerics;
using Windows.UI;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Media.Core;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Rise.App.UserControls
{
    public sealed partial class CustomMediaPlayerControl : UserControl
    {
        private MediaPlayer _player = App.PViewModel.Player;

        public CustomMediaPlayerControl()
        {
            this.InitializeComponent();

            _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                 SliderProgress.Maximum = sender.NaturalDuration.TotalSeconds;
                 SetSongTitle(App.PViewModel.CurrentSong.Title);
                 SongArtist.Text = App.PViewModel.CurrentSong.Artist;

                int seconds = (int)sender.NaturalDuration.TotalSeconds;
                int minutes = (int)sender.NaturalDuration.TotalMinutes;
                if (seconds < 10)
                {
                    MediaPlayingDurationRight.Text = $"{minutes}:0{seconds}";
                }
                else MediaPlayingDurationRight.Text = $"{minutes}:{seconds}";
            });

            if (sender.PlaybackState == MediaPlaybackState.Playing)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     PlayButtonIcon.Symbol = Symbol.Pause;
                 });
            }
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SliderProgress.Value = sender.Position.TotalSeconds;
                int seconds = (int)sender.Position.TotalSeconds;
                int minutes = (int)sender.Position.TotalMinutes;
                if (seconds < 10)
                {
                    MediaPlayingDurationLeft.Text = $"{minutes}:0{seconds}";
                }
                else MediaPlayingDurationLeft.Text = $"{minutes}:{seconds}";
            });
        }

        public void TogglePlayPause()
        {
            if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                _player.Play();
                PlayButtonIcon.Symbol = Symbol.Pause;
            }
            else if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                _player.Pause();
                PlayButtonIcon.Symbol = Symbol.Play;
            }
        }

        #region Getters/Setters
        public void SetSongTitle(string songTitle)
        {
            SongTitle.Text = songTitle;
        }
        #endregion

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode != ApplicationViewMode.CompactOverlay)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 400);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                GoToOverlayIcon.Visibility = Visibility.Collapsed;
                ExitOverlayIcon.Visibility = Visibility.Visible;
            } else
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(600, 700);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default, preferences);
                ExitOverlayIcon.Visibility = Visibility.Collapsed;
                GoToOverlayIcon.Visibility = Visibility.Visible;
            }

        }
    }
}

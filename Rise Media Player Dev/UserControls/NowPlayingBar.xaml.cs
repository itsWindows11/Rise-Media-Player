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
using Windows.System.Profile;
using Microsoft.Toolkit.Uwp.UI;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using ColorThiefDotNet;
using Windows.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace Rise.App.UserControls
{
    public sealed partial class NowPlayingBar : UserControl
    {
        #region Variables
        private MediaPlayer _player = App.PViewModel.Player;
        #endregion

        #region Properties

        public static readonly DependencyProperty ShowArtist = DependencyProperty.Register("IsArtistShown", typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(null));

        public bool IsArtistShown 
        {
            get => (bool)GetValue(ShowArtist);
            set => SetValue(ShowArtist, value);
        }

        public static readonly DependencyProperty TransparencyEnabled = DependencyProperty.Register("Transparent", typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(null));

        public bool Transparent
        {
            get => (bool)GetValue(TransparencyEnabled);
            set => SetValue(TransparencyEnabled, value);
        }

        #endregion

        public NowPlayingBar()
        {
            this.InitializeComponent();

            _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _player.SourceChanged += MediaPlayer_SourceChanged;

            if (!IsArtistShown)
            {
                Grid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Star);
            }

            if (Transparent)
            {
                Effects.SetShadow(Parent1, EmptyDropShadow);
                Grid.Background = new SolidColorBrush { Color = Colors.Transparent };
                Grid.BorderThickness = new Thickness(0);
            }

            DataContext = App.PViewModel;
        }

        #region Listeners

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.PlaybackState == MediaPlaybackState.Playing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                     PlayButtonIcon.Glyph = "\uF8AE";
                });
            } else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PlayButtonIcon.Glyph = "\uF5B0";
                });
            }
        }

        private async void MediaPlayer_SourceChanged(MediaPlayer sender, object args)
        {
            if (!Transparent)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    Uri imageUri = new Uri(App.PViewModel.CurrentSong.Thumbnail);
                    if (App.PViewModel.CurrentSong.Thumbnail != "ms-appx:///Assets/Default.png")
                    {
                        RandomAccessStreamReference random = RandomAccessStreamReference.CreateFromUri(imageUri);
                        using (IRandomAccessStream stream = await random.OpenReadAsync())
                        {
                            var decoder = await BitmapDecoder.CreateAsync(stream);
                            var colorThief = new ColorThief();
                            var color = await colorThief.GetColor(decoder);
                            Grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, color.Color.R, color.Color.G, color.Color.B));
                        }
                    }
                });
            }
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                int seconds = (int)sender.NaturalDuration.TotalSeconds;
                int minutes = (int)sender.NaturalDuration.TotalMinutes;
                SliderProgress.Maximum = sender.NaturalDuration.TotalSeconds;
                if (seconds < 10)
                {
                    MediaPlayingDurationRight.Text = $"{minutes}:0{seconds}";
                }
                else MediaPlayingDurationRight.Text = $"{minutes}:{seconds}";
            });

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
            }
            else
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(600, 700);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default, preferences);
                ExitOverlayIcon.Visibility = Visibility.Collapsed;
                GoToOverlayIcon.Visibility = Visibility.Visible;
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Window.Current.Bounds.Width >= 1000)
            {
                DefaultVolumeControl.Visibility = Visibility.Visible;
                VolumeFlyoutButton.Visibility = Visibility.Collapsed;
                AlbumArtContainer.Visibility = Visibility.Visible;
                if (IsArtistShown)
                {
                    Grid.ColumnDefinitions[0].Width = new GridLength(0.6, GridUnitType.Star);
                }
                Grid.ColumnDefinitions[2].Width = new GridLength(0.45, GridUnitType.Star);
            } else if (Window.Current.Bounds.Width >= 600)
            {
                DefaultVolumeControl.Visibility = Visibility.Collapsed;
                VolumeFlyoutButton.Visibility = Visibility.Visible;
                AlbumArtContainer.Visibility = Visibility.Collapsed;
                if (IsArtistShown) Grid.ColumnDefinitions[0].Width = new GridLength(0.6, GridUnitType.Star);
                Grid.ColumnDefinitions[2].Width = new GridLength(0.45, GridUnitType.Star);
            } else if (Window.Current.Bounds.Width >= 400)
            {
                DefaultVolumeControl.Visibility = Visibility.Collapsed;
                VolumeFlyoutButton.Visibility = Visibility.Visible;
                AlbumArtContainer.Visibility = Visibility.Collapsed;
                Grid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Star);
                Grid.ColumnDefinitions[2].Width = new GridLength(0.45, GridUnitType.Star);
            }
            else
            {
                DefaultVolumeControl.Visibility = Visibility.Collapsed;
                VolumeFlyoutButton.Visibility = Visibility.Visible;
                Grid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Star);
                Grid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Star);
                AlbumArtContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _player.Volume = VolumeSlider.Value;
        }

        private void SliderProgress_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _player.PlaybackSession.Position = TimeSpan.FromSeconds(SliderProgress.Value);
        }

        #endregion

        public void TogglePlayPause()
        {
            if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                _player.Play();
                PlayButtonIcon.Glyph = "\uF8AE";
            }
            else if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                _player.Pause();
                PlayButtonIcon.Glyph = "\uF5B0";
            }
        }
    }
}

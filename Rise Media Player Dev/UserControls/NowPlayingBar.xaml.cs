﻿using System;
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
using Rise.App.Converters;
using System.Diagnostics;

namespace Rise.App.UserControls
{
    public sealed partial class NowPlayingBar : UserControl
    {
        #region Variables
        private MediaPlayer _player = App.PViewModel.Player;

        private string PlayButtonText;
        #endregion

        #region Properties

        public static readonly DependencyProperty ShowArtist = DependencyProperty.Register("IsArtistShown", typeof(bool), typeof(NowPlayingBar), new PropertyMetadata(null));

        public bool IsArtistShown
        {
            get => (bool)GetValue(ShowArtist);
            set => SetValue(ShowArtist, value);
        }

        public static readonly DependencyProperty BackgroundStylesProperty = DependencyProperty.Register("BackgroundStyle", typeof(NowPlayingBarBackgroundStyles), typeof(NowPlayingBar), new PropertyMetadata(null));

        private long _backgroundStylesPropertyToken;

        private NowPlayingBarBackgroundStyles _backgroundStyle = NowPlayingBarBackgroundStyles.Acrylic;

        public NowPlayingBarBackgroundStyles BackgroundStyle
        {
            get => (NowPlayingBarBackgroundStyles)GetValue(BackgroundStylesProperty);
            set
            {
                SetValue(BackgroundStylesProperty, value);
            }
        }

        #endregion

        public NowPlayingBar()
        {
            this.InitializeComponent();

            DataContext = App.PViewModel;
            Loaded += NowPlayingBar_Loaded;
            Unloaded += NowPlayingBar_Unloaded;
        }

        #region Listeners

        private void SliderProgress_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _player.PlaybackSession.Position = TimeSpan.FromSeconds(SliderProgress.Value);
        }

        private void NowPlayingBar_Unloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(BackgroundStylesProperty, _backgroundStylesPropertyToken);
        }

        private void NowPlayingBar_Loaded(object sender, RoutedEventArgs e)
        {
            _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _backgroundStylesPropertyToken = RegisterPropertyChangedCallback(BackgroundStylesProperty, (sender1, dependencyObject) =>
            {
                if (dependencyObject == BackgroundStylesProperty)
                {
                    _backgroundStyle = (NowPlayingBarBackgroundStyles)sender1.GetValue(BackgroundStylesProperty);
                    switch ((NowPlayingBarBackgroundStyles)sender1.GetValue(BackgroundStylesProperty))
                    {
                        case NowPlayingBarBackgroundStyles.Transparent:
                            Grid.Background = new SolidColorBrush(Colors.Transparent);
                            Effects.SetShadow(Parent1, EmptyDropShadow);
                            break;
                        case NowPlayingBarBackgroundStyles.Acrylic:
                        case NowPlayingBarBackgroundStyles.UseAlbumArt:
                            Grid.Background = (Brush)Resources["SystemControlAcrylicElementBrush"];
                            Effects.SetShadow(Parent1, DropShadow);
                            break;
                    }
                }
            });
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.PlaybackState == MediaPlaybackState.Playing)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PlayButtonIcon.Glyph = "\uE62E";
                    ToolTipService.SetToolTip(PlayButton, "Pause");
                });
            }
            else if (sender.PlaybackState == MediaPlaybackState.Paused)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PlayButtonIcon.Glyph = "\uF5B0";
                    ToolTipService.SetToolTip(PlayButton, "Play");
                });
            } else if (sender.PlaybackState == MediaPlaybackState.Buffering)
            {
                ToolTipService.SetToolTip(PlayButton, "Buffering...");
            }
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                int seconds = (int)sender.NaturalDuration.TotalSeconds;
                int minutes = (int)sender.NaturalDuration.TotalMinutes;
                SliderProgress.Maximum = sender.NaturalDuration.TotalSeconds;
                MediaPlayingDurationRight.Text = TimeSpanToString.Convert(TimeSpan.FromSeconds(seconds));
            });

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SliderProgress.Value = sender.Position.TotalSeconds;
                int seconds = (int)sender.Position.TotalSeconds;
                int minutes = (int)sender.Position.TotalMinutes;
                MediaPlayingDurationLeft.Text = TimeSpanToString.Convert(TimeSpan.FromSeconds(seconds));
            });

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if ((NowPlayingBarBackgroundStyles)GetValue(BackgroundStylesProperty) == NowPlayingBarBackgroundStyles.UseAlbumArt && App.PViewModel.CurrentSong != null)
                {
                    Uri imageUri = new Uri(App.PViewModel.CurrentSong.Thumbnail);
                    RandomAccessStreamReference random = RandomAccessStreamReference.CreateFromUri(imageUri);
                    IRandomAccessStream stream = await random.OpenReadAsync();
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    ColorThief colorThief = new ColorThief();
                    var color = await colorThief.GetColor(decoder);
                    Grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, color.Color.R, color.Color.G, color.Color.B));
                }
            });
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            FontIcon fontIcon = OverlayButton.FindChildren().First() as FontIcon;
            if (ApplicationView.GetForCurrentView().ViewMode != ApplicationViewMode.CompactOverlay)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 400);
                _ = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                fontIcon.Glyph = "\uEE47";
            }
            else
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(600, 700);
                _ = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default, preferences);
                fontIcon.Glyph = "\uEE49";
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width >= 900)
            {
                DefaultVolumeControl.Visibility = Visibility.Visible;
                VolumeFlyoutButton.Visibility = Visibility.Collapsed;
                AlbumArtContainer.Visibility = Visibility.Visible;
                if (IsArtistShown)
                {
                    Grid.ColumnDefinitions[0].Width = new GridLength(0.6, GridUnitType.Star);
                }
                Grid.ColumnDefinitions[2].Width = new GridLength(0.45, GridUnitType.Star);
            }
            else if (e.NewSize.Width >= 600)
            {
                DefaultVolumeControl.Visibility = Visibility.Collapsed;
                VolumeFlyoutButton.Visibility = Visibility.Visible;
                AlbumArtContainer.Visibility = Visibility.Collapsed;
                if (IsArtistShown) Grid.ColumnDefinitions[0].Width = new GridLength(0.6, GridUnitType.Star);
                Grid.ColumnDefinitions[2].Width = new GridLength(0.45, GridUnitType.Star);
            }
            else if (e.NewSize.Width >= 400)
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

        public enum NowPlayingBarBackgroundStyles
        {
            Transparent,
            Acrylic,
            UseAlbumArt
        }

        private void PlayButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Button button = sender as Button;
            Border border = button.Parent as Border;
            border.BorderBrush = PlayBorderBrush;
        }

        private void PlayButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Button button = sender as Button;
            Border border = button.Parent as Border;
            border.BorderBrush = new SolidColorBrush();
        }
    }
}
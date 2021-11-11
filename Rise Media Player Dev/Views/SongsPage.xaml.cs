﻿using Microsoft.Toolkit.Uwp.UI;
using RMP.App.Common;
using RMP.App.ViewModels;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace RMP.App.Views
{
    public sealed partial class SongsPage : Page
    {
        #region Variables
        /// <summary>
        /// Gets the app-wide MViewModel instance.
        /// </summary>
        private MainViewModel MViewModel => App.MViewModel;

        /// <summary>
        /// Gets the app-wide PViewModel instance.
        /// </summary>
        private PlaybackViewModel PViewModel => App.PViewModel;

        private readonly NavigationHelper navigationHelper;
        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper => navigationHelper;

        private readonly static DependencyProperty SelectedSongProperty =
            DependencyProperty.Register("SelectedSong", typeof(SongViewModel), typeof(SongsPage), null);

        private SongViewModel SelectedSong
        {
            get => (SongViewModel)GetValue(SelectedSongProperty);
            set => SetValue(SelectedSongProperty, value);
        }

        private AdvancedCollectionView Songs => MViewModel.FilteredSongs;

        private string SortProperty = "Title";
        private SortDirection CurrentSort = SortDirection.Ascending;
        #endregion

        public SongsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            navigationHelper = new NavigationHelper(this);
            navigationHelper.LoadState += NavigationHelper_LoadState;
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Songs.Filter = null;
            Songs.SortDescriptions.Clear();
            Songs.SortDescriptions.Add(new SortDescription(SortProperty, CurrentSort));
            Songs.Refresh();
        }

        #region Event handlers
        private async void MainList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement).DataContext is SongViewModel)
            {
                int itemIndex = MainList.SelectedIndex;
                if (itemIndex < 0)
                {
                    return;
                }

                SelectedSong = null;
                using (Songs.DeferRefresh())
                {
                    await PViewModel.StartPlayback
                        (Songs.GetEnumerator(), itemIndex, Songs.Count);
                }
                Songs.Refresh();
            }
        }

        private void MainList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement).DataContext is SongViewModel song)
            {
                SelectedSong = song;
                SongFlyout.ShowAt(MainList, e.GetPosition(MainList));
            }
        }

        private async void Props_Click(object sender, RoutedEventArgs e)
            => await SelectedSong.StartEdit();

        private void ShowArtist_Click(object sender, RoutedEventArgs e)
        {
            _ = Frame.Navigate(typeof(ArtistSongsPage),
                App.MViewModel.Artists.FirstOrDefault(a => a.Name == SelectedSong.Artist));

            SelectedSong = null;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;
            if ((e.OriginalSource as FrameworkElement).DataContext is SongViewModel song)
            {
                index = MainList.Items.IndexOf(song);
            }
            else if (SelectedSong != null)
            {
                index = MainList.Items.IndexOf(SelectedSong);
                SelectedSong = null;
            }

            using (Songs.DeferRefresh())
            {
                await PViewModel.StartPlayback(Songs.GetEnumerator(), index, Songs.Count);
            }
            Songs.Refresh();
        }

        private async void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedSong = null;
            using (Songs.DeferRefresh())
            {
                await PViewModel.StartShuffle(Songs.GetEnumerator(), Songs.Count);
            }
            Songs.Refresh();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectedSong.StartEdit();
            SelectedSong = null;
        }

        private void SortFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            Songs.SortDescriptions.Clear();

            string tag = item.Tag.ToString();
            switch (tag)
            {
                case "Ascending":
                    CurrentSort = SortDirection.Ascending;
                    break;

                case "Descending":
                    CurrentSort = SortDirection.Descending;
                    break;

                case "Track":
                    Songs.SortDescriptions.
                        Add(new SortDescription("Disc", CurrentSort));
                    SortProperty = tag;
                    break;

                default:
                    SortProperty = tag;
                    break;
            }

            Songs.SortDescriptions.
                Add(new SortDescription(SortProperty, CurrentSort));
        }
        #endregion

        #region NavigationHelper registration
        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
            => navigationHelper.OnNavigatedTo(e);

        protected override void OnNavigatedFrom(NavigationEventArgs e)
            => navigationHelper.OnNavigatedFrom(e);
        #endregion
    }
}

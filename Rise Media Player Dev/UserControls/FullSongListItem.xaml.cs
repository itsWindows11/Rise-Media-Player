﻿using RMP.App.ViewModels;
using RMP.App.Views;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;

namespace RMP.App.UserControls
{
    public sealed partial class FullSongListItem : UserControl
    {
        public FullSongListItem()
        {
            InitializeComponent();
        }

        private AlbumViewModel Album;
        private ArtistViewModel Artist;

        public static readonly DependencyProperty IsPointerOverProperty =
            DependencyProperty.Register("IsPointerOver", typeof(bool), typeof(FullSongListItem),
                new PropertyMetadata(false));

        public bool IsPointerOver
        {
            get => (bool)GetValue(IsPointerOverProperty);
            set => SetValue(IsPointerOverProperty, value);
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            IsPointerOver = true;
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled(e);
            IsPointerOver = false;
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            IsPointerOver = false;
        }

        public event RoutedEventHandler Click;
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void ArtistLink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            if (Artist == null)
            {
                Artist = App.MViewModel.Artists.
                    FirstOrDefault(a => a.Name == ArtistLink.Text);
            }

            MainPage.Current.ContentFrame.
                Navigate(typeof(ArtistSongsPage), Artist);
        }

        private void AlbumLink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            if (Album == null)
            {
                Album = App.MViewModel.Albums.
                    FirstOrDefault(a => a.Title == AlbumLink.Text);
            }

            MainPage.Current.ContentFrame.
                Navigate(typeof(AlbumSongsPage), Album);
        }
    }
}

﻿using Rise.App.ViewModels;
using System;
using System.Globalization;
using System.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Rise.App.Dialogs
{
    public sealed partial class MusicStreamingDialog : ContentDialog
    {
        public MusicStreamingDialog()
        {
            InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool isValidSong;

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(StreamingTextBox.Text);
                req.Method = "HEAD";
                using var resp = req.GetResponse();
                isValidSong = resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                           .StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                isValidSong = false;
            }

            if (!(Uri.IsWellFormedUriString(StreamingTextBox.Text, UriKind.Absolute) && isValidSong))
            {
                // Not a well formed URL, show error and don't continue.
                if (InvalidUrlText.Visibility == Visibility.Collapsed)
                {
                    InvalidUrlText.Visibility = Visibility.Visible;
                }
                return;
            }

            // Well formed URL (if it isn't then we already stopped calling this function at this point)
            // TODO: create a song view model based on the information found in the file and play it
            if (InvalidUrlText.Visibility == Visibility.Visible)
            {
                InvalidUrlText.Visibility = Visibility.Collapsed;
            }

            Hide();

            SongViewModel song = new()
            {
                Title = "title",
                Track = 0,
                Disc = 0,
                Album = "UnknownAlbumResource",
                Artist = "UnknownArtistResource",
                AlbumArtist = "UnknownArtistResource",
                Location = StreamingTextBox.Text,
                IsOnline = true
            };

            await App.PViewModel.PlaySongFromUrlAsync(song);
        }
    }
}

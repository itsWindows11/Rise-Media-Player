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

namespace Rise.App.UserControls
{
    public sealed partial class CustomMediaPlayerControl : UserControl
    {
        public MediaPlayer MediaPlayer { get; set; }

        public CustomMediaPlayerControl()
        {
            this.InitializeComponent();

            //SongTitle.Text = App.PViewModel.CurrentSong.Title;
            MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            throw new NotImplementedException("Not implemented");
        }


        #region Getters/Setters
        public void SetSongTitle(string songTitle)
        {
            SongTitle.Text = songTitle;
        }
        #endregion

    }
}

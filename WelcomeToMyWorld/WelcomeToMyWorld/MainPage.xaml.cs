using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using Plugin.LocalNotifications;

namespace WelcomeToMyWorld {
    public partial class MainPage : ContentPage {
        Watcher Watcher { get; set; }

        public MainPage() {
            InitializeComponent();
            RestoreInfo();
        }

        async void RestoreInfo() {
            var username = await SecureStorage.GetAsync("username");
            var password = await SecureStorage.GetAsync("password");
            Device.BeginInvokeOnMainThread(() => {
                Username.Text = username;
                Password.Text = password;
                Application.Current.Properties.TryGetValue("worldId", out var worldId);
                if (worldId != null) WorldId.Text = worldId.ToString();
                Application.Current.Properties.TryGetValue("intervalMinutes", out var intervalMinutes);
                if (intervalMinutes != null && (int)intervalMinutes != 0) IntervalMinutes.Text = intervalMinutes.ToString();
            });
        }

        void OnToggleWatch(object sender, EventArgs args) {
            if (Watcher == null) {
                StartWatch();
            } else {
                StopWatch();
            }
        }

        async void StartWatch() {
            var username = Username.Text;
            var password = Password.Text;
            var worldId = WorldId.Text;
            var intervalMinutesText = IntervalMinutes.Text;
            int.TryParse(intervalMinutesText ?? "", out var intervalMinutes);

            if (intervalMinutes == 0) intervalMinutes = 5;
            if (username == null || username.Length == 0) { ErrorAlert("missing username!"); return; }
            if (password == null || password.Length == 0) { ErrorAlert("missing password!"); return; }
            if (worldId == null || worldId.Length == 0) { ErrorAlert("missing worldId!"); return; }

            ToggleWatch.Text = "Stop";
            Username.IsEnabled = false;
            Password.IsEnabled = false;
            WorldId.IsEnabled = false;
            IntervalMinutes.IsEnabled = false;
            IntervalMinutes.Text = intervalMinutes.ToString();

            await SecureStorage.SetAsync("username", username);
            await SecureStorage.SetAsync("password", password);
            Application.Current.Properties["worldId"] = worldId;
            Application.Current.Properties["intervalMinutes"] = intervalMinutes;

            Watcher = new Watcher(username, password, worldId, WatchCallback, WatchErrorCallback, TimeSpan.FromMinutes(intervalMinutes));
            Watcher.Start();
        }

        void StopWatch() {
            Watcher.Stop();
            Watcher = null;
            Username.IsEnabled = true;
            Password.IsEnabled = true;
            WorldId.IsEnabled = true;
            IntervalMinutes.IsEnabled = true;
            ToggleWatch.Text = "Watch!";
        }

        void WatchCallback(Watcher.WorldDiff diff) {
            Device.BeginInvokeOnMainThread(() => {
                if (diff.Changed) {
                    CrossLocalNotifications.Current.Show(diff.World.Name, diff.ToString());
                }
                WorldName.Text = diff.World.Name;
                WorldDescription.Text = diff.World.Description;
                WorldImage.Source = ImageSource.FromUri(new Uri(diff.World.ThumbnailImageUrl));
                // TotalLikes.Text = diff.World.TotalLikes.ToString();
                // TotalVisits.Text = diff.World.TotalVisits.ToString();
                ReleaseStatus.Text = Enum.GetName(typeof(ReleaseStatus), diff.World.ReleaseStatus);
                InstanceCount.Text = diff.World.Instances.Count.ToString();
                Occupants.Text = diff.World.Occupants.ToString();
            });
        }

        void WatchErrorCallback(Exception error) {
            Device.BeginInvokeOnMainThread(() => {
                ErrorAlert(error.Message);
            });
        }

        void ErrorAlert(string message) {
            DisplayAlert("Error", message, "OK");
        }
    }
}

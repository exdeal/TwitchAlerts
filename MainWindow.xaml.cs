using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TwitchAlerts.Classes;

namespace TwitchAlerts
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            CategoryImage.Source = null;

            string channelUrl = ChannelUrl.Text;
            string username = ExtractUsernameFromUrl(channelUrl);

            if (string.IsNullOrEmpty(username))
            {
                ResultText.Text = "Неверная ссылка на Twitch.";
                return;
            }

            TwitchAPI api = new TwitchAPI();
            string result = await api.GetChannelInfoAsync(username);
            string category = await api.GetStreamCategoryAsync(username);
            string avatarUrl = await api.GetUserAvatarAsync(username);
            string categoryImageUrl = await api.GetStreamCategoryImageAsync(username);
            string channelId = await api.GetChannelIdAsync(username);

            if (channelId.StartsWith("Ошибка"))
            {
                ResultText.Text = channelId;
                return;
            }

            if (!string.IsNullOrEmpty(categoryImageUrl))
            {
                string width = "1920";
                string height = "1080";

                categoryImageUrl = categoryImageUrl.Replace("{width}", width).Replace("{height}", height);

                try
                {
                    Uri imageUri = new Uri(categoryImageUrl);
                    BitmapImage bitmapImage = new BitmapImage(imageUri);
                    CategoryImage.Source = bitmapImage;
                }
                catch (UriFormatException ex)
                {
                    ResultText.Text = $"Ошибка формата URL изображения категории: {ex.Message}";
                }
            }
            else
            {
                ResultText.Text = "Изображение категории не найдено.";
            }

            if (!string.IsNullOrEmpty(avatarUrl))
            {
                try
                {
                    img.Source = new BitmapImage(new Uri(avatarUrl));
                }
                catch (UriFormatException ex)
                {
                    ResultText.Text = $"Ошибка формата URL аватара: {ex.Message}";
                }
            }

            ResultText.Text = result;
            ResultText.Text += category;
            ResultText.Text += $"\nID:{channelId}";
        }

        private string ExtractUsernameFromUrl(string url)
        {
            var match = Regex.Match(url, @"twitch\.tv/([\w\d_]+)");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

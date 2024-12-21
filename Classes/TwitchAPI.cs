using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace TwitchAlerts.Classes
{
    public class TwitchAPI
    {
        private static readonly IConfiguration Configuration;
        private static readonly string ClientId;
        private static readonly string AccessToken;
        static TwitchAPI()
        {
            try
            {
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                ClientId = Configuration["Twitch:ClientId"] ?? Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID");
                AccessToken = Configuration["Twitch:AccessToken"] ?? Environment.GetEnvironmentVariable("TWITCH_ACCESS_TOKEN");

                if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(AccessToken))
                {
                    throw new Exception("ClientId или AccessToken не настроены. Проверьте настройки приложения.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при инициализации TwitchAPI: " + ex.Message, ex);
            }
        }

        public async Task<string> GetChannelInfoAsync(string username)
        {
            string apiUrl = $"https://api.twitch.tv/helix/users?login={username}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientId);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(jsonResponse);

                    var userInfo = data["data"]?[0];
                    if (userInfo != null)
                    {
                        string displayName = userInfo["display_name"]?.ToString();
                        string description = userInfo["description"]?.ToString();

                        return $"Канал: {displayName}\nОписание: {description}\n";
                    }
                }
                else
                {
                    return $"Ошибка: {response.StatusCode}";
                }
            }

            return "Не удалось получить данные.";
        }

        public async Task<string> GetStreamCategoryAsync(string username, string width = "1920", string height = "1080")
        {
            string streamApiUrl = $"https://api.twitch.tv/helix/streams?user_login={username}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientId);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

                // Запрос информации о стриме
                var response = await client.GetAsync(streamApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Ошибка: {response.StatusCode}";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var streamData = JObject.Parse(jsonResponse)["data"]?.FirstOrDefault();

                // Если стример оффлайн, выводим информацию о категории
                if (streamData == null)
                {
                    return "Стрим не найден или оффлайн. Попробуем получить категорию из профиля...";
                }

                string gameId = streamData["game_id"]?.ToString();
                if (string.IsNullOrEmpty(gameId))
                {
                    return "Категория не указана.";
                }

                // Запрос информации о категории по game_id
                string gameApiUrl = $"https://api.twitch.tv/helix/games?id={gameId}";
                var gameResponse = await client.GetAsync(gameApiUrl);
                if (!gameResponse.IsSuccessStatusCode)
                {
                    return $"Ошибка при получении категории: {gameResponse.StatusCode}";
                }

                var gameJsonResponse = await gameResponse.Content.ReadAsStringAsync();
                var gameData = JObject.Parse(gameJsonResponse)["data"]?.FirstOrDefault();

                if (gameData != null)
                {
                    string gameName = gameData["name"]?.ToString();
                    string categoryImageUrl = gameData["box_art_url"]?.ToString();

                    // Если есть ссылка на изображение категории, заменяем параметры на конкретные размеры
                    if (!string.IsNullOrEmpty(categoryImageUrl))
                    {
                        categoryImageUrl = categoryImageUrl.Replace("{width}", width).Replace("{height}", height);
                        return $"Категория: {gameName}";
                    }

                    return $"Категория: {gameName}, Изображение категории не найдено.";
                }

                return "Категория не найдена.";
            }
        }


        public async Task<string> GetChannelIdAsync(string username)
        {
            string apiUrl = $"https://api.twitch.tv/helix/users?login={username}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientId);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return $"Ошибка: {response.StatusCode}, Ответ: {errorResponse}";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonResponse);

                // Выводим весь ответ для диагностики
                Console.WriteLine(jsonResponse);

                string channelId = data["data"]?[0]?["id"]?.ToString();
                return channelId ?? "ID не найден.";
            }
        }

        public async Task<string> GetUserAvatarAsync(string username)
        {
            string apiUrl = $"https://api.twitch.tv/helix/users?login={username}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientId);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(jsonResponse)["data"]?[0];

                    if (data != null)
                    {
                        string avatarUrl = data["profile_image_url"]?.ToString();
                        return !string.IsNullOrEmpty(avatarUrl)
                            ? $"{avatarUrl}"
                            : "Аватар не найден.";
                    }
                }
                else
                {
                    return $"Ошибка: {response.StatusCode}";
                }
            }

            return "Не удалось получить данные пользователя.";
        }

        public async Task<string> GetStreamCategoryImageAsync(string username)
        {
            string apiUrl = $"https://api.twitch.tv/helix/streams?user_login={username}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientId);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(jsonResponse);

                    var stream = data["data"]?.FirstOrDefault();
                    if (stream != null)
                    {
                        var gameName = stream["game_name"]?.ToString();
                        var categoryImageUrl = stream["thumbnail_url"]?.ToString();

                        Console.WriteLine($"Category: {gameName}");
                        Console.WriteLine($"Category Image URL: {categoryImageUrl}");

                        return categoryImageUrl;
                    }
                }
            }

            return null;
        }
    }
}

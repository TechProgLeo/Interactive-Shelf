using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers; // System.Timers for Timer class
using Microsoft.Maui.Controls;

namespace IShelf
{
    public partial class MainPage : ContentPage
    {
        private readonly List<string> allTeas = new List<string>
        {
            "bio_ayurvedische_gewuerze", // Previously "Bio Ayurvedische Gewuerze"
            "bio_fruechtetee",           // Previously "Bio Fruechtetee"
            "bio_gruener_tee",           // Previously "Bio Gruener Tee"
            "bio_heimische_gartenfruechte", // Previously "Bio Heimische Gartenfruechte"
            "bio_kraeutertee",           // Previously "Bio Kraeutertee"
            "darjeeling",                // Previously "Darjeeling"
            "earl_grey",                 // Previously "Earl Grey"
            "gruener_tee_mango_zitrone", // Previously "Gruener Tee Mango Zitrone"
            "gruener_tee_nana_minze",    // Previously "Gruener Tee Nana Minze"
            "tuerkischer_apfel"          // Previously "Tuerkischer Apfel"
        };

        private readonly List<string> esp32Ips = new List<string>
        {
            "192.168.160.80", "192.168.160.93", "192.168.160.112", "192.168.160.113"
        };

        private readonly Dictionary<string, string> teaToProductMap = new Dictionary<string, string>
        {
            { "bio_ayurvedische_gewuerze", "product1" },
            { "bio_fruechtetee", "product2" },
            { "bio_gruener_tee", "product3" },
            { "bio_heimische_gartenfruechte", "product4" },
            { "bio_kraeutertee", "product5" },
            { "darjeeling", "product6" },
            { "earl_grey", "product7" },
            { "gruener_tee_mango_zitrone", "product8" },
            { "gruener_tee_nana_minze", "product9" },
            { "tuerkischer_apfel", "product10" }
        };

        private System.Timers.Timer searchTimer;

        public MainPage()
        {
            InitializeComponent();
            searchTimer = new System.Timers.Timer(300); // Set the delay time to 300 milliseconds
            searchTimer.Elapsed += OnSearchTimerElapsed;
        }

        // Navigation to SettingsPage
        private async void OnSettingsButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        private async void OnImageButtonClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button)
            {
                // Extract the image source string (path or URI)
                string imageSource = button.Source?.ToString() ?? "Unknown Product";

                // Remove the "File:" prefix if present and extract the image filename without path or extension
                string cleanedSource = imageSource.Replace("File: ", string.Empty);
                string selectedProduct = System.IO.Path.GetFileNameWithoutExtension(cleanedSource);

                // Check if the product exists in the teaToProductMap
                if (teaToProductMap.ContainsKey(selectedProduct))
                {
                    string product = teaToProductMap[selectedProduct];

                    // Send the command to the ESP32 device
                    await SendCommandToESP(product);
                }
                else
                {
                    // Handle case where the product is not found in the map
                    await DisplayAlert("Error", $"No matching product found for image: {selectedProduct}", "OK");
                }
            }
        }

        private async void OnBioTeesButtonClicked(object sender, EventArgs e)
        {
            string[] bioButtons = {
                "bio_ayurvedische_gewuerze", "bio_fruechtetee", "bio_gruener_tee",
                "bio_heimische_gartenfruechte", "bio_kraeutertee", "darjeeling"
            };

            foreach (var buttonSource in bioButtons)
            {
                await SendCommandToESP(teaToProductMap[buttonSource]);
            }
        }

        private async void OnGreenTeesButtonClicked(object sender, EventArgs e)
        {
            string[] greenButtons = {
                "bio_gruener_tee", "gruener_tee_mango_zitrone", "gruener_tee_nana_minze"
            };

            foreach (var buttonSource in greenButtons)
            {
                await SendCommandToESP(teaToProductMap[buttonSource]);
            }
        }

        // Search functionality with debounce
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop(); // Stop the timer when the text changes
            searchTimer.Start(); // Start it again to wait for more input
        }

        private void OnSearchTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            // Stop the timer to prevent re-entrance
            searchTimer.Stop();

            // Invoke the filtering method on the UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                string searchText = searchEntry.Text?.ToLower() ?? "";
                var filteredTeas = allTeas.Where(tea => tea.ToLower().Contains(searchText)).ToList();

                // Update the ListView with filtered teas
                teaListView.ItemsSource = filteredTeas;
                teaListView.IsVisible = filteredTeas.Any(); // Show the ListView if there are results
            });
        }

        private async void OnTeaItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is string selectedTea)
            {
                await SendCommandToESP(teaToProductMap[selectedTea]); // Send command when tea item is tapped
            }

            // Deselect the item after tapping
            teaListView.SelectedItem = null; // Deselect the item after tap
        }

        private async Task SendCommandToESP(string product)
        {
            using (HttpClient client = new HttpClient())
            {
                foreach (var ip in esp32Ips)
                {
                    try
                    {
                        // Construct the URL
                        string url = $"http://{ip}/lightup?product={product}";

                        // Send the GET request
                        var response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            // Command sent successfully
                        }
                        else
                        {
                            // Handle the failure case if needed
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // Handle specific HTTP exceptions if needed
                    }
                    catch (Exception)
                    {
                        // Handle all other exceptions if needed
                    }
                }
            }
        }
    }
}

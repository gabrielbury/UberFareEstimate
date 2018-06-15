using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BitHead.Uber.RazorPages.Pages
{
    public class HomeModel : PageModel
    {
        IConfiguration appSettings;
        public HomeModel([FromServices]IConfiguration config)
        {
            appSettings = config;
        }
        [BindProperty]
        public Models.Uber UberAddress { get; set; }

        [BindProperty]
        public IEnumerable<Models.Estimate> FareEstimate { get; set; } = new List<Models.Estimate>();

        [BindProperty]
        public string UrlParameters { get; set; } = string.Empty;

        public void OnGet()
        {

        }

        public async Task OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                UrlParameters += "origin=" + UberAddress.From;
                UrlParameters += "&destination=" + UberAddress.To;

                var latLongOrigem = await GetCoordinates(UberAddress.From);
                var latLongDestino = await GetCoordinates(UberAddress.To);

                await GetUber(latLongOrigem.Item1, latLongOrigem.Item2, latLongDestino.Item1, latLongDestino.Item2);
            }
        }

        async Task<ValueTuple<string, string>> GetCoordinates(string address)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", appSettings.GetSection("UberConfig:ServerToken").Value);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.AcceptLanguage.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("pt_BR"));

                var url = appSettings.GetSection("GoogleConfig:BaseUrl").Value
                    + address
                    + $"&key={appSettings.GetSection("GoogleConfig:Token").Value}";

                var response = await client.GetAsync(url);
                var conteudo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                return new ValueTuple<string, string>(conteudo.results[0].geometry.location.lat.ToString(), conteudo.results[0].geometry.location.lng.ToString());
            }
        }

        async Task GetUber(string startLatitude, string startLongitude, string endLatitude, string endLongitude)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", appSettings.GetSection("UberConfig:ServerToken").Value);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.AcceptLanguage.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("pt_BR"));

                var url = appSettings.GetSection("UberConfig:ApiBaseUrl").Value + "estimates/price?"
                    + $"start_latitude={startLatitude}"
                    + $"&start_longitude={startLongitude}"
                    + $"&end_latitude={endLatitude}"
                    + $"&end_longitude={endLongitude}";

                var response = await client.GetAsync(url);
                var conteudo = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                List<Models.Estimate> cotacao = new List<Models.Estimate>();
                foreach (var item in conteudo.prices)
                {
                    cotacao.Add(new Models.Estimate
                    {
                        PriceRange = item.estimate,
                        Title = item.localized_display_name
                    });
                }
                FareEstimate = cotacao;
            }
        }
    }
}
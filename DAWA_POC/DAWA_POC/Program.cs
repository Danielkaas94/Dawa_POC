using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace DAWA_POC
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpClient client = new HttpClient();
            client.Timeout = new TimeSpan(0, 20, 0);
            client.BaseAddress = new Uri("http://dawa.aws.dk/");

            HentAdresse(client, "0a3f50a0-73bf-32b8-e044-0003ba298018"); // Test - Id-søgning
            //HentAdresse(client, "0255b942-f3ac-4969-a963-d2c4ed9ab943");
            HentAdresser(client, "vejnavn=Rødkildevej&postnr=2400"); // Test - Adressesøgning

            //HentAdresserStreaming(client, "kommunekode=0101");

            //AutoCompleteVejnavne(client, "q=rødkildevej");
            //AutoCompleteAdresser(client, "q=rødkildevej");

            DataWash(client, "Rante mester vej 8, 2400 København NV"); // Test - Datavask
        }




        private static void HentAdresse(HttpClient client, string id)
        {
            try
            {
                string url = "adresser/" + id;
                Console.WriteLine("GET " + url);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                dynamic adresse = JValue.Parse(responseBody);
                ColorMessage(FormatAdresse(adresse), color:ConsoleColor.Green);
            }
            catch (HttpRequestException fail)
            {
                ColorMessage(fail.Message);
            }
        }
        private static void HentAdresser(HttpClient client, string query)
        {
            try
            {
                string url = "adresser/" + (query.Length == 0 ? "" : "?") + query;
                Console.WriteLine("GET " + url);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                dynamic adresser = JArray.Parse(responseBody);

                int antal = 0;
                foreach (dynamic adresse in adresser)
                {
                    Console.WriteLine(FormatAdresse(adresse));
                    antal++;
                }
                Console.WriteLine($"{antal} adresser");
            }
            catch (HttpRequestException fail)
            {
                ColorMessage(fail.Message);
            }

        }
        private static void HentAdresserStreaming(HttpClient client, string query)
        {
            try
            {
                string url = "adresser" + (query.Length == 0 ? "" : "?") + query;
                Console.WriteLine("GET " + url);
                var stream = client.GetStreamAsync(url).Result;
                stream.ReadTimeout = 20 * 60 * 60 * 1000; // 20 minutter
                var streamReader = new StreamReader(stream);
                JsonTextReader reader = new JsonTextReader(streamReader);

                int antal = 0;
                using (reader)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            dynamic adresse = JObject.Load(reader);
                            Console.WriteLine($" {antal.ToString()}  {FormatAdresse(adresse)} ");
                            antal++;
                        }
                    }
                }

            }
            catch (HttpRequestException fail)
            {
                ColorMessage(fail.Message);
            }


        }
        private static void AutoCompleteVejnavne(HttpClient client, string queury)
        {
            try
            {
                string url = "vejnavne/autocomplete" + (queury.Length == 0 ? "" : "?") + queury;
                Console.WriteLine("GET " + url);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                dynamic vejnavne = JArray.Parse(responseBody);

                int antal = 0;
                foreach (dynamic vejnavn in vejnavne)
                {
                    Console.WriteLine(formatvejnavneAutoComplete(vejnavn));
                    antal++;
                }
            }
            catch (HttpRequestException fail)
            {
                ColorMessage(fail.Message);
            }
        }
        private static void AutoCompleteAdresser(HttpClient client, string query)
        {
            try
            {
                string url = "adresser/autocomplete" + (query.Length == 0 ? "" : "?") + query;
                Console.WriteLine("GET " + url);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string resonseBody = response.Content.ReadAsStringAsync().Result;
                dynamic adresser = JArray.Parse(resonseBody);

                int antal = 0;
                foreach (dynamic adresse in adresser)
                {
                    Console.WriteLine(FormatAdresseAutocomplete(adresse));
                    antal++;
                }
                    Console.WriteLine($" {antal} adresser");
            }
            catch (HttpRequestException fail)
            {
                ColorMessage(fail.Message);
            }
        }
        private static void DataWash(HttpClient client, string query)
        {
            try
            {
                string url = "datavask/adgangsadresser/" + (query.Length == 0 ? "" : "?") + "betegnelse=" + query;
                Console.WriteLine("Get " + url);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;


                responseBody = responseBody.Substring(38);
                responseBody = responseBody.TrimEnd('}');
                responseBody = responseBody.TrimEnd(']', '\n');

                Console.WriteLine(responseBody);
                //Console.ReadLine();

                dynamic adresser = JValue.Parse(responseBody);
                Console.WriteLine(adresser);

                ColorMessage("Formatting JSON Object", ConsoleColor.Green);

                Console.WriteLine(FormatAdresseDataWash(adresser));

            }
            catch (HttpRequestException fail)
            {
                ColorMessage(fail.Message);
            }

        }




        #region StringFormats

        private static string FormatAdresse(dynamic adresse)
        {
            return string.Format($"{adresse.adgangsadresse.vejstykke.navn} {adresse.adgangsadresse.husnr} , {adresse.etage} {adresse.dør} {adresse.adgangsadresse.postnummer.nr} {adresse.adgangsadresse.postnummer.navn} , {adresse.adgangsadresse.politikreds.navn} , {adresse.adgangsadresse.region.navn}");
        }

        private static string formatvejnavneAutoComplete(dynamic vejnavn)
        {
            return string.Format($" {vejnavn.tekst} {vejnavn.vejnavn.href}");
        }

        private static string FormatAdresseAutocomplete(dynamic adresse)
        {
            return string.Format($" {adresse.tekst}: {adresse.adresse.href} ");
        }
        private static string FormatAdresseDataWash(dynamic DWadresse)
        {
            return string.Format($"{DWadresse.adresse.vejnavn} {DWadresse.adresse.husnr} {DWadresse.adresse.postnr} {DWadresse.adresse.postnrnavn} - {DWadresse.vaskeresultat.parsetadresse.vejnavn}, {DWadresse.vaskeresultat.parsetadresse.husnr} {DWadresse.vaskeresultat.parsetadresse.postnr} {DWadresse.vaskeresultat.parsetadresse.postnrnavn} ");
        }


        #endregion

        /// <summary>
        /// string message with a color of your choice. Red is default
        /// </summary>
        /// <param name="message">Your message, you want to display</param>
        /// <param name="color">Red is default</param>
        private static void ColorMessage(string message, ConsoleColor color = ConsoleColor.Red)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("Message : {0}", message);
            Console.ResetColor();
        }
    }
}

using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace LoadTests
{
    public class ModsLoadTest
    {
        private static string BaseUrl = "http://localhost:5010"; // Ajustez selon votre configuration
        private static string AuthToken = string.Empty;
        
        public static void Main(string[] args)
        {
            // Obtenir le token d'authentification avant de démarrer les tests
            GetAuthToken().Wait();
            
            // Définir les scénarios de test
            var searchModsScenario = BuildSearchModsScenario();
            var getModDetailsScenario = BuildGetModDetailsScenario();
            var downloadModScenario = BuildDownloadModScenario();
            
            // Exécuter la suite de tests de charge
            NBomberRunner
                .RegisterScenarios(searchModsScenario, getModDetailsScenario, downloadModScenario)
                .WithReportFileName($"mods_load_test_{DateTime.Now:yyyyMMdd_HHmmss}")
                .WithReportFormats(ReportFormat.Txt, ReportFormat.Csv, ReportFormat.Html)
                .Run();
        }
        
        private static async Task GetAuthToken()
        {
            using (var client = new HttpClient())
            {
                var loginRequest = new
                {
                    Email = "test@example.com",
                    Password = "Password123!"
                };
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(loginRequest),
                    Encoding.UTF8,
                    "application/json");
                    
                var response = await client.PostAsync($"{BaseUrl}/api/auth/login", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                    AuthToken = responseObject.data.token;
                    Console.WriteLine("Authentication successful");
                }
                else
                {
                    Console.WriteLine($"Authentication failed: {response.StatusCode}");
                }
            }
        }
        
        private static Scenario BuildSearchModsScenario()
        {
            var searchStep = Step.Create("search_mods", async context =>
            {
                var query = $"?searchTerm=mod&gameId=game1&pageNumber={context.ScenarioInfo.ThreadNumber % 5 + 1}&pageSize=10";
                
                var request = Http.CreateRequest("GET", $"{BaseUrl}/api/mods/search{query}")
                    .WithHeader("Authorization", $"Bearer {AuthToken}")
                    .WithHeader("Accept", "application/json");
                    
                var response = await Http.Send(request, context);
                
                return response;
            });
            
            return ScenarioBuilder
                .CreateScenario("Search Mods Scenario", searchStep)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(
                    Simulation.InjectPerSec(rate: 50, during: TimeSpan.FromSeconds(30)),
                    Simulation.Pause(during: TimeSpan.FromSeconds(5)),
                    Simulation.RampConstant(copies: 100, during: TimeSpan.FromSeconds(20))
                );
        }
        
        private static Scenario BuildGetModDetailsScenario()
        {
            // Liste d'IDs de mods pour le test (exemple)
            var modIds = new List<string>
            {
                "60d21b4667d1d87945a1fd09",
                "60d21b4667d1d87945a1fd0a",
                "60d21b4667d1d87945a1fd0b",
                "60d21b4667d1d87945a1fd0c",
                "60d21b4667d1d87945a1fd0d"
            };
            
            var getModStep = Step.Create("get_mod_details", async context =>
            {
                var modId = modIds[context.ScenarioInfo.ThreadNumber % modIds.Count];
                
                var request = Http.CreateRequest("GET", $"{BaseUrl}/api/mods/{modId}")
                    .WithHeader("Authorization", $"Bearer {AuthToken}")
                    .WithHeader("Accept", "application/json");
                    
                var response = await Http.Send(request, context);
                
                return response;
            });
            
            return ScenarioBuilder
                .CreateScenario("Get Mod Details Scenario", getModStep)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(
                    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)),
                    Simulation.Pause(during: TimeSpan.FromSeconds(5)),
                    Simulation.RampConstant(copies: 200, during: TimeSpan.FromSeconds(20))
                );
        }
        
        private static Scenario BuildDownloadModScenario()
        {
            // Liste d'IDs de mods pour le test (exemple)
            var modIds = new List<string>
            {
                "60d21b4667d1d87945a1fd09",
                "60d21b4667d1d87945a1fd0a",
                "60d21b4667d1d87945a1fd0b"
            };
            
            var downloadStep = Step.Create("download_mod", async context =>
            {
                var modId = modIds[context.ScenarioInfo.ThreadNumber % modIds.Count];
                
                var request = Http.CreateRequest("GET", $"{BaseUrl}/api/mods/{modId}/download")
                    .WithHeader("Authorization", $"Bearer {AuthToken}")
                    .WithHeader("Accept", "application/octet-stream");
                    
                var response = await Http.Send(request, context);
                
                return response;
            });
            
            return ScenarioBuilder
                .CreateScenario("Download Mod Scenario", downloadStep)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(
                    // Moins d'utilisateurs simultanés pour le téléchargement car c'est une opération plus lourde
                    Simulation.InjectPerSec(rate: 20, during: TimeSpan.FromSeconds(30)),
                    Simulation.Pause(during: TimeSpan.FromSeconds(5)),
                    Simulation.RampConstant(copies: 50, during: TimeSpan.FromSeconds(20))
                );
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Linq;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;

namespace M365.DevBootcamp2019.Marvel
{
    public static class Heroes
    {
        [FunctionName("Claims")]
        public static IActionResult GetClaims(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ExecutionContext executionContext,
            ILogger log)
        {
            log.LogInformation("Getting user claims!");

            var user = req.HttpContext.User;
            var identity = user.Identity as ClaimsIdentity;

            var claims = from c in identity?.Claims
                         select new ClaimModel
                         {
                             Subject = c.Subject.Name,
                             Type = c.Type,
                             Value = c.Value
                         };

            return new OkObjectResult(claims);
        }

        [FunctionName("Heroes")]
        public static async Task<IActionResult> GetHeroes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ExecutionContext executionContext,
            ILogger log)
        {
            log.LogInformation("Getting super heroes list!");

            var heroesFilePath = $"{executionContext.FunctionAppDirectory}/heroes.json";

            var requestBody = await new StreamReader(heroesFilePath).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            return new OkObjectResult(data);
        }

        [FunctionName("GetMeOnGraph")]
        public static async Task<IActionResult> GetMeOnGraph([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, ILogger log)
        {
            try
            {
                string ClientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
                string ClientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);

                var tenantId = req.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process); //For some reason, in localhost only one claim is returned
                }

                string authority = $"https://login.microsoftonline.com/{tenantId}";

                var userImpersonationAccessToken = req.Headers["Authorization"].ToString().Replace("Bearer ", "");
                log.LogInformation("AccessToken: {0}", userImpersonationAccessToken);

                var app = ConfidentialClientApplicationBuilder.Create(ClientId)
                   .WithClientSecret(ClientSecret)
                   .WithAuthority(authority)
                   .Build();

                UserAssertion userAssertion = new UserAssertion(userImpersonationAccessToken);

                var authResult = await app.AcquireTokenOnBehalfOf(
                    new string[] { "https://graph.microsoft.com/.default" }, 
                    userAssertion).ExecuteAsync();

                var graphAccessToken = authResult.AccessToken;
                log.LogInformation("Token OnBehalfOf: {0}", graphAccessToken);

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me");

                var response = await httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                var user = JsonConvert.DeserializeObject<GraphUser>(content);

                return new OkObjectResult(user);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Something went wrong");
                throw;
            }
        }

        [FunctionName("GraphMeBinding")]
        public static async Task<IActionResult> GraphMeBinding(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            [Token(
                Identity = TokenIdentityMode.UserFromRequest,
                IdentityProvider = "AAD",
                Resource = "https://graph.microsoft.com")] string graphToken, 
            ILogger log)
        {
            if (string.IsNullOrEmpty(graphToken))
            {
                throw new ArgumentNullException("Token", "Graph Token is empty, ensure you´re running the Function from the cloud and the Graph Binding has been configured");
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/me");

            var response = await client.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            var user = JsonConvert.DeserializeObject<GraphUser>(content);

            return new OkObjectResult(user);
        }
    }

    public class ClaimModel
    {
        public string Subject { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class GraphUser
    {
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string JobTitle { get; set; }
        public string Mail { get; set; }
        public string MobilePhone { get; set; }
        public object OfficeLocation { get; set; }
        public string PreferredLanguage { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
        public string Id { get; set; }
    }
}

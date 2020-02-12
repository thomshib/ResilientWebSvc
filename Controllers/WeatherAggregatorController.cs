using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace ResilientWebSvc.Controllers
{

[ApiController]
[Route("[controller]")]
public class WeatherAggregatorController:ControllerBase{

        private readonly ILogger<WeatherAggregatorController> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;

        public WeatherAggregatorController(ILogger<WeatherAggregatorController> logger)
        {
            _logger = logger;

            //immediate retry
            //_httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>( r => !r.IsSuccessStatusCode)
            //    .RetryAsync(3);

            //wait and retry
            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>( r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,retryAttempt) / 2));
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var httpClient = GetHttpClient();
            var requestEndPoint = $"/WeatherForecast";

            //var response = await httpClient.GetAsync(requestEndPoint);
            var response = await _httpRetryPolicy.ExecuteAsync(() => httpClient.GetAsync(requestEndPoint));

            if(response.IsSuccessStatusCode){
                IEnumerable<WeatherForecast> result = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
                    await response.Content.ReadAsStringAsync()
                );

                return Ok(result);

            }
            return StatusCode((int) response.StatusCode,response.Content.ReadAsStringAsync());

        }

        private HttpClient GetHttpClient(){

            //ByPass SSL validation checks
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage,cert,certChain,policyErrors) => {
                return true;
            };


            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://localhost:5001/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;

        }


}

}
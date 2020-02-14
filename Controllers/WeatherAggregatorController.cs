using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;

namespace ResilientWebSvc.Controllers
{

[ApiController]
[Route("[controller]")]
public class WeatherAggregatorController:ControllerBase{

        private readonly ILogger<WeatherAggregatorController> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;
        private readonly AsyncFallbackPolicy<HttpResponseMessage> _httpRequestFallbackPolicy;
        private readonly AsyncTimeoutPolicy _timeoutPolicy;
        
        private HttpClient _httpClient;



#region combinedFalloutRetry&Timeout policy

        public WeatherAggregatorController(ILogger<WeatherAggregatorController> logger)
        {
            _logger = logger;
            var defaultResult = new List<WeatherForecast>(){};

            

            _timeoutPolicy = Policy.TimeoutAsync(1); //timeout after 1 sec

            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<TimeoutRejectedException>()
            .RetryAsync(3);


            _httpRequestFallbackPolicy = 
            Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<TimeoutRejectedException>()
            .FallbackAsync(
                new HttpResponseMessage(HttpStatusCode.OK){                        

                    Content = new ObjectContent(defaultResult.GetType(),defaultResult,new JsonMediaTypeFormatter() )

                }

            );

        }

       


   
        [HttpGet]
        public async Task<IActionResult> Get()
        {
             _httpClient = GetHttpClient();
            var requestEndPoint = $"/WeatherForecast";

            
            var response = await  _httpRequestFallbackPolicy.ExecuteAsync(() =>
                _httpRetryPolicy.ExecuteAsync(()=>
                    _timeoutPolicy.ExecuteAsync(
                        async token => await  _httpClient.GetAsync(requestEndPoint,token), CancellationToken.None
                    )
                
                )

            );
            

            if(response.IsSuccessStatusCode){
                IEnumerable<WeatherForecast> result = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
                    await response.Content.ReadAsStringAsync()
                );

                return Ok(result);

            }
            return StatusCode((int) response.StatusCode,response.Content.ReadAsStringAsync());

        }

         private HttpClient GetHttpClientInvalidURL(){

            //ByPass SSL validation checks
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage,cert,certChain,policyErrors) => {
                return true;
            };


            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://10.101.10.255/someUnreachableEndpoint");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;

        }




#endregion



#region HttpClientTimeout

/*         public WeatherAggregatorController(ILogger<WeatherAggregatorController> logger)
        {
            _logger = logger;
            var defaultResult = new List<WeatherForecast>(){};

            

            
            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .RetryAsync(3,onRetry:OnRetryHandler);

           



        }

        private void OnRetryHandler(DelegateResult<HttpResponseMessage> result, int retryCount)
        {
            if(result.Exception is HttpRequestException){
               
                    Console.WriteLine("Operation timed out");
                
            }
        }


 
        [HttpGet]
        public async Task<IActionResult> Get()
        {
             _httpClient = GetHttpClientInvalidURL();
            var requestEndPoint = $"/WeatherForecast";

            
            var response = await  _httpRetryPolicy.ExecuteAsync(() => _httpClient.GetAsync(requestEndPoint));

            

            if(response.IsSuccessStatusCode){
                IEnumerable<WeatherForecast> result = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
                    await response.Content.ReadAsStringAsync()
                );

                return Ok(result);

            }
            return StatusCode((int) response.StatusCode,response.Content.ReadAsStringAsync());

        }

         private HttpClient GetHttpClientInvalidURL(){

            //ByPass SSL validation checks
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage,cert,certChain,policyErrors) => {
                return true;
            };


            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://10.101.10.255/someUnreachableEndpoint");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;

        }



 */
#endregion

#region Fallback
/*     public WeatherAggregatorController(ILogger<WeatherAggregatorController> logger)
    {
        _logger = logger;
        var defaultResult = new List<WeatherForecast>(){};

        

        //retry with fallback 
        _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode).RetryAsync(3);

        _httpRequestFallbackPolicy = 
            Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.InternalServerError)
            .FallbackAsync(
                new HttpResponseMessage(HttpStatusCode.OK){                        

                    Content = new ObjectContent(defaultResult.GetType(),defaultResult,new JsonMediaTypeFormatter() )

                }

            );



    }


        //retry with Fallback
    [HttpGet]
    public async Task<IActionResult> Get()
    {
            _httpClient = GetHttpClient();
        var requestEndPoint = $"/WeatherForecast";

        
        var response = await _httpRequestFallbackPolicy.ExecuteAsync( () =>
        
            _httpRetryPolicy.ExecuteAsync(() => _httpClient.GetAsync(requestEndPoint))

        );

        if(response.IsSuccessStatusCode){
            IEnumerable<WeatherForecast> result = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
                await response.Content.ReadAsStringAsync()
            );

            return Ok(result);

        }
        return StatusCode((int) response.StatusCode,response.Content.ReadAsStringAsync());

    }

 */
 #endregion


#region UnAuthorized Retry

       /* //retry for Auth
        public WeatherAggregatorController(ILogger<WeatherAggregatorController> logger)
        {
            _logger = logger;
            //unauthorized retry
            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>( r => !r.IsSuccessStatusCode)
                .RetryAsync(3, onRetry: (httpResponseMessage,i) =>
                {
                    if (httpResponseMessage.Result.StatusCode == HttpStatusCode.Unauthorized)
                        PerformReAuthorization();                    
                        
                    }
                ); 

            



        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
             _httpClient = GetHttpClient("BadAuthCode");
            var requestEndPoint = $"/WeatherForecast";

            //var response = await httpClient.GetAsync(requestEndPoint);
            var response = await _httpRetryPolicy.ExecuteAsync(() => _httpClient.GetAsync(requestEndPoint));

            if(response.IsSuccessStatusCode){
                IEnumerable<WeatherForecast> result = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
                    await response.Content.ReadAsStringAsync()
                );

                return Ok(result);

            }
            return StatusCode((int) response.StatusCode,response.Content.ReadAsStringAsync());

        }

        private void PerformReAuthorization()
        {
          _httpClient = GetHttpClient("GoodAuthCode");
        }

        private HttpClient GetHttpClient(string authCode)
        {
            //ByPass SSL validation checks
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage,cert,certChain,policyErrors) => {
                return true;
            };

            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri("http://localhost") , new Cookie("Auth", authCode));
            handler.CookieContainer = cookieContainer;


            var httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri("https://localhost:5001/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

 */

 #endregion
        
        
#region BasicRetry&wait 
   /*
        
        //  //basic retry and wait

        public WeatherAggregatorController(ILogger<WeatherAggregatorController> logger)
        {
            _logger = logger;

            //immediate retry
            //_httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>( r => !r.IsSuccessStatusCode)
            //    .RetryAsync(3);

            wait and retry
            _httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>( r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,retryAttempt) / 2));



        }


        // [HttpGet]
        // public async Task<IActionResult> Get()
        // {
        //     var httpClient = GetHttpClient();
        //     var requestEndPoint = $"/WeatherForecast";

        //     //var response = await httpClient.GetAsync(requestEndPoint);
        //     var response = await _httpRetryPolicy.ExecuteAsync(() => httpClient.GetAsync(requestEndPoint));

        //     if(response.IsSuccessStatusCode){
        //         IEnumerable<WeatherForecast> result = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
        //             await response.Content.ReadAsStringAsync()
        //         );

        //         return Ok(result);

        //     }
        //     return StatusCode((int) response.StatusCode,response.Content.ReadAsStringAsync());

        // }

        */
    #endregion

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
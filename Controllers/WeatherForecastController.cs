using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ResilientWebSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        private static int _requestCount = 0;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }


#region combinedFalloutRetry&Timeout policy
     [HttpGet]
         public async Task<IActionResult>  Get()
         {
             _requestCount++;
            
                  if(_requestCount % 6 != 0){
                        await Task.Delay(10000); //simulate delay for 10 secs
                  }
                 var rng = new Random();
                 var resultTask = Task.Run(() => {
                     return
                         Enumerable.Range(1, 5).Select(index => new WeatherForecast
                         {
                             Date = DateTime.Now.AddDays(index),
                             TemperatureC = rng.Next(-20, 55),
                             Summary = Summaries[rng.Next(Summaries.Length)]
                         })
                         .ToArray();
                 });

                 var response = await resultTask;
                 return Ok(response);

             }

          

#endregion


#region Fallback

     
       /*  [HttpGet]
        public async Task<IActionResult>  Get()
        {
            
            return await Task.Run(() => StatusCode((int) HttpStatusCode.InternalServerError,"Error"));
           
        } */
#endregion
        
    #region UnAuthorized Retry

        // //AuthFlow
        // [HttpGet]
        // public async Task<IActionResult>  Get()
        // {
            
        //     string authCode = Request.Cookies["Auth"];
            
        //     if(authCode == "GoodAuthCode"){
        //         var rng = new Random();

        //         var resultTask = Task.Run(() => {
        //             return
        //                 Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //                 {
        //                     Date = DateTime.Now.AddDays(index),
        //                     TemperatureC = rng.Next(-20, 55),
        //                     Summary = Summaries[rng.Next(Summaries.Length)]
        //                 })
        //                 .ToArray();
        //         });

        //         var response = await resultTask;
        //         return Ok(response);

        //     }

        //     return StatusCode((int) HttpStatusCode.Unauthorized,"Not Authorized");
        // }

    #endregion
       
    #region BasicRetry&wait
     //// basic flow
        // [HttpGet]
        // public async Task<IActionResult>  Get()
        // {
        //     _requestCount++;
            
        //     if(_requestCount % 4 == 0){
        //         var rng = new Random();

        //         var resultTask = Task.Run(() => {
        //             return
        //                 Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //                 {
        //                     Date = DateTime.Now.AddDays(index),
        //                     TemperatureC = rng.Next(-20, 55),
        //                     Summary = Summaries[rng.Next(Summaries.Length)]
        //                 })
        //                 .ToArray();
        //         });

        //         var response = await resultTask;
        //         return Ok(response);

        //     }

        //     return StatusCode((int) HttpStatusCode.InternalServerError,"Error");
           
        // }
    #endregion
    }
}

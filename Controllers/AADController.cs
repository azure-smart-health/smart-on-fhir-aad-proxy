using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SmartOnFHIR_AAD_Proxy.Controllers
{
    [Route("/")]
    [ApiController]
    public class AADController : ControllerBase
    {
        
        private IConfiguration _configuration;
        private string AadHostName { get; set; } 

        public AADController(IConfiguration config)
        {
            _configuration = config;

            if (String.IsNullOrEmpty(_configuration["AadHostName"]))
            {
                AadHostName = "login.microsoftonline.com";
            } else {
                AadHostName = _configuration["AadHostName"];
            }
        }

        //V1 Authorize Endpoint
        [HttpGet("{tenant}/oauth2/authorize")]
        [HttpGet("{tenant}/oauth2/v1.0/authorize")]
        public ActionResult GetV1(string tenant, 
                                [FromQuery]string response_type,
                                [FromQuery]string client_id,
                                [FromQuery]string redirect_uri,
                                [FromQuery]string launch,
                                [FromQuery]string scope,
                                [FromQuery]string state,
                                [FromQuery]string aud)
        {
            string newQueryString = $"resource={aud}&response_type={response_type}&redirect_uri={redirect_uri}&client_id={client_id}";

            if (!String.IsNullOrEmpty(launch)) {
                //TODO: Implement appropriate behavior
            }

            if (!String.IsNullOrEmpty(state)) {
                newQueryString += $"&state={state}";
            }

            return RedirectPermanent($"https://{AadHostName}/{tenant}/oauth2/authorize?{newQueryString}");
        }

        //V2 Authorize endpoint
        [HttpGet("{tenant}/oauth2/v2.0/authorize")]
        public ActionResult GetV2(string tenant, 
                                [FromQuery]string response_type,
                                [FromQuery]string client_id,
                                [FromQuery]string redirect_uri,
                                [FromQuery]string launch,
                                [FromQuery]string scope,
                                [FromQuery]string state,
                                [FromQuery]string aud)
        {
            string newQueryString = $"response_type={response_type}&redirect_uri={redirect_uri}&client_id={client_id}";

            if (!String.IsNullOrEmpty(launch)) {
                //TODO: Implement appropriate behavior
            }

            if (!String.IsNullOrEmpty(state)) {
                newQueryString += $"&state={state}";
            }

            if (!String.IsNullOrEmpty(scope)) {
                String[] scopes = scope.Split(' ');
                var scopeString = "";
                foreach (var s in scopes) {
                    var newScope = s.Replace("/","-");
                    scopeString += $"{aud}/{newScope} ";
                }
                newQueryString += $"&scope={scopeString}";
            }

            return RedirectPermanent($"https://{AadHostName}/{tenant}/oauth2/v2.0/authorize?{newQueryString}");
        }

        //V1 and V2 token endpoints
        [HttpPost("{tenant}/oauth2/token")]
        [HttpPost("{tenant}/oauth2/v1.0/token")]
        [HttpPost("{tenant}/oauth2/v2.0/token")]
        public async Task<ActionResult> Post(string tenant,
                                            [FromForm]string grant_type,
                                            [FromForm]string code,
                                            [FromForm]string redirect_uri,
                                            [FromForm]string client_id,
                                            [FromForm]string client_secret)
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri($"https://{AadHostName}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", grant_type),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirect_uri),
                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("client_secret", client_secret)
            });

            var response = await client.PostAsync($"{Request.Path}", content);
           
            return new ContentResult() {
                Content = await response.Content.ReadAsStringAsync(),
                StatusCode = (int)response.StatusCode,
                ContentType = "application/json"
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RatingsAPI
{
    public static class RatingsAPI
    {
        [FunctionName("CreateRating")]
        public static async Task<IActionResult> CreateRating(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "createrating")] HttpRequest req,
            [CosmosDB(
                databaseName: "ratings",
                collectionName: "icecream",
                ConnectionStringSetting = "COSMOS_SETTING")] IAsyncCollector<Ratings> ratings,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Ratings>(json);
            var outPut = new Ratings();

            string userId = data.userId;
            string productId = data.productId;
            int rating = data.rating;
            bool validuserId = ValidateIds("https://serverlessohuser.trafficmanager.net/api/GetUser", "?userId=" + userId);
            bool validproductId = ValidateIds("https://serverlessohproduct.trafficmanager.net/api/GetProduct", "?productId=" + productId);

            if (rating > 5 || rating < 0)
            {
                throw new System.IndexOutOfRangeException("Rating is not in range of 0 to 5");
            }
            if (!validuserId)
                throw new System.InvalidProgramException("Invalid user");
            if (!validproductId)
                throw new System.InvalidProgramException("Invalid product");

            outPut.id = Guid.NewGuid();
            outPut.userId = userId;
            outPut.productId = productId;
            outPut.timestamp = DateTime.UtcNow;
            outPut.userNotes = data.userNotes;
            outPut.locationName = data.locationName;
            outPut.rating = rating;

            await ratings.AddAsync(outPut);

            return new OkObjectResult(outPut);
        }
        public static bool ValidateIds(string url, string parameter)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(url);
            HttpResponseMessage response = client.GetAsync(parameter).Result;
            if (response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }
        public class Ratings
        {
            public Guid id { get; set; }
            public DateTime timestamp { get; set; }
            public string userId { get; set; }
            public string productId { get; set; }
            public string locationName { get; set; }
            public int rating { get; set; }
            public string userNotes { get; set; }
        }

        [FunctionName("GetRatings")]
        public static IActionResult GetRatings(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getratings/{userId}")] HttpRequest req,
            [CosmosDB(
                databaseName: "ratings",
                collectionName: "icecream",
                ConnectionStringSetting = "COSMOS_SETTING",
            SqlQuery = "select * from ratings r where r.userId = {userId}")] IEnumerable<Ratings> ratings,
            ILogger log)
        {
            List<Ratings> userRatings = new List<Ratings>();
            foreach (Ratings ra in ratings)
            {
                log.LogInformation(ra.productId);
                userRatings.Add(ra);
            }
            return new OkObjectResult(userRatings);
        }

        [FunctionName("GetRating")]
        public static IActionResult GetRating(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getrating/{ratingId}")] HttpRequest req,
            [CosmosDB(
                databaseName: "ratings",
                collectionName: "icecream",
                ConnectionStringSetting = "COSMOS_SETTING",
            SqlQuery = "select * from ratings r where r.id = {ratingId}")] IEnumerable<Ratings> ratings,
            ILogger log)
        {
            Ratings rating = null;
            foreach (Ratings ra in ratings)
            {
                log.LogInformation(ra.productId);
                rating = ra;
            }
            return new OkObjectResult(rating);
        }
    }
}

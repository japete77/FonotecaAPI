using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Middleware
{
    public class ErrorResponse
    {
        public ErrorResponse(int code, string userMessage, ExceptionDetails details, bool includeFullExceptionInfo)
        {
            Error = new ExceptionDescription
            {
                Code = code,
                UserMessage = userMessage
            };

            if (includeFullExceptionInfo)
            {
                Error.Details = details;
            }
        }

        public ExceptionDescription Error { get; set; }

        public static async Task<ErrorResponse> FromHttpResponseAsync(HttpResponseMessage httpResponse)
        {
            var bodyResponse = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ErrorResponse>(bodyResponse);
        }
    }

}

namespace BuildHelper.Workflows
{
    using System.Net.Http;
    using Google.Apis.Http;

    public class CustomClientFactory : IHttpClientFactory
    {
        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            var handler = new HttpClientHandler
            {
                // Xử lý SSL cross-platform
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var configurableHandler = new ConfigurableMessageHandler(handler)
            {
                ApplicationName = args.ApplicationName
            };

            return new ConfigurableHttpClient(configurableHandler);
        }
    }
}
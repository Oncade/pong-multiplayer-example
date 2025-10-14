using UnityEngine;


namespace Elements.Client
{
    using Api;

    public static class HelloClient
    {
        private static HelloApi helloApi;
        
        private const string ELEMENT_NAME = "example-element";

        public static HelloApi HelloApi {

            get
            { 
                if (helloApi == null)
                {
                    InitializeHelloApi();
                }

                return helloApi;
            }
        }

        private static void InitializeHelloApi()
        {
            if(ElementsClient.Default == null)
            {
                Debug.LogError("HelloApi cannot be initialized until ElementsClient is initialized");
                return;
            }

            //Application base path typically uses app/rest instead of api/rest
            var basePath = ElementsClient.Default.Api.Configuration.BasePath.Replace("api", "app");
            helloApi = new HelloApi($"{basePath}/{ELEMENT_NAME}");
            helloApi.ApiClient.SerializerSettings = ElementsClient.Default.Api.ApiClient.SerializerSettings;
        }
    }

}

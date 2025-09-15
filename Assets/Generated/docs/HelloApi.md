# Elements.Api.HelloApi

All URIs are relative to *http://localhost*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**SayHello**](HelloApi.md#sayhello) | **GET** /helloworld | Hello world probe |
| [**SayHelloWithAuth**](HelloApi.md#sayhellowithauth) | **GET** /helloworldwithauthentication | Hello world probe with login required |

<a id="sayhello"></a>
# **SayHello**
> string SayHello ()

Hello world probe

Returns a simple greeting

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Elements.Api;
using Elements.Client;
using Elements.Model;

namespace Example
{
    public class SayHelloExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new HelloApi(config);

            try
            {
                // Hello world probe
                string result = apiInstance.SayHello();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling HelloApi.SayHello: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SayHelloWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Hello world probe
    ApiResponse<string> response = apiInstance.SayHelloWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling HelloApi.SayHelloWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

**string**

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **0** | default response |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="sayhellowithauth"></a>
# **SayHelloWithAuth**
> string SayHelloWithAuth ()

Hello world probe with login required

Checks if the session token in the header corresponds to at least a USER level user.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using Elements.Api;
using Elements.Client;
using Elements.Model;

namespace Example
{
    public class SayHelloWithAuthExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "http://localhost";
            var apiInstance = new HelloApi(config);

            try
            {
                // Hello world probe with login required
                string result = apiInstance.SayHelloWithAuth();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling HelloApi.SayHelloWithAuth: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SayHelloWithAuthWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Hello world probe with login required
    ApiResponse<string> response = apiInstance.SayHelloWithAuthWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling HelloApi.SayHelloWithAuthWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

**string**

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **0** | default response |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)


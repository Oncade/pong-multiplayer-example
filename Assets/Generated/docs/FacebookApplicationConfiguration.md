# Elements.Model.FacebookApplicationConfiguration
Houses the various parameters required which allow communication with the Faceook API.  The Facebook API will 

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **string** | The database assigned ID for the application configuration. | [optional] 
**Category** | **string** | The category for the application configuration. | 
**UniqueIdentifier** | **string** | The application-configuration specific unique ID.  (Varies by ConfigurationCategory) | [optional] 
**Parent** | [**Application**](Application.md) |  | 
**ProductBundles** | [**List&lt;ProductBundle&gt;**](ProductBundle.md) | The list of product bundles that may be rewarded upon successful IAP transactions. | [optional] 
**ApplicationId** | **string** | The AppID as it appears in the Facebook Developer Console | 
**ApplicationSecret** | **string** | The App Secret as it appears in the Facebook Developer Console | 
**BuiltinApplicationPermissions** | **List&lt;string&gt;** | The set of built-in permissions connected clients will need to request. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


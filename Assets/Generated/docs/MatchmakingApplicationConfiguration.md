# Elements.Model.MatchmakingApplicationConfiguration
This configures the matchmaking system.  More specifically, this configures which scripts andmethods will be called when a successful match has been made.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **string** | The database assigned ID for the application configuration. | [optional] 
**Category** | **string** | The category for the application configuration. | 
**UniqueIdentifier** | **string** | The application-configuration specific unique ID.  (Varies by ConfigurationCategory) | [optional] 
**Parent** | [**Application**](Application.md) |  | 
**ProductBundles** | [**List&lt;ProductBundle&gt;**](ProductBundle.md) | The list of product bundles that may be rewarded upon successful IAP transactions. | [optional] 
**Scheme** | **string** | A user-specified unique identifier for the matching scheme.  It is possible to specify multiple schemes per application, but each must be uniquely named.  Each scheme allows for the specification of different scripts to handle the successful match.  When requesting matchmaking services clients will specify the scheme to be used. | 
**Algorithm** | **string** | Specifies the matching algorithm to use.  Algorithms are builtin and implemented by the API services.  Currently, only FIFO is supported. | 
**Success** | [**CallbackDefinition**](CallbackDefinition.md) |  | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


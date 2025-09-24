using System;
public static class ElementsProperties
{
    //It's generally best to keep this in an external config to help determine the environment
    public const string ELEMENTS_ROOT_URL = "http://localhost:8080/api/rest";

    //Must match the name of the Application that was created in Elements
    public const string ELEMENTS_APPLICATION_NAME = "Pong";

    public const string MATCHMAKING_CONFIGURATION = "default";
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System;
using System.IO;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using DigitalRuby.WeatherMaker;

public class WeatherSync : MonoBehaviour
{
    private string pcUserIP;

    [Header("Weather Data")]
    [SerializeField] string cityName;
    [SerializeField] string currentTemp;
    [SerializeField] float cloudinessLevel;
    [SerializeField] string precipitationType;
    [SerializeField] float visibility;

    [Header("Variables")]
    [SerializeField] private float latitude = 49.2827f;
    [SerializeField] private float longitude = 123.1207f;
    [SerializeField] private string APIToken = "56f51b13a444f57726fe8c184b2af580";
    [SerializeField] private string APITokenIP = "053726aa8ee2ea01ab03714166f5c927";

    [Header("References - Menu")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField latitudeInput;
    [SerializeField] private TMP_InputField longitudeInput;
    [SerializeField] private GameObject introScreenObj;
    [SerializeField] private GameObject weatherDataOverlay;

    [Header("References - Weather Screen")]
    [SerializeField] private TextMeshProUGUI cityText;
    [SerializeField] private TextMeshProUGUI tempText;

    private string openWeatherAPIURL = "https://api.openweathermap.org/data/2.5/weather?";

    public void InitializeLocationServices()
    {
        StartCoroutine(StartLocationService());
    }

    public void GetLocationByIPAddress()
    {
        pcUserIP = GetGlobalIPAddress();

        StartCoroutine(GetIPLocation(pcUserIP));
    }

    public void StartWeatherSimulator()
    {
        StartCoroutine(GetWeather(latitudeInput.text, longitudeInput.text));
    }

    private IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "Location services not enabled (or not on mobile)!";
            yield break;
        }

        Input.location.Start();

        statusText.text = Input.location.status.ToString();

#if UNITY_EDITOR
        int maxWaitEditor = 20;

        //Wait until Unity connects to the Unity Remote, while not connected, yield return null
        while (!UnityEditor.EditorApplication.isRemoteConnected && maxWaitEditor > 0)
        {
            yield return null;
            maxWaitEditor--;
        }

        if (maxWaitEditor <= 0)
        {
            statusText.text = "Timeout while initializing! (Editor)";
            yield break;
        }
#endif

        int maxWait = 20;

        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            statusText.text = "Timeout while initializing!";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Unable to determine location");
            statusText.text = "Unable to determine location!";
            yield break;
        }

        SetLatitudeAndLongitude(Input.location.lastData.latitude, Input.location.lastData.longitude);

        yield break;
    }

    private void SetLatitudeAndLongitude(float _latitude, float _longitude)
    {
        if (_latitude == 0 || _longitude == 0)
        {
            statusText.text = "No location returned!";
            return;
        }

        latitude = _latitude;
        longitude = _longitude;

        statusText.text = $"Latitude: {latitude}, Longitude: {longitude}";
        latitudeInput.text = latitude.ToString();
        longitudeInput.text = longitude.ToString();
    }

    IEnumerator GetWeather(string _latitude, string _longitude)
    {
        string url = openWeatherAPIURL + "lat=" + _latitude + "&lon=" + _longitude + "&appid=" + APIToken + "&units=metric";

        UnityWebRequest weatherRequest = UnityWebRequest.Get(url);

        Debug.Log(url);

        yield return weatherRequest.SendWebRequest();

        if (weatherRequest.result == UnityWebRequest.Result.ConnectionError || 
        weatherRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(weatherRequest.error);
        }
        else
        {
            Debug.Log($"Response code: {weatherRequest.responseCode}");
            Debug.Log($"Response handler: {weatherRequest.downloadHandler.text}");

            var weatherOutputJson = JSON.Parse(weatherRequest.downloadHandler.text);
            WeatherData weatherData = JsonUtility.FromJson<WeatherData>(weatherRequest.downloadHandler.text);

            Debug.Log($"{weatherData.name} weather, timezone: {weatherData.timezone}, {weatherData.weather[0].main}");

            cityName = weatherData.name.ToString();
            currentTemp = weatherOutputJson["main"]["temp"].ToString() + "°C";
            cloudinessLevel = weatherOutputJson["clouds"]["all"];
            precipitationType = weatherData.weather[0].main.ToString();
            visibility = weatherData.visibility; // max is 10k, if less than 1k show fog

            cloudinessLevel = 50;
            precipitationType = "Rain";

            cityText.text = cityName;
            tempText.text = currentTemp;

            weatherDataOverlay.SetActive(true);
            introScreenObj.SetActive(false);

            UpdateWeathermaker();
        }
    }

    private void UpdateWeathermaker()
    {
        // Precipitation
        if (precipitationType == "Rain")
        {
            WeatherMakerPrecipitationManagerScript.Instance.Precipitation = (WeatherMakerPrecipitationType.Rain);
            WeatherMakerPrecipitationManagerScript.Instance.PrecipitationIntensity = 0.3f;
        }
        else if (precipitationType == "Snow")
        {
            WeatherMakerPrecipitationManagerScript.Instance.Precipitation = (WeatherMakerPrecipitationType.Snow);
            WeatherMakerPrecipitationManagerScript.Instance.PrecipitationIntensity = 0.5f;
        }
        
        // Wind
        WeatherMakerWindScript.Instance.SetWindProfileAnimated(WeatherMakerScript.Instance.LoadResource<WeatherMakerWindProfileScript>("WeatherMakerWindProfile_MediumWind"), 0.0f, 5.0f);

        // Cloudiness
        if (cloudinessLevel >= 90)
        {
            WeatherMakerFullScreenCloudsScript.Instance.ShowCloudsAnimated(2, "WeatherMakerCloudProfile_Overcast");
        }
        else if (cloudinessLevel >= 70)
        {
            WeatherMakerFullScreenCloudsScript.Instance.ShowCloudsAnimated(2, "WeatherMakerCloudProfile_HeavyScattered");
        }
        else if (cloudinessLevel >= 50)
        {
            WeatherMakerFullScreenCloudsScript.Instance.ShowCloudsAnimated(2, "WeatherMakerCloudProfile_MediumScattered");
        }
        else if (cloudinessLevel >= 30)
        {
            WeatherMakerFullScreenCloudsScript.Instance.ShowCloudsAnimated(2, "WeatherMakerCloudProfile_PartlyCloudy");
        }
        else if (cloudinessLevel >= 10)
        {
            WeatherMakerFullScreenCloudsScript.Instance.ShowCloudsAnimated(2, "WeatherMakerCloudProfile_LightScattered");
        }
        else
        {
            WeatherMakerFullScreenCloudsScript.Instance.ShowCloudsAnimated(2, "WeatherMakerCloudProfile_None");
        }
    }

    private string GetGlobalIPAddress()
    {
        var url = "https://api.ipify.org/";

        WebRequest request = WebRequest.Create(url);
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        Stream dataStream = response.GetResponseStream();

        using StreamReader reader = new StreamReader(dataStream);

        var ip = reader.ReadToEnd();
        reader.Close();

        return ip;
    }

    IEnumerator GetIPLocation (string _ipAddress)
    {
        string requestURL = "http://api.ipstack.com/" + _ipAddress + "?access_key=" + APITokenIP + "&fields=latitude,longitude";

        UnityWebRequest ipRequest = UnityWebRequest.Get(requestURL);

        yield return ipRequest.SendWebRequest();

        if (ipRequest.result == UnityWebRequest.Result.ConnectionError ||
        ipRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            statusText.text = $"Could not get IP: {ipRequest.error}";
        }
        else
        {
            var ipOutputJson = JSON.Parse(ipRequest.downloadHandler.text);
            SetLatitudeAndLongitude(ipOutputJson["latitude"], ipOutputJson["longitude"]);
        }
    }
}

[System.Serializable]
public class WeatherData
{
    public Weather[] weather;
    public float visibility;
    public float timezone;
    public float id;
    public string name;
    public int cod;
}

[System.Serializable]
public class Weather
{
    public string main;
}
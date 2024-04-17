using UnityEngine;

public class LoadKey : MonoBehaviour
{
    private APIKeys apiKeys = new APIKeys();

    private void Start()
    {
        LoadFromJson();
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.S))
        //{
            //SaveToJson();
        //}
    }

    public string GetIPAPI()
    {
        return apiKeys.ipLocationAPI;
    }

    public string GetWeatherAPI()
    {
        return apiKeys.weatherAPI;
    }

    private void SaveToJson()
    {
        string keyData = JsonUtility.ToJson(apiKeys);
        //string filePath = Application.persistentDataPath + "/API.json";
        string filePath = "API/API.json";

        System.IO.File.WriteAllText(filePath, keyData);
    }

    private void LoadFromJson()
    {
        string filePath = "API/API.json";
        string keyData = System.IO.File.ReadAllText(filePath);

        apiKeys = JsonUtility.FromJson<APIKeys>(keyData);
    }
}

[System.Serializable]
public class APIKeys
{
    public string weatherAPI;
    public string ipLocationAPI;
}
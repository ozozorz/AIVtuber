using UnityEngine;
using UnityEngine.UI;

public class DebugTest : MonoBehaviour
{
    public InputField inputField;

    private BliveDmService _bliveDmService;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _bliveDmService = BliveDmService.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendDm()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            return;
        }
        _bliveDmService.AddDm(inputField.text);
        inputField.text = "";
    }
}

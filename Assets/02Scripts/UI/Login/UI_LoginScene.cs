using System;
using TMPro;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;


[Serializable]
public class UI_AccountInputField
{
    public TextMeshProUGUI ResultText; // 결과 메세지
    public TMP_InputField IDInputField;
    public TMP_InputField PasswordInputField;
    public TMP_InputField ConfirmInputField;
    public Button ConfirmButton; //로그인 또는 회원가입 버튼
}

public class UI_LoginScene : MonoBehaviour
{
    private const string PREFIX  = "NiNjA_";
    private const string SALT  = "ThatMomentNinjaAppear";
    
    [Header("패널")]
    public GameObject LoginPanel;
    public GameObject RegisterPanel;
    [Header("로그인")]
    public UI_AccountInputField LoginAccountInputField;
     [Header("회원가입")]
    public UI_AccountInputField RegisterAccountInputField;

private Tweener _registerTextTweener;
private Tweener _loginTextTweener;
    private void Awake()
    {
        _registerTextTweener=   RegisterAccountInputField.ResultText.transform.DOShakeRotation(0.1f,new Vector3(1f, 0, 1f)).SetAutoKill(false)
            .Pause();
        
        _loginTextTweener=   LoginAccountInputField.ResultText.transform.DOShakeRotation(0.1f,new Vector3(1f, 0, 1f)).SetAutoKill(false)
            .Pause();
    }

    private void Start()
    {
        LoginPanel.SetActive(true);
        RegisterPanel.SetActive(false);
        LoginAccountInputField.IDInputField.text = "";
        LoginAccountInputField.PasswordInputField.text = "";
        
        LoginInputCheck();
    }

    public void OnClickToLoginButton()
    {
   
        Login();
    }

    public void OnClickRegisterButton()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(true);
    }

  
    
    public void LoginInputCheck()
    {
        string login = LoginAccountInputField.IDInputField.text;
        string password = LoginAccountInputField.PasswordInputField.text;
        LoginAccountInputField.ConfirmButton.interactable = !string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password);
    }
    
    
    public void Register()
    {
        //1. 아이디 입력을 확인
        string id = RegisterAccountInputField.IDInputField.text;
        if (string.IsNullOrEmpty(id))
        {
            RegisterAccountInputField.ResultText.text = "아이디를 입력해주십쇼";
            _registerTextTweener.Restart();
            
            return;
        }
        
        
        //2.비밀번호 입력을 확인
        string password = RegisterAccountInputField.PasswordInputField.text;
        if (string.IsNullOrEmpty(password))
        {
            RegisterAccountInputField.ResultText.text = "비밀번호를 입력해주세요.";
            _registerTextTweener.Restart();
            return;
        }
        //3. 2차 비밀번호를 확인 후 1차와 같은지 확인
        string confirmPassword = RegisterAccountInputField.ConfirmInputField.text;
        if (string.IsNullOrEmpty(confirmPassword) || password != confirmPassword)
        {
            RegisterAccountInputField.ResultText.text = "비밀번호가 다릅니다.";
            _registerTextTweener.Restart();
            return;
        }
        //4.Playerprefs를 이용하여 저장
        PlayerPrefs.SetString(PREFIX + id, Encryption(password + SALT));
        
        
        //5. 로그인 창으로 돌아가기(이 때 이미 아이디는 자동으로 입력되어있다.)
        OnClickToLoginButton();


        LoginAccountInputField.IDInputField.text = id;
        LoginAccountInputField.PasswordInputField.text = password;


    }

    public void Login()
    {
        //1. 아이디 입력을 확인
        string id = LoginAccountInputField.IDInputField.text;
        if (string.IsNullOrEmpty(id))
        {
            LoginAccountInputField.ResultText.text = "아이디를 입력해주십쇼";
            _registerTextTweener.Restart();
            
            return;
        }
        
        
        //2.비밀번호 입력을 확인
        string password = LoginAccountInputField.PasswordInputField.text;
        if (string.IsNullOrEmpty(password))
        {
            LoginAccountInputField.ResultText.text = "비밀번호를 입력해주세요.";
            _registerTextTweener.Restart();
            return;
        }
        
        //3.아이디가 와 비밀번호가 있는 건지 확인
        if (!PlayerPrefs.HasKey(PREFIX + id))
        {
            LoginAccountInputField.ResultText.text = "아이디와 비밀번호를 확인해주세요.";
            _loginTextTweener.Restart();
            return;
        }
        
        string hashedPassword = PlayerPrefs.GetString(PREFIX + id);
        if (hashedPassword != Encryption(password + SALT))
        {
            LoginAccountInputField.ResultText.text = "아이디와 비밀번호를 확인해주세요.";
            _registerTextTweener.Restart();
            return;
        }
        
        Debug.Log("로그인 성공!!!");
        SceneManager.LoadScene(1);



    }


    public string Encryption(string text)
    {
        //해시 암호화 알고리즘 인스턴스를 생성한다.
        SHA256 sha256 = SHA256.Create();
        
        //운영체제 혹은 언어 별로 string을 표현하는 방식이 다 다르므로 UTF8 버전으로 바꿔줘야한다.
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        byte[] hash = sha256.ComputeHash(bytes);
        string resultText = string.Empty;
        foreach (byte b in hash)
        {
            //byte를 다시 string으로 바꿔서 이어붙인다.
            resultText += b.ToString("x2");
            
        }
        
        return resultText;
        
    }
    
    
    
    
    // //-----------암호화------AES
    // public string EncryptAES(string textToEncrypt, string key)
    // {
    //     RijndaelManaged rijndaelCipher = GetRijndaelCipher(key);
    //     byte[] plainText = Encoding.UTF8.GetBytes(textToEncrypt);
    //     return Convert.ToBase64String(rijndaelCipher.CreateEncryptor().TransformFinalBlock(plainText, 0, plainText.Length));
    // }
    //
    // public string DecryptAES(string textToDecrypt, string key)
    // {
    //     RijndaelManaged rijndaelCipher = GetRijndaelCipher(key);
    //     byte[] encryptedData = Convert.FromBase64String(textToDecrypt);
    //     byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
    //     return Encoding.UTF8.GetString(plainText);
    // }
    //
    //
    // RijndaelManaged GetRijndaelCipher(string key)
    // {
    //     byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
    //     byte[] keyBytes = new byte[16];
    //     int len = pwdBytes.Length;
    //     if (len > keyBytes.Length) len = keyBytes.Length;
    //     Array.Copy(pwdBytes, keyBytes, len);
    //
    //     return new RijndaelManaged
    //     {
    //         Mode = CipherMode.CBC,
    //         Padding = PaddingMode.PKCS7,
    //         KeySize = 128,
    //         BlockSize = 128,
    //         Key = keyBytes,
    //         IV = keyBytes
    //     };
    // }
    
    
    
    
}

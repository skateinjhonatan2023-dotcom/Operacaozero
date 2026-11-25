using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class OZAccount : MonoBehaviour
{
    [Serializable] class Credentials { public string name, password; public bool register; }
    [Serializable] class Response { public string token, name; }
    [Serializable] class Entry { public string name; public int kills, wins; }
    [Serializable] class Entries { public Entry[] items; }

    public string Status { get; private set; } = "Sessão de convidado.";
    public string Ranking { get; private set; } = "";
    public string Token { get; private set; }
    public string Name { get; private set; }
    public bool AuthPending { get; private set; }
    bool rankingPending;

    public static bool ValidServer(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Length > 0 && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    public bool Auth(string url, string user, string password, bool register)
    {
        if (AuthPending) return false;
        if (!ValidServer(url))
        {
            Status = "Configure o servidor HTTPS em Ajustes.";
            return false;
        }
        user = user?.Trim() ?? "";
        password ??= "";
        if (user.Length < 3 || user.Length > 22 || !Array.TrueForAll(user.ToCharArray(),
                c => char.IsLetterOrDigit(c) || c == '_' || c == '-') ||
            password.Length < 8 || password.Length > 128)
        {
            Status = "Usuário de 3 a 22 caracteres e senha de 8 a 128.";
            return false;
        }
        StartCoroutine(Authenticate(url, user, password, register));
        return true;
    }

    IEnumerator Authenticate(string url, string user, string password, bool register)
    {
        AuthPending = true;
        Status = register ? "Criando conta…" : "Conectando…";
        using var request = new UnityWebRequest(url.TrimEnd('/') + "/api/auth", "POST");
        var credentials = new Credentials { name = user, password = password, register = register };
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(credentials)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 15;
        yield return request.SendWebRequest();
        AuthPending = false;
        if (request.result != UnityWebRequest.Result.Success)
        {
            Status = request.responseCode switch
            {
                401 => "Usuário ou senha inválidos.",
                409 => "Esse usuário já existe.",
                429 => "Muitas tentativas. Tente mais tarde.",
                _ => "Não foi possível conectar à conta."
            };
            yield break;
        }
        var data = Parse<Response>(request.downloadHandler.text);
        if (data == null || string.IsNullOrEmpty(data.token) || string.IsNullOrEmpty(data.name))
        {
            Status = "Resposta inválida do servidor.";
            yield break;
        }
        Token = data.token;
        Name = data.name;
        Status = "Conta conectada.";
        PlayerPrefs.SetString("OZ_SERVER", url);
        PlayerPrefs.Save();
    }

    public void SignOut()
    {
        StopAllCoroutines();
        AuthPending = rankingPending = false;
        Token = Name = null;
        Ranking = "";
        Status = "Sessão de convidado.";
    }

    public void Rank(string url)
    {
        if (rankingPending) return;
        if (!ValidServer(url))
        {
            Ranking = "Configure o servidor HTTPS em Ajustes.";
            return;
        }
        StartCoroutine(GetRank(url));
    }

    IEnumerator GetRank(string url)
    {
        rankingPending = true;
        Ranking = "Consultando ranking…";
        using var request = UnityWebRequest.Get(url.TrimEnd('/') + "/api/ranking");
        request.timeout = 15;
        yield return request.SendWebRequest();
        rankingPending = false;
        if (request.result != UnityWebRequest.Result.Success)
        {
            Ranking = "Ranking indisponível.";
            yield break;
        }

        var data = Parse<Entries>("{\"items\":" + request.downloadHandler.text + "}");
        if (data?.items == null)
        {
            Ranking = "O servidor retornou um ranking inválido.";
            yield break;
        }
        var lines = new StringBuilder();
        for (int index = 0; index < Mathf.Min(data.items.Length, 5); index++)
        {
            var entry = data.items[index];
            if (entry == null) continue;
            lines.AppendLine($"{index + 1}. {entry.name}  ·  {entry.wins} V  ·  {entry.kills} K");
        }
        Ranking = lines.Length > 0 ? lines.ToString().TrimEnd() : "Sem partidas no ranking do servidor.";
    }

    static T Parse<T>(string json) where T : class
    {
        try { return JsonUtility.FromJson<T>(json); }
        catch (ArgumentException) { return null; }
    }
}

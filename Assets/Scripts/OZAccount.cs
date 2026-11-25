using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
public sealed class OZAccount:MonoBehaviour {
 [Serializable] class Credentials{public string name,password;public bool register;} [Serializable] class Response{public string token,name,error;} [Serializable] class Entry{public string name;public int kills,wins;} [Serializable] class Entries{public Entry[] items;}
 public string Status="Sem conexão";public string Token{get;private set;}
 bool Valid(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme=="https";
 public void Auth(string url,string name,string password,bool register){if(!Valid(url)){Status="Informe um servidor HTTPS válido";return;}StartCoroutine(Authenticate(url,name,password,register));}
 IEnumerator Authenticate(string url,string name,string password,bool register){Status="Conectando…";using var r=new UnityWebRequest(url.TrimEnd('/')+"/api/auth","POST");r.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Credentials{name=name,password=password,register=register})));r.downloadHandler=new DownloadHandlerBuffer();r.SetRequestHeader("Content-Type","application/json");r.timeout=15;yield return r.SendWebRequest();if(r.result!=UnityWebRequest.Result.Success){Status="Falha de autenticação: "+r.responseCode;yield break;}var data=JsonUtility.FromJson<Response>(r.downloadHandler.text);if(data==null||string.IsNullOrEmpty(data.token)){Status="Resposta inválida do servidor";yield break;}Token=data.token;Status="Conectado: "+data.name;PlayerPrefs.SetString("OZ_SERVER",url);PlayerPrefs.Save();}
 public void Rank(string url){if(!Valid(url)){Status="Informe um servidor HTTPS válido";return;}StartCoroutine(GetRank(url));}
 IEnumerator GetRank(string url){using var r=UnityWebRequest.Get(url.TrimEnd('/')+"/api/ranking");r.timeout=15;yield return r.SendWebRequest();if(r.result!=UnityWebRequest.Result.Success){Status="Ranking indisponível";yield break;}var data=JsonUtility.FromJson<Entries>("{\"items\":"+r.downloadHandler.text+"}");if(data==null||data.items==null){Status="Ranking inválido";yield break;}Status="";for(int i=0;i<Mathf.Min(data.items.Length,5);i++){var e=data.items[i];Status+=(i+1)+". "+e.name+" | "+e.wins+" V | "+e.kills+" K\n";}}
}

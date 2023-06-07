using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using I2.Loc.SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class TwitchManager : Singleton<TwitchManager>
{
    internal Texture2D profilePicture;
    
    private string clientID = "ua8jlhti4nkasd890m44bguq8v9def";
    private string clientSecret = "ptyx8gb4afxm5e11l1ck2gmmedvk6r";
    private string accessToken = "pnohq3uy1sa2nkiek95vquaewupxx6";
    private string socketUri = "wss://irc-ws.chat.twitch.tv:443";
    private string nickName = "watchcraft";
    private string roomName;
    
    private const string PRIVMSG = "PRIVMSG";

    private Dictionary<string, string> _latestMessageInfoMap = new Dictionary<string, string>();
    private ClientWebSocket _ircClient;
    CancellationToken token = new CancellationToken();
    private bool accessTokenAvailable;
    
    /* typical message */
    //@badge-info=;
    //badges=broadcaster/1;                 --> badges in front of user
    //client-nonce=744548abbc774152af24ab91c614263a;
    //color=#0000FF;
    //display-name=thethibz;
    //emote-only=1;                         --> has only emotes
    //emotes=301544922:5-12,14-21;          --> emote:start_index-end_index(?),start_index-end_index(?)
    //first-msg=0;                          --> never posted on channel before
    //flags=;
    //id=bd5a9b3b-9db0-4d84-8d36-58b9eff33ac8;
    //mod=0;                                --> is moderator
    //returning-chatter=0;
    //room-id=22464897;
    //subscriber=0;                         --> is subscriber
    //tmi-sent-ts=1675160709912;
    //turbo=0;
    //user-id=22464897;
    //user-type= :thethibz!thethibz@thethibz.tmi.twitch.tv
    //PRIVMSG #thethibz :point ;
    
    /*private IEnumerator GetAccessToken()
    {
        accessTokenAvailable = true;
        yield break;
        //https://id.twitch.tv/oauth2/token
        //client_id=hof5gwx0su6owfnys0yan9c87zr6t&client_secret=41vpdji4e9gif29md0ouet6fktd2&grant_type=client_credentials

        var client = new HttpClient();

        var requestContent = new FormUrlEncodedContent(new [] {
            new KeyValuePair<string, string>("client_id", clientID),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        });
        var post_task = client.PostAsync(
            "https://id.twitch.tv/oauth2/token",
            requestContent);

        while( !post_task.IsCompletedSuccessfully )
        {
            yield return null;
        }
        
        HttpContent responseContent = post_task.Result.Content;

        var read_task = responseContent.ReadAsStreamAsync();
        
        while( !read_task.IsCompletedSuccessfully )
        {
            yield return null;
        }
        
        using (var reader = new StreamReader(read_task.Result ))
        {
            // {"access_token":"2j7bpqn4r4eo67dfafvn7srkmuyzl4","expires_in":4839966,"token_type":"bearer"}
            var response = JSON.Parse( reader.ReadToEnd() );
            accessToken = response[ "access_token" ];
            accessTokenAvailable = true;
        }
    }*/
/*
    private void OpenBrowser()
    {
        var scope = "chat%3Aread";
        
        var uri = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientID}&redirect_uri=http://localhost:3000&scope={scope}";
       
        //&state=c3ab8aa609ea11e793ae92361f002671
        
        Application.OpenURL( uri );
    }
*/
    private void DispatchMessage( string message )
    {
        Debug.LogWarning("<color=#0000AA>" + message + "</color>");
        // TODO send message to translator
        
        if( message.StartsWith( "PING", StringComparison.InvariantCulture ) )
        {
            StartCoroutine( SendPong() );
        }
        else if( message.Contains( ":Welcome, GLHF!" ) )
        {
            StartCoroutine( SendJoin(PlayerPrefs.GetString("account_name")) );
        }
        else if( message.Contains( "JOIN" ) )
        {
            
        }
        else if( message.Contains( "PART" ) )
        {
            
        }
        else if( message.Contains( "USERSTATE" ) )
        {
            
        }
        else if( message.Contains(PRIVMSG))
        {
            var index = message.IndexOf( PRIVMSG, StringComparison.InvariantCulture );

            var info_substring = message.Substring( 0, index - 1 );

            var info_array = info_substring.Split( ';' );
            
            foreach( string s in info_array )
            {
                var key_value_array = s.Split( '=' );
                _latestMessageInfoMap[ key_value_array[ 0 ] ] = key_value_array[ 1 ];
            }
            
            var message_substring = message.Substring( index, message.Length - index );

            var final_message_split = message_substring.Split( ':' );
            var final_message = "";
            
            for( var i = 0; i < final_message_split.Length; i++ )
            {
                if( i > 0 )
                {
                    final_message += final_message_split[ i ] + ":";
                }
            }

            final_message = final_message.Remove( final_message.Length - 1, 1 ).Trim();

            if( final_message.ToLower( CultureInfo.InvariantCulture ).Contains( "hello" ) || final_message.ToLower( CultureInfo.InvariantCulture ).Contains( "salut" ) )
            {
                StartCoroutine( SendChat( "Salut/Hello" ) );
            }
            else if( _latestMessageInfoMap["mod"] == "1" )
            {
                //StartCoroutine( SendChat( "SirUwU" ) );
            }
            else
            {
                //StartCoroutine( SendChat( (Random.Range(0.0f, 1.0f) > 0.5f) ? $"@{_latestMessageInfoMap["display-name"]} TIRE" : $"@{_latestMessageInfoMap["display-name"]} SAUTE" ) );
            }

            try
            {
                TranslatorManager.Instance.TranslateAndSend(_latestMessageInfoMap["user-id"], _latestMessageInfoMap["display-name"], final_message.ToLower(CultureInfo.InvariantCulture) );
            }
            catch( Exception e )
            {
                Debug.LogError( e );
            }

            //Debug.Log( string.IsNullOrEmpty(final_message) ? ">empty<" : final_message );
        }
        else
        {
            Debug.Log( message );
        }
    }

    private void CacheProfilePicture()
    {
        //curl -X GET 'https://api.twitch.tv/helix/users?id=141981764' -H 'Authorization: Bearer pnohq3uy1sa2nkiek95vquaewupxx6' -H 'Client-Id: ua8jlhti4nkasd890m44bguq8v9def'
        //GET https://api.twitch.tv/helix/users

        var user_request = UnityWebRequest.Get( $"https://api.twitch.tv/helix/users?login={PlayerPrefs.GetString( "account_name" )}" );
        user_request.SetRequestHeader( "Authorization", $"Bearer {accessToken}" );
        user_request.SetRequestHeader( "Client-Id", $"{clientID}" );
        
        user_request.SendWebRequest().completed += operation =>
        {
            JSONNode result = null;
            
            switch (user_request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(": Error: " + user_request.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(": HTTP Error: " + user_request.error);
                    break;
                case UnityWebRequest.Result.Success:
                    result = JSON.Parse( user_request.downloadHandler.text );
                    break;
            }

            if( result != null )
            {
                var url = result[ "data" ].Childs.First()[ "profile_image_url" ].ToString();
                var image_request = UnityWebRequest.Get( url.Substring( 1, url.Length - 2 ) );
               
                //[
                //  data,
                //[
                //{
                //"id":22464897,
                //"login":thethibz,
                //"display_name":thethibz,
                //"profile_image_url":https://static-cdn.jtvnw.net/user-default-pictures-uv/13e5fa74-defa-11e9-809c-784f43822e80-profile_image-300x300.png,
                //"view_count":73,
                //"created_at":2011-05-17T18:40:14Z}
                //]
                //]
                
                
                image_request.SendWebRequest().completed += async_operation =>
                {
                    if( image_request.result == UnityWebRequest.Result.Success )
                    {
                        profilePicture.LoadImage( image_request.downloadHandler.data );
                    }
                    else
                    {
                        Debug.LogError(image_request.error);
                    }
                };
            }
        };
    }

    public IEnumerator Connect()
    {
        if( string.IsNullOrEmpty( PlayerPrefs.GetString( "account_name" ) ) )
        {
            yield break;
        }
        
        //OpenBrowser();
        //yield return null;
        /*
        var access_coroutine = StartCoroutine( GetAccessToken() );
        while( !accessTokenAvailable )
        {
            yield return null;
        }
        */
        
        CacheProfilePicture();
        
        _ircClient = new ClientWebSocket();

        var connect_result = ConnectSocket();

        while( !connect_result.IsCompletedSuccessfully )
        {
            yield return null;
        }
        
        var update_socket = ReceiveTask();

        StartCoroutine( SendCredentials() );
        
        while( update_socket.Status == TaskStatus.Running )
        {
            yield return null;
        }
    }

    private IEnumerator SendCredentials()
    {
        string credential_1 = $"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands";
        string credential_2 = $"PASS oauth:{accessToken}";
        string credential_3 = $"NICK {nickName}";
        var sender = SendTask( $"{credential_1}", true );
        while( sender.Status == TaskStatus.Running )
        {
            yield return null;
        }
        sender = SendTask( $"{credential_2}", true );
        while( sender.Status == TaskStatus.Running )
        {
            yield return null;
        }
        sender = SendTask( $"{credential_3}", true );
        while( sender.Status == TaskStatus.Running )
        {
            yield return null;
        }
    }

    private IEnumerator SendPong()
    {
        string pong = $"PONG";
        var sender = SendTask( $"{pong}", true );
        while( sender.Status == TaskStatus.Running )
        {
            yield return null;
        }
    }

    private IEnumerator SendJoin(string room_name)
    {
        string join = $"JOIN #{room_name}";
        roomName = room_name;
        var sender = SendTask( $"{join}", true );
        while( sender.Status == TaskStatus.Running )
        {
            yield return null;
        }
    }

    private IEnumerator SendChat(string chat_message)
    {
        string chat = $"{PRIVMSG} #{roomName} :{chat_message}";
        var sender = SendTask( $"{chat}", true );
        while( sender.Status == TaskStatus.Running )
        {
            yield return null;
        }
    }
    
    private async Task ConnectSocket()
    {
        var connection_task = _ircClient.ConnectAsync(new Uri( socketUri ), token);

        await connection_task;
        
        Debug.Log( "connection_task " + connection_task.Status);
    }

    private async Task ReceiveTask()
    {
        StringBuilder message_builder = new StringBuilder();
        
        do
        {
            byte[] buffer = new byte[ 1024 ];
            
            var result = await _ircClient.ReceiveAsync( buffer, token );

            switch( result.MessageType )
            {
                case WebSocketMessageType.Text:
                {
                    message_builder.Append( System.Text.Encoding.Default.GetString( buffer, 0, result.Count ) );

                    if( result.EndOfMessage )
                    {
                        DispatchMessage( message_builder.ToString() );
                        message_builder.Clear();
                    }
                }
                    break;
                
                case WebSocketMessageType.Binary:
                {
                    // ?                    
                }
                    break;

                case WebSocketMessageType.Close:
                {
                    return;
                }
            }
        } while( !token.IsCancellationRequested );
    }

    private async Task SendTask( string message, bool is_final )
    {
        ArraySegment<byte> send_buffer = new ArraySegment<byte>( Encoding.Default.GetBytes( message ) );

        var send_task = _ircClient.SendAsync( send_buffer, WebSocketMessageType.Text, is_final, token );

        await send_task;

        switch( send_task.Status )
        {
            case TaskStatus.RanToCompletion:
                Debug.Log($"<color=#00AA00>{message}</color>");
                break;
            
            default:
                Debug.Log($"<color=#AA0000>{message}</color>");
                break;
        }
    }

    public override void Awake()
    {
        base.Awake();

        profilePicture = new Texture2D( 1, 1 );
    }

    private void OnDestroy()
    {
        _ircClient?.Dispose();
    }
}

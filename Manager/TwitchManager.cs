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

    private void DispatchMessage( string message )
    {
        Debug.LogWarning("<color=#0000AA>" + message + "</color>");
        
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

            try
            {
                TranslatorManager.Instance.TranslateAndSend(_latestMessageInfoMap["user-id"], _latestMessageInfoMap["display-name"], final_message.ToLower(CultureInfo.InvariantCulture) );
            }
            catch( Exception e )
            {
                Debug.LogError( e );
            }
        }
        else
        {
            Debug.Log( message );
        }
    }

    private void CacheProfilePicture()
    {
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

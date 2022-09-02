using BestHTTP.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionIndicator : MonoBehaviour
{
    private WebSocket webSocket = null;

    [SerializeField]
    private bool autoHide = false;

    public static Color StateOpenColor = Color.green;
    public static Color StateClosedColor = Color.red;
    public static Color StateUnknownColor = Color.grey;

    public WebSocket WebSocket
    {
        get => webSocket;
        set
        {
            // Disconnect from previous webSocket
            if (!(webSocket is null))
            {
                webSocket.OnClosed -= OnWebSocketClosed;
                webSocket.OnOpen -= OnWebSocketOpen;
                webSocket.OnError -= OnWebSocketError;
            }

            webSocket = value;

            if (!(webSocket is null))
            {
                webSocket.OnClosed += OnWebSocketClosed;
                webSocket.OnOpen += OnWebSocketOpen;
                webSocket.OnError += OnWebSocketError;
            }
        }
    }

    public bool AutoHide
    {
        get => autoHide;

        set
        {
            autoHide = value;

            SetIndication(WebSocketState);
        }
    }

    private State WebSocketState 
    { 
        get 
        {
            if (webSocket is null || webSocket.State == WebSocketStates.Unknown)
                return State.Unknown;

            return webSocket.IsOpen ? State.Open : State.Closed;
        } 
    }

    [field: SerializeField] public Image Image { get; set; }
    [field: SerializeField] public bool AutomaticallyCheckConnection { get; set; } = true;
    [field: SerializeField] public float CheckConnectionInterval { get; set; } = 1f;

    private void OnEnable()
    {
        SetIndication(WebSocketState);
        if (AutomaticallyCheckConnection)
            StartCoroutine(CheckPeriodically(CheckConnectionInterval));
    }

    // Start is called before the first frame update
    void Start()
    {
        Image ??= gameObject.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void SetIndication(State state)
    {
        if (!(Image is null))
        {
            Image.color = state switch
            {
                State.Open => StateOpenColor,
                State.Closed => StateClosedColor,
                _ => StateUnknownColor
            };

            if (autoHide)
                Image.enabled = WebSocketState != State.Open;
        }
    }

    private IEnumerator CheckPeriodically(float interval = 1f)
    {
        for (;;)
        {
            yield return new WaitForSeconds(interval);
            SetIndication(WebSocketState);
        }
    }

    #region Websocket Delegates
    private void OnWebSocketError(WebSocket webSocket, Exception ex)
    {
        SetIndication(State.Closed);

        Debug.Log($"State: {webSocket.State} | IsOpen: {webSocket.IsOpen}");
        Debug.Log(ex);
    }

    private void OnWebSocketClosed(BestHTTP.WebSocket.WebSocket webSocket, ushort code, string message)
    {
        SetIndication(State.Closed);
    }

    private void OnWebSocketOpen(BestHTTP.WebSocket.WebSocket webSocket)
    {
        SetIndication(State.Open);
    }
    #endregion

    enum State
    {
        Unknown,
        Open,
        Closed
    }
}

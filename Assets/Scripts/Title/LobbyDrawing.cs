using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class LobbyDrawing : MonoBehaviourPunCallbacks
{
    [SerializeField] RawImage rawImage;
    [SerializeField] int CanvasWidth;
    [SerializeField] int CanvasHeight;
    [SerializeField] bool isDrawable;

    Texture2D texture;
    Color drawColor;
    Color32[] clearBuffer;
    int penSize;
    Dictionary<int, Vector2Int?> lastPoints = new Dictionary<int, Vector2Int?>();


    private void Start()
    {
        texture = new Texture2D(CanvasWidth, CanvasHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        rawImage.texture = texture;
        drawColor = Color.white;
        penSize = 1;

        EnsureClearBuffer();
        ClearCanvas();
    }

    void Update()
    {
        if (!isDrawable || texture == null) return;
        if (!TryGetPixelPos(out var p)) return;

        if (isDrawable)
        {
            if (Input.GetMouseButton(0))
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("DrawAtPoint", RpcTarget.All, actorNumber, p.x, p.y, drawColor.r, drawColor.g, drawColor.b, drawColor.a, penSize);
            }

            if (Input.GetMouseButtonUp(0))
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("ResetLastPoint", RpcTarget.All, actorNumber);
            }
        }
    }

    public void SetDrawable(bool value)
    { 
        isDrawable = value;
    }

    [PunRPC]
    private void DrawAtPoint(int actorNumber, int x, int y, float r, float g, float b, float a, int size)
    {
        x = Mathf.Clamp(x, 0, texture.width - 1);
        y = Mathf.Clamp(y, 0, texture.height - 1);

        var point = new Vector2Int(x, y);
        var color = new Color(r, g, b, a);

        if (!lastPoints.ContainsKey(actorNumber))
        {
            lastPoints[actorNumber] = null;
        }

        var tempDrawer = new DrawingUtils(texture, color, size);

        if (lastPoints[actorNumber].HasValue)
        {
            tempDrawer.DrawLine(lastPoints[actorNumber].Value, point);
        }
        else
        {
            tempDrawer.DrawPoint(point);
        }
        lastPoints[actorNumber] = point;
        texture.Apply();
    }

    [PunRPC]
    private void ResetLastPoint(int actorNumber)
    {
        if (lastPoints.ContainsKey(actorNumber))
        {
            lastPoints[actorNumber] = null;
        }
    }

    private void ClearCanvas()
    {
        if (texture == null) return;

        EnsureClearBuffer();
        texture.SetPixels32(clearBuffer);
        texture.Apply();
    }

    private void EnsureClearBuffer()
    {
        int len = texture.width * texture.height;
        if (clearBuffer == null || clearBuffer.Length != len)
        {
            clearBuffer = new Color32[len];
            var t = new Color32(0, 0, 0, 255);
            for (int i = 0; i < len; i++) clearBuffer[i] = t;
        }
    }

    private bool TryGetPixelPos(out Vector2Int pixelPos)
    {
        pixelPos = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rawImage.rectTransform,
                Input.mousePosition,
                null,
                out var local))
            return false;

        Rect rect = rawImage.rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return false;

        int x = Mathf.FloorToInt((local.x - rect.x) / rect.width * texture.width);
        int y = Mathf.FloorToInt((local.y - rect.y) / rect.height * texture.height);

        if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            return false;

        pixelPos = new Vector2Int(x, y);
        return true;
    }

    private bool IsInsideCanvas(Vector2Int localPoint)
    {
        return localPoint.x >= 0 && localPoint.x < CanvasWidth && localPoint.y >= 0 && localPoint.y < CanvasHeight;
    }

    public void OnClickColorButton(int index)
    {
        drawColor = index switch
        {
            0 => Color.black,
            1 => Color.white,
            2 => Color.red,
            3 => Color.green,
            4 => Color.blue,
            5 => Color.yellow,
            6 => Color.magenta,
            7 => Color.cyan,
            8 => Color.gray,
            9 => new Color32(246, 184, 148, 255),
            _ => drawColor
        };
    }

    public void OnValueChangedPenSize(Slider slider)
    {
        penSize = Mathf.Max(1, Mathf.RoundToInt(slider.value));
    }

    public override void OnJoinedRoom()
    {
        // ホストは参加時にキャンバスを初期状態に戻す
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 1)
        { 
            ClearCanvas();
            lastPoints.Clear();
            return;
        }
        // 参加者は参加時点のキャンバス状態をホストにリクエストして受け取る
        photonView.RPC("RequestCanvasState", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RequestCanvasState(int requesterActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        byte[] raw = texture.GetRawTextureData().ToArray();

        var target = PhotonNetwork.CurrentRoom.GetPlayer(requesterActorNumber);
        photonView.RPC("ReceiveCanvasState", target, raw);
    }

    [PunRPC]
    private void ReceiveCanvasState(byte[] raw)
    {
        // 描画途中の線連結情報はリセット（途中参加での変な線を防ぐ）
        lastPoints.Clear();

        // 受け取った状態で復元
        texture.LoadRawTextureData(raw);
        texture.Apply();
    }
}

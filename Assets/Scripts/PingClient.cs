using UnityEngine;
using FishNet.Object;
using System.Collections;

public class PingClient : NetworkBehaviour
{
    private Coroutine _pingCoroutine;
    private ServerLogics _serverLogics;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            _pingCoroutine = StartCoroutine(PingServer());
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (_pingCoroutine != null)
        {
            StopCoroutine(_pingCoroutine);
            _pingCoroutine = null;
        }
    }

    private IEnumerator PingServer()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            // Mengirim ping dengan memanggil RPC pada objek ini sendiri.
            SendPingToServerRpc();
        }
    }

    [ServerRpc]
    private void SendPingToServerRpc()
    {
        // Kode ini sekarang berjalan di server.
        // Cari instance ServerLogics di server. Lakukan sekali saja untuk efisiensi.
        if (_serverLogics == null)
        {
            _serverLogics = FindObjectOfType<ServerLogics>();
        }

        // Panggil method di ServerLogics.
        if (_serverLogics != null)
        {
            // Kirim koneksi dari pengirim RPC ini.
            _serverLogics.ReceivePing(base.Owner);
        }
    }
}

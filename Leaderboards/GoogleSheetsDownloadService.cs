
using System.Net;
using MapsExt;
using MapsExt.MapObjects;
using MapsExt.Visualizers;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
namespace LopisParkourNS;
public class GoogleSheetsDownloadService
{
    private System.Uri _baseUrl;
    
    private WebClient _client;
    // private Action<string> _onDownloaded;

    public GoogleSheetsDownloadService(string formUrl)//, Action<string> onDownloaded)
    {
        if (string.IsNullOrEmpty(formUrl)) throw new ArgumentNullException(nameof(formUrl));
        _baseUrl = new System.Uri(formUrl);
        _client = new WebClient();
        // _onDownloaded = onDownloaded;
        // _client.DownloadStringCompleted += new
        //     DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
    }
    // void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
    // {
    //     if (_onDownloaded != null)
    //     {
    //         _onDownloaded(e.Result);
    //     }
    // }

    /// <summary>Submits the previously set data asynchronously and returns the response.</summary>
    /// <remarks>See https://stackoverflow.com/a/52404185/117797 for queryParams formatting details</remarks>
    public async Task<string> DownloadAsync()
    {
        LopisParkour.Log("downloading leaderboard");
        return await _client.DownloadStringTaskAsync(_baseUrl);
    }
}
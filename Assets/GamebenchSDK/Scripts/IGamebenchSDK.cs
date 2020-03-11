using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gamebench.Sdk
{
    /* *API example: v0.3        
     *  Gamebench.Sdk.IGamebenchSDK sdk = Gamebench.Sdk.GamebenchSDK.GetInstance();
     *  sdk.GetEndPointUri();
     * */

    interface IGamebenchSDK
    {
        string GetEndPointUri();
        void SetEndPointUri(string serverEndpoint);
        void SetSceneChangeAutoMarkerCreate(bool setState);
        bool GetSceneChangeAutoMarker();
    }
}

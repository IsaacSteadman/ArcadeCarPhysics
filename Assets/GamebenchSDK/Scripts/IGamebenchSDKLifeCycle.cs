using Gamebench.Sdk;
using System;
using System.Collections.Generic;

namespace Gamebench.Sdk
{
    /* *API example: v0.4        
 *  Gamebench.Sdk.IGamebenchSDKLifeCycle sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance();
 *  sdk.InitSDK();
 * */
    enum MetricType { FPS, GPU, MEM };
    enum GBStatus {
        GB_SUCCESS,
        GB_SDK_ALREADY_INITIALIZED, GB_SDK_ALREADY_STOPPED, //SDK start/stop
        GB_SESSION_ALREADY_STARTED, GB_SESSION_ALREADY_STOPPED, GB_SESSION_WITH_NONE_METRICS, //SDK session
        GB_SESSION_START_BEFORE_INITITIALIZED, GB_SESSION_STOP_AFTER_DESTROYED,
        GB_CLEAN_BEFORE_INITIALIZED, GB_CLEAN_DURING_SESSION, //SDK clean
        GB_UPLOAD_BEFORE_INITIALIZED, GB_UPLOAD_DURING_SESSION, GB_UPLOAD_NONE_FILES, //SDK upload
        GB_MARKER_BEFORE_SESSION, //SDK marker
        GB_UNKNOWN_ERROR
    }

    public delegate void UploadCallback(bool success, string errorReason = "");

    interface IGamebenchSDKLifeCycle : IGamebenchSDK, IGamebenchShared
    {
        GBStatus InitSDK();  //init sdk environment
        GBStatus StartPerformanceCapture(IDictionary<MetricType, bool> metrics=null); // performance data collecting, cannot start if all elements in metrics is false
        GBStatus StopPerformanceCapture(); //stop collect perf data
        GBStatus CleanPerformanceData(); //clean up perf data
        GBStatus UploadPerformanceData(UploadCallback callback = null); //flush perf data to server
        GBStatus DestroySDK(); //destroy sdk environment and release resources
        GBStatus StartMarker(string markerName);
        GBStatus StopMarker(string markerName);
        GBStatus ConfigCaptureFrequency(IDictionary<MetricType, float> metrics);
    }
}
